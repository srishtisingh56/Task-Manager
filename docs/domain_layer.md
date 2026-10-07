# TaskManager — Domain Layer Summary

**Status:** Complete (v1), with mid-project additions and decisions incorporated. This is the reference for Domain-layer design so Application, Infrastructure, and Presentation work does not re-derive or contradict established rules.

Pair with `project-context.md`, `application_layer.md`, `infrastructure_layer.md`, and `presentation_layer.md`.

---

## 1. Project structure

```text
TaskManager/
  TaskManager.slnx
  src/
    TaskManager.Domain/
      TaskManager.Domain.csproj   # net10.0; zero package/project references
      Common/
        Entity.cs
      Enums/
        TaskItemStatus.cs
        TaskPriority.cs
        NotificationTriggerEvent.cs
        NotificationOffsetUnit.cs
        NotificationRepeatMode.cs
        NotificationChannel.cs
        NotificationDeliveryStatus.cs
      Entities/
        User.cs
        TaskItem.cs
        NotificationRule.cs
        NotificationLog.cs
      Exceptions/
        DomainException.cs
        InvalidTaskDeadlineException.cs
        InvalidTaskStateTransitionException.cs
        TaskAlreadyDeletedException.cs
        TaskNotDeletedException.cs
        TaskAlreadyCompletedException.cs
        TaskAlreadyOverdueException.cs
        SelfManagementException.cs
        InvalidNotificationRuleException.cs
```

---

## 2. Core design principles

1. **Rich domain model.** Entity properties have `private` setters. State changes happen through business methods (`MarkCompleted()`, `Halt()`, etc.), never direct property assignment.

2. **Factories only.** Entities have no public constructors; creation uses static factories. This prevents invalid in-memory entities.

3. **Identity equality.** All entities inherit `Entity`; equality uses `Id + concrete type`. `Id` is a `Guid` generated client-side by the factory, so identity exists before `SaveChanges()`.

4. **Authorization vs. invariant split.**
   - Domain enforces rules an entity can verify from its own state.
   - Caller-aware authorization and cross-entity checks belong in Application.
   - Examples: "only the creating master can edit/delete" needs caller identity; manager-cycle detection needs other users. `User.AssignManager()` only prevents direct self-management.
   - This keeps Domain independent and unit-testable without mocks.

5. **Full-replace entity updates.** `UpdateDetails(...)` sets exactly the values passed. PATCH semantics ("omitted" vs. "explicitly cleared") are resolved in Application using the command/DTO and `Optional<T>` before calling the entity.

6. **`TaskItem`, not `Task`.** Avoids collision with `System.Threading.Tasks.Task`.

7. **Soft delete for tasks.** Required because deleted tasks can generate notifications and `NotificationLog`/`NotificationRule` retain `TaskId`. Rules are intentionally retained on soft delete so `Restore()` brings reminder configuration back. EF cascade delete only matters for manual/hard DB cleanup, not normal `TaskItem.Delete()`.

8. **Clock supplied by caller.** `TaskItem.CreatedAt` is passed to `Create(..., createdAtUtc)` by Application's `IDateTime`; Domain never calls `DateTime.UtcNow`. This keeps behavior deterministic/testable. `NotificationLog.RecordAttempt()` follows the same principle with `SentAt`.

---

## 3. Entities

### User

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | From `Entity` |
| `Name` | `string` | Required |
| `Email` | `string` | Required; pragmatic regex validation |
| `PhoneNumber` | `string` | Required; India-only, exactly 10 digits: `^d{10}$`. Application validates; DB `HasMaxLength(10)` is a backstop |
| `PasswordHash` | `string` | BCrypt hash; never plain password |
| `ManagerId` | `Guid?` | `null` = no manager/top-level master |
| `IsSystemAdmin` | `bool` | Independent of hierarchy; full read access for support/debug/demo |
| `IsActive` | `bool` | Inactive users cannot be acted on or receive notifications |

Methods:
- `Create(name, email, phoneNumber, passwordHash)`
- `UpdateContactDetails(...)`
- `AssignManager(User manager)` — throws `SelfManagementException` for self-management
- `RemoveManager()`
- `PromoteToSystemAdmin()` / `DemoteFromSystemAdmin()`
- `DeactivateUser()` / `ReactivateUser()`
- `IsDirectManagerOf(User worker)` — supports direct-only, non-transitive authorization

### TaskItem

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Identity |
| `Title` | `string` | Required |
| `Description` | `string?` | Optional |
| `Status` | `TaskItemStatus` | State machine below |
| `Priority` | `TaskPriority?` | Optional |
| `LenientDeadline` | `DateTime` | Must be `< StrictDeadline`; UTC |
| `StrictDeadline` | `DateTime` | UTC |
| `IsRepetitive` | `bool` | Informational; deliberately independent of `NotificationRule.RepeatMode` |
| `CreatedByUserId` | `Guid` | Owning master |
| `AssignedToUserId` | `Guid` | May equal creator |
| `IsDeleted` | `bool` | Soft-delete flag |
| `DeletedAt` | `DateTime?` | Soft-delete timestamp |
| `CreatedAt` | `DateTime` | UTC; supplied to factory |

**State machine**

```text
Pending  -> Completed   (MarkCompleted)
Pending  -> Halted      (Halt)
Pending  -> Overdue     (MarkOverdue; system/Hangfire)
Halted   -> Pending     (Resume)
Halted   -> Overdue     (MarkOverdue)
Overdue  -> Completed   (MarkCompleted; late completion is valid)
Completed -> terminal
```

Methods:
- `Create(..., createdAtUtc)`
- `UpdateDetails(title, description, priority)`
- `UpdateDeadlines(...)`
- `SetRepetitive(bool)`
- `Reassign(newAssignedToUserId)`
- `MarkCompleted()`, `Halt()`, `Resume()`, `MarkOverdue()`
- `Delete()` — soft delete, allowed from every status including `Completed`
- `Restore()` — restores deletion while preserving the previous status
- `IsPastStrictDeadline(asOfUtc)` / `IsPastLenientDeadline(asOfUtc)`

Rules:
- Every state-changing method first calls `EnsureNotDeleted()`; deleted tasks are inert until restored.
- `Reassign()` on `Completed` throws `TaskAlreadyCompletedException`.
- `UpdateDetails`, `UpdateDeadlines`, and `SetRepetitive` are allowed only for `Pending`/`Halted`.
  - `Completed` → `TaskAlreadyCompletedException`
  - `Overdue` → `TaskAlreadyOverdueException`
- `MarkOverdue()` is idempotent and silently does nothing for already-terminal/deleted tasks; Hangfire should already filter deleted tasks, making this defensive.
- Bulk operations are Application concerns (`DeleteTasksByStatusCommand` loops over entity methods; this is built).

### NotificationRule

One rule belongs to one task. **Immediate events never have rules; only the three offset-based events do.**

| Field | Type |
|---|---|
| `TaskId` | `Guid` |
| `TriggerEvent` | `NotificationTriggerEvent` |
| `OffsetValue` | `int?` — required and positive |
| `OffsetUnit` | `NotificationOffsetUnit?` — required |
| `RepeatMode` | `NotificationRepeatMode` |

`Channel` was deliberately removed; channel selection is Application policy (`ChannelPolicy`). The old field remains commented in source only as a future marker.

`ValidateTriggerEvent()` is shared by `Create()` and `UpdateSchedule()`:
- Trigger must be `AfterCreationOffset`, `BeforeLenientDeadline`, or `BeforeStrictDeadline`.
- `OffsetValue > 0` and `OffsetUnit` are always required.
- `Repeating` is valid only for `AfterCreationOffset`; deadline warnings are `Once`.

`CalculateFireTime(referencePointUtc)` is pure:
- `Before...` subtracts the offset.
- `After...` adds it.
- Unexpected/default trigger values throw.

**Uniqueness:** one rule per `(TaskId, TriggerEvent)`. This is intentionally **not Domain logic** because it requires sibling rows. Application checks it and the DB has a unique index.

### NotificationLog

Append-only audit record of one notification attempt.

| Field | Type |
|---|---|
| `NotificationRuleId` | `Guid?` — null for immediate/system dispatches |
| `TaskId` | `Guid` |
| `RecipientUserId` | `Guid` |
| `Channel` | `NotificationChannel` |
| `SentAt` | `DateTime` |
| `DeliveryStatus` | `NotificationDeliveryStatus` |
| `TriggerEvent` | `NotificationTriggerEvent` |

Methods:
- `RecordAttempt(...)` — factory; starts `Pending`, receives `SentAt`, `triggerEvent`, and the actual `notificationRuleId`
- `MarkSent()`
- `MarkFailed()`

The `NotificationRuleId` bug is important: it was once hardcoded to `null`, causing the offset scanner's dedupe check to fail and `Once` rules to refire. It is now correctly recorded.

Logs remain append-only; stale logs are not auto-deleted when deadlines change. They provide audit history and dedupe support for repeating/offset notifications.

---

## 4. Enums

| Enum | Values |
|---|---|
| `TaskItemStatus` | `Pending, Completed, Halted, Overdue` |
| `TaskPriority` | `Low, Medium, High, Critical` |
| `NotificationTriggerEvent` | `Created, Updated, Deleted, Completed, AfterCreationOffset, BeforeLenientDeadline, BeforeStrictDeadline, StrictDeadlinePassed` |
| `NotificationOffsetUnit` | `Minutes, Hours, Days, Weeks` |
| `NotificationRepeatMode` | `Once, Repeating` |
| `NotificationChannel` | `AppNotification, Email, Sms` |
| `NotificationDeliveryStatus` | `Pending, Sent, Failed` |

Only `AfterCreationOffset`, `BeforeLenientDeadline`, and `BeforeStrictDeadline` are valid `NotificationRule` triggers; the others are immediate/system events.

**Persistence rule:** all current enums are stored as strings using `HasConversion<string>()`. Therefore declaration order does not affect existing data. This was specifically important when `Minutes` was inserted into `NotificationOffsetUnit`. Do not assume future enums have this protection unless the same conversion is configured. `NotificationLog.TriggerEvent` was once missing this conversion and stored raw integers; it was fixed.

---

## 5. Exceptions

All Domain exceptions inherit `DomainException` → `Exception`. Presentation's global exception middleware maps Domain exceptions to HTTP 400 ProblemDetails; unexpected exceptions become 500s.

| Exception | Meaning |
|---|---|
| `InvalidTaskDeadlineException` | `LenientDeadline >= StrictDeadline` |
| `InvalidTaskStateTransitionException` | Invalid status transition |
| `TaskAlreadyDeletedException` | State-changing operation on a deleted task |
| `TaskNotDeletedException` | `Restore()` on a non-deleted task |
| `TaskAlreadyCompletedException` | Reassign/edit operation blocked by `Completed` |
| `TaskAlreadyOverdueException` | Edit operation blocked by `Overdue` |
| `SelfManagementException` | User assigned themselves as manager |
| `InvalidNotificationRuleException` | Invalid trigger/offset/repeat combination |

`ArgumentException`/`ArgumentNullException` handle basic argument-contract problems such as blank required strings, empty Guids, or null references.

These are **Application**, not Domain, exceptions but are listed because the same middleware maps them:
- `NotFoundException` → 404
- `ForbiddenAccessException` → 403
- `UnauthorizedException` → 401
- `ConflictException` → 409 (e.g. duplicate notification rule/email)

**Future cleanup note:** `TaskAlreadyCompletedException` and `TaskAlreadyOverdueException` are both "operation blocked by current status" errors rather than transition errors. They could eventually be consolidated into `InvalidTaskStateException`; this has intentionally not been done yet.

---

## 6. What is deliberately outside Domain

- **Domain events:** considered and rejected twice. Current notification design uses direct `INotificationDispatcher` calls from Task command handlers; do not add events without a new requirement.
- **Caller-aware authorization:** Application handlers/resource checks, not entities.
- **Manager-cycle detection:** Application `IManagerHierarchyService`; built.
- **PATCH/partial-update resolution:** Application `Optional<T>`; built.
- **Bulk operations:** Application; built via `DeleteTasksByStatusCommand`.
- **Notification-rule uniqueness:** Application + DB unique index; built.
- **Refresh tokens, SignalR, Redis:** not Domain concerns.
- **`TaskManager.Domain.Tests`:** still not created. This is the main outstanding Domain task; the state machine and invariants above form the test plan. Strongly recommended before frontend work progresses further.

---

## 7. Rules for future Domain changes

When adding or changing Domain code:

1. Check the invariant/authorization split first.
2. Preserve factory-only construction and caller-supplied clock values.
3. Prefer named business methods over setters.
4. Keep cross-entity/database checks out of entities.
5. Check the existing state machine before adding a transition.
6. Check `project-context.md` and the other layer docs before changing notification behavior.
7. If something appears to belong in Domain but currently lives in Application (or vice versa), flag it and discuss it rather than silently moving it. Existing decisions such as rule uniqueness, edit restrictions, cycle detection, and notification dispatch were deliberate.

# TaskManager — Domain Layer Summary

**Status:** Complete (v1). This document is the reference for everything
decided in the Domain layer so future chats building Application,
Infrastructure, and API don't have to re-derive it — and so any new
decision can be checked against what's already here instead of silently
conflicting with it.

Pair this with `project-context.md` (overall project scope/stack) — this
file is the detailed record of *how the Domain layer specifically* was
built, including reasoning that isn't in `project-context.md`.

---

## 1. Project structure so far

```
TaskManager/
  TaskManager.slnx
  src/
    TaskManager.Domain/
      TaskManager.Domain.csproj      — zero package/project references, by design
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
        InvalidTaskStateTransitionException.cs   (also holds TaskAlreadyDeletedException,
                                                    TaskNotDeletedException, TaskAlreadyCompletedException)
        SelfManagementException.cs
        InvalidNotificationRuleException.cs
```

Target framework: `net10.0` (adjust in the `.csproj` if your installed SDK differs — nothing here is version-specific).

---

## 2. Core design principles (apply these consistently in later layers)

1. **Rich domain model, not anemic.** Every entity property has a `private`
   setter. Entities are never mutated by assigning properties directly —
   only through named methods that express business intent
   (`task.MarkCompleted()`, not `task.Status = Completed`).

2. **Construction only through static factory methods** (`User.Create(...)`,
   `TaskItem.Create(...)`, `NotificationRule.Create(...)`,
   `NotificationLog.RecordAttempt(...)`). No public constructors. This
   guarantees an entity can never exist in memory in an invalid state.

3. **Identity-based equality.** All entities inherit `Entity` (see
   `Common/Entity.cs`), which implements `Equals`/`GetHashCode` based on
   `Id` + concrete type, not property values. `Id` is a `Guid`, generated
   in the factory method (client-side), not by the database — so an entity
   has a real, stable identity the moment it's created in C#, before
   `SaveChanges()` runs.

4. **The authorization/invariant split (important — will drive Application
   layer design):**
   - Entities enforce invariants **about themselves only** — rules
     checkable with data the entity already has (e.g. "lenient deadline
     must precede strict deadline," "can't complete an already-completed
     task," "can't manage yourself").
   - Entities do **not** know about the *current caller* or *other
     entities' state*. Two examples of things deliberately left out of
     Domain, to be handled in Application:
     - **"Only the creating master may reassign/edit/delete this task"**
       — needs to know who's asking → resource-based ASP.NET Core
       authorization handler.
     - **"No cycles across the whole manager hierarchy"** (A manages B
       manages A) — needs to walk other `User` rows → a check inside the
       command handler that creates/updates `ManagerId`, with repository
       access. `User.AssignManager()` only catches the single-hop case
       (self-management), which it *can* check in isolation.
   - This split is what keeps every entity trivially unit-testable with
     zero mocking.

5. **Full-replace over partial-update logic in entities.** Discussed and
   decided explicitly: methods like `TaskItem.UpdateDetails(title,
   description, priority)` always set exactly what they're given — they do
   **not** try to infer "was this field omitted vs. explicitly cleared."
   That inference (PATCH semantics: distinguishing "field not sent" from
   "field sent as empty/null") is an **Application-layer concern**, to be
   resolved in the command handler using the incoming DTO/command shape
   (e.g. an `IsDescriptionProvided` flag, or an `Optional<T>` wrapper),
   before calling the entity method with final resolved values. Putting
   that inference inside the entity was tried and reverted — it silently
   broke the ability to ever clear `TaskItem.Description` once set,
   because blank and omitted were indistinguishable. **Follow this pattern
   for any future "partial update" command handler.**

6. **`TaskItem`, not `Task`.** Avoids colliding with
   `System.Threading.Tasks.Task`.

7. **Soft delete, not hard delete**, for `TaskItem` (see §4). Decided
   because `NotificationTriggerEvent.Deleted` implies a notification needs
   to fire *after* deletion, and `NotificationLog` rows reference `TaskId`
   for audit — both break under a hard delete.

---

## 3. Entities

### `User`
| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | from `Entity` base |
| `Name` | `string` | required |
| `Email` | `string` | required, validated against a pragmatic regex (not full RFC 5322) |
| `PhoneNumber` | `string` | required |
| `ManagerId` | `Guid?` | null = no manager (top-level master) |
| `IsSystemAdmin` | `bool` | unrelated to manager hierarchy; full read access for support/debug/demo only |

Methods: `Create(Name, email, phoneNumber)` (factory) ·
`UpdateContactDetails(...)` · `AssignManager(User manager)` (throws
`SelfManagementException` if `manager.Id == this.Id`) · `RemoveManager()` ·
`PromoteToSystemAdmin()` / `DemoteFromSystemAdmin()` ·
`IsDirectManagerOf(User worker)` — predicate for the future
resource-based authorization handler; implements the **direct-only,
non-transitive** access rule from `project-context.md`.

### `TaskItem`
| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `Title` | `string` | required |
| `Description` | `string?` | optional |
| `Status` | `TaskItemStatus` | see state machine below |
| `Priority` | `TaskPriority?` | optional |
| `LenientDeadline` | `DateTime` | must be < `StrictDeadline` |
| `StrictDeadline` | `DateTime` | |
| `IsRepetitive` | `bool` | |
| `CreatedByUserId` | `Guid` | the owning master |
| `AssignedToUserId` | `Guid` | may equal `CreatedByUserId` |
| `IsDeleted` | `bool` | soft-delete flag |
| `DeletedAt` | `DateTime?` | |

**Status state machine:**
```
Pending    -> Completed   (MarkCompleted)
Pending    -> Halted      (Halt)
Pending    -> Overdue     (MarkOverdue — system/Hangfire only)
Halted     -> Pending     (Resume)
Halted     -> Overdue     (MarkOverdue)
Overdue    -> Completed   (MarkCompleted — "late but done" is valid, no double-punishment)
Completed  -> (terminal)
```

Methods: `Create(...)` (factory) · `UpdateDetails(title, description,
priority)` (full-replace, see §2.5) · `UpdateDeadlines(...)` ·
`SetRepetitive(bool)` · `Reassign(newAssignedToUserId)` (blocked on
`Completed` tasks — throws `TaskAlreadyCompletedException`, **not**
`InvalidTaskStateTransitionException`, since no status transition is
involved — this was a bug, fixed) · `MarkCompleted()` · `Halt()` ·
`Resume()` · `MarkOverdue()` (idempotent, system-only, silently no-ops on
already-terminal or deleted tasks — the query feeding the Hangfire job
should already filter `IsDeleted == false`, this is a defensive second
line) · `Delete()` (soft delete — **allowed from any status including
Completed**, per explicit requirement) · `Restore()` (status is preserved
exactly as it was at deletion time).

Every state-changing method calls a private `EnsureNotDeleted()` guard
first (throws `TaskAlreadyDeletedException`) — a deleted task is inert
until `Restore()`d.

Also: `IsPastStrictDeadline(DateTime asOfUtc)` / `IsPastLenientDeadline(...)`
— convenience predicates for the future Hangfire scan / query handlers.

**Deferred to Application layer:** bulk operations (e.g. "delete all
Pending + Completed tasks for this master") — an entity only represents
one task, so this will be a `DeleteTasksByStatusCommand` handler that
fetches matching tasks and calls `.Delete()` on each (or a bulk EF Core
`ExecuteUpdateAsync` if per-task notification triggering isn't needed for
that path).

### `NotificationRule`
**Revised mid-project (see `application_layer.md` for the full reasoning).**
Configuration for "when should this offset-based reminder fire." One rule
belongs to exactly one `TaskId`. **Immediate events no longer get a rule at
all** — they dispatch directly from the Application layer. Only the 3
offset-based trigger events may ever have a `NotificationRule`.

| Field | Type |
|---|---|
| `TaskId` | `Guid` |
| `TriggerEvent` | `NotificationTriggerEvent` — guarded to offset-based values only, see below |
| `OffsetValue` | `int?` — always required (positive) |
| `OffsetUnit` | `NotificationOffsetUnit?` — always required |
| `RepeatMode` | `NotificationRepeatMode` |

`Channel` field **removed** — channel selection is fully policy-driven now
(`ChannelPolicy` in Application), not stored per rule. The old field is
left commented out in the source intentionally, as a marker in case this
decision is revisited (e.g. if SMS or per-user channel preference returns).

`ValidateTriggerEvent` (used by both `Create` and `UpdateSchedule`) now
enforces, in one place:
- `TriggerEvent` must be one of `AfterCreationOffset`, `BeforeLenientDeadline`,
  `BeforeStrictDeadline` — anything else (an immediate/system event) throws
  `InvalidNotificationRuleException`.
- `OffsetValue` (positive) and `OffsetUnit` are always required — there's no
  more "immediate, no offset" branch since immediate events never reach this
  entity.
- `RepeatMode.Repeat` is only valid when `TriggerEvent == AfterCreationOffset`
  — the two deadline-warning triggers must be `Once`.

Methods: `Create(...)` · `UpdateSchedule(...)` ·
`CalculateFireTime(DateTime referencePointUtc)` — pure calculation turning
`OffsetValue`/`OffsetUnit` into a concrete UTC fire time relative to a
reference point the caller supplies (e.g. task's `CreatedAt` or
`StrictDeadline`); "Before" triggers subtract the offset, "After" triggers
add it. The `default` branch now throws (unreachable given the guard above)
instead of returning `null`.

### `NotificationLog`
Append-only audit record of one send attempt. Deliberately minimal editing
API — a log entry represents something that already happened.

| Field | Type |
|---|---|
| `NotificationRuleId` | `Guid?` — nullable; null for immediate/system dispatches that have no rule |
| `TaskId` | `Guid` |
| `RecipientUserId` | `Guid` |
| `Channel` | `NotificationChannel` |
| `SentAt` | `DateTime` |
| `DeliveryStatus` | `NotificationDeliveryStatus` |

Methods: `RecordAttempt(...)` (factory — starts as `Pending`) ·
`MarkSent()` · `MarkFailed()`. Two-step Pending→Sent/Failed flow exists
because async providers (SignalR hub, email/SMS gateway) don't always give
a synchronous result.

Purpose (from `project-context.md`): audit trail + duplicate-send
prevention for `Repeating` rules (check for an existing log entry in the
current window before firing again).

---

## 4. Enums

| Enum | Values |
|---|---|
| `TaskItemStatus` | `Pending, Completed, Halted, Overdue` |
| `TaskPriority` | `Low, Medium, High, Critical` |
| `NotificationTriggerEvent` | `Created, Updated, Deleted, Completed, AfterCreationOffset, BeforeLenientDeadline, BeforeStrictDeadline, StrictDeadlinePassed` — only the last 3 (offset-based) are valid on a `NotificationRule`; the rest are immediate/system events, see `application_layer.md` |
| `NotificationOffsetUnit` | `Hours, Days, Weeks` |
| `NotificationRepeatMode` | `Once, Repeating` |
| `NotificationChannel` | `AppNotification, Email, Sms` |
| `NotificationDeliveryStatus` | `Pending, Sent, Failed` |

---

## 5. Exceptions

All inherit `DomainException` (abstract base, itself inheriting `Exception`)
— the future global exception-handling middleware in the API layer should
catch `DomainException` specifically and translate it to a 400/RFC 7807
ProblemDetails response, letting unexpected exceptions surface as 500s.

| Exception | Thrown when |
|---|---|
| `InvalidTaskDeadlineException` | `LenientDeadline >= StrictDeadline` |
| `InvalidTaskStateTransitionException` | An invalid `TaskItemStatus` transition is attempted (e.g. completing an already-completed task, resuming a non-halted task) |
| `TaskAlreadyDeletedException` | Any state-changing method called on a soft-deleted task |
| `TaskNotDeletedException` | `Restore()` called on a task that isn't deleted |
| `TaskAlreadyCompletedException` | `Reassign()` called on a `Completed` task |
| `SelfManagementException` | A user assigned as their own manager |
| `InvalidNotificationRuleException` | Offset config doesn't match the trigger event's shape, or `RepeatMode.Repeating` used with a one-time trigger |

`ArgumentException`/`ArgumentNullException` are used (not custom domain
exceptions) for basic input shape problems — blank required strings, empty
Guids, null references — since those are argument-contract violations, not
business-rule violations.

---

## 6. Explicitly deferred / not yet built (by design)

- **Domain events** (e.g. publishing a `TaskCompletedEvent` from
  `MarkCompleted()`). Nothing currently requires them — notification
  dispatch is planned as a Hangfire recurring scan over `NotificationRule`s
  reading current state, not an in-process event reaction. Revisit only if
  that dispatch design changes; don't add "just in case."
  **Update:** this was reconsidered mid-project when building the
  Application-layer notification dispatch and explicitly rejected again —
  direct `INotificationDispatcher` calls from Task command handlers were
  judged the right-sized solution for the current handful of immediate
  events. See `application_layer.md` for the reasoning.
- **Cross-entity / caller-aware authorization** — see §2.4. Lives in
  Application (MediatR handlers + a resource-based ASP.NET Core
  authorization handler for the master/worker relationship check).
- **Partial-update (PATCH) resolution logic** — see §2.5. Lives in
  Application, resolved per-command before calling entity methods.
- **Bulk operations** (`DeleteTasksByStatusCommand` and similar) — Application layer, loops over single-entity domain methods.
- **Cycle detection across the manager hierarchy** — Application layer, needs repository access to walk `ManagerId` chains beyond the single-hop self-check `AssignManager()` already does.
- **`TaskManager.Domain.Tests`** (xUnit) — not yet created. Recommended next
  step alongside or before Application, to lock in the invariants and state
  machine above with tests before more layers are built on top.
- **Application, Infrastructure, API projects** — not started.

---

## 7. How to use this doc in a future chat

Paste or attach this file alongside `project-context.md`. When proposing
Application-layer code (commands, queries, handlers, repository
interfaces, the authorization handler), check new decisions against §2 and
§6 above — anything that looks like it belongs in Domain instead (a new
invariant an entity could check about itself) should be flagged rather
than implemented ad hoc in a handler, and vice versa (anything needing
caller identity or cross-entity/repository access does **not** belong back
in Domain).

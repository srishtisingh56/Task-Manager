# TaskManager — Application Layer Summary

**Status:** Functionally complete (v1). Reference for the Application-layer CQRS shape, validation, authorization, authentication flow, and notification dispatch. Pair with `project-context.md`, `domain_layer.md`, `infrastructure_layer.md`, and `presentation_layer.md`; newer decisions here supersede older references.

## 1. Project structure

```text
TaskManager.Application/
  Common/
    DependencyInjection.cs       — MediatR, FluentValidation, ValidationBehavior
    Behaviors/ValidationBehavior.cs
    Exceptions/
      ForbiddenAccessException.cs
      NotFoundException.cs
      UnauthorizedException.cs   — invalid login / maps to 401
      ConflictException.cs       — duplicate/conflict / maps to 409
  Interfaces/
    IApplicationDbContext.cs     — DbSets + SaveChangesAsync
    ICurrentUserService.cs       — Guid UserId, bool IsSystemAdmin
    IDateTime.cs                 — UTC time abstraction
    INotificationDispatcher.cs   — DispatchAsync(..., Guid? notificationRuleId = null)
    IJwtTokenGenerator.cs
    IPasswordHasher.cs
  Models/
    Optional.cs                  — PATCH support
    PagedResult.cs
  Notifications/ChannelPolicy.cs
  Auth/
    Commands/Login/
    Common/AuthResultDto.cs
  NotificationRules/
    Commands/CreateNotificationRule, UpdateSchedule, DeleteNotificationRule
    Common/NotificationRuleDto, NotificationTriggerEvents
    Queries/GetNotificationRuleById, GetNotificationRulesForTask,
           GetMyNotificationRules, GetNotificationLogs
  Tasks/
    Commands/CreateTask, UpdateTaskDetails, UpdateTaskDeadlines,
            SetRepetitiveTask, ReassignTask, DeleteTask,
            DeleteTasksByStatus, MarkTaskCompleted, HaltTask,
            ResumeTask, RestoreTask
    Common/TaskDto.cs
    Queries/GetTaskById, GetTasksForUser
  Users/
    Commands/AssignManager, CreateUser, DemoteFromSystemAdmin,
            PromoteToSystemAdmin, RemoveManager, UpdateContactDetails,
            DeactivateUser, ReactivateUser
    Common/UserDto.cs, WorkerDto.cs
    Interfaces/IManagerHierarchyService.cs
    Services/ManagerHierarchyService.cs
    Queries/GetAllUsers, GetMyDirectWorkers, GetUserById
```

Every command/query follows the same shape: request record, handler, and validator. Validation runs automatically through `ValidationBehavior`.

## 2. Core design decisions

1. **CQRS + MediatR.** One concern per request/handler/validator; handlers do not manually invoke validators.
2. **PATCH via `Optional<T>`.** Partial-update fields use `Optional<T>`. Handlers resolve with `request.Field.GetValueOrExisting(entity.Field)` before calling domain methods. Validators use `When(x => x.Field.IsSet, () => RuleFor(x => x.Field.Value)...)`.
   - `OptionalJsonConverter<T>` / factory is required in Presentation. Absent JSON fields stay `IsSet=false`; present fields, including explicit `null`, become `Some(value)`. This preserves omitted-vs-null semantics.
3. **Manual authorization per handler.** Resource ownership is checked inline; `IsSystemAdmin` is the universal override.
4. **Bulk operations reuse domain methods.** `DeleteTasksByStatus` loads matching tasks, calls `.Delete()` on each, saves once, then dispatches one `Deleted` notification per task.
5. **Hierarchy cycle detection belongs here.** `IManagerHierarchyService.WouldCreateCycleAsync` walks the `ManagerId` chain before `AssignManagerCommandHandler` calls `worker.AssignManager(manager)`.
6. **Deactivated users are checked in relevant handlers.** Assignment/reassignment and user-management operations verify `IsActive`; notification dispatch also skips deactivated recipients.

## 3. Notification model

### 3.1 Trigger categories

| Category | Events | Mechanism |
|---|---|---|
| Immediate/system | `Created`, `Updated`, `Deleted`, `Completed`, `StrictDeadlinePassed` | Direct dispatcher call from command handler or Hangfire overdue scan; no `NotificationRule` |
| Offset/scheduled | `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline` | `NotificationRule` with `OffsetValue` + `OffsetUnit`; only `AfterCreationOffset` may repeat |

`NotificationRule` and its validators reject immediate/system events.

### 3.2 Recipients

| Events | Recipient |
|---|---|
| `Created`, `Updated`, `Deleted`, `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline` | Assignee |
| `Completed`, `StrictDeadlinePassed` | Creator |

Self-assignment is allowed; no self-notification suppression.

### 3.3 Channel policy

Channels are **not user-configurable**:

| Events | Channels |
|---|---|
| `Created`, `Updated`, `Deleted` | App |
| `Completed`, `StrictDeadlinePassed` | App, Email |
| Offset triggers | App |

SMS is deferred. `NotificationRule.Channel` and `UpdateChannelCommand` were removed; old commented Channel code in `NotificationRule.cs` is intentionally retained as a future marker.

### 3.4 Dispatch

Task commands call `INotificationDispatcher` **after successful `SaveChangesAsync`**:

```text
CreateTask        → Created  → Assignee
UpdateTaskDetails → Updated  → Assignee
UpdateDeadlines   → Updated  → Assignee
SetRepetitive     → Updated  → Assignee
ReassignTask      → Updated  → new Assignee
DeleteTask        → Deleted  → Assignee
DeleteTasksByStatus → Deleted → each Assignee
MarkTaskCompleted → Completed → Creator
```

`Halt`, `Resume`, and `Restore` do not dispatch.

Domain events were considered but intentionally rejected for v1: the current surface is small and direct calls avoid extra event/interceptor machinery. Revisit only if a concrete need appears, such as notifying both old and new assignees.

### 3.5 Dispatcher contract

```csharp
Task DispatchAsync(
    Guid taskId,
    Guid recipientUserId,
    NotificationTriggerEvent triggerEvent,
    CancellationToken ct,
    Guid? notificationRuleId = null);
```

`notificationRuleId` is required for offset-rule deduplication.

Infrastructure responsibilities:
1. Resolve channels through `ChannelPolicy`.
2. Skip deactivated recipients.
3. Create one `NotificationLog` per channel before sending, including the correct nullable `NotificationRuleId`.
4. Attempt delivery and mark each log `Sent`/`Failed`.
5. Swallow notification/log-save failures so committed task writes are not turned into command failures.

The `NotificationRuleId` wiring bug was fixed after `Once` rules were observed firing every scan tick because logs had `null` rule IDs.

## 4. NotificationRules surface

| Command/Query | Behavior |
|---|---|
| `CreateNotificationRule` | Creator-only; offset events only; requires offset value/unit; repeat only for `AfterCreationOffset`; checks existing `(TaskId, TriggerEvent)` and throws `ConflictException`; DB unique index is final defense |
| `UpdateSchedule` | All fields use `Optional<T>`; cross-field repeat/trigger validation when both are supplied; `TriggerEvent` is not currently mutable |
| `DeleteNotificationRule` | Loads parent Task and checks creator before deleting; fixes the original IDOR |
| `GetNotificationRuleById` | Creator/admin only; returns NotFound if parent Task is soft-deleted |
| `GetNotificationRulesForTask` | Paginated; creator/admin only; same soft-deleted-task NotFound behavior |
| `GetMyNotificationRules` | New; paginated list of rules across caller-created, non-deleted tasks |
| `GetNotificationLogs` | New; paginated audit query, optional `TaskId`. Admin sees all; task creator sees that task's logs; recipient sees logs where they were notified. Does **not** hide logs for deleted tasks because logs are an append-only audit trail. |

## 5. Current-user abstraction and authentication

```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsSystemAdmin { get; }
}
```

Kept deliberately minimal. Handlers needing profile data fetch the current user's `User` row rather than expanding the interface.

`CurrentUserService` is implemented in **Presentation**, reading HTTP claims through `IHttpContextAccessor`. An earlier hardcoded Infrastructure placeholder was removed when JWT authentication was built.

`LoginCommandHandler`:
- Finds user by email.
- Uses the same generic `Invalid email or password.` response for missing user/wrong password.
- Rejects inactive users.
- Generates JWT and returns `AuthResultDto(Token, ExpiresAtUtc, UserDto)`.

**v1 limitation:** no refresh tokens or server-side revocation. JWT expiry is fixed at 60 minutes via `JwtSettings:ExpiryMinutes`; deactivation/demotion does not invalidate an already-issued token until expiry.

## 6. Deadline edits and stale reminders

Changing `LenientDeadline` or `StrictDeadline` does not reset an already-fired `Once` deadline reminder. Deduplication is based on whether a log already exists for that rule.

Instead, `UpdateTaskDeadlines` returns a `TaskDto.Warning` when an affected `Once` deadline rule has already fired:

```text
Deadlines changed. Existing 'before deadline' reminders already sent won't resend — delete and re-add them if you need a new reminder for the updated time.
```

Deleting the old log was rejected because `NotificationLog` is an append-only audit trail.

`Repeat` `AfterCreationOffset` rules are unaffected because they use creation/last-send timing, not deadlines.

## 7. Important bugs and lessons

1. **IDOR:** `DeleteNotificationRule` lacked ownership validation; fixed with parent-task creator check.
2. **Silent no-op:** `UpdateSchedule` resolved values but did not call `rule.UpdateSchedule(...)`; fixed.
3. **`Optional<T>` validation misuse:** unwrap with `When(IsSet, RuleFor(Value)... )`.
4. **EF projection:** static `FromEntity()` inside `.Select()` was not translatable; replaced with inline projection.
5. **Wrong user lookup:** relationship check fetched target instead of current user; fixed.
6. **Missing `NotificationRuleId` wiring:** interface parameter existed but was not passed through all layers, breaking `Once` dedupe; fixed.
7. **Missing `TriggerEvent` conversion:** `NotificationLog.TriggerEvent` initially stored enum integers instead of configured string names; fixed in Infrastructure.

Recurring lesson: bugs #2, #6, and #7 share the same shape—one-file changes requiring matching wiring/config elsewhere. Compilation did not catch them; tests are therefore the highest-value next step.

## 8. Deferred / not yet built

- Domain events + MediatR wrappers + SaveChanges interceptor.
- Per-user notification channel preferences.
- Separate old/new assignee notification events.
- Retry job for failed notifications.
- Refresh tokens.
- `TaskManager.Application.Tests` — not started; highest-priority deferred item.
- SignalR real-time push. Current App notifications are read by polling `GetNotificationLogs`; an `IRealtimeNotifier` Application boundary with Presentation/SignalR implementation was discussed but not built.

## 9. Future-chat usage

Use this with `project-context.md`, `domain_layer.md`, `infrastructure_layer.md`, and `presentation_layer.md`. For new Application code, check §§2–6 and re-check every call site/config whenever a signature, entity field, or persistence configuration changes.

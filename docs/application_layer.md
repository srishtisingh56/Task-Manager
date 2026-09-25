# TaskManager — Application Layer Summary

**Status:** In progress. This document is the reference for everything
decided in the Application layer — CQRS command/query shape, validation,
authorization, and the notification-dispatch redesign — so future chats
building Infrastructure/API don't have to re-derive it.

Pair with `project-context.md` (overall scope/stack) and `domain_layer.md`
(entity/invariant reference). Where this doc's decisions supersede
something written in those two, the other doc has been updated to point
here — check both if something looks inconsistent.

---

## 1. Project structure so far

```
TaskManager.Application/
  Common/
    DependencyInjection.cs        — AddApplication() extension: MediatR, FluentValidation, ValidationBehavior pipeline
    Behaviors/
      ValidationBehavior.cs        — runs all IValidator<TRequest> before the handler, throws FluentValidation.ValidationException
    Exceptions/
      ForbiddenAccessException.cs
      NotFoundException.cs
    Interfaces/
      IApplicationDbContext.cs    — DbSet<User/TaskItem/NotificationLog/NotificationRule> + SaveChangesAsync
      ICurrentUserService.cs      — Guid UserId, bool IsSystemAdmin (deliberately minimal, see §5)
      IDateTime.cs                — wraps DateTime.UtcNow for testability
      INotificationDispatcher.cs  — DispatchAsync(taskId, recipientUserId, triggerEvent, ct)
    Models/
      Optional.cs                 — Optional<T> struct (IsSet + Value) for PATCH-style partial updates
      PagedResult.cs               — record(Items, TotalCount, PageNumber, PageSize)
    Notifications/
      ChannelPolicy.cs             — static NotificationTriggerEvent -> IReadOnlyList<NotificationChannel> map
  NotificationRules/
    Commands/
      CreateNotificationRule/
      UpdateSchedule/
      DeleteNotificationRule/
    Common/
      NotificationRuleDto.cs
      NotificationTriggerEvents.cs — static OffsetBased HashSet, mirrors the Domain-side guard for validator use
    Queries/
      GetNotificationRuleById/
      GetNotificationRulesForTask/  — paginated (PagedResult<NotificationRuleDto>)
  Tasks/
    Commands/
      CreateTask, UpdateTaskDetails, UpdateTaskDeadlines, SetRepetitiveTask,
      ReassignTask, DeleteTask, DeleteTasksByStatus, MarkTaskCompleted,
      HaltTask, ResumeTask, RestoreTask
    Common/
      TaskDto.cs
    Queries/
      GetTaskById, GetTasksForUser (paginated)
  Users/
    Commands/
      AssignManager, CreateUser, DemoteFromSystemAdmin, PromoteToSystemAdmin,
      RemoveManager, UpdateContactDetails
    Common/
      UserDto.cs, WorkerDto.cs
      Interfaces/IManagerHierarchyService.cs
      Services/ManagerHierarchyService.cs   — cycle detection across the manager hierarchy (see domain_layer.md §6)
    Queries/
      GetAllUsers (paginated, admin-only), GetMyDirectWorkers, GetUserById
```

Every command/query follows the same 3-file shape: `XCommand.cs` (or
`XQuery.cs`, an `IRequest<TResponse>` record), `XCommandHandler.cs`
(`IRequestHandler<TCommand, TResponse>`), `XCommandValidator.cs`
(`AbstractValidator<TCommand>`, run automatically by `ValidationBehavior`).

---

## 2. Core design decisions

1. **CQRS via MediatR**, one file per concern. Validators are picked up by
   `ValidationBehavior` automatically — handlers never manually validate.

2. **PATCH semantics via `Optional<T>`.** Any partial-update command
   (`UpdateContactDetailsCommand`, `UpdateScheduleCommand`) wraps
   updatable fields in `Optional<T>`. Handlers resolve final values with
   `request.Field.GetValueOrExisting(entity.Field)` before calling the
   entity method — this is the Application-layer PATCH resolution
   `domain_layer.md` §2.5 deferred here. Validators guard `Optional<T>`
   fields with `When(x => x.Field.IsSet, () => RuleFor(x => x.Field.Value)...)`
   — **never call `.IsInEnum()`/`.Must()` directly on an `Optional<T>`
   property**, it won't compile/translate (caught twice during this
   project — see §6 pitfall list).

3. **Authorization is manual, per-handler, not attribute-based.** Every
   handler that mutates or reads a specific resource re-checks ownership
   inline (`task.CreatedByUserId != currentUser.UserId`, etc.) rather than
   relying on a generic policy. Consistent pattern across Tasks, Users,
   NotificationRules. `ICurrentUserService.IsSystemAdmin` is the universal
   override.

4. **Bulk operations loop over single-entity domain methods.**
   `DeleteTasksByStatusCommandHandler` fetches matching tasks, calls
   `.Delete()` on each, saves once, then dispatches one `Deleted`
   notification per task — per `domain_layer.md` §6.

5. **Cycle detection lives here, not in Domain.**
   `IManagerHierarchyService.WouldCreateCycleAsync` walks the `ManagerId`
   chain with repository access before `AssignManagerCommandHandler` calls
   `worker.AssignManager(manager)` (which only catches the single-hop
   self-management case).

---

## 3. Notification model — the big mid-project redesign

The original `domain_layer.md` model had **one shape** for
`NotificationRule` covering all 7 trigger events, with a user-picked
`Channel` per rule. This was revised here, after weighing against
over-engineering risk (see the reasoning trail — this doc records the
**final** decisions only).

### 3.1 Two categories of trigger

| Category | Events | Mechanism |
|---|---|---|
| **Immediate/system** | `Created`, `Updated`, `Deleted`, `Completed`, `StrictDeadlinePassed` | Dispatched directly from the relevant command handler (or Hangfire's overdue scan for `StrictDeadlinePassed`) via `INotificationDispatcher`. **No `NotificationRule` involved.** |
| **Offset-based/scheduled** | `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline` | User-configured via `NotificationRule`. Requires `OffsetValue` + `OffsetUnit`. Only `AfterCreationOffset` may repeat. |

`NotificationRule.Create`/`UpdateSchedule` guard against ever being given
an immediate-event trigger (throws `InvalidNotificationRuleException`) —
enforced once in the entity, mirrored in the two command validators via
`NotificationTriggerEvents.OffsetBased` for early/friendly feedback.

### 3.2 Recipients

| Event(s) | Recipient |
|---|---|
| `Created`, `Updated`, `Deleted`, `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline` | Assignee |
| `Completed`, `StrictDeadlinePassed` | Creator |

Self-assigned tasks follow the same flow unchanged (a user can notify
themselves — no suppression logic).

### 3.3 Channel policy — no longer user-configurable

`Common/Notifications/ChannelPolicy.cs` is a static
`NotificationTriggerEvent -> IReadOnlyList<NotificationChannel>` map:

| Event | Channels |
|---|---|
| `Created`, `Updated`, `Deleted` | App |
| `Completed`, `StrictDeadlinePassed` | App, Email |
| `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline` | App |

Consequence: `NotificationRule.Channel` was **removed** from the Domain
entity, the `Create`/`UpdateSchedule` signatures, `CreateNotificationRuleCommand`,
and `NotificationRuleDto`. The `UpdateChannelCommand` (command/handler/validator)
was **deleted entirely** — there's nothing left for a user to configure.
The old `Channel` code is left commented out in `NotificationRule.cs`
intentionally (a marker in case SMS or per-user preferences return later)
— **do not clean this up without asking**, it's a deliberate reminder, not
dead-code drift.

SMS is fully deferred — no channel policy entry targets it yet.

### 3.4 Dispatch mechanism — direct calls, not domain events

Considered and rejected: raising domain events on `TaskItem` (e.g.
`TaskCompletedEvent`), wrapping them for MediatR via a
`DomainEventNotification<T>` adapter, and publishing them from an EF Core
`SaveChanges` interceptor. This is the "correct" pattern for many
reactors/many events, but for ~5 handlers each triggering exactly one
notification, it added significant machinery (marker interface, event
classes, notification handlers, interceptor, ordering/failure-mode
questions) with no concrete pain point it solved yet.

**What was built instead:** every mutating Task command handler calls
`INotificationDispatcher.DispatchAsync(...)` directly, **after**
`SaveChangesAsync` succeeds (so a failed/dropped notification never rolls
back a committed write):

```
CreateTaskCommandHandler        → Created  → Assignee
UpdateTaskDetailsCommandHandler → Updated  → Assignee
UpdateTaskDeadlinesCommandHandler → Updated → Assignee
SetRepetitiveTaskCommandHandler → Updated  → Assignee
ReassignTaskCommandHandler      → Updated  → (new) Assignee
DeleteTaskCommandHandler        → Deleted  → Assignee
DeleteTasksByStatusCommandHandler → Deleted (once per task) → Assignee
MarkTaskCompletedCommandHandler → Completed → Creator
```

`Halt`, `Resume`, `Restore` deliberately **do not** dispatch — they aren't
in the notification event set.

**Revisit domain events only if a concrete pain point shows up** — e.g.
`Reassign` needing to notify *both* the old and new assignee, or audit
logging needing to piggyback on the same events without editing every
handler. Until then, this is intentionally the leaner option.

### 3.5 `INotificationDispatcher` responsibilities (contract, not yet implemented)

```csharp
Task DispatchAsync(Guid taskId, Guid recipientUserId, NotificationTriggerEvent triggerEvent, CancellationToken ct);
```

One call per event; the interface intentionally hides channel fan-out
from callers. The concrete Infrastructure implementation is expected to:
1. Resolve channels via `ChannelPolicy.GetChannels(triggerEvent)`.
2. Write a `NotificationLog` row per channel **before** attempting the
   send (so a failed send is still auditable/retryable later).
3. Attempt delivery per channel; call `MarkSent()`/`MarkFailed()` on the
   log entry.
4. **Swallow failures, don't throw** — the Task write is already
   committed by the time this runs; a failed notification shouldn't
   surface as an error to the original command's caller.

---

## 4. NotificationRules command/query surface

| Command/Query | Notes |
|---|---|
| `CreateNotificationRuleCommand` | Creator-only (`task.CreatedByUserId`). Rejects non-offset triggers, requires offset value/unit, enforces repeat-only-for-`AfterCreationOffset`. |
| `UpdateScheduleCommand` | All fields `Optional<T>` (PATCH). Cross-field repeat/trigger check only runs when **both** are provided in the same request — the entity's own guard is the final backstop when only one changes and the other resolves from the existing rule. |
| `DeleteNotificationRuleCommand` | No Domain method needed — deleting a rule row is a pure persistence operation, no invariant to protect (unlike `TaskItem`'s soft delete). Handler must load the parent Task and check `CreatedByUserId` — **this was originally missing and was a real IDOR vulnerability**, fixed during review. |
| `GetNotificationRuleByIdQuery` | Creator/admin only — rules are the creator's reminder config, not assignee-facing (assumption; revisit if assignees should see when they'll be reminded). |
| `GetNotificationRulesForTaskQuery` | Paginated (`PagedResult<NotificationRuleDto>`), same `PageNumber`/`PageSize` convention as `GetTasksForUserQuery`. Creator/admin only. |

---

## 5. `ICurrentUserService` — kept minimal on purpose

```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsSystemAdmin { get; }
}
```

Explicitly does **not** carry `ManagerId` or other profile fields. When
`GetUserByIdQueryHandler` needed to check "is the target user my direct
manager," the answer was to fetch the current user's own `User` row from
`IApplicationDbContext` inside the handler rather than growing this
interface — keeps the auth abstraction stable and avoids coupling it to
whatever claims a future JWT happens to carry. Handlers needing more than
identity + admin flag do one extra `FindAsync` for their own row.

---

## 6. Bugs found and fixed during Application-layer review

Recorded because the same *shapes* of bug recurred more than once — worth
checking for specifically in Infrastructure/API review passes too:

1. **Missing authorization check (IDOR).** `DeleteNotificationRuleCommandHandler`
   originally had no `ICurrentUserService` dependency at all — any
   authenticated user could delete any rule by ID. Fixed by loading the
   parent task and checking `CreatedByUserId`.
2. **Commented-out entity call, silent no-op.** `UpdateScheduleCommandHandler`
   resolved all the final field values correctly but never actually called
   `rule.UpdateSchedule(...)` — the command validated input and did
   nothing. Fixed.
3. **`Optional<T>` misuse in validators.** `IsInEnum()`/`.Must()` called
   directly on an `Optional<T>`-typed property doesn't compile (or
   silently no-ops) — must unwrap via `When(x.Field.IsSet, () => RuleFor(x => x.Field.Value)...)`.
   Hit in `UpdateScheduleCommandValidator` twice during this project.
4. **EF Core can't translate a static factory call inside `.Select()`.**
   `GetUserByIdQueryHandler` originally used
   `.Select(u => UserDto.FromEntity(u))` — throws at runtime. Fixed by
   inlining the projection: `.Select(u => new UserDto(u.Id, ...))`.
5. **Wrong entity fetched for a relationship check.** While adding "can
   view my own manager's profile" to `GetUserByIdQueryHandler`, the first
   attempt re-fetched the **target** user by `targetUser.Id` instead of
   the **current** user, making the `isMyManager` check compare the
   target's `ManagerId` to itself (always false). Fixed by fetching
   `currentUser.UserId`'s own row instead.

---

## 7. Explicitly deferred / not yet built (by design)

- **Domain events + MediatR notification wrappers + SaveChanges
  interceptor** — see §3.4. Revisit only on a concrete pain point.
- **Per-user notification channel preferences** — v1 uses one static
  policy for everyone.
- **Fine-grained Reassign events** (notifying the *old* assignee
  separately) — currently folded into the generic `Updated` event.
- **Retry job for failed notification sends** — `NotificationLog` is
  structured to support this later (log row written before send attempt),
  but no retry mechanism exists yet.
- **`TaskManager.Application.Tests`** — not yet created. Every bug in §6
  would have been caught by a handler-level test. Recommended before
  starting Infrastructure, given the recurring bug pattern.
- **Infrastructure, API projects** — not started.

---

## 8. How to use this doc in a future chat

Paste or attach alongside `project-context.md` and `domain_layer.md`. When
proposing Infrastructure-layer code (EF configurations, migrations,
`ICurrentUserService`/`IDateTime`/`INotificationDispatcher`
implementations, the Hangfire overdue-scan job), check new decisions
against §2–§5 here — especially the notification dispatch contract in §3,
since that's the piece Infrastructure will implement against directly.

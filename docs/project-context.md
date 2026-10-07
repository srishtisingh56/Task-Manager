# Project: TaskFlow (working name) — Accountability-First Task Manager

## Overview

A full-stack task management system with hierarchical delegation (master/worker relationships), escalating deadline-based notifications, and multi-channel alerts (in-app, email, SMS deferred). Built as a deep-learning portfolio project covering senior-level backend and frontend architecture, not a tutorial clone.

**Goal:** Learn production-grade patterns through a real, moderately complex domain — not to maximize feature count, but to build each piece the "right" way and understand *why*.

**Stack:** ASP.NET Core Web API (.NET 10) + React (TypeScript), PostgreSQL, Redis (planned, not yet added), SignalR (planned, not yet added), Hangfire, Docker.

**Status (as of this doc):** Backend (Domain, Application, Infrastructure, Presentation) functionally complete and manually verified end-to-end — auth, all CRUD/command endpoints, both Hangfire scan jobs, email + in-app notification dispatch. Automated test suite not yet started. Frontend not yet started. See `infrastructure_layer.md` and `presentation_layer.md` for the detailed build record of those two layers (new docs, paired with the existing `domain_layer.md` / `application_layer.md`).

---

## Core Domain Rules

### Users & Hierarchy

- Relationship-based, not fixed-role. Every `User` has a nullable `ManagerId` (self-referencing FK).
- A user can simultaneously be a "worker" (has a ManagerId) and a "master" (has other users pointing ManagerId at them).
- **Access is direct-only, non-transitive.** A master can only assign/manage tasks for users where `AssignedUser.ManagerId == CurrentUser.Id`. A master's master does NOT get automatic access to that master's workers — no chain-walking in v1.
- `IsSystemAdmin` (bool) is a separate, unrelated flag — grants full read access to all users/tasks, for support/debug/demo purposes only. Not part of the manager hierarchy.
- **`IsActive` (bool, added mid-project):** users can be deactivated/reactivated (`DeactivateUser`/`ReactivateUser` commands). Task and user commands check `IsActive` before allowing actions involving that user (e.g. can't assign a task to a deactivated user). The notification dispatcher also skips sending to a deactivated recipient. **Known gap:** a deactivated/demoted user's existing JWT access token stays valid until it naturally expires (up to 60 min) — there's no server-side token revocation. Acceptable for v1 given the short token lifetime; would need a revocation list or short-lived tokens + refresh to fully close.
- **Auth:** users authenticate with `Email` + `PasswordHash` (BCrypt). JWT access-token only — **refresh tokens were scoped out of v1**, despite being in the original stack plan below. This is a deliberate, documented deviation: access tokens expire after 60 minutes and the user must log in again. Revisit if session longevity becomes a real friction point.

### Tasks

- A master can create/assign tasks to themselves or their direct workers.
- Workers can only view task details and update status (Pending → Completed); they cannot edit, delete, or reassign tasks assigned to them.
- Only the creating master can edit/delete/reassign.
- Statuses: `Pending`, `Completed`, `Halted`, `Overdue`.
- Two deadlines per task: `LenientDeadline` (soft) and `StrictDeadline` (hard). Always stored and transmitted as UTC; no per-user timezone handling on the backend — that's a frontend-only concern (convert at display/input boundary, never on the server).
- After `LenientDeadline` passes without completion → escalating reminder notifications begin (per NotificationRule).
- After `StrictDeadline` passes without completion → task auto-transitions to `Overdue` status ("punishment") + notification to the creating master.
- `IsRepetitive` (bool) is informational/independent — it does **not** currently gate or link to `NotificationRule.RepeatMode`. These are deliberately decoupled fields; flagged as a possible future consistency rule, not built.
- **Edit restriction (added mid-project):** `UpdateDetails`, `UpdateDeadlines`, and `SetRepetitive` are now blocked when `Status` is `Completed` (throws `TaskAlreadyCompletedException`) or `Overdue` (throws `TaskAlreadyOverdueException`) — only `Pending`/`Halted` tasks can have their config edited. See `domain_layer.md` for the exception shapes.
- **Known gap:** editing a task's deadlines does not reset/invalidate `NotificationRule` `Once`-mode sends already recorded in `NotificationLog`. If a `BeforeStrictDeadline` reminder already fired under the old deadline, it will not re-fire after the deadline is moved later. Deliberate v1 decision: the API returns a `Warning` string on the `TaskDto` response when this situation is detected, telling the user to delete and re-add the affected rule manually. Auto-reset was considered and rejected — it would require deleting `NotificationLog` rows, conflicting with the log's "append-only audit trail" design principle (see `domain_layer.md` §7).
- Subtasks are explicitly out of scope for v1 (may revisit later as self-referencing `ParentTaskId`).

### Notifications

Two categories of trigger, split by whether they need user configuration:

- **Immediate/system events** — `Created`, `Updated`, `Deleted`, `Completed`, `StrictDeadlinePassed`. Fire automatically from the relevant Task command handler (or the Hangfire overdue scan for `StrictDeadlinePassed`). No `NotificationRule` involved — dispatched directly via `INotificationDispatcher`.
- **Offset-based/scheduled events** — `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline`. Require a user-created `NotificationRule` (`OffsetValue` + `OffsetUnit`: Hours/Days/Weeks/**Minutes**). Only `AfterCreationOffset` may repeat (`RepeatMode.Repeat`); the two deadline-warning triggers are always `Once`.
- **One rule per (Task, TriggerEvent) — enforced (added mid-project).** A task can have at most one `NotificationRule` per offset-based trigger event. Enforced in `CreateNotificationRuleCommandHandler` (checked before insert) and backed by a unique DB index on `(TaskId, TriggerEvent)` as a last line of defense. Creating a duplicate throws `ConflictException` → HTTP 409.
- Recipients: `Created`/`Updated`/`Deleted` and all 3 offset triggers → the assignee. `Completed`/`StrictDeadlinePassed` → the creator.
- **Channel is not user-configurable.** A static `ChannelPolicy` (Application layer) maps each trigger event to fixed channel(s) — immediate events mostly App-only, `Completed`/`StrictDeadlinePassed` get App+Email, offset triggers are App-only for v1. SMS is deferred — not wired to anything yet.
- **NotificationRule** = configuration ("when should this offset-based reminder fire"). Belongs to exactly one Task. Immediate events never get a rule.
- **NotificationLog** = actual sent record ("what was sent, when, to whom, success/fail, which trigger"). `NotificationRuleId` is nullable (null for immediate dispatches). `TriggerEvent` column added mid-project so log rows are identifiable without joining back to the (possibly-deleted) rule. Append-only by design — needed to avoid duplicate sends on repeating/once rules and for audit/debugging. Survives task soft-deletion and rule deletion intentionally (see `domain_layer.md` §7).
- Overdue scan job and offset-reminder scan job both run as Hangfire recurring jobs, currently every 1 minute (tunable; see `infrastructure_layer.md`).
- AI-generated motivational quotes sent on a user-defined interval — **not built**, still just an idea, lowest priority.

**Historical notification design (superseded, retained for context):**

The original v1 description had all seven trigger events on one `NotificationRule` shape and allowed channels to be user-configurable. This was superseded by the revised design above.

- Trigger events: Created, Updated, Deleted, Completed, AfterCreationOffset, BeforeLenientDeadline, BeforeStrictDeadline.
- Offset triggers use `OffsetValue` + `OffsetUnit` (Hours/Days/Weeks; Minutes were added later).
- `RepeatMode`: Once or Repeating.
- Channels: AppNotification (in-app/SignalR), Email, SMS — SMS/Email primarily for workers per the earlier scenarios.

**Architecture decision:** an earlier idea of using domain events + MediatR notifications for notification dispatch was rejected as overkill for v1's handful of handlers. Direct `INotificationDispatcher` calls from command handlers are used instead. Revisit only if a concrete pain point appears (e.g. many more reactors per event).

---

## Current Entity List (v1, domain-level — see `domain_layer.md` for full field-level detail)

**User**: Name, Email, PhoneNumber (10-digit India-only, validated `^\d{10}$`), PasswordHash, ManagerId (nullable, self-FK), IsSystemAdmin (bool), IsActive (bool)

**TaskItem**: Title, Description (optional), Status (enum), Priority (optional), LenientDeadline, StrictDeadline, IsRepetitive (bool), CreatedByUserId, AssignedToUserId, IsDeleted, DeletedAt, CreatedAt

**NotificationRule** (offset-based triggers only): TaskId, TriggerEvent (enum, restricted to the 3 offset-based values), OffsetValue, OffsetUnit (enum: Hours/Days/Weeks/Minutes), RepeatMode (enum). Unique per (TaskId, TriggerEvent).

**NotificationLog**: NotificationRuleId (nullable), TaskId, RecipientUserId, Channel, SentAt, DeliveryStatus (enum), TriggerEvent (enum)

> This list will evolve — treat it as the current source of truth, update it here as the schema changes across chats.

---

## Architecture & Patterns Applied

### Backend (.NET) — all items below are now built, not just planned

- **Clean Architecture**: Domain / Application / Infrastructure / Presentation (API) as separate projects, correct dependency direction (Domain has zero refs; Application → Domain; Infrastructure → Application; Presentation → Infrastructure, Application).
- **CQRS via MediatR**: separate Commands/Queries, one handler each, 3-file pattern (Command/Query, Handler, Validator) throughout.
- **Repository + Unit of Work:** **not added**. `IApplicationDbContext` + direct EF Core queries in handlers was judged sufficient; no separate repository layer was justified.
- **FluentValidation** for request validation, run automatically via a `ValidationBehavior` MediatR pipeline — handlers never validate manually.
- **DTOs everywhere at the API boundary** — never return EF entities directly.
- **JWT auth (access token only — see Users & Hierarchy above for the refresh-token deviation)**, manual per-handler authorization (not attribute/policy-based) — every handler that mutates/reads a specific resource re-checks ownership inline. `ICurrentUserService.IsSystemAdmin` is the universal override.
- **SignalR** is planned but not yet built. Current App-channel notifications are retrievable via a paginated notification-log query; the frontend will poll first and SignalR can be added later.
- **Hangfire**: two recurring jobs (overdue scan, offset-reminder scan) — the core "senior" mechanic of the project, fully working.
- **Redis**: planned but not yet added; no expensive aggregate read exists yet to justify it. When added, use it for caching expensive/frequent reads (e.g. dashboard aggregates), with explicit invalidation on writes.
- **Global exception-handling middleware** (`IExceptionHandler`), consistent ProblemDetails responses, exception→status mapping covers `NotFoundException` (404), `ForbiddenAccessException` (403), `DomainException` (400), `UnauthorizedException` (401), `ConflictException` (409), `ValidationException` (400, field-level), unhandled → 500 with a generic message (never leaks internals).
- **Structured logging**: not yet added (plain `ILogger`/console currently). Serilog deferred.
- **Pagination, filtering, sorting** should be implemented at the query/DB level, not in-memory. Pagination is currently implemented at the query/DB level (`PagedResult<T>`, `Skip`/`Take`) throughout. Continue watching for N+1 queries via `.Include()` / projection.
- **Soft delete** for `TaskItem`, with `NotificationRule`/`NotificationLog` deliberately **not** query-filtered so they remain visible/intact for soft-deleted tasks (audit trail requirement). See `domain_layer.md` §7 and `infrastructure_layer.md` for the EF query-filter reasoning.

### Explicitly deferred / not yet built

- Refresh tokens (see Users & Hierarchy above).
- SignalR live push for in-app notifications — currently App-channel notifications are retrievable via a paginated log query, polling-based from the frontend; SignalR is a planned UX upgrade, not a blocker.
- Redis caching (no expensive aggregate read exists yet to justify it).
- Serilog, CORS policy, rate limiting, health check endpoint — small, mostly deployment-readiness items, see `presentation_layer.md`.
- Automated test suite (`TaskManager.Domain.Tests`, `TaskManager.Application.Tests`) — zero tests exist currently; explicitly the next priority before frontend work goes too far, given the project's own bug history (`application_layer.md` §6) shows handler-level tests would have caught every recorded bug.
- Integration tests with `WebApplicationFactory`.
- API/React/Redis containerization beyond the currently verified Postgres Docker setup.
- GitHub Actions CI.
- Deployed target selection and deployment.

### Frontend (React + TypeScript) — not started

- TypeScript throughout, no `any` shortcuts.
- **TanStack Query (React Query)** for all server state — proper loading/error/empty states, cache invalidation, optimistic updates on task status changes.
- **Zustand** (or Redux Toolkit) for client-only state (UI state, auth session) — pick one, don't mix.
- **React Hook Form + Zod** for forms and validation, shared shape mirrors backend DTOs where reasonable.
- **Axios with interceptors** for auth token attach. Silent refresh is not possible yet given the refresh-token deviation; plan for a clean re-login flow on 401, or build refresh tokens first.
- SignalR client hook is deferred alongside backend SignalR work; plan for polling (`GET /api/notifications/logs`) first, upgrade later.
- Feature-folder structure (not one giant `components/` dump).
- Distinct, intentional visual design (not default Bootstrap/Material look) — dark-mode-first, consistent type/spacing/color system, good empty/loading states, since this project will be shown on LinkedIn.

### DevOps

- Docker Compose: Postgres running and verified. API, React, Redis containerization still pending.
- GitHub Actions CI: not yet set up.
- Deployed target: not yet chosen/started.

---

## Working Style / Instructions for Claude Across Chats

- Treat this document, `domain_layer.md`, `application_layer.md`, `infrastructure_layer.md`, and `presentation_layer.md` together as the current source of truth. If a new chat proposes a change, flag whether it conflicts with something already decided across these docs, rather than silently assuming it.
- Prioritize explaining *why* a pattern is used over just producing code — the primary goal is the user's understanding, not fastest implementation.
- Point out when a suggested feature is scope creep vs. core to the v1 goal.
- Assume basic .NET/React knowledge (small personal projects built before), not senior-level — explain non-obvious patterns (CQRS, MediatR, relationship-based auth, etc.) briefly when first introduced in a chat.
- Favor properly justified complexity over either (a) oversimplified tutorial-style code or (b) unnecessary abstraction for its own sake — this project is explicitly about learning real tradeoffs.
- When reviewing pasted code, check it against *all* these docs, not just the one most topically related — several real bugs in this project were caused by one layer's code not matching a decision recorded in another layer's doc.

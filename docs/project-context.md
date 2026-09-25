# Project: TaskFlow (working name) — Accountability-First Task Manager

## Overview
A full-stack task management system with hierarchical delegation (master/worker relationships), escalating deadline-based notifications, and multi-channel alerts (in-app, email, SMS). Built as a deep-learning portfolio project covering senior-level backend and frontend architecture, not a tutorial clone.

**Goal:** Learn production-grade patterns through a real, moderately complex domain — not to maximize feature count, but to build each piece the "right" way and understand *why*.

**Stack:** ASP.NET Core Web API (.NET 10) + React (TypeScript), SQL Server/PostgreSQL, Redis, SignalR, Hangfire, Docker.

---

## Core Domain Rules

### Users & Hierarchy
- Relationship-based, not fixed-role. Every `User` has a nullable `ManagerId` (self-referencing FK).
- A user can simultaneously be a "worker" (has a ManagerId) and a "master" (has other users pointing ManagerId at them).
- **Access is direct-only, non-transitive.** A master can only assign/manage tasks for users where `AssignedUser.ManagerId == CurrentUser.Id`. A master's master does NOT get automatic access to that master's workers — no chain-walking in v1.
- `IsSystemAdmin` (bool) is a separate, unrelated flag — grants full read access to all users/tasks, for support/debug/demo purposes only. Not part of the manager hierarchy.

### Tasks
- A master can create/assign tasks to themselves or their direct workers.
- Workers can only view task details and update status (Pending → Completed); they cannot edit, delete, or reassign tasks assigned to them.
- Only the creating master can edit/delete/reassign.
- Statuses: `Pending`, `Completed`, `Halted`, `Overdue`.
- Two deadlines per task: `LenientDeadline` (soft) and `StrictDeadline` (hard).
- After `LenientDeadline` passes without completion → escalating reminder notifications begin (per NotificationRule).
- After `StrictDeadline` passes without completion → task auto-transitions to `Overdue` status ("punishment") + notification to the creating master.
- `IsRepetitive` tasks reuse the same offset/interval defined in their NotificationRule for recurrence — no separate repeat-interval field on Task itself.
- Subtasks are explicitly out of scope for v1 (may revisit later as self-referencing `ParentTaskId`).

### Notifications
**Revised (supersedes the original single-model description below the divider).** Two categories of trigger, split by whether they need user configuration:

- **Immediate/system events** — `Created`, `Updated`, `Deleted`, `Completed`, `StrictDeadlinePassed`. Fire automatically from the relevant Task command handler (or the Hangfire overdue scan for `StrictDeadlinePassed`). No `NotificationRule` involved — dispatched directly via `INotificationDispatcher`.
- **Offset-based/scheduled events** — `AfterCreationOffset`, `BeforeLenientDeadline`, `BeforeStrictDeadline`. Require a user-created `NotificationRule` (`OffsetValue` + `OffsetUnit`, Hours/Days/Weeks). Only `AfterCreationOffset` may repeat (`RepeatMode.Repeat`); the two deadline-warning triggers are always `Once`.
- Recipients: `Created`/`Updated`/`Deleted` and all 3 offset triggers → the assignee. `Completed`/`StrictDeadlinePassed` → the creator.
- **Channel is no longer user-configurable.** A static `ChannelPolicy` (Application layer) maps each trigger event to fixed channel(s) — immediate events mostly App-only, `Completed`/`StrictDeadlinePassed` get App+Email, offset triggers are App-only for v1. SMS is deferred — not wired to anything yet.
- **NotificationRule** = configuration ("when should this offset-based reminder fire"). Belongs to exactly one Task. Immediate events never get a rule.
- **NotificationLog** = actual sent record ("what was sent, when, to whom, success/fail"), used for both immediate and scheduled dispatches. `NotificationRuleId` is nullable (null for immediate dispatches). Needed to avoid duplicate sends on repeating rules and for audit/debugging.
- AI-generated motivational quotes sent on a user-defined interval (separate lightweight feature — calls an LLM API, not core domain logic).

**⚠️ Flagged for later:** this reverses an earlier idea (briefly considered, not built) of using domain events + MediatR notifications for dispatch — rejected as overkill for v1's handful of handlers; direct `INotificationDispatcher` calls from command handlers were used instead. Revisit only if a concrete pain point appears (e.g. many more reactors per event).

---

*Original v1 description (channels were user-configurable per rule, all 7 trigger events lived on one `NotificationRule` shape) — superseded by the section above, kept for history:*
- Trigger events: Created, Updated, Deleted, Completed, AfterCreationOffset, BeforeLenientDeadline, BeforeStrictDeadline.
- Offset triggers use `OffsetValue` + `OffsetUnit` (Hours/Days/Weeks).
- `RepeatMode`: Once or Repeating.
- Channels: AppNotification (in-app/SignalR), Email, SMS — SMS/Email primarily for workers per the scenarios above.

---

## Current Entity List (v1, domain-level, no IDs/technical fields listed)

**User**: FullName, Email, PhoneNumber, ManagerId (nullable, self-FK), IsSystemAdmin (bool)

**Task**: Title, Description (optional), Status (enum), Priority (optional), LenientDeadline, StrictDeadline, IsRepetitive (bool), CreatedByUserId, AssignedToUserId

**NotificationRule** *(offset-based triggers only, see revised Notifications section above)*: TaskId, TriggerEvent (enum, restricted to the 3 offset-based values), OffsetValue, OffsetUnit (enum), RepeatMode (enum). `Channel` field removed — channel is policy-driven, not stored per rule.

**NotificationLog**: NotificationRuleId (nullable — null for immediate/system dispatches), TaskId, RecipientUserId, Channel, SentAt, DeliveryStatus (enum)

> This list will evolve — treat it as the current source of truth, update it here as the schema changes across chats.

---

## Architecture & Patterns to Apply

### Backend (.NET)
- **Clean Architecture**: Domain / Application / Infrastructure / API as separate projects with proper dependency direction (Domain has no dependencies; Infrastructure implements Application's interfaces).
- **CQRS via MediatR**: separate Commands (writes) and Queries (reads), each with its own handler. Use this to reinforce thinking about read/write differently rather than one bloated service class.
- **Repository + Unit of Work** only where it adds value on top of EF Core — avoid a pointless repository-over-DbContext wrapper; justify each abstraction.
- **FluentValidation** for request validation, kept out of controllers/handlers.
- **DTOs everywhere at the API boundary** — never return EF entities directly from endpoints.
- **JWT auth (access + refresh tokens)**, custom authorization handlers for the master/worker relationship check (not just role attributes — this needs a resource-based/relationship-based authorization handler).
- **SignalR** for real-time in-app notifications.
- **Hangfire** (or hosted background service) for scheduled/recurring deadline checks and notification dispatch — this is the core "senior" mechanic of the whole project.
- **Redis** for caching expensive/frequent reads (e.g. dashboard aggregates), with explicit invalidation on writes.
- **Global exception-handling middleware**, consistent problem-details error responses.
- **Structured logging** (Serilog).
- **xUnit + Moq** for unit tests on handlers/business rules; a few integration tests with WebApplicationFactory for critical flows (auth, task escalation).
- Pagination, filtering, sorting implemented properly at the query/DB level (not in-memory over full result sets) — watch for N+1 queries via `.Include()` / projection.

### Frontend (React + TypeScript)
- TypeScript throughout, no `any` shortcuts.
- **TanStack Query (React Query)** for all server state — proper loading/error/empty states, cache invalidation, optimistic updates on task status changes.
- **Zustand** (or Redux Toolkit — pick one, don't mix) for client-only state (UI state, auth session).
- **React Hook Form + Zod** for forms and validation, shared shape mirrors backend DTOs where reasonable.
- **Axios with interceptors** for auth token attach + silent refresh.
- SignalR client hook for real-time notification feed / task updates.
- Feature-folder structure (not one giant `components/` dump).
- Distinct, intentional visual design (not default Bootstrap/Material look) — dark-mode-first, consistent type/spacing/color system, good empty/loading states, since this project will be shown on LinkedIn.

### DevOps
- Docker Compose: API, React, DB, Redis, all containerized from week 1, not bolted on later.
- GitHub Actions CI: build, test, lint on push.
- Deployed target (Azure App Service / Render / Fly.io) for a live demo link.

---

## Working Style / Instructions for Claude Across Chats

- Treat this document as the current source of truth for domain rules, entities, and architecture decisions. If a new chat proposes a change, flag whether it conflicts with something already decided here, rather than silently assuming it.
- Prioritize explaining *why* a pattern is used over just producing code — the primary goal is the user's understanding, not fastest implementation.
- Point out when a suggested feature is scope creep vs. core to the v1 goal.
- Assume basic .NET/React knowledge (small personal projects built before), not senior-level — explain non-obvious patterns (CQRS, MediatR, relationship-based auth, etc.) briefly when first introduced in a chat.
- Favor properly justified complexity over either (a) oversimplified tutorial-style code or (b) unnecessary abstraction for its own sake — this project is explicitly about learning real tradeoffs.

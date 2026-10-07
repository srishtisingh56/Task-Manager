# TaskManager — Infrastructure Layer Summary

**Status:** Complete (v1), verified end-to-end — migrations applied, both
Hangfire jobs confirmed firing correctly, real email delivery confirmed
via Gmail SMTP. Reference for Infrastructure-layer decisions so other
chats don't re-derive them.

Pair with `project-context.md`, `domain_layer.md`, `application_layer.md`,
`presentation_layer.md`.

---

## 1. Structure

```text
TaskManager.Infrastructure/
Persistence/
ApplicationDbContext.cs
DesignTimeDbContextFactory.cs — needed for dotnet ef CLI
Configurations/ — one IEntityTypeConfiguration per entity
Services/
DateTimeService.cs — IDateTime
PasswordHasher.cs — IPasswordHasher (BCrypt, singleton)
JwtTokenGenerator.cs — IJwtTokenGenerator
JwtSettings.cs / EmailSettings.cs — bound from config
NotificationDispatcher.cs — INotificationDispatcher
Jobs/
OverdueTaskScanJob.cs
OffsetNotificationScanJob.cs
JobScheduler.cs — RegisterRecurringJobs()
DependencyInjection.cs — AddInfrastructure()
```

**`ICurrentUserService` is NOT implemented here.** It's HTTP-scoped, so it
lives in Presentation (reads JWT claims via `IHttpContextAccessor`). An
earlier hardcoded stub (`IsSystemAdmin = true` always) lived here
temporarily and was deleted once real auth was built — if it reappears,
that's a regression.

---

## 2. EF configuration — key decisions

- **All enums stored as strings** (`HasConversion<string>()`) across every
  config — makes enum declaration order irrelevant to stored data. One
  column (`NotificationLog.TriggerEvent`) briefly shipped *without* this
  conversion by mistake (stored raw ints until caught manually) — fixed.
- **No global soft-delete query filter on `TaskItem`.** Considered
  (`HasQueryFilter(t => !t.IsDeleted)`) but rejected — it conflicts with
  `NotificationRule`/`NotificationLog`'s required FK into `TaskItem`
  (EF's `RequiredNavigationWithQueryFilterWarning`), and those two tables
  need to keep seeing soft-deleted tasks for audit continuity anyway.
  **Consequence: every query against `TaskItems` must filter `!IsDeleted`
  manually — there is no automatic safety net.** This is the single most
  important thing to remember when adding new Task queries.
- **`PhoneNumber` is `HasMaxLength(10)`** (India-only mobile numbers,
  tightened from an initial `30`). This is a ceiling only — actual format
  (`^\d{10}$`) is enforced by FluentValidation in Application, not here.
- **Unique index on `NotificationRule(TaskId, TriggerEvent)`** — backs the
  Application-layer "one rule per task per trigger" check as a last line
  of defense.
- **Cascade delete on `NotificationRule.TaskId`/`NotificationLog.TaskId`
  is inert in practice** — `TaskItem.Delete()` is a soft delete, so no
  real SQL `DELETE` ever fires it. It only matters for manual DB cleanup.
- **Composite indexes** `(Status, StrictDeadline)` and
  `(Status, LenientDeadline)` added to `TaskItems` to support the two
  Hangfire scan jobs' queries.

---

## 3. Migrations

- **Provider: PostgreSQL (Npgsql)**, not SQL Server.
- A `DesignTimeDbContextFactory` is required so `dotnet ef` can build the
  context outside the app's DI container; its fallback connection string
  must match `docker-compose.yml`.
- **Approach:** dropped and recreated the DB/migration history multiple
  times during development instead of chaining incremental migrations,
  since all data was disposable test data. Current baseline is one
  consolidated `InitialCreate`. **Not the right approach once real data
  exists** — switch to proper incremental migrations (reviewed before
  applying) if this project continues past the test-data stage.

---

## 4. Hangfire — both jobs verified working

**Schedule: both run every 1 minute** (`*/1 * * * *`) — tightened from an
initial 5 minutes once `Minutes`-level `NotificationOffsetUnit` values
were added, so short offsets aren't delivered meaninglessly late.

**`OverdueTaskScanJob`:** finds `Pending`/`Halted`, non-deleted tasks past
`StrictDeadline`, calls `MarkOverdue()`, saves, *then* dispatches
`StrictDeadlinePassed` to the creator (save-before-dispatch, so a failed
notification never rolls back the status change). Confirmed working.

**`OffsetNotificationScanJob`:** loads all `NotificationRule`s, loads
matching non-deleted/non-`Completed` tasks into a dictionary (no `Task`
nav property exists on `NotificationRule`, so this is an in-memory join —
accepted scaling limitation, fine at current volume), computes each
rule's fire time via `CalculateFireTime`, and fires when due:
- `Once` mode: dedupe by checking whether any `NotificationLog` already
  has this `NotificationRuleId`.
- `Repeat` mode (only valid for `AfterCreationOffset`): next fire time is
  computed from the last log's `SentAt`, or `CreatedAt` if never sent.

**Real bug, fixed:** `FireAsync` originally called `DispatchAsync`
*without* passing `rule.Id`, so every log's `NotificationRuleId` was
always `null` — the `Once` dedupe check could never match anything, so
`Once` rules fired every single tick instead of once. Fixed by threading
`rule.Id` through as the dispatcher's final argument. Retested and
confirmed: `Once` rules now fire exactly once, `Repeat` rules wait the
full interval between fires.

Registered via `JobScheduler.RegisterRecurringJobs()`, called once from
Presentation's `Program.cs` after `app.Build()`.

---

## 5. `NotificationDispatcher`

Resolves channels via `ChannelPolicy`, skips entirely if the recipient is
null or `IsActive == false`, writes one `NotificationLog` row per channel
*before* attempting send, marks `Sent`/`Failed` per channel, and swallows
all failures — including the final `SaveChangesAsync`, which was
originally unguarded and could let a DB error escape back into the
calling command handler (fixed: wrapped in its own try/catch).

Email via `System.Net.Mail.SmtpClient` (legacy; `MailKit` recommended for
new code, not switched — acceptable at this scale). Confirmed working via
real Gmail SMTP with an App Password. SMTP send is synchronous within the
calling request/job — a known latency coupling, not fixed (would mean
enqueuing dispatch itself as a Hangfire job — deferred, not a current
problem).

---

## 6. Other services

- **`PasswordHasher`** — BCrypt, singleton. Password max length (72) in
  the validator matches BCrypt's byte limit, not arbitrary.
- **`JwtTokenGenerator`** — HS256, claims: `sub`, `email`, `jti`,
  custom `is_admin` (string `"true"`/`"false"`). Presentation sets
  `MapInboundClaims = false` so `sub` isn't silently renamed on the
  reading side — both sides must agree on raw claim names.
  `JwtSettings:Secret` must be ≥32 chars; app fails fast at startup if
  missing/short.
- **`DateTimeService`** — wraps `DateTime.UtcNow`, singleton. All
  time-dependent logic (jobs, `CreatedAt` stamping, `SentAt`) goes through
  `IDateTime` for testability — not yet exercised by any test, since none
  exist yet.

---

## 7. `AddInfrastructure` registrations

DbContext (Npgsql), `EmailSettings`/`JwtSettings` config binding,
`IDateTime`/`IPasswordHasher` (singleton), `IJwtTokenGenerator`/
`INotificationDispatcher` (scoped), Hangfire + Postgres storage +
server, both scan jobs (scoped). **No `ICurrentUserService` here** —
registered in Presentation.

**Secrets:** `ConnectionStrings:DefaultConnection` sits in
`appsettings.json` (local Docker Postgres, not treated as sensitive).
`EmailSettings:*` and `JwtSettings:Secret` are real secrets, set via
`dotnet user-secrets` against the **Presentation** project (secrets are
tied to the entry project), never committed.

Postgres runs via Docker Compose locally; Hangfire auto-creates its own
`hangfire` schema in the same database on first run.

---

## 8. Deferred / not built

Redis caching (nothing expensive to cache yet), Serilog, MailKit
migration, proper incremental migration discipline (currently
drop-and-recreate), SQL-level join in the offset scan job (currently
in-memory), `TaskManager.Infrastructure.Tests` (none exist).

---

## 9. For future chats

Two things to always re-check here: **(1)** any new query against
`TaskItems` needs an explicit `!IsDeleted` filter — there's no global
filter catching this automatically (§2). **(2)** whenever an interface
signature changes (like `INotificationDispatcher` did), verify *every*
call site and implementation was updated — the dedupe bug in §4 happened
because one call site was missed and it still compiled fine.
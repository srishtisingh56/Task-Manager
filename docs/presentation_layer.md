# TaskManager — Presentation Layer Summary

**Status:** Complete (v1), verified end-to-end via Postman — login, all
CRUD/command endpoints, authorization checks, PATCH semantics, and
notification dispatch all manually confirmed working. Reference for
Presentation-layer decisions so other chats don't re-derive them.

Pair with `project-context.md`, `domain_layer.md`, `application_layer.md`,
`infrastructure_layer.md`.

---

## 1. Structure

```text
TaskManager.Presentation/ (entry project — this is "Presentation")
Controllers/
AuthController.cs
UsersController.cs
TasksController.cs
NotificationRulesController.cs — includes GetMyNotificationRules, GetNotificationLogs
Middleware/
GlobalExceptionHandler.cs — IExceptionHandler
Services/
CurrentUserService.cs — ICurrentUserService, reads JWT claims
Json/
OptionalJsonConverter.cs
OptionalJsonConverterFactory.cs
Startup/
DbSeeder.cs — dev-only, seeds 3 users if none exist
Program.cs
appsettings.json
```

**`NotificationsController` (standalone `GET /api/notifications/me`) was
dropped.** Its backing query (`GetMyNotificationsQuery`) was never built —
superseded by `GetNotificationLogsQuery` under `NotificationRulesController`,
which covers the same need plus more (see `application_layer.md` §4).

---

## 2. Auth

JWT access-token only (no refresh — see `project-context.md` for the
deliberate deviation from the original plan).

- `POST /api/auth/login` → `AuthController`, `[AllowAnonymous]`, returns
  `AuthResultDto { Token, ExpiresAtUtc, User }`.
- **Deny-by-default**: `AuthorizeFilter` added globally in
  `AddControllers`, so every endpoint requires a valid token unless
  explicitly marked `[AllowAnonymous]`.
- `JwtBearer` configured with `MapInboundClaims = false` — keeps `sub` as
  `sub` instead of being silently renamed to `ClaimTypes.NameIdentifier`,
  so `CurrentUserService` can read it directly.
- `JwtSettings:Secret` is validated at startup (must exist, ≥32 chars) —
  fails fast with a clear exception instead of a confusing error on first
  login attempt.

**`CurrentUserService` (real implementation, lives here not Infrastructure):**
```csharp
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id : throw new UnauthorizedException("No authenticated user.");

    public bool IsSystemAdmin =>
        accessor.HttpContext?.User.FindFirst("is_admin")?.Value == "true";
}
```
Throws rather than defaulting to `Guid.Empty` — a missing identity can
never silently match a real row. Replaced an earlier Infrastructure-layer
hardcoded stub (`IsSystemAdmin = true` always) once this was built — see
`infrastructure_layer.md` §1.

**No endpoint exists yet to create the very first user** without already
having a token. Resolved via `DbSeeder` (below), not a special
unauthenticated bootstrap endpoint.

---

## 3. Global exception handling

`GlobalExceptionHandler : IExceptionHandler`, registered via
`AddExceptionHandler<T>()` + `AddProblemDetails()`, `app.UseExceptionHandler()`
placed first in the pipeline.

Maps: `ValidationException` → 400 (field-level `ValidationProblemDetails`),
`NotFoundException` → 404, `ForbiddenAccessException` → 403,
`DomainException` → 400, `UnauthorizedException` → 401,
`ConflictException` → 409, anything else → 500 with a generic message
(real exception never leaked; full detail still logged server-side).

---

## 4. `Optional<T>` JSON converter

Required for PATCH endpoints (`UpdateContactDetails`, `UpdateSchedule`) to
correctly distinguish "field omitted" from "field explicitly sent as
null" per the domain's PATCH design (`domain_layer.md` §2.5).

```csharp
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        => Optional<T>.Some(JsonSerializer.Deserialize<T>(ref reader, o)!); // reaching Read at all means the field WAS present

    public override void Write(Utf8JsonWriter w, Optional<T> v, JsonSerializerOptions o)
    {
        if (v.IsSet) JsonSerializer.Serialize(w, v.Value, o);
        else w.WriteNullValue();
    }
}
```
Registered via `.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory()))`.

**Real bug, fixed:** the converter was written and present in the project
for a while but never actually registered in `AddJsonOptions` — so it
silently did nothing, and PATCH requests failed with a confusing
"could not be converted to Optional`1[...]" error that looked like a data
problem but was actually a missing wire-up. Same bug *shape* as the
`INotificationDispatcher` issue in Infrastructure — a piece built but
never connected.

Also registered: `JsonStringEnumConverter()`, so enums serialize as
readable names in API responses/requests, not raw ints.

---

## 5. Controllers — pattern

Thin throughout: bind request, `mediator.Send(...)`, return. No logic in
controllers. PATCH-style endpoints build the command explicitly with the
route id forced in (`body with { TaskId = id }` or an equivalent `new(...)`
call), so the route always wins over anything in the body.

- `TasksController` — full CRUD + status transitions (`complete`, `halt`,
  `resume`, `restore`) + `bulk-delete`.
- `UsersController` — CRUD + `assign-manager`/`remove-manager` + 
  `promote`/`demote` + `deactivate`/`reactivate` + `me/workers`.
- `NotificationRulesController` — CRUD under `/api/tasks/{taskId}/notification-rules`
  and `/api/notification-rules/{id}`, plus `GET /api/notification-rules/me`
  (all of caller's rules) and `GET /api/notification-rules/logs` (audit
  trail, see `application_layer.md` §4 for its three-way authorization).

**Real routing bug, fixed:** an attempted `[HttpGet("/all")]` on
`TasksController` used a leading `/`, making it an *absolute* route
(`GET /all`, ignoring the controller's `api/tasks` base) instead of the
intended `GET /api/tasks/all`. Removed — `GetAll` doesn't need a separate
route at all, since its filters (`TargetUserId`, `Status`, paging) are
all `[FromQuery]`, not path segments; it was conflicting with
`GetById`'s `{id:guid}` route only because of how it was being called in
Postman (GUID placed in the path instead of as a query parameter).

---

## 6. `Program.cs` — current shape (summary, not full listing)

Order matters:
1. `AddApplication()`, `AddInfrastructure(configuration)`
2. `AddHttpContextAccessor()` + `AddScoped<ICurrentUserService, CurrentUserService>()`
3. `AddExceptionHandler` + `AddProblemDetails`
4. JWT secret validated, `AddAuthentication().AddJwtBearer(...)`, `AddAuthorization()`
5. `AddControllers(... AuthorizeFilter ...).AddJsonOptions(... enum + Optional<T> converters ...)`
6. `app.UseExceptionHandler()` → `UseHttpsRedirection()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`
7. `app.Services.RegisterRecurringJobs()` (Hangfire)
8. `/hangfire` dashboard mapped in Development only, no admin auth filter yet (flagged, not fixed — see §8)

---

## 7. Dev seeding

`DbSeeder.SeedUsersAsync()` — runs once if `Users` table is empty, creates
3 users (1 admin, 2 workers reporting to the admin) with real BCrypt
hashes via `IPasswordHasher`. Guarded to run only in `Development`.
Passwords are hardcoded dev-only values — never meant to run outside
local dev.

---

## 8. Testing notes (Postman)

- All deadlines must be sent as UTC (`...Z` suffix) — backend stores and
  compares UTC only, no timezone conversion happens server-side; that's
  entirely a frontend concern (see `project-context.md`).
- Bearer token set once at the collection level, inherited by all requests.
- Confirmed: wrong password → 401 (not 500), missing token → 401,
  cross-manager task assignment → 403, duplicate notification rule → 409,
  `Created`/`Updated`/`Completed`/`StrictDeadlinePassed` notifications all
  logged correctly with the right `TriggerEvent`/`Channel`.

---

## 9. Explicitly deferred / not yet built

- **SignalR** (`NotificationHub`, `IRealtimeNotifier` SignalR
  implementation) — interface boundary discussed (Application defines
  `IRealtimeNotifier`, Presentation implements it, since Infrastructure
  can't reference Presentation) but not built. App-channel notifications
  currently rely on the frontend polling `GetNotificationLogsQuery`.
- **CORS** — not yet configured; will be needed the moment the React app
  calls the API from a different origin.
- **Rate limiting** (e.g. on `/api/auth/login`) — not added.
- **Serilog / structured request logging** — plain default logging only.
- **Health check endpoint** — not added.
- **Hangfire dashboard auth filter** — currently open in Development with
  no restriction; must be locked down before any non-local deployment.
- **`TaskManager.Presentation.Tests`** (`WebApplicationFactory`
  integration tests) — none exist. Would be the natural place to test the
  full auth flow and authorization 403 cases end-to-end.

---

## 10. For future chats

Two patterns to watch for, both already bitten this project once each:
**(1)** a converter/service/interface can be fully written and still do
nothing if it isn't actually registered in DI or `AddJsonOptions` —
always confirm wiring, not just existence (§4). **(2)** a route attribute
starting with `/` is absolute and silently ignores the controller's base
route — easy to miss, causes confusing 404s that look like a different
bug entirely (§5).
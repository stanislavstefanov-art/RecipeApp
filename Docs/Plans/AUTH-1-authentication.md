# AUTH-1 — Implementation Plan

Reference spec: `Docs/specs/AUTH-1-authentication.md`

Bundles below are in dependency order. Backend domain → JWT pipeline → handler enforcement → Entra → seeder → React → Angular → translations → docs. Each bundle is a single commit.

---

## Bundle AUTH-1-1 — Domain & schema (≈ 60 minutes)

**Goal:** `User` aggregate, `HouseholdMember`, `HouseholdId` on `Recipe` and `Person`, EF Core configurations, migration.

Steps:
1. `Recipes.Domain/Users/`:
   - `UserId` strongly-typed id (`readonly record struct` with `New()` and `From(Guid)` guarding empty).
   - `AuthProvider` enum `{ Local = 1, Entra = 2 }`.
   - `User` aggregate root with `UserId`, `Email`, `DisplayName`, `AuthProvider`, `PasswordHash` (nullable), `EntraObjectId` (nullable), `CreatedAt`, `LastLoginAt`. Static factories `CreateLocal(email, hash, displayName)` and `CreateEntra(email, oid, displayName)`. Instance method `RecordLogin(now)` updates `LastLoginAt`.
2. `Recipes.Domain/Households/HouseholdMember.cs` — entity (not aggregate root) with `UserId`, `HouseholdId`, `JoinedAt`. Add `Household.AddUser(userId, now)` and `Household.RemoveUser(userId)` methods that mutate the new `Members` collection. The existing `Person` `Members` collection is renamed `People` to avoid the name collision.
3. `Recipes.Domain/Recipes/Recipe.cs` — add `HouseholdId` (required, immutable after creation). All `Recipe.Create(...)` factories take it.
4. `Recipes.Domain/Persons/Person.cs` — add `HouseholdId` (required, immutable). Update factories.
5. `Recipes.Infrastructure/Persistence/Configurations/`:
   - `UserConfiguration.cs` — table `Users`, `Email` indexed unique, `EntraObjectId` indexed unique-but-nullable, value conversion for `UserId`, `AuthProvider` stored as int.
   - `HouseholdMemberConfiguration.cs` — table `HouseholdMembers`, composite PK (`HouseholdId`, `UserId`), FK to `Users` and `Households` (cascade delete on Household, restrict on User).
   - Update `HouseholdConfiguration.cs` to map the new `Members` collection and rename `Members` → `People` mapping.
   - Update `RecipeConfiguration.cs` and `PersonConfiguration.cs` to map `HouseholdId` with FK + index.
6. `Recipes.Infrastructure/Persistence/RecipesDbContext.cs` — `DbSet<User> Users`, `DbSet<HouseholdMember> HouseholdMembers`. Update `IRecipesDbContext`.
7. `dotnet ef migrations add AddAuthSchema --project Backend/src/Recipes.Infrastructure --startup-project Backend/src/Recipes.Api`.

**Verification:** `dotnet build Backend/Recipes.sln` passes. The `architecture-guard` check still passes (no new `IRecipesDbContext` references in `Application`).

---

## Bundle AUTH-1-2 — JWT issuance & Local auth endpoints (≈ 90 minutes)

**Goal:** `POST /api/auth/register`, `POST /api/auth/login`, password hashing, JWT signing.

Steps:
1. `Recipes.Api/Auth/`:
   - `JwtOptions.cs` — bound to `Jwt:` config: `SigningKey`, `Issuer`, `Audience`, `LifetimeDays`.
   - `IPasswordHasher.cs` interface with `Hash(password)` and `Verify(password, stored)`. Default implementation `Pbkdf2PasswordHasher` using `Rfc2898DeriveBytes(SHA-256, 100_000)`, 16-byte salt, 32-byte hash, stored as `{base64-salt}.{base64-hash}`.
   - `IJwtIssuer.cs` interface with `Issue(User user)` returning `(string token, DateTimeOffset expiresAt)`. Default implementation uses `JsonWebTokenHandler` (`Microsoft.IdentityModel.JsonWebTokens` package) with HS256.
2. `Recipes.Application/Auth/Register/`:
   - `RegisterCommand.cs` — `(string Email, string Password, string DisplayName) -> AuthResultDto`.
   - `RegisterCommandValidator.cs` — email format, password ≥ 8 chars + ≥ 1 letter + ≥ 1 digit, displayName 1–100 chars.
   - `RegisterCommandHandler.cs` — lower-case email, lookup, throw `ConflictException` (new) on dup, hash password, create `User`, save, issue JWT, return DTO. **No** household creation here — first household is a separate flow handled in onboarding.
   - `AuthResultDto.cs` — `(string Token, DateTimeOffset ExpiresAt, AuthUserDto User)`.
3. `Recipes.Application/Auth/Login/`:
   - `LoginCommand.cs`, `LoginCommandValidator.cs` (email + password required), `LoginCommandHandler.cs` — generic 401 message on either missing user or hash mismatch (no leak of which side was wrong). Update `LastLoginAt` on success.
4. `Recipes.Api/Endpoints/AuthEndpoints.cs` — minimal-API group at `/api/auth`. `POST register`, `POST login`. Returns `AuthResultDto`. Map `ConflictException` to `409 Conflict` with `code = "EmailExists"`. Bad credentials → `401` with `code = "InvalidCredentials"`.
5. `Program.cs` — `services.Configure<JwtOptions>(...)`, `services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>()`, `services.AddSingleton<IJwtIssuer, JwtIssuer>()`. Register the auth endpoints group.

**Verification:** `dotnet test`. Manually:
```bash
curl -X POST http://localhost:5106/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@test.com","password":"hunter22x","displayName":"Alice"}'
```
returns a JWT; second call returns `409`. `login` with the same creds returns the same DTO shape; wrong password returns `401`.

---

## Bundle AUTH-1-3 — ASP.NET auth pipeline + `ICurrentUser` (≈ 60 minutes)

**Goal:** Bearer authentication wired, every endpoint requires it (with explicit anonymous opt-in), `ICurrentUser` available to handlers.

Steps:
1. `Program.cs`:
   - `services.AddAuthentication("Bearer").AddJwtBearer(...)` with `TokenValidationParameters` that match `JwtOptions`, `ValidateIssuer/Audience/Lifetime/SigningKey` all true.
   - `services.AddAuthorization()`.
   - `app.UseAuthentication(); app.UseAuthorization();` between `UseCors` and the endpoint group registrations.
2. `Recipes.Application/Common/ICurrentUser.cs` — interface:
   ```csharp
   public interface ICurrentUser
   {
       UserId UserId { get; }
       Task<IReadOnlyList<HouseholdId>> GetHouseholdIdsAsync(CancellationToken ct);
       void InvalidateHouseholdCache();
   }
   ```
3. `Recipes.Api/Auth/CurrentUser.cs` — `IHttpContextAccessor`-backed implementation. Reads `sub` claim, throws `UnauthorizedAccessException` if missing (should never happen behind `[Authorize]`). `GetHouseholdIdsAsync` uses `IMemoryCache` (already in stack) keyed by `userId` with 60-second TTL, falling back to a query against `HouseholdMembers`.
4. `Recipes.Api/Endpoints/RecipesEndpoints.cs` (and the others — meal-plans, households, persons, shopping-lists, expenses) — apply `.RequireAuthorization()` at the group level. Auth endpoints stay anonymous via `.AllowAnonymous()`. `/api/health` stays anonymous.
5. New endpoint `GET /api/auth/me` — `[Authorize]`. Returns `{ user: AuthUserDto, households: [{ id, name }] }`. Implemented via `MeQueryHandler` that reads from `HouseholdMembers` joined to `Households` — no other side-effects.

**Verification:** Hit `/api/recipes` without a token → `401`. With a token issued by `register`/`login` → `200 []` (still empty because no household scoping yet — that's the next bundle). `/api/auth/me` returns the user and an empty households array.

---

## Bundle AUTH-1-4 — Household-scoped handlers (≈ 90 minutes)

**Goal:** Existing query/command handlers filter or validate against the caller's household memberships. Cross-household reads return 404.

Steps:
1. **Households**:
   - `GetHouseholdsQueryHandler` — filter by `Households.Where(h => h.Members.Any(m => m.UserId == currentUser.UserId))`.
   - `GetHouseholdByIdQueryHandler` — same filter; if not found, return null → endpoint returns 404. (No 403 vs 404 distinction.)
   - `CreateHouseholdCommandHandler` — after creating the household, immediately add a `HouseholdMember(currentUser.UserId, household.Id, now)`. Also call `currentUser.InvalidateHouseholdCache()` so the next call sees the new membership without waiting for TTL.
   - `AddPersonToHouseholdCommandHandler` and `RemovePersonFromHouseholdCommandHandler` (existing) — validate caller is a member of the target household.
2. **Recipes**:
   - `CreateRecipeCommand` gains a `HouseholdId` field; validator checks it's non-empty and current user is a member. Handler passes it to `Recipe.Create(...)`.
   - `GetRecipesQueryHandler` — filter by recipes whose `HouseholdId` is in the caller's set.
   - `GetRecipeByIdQueryHandler` — same; null → 404.
   - All mutating recipe handlers (`UpdateRecipeName`, `AddIngredient`, `AddStep`, `DeleteRecipe`) — load the recipe, verify caller is in the recipe's household, else 404.
3. **Persons** — same: `CreatePersonCommand` gains `HouseholdId`. Listing filters by membership.
4. **Meal plans, shopping lists, expenses** — already carry `HouseholdId`. Add the membership check to every handler. Reuse a `RequireHouseholdMembership(householdId)` helper on `ICurrentUser` that throws `ForbiddenAccessException` (mapped to 404 at the endpoint layer for read paths and 403 for write paths — see exception mapping in step 5).
5. `Recipes.Api/Common/ExceptionMapping.cs` (or extend the existing handler) — `ConflictException → 409`, `ForbiddenAccessException → 403` (for explicit POST/PUT/DELETE) or → 404 for GET. Confirm each endpoint's exception filter selects the right one.

**Verification:**
- User A registers, creates household H1, creates recipe R. `GET /api/recipes` returns [R].
- User B registers, sees `GET /api/recipes` → []. `GET /api/recipes/{R.id}` → 404. `POST /api/recipes` with `householdId: H1.id` → 403.
- `architecture-guard` still green (every new `*Command.cs` has a matching `*Validator.cs`).

---

## Bundle AUTH-1-5 — Entra exchange endpoint (≈ 60 minutes)

**Goal:** `POST /api/auth/entra/exchange` validates an Entra `id_token` and issues an app JWT.

Steps:
1. `Recipes.Api/Auth/EntraOptions.cs` — bound to `Entra:` config: `Enabled` (bool), `TenantId`, `ClientId`. The `Enabled` flag short-circuits the endpoint to `404 Not Found` so dev environments without Entra don't 500.
2. `Recipes.Api/Auth/IEntraTokenValidator.cs` — interface with `Task<EntraIdentity?> ValidateAsync(string idToken, CancellationToken ct)` returning `(string Email, Guid ObjectId, string DisplayName)?` or null.
3. `Recipes.Api/Auth/EntraTokenValidator.cs` — implementation:
   - Configures `JwtBearerHandler` `TokenValidationParameters` with `OpenIdConnectConfigurationRetriever` pointing at the Entra discovery endpoint.
   - Validates audience = `Entra:ClientId`, issuer = `https://login.microsoftonline.com/{tenant}/v2.0`.
   - Returns null on any failure (signature, audience, issuer, expiry).
4. `Recipes.Application/Auth/EntraExchange/EntraExchangeCommand.cs`, `Validator`, `Handler`:
   - Validator: idToken non-empty, max 8 KiB.
   - Handler: validate token via `IEntraTokenValidator`. On null → throw `UnauthorizedException`. Then reconciliation per the spec: lookup by `EntraObjectId`, fallback by email, throw `ConflictException("AuthProviderMismatch")` if Local user with same email exists, otherwise create new Entra user. Issue JWT.
5. `AuthEndpoints.cs` — `POST /entra/exchange` mapped, returns 404 when `EntraOptions.Enabled == false`.

**Verification:** With `Entra:Enabled=true` plus a valid `Entra:TenantId` / `Entra:ClientId`, manually exchange an `id_token` from the MSAL flow on the frontend and get back a working app JWT. Bad token → 401. Conflicting email → 409 with code `AuthProviderMismatch`. With `Enabled=false` the endpoint returns 404.

---

## Bundle AUTH-1-6 — Seeder updates (≈ 30 minutes)

**Goal:** Local-dev seed creates the `demo@local` user, attaches all seeded households / recipes / etc. to them.

Steps:
1. `Recipes.Infrastructure/Persistence/Seeding/DemoSeeder.cs` (existing M3 seeder — find and edit):
   - Phase 0: insert `User.CreateLocal("demo@local", hash("demo1234"), "Demo User")`.
   - Phase 1: existing households → for each, add `HouseholdMember(demoUser.Id, household.Id, now)`.
   - Phase 2: existing recipes — assign each to the first seeded household.
   - Phase 3: existing persons — same.
2. `Backend/src/Recipes.Api/appsettings.Development.json.example` — add `Jwt:SigningKey` placeholder, `Entra:Enabled=false`, `Entra:TenantId=""`, `Entra:ClientId=""`.
3. Update `CLAUDE.md` mock-mode section: log in with `demo@local` / `demo1234` to see seeded data.

**Verification:** Drop and recreate (or just restart with `Database:Provider=InMemory`). `POST /api/auth/login` with the demo creds returns a JWT. `GET /api/recipes` returns the seeded set. A second user registering and creating their own household sees an empty recipe list.

---

## Bundle AUTH-1-7 — React frontend integration (≈ 2 hours)

**Goal:** Login / register UX, JWT storage, request authentication, protected routing, MSAL Microsoft sign-in.

Steps:
1. `npm i @azure/msal-browser @azure/msal-react zustand` (zustand likely already installed; verify).
2. `src/features/auth/`:
   - `api/authClient.ts` — typed wrappers for `register`, `login`, `entraExchange`, `me`.
   - `schemas.ts` — zod for the three forms (builder functions taking `t`).
   - `store/authStore.ts` — Zustand: `{ token, user, householdIds, isAuthenticated }`. Hydrates from `localStorage.auth.session` on init. Selectors `useToken()`, `useUser()`. Actions `login(result)`, `logout()`, `setHouseholds(ids)`.
   - `hooks/useLogin.ts`, `useRegister.ts`, `useEntraLogin.ts` — TanStack Query mutations. On success, write to `authStore` + `localStorage`.
   - `components/LoginForm.tsx`, `RegisterForm.tsx`, `EntraLoginButton.tsx`, `RequireAuth.tsx`.
3. `src/api/client.ts` — axios interceptor: request → attach `Authorization: Bearer ${token}` from store; response → on 401, clear session and redirect to `/login`.
4. `src/main.tsx` — wrap the app in `<MsalProvider>` configured from `import.meta.env`. Add `/login` and `/register` routes (lazy). Wrap the rest of the routing tree in `<RequireAuth>`.
5. `src/components/layout/AppLayout.tsx` — header: replace the existing language-switcher-only strip with `[LanguageSwitcher] [UserMenu (displayName + logout)]`. Move switcher into a small flex container.
6. Empty-household onboarding: if `me.households.length === 0`, the existing `<RequireAuth>` redirects to `/households/new` (a one-time onboarding page that creates the first household and lands the user on `/recipes`).

**Verification:**
- `npm run build` passes.
- Open the React app — redirected to `/login`.
- Log in as `demo@local` — recipes list loads.
- Log out — back on `/login`.
- Register a new user — onboarded to `/households/new`.
- Refresh while authenticated — stay logged in (session restored from localStorage).

---

## Bundle AUTH-1-8 — Angular frontend integration (≈ 2 hours)

**Goal:** Mirror AUTH-1-7 idiomatically in Angular.

Steps:
1. `npm i @azure/msal-browser @azure/msal-angular --legacy-peer-deps`.
2. `src/app/core/auth.store.ts` — signal-based store, same API as React: `token`, `user`, `householdIds`, computed `isAuthenticated`. Hydrates from `localStorage`.
3. `src/app/core/auth.interceptor.ts` — functional `HttpInterceptorFn` attaches Bearer header; on 401 clears the store and `router.navigate(['/login'])`.
4. `src/app/core/auth.guard.ts` — functional `CanActivateFn` reads the store, returns `true` or `router.parseUrl('/login')`.
5. `src/app/features/auth/`:
   - `auth.client.ts` — typed `HttpClient` calls.
   - `login.component.ts/.html` standalone, ReactiveFormsModule, validates and calls the client.
   - `register.component.ts/.html`.
   - `entra-login-button.component.ts` — uses `MsalService.loginPopup` (or redirect — popup gives a smoother dev experience, redirect is more robust on iOS — start with popup).
6. `app.config.ts` — `provideMsal({...})` with config from `environment`. Add the auth interceptor to `provideHttpClient(withInterceptors([authInterceptor]))`.
7. `app.routes.ts` — `/login`, `/register`, `/households/new` (onboarding); existing routes wrapped by `canActivate: [authGuard]`.
8. `app.html` — header gets a `<app-user-menu>` next to `<app-language-switcher>`.

**Verification:** Same click-through as AUTH-1-7 against the Angular dev server.

---

## Bundle AUTH-1-9 — Translations & error mapping (≈ 30 minutes)

**Goal:** New auth strings localized on both frontends, backend error codes mapped.

Steps:
1. Both `Frontend/src/locales/{bg,en}.json` and `FrontendAngular/public/i18n/{bg,en}.json` gain:
   - `auth.*` block: `signIn`, `signUp`, `email`, `password`, `displayName`, `signInWithMicrosoft`, `noAccount`, `haveAccount`, `welcomeBack`, `welcome`, `logout`, `firstHouseholdTitle`, `firstHouseholdDesc`.
   - `errors.*` additions: `AuthRequired`, `InvalidCredentials`, `EmailExists`, `AuthProviderMismatch`, `SessionExpired`, `NotAMember`.
2. Backend ProblemDetails — every auth-related exception filter sets `extensions.code` to one of the above values so the frontend `getErrorMessage` helpers map them.
3. Cross-app key parity check — same `node` script we used for L1.

**Verification:** Trigger each error path manually and confirm the toast / inline message renders in the active language.

---

## Bundle AUTH-1-10 — Documentation (≈ 30 minutes)

Steps:
1. Update `CLAUDE.md`:
   - Add an `## Authentication` section: `User` vs `Person`, `HouseholdMember` ownership, JWT shape, the `Entra:Enabled` flag.
   - Update the mock-mode section: log in with `demo@local` / `demo1234`.
   - Update Azure deployment list: Key Vault now also stores `Jwt:SigningKey`.
2. Architecture invariant note in `.claude/commands/architecture-check.md`: every household-scoped query/command handler must call `currentUser.RequireHouseholdMembership(...)` — flag with a grep rule (or add to the existing CI check if cheap).
3. `appsettings.json.example` (production template) gets `Entra:` block.

**Verification:** Read-through. No code/build implications.

---

## Files to modify (cross-bundle index)

| Path | Bundles |
|---|---|
| `Recipes.Domain/Users/User.cs` (new) | 1 |
| `Recipes.Domain/Users/UserId.cs`, `AuthProvider.cs` (new) | 1 |
| `Recipes.Domain/Households/HouseholdMember.cs` (new) | 1 |
| `Recipes.Domain/Households/Household.cs` | 1 |
| `Recipes.Domain/Recipes/Recipe.cs` | 1 |
| `Recipes.Domain/Persons/Person.cs` | 1 |
| `Recipes.Infrastructure/Persistence/Configurations/*.cs` | 1 |
| `Recipes.Infrastructure/Persistence/RecipesDbContext.cs` | 1 |
| `Recipes.Infrastructure/Persistence/Migrations/*` (new) | 1 |
| `Recipes.Infrastructure/Persistence/Seeding/DemoSeeder.cs` | 6 |
| `Recipes.Api/Auth/*` (new — JwtOptions, IPasswordHasher, IJwtIssuer, ICurrentUser impl, IEntraTokenValidator) | 2, 3, 5 |
| `Recipes.Api/Endpoints/AuthEndpoints.cs` (new) | 2, 5 |
| `Recipes.Api/Endpoints/*Endpoints.cs` | 3 |
| `Recipes.Api/Common/ExceptionMapping.cs` | 4 |
| `Recipes.Api/Program.cs` | 2, 3, 5 |
| `Recipes.Api/appsettings.Development.json.example` | 6 |
| `Recipes.Application/Common/ICurrentUser.cs` (new) | 3 |
| `Recipes.Application/Auth/Register/*` (new) | 2 |
| `Recipes.Application/Auth/Login/*` (new) | 2 |
| `Recipes.Application/Auth/EntraExchange/*` (new) | 5 |
| `Recipes.Application/Auth/Me/*` (new) | 3 |
| `Recipes.Application/Recipes/*` | 4 |
| `Recipes.Application/Households/*` | 4 |
| `Recipes.Application/Persons/*` | 4 |
| `Recipes.Application/MealPlans/*` | 4 |
| `Recipes.Application/ShoppingLists/*` | 4 |
| `Recipes.Application/Expenses/*` | 4 |
| `Frontend/package.json` | 7 |
| `Frontend/src/features/auth/**` (new) | 7 |
| `Frontend/src/api/client.ts` | 7 |
| `Frontend/src/main.tsx` | 7 |
| `Frontend/src/components/layout/AppLayout.tsx` | 7 |
| `Frontend/src/locales/{bg,en}.json` | 9 |
| `FrontendAngular/package.json` | 8 |
| `FrontendAngular/src/app/core/auth.{store,interceptor,guard}.ts` (new) | 8 |
| `FrontendAngular/src/app/features/auth/**` (new) | 8 |
| `FrontendAngular/src/app/app.config.ts` | 8 |
| `FrontendAngular/src/app/app.routes.ts` | 8 |
| `FrontendAngular/src/app/app.html` | 8 |
| `FrontendAngular/public/i18n/{bg,en}.json` | 9 |
| `CLAUDE.md` | 10 |

---

## Recommended execution order

`AUTH-1-1` → `AUTH-1-2` → `AUTH-1-3` → `AUTH-1-4` → `AUTH-1-6` → `AUTH-1-5` → `AUTH-1-7` → `AUTH-1-8` → `AUTH-1-9` → `AUTH-1-10`

Why this order:
- 1 → 2 → 3 → 4 lets the backend run end-to-end with Local auth before touching anything else.
- 6 (seeder) before 5 (Entra) so we can verify the auth pipeline manually with seeded data while Entra is still off.
- 5 layered on top of a working Local pipeline so the Entra reconciliation can be tested against existing users.
- 7 then 8 — React first since the user is already comfortable there, then mirror to Angular. Both could go in parallel but sequencing keeps each commit reviewable.
- 9 and 10 last as polish.

---

## Verification (end-to-end)

```bash
# Backend
dotnet test Backend/Recipes.sln
dotnet run --project Backend/src/Recipes.Api

# React
cd Frontend; npm install; npm run build; npm run dev

# Angular
cd FrontendAngular; npm install --legacy-peer-deps; npm run build; npm run start
```

Manual click-through on both apps:
1. Open app — redirected to `/login`.
2. Sign in as `demo@local` / `demo1234` — see seeded recipes.
3. Sign out, register a new user — onboarding redirects to `/households/new`.
4. Create household "My Place", land on `/recipes` (empty).
5. Add a recipe — visible to this user.
6. Log out, log back in as `demo@local` — original recipes still present, the new user's recipe **not** visible.
7. With `Entra:Enabled=true` — "Sign in with Microsoft" button completes the flow and lands authenticated.
8. Toggle BG ↔ EN on the login / register screens — all auth strings localize.
9. JWT survives a hard refresh; an expired or tampered JWT redirects to `/login`.

---

## Review pass (after implementation)

Manual checklist on each PR:

- [ ] No endpoint outside `/api/auth/*` and `/api/health` is reachable without `[Authorize]`.
- [ ] Every household-scoped handler calls `currentUser.RequireHouseholdMembership(...)` or filters by membership.
- [ ] `dotnet test`, `npm run build` (both apps), `architecture-guard` workflow all green.
- [ ] No JWT secret or Entra client ID committed in any `appsettings.Development.json` (only `*.example`).
- [ ] `bg.json` / `en.json` parity preserved on both frontends.
- [ ] Cross-household read returns 404, not 403, on GET endpoints (no existence leak).
- [ ] Refresh-after-login works (token survives reload via localStorage).
- [ ] Logging out invalidates the in-memory `ICurrentUser` cache for that user (not strictly needed since the JWT is gone, but verify there's no stale data leak between sessions in the same browser tab).

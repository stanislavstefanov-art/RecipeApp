# AUTH-1 — Authentication & Household-Scoped Authorization

## Summary

Add user authentication to RecipesApp with two interchangeable identity providers — **local email/password** and **Microsoft Entra ID** — both ending in an app-issued JWT bearer token. Introduce a **`User`** aggregate distinct from the existing `Person` profile concept, and a **`HouseholdMember`** join binding users to households. All existing data (recipes, meal plans, shopping lists, expenses) becomes household-scoped: an authenticated user only sees data belonging to households they're a member of.

The scope of this feature is everything required for a usable single-region single-tenant deployment: register / log in / log out, household ownership and membership, JWT issuance and validation, frontend login flows on both React and Angular. Things explicitly punted to future plans: password reset, email verification, 2FA, refresh-token rotation, account deletion, sharing-by-link, role-based permissions inside a household.

---

## Goals

- A **User** identity, stored in our DB, that can authenticate via either local password **or** federated Entra OIDC. The two paths converge to the same `User` row and the same JWT shape.
- **Household = unit of ownership.** Every read/write of recipes, meal plans, shopping lists, and expenses is scoped to households the caller is a member of.
- **JWT bearer in `Authorization` header**, no cookies, no CSRF surface.
- **Login UX on both frontends** in parallel, behaving identically.
- **Local dev keeps demo data** (seeded under a default user); production starts empty (the first registered user becomes the owner of a fresh household).

## Non-goals

- Refresh tokens. JWT lifetime is 7 days; user re-authenticates on expiry.
- Password reset / forgotten-password email. (Will be a separate plan.)
- Email verification on registration.
- 2FA / MFA, including Entra MFA enforcement (Entra-managed users still authenticate via Entra; we don't ourselves prompt for second factor).
- Account deletion / GDPR data export.
- Per-household roles (Owner vs Member with different permissions). All household members have equal read/write power for now.
- Inviting users by email link. Adding a member today still happens via "select an existing User and add to household" — UX for invitation flow comes later.
- Backend `IStringLocalizer`. Auth error responses come back in the existing untranslated form; the React/Angular `errors.*` translation maps catch them.

---

## Domain model

### New aggregates / entities

```
User (aggregate root)
  UserId               readonly record struct (Guid)
  Email                string (unique, case-insensitive)
  DisplayName          string
  AuthProvider         enum { Local = 1, Entra = 2 }
  PasswordHash         string?           // Local only; null for Entra
  EntraObjectId        Guid?             // Entra only; null for Local
  CreatedAt            DateTimeOffset
  LastLoginAt          DateTimeOffset?

HouseholdMember (entity owned by Household aggregate)
  UserId               UserId
  HouseholdId          HouseholdId
  JoinedAt             DateTimeOffset
```

### Modifications

- `Household` gains a collection of `HouseholdMember` (alongside its existing `Members` collection of `Person`).
  *Important:* `Person` and `User` are different concepts. `Person` is an eater profile (with dietary preferences, health concerns) — a household can include children, guests, or anyone for whom meals are planned. `User` is a login identity. Many users may exist with no `Person`; many `Person` records may exist with no `User`. There is **no** required link between them in this slice.
- All other aggregates stay the same. **No `OwnerUserId` is added to Recipe, MealPlan, ShoppingList, or Expense.** Authorization happens at the `HouseholdId` boundary already present in those entities (or, for Recipe, via a new `HouseholdId` field — see below).

### Recipe ownership

Currently `Recipe` has no household. With auth, a recipe must belong to a household so other households can't read it. Add:

```
Recipe
  + HouseholdId        HouseholdId       // new, required, indexed
```

On migration to a real DB, this is a breaking change. For local InMemory dev the seeder assigns existing seeded recipes to the default household. For production (currently empty) it's a fresh start — no migration data shaping required.

---

## Authentication flows

### Local (email + password)

```
POST /api/auth/register
  { email, password, displayName }
→ 201 Created  { token, expiresAt, user: { id, email, displayName } }
→ 409 Conflict if email exists

POST /api/auth/login
  { email, password }
→ 200 OK       { token, expiresAt, user }
→ 401 Unauthorized on bad credentials (generic message — don't leak which side was wrong)
```

- Password hashed with **PBKDF2** (`Rfc2898DeriveBytes`, SHA-256, 100k iterations, 16-byte salt, 32-byte hash). Format stored as `{salt}.{hash}` base64. BCrypt would also work; PBKDF2 stays in `System.Security.Cryptography` with no extra package.
- Password rules: min 8 chars, must contain at least one letter and one digit. Validation in `RegisterCommandValidator`.
- Email lower-cased on store and lookup.

### Entra (Microsoft Entra ID, OIDC)

We use the **Authorization Code flow with PKCE** initiated by the frontend via MSAL.js (React) / @azure/msal-angular (Angular). The frontend completes the OIDC dance with Entra and obtains an Entra `id_token`. It then exchanges that with our backend for an app JWT:

```
POST /api/auth/entra/exchange
  { idToken }                            // the Entra id_token obtained client-side
→ 200 OK  { token, expiresAt, user }
→ 401 if id_token validation fails
```

Server-side validation of the `id_token`:
- Signature verified against Entra JWKS (`https://login.microsoftonline.com/<tenant>/discovery/v2.0/keys`) using `Microsoft.IdentityModel.Tokens` (already in the .NET stack).
- Audience must equal our Entra app registration's client ID.
- Issuer must equal `https://login.microsoftonline.com/<tenant>/v2.0`.
- `oid` (object ID) and `email` claims read from the validated token.

User-row reconciliation on first Entra login:
- Look up `User` by `EntraObjectId = oid`. If found, update `LastLoginAt`, return JWT.
- Otherwise look up by `Email = lower(email)`. If found and `AuthProvider == Local` → **409 Conflict** (`AuthProvider mismatch — log in with your password`). We do not silently merge Local and Entra users in this slice.
- Otherwise create a new `User` with `AuthProvider = Entra`, `EntraObjectId = oid`, no password hash, return JWT.

Both providers ultimately yield an app-issued JWT — endpoints that consume it don't care which provider got the user there.

### Logout

Logout is purely a frontend concern: drop the JWT from storage. We do not maintain a server-side blacklist in this slice. JWT lifetime is short enough (7d) that the trade-off is acceptable.

---

## App-issued JWT

Algorithm: **HS256** with a 256-bit secret in `Jwt:SigningKey` config (Key Vault in production, local file in dev).

Claims:

| Claim | Value |
|---|---|
| `sub` | `User.Id` (Guid string) |
| `email` | `User.Email` |
| `name` | `User.DisplayName` |
| `provider` | `Local` or `Entra` |
| `iat`, `exp`, `iss`, `aud` | standard; `iss = "RecipesApp"`, `aud = "RecipesApp"` |

**`householdIds` is *not* a claim.** Membership is fetched per request from a small in-memory cache keyed by `UserId` with a 60-second TTL. This keeps the token small and means revoking a user's household membership takes effect within a minute without the user having to re-log in. The cache is invalidated immediately on `add-member` / `remove-member` mutations.

JWT lifetime: **7 days**. Sliding lifetime via re-issue on every successful authenticated request would extend this; out of scope for now — fixed expiry, user re-logs in.

---

## Authorization model

### Per-request pipeline

1. ASP.NET Core auth middleware validates the JWT against `Jwt:SigningKey`, populating `HttpContext.User` with claims.
2. A `RequiresAuthorizationFilter` (or `[Authorize]` minimal-API extension) rejects unauthenticated requests with `401 Unauthorized` for every endpoint **except** the auth endpoints themselves and `GET /api/health`.
3. Inside command/query handlers that touch household-scoped data, a new abstraction `ICurrentUser` exposes:
   - `UserId CurrentUserId { get; }`
   - `Task<IReadOnlyList<HouseholdId>> GetHouseholdIdsAsync(CancellationToken)`

### Authorization rules per endpoint family

- **Recipes** — `GET /api/recipes` returns only recipes whose `HouseholdId` is in the caller's household set. Same for `GET /api/recipes/{id}` (404 if recipe exists but caller not a member). `POST /api/recipes` requires a `householdId` in the body and validates membership.
- **Households** — `GET /api/households` returns only households the caller belongs to. `POST /api/households` creates a household and adds the creator as the first member in one transaction.
- **Persons** — Person is global today. With this slice, a `Person` becomes household-scoped (gains `HouseholdId`). Listing persons returns only those in the caller's households.
- **Meal plans, shopping lists, expenses** — already carry `HouseholdId`; only the membership check is added. `403 Forbidden` if the caller is authenticated but not a member of the requested household; `404 Not Found` if the household doesn't exist (don't disclose existence). For read endpoints we collapse 403 → 404 to avoid existence leaks.
- **AI endpoints** (recipe import, meal plan suggestion, ingredient substitution, etc.) — `[Authorize]` only. They don't need household scoping themselves; the resulting saved recipe / meal plan will be scoped when persisted.

### Architecture invariants

- `ICurrentUser` lives in `Recipes.Application/Common/`. Its implementation lives in `Recipes.Api/Auth/`. No additional reference cycles.
- No new direct `IRecipesDbContext` references in `Application` (existing invariant from `architecture-guard`).
- The `RegisterCommand` / `LoginCommand` / `EntraExchangeCommand` each get a matching `*Validator` per the existing CI rule.

---

## Frontend integration

### Shared shape

Both frontends store the JWT and the user object in browser **`localStorage`** under key `auth.session`. The format:

```json
{ "token": "...", "expiresAt": "...", "user": { "id": "...", "email": "...", "displayName": "...", "provider": "Local" } }
```

Both apps add an Authorization header to every API request (axios interceptor in React, functional `HttpInterceptorFn` in Angular) and treat a `401` response as "session expired — clear localStorage and redirect to /login".

### React (`/Frontend`)

- New routes: `/login`, `/register`. Not protected.
- Wrap protected routes in a `<RequireAuth>` component that reads from a Zustand `authStore` and redirects to `/login` if unauthenticated. The wrapper lives at `src/features/auth/components/RequireAuth.tsx` and protects the existing `<AppLayout>` outlet.
- New feature folder `src/features/auth/` with: `api/` (login, register, exchange), `hooks/` (useLogin, useRegister, useEntraLogin), `components/` (LoginForm, RegisterForm, EntraLoginButton).
- Microsoft sign-in: `@azure/msal-browser` + `@azure/msal-react`. Configuration (clientId, tenantId, redirectUri) read from `import.meta.env`.
- Header shows the user's `displayName` and a logout button when authenticated; both replace the existing `<LanguageSwitcher>`-only header strip.

### Angular (`/FrontendAngular`)

- New routes: `/login`, `/register`, both lazy-loaded standalone components.
- Functional `authGuard: CanActivateFn` reads from a `signal`-based `AuthStore` (in `src/app/core/auth.store.ts`) and either lets the route through or `router.parseUrl('/login')`.
- New feature folder `src/app/features/auth/` mirroring React: standalone `LoginComponent`, `RegisterComponent`, `EntraLoginButton`.
- Microsoft sign-in: `@azure/msal-angular`. Config provided via `provideMsal(...)` at app bootstrap.
- App header gains a user-menu standalone component that replaces / sits next to the language switcher.

### Both: error handling

The existing `getErrorMessage` helpers (React `lib/getErrorMessage.ts`, Angular `shared/get-error-message.ts`) already map `errors.*` translation keys. Add new keys:

- `errors.AuthRequired` — "Please log in to continue."
- `errors.InvalidCredentials` — "Wrong email or password."
- `errors.EmailExists` — "An account with this email already exists."
- `errors.AuthProviderMismatch` — "This email is registered with a password — log in that way instead."
- `errors.SessionExpired` — "Your session has expired. Please log in again."
- `errors.NotAMember` — "You don't have access to this household."

Backend returns ProblemDetails with `extensions.code` set to one of the above values.

---

## Local dev / mock mode

The existing seeder (M3) inserts demo data unconditionally. With auth, the seeder runs in two phases:

1. Create a **default user**: `demo@local`, password `demo1234`, display name `Demo User`, `AuthProvider = Local`.
2. Insert the existing seed (households, persons, recipes, meal plans, shopping lists, expenses) **owned by the default user** — i.e. the default user is the sole `HouseholdMember` of every seeded household. Recipes get `HouseholdId` of one of those households.

`appsettings.Development.json.example` is updated with:
```json
"Jwt": { "SigningKey": "dev-only-256-bit-base64-secret-replace-in-production" },
"Entra": { "Enabled": false }
```

In production (`Entra:Enabled = true`), the configuration also requires `Entra:TenantId`, `Entra:ClientId`. The JWT signing key is a Key Vault reference.

---

## API contract (full surface)

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/auth/register` | none | `{ email, password, displayName }` | `{ token, expiresAt, user }` |
| POST | `/api/auth/login` | none | `{ email, password }` | `{ token, expiresAt, user }` |
| POST | `/api/auth/entra/exchange` | none | `{ idToken }` | `{ token, expiresAt, user }` |
| GET | `/api/auth/me` | Bearer | — | `{ user, households: [{ id, name }] }` |
| All existing endpoints | (unchanged paths) | **Bearer (now required)** | (unchanged) | (unchanged + 401/403 on auth failure) |

`/api/auth/me` is a convenience used by the frontends to verify the token is still valid on app boot and to populate the user / household list. It returns the same shape both sides expect.

---

## Acceptance criteria

1. A fresh production database accepts `POST /api/auth/register`, returns a JWT, and the JWT works for `GET /api/auth/me`.
2. With local-mock data, `POST /api/auth/login` with `demo@local` / `demo1234` succeeds and `GET /api/recipes` returns the seeded recipes.
3. A second user that registers and creates their own household sees an empty list at `GET /api/recipes`.
4. Cross-household access — user A trying to read a household-B recipe by id — returns `404 Not Found` (not 403, not the recipe).
5. Both frontends route to `/login` when an unauthenticated user opens any page. After login they land on `/recipes`.
6. The Entra **Sign in with Microsoft** button completes a redirect dance and returns the user authenticated, `provider == "Entra"` in the JWT.
7. Removing a user from a household (existing endpoint, soon `DELETE /api/households/{id}/members/{personId}` extended to also delete the `HouseholdMember`) takes effect for that user within 60 seconds without re-login.
8. `dotnet test Backend/Recipes.sln` and both `npm run build` commands pass clean. The `architecture-guard` workflow stays green.

---

## Risks and open questions

- **Linking Local and Entra accounts that share an email.** The spec rejects this with a 409 in the Entra exchange flow. A follow-up plan adds a "link accounts" UX. For now, users pick one provider per account.
- **JWT secret rotation.** Rotating `Jwt:SigningKey` invalidates every active token. Acceptable in the current single-region single-instance setup; a later plan introduces a JWKS-backed scheme.
- **Household creation for first Entra user.** When an Entra user first logs in we create their `User` row but they have no household. The frontend lands them on `/households/new` with a one-time onboarding flow. UX for this lives in the plan, not this spec.
- **Rate-limiting login attempts.** Out of scope here; covered by ASP.NET Core's `Microsoft.AspNetCore.RateLimiting` in a later plan.

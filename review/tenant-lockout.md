# Tenant lockout (cross-cut)

Agent-mode, 2026-11-08. Owner decision: a deactivated or deleted tenant locks its users out. Nothing was built and no
test was run (owner's serial pass).

## 1. Access-path map

| Path                                                                                      | Code                                                                                                                  | Tenant state enforced?                                                                                                                                |
|-------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|
| Password sign-in                                                                          | `Authentication.AuthenticateByUserNamePasswordAsync` (called by `AuthenticationLoginService.ByUserNamePasswordAsync`) | Yes, new. Same branch as an inactive user: dummy hash work, failure type 2, `NotActiveException`, caught as plain `Unauthorized`.                     |
| Negotiate / OAuth sign-in, Negotiate MFA                                                  | `Authentication.AuthenticateByNegotiateAsync` (login service lines about 125 and 200)                                 | Yes, new. Failure surfaces as the existing Forbidden or Unauthorized for an inactive user.                                                            |
| MFA completion, change password                                                           | reached only after one of the above                                                                                   | Covered transitively.                                                                                                                                 |
| JWT or cookie session, per request                                                        | `HybridAuthHandler.ValidateJwtAsync` -> `TokenValidity.IsRevokedAsync` -> `GetSessionStateAsync`                      | Yes, new: `Blocked` now includes a disabled non-landlord tenant. One DB read per request, unchanged count.                                            |
| Cookie refresh                                                                            | `TokenRefreshMiddleware` runs after authentication only                                                               | Covered: a refused session is never refreshed.                                                                                                        |
| API key                                                                                   | `HybridAuthHandler.ValidateApiKeyAsync` (in-memory `ApiTokensCache`, no DB before)                                    | Yes, new: one `GetSessionStateAsync` read per API-key request, identical to the JWT path. Fails with the same "API Key Not Found." as an unknown key. |
| Permission lookup (all service and endpoint checks, `InvokeService` callbacks, `/api/Me`) | `Jube.Data.Security.PermissionValidation`                                                                             | Yes, new: `ActiveUser` and non-landlord permissions require an enabled tenant; landlord tenant exempt by `Landlord = 1`.                              |
| Landlord switching                                                                        | `UserInTenantDtoValidator` -> `UserInTenantRepository.ExistsTenantRegistryAsync`                                      | Yes, new: tenant must be `Active = 1` and not deleted; same existing "tenant not found" error code and token.                                         |
| SignalR / SSE connections                                                                 | `RevokedSessionSweeper` -> `TokenValidity.GetSessionStatesAsync`                                                      | Yes, new: same `Blocked` expression, so live connections of a disabled tenant are closed on the next sweep.                                           |
| `/api/Me`                                                                                 | `MeEndpoints` behind the handler plus `PermissionValidation`                                                          | Covered by both.                                                                                                                                      |
| Model invoke user list                                                                    | `GetEntityAnalysisModelApiUsersQuery` snapshot (`Users`), synced with the model                                       | Not changed; reached only through the handler, which now refuses first.                                                                               |
| Landlord tenant and landlord users                                                        | all of the above                                                                                                      | Exempt: `Landlord = 1` in the predicates.                                                                                                             |

## 2. What changed

- `Jube.Data/Security/PermissionValidation.cs`: shared `TenantEnabledJoin` and `TenantEnabledPredicate` added to the
  permissions and `IsActiveUserAsync` queries. `IsLandlordAsync` left as it was (already requires an active tenant).
- `Jube.Data/Security/TokenValidity.cs`: `TenantDisabledPredicate` (EXISTS on RoleRegistry and TenantRegistry) added to
  both `Blocked` expressions.
- `Jube.Data/Query/GetUserTenantEnabledQuery/GetUserTenantEnabledQuery.cs` (new): tenant enabled test by role guid, used
  at sign-in.
- `Jube.Service/Authentication/Authentication.cs`: both sign-in methods fold the tenant test into the existing
  inactive-user branch.
- `Jube.App/Middlewares/HybridAuthorisationHandler.cs`: API key path now async and checks `Blocked`.
- `Jube.Data/Repository/UserInTenantRepository.cs`: `ExistsTenantRegistryAsync` requires `Active = 1`.
- No new message text, so no localiser or resx change: every refusal reuses an existing generic outcome or the existing
  `TenantRegistryIdNotFound` token (already in all nine locales).
- Locked tenants are deliberately not covered (owner said inactive or deleted).
  `UserOfADisabledTenant_CanStillAuthenticateAsync` asserted the old behaviour; it is now
  `UserOfALockedTenant_CanStillAuthenticateAsync`.

## 3. Staleness bound

Zero for sessions, sign-in, permissions, API keys and switching: none of these paths has a cache, each reads the DB per
request, so deactivation applies to the next request. SignalR/SSE connections close on the next `RevokedSessionSweeper`
tick (its interval, not a new cost). `ApiTokensCache` key set (60 s refresh) is unchanged and no longer decides tenant
state.

## 4. Tests (unrun; owner's serial pass)

- Updated: `AuthenticationAccountStateTests.UserOfALockedTenant_CanStillAuthenticateAsync` (Service).
- New `Jube.Tests/Service/Authentication/TenantLockoutTests.cs` (11): inactive and deleted sign-in is plain
  Unauthorized, dummy hash only, negotiate refused, reactivation, other tenant and landlord unaffected, `IsRevokedAsync`
  and session states, permissions empty.
- New `UserInTenantServiceTests.UpdateToADisabledTenantIsRefusedAndTheLandlordStaysWhereTheyAreAsync` (Service).
- New `Jube.Tests/Security/Identity/PenTestTenantLockout.cs` (3, PenTest): bearer and cookie sessions 401 on `/api/Me`
  and a gated route with the same shape as an unauthenticated refusal, real sign-in refused with the same shape as a bad
  password, landlord and other tenants unaffected, landlord switch into an inactive tenant refused.
- Overlap check: existing `PenTestUserInTenant` covers switching into a soft-deleted tenant (still true);
  `PenTestUserRegistryApiKey.ADisabledUsersApiKeyIsExcludedFromTheValidationSet` is repository-level and untouched.
- Docs catalogue updated in `docs/Testing/ServiceAuthenticationAndUsers.md`, `SecuritySurfaceAndPenTest.md`,
  `ServiceRepositories.md`.

Run: `TenantLockoutTests`, `AuthenticationAccountStateTests`, `PermissionValidationTenantTests`,
`UserInTenantServiceTests`, `PenTestTenantLockout`, `PenTestUserInTenant`, `PenTestTenantRegistry`, then the serial
PenTest and Service passes (many suites sign in or build permissions, any of which a tenant fixture without an Active
tenant would now fail).

## 5. Risks and not covered

- API-key refusal has no HTTP test: pen hosts build `ApiTokensCache` uninitialised (`GetUninitializedObject`), so no key
  is ever valid. Covered by the shared `Blocked` state tests, not by the handler itself. A handler-level test needs a
  seeded `ApiTokensCache`.
- API-key requests now cost one extra indexed read each (invoke hot path). If it shows in load tests, add a short
  in-process TTL for `Blocked`, trading a few seconds of staleness.
- The API-key check also now refuses keys of inactive, locked or deleted users immediately (previously up to 60 s).
- A user whose role has no tenant row is refused by permissions (inner join, fail closed); sign-in and sessions treat a
  missing tenant row differently (sign-in refuses, sessions allow). Not expected in real data.
- A landlord already switched into a tenant that is then deactivated stays pointed at it (not locked out; can switch
  away). No reset-on-deactivate was added.
- `IsLandlordAsync` still does not check role Active, nor tenant Deleted; tenant-registry already refuses deleting or
  deactivating the Landlord tenant.
- Out of scope: data cascade on tenant delete (users, keys, models, cases remain), and the model `Users` snapshot query,
  which is not tenant-aware.

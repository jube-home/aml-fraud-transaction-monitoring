# Slice review: login-audit

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no DB-backed test was run (no infrastructure
assigned).

## 1. Summary and verdict

Verdict: READY AFTER OWNER ACTIONS. The slice is thin and sound. It is one Vue page, two read-only endpoints and two
services. No defect was found in code. Gaps were in test coverage (login service tests lacked the audit, span and tool
checks that the logout sibling has; no login-specific pen test sibling to PenTestUserLogout) and docs (none existed).
Both were written but are **unrun**. Before commit:

1. Owner runs the manual test (section 9).
2. Someone with a test database builds and runs `UserLoginServiceTests` (22 tests) and `PenTestUserLogin` (5 tests) in
   the serial PenTest pass.
3. Owner edits `slices.json` (see shared TODOs).
4. Owner supplies the three screenshots and resolves the docs TODOs.

## 2. Files reviewed (first-touch pull-ins)

| File                                                                                                                                                  | Read                       | Notes                                                                                         |
|-------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------|-----------------------------------------------------------------------------------------------|
| Jube.App/Vue/src/pages/AdminSecurityPage.vue                                                                                                              | whole                      | Claimed. Hosts all five security roots; only `logins` and `logouts` branches are this slice.  |
| Jube.App/Vue/e2e/adminSecurity.spec.ts                                                                                                                    | lines 236-315, 590-610     | Claimed. Sign-in/out journey at 594.                                                          |
| Jube.App/Vue/src/security/roots.ts and __tests__/roots.spec.ts                                                                                            | whole                      | Not claimed. Permission rules for logins and logouts (35).                                    |
| Jube.App/Endpoints/UserLoginEndpoints.cs, UserLogoutEndpoints.cs                                                                                      | whole                      | Single GET, `RequireAuthorization`, 403 for forbidden, 500 handled.                           |
| Jube.Service/UserLogin/UserLoginService.cs, UserLogout/UserLogoutService.cs                                                                           | whole (logout to line 250) | Permission `[35]`, tenant resolution, landlord unscoped, audit and span via `OperationScope`. |
| Jube.Data/Repository/UserLoginRepository.cs                                                                                                           | whole                      | Tenant scope is `UserInTenant` join on `CreatedUser`.                                         |
| Jube.Dto/UserLogin/UserLoginDto.cs, UserLogout/UserLogoutDto.cs                                                                                       | columns and descriptions   |                                                                                               |
| Jube.Tests/Service/UserLogin and UserLogout tests, Security/Identity/PenTestUserLogout.cs, Security/Surface/PenTestAuthenticationSurface.cs 1352-1470 | whole or relevant          |                                                                                               |

`--closure login-audit`: none, the slice is self-contained. `--orphans` was not needed: the backend files above belong
to the platform and are not in any claimed list. Back-end pull-ins reviewed by hand: the endpoints, services, repository
and DTOs listed above. Migrations (`AddUserLogoutTable`, `AddUserLoginTableIndex`, `AddAuthenticationIdToUserLogin`) not
read; see Not covered.

## 3. Findings

| # | Severity           | Where                                                 | What                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Status                                                                  |
|---|--------------------|-------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| 1 | Medium (inventory) | slices.json login-audit                               | `permissions: [34]` is wrong. Services use `[35]` (UserLoginService.cs line 33 region, UserLogoutService.cs line 33) and `roots.ts` maps logins/logouts to `securityUsers = 35`. `docs: []` empty.                                                                                                                                                                                                                                                           | Left, agents may not edit slices.json. Shared TODO.                     |
| 2 | Low (test gap)     | Jube.Tests/Service/UserLogin/UserLoginServiceTests.cs | No audit line, span, forbidden-audit or tool-catalogue test (logout has them at UserLogoutServiceTests 518-570). 19 tests against 27.                                                                                                                                                                                                                                                                                                                        | Fixed, 3 tests added, unrun.                                            |
| 3 | Low (test gap)     | Jube.Tests/Security                                   | No `PenTestUserLogin`. Partial cover exists in `PenTestAuthenticationSurface` (`UserLoginEndpoint_AuthorizationMatrix_AndParameterAbuseAsync`, `UserLoginEndpoint_ListIsTenantScopedAsync`): anonymous 401, no-permission refusal, verbs, parameter abuse, one tenant-B check. Not covered there: positive tenant A/B isolation by seeded rows, landlord seeing all and unknown-name rows, header/path spoofing, no row written by any verb, secret leakage. | Fixed, sibling copy added, unrun. Owner may prefer to drop the overlap. |
| 4 | Info               | UserLoginRepository.cs `BuildFilteredQuery`           | Tenant scope is by user name through `UserInTenant`. Failed attempts for names that do not exist (FailureTypeId 1) are landlord-only. Documented in Authentication concepts and asserted by `AttemptsAgainstUnknownNamesAreVisibleToALandlordOnlyAsync`. Consequence: a tenant admin never sees attacks by unknown names.                                                                                                                                    | Behaviour by design, documented.                                        |
| 5 | Info               | UserLoginDto, UserLogoutDto                           | `[GridEndpoint(Title=...)]`, `ListColumn Title` and DTO descriptions are English literals, so the grid column headers do not follow the pl/ar locale. Not verified against how GridModel is localised.                                                                                                                                                                                                                                                       | Left. Shared TODO.                                                      |
| 6 | Info               | e2e/adminSecurity.spec.ts 249-266                     | Stubbed rows use `userName`, but the DTO field is `createdUser`. The journey only asserts the first row contains `sam`/`kim`, so it passes on a shape the real API does not return. The e2e would not catch a column rename.                                                                                                                                                                                                                                 | Left. Not in my ownership to widen; flagged.                            |
| 7 | Info               | AdminSecurityPage.vue                                 | Shared with users/roles/permissions slices but claimed only by this one.                                                                                                                                                                                                                                                                                                                                                                                     | Shared TODO.                                                            |
| 8 | Info               | locales/en.json 2244                                  | "Sign-ins"/"Sign-outs" in UI; docs and Authentication concepts say "User Login".                                                                                                                                                                                                                                                                                                                                                                             | Shared TODO.                                                            |

No `.only`, `.skip` or `xit` in adminSecurity.spec.ts (grep). No code comments were added or altered.

## 4. Fixes made

1. `Jube.Tests/Service/UserLogin/UserLoginServiceTests.cs`: added
   `ListEmitsOneAuditLineAndOneSpanAndNoChangeEventAsync`, `ForbiddenListIsAuditedAsForbiddenAsync`,
   `ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue`, copied from the logout sibling with names swapped (area
   `UserLogin`, tool `UserLoginList`), plus the usings the logout file has (Diagnostics, Reflection, Agent.Models,
   Agent.ServiceToolCatalogue, Observability). Unrun, and not compiled.
2. `Jube.Tests/Security/Identity/PenTestUserLogin.cs` (new): sibling copy of `PenTestUserLogout.cs`. Differences from
   the sibling: seeds `UserLogin` rows and, for tenant A and B users, matching `UserInTenant` rows (the login scope is
   by user name, not by a tenant column), the no-tenant user's row is a failed attempt (FailureTypeId 1); seed uses
   AuthenticationTypeId 2 so the shape test's forbidden-word list ("password", "token") cannot collide with the rendered
   scheme text "Username and Password"; teardown removes both tables. Unrun and not compiled.
3. `docs/Testing/SecuritySurfaceAndPenTest.md`: new `PenTestUserLogin` section.
   `docs/Testing/ServiceAuthenticationAndUsers.md`: count 19 to 22 and 3 rows.
4. `docs/Configuration/Administration/LoginAudit/index.md` (new).

## 5. Test and pen-test matrix

| Layer                                                | Required                                    | Evidence read                                                                                                                                                                                                                                     | Status                                                     |
|------------------------------------------------------|---------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------|
| Service, list and clamp                              | list, take clamp, filters, sort, sample     | UserLoginServiceTests: `ListReturnsMostRecentFirstAndProjectsFieldsAsync`, `ListClampsTakeToOneHundredThousandAsync`, `ListFiltersByDateRangeAsync`, `ListFiltersBySearchAsync`, both sample-percentage tests, sort tests, `ListTotalReflects...` | Covered                                                    |
| Service, refusals                                    | permission refusal, no user, unknown tenant | `ListWithoutPermissionThrowsForbiddenAsync`, `CreateWithNullUserNameThrowsNotAuthenticatedAsync`, `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                                                                                            | Covered                                                    |
| Service, tenant isolation                            |                                             | `ListIsScopedToTheCallersTenantAsync`, `AttemptsAgainstUnknownNamesAreVisibleToALandlordOnlyAsync`, `LandlordSeesTheLoginAttemptsOfEveryTenantAsync`                                                                                              | Covered                                                    |
| Service, audit, span, change event                   |                                             | New `ListEmitsOneAuditLine...`, `ForbiddenListIsAuditedAsForbiddenAsync`                                                                                                                                                                          | Added, unrun                                               |
| Service, tool published                              |                                             | New `ToolIsDeclared...`                                                                                                                                                                                                                           | Added, unrun                                               |
| Service, create/update/delete/concurrency/validators |                                             | Not applicable, read-only list; there is no write path (endpoint maps GET only, UserLoginEndpoints.cs).                                                                                                                                           | N/A                                                        |
| Logout service                                       | same                                        | UserLogoutServiceTests lists 27 tests incl. audit (518), forbidden audit (541), tool (553), every reason and outcome (137), session seconds (156, 173), every sort column (451)                                                                   | Covered                                                    |
| Pen test, logout                                     |                                             | PenTestUserLogout: authz matrix (73), tenant scope and landlord (103), read-only verbs (158), hostile parameters (196), shape and secrets (264)                                                                                                   | Covered (read, not run)                                    |
| Pen test, login                                      |                                             | Existing: PenTestAuthenticationSurface 1352 and 1424. New: PenTestUserLogin, the same five tests.                                                                                                                                                 | Added, unrun                                               |
| Antiforgery, mass assignment, IDOR by id             |                                             | No mutation and no per-row route; verified by the verb tests (POST/PUT/PATCH/DELETE refused, `/api/UserLogin/1` is 404/405)                                                                                                                       | N/A, asserted in the new test                              |
| Permission parity                                    |                                             | PermissionParityTests lines 148 and 178 reference `/Administration/UserLogin` and `/api/UserLogin/` and the 35 holders (read only those lines)                                                                                                    | Covered, not examined further                              |
| Vitest                                               | pure modules                                | roots.spec.ts: 4 tests passed locally (run); asserts permission 35 yields users, logins, logouts. No test of the page itself.                                                                                                                     | Adequate: the page holds no logic of its own beyond wiring |
| Playwright                                           | happy and refusal                           | adminSecurity.spec.ts: `follows the permission of each card` (permissions [34], logins absent), `sign-ins and sign-outs are single grid beats` (594). Whole spec has 21 tests; not run by me.                                                     | Covered, with finding 6                                    |

## 6. Translations

SPA (ka, id, sv, ur, hi not audited here; SPA locale JSON is shared, integration pass): 80 `security.*` keys, pl and ar:
0 missing, 0 empty, 0 identical-to-English. 19 `t()` keys in the slice, all in en.json. Re-audit of the slice's Vue file
(AdminSecurityPage.vue, inherited by user-registry): all text goes through `t()` (`nav.adminSecurity`,
`security.loading`, `security.denied`); no hard-coded strings, no aria-label or placeholder literals. No proposed SPA
keys. Terminology flag (en "Sign-ins", pl "Logowania") is on the shared TODO list.

Server, closed: `UserLoginResources` and `UserLogoutResources` each have exactly two keys (`PermissionDenied`,
`NotAuthenticated`) and previously had `.resx` and `.fr.resx` only. Created `.pl.resx`, `.ar.resx`, `.ka.resx`,
`.id.resx`, `.sv.resx`, `.ur.resx` and `.hi.resx` (required set: en, pl, ar, ka, id, sv, ur, hi) for both, copied from
the `.fr.resx` structure, same key set, machine translation. These two classes are the only `.resx` classes the slice
uses: the endpoints are GET-only and read-only, so there are no validators, and neither DTO has a `FormModelResources`
entry (no `UserLogin`/`UserLogout` keys in any FormModelResources file).

Server, open (shared): the `[GridEndpoint(Title=...)]`, `[GridSearch(Label=...)]`, `ListColumn Title` ("User", "Scheme")
and `Description` literals in `UserLoginDto`/`UserLogoutDto` are hard-coded English. Moving them into resources needs
new keys in the shared `FormModelResources*.resx` and a lookup in the shared grid-model builder, so it is on
`review/SHARED-TODO.md`, not done here. The DTO files themselves are platform files not claimed by this slice. Until
then the grid headers stay English under pl/ar.

RTL audit of the slice's Vue files: AdminSecurityPage.vue has no physical left/right/ml/mr/pl/pr/text-left/text-right
classes, no scoped CSS, and its only icon is `ShieldAlert` (symmetric, no flip needed). Uses flex/gap and logical layout
only. Nothing to fix. Inherited note for user-registry: the page's grids are rendered by shared `PayloadGrid.vue`, so
the mixed-direction risks (RemoteIp, LocalIp, UserAgent, timestamps, Latin user names inside Arabic rows) and numeric
column alignment live there; the cells should carry `dir="auto"` or `<bdi>`/`dir="ltr"` for IPs, timestamps and user
agents. Added to the shared TODO list.

## 7. Docs written and TODO markers

Page: `docs/Configuration/Administration/LoginAudit/index.md` (new, nav_order 6 under Administration). Written from the
DTOs, services and tests only, not from the running UI. Markers (file line, all in that page): screenshots at 19
(`login-audit-security-hub.png`), 40 (`login-audit-signins-grid.png`), 95 (`login-audit-signouts-grid.png`). TODO (docs)
at 11, 17, 28, 34, 52, 53, 54, 88, 89, 118, 119, 127, 135. Line numbers are as at writing, re-grep with
`grep -n "TODO(" `. Also: `docs/Configuration/Administration/Users/index.md` lines 84-95 still describe the UserLogin
SQL journal with `LoginJournal.png` (Razor-era); not edited, outside this slice's page. Raised as a TODO (docs) at line
17 of the new page.

## 8. Shared-code TODOs raised

Appended to `review/SHARED-TODO.md` (4 lines): the AdminSecurityPage first-touch ownership, slices.json corrections,
server locale resources and hard-coded grid titles, and the terminology mismatch.

## 9. Manual test sheet

Built from UserLoginDto, UserLogoutDto, the services, roots.ts and the page. Both screens are read-only.

1. Permissions: user with 35 sees Sign-ins and Sign-outs cards; user with only 34 or 36 sees neither (and the grids, if
   the URL is forced, give a 403, not data). User with nothing sees the denied alert.
2. Tenancy: tenant admin sees own tenant rows only; a landlord sees all. Fail a login with a name that does not exist:
   visible to a landlord only.
3. Sign-ins grid: success and each of failure reasons 1, 2, 3, 5, 6 (via the login page); an OAuth or Negotiate failure
   if configured. Check Scheme text and Failure Reason text, and that Failure Message is empty for password failures.
4. Sign-outs grid: logout from the menu, administrator Revoke Sessions, a password change. Check Reason, Outcome,
   Session Started and Session seconds.
5. Controls: From/To default to the last hour (a new page is short; widen it); Search over user, IP, user agent, failure
   message (and cut-by for sign-outs); Apply, Clear, Refresh; CSV and Excel export; sort by each header; total count.
6. Refresh and deep link at `/administration/security`; back button from a grid to the hub.
7. Narrow width, dark theme, en/pl/ar with right-to-left (note the column headers come from the server and may stay
   English, finding 5).
8. Keyboard only through the hub cards, the filters and the export buttons.
9. Hostile input: search with `'; drop table`, `%`, `_`, a 20000 char string, an emoji, an RLM/RLO character; a user
   agent header with script; confirm no HTML execution in the grid and no 500.
10. Two tabs: not applicable beyond a refresh, the grid is read-only.

## 10. Commit pathspecs

Locale files created (2026-11-08): `Jube.Resources/UserLoginResources.pl.resx`, `UserLoginResources.ar.resx`,
`UserLogoutResources.pl.resx`, `UserLogoutResources.ar.resx`, plus `.ka`, `.id`, `.sv`, `.ur`, `.hi` of each (10 more
files).

`--paths` output:

```
Jube.App/Vue/src/pages/AdminSecurityPage.vue
Jube.App/Vue/e2e/adminSecurity.spec.ts
```

Hand-picked (not in `--paths`): `Jube.Tests/Security/Identity/PenTestUserLogin.cs`,
`Jube.Tests/Service/UserLogin/UserLoginServiceTests.cs`, `docs/Testing/SecuritySurfaceAndPenTest.md` (PenTestUserLogin
hunk only), `docs/Testing/ServiceAuthenticationAndUsers.md` (UserLogin hunk only),
`docs/Configuration/Administration/LoginAudit/`. Backend endpoints, services, repository, DTOs and tests already
existing are presumably committed with the platform and are not in my list. AdminSecurityPage.vue is shared (finding 7).

## 11. Not covered

- Nothing was compiled or run on the .NET side. The two new C# edits are unverified by a compiler and by Rider lint. In
  particular `Required()` and `TestLog.Entries` are used because the logout file uses them with the same usings, not
  because I read their definitions. In `PenTestUserLogin`, the `UserLogin` type name resolving to the Poco and
  `UserInTenant` having just `User` and `TenantRegistryId` as required columns are inferred from
  `UserLoginServiceTests.CreateSampleAsync`.
- Pen tests not run (serial pass owed). The seed uses `Fx.Seed.UserTenantB` as the sibling does.
- Playwright was not run. Only `roots.spec.ts` was run (4 passed); prettier check on the two claimed Vue files passed
  and eslint printed nothing. `type-check` was not run.
- Rider lint on the changed C# files was not run. House style (no comments, one class per file) was kept by
  construction, not machine-checked.
- Migrations for UserLogin and UserLogout, `AuthenticationEndpoints.cs`, `Authentication.cs` and `UserLogoutRecorder.cs`
  (who writes the rows, when, with what `TenantRegistryId`) were not read. The write side is in the authentication
  slice.
- `PayloadGrid.vue` and `GridModelEndpoints.cs` were only skimmed (test ids, export buttons). I did not verify
  server-side sorting, the row cap shown in the UI, or how titles are localised.
- The `PermissionParityTests` and `PenTestBolaHeuristics` references were seen by grep only.
- The docs were written from code and tests, not the running UI, hence the markers.
- Server locale decision and the shared-TODO items are not resolved.

## Owner decisions (2026-11-08)

- `PenTestUserLogin.cs` dropped as overlapping `PenTestAuthenticationSurface`; its catalogue section in
  `docs/Testing/SecuritySurfaceAndPenTest.md` was removed. Finding 3 is closed as covered by the existing tests, no new
  pen test needed. The two lines above that mention `PenTestUserLogin` are superseded.
- Locale coverage must be total (SPA and server). The missing pl and ar `.resx` for this slice's resource classes:
  CLOSED 2026-11-08, files created (section 6). Hard-coded DTO grid titles remain open as a shared TODO.
- `AdminSecurityPage.vue`: first-touch by review order, so it belongs to `user-registry`, not this slice. Inherited
  here.

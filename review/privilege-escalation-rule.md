# Privilege-escalation rule (owner decision, 2026-11-08)

## Rule

Tenant-wide, landlord users exempt: a caller may not grant, or assign a Role carrying, a permission they do not hold;
and may not reset the password of, or mint an API key as, a user whose permissions are not a subset of the caller's own.

Effective permissions: the PermissionSpecification ids of active, undeleted grants of the active, undeleted Role of an
active, unlocked, undeleted user in an enabled tenant (`Jube.Data.Security.PermissionValidation`). Landlord users
resolve to every permission and are additionally short-circuited in the guard. For the target side ("carried"
permissions) the guard counts active undeleted grants of the target's Role regardless of `Role.Active` or `User.Active`,
so reactivating a Role or user cannot be used to bypass the rule; a target Role in a landlord tenant carries everything.

## Choke point

- `Jube.Service/Security/PrivilegeEscalationGuard.cs` (new): `MayGrant`, `MayAssignRoleAsync`, `MayActAsUserAsync`.
  Built per service from the caller's `Jube.Service.Security.PermissionValidation`, which gained `Holds` and `HoldsAll`.
- `Jube.Data/Query/GetCarriedPermissionsQuery/GetCarriedPermissionsQuery.cs` (new): tenant-scoped carried permissions of
  a Role or a user.
- Services call it next to `EnsurePermitted`, after validation and before the write. Every endpoint, agent tool and
  SignalR/change path goes through these services, so there is one rule, not per-endpoint copies.

## Paths covered

- `RoleRegistryPermissionService`: Insert (new spec), Update (new spec and the existing grant's spec), Delete (existing
  grant's spec).
- `UserRegistryService`: Insert (role), Update (only when the Role changes, including the caller's own account),
  UpdatePassword.
- `UserRegistryApiKeyService.InsertAsync`.
- Refusal: each slice's `ForbiddenException` with new key `InsufficientPrivilege` ("You do not have sufficient privilege
  to perform this action.") in `RoleRegistryPermissionResources`, `UserRegistryResources`,
  `UserRegistryApiKeyResources`, neutral plus pl, ar, ka, id, sv, ur, hi (`.fr.resx` untouched). Endpoints return a bare
  403, identical to a plain permission refusal, so nothing leaks over HTTP.

## Landlord role-active bug (added scope)

`IsLandlordAsync` now requires `rr.Active = 1`; an inactive landlord Role resolves as non-landlord with no permissions.
Edited surgically on top of the tenant-lockout changes. shell-auth finding 12 marked fixed.

## Further fixes (owner policy: fix bugs as found)

- Grant updates had no version check: `RoleRegistryPermissionRepository.UpdateAsync` now calls
  `OptimisticConcurrency.Verify` (the service and endpoint already mapped the conflict, 409). Existing tests that relied
  on this: service `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync` no longer sets `Version = 999` (that is now
  `UpdateWithAStaleVersionThrowsVersionConflictAndChangesNothingAsync`); pen
  `MassAssignment_OnCreateAndUpdate_IsIgnoredAsync` sends the real version 1 instead of 800.
- Locked Role did not freeze its grants: `ExistsRoleRegistryAsync` now excludes locked Roles (create and update refused
  with the existing RoleRegistryNotFound validation), and grant update and delete require the parent Role to be unlocked
  (NotFound). Tests: `GrantsOfALockedRoleCannotBeAddedChangedOrRemovedAsync`, pen
  `StalePut_IsAConflict_AndAGrantOfALockedRoleIsFrozenAsync`. Owner decision remaining: none; the choice that Locked
  means frozen is taken from the flag's documented meaning on other entities.
- A landlord switched into a tenant that is then deactivated or deleted stays pointed at it: `TenantRegistryRepository`
  now returns every `UserInTenant` row pointing at the tenant to the user's own Role tenant on deactivation (Update with
  Active off) and on delete. Test: `ALandlordSwitchedIntoATenantThatIsThenDisabledIsReturnedToTheirOwnTenantAsync`
  (TenantRegistryServiceTests).
- Additional tests to run: `TenantRegistryServiceTests`, `PenTestTenantRegistry`.

## Tests

Added (unrun): `Jube.Tests/Service/Security/PrivilegeEscalationServiceTests.cs` (21),
`Jube.Tests/Security/Identity/PenTestPrivilegeEscalation.cs` (6), three tests in `PermissionValidationTenantTests`
(landlord inactive role, landlord active role, non-landlord inactive role).
Changed deliberately (they asserted the old permissive behaviour, the authorised actor holding only 35 or 36 acted on
higher targets):

- `PenTestRoleRegistryPermission.RequestAsync` (used by `Matrix_IsEnforcedPerPermissionAsync`): grants and updates now
  use spec 36, the only permission the 36-only actor holds (was 3, 4, 5).
- `PenTestUserRegistry.Matrix_...`: the authorised 35-only call now targets an empty Role instead of RoleA.
- `PenTestUserRegistryApiKey.RequestAsync` (Matrix): victim user now in an empty Role instead of RoleA.
  Service tests were read and need no change (actors are self or empty-Role targets; specs 1 to 4 are held by the seeded
  actor).
  Tests to run: the three new/extended classes, `PenTestRoleRegistryPermission`, `PenTestUserRegistry`,
  `PenTestUserRegistryApiKey`, `RoleRegistryPermissionServiceTests`, `UserRegistryServiceTests`,
  `UserRegistryApiKeyServiceTests`, `RoleAllocationEffectTests`, `TenantLockoutTests`,
  `PermissionValidationTenantTests`.

## Gaps closed in the follow-up pass

1. Edits to a more privileged user: `UserRegistryService.UpdateAsync` (every edit, including Active, lock, email, name,
   not only role changes), `DeleteAsync` and `RevokeTokensAsync` now call `MayActAsUserAsync`;
   `UserRegistryApiKeyService.DeleteAsync` applies it to the key's owner. Self and landlord are exempt. Tenant mapping
   (`UserInTenantService`) is landlord-only already, and there is no administrator 2FA path (MFA is self-service in the
   authentication endpoints), so nothing more was needed there.
2. Preservation import (`Jube.Preservation.Preservation.ApplyWrapperAsync`): import and snapshot restore replace the
   tenant's Roles and grants. Before anything is written, a non-landlord caller must hold every permission carried by
   the Roles in the package and by the Roles about to be replaced (`GetCarriedPermissionsQuery.ForTenantAsync`),
   otherwise `PrivilegeEscalationImportException` is raised and mapped to the Preservation `ForbiddenException` with the
   existing generic PermissionDenied message (no new resource key; `PreservationResources` has no pl/ar/... files, a
   shared locale gap). The orphan recovery Role (`RecoverOrphanedUsersQuery`) used to grant every permission in the
   catalogue to orphaned users, including the importer; it now carries only the importer's own permissions (landlord:
   all). Caller permissions are read before the import deletes anything.
3. Role update and delete (`RoleRegistryService`): `MayEditRoleAsync` refuses rename, deactivate, lock, unlock and
   delete of a Role carrying a permission the caller lacks (new `InsufficientPrivilege` key, all locales). This also
   stops editing a Role to reach a more privileged user.
   New tests (unrun): seven in `PrivilegeEscalationServiceTests`, one pen test in `PenTestPrivilegeEscalation`, two in
   `PreservationServiceTests`.
   Owner decision remaining: whether a `PermissionDenied` message for the import is enough or a dedicated token is
   wanted (it would need new resx for the Preservation slice).

## Existing tests changed (old versus new)

| Test                                                                                                                                    | Old                                                                                         | New                                                           | Why                                                                   |
|-----------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|---------------------------------------------------------------|-----------------------------------------------------------------------|
| `PenTestRoleRegistryPermission.Matrix_IsEnforcedPerPermissionAsync` (via `RequestAsync`)                                                | 36-only actor grants specs 4, updates to 5, reads and deletes a grant of 3; expects success | all use spec 36                                               | 36-only actor now cannot grant, change or remove permissions it lacks |
| `PenTestRoleRegistryPermission.MassAssignment_OnCreateAndUpdate_IsIgnoredAsync`                                                         | PUT body carries `version` 800 and expects success and version 2                            | `version` 1                                                   | grant updates now check the version, a stale 800 is a 409             |
| `PenTestUserRegistry.Matrix_...`                                                                                                        | 35-only actor creates, resets and updates against RoleA victims                             | victims in an empty Role                                      | actor lacks RoleA's permissions                                       |
| `PenTestUserRegistryApiKey.RequestAsync` (Matrix)                                                                                       | key create and delete for a RoleA victim                                                    | victim in an empty Role                                       | same                                                                  |
| `PenTestUserRegistryRevokeTokens` (all `CreateUserAsync(TenantAId, RoleA.Guid)` victims)                                                | victims in RoleA, revoked by a 35-only administrator                                        | victims in an empty Role                                      | 35-only administrator cannot revoke a RoleA user now                  |
| `RoleRegistryPermissionServiceTests.UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                                              | sets `Version = 999`, expects success and version 2                                         | no version tamper (stale-version case is a separate new test) | version check                                                         |
| `PenTestUserRegistryRevokeTokens`, `PenTestRoleRegistry`, `UserRegistryServiceTests`, `RoleRegistryServiceTests`, `RoleGrantAccessBase` | read                                                                                        | unchanged                                                     | actors act on self, equal or empty-Role targets                       |

Possible existing breakage to watch in the serial pass: preservation tests where the importer's tenant holds Roles with
permissions the importer's own Role lacks, and `PenTestPreservation` (tenant A seed Roles include specs the pen
`WithPermission` user holds, expected fine).

## Docs

Users, Roles and Permissions pages updated with the rule and TODO markers; catalogue entries added in
`docs/Testing/ServiceAuthenticationAndUsers.md` and `SecuritySurfaceAndPenTest.md`.

## Risks

- Update/Delete of a grant also requires holding the grant's current permission (decision beyond the literal rule:
  otherwise a lacking grant could be revoked or re-pointed).
- A 36-only administrator can no longer manage most grants; real administrators need the permissions they delegate.
- Tests were not compiled or run; `PenTestPrivilegeEscalation` relies on base helpers copied from siblings.

## Not covered

- Session revocation by the user themselves and API key listing are unrestricted by design.
- Pen test for the import path (service tests only).
- Role insert is unrestricted (a new Role carries no permission).
- Tests unrun; the localised refusal reaches only service callers, HTTP clients get a bare 403.
  Tests to add to the run list: `PreservationServiceTests`, `PenTestPreservation`, `PreservationHarness` based suites,
  `RoleRegistryServiceTests`, `PenTestRoleRegistry`, `PenTestUserRegistryRevokeTokens`.

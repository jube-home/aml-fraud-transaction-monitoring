---
layout: default
title: Roles
nav_order: 2
parent: Administration
grand_parent: Configuration
---

# Roles

<!-- TODO(docs): the previous revision of this page described the Razor tree view (Administration >> Security >> Roles as a tree with New, Add and Back buttons) and its screenshots. Every sentence below has been rewritten from the DTO, the services and the Vue components, not from the running UI; the owner should read the page end to end against the running UI. -->

A Role is a named set of permissions. Users are not given permissions one by one: a User is allocated exactly one Role
(the Role field of the [User](../Users/index.html) record) and receives whatever the Role grants. Permissions are
allocated to the Role on the [Permissions](../Permissions/index.html) page. Roles are also allocated to individual
records such as Models, Case Workflows, Visualisations and Graphs, to decide who may use those records (see Allocating
Roles to Records below).

Roles belong to a tenant. A tenant administrator sees and changes only the Roles of their own tenant.

## Where to find Roles

Roles are one card of the Security story at Administration >> Security (route `/administration/security`). The card is
shown to a user holding permission 34 (Roles). The Users card needs permission 35 and the Permissions card permission
36;
the three are separate so that duties can be split.

<!-- TODO(screenshot): file=role-registry-security-story.png route=/administration/security state="Security story with the Roles card showing a count of roles, alongside Users, Permissions, Sign-ins and Sign-outs" -->
![Security story with the Roles card](role-registry-security-story.png)

Opening the Roles card shows the list of Roles in the tenant, with the Name and Active columns:

<!-- TODO(screenshot): file=role-registry-list.png route=/administration/security state="Roles list with three roles, one inactive" -->
![List of Roles](role-registry-list.png)

<!-- TODO(docs): confirm the list offers search, sort, CSV and Excel export as other lists do, and whether Version is hidden by default (the DTO hides it). -->

## Role properties

<!-- TODO(screenshot): file=role-registry-edit.png route=/administration/security state="An existing Role open: Identity group with Name, Active, Locked; collapsed Audit group; Users and Permissions cards beneath" -->
![Editing a Role](role-registry-edit.png)

| Field  | Group    | What it does                                                                                                                                      |
|--------|----------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| Name   | Identity | Display name shown when allocating Permissions or Users. Required, at most 256 characters, unique within the tenant, compared case-insensitively. |
| Active | Identity | When off, the Role grants nothing: every User holding it loses all permissions (see Active below).                                                |
| Locked | Identity | When on, the Role cannot be edited or deleted (see Locked below).                                                                                 |
| Audit  | Audit    | Created and updated user and date and version. Read-only, collapsed by default.                                                                   |

<!-- TODO(docs): the DTO also carries a Description property that is not stored and always returns null, the form does not show it. Confirm nothing in the user guide should mention it. -->

The record page also offers the Users in the Role (the child list of the Role) and, to a holder of permission 36, the
Permissions card for the Role.

### Validation

| Field | Refusal                                             |
|-------|-----------------------------------------------------|
| Name  | Required; at most 256 characters; unique in tenant. |

The same Name may be used by Roles in different tenants. The server refuses an invalid Role with a 400 response and the
field message, the page shows the message beside the
field. <!-- TODO(docs): confirm the wording of the messages in each language against the UI. -->

### Active

An inactive Role is ignored when permissions are resolved: the Users holding it have no permissions, cannot use any
page or API that is permission gated, and are treated as inactive. Reactivating the Role restores the permissions
immediately because permissions are read on every request, there is no cache to flush. A Role that is deleted behaves
in the same way as an inactive one.

The Landlord tenant is an exception. A User whose Role belongs to the landlord tenant is granted every permission in
the catalogue whatever the Role says, and the Role's Active switch and permission grants are not consulted.
<!-- TODO(docs): owner decision pending. This is the behaviour of PermissionValidation today (review/shell-auth.md finding 12): a landlord user whose Role is inactive still holds every permission. Reword this paragraph if the code is changed to honour Role.Active for landlords. -->

### Locked

A locked Role cannot be updated or deleted: both are refused as not found. Locking is a protection against editing, it
does not revoke anything, the Role keeps granting its permissions. The permissions of a locked Role can still be added
and removed on the Permissions page unless the individual grant is itself locked.
<!-- TODO(docs): confirm with the owner whether locking a Role should also freeze its grants. The code does not (RoleRegistryPermissionRepository.ExistsRoleRegistryAsync ignores Locked). -->

### Deleting a Role

Deleting is a soft delete (the row is kept, marked deleted, with who and when) and cannot be undone through the user
interface. A Role cannot be deleted while locked. Deleting a Role that Users still hold is not blocked: those Users keep
their record but hold no permissions until they are moved to another Role.
<!-- TODO(docs): confirm the warning wording the UI shows before deleting a Role that has Users. -->

### Concurrent edits

Each update carries the version it was loaded at. If someone else saved first, the server refuses the update with a
conflict and the page offers to reload. A version of zero skips the check, which is what an API client that does not
track versions sends. <!-- TODO(docs): confirm the conflict message shown by the page. -->

## Allocating Roles to Records

Besides allocating a Role to a User, a Role can be allocated to the records of several areas: Entity Analysis Models,
Case Workflows and their Statuses, Actions, Forms, Filters, Displays, Macros and XPaths, Visualisations, Visualisation
Datasources and Parameters, and Graphs. Those record pages show a Roles card with two lists, Available roles and
Assigned roles, and arrows to move Roles between them (the arrows mirror in right-to-left languages). Nothing is saved
until Save changes is pressed; Discard changes returns to the stored assignment. Only the differences are sent: one
create per Role added and one delete per Role removed.

<!-- TODO(screenshot): file=role-registry-allocation-transfer-list.png route=/case-workflows state="A Case Workflow Status open with the Roles card: two Roles assigned, three available, two unsaved changes" -->
![Allocating Roles to a record](role-registry-allocation-transfer-list.png)

Role allocation is for these records, not for Users. A User holds one Role, chosen in the Role field of the User record.
<!-- TODO(docs): confirm which record pages show the Roles card in the Vue UI and list them (the cards are driven by the form model of each area). -->

The server checks every assignment: the Role must exist in the caller's own tenant, so a Role of another tenant cannot
be allocated by guessing its guid. Each area applies its own permission for the allocation.

## Roles and Model Invocation Access

Beyond user interface permissions, a Role can also be allocated to
an [Entity Analysis Model](../../Models/Models/index.html), via the Role Manager control on the Model's own
configuration page. This is a separate concern from the Permission allocation: it does not affect what a signed-in
user can see or do in the user interface, it governs which users are allowed to invoke that specific model through the
API (`api/Invoke` and related model-scoped endpoints).

A User's own Role (set on the [User](../Users/index.html) record) determines the set of models they may invoke: the
engine continuously resolves, in the background, which Users fall under each Role allocated to a Model, and keeps that
resolved list synchronised as Users, Roles, and Role allocations change - there is no need to restart the engine or
explicitly resynchronise a model for a Role change to take effect. A request made with a valid identity (JWT or API Key)
for a User outside that resolved list is rejected in the same way as an unauthenticated request, regardless of whether
the calling User otherwise holds every relevant Permission.

This is the mechanism that makes it practical to share a single Model Guid across multiple tenants while still routing
each tenant's traffic only to the Models their Role is entitled to invoke -
see [Multi Tenancy](../../../Concepts/MultiTenancy/index.html) for the broader tenancy model this supports.
<!-- TODO(docs): retained from the previous revision, not re-verified in this review. -->

## Security notes

| Permission | What it allows                                                                     |
|------------|------------------------------------------------------------------------------------|
| 34         | Create, change and delete Roles of the tenant. Cannot grant permissions to a Role. |
| 36         | Grant and revoke permissions on Roles of the tenant. Cannot create Roles or Users. |
| 35         | Create Users and move them between the Roles of the tenant.                        |

Listing Roles (needed by pickers) is also allowed to holders of 34, 35 and 36 and of the workflow, case, visualisation
and model permissions that allocate Roles to records (6, 18 to 25, 31 to 33).

<!-- TODO(docs): confirm the intended reading of the list above with the owner; it is taken from the service code (RoleRegistryService.listPermissions) and from PenTestRoleRegistry.Matrix_IsEnforcedPerPermissionAsync. -->

Splitting 34, 35 and 36 across people gives separation of duties, but each of them is powerful in combination: a
holder of 36 can grant only the permissions they hold themselves, to any Role of the tenant including the Role they
hold, and can only change or remove a grant of a permission they hold.
No one may grant, or assign a Role carrying, a permission they do not hold themselves, and no one may reset the password
of, or issue an API key as, a user whose permissions are not a subset of their own. A refused request is answered as an
ordinary permission refusal (403) that does not say which permission or user was involved. Renaming, deactivating,
locking or deleting a Role is likewise refused when the Role carries a permission the caller does not hold, so a Role
cannot be edited to get at a more privileged user. Landlord users are
exempt. <!-- TODO(docs): owner to confirm the wording and where the rule is best explained; see review/privilege-escalation-rule.md. --> <!-- TODO(screenshot): file=roles-insufficient-privilege.png route=/administration/security state="Role grants card, a grant of a permission the signed-in user does not hold refused" -->

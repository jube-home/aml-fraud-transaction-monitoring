---
layout: default
title: Permissions
nav_order: 3
parent: Administration
grand_parent: Configuration
---

# Permissions

<!-- TODO(docs): the previous revision described the Razor tree (expand Administrator, Read Write Case, Delete button, Add and refresh) with Razor screenshots. It has been rewritten from the Vue components, the DTO and the services, not from the running UI; the owner should read the page end to end against the running UI. -->

Permissions grant access to resources in the user interface, typically pages, and to the API behind them. Permissions
are granted to Roles, and a Role is allocated to a User (see [Roles](../Roles/index.html)).

In practice, it is unlikely that a user with day to day responsibilities in customer services would require access
beyond Case Management, Lists, Dictionary or Override. Similarly, an analyst that has responsibility for the creation of
rule and reports is unlikely to need access to those pages enjoyed by customer service.

Most permissions grant access to a page. A small number instead gate a single action that is materially more
dangerous than the rest of the page, and are withheld by default. **Apply Force Override** is one of these: the
ordinary Override permission lets an operator mute an Activation Rule for a value, whereas Apply Force Override
lets them make one fire, raising response elevations, creating cases and sending notifications. **Import
Preservation Snapshot** is another: the ordinary Preservation permission lets an operator take a snapshot and
export, whereas this one lets them roll the whole tenant's configuration back to an earlier one. **Approve Case
Suppressions** is a third: a suppression requested from a case changes nothing in the engine until someone other than
the
person who made the request approves it, and this permission is what allows that approval. All three are granted on
upgrade to the Administrator role only, and must be granted explicitly to any other role that needs them.
<!-- TODO(docs): retained from the previous revision; confirm the three names and the upgrade statement. -->

Reprocessing is split across three permissions so that running a filter can be delegated without delegating its
editing: **Read Write Reprocessing Rule** (26) edits the rule of a filter, **Trigger Reprocessing Run** (73) starts a
run, and **Stop or Delete Reprocessing Run** (74) stops or deletes one. Any of the three allows reading both. On upgrade
every Role that held 26 was given 73 and 74 as well, with the same active state, so no Role gained or lost an ability;
see [Reprocessing Instances](../../Reprocessing/ReprocessingInstances/index.html).
<!-- TODO(docs): confirm the relative link and that the three names match the Roles screen. -->

In the absence of a permission the user will not see the menu entry. In the event of deep linking to a resource without
a
permission, an unauthorised http status code will be returned.

The Administrator role is created during migrations and has all available permissions allocated.

## Where to find Permissions

Permissions are a card of the Security story at Administration >> Security (route `/administration/security`), shown to
a user holding permission 36 (Permissions). Permissions can also be reached from the Role record, where a Permissions
card shows how many permissions the Role grants and opens the allocation for that
Role. <!-- TODO(docs): confirm how the Role record opens the Permissions card in the running UI. -->

<!-- TODO(screenshot): file=role-registry-permissions-card.png route=/administration/security state="Role record open with the Permissions card showing the number granted" -->
![Permissions card on a Role](role-registry-permissions-card.png)

## Granting and revoking permissions

Choose a Role (the Role selector is disabled when the page was opened from a Role record, and while there are unsaved
changes). The permissions are grouped by area, in this order: Cases, Rules, Models, Lists and data, Visualisation,
Security, Operations, Analytics, then Other for any permission the page does not know. Each permission has a switch,
its name and its number. The Find a permission box filters by name, and a badge shows how many permissions the Role has.

<!-- TODO(screenshot): file=role-registry-permission-allocation.png route=/administration/security state="Permissions for the Analyst role, grouped by area, three switches on, one locked grant, two unsaved changes" -->
![Permission allocation for a Role](role-registry-permission-allocation.png)

<!-- TODO(screenshot): file=role-registry-permission-search.png route=/administration/security state="Find a permission box with a search term and one matching group" -->
![Searching permissions](role-registry-permission-search.png)

Switching a permission on or off changes nothing on the server until Save changes is pressed. Discard changes returns to
the stored state. The page shows the number of unsaved changes. When saved, only the differences are sent:

| Change you made                         | What the server is asked to do                                                            |
|-----------------------------------------|-------------------------------------------------------------------------------------------|
| Switch on a permission never granted    | Create an active grant of that permission on the Role.                                    |
| Switch on a permission switched off     | Reactivate the existing inactive grant (a permission is never granted twice to one Role). |
| Switch off an active permission         | Delete the grant (soft delete).                                                           |
| Duplicate grants found for a permission | The extra rows are removed.                                                               |

A grant that is locked is shown with a Locked badge and its switch is disabled: it cannot be changed or removed here.

If any request fails the page reports the problem and reloads the stored grants, so the switches always show what is
stored. The requests are sent together, so a partial result is possible after an error: check the reloaded state.
<!-- TODO(docs): confirm the message shown on a refused save (403 or 400). -->

### Validation

| Field      | Refusal                                                                             |
|------------|-------------------------------------------------------------------------------------|
| Role       | Required, and must be a Role of the caller's own tenant.                            |
| Permission | Required, must exist in the catalogue, and must not already be granted to the Role. |

A grant naming a Role of another tenant, or a Role that does not exist, is refused with the same 400 response, so a
caller cannot tell which of the two it was.

### The catalogue

The list of permissions is fixed. It is created by database migration and cannot be changed through the user
interface or the API (the catalogue endpoint is read only). Two retired entries are not offered. The catalogue is the
same for every tenant. The number of a permission is the number every service checks.

<!-- TODO(docs): add a table of every permission number, name and area. It should be generated from the catalogue and from permissions/areas.ts rather than typed by hand; permissions without an entry in areas.ts appear under Other. -->

### When a change takes effect

Services read the permissions of the caller on every request, so a grant, revoke or deactivation applies to the next
request. The menu is built when the page loads, so a User whose Role changed sees the new menu after refreshing the
page.
<!-- TODO(docs): confirm the menu refresh behaviour in the Vue shell (Me query refetch interval). -->

A grant counts only when the grant is active, the Role is active and not deleted, the grant is not deleted, and the
User is active, not locked and not deleted. Locking a Role or a grant does not revoke it. Users of the landlord tenant
hold every permission whatever is granted (see [Roles](../Roles/index.html)).

## Security notes

Granting permissions needs permission 36 only, creating or changing Roles needs 34, and managing Users needs 35. A
holder of only 36 cannot create Roles or Users, and a holder of only 34 cannot grant permissions.

The page does not stop an administrator removing permission 36 from the Role they themselves hold; they would lose
access to this page on their next
request. <!-- TODO(docs): owner to confirm whether a warning or a refusal should be offered when removing the last Role grant of permission 36 or 34. -->

A holder of permission 36 can grant, change or remove only grants of permissions they hold themselves (landlord users
are exempt), so a holder of 36 alone cannot give themselves 34 or 35. Effective permissions are the active, undeleted
grants of the user's active, undeleted Role; a landlord whose Role is inactive has no permissions, exactly like any
other user. <!-- TODO(docs): owner to confirm this wording; see review/privilege-escalation-rule.md. -->

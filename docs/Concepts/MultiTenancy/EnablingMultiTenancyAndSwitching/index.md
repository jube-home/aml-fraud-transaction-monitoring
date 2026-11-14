---
layout: default
title: Enabling Multi Tenancy And Switching
nav_order: 2
parent: Multi Tenancy
grand_parent: Concepts
---

# Enabling Multi Tenancy And Switching

On each request the UserInTenant table is inspected for the authentication user to identify the allocated
TenantRegistryId:

``` sql
select * from "UserInTenant"
```

Returning the following data:

![Image](AllocatedUser.png)

Only a single instance of a given user name may be in use for the entire implementation of Jube. It is not possible to
switch users between tenant, except on the basis that the tenant that the user belongs to is designated as a Landlord,
which means that all authorisation mechanisms are bypassed, all permissions are available, across all tenants, for all
users belonging to that tenant.

The designation of a Tenant as being of type Landlord may only happen via database update. The following SQL will update
the Administrator Tenant to be a Landlord:

``` sql
Update "TenantRegistry" set "Landlord" = 1 where "Id" = 1
```

Assuming however the Landlord permission has been updated, it becomes possible to create new Tenants and move that user
between tenants, with total authorisation assumed. Switching requires the target Tenant to actually exist (and not be
deleted); requesting a switch to an unknown or removed Tenant Registry identifier is rejected with a validation error
naming the field, rather than a generic server error.

Upon Landlord having been designated, navigate to Administration >> Tenants:

![Image](ModelsUnderTheAdministratorTenant.png)

There already exists a default tenant that has been created by migration. In the absence of Landlord having been
allocated, the default tenant just remains the solitary tenant (it could be said single tenanted).

Note the button to the Administrator tenant:

![Image](LocationOfAdministratorLink.png)

Clicking on the button expands upon the properties of the tenant:

![Image](PropertiesOfTenant.png)

Locate the close button:

![Image](LocationOfBackButton.png)

Clicking on the Back button returns to the list of tenants:

![Image](ReturnedToListOfTenants.png)

To create a new Tenant locate the New button:

![Image](LocationOfNewButton.png)

Clicking on the New button to expose an empty tenant:

![Image](EmptyTenant.png)

Create a tenant titled Test as follows:

![Image](ExampleTenant.png)

Click the Create button to create a version of the tenant:

![Image](LoctionOfCreateButton.png)

Click close to return on save:

![Image](LoctionOfCloseAfterSave.png)

By way of illustration, navigate to Models >> Models:

![Image](ModelsUnderTheAdministratorTenant.png)

There are several models being returned as belonging to the Administrator tenant.

The availability of a new tenant allows for the switching to that tenant, given the Landlord
permission. Note since Landlord permission, in the header of the user interface, there is a tenant switcher:

![Image](LocationOfTenantSwitcher.png)

Opening the tenant switcher shows a search box and the tenants available. The selection is the current allocation of
user to tenant:

![Image](NewlyAssignedTenant.png)

In selecting a different tenant, the currently authenticated user will be allocated to the newly
selected tenant, with
the screen being refreshed:

![Image](SwitchedToTenant.png)

Given a new tenant, there is no data, as only data owned by this tenant is returned. All pages in the user interface may
be used in a manner as documented, with data being completely isolated from one tenant to another.

## Tenant properties

The Tenant form is laid out in four groups. Every property is set by a Landlord user only, as described
under [Rules enforced when managing tenants](#rules-enforced-when-managing-tenants).

| Group           | Property                             | Default  | Description                                                                                                                                                                                                                                                                                                                                                                                              |
|-----------------|--------------------------------------|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Identity        | Name                                 |          | Required, at most 256 characters, unique across the installation ignoring case. It may not start or end with a space and may not contain control characters or text-direction override characters (the Unicode left-to-right and right-to-left marks, embeddings, overrides and isolates, and the Arabic letter mark), so that two tenants cannot be made to look alike or reorder their displayed name. |
| Identity        | Active                               | Off      | Whether the tenant is available for use. A Landlord tenant may not be deactivated; the update is refused with a validation error. Deactivating a tenant returns every user who had switched into it to their home tenant (the tenant of their role).                                                                                                                                                     |
| Identity        | Locked                               | Off      | A locked tenant cannot be edited or deleted through the user interface or API. Unlocking is only possible by database update.                                                                                                                                                                                                                                                                            |
| Http adaptation | Http adaptation URL                  | Empty    | The base URL that the HTTP Endpoint of each of this tenant's HTTP Adaptations is appended to. Empty means the tenant uses the installation-wide `HttpAdaptationUrl`, timeout and certificate setting. See [HTTP Adaptation URL](#http-adaptation-url).                                                                                                                                                   |
| Http adaptation | Http adaptation timeout milliseconds | 0        | The timeout for this tenant's HTTP Adaptation calls. Zero uses the installation-wide `HttpAdaptationTimeout` (default 1000); otherwise between 100 and 30000. Only used when the tenant has a URL of its own.                                                                                                                                                                                            |
| Http adaptation | Http adaptation validate ssl         | On       | Whether the TLS certificate of the tenant's HTTP Adaptation endpoint is validated. Only used when the tenant has a URL of its own; otherwise the installation-wide `HttpAdaptationValidateSsl` applies.                                                                                                                                                                                                  |
| Data protection | Svirfd remote lookups enabled        | On       | Whether this tenant's IP addresses may be sent to the remote Svirfd service. See [Data protection: remote Svirfd lookups](#data-protection-remote-svirfd-lookups).                                                                                                                                                                                                                                       |
| Audit           | Created date, Created user           | Assigned | Read-only. Set when the tenant is created.                                                                                                                                                                                                                                                                                                                                                               |
| Audit           | Updated user, Updated date           | Assigned | Read-only. Set on each update.                                                                                                                                                                                                                                                                                                                                                                           |
| Audit           | Version                              | 1        | Read-only. Incremented on each update and used for the conflict check described below.                                                                                                                                                                                                                                                                                                                   |

The Landlord designation is not a property of the form. It is set by database update only, as shown at the top of this
page, and an update through the form or API leaves it as it was.

## HTTP Adaptation URL

An [HTTP Adaptation](../../../Configuration/Models/HTTPAdaptation/index.md) on a model does not hold a full address. It
holds an HTTP Endpoint, a path beginning with `/`, and the engine prepends a base URL to make the address it posts the
transaction to. By default that base URL is the installation-wide `HttpAdaptationUrl`
[Environment Variable](../../EnvironmentVariables/index.md), shared by every tenant. The HTTP Adaptation URL on the
Tenant record replaces it for that tenant alone, so each tenant's adaptations can be sent to that tenant's own scoring
service.

For example, with an HTTP Endpoint of `/api/score`:

| Tenant HTTP Adaptation URL    | Installation-wide `HttpAdaptationUrl` | Address called                          |
|-------------------------------|---------------------------------------|-----------------------------------------|
| Empty                         | `http://localhost:5001`               | `http://localhost:5001/api/score`       |
| `https://scoring.example.com` | `http://localhost:5001`               | `https://scoring.example.com/api/score` |

The URL should not end with a `/`, because the HTTP Endpoint already begins with one.

### Which settings apply

The three Http adaptation properties work as one set, switched on by the URL. The timeout and the certificate switch on
the Tenant record are ignored while the URL is empty.

| Setting                | Tenant URL empty                                               | Tenant URL set                                                                                                  |
|------------------------|----------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| Base URL               | `HttpAdaptationUrl`                                            | The tenant's HTTP Adaptation URL.                                                                               |
| Timeout                | `HttpAdaptationTimeout` (default 1000 milliseconds)            | The tenant's timeout, or `HttpAdaptationTimeout` when the tenant's timeout is zero.                             |
| Certificate validation | `HttpAdaptationValidateSsl` (default True)                     | The tenant's switch (default on).                                                                               |
| Address rules          | Trusted: may be http and may be a loopback or private address. | Held to the rules below, with `HttpAdaptationAllowInsecureTargets` (default False) as the development override. |

### Rules for the URL

| Rule                              | Detail                                                                                                                                                                                                                                                                          |
|-----------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Absolute `https` URL              | A relative address, `http` and any other scheme are refused.                                                                                                                                                                                                                    |
| No credentials                    | A user name or password in the URL (`https://user:secret@host`) is refused.                                                                                                                                                                                                     |
| No query string or fragment       | Anything after a `?` or `#` is refused. A path is allowed.                                                                                                                                                                                                                      |
| No backslash or control character | Refused anywhere in the URL.                                                                                                                                                                                                                                                    |
| Public host                       | `localhost`, any name ending `.localhost`, and a literal address that is loopback, link-local (including the cloud metadata address `169.254.169.254`), private or otherwise internal are refused, including unusual spellings of such an address (`2130706433`, `0x7f000001`). |
| Length                            | At most 2048 characters.                                                                                                                                                                                                                                                        |
| Timeout                           | Zero, or between 100 and 30000 milliseconds.                                                                                                                                                                                                                                    |

A URL that breaks a rule is refused when the tenant is saved, with the validation message "The HTTP Adaptation URL must
be an https URL to a public host, without credentials, a query string or a fragment." Leading and trailing spaces are
removed on save, and a blank URL is stored as empty.

The rules are applied three times, so that a URL that arrived other than through the form, or a host name later
repointed at an internal address, is still caught:

| When                  | What is checked                                                                                                                                   | If it fails                                                                                                                                              |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Saving the tenant     | The rules above, always in full. `HttpAdaptationAllowInsecureTargets` does not relax the save.                                                    | The save is refused with a validation error.                                                                                                             |
| Model synchronisation | The rules above, relaxed to allow http and internal hosts when `HttpAdaptationAllowInsecureTargets` is True.                                      | An error is written to the server log and none of that tenant's HTTP Adaptations are loaded. The tenant does not fall back to the installation-wide URL. |
| Making the call       | The host name is resolved and every address it resolves to is checked; the connection is made to a checked address. Redirects are never followed. | The adaptation is recorded with an Error and no Value, and the transaction carries on.                                                                   |

Because the save is always checked in full, `HttpAdaptationAllowInsecureTargets` only helps a tenant URL that was
written by database update; for development it is simpler to leave the tenant URL empty and use the installation-wide
`HttpAdaptationUrl`, which is trusted.

### When a change takes effect

The tenant's HTTP adaptation settings are read when models are synchronised, not on each transaction. After changing
them, synchronise the tenant's models for the change to be used. A tenant that has been deleted uses the
installation-wide settings.

The full behaviour of the call (response size limit, retries, circuit breaker, what is and is not sent) is described
under [HTTP Adaptation](../../../Configuration/Models/HTTPAdaptation/index.md).

## Rules enforced when managing tenants

Every operation on the Tenants page, and on the underlying `/api/TenantRegistry` routes, is restricted to users whose
own tenant is a Landlord. A tenant administrator holding every permission is refused (HTTP 403) on list, read, create,
update and delete, the Tenants menu entry is not shown to them, and opening `/administration/tenants` directly shows an
access denied message. There is no permission number for tenants; the Landlord designation is the only test.

Tenants are global rather than owned by another tenant, so there is no per-tenant scoping to bypass: a tenant that is
not the Landlord simply cannot reach the tenant registry at all.

Deleting a tenant is a soft delete: the row is flagged deleted and disappears from the list, the switcher and the switch
validation. A Landlord tenant and a locked tenant cannot be deleted. Deleting a tenant does not remove, deactivate or
move the users, roles, models and other data that belong to it, and it does not by itself stop those users signing in.

Updates carry the version number of the row being edited. If another administrator saved first, the update is refused
with a conflict (HTTP 409) and the page must be reloaded. Sending no version (or zero) skips the check.

Each update writes the previous row, including its HTTP adaptation and data protection settings, to the tenant version
history, and every list, read, create, update and delete writes an audit record, including refused attempts.

## Data protection: remote Svirfd lookups

The Tenant form has a Data protection group with one switch, Svirfd remote lookups enabled, on by default for every
tenant. Switch it off to stop the engine, the Svirfd page and the case journal sending that tenant's IP addresses to the
remote Svirfd service; local and directory lookups are unchanged. Like the HTTP adaptation settings it can be changed
only by a Landlord user, because the tenant's data-protection posture is decided by the platform operator, not by the
tenant, and like them it is read when models are synchronised.
See [Svirfd on models](../../../Configuration/Models/Svirfd/index.md) for what is sent and
when.

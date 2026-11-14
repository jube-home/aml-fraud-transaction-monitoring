---
layout: default
title: Users
nav_order: 4
parent: Administration
grand_parent: Configuration
---

# Users

All user interactions in the user interface are subject to authentication and authorisation. Authentication is rooted in
a
user account, which is more often than not accompanied by a password (keeping in mind the software also supports JSON
Web Tokens and API keys for authentication).

Users are allocated to Roles, which serve to consolidate authorisations upon authentication (in the form of
permissions).

<!-- TODO(docs): the previous revision of this page described the Razor tree view (roles as a tree, Get Temporary Password button, Complete the page for a new user). The Vue story page replaced it. Every sentence below has been rewritten from the DTO, the page and the services; the owner should read it end to end against the running UI. -->

## Opening the Users page

Navigate Administration >> Security >> Users. The Security story offers one card for each part of Security the signed-in
user may use. The Users card requires permission 35 (the Roles card requires 34 and the Permissions card 36). A user
holding none of the three sees an access-denied message instead.

<!-- TODO(screenshot): file=user-registry-security-story.png route=/administration/security state="Security story, Users, Roles, Permissions, Sign-ins and Sign-outs cards with counts" -->
![Image](user-registry-security-story.png)

Opening the Users card lists the users of the signed-in user's own tenant (a landlord user sees the users of their own
tenant, not of every tenant). Choose a user to open the record, or choose New to create one.

<!-- TODO(screenshot): file=user-registry-list.png route=/administration/security state="Users list, three users, one locked" -->
![Image](user-registry-list.png)

## The user record

| Value                                                 | Group    | Description                                                                                                                                                                                   | Example       |
|-------------------------------------------------------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| Name                                                  | Identity | The sign-in name. Matches the Name claim of the JSON Web Token issued at authentication.                                                                                                      | test          |
| Role                                                  | Identity | The Role the user is allocated to, chosen from the Roles of the tenant. Changing it moves the user from one Role to another and so changes the permissions they hold.                         | Administrator |
| Email                                                 | Identity | The email address of the account holder. It is not used to deliver the temporary password, which is shown once to the administrator who issues it.                                            | test@test.com |
| Active                                                | Identity | When off, sign-in is refused regardless of the password. New users default to inactive.                                                                                                       | On            |
| Wire password hash                                    | Security | When on, a temporary password issued by the administrator is salted with the user name and SHA-256 hashed before it is Argon2 hashed (the format some clients send). New users default to on. | On            |
| Password locked                                       | Security | Read-only. On when repeated failed sign-ins locked the account, or when the account was created. Cleared by issuing a temporary password.                                                     | Off           |
| Id, Created user, Created date, Version, Inherited id | Audit    | Read-only, server assigned. The Audit group is collapsed by default. Version is the optimistic concurrency token.                                                                             |               |

<!-- TODO(docs): confirm the Audit group order and the default of Active (the DTO says NewDefault false) against the running form; confirm whether Password locked is really read-only in the UI (FormField ReadOnly = true). -->

Validation:

| Value | Rule                                                                                                                                                                                                                           |
|-------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Name  | Required, 256 characters or fewer, and unique (case-insensitive) across the whole instance among users that are not deleted, because the name is the login identifier and users of different tenants share the one login page. |
| Email | Required, must be a valid email address, 256 characters or fewer.                                                                                                                                                              |
| Role  | Required, and must resolve to a Role Registry that exists in the caller's own tenant.                                                                                                                                          |

The server ignores any identity, tenant, created-by, created-date, version and inherited-id value sent in the
body of a create or update: they are set by the server. A user created through the page has no usable password and is
considered locked until a temporary password is issued.

<!-- TODO(screenshot): file=user-registry-new.png route=/administration/security state="New user form filled in for a user called Test, Identity and Security groups" -->
![Image](user-registry-new.png)

Choose Save to create the user. The list folds into a receipt and saving opens the new record.

<!-- TODO(screenshot): file=user-registry-created.png route=/administration/security state="Receipt after saving a new user" -->
![Image](user-registry-created.png)

Renaming a user is allowed. The name is the key the tenant allocation is held under, so the allocation moves with the
new
name in the same operation. Sessions and API keys issued under the old name are not carried over in any way the page
promises, so revoke sessions after a rename.

<!-- TODO(docs): confirm with the owner whether a rename should also revoke sessions and what happens to a JWT carrying the old name; behaviour was verified in code only (UserRegistryRepository.UpdateAsync). -->

Deleting a user is a soft delete: the user disappears from the list and can no longer sign in. The user cannot be
restored from the page.

<!-- TODO(docs): confirm the delete button location and the confirmation text; the delete does not revoke the API keys of the user from the page, they stop working because only keys of active, non-deleted users are loaded into the validation set. -->

## The cards on a user

Opening a user shows cards under the record.

| Card     | What it does                                                                                                          |
|----------|-----------------------------------------------------------------------------------------------------------------------|
| Role     | Shows the Role the user holds and opens it (when the signed-in user may use the Roles card).                          |
| API keys | Counts the keys held by the user and opens the key list.                                                              |
| Sign-in  | Shows a Locked pill when the account is locked, and has two buttons: issue a temporary password, and revoke sessions. |

<!-- TODO(screenshot): file=user-registry-cards.png route=/administration/security state="User record with Role, API keys and Sign-in cards" -->
![Image](user-registry-cards.png)

### Issuing a temporary password

Choose the key icon on the Sign-in card and confirm. A random 16 character password is generated on the server, stored
as an Argon2 hash (never in clear text) and shown once, with a Copy button, in a dialog. Closing the dialog removes it;
it cannot be shown again. The password is set to have already expired, which forces the user to change it on first
sign-in, and any lock on the account is cleared. The response is sent with caching disabled. The action requires
permission 35 and works only for users of the signed-in user's own tenant. It is not available to the AI agent tools.

<!-- TODO(screenshot): file=user-registry-temporary-password.png route=/administration/security state="Temporary password dialog showing a freshly issued password and its expiry" -->
![Image](user-registry-temporary-password.png)

To test the account, make a note of the password and sign out, then sign in as the new user:

<!-- TODO(screenshot): file=user-registry-sign-in-as-new-user.png route=/login state="Login page completed with the new user and the temporary password" -->
![Image](user-registry-sign-in-as-new-user.png)

<!-- TODO(screenshot): file=user-registry-forced-change.png route=/login state="Forced password change after the temporary password" -->
![Image](user-registry-forced-change.png)

Signing out clears the JSON Web Token from the header and cookies and revokes every browser and bearer session of the
user in the database (see Authentication Concepts). Upon change of the password the permissions are afforded as the Role
the user was allocated to.

### Revoking sessions

Choose the sign-out icon on the Sign-in card and confirm. Every browser and bearer (JWT) session of that user is refused
on its next request and the user must sign in again. API keys are not affected and are revoked separately from the API
key list. The action is recorded in the `UserLogout` table with the administrator as the user who cut it, and is
available for users of the signed-in user's own tenant only.

<!-- TODO(screenshot): file=user-registry-revoke-sessions.png route=/administration/security state="Confirmation dialog for revoking every session of a user" -->
![Image](user-registry-revoke-sessions.png)

### API keys

An API key lets a service call the API as the user, with that user's permissions. Open the API keys card to see the keys
held by the user: name, description, the first eight characters of the key, and when it was created.

<!-- TODO(screenshot): file=user-registry-api-keys.png route=/administration/security state="API key list for a user with two keys" -->
![Image](user-registry-api-keys.png)

To generate a key choose Generate key, give it a name (required, 256 characters or fewer) and optionally a description
(1024 characters or fewer), and choose Generate. The complete key is shown once, with Copy and Done buttons. Only a
SHA-256 hash of the key and its first eight characters are stored, so the key cannot be recovered: if it is lost, revoke
it
and generate another. The response is sent with caching disabled and key generation is not available to the AI agent
tools.

<!-- TODO(screenshot): file=user-registry-api-key-reveal.png route=/administration/security state="Newly generated API key shown once with the Copy and Done buttons" -->
![Image](user-registry-api-key-reveal.png)

To revoke a key choose Revoke on its row and confirm. Anything using the key stops authenticating at once and this
cannot
be undone. Revoking a key that is already gone, or one that belongs to another tenant, succeeds without doing anything.

<!-- TODO(screenshot): file=user-registry-api-key-revoke.png route=/administration/security state="Revoke confirmation for an API key" -->
![Image](user-registry-api-key-revoke.png)

<!-- TODO(docs): describe how a client presents the key (header name and format) and link to the API documentation; not verified in this slice. -->

## Tenants and moving users

A user belongs to the tenant of the Role they hold. Only a landlord user can move a user between tenants (the tenant
allocation, UserInTenant), and the Administration >> Security >> Tenants page owns that action. A user name is unique
across all tenants, so a name already in use in another tenant is refused with the same message as a duplicate in your
own tenant.

<!-- TODO(docs): link to the Tenants page once its docs are drafted. -->

## Sign-in audit

An authentication journal is available in the database which logs successful and unsuccessful logins, the local IP, the
remote IP and user agent of the browser. It is also shown on Administration >> Security >> Sign-ins. The audit is
available via selection of the UserLogin table:

```sql
select *
from "UserLogin"
order by 1 desc
```

Returning the following data:

<!-- TODO(screenshot): file=user-registry-login-journal.png route=/administration/security state="Result of select from UserLogin ordered descending" -->
![Image](user-registry-login-journal.png)

Sessions that are cut (a user logging out, an administrator using Revoke Sessions, a password change) are recorded in
the `UserLogout` table and shown on the Administration > Security > Sign-outs page. See the Logout Audit Trail in the
Authentication concepts page.

## Who may do what

Permission 35 is a powerful permission. A holder can create users, move users of the tenant to a Role, issue a temporary
password for a user, revoke any user's sessions and generate API keys that act as a user, but only within their own
privilege: the Role assigned (to a new user, to another user or to their own account) may carry only permissions the
holder also has, and a temporary password or API key may only be issued for a user whose permissions are all held by the
holder. Landlord users are exempt. A refusal is a plain 403 that does not say which permission or user was involved. The
same limit applies to editing (including deactivating or locking), deleting and revoking the sessions of a user, and to
revoking a user's API key: a user whose permissions are not all held by the caller cannot be touched. Rights over
yourself are never restricted.

<!-- TODO(docs): owner to confirm this wording; see review/privilege-escalation-rule.md. Importing a preservation package is held to the same rule, see the Preservation page. -->

In the event of all users having invalid credentials, the Command Line Interface (CLI) will need to be used to reset the
Administrator password. The CLI is documented separately.

<!-- TODO(docs): the eleven Razor-era PNGs beside this page (AuthenticationPage.png, CompletedForNewUserAuthentication.png, EmptyUser.png, ExampleUser.png, LocationOfPassword.png, LocationOfPasswordResetButton.png, LoginJournal.png, OnLoginOfTestUser.png, TopOfTreeForUsers.png, UpdatingPasswordForTestLogin.png, UserCreated.png) are no longer referenced; delete once the new screenshots are in. -->

<!-- TODO(docs): legacy statements retained from the Razor era that are not re-verified here: the CLI reset, and the default Administrator Role. -->

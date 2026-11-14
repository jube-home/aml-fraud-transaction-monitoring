---
layout: default
title: Login and Logout Audit
nav_order: 6
parent: Administration
grand_parent: Configuration
---

# Login and Logout Audit

<!-- TODO(docs): owner to confirm the page title and nav_order 6 (Users is 4, Outbound Host Policy is 5); slices.json lists no docs path for login-audit so one must be added -->

Jube keeps two permanent, read-only audit trails of authentication. The Login trail records every sign-in attempt,
successful or not, and the Logout trail records every session that was cut. Both are shown as a grid in
Administration >> Security.

<!-- TODO(docs): the Razor-era statements in Users/index.md ("An authentication journal is available in the database ... UserLogin table", with LoginJournal.png) describe a SQL query, not the page; decide whether to keep them or point at this page -->

<!-- TODO(screenshot): file=login-audit-security-hub.png route=/administration/security state="Security hub with the Users, Roles, Permissions, Sign-ins and Sign-outs cards, signed in as an administrator holding permissions 34, 35 and 36" -->
![Image](login-audit-security-hub.png)

## Who can see them

Both grids require permission 35, the same permission that governs Users. A person without it is not offered the
Sign-ins or Sign-outs cards. The server refuses the read with a 403 independently of the screen, and an unauthenticated
call is a 401.

<!-- TODO(docs): confirm permission 35 is named "Users" in the permission list shown to administrators; the slice inventory claims 34, which is Roles, and the code (UserLoginService, UserLogoutService, security/roots.ts) says 35 -->

A tenant administrator sees the rows of the users in their own tenant only. A landlord sees every tenant. Sign-in
attempts against a user name that does not exist belong to no tenant, so they are visible to a landlord only. The same
applies to sign-outs that belong to no tenant.

<!-- TODO(docs): verify the sentence about sign-outs belonging to no tenant against a real unauthenticated logout in a running instance -->

## Using the grid

Open Administration >> Security and choose Sign-ins or Sign-outs.

<!-- TODO(screenshot): file=login-audit-signins-grid.png route=/administration/security state="Sign-ins grid, a mix of successful and failed attempts, time range at its default" -->
![Image](login-audit-signins-grid.png)

| Control        | What it does                                                                                                                          |
|----------------|---------------------------------------------------------------------------------------------------------------------------------------|
| From and To    | Restrict rows by the time of the event. When omitted, each defaults independently to the last hour, which is why a new page is short. |
| Search         | Case-insensitive substring match. Sign-ins: user, IP, user agent, failure message. Sign-outs: user, cut by, IP, user agent, message.  |
| Apply, Clear   | Run the filters, or reset them.                                                                                                       |
| Refresh        | Reload with the current filters.                                                                                                      |
| CSV, Excel     | Export the rows currently loaded in the grid.                                                                                         |
| Column headers | Sort the grid. The default is the most recent first.                                                                                  |

<!-- TODO(docs): verify in the running UI that column header sorting is server-side (sortField/sortDirection) rather than in the browser, which matters for results capped by take -->
<!-- TODO(docs): state the row cap shown in the UI; the server clamps take to 100000 and defaults to 100000, and the grid shows a total count -->
<!-- TODO(docs): the statistics strip is empty for both trails by design (no continuous columns); confirm nothing is shown in the UI -->

The grids are read-only. Nothing can be added, changed or deleted through the screen or the API, and the trails are not
purged.

## Sign-ins

Source table `UserLogin`, API `GET /api/UserLogin`.

| Column                          | Meaning                                                                                       |
|---------------------------------|-----------------------------------------------------------------------------------------------|
| Created Date                    | UTC time of the attempt, shown in local time.                                                 |
| User                            | The user name the attempt was for. It is the raw supplied name even when no such user exists. |
| Failed                          | 1 for a failed attempt, 0 for a success.                                                      |
| Scheme                          | Username and Password, Negotiate (Windows/Kerberos), OAuth / OpenID Connect.                  |
| Failure Reason                  | Empty on success. Otherwise one of the ten reasons below.                                     |
| Failure Message                 | OAuth failures only: the identity provider's error or the token validation message.           |
| Remote IP, Local IP, User Agent | Recorded for the attempt when available.                                                      |

Failure reasons, from `UserLoginService.DescribeFailureReason`:

| Id | Reason                                                |
|----|-------------------------------------------------------|
| 1  | No User Registry found matching the supplied username |
| 2  | User Registry matched is not Active                   |
| 3  | User Registry matched is Password Locked              |
| 4  | Password has expired and must be changed              |
| 5  | Bad credentials (password did not match)              |
| 6  | No password was supplied                              |
| 7  | OAuth: no usable identity claim in principal payload  |
| 8  | OAuth: remote/identity-provider failure               |
| 9  | OAuth: local token validation failed                  |
| 10 | OAuth: internal error during processing               |

<!-- TODO(docs): the Authentication concepts page documents the same legend; keep one as the source of truth and link the other -->
<!-- TODO(docs): the DTO column order puts Scheme, Failure Reason, Failure Message, Remote IP, Local IP, User Agent after User and Failed; verify the order and visible titles in the running UI, including whether the Id columns are hidden -->

## Sign-outs

Source table `UserLogout`, API `GET /api/UserLogout`.

<!-- TODO(screenshot): file=login-audit-signouts-grid.png route=/administration/security state="Sign-outs grid showing a user logout, an administrator revoke and a password change, with Session length populated" -->
![Image](login-audit-signouts-grid.png)

| Column                          | Meaning                                                                         |
|---------------------------------|---------------------------------------------------------------------------------|
| Logged Out                      | UTC time the session was cut, shown in local time.                              |
| User                            | The user whose session ended.                                                   |
| Reason                          | Why it ended (the user, an administrator revoking sessions, a password change). |
| Outcome                         | How the cut went.                                                               |
| Session Started                 | When the session began, when known.                                             |
| Session                         | How long the session lived, in seconds. Empty when the start is unknown.        |
| Cut By                          | The user who cut the session, when it was not the user.                         |
| Message                         | Free text detail.                                                               |
| Remote IP, Local IP, User Agent | Recorded when available.                                                        |

Reasons and outcomes as asserted by `ListDescribesEveryReasonAndOutcomeAsync` in `UserLogoutServiceTests`:

| Id | Reason                             | Outcome                             |
|----|------------------------------------|-------------------------------------|
| 1  | User logout                        | Revoked                             |
| 2  | Tokens revoked by an administrator | No session (cookies cleared only)   |
| 3  | Password change                    | Revocation failed (cookies cleared) |

<!-- TODO(docs): the test pairs reason and outcome by id only to exercise the describers; confirm which reason and outcome combinations occur in practice -->
<!-- TODO(docs): confirm which reasons are written by which flows (logout page, Revoke Sessions, change password, expiry) before describing them -->

## Reading the trail

Repeated failures with reason 1 from one remote IP across many user names suggest user name enumeration. Repeated
failures with reason 5 against one user name, followed by reason 3, show the lockout described in the Authentication
concepts page.

<!-- TODO(docs): owner to confirm the investigative guidance above is wanted in the product docs and that reason 3 follows reason 5 per the lockout threshold -->

## Behaviour to know

- A user name is not hidden from the trail: the raw name typed is stored, including a mistyped password pasted into the
  user name field. Treat the trail as sensitive.
- The trails are not purged, so they grow without limit.

<!-- TODO(docs): confirm no retention or archival guidance exists elsewhere (Preservation?) that should be linked here -->

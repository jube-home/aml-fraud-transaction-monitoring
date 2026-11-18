---
layout: default
title: Authentication Concepts
nav_order: 2
parent: Concepts
---

# Authentication Concepts

The Jube user interface sits atop of an API stage that can be viewed by a Swagger interface as follows:

http://localhost:5001/swagger/index.html

The Swagger interface and the OpenAPI document describe every route, DTO and permission of the application, so they are only served to an authenticated caller: a browser with a signed-in session sees them, anyone else is challenged like any other protected page.

All but the Authentication endpoints, which exist for the purpose of establishing an identity in the first place, require an authentication token. The Swagger interface has an Authorize button, which allows for keys to be entered once and used throughout the Swagger session, and it offers two of them, being analogous to HTTP headers:

| Swagger key | HTTP header     | Value                                                                       |
|-------------|-----------------|-----------------------------------------------------------------------------|
| Bearer      | `Authorization` | The JWT token, after the word Bearer.                                       |
| ApiKey      | `x-api-key`     | An API Key, obtained by creating an API Key under a user in the application. |

Which is to say that a request carries one or the other in its headers, as follows for a JWT:

```http
GET /api/EntityAnalysisModel HTTP/1.1
Host: localhost:5001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

And as follows for an API Key:

```http
GET /api/EntityAnalysisModel HTTP/1.1
Host: localhost:5001
x-api-key: 1qlBx0PorwtctOV0dz4pwd1u2OijaWpuIrkS7PFohuNygukruZSXXmCu9egSgntZcAzv0FAnAcIB38T0y21eYXyv900k
```

The keys in the above examples are for illustrative purposes only.

Both methods of authentication are perfectly valid, and they share the same permissions approach, which means to say that both tokens only claim a user identity, with permissions being evaluated server side, and without the use of claims in the token.

JWT is not intended for API integration, notwithstanding that the user interface is a fascia of the API's, and the use of API Key is encouraged for all formal integration beyond the user interface.

## JWT Token Generation and Cookie Middleware

JWT Token Generation is not necessarily a visible concept, keeping in mind the remarks as to it being authentication made available for the user interface primarily. For completeness however, the user interface makes use of the following endpoint to trade a username and password for a JWT token:

```http
POST /api/Authentication/ByUserNamePassword HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "userName": "Administrator",
  "password": "Administrator",
  "newPassword": null,
  "repeatNewPassword": null,
  "passwordChangeState": false,
  "mfa": null
}
```

The above endpoint is highly inappropriate for use in external integration however, as it has a defined protocol that may signal password reset demands (which is not to say that an API Key does not tie to an account, but it is evaluated on a straight through real-time basis, and is not subject to expiry).

As the JWT Token Generation is intended for User Interface authentication, it also includes a step to pack the JWT Token into a cookie, and evaluates this cookie on each request, replacing the JWT token if close to expiry. For real-time integration especially, the Invoke endpoint, cookie maintenance is not appropriate.

## The sign-in page

The sign-in page is the first page of the user interface, and it is presented at the root of the site. Upon being opened it asks the server which methods of signing in are on, and it follows that the page takes one of three courses, as described under Authentication Schemes: the form for a User name and Password, a redirection to the identity provider of the organisation, or a silent sign-in by the computer itself.

For a User name and Password, signing in is arranged as a short sequence of steps, shown at the head of the form, of which only the first is always taken:

| Step         | Heading               | Presented when                                                                                                     |
|--------------|-----------------------|--------------------------------------------------------------------------------------------------------------------|
| Credentials  | Sign in               | Always. The User name and Password are entered, then Sign in.                                                      |
| Verification | Verify it is you      | Multi-Factor Authentication is on. A Verification code is entered, digits only, then Verify and continue.          |
| New password | Choose a new password | The password has expired or was issued as temporary. A New password is entered and confirmed, then Change password and sign in. |

Start again returns to the first step from either of the others. Where the details are refused the page states only that The details were not accepted, without distinguishing a wrong user name from a wrong password, a wrong code or a locked account, for the purposes of giving nothing away to a person guessing.

The messages that the sign-in page can present are as follows, and between them they are the whole of what a person signing in is told:

| Message                                                    | Presented when                                                                                                             |
|------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| Checking how you sign in…                                  | The page has just been opened and is asking the server which methods are on.                                               |
| Use your Jube account to continue.                         | Beneath the heading of the Credentials step.                                                                               |
| The details were not accepted. Check them and try again.   | The server answered 401: a wrong user name, password or code, or an account that is locked or not active.                  |
| The two new passwords do not match.                        | At the New password step, the New password and Confirm new password differ. Nothing is sent to the server.                 |
| The new password does not meet every requirement.          | At the New password step, for a user whose password is hashed in the browser, a rule under Your new password needs is not met. Nothing is sent. |
| The server did not accept the request.                     | The server answered 400 for a reason other than a missing code, the reasons it gave being listed beneath.                  |
| Jube could not be reached. Try again in a moment.          | No answer was had from the server, or it answered with a fault.                                                            |

While a step is being submitted its boxes and buttons are not available and Sign in reads Signing in. The Verification code box takes at most eight characters, and the Password box has a control to Show password or Hide password.

On a clean installation Username and Password is the scheme in use and Multi-Factor Authentication is off, hence only the Credentials step is met, followed by the New password step where the password of the account was issued as temporary.

{% comment %}SHOT id=concepts-authentication-sign-in
     route=/
     steps=sign-out
     frame=viewport
     state=the sign-in page at the Credentials step, with the User name and Password boxes empty and the Sign in button, and the steps indicator at the head of the form
     conveys=signing in to the user interface starts from a single form, which extends into further steps only where a code or a new password is called for
     alt=The Jube sign-in page showing the Sign in heading, the User name and Password boxes and the Sign in button{% endcomment %}

![The Jube sign-in page showing the Sign in heading, the User name and Password boxes and the Sign in button](concepts-authentication-sign-in.png)

## API Key Generation

API Keys are created for Users via the User Interface. Navigate through the menu to Administration, then Security, then Users, open the User, then the API keys hub card, which presents the panel Keys held by this user. A user can have many API Keys to facilitate rotation.

The panel lists the keys already held, with the following columns:

| Column      | Description                                                                         |
|-------------|-------------------------------------------------------------------------------------|
| Name        | The name given to the key, being where the key will be used.                        |
| Description | What the key is for, if given.                                                      |
| Key         | The first eight characters of the key only, as the remainder is not kept.           |
| Created     | The date and time the key was generated.                                            |
| Actions     | Revoke, upon which anything using the key stops authenticating at once.             |

Generate key presents two fields:

| Value       | Description                                                             | Example                   |
|-------------|-------------------------------------------------------------------------|---------------------------|
| Name        | A name for the key. Required, limited to 256 characters.                | Core Banking Integration  |
| Description | An optional description of the key, limited to 1024 characters.         | Invoke from core banking. |

Upon clicking Generate, the API Key is exposed in full, with a Copy button, under a heading that asks for the key to be copied now.

The API Key is not stored and this is the only time the API Key is available in the clear. The API Key is stored as a hash and is evaluated on a cryptographic basis. Keep the API Key safe, as it can't be recovered, but of course new API Keys can be rotated in.

Each API key encodes the user identity and a tamper-evident checksum, secured with HMAC-SHA256. When a key is included in a request, it is validated against an in-memory cache of active tokens, which is to say verifying the embedded checksum and confirming the key has not been revoked, before the request is processed. Keys are stored and compared by their SHA-256 hash, so the raw key is never retained server-side. The first eight characters of the key serve as a display-safe identifier for auditing and support purposes.

The active API Key cache is maintained in the application and refreshed each minute, while also subscribing to publish events emitted via the Resp cache for more timely synchronisation of API Key creation and revoke. Resp publish and subscribe is reliable, but not guaranteed, hence the fall back in synchronisation each minute. Publish and subscribe is most material in revoke events.

The API Key depends on a secret set in the Environment Variable `ApiHmacKey`, which is intrinsic to the application's security and the integrity of the API Key. Where `ApiHmacKey` is not set no API Key can be issued, and the creation of one is refused with the message API keys cannot be issued because the ApiHmacKey Environment Variable is not set on this installation.

In the case of no active key found or cryptographic evaluation failure, the authentication is failed for the same outward reason of "API Key Not Found".

Generating, listing and revoking API Keys requires permission 35, Read Write Security User.

{% comment %}SHOT id=concepts-authentication-user-api-keys
     route=/administration/security
     steps=open-hub:Users, open-row:Administrator, open-hub:API keys
     frame=page
     state=the Administrator user opened in Security, with the panel Keys held by this user listing two keys, each showing its name, description, the first eight characters of the key and its created date
     conveys=API Keys belong to a user, many can be held at once, and only the first eight characters of each remain visible
     alt=A user opened in Security with the API keys panel listing the keys held by the user, each with a name, a description, a truncated key and a Revoke action{% endcomment %}

![A user opened in Security with the API keys panel listing the keys held by the user, each with a name, a description, a truncated key and a Revoke action](concepts-authentication-user-api-keys.png)

## Order of Precedence

The authentication middleware handles tokens in the following order of precedence, and will evaluate exclusively:

* x-api-key.
* bearer (JWT).
* cookie (JWT).

Everything above this point concerns how a request is authenticated once the identity of a user has already been established. The sections below cover the three ways that identity can be established in the first place, being Jube's own Username and Password, Negotiate (Windows Integrated or Kerberos), and OAuth or OpenID Connect, along with Multi-Factor Authentication, which layers on top of the first two. In every case, once identity is established Jube issues its own JWT and cookie exactly as described above, and it follows that none of the three schemes changes how the rest of the API is authenticated afterward.

## Authentication Schemes

Jube supports three mutually exclusive schemes for establishing the identity of a user at login. Username and Password is Jube's own built-in scheme, whereas OAuth and Negotiate are alternatives that delegate the verification of identity elsewhere. OAuthAuthentication and NegotiateAuthentication cannot both be True at once, and Username and Password login (including password changes) is disabled whenever either of the other two is enabled, which is to say that there is no scenario where more than one scheme is live at a time.

### OAuth / OpenID Connect

Setting OAuthAuthentication to True enables OAuth 2.0 / OpenID Connect as the login scheme, using the authorization code flow with PKCE. The identity provider is configured via OAuthAuthority, OAuthClientId and OAuthClientSecret, as described in [Environment Variables](../EnvironmentVariables/index.html).

When OAuth is enabled, the sign-in page asks `GET api/Authentication/Methods` which methods are on and, finding OAuth, sends the browser to `GET api/Authentication/OAuth?redirectUrl=...` rather than showing Jube's own form, stating in the meantime that it is Taking you to your organisation's sign-in, with a Continue to sign-in link should the browser not follow of its own accord. That endpoint starts the identity provider challenge and returns the person to the page they asked for. Only a local return address is honoured, such that an address that is not a path on this site (for example one beginning `//`, or carrying a scheme) is replaced by the home page, and the endpoint answers 404 unless OAuth is on and 409 if OAuth and Negotiate are both on.

On successful sign-in, Jube looks up the returned identity claim (trying, in order, the claims a provider is most likely to populate) against the UserRegistry. The user must already exist and be active in Jube, keeping in mind that OAuth verifies who the person is, and does not create an account for them. If no matching active user is found, sign-in fails and the browser is returned to the sign-in page.

Jube continues to issue its own JWT and session cookie after a successful OAuth sign-in, exactly as it does for Username and Password or Negotiate, which is to say that the verification of identity is delegated to the OAuth provider, but the issuing of the session and token is not.

Multi-Factor Authentication is assumed to always be handled by the OAuth identity provider itself, and Jube does not add a second step of its own on top of an OAuth sign-in in the manner that it can for Negotiate, as described below.

By default the callback response uses OpenID Connect's form_post mode, while OAuthForceGet switches to a GET and query string response for identity providers that require it. Redirection after login is restricted to local URLs only (so a user returns to the page they were on before being redirected to sign in), guarded against open redirect attempts.

The `OAuthForceRedirect` Environment Variable provides the option to ignore the RedirectUrl propagated through OAuth, and override it with an explicit redirect, so that a singular landing page can be enforced.

### Negotiate (Windows Integrated / Kerberos)

Setting NegotiateAuthentication to True enables Negotiate authentication, commonly known as Windows Integrated Authentication against Active Directory, as an alternative to Username and Password. This is common in corporate environments where the browser can silently present the credentials of the signed-in Windows user.

The sign-in page, having read `GET api/Authentication/Methods`, shows no form and immediately calls `GET api/Authentication/ByNegotiate` in the background. That endpoint requires Negotiate authentication, so a browser configured for integrated authentication (for example, a domain-joined machine with the site in its trusted intranet zone) completes the Kerberos or NTLM handshake transparently and the call succeeds without the user seeing a login form at all, whereas a browser that cannot complete the handshake gets a 401. Because Negotiate replaces the password login rather than sitting beside it (`ByUserNamePassword` answers 404 while Negotiate is on), the page does not offer a form in that case: it states that the computer did not sign the person in automatically, and that the installation does not accept a password, and offers to Try again. If Multi-Factor Authentication is also enabled, a successful Negotiate handshake returns 202 instead of signing in directly, and the page asks only for the Verification code, which it posts to `POST api/Authentication/ByNegotiateMfa`.

### Multi-Factor Authentication

Setting EnableMultifactorAuthentication to True adds a second step, a one-time code, after Username and Password or Negotiate sign-in. It is never added after OAuth, which is assumed to cover MFA at the identity provider. Which second factor is asked for is chosen by MultifactorAuthenticationProvider, and two are shipped:

| MultifactorAuthenticationProvider | Second factor                                                        | Where the code is checked                    |
|-----------------------------------|----------------------------------------------------------------------|----------------------------------------------|
| `Rsa` (the default)               | An RSA SecurID one-time passcode.                                    | An RSA Authentication Manager, over HTTPS.   |
| `Totp`                            | A time based one-time password from an authenticator app (RFC 6238). | Inside Jube, against a secret held per user. |

Any value other than `Totp` selects RSA. The login logic does not know which factor is in use, as it hands a user name and a code to a verifier and gets back yes or no, and it follows that everything under Common Behaviour below is the same for both, and for any factor added later. RSA SecurID is described after that as the worked example of a factor, followed by the authenticator app.

#### Common Behaviour

For Username and Password, the code travels in the `Mfa` field of the same `POST api/Authentication/ByUserNamePassword` request as the user name and password, which is to say that there is no separate MFA endpoint and no half signed-in state held between two requests. With MFA on, a request that carries no code is refused with 400 naming `mfa`, before any credential is checked, so nothing is counted and nothing is written to the Login Audit Trail. The sign-in page uses that answer to present its Verification step and then sends the user name, password and code together. The password is always checked first, such that a wrong password, or an account that is not active or is locked, is refused without the verifier being called.

For Negotiate, a successful handshake at `GET api/Authentication/ByNegotiate` answers 202 rather than signing in, and the page posts the code to `POST api/Authentication/ByNegotiateMfa`, which repeats the handshake and then checks the code. A post with no code answers 202 again and is not counted.

A wrong code answers 401, the same answer as a wrong password, and is counted against the same counter as wrong passwords (see Login Lockout and Session Behaviour), henceforth codes cannot be guessed indefinitely and mixing wrong passwords with wrong codes does not dodge the lock. It is recorded in the Login Audit Trail with `FailureTypeId` 11.

A right code completes the sign-in, resets the counter and issues the JWT and cookie exactly as a sign-in without MFA would. For Username and Password the success row in the Login Audit Trail is written only at this point, not when the password alone was correct.

A verifier that cannot answer (for example where the RSA Authentication Manager is unreachable or returns an error) is treated as a fault, not as a wrong code: the request answers 500, nobody is signed in, and the attempt is given back, for the purposes of an outage not locking users out.

#### Interfaces and Adding a Factor

There are two interfaces, at two levels:

| Interface      | Project                                      | Contract                                                                                                                                                               | Implemented by                      |
|----------------|----------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------|
| `IMfaVerifier` | `Jube.Service` (`Authentication/Interfaces`) | `Task<bool> VerifyAsync(string userName, string code, CancellationToken token)`. The only thing the login logic depends on.                                            | `RsaMfaVerifier`, `TotpMfaVerifier` |
| `IMfaProvider` | `Jube.Mfa` (`Interfaces`)                    | `Task<MfaVerificationResult> VerifyAsync(MfaVerificationRequest request, CancellationToken token)`. A client for an external verification service, free of Jube types. | `RsaSecurIdMfaProvider`             |

`IMfaVerifier` is the seam. A verifier returns true for a code it accepts, false for one it refuses, and throws only when it could not reach a verdict. `IMfaProvider` is the richer shape for a factor that is checked by a remote service: an `MfaVerificationRequest` carries a `SubjectName`, a list of `MfaFactor` (each a `MethodId` and a `Value`) and free `Metadata`, and an `MfaVerificationResult` carries `IsSuccessful`, the service's own `ResponseCode` and `Metadata`.

The two shipped factors show the two ways of filling the seam:

* RSA is a remote factor, so it uses both levels. `RsaMfaVerifier` (in `Jube.App`) reads the settings, builds an `RsaSecurIdMfaProvider` (in `Jube.Mfa/Rsa`), sends one factor with the method `SECURID` and returns the provider's `IsSuccessful`.
* The authenticator app is a local factor, so there is no remote service and it does not implement `IMfaProvider`. `TotpMfaVerifier` (in `Jube.App`) reads the user's secret and checks the code itself with `TotpAuthenticator` and `TotpSecretProtector` (in `Jube.Mfa/Totp`).

To add another factor, it is a matter of implementing `IMfaVerifier`, following `RsaMfaVerifier` for a remote service or `TotpMfaVerifier` for one checked inside Jube, and registering it in the application's service collection. A registered `IMfaVerifier` is used for every MFA check and takes the place of the choice made by MultifactorAuthenticationProvider, and only when none is registered, which is how Jube ships, does that setting choose between the two built in. Lockout, the audit trail and the endpoints above then apply to the new factor without further work. A factor that needs its own set up step, as the authenticator app does, has to bring that itself.

#### RSA SecurID

With MultifactorAuthenticationProvider left at `Rsa`, the code is an RSA SecurID one-time passcode and Jube asks an RSA Authentication Manager whether it is right. The endpoint is configured via MultifactorAuthenticationEndpoint, MultifactorAuthenticationApplicationId and MultifactorAuthenticationClientKey, as described in [Environment Variables](../EnvironmentVariables/index.html). There is nothing to set up per user in Jube, as tokens are assigned in RSA, and the Jube user name is sent as the RSA subject name, so the two must match.

For each check Jube posts a JSON document to MultifactorAuthenticationEndpoint, with MultifactorAuthenticationClientKey in the `client-key` header and MultifactorAuthenticationApplicationId as the `clientId`, the user name as the `subjectName` and the code as a `SECURID` credential. The code is accepted only when the response's `attemptResponseCode` is `SUCCESS`, any other response code is a wrong code, and a response that is not a 2xx, or no response at all, is a fault as described under Common Behaviour.

MultifactorAuthenticationEndpoint defaults to `http://localhost:5001/api/mfa`, which resolves to Jube's own bundled mock RSA endpoint (it accepts only the fixed OTP value `12345678`, and only while MultifactorAuthenticationClientKey is left at its own shipped default). The mock is not mapped unless both EnableMockEndpoints and EnableMockMfaEndpoint are True, and EnableMockMfaEndpoint defaults to False, so a default deployment never exposes an anonymous verifier with a hard-coded passcode. For development, set EnableMockMfaEndpoint to True alongside EnableMultifactorAuthentication. For production, point MultifactorAuthenticationEndpoint at a real RSA Authentication Manager and leave EnableMockMfaEndpoint False.

For outbound calls to the RSA endpoint, OutboundHttpRsaAmCertificateBypass disables TLS certificate validation entirely (to be avoided in production), and OutboundHttpRsaAmCertificateThumbprint instead pins trust to one specific certificate by its SHA-1 thumbprint, tolerating chain-trust errors only, which is the safer option of the two where the RSA endpoint sits behind a certificate that doesn't chain to a publicly trusted root. Both are currently implemented for the RSA MFA endpoint only, not for HTTP Adaptation or other outbound webhooks. Generally speaking, the cause of a certificate error should be fully understood, and while in middleware it is commonplace, it will nonetheless require some explanation.

#### Authenticator App (TOTP)

Set EnableMultifactorAuthentication to True, MultifactorAuthenticationProvider to `Totp` and MfaSecretEncryptionKey to a long random value kept with the other secrets. Any authenticator app that supports the standard (Google Authenticator, Microsoft Authenticator, Authy, 1Password and others) works: codes are six digits, change every 30 seconds, and a code from one step either side of now is accepted to allow for clock drift. A code is accepted once only, which is to say that after a code is used, that code and any earlier one are refused, so a code seen over a shoulder or in a log cannot be replayed. MfaTotpIssuer (default `Jube`) is the name the app shows beside the account.

Setting up takes place on the sign-in page, where a user who enters their user name and password reaches the Verification step. If they have no authenticator yet they choose Set up an authenticator app, which asks the server (`POST api/Authentication/TotpEnrolment`, after checking the password, with the same lockout and rate limiting as signing in) for a new secret. The page presents a QR code, and beneath it the secret as text under the label Or type this key into the app. The user scans or types it into their app and enters the six digit code the app shows as the Verification code. That first valid code completes set up and signs them in. Until it does, set up is pending and choosing to set up again replaces the pending secret. Once set up is complete, the option no longer works for that user: it answers that An authenticator has already been set up for this account, because otherwise anyone who learned the password could attach their own phone.

Upon the loss of a phone, an administrator having permission 35, Read Write Security User, resets the authenticator of the user. Navigate through the menu to Administration, then Security, then Users, and open the User: the Sign-in tile of the user offers Reset authenticator, alongside Temporary password and Revoke sessions, and seeks confirmation before doing so. The user is then asked to set up again at their next sign-in. A reset should be preceded by checking who is asking, since it removes the second factor.

The secret is kept in UserRegistry.TotpSecret, encrypted with AES-GCM under a key derived from MfaSecretEncryptionKey, and is never returned by any API after set up. TotpEnrolledDate records when the user proved their authenticator and TotpLastAcceptedStep records the last accepted time step. Changing MfaSecretEncryptionKey makes every stored secret unreadable, and it follows that every user must set up again.

The authenticator app is to be used with Username and Password only. Set up proves a password, so `TotpEnrolment` answers 404 while Negotiate or OAuth is on and the sign-in page offers no set up there. Selecting `Totp` together with Negotiate is not a working combination: `ByNegotiateMfa` still checks codes with the authenticator app verifier, but no user can set one up, so every code is refused and counts towards the lockout. A user who has no authenticator, and any failure reading or decrypting a stored secret, is likewise a refused code rather than a fault.

Several limitations follow from the design as it stands, and should be kept in mind when choosing the authenticator app:

* First-time set up is protected only by the password, so a password known to an attacker who signs in before the real user lets the attacker attach their own phone.
* MfaSecretEncryptionKey has no check at startup, such that with `Totp` selected and the key unset, set up fails with a server error and verification fails closed.
* Recovery codes are not issued, the reset by an administrator being the only route back for a user without their phone.

{% comment %}TODO(verify): owner decisions outstanding on the authenticator app, carried from the previous version of this page: how a Negotiate user is to enrol (an administrator provisioning link is the likely answer), whether first-time set up should need an administrator issued code or an already signed-in session, whether a startup validation of MfaSecretEncryptionKey is to be added, and whether recovery codes are to be issued. The limitations above are to be revised upon each being decided.{% endcomment %}

### Login Lockout and Session Behaviour

PasswordAttempts (default 3), controlled in the Environment Variable `PasswordAttempts`, is the number of consecutive failed attempts at which an account is locked: with the default value the account locks on the third consecutive failure. With Multi-Factor Authentication on, a wrong code is a failed attempt exactly as a wrong password is, both feed the one counter, and the counter is reset only when the whole sign-in, code included, succeeds. Once locked, every attempt is refused, the right password and the right code included, until the lock is lifted by an administrator or, where EnablePasswordLockoutReset is True, expires after `PasswordLockoutInterval`/`PasswordLockoutIntervalValue`. A locked account carries a Locked badge on the Sign-in tile of the user, and the issuing of a Temporary password from that tile unlocks it.

The `SessionCookie` Environment Variable (default True) controls whether the authentication cookie is a browser-session cookie, cleared when the browser fully closes, rather than persisting per its own expiry regardless of the browser closing.

### Session Lifetime, Refresh and Revocation

The JWT issued at login (and carried in the `authentication-jwt` cookie) is valid for 15 minutes. Each authenticated request made through the user interface replaces it with a fresh 15 minute token, so an active session slides forward. A session is never extended beyond `SessionAbsoluteLifetimeInterval`/`SessionAbsoluteLifetimeIntervalValue` (default `n`/720, twelve hours) counted from the original login, because every refreshed token carries the instant the session began: after that age the user must log in again, however active they were.

The user interface follows the expiry of the session from the `X-Session-Expiry` header returned with each response, rather than by asking the server, as asking would itself slide the session forward. It follows that:

* Activity in the page (a click, a change or the submission of a form) extends the session in the background, by a call to `GET /api/Session/Extend`, at most once every three minutes.
* Where five minutes or fewer remain, a warning is presented across the head of the page, Your session is about to expire, with an Extend session button.
* Upon the session expiring, the user is returned to the sign-in page with the notice Your session has expired. Sign in again to continue, and is returned to the page they were on after signing in.

Tokens are also revocable. `UserRegistry.TokensValidFrom` holds, per user, an instant before which every JWT is refused with a 401 on its next request. It is read from the database on every authenticated request, so a revocation takes effect on every node at once without any shared cache. The instant is set when the user logs out, when the password of the user is changed, and when an administrator uses Revoke sessions on the Sign-in tile of the user (Administration, then Security, then Users, permission 35, Read Write Security User, `POST /api/UserRegistry/RevokeTokens/{id}`, which answers 204 and only reaches users of the caller's own tenant). API Keys are not affected by any of this: they are validated separately, on every request, against the key list, and are revoked from the API keys panel of the user. Open streaming connections (Watcher, Service Change) are re-checked against the same rules every `RevokedSessionCheckInterval`/`RevokedSessionCheckIntervalValue` (default `s`/30) and closed when their session has ended.

Logging out is described under Logout and the Logout Audit Trail below.

### Login Audit Trail

Every sign-in attempt, successful or not, and regardless of which of the three schemes was used, is recorded to the `UserLogin` table. It is browsable by navigating through the menu to Administration, then Security, then Sign-ins, or directly via `GET /api/UserLogin`, which follows the same conventions as every other observability grid (`take`/`from`/`to`/`search`/`samplePercentage`, CSV export, gated by permission). A tenant administrator sees the sign-ins of their own tenant only, whereas a landlord sees all of them, and failed attempts against a user name that does not exist are shown to a landlord only. It is not purged: unlike the ephemeral operational telemetry elsewhere (the Resp and Postgres metrics, and so on), the Login Audit Trail is a permanent record. It can equally be queried directly, for example:

```sql
select "CreatedUser", "RemoteIp", "AuthenticationTypeId", "Failed", "FailureTypeId", "FailureMessage", "CreatedDate"
from "UserLogin"
order by "CreatedDate" desc
```

`AuthenticationTypeId` records which scheme was used:

| Value | Scheme                       |
|-------|------------------------------|
| 1     | Username and Password        |
| 2     | Negotiate (Windows/Kerberos) |
| 3     | OAuth / OpenID Connect       |

`FailureTypeId` is only populated when `Failed` is set, and records why:

| Value | Meaning                                                                                                                  |
|-------|--------------------------------------------------------------------------------------------------------------------------|
| 1     | No User Registry found matching the supplied username.                                                                   |
| 2     | The User Registry matched is not Active.                                                                                 |
| 3     | The User Registry matched is Password Locked.                                                                            |
| 4     | Username/Password only: the password has expired and must be changed, but no NewPassword was supplied.                   |
| 5     | Username/Password only: the supplied password did not match (bad credentials).                                           |
| 6     | Username/Password only: no password was supplied at all.                                                                 |
| 7     | OAuth only: the identity provider's ticket contained no usable identity claim. See the note on earlier releases below.   |
| 8     | OAuth only: a remote/identity-provider failure (the IdP itself returned an error, e.g. access denied, misconfiguration). |
| 9     | OAuth only: local authentication/token-validation failed (e.g. an expired or malformed token).                           |
| 10    | OAuth only: an internal error occurred while processing the ticket (e.g. a database error validating the user).          |
| 11    | Username/Password or Negotiate with Multi-Factor Authentication: the one-time code was not accepted.                     |

Earlier releases recorded a refused Multi-Factor Authentication code as `FailureTypeId` 7, the same value as the OAuth claim failure. Those rows are left as they were written, the Login Audit Trail being a permanent record, and are told apart by `AuthenticationTypeId`: 7 with scheme 1 or 2 is a refused code, 7 with scheme 3 is the OAuth failure. The Sign-ins page and `GET /api/UserLogin` describe them accordingly. A query that filters on `FailureTypeId` alone should take both 7 (with scheme 1 or 2) and 11 as refused codes.

`FailureMessage` is populated for OAuth failures only (`FailureTypeId` 7 to 10), being the identity provider's own error message or the message of a token-validation exception. Username and Password and Negotiate failures already have a small fixed `FailureTypeId` vocabulary that needs no free text, so `FailureMessage` is left null for those.

Every OAuth failure branch (no usable claim, no matching or active User Registry, a database error, a remote or IdP failure, a local token-validation failure, or any other unexpected exception) records a `UserLogin` row with `Failed = 1` and an appropriate `FailureTypeId`, exactly as Username and Password and Negotiate do.

### Logout and the Logout Audit Trail

Logging out is `POST /api/Authentication/Logout`. It is anonymous by design and fails closed: whatever the state of the credential presented (none, malformed, expired, already revoked), the response deletes the `authentication-jwt` cookie and answers `200`. When a valid session is presented, every token the user has been issued so far is revoked in the database as described above (so it is honoured on every node) and the open streaming connections of the user are closed. API keys are not touched, and a request that carries an `X-API-KEY` header is not treated as a browser session. If the revocation cannot be written the browser is still logged out, the response carries an `X-Jube-Logout-Warning: revocation-failed` header and the reason is in the application log at `ERROR`, because a copied token then stays valid until it expires.

There is no antiforgery token on logout. A logout is protected against cross-site requests by the same rule that protects every other cookie-authenticated `POST` (see `CsrfOriginCheck`): a request that carries the session cookie and is not provably same-origin (`Sec-Fetch-Site`, `Origin` or `Referer`) is refused with `403`. It is a POST, never a GET, so that a web page cannot log a user out by embedding a link. In the user interface, Sign out in the Account menu posts to the endpoint and returns to the sign-in page, which then confirms You have signed out.

Every session that is cut is recorded in the `UserLogout` table, browsable by navigating through the menu to Administration, then Security, then Sign-outs, or via `GET /api/UserLogout` (the same conventions and permission as the Login Audit Trail, such that a tenant administrator sees the sessions cut for the users of their own tenant, and a landlord sees all of them). Each row records who, when, why (`ReasonId`: 1 the user logged out, 2 an administrator revoked the tokens of the user through Revoke sessions, 3 a password change revoked the earlier sessions), the outcome (`OutcomeId`: 1 revoked, 2 no session was presented and only the cookies were cleared, 3 the revocation failed), the remote and local address, the user agent, when the session that was cut had started, and for an administrator revocation who cut it. Callers with no session are recorded at most once a minute per address, and every text column is length bounded and stripped of control characters, so a hostile `User-Agent` is stored as inert data. The Logout Audit Trail is a permanent record, like the Login Audit Trail.

### Password Transport Hardening

Two independent, combinable options control how a password is protected in transit from browser to server, on top of the connection already being HTTPS.

Wire password hashing (WirePasswordHash, a flag per user set when a password is administered or reset) has the browser compute SHA-256 (password + username) in JavaScript and send only that hash, rather than the plaintext password, as the credential. Argon2 hashing of the credential still happens server-side as normal, which is to say that this is an additional privacy layer in front of it, not a replacement. Because the flag is per user and defaults to unset, existing users continue sending their plaintext password (over HTTPS, still Argon2-hashed server-side) until an administrator resets their password with the flag enabled. Because the server only ever receives a hash once this is on, password strength rules can only be enforced in the browser at that point, not re-checked server-side. It follows that for such a user the New password step of the sign-in page presents the rules under the heading Your new password needs, marking each as met or not met as the password is typed, and declines to submit a password that does not meet every one:

* At least 12 characters, and no more than 128 characters.
* One uppercase letter, one lowercase letter, one number and one special character.
* No character repeated more than twice in a row.
* Not starting with a common pattern such as 123456.

RSA asymmetric password transport (PasswordAsymmetricEncryption, default False) additionally has the browser encrypt the password with an RSA public key before sending it, decrypted server-side with the matching private key (PasswordAsymmetricEncryptionPublicKey and PasswordAsymmetricEncryptionPrivateKey). Both key Environment Variables default to unset, and Jube refuses to start if PasswordAsymmetricEncryption is True while either is missing, so generating and setting a fresh keypair for each environment before turning this on is enforced, not just recommended. See [Environment Variables](../EnvironmentVariables/index.html) for the specific variable names.

{% comment %}TODO(verify): the Vue sign-in page (Jube.App/Vue/src/auth/authentication.ts) sends the password either in the clear or as the wire hash, and has no step to encrypt it with PasswordAsymmetricEncryptionPublicKey, whereas the server decrypts with the private key when PasswordAsymmetricEncryption is True. Confirm on a running system whether signing in through the user interface works with PasswordAsymmetricEncryption set to True, and correct the paragraph above or the sign-in page accordingly.{% endcomment %}

Where wire hashing and asymmetric encryption are both of interest, RSA transport plus WirePasswordHash left disabled is the recommended combination, as it keeps server-side strength validation working (since the server still receives the real password, just encrypted in transit) while still avoiding plaintext on the wire.

A user changes their own password, where Username and Password is the scheme in use, via `POST /api/Authentication/ChangePassword`, and is obliged to do so at the New password step of the sign-in page where the password has expired or was issued as temporary. The user interface has no page of its own for the changing of a password by a user who is already signed in.


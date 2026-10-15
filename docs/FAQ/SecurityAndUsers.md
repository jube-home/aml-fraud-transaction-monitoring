---
layout: default
title: Security and Users
nav_order: 8
parent: FAQ
---

# Security and Users

## Authentication

**Q: How does authentication work?**

A: Three schemes: username and password, Negotiate (Kerberos), and OAuth / OpenID Connect (authorization code with
PKCE). They are mutually exclusive — OAuth and Negotiate cannot both be on, and password login is disabled when either
is. Jube always issues its own JWT and session cookie, even after OAuth sign-in, and the OAuth user must match an
existing active user record; accounts are never auto-created. Offloading authentication to an identity provider via
OAuth is the strongly encouraged production posture, with MFA handled at the provider. Settings:
`OAuthAuthentication`, `OAuthAuthority`, `OAuthClientId`, `OAuthClientSecret`; set `UseForwardedHeaders` behind proxies.
See [Authentication](../Concepts/Authentication/index.html).

**Q: How do API keys work?**

A: Generated per user, shown once, and stored only as a hash with an HMAC checksum. The `X-API-KEY` header takes
precedence over the JWT and the cookie. Keys belonging to disabled or deleted users (or users whose role is deleted)
are rejected; revocations propagate immediately over Redis, while a user deactivation takes effect within about a minute
as the key list reloads. A request authenticated by API key never mints a browser session cookie.

**Q: Is MFA supported?**

A: A pluggable MFA (RSA-style token) exists with a mock endpoint for development, but offloading to an identity provider
via OAuth/SSO is the recommended approach.

## Secrets and cryptography

**Q: How are passwords and secrets protected?**

A: Passwords are stored with Argon2 keyed by a secret. API keys and JWTs use HMAC-SHA256. Default keys must be changed
before production, and in production secrets are supplied as Docker secrets via the `[@Name@]` tokenised environment
variables. Per user, `WirePasswordHash` makes the browser send SHA-256 (password + username) instead of the plaintext
password, and the server hashes that again — with the lookup behaving identically for unknown usernames so it cannot be
used to enumerate accounts. The password can additionally be encrypted in the browser with RSA-OAEP
(`PasswordAsymmetricEncryption`).

**Q: Is there field-level encryption?**

A: Yes. AES string encryption is available in inline scripts, inline functions and Request XPath via
`ElementSymmetricEncryptionKey` (`ENCRYPTION_KEY` in the secrets init). It is intended for PII.

## Hardening

**Q: What security headers and protections are in place?**

A: A seeded response-header table (X-Frame-Options, Referrer-Policy, Permissions-Policy, a CSP, long HSTS and others),
secure cookies (`SecureHttpCookie`), per-IP login rate limiting, optional lockout auto-expiry
(`EnablePasswordLockoutReset`), a global HTTP rate limit (excluding Invoke) with an optional Redis backplane, parser
depth guards, and a [Web Application Firewall](../Concepts/WebApplicationFirewall/index.html) layer that blocks
SQL-injection-shaped input with 403.

**Q: How do roles and permissions work?**

A: Users belong to roles. Permissions grant page-level access, and role assignments on entities (case workflows, models)
isolate data. Role changes take effect without a resynchronisation.

**Q: How was SQL injection addressed?**

A: Almost all data access goes through LINQ to DB with bind variables. The dynamic SQL paths (visualisation, the AI
assistant) use bind variables, a read-only connection and a SELECT-only parser, and the switch that could disable that
parser was removed. The WAF blocks injection-shaped input before it reaches a handler.

**Q: How is the platform security-tested?**

A: An in-house automated penetration-testing suite — a dedicated xUnit category of roughly 6,000 cases run against a
real Kestrel server and PostgreSQL — follows the OWASP API Security Top 10 (2023) and ASVS, with a generic every-route
layer plus targeted suites for authorisation (BOLA), mass assignment and dynamic SQL. It runs in CI on every pull
request, on pushes to master and weekly, with a findings policy, and is complemented by a scheduled OWASP ZAP scan run
against a production-shaped stack (with `EnableMockEndpoints` set to False, as it should be in any production
deployment). See [Software Validations](../Concepts/SoftwareValidations/index.html).

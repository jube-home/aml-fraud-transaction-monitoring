---
layout: default
title: CLI Password Reset
nav_order: 2
parent: CLI
grand_parent: Concepts
---

# CLI Password Reset

In the event of total password lock and unavailability of the application for all administrative users, the password can be reset via the CLI:

```shell
.Jube.CLI -cs "<Insert Database Connection String>" -urpr <Insert Password Salt> <Insert User Name> <Insert New Password>
```

The parameters for the function -urpr, which are passed in the following order, are:

| Parameter | Description                                 | Example Value             |
|-----------|---------------------------------------------|---------------------------|
| Salt      | The PasswordHashingKey Environment Variable | ExtraSuperSecretRandomKey |
| User Name | The user name to be reset.                  | Administrator             |
| Password  | The new password to set for the user name.  | StrongPassword            |

As in the following example (noting that the -cs function requires the database connection string as its parameter, which has been wrapped in double quotations given the necessary presence of the space character in the string):

```shell
.Jube.CLI -cs "Host=127.0.0.1;Database=test;Username=postgres;Password=secret;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;" -urpr ExtraSuperSecretRandomKey Administrator StrongPassword
```

Each of the three parameters, and the connection string before them, is required, and where one is absent the CLI writes a message naming it and makes no change.

The new password is passed in the clear, as shown above, regardless of the wire hashing scheme of the target user, as the CLI handles this transparently. If the user has `WirePasswordHash` enabled (see [Password Transport Hardening](../../Authentication/index.html#password-transport-hardening)), the tool applies the same SHA-256 with username pre-hash used by the browser, before Argon2 hashing the result for storage, such that the record written stays consistent with however the user would otherwise have set their own password. It follows that the password need not be hashed beforehand in either case.

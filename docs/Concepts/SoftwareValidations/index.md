---
layout: default
title: Validation Patterns
nav_order: 10
parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

## Native .NET MVC Pipeline

Jube is substantially a .NET ASP.NET application and uses the native MVC pipeline. It follows that certain injection
risks are natively handled, and XSS risks are substantially reduced. Any malicious code is escaped far upstream of the
software architectures.

For example:

![TryingToInject](TryingToInject.png)

![EscapedProperlyNoAlert](EscapedProperlyNoAlert.png)

Unless developer mode is enabled explicitly, all errors rendered from the ASP.NET pipeline will suppress the actual
error, and only general errors are communicated (Div by Zero thrown internally for the purpose of example):

![ExceptionBeingThrown](ExceptionBeingThrown.png)

![ProductionModeNoException](ProductionModeNoException.png)

In developer mode, which is set via an environment variable, the picture is different:

![InDeveloperModeException](InDeveloperModeException.png)

## Authenticate Attribute Decoration

The .NET authentication pipeline is implemented, which takes care of identity. Every endpoint and page requires
authentication, with the exception of a short, named list of anonymous routes: the login and logout endpoints and pages
(`/api/Authentication/ByUserNamePassword`, `WirePasswordHash` and `Logout`, `/Account/Login` and `/Account/Logout`), the
readiness probe (`/api/Ready`), the error and landing pages, and the mock endpoints used for demonstration. A test fails
the build if any other route can be reached anonymously. For example:

![AuthorizeAttribute](AuthorizeAttribute.png)

When authenticated, attempts to access this page without authentication will be redirected to login:

![RedirectedToLogin](RedirectedToLogin.png)

In the case of an API recall without authentication, this will be served a 401:

![APIWithoutAuthorize](APIWithoutAuthorize.png)

## Data Transformation Object (DTO)

A DTO is a model (class) that exists for the sole purpose of serialising and deserialising data, to and from, a user's
request. For example, suppose the UI or independent service invokes an API endpoint with a JSON payload — the JSON
payload must deserialise to the contract specified in the DTO. Taking the DTO `AuthenticationRequestDto`:

![ADtoClass](ADtoClass.png)

This DTO forms the contract for validation in the endpoint:

![MVCHandlingOfDto](MVCHandlingOfDto.png)

It follows that the first stage of validation is that the JSON passed to the endpoint serialises as per contract. In the
event that it does not serialise, the endpoint will be passed null. Assuming a vague attempt to serialise to the DTO,
the next step in each endpoint is validation via strongly typed validators. Fluent Validation is used as the very first
step of controller logic:

![CallingFluentValidator](CallingFluentValidator.png)

The validation will apply a series of validation rules which are more akin to rules:

![FluentValidatorRules](FluentValidatorRules.png)

Assuming validation, there is other practical validation that takes place in the mapping of the DTO to the corresponding
strongly typed data objects, which is handled directly as logic in the controller, or via AutoMapper where the data
layer is being invoked.

It is worth special mention that .NET is a strongly typed programming language and there is no dynamic invocation of
code.

# Dynamic SQL Validation

The vast majority of the database interactions take place via repository patterns, where each repository performs
database interactions via strongly typed C# LINQ2DB, an Object Relation Mapper (ORM) which is transposed to SQL without
intervention from the developer. It follows that parameterisation is almost universally assured and there is no means to
inject SQL where ORMs are used.

![LINQ](LINQ.png)

There are certain cases where the repository pattern is not appropriate, and in such cases LINQ remains the first choice
to constitute a query, albeit with a query pattern:

![BigLINQ](BigLINQ.png)

In certain cases, and it is minimal, amounting to around ten database interactions across the whole platform, there is
rendered and truly dynamic SQL constructed. In such cases parameterisation is fully implemented, and is something that
our own software scanners monitor for (in the form of direct user input mapping to SQL inputs):

![DynamicSQL](DynamicSQL.png)

In the case of all dynamically generated SQL, they are executed only via read replicas and database service accounts
that only have access to read. Yet notwithstanding, there exists a further level of validation for all dynamically
generated SQL, in the form of the Assert Select Only Parser, which uses the Postgres SQL libraries to perform a parse of
the SQL and allow only on it being of the form `SELECT`:

![ScreenshotOfCallToParser](ScreenshotOfCallToParser.png)

![TheSQLSoftParserCode](TheSQLSoftParserCode.png)

In the case of malicious SQL, the query will simply fail and log out the exception.

The parser walks the whole syntax tree rather than matching text, so comments, letter case, whitespace, unicode and
dollar quoting cannot disguise a statement. Exactly one plain `SELECT` is accepted; anything that writes (including data
modifying `WITH` clauses and `SELECT INTO`), locks rows (`FOR UPDATE`), stacks a second statement or changes session
state is refused, as are functions that sleep, change settings or sequences, read the server file system, open
connections or return the text of another query (`pg_sleep`, `set_config`, `nextval`, `lo_import`, `pg_read_file`,
`dblink`, `query_to_xml` and the like), and the system catalogues. SQL a user authors (Visualisation Datasources) is
additionally refused access to the identity and credential tables (`UserRegistry`, `TenantRegistry` and their siblings).
Execution then runs inside a read only transaction with a statement timeout (30 seconds) and a ceiling of 100,000 rows,
so that the database itself refuses a write and a runaway query is cancelled even if the parser were to miss something.
An absent `ParserAssertSelectOnly` setting fails closed: the gate stays on.

The Assert Select Only Parser itself can be bypassed via the `ParserAssertSelectOnly` Environment Variable, which exists
purely to make certain test scenarios easier to construct where standing up a full Postgres-backed integration test is
impractical. **This must be set to True for every production workload** - see
[Environment Variables](../EnvironmentVariables/index.html) - since setting it to False removes this validation layer
from every dynamic SQL execution path in the platform, not just the one being tested.

There exists one administrator-only page which allows for the embedding of SQL:

![DangerZone](DangerZone.png)

Noting the Assert Select Only Parser.

# Validation of Dynamic .NET via Rule Token Parser

The rules engine operates by the compilation of .NET code dynamically, on model synchronisation. It stands to reason
that this would be a significant attack vector. The validation is more comprehensively explained in the Rule Parser
section of
the [Rule Compilation Algorithm](/aml-fraud-transaction-monitoring/Configuration/Models/RuleCompilationAlgorithm/), but
it suffices to say that .NET code is filtered on a token basis for allowed tokens only.

In addition to the token allow-list, the parser rejects rule text that nests expressions beyond a fixed depth
(`MaximumNestingDepth`, 64) before the text is ever handed to the compiler. Pathologically nested input — long runs of
the unary `Not` or `-` operators, or deeply nested parentheses and function calls — can otherwise exhaust the compiler's
stack, and a stack overflow in .NET cannot be caught and terminates the whole process. A legitimate rule nests only a
handful of levels deep, so the limit is invisible in normal authoring; a rule that exceeds it is refused in the same way
as one carrying a restricted token, in well under a millisecond, before any compilation is attempted.

This guard, and the hostile-rule corpus it sits alongside (process execution, reflection, file and environment access,
and known VB escape tricks, each checked under a dozen obfuscation spellings — case, whitespace, comments, line
endings, byte-order mark, homoglyphs, zero-width characters), are proved directly against the parser by 1,649 unit
tests that need no database or HTTP host and run in about twenty seconds, in addition to the same corpus's coverage
through the live HTTP endpoint. See [Automated Penetration Testing](#automated-penetration-testing) below.

# Web Application Firewall

The layers above validate the *shape* of a request (its contract, its identity, its permissions) and the *content* of
the two places where user text becomes executable — dynamic SQL and the rule language. The Web Application Firewall adds
a general, content-level layer in front of the application that inspects the values carried by any request against a set
of signatures, records every match, and — for signatures configured to do so — refuses the request. It is aimed at the
OWASP Top 10 content classes (SQL injection, cross-site scripting, path traversal, template injection, XML external
entity, OS command injection, null-byte) rather than network-level signatures.

It is defence-in-depth and audit, not a replacement for the structural defences described above: parameterised queries,
the Assert Select Only parser, the rule token parser, output encoding, authentication and RBAC remain the primary
controls. The WAF is a second net, and a record of what was thrown at the application.

## Disabled by default

The WAF is controlled by the `WafEnabled` Environment Variable, which **defaults to False**. Because a dropping
signature rejects a matching request with `403 Forbidden`, and because a fraud or anti-money-laundering payload can
legitimately contain text that resembles an attack — a transaction narrative, or a name such as `Jean-Pierre O'Brien`,
containing an apostrophe, angle brackets or SQL-looking words — it is intended to be enabled deliberately, once its
signatures have been reviewed and any needed exceptions added. See
[Web Application Firewall](../WebApplicationFirewall/index.html) for the full operator's guide and
[Environment Variables](../EnvironmentVariables/index.html) for the `Waf*` settings.

## What is inspected

A single rule set — read from the database, compiled, and swapped in atomically on a poll — is evaluated against three
request surfaces, each with a scope flag so a signature can be limited to one of them:

- **Path**: the request path.
- **Query**: each query-string value.
- **Body**: each string, number and boolean value of a JSON request body, addressed by a JSON path such as
  `$.customer.name`; object keys are inspected too. A body that is not JSON, is larger than `WafMaxInspectBytes`, or
  belongs to a WebSocket upgrade or streaming request, is not buffered — its path and query are still inspected.

The inspection core is transport-agnostic and is reached from two call sites so that the signatures and the audit
trail are shared: the HTTP middleware for ordinary requests — which also covers the path and query string of the
Server-Sent Events streaming endpoints, since they are reached through the same pipeline — and any future streaming
handler that could inspect events written onto an already-open stream.

## The three tables

- **WafSignature** — the signatures. Each row is a name, an OWASP category, a description, a .NET regular-expression
  pattern, a target scope, a per-row match timeout, a Drop flag and an Active flag. A signature whose Drop flag is set
  refuses a matching request; a signature with Drop cleared is *detect-only* — the match is recorded but the request
  continues. A pattern that fails to compile is logged and skipped, so one bad row cannot stop the WAF loading. A
  starter set covering the OWASP content classes above is seeded by migration.
- **WafException** — the allow-list, and the escape hatch for false positives. Each row is a route regular expression
  (required), an optional field regular expression, and an optional signature reference. An exception suppresses a match
  when its route regex matches the request route, and — where given — its field regex matches the matched field's name
  and its signature reference is the signature that matched. A row with no route regex is skipped, so a blank exception
  can never silently disable the WAF. This is how a specific free-text field on a specific route — a case narrative, a
  customer name — is exempted without weakening the signature everywhere else.
- **WafAttack** — the evidence log. Every match, dropped or detect-only, is queued in memory and bulk-inserted here on a
  short interval. Each row records the transport, route, method, remote IP, user (when known), the matched signature and
  category, the matched field's JSON path, a truncated copy of the matched value, the action taken, a correlation id and
  the instance name. The table carries trigram (`pg_trgm`) and btree indexes on the same basis as the other operational
  log tables, so it can be searched and filtered from the DevOps pages.

## Guarding the guard

A content firewall driven by regular expressions is itself a potential denial-of-service surface: a pattern with
catastrophic backtracking, fed a hostile input, can hang. Every signature and exception is therefore compiled with a
match timeout, capped at `WafMaxRegexTimeoutMilliseconds`, and a match that times out is treated as a non-match rather
than being allowed to hang the request. JSON traversal is likewise bounded in both depth and number of values, and the
request body is only buffered up to `WafMaxInspectBytes`, so the inspection cost of any one request is bounded.

## Rolling it out

Because signatures and exceptions are read from the database on a poll, they can be added, edited or disabled with no
restart. The intended sequence is tune-then-enforce: begin with signatures as detect-only, watch the WafAttack table for
false positives, add WafException rows for the legitimate ones, and only then set the Drop flag where you are confident.

66 tests (unit and database-backed) cover every seeded signature against its matching attack, against a set of
benign look-alike payloads that must **not** match (an ordinary transaction narrative, a name such as
`Jean-Pierre O'Brien`), the exception mechanism, scope, detect-versus-drop behaviour, and a real catastrophic-
backtracking pattern proven not to hang a request.

# Automated Penetration Testing

Everything described on this page is not only designed in but continuously verified, and not lightly: `Jube.Tests`
runs to 19,301 individual test cases at the time of writing, across 465 files, and every one of them is checked before
a change is considered finished. `Unit` (7,813 cases) proves logic in isolation with no database or HTTP; `Service`
(5,292) proves it against the real Postgres database; and `Category=PenTest` (5,972) is the suite this section
describes — real attack traffic against a real host. A dedicated page cataloguing what each individual test proves is
planned; for now, this is the shape of that number.

The `PenTest` suite runs as ordinary xUnit tests against a real Kestrel host bound to an ephemeral local port,
exercised through a plain HTTP client rather than an in-memory test server. What is exercised is therefore exactly
what a real attacker sees on the wire: real routing, real middleware, real serialisation, real status codes, nothing
short-circuited for the sake of the test.

Coverage is structured around the OWASP API Security Top 10 (2023) and the relevant OWASP ASVS/Top 10 web items, in
two layers:

- **A generic layer** discovers every mapped route — Minimal API and MVC controller alike, read directly from
  `EndpointDataSource` — and runs the same checks against every one of them: anonymous access is refused everywhere
  except a short, explicit allow-list (a small number of login, logout, readiness-probe, error and demonstration-mock
  routes), and a test fails if the real anonymous-reachable surface ever drifts from that list; every route is checked
  for verb tampering, oversized and malformed request bodies, content-type confusion, and null-byte or traversal
  characters in route parameters; no error response is ever allowed to leak a stack trace; and Swagger and static file
  serving are checked for what they expose to an unauthenticated caller.
- **Targeted suites per functional area** — organised the same way as the platform itself (Identity, Model, Case,
  Permissions, the dynamic SQL surface, Monitoring, Observability, Visualisations and the query families each have
  their own) — exercise the authorisation matrix for every role, cross-tenant object references using real seeded ids
  from a second tenant (Broken Object Level Authorization), the hostile-rule and dynamic-SQL corpora described above,
  mass assignment of server-controlled fields (`Id`, `TenantRegistryId`, `CreatedUser`, `Deleted`, `Locked`), type
  confusion and deeply nested or malformed JSON, and the specific attack surface of each feature — a case workflow's
  role grants, the SQL gate's obfuscated write statements, and so on.

A genuine finding from this exercise is fixed in the production code, not merely documented, and is then backed by a
permanent regression test at the fastest level available to prove it. The two findings on this page are both recent
examples: the rule-parser nesting-depth guard and the Web Application Firewall each began life as something this
suite's approach surfaced, and each now also carries its own fast test coverage close to the code — 1,649 tests
against the parser directly, needing no database or HTTP host at all, and 66 for the WAF, almost all to the same
standard — precisely so the regression signal does not depend on standing up the full HTTP harness. Where a
capability is instead an intentional design trade-off rather than a defect — for example, that a named
administrator-only page is deliberately able to execute arbitrary read-only SQL, or that a case workflow macro is
deliberately able to make an outbound HTTP call — it is recorded as accepted by design rather than re-raised on every
run.

## Running the suite locally

`Unit` needs nothing beyond `dotnet test` — no database, no HTTP host, no environment variables. `Service` and
`PenTest` both need a real Postgres instance and read its connection string from `JubeTestConnectionString`, an
ordinary Npgsql connection string (`Host=...;Port=...;Database=...;Username=...;Password=...;`) pointed at the same
database docker-compose's `POSTGRES_PASSWORD` creates locally; a handful of tests that exercise Redis-backed behaviour
directly also read `JubeTestRedisConnectionString`, which defaults to `localhost` when unset. Neither variable has
anything to do with a deployed instance's own `ConnectionString` setting — they exist purely so the test host can
stand up its own connection without touching production configuration.

`dotnet test` from a shell picks up whatever is exported in that shell. An IDE's own test runner does not: Rider and
Visual Studio launch the test host with only the environment configured on the run configuration itself, so a shell
`export` made before opening the IDE has no effect on a run started from its Unit Tests window — the usual symptom is
every database-backed test failing at once, rather than the handful a real regression would touch. In Rider this is
set once, for every future run, on the run configuration template — Run → Edit Configurations → Templates → xUnit →
Environment Variables — rather than on each test's own configuration; a run configuration saved before the template
is changed keeps its own copy and needs recreating. Set the variable before the test host process starts, not after —
some fixtures temporarily rewrite `DynamicEnvironment` settings around a single test and restore them afterwards, and
a variable that is not present from the start of the run can be raced by parallel test classes doing the same thing.

Only one process should run tests against a given Postgres instance at a time. `Service` and `PenTest` classes seed
and tear down their own rows around each test, and two concurrent `dotnet test` invocations — a terminal and an IDE,
say — racing the same database produce cascading, spurious failures (typically `Sequence contains no elements` from a
fixture's own setup) in test classes entirely unrelated to whatever is actually being worked on.

Running the whole `Unit`+`Service` set in one invocation is a separate case worth calling out on its own: xUnit
parallelises by test class by default, and a number of `Service` tests each stand up a real engine host with its own
Npgsql connection pool against `JubeTestConnectionString`. `[Collection("Database")]` only serialises the tests
*within* that collection against each other — it does nothing to limit how many other, unrelated test classes are
concurrently opening connections of their own — so a full run can demand more concurrent connections than a small
pool serves. The visible symptom is either an explicit `Npgsql.NpgsqlException: The connection pool has been
exhausted, either raise MaxPoolSize (currently 20) or Timeout (currently 15 seconds)`, or, worse, no exception at
all — an engine-hosting test's `StartAsync` can simply never observe `Ready` and time out after its own deadline,
since the stall happens inside the engine's own connection acquisition, several layers below anything the test itself
can catch and log. Neither is a sign that anything is actually broken; it means the pool is undersized for the full
suite's aggregate concurrency, not for any one test's needs. Raise `Maximum Pool Size` in `JubeTestConnectionString`
for a full local run; a narrower `--filter` against one area under active work does not need it. The CI pipeline's
own connection string uses the same default (`Maximum Pool Size=20`) and runs the full suite unfiltered, so it likely
carries the same exposure and is worth revisiting there too rather than assuming a GitHub Actions runner's lower core
count keeps it under the threshold by luck.

# Generic Validations and Display of Error Messages

The following describes the scenario where there is database interuption which will bring about errors in the Jube
software. Such errors are not displayed directly in the software and are instead bubbled up as a generic message. For
example, a fairly standard CRUD process as follows, where the database has been terminated:

In the case above, the following error is bubbled up to the user interface:

![ErrorInUiForCRUD.png](ErrorInUiForCRUD.png)

Further proof as follows that the exception does not come across the wire in the background either:

Meanwhile the error is available in the logs:

![ErrorInLogs.png](ErrorInLogs.png)

There are certain administrative pages in the application that do bubble up more detailed errors, given that their
purpose is the to created reports on the basis of SQl, it does provide more reliable feedback as to the error.

## Direct Object Reference and Role Based Access (RBAC) Validation

The vertical slices that exist in Jube follow the pattern of Endpoint > Service > Repository > Data Context (Object
Relation Mapper) > Database (the screenshots below show the original Controller form, whose logic now lives in the
service). The create bulk of the system does not make direct SQL calls to the database and instead pushes SQL down via
LINQ. The approach makes for a very strongly typed approach where models are mapped through the layers of the
application whereby there is no direct object reference. In the following case it can be seen that the input from the
user is mapped indirectly to the object required of the Repository layer:

![PassingObjectsAround.png](PassingObjectsAround.png)

Meanwhile, the repository layer maps once more via LINQ to the underlying SQL:

![StronglyTypedDatabaseAccess.png](StronglyTypedDatabaseAccess.png)

In terms of RBAC, much of the horizontal data isolation is achived by passing the users identity as part of
paramaterised query, where the identity is taken from the .NET authentication pipeline only:

![CheckingPermissionsAtController.png](CheckingPermissionsAtController.png)

In addition to validations set out above, every call to an API will first validate RBAC for that functionality, and the
functionality is only adressible in the case a Permission is added:

![ControllerPermissions.png](ControllerPermissions.png)

Or in the case of horizontal data isolation:

![HorizontalPagePemissions.png](HorizontalPagePemissions.png)
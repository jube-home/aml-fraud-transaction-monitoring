---
layout: default
title: Validation Patterns
nav_order: 10
parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Native .NET MVC Pipeline

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

# Authenticate Attribute Decoration

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

# Data Transformation Object (DTO)

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

The Assert Select Only Parser is unconditional: there is no Environment Variable or other switch that bypasses it, so
this validation layer applies to every dynamic SQL execution path in the platform with no opt-out.

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

Every control described above is verified continuously, and the penetration tests in particular run against a real host
rather than a test double. The suite starts Kestrel on an ephemeral local port and drives it with a plain HTTP client,
so routing, middleware, serialisation and status codes are exactly those a caller meets on the wire; nothing is
short-circuited for the convenience of the test.

## Suite composition

`Jube.Tests` holds 7,689 test methods across 485 files, carrying 14,850 written assertions. Because a `[Theory]`
executes once per data row, those methods expand to 19,566 executed cases:

| Category           | Cases | Proves                                                        | Requires             |
|--------------------|-------|---------------------------------------------------------------|----------------------|
| `Unit`             | 8,080 | logic in isolation                                            | nothing              |
| `Service`          | 5,393 | behaviour against the real database, with `Jube.Tests/Volume` | Postgres             |
| `PermissionParity` | 20    | that the permission surface matches its declared shape        | Postgres             |
| `PenTest`          | 6,073 | the controls on this page, under real attack traffic          | Postgres and Kestrel |

The assertion count is the conservative measure, and most so for the penetration tests: a single method drives a whole
payload corpus and checks every response as it returns, so an assertion written once is made thousands of times at run
time. The monitoring sweeps alone issue 25,670 requests, each checked for status, shape, content type, credential
leakage, reflected markup, header injection and timing.

## Coverage

Coverage follows the OWASP API Security Top 10 (2023) and the relevant OWASP ASVS and Top 10 web items, in two layers.

A **generic layer** reads every mapped route from `EndpointDataSource` — Minimal API and MVC controller alike — and
applies the same checks to all of them:

- anonymous access is refused except on a short, explicit allow-list (login, logout, the readiness probe, the error
  pages and the demonstration mocks), and the suite fails if the anonymous-reachable surface ever drifts from that list;
- every route is checked for verb tampering, oversized and malformed request bodies, content-type confusion, and
  null-byte or traversal characters in route parameters;
- no error response may carry a stack trace;
- Swagger and static file serving are checked for what they expose to an unauthenticated caller.

**Targeted suites** follow the platform's own structure — Identity, Model, Case, Permissions, the dynamic SQL surface,
Monitoring, Observability, Visualisations and the query families each have their own — and exercise the authorisation
matrix for every role, cross-tenant object references using real seeded identifiers from a second tenant (Broken Object
Level Authorization), the hostile-rule and dynamic-SQL corpora described above, mass assignment of server-controlled
fields (`Id`, `TenantRegistryId`, `CreatedUser`, `Deleted`, `Locked`), type confusion and malformed or deeply nested
JSON, and the particular attack surface of each feature, such as a case workflow's role grants or the SQL gate's
obfuscated write statements.

## Findings policy

A finding is fixed in production code and then held by a permanent regression test at the fastest level that can prove
it, so that the regression signal does not depend on standing up the HTTP harness. The rule-parser nesting-depth guard
and the Web Application Firewall both arrived this way, and both carry close-to-the-code coverage of their own: 1,649
parser tests that need neither a database nor a host, and 66 for the firewall.

A capability that is an intentional trade-off rather than a defect is recorded as accepted by design and is not
re-raised on each run. The administrator-only page that executes read-only SQL, and the case workflow macro that makes
an outbound HTTP call, are both of this kind.

## Running the suite locally

`Unit` needs nothing beyond `dotnet test`. `Service` and `PenTest` need a real Postgres instance and read its connection
string from `JubeTestConnectionString`, an ordinary Npgsql connection string
(`Host=...;Port=...;Database=...;Username=...;Password=...;`) pointed at the database that docker-compose creates
locally. Tests exercising Redis-backed behaviour directly read `JubeTestRedisConnectionString`, which defaults to
`localhost`. Neither variable has anything to do with a deployed instance's own `ConnectionString`; they exist so that
the test host can open its own connection without touching production configuration.

**Set the variables before the test host process starts.** A shell `export` reaches `dotnet test` in that shell and
nothing else. An IDE runner launches the host with only the environment configured on the run configuration, so in Rider
the variables belong on the template — Run → Edit Configurations → Templates → xUnit → Environment Variables — and a
configuration saved before the template was changed keeps its own copy and needs recreating. The symptom of a missing
variable is every database-backed test failing at once, rather than the handful a real regression would touch. Some
fixtures rewrite `DynamicEnvironment` settings around a single test and restore them afterwards, so a variable that
appears part way through a run can be raced by parallel classes doing the same.

**Run one process against a given database at a time.** `Service` and `PenTest` classes seed and tear down their own
rows around each test, so two concurrent invocations — a terminal and an IDE, say — produce cascading failures in
classes unrelated to the work in hand, typically `Sequence contains no elements` from a fixture's own setup. Two further
mechanics have the same consequence and the same misleading symptom:

- Stopping a run means stopping its `testhost` as well as `dotnet test`. The driver exits first, and the surviving host
  continues to its own teardown, which deletes the seeded tenant underneath whatever starts next.
- Rebuilding the test project while a run is in flight replaces an assembly and the application's static asset manifest
  that the host loaded once at start, and every subsequent host start fails.

**Size the connection pool for the whole run rather than for one test.** xUnit parallelises by class, and several
`Service` tests each stand up an engine host with an Npgsql pool of its own; `[Collection("Database")]` serialises only
the classes within that collection, so aggregate demand is higher than any one test suggests. An undersized pool appears
either as `Npgsql.NpgsqlException: The connection pool has been exhausted` or, less helpfully, as an engine host that
never observes `Ready` and times out, the stall occurring inside the engine's connection acquisition and below anything
the test itself can catch. Neither indicates a defect. `Maximum Pool Size=100` matches the fixture's own default, and
`docker-compose.yml` raises the server's `max_connections` to 300 so that the application's pool, the monitoring
service's and the test host's coexist rather than ration one another. A narrower `--filter` against one area under
active work does not need it.

## The nightly pipeline

The nightly end-to-end workflow runs two slices in parallel, each standing up a private copy of the whole docker-compose
stack, so the wall clock is the slower of the two rather than their sum:

| Slice | Content                                  | Duration on four cores |
|-------|------------------------------------------|------------------------|
| 1     | `Unit`, `Service` and `PermissionParity` | under four minutes     |
| 2     | `PenTest`                                | seven to eight minutes |

Each slice carries its own `timeout-minutes` rather than sharing an estimated ceiling, because a slice that legitimately
wants an hour is not a problem whereas a slice silently truncated at an estimate is. The split exists for isolation: run
unfiltered, the penetration tests' thousands of deliberately concurrent hostile requests compete with the `Service`
tests for the same Postgres instance and the same pool, and starve them of connections, failing classes that have
nothing to do with the change under test.

Two slices suffice. A battery's wall clock is governed by its individually slow tests rather than by its breadth, so
sharding the penetration tests would multiply the nightly's docker and dotnet builds without shortening it. Sweep
parallelism derives from `Environment.ProcessorCount`, so a sweep widens on a workstation and stays modest on a shared
runner; the deliberate burst tests keep fixed concurrency, because there the load shape is the subject of the test.

## Timing assertions

### No performance assertions

These suites assert no throughput or latency figure. What was once a `Load` category is now `Jube.Tests/Volume` under
`Category=Service`, and those tests assert function *across* volume: that reprocessing reads every archived document
exactly once, that every page but the last is full, that a backtest counts millions of generated rows exactly, and that
memory stays bounded because the work streams rather than materialises. Each needs more rows than fit in a single page
to mean anything, and none is a statement about speed.

A throughput floor measured on whatever hardware the gate lands on cannot distinguish a regression from a busy
neighbour. A floor of 2,000 records a second previously failed a reprocessing test that recorded 741 on four cores,
while the same run's own stage breakdown showed sanctions screening consuming 34.5 ms of each of 4,914 re-invocations:
the floor was measuring sanctions latency, not reprocessing. Performance testing is worth doing, but it belongs in its
own suite on hardware of known capacity, where a number can be compared with the same number from yesterday.

Volumes default to 100,000 rows, chosen as the smallest number that still crosses the thresholds where behaviour changes
— many pages rather than one, a keyset cursor that has to advance, and enough samples for heap behaviour to mean
something. `JubeBacktestVolumeRows`, `JubeBacktestVolumeDatabaseRows` and `JubeReprocessingVolumeRows` raise them for a
deeper run.

### Confirming a timing candidate

Time-blind injection can only be detected by measuring elapsed time, so the penetration tests do carry wall-clock
bounds. A breach of one is a *candidate*, never a finding in itself. Every bound in the battery is evaluated by
`PenTestRun`, which confirms a candidate before reporting it:

1. **Control.** `GET /api/Ready` is issued at once. The route is anonymous and touches neither the database nor the
   cache, so its latency measures nothing but the host's ability to answer. If the control cannot itself return inside
   the same bound, the runner was saturated and the candidate is dropped.
2. **Replay.** Where the request under suspicion is idempotent — `GET`, `HEAD` or `OPTIONS`, logout excluded —
   `PenTestClient` carries a replay delegate on the response, and the candidate must breach a second time before it is
   reported. A one-off scheduling spike therefore cannot produce a finding, while a planted `pg_sleep` or a genuine
   algorithmic blow-up reproduces on demand.
3. **Mutations rest on the control alone**, because re-sending a `POST` would create a second row.

A reported violation carries all three measurements — the original time, the replay, and what the control managed at
that moment — so a reader can see which conclusion the evidence supports. Each run's summary states how many candidates
were dropped and for which of the two reasons, so suppression is visible rather than silent.

Confirmation is also what allows a bound to be *tight*. The monitoring parameter sweep compares a payload's latency
against the area's own baseline plus six seconds, comfortably under the eight-second `pg_sleep` its corpus plants, where
a wide multiplier would be capable of stepping over a genuine finding.

A bound is only meaningful where the request's own cost is bounded, so a sweep whose subject is parameter handling sends
`take=5`. The observability sort, search and identifier-filter sweeps assert that an unrecognised sort field falls back
to ordering by identifier and that a hostile value is never concatenated into a statement, neither of which needs the
hundred thousand row default; response size and clamping are proved where they belong, in the paging test that
deliberately asks for a hundred thousand rows, the widest date range and a burst.

### Differentials inside the process

Three guards measure work in the process rather than over the wire, and each compares against a control measured beside
it: hostile redaction input against inert text of the same length; the refusal of an over-cap statement against the cost
of validating a statement that really is parsed, and the per-character cost of a 65 kB statement against an 8,000-value
one; and a datasource parameter's default value against the fastest of the same set of attack values. A saturated host
inflates both sides of such a comparison equally, where it would carry a single absolute measurement past a fixed
bound.

### Response budget

`PenTestClient.ResponseBudget` is 32 MB, and the client reads one byte beyond it so that a response exceeding the budget
is detected rather than silently truncated. A response over budget is reported by the size rules — `SIZE`, `WIDE-SIZE`,
`RESPONSE-SIZE`, and the login and logout equivalents. Row-count and clamping assertions are not evaluated against a
body the client could not read to the end, since such a body cannot be parsed and would otherwise be reported as an
unclamped row count.

### Recognising a tenant's own data

A cross-tenant assertion has to decide whether a response carries the other tenant's data, and the exhaustive query
battery tells its two tenants apart by a number — tenant A's rows score 7.31 and tenant B's 2.64 — because most of those
endpoints return nothing but numbers. Looking for that number as text in the response body is not the same question.
`GET /api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/291` returned 186 bytes of tenant A's own row and was
reported as leaking tenant B's data, because the row's `createdDate` ended `:12.6423456`, and `2.64` sits inside it. The
collision needs only a seconds value whose last digit is the marker's first and a fraction beginning with the rest, so
it arrives in roughly one dated response in a thousand, which is to say somewhere in most full runs of the battery.

`PenTestMarker.Carries` therefore asks the question structurally. A numeric marker is compared against the response's
JSON numbers, equal to within half of the last digit the marker was printed with, so 7.31 matches a score of
7.3100000000000005 and matches neither a score of 17.31 nor an identifier of 2641. Where a numeric marker could only
appear inside a JSON string, or in a body that is not JSON at all, it counts only where it stands as a number in its own
right — no digit, decimal point or sign immediately before it, and no digit or decimal point immediately after — which
is enough to catch `score 2.64 of tenant B` and not enough to catch a timestamp's fractional seconds. A marker that is
not a number is still matched as text, which is what the markers elsewhere in the battery are: `ZzTest`-prefixed names,
`QxAJson`, a GUID.

## Rules for writing tests in these suites

- A test must not provoke `ResilientNpgsqlConnection`'s retry budget unless the retry is itself the subject. Ten
  attempts with exponential backoff capped at thirty seconds cost `2+4+8+16+30*6`, which is two hundred and ten seconds
  every time. Where a test needs a failed connection for some other reason, choose a state the retry classification
  excludes, such as `3D000` from a database that does not exist, rather than a refused socket.
- `25006` is deliberately retryable, because a read-only transaction error after a failover means the connection has
  landed on a replica and should be recycled. A test asserting that the read-only guard survives a reconnect therefore
  issues its write probe on the underlying connection; the classification itself is covered in
  `PostgresErrorClassificationTests`, where it costs nothing.
- FluentAssertions' `BeEquivalentTo` builds a structural match graph and is quadratic in collection size. Where the
  claim is set equality and uniqueness is already asserted separately, two `Except` assertions state the same thing, run
  in milliseconds, and name the offending items on failure.
- Payload corpora stay exhaustive — every injection seed crossed with every encoding and evasion transform, against
  every parameter of every area. Where a sweep is slow, find what is slow about it rather than sampling the corpus down
  to fit a budget.

`ResilientNpgsqlConnection`'s budget also means that an unreachable or failed-over Postgres costs a caller about three
and a half minutes before the layer gives up. That is reasonable patience for engine work waiting on a leader election,
and long past useful on a synchronous request path. `maxRetries` is already a constructor parameter with nothing
configuring it, so a `DynamicEnvironment` key would expose it in the same shape as `PgPoolClearDebounceMilliseconds`.

# Generic Validations and Display of Error Messages

A database interruption brings about errors in the Jube software. Such errors are never displayed directly and are
instead bubbled up as a generic message. A standard CRUD operation attempted while the database is terminated shows the
user interface the following:

![ErrorInUiForCRUD.png](ErrorInUiForCRUD.png)

The exception does not cross the wire in the background either. The detail is available only in the logs:

![ErrorInLogs.png](ErrorInLogs.png)

Certain administrative pages do bubble up more detailed errors. Their purpose is to author reports on the basis of
SQL, where the text of the database's own error is the feedback the author needs.

# Direct Object Reference and Role Based Access (RBAC) Validation

The vertical slices that exist in Jube follow the pattern of Endpoint > Service > Repository > Data Context (Object
Relation Mapper) > Database (the screenshots below show the original Controller form, whose logic now lives in the
service). The great bulk of the system makes no direct SQL call to the database and instead pushes SQL down via LINQ.
The approach is strongly typed throughout: models are mapped through the layers of the application, so there is no
direct object reference. In the following case the input from the user is mapped indirectly to the object the
Repository layer requires:

![PassingObjectsAround.png](PassingObjectsAround.png)

Meanwhile, the repository layer maps once more via LINQ to the underlying SQL:

![StronglyTypedDatabaseAccess.png](StronglyTypedDatabaseAccess.png)

Much of the horizontal data isolation is achieved by passing the user's identity as part of a parameterised query,
where that identity is taken from the .NET authentication pipeline and from nowhere else:

![CheckingPermissionsAtController.png](CheckingPermissionsAtController.png)

In addition to the validations set out above, every call to an API first validates RBAC for that functionality, which
is addressable only where a Permission has been added:

![ControllerPermissions.png](ControllerPermissions.png)

Or in the case of horizontal data isolation:

![HorizontalPagePemissions.png](HorizontalPagePemissions.png)
---
layout: default
title: Validation Patterns
nav_order: 10
parent: Concepts
---

# Validation Patterns

Jube validates a request in layers, which is to say that a request must pass through the native ASP.NET pipeline, authentication, the contract of a Data Transformation Object, role based permissions, strongly typed validators and a strongly typed data layer before anything is written to, or read from, the database. Where text authored by a user becomes executable, as it does for dynamic SQL and for rules, a parser stands in front of execution, and a Web Application Firewall inspects the content of requests in general. This page describes each layer in turn, with the code that implements it, and closes with the automated penetration testing that proves the layers continuously.

## Native ASP.NET Pipeline

Jube is substantially a .NET ASP.NET application and uses the native ASP.NET pipeline: the API is a set of minimal API endpoints, and the user interface is a single page application which is a fascia of that API. It follows that certain injection risks are natively handled, and XSS risks are substantially reduced. Any malicious code is escaped far upstream of the software architectures.

For example, suppose the following is entered as a case note:

```html
<script>alert();</script>
```

The note is stored, and upon being presented in the notes of the case it is written out as the literal text `<script>alert();</script>`, with no alert being raised. The user interface binds every value as text, there being no place in it where a value returned by the server is injected as markup, and a case note in particular is reduced to plain text before it is presented.

Errors are handled in the same spirit. Each endpoint catches any exception raised beneath it, writes the exception to the log and returns only a status code, so the actual error is suppressed and only a general error is communicated. Taking an exception thrown internally for the purpose of example (`System.Exception: Here lies a sinister exception`), the response to the caller is as follows, with nothing in its body:

```http
HTTP/1.1 500 Internal Server Error
Content-Length: 0
```

For an exception that escapes an endpoint altogether, the pipeline's own exception handler takes the same position, unless developer mode is enabled explicitly:

```csharp
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

In developer mode, which is set via the `ASPNETCORE_ENVIRONMENT` environment variable having the value `Development`, the picture is different: the body of the response carries the exception, its message and the full stack trace, followed by the headers of the request. Developer mode is henceforth for a developer's workstation only.

## Authentication at the Endpoint

The .NET authentication pipeline is implemented, which takes care of identity. Every group of endpoints requires authentication, declared where the endpoints are mapped. For example:

```csharp
var group = endpoints.MapGroup(Base)
    .RequireAuthorization()
    .WithTags("EntityAnalysisModelRequestXPath");
```

The exception is a short, named list of anonymous routes, each declared with `AllowAnonymous()`:

| Route                                                             | Reason for being anonymous                                                          |
|-------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| `/api/Authentication/WirePasswordHash`                            | The login page must learn the password hash scheme before authenticating.           |
| `/api/Authentication/Methods`                                     | The login page must learn which single sign-on methods are switched on.             |
| `/api/Authentication/OAuth`                                       | Starts the OpenID Connect challenge for a browser not yet authenticated.            |
| `/api/Authentication/ByUserNamePassword`                          | The password login itself.                                                          |
| `/api/Authentication/TotpEnrolment`                               | First time set up of an authenticator app, refused unless the password is correct.  |
| `/api/Authentication/Logout`                                      | Logout clears the cookies whatever the state of the credential.                     |
| `/api/Ready`                                                      | The readiness probe for orchestration.                                              |
| The mock endpoints (`/api/MockHttpAdaptation`, `/api/Mfa`)        | Test doubles used for demonstration, carrying no data.                              |

The shell of the user interface is a static page carrying no data, and it is served to any caller, keeping in mind that everything it goes on to show is fetched through the API and so is subject to authentication. A test fails the build if any other route can be reached anonymously.

An attempt to open a page of the user interface without authentication is redirected to login, carrying the address asked for so that it can be returned to upon logging in, taking the form `/login?redirect=%2Fmodels`.

In the case of an API recall without authentication, this will be served a 401:

```http
POST /api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc
Content-Type: application/json
X-API-KEY:
```

```http
HTTP/1.1 401 Unauthorized
Content-Length: 0
```

## Data Transformation Object (DTO)

A DTO is a model (class) that exists for the sole purpose of serialising and deserialising data, to and from, a user's request. For example, suppose the UI or independent service invokes an API endpoint with a JSON payload, then the JSON payload must deserialise to the contract specified in the DTO. Taking the DTO `AuthenticationRequestDto`:

```csharp
[MessagePackObject]
public class AuthenticationRequestDto
{
    [Key(0)]
    public string? UserName { get; set; }
    [Key(1)]
    public string? Password { get; set; }
    [Key(2)]
    public string? NewPassword { get; set; }
    [Key(3)]
    public string? RepeatNewPassword { get; set; }
    [Key(4)]
    public string? RemoteIp { get; set; }
    [Key(5)]
    public string? LocalIp { get; set; }
    [Key(6)]
    public string? UserAgent { get; set; }
    [Key(7)]
    public bool PasswordChangeState { get; set; }
    [Key(8)]
    public string? Mfa { get; set; }
}
```

This DTO forms the contract for validation in the endpoint, being the type of the model the endpoint receives:

```csharp
private static Task<IResult> ByUserNamePasswordAsync(AuthenticationRequestDto model, HttpContext httpContext,
    ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
```

It follows that the first stage of validation is that the payload passed to the endpoint deserialises as per contract. In the event that it does not deserialise it is refused as a bad request, and the service guards against a null model besides. The DTO serves a second purpose in that the forms of the user interface are generated from it, by way of form layout metadata on its properties, such that the form and the contract cannot drift apart.

Assuming a vague attempt to serialise to the DTO, the next step is validation via strongly typed validators. The endpoint itself holds no logic and hands the model to a service, where Fluent Validation is used before any other work is done with the model:

```csharp
var validator = new AuthenticationRequestDtoValidator(dynamicEnvironment, localiser);
var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

if (!results.IsValid)
{
    op.Outcome("invalid");

    return AuthenticationOutcome.ValidationFailed(results);
}
```

The validator, of which there is one for each DTO in `Jube.Validations`, will apply a series of validations which are more akin to rules:

```csharp
RuleFor(p => p.UserName).NotEmpty().WithMessage(Message(localiser,
    AuthenticationResources.UserNameRequired, "'User Name' must not be empty."));
RuleFor(p => p.Password).NotEmpty().When(w => !w.PasswordChangeState).WithMessage(Message(localiser,
    AuthenticationResources.PasswordRequired, "'Password' must not be empty."));

RuleFor(p => p.Mfa).NotEmpty().WithMessage(Message(localiser,
        AuthenticationResources.MfaRequired, "'Mfa' must not be empty."))
    .When(_ => dynamicEnvironment.AppSettings("EnableMultifactorAuthentication")
        .Equals("True", StringComparison.OrdinalIgnoreCase));

RuleFor(p => p.NewPassword)
    .NotEmpty().WithMessage(Message(localiser, AuthenticationResources.NewPasswordRequired,
        "'New Password' must not be empty.")).When(w => w.PasswordChangeState);
```

A validator is not confined to the shape of a value, and may consult the database, as is the case for the validator of a Request XPath, which refuses a name already taken within the Model:

```csharp
.MustAsync(async (dto, name, cancellation) =>
{
    var existing = await repository
        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);

    return existing == null || existing.Id == dto.Id;
})
.WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.NameAlreadyExists])
.WithErrorCode("NameDuplicate");
```

Where validation fails, nothing is written and the endpoint returns 400 with the result of the validation, which names each property at fault alongside a message in the language of the user. The user interface writes each problem beneath the field it relates to, and a summary at the head of the form gives the number of problems to fix (for example 2 problems to fix), each of which can be clicked to move to the field in question, as described in [Child Objects](../../Navigation/ChildObjects/index.html). As the validation takes place in the server, the same rules apply to a request made through the API directly.

Assuming validation, there is other practical validation that takes place in the mapping of the DTO to the corresponding strongly typed data objects, which is handled by a mapper class kept beside the service (for example `EntityAnalysisModelRequestXPathMapper`), or via AutoMapper within the data layer.

It is worth special mention that .NET is a strongly typed programming language and there is no dynamic invocation of code.

## Dynamic SQL Validation

The vast majority of the database interactions take place via repository patterns, where each repository performs database interactions on a single table via strongly typed C# LINQ2DB, an Object Relation Mapper (ORM) which is transposed to SQL without intervention from the developer. It follows that parameterisation is almost universally assured and there is no means to inject SQL where ORMs are used. For example, in the repository for Request XPath:

```csharp
public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> GetByEntityAnalysisModelIdOrderByIdAsync(
    int entityAnalysisModelId, CancellationToken token = default, bool includeDeleted = false)
{
    return await dbContext.EntityAnalysisModelRequestXpath
        .Where(w =>
            (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
            && w.EntityAnalysisModelId == entityAnalysisModelId
            && (includeDeleted || w.Deleted == 0 || w.Deleted == null))
        .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
}
```

There are certain cases where the repository pattern is not appropriate, typically where several tables are joined, and in such cases LINQ remains the first choice to constitute a query, albeit with a query pattern:

```csharp
public class GetEntityAnalysisModelRequestXpathByCaseWorkflowQuery(DbContext dbContext, int tenantRegistryId)
{
    public async Task<IEnumerable<EntityAnalysisModelRequestXpath>> ExecuteAsync(int casesWorkflowId,
        CancellationToken token = default)
    {
        var query =
            from x in dbContext.EntityAnalysisModelRequestXpath
            join m in dbContext.EntityAnalysisModel
                on x.EntityAnalysisModelId equals m.Id
            join c in dbContext.CaseWorkflow on m.Id equals c.EntityAnalysisModelId
            where m.TenantRegistryId == tenantRegistryId
                  && c.Id == casesWorkflowId && (x.Deleted == 0 || x.Deleted == null)
            select x;

        return await query.ToListAsync(token);
    }
}
```

In certain cases, and it is minimal across the whole platform, there is rendered and truly dynamic SQL constructed. In such cases parameterisation is fully implemented, and is something that our own software scanners monitor for (in the form of direct user input mapping to SQL inputs):

```csharp
await using var command = new ResilientNpgsqlCommand(connection, sql);
command.CommandTimeout = limits.StatementTimeoutSeconds + 5;

foreach (var (key, o) in parameters)
{
    command.Parameters.AddWithValue("@" + key, o);
}
```

In the case of all dynamically generated SQL, the connection upon which it is executed is itself set to be read only, whatever the account. Where the `ReportConnectionString` Environment Variable is set, as it should be in production, reporting SQL is executed through it, such that a read replica and a database service account that only has access to read can be given. Yet notwithstanding, there exists a further level of validation for all dynamically generated SQL, in the form of the Assert Select Only Parser, which uses the Postgres SQL libraries to perform a parse of the SQL and allow only on it being of the form `SELECT`. The call to the parser is the first statement of every method that executes dynamic SQL:

```csharp
public async Task<List<IDictionary<string, object>>> ExecuteByNamedParametersAsync(string sql,
    Dictionary<string, object> parameters, CancellationToken token = default)
{
    PostgresSqlValidator.AssertSelectOnly(sql, blockProtectedRelations);

    await using var guard = await BeginGuardAsync(token).ConfigureAwait(false);
```

The parser itself, in `Jube.Data/Reporting/PostgresSqlValidator.cs`, refuses anything other than a single `SELECT` before inspecting the whole of its syntax tree:

```csharp
var result = Parser.Parse(normalizedSql);

if (result.Error is not null)
{
    throw new InvalidOperationException(
        $"SQL parse error: {result.Error.Message}");
}

var parsed = result.Value;

if (parsed.Stmts.Count == 0)
{
    throw new InvalidOperationException("No SQL statement provided.");
}

if (parsed.Stmts.Count != 1)
{
    throw new InvalidOperationException("Only a single SELECT statement is permitted.");
}

var rawStmt = parsed.Stmts[0];

if (rawStmt.Stmt?.SelectStmt == null)
{
    throw new InvalidOperationException(
        "Only SELECT statements are permitted. Blocked statement detected in query.");
}

AssertTreeIsReadOnly(rawStmt.Stmt.SelectStmt, blockProtectedRelations, allowCatalogueRelations);
```

In the case of malicious SQL, the query will simply fail and log out the exception.

The parser walks the whole syntax tree rather than matching text, so comments, letter case, whitespace, unicode and dollar quoting cannot disguise a statement. Exactly one plain `SELECT` is accepted; anything that writes (including data modifying `WITH` clauses and `SELECT INTO`), locks rows (`FOR UPDATE`), stacks a second statement or changes session state is refused, as are functions that sleep, change settings or sequences, read the server file system, open connections or return the text of another query (`pg_sleep`, `set_config`, `nextval`, `lo_import`, `pg_read_file`, `dblink`, `query_to_xml` and the like), and the system catalogues. SQL a user authors (Visualisation Datasources) is additionally refused access to the identity and credential tables (`UserRegistry`, `TenantRegistry` and their siblings). Execution then runs inside a read only transaction with a statement timeout (30 seconds) and a ceiling of 100,000 rows, so that the database itself refuses a write and a runaway query is cancelled even if the parser were to miss something:

```csharp
$"BEGIN READ ONLY; SET LOCAL statement_timeout = {limits.StatementTimeoutSeconds * 1000}; SET LOCAL lock_timeout = 5000"
```

The Assert Select Only Parser is unconditional: there is no Environment Variable or other switch that bypasses it, so this validation layer applies to every dynamic SQL execution path in the platform with no opt-out.

There exists one page, reserved for administrators by way of permission 33, Read Write Visualisation Datasource, which allows for the embedding of SQL. It is reached through the menu at Administration, then Visualisation setup, where the SQL command of a Visualisation Datasource is authored. For example, the SQL command of a datasource taking two parameters:

```sql
select
"ActivationRuleName",
"Frequency",
"PercentageContribution"
from "ExampleCaseVolumeEntry"
where "Frequency" > @Frequency_Greater_Than
and "PercentageContribution" > @Percentage_Contribution_Greater_Than
order by "Frequency" desc
```

Noting the Assert Select Only Parser, and that the parameters are passed as parameters and never concatenated into the statement.

## Validation of Dynamic .NET via Rule Token Parser

The rules engine operates by the compilation of .NET code dynamically, on model synchronisation. It stands to reason that this would be a significant attack vector. The validation is more comprehensively explained in the Rule Parser section of the [Rule Compilation Algorithm](../../Configuration/Models/RuleCompilationAlgorithm/index.html), but it suffices to say that .NET code is filtered on a token basis for allowed tokens only.

In addition to the token allow-list, the parser rejects rule text that nests expressions beyond a fixed depth (`MaximumNestingDepth`, 64) before the text is ever handed to the compiler. Pathologically nested input, being long runs of the unary `Not` or `-` operators, or deeply nested parentheses and function calls, can otherwise exhaust the compiler's stack, and a stack overflow in .NET cannot be caught and terminates the whole process. A legitimate rule nests only a handful of levels deep, so the limit is invisible in normal authoring; a rule that exceeds it is refused in the same way as one carrying a restricted token, in well under a millisecond, before any compilation is attempted.

This guard, and the hostile-rule corpus it sits alongside (process execution, reflection, file and environment access, and known VB escape tricks, each checked under a dozen obfuscation spellings, namely case, whitespace, comments, line endings, byte-order mark, homoglyphs and zero-width characters), are proved directly against the parser by 1,649 unit tests that need no database or HTTP host and run in about twenty seconds, in addition to the same corpus's coverage through the live HTTP endpoint. See [Automated Penetration Testing](#automated-penetration-testing) below.

## Web Application Firewall

The layers above validate the *shape* of a request (its contract, its identity, its permissions) and the *content* of the two places where user text becomes executable, those being dynamic SQL and the rule language. The Web Application Firewall adds a general, content-level layer in front of the application that inspects the values carried by any request against a set of signatures, records every match, and, for signatures configured to do so, refuses the request. It is aimed at the OWASP Top 10 content classes (SQL injection, cross-site scripting, path traversal, template injection, XML external entity, OS command injection, null-byte) rather than network-level signatures.

It is defence-in-depth and audit, not a replacement for the structural defences described above: parameterised queries, the Assert Select Only parser, the rule token parser, output encoding, authentication and RBAC remain the primary controls. The WAF is a second net, and a record of what was thrown at the application.

### Enabled by default

The WAF is controlled by the `WafEnabled` Environment Variable, which defaults to True. Because a dropping signature rejects a matching request with `403 Forbidden`, and because a fraud or anti-money-laundering payload can legitimately contain text that resembles an attack (a transaction narrative, or a name such as `Jean-Pierre O'Brien`, containing an apostrophe, angle brackets or SQL-looking words), it follows that the seeded signatures are to be reviewed, and any needed exceptions added, as part of bringing a deployment into service. See [Web Application Firewall](../WebApplicationFirewall/index.html) for the full operator's guide and [Environment Variables](../EnvironmentVariables/index.html) for the `Waf*` settings.

{% comment %}TODO(verify): this page formerly stated that WafEnabled defaults to False, as docs/Concepts/WebApplicationFirewall/index.md and docs/Concepts/API/WafAttack/index.md still do, whereas Jube.DynamicEnvironment/DynamicEnvironment.cs and the Environment Variables page give True. Owner to confirm that True is the intended default.{% endcomment %}

### What is inspected

A single rule set, read from the database, compiled, and swapped in atomically on a poll, is evaluated against three request surfaces, each with a scope flag so a signature can be limited to one of them:

| Surface | What is inspected                                                                                                                                                                                                                                                                                              |
|---------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Path    | The request path.                                                                                                                                                                                                                                                                                              |
| Query   | Each query-string value.                                                                                                                                                                                                                                                                                       |
| Body    | Each string, number and boolean value of a JSON request body, addressed by a JSON path such as `$.customer.name`; object keys are inspected too. A body that is not JSON, is larger than `WafMaxInspectBytes`, or belongs to a WebSocket upgrade or streaming request, is not buffered, yet its path and query are still inspected. |

The inspection core is transport-agnostic and is reached from two call sites so that the signatures and the audit trail are shared: the HTTP middleware for ordinary requests, which also covers the path and query string of the Server-Sent Events streaming endpoints, since they are reached through the same pipeline, and any future streaming handler that could inspect events written onto an already-open stream.

### The three tables

| Table        | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| WafSignature | The signatures. Each row is a name, an OWASP category, a description, a .NET regular-expression pattern, a target scope, a per-row match timeout, a Drop flag and an Active flag. A signature whose Drop flag is set refuses a matching request; a signature with Drop cleared is *detect-only*, which is to say that the match is recorded but the request continues. A pattern that fails to compile is logged and skipped, so one bad row cannot stop the WAF loading. A starter set covering the OWASP content classes above is seeded by migration.                                       |
| WafException | The allow-list, and the escape hatch for false positives. Each row is a route regular expression (required), an optional field regular expression, and an optional signature reference. An exception suppresses a match when its route regex matches the request route, and, where given, its field regex matches the matched field's name and its signature reference is the signature that matched. A row with no route regex is skipped, so a blank exception can never silently disable the WAF. This is how a specific free-text field on a specific route, such as a case narrative or a customer name, is exempted without weakening the signature everywhere else. |
| WafAttack    | The evidence log. Every match, dropped or detect-only, is queued in memory and bulk-inserted here on a short interval. Each row records the transport, route, method, remote IP, user (when known), the matched signature and category, the matched field's JSON path, a truncated copy of the matched value, the action taken, a correlation id and the instance name. The table carries trigram (`pg_trgm`) and btree indexes on the same basis as the other operational log tables, so it can be searched and filtered in the user interface, where it is presented as WAF attack log.                |

### Guarding the guard

A content firewall driven by regular expressions is itself a potential denial-of-service surface: a pattern with catastrophic backtracking, fed a hostile input, can hang. Every signature and exception is therefore compiled with a match timeout, capped at `WafMaxRegexTimeoutMilliseconds`, and a match that times out is treated as a non-match rather than being allowed to hang the request. JSON traversal is likewise bounded in both depth and number of values, and the request body is only buffered up to `WafMaxInspectBytes`, so the inspection cost of any one request is bounded.

### Rolling it out

Because signatures and exceptions are read from the database on a poll, they can be added, edited or disabled with no restart. The intended sequence is tune-then-enforce: begin with signatures as detect-only, watch the WafAttack table for false positives, add WafException rows for the legitimate ones, and only then set the Drop flag where there is confidence in the signature.

66 tests (unit and database-backed) cover every seeded signature against its matching attack, against a set of benign look-alike payloads that must not match (an ordinary transaction narrative, a name such as `Jean-Pierre O'Brien`), the exception mechanism, scope, detect-versus-drop behaviour, and a real catastrophic-backtracking pattern proven not to hang a request.

## Automated Penetration Testing

Every control described above is verified continuously, and the penetration tests in particular run against a real host rather than a test double. The suite starts Kestrel on an ephemeral local port and drives it with a plain HTTP client, so routing, middleware, serialisation and status codes are exactly those a caller meets on the wire; nothing is short-circuited for the convenience of the test.

### Suite composition

`Jube.Tests` holds 7,689 test methods across 485 files, carrying 14,850 written assertions. Because a `[Theory]` executes once per data row, those methods expand to 19,566 executed cases:

| Category           | Cases | Proves                                                        | Requires             |
|--------------------|-------|---------------------------------------------------------------|----------------------|
| `Unit`             | 8,080 | logic in isolation                                            | nothing              |
| `Service`          | 5,393 | behaviour against the real database, with `Jube.Tests/Volume` | Postgres             |
| `PermissionParity` | 20    | that the permission surface matches its declared shape        | Postgres             |
| `PenTest`          | 6,073 | the controls on this page, under real attack traffic          | Postgres and Kestrel |

{% comment %}TODO(verify): the counts on this page (7,689 methods, 485 files, 14,850 assertions, 19,566 cases, the four category counts, 25,670 sweep requests, 1,649 parser tests, 66 firewall tests) and the slice durations predate this branch and cannot be settled without running the suite. Re-count on vue-vapor and correct.{% endcomment %}

The assertion count is the conservative measure, and most so for the penetration tests: a single method drives a whole payload corpus and checks every response as it returns, so an assertion written once is made thousands of times at run time. The monitoring sweeps alone issue 25,670 requests, each checked for status, shape, content type, credential leakage, reflected markup, header injection and timing.

### Coverage

Coverage follows the OWASP API Security Top 10 (2023) and the relevant OWASP ASVS and Top 10 web items, in two layers.

A generic layer reads every mapped route from `EndpointDataSource` and applies the same checks to all of them:

- anonymous access is refused except on a short, explicit allow-list (login, logout, the readiness probe and the demonstration mocks, as set out in Authentication at the Endpoint above), and the suite fails if the anonymous-reachable surface ever drifts from that list;
- every route is checked for verb tampering, oversized and malformed request bodies, content-type confusion, and null-byte or traversal characters in route parameters;
- no error response may carry a stack trace;
- Swagger and static file serving are checked for what they expose to an unauthenticated caller.

Targeted suites follow the platform's own structure, with Identity, Model, Case, Permissions, the dynamic SQL surface, Monitoring, Observability, Visualisations and the query families each having their own, and exercise the authorisation matrix for every role, cross-tenant object references using real seeded identifiers from a second tenant (Broken Object Level Authorization), the hostile-rule and dynamic-SQL corpora described above, mass assignment of server-controlled fields (`Id`, `TenantRegistryId`, `CreatedUser`, `Deleted`, `Locked`), type confusion and malformed or deeply nested JSON, and the particular attack surface of each feature, such as a case workflow's role grants or the SQL gate's obfuscated write statements.

### Findings policy

A finding is fixed in production code and then held by a permanent regression test at the fastest level that can prove it, so that the regression signal does not depend on standing up the HTTP harness. The rule-parser nesting-depth guard and the Web Application Firewall both arrived this way, and both carry close-to-the-code coverage of their own: 1,649 parser tests that need neither a database nor a host, and 66 for the firewall.

A capability that is an intentional trade-off rather than a defect is recorded as accepted by design and is not re-raised on each run. The administrator-only page that executes read-only SQL, and the case workflow macro that makes an outbound HTTP call, are both of this kind.

### Running the suite locally

`Unit` needs nothing beyond `dotnet test`. `Service` and `PenTest` need a real Postgres instance and read its connection string from `JubeTestConnectionString`, an ordinary Npgsql connection string (`Host=...;Port=...;Database=...;Username=...;Password=...;`) pointed at the database that docker-compose creates locally. Tests exercising behaviour backed by the Resp cache directly read `JubeTestRespConnectionString`, which defaults to `localhost`. Neither variable has anything to do with a deployed instance's own `ConnectionString`; they exist so that the test host can open its own connection without touching production configuration.

The variables are to be set before the test host process starts. A shell `export` reaches `dotnet test` in that shell and nothing else. An IDE runner launches the host with only the environment configured on the run configuration, so in Rider the variables belong on the template (Run, Edit Configurations, Templates, xUnit, Environment Variables), and a configuration saved before the template was changed keeps its own copy and needs recreating. The symptom of a missing variable is every database-backed test failing at once, rather than the handful a real regression would touch. Some fixtures rewrite `DynamicEnvironment` settings around a single test and restore them afterwards, so a variable that appears part way through a run can be raced by parallel classes doing the same.

Only one process is to be run against a given database at a time. `Service` and `PenTest` classes seed and tear down their own rows around each test, so two concurrent invocations, a terminal and an IDE for example, produce cascading failures in classes unrelated to the work in hand, typically `Sequence contains no elements` from a fixture's own setup. Two further mechanics have the same consequence and the same misleading symptom:

- Stopping a run means stopping its `testhost` as well as `dotnet test`. The driver exits first, and the surviving host continues to its own teardown, which deletes the seeded tenant underneath whatever starts next.
- Rebuilding the test project while a run is in flight replaces an assembly and the application's static asset manifest that the host loaded once at start, and every subsequent host start fails.

The connection pool is to be sized for the whole run rather than for one test. xUnit parallelises by class, and several `Service` tests each stand up an engine host with an Npgsql pool of its own; `[Collection("Database")]` serialises only the classes within that collection, so aggregate demand is higher than any one test suggests. An undersized pool appears either as `Npgsql.NpgsqlException: The connection pool has been exhausted` or, less helpfully, as an engine host that never observes `Ready` and times out, the stall occurring inside the engine's connection acquisition and below anything the test itself can catch. Neither indicates a defect. `Maximum Pool Size=100` matches the fixture's own default, and `docker-compose.yml` raises the server's `max_connections` to 300 so that the application's pool, the monitoring service's and the test host's coexist rather than ration one another. A narrower `--filter` against one area under active work does not need it.

### The end-to-end pipeline

The end-to-end workflow runs two slices in parallel, each standing up a private copy of the whole docker-compose stack, so the wall clock is the slower of the two rather than their sum:

| Slice | Content                                  | Duration on four cores |
|-------|------------------------------------------|------------------------|
| 1     | `Unit`, `Service` and `PermissionParity` | under four minutes     |
| 2     | `PenTest`                                | seven to eight minutes |

Each slice carries its own `timeout-minutes` rather than sharing an estimated ceiling, because a slice that legitimately wants an hour is not a problem whereas a slice silently truncated at an estimate is. The split exists for isolation: run unfiltered, the penetration tests' thousands of deliberately concurrent hostile requests compete with the `Service` tests for the same Postgres instance and the same pool, and starve them of connections, failing classes that have nothing to do with the change under test.

Two slices suffice. A battery's wall clock is governed by its individually slow tests rather than by its breadth, so sharding the penetration tests would multiply the workflow's docker and dotnet builds without shortening it. Sweep parallelism derives from `Environment.ProcessorCount`, so a sweep widens on a workstation and stays modest on a shared runner; the deliberate burst tests keep fixed concurrency, because there the load shape is the subject of the test.

It runs on every pull request, on the push to master that each merge produces, and weekly. The three are not redundant. A pull request event builds the merge commit rather than the branch alone, so it proves post-merge state, but only the merge as it stood when the run happened, which is why "Require branches to be up to date before merging" belongs in branch protection: without it two pull requests can each be green against an older master, merge cleanly, and break it between them. The push run proves the commit that actually exists and is what the README's badge reports, so the badge describes master rather than whatever the last schedule happened to catch. The weekly run is the only one of the three that can say anything about code nobody has touched, and that is its whole purpose: the stack is built from docker-compose, so base images, the runner image and the restored package graph all move underneath a repository that is standing still.

A pull request run is cancelled when the branch is pushed again, because a superseded run answers a question nobody is asking any more; a master run is not, because its conclusion is the recorded verdict on that commit and a cancellation says nothing about the code.

The first run of a slice is allowed to fail, and only the test methods it recorded as failed are run again, named from the TRX by class and method rather than by display name so that a `[Theory]`'s data row cannot break the filter expression. A slice is green when the retry is green, which keeps a single non-deterministic failure from blocking a merge while leaving it in the artifacts to be read. A first run that fails *without* recording any failed test is a different animal, such as a crashed test host or a stack that never came up, and fails the slice outright, because there is nothing to retry and nothing has been proven.

### Timing assertions

#### No performance assertions

These suites assert no throughput or latency figure. What was once a `Load` category is now `Jube.Tests/Volume` under `Category=Service`, and those tests assert function *across* volume: that reprocessing reads every archived document exactly once, that every page but the last is full, that a backtest counts millions of generated rows exactly, and that memory stays bounded because the work streams rather than materialises. Each needs more rows than fit in a single page to mean anything, and none is a statement about speed.

A throughput floor measured on whatever hardware the gate lands on cannot distinguish a regression from a busy neighbour. A floor of 2,000 records a second once failed a reprocessing test that recorded 741 on four cores, while the same run's own stage breakdown showed sanctions screening consuming 34.5 ms of each of 4,914 re-invocations: the floor was measuring sanctions latency, not reprocessing. Performance testing is worth doing, but it belongs in its own suite on hardware of known capacity, where a number can be compared with the same number from yesterday.

Volumes default to 100,000 rows, chosen as the smallest number that still crosses the thresholds where behaviour changes, those being many pages rather than one, a keyset cursor that has to advance, and enough samples for heap behaviour to mean something. `JubeBacktestVolumeRows`, `JubeBacktestVolumeDatabaseRows` and `JubeReprocessingVolumeRows` raise them for a deeper run.

#### Confirming a timing candidate

Time-blind injection can only be detected by measuring elapsed time, so the penetration tests do carry wall-clock bounds. A breach of one is a *candidate*, never a finding in itself. Every bound in the battery is evaluated by `PenTestRun`, which confirms a candidate before reporting it:

1. Control. `GET /api/Ready` is issued at once. The route is anonymous and touches neither the database nor the cache, so its latency measures nothing but the host's ability to answer. If the control cannot itself return inside the same bound, the runner was saturated and the candidate is dropped.
2. Replay. Where the request under suspicion is idempotent, being `GET`, `HEAD` or `OPTIONS`, logout excluded, `PenTestClient` carries a replay delegate on the response, and the candidate must breach a second time before it is reported. A one-off scheduling spike therefore cannot produce a finding, while a planted `pg_sleep` or a genuine algorithmic blow-up reproduces on demand.
3. Mutations rest on the control alone, because re-sending a `POST` would create a second row.

A reported violation carries all three measurements, those being the original time, the replay, and what the control managed at that moment, so a reader can see which conclusion the evidence supports. Each run's summary states how many candidates were dropped and for which of the two reasons, so suppression is visible rather than silent.

Confirmation is also what allows a bound to be *tight*. The monitoring parameter sweep compares a payload's latency against the area's own baseline plus six seconds, comfortably under the eight-second `pg_sleep` its corpus plants, where a wide multiplier would be capable of stepping over a genuine finding.

A bound is only meaningful where the request's own cost is bounded, so a sweep whose subject is parameter handling sends `take=5`. The observability sort, search and identifier-filter sweeps assert that an unrecognised sort field falls back to ordering by identifier and that a hostile value is never concatenated into a statement, neither of which needs the hundred thousand row default; response size and clamping are proved where they belong, in the paging test that deliberately asks for a hundred thousand rows, the widest date range and a burst.

#### Differentials inside the process

Three guards measure work in the process rather than over the wire, and each compares against a control measured beside it: hostile redaction input against inert text of the same length; the refusal of an over-cap statement against the cost of validating a statement that really is parsed, and the per-character cost of a 65 kB statement against an 8,000-value one; and a datasource parameter's default value against the fastest of the same set of attack values. A saturated host inflates both sides of such a comparison equally, where it would carry a single absolute measurement past a fixed bound.

#### Response budget

`PenTestClient.ResponseBudget` is 32 MB, and the client reads one byte beyond it so that a response exceeding the budget is detected rather than silently truncated. A response over budget is reported by the size rules, which are `SIZE`, `WIDE-SIZE`, `RESPONSE-SIZE`, and the login and logout equivalents. Row-count and clamping assertions are not evaluated against a body the client could not read to the end, since such a body cannot be parsed and would otherwise be reported as an unclamped row count.

#### Recognising a tenant's own data

A cross-tenant assertion has to decide whether a response carries the other tenant's data, and the exhaustive query battery tells its two tenants apart by a number (tenant A's rows score 7.31 and tenant B's 2.64) because most of those endpoints return nothing but numbers. Looking for that number as text in the response body is not the same question. `GET /api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/291` returned 186 bytes of tenant A's own row and was reported as leaking tenant B's data, because the row's `createdDate` ended `:12.6423456`, and `2.64` sits inside it. The collision needs only a seconds value whose last digit is the marker's first and a fraction beginning with the rest, so it arrives in roughly one dated response in a thousand, which is to say somewhere in most full runs of the battery.

`PenTestMarker.Carries` therefore asks the question structurally. A numeric marker is compared against the response's JSON numbers, equal to within half of the last digit the marker was printed with, so 7.31 matches a score of 7.3100000000000005 and matches neither a score of 17.31 nor an identifier of 2641. Where a numeric marker could only appear inside a JSON string, or in a body that is not JSON at all, it counts only where it stands as a number in its own right, with no digit, decimal point or sign immediately before it, and no digit or decimal point immediately after, which is enough to catch `score 2.64 of tenant B` and not enough to catch a timestamp's fractional seconds. A marker that is not a number is still matched as text, which is what the markers elsewhere in the battery are: `ZzTest`-prefixed names, `QxAJson`, a GUID.

### Rules for writing tests in these suites

- A test must not provoke `ResilientNpgsqlConnection`'s retry budget unless the retry is itself the subject. Ten attempts with exponential backoff capped at thirty seconds cost `2+4+8+16+30*6`, which is two hundred and ten seconds every time. Where a test needs a failed connection for some other reason, choose a state the retry classification excludes, such as `3D000` from a database that does not exist, rather than a refused socket.
- `25006` is deliberately retryable, because a read-only transaction error after a failover means the connection has landed on a replica and should be recycled. A test asserting that the read-only guard survives a reconnect therefore issues its write probe on the underlying connection; the classification itself is covered in `PostgresErrorClassificationTests`, where it costs nothing.
- FluentAssertions' `BeEquivalentTo` builds a structural match graph and is quadratic in collection size. Where the claim is set equality and uniqueness is already asserted separately, two `Except` assertions state the same thing, run in milliseconds, and name the offending items on failure.
- Payload corpora stay exhaustive, which is to say every injection seed crossed with every encoding and evasion transform, against every parameter of every area. Where a sweep is slow, find what is slow about it rather than sampling the corpus down to fit a budget.

`ResilientNpgsqlConnection`'s budget also means that an unreachable or failed-over Postgres costs a caller about three and a half minutes before the layer gives up. That is reasonable patience for engine work waiting on a leader election, and long past useful on a synchronous request path. `maxRetries` is already a constructor parameter with nothing configuring it, so a `DynamicEnvironment` key would expose it in the same shape as `PgPoolClearDebounceMilliseconds`.

## Generic Validations and Display of Error Messages

A database interruption brings about errors in the Jube software. Such errors are never displayed directly and are instead bubbled up as a generic message. A standard CRUD operation attempted while the database is terminated, the saving of a Request XPath for example, is answered with a 500 carrying no body, and the user interface presents the following message, in the language of the user:

```
The server could not complete the request.
```

The other failures of a request are presented in the same generic manner, none of them carrying detail from the server:

| Circumstance                                 | Message                                                |
|----------------------------------------------|--------------------------------------------------------|
| The server cannot be reached at all.         | The server could not be reached. Check your connection. |
| The user lacks the permission.               | You are not permitted to do that.                      |
| The record is not found, or is not visible.  | That record could not be found.                        |
| The record changed since it was opened.      | That record changed. Reload it and try again.          |
| Anything else that is not expected.          | Something unexpected went wrong.                       |

The exception does not cross the wire in the background either. The detail is available only in the logs, where the endpoint writes a line at the level of ERROR naming the request and the user (taking the form `PUT /api/EntityAnalysisModelRequestXPath: 500 user=Administrator`), followed by the exception in full. In the case of a terminated database, the exception is `Npgsql.NpgsqlException: Exception while connecting`, wrapping `System.Net.Sockets.SocketException (111): Connection refused`, with the stack trace passing through `Jube.ResilientNpgsqlConnection` and the repository having made the call.

Pages that author reports on the basis of SQL give the author a generic, localised message and the line and column of the problem, the SQL command of a Visualisation Datasource for example reading Command has errors alongside a position of the form (line 3, column 8). The text of the database's own error, its SQLSTATE code and the statement are never returned; the full exception is written to the log only.

## Direct Object Reference and Role Based Access (RBAC) Validation

The vertical slices that exist in Jube follow the pattern of Endpoint > Service > Repository > Data Context (Object Relation Mapper) > Database. The endpoint holds no logic of its own, such that the permission checks, the validation and the mapping all live in the service. The great bulk of the system makes no direct SQL call to the database and instead pushes SQL down via LINQ. The approach is strongly typed throughout: models are mapped through the layers of the application, so there is no direct object reference. In the following case, taken from the service for Request XPath, the input from the user is validated and then mapped indirectly to the object the Repository layer requires:

```csharp
var saved = await repository
    .InsertIncrementCacheIndexIdAsync(EntityAnalysisModelRequestXPathMapper.ToPoco(model), token)
    .ConfigureAwait(false);
```

Meanwhile, the repository layer maps once more via LINQ to the underlying SQL, as shown in the repository method quoted under Dynamic SQL Validation above.

Much of the horizontal data isolation is achieved by passing the user's identity as part of a parameterised query, where that identity is taken from the .NET authentication pipeline and from nowhere else. The endpoint takes the name of the user from the authenticated request and creates the service with it:

```csharp
var user = httpContext.User.Identity?.Name;
```

```csharp
var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
    stringLocalizerFactory, serviceChangeBus, token);
```

The service resolves the tenant of that user, refusing a user that resolves to no tenant, and every query of the repository is thereafter scoped to that tenant, as in the predicate `w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId` of the repository method quoted above. It follows that an identifier belonging to another tenant behaves as if it did not exist.

In addition to the validations set out above, every call to an API first validates RBAC for that functionality, which is addressable only where a Permission has been added. Each service declares the permissions that each of its operations accepts:

```csharp
private static readonly int[] listPermissions = [7];
private static readonly int[] readPermissions = [7];
private static readonly int[] readByEntityAnalysisModelPermissions = [7, 13];
private static readonly int[] readByDataTypePermissions = [7, 12];
private static readonly int[] writePermissions = [7];
private static readonly int[] overridePermissions = [2];
```

Permission 7 is Read Write Request XPath. The check is the first step of each operation, ahead even of the validation of the DTO:

```csharp
ArgumentNullException.ThrowIfNull(model);
EnsurePermitted(writePermissions, "EntityAnalysisModelRequestXPath.Create");

var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
```

```csharp
private void EnsurePermitted(int[] specs, string op)
{
    if (permissionValidation.Validate(specs))
    {
        return;
    }

    if (log.IsWarnEnabled)
    {
        log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
    }

    throw new ForbiddenException(strings[EntityAnalysisModelRequestXPathResources.PermissionDenied], specs);
}
```

The endpoint answers the refusal with 403. The permissions of a user are themselves read from the database by a parameterised query on the name of the user, and are granted only through an active role, of an active tenant, to an active user whose password is not locked.

Or in the case of horizontal data isolation, the roles assigned to a record are made part of the query itself. A Case Workflow and each Case Workflow Status carry the roles allowed to them, and a case is returned only where the role of the user has been assigned to both, as in this part of the query that fetches a case by its identifier:

```csharp
&& dbContext.CaseWorkflowStatusRole
    .Where(r => r.CaseWorkflowStatusGuid == s.Guid
                && (r.Deleted == 0 || r.Deleted == null))
    .Any(r => dbContext.RoleRegistry
        .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
        .Any(rr => dbContext.UserRegistry
            .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
```

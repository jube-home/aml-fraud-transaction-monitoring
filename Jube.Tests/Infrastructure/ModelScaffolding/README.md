# Model scaffolding

Shared test infrastructure for integration tests that need a **real, synchronised model** in front of the **real
engine**: reconstitute the example model, have the engine synchronise it, invoke it over HTTP, read the answer back, and
remove everything again.

Namespace `Jube.Test.Infrastructure.ModelScaffolding`. It is used by the Invoke tests (`Jube.Tests/Service/Invoke`) and
is tested by `Jube.Tests/Service/ModelScaffolding`.

| Type                   | What it is                                                                              |
|------------------------|-----------------------------------------------------------------------------------------|
| `ModelScaffold`        | The handle: a model copy in a tenant, its callers, the id maps, `DisposeAsync` cleanup. |
| `ModelScaffoldOptions` | Source model, tenant, guid, name prefix, include/exclude tables, flags.                 |
| `ModelEngineHost`      | The real engine, in process. Owns start, ready-wait and teardown.                       |
| `ModelSync`            | `SyncAsync(engine, scaffold, timeout)`: triggers the real synchronisation and waits.    |
| `ModelInvokeHost`      | Kestrel host of the Invoke Minimal API, with a typed `InvokeAsync(scaffold, payload)`.  |
| `Payloads`             | `Payloads.Example()`, the documentation JSON, with `With` / `Without` overrides.        |
| `ModelScaffoldFixture` | xUnit class fixture: ONE engine, ONE synced shared model, isolated copies on demand.    |

## The short version

```csharp
[Collection("Database")]
public sealed class MyTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
{
    [Fact]
    public async Task InvokesTheExampleModel()
    {
        var response = await fixture.Api.InvokeAsync(fixture.Shared,
            Payloads.Example().With("TxnId", "my-txn-1"));

        Assert.Equal(200, response.Status);
        Assert.Equal("my-txn-1", response.String("EntityInstanceEntryId"));
    }

    [Fact]
    public async Task ChangesTheModel()
    {
        // A private, synchronised copy (random guid, its own tenant) for a test that mutates the model.
        await using var model = await fixture.CreateIsolatedAsync();
        // ... change rows for model.ModelId, then ModelSync.SyncAsync(fixture.Engine, model) ...
    }
}
```

`[Collection("Database")]` is required: it serialises the class against every other database test (the pen-test classes
included), which matters because there is one documentation guid, one Redis and one node-status row per host name. The
class fixture is created once per class, so the slow part (starting the engine, about 5 seconds, and the first
synchronisation, a second or two) is paid once per class, not once per test.

## What the copy depends on in the shared database (and how to stop depending on it)

The scaffold copies the example model's rows out of the SHARED development database, so a test that reads what the model
does can silently depend on database state that nobody seeded for it. Two dependencies were found, both when a
preservation import replaced the example model on 2026-09-19:

- **A deleted source.** A preservation import soft deletes the example model (Id 1) and everything under it and creates
  a new model (same guid, new id). When the requested source model is itself deleted, `ModelGraphCopier`
  copies its rows as LIVE rows (`Deleted = 0`, `DeletedDate` and `DeletedUser` null), so the copy is the example
  configuration whatever has been imported since. Nothing else is changed in the copied rows.
- **Global sanction entries.** The documentation payload's `JoinedName` is "Robert Mugabe", and the example model's
  `ThresholdSanctionsDistance` rule fires when the sanction distance is below 1, that is on an exact match. The entries
  the database happened to hold made that rule fire; a sanctions import (or the loader's reconcile) that soft deletes
  them made "Activation" empty, so the test that expects activations and a response elevation failed. Sanction entries
  are global reference data (no tenant), so a scaffolded model cannot carry them.
  `InvokeEndpointTests.InvokeModelFixture` therefore seeds a test prefixed sanction source with the entry
  "Robert Mugabe" in `BeforeEngineStartAsync` (the engine loads the entries when it starts) and removes it in
  `AfterDisposeAsync`. Use the same two hooks on `ModelScaffoldFixture` for any other global data a test needs (a
  sanction entry, a stop token, a global setting): seed it through the fixture, with the test prefix, and clean it up.

- **Engine state that outlives a test.** The shared engine is used by every test in a class, and the example model
  suppresses a rule that has already activated for the same `AccountId` (the documentation payload's is `Test1`), so a
  later invocation with `Test1` shows the activation but no response elevation. A test that asserts on elevation or
  activation must use its own unique `AccountId` (`Payloads.Example().With("AccountId", ...)`), which is what
  `Invoke_ResponseCarriesTheActivationsAndElevationOfTheModelAsync` does.
- **Deleted source rows.** `ModelGraphCopier.ResolveLiveSourceAsync` maps a deleted source model to the live model that
  has the same guid (what a preservation import leaves), so a copy is never made from deleted rows or from junk rows
  that were deleted long before the import.

Rules the example model's other rules depend on, for reference: `ThresholdTtlCounterAll` needs the TTL counter above 5
(it accumulates across invocations of one account, so a test must not rely on it), `AllIPDenyList` needs the payload IP
in the list (the documentation list holds 123.456.789.123, the payload IP is 123.456.789.200, so it does not fire), and
`VolumeThresholdByAccountId` compares the abstraction volume (123.45) with the dictionary value for
`Test1` (1000), so it does not fire either. Only the sanction rule is expected to fire on the documentation payload.

## Scaffolding without an engine

```csharp
await using var scaffold = await ModelScaffold.CreateAsync();                       // documentation guid
await using var second   = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated); // random guid
```

`ModelScaffoldOptions`:

| Option             | Default                                | Meaning                                                                                                                      |
|--------------------|----------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| `SourceModelId`    | `1` (the example model)                | The model to copy.                                                                                                           |
| `TenantRegistryId` | `null`                                 | An existing tenant to put the copy in. `null` creates a fresh test tenant.                                                   |
| `ModelGuid`        | `81abd51c-0013-41c1-a4c7-4a6270eb5aa4` | The guid of the copy. `null` is random, so scaffolds can live side by side.                                                  |
| `NamePrefix`       | `ZzTest`                               | Must start with `DatabaseFixture.Prefix`, so the fixture's own clean-up finds leftovers.                                     |
| `IncludeTables`    | none                                   | Tables the default exclusions skip, copied anyway. An unreachable name is an error.                                          |
| `ExcludeTables`    | none                                   | Tables (and so everything below them) not copied. Exclude wins over include.                                                 |
| `Active`/`Locked`  | `true` / `false`                       | The flags of the copy.                                                                                                       |
| `CreateCallers`    | `true`                                 | A role granted the model with a user holding an API key (`UserName`), and a second user without the grant (`OtherUserName`). |

The handle exposes `ModelId`, `ModelGuid`, `ModelName`, `TenantRegistryId`, `TenantName`, `OwnsTenant`, the caller
names, `IdMap` (per table, source id to copy id) and `CopiedRows`.

### How the copy works

Nothing is listed by hand. Everything below the model is discovered from the database:

1. the foreign keys (`pg_constraint`) that lead, transitively, to `EntityAnalysisModel`;
2. every table with an `EntityAnalysisModelGuid` uuid column (lists, dictionaries and so on refer to the model by guid,
   not by foreign key).

Tables are copied parents first. Every copied row takes a **fresh identity**: an identity value is never inserted and a
sequence is never touched (ids are sequences in the database). Foreign keys are rewritten through the old-to-new id
maps,
`TenantRegistryId` becomes the target tenant, `Guid` columns get fresh guids, and any uuid column that refers to a
copied row's guid (no foreign key) follows the copy. A table added by a later migration is picked up on its own.

It **fails loudly**, naming the table and column, rather than skipping: no identity `Id` column, a composite foreign
key, a non-nullable column pointing at an excluded parent, a row whose parent was not copied, an unreachable
`IncludeTables`
entry, a missing source model. The whole copy is one transaction, and a failed `CreateAsync` removes what it had already
created (tenant, users, roles).

Not copied by default (`ModelScaffold.DefaultExcludedTablePattern`): archive and archive keys, model instances, sample
execution logs, reprocessing instances, `*Version` history tables, `*CounterHistory`, processing counters and queue
balances, and case data (cases, events, files, notes, form entries). The scaffold is **configuration**, not history.

### How cleanup works

`DisposeAsync` (idempotent, also run after a failed `CreateAsync`) deletes **children first**: for each thing it created
it walks the foreign keys downward (`pg_constraint` again), deleting the referencing rows before the row, so runtime
output an invoke wrote (archive rows, entries) goes with it. In order: the schedule rows the scaffold inserted, the
`EntityAnalysisModelRole` link, everything that refers to the model by guid, the model, the callers (user, API key,
tenant membership, roles), and, only when the scaffold created it, the tenant. A supplied tenant is never removed.

If a test process is killed, the leftovers are named with the fixture prefix, and `DatabaseFixture.DisposeAsync` removes
prefixed models and their children on the next run.

## Synchronising

```csharp
var sync = await ModelSync.SyncAsync(engine, scaffold, TimeSpan.FromSeconds(30));
```

The engine holds a model only after its model synchronisation. `SyncAsync` does what the Synchronisation page's button
does: it inserts a schedule row for the tenant (dated after the node's last synchronisation of it and before now, which
is what the engine treats as pending), then polls (100 ms, bounded) until the node status entry of the tenant on this
host has moved past its earlier value, the engine holds the model and, when the scaffold has callers, the model lists
them. On timeout it throws `ModelSyncTimeoutException` saying what it was waiting on: the status date it expected to
move, the schedule rows that exist, whether the engine holds the model or lists the caller, the synchronisation error
rows added and the engine's recent errors. `ModelSyncResult` reports the elapsed time.

Creating the scaffold **before** starting the engine also works: the engine's first pass synchronises every tenant that
has a schedule row (`ModelScaffold.CreateAsync` does not insert one; `SyncAsync` does, so call it either way).

## Invoking

```csharp
await using var api = await ModelInvokeHost.StartAsync(engine);
var response = await api.InvokeAsync(scaffold, Payloads.Example().With("Currency", "978"), async: false);

response.Status;                        // 200
response.String("EntityInstanceEntryId");
response.ActivatedRules;                // the rules that fired: the properties of "Activation"
response.ResponseElevationValue;        // ResponseElevation.Value
response.PayloadField("AccountId");     // Payload.AccountId, as the model read it
response.AbstractionValue("...");       // Abstraction.{name}
response.Find("Sanction", "...");       // JsonElement? by path, names matched ignoring case
response.PropertyNames;
```

`ModelInvokeHost` is a real Kestrel host on a loopback port over the real engine, with the real `MapInvokeEndpoints`. A
request is authenticated as the user in its `X-Test-User` header (anonymous without one), so the endpoint's own
authorisation and the per-model user check run. `InvokeAsync` sends as `scaffold.UserName`; pass `user:` for another.
`SendAsync` is the raw form (verb, path, body, content type, chunked, headers, cancellation token).

`Payloads.Example()` is the JSON from *Configuration / Models*: a flat object of 66 string fields (`AccountId "Test1"`,
`TxnId "0987654321"`, `TxnDateTime "2018-08-19T21:41:37.247"`, `Currency "826"`,
`CurrencyAmount "123.45"` and so on). `With` replaces in place or appends, `Without` removes.

## Adding a new scaffolded example

The scaffold copies any model, so a new example needs no new code: make the model in the database (through the pages or
a migration), then `new ModelScaffoldOptions { SourceModelId = <id> }`. Give it its own payload with
`new PayloadBuilder([("Field", "value"), ...])`. If it carries tables the default exclusions skip that it needs, list
them in `IncludeTables`.

## What is real, and what is not

| Real                                                                 | Not real                                                         |
|----------------------------------------------------------------------|------------------------------------------------------------------|
| Postgres (the shared test database, real migrations)                 | RabbitMQ (`AMQP=False`, as in a plain deployment)                |
| Redis on `localhost` (cache, callbacks)                              | The application's cookie/JWT authentication (test header scheme) |
| The engine, its synchronisation task, the invoke pipeline, sanctions | Background services unrelated to invoking (switched off)         |
| The Minimal API endpoints, routing, authorisation, JSON              | Sanction list data (none is loaded)                              |

The engine needs Redis on `localhost:6379` and Postgres at `JubeTestConnectionString`.

## Timing and cautions

* Engine start plus the first synchronisation is about 5 to 10 seconds per fixture; an isolated copy costs a further
  second or two. Prefer the shared model for read-only tests.
* One engine per running class fixture. Do not run two fixtures' engines at once: keep classes in the `Database`
  collection. Two engines on one host name share one node-status row per tenant.
* Only one scaffold at a time may hold the documentation guid; use `ModelScaffoldOptions.Isolated` for any others.
* `TestLog` captures errors always and everything else only when enabled: assert on `engine.Log.Entries` for `ERROR`.
* An invoke that hits an engine error is logged and answers 200 with an empty body (the engine's own behaviour), so
  check the body, not only the status.

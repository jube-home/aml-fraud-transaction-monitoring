# Plan: nothing is first heard from the database or the wire

Owner principle (2026-11-09): the first we hear of a deterministic fault must never be the database or the wire. A
missing table or column name is deterministic, so it must be caught in .NET, with our own localised refusal, before SQL
is sent. Database error text is never surfaced (see memory `no-database-error-text-to-agents-or-clients`): browser and
Ask Jooby agents get a generic message plus, for agents, the SQLSTATE only. The R server's `POST /validate` (syntax,
disallowed functions, non-blocking `lintr`, without running) is the model to copy: cheap, called often, an LLM validates
in a loop before it executes.

## Starting point (verified in the code)

- `Jube.Data/Reporting/PostgresSqlValidator.cs` already parses with `PgSqlParser` (protobuf AST) and does: select-only,
  denied functions, denied/protected relations, schema qualification, `CollectTableReferences`,
  `RewriteTableReferences`.
- `Jube.Service/DynamicSql/AskJoobySqlQueryClient.ValidateAsync` validates by running `EXPLAIN` against the database, so
  a missing column is first discovered by the database (now reduced to SQLSTATE only). This is the main gap.
- Callers of the validator: visualisation datasource (`Jube.Data/Reporting/Postgres.cs`), case SQL (`CaseSqlGuard`),
  form validator authoring (`FormValidatorAuthoring`), checklist item SQL (`ChecklistItemSqlAuthoring`), Ask Jooby
  (`AskJoobySqlQueryClient`, `DynamicSqlTableAccess`).
- Other author-supplied text that can fault late: Exhaustive `filterSql` (browser-built, stored, run by the engine; see
  memory `exhaustive-filtersql-is-client-built-and-stored`), case search compiled SQL (`SessionCaseSearchCompiler`),
  abstraction/activation rule expressions (server `BuilderRuleTranslator`), R scripts (already has `/validate`),
  parameters bound to SQL (named parameters).

## Work packages

### WP1. Schema-aware, parse-only SQL validation (the core)

New in `Jube.Data/Reporting` (or `Jube.Service/DynamicSql`, follow the Repository/Query pattern in memory
`pattern-hygiene-repository-query-only`, one class per file, no comments, no partial classes):

1. A **schema catalogue snapshot**: tables and columns with types for exactly the relations a caller may reach (the same
   allow-list the caller already computes: tenant tables, `Archive`, the datasource's permitted tables, Ask Jooby's
   permitted set). Loaded by one `Query/` class from `pg_catalog` (permitted names only), cached with a short TTL and
   invalidated on migration or `ServiceChange`. Never queries per validation.
2. A **semantic checker** that walks the `PgSqlParser` AST with that catalogue: unknown table, unknown column (qualified
   and unqualified, with alias and CTE and subquery scope), ambiguous column, wrong arity or unknown function
   (allow-list of functions with signatures), obvious type misuse (comparing a boolean column with a string literal,
   `sum` over text, date arithmetic on text), `GROUP BY` / aggregate misuse (non-aggregated column not grouped), unknown
   named parameters versus supplied parameters, `LIMIT` and `OFFSET` expression types. Return structured findings: code,
   localised message from our own resources, line and column (reuse `PostgresErrorPosition` semantics), and for agents a
   "did you mean" from the catalogue (nearest column, Levenshtein) which is safe because it only names things the caller
   is already permitted to see.
3. Wire it into **every** SQL entry point before any connection is opened, with the checker's refusal as the only
   message: datasource Card, Preview, Introspect, Create, Update; Ask Jooby `ValidateAsync`, `ExplainSqlQuery` and
   execution tools; case SQL; form validator; checklist item SQL.
4. `EXPLAIN` stays only as a last-resort second check for things the checker deliberately cannot decide (planner-only
   errors); anything it still rejects is logged at warning with "validator gap" so gaps are findable, and answers with
   SQLSTATE only.

Acceptance: a Service test per finding code; a table-driven test of 150+ good and bad statements; a **differential
test** that runs each bad statement against a real Postgres and asserts that the checker rejects it before the database
(the test fails for any statement where the database rejects and the checker passed, listing them as gaps); the
agent-facing `ValidateAsync` no longer needs the database for missing relation or column. No performance assertions.

### WP2. A reusable cheap validate endpoint and Ask Jooby tool

Mirror the R server: `POST /api/.../Validate` for datasource SQL (authoring editor calls it as the user types,
debounced), returning findings with positions. SPA: show findings inline in the existing editor (line and column
markers), localised in all nine locales (tokenised, RTL correct). Ask Jooby: the existing SQL validate tool returns the
checker's findings (our messages plus "did you mean") plus SQLSTATE only when the database still rejects.

### WP3. Other deterministic validations worth doing (assess each, build those that are clearly deterministic)

- **Exhaustive `filterSql`** and **case search compiled SQL**: run the WP1 checker server-side at save time against the
  Archive column set, not only at run time.
- **Rule expressions and the builder translator**: confirm every field and method reference resolves at save time;
  surface positions through `ErrorSpan` (memory `errorspan-columns-are-original-line-coordinates`).
- **R scripts**: confirm the Ask Jooby R path calls `/validate` before `/execute` always, not only when the agent
  chooses.
- **Wire and API**: DTO validation already exists (FluentValidation); sweep for server-detected faults that today
  surface as a 500 or a database constraint (unique key, foreign key, length overflow, null into non-null).
  Pre-validate: unique natural key, foreign key target exists and belongs to the tenant, string length against column
  size, enum range. The first error the user sees must be ours, localised, field-keyed. Use the column metadata the form
  model already has.
- **Migrations and seeds**: a Convention test that fails if a repository or query class references a table or column
  that does not exist in the migrated template database (compile-time-ish guard for linq2db string SQL and raw SQL).
- **Parameters** bound to author SQL: type-check supplied values against the column type before binding.
- **Documentation**: update `docs/` (SQL authoring, Ask Jooby, Testing catalogue via the docs tests) and
  `review/pentest-triage.md`.

### WP5. Prompts and agent direction: validations are gold, use them

Every agent and prompt that writes SQL, R, rules or any other validated artefact must be told, in its instructions, to
validate first and to treat findings as authoritative:

1. **Validate before execute, always.** The agent instructions (`Jube.AskJooby/Agents/Basic/AgentDefinitions.cs` and the
   agent hierarchy docs in `docs/AgentHierarchy`, which are hand-curated; see memory
   `agent-hierarchy-docs-not-regenerable`) say: call the validate tool first, loop on its findings until clean, only
   then run. Same for R (`/validate` before `/execute`), rule expressions and any other tool that has a validator.
2. **Findings are gold.** The prompt explains the finding shape (code, message, line and column, "did you mean") and
   tells the agent to fix exactly what is named, not to guess, not to retry the same statement, and not to ask the user
   about a fault the validator already described.
3. **Never expect database text.** The prompt states that a database rejection carries only a SQLSTATE and means the
   validator missed something; the agent reports it plainly and does not speculate about names or schema.
4. **Tool descriptions carry the direction too:** the `[Description]` text of the validate tools says "call before
   running", and the run tools' descriptions say "validate first". Keep the tool-icon invariant and routing-rules gate
   in mind (memories `askjooby-tool-icons-literal-entry-invariant`, `askjooby-routing-rules-gate-the-hierarchy`).
5. **Enforcement, not only prose:** add a lightweight guard so a run tool refuses (with our own message pointing at the
   validate tool) when the same statement has not been validated in the conversation, and make the repeated-tool ledger
   (`RepeatedToolLedger`) treat validate-then-run as the expected pair. Prompt-lint tests assert the instruction text
   contains the validate-first direction for each SQL-writing agent, so it cannot silently regress.
6. **Docs:** update `docs/AskJooby/SqlQueryTools.md` and the agent hierarchy docs; add the new prompt text to the
   Testing catalogue.

### WP4. Guardrails and tests

- Convention test: every code path that opens a connection with author-supplied SQL calls the checker first (scan for
  the entry points, same style as `NoAsParametersConventionTests`).
- PenTest: ERROR-LEAK markers stay; add the "column that does not exist" probes to the leak scanner for every author-SQL
  route and assert our own message.
- Unit/Service tests under existing categories (Unit, Service, AskJooby, Convention, PenTest). Locale parity and compile
  tests must pass with new keys in all nine locales.

## Order and verification

WP1 first (checker + catalogue + differential test), then wire entry points, then WP2, then WP5 (prompts and agent
direction, once the tools return the new findings), then WP3 items one by one, WP4 throughout. Gates before reporting:
clean build, Unit, Convention, SPA (`vue-tsc`, eslint, Vitest), Service slice, and one PenTest run, each on its own
freshly cloned database and containers (never the ambient `JubeTestConnectionString`, never `docker volume prune`,
export connection strings in the same command, kill leftover processes by PID).

## House rules to carry

No code comments, one class per file, no partial classes, never `git checkout`/revert/stage/commit, DTO ReSharper
disables, `Interfaces/` and `Exceptions/` nesting, Repository (one table) vs Query pattern, no remote fonts, locale
coverage total (en, pl, ar, ka, id, sv, ur, hi, fr), fix bugs as found, no performance assertions, Rider lint last
(`rider-lint-sweep`).

## Open owner decisions (agent decides sensibly and records them)

- Whether "did you mean" suggestions go to the browser editor as well as agents (default: yes, only permitted names).
- Strictness for type misuse warnings: error versus warning (default: error only when the database would certainly
  reject).

## Status (2026-11-09)

### Done

- WP1: schema-aware parse-only checker `Jube.Data/Reporting/Check` (catalogue snapshot query and cache, semantic walker,
  35 finding codes, nine-locale messages in `Jube.Resources/SqlCheckResources`), wired before any connection into
  datasource Card, Preview, Introspect, Create, Update, the datasource command execution query, Ask Jooby validate,
  explain and run, case SQL (guard and runner), form validator (guard and runner), checklist item SQL and the compiled
  case search prepare step. `EXPLAIN` stays as the Ask Jooby second check; a rejection there logs "validator gap" and
  answers with SQLSTATE only. Differential test against real Postgres (217 statements, no gaps), unit tests per code and
  for positions and parameters, localisation tests, Convention test `AuthorSqlPreflightConventionTests`.
- WP2: `POST /api/VisualisationRegistryDatasource/CheckSql` plus SPA wiring (debounced, every finding listed under the
  status badge, positions from the existing token, so no new locale text). Vitest `checkSql.spec.ts`.
- WP5: `ValidateFirst` instructions in every agent, tool descriptions, `SqlValidationLedger` run guard, R validated
  before every execute, `ValidateRScript` repeatable, prompt-lint tests, docs (`docs/AskJooby/SqlQueryTools.md`,
  `docs/AgentHierarchy/orchestration.md`) and the Testing catalogue.
- WP3 built: Exhaustive `filterSql` checked at save against Archive (`FilterSqlSchema`); R scripts always validated
  first; parameter values type-checked against the column type (datasource parameters, case Id and CaseKeyValue).
- WP4: Convention test and PenTest probes (missing table, column in several positions, function, literal, syntax)
  asserting our own message; ERROR-LEAK markers kept for database text. Our own wording avoids the leak scanner
  patterns.

### Deferred (assessed)

- Case search compiled SQL at save: it is generated by our compiler from a validated filter tree, not authored, so the
  checker would only find our own bugs; it is checked at the prepare step instead (a finding sets Prepared to 0 and
  logs).
- Rule expressions and builder translator positions: already resolved at save by the rule parser with ErrorSpan; no
  deterministic gap was found, so nothing was added.
- Wire and API sweep (unique key, foreign key, length overflow, null into non-null): a per-service audit across hundreds
  of DTOs; `UniqueViolation.GuardAsync` already covers many services. Needs its own pass with the form model column
  metadata.
- Convention test that every repository and query class references only tables and columns present in the migrated
  template database: needs a model of linq2db mapped names; not started.
- Rider lint sweep: Rider MCP lint timed out on the first batch; not run.
- Schema snapshot invalidation is by 5 minute TTL and process restart (migrations run at boot); no ServiceChange hook.

### Owner decisions taken (change if you disagree)

- "Did you mean" goes to the browser editor as well as agents, drawn only from names in the caller's own query scope
  (allow-list names for tables; none in deny mode).
- Type misuse is an error only when the database would certainly reject (cross-category comparison, invalid literal,
  wrong aggregate or function argument); anything uncertain is left to the database second check.
- Our English wording avoids the phrases the PenTest leak scanner uses for database text.
- The checker fails open when the catalogue cannot be read (the database then speaks, as before) and is skipped for
  non-SELECT text, which the select-only gate still refuses.
- `RunSqlQuery` enforcement is per conversation (per user when there is no conversation) and keyed on the
  whitespace-normalised statement; unit tests of the client leave it off.
- Fr translations exist for the new resource set; `DatasourceChartResources` still has no fr file (existing gap, not
  widened).

# Slice review: model-references

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing built, no .NET or DB test run. Prettier, eslint and vitest
were run on `src/valueGrid` and the two e2e specs (clean, 17 vitest tests pass); `vue-tsc --build` printed nothing
(clean).

## 1. Summary and verdict

Verdict: READY AFTER OWNER MANUAL TEST AND THE SERIAL TEST RUN. Isolation questions answered:

1. **Inline scripts (C# or VB code): tenants cannot author code.** `EntityAnalysisInlineScriptDto` is "Read-only, rows
   are created and compiled outside the API" and the endpoints only list; the model-level Inline Script row
   (`EntityAnalysisModelInlineScript`) only references a catalogue id. Compile uses Roslyn with every loaded assembly
   referenced and no sandbox (`InlineScriptCompilationExtensions.BuildDependencyArray`), which is acceptable only
   because the code is installer-owned. Recorded as a design fact, not a bug.
2. **Inline functions and gateway rules (VB fragments typed by tenants): guarded by a token allow list, applied twice.**
   `Parser.Parse` (Jube.Parser/Parser.cs 328) splits each line on a fixed separator set, strips string literals and
   refuses any token not in the allow list; it runs on save (`RuleScriptParser` rule set `"default,RuleScript"`, so it
   is part of Insert and Update) and again at synchronisation (`SyncEntityAnalysisModelGatewayRulesExtensions` lines
   219-236, 190-200). Vocabulary has no loops, imports, types or reflection, so rules terminate. Corpus:
   `RuleParseSecurityCorpusTests` (70 seeds x 12 spellings, gateway and activation). The gap was that nothing proved the
   save path wired it for these two areas: fixed with tests.
3. **XPath is JSONPath, not XML**: `SelectToken` in `RequestFieldExtraction`/`RequestJsonAccessor`. No XML parser, so
   XXE does not apply. **Real hole found and fixed**: the regex filter operator `=~` was accepted and (to my
   recollection of Newtonsoft 13, not verified in source) evaluated without a timeout, a per-transaction ReDoS. Now
   refused on save.
4. **Tenant isolation, IDOR, mass assignment, permission verbs**: closed and covered by the shared PenTest base classes
   (section 5). Verbs: XPath 7 (13 and 12 for by-model and by-type reads, 2 override), Inline Function 8, Inline Script
   9, Gateway Rule 10, Sanction 11, Tag 37 (1 for listing by model), List 3, Dictionary 4.
5. **Hostile list and dictionary values**: stored as data; CSV rows validated (512-character value, duplicates, expiry),
   covered by PenTest `Csv_ContentAttacksAreRowErrorsOrInertDataAsync` and
   `CsvUpload_ImportsCellsAsData_IncludingFormulaAndMarkupKeysAsync`.

## 2. Files reviewed (first-touch pull-ins)

`--closure`: none. Claimed: `Jube.App/Vue/src/valueGrid` (also claimed by data-grid, which is earlier: inherited, only
checked), the ten endpoint files, e2e `valueGrid`, `hierarchy`, seven docs pages. Read: Jube.Validations (XPath,
InlineFunction, GatewayRule validators whole; ListValue partly), `RuleScriptParser.cs` 20-60, Jube.Parser `Parser.cs`
29-470 and 767-835, `NumericHelper.cs`, `Compiler/Compile.cs`, `Compiler/RuleSandbox.cs` 1-200,
`RuleParse.DefaultReferences`, Engine `InlineScriptCompilationExtensions.cs`,
`ConfigureTokenParserForSecurityExtensions.cs`, `SyncEntityAnalysisModelGatewayRulesExtensions.cs` 168-330,
`RequestFieldExtraction.cs`, `RequestJsonAccessor.cs`, services (permission arrays, InlineFunction
Insert/Update/ParseRule, ListValue upload loader), `EntityAnalysisModelListValueEndpoints.cs` 88-175, repositories
(tenant reference counts only; XPath and GatewayRule repository start read), `EntityAnalysisInlineScriptService.cs`
30-120, DTO of the catalogue, resx for all ten classes, existing PenTest and service tests named below.

## 3. Findings

| #  | Severity          | Where                                                                                                                        | What                                                                                                                                                                                                                                   | Status                                                                                                                                                                                  |
|----|-------------------|------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | High (ReDoS)      | EntityAnalysisModelRequestXPathDtoValidator.cs XPath rule                                                                    | `=~` regex operator in a JSONPath accepted and run per transaction by Newtonsoft with no timeout.                                                                                                                                      | Fixed: rule `XPathRegexNotPermitted`, resource key in .cs and 8 resx, 2-case service test. Recollection of Newtonsoft behaviour not source-verified, the refusal is correct either way. |
| 2  | High (same hole)  | EntityAnalysisModelDtoValidator.cs EntryXPath, ReferenceDateXPath                                                            | Same. File belongs to model-core, running now.                                                                                                                                                                                         | BUG line in SHARED-TODO                                                                                                                                                                 |
| 3  | Medium (test gap) | Gateway Rule and Inline Function services                                                                                    | No test that a hostile Coder, Builder or function script is refused by Insert and Update and that the stored row is unchanged (corpus tests `RuleParse.Execute` directly; `ParseRule` has no test in either file).                     | Fixed, 5 tests                                                                                                                                                                          |
| 4  | Low (stale test)  | PenTestModelRequestXPath.XPathExpressions_AreStoredAsInertDataAsync                                                          | Uses XPath 1.0 strings (`document('file:///etc/passwd')`) although the engine evaluates JSONPath; passes only because everything is stored. It asserts nothing about JSONPath.                                                         | Left; covered by finding 1 test                                                                                                                                                         |
| 5  | Medium            | live rule execution (ReflectRuleHelper, GatewayRulesExtensions, InlineFunctionsExtensions, activation and abstraction paths) | Delegates run synchronously on the engine thread with no time or memory bound; RuleSandbox bounds only simulation and its timed-out task keeps running.                                                                                | See section 12: structural bounds proven, unbounded primitive `Repeat` fixed; wall-clock bound is a design decision with reason                                                         |
| 6  | Low (DoS cost)    | Parser.Parse                                                                                                                 | Quadratic literal stripping and a scan of every allow-list entry per token.                                                                                                                                                            | Fixed, section 12                                                                                                                                                                       |
| 7  | Low               | EntityAnalysisModelRequestXPathDtoValidator parent check                                                                     | Uses `EntityAnalysisModelRepository.GetByIdAsync`, whose filter admits rows with a null TenantRegistryId (memory: tenantless GetByIdAsync trap), so system models are visible to every tenant. Tenant repositories are used otherwise. | Left, owner decision (no such models known)                                                                                                                                             |
| 8  | Info              | CSV upload endpoints                                                                                                         | No per-endpoint size or row cap; rely on the Kestrel limit (owner decided to leave).                                                                                                                                                   | None                                                                                                                                                                                    |
| 9  | Info              | Inline Script catalogue                                                                                                      | Not tenant scoped by design.                                                                                                                                                                                                           | None                                                                                                                                                                                    |
| 10 | Info (locale)     | all ten `.cs`/`.resx` classes                                                                                                | Only en and fr existed.                                                                                                                                                                                                                | Fixed, 70 files                                                                                                                                                                         |

RTL grep for physical left/right in `src/valueGrid`: none (TypeScript only). No code comments were added or touched.

## 4. Fixes made

1. `Jube.Validations/EntityAnalysisModelRequestXPath/EntityAnalysisModelRequestXPathDtoValidator.cs`: XPath refused when
   it contains `=~` (`XPathRegexNotPermitted`). `Jube.Resources/EntityAnalysisModelRequestXPathResources.cs` and the 8
   resx gained the key.
2. Tests (unrun, uncompiled):
   `EntityAnalysisModelGatewayRuleServiceTests.SavingAHostileRuleScriptIsRefusedAndNothingIsStoredAsync` (Theory,
   builder and coder) and `UpdatingARuleToAHostileScriptIsRefusedAndTheStoredScriptIsUnchangedAsync`;
   `EntityAnalysisModelInlineFunctionServiceTests.SavingAHostileFunctionScriptIsRefusedAndNothingIsStoredAsync` and
   `UpdatingAFunctionToAHostileScriptIsRefusedAndTheStoredScriptIsUnchangedAsync`;
   `EntityAnalysisModelRequestXPathServiceTests.AnXPathUsingTheRegularExpressionOperatorIsRefusedAndNothingIsStoredAsync`
   (Theory, 2 cases). Modelled on
   `EntityAnalysisModelAbstractionRuleValidateTests.SavingARuleWhoseTextDoesNotParseIsRefusedAndNothingIsStoredAsync`.
3. 70 resx files, docs pages, `docs/Testing/ServiceModelConfiguration.md` rows.

## 5. Test and pen-test matrix

| Layer                                                                                      | Evidence read                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Status                                  |
|--------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------|
| Service CRUD, validators, permission per verb, tenant isolation, audit, event, concurrency | ten service test files by script count (XPath 53, Function 47, Script 45, Gateway 59, Sanction 53, Tag 43, List 45, ListValue 70, Dictionary 46, Kvp 54 + CSV 15); Gateway file test names read in full                                                                                                                                                                                                                                                                                                                                                                                                  | Covered                                 |
| Service, hostile script through Insert and Update                                          | none before                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Added (finding 3)                       |
| Service, JSONPath regex operator                                                           | none before                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Added (finding 1)                       |
| Parser security corpus                                                                     | `RuleParseSecurityCorpusTests`, `RuleSandboxTests`, `RuleParseNestingDepthTests` (names)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Covered                                 |
| PenTest generic matrix                                                                     | `PenTestModelAreaBase` (`Matrix_EveryRouteEveryRoleAsync`, `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`, `Bola_ForeignParentReferencesAreRefusedAsync`, `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`, `Injection_*`, `TypeConfusion_*`, `RequestStructure_*`, `BusinessFlow_*`, `Limits_MaxLengthOversizeAndHostileExpressionsAsync` read) used by PenTestModelInlineFunction, InlineScript, List, ListValue, Tag, Sanction, RequestXPath; `ModelAreaPenTestBase` (CrossTenant, CrossTenantParent, MassAssignment, JsonColumns, Locked, Concurrent, Oversized) by GatewayRule and Dictionary | Covered (script count 0 is inheritance) |
| PenTest, CSV (list, dictionary)                                                            | `Csv_*` x6 in ListValue, `CsvUpload_*` x6 in Kvp (names, ListValue endpoint registration read: antiforgery present)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Covered                                 |
| PenTest, Inline Script catalogue                                                           | `PenTestEntityAnalysisInlineScript` (matrix, tenant, injection)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Covered                                 |
| Engine units                                                                               | Gateway, InlineFunction, InlineScript extensions tests, `InlineScriptCompilationExtensionsTests`, `FastPathRequestReaderTests`                                                                                                                                                                                                                                                                                                                                                                                                                                                                           | Names only                              |
| Vitest                                                                                     | `src/valueGrid/__tests__/model.spec.ts` 17 pass; `api.ts` has no spec                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | `api.ts` gap belongs to data-grid       |
| Playwright                                                                                 | valueGrid 13, hierarchy 6 counted; no `.only`/`.skip`; not run                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           | Counted only                            |

To run in the owner's pass: the five Service tests above (Category=Service, `GatewayRule`, `InlineFunction`,
`RequestXPath`), then `Category=PenTest` for the model area (especially `PenTestModelRequestXPath`, which must still
pass with the new rule: none of its XPath strings contains `=~`).

## 6. Translations

Server: created `.pl .ar .ka .id .sv .ur .hi` for all ten classes (EntityAnalysisModel{RequestXPath, InlineFunction,
InlineScript, GatewayRule, Sanction, Tag, List, ListValue, Dictionary, DictionaryKvp}Resources), 70 files, written from
the English resx structure; verified by script: well-formed XML, key set equal to English, `{0}` kept. The five common
messages reuse the RoleRegistry wording. In ar and ur every Latin phrase (String, XPath, TTL, JSON, Coder...) is wrapped
in U+2066 and U+2069 bidi isolates; Western digits only. `.fr.resx` untouched. SPA: slice owns no keys (valueGrid
inherited, proposals in data-grid.md); `review/model-references.locale-proposals.json` states that. Georgian, Nastaliq
and Devanagari rendering is a manual-test item (fonts are a shared TODO). Hard-coded English in the DTOs' grid titles is
the existing shared TODO.

## 7. Docs

Seven pages under `docs/Configuration/Models/` (RequestXPath, InlineFunctions, InlineScripts, GatewayRules, Tags, Lists,
Dictionaries). Each gained a "Permissions and security" section and TODO markers: one `TODO(screenshot)` before every
existing image (file prefix `model-references-`) plus four `TODO(docs)` per page. Find them with
`grep -n "TODO(" docs/Configuration/Models/<page>/index.md`. Old Razor PNGs left on disk. Not rewritten: the pages still
describe the Razor layout (marked stale).

## 8. Shared-code TODOs raised

Six lines under `model-references` in `review/SHARED-TODO.md` (two BUG, one design, three info).

## 9. Manual test sheet

1. Permission per area (7, 8, 9, 10, 11, 37, 3, 4): without it list and each verb is 403; with it works.
2. XPath: create `$.a.b`; try `$.a[?(@.b =~ /x/)]` (refused with the new message in each locale); a malformed path falls
   back to the default value at invoke time.
3. Inline Function and Gateway Rule: save `Return "x"`, then `Return System.Diagnostics.Process.Start("id") Is Nothing`
   (refused, row unchanged), a Builder rule and a Coder rule; Retract and Resubmit.
4. Inline Script: pick a catalogue script, confirm no code editor exists.
5. Lists and Dictionaries: add, edit, delete pending approval, duplicate value, 513 characters, past expiry date, CSV
   with blank, long and formula rows, two tabs.
6. Cross-tenant: other tenant's ids behave like missing.
7. en, pl, ar (RTL, IDs and XPath stay left-to-right), ka, id, sv, ur, hi; narrow width; dark theme; keyboard only; one
   hostile string per field.

## 10. Commit pathspecs

`--paths`: `Jube.App/Vue/src/valueGrid` (data-grid first), `Jube.App/Vue/e2e/valueGrid.spec.ts`,
`Jube.App/Vue/e2e/hierarchy.spec.ts`, the ten `Jube.App/Endpoints/EntityAnalysisModel*Endpoints.cs` globs, seven docs
folders. Hand-picked: 70 resx files and the key addition to `EntityAnalysisModelRequestXPathResources.cs`/`.resx`/
`.fr.resx` is untouched;
`Jube.Validations/EntityAnalysisModelRequestXPath/EntityAnalysisModelRequestXPathDtoValidator.cs`; the three service
test files (new tests only); `docs/Testing/ServiceModelConfiguration.md` (three hunks); `review/model-references*`.

## 11. Not covered

- Nothing compiled or run; the five new tests and the validator edit are unverified by a compiler.
- Newtonsoft 13.0.4 regex-filter timeout behaviour not verified in source.
- Not read: the ten endpoint files except ListValue, most repositories, Sanction engine code (`SanctionsExtensions`),
  tag and list engine paths, `RuleScriptParser.ParseAsync` beyond 60 lines, migrations, `FastPathRequestReader` (whether
  `=~` can reach it), Visual Basic interpolated-string behaviour against the token scanner (suspected, unproven:
  `$"{...}"` is stripped as a literal but the leading `$` token fails the allow list).
- PenTest, Playwright not run; Rider lint not run on the C# edits; `.vue` not changed.
- XPath repository `Update` parent re-check read by name only.

## 12. Addendum (owner policy: fix bugs, 2026-11-08)

**Live execution bounds (finding 5).** Verified: live rules are compiled delegates called synchronously
(`ReflectRuleHelper.Execute` x4), no timeout. Why a wall-clock bound was not added: .NET cannot abort a running thread,
so a bound means running each rule on another thread and abandoning it on timeout (the same leak RuleSandbox has) and
paying a thread hop per rule per transaction; that is a design decision, left to the owner with that reason. What bounds
a rule today: the allow list has no loops, declarations, imports or reflection, so a fragment is a finite expression;
every regex in the vocabulary carries a timeout (`RegexSupport.matchTimeout` 250 ms, `FlowPredicates` 100 ms) or is a
fixed linear pattern (`IsValidEmail` read, not catastrophic); other loops are capped (geohash precision 12, radix 2-36).
One unbounded primitive found and fixed: `String.Repeat(int)` and `Char.Repeat(int)` (token `Repeat` is allowed because
the parser adds every method name of `Jube.Dictionary.Extensions`) could allocate gigabytes or overflow; capped at
100000 characters (same as the `RepeatText` agent tool) with `ArgumentOutOfRangeException`, which the rule's try/catch
logs without stopping the engine thread. Tests:
`StringExtensionsPart3Tests.RepeatRefusesAResultOverTheCharacterLimitInsteadOfAllocatingIt`,
`CharExtensionsTests.RepeatRefusesAResultOverTheCharacterLimitInsteadOfAllocatingIt`. No DynamicEnvironment setting
added since nothing configurable was introduced.

**Quadratic parse (finding 6).** `Parser.Parse` now strips literals with one `Regex.Replace` (5 s match timeout, a
timeout falls into the existing "could not be parsed" refusal) and checks single-word tokens against a hash set
(`Jube.Parser/TokenIndex.cs`), looping only over multi-word entries (such as "End If"). Semantics kept
(case-insensitive, now ordinal). Length is already bounded: `RuleParse.MaximumRuleTextLength` (test
`RuleTextOverTheMaximumLengthIsAnErrorWithoutParsing`) and the validators' 65,536 limit with tokenised, all-locale
messages (`FunctionScriptMaxLength`, `BuilderRuleScriptMaxLength`, `CoderRuleScriptMaxLength`), so no new limit was
needed. Test: `RuleParseTests.ALineOfManyStringLiteralsStillParsesAndAHostileTokenAfterThemIsStillLocated`.

**Newtonsoft `=~` (finding 1), verified in source.** Decompiled `Newtonsoft.Json 13.0.4`
`BooleanQueryExpression.RegexEquals`: `matchTimeout = settings?.RegexMatchTimeout ?? Regex.InfiniteMatchTimeout`, so
`SelectToken(path)` without `JsonSelectSettings` runs the regex with no timeout. Entry points: the RequestXPath
validator now refuses `=~`. Still open: every `SelectToken(path)` of a stored path. In the live invoke path I fixed
them: new `Jube.Engine/.../Extraction/BoundedJsonPath.cs` (250 ms timeout, a timeout selects nothing) used by
`RequestFieldExtraction` (2 sites), `RequestJsonAccessor` (2) and `InvocationContextBuilder.EntryId`; this also protects
rows stored before the validator rule, and the Model's EntryXPath and ReferenceDateXPath at run time. Test:
`BoundedJsonPathTests` (3). Other entry points (cases, journal, exhaustive, sample service) are listed as a BUG line in
SHARED-TODO for their owners. The model validator is model-core's and was not edited.

**Cross-reference (item 4).** Null-tenant model visibility (finding 7) is being decided by model-core; untouched.

Tests to run: `Category=Unit` classes `BoundedJsonPathTests`, `RuleParseTests`, `RuleParseSecurityCorpusTests`,
`StringExtensionsPart3Tests`, `CharExtensionsTests`, `ParserDynamicEvalTests`, `RuleVocabularyTests`; Service tests
listed in section 5; PenTest model area. Nothing compiled or run: unverified are the `TokenIndex` and `Regex` edits in
`Parser.cs`, the three edited Engine files, and the new tests (the hostile-token assertion assumes `RuleParse.Execute`
keeps the parser's `'Process'` message).

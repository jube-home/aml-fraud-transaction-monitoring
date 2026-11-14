# Slice review: wire-and-api

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET or DB-backed test was run. Run
locally: `npx vitest run src/api` (12 files, 107 tests passed, includes my new file), prettier check on `src/api` and
`ProblemAlert.vue` (clean), eslint on `src/api` and `ProblemAlert.vue` (no output).

## 1. Summary and verdict

Verdict: READY AFTER OWNER ACTIONS. The wire path is well built and unusually well tested; I found no exploitable hole.
The decryption path is authenticated-only, AAD-bound, replay-protected (fail-closed), skew-limited; MessagePack is read
with UntrustedData security and depth 64; compressed requests are never inflated; row grids are capped. Real findings
are small: server culture list lacked ka/id/sv/ur/hi (fixed), a stray tracked stamp file, a misleading test message, a
duplicate-key bug in `ProblemAlert.vue` (fixed), and thin coverage of Accept-Language, the stream helper and encrypted
body limits (tests added). Before commit:

1. Someone with the .NET SDK builds and runs `WireEncryptionBodyLimitsTests` (new, uncompiled) and
   `RequestLocalisationTests` (edited), plus `WireGeneratedDecodersTests` (sync check).
2. Owner decides French (finding 3) and the shared TODOs.
3. Owner edits `slices.json` (finding 8).
4. Owner supplies screenshots and resolves docs TODOs.

## 2. Files reviewed (first-touch pull-ins)

Front end, all read whole:
`src/api/{language,problems,queryClient,wireCompression,wireRuntime,wireCrypto,wire,useApi}.ts`,
`src/components/ProblemAlert.vue`, test names of all 10 specs in `src/api/__tests__`, `queryKeys.ts` (grep for
apiLanguage only; 169 lines, not read in full).

`SLICE_BASE=master --closure` lists as pull-ins: `generated/wire.generated.ts` and `wireTypes.generated.json` (unowned,
now mine), and `auth/sessionExpiry.ts` (shell-auth), `overrides/types.ts`, `sanction/types.ts`, `datasource/types.ts`
(later slices): read only as dependencies, not reviewed.

Back end, read by hand: `Jube.App/Wire/` WireEncryptionMiddleware, WireCipher, MessagePackRequestMiddleware,
MessagePackResponseFilter, MessagePackHttpResult, WireMediaType, WireEndpointConventions, WireMode, WireCompression,
WireOptions, WireSerializer, WireHttpCompressionExtensions, WireDictionaryKeys, WireGeneration, WireTypeScriptGenerator,
WireEndpointCatalogue, WireCatalogueProviderFactory, WireCatalogueServiceGuess, JsonShapedObjectFormatter,
StringKeyedMapFormatter, ColumnarRowsFormatter (60 onward); `Endpoints/Query/WireSchemaEndpoints.cs`; `Startup.cs` lines
165-210, 276-312, 410-426; `Program.cs` 80-105 (Kestrel limits); `Middlewares/RequestHardeningMiddleware.cs` 13-200;
`Code/RequestLocalisation.cs`; `Jube.App.csproj` GenerateWireDecoders target; `DynamicEnvironment.cs` wire keys (grep)
and docs table in `docs/Concepts/EnvironmentVariables/index.md` 163-170.

Not read: WireTypeRegistry (404 lines), ColumnarJson*, ColumnarRows, ColumnarRowsResolver,
Utc/Finite/Decimal/Char/Nullable formatters, StringKeyedMapResolver, WireHttpCompressionKind; see section 11. The
Garnet/Resp implementation behind `ICacheWireReplayRepository` is residual infrastructure and was not read; the wire
path depends on it (`StringSetIfAbsentAsync`, fail-closed per test).

## 3. Findings

| #  | Severity           | Where                                                 | What                                                                                                                                                                                                                                                                                                                               | Status                                                                              |
|----|--------------------|-------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| 1  | Medium (i18n)      | Code/RequestLocalisation.cs:25                        | Supported UI cultures were `en, pl, ar`; `ka`, `id`, `sv`, `ur`, `hi` fall back to English.                                                                                                                                                                                                                                        | Fixed: list is now en, pl, ar, ka, id, sv, ur, hi; tests added.                     |
| 2  | Low                | Jube.App/wire.generated.stamp                         | Empty tracked file. The csproj stamps under `$(IntermediateOutputPath)`, so this file is a leftover.                                                                                                                                                                                                                               | Left, shared TODO.                                                                  |
| 3  | Decision           | Jube.Resources                                        | About 180 `*.fr.resx` exist but the server never selects French (`RequestLocalisationTests` asserts `fr` gives `en`, line ~46).                                                                                                                                                                                                    | Owner decision, shared TODO.                                                        |
| 4  | Low                | Jube.Tests/App/Wire/WireGeneratedDecodersTests.cs:198 | Failure message told the developer to run `npm run wire:generate`, which does not exist in package.json.                                                                                                                                                                                                                           | Fixed: names the build and `--wire-generate`.                                       |
| 5  | Low                | components/ProblemAlert.vue                           | `li` key was the reason text; two identical reasons give a duplicate key.                                                                                                                                                                                                                                                          | Fixed: keyed by index.                                                              |
| 6  | Info (hardening)   | WireEncryptionMiddleware.cs                           | `MaximumBodyBytes` is 64 MB but Kestrel is not configured (Program.cs), so its default 30,000,000 byte limit applies first. The 64 MB cap is therefore not the effective limit; unbuffered chunked sealed bodies are refused (no ContentLength). Same default bounds `MessagePackRequestMiddleware`, which buffers the whole body. | Left; documented with TODO; consider lowering the cap or setting an explicit limit. |
| 7  | Info               | WireEncryptionMiddleware.cs                           | Whole response is buffered to be sealed, so streaming endpoints cannot be sealed. The client never seals streams or uploads (`useApiStream`, `formData`), and docs say so.                                                                                                                                                         | By design.                                                                          |
| 8  | Medium (inventory) | slices.json wire-and-api                              | `docs` is `docs/Concepts/API` (14 unrelated pages); should be `docs/Concepts/API/WireFormat`. `backendKeywords` lacks `RequestLocalisation`, `WireEncryptionBodyLimits`. Unowned `src/generated/*` should be claimed; `Jube.App/Wire/**`, `Jube.App/Code/RequestLocalisation.cs` and `Startup.cs` wire lines are unclaimed.        | Left, agents may not edit slices.json.                                              |
| 9  | Info               | api/useApi.ts, `readBody`                             | A sealed request whose response is not `application/x-jube-encrypted` is read as plain; a same-origin attacker cannot gain from this, and the docs claim only observer defence.                                                                                                                                                    | By design.                                                                          |
| 10 | Info               | api/wireCompression.ts                                | Declared inflated size up to 256 MB is allocated before inflating; the source is the authenticated server over TLS, and the length is checked afterwards.                                                                                                                                                                          | Accepted.                                                                           |
| 11 | Info               | Wire path touches infrastructure                      | `ICacheWireReplayRepository` (Resp/Garnet) and `TimeProvider`.                                                                                                                                                                                                                                                                     | Noted, residual.                                                                    |

No `.only`, `.skip`, `xit` in `src/api/__tests__` (grep of test names above showed none). No code comments added or
altered.

Security review notes (all read, each verified in code):

- Unauthenticated callers never reach unwrap (`context.User.Identity?.IsAuthenticated`), test
  `AnUnauthenticatedCallerCannotMakeTheServerAttemptADecryptionAsync`.
- All rejection paths give the same 400 with no body, so no padding oracle.
- Nonce is random per seal under a per-request key (client) and per-seal (server); AAD binds method, raw target,
  timestamp, inner type, and response status.
- Replay fingerprint is claimed after a successful open; TTL 600s is double the 300s skew.
- MessagePack: `UntrustedData`, depth 64, map keys must be strings, array allocations capped (`Math.Min(length, 1024)`),
  columnar rows capped at 1,000,000 and column length must equal row count.
- `__proto__` column and map keys are defined as own properties (`setOwn`), tested.

## 4. Fixes made

1. `Jube.App/Code/RequestLocalisation.cs`: supported UI cultures add ka, id, sv.
2. `Jube.Tests/Service/Query/FormModel/RequestLocalisationTests.cs`: 10 `InlineData` rows (ka, ka-GE list, id, id-ID
   list, sv, sv-SE list, ur, ur-PK list, hi, hi-IN list). Unrun.
3. `Jube.Tests/App/Wire/WireGeneratedDecodersTests.cs`: failure message corrected.
4. `Jube.App/Vue/src/components/ProblemAlert.vue`: key by index.
5. New `Jube.App/Vue/src/api/__tests__/apiLanguageAndKinds.spec.ts` (run, 22 tests passing): Accept-Language for en, pl, ar,
   ka, id, sv, ur, hi (and anonymous and streamed calls), `ApiError.kind` for every status, hostile `fieldErrors`
   shapes, `useApiStream` antiforgery, 401 handler, network and abort.
6. New `Jube.Tests/App/Wire/WireEncryptionBodyLimitsTests.cs` (5 tests, uncompiled and unrun): sealed body without
   length refused, over 64 MB refused, inner types outside JSON/MessagePack refused (no replay claim made in either
   case), and a positive control. Modelled on `Through(...)` in `WireEncryptionEndToEndTests` lines 322-345. `id` and
   `sv` are not asserted for Accept-Language in the SPA test because `setApiLanguage` is a plain setter.
7. `docs/Testing/AppAndInfrastructure.md`: catalogue section for the new C# test class.
8. `docs/Concepts/API/WireFormat/index.md` (new).

## 5. Test and pen-test matrix

| Behaviour                                                                                                                                                                                  | Evidence read                                                                                               | Status                                                                                                                       |
|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| Garbage MessagePack is 400                                                                                                                                                                 | WireEndToEndTests `GarbageMessagePackIsA400NotA500Async`                                                    | Covered                                                                                                                      |
| Depth bomb, nil body, same attack JSON vs MessagePack, 415 for bodyless endpoint                                                                                                           | WirePipelineTests (names above)                                                                             | Covered                                                                                                                      |
| Compressed request refused (decompression bomb on input)                                                                                                                                   | WireCompressionTests `ACompressedRequestBodyIsRefusedSoNoDecompressionIsEverDoneOnInputAsync`               | Covered                                                                                                                      |
| Server compression, threshold, none                                                                                                                                                        | WireCompressionTests, WireHttpCompressionTests (10)                                                         | Covered                                                                                                                      |
| Client LZ4: unknown name, non-extension, truncated, absurd size, size mismatch                                                                                                             | wireCompression.spec.ts                                                                                     | Covered                                                                                                                      |
| Malformed columnar rows                                                                                                                                                                    | WireFormatterTests `MalformedColumnarRowsAreRejected`                                                       | Covered                                                                                                                      |
| Crypto: tamper, wrong path, replay (same and other node), Resp failure fail-closed, skew, wrong server key, malformed key header, unauthenticated, WAF after decrypt, key size and pairing | WireEncryptionEndToEndTests (20)                                                                            | Covered                                                                                                                      |
| Crypto: no length, oversize, bad inner type                                                                                                                                                | New WireEncryptionBodyLimitsTests                                                                           | Added, unrun                                                                                                                 |
| Crypto interop with WebCrypto, pinned bytes                                                                                                                                                | WireEncryptionInteropTests (3); wireCrypto.spec.ts                                                          | Covered                                                                                                                      |
| Chunked body cap, NUL, null literal                                                                                                                                                        | PenTestSessionAndRequestHardening CR01 (82, 92, 105)                                                        | Covered (read, not run); `Category=PenTest` serial pass owed                                                                 |
| Generated files in sync with DTOs                                                                                                                                                          | WireGeneratedDecodersTests line ~198 using `WireTypeScriptGenerator.Stale` against `Jube.App/Vue/src/generated` | Covered. Also I compared by hand: `typesHash` 7C46FC1E28CB1807 in both files, 592 types in both. Not regenerated (no build). |
| WireSchema endpoint, auth, ETag, immutable cache                                                                                                                                           | WirePipelineTests (3 schema tests); PenTestWireSchemaTests (3, read by name only)                           | Covered                                                                                                                      |
| Accept-Language to culture, fallback, formatting culture                                                                                                                                   | RequestLocalisationTests                                                                                    | Covered, extended                                                                                                            |
| Accept-Language sent by client                                                                                                                                                             | New apiLanguageAndKinds.spec.ts                                                                             | Added, run                                                                                                                   |
| Problem text selection                                                                                                                                                                     | problems.spec.ts (4), `describeApiError`                                                                    | Covered                                                                                                                      |
| Retry policy                                                                                                                                                                               | queryClient.spec.ts                                                                                         | Covered                                                                                                                      |
| ProblemAlert component                                                                                                                                                                     | No test                                                                                                     | Gap, trivial presentational component, left                                                                                  |
| Playwright                                                                                                                                                                                 | `e2e/api.spec.ts` exists; not read or run. Slice has no claimed e2e                                         | Not covered                                                                                                                  |
| Service/CRUD, IDOR, mass assignment                                                                                                                                                        | Not applicable: no CRUD area; the only endpoint is a GET of the schema                                      | N/A                                                                                                                          |

## 6. Translations

SPA `errors` group (6 keys): pl and ar: 0 missing, 0 empty, 0 identical to English (script output and my read of the
three values). `ProblemAlert.vue` and `src/api` contain no UI text (the alert renders strings passed in; all `t()` calls
are in callers). No hard-coded UI strings in slice files.

Locales ka, id, sv, ur, hi: do not exist in `Jube.App/Vue/src/locales` (only en, ar, pl) and `i18n/index.ts`
`supportedLocales` is `['en','pl','ar']` (shell-auth's file; shared TODO). Proposed `errors` keys, machine quality:

| key        | ka                                                     | id                                                  | sv                                                |
|------------|--------------------------------------------------------|-----------------------------------------------------|---------------------------------------------------|
| unexpected | მოულოდნელი შეცდომა მოხდა.                              | Terjadi kesalahan yang tidak terduga.               | Något oväntat gick fel.                           |
| forbidden  | თქვენ არ გაქვთ ამის უფლება.                            | Anda tidak diizinkan melakukan itu.                 | Du har inte behörighet att göra det.              |
| conflict   | ჩანაწერი შეიცვალა. გადატვირთეთ და სცადეთ ხელახლა.      | Data itu berubah. Muat ulang lalu coba lagi.        | Posten har ändrats. Ladda om den och försök igen. |
| network    | სერვერთან დაკავშირება ვერ მოხერხდა. შეამოწმეთ კავშირი. | Server tidak dapat dihubungi. Periksa koneksi Anda. | Servern kunde inte nås. Kontrollera anslutningen. |
| notFound   | ჩანაწერი ვერ მოიძებნა.                                 | Data itu tidak ditemukan.                           | Posten kunde inte hittas.                         |
| server     | სერვერმა მოთხოვნა ვერ დაასრულა.                        | Server tidak dapat menyelesaikan permintaan.        | Servern kunde inte slutföra begäran.              |

Server `.resx`: this slice owns no resource class. Searched `Jube.Resources` for wire, problem and error resource
classes: none relate to this slice (the only match is an unrelated Exhaustive histogram class). So no
`.pl/.ar/.ka/.id/.sv.resx` were created. The server fallback texts the SPA shows come from `errors.*` above. Server
culture list: now en, pl, ar, ka, id, sv. Status: `.sv.resx` files in the whole `Jube.Resources` folder: 0; the wider
gap is on the shared TODO.

Language header audit:

- Client sends only `Accept-Language` (api/language.ts, useApi.ts, `useApiStream`), set by `i18n/index.ts` line 75;
  `apiLanguage` is in query keys of locale-dependent data (queryKeys.ts), so a language switch refetches. No custom
  `Language` header exists anywhere (grep in Jube.App and Jube.Service).
- Server: `UseRequestLocalization` before authentication (Startup.cs 292); supported cultures `en` only for formatting,
  UI cultures per the list above. Region tags such as `id-ID`, `ka-GE`, `sv-SE` resolve to their parent via the default
  AcceptLanguage provider (tests added).
- Culture `id` confusion: the server list is literal strings and the client passes the locale string unchanged, so there
  is no identifier-based derivation in my files. In my area the only `'id'` is a column name in
  `wireColumnarJson.spec.ts:60`. Locale files are discovered by `import.meta.glob('../locales/*.json')` (i18n/index.ts
  line 18, shell-auth), a file-name glob, so `id.json` is safe provided nothing keys a locale off a property name.
- Georgian: no UI font stack in my files to check; ka needs a font review in the shell (shared).

RTL: `ProblemAlert.vue` uses flex, gap and `items-start`, no physical left or right classes; no icon. Nothing to fix.

## 7. Docs written and TODO markers

Page `docs/Concepts/API/WireFormat/index.md` (nav_order 14 under API), drafted from code; the Environment Variables page
already documents the settings thoroughly (verified lines 163-170 against `DynamicEnvironment` and the middleware).
Markers: run `grep -n "TODO(" docs/Concepts/API/WireFormat/index.md`. There are TODO (docs) markers near the top
(read-through, `/api/invoke` behaviour), after the settings table (link target), after the hardening list (Kestrel body
limit), after the language section (French), after regeneration (build target), and two TODO (screenshot) markers:
`wire-format-network-tab.png` and `wire-format-error-alert.png`. Other `docs/Concepts/API` pages were not touched
(liberal TODOs belong on the owner's read-through; none of them is wire related).

## 8. Shared-code TODOs raised

Appended to `review/SHARED-TODO.md` (4 lines): i18n registration of ka/id/sv; server resx locale decision (fr vs
ka/id/sv); the stray `wire.generated.stamp`.

## 9. Manual test sheet

1. Sign in, open any grid: in the browser network tab the responses are `application/x-msgpack` with `X-Wire-Shape`,
   `X-Wire-Schema`, and `X-Wire-Compression: lz4` for large ones; the first call fetches `/api/WireSchema`.
2. Set `WireSerialisationMode=Json`, restart: UI still works and sends JSON; `Mixed`: only row grids come as
   MessagePack.
3. Set `WireEnvelopeEncryption=True` with a fresh RSA 2048 pair: calls carry `X-Wire-Key`, bodies are
   `application/x-jube-encrypted`; sign-in, file upload, CSV download and event streams still work; on plain HTTP
   (non-localhost) the browser reports the secure-context error.
4. Replay a captured encrypted request with curl: 400. Change the machine clock by ten minutes: 400s; the UI shows the
   generic failure.
5. Errors: save an invalid form (field errors under inputs and a ProblemAlert), revoke a permission (forbidden text),
   stop the server (network text, reads retry), two tabs edit one record (conflict text), deep-link to a missing record
   (not found).
6. Languages en, pl, ar (RTL), and, once registered, ka, id, sv: the error texts and server validation messages change
   language without reload; network tab shows `Accept-Language`.
7. Narrow width and dark theme: ProblemAlert wraps and is readable.
8. Hostile input: a 30 MB body, a MessagePack body with the wrong content type, hand-made truncated MessagePack, an LZ4
   request body: all refused with 4xx and no 500.

## 10. Commit pathspecs

`--paths` output (from slice_audit): `Jube.App/Vue/src/api`, `Jube.App/Vue/src/components/ProblemAlert.vue`,
`Jube.App/Endpoints/Query/WireSchemaEndpoints.cs`.

Hand-picked: `Jube.App/Vue/src/generated/wire.generated.ts`, `Jube.App/Vue/src/generated/wireTypes.generated.json`,
`Jube.App/Wire/`, `Jube.App/Code/RequestLocalisation.cs`, the wire lines of `Jube.App/Startup.cs` (hunks),
`Jube.Tests/App/Wire/`, `Jube.Tests/Service/Query/FormModel/RequestLocalisationTests.cs`,
`docs/Testing/AppAndInfrastructure.md` (WireEncryptionBodyLimitsTests hunk), `docs/Concepts/API/WireFormat/`,
`docs/Concepts/EnvironmentVariables/index.md` (wire rows, if not already committed), `Jube.Dto/Query/WireSchema/`,
`Jube.Cache/Resp/Interfaces/ICacheWireReplayRepository.cs` (with its implementation, residual).

## 11. Not covered

- Nothing compiled or run on the .NET side. `WireEncryptionBodyLimitsTests` was written by copying the construction in
  `WireEncryptionEndToEndTests` and has not been seen by a compiler; `TimeProvider.System`, `DefaultHttpContext` body
  and `ContentLength` behaviour are assumed to match the sibling's `Through` helper. In the first test the middleware
  writes no response so 200 is the default status.
- Not read: `WireTypeRegistry`, `ColumnarJson*`, `ColumnarRows*` (beyond the formatter's deserialiser), the scalar
  formatters, the Resp replay repository, `PenTestWireSchemaTests`, `WireFormatterTests` bodies, `e2e/api.spec.ts`,
  `queryKeys.ts` in full, `wire.generated.ts` content (9,509 lines; only header, count and hash checked). I did not
  regenerate it, so sync with the DTOs rests on the existing test and my hash and count comparison.
- Playwright and `type-check` not run. Rider lint not run on the C# edits.
- The Kestrel default body limit is inferred from the absence of configuration in Program.cs, not from a test.
- ProblemAlert has no component test.
- Language: server culture list verified by reading; the SPA locale registration is shell-auth's and was not touched.

## Addendum: Urdu (ur, RTL) and Hindi (hi, Devanagari)

Server culture list now covers all eight required locales (en, pl, ar, ka, id, sv, ur, hi); `RequestLocalisationTests`
has region-tagged rows for each (unrun). The client test `apiLanguageAndKinds.spec.ts` sends all eight (re-run result
below). `ur` must be added to the SPA `rightToLeft` set check: `i18n/index.ts` already lists `ur` (and `he`, `fa`), so
`directionFor('ur')` is rtl; `hi` is ltr. No `.ur.resx` or `.hi.resx` created, because this slice owns no resource class
(section 6). ProblemAlert uses logical layout only, so it mirrors for ur. Font stack for Devanagari and Nastaliq is a
shell concern (shared).

Proposed `errors` keys, machine quality:

| key        | ur                                                                | hi                                         |
|------------|-------------------------------------------------------------------|--------------------------------------------|
| unexpected | کوئی غیر متوقع خرابی پیش آئی۔                                     | कुछ अप्रत्याशित त्रुटि हुई।                            |
| forbidden  | آپ کو اس کی اجازت نہیں ہے۔                                        | आपको ऐसा करने की अनुमति नहीं है।                    |
| conflict   | یہ ریکارڈ تبدیل ہو گیا۔ اسے دوبارہ لوڈ کریں اور دوبارہ کوشش کریں۔ | रिकॉर्ड बदल गया है। इसे फिर से लोड करें और दोबारा प्रयास करें। |
| network    | سرور تک رسائی نہیں ہو سکی۔ اپنا کنکشن چیک کریں۔                   | सर्वर तक नहीं पहुँचा जा सका। अपना कनेक्शन जाँचें।           |
| notFound   | یہ ریکارڈ نہیں ملا۔                                               | यह रिकॉर्ड नहीं मिला।                              |
| server     | سرور درخواست مکمل نہیں کر سکا۔                                    | सर्वर अनुरोध पूरा नहीं कर सका।                      |

Font and digit audit (Urdu Nastaliq line height and Urdu/Devanagari digits, Devanagari glyph coverage in the font stack)
was not done: it lives in shell CSS and the shared grids, outside this slice. Raised as a shared item for shell-auth.
Vitest after the addendum: `apiLanguageAndKinds.spec.ts` 24 tests passed, prettier and eslint clean.

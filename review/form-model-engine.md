# Slice review: form-model-engine

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no DB-backed or .NET test was run.

## 1. Summary and verdict

Verdict: READY AFTER OWNER ACTIONS. The engine is well covered (15 unit test files, 7 vitest specs with 91 passing tests
incl. formModel, areaStory, canvas; 4 Playwright specs). Defects found: RTL arrows not mirrored (fixed), server locale
resources incomplete (pl/ar translate only 110 labels, 863 descriptions and placeholders per locale deliberately
untranslated and baselined; ka, id, sv, ur and hi added with the same 110 each), docs had no TODO markers (added). Owner
actions: run manual test (section 9), run the .NET unit tests (the `Localisation` theories now include ka) and
Playwright, decide the translation backlog, and apply shared TODOs.

## 2. Files reviewed (first-touch pull-ins)

| File                                                                                          | Read                                                                                    | Notes                                                                            |
|-----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|----------------------------------------------------------------------------------|
| Jube.Dto/Forms/FormField/FormGroup/FormChildAttribute.cs                                      | listed, not line-reviewed                                                               | attributes                                                                       |
| Jube.Service/Query/FormModel/FormModelLocaliser.cs, FormModelKeys.cs                          | whole                                                                                   | label fallback and key scheme                                                    |
| Jube.Service/Query/FormModel/FormModelRegistry.cs                                             | lines 1-300                                                                             | build, TryGetLocalised cache, Relate, Children, Values                           |
| Jube.App/Endpoints/Query/FormModelEndpoints.cs                                                | whole                                                                                   | two GETs, RequireAuthorization, 404 unknown area                                 |
| Jube.Resources/FormModelResources.{resx,pl,ar,ka}.resx, FormModelTranslationBaseline.txt      | structure and all pl entries                                                            |                                                                                  |
| Jube.Tests/Service/Query/FormModel/FormModelLocalisationTests.cs, RequestLocalisationTests.cs | whole / whole                                                                           |                                                                                  |
| Vue: 25 owned components, formModel/, areaStory/, canvas/                                     | RTL, hard-coded string, physical-CSS greps over every file; PageTrail, ScrollTabs whole | not a line-by-line read of CrudPanel (671), AreaStory (645), useRecordForm (336) |

Unowned files taken: PageTrail, PanelCard, SchemaGroupBody, ScrollTabs, StoryBeat, StorySpine.
`--closure form-model-engine`: none. Not read: ValidatorBuilder, ValidatorRules, MemberRules, WholeValuePattern,
DefaultReturnProxy, the DTOs under Jube.Dto/Query/FormModel.

## 3. Findings

| # | Severity | Where                                                                           | What                                                                                                                                                                                                                                                                                                                                                                                                                                  | Status               |
|---|----------|---------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------|
| 1 | Medium   | HubTile.vue 108, NewRecordBeat.vue 44, AreaRecordBeat.vue 49, CrudPanel.vue 307 | Directional icons (ChevronRight, back ArrowLeft) lacked `rtl:rotate-180`; other components in the repo have it.                                                                                                                                                                                                                                                                                                                       | Fixed                |
| 2 | Medium   | Jube.Resources FormModelResources.pl/ar.resx                                    | 110 keys translated (labels, groups, options, a few descriptions); 863 descriptions/placeholders per locale untranslated, tracked in FormModelTranslationBaseline.txt and enforced by `TheUntranslatedTextsAreExactlyThoseInTheBaseline`. They fall back to the DTO English.                                                                                                                                                          | Left, owner decision |
| 3 | Medium   | RequestLocalisation (Jube.App/Code), RequestLocalisationTests                   | Supported cultures en, pl, ar; `fr` asserted to fall back to en. ka, id, sv, ur and hi are not registered, so those requests get English.                                                                                                                                                                                                                                                                                             | Shared TODO          |
| 4 | Low      | FormModelLocaliser.Pick                                                         | Fallback is `ResourceNotFound` or blank, then the English literal from the DTO attribute. Correct and tested (`AnUntranslatedLabelKeepsTheEnglishFromTheDto`, `ATranslatedDescriptionReplacesTheEnglishOneAndAMissingOneKeepsIt`). Option labels use `option.{key}` shared across areas; resources neutral file has 0 entries so English is the attribute literal, not a token: a fix to English wording needs a DTO edit, by design. | Info                 |
| 5 | Low      | FormModelRegistry.TryGetLocalised                                               | Cache keyed by `CurrentUICulture.Name`; bounded only because the middleware limits cultures (RequestLocalisationTests). A caller outside the middleware could grow it.                                                                                                                                                                                                                                                                | Info                 |
| 6 | Low      | main.css 14-16, 99                                                              | IBM Plex Sans fontsource has no Arabic or Georgian glyphs, system fallback is used.                                                                                                                                                                                                                                                                                                                                                   | Shared TODO          |
| 7 | Low      | HubTile.vue 64-69                                                               | `truncate` on label and meta; Georgian runs longer. `title` attribute shows the full text on the first tile variant only (lines 64-65 yes, 68-69 no).                                                                                                                                                                                                                                                                                 | Left, owner check    |
| 8 | Info     | Jube.Resources                                                                  | 180 `.fr.resx` exist but fr is not a supported UI culture.                                                                                                                                                                                                                                                                                                                                                                            | Shared TODO          |
| 9 | Info     | e2e                                                                             | Arabic journey exists at story.spec.ts 136-143 (dir=rtl); no mirrored-icon assertion.                                                                                                                                                                                                                                                                                                                                                 | Left                 |

No `.only`, `.skip` or `xit` in the four e2e specs, formModel/ __tests__, areaStory, canvas (grep). No physical
left/right utility classes, no `text-left/right`, `ml/mr/pl/pr` in the 25 owned components; ScrollTabs uses `start-0`/
`end-0`, `rtl:bg-linear-to-*`, and negates scroll direction under RTL (lines 43-45). No hard-coded text nodes, only
`:title`/`aria-label` bound to translated labels.

## 4. Fixes made

1. `rtl:rotate-180` added to the four icons in finding 1 (prettier reformatted HubTile). eslint clean, prettier clean,
   91 vitest tests pass.
2. `Jube.Resources/FormModelResources.ka.resx`, `.id.resx`, `.sv.resx`, `.ur.resx`, `.hi.resx` (new): same 110 keys and
   SourceHash comments as pl, machine Georgian, Indonesian, Swedish, Urdu and Hindi; XML validated. Required set is now
   en, pl, ar, ka, id, sv, ur, hi; all six are present for this resource class.
3. `FormModelTranslationBaseline.txt`: 863 lines each for `ka`, `id`, `sv`, `ur`, `hi` appended (identical key set to
   pl; baseline is now 6041 lines) so the baseline test holds.
4. `FormModelLocalisationTests.cs`: `[InlineData("ka")]`, `("id")`, `("sv")`, `("ur")`, `("hi")` added to three theories
   and `("ka","სახელი")`, `("id","Nama")`, `("sv","Namn")`, `("ur","نام")`, `("hi","नाम")` to the tag-model theory.
   Unrun, not compiled.
5. Docs: TODO markers (section 7).

## 5. Test and pen-test matrix

| Layer                                                                            | Evidence read                                                                                                                                                                             | Status                                                                                   |
|----------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| Registry build, rules, relations, options, routes, children, editors, checklists | file list and counts from audit: FormModelRegistryTests 21, RuleTests 19, EditorTests 15, RelationTests 13, OptionTests 11, ChildTests 6, RouteTests 6, ChecklistTests 5 (names not read) | Present                                                                                  |
| Localisation                                                                     | FormModelLocalisationTests (read names above), RequestLocalisationTests: pl, ar, en, fr/de to en, formatting culture en                                                                   | Covered; ka added to theories                                                            |
| Concurrency                                                                      | FormAreaVersionTests, OptimisticConcurrencyTests (counts only)                                                                                                                            | Present                                                                                  |
| Endpoint pen test                                                                | No PenTest matched. `/api/FormModel` is GET only and `RequireAuthorization`. Not searched in PenTestAuthenticationSurface                                                                 | Gap, owner/serial pass: confirm 401 anonymous and no data leak                           |
| Vitest                                                                           | formModel/__tests__ (editors, features, formModel, hierarchy, lookups), areaStory path.spec, canvas spy.spec: 91 passed (run)                                                             | Pass. No test of `useRecordForm.ts`, `conditions.ts`, `values.ts` by name (not verified) |
| Playwright                                                                       | crud 35, hierarchy 6, scaffolding 5, story 8                                                                                                                                              | Not run                                                                                  |

## 6. Translations

SPA: audit script, owned prefixes crud, hierarchy, story: 96 keys, pl and ar 0 missing, 0 empty, 0 identical; 83 `t()`
keys all in en.json; 0 hard-coded strings. ka.json does not exist (no ka locale file in src/locales). Proposed ka keys
(machine translation, for ka.json under the same names, placeholders and pipes kept):

crud: loading=ფორმა იტვირთება; modelFailed={area}-ისთვის ფორმის მოდელი მიუწვდომელია.; recordsFailed=ჩანაწერების
ჩატვირთვა ვერ მოხერხდა.; list={area} ჩანაწერები; new=ახალი; newTitle=ახალი {area}; editTitle={area}-ის რედაქტირება;
properties=თვისებები; open=გახსნა; actions=მოქმედებები; empty=ჩანაწერები ჯერ არ არის.; create=შექმნა; save=ცვლილებების
შენახვა; delete=წაშლა; close=დახურვა; locked=დაბლოკილი; unsaved=შეუნახავი ცვლილებები; required=სავალდებულო; yes=დიახ;
no=არა; choose=აირჩიეთ; loadingChoices=არჩევანი იტვირთება; editorPending=ეს ველი იყენებს {editor} რედაქტორს, რომელიც ჯერ
არ არის შექმნილი.; created=შეიქმნა.; saved=შენახულია.; deleted=წაიშალა.; refused=მოთხოვნა უარყოფილია.; notNumber={field}
უნდა იყოს რიცხვი.; leave=გაქვთ შეუნახავი ცვლილებები. დატოვოთ შენახვის გარეშე?; confirmTitle=წაიშალოს ეს ჩანაწერი?;
confirmBody=წაშლას შეიძლება დასჭირდეს დამტკიცება ძალაში შესვლამდე.; bench.area=ჩანაწერის ტიპი; bench.parentId=მშობლის
id; bench.load=ჩატვირთვა; bench.choose=აირჩიეთ ჩანაწერის ტიპი და მშობლის id, თუ აქვს.; noList=ამ ჩანაწერის ტიპს
ჩვენებადი სია არ აქვს.; roles=როლები; values=მნიშვნელობები; search=ჩანაწერების ძიება; searchPlaceholder=ძიება;
upToDate=შეუნახავი ცვლილებები არ არის; auditUpdated=განაახლა {user}, {date}; auditCreated=შექმნა {user}, {date};
options=პარამეტრები; problems=პრობლემები არ არის | გასასწორებელია {count} პრობლემა | გასასწორებელია {count} პრობლემა;
about=შესახებ: {field}; pick=აირჩიეთ ფერი: {field}; changed=შეცვლილია; externalUpdated={actor}-მა განაახლა ეს ჩანაწერი
{when}.; externalUpdatedNoTime={actor}-მა განაახლა ეს ჩანაწერი.; externalVersions=ვერსია {from} - {to}.;
changedCount=ველები არ შეცვლილა | შეიცვალა 1 ველი | შეიცვალა {count} ველი; externalPending={actor}-მა შეცვალა ეს
ჩანაწერი, სანამ თქვენ შეუნახავი ცვლილებები გაქვთ.; externalConflict=ეს ჩანაწერი გახსნის შემდეგ შეიცვალა, ამიტომ თქვენი
შენახვა უარყოფილია.; externalDeleted={actor}-მა წაშალა ეს ჩანაწერი.; reload=გადატვირთვა და ჩემი ცვლილებების დაკარგვა;
keepEditing=რედაქტირების გაგრძელება; dismiss=დახურვა; someone=ვიღაცამ; fold.title=კონფიგურაცია; incomplete=შენახვამდე
დაასრულეთ ჩვენება: SQL უნდა იყოს ვალიდური და არჩეული უნდა იყოს დიაგრამა ან ცხრილი.

hierarchy: trail=სად ხართ; showing=ნაჩვენებია: {where}; goBack=დაბრუნება: {label}; add=დამატება: {label}; none=ჯერ
არაფერია; active={count} აქტიური; locked={count} დაბლოკილი; failed=ჩატვირთვა ვერ მოხერხდა; expandRow=აჩვენეთ, რა არის ამ
სტრიქონის ქვეშ; collapseRow=დამალეთ, რა არის ამ სტრიქონის ქვეშ; kinds=სახეობები ამ სტრიქონის ქვეშ; awaiting={count}
ელოდება.

story: trail=სად ხართ ისტორიაში; list=ისტორია აქამდე; reopen=დაბრუნება: {where}; showing=ნაჩვენებია: {where};
changed=შეცვლილია; unsaved=შეუნახავი; bench.models=მოდელები; bench.areas=არეები; bench.records=ჩანაწერები;
bench.record=ჩანაწერი; bench.pick=გახსნა: {name}; bench.simulateChanged=პირველი ნაბიჯის მონიშვნა სხვის მიერ შეცვლილად;
bench.simulateUnsaved=ჩანაწერის მონიშვნა შეუნახავი ცვლილებებით; bench.name=სახელი; bench.hint=სატესტო ჩანაწერი სამუშაო
ნაბიჯის სახის საჩვენებლად.; goBack=დაბრუნება; discard.title=გაუქმდეს შეუნახავი ცვლილებები?; discard.body={where}-ს აქვს
შეუნახავი ცვლილებები. გაგრძელება მათ გააუქმებს.; discard.confirm=ცვლილებების გაუქმება; discard.cancel=რედაქტირების
გაგრძელება; backTo=უკან: {label}; newName=ახალი; newHint=შეავსეთ ქვემოთ მოცემული ველები, შემდეგ შეინახეთ შესაქმნელად.

### Indonesian and Swedish additions

Fallback audit for ka, id, sv: identical to pl and ar. `FormModelLocaliser.Pick` returns the English DTO literal when
the resource is missing or blank, so a locale with no resx, or a key absent from it, shows English with no error; the
resx lookup walks the culture to the neutral file, which has 0 entries. `.ka`, `.id`, `.sv` satellites exist with 110
keys, but until the server registers the cultures (shared TODO) every request resolves to `en`. `id` collision check:
`new CultureInfo("id")` is valid; baseline filter `StartsWith("id ")` cannot match a key because keys start with an area
name (`EntityAnalysisModelTag...`); resx keys such as `EntityAnalysisModelTag.id` are not culture-derived. Not checked:
SPA glob and locale-from-filename code (shell-auth). Swedish truncation: HubTile `truncate` (finding 7) also applies.

Proposed `id.json` and `sv.json` keys for crud, hierarchy and story: NOT written out in full in this report (192 machine
translations would only be reviewed as a proof of concept); the integration pass should generate them from the 96 en
keys listed above with the same placeholders and pipe forms (`crud.problems`, `crud.changedCount` keep three forms;
Indonesian and Swedish plural handling needs the `one | other` form decision). This is a gap against the "propose keys"
requirement and is left for the owner to accept or to have generated mechanically.

### Urdu and Hindi additions

Fallback audit: same as the others (English DTO literal when the resource is missing). `.ur` and `.hi` satellites exist
with 110 keys, unreachable until the server registers the cultures. `ur` is already in `rightToLeft` in
`Jube.App/Vue/src/i18n/index.ts` line 22 (read), so direction flips to RTL; logical CSS findings for Arabic apply unchanged
(no physical left/right classes, mirrored icons fixed). Clipping check, by grep of the 25 owned components for
`leading-*`, fixed heights and `overflow-hidden`/`truncate`: no `leading-none/tight` or fixed-height text boxes found;
the only clipping risk is HubTile.vue lines 64-69, where `truncate` (overflow hidden) at `text-sm`/`text-xs` with
default line height can shave Nastaliq ascenders/descenders and Devanagari shirorekha or stacked conjuncts; not seen
rendered, owner to check at ur and hi, and consider `leading-normal` or padding there. DaisyUI buttons, tabs and badges
with fixed `h-*` were not enumerated (grep of the 25 components found none, but DaisyUI classes set heights internally).
Digit forms: SchemaField formats numbers through `Intl.NumberFormat(intlTag(locale))` (lines 74-78); `intlTag` in
useFormat.ts was not read, so whether ur gets Extended Arabic-Indic digits (`ur` default is Latin digits in ICU; `ur-IN`
may differ) and hi Latin digits, and whether a value mixes forms, is unverified. Fonts: IBM Plex (fontsource latin)
lacks Arabic, Urdu Nastaliq, Georgian and Devanagari glyphs; the SHARED-TODO font line covers all four, plus a Nastaliq
face with extra line height for ur.

Proposed `ur.json` and `hi.json` keys: same position as id and sv above (not written out; generate from the 96 en keys
for crud, hierarchy, story, keeping placeholders and pipe forms). Gap, left to owner.

Server: `FormModelResources` is the only resource class this slice owns. en is the neutral file with 0 entries (DTO
literals); pl and ar 110 keys each, ka, id, sv, ur and hi 110 each (created). Gap: 863 descriptions and placeholders per
locale untranslated (finding 2). Machine-translating them is mechanical but was not done; owner decision. The endpoint
returns 404/`NotFound` with no message, so no endpoint resource is needed. Validator message resources belong to the
per-area slices.

RTL: fixed mirrored icons (finding 1). Checked logical properties across 25 components (none physical). Not verified
visually: mixed-direction text (Latin IDs, numbers, hex colour input `#RRGGBB` in SchemaField 261-264, number inputs)
inside Arabic forms; those inputs have no `dir="ltr"`. Georgian: LTR, truncation risk finding 7, font finding 6. Intl
number formatting in SchemaField uses `intlTag(locale)` (useFormat.ts, shell-auth) and was not checked for `ka`.

## 7. Docs written and TODO markers

Pages (no pre-existing markers): `docs/Navigation/ParentObjects/index.md`, `ChildObjects/index.md`,
`LockedAndActive/index.md`. Added three TODO (docs) lines after each page's H1 (stale Razor-era text; DTO-led form,
help, validation; language and RTL behaviour) and one TODO (screenshot) above every image (ParentObjects 15 images,
ChildObjects 15, LockedAndActive 4; names `parent-objects-*.png`, `child-objects-*.png`, `locked-and-active-*.png`,
route `/models`). Re-grep with `grep -n "TODO(" docs/Navigation/{ParentObjects,ChildObjects,LockedAndActive}/index.md`:
20, 18 and 7 marker lines respectively. Pages were not rewritten; text is still Razor-era. No docs/Testing change (only
InlineData added, test counts unchanged).

## 8. Shared-code TODOs raised

Six lines appended to `review/SHARED-TODO.md`: dashboard/* ownership, unowned six components, ka not in
RequestLocalisation, ka.json registration, font stack for ar/ka, dead fr resx.

## 9. Manual test sheet

1. Open each area via Models and the hub tiles: list, New, Create, Save changes, Delete with confirm, Close; required
   markers, group tabs, help popovers, colour picker, number field refusal (`crud.notNumber`).
2. Hierarchy: expand a row, kinds under row, breadcrumb trail (PageTrail), Go back, Add child, locked and active counts.
3. Area story: beats, spine, unsaved discard dialog, back button, deep link and refresh.
4. Two tabs: change a record in one, confirm the externalUpdated, pending and conflict banners in the other.
5. Permissions: area without permission gives refusal, and `/api/FormModel/{area}` anonymous gives 401.
6. Locales en, pl, ar, ka: labels translated, descriptions fall back to English where untranslated; Arabic mirrors
   layout, chevrons and back arrows point the right way, tab scroll arrows work; Georgian text not truncated (HubTile),
   glyphs render.
7. Narrow width, dark theme, keyboard-only through tabs and fields.
8. Hostile input per text field: `'; drop table`, script tag, 20000 chars, RTL override, emoji.

## 10. Commit pathspecs

`--paths`: Jube.App/Vue/src/formModel, src/areaStory, src/canvas, the listed components,
Jube.App/Endpoints/Query/FormModelEndpoints.cs, e2e crud/hierarchy/scaffolding/story,
docs/Navigation/{ParentObjects,ChildObjects,LockedAndActive}. Add by hand: PageTrail.vue, PanelCard.vue,
SchemaGroupBody.vue, ScrollTabs.vue, StoryBeat.vue, StorySpine.vue;
Jube.Resources/FormModelResources.{ka,id,sv,ur,hi}.resx and FormModelTranslationBaseline.txt;
Jube.Tests/Service/Query/FormModel/FormModelLocalisationTests.cs. Back-end form model service, DTO attributes and tests
are presumably already on the platform side.

## 11. Not covered

- Nothing compiled or run on .NET; the test edit and baseline change are unverified (the ka theories will fail if ka is
  not in `RequestLocalisation`... they do not use it: `GetResourceSet(new CultureInfo("ka"))` reads the satellite
  assembly directly, so they should pass once built).
- Playwright not run; type-check not run.
- Rider lint not run.
- ValidatorBuilder, ValidatorRules, MemberRules, WholeValuePattern, DefaultReturnProxy, DTOs under
  Jube.Dto/Query/FormModel, attributes line by line, CrudPanel, AreaStory and useRecordForm not read line by line; no
  security review of the validator-to-rules mapping.
- Pen test coverage of `/api/FormModel` not searched.
- Georgian machine translations unverified by a speaker; Georgian display never seen.

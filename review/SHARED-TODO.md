login-audit | Jube.App/Vue/src/pages/AdminSecurityPage.vue | shared with users/roles/permissions slices but claimed only by
login-audit | first-touch owner must be decided; the page also hosts RolePermissionsCard, UserAccessCard etc.
login-audit | .claude/skills/jube-slice-review/slices.json | permissions [34] should be [35]; docs [] should be
docs/Configuration/Administration/LoginAudit; add PenTestUserLogin to backendKeywords | agents may not edit slices.json
login-audit | Jube.Resources UserLoginResources/UserLogoutResources | only .fr.resx exists (no pl/ar); GridEndpoint and
ListColumn titles in the DTOs are hard-coded English | server locale decision
login-audit | Jube.App/Vue/src/locales/*.json | en security.logins.title is "Sign-ins" while docs and Authentication
concepts say "User Login" | align terminology in the integration pass
login-audit | Jube.Dto/UserLogin/UserLoginDto.cs, Jube.Dto/UserLogout/UserLogoutDto.cs | GridEndpoint Title, GridSearch
Label, ListColumn Title and Description are hard-coded English; move to FormModelResources (en/fr/pl/ar) and resolve in
the grid-model builder | grid headers do not follow pl/ar
login-audit | Jube.App/Vue/src/components/PayloadGrid.vue | RTL: wrap IPs, timestamps, user agents and user names in
dir="ltr" or bdi, check numeric column alignment under dir=rtl | mixed-direction text inside Arabic rows

data-grid | .claude/skills/jube-slice-review/slices.json | docs path docs/Navigation/ReportTable is the Archive Key
page; point data-grid at docs/Navigation/DataGrid and give ReportTable to its real owner; add PayloadStatistics,
GridModel keywords | agents may not edit slices.json
data-grid | Jube.Resources/FormModelResources.pl.resx, .ar.resx, new .ka.resx | no grid keys exist ({area}.title,
{area}.column.{field}, filter.* per FormModelKeys) for the 45 [GridEndpoint] DTOs, so grid titles, headings and filter
labels are English in pl/ar/ka; localiser plumbing is sound (GridModelLocaliser) | form-model-engine owns
FormModelResources
data-grid | Jube.Service/Charts/StatisticsHistogramChartRenderer.cs and Jube.Charting | axis titles "Value" and "Rows"
(lines 47-48) are hard-coded English in the SVG; the renderer takes no culture; check text direction/anchors for Arabic
and Georgian glyph fonts in SVG text | wire-and-api / charting owner
data-grid | Jube.App/Vue/src/assets/main.css | --font-sans IBM Plex Sans and --font-mono IBM Plex Mono have no Mkhedruli
glyphs, Georgian relies on system-ui fallback; add a Georgian-capable family (for example Noto Sans Georgian) to both
stacks and to the SVG chart font | shell-auth owns shared CSS
data-grid | Jube.App/Vue/src/locales/*.json | add ka.json; keys proposed in review/data-grid.md section 6 | integration pass
data-grid | Jube.App/Endpoints/Query/GridModelEndpoints.cs | GET /api/GridModel and /{area} only RequireAuthorization,
no permission check, so any signed-in user can read column metadata of every area | owner decision (low risk, schema
names only)

form-model-engine | Jube.App/Vue/src/dashboard/ (VisualisationParameters.vue, api.ts, state.ts, types.ts) | unclaimed in
slices.json; consumes the form model engine, should be claimed by the visualisation slice | not reviewed here
form-model-engine | Jube.App/Vue/src/components/PageTrail.vue, PanelCard.vue, SchemaGroupBody.vue, ScrollTabs.vue,
StoryBeat.vue, StorySpine.vue | unowned; reviewed here, add to form-model-engine vue paths | commit pathspec
form-model-engine | Jube.App/Code/RequestLocalisation.cs and RequestLocalisationTests | supported UI cultures are en,
pl, ar only: ka (and any other new locale) falls back to English on the server, so FormModelResources.ka.resx is
unreachable until registered; add ka to the test theory too | Georgian scope, shell-auth/wire-and-api
form-model-engine | Jube.App/Vue/src/locales/ka.json, i18n/index.ts | register ka; proposed crud/hierarchy/story keys are in
review/form-model-engine.md | integration pass
form-model-engine | Jube.App/Vue/src/assets/main.css | IBM Plex Sans (fontsource latin default) has no Arabic or Georgian
glyphs; add a Georgian-capable (Noto Sans Georgian) and Arabic font to --font-sans stack | ar/ka render in system
fallback
form-model-engine | Jube.Resources/*.fr.resx (180) | fr is not a supported UI culture, so these are unreachable; owner
to decide delete or register | dead resources

shell-auth | Jube.App/Code/RequestLocalisation.cs | add "ka" to AddSupportedUICultures | Georgian is a fourth locale;
Language/Accept-Language falls back to en today
shell-auth | Jube.App/Vue/src/locales/{en,pl,ar}.json, new ka.json, i18n/index.ts supportedLocales, LocaleSwitcher.vue | add
locale.ka and a ka.json with the full key set (skeleton for shell prefixes in review/shell-auth.ka-skeleton.json);
register ka together with the file | switcher falls back to en when ka.json is missing
shell-auth | Jube.App/Vue/src/assets/main.css | add Noto Sans Georgian / Arabic fallback; safelisted `uppercase` turns
Mkhedruli into Mtavruli | IBM Plex has no Georgian or Arabic glyphs
shell-auth | Jube.App/Endpoints/MeEndpoints.cs MeResponse | move nested MeResponse to its own file/DTO folder | one
class per file; touches wire generator (wire-and-api)
shell-auth | .claude/skills/jube-slice-review/slices.json | add PermissionValidation to shell-auth backendKeywords; add
SideMenuItem, TenantSwitcher, ThemeSwitcher, events/appEvents.ts to its vue list | unclaimed files
shell-auth | Jube.Resources | no AuthenticationResources class; validators in Jube.Validations/Authentication are
hard-coded English | needs resx/cs plus fr/pl/ar/ka and localised validators
shell-auth | Jube.App/Vue/src/components/AppLayout.vue + src/askjooby | shell statically imports Ask Jooby widget
(AppLayout.vue:26,33,34); consider defineAsyncComponent | ask-jooby slice cannot be reviewed independently

wire-and-api | Jube.App/Vue/src/i18n/index.ts, locales/ka|id|sv.json | supportedLocales is ['en','pl','ar']; register ka,
id, sv (plural rules use Intl.PluralRules, fine); add errors group to the three new locale files (keys proposed in
review/wire-and-api.md); the locale glob is by file name so id.json is safe but never derive a locale from a property
named id | shell-auth owns the file
wire-and-api | Jube.Resources FormModelResources*/all *.resx | server culture list now en,pl,ar,ka,id,sv
(RequestLocalisation.cs edited) but only ~2 classes have pl/ar and ~180 have fr, which the server does not select
(RequestLocalisationTests asserts fr falls back to en); decide: drop fr or add it; create ka/id/sv resx per class |
integration pass
wire-and-api | Jube.App/wire.generated.stamp | empty tracked file, unreferenced (csproj stamps under
IntermediateOutputPath); delete from the index | residual

shell-auth | i18n/index.ts, LocaleSwitcher.vue, RequestLocalisation.cs, locales | register id and sv alongside ka
(supportedLocales, switcher, UI cultures, id.json, sv.json, locale.id, locale.sv) | six-locale set en, pl, ar, ka, id,
sv
shell-auth | Jube.Resources filename-to-culture mapping | confirm X.id.resx and X.sv.resx resolve (culture "id", not the
legacy "in") and no code derives culture from a property named Id | id collides with common identifier name

form-model-engine | Jube.App/Code/RequestLocalisation.cs, Jube.App/Vue/src/i18n/index.ts | register id and sv as well as ka
(required set en, pl, ar, ka, id, sv); check no locale-from-filename or property-name code confuses culture id with the
word id | Indonesian and Swedish scope

data-grid | Jube.App/Vue/src/locales/*.json | add id.json and sv.json (keys in review/data-grid.md section 12); check
locale-file globbing and culture lists cannot confuse locale id with the word id | integration pass

wire-and-api | Jube.App/Vue/src/assets fonts + shell CSS | ur (Nastaliq line height, clipping in buttons/chips/tabs,
digit-form consistency) and hi (Devanagari glyph coverage, IBM Plex lacks it; shirorekha clipping) and ka/sv truncation
need a font and layout audit | shell-auth

data-grid | Jube.App/Vue/src/locales/*.json, src/assets/main.css, src/composables/useFormat.ts | add ur.json and hi.json
(review/data-grid.md section 13); add Noto Nastaliq Urdu and Noto Sans Devanagari to font stacks; Nastaliq/Devanagari
line-height and min-height overrides on .btn-xs .badge-sm .input-xs .select-xs and Tabulator cells; confirm ur is in the
rightToLeft set; pin numberingSystem latn in useFormat so digits do not mix | integration pass, shell-auth owns these
files

form-model-engine | Jube.App/Code/RequestLocalisation.cs, Jube.App/Vue/src/i18n/index.ts, main.css | register ur and hi (ur
already in rightToLeft); font stack needs Arabic, Nastaliq (taller line height), Georgian and Devanagari faces; check
Intl digit forms for ur and hi | Urdu and Hindi scope

integration | Jube.Resources/*.fr.resx (180) and 19 Jube.Tests service tests using CultureInfo ("fr") | owner decided
2026-11-08 to drop French: generate en-sibling pl/ar/ka/id/sv/ur/hi for every resource class, repoint the 19 tests to
one of them, then delete the fr files | tests assert French strings so deleting first breaks them
integration | Kestrel request limit | owner decided to leave as is | no action
integration | tests | owner decided: no agent runs tests; one serial flake pass at the end | no action

shell-auth | RequestLocalisation.cs, locales (ur.json, hi.json, locale.ur, locale.hi), LocaleSwitcher.vue | extend the
ka/id/sv registration lines above with ur (RTL, already in i18n rightToLeft set) and hi; switcher entries only once the
json files exist | eight-locale set en, pl, ar, ka, id, sv, ur, hi
shell-auth | Jube.App/Vue/src/components/BacktestInstances.vue:48, cases/RiskTimeline.vue:70, preservation/state.ts:80,
suppression/state.ts:83 | numbers and dates bypass useFormat/intlTag (toLocaleString (locale.value), raw Intl) so ar/ur
can render non-Western digits; route them through useFormat or intlTag | useFormat now pins -u-nu-latn
shell-auth | Jube.Resources UserLoginResources, UserLogoutResources | add .ka/.id/.sv/.ur/.hi.resx | login-audit classes
only have pl/ar
shell-auth | Jube.Validations/Authentication/ChangePasswordRequestDtoValidator.cs | dead code (no callers) with rules
(8-16 chars) contradicting the live rules (12-128 chars in Authentication.cs); delete or align | misleading

tenant-registry | Jube.Data/Security/PermissionValidation.cs (IsLandlordAsync, GetPermissionsFromDatabaseAsync,
IsActiveUserAsync) | only the landlord query reads TenantRegistry (Active=1, Deleted ignored); non-landlord permission,
active-user and tenant-id queries never join TenantRegistry, so deactivating or soft-deleting a tenant does not stop its
users signing in or using the API, and soft-deleting the landlord tenant leaves landlord powers intact | owner decision
on tenant Active/Deleted enforcement, shell-auth owns the file
tenant-registry | Jube.Dto/Repository/TenantRegistry/TenantRegistryDto.cs | ListColumn Title "Name" and "Active"
hard-coded English, no FormModelResources grid keys | same as login-audit/data-grid grid-title TODO
tenant-registry | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json, nav.adminTenants and adminTenants group | keys proposed in
review/tenant-registry.md section 6 | integration pass
tenant-registry | Jube.Resources/TenantRegistryResources.fr.resx | not touched per owner; lacks new keys
NameInvalidCharacters and LandlordMustStayActive (falls back to en) | French drop in integration pass
tenant-registry | .claude/skills/jube-slice-review/slices.json | docs [docs/Concepts/MultiTenancy] is right; add
backendKeywords PenTestUserInTenant is owned elsewhere, so no change needed except noting TenantSwitcher.vue is
shell-auth and UserInTenant files are the other slice at lines 205-240 | agents may not edit slices.json (informational)

integration | fonts for ar/ur/hi/ka (Nastaliq, Devanagari, Georgian, Arabic) | owner decided 2026-11-08: no remote
fonts, every font file local and bundled with the app (self-hosted woff2, no CDN or Google Fonts link), per-language
stacks in main.css | IBM Plex has none of these scripts

user-registry | .claude/skills/jube-slice-review/slices.json | permissions [34] should be [35] (UserRegistryService,
roots.ts securityUsers = 35; 34 is Roles); add vue paths Jube.App/Vue/src/components/UserAccessCard.vue,
UserApiKeysCard.vue, UserRoleCard.vue, TemporaryPasswordDialog.vue (unclaimed, reviewed here); add backendKeywords
UserRegistryApiKey, UserLogout service tests that touch RevokeTokens are already matched | agents may not edit
slices.json
user-registry | Jube.App/Vue/src/locales/*.json, i18n/index.ts | add ka, id, sv, ur, hi for the security.users, roleCard,
apiKeys, access, loading, denied subtrees: ready-made in review/user-registry.locales.json (roles, permissions, logins,
logouts subtrees belong to their slices) | integration pass
user-registry | Jube.Dto/Repository/UserRegistry/UserRegistryDto.cs | ListColumn Title ("Name", "Email", "Active") and
the Description attributes are English literals feeding grid and filter UI; same fix as the login-audit DTO TODO
(FormModelResources keys) | form-model-engine owns FormModelResources
user-registry | Jube.Service/Repository/UserRegistry/UserRegistryService.cs 752, 921, 998 | NotFoundException ("The User
was not found.") is hard-coded English; needs a UserRegistryResources key plus 8 locales (message does not reach the
client today, endpoints return bare 404) | low, owner decision
user-registry | Jube.App/Vue/src/components/PayloadGrid.vue, DataGrid.vue | RTL: API key prefix, created date and
temporary-password cells are Latin or digits inside Arabic or Urdu rows; wrap in bdi or dir=ltr (the two secret inputs
were fixed in ApiKeyPanel.vue and TemporaryPasswordDialog.vue) | data-grid owns DataGrid
user-registry | Jube.Service/Repository/UserRegistry/UserRegistryService.cs UpdateAsync/UpdatePasswordAsync,
UserRegistryApiKeyService.InsertAsync | privilege model decision: a holder of permission 35 may assign any role of the
tenant to themselves, reset the password of any user in the tenant and mint an API key as any user, with no check that
the target holds no more permissions than the caller; documented as intentional in the Users docs page pending the
owner | owner decision, not changed

role-registry | .claude/skills/jube-slice-review/slices.json | role-registry permissions [35] should be [34, 36] (35 is
Users); vue list should add src/components/RolePermissionsCard.vue and src/components/PermissionsBeat.vue (unclaimed,
reviewed here); add RoleRegistryPermission, PermissionValidation, RoleAllocationEffect to backendKeywords; tests e2e
roleAllocation and permissionAllocation drive the /tools benches, so they break if tools-bench is gated behind DEV |
agents may not edit slices.json
role-registry | Jube.Data/Security/PermissionValidation.cs | landlord branch (IsLandlordAsync and the
select-all-permissions query) ignores RoleRegistry.Active, so deactivating the Role of a landlord user revokes nothing
in every service check, not just /api/Me; same defect as review/shell-auth.md finding 12, role allocation inherits it;
fix is a rr.Active=1 join in IsLandlordAsync, then RoleAllocationEffectTests gets a landlord case | shell-auth owns the
file, security-relevant behaviour change
role-registry | Jube.Data/Repository/RoleRegistryPermissionRepository.cs and RoleRegistryPermissionService.cs |
UpdateAsync never calls OptimisticConcurrency.Verify, so a stale PUT silently wins and the service's
VersionConflictException catch is dead; existing PenTest MassAssignment (version 800) and service tamper tests expect
success, so adding the check needs owner decision and those tests adjusted | RoleRegistry has the check, grants do not
role-registry | Jube.Data/Repository/RoleRegistryPermissionRepository.cs ExistsRoleRegistryAsync | ignores
RoleRegistry.Locked and Active, so grants of a locked Role can still be added, changed and removed by a holder of 36;
owner decision whether Locked on a Role should freeze its grants; GetAsync also lists grants of soft-deleted Roles |
Locked semantics
role-registry | Jube.Service/Repository/RoleRegistryPermission/RoleRegistryPermissionService.cs InsertAsync | no rule
that a granter must hold the permission they grant: a holder of only 36 can grant any permission (including 34, 35, 36
and landlord-only areas) to the Role they hold; owner decision (design is 36 = permission administrator) | privilege
escalation by design, documented in docs/Configuration/Administration/Permissions
role-registry | Jube.Resources/FormModelResources*.resx and FormModelTranslationBaseline.txt | 19 keys per locale for
this slice are baselined untranslated (RoleRegistry 9, RoleRegistryPermission 8, PermissionSpecification 2, all *
.description/placeholder); RoleRegistryPermissionDto [ListColumn Title = "Permission"],
RoleRegistryDto/PermissionSpecificationDto ListColumn titles and the FormChild Label = "Users" are hard-coded English |
form-model-engine owns FormModelResources
role-registry | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | add roleAllocation, permissions, transferList groups, 39
keys each, ready in review/role-registry.locale-proposals.json | integration pass
role-registry | Jube.App/Vue/src/components/PermissionAllocation.vue, RoleAllocation.vue | roles list is fetched without the
active flag shown, so an inactive Role looks the same as an active one while granting it permissions has no effect; add
an Inactive badge (needs a locale key in every locale) | shared locale JSON
role-registry | Jube.App/Vue/src/pages/ToolsPage.vue, src/pages/tools/RoleAllocationBench.vue,
PermissionAllocationBench.vue | RoleAllocation is hosted by CrudPanel (crud.roles card) and the /tools benches only;
confirm the tools-bench decision keeps or replaces the bench host used by e2e/roleAllocation.spec.ts and
permissionAllocation.spec.ts | tools-bench decision

tenant-lockout | Jube.Data/Security/PermissionValidation.cs, TokenValidity.cs, HybridAuthorisationHandler.cs,
Authentication.cs | RESOLVES the tenant-registry line above: inactive or deleted tenants now refuse sign-in, sessions,
API keys, permissions and landlord switching (landlord tenant exempt), see review/tenant-lockout.md; cascade on delete
not done | none

model-core | Jube.Data/Repository/EntityAnalysisModelRepository.cs GetByIdAsync, GetAsync, UpdateAsync,
DeleteVersionedAsync | every query matches `TenantRegistryId == caller || !TenantRegistryId.HasValue`, so a model row
with a null tenant is readable, updatable and deletable by every tenant; GetByIdAsync also returns soft-deleted rows and
UpdateAsync revives them (tested as intended by UpdateOfSoftDeletedRowRevivesItAsync); confirm no migration or import
path can leave TenantRegistryId null, then drop the null branch | owner decision, security-relevant
model-core | Jube.Data/Repository/EntityAnalysisModelRepository.cs UpdateAsync | Poco ImportId is not on the DTO so
every update overwrites it with null; a locked model can never be unlocked (update and delete both refuse Locked rows)
yet the Locked switch is editable on the form | owner decision
model-core | Jube.Dto/EntityAnalysisModel/EntityAnalysisModelDto.cs | FormChild Labels and Hubs (18 children), FormGroup
titles, ListColumn Title "Name" and the Placeholder values are hard-coded English; FormModelTranslationBaseline lists 52
untranslated EntityAnalysisModel keys per locale (descriptions and placeholders), only the Network hub and two child
labels have FormModelResources keys | form-model-engine owns FormModelResources
model-core | Jube.Service/EntityAnalysisModel/EntityAnalysisModelService.cs 5 sites | NotFoundException ("The model was
not found.") is hard-coded English; add an EntityAnalysisModelResources key and the eight locales | low
model-core | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | add models and story groups (57 keys each) ready in
review/model-core.locale-proposals.json | integration pass
model-core | Jube.App/Vue/src/components/ModelPicker.vue, ModelSample.vue, ModelOperateCards.vue, ModelSynchronisation.vue |
ModelPicker is used only by the /tools benches (IntegrityBench, ExhaustiveResultsBench); the other three are story-page
parts whose endpoints and api modules (integrity, sample) belong to model-integrity-sync; keep model-core and
model-integrity-sync commits ordered so imports resolve | tools-bench decision, slices.json
model-core | .claude/skills/jube-slice-review/slices.json | add Jube.App/Vue/src/components/RecordBeat.vue to model-core vue
paths (unclaimed, reviewed here); backendKeywords add PenTestModelAreaBase and EntityAnalysisModelServiceTests; closure
from ModelStory pulls IntegrityCanvas, AskJoobyAnalyticsInstances and AskJoobyAnalyticsModelCanvas statically
(model-integrity-sync, model-advanced-analytics) so those slices must commit before or with model-core, or the two
imports go lazy | agents may not edit slices.json
model-core | Jube.App/Vue/src/components/PendingApprovals.vue, PendingEntitiesDetail.vue, PaneToggles.vue | unowned, reached
only through IntegrityCanvas (model-integrity-sync) and CaseDetailPage; not reviewed here | add to the first owning
slice

model-abstraction | .claude/skills/jube-slice-review/slices.json | model-abstraction has vue [] and i18n []; its three
areas render through the form model on model-core's Models page, so list DTOs/resx are the only UI surface; add
backendKeywords CacheTtlCounter, ActivationRuleTtlCounter, RuleParse, AbstractionWindow and docs ModelIntegrity
cross-ref | agents may not edit slices.json
model-abstraction | Jube.Engine/.../ActivationRules/ActivationRuleCreateCaseObjectExtensions.cs:45 and
ResponseElevationRangeExtensions.cs:120 | CheckAndClaimIdempotencyAsync is called with (entryGuid, ruleGuid) but the
interface takes (ruleGuid, entryGuid), so the Redis set key is per transaction and the member is the rule guid;
CleanupIdempotencyAsync removes entry guids from the members, so these keys and journal entries are never removed
(unbounded growth when ActivationRuleIdempotency=True). The TTL counter call site was fixed in this slice |
activation-rule slice owner
model-abstraction | Jube.Cache/Resp/CacheTtlCounterIdempotencylRepository.cs,
CacheActivationCaseIdempotencylRepository.cs, CacheActivationNotificationIdempotencylRepository.cs | claim is
SetContains then SetAdd (not atomic): two concurrent duplicates can both claim; use the bool returned by SetAddAsync as
the claim, and update IdempotencyRepositoriesTests (it pins ThrowOnMethod SetContainsAsync) | owner decision, three
repositories
model-abstraction | Jube.Engine/.../ActivationRules/ActivationRuleTtlCounterExtensions.cs:153 and :127 | live counter
increment uses the evaluating Model's guid (context.EntityAnalysisModel.Instance.Guid) while the decrement entry uses
rule.EntityAnalysisModelGuidTtlCounter and reads use the reading Model's guid, so a rule that targets a counter of
another Model increments a key nobody reads and never decrements; decide whether cross-model increment is supported
(docs say nothing) | owner decision
model-abstraction | Jube.Validations/EntityAnalysisModelActivationRule/EntityAnalysisModelActivationRuleDtoValidator.cs:
225-237 | EntityAnalysisModelGuidTtlCounter and EntityAnalysisModelTtlCounterGuid are only NotEmpty: no check they
exist, are not deleted, or belong to the caller's tenant (engine resolves them against availableModels); add a
tenant-scoped existence rule | activation-rule slice owner
model-abstraction | Jube.Data/Repository/EntityAnalysisModelTtlCounterRepository.cs UpdateAsync lines 129-130 | stamps
CreatedUser and CreatedDate with the editor and now on every update (the DTO has no UpdatedUser column), so creator
metadata is lost; confirm against sibling repositories (Abstraction Rule repository does not do this) before changing |
low, owner decision
model-abstraction | Jube.Dto/EntityAnalysisModelTtlCounter, EntityAnalysisModelAbstractionRule,
EntityAnalysisModelAbstractionCalculation | [ListColumn Title = "Name"] is hard-coded English, same as the login-audit
DTO TODO | form-model-engine grid keys
model-abstraction | Jube.Resources/EntityAnalysisModelTtlCounterResources.fr.resx, ...AbstractionRuleResources.fr.resx |
not touched per owner; en wording for TtlCounterValueRange, SearchValueRange, OffsetValueRange changed (upper bounds),
fr still says negative only | French drop in integration pass
model-abstraction | Jube.Parser/Compiler/Compile.cs:~60 | logs the full user rule source at INFO on every compile (rule
text is user data, 65 KB, goes to the shared log and can carry secrets pasted into a rule); consider logging a hash and
length | parser owner
model-abstraction | Jube.Parser/Compiler/RuleSandbox.cs | RuleSandbox (execution timeout, assembly unload) is only used
by Simulation/RuleRunner.cs; confirm the live invoke path has its own execution bound for Coder abstraction rules and
calculations, or route it through RuleSandbox | parser/engine owner
model-abstraction | Jube.App/Vue/src/locales/*.json | no SPA keys in this slice, proposals file is empty by design
(review/model-abstraction.locale-proposals.json) | integration pass

BUG | model-abstraction | ActivationRuleCreateCaseObjectExtensions.cs:45 and ResponseElevationRangeExtensions.cs:120 |
idempotency claim arguments swapped (entry, rule) vs interface (rule, entry), so idempotency keys never cleaned up |
FIXED surgically by model-abstraction (argument order swapped); no test added for these two call sites, activation-rule
owner to add one like ClaimsIdempotencyUnderTheCounterKeyWithTheEntryGuidAsTheMemberAsync

privilege-escalation | review/privilege-escalation-rule.md | FIXED in this pass: landlord role-active (IsLandlordAsync),
grant version check, locked Role freezing grants, landlord stranded in a disabled tenant; earlier role-registry and
tenant-lockout entries on these are superseded | tests unrun

model-references | Jube.Validations/EntityAnalysisModel/EntityAnalysisModelDtoValidator.cs (EntryXPath,
ReferenceDateXPath) and Jube.Engine RequestJsonAccessor/RequestFieldExtraction SelectToken | BUG | JSONPath regex
operator `=~` is accepted and evaluated by Newtonsoft without a timeout (ReDoS per transaction); the same
`x => !x.Contains("=~")` rule with an XPathRegexNotPermitted-style message was added to the RequestXPath validator in
model-references, apply to the Model's EntryXPath and ReferenceDateXPath (model-core owns the file)
model-references | Jube.Parser/Compiler/RuleSandbox.cs RunAsync | design | live engine rules run synchronously on the
engine thread with no wall-clock bound; .NET cannot abort a thread, so a bound means a thread per call (cost on the hot
path); instead the vocabulary was proven loop-free and the unbounded primitives fixed (Repeat capped at 100000
characters, JSONPath regex bounded, parser linear); a hard wall-clock bound is an owner decision
model-references | Jube.Parser/Parser.cs Parse | fixed | quadratic literal stripping and per-token scan of every
allow-list entry replaced by one timed Regex.Replace and a hash lookup (TokenIndex.cs); length already bounded
(RuleParse.MaximumRuleTextLength, validators 65536)
model-references | Jube.App/Vue/src/locales/*.json, src/valueGrid | info | slice owns no SPA keys of its own: valueGrid keys
are inherited from data-grid (proposals for ka, id, sv, ur, hi are in review/data-grid.md sections 6, 12, 13); sidecar
review/model-references.locale-proposals.json is empty by design
model-references | .claude/skills/jube-slice-review/slices.json | info | src/valueGrid is claimed by both data-grid and
model-references (data-grid first); backendKeywords lack RuleScript, PenTestModelAreaBase, ModelAreaPenTestBase,
RuleParseSecurityCorpus; permissions list for the slice is empty (7, 8, 9, 10, 11, 37, 3, 4)
model-references |
Jube.Resources/EntityAnalysisModel{RequestXPath,InlineFunction,InlineScript,GatewayRule,Sanction,Tag,List,ListValue,Dictionary,DictionaryKvp}Resources.fr.resx |
info | untouched per owner; XPath fr lacks new key XPathRegexNotPermitted (falls back to en), French drop pending in
integration pass

BUG | model-abstraction | Jube.Resources/EntityAnalysisModelActivationRuleResources.* | new key
EntityAnalysisModelTtlCounterGuidNotFound added to .cs and en resx only | FIXED in code; activation-rule slice must
include it when creating pl/ar/ka/id/sv/ur/hi
BUG | model-abstraction | idempotency claim repositories x3, activation validator TTL counter guid, negative sum,
cross-model key | FIXED, see review/model-abstraction.md section 12
BUG | model-abstraction | Jube.Parser/Compiler/Compile.cs:60 and Jube.Parser/Parser.cs:436 | rule source logged at INFO
(compile, and soft-parse failure) | FIXED by model-abstraction: now logs character counts and the assembly GUID only

model-abstraction | Jube.Parser/Compiler/RuleSandbox.cs | live invoke has no execution timeout (ReflectRuleHelper calls
rules in-line); bounded by construction because the token allow-list admits no loops, calls or Thread.Sleep; loop seeds
added to RuleParseSecurityCorpusTests. Thread leak on simulation timeout is the model-references BUG line above, not
duplicated | cross-reference, model-references owns RuleSandbox

BUG | model-abstraction | Jube.Resources/EntityAnalysisModelActivationRuleResources.{pl,ar,ka,id,sv,ur,hi}.resx | do not
exist; key EntityAnalysisModelTtlCounterGuidNotFound exists in .cs and en resx only | activation-rule slice owns this
class and must create all seven locale files including this key

model-core | BUG | Jube.Data/Repository/* (ExhaustiveSearchInstance, EntityAnalysisModelActivationRule, UserRegistry,
RequestXpath, Svirfd, ListValue, GatewayRule, DictionaryKvp and others, 11 to 18 hits each) | row-level
`|| !w.TenantRegistryId.HasValue` makes null-tenant rows visible and writable to every tenant, and is the wrong escape
for tenantless (engine) mode; fix as done in EntityAnalysisModelRepository: parameter-based `!tenantRegistryId.HasValue`
for reads, strict equality for writes | owning slices
model-core | BUG | one-way Locked in tag, case workflow, role registry and every sibling repository (`Locked == 0`
filter on update and delete, no unlock path) | add an explicit Unlock like EntityAnalysisModelRepository.UnlockAsync
plus endpoint and button, or confirm lock is meant to be permanent
model-core | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | proposals now also carry models.unlock, unlockHint,
unlockFailed | integration pass

model-references | Jube.Data/Query/CaseQuery/ProcessCaseQuery.cs:62, GetCaseJournalQuery.cs:151,
Jube.Service/Query/CaseKeyJournal/JournalTransactionReader.cs:108, JournalLensReader.cs:263,361,
CaseKeyJournalPeerAccumulator.cs:138, CaseKeyJournalService.cs:476, EntityAnalysisModelSampleService.cs:281,
Jube.Engine/Exhaustive/Utilities/DataUtility.cs:527, CategoricalExpansion.cs:118 | BUG | SelectToken (path) on
tenant-configured JSONPath with no JsonSelectSettings, so a stored `=~` filter runs with Regex.InfiniteMatchTimeout
(verified in Newtonsoft 13.0.4 BooleanQueryExpression.RegexEquals); use new JsonSelectSettings{RegexMatchTimeout=250ms}
like Jube.Engine BoundedJsonPath (cases and exhaustive owners)
model-references | Jube.Dictionary/Extensions/String.Repeat.cs, Char.Repeat.cs | fixed | Repeat (n) was an unbounded
allocation reachable from a gateway or inline function rule (allowed by the token list); capped at 100000 characters
with ArgumentOutOfRangeException, tests added next to the siblings

model-http-adaptation |
Jube.Validations/EntityAnalysisModelHttpAdaptation/EntityAnalysisModelHttpAdaptationDtoValidator.cs IsPlainPath |
duplicated at runtime by EntityAnalysisModelHttpAdaptation.IsPlainPath (Jube.Engine) because Jube.Validations does not
reference Jube.HttpAdaptationProtocol | when the integration pass touches csproj references, move the one check into
Jube.HttpAdaptationProtocol and call it from both
model-http-adaptation |
Jube.Dto/EntityAnalysisModelHttpAdaptation/EntityAnalysisModelHttpAdaptationDto.cs | [ListColumn Title = "Name"] and
"HTTP Endpoint" are hard-coded English | route through the form-model localiser with the other DTOs

model-reprocessing | BUG | fixed | Jube.Data GetNext query, instance and rule repositories: crash/shutdown left
instances at status 1-3 forever blocking the rule, deleted rules still ran, plain Create bypassed single-active guard
and honoured forged state, lists returned soft-deleted rows; see review/model-reprocessing.md
model-reprocessing | Jube.Dto/EntityAnalysisModelReprocessingRule*/ ListColumn Title / FormField hard-coded English |
grid titles are literals, same house pattern as other DTOs | integration pass with form-model localisation
model-reprocessing |
Jube.Resources/EntityAnalysisModelReprocessingRule{,Instance}Resources.{pl,ar,ka,id,sv,ur,hi}.resx | created (14
files) | none, listed for audit

rule-authoring | Jube.App/Vue/src/locales/{en,pl,ar}.json | add the 8 new keys in
review/rule-authoring.locale-proposals.json (_en_new, pl, ar); components fall back to English via te () until then |
integration pass

model-integrity-sync | Jube.App/Vue/src/locales/{en,pl,ar,ka,id,sv,ur,hi}.json | add integrity.category. *,
integrity.entityKind.*, integrity.approvalKind.* and integrity.deps.entityLabel/missingName (46 keys, all eight locales)
from review/model-integrity-sync.locale-proposals.json; until then the labels fall back to the stable code (te ()
guard), never to a raw key | integration pass
model-integrity-sync |
Jube.Dto/Repository/EntityAnalysisModelSynchronisationSchedule/EntityAnalysisModelSynchronisationScheduleDto.cs |
ListColumn Titles "Scheduled For", "Created By", "Created" are hard-coded English; and InsertAsync returns the
SchedulePoco, not the DTO | low
model-integrity-sync | Jube.App/Vue/src/components/ModelSynchronisation.vue, ModelSample.vue (model-core listed them as
pull-ins) | claimed here; add to model-integrity-sync vue paths in slices.json, plus PendingApprovals.vue,
PendingEntitiesDetail.vue, PaneToggles.vue, SeverityBadge.vue, RuleIntegrity.vue | agents may not edit slices.json
model-integrity-sync | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | add integrity, sample, download groups (162 keys
each) ready in review/model-integrity-sync.locale-proposals.json | integration pass

BUG | model-integrity-sync |
Jube.Engine/EntityAnalysisModelManager/BackgroundTasks/TaskStarters/ModelSyncTaskStarter.cs | FIXED surgically:
SynchronisedDate was stamped at confirmation time so a schedule row inserted during a long sync was never honoured; now
stamped with the pending query's observed time
BUG | model-activation | Jube.Engine/.../ActivationRules/ActivationRuleOverridesExtensions.cs Admissible | a model level
override (all activation rules) skipped the rule EnableForce, OverrideKey binding and EnableOverride admission that
named overrides get, so a model level Force could force rules that never opted in | FIXED (expanded per admitting rule);
3 unit tests in ActivationRulesHelperExtensionsTests
BUG | model-activation | EntityAnalysisModelActivationRuleDtoValidator CaseWorkflowGuid/CaseWorkflowStatusGuid | only
NotEmpty, so a rule could point at another tenant's Case Workflow and the engine would create cases there | FIXED
(CaseWorkflowStatusVisibleAsync, new key CaseWorkflowStatusGuidNotFound); 4 service tests
BUG | model-activation | EntityAnalysisModelActivationRuleRepository.UpdateAsync /
ResponseElevationRangeRepository.UpdateAsync | model id, evaluation/activation/forced counters came from the client body
(rule could be moved, counters forged or wiped) | FIXED (pinned from the existing row; counters zeroed on service
insert); range validator now uses EntityAnalysisModelParentGuard instead of tenantless GetByIdAsync
BUG | model-activation | Override repositories + services | Delete Expiry Date dropped on create (mapper), Update with
unknown Id created a row, model Override Update lookup had no tenant filter, expiry change of a Force override needed no
permission 62 | FIXED

design | model-activation | Jube.Validations/EntityAnalysisModelActivationRule/*DtoValidator.cs | `sealed partial class`
only for GeneratedRegex, conflicts with the no partial classes house rule | owner decision (use a static Regex field)
design | model-activation | Jube.Dto activation rule, range DTO [ListColumn Title = "Name"/"From"/...] | hard-coded
English grid titles | shared TODO as in model-abstraction finding 11
design | model-activation | Activation rule and range repositories UpdateAsync | CreatedUser and CreatedDate overwritten
with the editor and now on every update | owner decision, same as TTL counters

slices.json | model-activation | .claude/skills/jube-slice-review/slices.json | add backendKeywords
ResponseElevationRange, ActivationRuleOverride; add Jube.Resources per-class resx and
review/model-activation.locale-proposals.json to the commit | agents may not edit slices.json

model-advanced-analytics | docs/AnalyticalMethodology/ *.md (Introduction.md:16, NaiveBayes.md:373,381,
DecisionTreesAndBoosting.md:529-531, OtherModelTypes/AdaBoostClassifier.md:83-86, BaggingClassifier.md:67-72) | name the
vendored library "Accord" in user-facing docs; naming rule says generic names outside the libraries | owner of
AnalyticalMethodology to reword (these pages are about library internals, may be an accepted exception: design)
model-advanced-analytics | Jube.App/Vue/src/components/JubeChart.vue | BUG | sanitiser strips script/foreignObject/on*/non-#
href but keeps <style> and style attributes (CSS url ()/@import can fetch external resources); server SVG is trusted
today, so only hardening | also drop 'style' elements containing url (or @import, data-grid slice
model-advanced-analytics | Jube.Charting/Theme/ChartCss.cs | RTL/bidi of SVG text (text-anchor, direction, bidi
isolation) and bundled fonts not audited here | charting owner to audit with Arabic and Urdu labels
model-advanced-analytics | .claude/skills/jube-slice-review/slices.json | add
Jube.Service/Repository/AskJoobyAnalytics*, EntityApproval service/endpoint and tests to backendKeywords; Jube.Resources
AskJoobyAnalytics*, EntityApprovalResources | agents may not edit slices.json
model-advanced-analytics | Jube.Data/Repository/AskJoobyAnalyticsInstanceRepository.cs | Update/Allocate/SetActive have
no OptimisticConcurrency.Verify though DTO carries Version | add Verify like VersionedApprovalRepository siblings; not
done, touches service signatures

BUG | model-activation | Jube.Dto/EntityAnalysisModelActivationRule/EntityAnalysisModelActivationRuleDto.cs:53
([ListColumn Title = "Name"]) and EntityAnalysisModelResponseElevationRangeDto.cs:50-102 (
"Name","From","To","Decline","Hold","Active") | hard-coded English grid titles; needs {area}.column.{field} keys in
FormModelResources pl/ar/ka/id/sv/ur/hi | form-model-engine owns FormModelResources (same as data-grid finding 3); not
fixed here

model-network | Jube.App/Vue/src/components/SvirfdSearch.vue | IP input has no dir="ltr" so an address typed in an RTL
locale can reorder; add dir="ltr" to the input | whoever owns SvirfdSearch
model-network | docs/Concepts/EnvironmentVariables/index.md | SvirfdBaseUrl row should say it must be https (loopback
http only) and EnableSvirfdRemoteEngineLookup row should say private addresses are never sent | docs integration pass
model-network | BUG | Jube.Data/Query/GetGraphRecallEdgesQuery (fixed here) | edges of deactivated or deleted registries
were loaded and merged into null-registry edge facts while Connects ignored them | fixed

model-http-adaptation |
Jube.Engine/EntityAnalysisModelManager/EntityAnalysisModel/Models/Models/EntityAnalysisModelHttpAdaptation.cs
(HttpClient handler) | Svirfd has its own breaker in SvirfdHost.cs; Jube.ResilientHttp now exists | after the
model-network agent finishes Svirfd, adopt Jube.ResilientHttp (ResilientHttpExecutor keyed per host,
HttpResilienceOptions, HttpFailureClassification) in place of the bespoke breaker; not touched by this slice | DONE by
model-network: SvirfdHost uses ResilientHttpExecutor (own instance per host)
model-http-adaptation | TenantRegistryResources.* (neutral, pl, ar, ka, id, sv, ur, hi), TenantRegistryDto,
TenantRegistryMapper, TenantRegistryDtoValidator, TenantRegistry and TenantRegistryVersion pocos,
TenantRegistryServiceTests, Jube.Tests.csproj, Jube.Engine.csproj, Jube.sln, DynamicEnvironment.cs | touched surgically
for tenant-scoped HTTP Adaptation settings (3 columns, 2 validator messages, new DynamicEnvironment keys); .fr.resx not
touched so fr falls back to English for the two new messages | tenant-registry owner to read the hunks;
FormModelResources needs a group title for "HttpAdaptation" and labels for the three DTO fields; check any form-model
golden or wire.generated stamp covering TenantRegistryDto
model-advanced-analytics | Jube.Charting/Theme/ChartCss.cs and every golden (Jube.Tests/Charting/**/Golden/ *.svg,
External/Golden/jube-charting.css, Jube.App/wwwroot/css/jube-charting.css) | changed: charts now force direction:ltr (
text-anchor start/end flip inside an RTL page) and the font stack gains Noto fallbacks; goldens patched by exact string
replacement, not regenerated | other chart slices: run Jube.Tests Charting goldens (ChartStylesheetTests,
ChartGoldenTests, DiagramGoldenTests); regenerate with CHARTING_EXTERNAL_GOLDEN_GENERATE_DIR if any differ
model-advanced-analytics | Jube.App/Vue/src/assets/main.css | SPA bundles only IBM Plex (Latin); Arabic, Urdu, Devanagari
and Georgian rely on system fonts | shell-auth: bundle Noto fonts locally if offline rendering is required
model-advanced-analytics | Jube.Service/Repository/AskJoobyAnalyticsModel | promote/rollback now refused while instance
is live (InstanceIsLive); ApprovalsRequiredResolver.Default is used because the service has no access to the
ApprovalsRequired setting | pass the real setting if ApprovalsRequired > 1 deployments matter
model-network | slices.json | model-network `permissions` is [60, 72] but 60 is View Model Integrity (
model-integrity-sync); the slice's permissions are [15, 72] | owner (slices.json not editable by agents)
model-exhaustive | Jube.Engine/Exhaustive (z-score clipping) | RESOLVED in the follow-up pass: ported by hand from
dfad0797 (see review/model-exhaustive.md section 12); branch exhaustive-zscore-clipping must NOT be merged on top, it
would conflict | owner
model-exhaustive | Jube.Service/Charts/Exhaustive*Renderer.cs, ExhaustiveNarrativeRules.cs,
ExhaustiveNarrativeFacts.cs | hard-coded English chart titles, series names, metric tile labels and the narrative
verdict sentences (about 100 strings); the renderers take no culture and are not localised, so SPA locales other than en
show English charts; needs a Jube.Resources class and culture plumbing through the chart services | model-exhaustive
follow-up or charting owner (follow-up pass: still open, about 330 literals and 209 test references, see
review/model-exhaustive.md section 12)
model-exhaustive | Jube.Data/Repository/ExhaustiveSearchInstancePromotedTrialInstanceRepository.cs and ~10 sibling
Exhaustive repositories | BUG | `|| !tenantRegistryId.HasValue` row escape still present in UpdateActiveAsync (same
pattern as the model-core BUG line); the soft-delete filter was added, tenant escape left because the tenantless
constructor is the engine mode | owning slices
model-exhaustive | Jube.Resources/ExhaustiveSearchInstanceVariableStatisticsResources.*.resx | the seven locale files
existed untracked before this pass (not created by model-exhaustive); key parity with English verified | integration
pass
model-exhaustive | Jube.App/Vue/src/locales/ka.json id.json sv.json ur.json hi.json | 219 keys under exhaustive, dataset,
upload proposed in review/model-exhaustive.locale-proposals.json | integration pass
model-exhaustive | Jube.App/Vue/src/locales/en.json pl.json ar.json + Jube.App/Vue/src/components/ExhaustivePromotedModels.vue |
add key exhaustive.promoted.instanceLive (en: "This Exhaustive adaptation is live. Switch it off, change the promoted
model and switch it on again through approval.", pl and ar to translate; ka id sv ur hi are in
review/model-exhaustive.locale-proposals.json) and show it in the onError handler when the PUT answers 409
(describeApiError currently maps 409 to the generic activateFailed) | shell-auth owns the locale JSON; the component is
model-exhaustive's
model-exhaustive | Jube.Engine/Exhaustive/Statistics/PopulationStatisticsWriter.cs | the triangular distribution bounds
use the static Normalisation.ZScore with the default clip bound 5 whatever ExhaustiveZScoreClipBound says (the clipping
branch disclosed this); thread DynamicEnvironment through StatisticsCalculator and the writers if a non-default bound
should reach it | model-exhaustive follow-up

case-suppression | BUG | Jube.Data/Repository/CaseRepository.cs:85,118,146 | `|| !tenantRegistryId.HasValue` row escape
in GetByIdActiveOnlyAsync and two siblings: a caller with no tenant row would match cases of every tenant (suppression
services refuse a caller with no tenant first, so not reachable from them) | case-record owns CaseRepository: drop the
`|| !tenantRegistryId.HasValue` and require `tenantRegistryId.HasValue &&` as CaseSuppressionRepository.Visible does
case-suppression | Jube.App/Vue/src/locales/en.json pl.json ar.json ka.json id.json sv.json ur.json hi.json | suppression.*
(130 keys) exists in en, pl, ar; ka, id, sv, ur, hi proposals in review/case-suppression.locale-proposals.json |
integration pass; also CaseSuppressionResources.{pl,ar,ka,id,sv,ur,hi}.resx created (46 keys each)
case-suppression | docs/Configuration/Administration/Permissions/index.md | add permission 70 Approve Case
Suppressions | permissions slice owner
case-suppression | Jube.App/Vue/src/cases (Approvals panel, cases-approvals) | panel builds 'key = value' without bidi
isolation (suppression/state.ts now exports isolate and targetName) | case-workspace owner: wrap key, value, maker in
isolate ()

case-journal | BUG | Jube.Data/Query/CaseQuery/ProcessCaseQuery.cs:62 | plain SelectToken on tenant-configured XPath, no
regex timeout; journal sites are fixed with the new Jube.Data.Helpers.BoundedJsonPath | cases-workspace owner: replace
with Jube.Data.Helpers.BoundedJsonPath.Select (json, xPath.XPath)
case-journal | Jube.Data/Helpers/BoundedJsonPath.cs vs
Jube.Engine/EntityAnalysisModelInvoke/Extraction/BoundedJsonPath.cs | duplicate helper because Jube.Data cannot
reference Jube.Engine | integration pass: keep one (Engine could delegate to the Data one) and merge the tests
case-journal | Jube.Service/Charts/CaseKeyJournalChartRenderer.cs, CaseKeyJournalTransactionChartRenderer.cs | about 35
hard-coded English chart titles, axis titles and subtitles, no culture plumbing | add a CaseKeyJournalChartResources
class plus an IStringLocalizer parameter on Render, same fix as the Exhaustive renderers
case-journal | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | 344 journal.* keys proposed in
review/case-journal.locale-proposals.json | integration pass
case-journal | Jube.Data/Repository/CaseKeyJournalKeyIndexRepository.cs, Poco/CaseKeyJournalKeyIndex.cs | retired
runtime index job; repository only used by the migration script test | owner: delete once the table is dropped
case-journal |
Jube.Resources/{CaseByCaseKeyValue,CaseEventByCaseKeyValue,CaseJournal,CaseKeyJournal,CaseNoteByCaseKeyValue,CaseWorkflowFormEntryByCaseKeyValue,SessionCaseJournal}Resources.{pl,ar,ka,id,sv,ur,hi}.resx |
created (49 files), parity verified by script | none, listed for audit
case-journal | slices.json | Jube.App/Vue/src/cases is claimed by cases-workspace and case-journal; journal owns
src/cases/journal | owner (agents may not edit slices.json)
BUG | case-workflow-config | Jube.Service/CaseDispatch/CaseDispatcher.cs LegacyCallAsync (lines 237-260),
Jube.Engine/CaseEscalation/CaseStatusNotifier.cs:61,66, Jube.Case/SendHttpEndpoint.cs | SSRF: a Status, Action or Macro
HttpEndpoint with no SQL (payload mode) is called through the bare SendHttpEndpoint HttpClient, not GuardedHttpEndpoint,
so a tenant author can make the server call loopback, private or cloud-metadata addresses (the SQL mode CallAsync and
the release hook are guarded; authoring validators of the five DTOs only require the field to be non-empty) | route
LegacyCallAsync and CaseStatusNotifier through GuardedHttpEndpoint.For (dynamicEnvironment), add an
OutboundHostPolicy.CheckTemplate rule to the Status/Action/Display/Macro/Form validators like the release hook, and give
CaseWorkflowMacroExecutionServiceTests/Hook an allowed-hosts entry for the loopback listener | cases-workspace owns
CaseDispatch, not changed here
BUG | case-workflow-config | Jube.Data/Repository/CaseWorkflowFormEntryRepository.cs:56,64 |
`|| !tenantRegistryId.HasValue` on an int? field fed by the tenantless constructor (unused outside tests): GetAsync and
GetByCaseKeyValueActiveOnlyAsync return every tenant's entries when constructed without a tenant, and GetAsync has no
deleted or role filter | make the field int, drop the tenantless constructor and the `|| !HasValue` branches, filter
GetAsync like the ByCaseKeyValue query | case-record owns the file
BUG | case-workflow-config | Jube.Data/Query/GetCaseJournalQuery/GetCaseJournalQuery.cs:177 |
`new Regex(xPath.RegularExpression)` has no match timeout for the tenant-authored CaseWorkflowXPath regular expression
(ProcessCaseQuery.cs:81 uses 1 s; the XPath validator now refuses back references and look-around, but stored rows
pre-date it) | pass RegexOptions.NonBacktracking and a 250 ms timeout as FormValidatorEngine.Matches does | case-journal
owns the file
BUG | case-workflow-config | Jube.Service/CaseDispatch/CaseSqlRunner.cs:80,90 and CaseDispatcher.cs refusal strings |
hard-coded English (The SQL took too long and was stopped., The SQL could not be run., The rendered destination is
not ..., The endpoint was refused: ...) are stored in the dispatch audit and shown to the author | move to
CaseWorkflowSqlResources with keys, resolve through the localiser | cases-workspace owns CaseDispatch
BUG | case-workflow-config | Jube.Dto/Repository/CaseWorkflow*/ (12 DTOs: Name, Active, Priority, Outcome, Field, Kind,
Source field, Source value, SLA minutes ListColumn titles) | hard-coded English grid titles, same as the login-audit DTO
line | add {area}.column.{field} keys to FormModelResources for en, pl, ar, ka, id, sv, ur, hi | form-model-engine owns
FormModelResources
FIXED | case-workflow-config | Jube.Data/Query/GetEntityAnalysisModelRequestXpathByCaseWorkflowQuery |
`|| !tenantRegistryId.HasValue` null-tenant escape removed, tenant parameter now int (single caller,
EntityAnalysisModelRequestXPathService passes its resolved tenant) | none
design | case-workflow-config | Jube.Service/CaseDispatch/CaseSqlRunner.cs, FormSqlRunner.cs, CaseChecklistReconciler |
case SQL is vetted against the author's table permissions at save (permission 67) but runs for every viewer with
BuildVisualisationAllowedTables (userName, _ => true): tenant scope without the viewer's table permissions, so a display
can show a viewer rows from tables the viewer cannot query directly; documented in DatasourceTemplateDialect.md | owner
to confirm this is intended
BUG | case-record | Jube.Data/Repository/CaseWorkflowFormEntryRepository.cs GetByCaseKeyValueActiveOnlyAsync (and
CaseWorkflowFormEntryByCaseKeyValue query if it shares the shape) | submitted form entries are listed to anyone holding
the workflow and status roles; the CaseWorkflowFormRole of the entry's form is never checked (insert does check it via
GetByIdActiveForCaseWorkflowGuidAsync), so a form restricted to a supervisor role is readable by every analyst on the
case; also keeps `|| !tenantRegistryId.HasValue` | add the same CaseWorkflowFormRole -> RoleRegistry -> UserRegistry
predicate on the entry's CaseWorkflowFormId, make the tenant test strict like CaseFileRepository now is
(case-workflow-config owns the file)
BUG | case-record | Jube.Data/Repository/CaseRepository.cs GetByIdActiveOnlyAsync and siblings |
`(...TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)` is the gate every case child (notes, files,
research, hold, checklist, summary) relies on; a caller whose UserInTenant row is missing would see every tenant if a
service ever skipped its own tenant guard | replace with `tenantRegistryId.HasValue && ... == tenantRegistryId` as done
in CaseFileRepository and CaseNoteRepository this pass (cases-workspace owns the file)
case-record | Jube.App/Vue/src/cases/detail/CaseNoteItem.vue, CaseNoteFiles.vue, CaseResearchItem.vue,
CaseNarrativeCard.vue | RTL: user-authored note text, research topic/findings and file names render without dir="auto"
or <bdi>, so an Arabic note in an English UI (or the reverse) reorders punctuation and a bidi-laden file name can still
visually spoof an extension; wrap the file name and author in <bdi> and give the note and research text elements
dir="auto" | cases-workspace owns src/cases; server now strips bidi controls from file names but old rows are unchanged
case-record | Jube.App/Vue/src/cases/detail/CaseResearchItem.vue (source.title) and CaseNoteFiles.vue | source titles are
stored HTML-sanitised and rendered through {{ }} so a title containing angle brackets shows tag text literally; render
with plainText like notes do | cosmetic, cases-workspace owns the file
case-record | Jube.App/Vue/src/cases/detail/DecideNoteDialog.vue, DecideResearchDialog.vue | the file input has no
client-side size check against the 25 MB server limit and no accept hint, so a 200 MB pick uploads fully before the 400;
pre-check file.size and show the existing notAttached toast | UX, cases-workspace owns the files
case-record | Jube.Service/Security/HtmlSanitiser.cs | `public static partial class` (needed for GeneratedRegex) breaks
the no-partial-classes house style, owner decision whether GeneratedRegex is an accepted exception | foundation-owned
file, not changed
cases-workspace | Jube.Data/Query/CaseQuery/ProcessCaseQuery.cs | BUG | FIXED surgically: SelectToken on tenant XPath
now uses JsonSelectSettings RegexMatchTimeout 250ms (Jube.Data cannot reference Engine BoundedJsonPath); remaining plain
SelectToken sites belong to case-journal (GetCaseJournalQuery:151, JournalTransactionReader:108, JournalLensReader:
263,361, CaseKeyJournalPeerAccumulator:138, CaseKeyJournalService:476)
cases-workspace | Jube.App/Vue/src/cases/lastUsed.ts | FIXED (scoped by tenant and user) BUG (low) | localStorage key
jube.cases.last is not scoped per user, so on a shared browser the next user restores the previous user's workflow and
filter guids (server refuses what they cannot see); scope the key by user name
cases-workspace | Jube.Service/Repository/CaseTag/CaseTagService.cs MergeAsync | FIXED (409 on a case locked by another
user) was design | tags can be merged on a case locked to another analyst or closed, unlike Diary and Case.Update; owner
to decide whether the lock applies to tags (notes behave the same)
case-journal | BoundedJsonPath consolidation | every caller now uses Jube.Data.Helpers.BoundedJsonPath
(RequestFieldExtraction, RequestJsonAccessor, InvocationContextBuilder, CategoricalExpansion, DataUtility,
EntityAnalysisModelSampleService, journal files); the Engine copy was byte-for-byte equivalent | owner: delete
Jube.Engine/EntityAnalysisModelInvoke/Extraction/BoundedJsonPath.cs and
Jube.Tests/Engine/EntityAnalysisModelInvoke/Extraction/BoundedJsonPathTests.cs (the same three assertions now live in
Jube.Tests/Data/Helpers/BoundedJsonPathTests.cs); my deletion was refused by the permission classifier
case-journal | Jube.Service/Charts/CaseKeyJournal *Renderer.cs | PLAN for the chart localisation, to do when code can be
built: (1) add Jube.Resources/CaseKeyJournalChartResources.cs and .resx plus the seven locale resx, one key per title,
subtitle, axis, legend and total string (about 35, extracted from the
Title/Subtitle/XAxisTitle/YAxisTitle/LegendTitle/Step/Total/Series calls and the JournalScoreBands.Word text); (2) add
an optional trailing IStringLocalizer? parameter to both Render methods, falling back to the English literals so
existing renderer tests stay valid; (3) in CaseKeyJournalEndpoints.ChartAsync and TransactionChartAsync create the
localizer from IStringLocalizerFactory; (4) add a renderer test per locale that no English literal survives
FIXED | case-workflow-config | SSRF in payload-mode HTTP endpoints (Status, Action, Macro, Form):
Jube.Case/Outbound/PayloadEndpointCall.cs (new), Jube.Case/Outbound/GuardedHttpEndpoint.cs (send now runs through
Jube.ResilientHttp per-authority breaker, 10 s timeout, no retry), Jube.Case/Jube.Case.csproj (ProjectReference to
Jube.ResilientHttp), Jube.Service/CaseDispatch/CaseDispatcher.cs LegacyCallAsync (lines 243-262),
Jube.Engine/CaseEscalation/CaseStatusNotifier.cs CallHttpEndpointAsync (new dynamicEnvironment parameter) and
LegacyCaseStatusDispatch.cs, validators of Action, Form, Status, Macro (HttpEndpointInvalid) with resx and cs keys |
surgical edits in cases-workspace files, the existing GuardedHttpEndpoint and OutboundHostPolicy (connect-time IP check,
DNS rebinding, allow-list keys) were reused rather than a second guard; tests PayloadEndpointCallTests (4), four
validator theories | cases-workspace reviewer please re-read these hunks
integration | cases-workspace minor items | (a) CaseStatusMover.MoveAsync: narrow window remains between the still-open
check and the update before the checklist-bypass audit is recorded (the bypass evaluation reads the current status):
make check, audit and update atomic (one transaction or a conditional update followed by the audit) | (b)
CasesAnalyticsPanel: each chart triggers a full server report: batch or share one report call | (c) case list actions
are not labelled with the case (a11y: add the case key to the accessible name) | (d) CasesAnalyticsPanel fixes `now` at
mount so it goes stale in a long-lived tab | owner policy: bugs get fixed; do these in the integration pass
watcher | Jube.App/Vue/src/locales/en.json pl.json ar.json | add watcher.popup.elevation, watcher.popup.travel,
watcher.popup.travelInstant, watcher.popup.impossible, watcher.popup.plausible (en/pl/ar/ka/id/sv/ur/hi in
review/watcher.locale-proposals.json); until then locales.spec.ts "references only keys that exist in en" lists them (
code ships English defaults so the UI is correct) | shell-auth owns the locale JSON
watcher | Jube.App/Pages/Watcher/Activation.cshtml (.cs), Jube.App/wwwroot/js/Watcher/Activation.js,
Jube.App/Pages/Shared/_Layout.cshtml link, Jube.App/Vue/src/pages/ToolsPage.vue entry | Razor pages are still mapped (
MapRazorPages) so watcher removed the OpenStreetMap tile layer from Activation.js (kendo map now draws markers on a
blank canvas, no remote request); delete the page with the rest of the Razor UI | legacy removal owner
watcher | Jube.Migrations/Branches/PenTestGbkRemediation/SeedDefaultHttpResponseHeaders.cs,
Branches/ZapWarnNewHardening/HardenSiteIsolationAndContentSecurityPolicy.cs | both seed a CSP naming https://*
.tile.openstreetmap.org; Branches/WatcherMap/RemoveRemoteTileHostsFromContentSecurityPolicy.cs (rewritten by watcher to
name no remote host) is the migration that supersedes them, so fresh installs end with no remote host; renamed by
watcher to RemoveRemoteTileHostsFromContentSecurityPolicy (unreleased, new on the branch); the two older seed migrations
exist on master and are left alone | migrations owner
watcher | Jube.Charting + Jube.Service/Charts/WatcherSummaryChartRenderer.cs | series names "Activations" and "Aggregate
response elevation" in the Watcher summary SVG are hard-coded English; the request needs optional label fields filled
from t () in ActivationWatcher.vue, and POST /api/Chart/Watcher/ActivationSummary is RequireAuthorization only (pure
render of the posted rules, no tenant data, so no leak) but could take permission 30 | needs a compiler, charting owner
watcher | Jube.App/Code/Sse/SseConnectionRegistry.cs | Tracked () allocates a list of every connection on each call and
the per-user stream cap in WatcherStreamEndpoints uses it on connect; add CountForUser (userName) if connect storms
matter. Also ServiceChangeStreamEndpoints has no per-user cap and no periodic authorisation recheck like the Watcher
stream now has | shell-auth owns the registry
sanctions | BUG fixed | Jube.Service/Invoke/InvokeService.cs + Jube.App/Endpoints/InvokeEndpoints.cs | GET
/api/Invoke/Sanction had no server-side permission check (any signed-in user of any tenant could search; only the SPA
page checked permission 29), no rate limit and no cap on returned entries; now SanctionAsync requires permission 29
(403), reuses ParseRateLimiter (429, SanctionSearch* keys) and caps entries. InvokeEndpointTests grants 29 to the
scaffold role in InitializeAsync; other tests that call the endpoint as a scaffold user need the same grant.
sanctions | Jube.DynamicEnvironment/DynamicEnvironment.cs | three surgical keys added (SanctionSearchWindowSeconds,
SanctionSearchMaxPerUserPerWindow, SanctionSearchMaxEntries); docs/Concepts/EnvironmentVariables rows added.
sanctions | Jube.Case/Outbound/GuardedHttpEndpoint.cs | one public method OpenGetAsync (Uri) added so the sanctions
loader reuses the SSRF-guarded client (policy check plus connect-time resolved-address check, no auto redirect); the
loader follows at most five redirects itself through Jube.Engine/Sanctions/SanctionListDownloader.cs.
sanctions | Jube.App/Vue/src/components/DataGrid.vue (data-grid) | cell text has no dir="auto" or bdi isolation, so
right-to-left names (Arabic, Urdu) inside the sanctions grids can reorder neighbouring numbers; SanctionSearch.vue works
around it with U+2068/U+2069 in the column format, a grid-wide fix is preferable.
sanctions | Jube.App/Vue/src/upload/file.ts (model-exhaustive) | no maximum size in uploadProblem; the sanctions upload
relies on the server 64 MiB refusal; a client-side maximum option on FileUpload would give an earlier message.
sanctions | Jube.Resources/SanctionEntrySourceResources.fr.resx | untouched per owner; lacks the new keys ImportFailed
and FileTooLarge (fall back to en).
graph | locale integration | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | apply review/graph.locale-proposals.json (259
graph.* and adminGraphs.* keys plus nav.graph and nav.adminGraphs) when those SPA locale files exist | slice owns no SPA
locale JSON
graph | design | Jube.Service/Query/Graph/GraphAnalyticsService.cs, GraphService.cs GetNodeAsync | community, pattern,
hub lists and cached node features are tenant-wide and built from restricted edges too, and node case counts ignore the
viewer's case workflow role; needs an owner decision on registry scoping | see review/graph.md finding 6 and 7
graph | pattern | every service that opens a long-lived DbContext per request | GraphService and GraphAnalyticsService
now set DbContext.CommandTimeout to 30 seconds; other query-heavy services (Visualisation, Sanctions search) may want
the same | none is a bug found in this slice
ask-jooby | BUG | Jube.App/Vue/src/askjooby/format.ts, offlineWelcome.ts, welcome.ts, render.ts | remaining hard-coded
English (formatStatParts "Joobrik (s)", "cost unknown", "distinct", welcome and offline welcome text,
defaultFeedbackLabels fallback) | pass a translator into the pure modules; proposals in
review/ask-jooby.locale-proposals.json cover controller strings only
ask-jooby | locale | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json do not exist; pl.json and ar.json need the 11 new
askJooby keys | apply review/ask-jooby.locale-proposals.json in the integration pass
ask-jooby | .claude/skills/jube-slice-review/slices.json | add Jube.App/Vue/src/stores/joobyContext.ts to ask-jooby vue
paths; backendKeywords add AskJoobyConversation, AskJoobyFile, AskJoobyMemory; Jube.Resources
AskJoobyConversationResources and AskJoobyMemoryResources locale resx | agents may not edit slices.json
BUG | case-record | Jube.Data/Repository/CaseWorkflowFormEntryRepository.cs and
Jube.Data/Query/GetCaseWorkflowFormEntryByCaseKeyValueQuery | form entry role gate and null-tenant escape | FIXED by
case-record in the follow-up pass (both now require a CaseWorkflowFormRole grant; the repository tenant test is strict);
tests added in CaseWorkflowFormEntryServiceTests, CaseWorkflowFormEntryByCaseKeyValueServiceTests, and the two query
test seeders now grant a form role
BUG | case-record | Jube.Data/Repository/CaseRepository.cs | `|| !tenantRegistryId.HasValue` | FIXED by case-record:
GetByIdActiveOnlyAsync is strict; GetAsyncActiveOnlyAsync and GetByExpiredAsync keep the tenantless engine mode only for
the `CaseRepository(dbContext)` constructor (userName null), a user without a tenant row sees nothing
case-record | Jube.Resources/CaseWorkflowFormEntryResources.resx and .cs | new key CaseLocked added to the neutral file
and the constants class only; case-workflow-config must include it in the seven locale files it creates (English: "The
case is locked by another user."; translations in review/case-record.md section 12) | resx owner
case-record | Jube.App/Vue/src/cases/detail (note, research, files, forms dialogs) | the server now answers 409 {code:
"CaseLocked"} for note, finding, research, upload, file removal and form submission on a case locked to another user,
and 400 FileTypeNotAllowed/FileContentNotAllowed/FileRejectedByScanner/FileScanUnavailable for uploads; make sure the
toasts show the server message (toasts.apiError) and that attach errors in DecideNoteDialog are not swallowed as a bare
count | cases-workspace owns the files
BUG | visualisation | Jube.Data/Reporting/PostgresSqlValidator.cs | FIXED: a CTE named after a forbidden table plus a
schema-qualified reference (WITH "UserRegistry" AS (...) SELECT * FROM public."UserRegistry") bypassed the allow-list,
and a CTE named after a table a tenant template reads (UserInTenant, TenantRegistry, RoleRegistry) captured the
template's own tenancy check; fixed by never treating qualified names as CTEs and schema-qualifying template relations
with public | other agents using RewriteTableReferences (ask-jooby, case SQL): run AskJooby/DynamicSql* and Reporting
tests; rewritten SQL now reads public."X"
BUG | visualisation | Jube.Service/CaseDispatch/CaseSqlRunner.cs, FormSqlRunner.cs, ChecklistItemSqlAuthoring.cs |
inherit the PostgresSqlValidator fix; the design question on viewer table scope (see case-workflow-config) is unchanged
design | visualisation | Jube.Service/Repository/VisualisationRegistry/VisualisationRegistryService.cs
(GetByShowInDirectoryAsync, GetDirectorySummaryAsync) | directory and summary need no permission, only a role grant
(pinned by test ListByShowInDirectoryNeedsNoPermissionOnlyTheRoleGrantAsync); summary exposes LastExecutionUser names to
any granted viewer | owner to confirm
BUG | visualisation | Jube.Service/Charts/Datasource, DatasourceChartKinds.cs and the other chart renderers | chart SVG
text (axis titles, tile labels) is English from the server except datasource kind/role/message text; see
model-exhaustive line
tools-bench | .claude/skills/jube-slice-review/slices.json | tools-bench vue list claims Jube.App/Vue/src/sample, which is
production code (components/ModelSample.vue imports sample/api and sample/model), move it to model-integrity-sync; add
src/benches.ts, src/ __tests__/benches.spec.ts, .env.bench, review of the build-bench script, and e2e names modelStory,
story, graphRecall, integrity etc. that also open /tools
tools-bench | Jube.App/Vue/package.json, .github/workflows/jube-vue.yml, Jube.App/Vue/.env.bench | edited surgically: new
build-bench script (vite build --mode bench) and CI runs it after build-only because CI Playwright serves a production
build via npm run preview, so a DEV-only gate would 404 every /tools spec; the shipped image must only ever use
build-only
tools-bench | Jube.App/Vue/src/router/index.ts, Jube.App/Vue/src/layout/menuTree.ts | edited surgically: /tools and /scaffold
routes and the Tools menu entry are now behind benchesEnabled (src/benches.ts); /dev/graph still uses
import.meta.env.DEV directly (GraphBench has no e2e) so it is unreachable in the CI bench build, keep or switch to
benchesEnabled
tools-bench | Jube.App/Vue/src/components/ModelPicker.vue, RuleTools.vue, GridLevel.vue, HierarchyStack.vue | design: no
production page imports these components, only /tools benches and tests, so in a plain production build they are dead
code, owning slices to decide host or delete
tools-bench | Jube.DynamicEnvironment/DynamicEnvironment.cs:312, Jube.App/Startup.cs:612 | design: EnableMockEndpoints
defaults True so a default production deployment maps unauthenticated /api/MockHttpAdaptation/* and /api/Mfa (hard-coded
client-key and OTP 12345678); owner documented True because the seeded example adaptation targets the mock,
PenTestMockEndpointsGate.TheFlagDefaultsOn pins it; left unchanged, owner to decide on default False with a dev override
graph | resolved | review/graph.md findings 6 and 7 | the tenant-wide analytics and node case counts design lines above
are now fixed in code (analytics from unrestricted edges only; case counts by workflow and status role); only case nodes
still expose case ids to any graph viewer (finding 18, design) | follow-up pass
sanctions | Jube.Engine/Sanctions | transliteration between scripts (Arabic, Cyrillic, Georgian, Devanagari to Latin) is
a design decision, not built: no transliteration source in the repo; recommendation is a separate maintained source (ICU
Any-Latin or per-list alias fields). The lookalike (confusables) fold and cached entry tokens are built.
admin-performance | .claude/skills/jube-slice-review/slices.json | slice claims only Resp *Endpoints.cs; the other 36
grid endpoint files, their Service, Query/Repository folders and {Area}Resources.* are unclaimed: claim them (first
touch by admin-performance, reviewed there) | agents may not edit slices.json
admin-performance | Jube.Tests/Security/Observability/PenTestObservabilityContent.cs | add WafAttack to TextAreas with a
SeedTextAsync case (MatchedValue = text) and TextFields entry so the stored-hostile-text and secret-redaction pen tests
cover it; WafAttackService now redacts MatchedValue and WafAttackFlushService redacts at capture | admin-telemetry owns
Observability test files
admin-performance | Jube.App/Vue/src/locales/{ka,id,sv,ur,hi}.json | merge review/admin-performance.locale-proposals.json
(perfHub.* and payloadGrid. *, 77 keys each) | integration pass
admin-performance | design | each grid list query also loads up to 100000 rows for statistics and runs a COUNT even when
take is small; landlord-only and capped, so left; consider caching statistics or computing them in SQL
BUG | visualisation | Jube.Data/Reporting/PostgresSqlValidator.cs | FIXED second pass: CTE names were compared
case-insensitively (WITH userregistry AS (...) SELECT * FROM "UserRegistry" skipped the allow-list), user alias column
lists were dropped by the template swap, operator and UNION chains were unbounded, and format, crypt, gen_salt and
several metadata functions were allowed | owner: run every class in review/visualisation.md section 12.6; Ask Jooby or
case SQL that uses format (now fails
graph | resolved | review/graph.md finding 18 | case nodes are now filtered per viewer by case workflow and status
role (GraphViewerNodes); no design question remains open in this slice | follow-up pass
admin-telemetry | Jube.App/Startup.cs AddAspNetCoreInstrumentation/AddHttpClientInstrumentation | design | traces are
exported with full url paths and OperationScope tags jube.actor (user name) to any OTLP backend, and outbound HttpClient
urls (webhooks) may carry secrets in query strings; add EnrichWith* redaction of url.query and decide whether jube.actor
should be exported | exporter owner
admin-telemetry | Jube.App/Vue/src/locales en/pl/ar/ka/id/sv/ur/hi | adminTelemetry.loading and adminTelemetry.denied needed
by AdminTelemetryPage.vue, see review/admin-telemetry.locale-proposals.json | integration pass
ask-jooby | BLOCKER | Jube.App/Vue/src/locales/en.json, pl.json, ar.json (+ new ka, id, sv, ur, hi) | 19 new askJooby keys
used by the approval card, controller messages and chat log label (approve, decline, approvalTitle, approvalTool,
conversationLog, errorApproval, approvedMessage, declinedMessage, errorWentWrong, ...) | apply
review/ask-jooby.locale-proposals.json (en under en_new) or the card shows raw keys
ask-jooby | docker | docker-compose.yml, docker-compose.redis.yml | jube-r moved to an internal-only network `rsandbox`
shared with the app so a script cannot reach postgres, resp or the app | review: any other compose file or Kubernetes
manifest running jube.r (Jube.Cluster not changed) needs the same isolation
ask-jooby | Jube.DynamicEnvironment/DynamicEnvironment.cs | six new defaults AskJoobyRequireWriteApproval and five
AskJoobyRate*/MaxMessages*/MaxConcurrentTurns* keys added in one surgical edit | add the nine settings to
docs/Concepts/EnvironmentVariables/index.md
ask-jooby | Jube.App/Startup.cs | two HttpClient constructions replaced by AskJoobyOutboundHttpClient.Create () (no
redirects, 60 s, 8 MB) for web search and OpenCorporates | the inference provider clients (BuildOpenAiClient) were not
reviewed for the same options
ask-jooby | BUG | Jube.App/Vue/src/benches.ts (16) | `vue-tsc --build` fails with TS1010 '*/' expected | not mine, another
agent is probably mid-edit
admin-performance | Jube.Tests/Security/Observability/PenTestObservabilityContent.cs | DONE by admin-performance (
WafAttack added to TextAreas, seed case, TextFields); supersedes the earlier line | none
admin-performance | design | list search (ApplicationLogEntry, ContainerLogEntry, PostgresLogEntry, ModelInvokeWarning
and other repo-searched logs) matches the stored raw text while display is redacted, so a landlord can probe a secret by
searching; PostgresActivity and PostgresStatementStatistics redact before search; CaseCreationWarning.CaseKeyValue and
ModelInvokeWarning.Message show customer business keys across tenants to the landlord
preservation | Jube.App/Vue/src/locales/en.json (also pl, ar) preservation.password.hint | BUG | still says the same salt on
the server is needed to import; salt was removed (JEMP v3 password-only); change to "The password is the encryption key
of the package. The same password is needed to import it again." and align pl/ar (proposals file already uses the
corrected text) | shared locale JSON
preservation | Jube.App/Vue/src/locales ka/id/sv/ur/hi | proposals for all 113 preservation and nav keys in
review/preservation.locale-proposals.json | locale integration pass
preservation | Jube.App/Endpoints/Repository/PreservationEndpoints.cs, PreservationSnapshotEndpoints.cs | no explicit
request size limit on Import/ImportPreview (default Kestrel 30MB), no snapshot retention | owner decision, see
review/preservation.md
design | visualisation | docker-compose.yml, docker-compose.redis.yml | quickstart sets ReportConnectionString to the
same superuser as ConnectionString, so the new startup warning (ReportConnectionAdvice) fires on every quickstart;
intended as evaluation-only, owner to confirm the noise is acceptable
ask-jooby | Jube.Data/Reporting/Postgres.cs | ExecuteAlreadyTokenSwitchedAsync (Ask Jooby RunSqlQuery only) now enforces
ReportLimits cell and result byte caps with plain InvalidOperationException (model-facing English); surgical edit, same
ApproximateBytes helper as the card path | visualisation owner: confirm no conflict with your edits in this file
ask-jooby | Jube.Cluster/docker-compose.yml, docker-compose.redis.yml | jube-r moved to internal overlay network
`rsandbox` shared only with jube-api (encrypted); proved the only caller is the app over HTTP request/response | owner:
confirm swarm accepts an internal overlay with dnsrr replicas as expected on the real cluster
preservation | Jube.App/Vue/src/locales/{en,pl,ar,ka,id,sv,ur,hi}.json | PLAN | (a) REPLACE preservation.password.hint with
the values under _REPLACE_existing_keys in review/preservation.locale-proposals.json; (b) ADD
preservation.import.tooLarge (_NEW_keys) to all eight files; PreservationImport.vue already uses it | locale merge
reprocessing-permissions | Jube.App/Vue/src/locales/{en,pl,ar,ka,id,sv,ur,hi,fr}.json | add crud.restrictedByPermission (1
key each), ready in review/reprocessing-permissions.locale-proposals.json | integration pass
reprocessing-permissions | Jube.App/Pages/Model/Frame/EntityAnalysisModelReprocessingRule.cshtml.cs and the
Razor _Layout Models group (AdvancedAnalyticsPageWiringTests pins its list) | legacy Razor page still gates on [26] only
and the Razor Models menu lacks 73 and 74, so a trigger-only or stop-only role cannot open the old frame or see the old
menu (the API and the SPA honour all three) | decide whether the Razor shell is retired before the Vue port ships
reprocessing-permissions | Jube.App/Vue/src/permissions/areas.ts | 26 was grouped under cases; moved to models together with
the new 73 and 74 | none
reprocessing-permissions | Jube.Migrations PermissionSpecification names | permission names are English-only strings in
the catalogue and no resx or locale key translates them (no description column either) | owner decision whether to add a
localised name layer
force-override-approval | Jube.App/Vue/src/locales/*.json | apply review/force-override-approval.locale-proposals.json
(override.col.approval, override.approval.*, integrity.approvalKind.22 and 23; nine locales incl. fr; plural forms
differ per locale) | the Override page and integrity labels read these keys
force-override-approval | Jube.App/Razor Override.js (legacy Override page) and Jube.App/wwwroot/js/Override.js | legacy
page still toggles Force overrides with no approval badge or review; it works through the same services so a Force it
creates is pending, but it shows no state | owner decision whether to keep the legacy page or retire it
force-override-approval | docs/Configuration/Administration/Permissions/index.md | permission 62 row should say a Force
override also needs approval by a holder of 44 | docs
BUG | Jube.App/Vue/src/components/PendingApprovals.vue (integrity/model.ts approvalKinds historyRoute) | the review panel
passed the bare history route (no leading /api/) so the version history and compare calls were relative URLs; fixed
surgically with approvalHistoryRoute () | none
tools-bench | REMOVED 2026-11-09 | benches, ToolsPage, benchesEnabled and VITE_ENABLE_BENCHES are gone; scaffold gate
renamed to scaffoldEnabled / VITE_ENABLE_SCAFFOLD (build-scaffold). Earlier tools-bench lines above are history. Still
hostless: HierarchyStack, GridLevel (its open () still pushes to the removed /tools route), RuleTools, ModelPicker.

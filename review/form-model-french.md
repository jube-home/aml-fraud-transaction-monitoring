# Form model: French locale and baseline gap

2026-11-08. Nothing built or run; XML and key sets verified by script (scratch: the session scratchpad formmodel-fr).

## Created

- `Jube.Resources/FormModelResources.fr.resx`: 121 keys (the 113 shared by pl, ar, ka, id, sv, ur, hi plus the 8 below),
  formal "vous" register, dossier / flux de travail / locataire / charge utile / archive, SourceHash comments computed
  from the DTO English (algorithm verified against all pl hashes: 0 mismatches). Header copied from pl.

## Baseline gap (all eight locales)

The Svirfd label, description and DataProtection group were already translated in every locale (113 keys each), so only
HttpAdaptation was missing. Added 8 keys to each of pl, ar, ka, id, sv, ur, hi and fr, appended before `</root>`:
`TenantRegistry.httpAdaptationUrl`, `.httpAdaptationTimeoutMilliseconds`, `.httpAdaptationValidateSsl`,
`group.HttpAdaptation`, their three `.description` keys and `httpAdaptationUrl.placeholder` (URL left unchanged). Real
translations, so no baseline lines needed for them.

## Key counts

Each of the nine locales: 121 keys, 0 empty, identical key sets, same SourceHash comments. Baseline: 863 lines per
locale (pl, ar, ka, id, sv, ur, hi existing, fr appended, 6904 lines total), identical sets, none overlapping a
translated key.

## Tests

`FormModelLocalisationTests.cs`: `[InlineData("fr")]` added to the three locale theories, `("fr", "Nom")` to the
tag-model theory. RequestLocalisationTests untouched (fr is still not a supported culture and is asserted to fall back
to en).

## Run

`dotnet test --filter FullyQualifiedName~FormModelLocalisationTests`

## Risks

- Machine-quality translations for HttpAdaptation in ar, ka, ur, hi, id, sv, pl; fr and the rest need native review.
- fr stays unreachable until the server registers the culture (existing shared TODO).
- `{Nom}` / `{Composante}` in French descriptions mirror pl's translated `{Nazwa}`; revert to `{Name}` if they must
  match the rule syntax exactly.

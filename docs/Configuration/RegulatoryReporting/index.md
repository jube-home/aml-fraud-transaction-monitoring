---
layout: default
title: Regulatory Reporting
nav_order: 11
parent: Configuration
---

# Regulatory Reporting

`Jube.RegulatoryReport` builds and validates the XML a financial intelligence unit (FIU) asks for when a suspicious
activity or transaction report is filed. It is a stand-alone library: it references nothing else in Jube, not the
database, the web host or any other Jube project, and it has no package dependencies, so it can be packed and published
to NuGet on its own (`dotnet pack Jube.RegulatoryReport`). It is licensed AGPL-3.0-or-later like the rest of the
repository; change `PackageLicenseExpression` in its project file before publishing if a different licence is wanted.

## Why a profile and not a goAML writer

goAML is a family, not one format. The UNODC publishes a baseline schema, and each FIU issues its own variant: extra
national fields, different code lists, stricter rules, sometimes a different major version. A writer that hard-coded one
version would be wrong for most FIUs and would have to be re-released each time one of them changed. So the library
holds no goAML element names at all. Everything FIU-specific lives in a **profile**, which is data:

| Part           | What it is                                                                                                                                                            |
|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `template.xml` | The report as the FIU expects it, with directives saying where each value comes from.                                                                                 |
| `schemas`      | The XSD files the FIU issued, exactly as issued. Nothing is embedded in the library: the FIU's copy is the authority and its terms of use are the deployer's to read. |
| `codeLists`    | Maps from Jube's values to the FIU's codes (countries, currencies, transaction types, indicators).                                                                    |
| `profile.json` | Names the above.                                                                                                                                                      |

A report is built by running the template over JSON data and then validating the result against the FIU's own schema, so
a template that has drifted from the schema is caught the first time it is used, with the element path, line and
position of the first problem.

## profile.json

```json
{
  "name": "example-fiu",
  "description": "Optional.",
  "template": "template.xml",
  "schemas": ["fiu-schema.xsd"],
  "codeLists": { "countries": { "United Kingdom": "GB", "Kuwait": "KW" } }
}
```

Every file must be inside the profile folder. `ReportProfileLoader.Load(folder)` reads it;
`ReportBuilder.Build(profile, data)` returns the XML, its SHA-256 and a list of issues.
`ReportBuilder.Validate(profile, xml)` checks XML produced elsewhere. No XML is returned while there are mapping issues.

## The template

An ordinary XML document in the FIU's namespace, plus the directive namespace `urn:jube:regulatory-report:template:1`
(conventionally `jr`). Directive attributes never appear in the output.

| Directive                               | Meaning                                                                                                                          |
|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| `jr:value="path\|format\|format"`       | The element's text. If the value is empty the element is left out, unless `jr:default` or `jr:keep-empty="true"` says otherwise. |
| `jr:required="true"`                    | Report an issue, naming the data path, when the value is missing or empty. On `jr:each`, when the list is missing or empty.      |
| `jr:default="text"`                     | Text to use when the value is empty.                                                                                             |
| `jr:keep-empty="true"`                  | Emit the element even with no value.                                                                                             |
| `jr:each="path"`                        | Repeat the element once per item of a list; paths inside are relative to the item.                                               |
| `jr:if="path"`                          | Include the element only when the value is present, not false and not empty.                                                     |
| `jr:attributes="a=path\|format;b=path"` | Set attributes; an empty value leaves the attribute out.                                                                         |
| `jr:prune="true"`                       | Drop the element if nothing was written inside it.                                                                               |

Paths use dots and indexes (`party.name`, `transactions[2].amount`); a leading `$.` starts from the root of the data
rather than the current item. Formats, applied left to right: `trim`, `upper`, `lower`, `truncate:N`, `date:FORMAT` (a
.NET custom date format, for example `date:yyyy-MM-ddTHH:mm:ss`), `number:FORMAT` (for example `number:0.00`) and
`map:codelist`. All formatting is culture invariant. A value that is not in its code list is an issue; the library never
guesses a code.

Text is always escaped; data cannot add markup. The template and schemas are read with DTDs prohibited and no external
resolution, so neither can pull in a file or a URL. Templates are limited to 64 levels, lists to 100,000 items.

## Issues

Each issue has a stage (Profile, Mapping or Schema), a code, a message, and where it applies the data path (Mapping) or
element path (Schema), with the line and position. The codes include `MissingRequired`, `UnknownCode`, `NotADate`,
`NotANumber`, `NotAList`, `NotAValue`, `UnknownFormat`, `BadAttributes`, `TemplateInvalid`, `NoSchema`, `SchemaInvalid`,
`SchemaViolation` and `XmlInvalid`.

## Which FIUs

The first profiles are for **Kuwait** (KwFIU), **Indonesia** (PPATK) and the **United Kingdom** (NCA), which use goAML,
and the **United States**.

<!-- TODO(docs): none of the four profiles is written yet. Each needs the schema the FIU issues to registered reporting entities (usually behind the FIU's portal registration, so it cannot be fetched here), and ideally an accepted sample report; the profile is then an afternoon's work and the library will say exactly where the template differs from the schema. The public goAML 5.0 schema copies found online carry no licence statement and are national variants, so none is vendored. -->

The United States is different in kind: FinCEN does not use goAML. Reports go through BSA E-Filing in FinCEN's own SAR
XML format. That schema is a work of the US Government and so can be shipped with the library, but it is a separate
profile with its own template, not a goAML one; the library handles it the same way.

## In Jube

Profiles live in the database (`CaseReportProfile`), one append-only version per import, and are imported by a holder of
the Approve Case Reports permission through `POST /api/CaseReportProfile/Import` with the template, the FIU's schema
files and the code lists. An import is refused, with the issues, if the template does not parse or a schema does not
compile. A report records which profile version it was started with. How a report is prepared, approved and filed from a
case is described in Working Regulatory Reports.

<!-- TODO(docs): not built: a page for managing profiles, per-tenant reporting-entity defaults, field roles and Case Key Journal transaction selection, a submission adapter that transmits the XML, and the FinCEN profile. -->

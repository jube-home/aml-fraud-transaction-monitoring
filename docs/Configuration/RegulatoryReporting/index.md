---
layout: default
title: Regulatory Reporting
nav_order: 11
parent: Configuration
---

# Regulatory Reporting

Where an investigation concludes that activity is suspicious, the institution is obliged to tell its regulator, and the regulator will accept the report only as a file of a precise shape. Regulatory Reporting is the means by which Jube writes that file. A few terms are needed: a financial intelligence unit, or FIU, is the national body that receives such reports; goAML is the software, published by the United Nations Office on Drugs and Crime, that most FIUs use to receive them, and by extension the XML format it expects; an XSD, or schema, is the file issued by an FIU stating exactly which elements a report may and must contain, against which a report can be checked by machine; and a profile is the description, held as data, of how the report of one FIU is to be assembled.

By way of example, an institution reporting to an FIU that uses goAML obtains the schema of that FIU, writes a template laying out the report with a directive at each place a value belongs, maps the country and currency names used in Jube to the codes of the FIU, and imports the three together as a profile named after the FIU. Thereafter an analyst preparing a report from a case selects that profile, and the report is built and checked against the schema before it can be filed.

`Jube.RegulatoryReport` builds and validates the XML that a financial intelligence unit (FIU) asks for when a suspicious activity or transaction report is filed. It is a stand-alone library, which is to say that it references nothing else in Jube, not the database, the web host nor any other Jube project, and it has no package dependencies, such that it can be packed and published to NuGet on its own (`dotnet pack Jube.RegulatoryReport`). It is licensed AGPL-3.0-or-later in the manner of the rest of the repository, and `PackageLicenseExpression` in its project file is to be changed before publishing if a different licence is wanted.

This page describes the library and the profile from which a report is built. How a report is prepared, approved and filed from a case is described in [Working Regulatory Reports](../CaseManagement/WorkingRegulatoryReports/index.html).

## Why a profile and not a goAML writer

goAML is a family, not one format. The UNODC publishes a baseline schema, and each FIU issues its own variant, having extra national fields, different code lists, stricter rules, and sometimes a different major version. A writer that hard-coded one version would be wrong for most FIUs and would have to be released again each time one of them changed. It follows that the library holds no goAML element names at all, and everything specific to an FIU instead lives in a profile, which is data:

| Part           | What it is                                                                                                                                                            |
|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `template.xml` | The report as the FIU expects it, with directives saying where each value comes from.                                                                                 |
| `schemas`      | The XSD files the FIU issued, exactly as issued. Nothing is embedded in the library: the FIU's copy is the authority and its terms of use are the deployer's to read. |
| `codeLists`    | Maps from Jube's values to the FIU's codes (countries, currencies, transaction types, indicators).                                                                    |
| `profile.json` | Names the above.                                                                                                                                                      |

A report is built by running the template over JSON data and then validating the result against the FIU's own schema, so a template that has drifted from the schema is caught the first time it is used, with the element path, line and position of the first problem.

## Where it lives

There is no page in the user interface for the administration of profiles. In Jube, a profile is held in the database and is imported through the API, as described under In Jube below, and it is thereafter offered for selection when a report is started in the Reports section of a case.

Outside of Jube, where the library is used on its own, a profile is a folder on disk, named by its `profile.json`.

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

| Value       | Description                                                                               | Example           |
|-------------|-------------------------------------------------------------------------------------------|-------------------|
| name        | The name of the profile. Required.                                                        | example-fiu       |
| description | A description of the profile. Optional.                                                   |                   |
| template    | The file holding the template, relative to the profile folder. Required.                  | template.xml      |
| schemas     | The XSD files the FIU issued, relative to the profile folder.                             | fiu-schema.xsd    |
| codeLists   | Each code list by name, being a map from the value held in Jube to the code of the FIU.   | countries         |

Every file must be inside the profile folder, and a file named outside of it is refused. The library is then used as follows:

| Call                                    | Description                                                                                         |
|-----------------------------------------|-----------------------------------------------------------------------------------------------------|
| `ReportProfileLoader.Load(folder)`      | Reads the profile from the folder.                                                                  |
| `ReportBuilder.Build(profile, data)`    | Returns the XML, its SHA-256 and a list of issues.                                                  |
| `ReportBuilder.Validate(profile, xml)`  | Checks XML produced elsewhere against the schemas of the profile.                                   |
| `ReportBuilder.CheckProfile(profile)`   | Checks that the template can be read and the schemas compile, without building a report.            |

No XML is returned while there are mapping issues. A report has succeeded only where XML was returned and there are no issues of any kind.

## The template

The template is an ordinary XML document in the namespace of the FIU, plus the directive namespace `urn:jube:regulatory-report:template:1` (conventionally `jr`). Directive attributes never appear in the output.

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

Paths use dots and indexes (`party.name`, `transactions[2].amount`), and a leading `$.` starts from the root of the data rather than the current item.

Formats are applied left to right, and all formatting is culture invariant:

| Format          | Result                                                                                         | Example                      |
|-----------------|------------------------------------------------------------------------------------------------|------------------------------|
| `trim`          | The value without leading and trailing white space.                                            |                              |
| `upper`         | The value in capitals.                                                                         |                              |
| `lower`         | The value in lower case.                                                                       |                              |
| `truncate:N`    | The value cut to N characters.                                                                 | `truncate:50`                |
| `date:FORMAT`   | The value as a date, in a .NET custom date format. A date without an offset is taken as UTC.   | `date:yyyy-MM-ddTHH:mm:ss`   |
| `number:FORMAT` | The value as a number, in a .NET custom numeric format.                                        | `number:0.00`                |
| `map:codelist`  | The code for the value, taken from the named code list of the profile.                         | `map:countries`              |

A value that is not in its code list is an issue, as the library never guesses a code.

### Example

Taking the following data:

```json
{
  "entity": { "name": "Example Bank", "country": "United Kingdom" },
  "transactions": [
    { "reference": "TX-1", "amount": 1250.5, "date": "2026-10-20T10:30:00Z" }
  ]
}
```

And the following template, with the namespace of the FIU left out for brevity:

```xml
<report xmlns:jr="urn:jube:regulatory-report:template:1">
  <entity_name jr:value="entity.name|trim|truncate:255" jr:required="true"/>
  <country jr:value="entity.country|map:countries" jr:required="true"/>
  <transactions jr:prune="true">
    <transaction jr:each="transactions">
      <reference jr:value="reference"/>
      <amount jr:value="amount|number:0.00"/>
      <date jr:value="date|date:yyyy-MM-ddTHH:mm:ss"/>
      <comments jr:value="comments"/>
    </transaction>
  </transactions>
</report>
```

The report is built as follows, the comments element being left out as it has no value:

```xml
<?xml version="1.0" encoding="utf-8"?>
<report>
  <entity_name>Example Bank</entity_name>
  <country>GB</country>
  <transactions>
    <transaction>
      <reference>TX-1</reference>
      <amount>1250.50</amount>
      <date>2026-10-20T10:30:00</date>
    </transaction>
  </transactions>
</report>
```

## Limits

| Limit                                           | Value     |
|-------------------------------------------------|-----------|
| Characters in a template                        | 1,000,000 |
| Levels to which a template nests                | 64        |
| Items in a list repeated by `jr:each`           | 100,000   |
| Elements written to a report                    | 1,000,000 |

## Issues

Each issue has a stage (Profile, Mapping or Schema), a code, a message, and where it applies the data path (Mapping) or element path (Schema), with the line and position. The codes are as follows:

| Stage   | Code              | Meaning                                                                          |
|---------|-------------------|----------------------------------------------------------------------------------|
| Profile | `TemplateInvalid` | The template cannot be read as XML, or is too large.                             |
| Profile | `NoSchema`        | The profile has no schema to validate against.                                   |
| Profile | `SchemaInvalid`   | A schema cannot be read or does not compile.                                     |
| Mapping | `NoData`          | There is no report data.                                                         |
| Mapping | `MissingRequired` | A required value or list is missing or empty.                                    |
| Mapping | `UnknownCode`     | A value is not in its code list.                                                 |
| Mapping | `UnknownCodeList` | A code list named by `map` is not in the profile.                                |
| Mapping | `NotADate`        | A value given to `date` is not a date.                                           |
| Mapping | `NotANumber`      | A value given to `number` is not a number.                                       |
| Mapping | `NotAList`        | A path given to `jr:each` is not a list.                                         |
| Mapping | `NotAValue`       | A path given to `jr:value` is a list or an object, not a single value.           |
| Mapping | `UnknownFormat`   | A format is not known.                                                           |
| Mapping | `BadAttributes`   | An entry of `jr:attributes` is not of the form name=expression.                  |
| Mapping | `TemplateTooDeep` | The template nests deeper than 64 levels.                                        |
| Mapping | `TooManyItems`    | A list has more than 100,000 items.                                              |
| Mapping | `OutputTooLarge`  | The report has too many elements.                                                |
| Mapping | `TemplateFailed`  | The template could not be applied to the data.                                   |
| Mapping | `NothingRendered` | The template produced no report.                                                 |
| Mapping | `NotSerialisable` | The data contains characters that cannot be written as XML.                      |
| Schema  | `SchemaViolation` | The report does not conform to the schema of the FIU.                            |
| Schema  | `XmlInvalid`      | The XML given for validation cannot be read.                                     |

## Which FIUs

No profile is shipped with Jube. The first profiles intended are for Kuwait (KwFIU), Indonesia (PPATK) and the United Kingdom (NCA), which use goAML, and the United States.

A profile needs the schema that the FIU issues to registered reporting entities, which is usually behind the portal registration of the FIU and so cannot be distributed here, and ideally a sample report that the FIU has accepted. Given those, the writing of a profile is a short piece of work, keeping in mind that the library will state exactly where the template differs from the schema. The copies of the public goAML 5.0 schema to be found online carry no licence statement and are national variants, hence none is included.

The United States is different in kind, as FinCEN does not use goAML. Reports go through BSA E-Filing in FinCEN's own SAR XML format. That schema is a work of the US Government and so can be distributed with the library, yet it is a separate profile with its own template, not a goAML one, and the library handles it in the same manner.

## In Jube

Profiles live in the database, in the CaseReportProfile table, held separately for each tenant, with one append-only version per import. A profile is imported by a POST to `/api/CaseReportProfile/Import`, with the template, the schema files of the FIU and the code lists in the body:

```json
{
  "name": "example-fiu",
  "description": "Optional.",
  "templateXml": "<report xmlns:jr=\"urn:jube:regulatory-report:template:1\">...</report>",
  "schemaDocuments": { "fiu-schema.xsd": "<xs:schema>...</xs:schema>" },
  "codeLists": { "countries": { "United Kingdom": "GB", "Kuwait": "KW" } }
}
```

An import is refused, with the issues, if the template does not parse or a schema does not compile, and nothing is stored. Otherwise the version imported is returned, being one greater than the version before it. The profiles available are listed by a GET to `/api/CaseReportProfile`, and the current version of one is returned by a GET to `/api/CaseReportProfile/{name}`.

A report records which profile version it was started with.

### Validation

An import is rejected with 400 where:

* The name is empty or is longer than 100 characters.
* The description is longer than 500 characters.
* The template is empty or is longer than 1,000,000 characters.
* There is no schema file, or there are more than 20.
* A schema file is longer than 2,000,000 characters.

An import that collides with another import of the same profile, made at the same moment, is refused with 409 and is to be tried again.

### What is not included

The following are not available at present:

* A page for the administration of profiles.
* Defaults for the reporting entity held for each tenant.
* Field roles, and the selection of transactions from the Case Key Journal.
* A submission adapter that transmits the XML to the FIU.
* The FinCEN profile, and the profiles of the other FIUs named above.

## Permissions and security

Importing a profile, and reading a profile in full, requires permission 76, Approve Case Reports. Listing the profiles available requires permission 75, File Case Reports, permission 76, or permission 77, View Case Reports. A user without the permission receives 403, and the import also needs the antiforgery token for cookie sessions.

A profile belongs to the tenant of the user having imported it, and a profile of another tenant behaves as one that does not exist.

Text is always escaped, such that data cannot add markup to a report. The template and the schemas are read with DTDs prohibited and with no external resolution, so neither can pull in a file or a URL.

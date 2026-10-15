---
layout: default
title: Extensions and Helpers Agent Tools
nav_order: 1
parent: Service Layer & DTO Model
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Extensions and Helpers Agent Tools

An agent should not work out a weekday, a percentage change, an IBAN checksum or a time zone offset by reasoning,
because it will sometimes be wrong. The service layer therefore offers 178 small, exact, pure functions as agent tools,
in 22 groups of 7 to 10. Each group is meant to be handed to one agent. Statistical, numerical and machine-learning
tools are not here; they are the separate Advanced Analytics tool layer.

## Where the logic lives

* **`Jube.Dictionary.Extensions`** holds the logic as extension methods, which are also the rule language's tokens (see
  [Rule Compilation Algorithm](../../../Configuration/Models/RuleCompilationAlgorithm/index.html#reference-identifier-and-utility-extensions)).
  A rule and an agent tool therefore run the same code and cannot disagree.
* **`Jube.Service.Extensions.<Group>`** is a thin facade over those methods. Each tool adds an LLM-oriented description,
  input limits and plain-English errors, and sometimes combines several extension calls into one small result record,
  such as `ValidateIban`.
* **`Jube.Service.Helpers.<Group>`** holds the few functions that are not extension methods: reading the clock, creating
  UUIDs, and set operations over lists of text.

## Contract

* Tools are marked `[FunctionToolGroup(name, description)]` on the class and `[FunctionTool(name)]` with `[Description]`
  on each method and parameter. Tests hold the decoration to a standard: descriptions of at least 30 characters (8 for
  parameters), globally unique tool names, a group size of 7 to 20, and a namespace that ends in the group name.
  Discovering the groups and binding them as agent functions is the agent host's job, beside the existing service tool
  binder, and is not part of this layer.
* **Errors are for the model.** Invalid input, and any result that would be undefined (division by zero, an unknown
  currency, a skipped local time), throws `ArgumentException` with the reason and, where useful, the valid options. The
  hosting middleware should return that message to the model as the tool result. Results never contain `NaN` or
  infinity.
* **Bounded input.** Text is limited to 100,000 characters (1,000 for names in fuzzy matching and 1,000,000 for JSON/XML
  documents), and lists to 10,000 items. Regular expressions time out after 250 ms, and XML with a DTD is refused.
* **Pure.** A unit test reads the compiled IL of every facade and rule-library type and fails on any reference to file,
  network, process, environment, dynamic loading, `Activator`, `Reflection.Emit`, native interop or Win32, or on any
  platform invoke. Extension facades are also barred from the clock and from random sources, so they always return the
  same answer for the same input; only `Helpers.ClockAndIdentifiers` reads the clock, through an injectable
  `TimeProvider`.
* **Host.** Linux containers only. Time zones are IANA ids resolved from the image's tzdata, and Unicode normalisation
  and international host names use ICU; the runtime image carries both.
* Every tool is named by at least one test through `[Trait("Covers", "<ToolName>")]`, and a catalogue test fails when
  one is not.

## Extension groups

### CalendarFacts

`Jube.Service.Extensions.CalendarFacts` — Calendar facts about a date: weekday, week number, quarter, fiscal period,
month length, leap years, business hours and the start and end of its day, week, month, quarter or year.

| Tool                    | What it does                                                                                                                                                                                                                                                                              |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DescribeDate`          | Calendar facts about one date in a single call: weekday name, day of year, ISO week number, calendar quarter, days in the month, whether it is a weekend, the last day of its month, and whether its year is a leap year. Use it instead of working out weekdays or week numbers by hand. |
| `PeriodBoundaries`      | First and last instant of the day, week, month, quarter or year containing a date; the week can start on any weekday. End is the last tick of the period, so an instant is inside when Start <= instant <= End.                                                                           |
| `FiscalPeriod`          | Fiscal year and fiscal quarter of a date when the fiscal year starts in a given month. The fiscal year is named after the calendar year in which it ends; with an April start, 2026-08-17 is fiscal 2027 quarter 2.                                                                       |
| `DaysInMonth`           | Number of days in a given month of a given year, allowing for leap years; February 2028 has 29.                                                                                                                                                                                           |
| `IsLeapYear`            | Whether a year is a Gregorian leap year: divisible by 4, except centuries not divisible by 400; 2000 and 2028 are, 1900 and 2026 are not.                                                                                                                                                 |
| `IsWithinBusinessHours` | Whether the clock time of a date-time falls in business hours, from startHour inclusive to endHour exclusive, optionally on weekdays only. Convert to the customer's local time zone first when that matters.                                                                             |
| `IsSameCalendarDay`     | Whether two date-times fall on the same calendar date, ignoring the time of day. Convert both to the same time zone first.                                                                                                                                                                |

### CurrencyAndCountry

`Jube.Service.Extensions.CurrencyAndCountry` — ISO 4217 currency and ISO 3166 country reference data with exact
minor-unit money conversion, from tables embedded in the product.

| Tool                      | What it does                                                                                                                                                 |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CurrencyMinorUnitDigits` | Number of decimal places an ISO 4217 currency uses: USD 2, JPY 0, KWD 3, CLF 4. Use it before storing, rounding or comparing money in minor units.           |
| `IsValidCurrencyCode`     | Whether a three-letter code is an active ISO 4217 currency, case-insensitive. Use it to check payment currency fields.                                       |
| `ToMinorUnits`            | Converts a money amount to whole minor units for a currency, exactly and rounding halves away from zero; 19.99 USD is 1999 cents and 1500 JPY is 1500.       |
| `FromMinorUnits`          | Converts whole minor units back to a money amount for a currency; 1999 USD cents is 19.99 and 1234 KWD fils is 1.234.                                        |
| `RoundToCurrency`         | Rounds a money amount to the precision of its currency, halves away from zero, using exact decimal arithmetic; 1234.5678 JPY is 1235 and in BHD is 1234.568. |
| `ConvertAtExchangeRate`   | Multiplies an amount by a supplied exchange rate; 100 at 1.25 is 125. This tool knows no live rates, so the rate must come from the conversation or data.    |
| `IsValidCountryCode`      | Whether a two-letter code is an ISO 3166-1 alpha-2 country code, case-insensitive; GB is valid and UK is not. Use it to check country fields.                |
| `DescribeCountry`         | Looks up an ISO 3166-1 country by alpha-2 or alpha-3 code and returns both codes and the English short name; DEU gives DE, DEU, Germany.                     |

### DateArithmetic

`Jube.Service.Extensions.DateArithmetic` — Adds time to dates and measures the time between them exactly, including
business days, whole months and years, ages as of a date, and interval overlap.

| Tool                  | What it does                                                                                                                                                                                                                 |
|-----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddToDate`           | Adds (or with a negative amount subtracts) a number of units to a date-time. Months and Years keep the day where possible and clamp to month end, so 2026-01-31 plus 1 month is 2026-02-28.                                  |
| `AddBusinessDays`     | Moves a date forward (or backward, if negative) by a number of Monday-to-Friday business days, skipping weekends; Friday plus 1 is Monday. Public holidays are not known and are not skipped.                                |
| `BusinessDaysBetween` | Number of Monday-to-Friday days after the start date up to and including the end date; negative when the end is earlier. Wednesday to the following Wednesday is 5. Public holidays are not known.                           |
| `TimeBetween`         | Exact elapsed time from a start to an end date-time in several units at once: total days, hours, minutes and seconds (fractional), plus completed whole months and whole years. Values are negative when the end is earlier. |
| `AgeAsOf`             | Age in completed years and in days of someone or something born on a date, measured at an as-of date; born 1990-06-15 is 35 on 2026-06-14 and 36 the next day. Use CurrentUtcTime for today's date.                          |
| `IntervalsOverlap`    | Whether two time intervals overlap. Intervals are half-open (start included, end excluded), so one ending exactly when the other starts does not overlap.                                                                    |
| `IsDateBetween`       | Whether a date-time lies within an inclusive range from a start to an end. Use it for effective dates, statement periods and lookback windows.                                                                               |

### EncodingAndHashing

`Jube.Service.Extensions.EncodingAndHashing` — Encodes and decodes text (Base64, Base64Url, hex, URL, HTML, XML,
JavaScript) and computes digests, HMACs and checksums over UTF-8 text.

| Tool            | What it does                                                                                                                                                                                                                                 |
|-----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `EncodeText`    | Encodes UTF-8 text in a scheme: Base64, Base64Url (URL-safe, no padding), Hex (lowercase), Url (form encoding, spaces as +), Html, XmlEscape or JavaScript (string-literal escaping). Use DecodeText to reverse.                             |
| `DecodeText`    | Decodes text from a scheme back to UTF-8 text: Base64, Base64Url, Hex (an optional 0x prefix is allowed), Url or Html. Malformed input is refused with the reason.                                                                           |
| `HashText`      | Lowercase hex digest of the UTF-8 bytes of a text with Sha256 (default), Sha384, Sha512, Sha1 or Md5. Sha1 and Md5 are for matching legacy values only, never for security. Use it to compare or pseudonymise values without revealing them. |
| `HmacText`      | Lowercase hex HMAC of the UTF-8 bytes of a text under a secret key, with Sha256 (default) or Sha512. Use it to verify a signed payload or to pseudonymise with a key. Never put a production secret into a conversation.                     |
| `Crc32Checksum` | CRC-32 (IEEE) checksum of the UTF-8 bytes of a text as eight lowercase hex digits; '123456789' gives cbf43926. For detecting accidental change, not tampering.                                                                               |
| `HashesMatch`   | Whether a text hashes to a given hex digest with an algorithm, comparing case-insensitively. Use it to check a value against a stored fingerprint without ever handling the original.                                                        |
| `DescribeUuid`  | Whether a text is a UUID (GUID) and, if so, its canonical lowercase form and version number; version 4 is random and version 7 is time-ordered. SWIFT UETRs are version 4 UUIDs.                                                             |

### FinancialIdentifiers

`Jube.Service.Extensions.FinancialIdentifiers` — Validates and dissects payment and securities identifiers: IBAN,
BIC/SWIFT, card numbers and expiry, Luhn check digits, ABA routing numbers, ISIN, LEI, CUSIP and SEDOL.

| Tool                      | What it does                                                                                                                                                                                                                                       |
|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValidateIban`            | Checks an IBAN's structure and ISO 7064 mod-97 check digits (spaces allowed) and, when valid, returns its country, check digits, basic bank account number and print format in groups of four. A valid checksum does not prove the account exists. |
| `ValidateBic`             | Checks the format of a BIC (SWIFT code, 8 or 11 characters) and, when valid, splits it into institution, country, location and branch codes; an 8-character BIC is the primary office, branch XXX.                                                 |
| `ValidateCardNumber`      | Checks a payment card number with the Luhn algorithm (spaces and hyphens allowed) and reports the card network from its prefix, the 6 and 8 digit issuer identification numbers and a masked form showing only the last four digits.               |
| `LuhnCheckDigit`          | The Luhn check digit that would complete a digit string; 7992739871 needs 3. Use it to generate or explain card, IMEI and account check digits.                                                                                                    |
| `IsCardExpiryValid`       | Whether a card expiry written MM/YY or MM/YYYY is still valid at a given date; a card is valid through the last day of its expiry month. Use CurrentUtcTime for today.                                                                             |
| `IsValidAbaRoutingNumber` | Whether a nine-digit US ABA routing transit number passes its 3-7-1 weighted checksum; 021000021 does.                                                                                                                                             |
| `IsValidIsin`             | Whether a 12-character ISIN (International Securities Identification Number) has a valid country prefix shape and Luhn check digit; US0378331005 does.                                                                                             |
| `IsValidLei`              | Whether a 20-character LEI (Legal Entity Identifier) passes its ISO 17442 mod-97 check digits. A valid checksum does not prove the entity is registered.                                                                                           |
| `IsValidCusip`            | Whether a nine-character CUSIP (North American security identifier) has a valid check digit; 037833100 does.                                                                                                                                       |
| `IsValidSedol`            | Whether a seven-character SEDOL (London Stock Exchange security identifier) has a valid weighted check digit and no vowels; 0263494 does.                                                                                                          |

### Geospatial

`Jube.Service.Extensions.Geospatial` — Great-circle geometry on latitude and longitude: distances, radius checks,
implied travel speed for impossible-travel analysis, bearings and geohashes.

| Tool                    | What it does                                                                                                                                                                                                                      |
|-------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DistanceBetweenPoints` | Great-circle (haversine) distance between two points given as latitude and longitude in decimal degrees, in both kilometres and miles.                                                                                            |
| `IsWithinRadius`        | Whether a point lies within a radius in kilometres of a centre point, by great-circle distance. Use it for geofences around a home address or merchant.                                                                           |
| `ImpliedTravelSpeed`    | Speed in km/h someone would need to travel between two locations in the time between two events. Over about 900 km/h (a commercial flight) suggests impossible travel, such as the same card used in two countries an hour apart. |
| `InitialBearing`        | Initial compass bearing in degrees (0 north, 90 east) for the great-circle route from a first point to a second.                                                                                                                  |
| `EncodeGeohash`         | Encodes a latitude and longitude as a geohash of 1 to 12 characters; longer is more precise (6 is about 1.2 km, 9 about 5 m). Points sharing a prefix are near each other, which makes geohashes good grouping keys.              |
| `DecodeGeohash`         | The centre latitude and longitude of the cell a geohash names.                                                                                                                                                                    |
| `IsValidCoordinate`     | Whether a latitude and longitude pair is in range: latitude -90 to 90 and longitude -180 to 180. Swapped coordinates often fail this check.                                                                                       |

### ListScreening

`Jube.Service.Extensions.ListScreening` — Screens a name or text against a list, such as a watch list or allow list,
with configurable fuzzy matching, and reports the best match, its score and how many entries matched.

| Tool                       | What it does                                                                                                                                                                                                                                                           |
|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ScreenAgainstList`        | Screens one value against a list with a fuzzy rule and reports whether anything matched, how many entries matched, the best matching entry and its score from 0 to 1. The main tool for checking a name against a watch list or a counterparty against known entities. |
| `IsFuzzilyInList`          | Whether a value fuzzily equals any entry of a list under a fuzzy rule. Use ScreenAgainstList when the matching entry or score is also wanted.                                                                                                                          |
| `ContainsFuzzilyAnyInList` | Whether any list entry fuzzily appears inside a longer text, such as a sanctioned name inside a payment narrative. The within modifier is applied automatically.                                                                                                       |
| `IsExactlyInList`          | Whether a value equals any list entry exactly, optionally ignoring case; no fuzziness. Use it for code lists such as country or merchant category codes.                                                                                                               |
| `InitialsMatchInList`      | Whether a name matches any list entry when initials are allowed to stand for whole words; 'J R R Tolkien' matches 'John Ronald Reuel Tolkien'.                                                                                                                         |
| `SoundsLikeAnyInList`      | Whether a name sounds like any list entry word by word under Soundex after case and accent folding. Phonetic only, so pair it with an edit-distance check.                                                                                                             |
| `CheckFuzzyOptions`        | Checks a fuzzy options string and returns an empty text when it is valid, or the reason it is not. Use it before saving a list-matching rule.                                                                                                                          |

### NameSimilarity

`Jube.Service.Extensions.NameSimilarity` — Scores how alike two names or references are with edit-distance, phonetic and
token algorithms, and normalises names for matching; for pairwise comparison rather than screening a list.

| Tool                       | What it does                                                                                                                                                                                                                                                                                                  |
|----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CompareNames`             | Scores two names with every similarity algorithm at once: Levenshtein and Damerau edit distances, normalised similarity, Jaro-Winkler, Dice bigrams, token-sort, token-set (0 to 1, higher is closer), and whether they sound alike by Soundex. Use it to explain why two parties may or may not be the same. |
| `LevenshteinDistance`      | Minimum number of single-character insertions, deletions or substitutions turning one text into another, case-sensitive; 'kitten' to 'sitting' is 3.                                                                                                                                                          |
| `DamerauDistance`          | Edit distance that also counts swapping two adjacent characters as one edit, so typing transpositions score 1; 'abcd' to 'abdc' is 1.                                                                                                                                                                         |
| `JaroWinklerSimilarity`    | Jaro-Winkler similarity from 0 to 1, rewarding a shared prefix; well suited to short personal names. Case-sensitive; 0.92 and above is a typical match threshold.                                                                                                                                             |
| `SoundexCode`              | The four-character American Soundex code of a word, grouping names that sound alike; Robert and Rupert are both R163.                                                                                                                                                                                         |
| `NamesSoundAlike`          | Whether two names sound alike word by word under Soundex after case and accent folding; 'Robert Smith' and 'Rupert Smyth' do.                                                                                                                                                                                 |
| `InitialsOf`               | The initials of a name, one uppercase letter per word; 'john ronald reuel tolkien' gives JRRT.                                                                                                                                                                                                                |
| `NormaliseNameForMatching` | The canonical form used for name matching: lowercase, accents and punctuation removed, leading titles (Mr, Dr, Frau) and trailing company suffixes (Ltd, GmbH, LLC) dropped; 'Dr. José Núñez' gives 'jose nunez'.                                                                                             |
| `NamesMatchWithOptions`    | Whether two names match under a configurable fuzzy rule, the same rule language used by list screening, so a pair can be tested before a list rule is written.                                                                                                                                                |

### OnlineIdentifiers

`Jube.Service.Extensions.OnlineIdentifiers` — Validates and dissects digital identifiers: email addresses and
plus-aliases, phone numbers, domain names, URLs, IPv4 and IPv6 addresses and CIDR network ranges. Pure parsing; nothing
is looked up on the network.

| Tool                                 | What it does                                                                                                                                                                                                                                                                                      |
|--------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValidateEmailAddress`               | Checks an email address's syntax and splits it into local part, domain and top-level domain, plus the plus-alias-normalised address (ann+promo@x.com becomes ann@x.com), which exposes one person registering many aliases. Syntax only; no mailbox lookup.                                       |
| `EmailAddressesMatchIgnoringAliases` | Whether two email addresses reach the same mailbox once case and plus-aliases are ignored; ann+1@x.com and ANN@x.com match.                                                                                                                                                                       |
| `ValidatePhoneNumber`                | Reduces a phone number to digits (keeping a leading + and turning a 00 prefix into +) and reports whether that is a valid E.164 international number (+ then up to 15 digits, not starting 0). Format only; no carrier lookup.                                                                    |
| `DescribeUrl`                        | Parses an absolute URL and returns its lowercase scheme and host (internationalised hosts in punycode) and its top-level domain; file and network-share URLs are refused. Nothing is fetched.                                                                                                     |
| `IsValidDomainName`                  | Whether a text is a syntactically valid internet domain name of at least two labels, each 1 to 63 letters, digits or hyphens, not starting or ending with a hyphen. No DNS lookup.                                                                                                                |
| `DescribeIpAddress`                  | Parses an IPv4 or IPv6 address and classifies it: canonical form, family, private (RFC 1918, carrier-grade NAT, unique local), loopback, reserved (link-local, multicast, documentation and similar) or public, and the IPv4 address as a number. IPv4-mapped IPv6 addresses are treated as IPv4. |
| `IsIpAddressInRange`                 | Whether an IP address falls inside a CIDR network range such as 10.0.0.0/8 or 2001:db8::/32. Addresses and ranges of different families never match.                                                                                                                                              |
| `DescribeIpRange`                    | The first and last address and the number of addresses in a CIDR network range; 192.168.1.77/24 spans 192.168.1.0 to 192.168.1.255, 256 addresses.                                                                                                                                                |
| `NumberToIPv4Address`                | Writes a number from 0 to 4294967295 as a dotted IPv4 address; 3232235777 is 192.168.1.1. The reverse is in DescribeIpAddress.                                                                                                                                                                    |

### PatternMatching

`Jube.Service.Extensions.PatternMatching` — Regular-expression and wildcard matching with a hard time limit per call:
test, find, capture, count, replace and escape.

| Tool                | What it does                                                                                                                                                                                                         |
|---------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `RegexIsMatch`      | .NET regular expression test: whether the pattern matches anywhere in the text. Matching is time-limited, so a pattern that would backtrack catastrophically is refused rather than hanging.                         |
| `RegexFindAll`      | Every match of a .NET regular expression in a text, in order, as the matched texts (up to 10000). Matching is time-limited.                                                                                          |
| `RegexCapture`      | The text captured by a numbered group of the first match of a .NET regular expression, or empty if there is no match; group 1 of 'order-([0-9]+)' in 'order-12345-x' is '12345'. Group 0 is the whole match.         |
| `RegexCountMatches` | Number of non-overlapping matches of a .NET regular expression in a text. Matching is time-limited.                                                                                                                  |
| `RegexReplaceAll`   | Replaces every match of a .NET regular expression; the replacement may use $1, $2 for groups. 'a1b22' with [0-9]+ replaced by # is 'a#b#'. Matching is time-limited.                                                 |
| `EscapeForRegex`    | Escapes a literal text so it can be placed inside a regular expression and match itself exactly; '1.5*2' becomes '1\.5\*2'.                                                                                          |
| `WildcardIsMatch`   | Whether a whole text matches a simple wildcard pattern: * is any run of characters, ? is one character, # is one digit and [abc] or [!abc] is a character set; 'INV-2026-*' matches 'INV-2026-0042'. Case-sensitive. |

### PercentagesAndChanges

`Jube.Service.Extensions.PercentagesAndChanges` — Percentages, ratios and growth between two figures, such as the change
from last month or the share of a total, computed exactly.

| Tool                          | What it does                                                                                                                                                     |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `PercentOfTotal`              | What percent a part is of a total; 25 of 200 is 12.5. Use it for shares such as declined spend as a percent of total spend.                                      |
| `PercentageChange`            | Percentage change from a previous value to a current value; 80 to 100 is +25 and 100 to 80 is -20. Use it for period-on-period movement.                         |
| `PercentDifference`           | Symmetric percent difference between two values relative to their average; 90 and 110 differ by 20 percent. Use it when neither value is the baseline.           |
| `RatioOfValues`               | One value divided by another as a plain ratio; 30 over 120 is 0.25. Use it for velocity ratios, coverage and other quotients.                                    |
| `LogRatio`                    | Natural log of numerator over denominator, a symmetric measure of relative size: doubling is +0.693 and halving is -0.693. Both values must be positive.         |
| `CompoundGrowthRate`          | Average compound growth rate per period between a starting and an ending value, as a fraction; 100 growing to 121 over 2 periods is 0.1 (10 percent per period). |
| `AddPercentage`               | Increases an amount by a percentage; 100 plus 20 percent is 120. Use it for tax, fees, uplifts and mark-ups.                                                     |
| `SubtractPercentage`          | Decreases an amount by a percentage; 100 less 20 percent is 80. Use it for discounts, haircuts and deductions.                                                   |
| `AmountBeforePercentageAdded` | Recovers the original amount before a percentage was added; 120 including 20 percent was 100 before it. Use it to back tax or fees out of a gross amount.        |
| `IsWithinPercentOf`           | Whether a value lies within a percentage tolerance of a target, inclusive; 104 is within 5 percent of 100. Use it for near-match checks on amounts.              |

### RoundingAndPowers

`Jube.Service.Extensions.RoundingAndPowers` — Exact rounding, clamping, powers, roots and logarithms of single numbers,
so an answer never depends on mental arithmetic.

| Tool                      | What it does                                                                                                                                                                                                                                                  |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `RoundNumber`             | Rounds a number to a number of decimal places using an explicit rule for halves. Use it whenever a figure must be presented or compared at a fixed precision; AwayFromZero is the everyday rule (2.5 becomes 3), ToEven is banker's rounding (2.5 becomes 2). |
| `RoundToNearestIncrement` | Rounds a number to the nearest multiple of an increment, halves away from zero; for example 1234 to the nearest 50 is 1250, and 0.125 to the nearest 0.05 is 0.15. Use it for price points, reporting bands and cash denominations.                           |
| `RoundDownToWholeNumber`  | Largest whole number less than or equal to the value (the floor); -2.5 becomes -3. Use it for completed units such as whole days or whole bands.                                                                                                              |
| `RoundUpToWholeNumber`    | Smallest whole number greater than or equal to the value (the ceiling); 2.1 becomes 3. Use it for counts of units needed to cover an amount.                                                                                                                  |
| `TruncateToWholeNumber`   | Drops the fractional part of a number, moving towards zero; -2.7 becomes -2 and 2.7 becomes 2. Use it when the integer part is wanted regardless of sign.                                                                                                     |
| `ClampNumber`             | Limits a number to an inclusive range, returning the minimum when below it and the maximum when above it. Use it to cap scores, limits or percentages.                                                                                                        |
| `RaiseToPower`            | Raises a base to an exponent, including fractional and negative exponents; 2 to the 10 is 1024 and 9 to the 0.5 is 3. Use it for compounding and scale factors.                                                                                               |
| `NthRoot`                 | The real n-th root of a number; the cube root of 27 is 3 and the cube root of -8 is -2. An even root of a negative number has no real answer and is refused.                                                                                                  |
| `Logarithm`               | Logarithm of a positive number in any base; base 10 of 1000 is 3, base 2 of 8 is 3, and base e (2.718281828) gives the natural log. Use it to compare orders of magnitude.                                                                                    |

### StructuredData

`Jube.Service.Extensions.StructuredData` — Reads values out of JSON and XML documents by path without loading them into
the conversation, and checks that documents are well formed. DTDs and external entities are refused.

| Tool                    | What it does                                                                                                                                                                                                                                                                        |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IsValidJson`           | Whether a text is well-formed JSON (up to 1,000,000 characters and 64 levels deep).                                                                                                                                                                                                 |
| `JsonValueAtPath`       | The value at a path in a JSON document as text: strings unquoted, anything else as compact JSON. Paths use dots and brackets with an optional $, such as $.customer.name, customer.tags[0], items[-1] (last) or $['odd key']. Empty when the path is missing.                       |
| `JsonNumberAtPath`      | The number at a path in a JSON document; refused when the path is missing or not a number. Uses the same paths as JsonValueAtPath.                                                                                                                                                  |
| `JsonArrayLengthAtPath` | Number of elements in the array at a path in a JSON document; -1 when the path is missing or not an array.                                                                                                                                                                          |
| `JsonHasPath`           | Whether a path exists and is not null in a JSON document; the same paths as JsonValueAtPath.                                                                                                                                                                                        |
| `MinifyJson`            | Removes insignificant whitespace from a JSON document; refused when the text is not valid JSON.                                                                                                                                                                                     |
| `IsValidXml`            | Whether a text is a well-formed XML document without a DTD (up to 1,000,000 characters). Documents declaring a DTD or entities are refused for safety.                                                                                                                              |
| `XmlValueAtXPath`       | Text content of the first node selected by an XPath 1.0 expression, such as /Document/GrpHdr/MsgId or //Nm, in an XML document such as an ISO 20022 payment message; empty when nothing is selected. Namespaced documents need local-name(), for example //*[local-name()='MsgId']. |

### TextCleaning

`Jube.Service.Extensions.TextCleaning` — Cleans and normalises text before comparison or storage: accents, whitespace,
control characters, Unicode forms, keeping or removing letters and digits, and URL slugs.

| Tool                      | What it does                                                                                                                                                                                                                                                       |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `RemoveAccents`           | Removes accents and other combining marks while keeping the base letters; 'Crème Brûlée' becomes 'Creme Brulee'. Use it before comparing names from different systems.                                                                                             |
| `CollapseWhitespace`      | Trims the text and replaces every run of spaces, tabs and line breaks with a single space.                                                                                                                                                                         |
| `RemoveControlCharacters` | Removes invisible control characters such as NUL and BEL, keeping tabs and line breaks. Use it on data pasted from other systems before display or matching.                                                                                                       |
| `NormaliseUnicode`        | Applies a Unicode normalisation form: NFC composes accents (the usual storage form), NFD decomposes them, NFKC and NFKD also fold compatibility characters such as the 'fi' ligature and full-width letters to their plain forms, which defeats look-alike tricks. |
| `KeepOnlyDigits`          | Keeps only the numeric characters of a text, dropping everything else; '+44 (0) 7911-123456' gives '4407911123456'.                                                                                                                                                |
| `KeepOnlyLetters`         | Keeps only the letters of a text, in any alphabet, dropping digits, spaces and punctuation; 'AB-12 cd' gives 'ABcd'.                                                                                                                                               |
| `RemoveDigits`            | Removes every numeric character from a text, keeping letters, spaces and punctuation.                                                                                                                                                                              |
| `RemoveLetters`           | Removes every letter from a text, keeping digits, spaces and punctuation.                                                                                                                                                                                          |
| `ToSlug`                  | Turns text into a lowercase ASCII slug of letters and digits joined by hyphens; 'Crème Brûlée & Co.!' becomes 'creme-brulee-co'. Useful for stable keys and file-safe names.                                                                                       |

### TextEditing

`Jube.Service.Extensions.TextEditing` — Edits text exactly: case, reversing, repeating, truncating, taking the left or
right characters, masking and replacing the first or last occurrence.

| Tool                     | What it does                                                                                                                                                                           |
|--------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ToTitleCase`            | Capitalises the first letter of each word and lowercases the rest, except words already all in capitals, which are kept as acronyms; 'john SMITH-jones' becomes 'John SMITH-Jones'.    |
| `ReverseText`            | Reverses the order of the characters in a text; 'abc' becomes 'cba'.                                                                                                                   |
| `RepeatText`             | Repeats a text a number of times end to end; 'ab' three times is 'ababab'. The result is limited to 100000 characters.                                                                 |
| `TruncateText`           | Shortens a text to at most a maximum length, ending with a suffix (default ...) when it was cut; texts already short enough are returned unchanged.                                    |
| `TakeFirstCharacters`    | The first n characters of a text, or the whole text if it is shorter; the first 4 of 'GB82WEST' is 'GB82'.                                                                             |
| `TakeLastCharacters`     | The last n characters of a text, or the whole text if it is shorter; the last 4 of '4111111111111111' is '1111'.                                                                       |
| `MaskAllButLast`         | Replaces all but the last few characters with a mask character, for safe display of card numbers, accounts and phone numbers; '4111111111111111' with 4 visible is '************1111'. |
| `ReplaceFirstOccurrence` | Replaces only the first case-sensitive occurrence of a substring; texts without it are returned unchanged.                                                                             |
| `ReplaceLastOccurrence`  | Replaces only the last case-sensitive occurrence of a substring; texts without it are returned unchanged.                                                                              |

### TextExtraction

`Jube.Service.Extensions.TextExtraction` — Pulls pieces out of text: the part before, after or between markers, the n-th
token, all tokens, and numbers written inside free text.

| Tool                  | What it does                                                                                                                                                                                                                |
|-----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `TextAfter`           | The part of a text after the first occurrence of a marker, or empty if the marker is absent; after '@' in 'ann@example.com' is 'example.com'.                                                                               |
| `TextBefore`          | The part of a text before the first occurrence of a marker, or empty if the marker is absent; before '@' in 'ann@example.com' is 'ann'.                                                                                     |
| `TextBetween`         | The text between the first occurrence of a start marker and the next end marker after it, or empty if either is missing; between 'ref:' and ';' in 'x ref:AB12; y' is 'AB12'.                                               |
| `TokenAt`             | The token at a zero-based position after splitting on a separator; a negative position counts from the end, so -1 is the last. Out-of-range positions give empty text.                                                      |
| `SplitText`           | Splits a text on a separator into a list, optionally trimming each part and dropping empty parts. Returns at most 10000 parts.                                                                                              |
| `ExtractNumbers`      | Every decimal number written in a free text, in order, reading '.' as the decimal point and a leading '-' as negative; 'paid 12.50 then -3 twice' gives 12.5, -3. Thousands separators split numbers, so remove them first. |
| `ExtractWholeNumbers` | Every whole number written in a free text, in order, with a leading '-' read as negative; 'invoices 1001 and 1002' gives 1001, 1002. Digit runs too long for a 64-bit integer are refused.                                  |
| `QueryStringValue`    | The decoded value of a named parameter in a URL or query string, or empty if absent; name in 'https://x.test/p?name=J%C3%BCbe+Ltd' is 'Jübe Ltd'.                                                                           |

### TextInspection

`Jube.Service.Extensions.TextInspection` — Counts and tests on text: words, lines, letters, digits, character variety,
entropy, repeated or sequential characters, palindromes, anagrams and containment.

| Tool                  | What it does                                                                                                                                                                                                                                                                                                                            |
|-----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DescribeText`        | Measures a text in one call: characters, words, lines, letters, digits, distinct characters, longest run of one repeated character, Shannon entropy in bits per character, and whether it is all ASCII letters, all ASCII digits, or one repeated character. Low entropy or long runs suggest synthetic or keyboard-mashed identifiers. |
| `CountOccurrences`    | Counts non-overlapping, case-sensitive occurrences of a substring in a text; 'aba' occurs twice in 'abababa'.                                                                                                                                                                                                                           |
| `HasSequentialDigits` | Whether a text contains a run of consecutive ascending or descending digits of at least a given length, such as 1234 or 9876. Common in weak PINs, test card numbers and fabricated identifiers.                                                                                                                                        |
| `IsPalindrome`        | Whether a text reads the same backwards, ignoring case, spaces and punctuation; 'A man, a plan, a canal: Panama' is a palindrome.                                                                                                                                                                                                       |
| `AreAnagrams`         | Whether two texts use exactly the same characters the same number of times, case-sensitive and counting spaces; 'listen' and 'silent' are anagrams. Normalise case first if case should not matter.                                                                                                                                     |
| `EqualsIgnoringCase`  | Whether two texts are equal when upper and lower case are ignored (ordinal, culture-independent comparison).                                                                                                                                                                                                                            |
| `ContainsAllOf`       | Whether a text contains every one of several substrings, optionally ignoring case. Use it for keyword checks in narratives and references.                                                                                                                                                                                              |
| `ContainsAnyOf`       | Whether a text contains at least one of several substrings, optionally ignoring case. Use it for keyword or watch-word checks.                                                                                                                                                                                                          |

### ThresholdsAndLimits

`Jube.Service.Extensions.ThresholdsAndLimits` — Amount-against-limit arithmetic for structuring, smurfing and limit
monitoring: distance and headroom to a threshold, excess and shortfall, round amounts and position in a range.

| Tool                      | What it does                                                                                                                                                                                           |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DistanceToThreshold`     | Absolute distance between an amount and a threshold, whichever side it is on; 9600 and 10400 are both 400 from 10000. Use it to see how close a payment came to a reporting limit.                     |
| `PercentBelowThreshold`   | How far an amount is below a threshold as a percent of the threshold; 9500 against 10000 is 5. Use it to express near-threshold behaviour on a common scale.                                           |
| `IsJustBelowThreshold`    | Whether an amount sits under a threshold but within a margin of it, the classic structuring pattern; 9600 is just below 10000 with a 5 percent margin.                                                 |
| `HeadroomToLimit`         | Remaining room under a limit (limit minus used), negative once the limit is breached; 700 used against 1000 leaves 300 and 1200 used leaves -200. Use it for available balance or remaining allowance. |
| `ExcessOverLimit`         | How much an amount exceeds a limit, never negative; 1200 against a 1000 limit is 200 over. Use it to size a breach.                                                                                    |
| `ShortfallBelowMinimum`   | How much an amount falls short of a required minimum, never negative; 80 against a minimum of 100 is 20 short. Use it for minimum balances and floors.                                                 |
| `IsRoundAmount`           | Whether an amount is an exact multiple of a round unit such as 100 or 1000. Round amounts are a common money-laundering indicator when frequent.                                                       |
| `LeadingSignificantDigit` | The first non-zero digit of an amount (1 to 9), or 0 for zero; 4567 gives 4 and 0.0456 gives 4. Collect these across many amounts to compare with the first-digit law.                                 |
| `PositionInRange`         | Where a value sits inside a range as a percent from minimum (0) to maximum (100); 75 in 50 to 150 is 25. Values outside the range give below 0 or above 100.                                           |

### TimeZonesAndFormats

`Jube.Service.Extensions.TimeZonesAndFormats` — Converts date-times between UTC and IANA time zones, reports offsets and
daylight saving, and converts to and from Unix epoch and standard text formats.

| Tool                      | What it does                                                                                                                                                                                                       |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ConvertUtcToTimeZone`    | Converts a UTC date-time to the local wall-clock time in an IANA time zone such as Europe/London or Asia/Kolkata, allowing for daylight saving. The result has no zone marker.                                     |
| `ConvertTimeZoneToUtc`    | Converts a local wall-clock date-time in an IANA time zone to UTC, allowing for daylight saving. A local time skipped by a clock change is refused.                                                                |
| `ConvertBetweenTimeZones` | Converts a local wall-clock date-time in one IANA time zone to the wall-clock time in another; 09:00 in Europe/London in July is 04:00 in America/New_York.                                                        |
| `DescribeTimeZoneAt`      | Offset from UTC in hours and whether daylight saving is in force for an IANA time zone at a given UTC instant; Europe/London is +1 with daylight saving in July.                                                   |
| `IsValidTimeZoneId`       | Whether a text is a time zone id known to this system, such as Europe/Paris. Use it before converting times with a zone taken from data.                                                                           |
| `ToUnixEpoch`             | Converts a UTC date-time to Unix epoch time, returning both seconds and milliseconds since 1970-01-01T00:00:00Z.                                                                                                   |
| `FromUnixEpoch`           | Converts a Unix epoch value to a UTC date-time. Values above 100000000000 are usually milliseconds; say which unit the value is in.                                                                                |
| `FormatDateTime`          | Writes a date-time in a standard text format: Iso8601 (2026-09-23T12:00:00.0000000Z for UTC), Rfc1123 (Wed, 23 Sep 2026 12:00:00 GMT), Sortable (2026-09-23T12:00:00) or UniversalSortable (2026-09-23 12:00:00Z). |
| `ParseDateTimeWithFormat` | Reads a date-time from text using an exact .NET format pattern such as dd/MM/yyyy HH:mm, treating it as UTC. Use it for unambiguous parsing of data where day and month order matter.                              |

### UnitsAndNumerals

`Jube.Service.Extensions.UnitsAndNumerals` — Converts measurements between units and whole numbers between Roman
numerals and other bases, plus simple whole-number facts.

| Tool                     | What it does                                                                                                                                                                                                                                                                                         |
|--------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ConvertUnits`           | Converts a measurement between units of the same kind. Length: m km cm mm mi yd ft in nmi. Mass: kg g mg t lb oz st ozt. Area: m2 km2 ha acre ft2 mi2. Volume: l ml m3 gal ukgal qt pt floz. Speed: m/s km/h mph kn. Time: s ms min h d wk. Data: b kb mb gb tb kib mib gib tib. Temperature: c f k. |
| `IntegerToRomanNumerals` | Writes a whole number from 1 to 3999 as Roman numerals; 1994 is MCMXCIV.                                                                                                                                                                                                                             |
| `RomanNumeralsToInteger` | Reads canonical Roman numerals as a whole number, case-insensitive; MCMXCIV is 1994. Non-canonical forms such as IIII are refused.                                                                                                                                                                   |
| `IntegerToBase`          | Writes a whole number in another base from 2 to 36 using lowercase digits; 255 in base 16 is ff and -10 in base 2 is -1010.                                                                                                                                                                          |
| `BaseToInteger`          | Reads a whole number written in a base from 2 to 36, case-insensitive, with an optional leading minus; ff in base 16 is 255.                                                                                                                                                                         |
| `IsPrimeNumber`          | Whether a whole number is prime (divisible only by 1 and itself); 1 and negative numbers are not prime.                                                                                                                                                                                              |
| `DigitSum`               | Sum of the decimal digits of a whole number, ignoring the sign; 9999 gives 36. Useful for check-digit reasoning and digit patterns.                                                                                                                                                                  |

## Helper groups

### ClockAndIdentifiers

`Jube.Service.Helpers.ClockAndIdentifiers` — The current date and time in UTC or any time zone, the list of time zone
ids, and fresh random or time-ordered UUIDs; the only tools here that are not repeatable, because they read the clock or
a random source.

| Tool                    | What it does                                                                                                                                                                                                         |
|-------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CurrentUtcTime`        | The current date and time in UTC from the server clock, with today's UTC date. Call it whenever a question depends on now or today; never assume the current date.                                                   |
| `CurrentTimeInZone`     | The current local date and time, today's local date and the UTC offset in an IANA time zone such as Europe/London. Use it for questions about local business hours or the local date.                                |
| `ListTimeZoneIds`       | Time zone ids known to this system whose id contains a filter, case-insensitive, sorted; filter by city or region such as London, America or Kolkata. At most 100 are returned.                                      |
| `NewRandomUuid`         | One or more new random (version 4) UUIDs in canonical lowercase form, for correlation ids or test data. Up to 100 at once.                                                                                           |
| `NewTimeOrderedUuid`    | One or more new time-ordered (version 7) UUIDs, which sort by creation time and suit database keys. Up to 100 at once.                                                                                               |
| `UuidVersion7Timestamp` | The UTC creation time embedded in a version 7 UUID, to the millisecond. Refused for other UUID versions, which carry no time.                                                                                        |
| `TimeSince`             | Exact time elapsed from a past date-time until now, in total days, hours and minutes and as completed whole years; negative for a future date-time. Use it for questions such as how long ago an account was opened. |

### TextLists

`Jube.Service.Helpers.TextLists` — Exact set operations on lists of text values: distinct values, union, intersection,
difference, symmetric difference, joining and comparing lists, optionally ignoring case. Numeric sorting and frequency
tables are analytics tools.

| Tool                  | What it does                                                                                                                                               |
|-----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DistinctValues`      | The list with duplicates removed, keeping the first occurrence of each value in its original order, optionally ignoring case and surrounding spaces.       |
| `UnionOfLists`        | Every distinct value that appears in either list, first list order then new values from the second.                                                        |
| `IntersectionOfLists` | The distinct values that appear in both lists, in first-list order. Use it to find shared devices, addresses or counterparties.                            |
| `DifferenceOfLists`   | The distinct values in the first list that are not in the second, in first-list order; for example new counterparties this month compared with last month. |
| `CompareLists`        | Compares two lists in one call: values only in the first, only in the second, in both, and whether they hold the same set of distinct values.              |
| `CountDistinctValues` | How many different values a list holds, optionally ignoring case; the list a, A, b has 3 distinct values, or 2 ignoring case.                              |
| `JoinValues`          | Joins a list into one text with a separator, such as a comma and a space, for display or for building a rule's list literal.                               |

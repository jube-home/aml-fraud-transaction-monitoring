---
layout: default
title: Sanction Searching
nav_order: 2
parent: Sanctions
grand_parent: Configuration
---

# Sanction Searching

The Sanctions page in Jube exists for the purposes of performing individual and batch sanction name matching against the
entries loaded by the engine.

The Sanctions page uses the matching functionality available to the engine and is therefore exactly the same
functionality available in model invocation real time.

Subject to the EnableSanction application setting being set to the string "True" in Environment Variable, the thread
will be started to support the aforementioned. The thread should be started on all servers that require online recall of
sanctions either directly or via model invocation:

```text
EnableSanction=True
```

| Value          | Description                                              |
|----------------|----------------------------------------------------------|
| EnableSanction | A flag indicating if sanctions is enabled in the engine. |

In the default installation of Jube there is only a single Sanction record by name "Robert Mugabe", although Jube
supports the remote updating of sanctions from a variety of sources on the internet.

Manual Sanction invocation is available from the top level of the menu:

<!-- TODO(screenshot): file=sanctions-location-of-sanctions-search.png route=/sanctions state="replaces the Razor-era LocationOfSanctionsSearch.png; capture from the Vue UI" -->
![Image](sanctions-location-of-sanctions-search.png)

Navigate to the sanctions page by clicking the top menu item as above:

<!-- TODO(screenshot): file=sanctions-empty-sanctions-page.png route=/sanctions state="replaces the Razor-era EmptySanctionsPage.png; capture from the Vue UI" -->
![Image](sanctions-empty-sanctions-page.png)

Sanctions matching in Jube uses a concept of a multipart string, which means to say that a string (most likely a name)
be comprised of several elements (e.g first name and last name), separated by a space character (and only a space
character).

A multipart string in Jube makes no distinction in the order in which strings are presented (i.e. first then last name,
would be the same as last then first name for the purposes of matching).

The concept of distance is the implementation of the Levenshtein Distance distance algorithm and the number of
characters that would need to be changed to achieve an absolutely perfect match.

Consider the following table which evaluates the multipart string  "Robert Mugabe":

| Multipart String | Distance                                                    |
|------------------|-------------------------------------------------------------|
| Robert Mugabe    | 0 characters need be changed to achieve perfect match.      |
| Robert Mugaby    | 1 character need be changed to achieve a perfect match.     |
| Robert Fugaby    | 2 characters need to be changed to achieve a perfect match. |

The Sanctions page takes two parameters as described in the following table:

| Value              | Description                                                                                                                                                                                                                                                                  | Example       |
|--------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| Multipart String   | The multipart string to be checked against sanctions,  where strings are separated by a space character (and nothing else).                                                                                                                                                  | Robert Mugabe |
| Distance           | The maximum distance to be checked for and returned by for the Levenshtein distance algorithm.Results will not be returned for more than this specified distance threshold - the number of characters required to be changed for a match to be returned - has been exceeded. | 1             |
| Max Distance Ratio | Scales down the allowed distance in proportion to the shorter token being compared, so short tokens cannot match too loosely. Defaults to the server-wide SanctionsLevenshteinMaxDistanceRatio Environment Variable (0.3) if left at its default.                            | 0.3           |
| Max Coverage Ratio | Rejects a candidate whose token count differs too greatly from the searched string's token count. Defaults to the server-wide SanctionsLevenshteinMaxCoverageRatio Environment Variable (2.0) if left at its default.                                                        | 2.0           |

The values are validated, and a failure is a 400 with the standard validation body (the same structure the user
interface shows for any form). The multipart string is required, no longer than 500 characters once trimmed, and may not
contain a NUL character; Distance, where given, is a whole number from 0 to 50; Max Distance Ratio, where given, is a
number from 0 to 1; and Max Coverage Ratio, where given, is a number from 0 to 100. Not a number, infinity and negative
values are refused. An absent value takes its default. The endpoint answers 404 when `EnablePublicInvokeController` is
False and 503 until the engine has finished starting. Sanction lists are reference data shared by every tenant, so a
result is not scoped to a tenant.

Complete the Sanctions page as the above parameters:

<!-- TODO(screenshot): file=sanctions-query-sanctions-page.png route=/sanctions state="replaces the Razor-era QuerySanctionsPage.png; capture from the Vue UI" -->
![Image](sanctions-query-sanctions-page.png)

Click the Check button:

<!-- TODO(screenshot): file=sanctions-check-button-with-value-to-check.png route=/sanctions state="replaces the Razor-era CheckButtonWithValueToCheck.png; capture from the Vue UI" -->
![Image](sanctions-check-button-with-value-to-check.png)

The Sanction functionality is also exposed in the Invoke API, and Check button simply compiles and sends the following
GET statement to the Jube Engine server side:

[https://localhost:5001/api/Invoke/Sanction?MultiPartString=Robert+Mugabe&Distance=2](https://localhost:5001/api/Invoke/Sanction?MultiPartString=Robert+Mugabe&Distance=2)

(MaxDistanceRatio and MaxCoverageRatio query string parameters are added when the sliders above are moved from their
defaults.)

Returning the following response payload - a grand-total and per-source aggregation breakdown alongside the raw matches,
rather than a bare array of matches as in earlier versions of this page:

``` json
{
  "aggregations": {
    "total": {
      "sum": 0, "average": 0, "count": 1, "max": 0, "min": 0,
      "first": 0, "last": 0, "confidence": 1.0
    },
    "bySource": [
      {
        "sourceId": 1, "sourceName": "SDN",
        "sum": 0, "average": 0, "count": 1, "max": 0, "min": 0,
        "first": 0, "last": 0, "confidence": 1.0
      }
    ]
  },
  "entries": [
    {
      "id": 1,
      "value": "robert mugabe",
      "sanctionEntrySourceId": 1,
      "source": "SDN",
      "reference": "Testing",
      "distance": 0
    }
  ]
}
```

Every aggregation mode is returned together on every search - see
[Aggregation Type and Confidence](../SanctionModelInvocation/index.html#aggregation-type-and-confidence) for what each
one means - rather than a single mode having to be pre-selected.

Of course presented in a manner friendly to the end user in three grids: the raw matches, a single-row grand total
across every source, and a by-source breakdown, as follows:

<!-- TODO(screenshot): file=sanctions-returned-sanction-check-to-grid.png route=/sanctions state="replaces the Razor-era ReturnedSanctionCheckToGrid.png; capture from the Vue UI" -->
![Image](sanctions-returned-sanction-check-to-grid.png)

In this example, across all Sanctions lists, it can be seen that there are one record having been returned at matching,
and in all instances the distance to an absolute match of 0 characters requiring change:

<!-- TODO(screenshot): file=sanctions-zero-distance-match.png route=/sanctions state="replaces the Razor-era ZeroDistanceMatch.png; capture from the Vue UI" -->
![Image](sanctions-zero-distance-match.png)

To broaden the allowable distance, simply move the distance slider control to the new threshold, although keep in mind
that this will greatly increase false positive.

To demonstrate distance, change the multipart string to "Robert Mugaby" (a deliberate misspelling by one character in
the surname Mugabe):

<!-- TODO(screenshot): file=sanctions-bad-spelling-of-sanctioned.png route=/sanctions state="replaces the Razor-era BadSpellingOfSanctioned.png; capture from the Vue UI" -->
![Image](sanctions-bad-spelling-of-sanctioned.png)

Click Check to match with the revised distance:

<!-- TODO(screenshot): file=sanctions-matches-with-bigger-distance.png route=/sanctions state="replaces the Razor-era MatchesWithBiggerDistance.png; capture from the Vue UI" -->
![Image](sanctions-matches-with-bigger-distance.png)

It can be seen that matches have still been achieved, this time specifying the distance of 1, which indicates that one
character needs to be changed in order to achieve an exact match.

<!-- TODO(screenshot): file=sanctions-distance-one-match.png route=/sanctions state="replaces the Razor-era DistanceOneMatch.png; capture from the Vue UI" -->
![Image](sanctions-distance-one-match.png)

There are some additional processing beyond just the use of the Levenshtein Distance distance algorithm that are worth
noting. The Sanctions functionality is an implementation of the Levenshtein Distance algorithm that compares strings
(e.g. Richard vs Gichard) and returns an integer value specifying the number of characters that have to be changed in
order for the two strings to be absolutely matched (e.g. 1 in this example). The purpose of fuzzy matching in the realm
of Sanctions checking is to detect the scenario where names are subtly changed by error, deliberate action or cultural
reasons (e.g. Mohammed has several possible spellings in practice). The Sanctions process will take the Multipart String
that has been passed in the query string in the first instance (e.g. Robert Mugabe). The multipart string takes each
name component separated by a space (i.e. '  ') where the order of the name components does not matter (i.e. Mugabe
Robert is identical to Robert Mugabe for the purposes of the processing of sanctions matches). The distance query string
parameter is important to limit the return to only records that have a certain match quality (i.e. on a small number of
characters need to be changed in order for a match to be achieved). All Sanctions lists are held in the engines memory
to assure fast response times. Although the Levenshtein Distance algorithm exists outside the scope of this document, as
it is a pure implementation, it is explained in more detail as follows:

[https://en.wikipedia.org/wiki/Levenshtein_distance](https://en.wikipedia.org/wiki/Levenshtein_distance)

The first step in the sanctions processing is to break the Multipart string passed into component parts as an array. For
example, the string Robert Mugabe passed as a string would be broken into two strings, Robert and Mugabe, for the
purposes of matching. During the breaking of the multipart string, each string will be cleaned replacing characters
based on the following dictionary (currently hard coded):

| Value            | Description                                                                                                                                                 |
|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ,                | Removed.                                                                                                                                                    |
| - and .          | Replaced with a space, so hyphenated or abbreviated name components are still split into separate tokens.                                                   |
| ' (apostrophe)   | Removed outright (e.g. O'Brien becomes OBrien) rather than treated as a token boundary.                                                                     |
| " " (i.e. space) | Removed,  keeping in mind that it should have been split on this value in any event, it serves to remove the curious instance of double space in a string.. |

Accented characters are also normalised (diacritics stripped) and everything is lower-cased before comparison, so
"Müller" and "Muller" tokenize identically.

After splitting and cleaning, any token that is a single character, or that matches an entry in the server-wide **Stop
Token** list, is dropped from both the searched string and every sanction entry before matching. Stop tokens are common
honorific and religious/cultural titles (for example, titles equivalent to "Sheikh" or "Imam" that are common in some
naming conventions), so that a name written with such a title compares correctly against a list entry that omits it, and
vice versa, without the extra token itself counting against the match. Stop tokens are seeded via a database migration
rather than being administrable from this page - see
[Sanctions Loader](../SanctionsLoader/index.html#stop-tokens).

The routine now has a list of strings that need to be matched against the sanctions loaded to memory, keeping in mind
that the components of the multipart string (i.e. Robert and Mugabe) will need to be matched with each component of the
sanction entry (e.g. Robert AND Gabriel AND Mugabe). The reason multipart strings are broken into components for both
the request (i.e. what the user has provided to be tested) and the file entries is to assure a rigorous matching
analysis without the user being required to perform several searches with subtle changes to the multipart string.

For each Sanctions entry available in the engines memory, the process will step through each entry one by one. The
process will be repeated in a step wise fashion depending upon the maximum distance that has been specified in the the
request. For example, if the user has passed a value that the maximum distance to be considered is five, then there will
be five iterations of sanctions checking, with any matches being recorded alongside the distance threshold that yielded
a match. For each Sanction Entry, for each distance step each component of the multipart string passed by the user will
be tested against each component element of the Sanction Entry. The component entries are broken out of the multipart
string during the load to save processing time and are referenced distinctly \ only once in this process (e.g. Richard
David Richard, would be tested as Richard David). When processing values for the multipart string either passed by the
end user of available in memory from the sanctions loader, it is done so on a distinct basis. For each component of the
multipart string passed by the user, it is tested against each component part of the sanctions entry using the
Levenshtein distance algorithm. The Levenshtein Distance algorithm will return an integer detailing the number of
characters that need to be changed in order for the comparison to be a perfect match. If the distance is less than the
value specified in the querystring parameter from the user, the multipart string element is recorded as having matched
in a buffer. The process is repeated until each multipart string element passed by the user is tested against each
multipart string element from the sanctions loader (and this in the engine memory).

## Access, limits and Unicode handling

<!-- TODO(docs): owner to read this whole section; written from the code. -->

Searching requires the View Sanction permission (29) in addition to being signed in; a caller without it receives 403
from
the Invoke API as well as a refusal on the page. Searches are limited per user (default 300 per 60 seconds,
`SanctionSearchMaxPerUserPerWindow` and `SanctionSearchWindowSeconds`, counted per server process) and answer 429 when
the limit is
exceeded. At most `SanctionSearchMaxEntries` (default 1000) matching entries are returned, closest first; the
aggregations
are always computed over every match, so the count can exceed the number of rows listed.

Before comparison both the searched string and every entry are folded the same way: compatibility forms are
normalised (full-width Latin letters, ligatures, Arabic presentation forms), combining marks are removed (accents,
Arabic short vowels), zero-width and bidirectional control characters are removed, other white space becomes a space,
and
the text is lower-cased. Latin, Cyrillic, Greek, Arabic, Georgian and Devanagari are compared letter for letter in their
own script. No transliteration between scripts is performed, so a Cyrillic or Arabic spelling will not match a Latin
list
entry.

Lookalike letters are folded identically when the list is loaded and when a search or model lookup is made: within a
word, if every letter is either Latin or a Cyrillic or Greek letter that looks like a Latin one (for example Cyrillic
a, e, o, p, c, x, y or Greek alpha, omicron, nu), the lookalikes are replaced by their Latin letters, so a Cyrillic or
Greek lookalike spelling of a listed Latin name is found at the same distance as the Latin spelling. A word containing
any letter that has no Latin lookalike (for example Cyrillic g, d, l or Greek final sigma), and every Arabic, Urdu,
Georgian and Devanagari word, is left exactly as written. Digits that resemble letters (0 and O, 1 and l) are not
folded.
Transliteration between scripts (Arabic or Cyrillic to Latin) is a separate feature and is not performed.
<!-- TODO(docs): owner to confirm the lookalike table (embedded in Jube.Engine/Sanctions/SanctionTextFolding.cs) and the transliteration position. -->

The list is tokenised and folded once when an entry is loaded or replaced and kept with the entry in memory, so a search
only folds the searched string. Since the page is now permission checked on the server, the role permission list in the
Administration documentation should show View Sanction (29) as the permission that the Sanctions menu item and the
search API both require.
<!-- TODO(docs): add View Sanction (29) to the roles documentation if it is not listed there. -->

Names are displayed with the page direction; entries in right-to-left scripts inside a left-to-right page are not yet
isolated from the surrounding text.
<!-- TODO(docs): remove this sentence once the grid isolates cell text with dir="auto". -->


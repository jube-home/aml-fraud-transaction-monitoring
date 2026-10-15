---
layout: default
title: Sanctions Screening
nav_order: 4
parent: FAQ
---

# Sanctions Screening

## Matching

**Q: How does sanctions fuzzy matching work?**

A: Input and each entry are tokenised: Unicode-normalised, diacritics stripped, lowercased, split on spaces and
punctuation, short tokens dropped, duplicates removed. Entries are evaluated in parallel. A coverage ratio rejects
entries when the input has too few tokens. Each input token (longest first) is greedily paired to the closest unused
entry token by Levenshtein distance. If any pairing exceeds the maximum distance, the entry is rejected; otherwise the
worst pairing distance is the match score. See
[Sanction Searching](../Configuration/Sanctions/SanctionSearching/index.html).

**Q: Is matching order-sensitive?**

A: No. Tokens are matched regardless of order, so "Smith John" matches "John Smith".

**Q: How do aggregations and confidence work?**

A: Per-model aggregation can be Sum, Average (the default), Count, Max, Min, First, Last or Confidence. Confidence
combines closeness (1/ (1+min distance)), separation (a sigmoid of the z-score of the best match against the rest) and a
skewness weight, multiplied together so any weak component lowers the score. The result is a 0 to 1 value suitable for
thresholds.

**Q: Can I remove common words like "Ltd" from matching?**

A: Yes. Stop tokens are administratively available in a server-wide table (seeded with examples by migration) and are
removed before matching. They refresh on the next cache sync without a restart.

**Q: How are the maximum distance and coverage tuned?**

A: Max Distance Ratio and Max Coverage Ratio (defaults 0.3 and 2.0) are configurable per model on the Sanctions tree
page, with `SanctionsLevenshteinMaxDistanceRatio` and `SanctionsLevenshteinMaxCoverageRatio` as the server-wide
fallbacks.

**Q: Is sanctions matching expensive to run?**

A: Levenshtein distance is the most CPU-intensive part. It runs entirely in memory and results can be cached.

## List loading

**Q: How are the sanctions lists updated?**

A: Sources are defined in the SanctionEntrySource table (an HTTP URL or directory path, CSV with delimiter and name
fields; OFAC SDN with alternate names, EU and UN sources are seeded). A background loader reconciles by hash,
soft-deletes entries no longer present in the source, and keeps import and rejection audit tables. A landlord-only
upload page allows manual CSV upload, merged by hash with a created-user marker. The default polling interval is hourly
to avoid upstream rate limits. See [Sanctions Loader](../Configuration/Sanctions/SanctionsLoader/index.html).

**Q: What if the source list has aliases or is not a CSV?**

A: Convert it to CSV and split on alias so each alias is its own record.

## Serving

**Q: Why does the sanctions search page return 404 or 500 from the UI?**

A: Sanctions run in the engine, and the recall endpoint sits under the invoke controller. The node serving the request
therefore needs both `EnableEngine` and `EnablePublicInvokeController` set to True; otherwise it returns 404, and 503
while the engine is still warming its sanctions cache. A UI-only node has no sanctions cache and nothing to answer with,
so route the sanctions search at the load balancer to an engine-enabled node.

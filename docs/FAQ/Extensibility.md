---
layout: default
title: Extensibility
nav_order: 10
parent: FAQ
---

# Extensibility

**Q: What are inline functions versus inline scripts?**

A: [Inline functions](../Configuration/Models/InlineFunctions/index.html) are small VB.NET fragments that create extra
payload fields, with optional field encryption. [Inline scripts](../Configuration/Models/InlineScripts/index.html) are
full C# or VB classes implementing the inline-script contract that can add or override payload fields, call external
services, and intercept pipeline events. Both add values to the payload for rules to use.

**Q: How do I add an inline script without database access?**

A: You don't — by design. The script library is read-only from the application, and scripts are added only by inserting
into the inline-script table, because arbitrary code is an injection risk. Treat a new script as a release: keep the
script and its insert in version control with a runbook. Preservation (JEMP) exports carry only the link between a model
and a script, never the script code itself, so the script must already exist in the target environment. In practice
inline scripts are mostly static after being built.

**Q: What can inline scripts do that functions can't?**

A: Parse complex or nested payloads, call external services (with cache-backed memoisation), expose search keys
(including decorating composite keys), read dictionaries from the context, and hook into pipeline events. They carry
risk in multi-tenant setups because the context exposes the cache, the database and HTTP, so author them centrally.

**Q: Are there extension methods for rules?**

A: Yes — a curated set is available to the rule compiler through its token allow-list, and administrators can register
additional [curated dynamic expressions](../Configuration/Models/CuratedDynamicExpressions/index.html) as named rows in
the database, which become callable from rules. Dynamic expression evaluation is gated by `EnableDynamicEval` (default
False). See [Rule Extension Tools](../RuleTaxonomy/RuleExtensionTools.html).

**Q: What if an inline script throws?**

A: The exception is trapped and logged, and the pipeline continues without that value. Rules should treat
script-populated fields as optional and read them defensively, exactly as with a suppressed HTTP adaptation.

---
layout: default
title: Models, Payload and Configuration
nav_order: 1
parent: FAQ
---

# Models, Payload and Configuration

## Data model and payload

**Q: Does Jube have a fixed data model?**

A: No. Any data that can be represented as JSON can be configured as a model (a payment channel, audit events and so on). You define field names, the JSON path for each value, and its data type in [Request XPath](../Configuration/Models/RequestXPath/index.html), which is reached, as is everything else belonging to a model, from the hub cards of the model as described in [Child Objects](../Navigation/ChildObjects/index.html).

**Q: Do I have to conform to the demo payload?**

A: No. It is only an example. Use your own field names and map them in the Request XPath.

**Q: Can I send fields I haven't mapped in Request XPath?**

A: Yes, as long as the JSON is valid. Unmapped fields are dropped after parsing. The advice is to map everything you might need, excluding sensitive data such as card numbers.

**Q: What data types are supported for payload fields?**

A: String, Integer, Float (a double under the hood), Date, Boolean, Latitude and Longitude (doubles with special meaning for the Activation Watcher map) and IP (an IPv4 or IPv6 address held as text, so it works wherever a string does, and looked up for IP intelligence when a Svirfd entry of the model names it). The type controls which operators rules can use against the field.

**Q: Is the JSON path in Request XPath case-sensitive?**

A: Yes. A JSON payload in camelCase will not match Request XPath names defined in PascalCase. The symptom is the fallback or default value appearing in the response instead of the sent value.

**Q: Should I set default values for fields that may be missing?**

A: No, unless you really need to. Empty values are inferred as null and are not stored, which saves bytes. If a default is set, a mismatched path silently returns the default, which hides integration mistakes such as a case mismatch.

**Q: Can I use Request XPath names containing spaces?**

A: Avoid it. Rules refer to a field by its name, and in earlier versions the case grids built from names with spaces failed silently. Use PascalCase names.

**Q: Why can't I choose an Integer field as a function key, for example in a Sum?**

A: Aggregations such as Sum need a Float. Integers exist as a better-typed alternative to strings for discrete values. Same Count works on strings only, so for numeric values use an inline function or a second XPath that clips or rounds the value and casts it to string.

**Q: Can I change or rename fields after go-live?**

A: It is advisable to lock Request XPath entries once they are stable, because renaming a field breaks the interface. Dependency management refuses the deletion of anything still in use, yet a rename is not checked, and a rule that names a renamed field no longer compiles, so always lock the critical dependencies. The section What it depends on, of [Model Integrity](../Configuration/Models/ModelIntegrity/index.html), shows what uses a field before it is changed. See [Locked and Active](../Navigation/LockedAndActive/index.html).

## Reference dates and timestamps

**Q: What is the reference date and why does it matter?**

A: It is the date the system treats as "now" for velocity rules, TTL counters and purging. It must be consistent and in order. If you can't guarantee that (a real-time integration), use the system date. Use the payload date only for batch or back-processing.

**Q: What happens if a transaction has a future date?**

A: It is rejected with a 400 response ("Reference Date can't be in the future."), with no tolerance, because a future reference date would trigger purging of data. Dates older than the latest stored one are still processed; they simply don't move the stored reference date forward. Note also `AssumeLocalDateInPayloadExtraction`, True by default, which controls how dates without a time zone are interpreted.

**Q: Does an out-of-order timestamp by a second or two matter?**

A: No. The reference date only moves forwards, so small differences are immaterial. Differences of days or weeks are a problem.

**Q: Are timestamps UTC?**

A: Yes. Timestamps are standardised to UTC across the stack, and the server assumes a UTC clock, with the user interface presenting them in the local time of the browser. Mixed server time zones previously caused mismatches, such as the UI showing the wrong hour. Legacy timestamps are not migrated and must be updated manually if needed.

## Field switches

**Q: What does "Response Payload" do on a field?**

A: It controls whether the element is echoed in the response JSON. It is useful in development and can be switched off later to reduce payload size.

**Q: What does the "Archive Key" switch do?**

A: It promotes the key to the Archive Key table, which has a typed, indexed (composite key plus value, covering) access path into the Archive table. It is forward-only: existing data is not back-filled. It is also what allows a field to be drilled in the Case Key Journal.

## Multi-tenancy

**Q: What is multi-tenancy in Jube?**

A: Logical isolation, not separate infrastructure. All tenants share the same database, cache and engines, and every query is predicated by tenant. Users belong to a tenant, and a landlord is presented with a Tenant switcher at the top of the page with which to move between tenants. Configuration can be copied between tenants via [Preservation](../Configuration/Preservation/index.html) export and import. See [Multi Tenancy](../Concepts/MultiTenancy/index.html).

**Q: Can I run sandbox and production as separate tenants?**

A: Yes. Import (with rekeying) from one tenant to another is supported. Users are not exported, as they are PII, so an import that replaces roles can orphan users: orphaned users are moved to an "Orphaned Preservation Import" role and are deactivated, while the importing user stays active and is never locked out. See the Preservation questions in [Backup, Preservation and Support](BackupPreservationAndSupport.html).

## Configuration, secrets and synchronisation

**Q: Why is there no application configuration file?**

A: All configuration is via [environment variables](../Concepts/EnvironmentVariables/index.html), of which there are now several hundred, nearly all with a sensible default, which suits containers. The same binary behaves as a UI node, an API node or a job server depending on those variables.

**Q: How are secrets kept out of environment variables?**

A: Any environment variable value may carry a `[@Name@]` token, which resolves to the contents of a file of that name under `SecretsPath`. This is compatible with Docker Secrets mounted at `/run/secrets`. Unresolved tokens are left in place and logged to standard output. In the cluster deployment, the `secrets-init.sh` script generates credentials and an RSA keypair into Docker Swarm secrets and prints them exactly once, keeping no record of them on disk. Default cryptographic keys must always be changed before production.

**Q: Do model changes still need an explicit synchronisation?**

A: No. Changes are gated by [Maker Checker](../Concepts/MakerChecker/index.html) approval first: the engine only ever loads approved versions of model components. Once approved, a change is picked up automatically on the engine's next synchronisation cycle (`ModelSynchronisationWait`, 10 seconds by default), so no manual step is required, and there is no longer a synchronisation item in the menu. Should one be wanted regardless, open the model, then Integrity, where the panel Synchronise this model offers Synchronise this model now and Schedule this model; Synchronise all models, above the table of models, does the same for every model in the tenant. Both need the Allow Synchronisation permission. Lists, dictionaries, overrides and case configuration take effect in the background without model synchronisation. See [Model Integrity](../Configuration/Models/ModelIntegrity/index.html).

**Q: What needs approval under Maker Checker, and who can approve?**

A: Model components (rules, Request XPath, TTL counters, lists and dictionaries and their values, and other model artefacts, some 23 kinds in all) are versioned, and only approved versions run. `ApprovalsRequired` defaults to 1, and self-approval is refused except for the landlord. A change is approved or rejected in the Approval section beneath the record itself, where Compare presents the fields changed in a version, or for a whole model in one place under Integrity, in the section What is waiting for approval. There are dedicated permissions for approving (Allow Approval) and for viewing what is pending (View Pending Approvals). List and dictionary values are approved individually, with Approve all pending as a shortcut. A Force override needs approval; a Suppress override, case workflows and reprocessing rules do not. A Preservation import carries the approval history of what it imports, and where the package has none the import is approved automatically on behalf of the importer. See [Child Objects](../Navigation/ChildObjects/index.html).

## Invocation responses

**Q: Why does an invoke call return 404 even with a valid key?**

A: Either `EnablePublicInvokeController` is off on that node, the model GUID doesn't parse, or no active model has that GUID. A 401 means a missing, invalid, expired or revoked credential.

**Q: When does an invoke call return 403?**

A: Three cases: the model exists but the caller is not one of the model's users; the Web Application Firewall blocked the request (`WafEnabled` is True by default); or a request authenticated only by the session cookie failed the same-origin check (`CsrfOriginCheck`, also True by default). You may also see 503 while the engine is starting, 400 for an empty, oversized or malformed body or a future reference date, and 429 when the asynchronous queue is full.

**Q: Why does a new rule or TTL counter not show in the first transaction?**

A: The fetch happens earlier in the pipeline than the activation that increments the counter, so the first increment appears on the next transaction.

## Model design

**Q: Should different payment types share one model or use separate ones?**

A: Prefer one model with a type or discriminator key, because aggregation across models is limited: TTL counters can cross models, abstraction rules cannot. Keep financial transactions and non-monetary events in separate models.

**Q: How do I store multiple transaction legs (pre-auth, post-auth)?**

A: Send each leg as a separate event, distinguished by a type field, into the same model. Filter volume rules to the legs and statuses you need (for example the approved post-auth event), and expect two records per real transaction in the case journal. Archive data is immutable in normal operation; the one exception is [reprocessing](../Configuration/Reprocessing/index.html), which rewrites archive records and preserves the superseded versions in the ArchiveVersion table.

**Q: How does the pipeline order work?**

A: JSON extraction and the check of the reference date, then IP intelligence (Svirfd), inline functions, inline scripts and the gateway rule; then sanctions, dictionaries and the rest of the pipeline. Dictionary lookups happen inline as soon as the source field is populated, so a key built by an inline function can drive a dictionary lookup. See the [Rule Pipeline Reference](../RuleTaxonomy/RulePipelineReference.html).

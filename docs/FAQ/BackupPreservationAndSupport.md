---
layout: default
title: Backup, Preservation and Support
nav_order: 14
parent: FAQ
---

# Backup, Preservation and Support

## Preservation

**Q: How do I back up configuration?**

A: Use [Preservation](../Configuration/Preservation/index.html), under Administration in the menu, which has a tab for each of two concerns. JEMP import and export carries configuration between environments as a JEMP file (Jube Encrypted Message Pack: LZ4-compressed MessagePack under AES-256-GCM, with the key derived from a password of at least 12 characters). A file exported by an earlier release of Jube cannot be imported. Import is transactional and all-or-nothing: it rolls back completely on failure. Before an import is committed, a preview shows what it would add, remove and change. Snapshots keeps copies of the configuration of the one estate, which can be compared with one another or with the live configuration, and are restorable with a dedicated permission (Import Preservation Snapshot).

**Q: Why can I be locked out after importing a JEMP between tenants?**

A: You can't any more. Users are not exported (they are PII), so importing roles replaces the target tenant's roles and can orphan users. The import now recovers this inside the same transaction: users whose role no longer exists are moved to a new "Orphaned Preservation Import" role, holding only the permissions of the importing user (which is every permission where that user is a landlord), the importing user stays active, and all other orphaned users are deactivated pending review under Administration, Security. Make sure the export included roles, and, for a landlord, use the Tenant switcher at the top of the page to return to the original tenant afterwards.

**Q: Why does an import fail with "An item with the same key has already been added"?**

A: Older builds tripped over a duplicate GUID in dictionary entries during the rekey step. The import now tolerates duplicates; pull a current build if you see this.

## Support and release flow

**Q: What does support usually need?**

A: A JEMP export, the logs, and the exact POST body. Asynchronous support, by messages and short screen recordings, is the most efficient.

**Q: Can I reprocess historical data?**

A: It exists but is not recommended in production; see [Integration and API](IntegrationAndApi.html). Fine for testing.

**Q: What is the recommended release flow?**

A: Treat the training or sandbox environment as disposable. Export a JEMP and promote it through dev, test and production, ideally on a cluster even if single-host, using the [runbook](../GettingStarted/DeployingWithJubeCluster/DeploymentRunbook/index.html). Take a snapshot before each import, so there is always a way back. Remember that overrides and users never travel in an export, and inline script code must already exist in the target environment.

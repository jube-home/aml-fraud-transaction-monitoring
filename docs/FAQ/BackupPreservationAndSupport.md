---
layout: default
title: Backup, Preservation and Support
nav_order: 14
parent: FAQ
---

# Backup, Preservation and Support

## Preservation

**Q: How do I back up configuration?**

A: Use [Preservation](../Configuration/Preservation/index.html) to export a JEMP file (Jube Encrypted Message Pack:
LZ4-compressed MessagePack under AES256 with an HMAC, optionally password-protected with `PreservationSalt`). Import is
transactional and all-or-nothing: it rolls back completely on failure. Peek renders an export as JSON for inspection but
cannot be imported. Snapshots of configuration are also kept under Preservation and are restorable with a dedicated
permission.

**Q: Why can I be locked out after importing a JEMP between tenants?**

A: You can't any more. Users are not exported (they are PII), so importing roles replaces the target tenant's roles and
can orphan users. The import now recovers this inside the same transaction: users whose role no longer exists are moved
to a new "Orphaned Preservation Import" role holding every permission, the importing user stays active, and all other
orphaned users are deactivated pending review. Make sure the export included roles, and use the explicit tenant switcher
to return to the original tenant afterwards.

**Q: Why does an import fail with "An item with the same key has already been added"?**

A: Older builds tripped over a duplicate GUID in dictionary entries during the rekey step. The import now tolerates
duplicates; pull a current build if you see this.

## Support and release flow

**Q: What does support usually need?**

A: A JEMP export, the logs, and the exact POST body. Asynchronous support — messages and short screen recordings — is
the most efficient.

**Q: Can I reprocess historical data?**

A: It exists but is not recommended in production; see
[Integration and API](IntegrationAndApi.html). Fine for testing.

**Q: What is the recommended release flow?**

A: Treat the training or sandbox environment as disposable. Export a JEMP and promote it through dev, test and
production, ideally on a cluster even if single-host, using the runbook. Remember that overrides and users never travel
in an export, and inline script code must already exist in the target environment.

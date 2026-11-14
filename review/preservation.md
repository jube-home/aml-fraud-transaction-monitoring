# Slice: preservation

## Summary and verdict

Not ready until the serial test pass runs: nothing was built or run. The JEMP format (AES-256-GCM v3, PBKDF2-SHA256
600k, per-file salt, header as AAD, 12-char minimum) is sound. Real defects found and fixed: concurrent imports could
duplicate a tenant's configuration; EntityPermission rows in a package could attach role grants to another tenant's
models, workflows, visualisations or roles (cross-tenant write by a permission-38 tenant admin); same-tenant
visualisation guid was always re-keyed (wrong lookup table); any caller could label snapshots with the reserved
BeforeImport source or unknown values, with unbounded names; Import/Export audit rows had CreatedDate/CreatedUser
overwritten on completion; PBKDF2 iteration cap of 5M was a DoS amplifier; empty snapshot restore returned 500.

## Files reviewed

Jube.Cryptography/JempAesEncryption.cs; Jube.Preservation/Preservation.cs (export, ApplyWrapperAsync, ReadPackage,
snapshot, escalation guard), RecoverOrphanedUsersQuery, GetCarriedPermissionsQuery, PreservationComparer,
PreservationService (both: Repository/Preservation and Preservation/), PreservationSnapshotService, both endpoint files,
Import/Export/PreservationSnapshot repositories, PreservationResources; Vue: api.ts, state.ts,
PreservationImport/Export/Move/Restore/Preview/Compare/Take, AdminPreservationPage. Not read in full: Snapshots,
DiffView, Safeguard vue; AdvancedAnalyticsPreservation; PreservationDiffer/Summariser; per-entity insert loops
(sampled).
Tests read: PenTestPreservation (all), PreservationServiceTests (setup, round trip, rollback, escalation, snapshot),
JempAesEncryptionTests, privilege-escalation-rule.md.

## Findings

1. HIGH fixed: Preservation.cs EntityPermission import inserted rows verbatim; role tables have no tenant column, so
   foreign guids were written. Now filtered by PackageReferenceGuard (new file) against guids in the package.
2. HIGH fixed: no serialisation of imports (Preservation.cs); now pg_advisory_xact_lock per tenant right after
   BeginTransaction.
3. MED fixed: visualisation existsInTenant read roleRegistryAcrossAllTenants.
4. MED fixed: PreservationSnapshotService.CreateAsync accepted any source and name; now rejects unknown/BeforeImport and
   names over 200 (endpoint maps ArgumentException to 400, previously null body was 500).
5. MED fixed: JempAesEncryption max iterations 5M to 1M.
6. LOW fixed: Import/ExportRepository.UpdateAsync overwrote CreatedUser/CreatedDate.
7. LOW fixed: empty-body snapshot restore now NotFound (204) not 500.
8. Escalation work re-verified: guard runs before snapshot and transaction; caller permissions read before deletes;
   orphan role limited to importer permissions. Gap fixed: preview now shows a Privilege blocker (new resx key
   BlockerPrivilege).
9. Stale text: SPA password.hint mentions salt (shared TODO).
10. Vue: dir="auto" added on user-controlled badges (restore target, orphan names, diff labels).
    No plain SelectToken sites and no null-tenant pattern in owned files (tenant ids come from UserInTenant and are
    stamped by repositories).

## Follow-up pass (coordinator request)

- Upload cap: DynamicEnvironment PreservationImportMaxBytes (default 100 MiB). Endpoint metadata
  RequestSizeLimitAttribute (cap + 1 MiB) on Import and ImportPreview is applied by EmptyBodyGuardMiddleware before the
  form is read, so Kestrel refuses while streaming (413); the service re-checks with a bounded chunked copy
  (PackageTooLargeException, localised PackageTooLarge in neutral + pl/ar/ka/id/sv/ur/hi, 413). Decompressed cap:
  PreservationImportMaxDecompressedBytes (default 1 GiB), read from the LZ4BlockArray header in PackageSizeGuard after
  decryption and before decompression. There is no snapshot upload route (snapshots are server-built), so nothing else
  to cap. SPA maps 413 to new key preservation.import.tooLarge (proposed in the locale JSON).
- Retention: PreservationSnapshotMaxPerTenant (200) and PreservationSnapshotMaxAgeDays (365), 0 disables each. Pruned
  after every snapshot take and restore/import (PreservationSnapshotRetention, repository PruneAsync): newest N kept,
  most recent non-failed BeforeImport always kept, nothing pruned while a tenant Import is in flight (not completed, no
  error, under 6 h old), Json purged and Deleted set by "retention", audit op=PreservationSnapshot/Prune. User delete
  now also purges the body (matches the UI wording). Agent-side snapshot callers (AskJoobyAutoSnapshot,
  Jube.Service.Preservation.PreservationService) construct the service without the environment, so they prune with the
  built-in defaults, not overrides.
- Approval forgery (design, unchanged): forging needs only the package password (anyone who knows it can decrypt, edit
  EntityApproval rows with any approver and timestamps, and re-encrypt with the public Jemp code) plus permission 38 to
  import. Recommendation, not built: add an HMAC-SHA256 over the canonical EntityApproval records (and the entity ids
  and versions they bind) under a server-held key (for example derived from ElementSymmetricEncryptionKey) written into
  the Wrapper at export; import verifies it and, on absence or mismatch, falls back to the legacy
  blanket-approve-as-importer path. This binds a package to the estate that exported it, so moving between environments
  would need the key shared or a signed-by-source step; that trade-off is the owner decision.
- Lint: every changed C# file linted one at a time (PackageSizeGuard, PreservationLimits, PreservationSnapshotRetention,
  PackageReferenceGuard warning-level: allocation hints only; PreservationComparer, PreservationService,
  PreservationSnapshotService, both endpoint files, PreservationSnapshotRepository, Preservation.cs, JempAesEncryption,
  Import/ExportRepository, PreservationServiceTests, PenTestPreservation, JempAesEncryptionTests at error level: clean,
  though Rider analysis may be off for the very large files). Not linted: the small new exception files, ImportOptions,
  PreservationResources.cs, DynamicEnvironment.cs.
- New tests (unrun): PreservationServiceTests +6 (size limit, decompressed limit, retention keep/purge/audit, age,
  in-flight, delete purge). Run list additions: EnvironmentVariablesDocumentationTests, TestDocumentationTests,
  PenTestPreservation (oversize test now reaches decrypt at 25 MB and still expects 400).

## Design questions (owner)

1. Approval history is carried verbatim, so a password holder can craft a package with forged approvers or authorship;
   permission 38 is effectively a maker-checker bypass. Evidence: Preservation.cs EntityApproval loop only remaps ids.
2. (Resolved above: caps and retention built.) Export.Bytes keeps plaintext (accepted earlier, not raised). Approval
   integrity binding: see recommendation above.
4. Wrapper.Version is not validated on import (newer versions accepted).

## Tests

Added (unrun): JempAesEncryptionTests +2; PreservationServiceTests +6 (foreign refs and tenant stamp, concurrent
imports, preview Privilege blocker, visualisation guid, snapshot restore escalation, snapshot source/name);
PenTestPreservation +5 methods (crafted escalation package, foreign reference package, label refusal, snapshot route
matrix, snapshot import 63). docs/Testing tables updated.
Run: JempAesEncryptionTests, PreservationServiceTests, PenTestPreservation, PreservationModelTreeCoverageTests,
EntityApprovalPreservationTests, ExhaustivePreservationTests, AdvancedAnalyticsPreservationTests,
PreservationDiffServiceTests, TestDocumentationTests.
Vitest preservation 19 pass; prettier clean.

## Translations

Created PreservationResources pl, ar, ka, id, sv, ur, hi .resx (13 keys, {n} parity scripted). SPA ka/id/sv/ur/hi
proposals: review/preservation.locale-proposals.json (113 keys).

## Docs

docs/Configuration/Preservation/index.md: 14 TODO markers (screenshots lines 93, 94, 396, 406; others inline).

## Shared TODOs raised

See three lines appended to review/SHARED-TODO.md.

## Manual test sheet

Export with 11 and 12 char passwords; preview a package from another tenant as a user lacking permissions (Privilege
blocker); import twice in two tabs; restore snapshot with and without 63; ar/ur layout with Latin snapshot names.

## Not covered

Playwright specs not run; the LZ4BlockArray header layout in PackageSizeGuard is from memory of MessagePack 3.x and is
only proven by the decompressed-limit test when run. Child-guid collisions across tenants, null child collections in
crafted packages (500 with rollback), AdvancedAnalyticsPreservation, and the Snapshots/DiffView/Safeguard Vue bodies not
reviewed in depth.

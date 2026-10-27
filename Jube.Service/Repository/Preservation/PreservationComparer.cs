/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */


using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.Preservation;
using Jube.Preservation.Diff;
using Jube.Preservation.Models;
using Jube.Resources;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnapshotRow = Jube.Data.Poco.PreservationSnapshot;
using UserRow = Jube.Data.Poco.UserRegistry;
using NotFoundException = Jube.Service.Exceptions.Repository.PreservationSnapshot.NotFoundException;

namespace Jube.Service.Repository.Preservation
{
    using Library = Jube.Preservation.Preservation;

    internal sealed class PreservationComparer(
        DbContext dbContext,
        string userName,
        int tenantRegistryId,
        IStringLocalizer strings,
        ILog log)
    {
        private const int OrphanNamesShown = 50;

        private static readonly ExportOptions everything = new()
        {
            Exhaustive = true,
            Lists = true,
            Dictionaries = true,
            Visualisations = true,
            Roles = true,
            AdvancedAnalytics = true
        };

        internal async Task<PreservationSummaryDto> SummaryAsync(CancellationToken token)
        {
            var live = await LiveAsync(token).ConfigureAwait(false);
            var sections = PreservationSummariser.Summarise(live);

            return new PreservationSummaryDto
            {
                Sections = [.. sections.Select(s => new PreservationSummarySectionDto
                {
                    Key = s.Key, Count = s.Count, Rows = s.Rows, Bytes = s.Bytes
                })],
                TotalBytes = System.Text.Encoding.UTF8.GetByteCount(live.ToString(Formatting.None))
            };
        }

        internal async Task<PreservationDiffDto> CompareSnapshotsAsync(int? leftId, int? rightId,
            CancellationToken token)
        {
            if (leftId is null && rightId is null)
            {
                throw new ArgumentException(strings[PreservationResources.DiffNeedsSnapshot]);
            }

            if (leftId is not null && leftId == rightId)
            {
                throw new ArgumentException(strings[PreservationResources.DiffSameSnapshot]);
            }

            var left = leftId is null ? null : await SnapshotAsync(leftId.Value, token).ConfigureAwait(false);
            var right = rightId is null ? null : await SnapshotAsync(rightId.Value, token).ConfigureAwait(false);

            var leftPayload = left is null
                ? await LiveAsync(token).ConfigureAwait(false)
                : PreservationDiffer.PayloadOf(left.Json);

            var rightPayload = right is null
                ? await LiveAsync(token).ConfigureAwait(false)
                : PreservationDiffer.PayloadOf(right.Json);

            var kind = left is not null && right is not null
                ? PreservationDiffKind.TwoSnapshots
                : PreservationDiffKind.SnapshotAgainstLive;

            return await RecordAsync(kind, Side(left), Side(right), PreservationDiffer.Compare(leftPayload,
                rightPayload), token).ConfigureAwait(false);
        }

        internal async Task<PreservationImportPreviewDto> PreviewSnapshotAsync(int id, CancellationToken token)
        {
            var snapshot = await SnapshotAsync(id, token).ConfigureAwait(false);
            var wrapper = JObject.Parse(snapshot.Json);

            return await PreviewAsync(PreservationDiffKind.SnapshotRestorePreview, Side(snapshot),
                wrapper["Payload"] as JObject ?? [], (int?)wrapper["Version"] ?? 0,
                Guid.TryParse((string?)wrapper["Guid"], out var guid) ? guid : Guid.Empty, snapshot.Id, token)
                .ConfigureAwait(false);
        }

        internal Task<PreservationImportPreviewDto> PreviewPackageAsync(byte[] bytes, string? password,
            CancellationToken token)
        {
            var library = new Library(dbContext, userName);
            var package = library.ReadPackage(bytes, new ImportOptions { Password = password });
            var payload = package.Payload is null
                ? []
                : JObject.Parse(Library.PayloadJson(package.Payload));

            return PreviewAsync(PreservationDiffKind.PackageImportPreview,
                new PreservationDiffSideDto { Label = package.Guid.ToString() }, payload, package.Version,
                package.Guid, null, token);
        }

        private async Task<PreservationImportPreviewDto> PreviewAsync(PreservationDiffKind kind,
            PreservationDiffSideDto incoming, JObject payload, int version, Guid guid, int? snapshotId,
            CancellationToken token)
        {
            var live = await LiveAsync(token).ConfigureAwait(false);
            var diff = PreservationDiffer.Compare(live, payload);

            var recorded = await RecordAsync(kind, LiveSide(), incoming, diff, token, snapshotId)
                .ConfigureAwait(false);

            var preview = new PreservationImportPreviewDto
            {
                Diff = recorded,
                PackageVersion = version,
                PackageGuid = guid
            };

            await AddAdviceAsync(preview, live, payload, token).ConfigureAwait(false);

            return preview;
        }

        private async Task AddAdviceAsync(PreservationImportPreviewDto preview, JObject live, JObject payload,
            CancellationToken token)
        {
            const int currentVersion = 2;

            if (preview.PackageVersion < currentVersion)
            {
                preview.Warnings.Add(Warning("OlderFormat", PreservationResources.WarningOlderFormat,
                    preview.PackageVersion, currentVersion));
            }

            if (payload["EntityApproval"] is null)
            {
                preview.Warnings.Add(Warning("NoApprovalHistory", PreservationResources.WarningNoApprovalHistory));
            }

            var summary = PreservationSummariser.Summarise(payload);
            int Rows(string key) => summary.First(s => s.Key == key).Rows;

            var sensitive = Rows("lists") + Rows("dictionaries") + Rows("exhaustive");

            if (sensitive > 0)
            {
                preview.Warnings.Add(Warning("ProductionData", PreservationResources.WarningProductionData,
                    Rows("lists"), Rows("dictionaries"), Rows("exhaustive")));
            }

            var incomingRoles = (payload["RoleRegistry"] as JArray)?
                .Select(r => Guid.TryParse((string?)r["Guid"], out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty).ToHashSet() ?? [];

            var liveRoles = (live["RoleRegistry"] as JArray)?.Count ?? 0;

            if (incomingRoles.Count == 0 && liveRoles > 0)
            {
                preview.Warnings.Add(Warning("NoRoles", PreservationResources.WarningNoRoles, liveRoles));
            }

            var users = await TenantUsersAsync(token).ConfigureAwait(false);
            var orphaned = users.Where(u => !incomingRoles.Contains(u.RoleRegistryGuid)).ToList();

            preview.OrphanedUsers = new PreservationOrphanedUsersDto
            {
                Count = orphaned.Count,
                Names = [.. orphaned.Select(u => u.Name).Order(StringComparer.Ordinal).Take(OrphanNamesShown)],
                ImporterStaysActive = orphaned.Any(u => u.Name == userName)
            };

            if (incomingRoles.Count > 0 && users.Count > 0 && await RekeyWouldBeRefusedAsync(incomingRoles, token)
                    .ConfigureAwait(false))
            {
                preview.Blockers.Add(Warning("RoleRekey", PreservationResources.BlockerRoleRekey));
            }
        }

        private async Task<bool> RekeyWouldBeRefusedAsync(HashSet<Guid> incomingRoles, CancellationToken token)
        {
            var existing = await new RoleRegistryRepository(dbContext, tenantRegistryId)
                .GetAllTenantsAsync(token).ConfigureAwait(false);

            return incomingRoles.Any(guid =>
                existing.Any(r => r.Guid == guid) &&
                !existing.Any(r => r.Guid == guid && r.TenantRegistryId == tenantRegistryId));
        }

        private async Task<List<UserRow>> TenantUsersAsync(CancellationToken token)
        {
            var names = await dbContext.UserInTenant.Where(w => w.TenantRegistryId == tenantRegistryId)
                .Select(s => s.User).ToListAsync(token).ConfigureAwait(false);

            return await dbContext.UserRegistry
                .Where(w => names.Contains(w.Name) && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token).ConfigureAwait(false);
        }

        private PreservationImportWarningDto Warning(string code, string resource, params object[] arguments)
        {
            return new PreservationImportWarningDto
            {
                Code = code,
                Message = string.Format(System.Globalization.CultureInfo.CurrentUICulture, strings[resource].Value,
                    arguments)
            };
        }

        private async Task<JObject> LiveAsync(CancellationToken token)
        {
            var library = new Library(dbContext, userName);
            var payload = await library.LivePayloadAsync(everything, token).ConfigureAwait(false);

            return JObject.Parse(Library.PayloadJson(payload));
        }

        private async Task<SnapshotRow> SnapshotAsync(int id, CancellationToken token)
        {
            var snapshot = await new PreservationSnapshotRepository(dbContext, userName).GetByIdAsync(id, token)
                .ConfigureAwait(false);

            if (snapshot is null || string.IsNullOrEmpty(snapshot.Json))
            {
                throw new NotFoundException("The snapshot was not found.");
            }

            return snapshot;
        }

        private static PreservationDiffSideDto Side(SnapshotRow? snapshot)
        {
            return snapshot is null
                ? LiveSide()
                : new PreservationDiffSideDto
                {
                    SnapshotId = snapshot.Id,
                    Label = string.IsNullOrWhiteSpace(snapshot.Name) ? "#" + snapshot.Id : snapshot.Name,
                    CreatedDate = snapshot.CreatedDate
                };
        }

        private static PreservationDiffSideDto LiveSide()
        {
            return new PreservationDiffSideDto { Label = "Live" };
        }

        private async Task<PreservationDiffDto> RecordAsync(PreservationDiffKind kind, PreservationDiffSideDto left,
            PreservationDiffSideDto right, DiffResult result, CancellationToken token, int? restoreSnapshotId = null)
        {
            var dto = new PreservationDiffDto
            {
                DiffKindId = kind,
                Left = left,
                Right = right,
                Identical = result.Identical,
                Added = result.Added,
                Removed = result.Removed,
                Changed = result.Changed,
                Unchanged = result.Unchanged,
                ReferencesIgnored = result.ReferencesIgnored,
                Truncated = result.Truncated,
                Sections = [.. result.Sections.Select(s => new PreservationDiffSectionDto
                {
                    Name = s.Name, Added = s.Added, Removed = s.Removed, Changed = s.Changed, Unchanged = s.Unchanged
                })],
                Entries = [.. result.Entries.Select(e => new PreservationDiffEntryDto
                {
                    Section = e.Section,
                    Path = [.. e.Path],
                    KindId = (PreservationDiffEntryKind)(byte)e.Kind,
                    NestedCount = e.NestedCount,
                    Changes = [.. e.Changes.Select(c => new PreservationDiffChangeDto
                    {
                        Property = c.Property, Before = c.Before, After = c.After
                    })]
                })]
            };

            var repository = new PreservationDiffRepository(dbContext, userName);

            var row = await repository.InsertAsync(new PreservationDiff
            {
                Guid = Guid.NewGuid(),
                DiffKindId = (byte)kind,
                LeftSnapshotId = left.SnapshotId,
                RightSnapshotId = right.SnapshotId ?? restoreSnapshotId,
                RightLabel = right.SnapshotId is null && restoreSnapshotId is null ? right.Label : null,
                AddedCount = result.Added,
                RemovedCount = result.Removed,
                ChangedCount = result.Changed,
                Json = JsonConvert.SerializeObject(dto, Formatting.Indented),
                CompletedDate = DateTime.UtcNow
            }, token).ConfigureAwait(false);

            dto.Id = row.Id;

            if (log.IsDebugEnabled)
            {
                log.Debug($"Preservation.Diff: id={row.Id} kind={kind} user={userName}");
            }

            return dto;
        }
    }
}

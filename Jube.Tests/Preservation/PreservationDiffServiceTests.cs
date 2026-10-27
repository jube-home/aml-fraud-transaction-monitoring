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


using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Repository.Preservation;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using NotFoundException = Jube.Service.Exceptions.Repository.PreservationSnapshot.NotFoundException;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationDiffServiceTests(DatabaseFixture fx)
    {
        private readonly PreservationHarness harness = new(fx);

        private static PreservationSnapshotRequestDto Request(string name)
        {
            return new PreservationSnapshotRequestDto { Name = name };
        }

        private async Task<int> TakeAsync(PreservationTenant tenant, string name)
        {
            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildSnapshotServiceAsync(db, tenant.UserName);

            return (await service.CreateAsync(Request(name))).Id;
        }

        private async Task<PreservationDiffDto> DiffAsync(PreservationTenant tenant, int? left, int? right)
        {
            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildSnapshotServiceAsync(db, tenant.UserName);

            return await service.DiffAsync(new PreservationDiffRequestDto
            {
                LeftSnapshotId = left, RightSnapshotId = right
            });
        }

        private async Task<PreservationImportPreviewDto> PreviewPackageAsync(PreservationTenant tenant, byte[] bytes,
            string password = PreservationHarness.Password)
        {
            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildServiceAsync(db, tenant.UserName);
            await using var stream = new MemoryStream(bytes);

            return await service.ImportPreviewAsync([new PreservationImportFileDto { FileName = "x.jemp", Content = stream }],
                password);
        }

        [Fact]
        public async Task ComparingTwoSnapshotsReportsTheModelAddedBetweenThemAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfA");
            var first = await TakeAsync(tenant, "first");
            var modelId = await harness.CreateModelAsync(tenant, "DfA");
            var second = await TakeAsync(tenant, "second");

            var diff = await DiffAsync(tenant, first, second);

            diff.DiffKindId.Should().Be(PreservationDiffKind.TwoSnapshots);
            diff.Added.Should().BeGreaterThanOrEqualTo(1);
            diff.Identical.Should().BeFalse();
            diff.Left.SnapshotId.Should().Be(first);
            diff.Right.SnapshotId.Should().Be(second);
            diff.Entries.Should().Contain(e => e.KindId == PreservationDiffEntryKind.Added &&
                                               e.Section == "EntityAnalysisModel");

            await using var db = fx.GetDbContext();
            (await db.EntityAnalysisModel.CountAsync(w => w.Id == modelId)).Should().Be(1);
        }

        [Fact]
        public async Task ComparingASnapshotWithTheLiveTenantIsIdenticalWhenNothingHasChangedAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfB");
            await harness.CreateModelAsync(tenant, "DfB");
            var snapshot = await TakeAsync(tenant, "same");

            var diff = await DiffAsync(tenant, snapshot, null);

            diff.DiffKindId.Should().Be(PreservationDiffKind.SnapshotAgainstLive);
            diff.Right.Label.Should().Be("Live");
            diff.Identical.Should().BeTrue(string.Join("; ", diff.Entries.Select(e => string.Join(" > ", e.Path) + " " + string.Join(",", e.Changes.Select(c => c.Property + ":" + c.Before + "->" + c.After)))));
        }

        [Fact]
        public async Task ComparingAgainstTheLiveTenantReportsAModelRemovedSinceTheSnapshotAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfC");
            var modelId = await harness.CreateModelAsync(tenant, "DfC");
            var snapshot = await TakeAsync(tenant, "before");

            await using (var db = fx.GetDbContext())
            {
                await db.EntityAnalysisModel.Where(w => w.Id == modelId).Set(p => p.Deleted, (byte)1).UpdateAsync();
            }

            var diff = await DiffAsync(tenant, snapshot, null);

            diff.Removed.Should().BeGreaterThanOrEqualTo(1);
            diff.Entries.Should().Contain(e => e.KindId == PreservationDiffEntryKind.Removed);
        }

        [Fact]
        public async Task ComparingReportsAChangedPropertyWithItsBeforeAndAfterAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfD");
            var modelId = await harness.CreateModelAsync(tenant, "DfD");
            var snapshot = await TakeAsync(tenant, "before");

            await using (var db = fx.GetDbContext())
            {
                await db.EntityAnalysisModel.Where(w => w.Id == modelId).Set(p => p.Active, (byte)0).UpdateAsync();
            }

            var diff = await DiffAsync(tenant, snapshot, null);

            var entry = diff.Entries.Single(e => e.KindId == PreservationDiffEntryKind.Changed);

            entry.Changes.Should().Contain(c => c.Property == "Active" && c.Before == "1" && c.After == "0");
        }

        [Fact]
        public async Task EveryComparisonIsRecordedForAuditWithItsCountsAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfE");
            var first = await TakeAsync(tenant, "a");
            await harness.CreateModelAsync(tenant, "DfE");
            var second = await TakeAsync(tenant, "b");

            var diff = await DiffAsync(tenant, first, second);

            await using var db = fx.GetDbContext();
            var row = await db.PreservationDiff.SingleAsync(w => w.Id == diff.Id);

            row.TenantRegistryId.Should().Be(tenant.Id);
            row.DiffKindId.Should().Be((byte)PreservationDiffKind.TwoSnapshots);
            row.LeftSnapshotId.Should().Be(first);
            row.RightSnapshotId.Should().Be(second);
            row.AddedCount.Should().Be(diff.Added);
            row.Json.Should().Contain("\"Entries\"");
        }

        [Fact]
        public async Task AComparisonNeedsAtLeastOneSnapshotAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfF");

            var act = () => DiffAsync(tenant, null, null);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ASnapshotCannotBeComparedWithItselfAsync()
        {
            var tenant = await harness.CreateTenantAsync("DfG");
            var snapshot = await TakeAsync(tenant, "x");

            var act = () => DiffAsync(tenant, snapshot, snapshot);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task AnotherTenantsSnapshotCannotBeComparedAsync()
        {
            var tenantA = await harness.CreateTenantAsync("DfH");
            var tenantB = await harness.CreateTenantAsync("DfI");
            var foreign = await TakeAsync(tenantB, "foreign");

            var act = () => DiffAsync(tenantA, foreign, null);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task PreviewingAPackageWorksOutTheImpactWithoutChangingAnythingAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvA");
            var modelId = await harness.CreateModelAsync(tenant, "PvA");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor());
            await harness.CreateModelAsync(tenant, "PvA2");

            await using var before = fx.GetDbContext();
            var importsBefore = await before.Import.CountAsync(w => w.TenantRegistryId == tenant.Id);
            var modelsBefore = await before.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id);

            var preview = await PreviewPackageAsync(tenant, bytes);

            preview.Diff.DiffKindId.Should().Be(PreservationDiffKind.PackageImportPreview);
            preview.Diff.Removed.Should().BeGreaterThanOrEqualTo(1);
            preview.PackageVersion.Should().Be(2);
            preview.PackageGuid.Should().NotBeEmpty();

            await using var after = fx.GetDbContext();
            (await after.Import.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(importsBefore);
            (await after.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should()
                .Be(modelsBefore);
            (await after.EntityAnalysisModel.CountAsync(w => w.Id == modelId)).Should().Be(1);
        }

        [Fact]
        public async Task APackageWithoutRolesWouldOrphanTheTenantsUsersAndSaysSoAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvB");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor(roles: false));

            var preview = await PreviewPackageAsync(tenant, bytes);

            preview.Warnings.Should().Contain(w => w.Code == "NoRoles");
            preview.OrphanedUsers.Count.Should().Be(1);
            preview.OrphanedUsers.Names.Should().Equal(tenant.UserName);
            preview.OrphanedUsers.ImporterStaysActive.Should().BeTrue();
        }

        [Fact]
        public async Task APackageWithTheTenantsRolesLeavesNoUserOrphanedAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvC");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor(roles: true));

            var preview = await PreviewPackageAsync(tenant, bytes);

            preview.OrphanedUsers.Count.Should().Be(0);
            preview.Warnings.Should().NotContain(w => w.Code == "NoRoles");
            preview.Blockers.Should().BeEmpty();
        }

        [Fact]
        public async Task APackageWhoseRolesBelongToAnotherTenantIsFlaggedAsRefusedWhenUsersExistAsync()
        {
            var tenantA = await harness.CreateTenantAsync("PvD");
            var tenantB = await harness.CreateTenantAsync("PvE");
            var foreign = await harness.ExportAsync(tenantB, PreservationHarness.ExportOptionsFor(roles: true));

            var preview = await PreviewPackageAsync(tenantA, foreign);

            preview.Blockers.Should().Contain(b => b.Code == "RoleRekey");
        }

        [Fact]
        public async Task APackageWithoutAnApprovalHistoryIsWarnedAboutAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvF");
            var wrapper = PreservationHarness.Decrypt(await harness.ExportAsync(tenant,
                PreservationHarness.ExportOptionsFor()));
            wrapper.Payload!.EntityApproval = null;

            var preview = await PreviewPackageAsync(tenant, PreservationHarness.Encrypt(wrapper));

            preview.Warnings.Should().Contain(w => w.Code == "NoApprovalHistory");
        }

        [Fact]
        public async Task APackageFromAnOlderFormatIsWarnedAboutAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvG");
            var wrapper = PreservationHarness.Decrypt(await harness.ExportAsync(tenant,
                PreservationHarness.ExportOptionsFor()));
            wrapper.Version = 1;

            var preview = await PreviewPackageAsync(tenant, PreservationHarness.Encrypt(wrapper));

            preview.PackageVersion.Should().Be(1);
            preview.Warnings.Should().Contain(w => w.Code == "OlderFormat");
        }

        [Fact]
        public async Task APreviewWithTheWrongPasswordIsRefusedAsAnInvalidFileAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvH");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor());

            var act = () => PreviewPackageAsync(tenant, bytes, "wrong password");

            await act.Should().ThrowAsync<InvalidPreservationFileException>();
        }

        [Fact]
        public async Task APreviewWithNoFileIsRefusedAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvI");
            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildServiceAsync(db, tenant.UserName);

            var act = () => service.ImportPreviewAsync([], PreservationHarness.Password);

            await act.Should().ThrowAsync<NoFileException>();
        }

        [Fact]
        public async Task PreviewingASnapshotRestoreShowsWhatItWouldReplaceAsync()
        {
            var tenant = await harness.CreateTenantAsync("PvJ");
            var snapshot = await TakeAsync(tenant, "restore point");
            await harness.CreateModelAsync(tenant, "PvJ");

            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildSnapshotServiceAsync(db, tenant.UserName);

            var preview = await service.ImportPreviewAsync(snapshot);

            preview.Diff.DiffKindId.Should().Be(PreservationDiffKind.SnapshotRestorePreview);
            preview.Diff.Left.Label.Should().Be("Live");
            preview.Diff.Right.Label.Should().Be("restore point");
            preview.Diff.Removed.Should().BeGreaterThanOrEqualTo(1);
            preview.OrphanedUsers.Count.Should().Be(0);

            var row = await db.PreservationDiff.SingleAsync(w => w.Id == preview.Diff.Id);
            row.DiffKindId.Should().Be((byte)PreservationDiffKind.SnapshotRestorePreview);
            row.RightSnapshotId.Should().Be(snapshot);
        }

        [Fact]
        public async Task ImportingTakesASnapshotFirstSoTheImportCanBeUndoneAsync()
        {
            var tenant = await harness.CreateTenantAsync("SnA");
            await harness.CreateModelAsync(tenant, "SnA");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor(roles: true));

            await harness.ImportAsync(tenant, bytes);

            await using var db = fx.GetDbContext();
            var taken = await db.PreservationSnapshot
                .Where(w => w.TenantRegistryId == tenant.Id && w.SnapshotSourceId == 4).ToListAsync();

            taken.Should().ContainSingle();
            taken[0].Name.Should().StartWith("Before import of ");
            taken[0].InError.Should().NotBe(1);
            taken[0].EntityAnalysisModelCount.Should().Be(1);
        }

        [Fact]
        public async Task TheSummaryCountsWhatAnExportWouldCarryAsync()
        {
            var tenant = await harness.CreateTenantAsync("SmA");
            await harness.CreateModelAsync(tenant, "SmA");

            await using var db = fx.GetDbContext();
            var service = await PreservationHarness.BuildServiceAsync(db, tenant.UserName);

            var summary = await service.SummaryAsync();

            summary.Sections.Select(s => s.Key).Should().Contain(["models", "lists", "dictionaries", "roles"]);
            summary.Sections.Single(s => s.Key == "models").Count.Should().Be(1);
            summary.Sections.Single(s => s.Key == "roles").Count.Should().Be(1);
            summary.TotalBytes.Should().BeGreaterThan(0);
            summary.MinimumPasswordLength.Should().Be(12);
        }
    }
}

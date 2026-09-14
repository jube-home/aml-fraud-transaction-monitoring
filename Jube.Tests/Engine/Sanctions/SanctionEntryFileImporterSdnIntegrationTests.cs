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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("SanctionsSdnLoad")]
    public sealed class SanctionEntryFileImporterSdnIntegrationTests(SanctionsSdnLoadFixture fx)
    {
        [Fact]
        public void TheLoadParsedEveryRowAndRejectedExactlyTheOneKnownMalformedTrailingLine()
        {
            fx.ImportResult.TotalRows.Should().Be(fx.ImportResult.RejectedRows + fx.InsertedCount);
            fx.ImportResult.RejectedRows.Should().Be(1);
            fx.InsertedCount.Should().BeGreaterThan(19000);
        }

        [Fact]
        public void OnItsFirstEverLoadEveryPersistedRowWasAFreshInsertNotARevivalOrNoOp()
        {
            fx.RevivedCount.Should().Be(0);
            fx.UnchangedCount.Should().Be(0);
        }

        [Fact]
        public void EveryHashParsedFromTheFileIsUnique()
        {
            fx.ImportResult.Hashes.Should().HaveCount(fx.InsertedCount);
        }

        [Fact]
        public async Task ThePersistedRowCountForTheSourceExactlyMatchesTheInsertedCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            var active = (await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId)).ToList();

            active.Should().HaveCount(fx.InsertedCount);
            active.Should().OnlyContain(e => e.Deleted == null);
        }

        [Fact]
        public async Task EveryPersistedRowHasANonEmptyHashElementValueAndReferenceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId);

            active.Should().OnlyContain(e =>
                !string.IsNullOrEmpty(e.SanctionEntryHash) &&
                !string.IsNullOrEmpty(e.SanctionEntryElementValue) &&
                !string.IsNullOrEmpty(e.SanctionEntryReference));
        }

        [Fact]
        public async Task NoTwoPersistedRowsShareTheSameHashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            var active = (await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId)).ToList();

            active.Select(e => e.SanctionEntryHash).Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData("36", "AEROCARIBBEAN AIRLINES")]
        [InlineData("173", "ANGLO-CARIBBEAN CO., LTD.")]
        [InlineData("306", "BANCO NACIONAL DE CUBA")]
        [InlineData("58562", "AL ABADA, Khaldoon Naser Maryoosh")]
        public async Task KnownRealSdnEntriesArePersistedWithTheirExactNameAsync(string reference, string name)
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var active = (await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId)).ToList();

            var entry = active.Should().ContainSingle(e => e.SanctionEntryReference == reference).Subject;
            entry.SanctionEntryElementValue.Should().Be(name);
        }

        [Fact]
        public async Task AnEntryWithAnEmbeddedCommaInItsQuotedNameIsNotTruncatedAtTheCommaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId);

            var entry = active.Should().ContainSingle(e => e.SanctionEntryReference == "173").Subject;
            entry.SanctionEntryElementValue.Should().Contain(",");
            entry.SanctionEntryElementValue.Should().Be("ANGLO-CARIBBEAN CO., LTD.");
        }

        [Fact]
        public async Task ThePayloadColumnRetainsTheFullOriginalRawRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(fx.SourceId);

            var entry = active.Should().ContainSingle(e => e.SanctionEntryReference == "36").Subject;
            entry.SanctionPayload.Should().Contain("AEROCARIBBEAN AIRLINES");
            entry.SanctionPayload.Should().Contain("CUBA");
        }

        [Fact]
        public async Task TheImportAuditRowRecordsCountsThatAgreeWithWhatWasActuallyPersistedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var import = await dbContext.SanctionEntryImport.FirstAsync(w => w.Id == fx.ImportId);

            import.Successful.Should().Be(1);
            import.TotalRows.Should().Be(fx.ImportResult.TotalRows);
            import.InsertedCount.Should().Be(fx.InsertedCount);
            import.RevivedCount.Should().Be(0);
            import.UnchangedCount.Should().Be(0);
            import.RejectedCount.Should().Be(1);
            import.EndDate.Should().NotBeNull();
            import.StartDate.Should().NotBeNull();
            import.EndDate.Should().BeOnOrAfter(import.StartDate!.Value);
        }

        [Fact]
        public async Task TheRejectionAuditRowMatchesTheOneKnownMalformedTrailingLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var rejectionRepository = new SanctionEntryRejectionRepository(dbContext);

            var rejections = (await rejectionRepository.GetBySanctionEntryImportIdAsync(fx.ImportId)).ToList();

            rejections.Should().ContainSingle();
            rejections[0].ReasonId.Should().Be((int)SanctionEntryRejectionReason.InsufficientFields);
        }

        [Fact]
        public async Task GetAsyncAcrossAllSourcesStillIncludesOurIsolatedSourcesRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            var all = await repository.GetAsync();

            all.Count(e => e.SanctionEntrySourceId == fx.SourceId).Should().Be(fx.InsertedCount);
        }
    }
}
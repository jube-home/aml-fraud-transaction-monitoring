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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionEntryRejectionRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private int importId;
        private int sourceId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            sourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}SanctionSource{Guid.NewGuid():N}"[..40],
                Delimiter = ',',
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                Skip = 0
            });

            importId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = sourceId,
                StartDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionEntryRejection.Where(w => w.SanctionEntryImportId == importId).DeleteAsync();
            await dbContext.SanctionEntryImport.Where(w => w.Id == importId).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
        }

        private SanctionEntryRejection NewRejection(int rowNumber, SanctionEntryRejectionReason reason,
            string rawData = "bad,row")
        {
            return new SanctionEntryRejection
            {
                SanctionEntryImportId = importId,
                SanctionEntrySourceId = sourceId,
                RowNumber = rowNumber,
                RawData = rawData,
                ReasonId = (int)reason,
                CreatedDate = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task InsertAsyncPersistsTheRejectionWithItsReasonAndRawDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryRejectionRepository(dbContext);

            var inserted = await repository.InsertAsync(
                NewRejection(42, SanctionEntryRejectionReason.InsufficientFields, "onlyonefield"));

            inserted.Id.Should().BeGreaterThan(0);
            var persisted = await repository.GetByIdAsync(inserted.Id);
            persisted.Should().NotBeNull();
            persisted.RowNumber.Should().Be(42);
            persisted.RawData.Should().Be("onlyonefield");
            persisted.ReasonId.Should().Be((int)SanctionEntryRejectionReason.InsufficientFields);
        }

        [Theory]
        [InlineData(SanctionEntryRejectionReason.InsufficientFields)]
        [InlineData(SanctionEntryRejectionReason.NoReferenceIndexConfigured)]
        [InlineData(SanctionEntryRejectionReason.ParseError)]
        public async Task InsertAsyncRoundTripsEveryRejectionReasonAsync(SanctionEntryRejectionReason reason)
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryRejectionRepository(dbContext);

            var inserted = await repository.InsertAsync(NewRejection(1, reason));

            var persisted = await repository.GetByIdAsync(inserted.Id);
            persisted!.ReasonId.Should().Be((int)reason);
        }

        [Fact]
        public async Task GetBySanctionEntryImportIdAsyncReturnsAllRejectionsForThatImportInAnyOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryRejectionRepository(dbContext);
            await repository.InsertAsync(NewRejection(1, SanctionEntryRejectionReason.InsufficientFields));
            await repository.InsertAsync(NewRejection(2, SanctionEntryRejectionReason.ParseError));
            await repository.InsertAsync(NewRejection(3, SanctionEntryRejectionReason.NoReferenceIndexConfigured));

            var results = (await repository.GetBySanctionEntryImportIdAsync(importId)).ToList();

            results.Should().HaveCount(3);
            results.Select(r => r.RowNumber).Should().BeEquivalentTo([1, 2, 3]);
        }

        [Fact]
        public async Task GetBySanctionEntryImportIdAsyncForAnImportWithNoRejectionsReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryRejectionRepository(dbContext);

            var results = await repository.GetBySanctionEntryImportIdAsync(importId);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsyncForAMissingIdReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryRejectionRepository(dbContext);

            var result = await repository.GetByIdAsync(int.MaxValue - 1);

            result.Should().BeNull();
        }
    }
}
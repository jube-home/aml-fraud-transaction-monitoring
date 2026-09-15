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
    public sealed class SanctionEntryImportRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
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
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
        }

        private SanctionEntryImport NewImport(byte? successful = null)
        {
            return new SanctionEntryImport
            {
                SanctionEntrySourceId = sourceId,
                StartDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow,
                Successful = successful
            };
        }

        [Fact]
        public async Task InsertAsyncPersistsAndAssignsAnIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryImportRepository(dbContext);

            var inserted = await repository.InsertAsync(NewImport());

            inserted.Id.Should().BeGreaterThan(0);
            var persisted = await repository.GetByIdAsync(inserted.Id);
            persisted.Should().NotBeNull();
            persisted.SanctionEntrySourceId.Should().Be(sourceId);
        }

        [Fact]
        public async Task UpdateAsyncPersistsTheCompletionCountsAndOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryImportRepository(dbContext);
            var inserted = await repository.InsertAsync(NewImport());

            inserted.EndDate = DateTime.UtcNow;
            inserted.TotalRows = 100;
            inserted.InsertedCount = 90;
            inserted.RevivedCount = 5;
            inserted.UnchangedCount = 3;
            inserted.RemovedCount = 1;
            inserted.RejectedCount = 1;
            inserted.Successful = 1;

            await repository.UpdateAsync(inserted);

            var persisted = await repository.GetByIdAsync(inserted.Id);
            persisted!.TotalRows.Should().Be(100);
            persisted.InsertedCount.Should().Be(90);
            persisted.RevivedCount.Should().Be(5);
            persisted.UnchangedCount.Should().Be(3);
            persisted.RemovedCount.Should().Be(1);
            persisted.RejectedCount.Should().Be(1);
            persisted.Successful.Should().Be(1);
            persisted.EndDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsyncPersistsAFailureWithAnErrorMessageAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryImportRepository(dbContext);
            var inserted = await repository.InsertAsync(NewImport());

            inserted.EndDate = DateTime.UtcNow;
            inserted.Successful = 0;
            inserted.ErrorMessage = "HTTP request to OFAC timed out";

            await repository.UpdateAsync(inserted);

            var persisted = await repository.GetByIdAsync(inserted.Id);
            persisted!.Successful.Should().Be(0);
            persisted.ErrorMessage.Should().Be("HTTP request to OFAC timed out");
        }

        [Fact]
        public async Task GetBySanctionEntrySourceIdAsyncReturnsOnlyImportsForThatSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryImportRepository(dbContext);
            var otherSourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}SanctionSourceOther{Guid.NewGuid():N}"[..40],
                Delimiter = ',',
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                Skip = 0
            });

            try
            {
                var ours = await repository.InsertAsync(NewImport());
                var other = await repository.InsertAsync(new SanctionEntryImport
                {
                    SanctionEntrySourceId = otherSourceId,
                    StartDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow
                });

                var results = (await repository.GetBySanctionEntrySourceIdAsync(sourceId)).ToList();

                results.Should().Contain(i => i.Id == ours.Id);
                results.Should().NotContain(i => i.Id == other.Id);
            }
            finally
            {
                await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == otherSourceId)
                    .DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == otherSourceId).DeleteAsync();
            }
        }

        [Fact]
        public async Task GetByIdAsyncForAMissingIdReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntryImportRepository(dbContext);

            var result = await repository.GetByIdAsync(int.MaxValue - 1);

            result.Should().BeNull();
        }
    }
}
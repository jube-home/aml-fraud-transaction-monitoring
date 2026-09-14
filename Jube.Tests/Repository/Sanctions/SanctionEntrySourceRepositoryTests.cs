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
    public sealed class SanctionEntrySourceRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private int sourceId;
        private string sourceName = null!;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            sourceName = $"{DatabaseFixture.Prefix}SanctionSource{Guid.NewGuid():N}"[..40];

            sourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = sourceName,
                Severity = 2,
                Delimiter = ';',
                MultiPartStringIndex = "3",
                ReferenceIndex = 8,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 0,
                DirectoryLocation = "/tmp/does-not-matter",
                Skip = 1
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
        }

        [Fact]
        public async Task GetByIdAsyncReturnsThePersistedConfigurationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntrySourceRepository(dbContext);

            var result = await repository.GetByIdAsync(sourceId);

            result.Should().NotBeNull();
            result.Name.Should().Be(sourceName);
            result.Severity.Should().Be(2);
            result.Delimiter.Should().Be(';');
            result.MultiPartStringIndex.Should().Be("3");
            result.ReferenceIndex.Should().Be(8);
            result.Skip.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsyncForAMissingIdReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntrySourceRepository(dbContext);

            var result = await repository.GetByIdAsync(int.MaxValue - 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncIncludesEveryConfiguredSourceIncludingOursAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionEntrySourceRepository(dbContext);

            var all = (await repository.GetAsync()).ToList();

            all.Should().Contain(s => s.Id == sourceId && s.Name == sourceName);
        }
    }
}
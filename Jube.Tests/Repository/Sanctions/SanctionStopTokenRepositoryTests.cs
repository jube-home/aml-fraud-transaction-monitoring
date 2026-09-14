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
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Repository.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionStopTokenRepositoryTests(DatabaseFixture fx)
    {
        [Fact]
        public async Task GetAsyncReturnsTheSeededBaselineHonorificsAndTitlesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionStopTokenRepository(dbContext);

            var tokens = (await repository.GetAsync()).ToList();

            tokens.Should().NotBeEmpty();
            tokens.Should().Contain(t => t.Token == "sheikh");
            tokens.Should().Contain(t => t.Token == "imam");
            tokens.Should().Contain(t => t.Token == "hajji");
        }

        [Fact]
        public async Task GetAsyncOnlyReturnsTheTwoDocumentedCategoriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionStopTokenRepository(dbContext);

            var tokens = await repository.GetAsync();

            tokens.Should().OnlyContain(t => t.CategoryId == 1 || t.CategoryId == 2);
        }

        [Fact]
        public async Task GetAsyncTokensAreAllLowercaseAsStoredByTheMigrationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionStopTokenRepository(dbContext);

            var tokens = await repository.GetAsync();

            tokens.Should().OnlyContain(t => t.Token == t.Token.ToLowerInvariant());
        }

        [Fact]
        public async Task GetAsyncTokensAreAllUniqueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionStopTokenRepository(dbContext);

            var tokens = (await repository.GetAsync()).ToList();

            tokens.Select(t => t.Token).Should().OnlyHaveUniqueItems();
        }
    }
}
/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class DynamicSqlQueryLogRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<string> createdUsers = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (createdUsers.Count == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();
            await dbContext.DynamicSqlQueryLog.Where(w => createdUsers.Contains(w.CreatedUser)).DeleteAsync();
        }

        private string NewUserName()
        {
            var userName = $"{DatabaseFixture.Prefix}dynsqllog-{Guid.NewGuid():N}";
            createdUsers.Add(userName);

            return userName;
        }

        [Fact]
        public async Task InsertAsyncPersistsASuccessfulRunAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new DynamicSqlQueryLogRepository(dbContext);
            var userName = NewUserName();

            await repository.InsertAsync(
                "SELECT 1", null, userName, true, 3, 42,
                null);

            var row = (await dbContext.DynamicSqlQueryLog.Where(w => w.CreatedUser == userName).ToListAsync())
                .Single();

            row.Sql.Should().Be("SELECT 1");
            row.Succeeded.Should().BeTrue();
            row.RowCount.Should().Be(3);
            row.DurationMs.Should().Be(42);
            row.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task InsertAsyncPersistsAFailedRunWithTheErrorMessageAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new DynamicSqlQueryLogRepository(dbContext);
            var userName = NewUserName();

            await repository.InsertAsync(
                "SELECT * FROM \"NotAllowed\"", null, userName, false,
                null, 7, "Table \"NotAllowed\" is not permitted.");

            var row = (await dbContext.DynamicSqlQueryLog.Where(w => w.CreatedUser == userName).ToListAsync())
                .Single();

            row.Succeeded.Should().BeFalse();
            row.RowCount.Should().BeNull();
            row.ErrorMessage.Should().Contain("NotAllowed");
        }
    }
}
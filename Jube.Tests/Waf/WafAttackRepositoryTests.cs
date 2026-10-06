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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class WafAttackRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string marker = "ZzWafTest" + Guid.NewGuid().ToString("N")[..8];
        private DateTime baseTime;

        public async Task InitializeAsync()
        {
            baseTime = DateTime.UtcNow;
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(Row("OWASP-XSS-SCRIPT", "A7-XSS", "/api/Alpha",
                "<script>alert(1)</script>", "Dropped", baseTime.AddMinutes(-3)));
            await dbContext.InsertAsync(Row("OWASP-SQLI-UNION", "A1-Injection", "/api/Beta",
                "1 UNION SELECT x", "Dropped", baseTime.AddMinutes(-2)));
            await dbContext.InsertAsync(Row("OWASP-TRAVERSAL-DOTDOT", "A5-BrokenAccessControl", "/api/Gamma",
                "../../etc/passwd", "Detected", baseTime.AddMinutes(-1)));
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.WafAttack.Where(w => w.CorrelationId == marker).DeleteAsync();
        }

        private WafAttack Row(string signature, string category, string route, string value, string action,
            DateTime createdDate)
        {
            return new WafAttack
            {
                CreatedDate = createdDate,
                Transport = "Http",
                Route = route,
                Method = "POST",
                RemoteIp = "127.0.0.1",
                UserName = "tester",
                SignatureName = signature,
                Category = category,
                MatchedField = "$.field",
                MatchedValue = value,
                Action = action,
                CorrelationId = marker,
                Instance = "test"
            };
        }

        private async Task<List<WafAttack>> MineAsync(IEnumerable<WafAttack> rows)
        {
            await Task.CompletedTask;
            return [.. rows.Where(w => w.CorrelationId == marker)];
        }

        [Fact]
        public async Task GetLastReturnsTheInsertedRowsWithinTheDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var rows = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                null, null, "createdDate", "desc");

            (await MineAsync(rows)).Should().HaveCount(3);
        }

        [Fact]
        public async Task ADateRangeExcludesOlderRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var rows = await repository.GetLastAsync(100000, baseTime.AddMinutes(-90), baseTime.AddSeconds(-90),
                null, null, "createdDate", "desc");

            var mine = await MineAsync(rows);
            mine.Should().HaveCount(2);
            mine.Should().OnlyContain(w => w.SignatureName != "OWASP-TRAVERSAL-DOTDOT");
        }

        [Fact]
        public async Task SearchMatchesTheMatchedValueCaseInsensitivelyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var rows = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                "UNION", null, "createdDate", "desc");

            var mine = await MineAsync(rows);
            mine.Should().ContainSingle();
            mine[0].SignatureName.Should().Be("OWASP-SQLI-UNION");
        }

        [Fact]
        public async Task SearchMatchesTheRouteAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var rows = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                "gamma", null, "createdDate", "desc");

            var mine = await MineAsync(rows);
            mine.Should().ContainSingle();
            mine[0].Route.Should().Be("/api/Gamma");
        }

        [Fact]
        public async Task CountReflectsTheSearchFilterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var all = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                null, null, null, null);
            var traversal = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                "passwd", null, null, null);

            (await MineAsync(all)).Should().HaveCount(3);
            (await MineAsync(traversal)).Should().ContainSingle();
        }

        [Fact]
        public async Task SortAscendingOrdersByCreatedDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new WafAttackRepository(dbContext);

            var rows = await repository.GetLastAsync(100000, baseTime.AddMinutes(-10), baseTime.AddMinutes(1),
                null, null, "createdDate", "asc");

            var mine = await MineAsync(rows);
            mine.Should().BeInAscendingOrder(w => w.CreatedDate);
        }
    }
}
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
using Jube.Service.Exceptions.WafAttack;
using Jube.Service.Reactivity;
using Jube.Service.WafAttack;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.WafAttack
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class WafAttackServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly string marker = DatabaseFixture.Prefix + "Waf" + Guid.NewGuid().ToString("N")[..8];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.WafAttack.Where(w => w.CorrelationId == marker).DeleteAsync();
        }

        private async Task InsertAsync(string value)
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(new Data.Poco.WafAttack
            {
                CreatedDate = DateTime.UtcNow,
                Transport = "Http",
                Route = "/api/test",
                Method = "POST",
                RemoteIp = "127.0.0.1",
                UserName = "tester",
                SignatureName = "Test signature",
                Category = "Test",
                MatchedField = "$.field",
                MatchedValue = value,
                Action = "Detected",
                CorrelationId = marker,
                Instance = "test"
            });
        }

        [Fact]
        public async Task ListMasksCredentialsInTheStoredMatchedValueAsync()
        {
            await InsertAsync("{\"password\":\"hunter2pw\",\"x\":\"' or 1=1--\"}");
            await InsertAsync("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.c2lnbmF0dXJl");
            await InsertAsync("<script>alert(1)</script>");

            await using var dbContext = fx.GetDbContext();
            var service = await WafAttackService.CreateAsync(dbContext, fx.Seed.LandlordUser, TestLog.NoOp, localizers,
                new NullServiceChangeBus());

            var result = await service.ListAsync(search: "Test signature");

            var mine = result.Rows.Where(r => r.CorrelationId == marker).ToList();
            mine.Should().HaveCount(3);
            mine.Select(r => r.MatchedValue).Should().NotContain(v => v.Contains("hunter2pw"));
            mine.Select(r => r.MatchedValue).Should().NotContain(v => v.Contains("eyJhbGciOiJIUzI1NiJ9"));
            mine.Select(r => r.MatchedValue).Should().Contain("<script>alert(1)</script>");
            mine.Select(r => r.MatchedValue).Should().Contain(v => v.Contains("[REDACTED]"));
        }

        [Fact]
        public async Task ListWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await WafAttackService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp,
                localizers, new NullServiceChangeBus());

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }
    }
}

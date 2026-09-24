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
using Jube.Service.Exceptions.Query.EntityAnalysisModelTimeWindow;
using Jube.Service.Query.EntityAnalysisModelTimeWindow;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelTimeWindow
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelTimeWindowServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly DateTime reference = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        private int abstractionRuleId;
        private int foreverCounterId;
        private int modelId;
        private int nonSearchRuleId;
        private int onlineCounterId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            modelId = (await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Tw{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                })).Id;
            await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId, Name = "AccountId", DataTypeId = 1, XPath = "$.AccountId",
                Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid(), SearchKey = 1,
                SearchKeyTtlInterval = "h", SearchKeyTtlIntervalValue = 12
            });
            abstractionRuleId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelAbstractionRule
                {
                    EntityAnalysisModelId = modelId, Name = "CountLastDay", Search = 1, SearchKey = "AccountId",
                    SearchInterval = "d", SearchValue = 1, SearchFunctionTypeId = 1, Active = 1, Deleted = 0,
                    Guid = Guid.NewGuid()
                });
            nonSearchRuleId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelAbstractionRule
                {
                    EntityAnalysisModelId = modelId, Name = "NotSearch", Search = 0, Active = 1, Deleted = 0,
                    Guid = Guid.NewGuid()
                });
            onlineCounterId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "Online", TtlCounterDataName = "AccountId",
                TtlCounterInterval = "m", TtlCounterValue = 1, OnlineAggregation = 1, Active = 1, Deleted = 0,
                Guid = Guid.NewGuid()
            });
            foreverCounterId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId, Name = "Forever", TtlCounterDataName = "AccountId",
                TtlCounterInterval = "d", TtlCounterValue = 1, EnableLiveForever = 1, Active = 1, Deleted = 0,
                Guid = Guid.NewGuid()
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelAbstractionRule>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTtlCounter>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        private Task<EntityAnalysisModelTimeWindowService> ServiceAsync(Data.Context.DbContext dbContext,
            string? userName = null)
        {
            return EntityAnalysisModelTimeWindowService.CreateAsync(dbContext,
                userName ?? fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        [Fact]
        public async Task ACalendarWindowEndsAtTheReferenceDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var window = await service.CalculateAsync("calendar", "w", 2, reference);

            window.Valid.Should().BeTrue();
            (window.From, window.To).Should().Be((reference.AddDays(-14), reference));
            window.LengthSeconds.Should().Be(14 * 86400);
            window.IntervalName.Should().Be("weeks");
        }

        [Fact]
        public async Task ADateWithoutAKindIsTakenAsUtcAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var window = await service.CalculateAsync("TtlCounter", "h", 1,
                DateTime.SpecifyKind(reference, DateTimeKind.Unspecified));

            window.To!.Value.Kind.Should().Be(DateTimeKind.Utc);
            window.From.Should().Be(reference.AddHours(-1));
        }

        [Fact]
        public async Task AnAbstractionRuleWindowIsShortenedToTheSearchKeysTtlAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var window = await service.AbstractionRuleAsync(abstractionRuleId, reference);

            window.Valid.Should().BeTrue();
            window.SearchKeyShortensTheWindow.Should().BeTrue();
            window.From.Should().Be(reference.AddHours(-12));
            window.Note.Should().Contain("AccountId");
            (await service.AbstractionRuleAsync(nonSearchRuleId, reference)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "NotASearchRule");
        }

        [Fact]
        public async Task TtlCounterWindowsFollowTheCountersSettingsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var online = await service.TtlCounterAsync(onlineCounterId, reference);
            var forever = await service.TtlCounterAsync(foreverCounterId, reference);

            online.From.Should().Be(reference.AddMonths(-1));
            online.Note.Should().Contain("online aggregation");
            forever.LiveForever.Should().BeTrue();
            forever.From.Should().BeNull();
        }

        [Fact]
        public async Task InvalidInputComesBackAsErrorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            (await service.CalculateAsync("Fortnight", "d", 1)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "KindInvalid");
            (await service.CalculateAsync("AbstractionRule", "m", 1)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "IntervalInvalid" && e.Message.Contains("s, n, h, d"));
            (await service.CalculateAsync("Calendar", "d", -1)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "ValueInvalid");
        }

        [Fact]
        public async Task TheIntervalsListEveryKindAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var kinds = await service.IntervalsAsync();

            kinds.Select(k => k.Kind).Should().BeEquivalentTo("AbstractionRule", "SearchKey", "TtlCounter", "Calendar");
            kinds.Single(k => k.Kind == "TtlCounter").Intervals.Select(i => i.Code).Should()
                .Equal("s", "n", "h", "d", "m", "y");
        }

        [Fact]
        public async Task AnotherTenantsRuleIsNotFoundAndNoPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserTenantB)).AbstractionRuleAsync(abstractionRuleId));
            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission)).CalculateAsync("Calendar", "d",
                    1));
        }
    }
}
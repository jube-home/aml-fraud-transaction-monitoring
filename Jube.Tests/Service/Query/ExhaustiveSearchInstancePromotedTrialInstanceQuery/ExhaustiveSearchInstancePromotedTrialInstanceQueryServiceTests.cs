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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceQueryServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPromotedIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdPromotedIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var id in createdTrialInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceQueryService> BuildServiceAsync(
            DbContext dbContext, string? userName,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceQueryService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateSearchInstanceAsync(DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            createdModelIds.Add(model.Id);

            var searchInstance = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 0
                });
            createdSearchInstanceIds.Add(searchInstance.Id);
            return searchInstance.Id;
        }

        private async Task<(int Id, DateTime CreatedDate)> CreatePromotedAsync(DbContext dbContext,
            int searchInstanceId, double score, int complexity, bool active, string json)
        {
            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstanceId,
                    CreatedDate = DateTime.UtcNow
                });
            createdTrialInstanceIds.Add(trial.Id);

            var createdDate = DateTime.UtcNow;
            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                    Active = (byte)(active ? 1 : 0),
                    Score = score,
                    TopologyComplexity = complexity,
                    Json = json,
                    CreatedDate = createdDate
                });
            createdPromotedIds.Add(promoted.Id);
            return (promoted.Id, createdDate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                ExhaustiveSearchInstancePromotedTrialInstanceQueryService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(),
                    TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldExactlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var (id, createdDate) = await CreatePromotedAsync(dbContext, searchId, 0.8125, 7, true, "{\"a\":1}");
            var trialId = await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.Id == id).Select(s => s.ExhaustiveSearchInstanceTrialInstanceId).FirstAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchId);

            var dto = result.Should().ContainSingle().Subject;
            dto.Id.Should().Be(id);
            dto.ExhaustiveSearchInstanceTrialInstanceId.Should().Be(trialId.Required());
            dto.Active.Should().BeTrue();
            dto.Score.Should().Be(0.8125);
            dto.TopologyComplexity.Should().Be(7);
            Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.Parse(dto.Json.Required()),
                Newtonsoft.Json.Linq.JToken.Parse("{\"a\":1}")).Should().BeTrue();
            dto.CreatedDate.Should().BeCloseTo(createdDate, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task GetAsyncMapsInactiveAsFalseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            await CreatePromotedAsync(dbContext, searchId, 0.1, 1, false, "{}");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchId);

            result.Should().ContainSingle().Which.Active.Should().BeFalse();
        }

        [Fact]
        public async Task GetAsyncOrdersByIdDescendingAndFiltersToRequestedSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var otherSearchId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await CreatePromotedAsync(dbContext, searchId, 0.1, 1, false, "{}");
            var second = await CreatePromotedAsync(dbContext, searchId, 0.2, 2, false, "{}");
            await CreatePromotedAsync(dbContext, otherSearchId, 0.3, 3, false, "{}");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchId);

            result.Select(r => r.Id).Should().Equal(second.Id, first.Id);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForSearchInstanceWithNoPromotedRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchB = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserTenantB);
            await CreatePromotedAsync(dbContext, searchB, 0.9, 9, true, "{}");
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantADataIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchA = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            await CreatePromotedAsync(dbContext, searchA, 0.9, 9, true, "{}");
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOwnDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchA = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var searchB = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserTenantB);
            var a = await CreatePromotedAsync(dbContext, searchA, 0.1, 1, false, "{}");
            var b = await CreatePromotedAsync(dbContext, searchB, 0.2, 2, false, "{}");

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(searchA)).Select(r => r.Id).Should().Equal(a.Id);
            (await serviceB.GetAsync(searchB)).Select(r => r.Id).Should().Equal(b.Id);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(1, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(1);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(1);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceQueryGet");
        }
    }
}
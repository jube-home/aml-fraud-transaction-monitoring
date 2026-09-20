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
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram;
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
using PocoSearchInstance = Jube.Data.Poco.ExhaustiveSearchInstance;
using PocoPredictedActual = Jube.Data.Poco.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPredictedActualIds = [];
        private readonly List<int> createdPromotedTrialInstanceIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<PocoPredictedActual>()
                .Where(w => createdPredictedActualIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => createdPromotedTrialInstanceIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => createdTrialInstanceIds.Contains(w.Id)).DeleteAsync();

            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<PocoSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> SeedAsync(DbContext dbContext, string createdUser, double[] values,
            bool promotedActive = true)
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
                new PocoSearchInstance
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

            var trialInstance = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstance.Id,
                    CreatedDate = DateTime.UtcNow
                });
            createdTrialInstanceIds.Add(trialInstance.Id);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                    Active = (byte)(promotedActive ? 1 : 0),
                    Score = 0.5,
                    Json = "{}"
                });
            createdPromotedTrialInstanceIds.Add(promoted.Id);

            foreach (var error in values)
            {
                var id = await dbContext.InsertWithInt32IdentityAsync(
                    new PocoPredictedActual
                    {
                        Predicted = 1.0,
                        Actual = 1.0 + error,
                        ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id
                    });
                createdPredictedActualIds.Add(id);
            }

            return searchInstance.Id;
        }

        private static readonly double[] errors = [0, 0, 1, 1, 1, 2, 5, 5, 9, 10];

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramService.CreateAsync(dbContext, userName, log,
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
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(instanceId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncReturnsTenBinsWhoseFrequenciesSumToTheErrorCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(instanceId);

            result.Should().HaveCount(10);
            result.Sum(s => s.Frequency).Should().Be(errors.Length);
            result.Select(s => s.Bin).Should().BeInAscendingOrder();
            result[0].Bin.Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldExactlyFromTheDataLayerQueryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var expected = (await new global::Jube.Data.Query
                .GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramQuery(dbContext,
                    fx.Seed.UserWithPermission).ExecuteAsync(instanceId)).ToList();
            var result = await service.GetAsync(instanceId);

            result.Should().HaveCount(expected.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                result[i].Bin.Should().Be(expected[i].Bin);
                result[i].Frequency.Should().Be(expected[i].Frequency);
            }
        }

        [Fact]
        public async Task GetAsyncUsesOnlyTheActivePromotedTrialInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors, false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(instanceId);

            result.Sum(s => s.Frequency).Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncForUnknownInstanceReturnsNoFrequencyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(int.MaxValue);

            result.Sum(s => s.Frequency).Should().Be(0);
        }

        [Fact]
        public async Task TenantBInstanceIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var instanceB = await SeedAsync(dbContext, fx.Seed.UserTenantB, [1, 2, 3]);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(instanceA)).Sum(s => s.Frequency).Should().Be(errors.Length);
            (await serviceB.GetAsync(instanceB)).Sum(s => s.Frequency).Should().Be(3);
            (await serviceA.GetAsync(instanceB)).Sum(s => s.Frequency).Should().Be(0);
            (await serviceB.GetAsync(instanceA)).Sum(s => s.Frequency).Should().Be(0);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(instanceId, cts.Token));
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
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(instanceId);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var instanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, errors);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(instanceId);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(instanceId));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramGet");
        }
    }
}
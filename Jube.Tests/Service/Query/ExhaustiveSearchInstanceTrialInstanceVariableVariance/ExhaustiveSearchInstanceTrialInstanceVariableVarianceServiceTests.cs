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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance;
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

namespace Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstanceTrialInstanceVariableVarianceServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCollinearityIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdVariableIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.ExhaustiveSearchInstanceVariableMultiCollinearity
                .Where(w => createdCollinearityIds.Contains(w.Id)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstanceVariable
                .Where(w => createdVariableIds.Contains(w.Id)).DeleteAsync();
            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<global::Jube.Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstanceTrialInstanceVariableVarianceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstanceTrialInstanceVariableVarianceService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static string Nm(string suffix) => $"{DatabaseFixture.Prefix}Var{suffix}{Guid.NewGuid():N}"[..30];

        private async Task<int> InsertSearchInstanceAsync(DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new global::Jube.Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            createdModelIds.Add(model.Id);

            var searchInstance = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new global::Jube.Data.Poco.ExhaustiveSearchInstance
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

        private async Task<int> InsertVariableAsync(DbContext dbContext, int searchInstanceId, string name)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(
                new global::Jube.Data.Poco.ExhaustiveSearchInstanceVariable
                {
                    ExhaustiveSearchInstanceId = searchInstanceId,
                    Name = name,
                    Mean = 1,
                    StandardDeviation = 2,
                    Maximum = 3,
                    Minimum = 4,
                    NormalisationTypeId = 1,
                    ProcessingTypeId = 2,
                    VariableSequence = 1
                });
            createdVariableIds.Add(id);
            return id;
        }

        private async Task AddCorrelationAsync(DbContext dbContext, int variableId, int testVariableId,
            double correlation, int rank)
        {
            createdCollinearityIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstanceVariableMultiCollinearity
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    TestExhaustiveSearchInstanceVariableId = testVariableId,
                    Correlation = correlation,
                    CorrelationAbsRank = rank
                }));
        }

        private async Task<int> SeedAsync(DbContext dbContext, string createdUser,
            params (string Name, double Correlation, int Rank)[] correlations)
        {
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, createdUser);
            var subjectId = await InsertVariableAsync(dbContext, searchInstanceId, Nm("Subject"));
            foreach (var (name, correlation, rank) in correlations)
            {
                var testId = await InsertVariableAsync(dbContext, searchInstanceId, name);
                await AddCorrelationAsync(dbContext, subjectId, testId, correlation, rank);
            }

            return subjectId;
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
                ExhaustiveSearchInstanceTrialInstanceVariableVarianceService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(), TestLog.NoOp));

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
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (Nm("T"), 0.5, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(variableId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldFromSeededRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = Nm("Test");
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (name, -0.8125, 3));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(variableId);

            result.Should().ContainSingle();
            result[0].Name.Should().Be(name);
            result[0].Correlation.Should().Be(-0.8125);
            result[0].CorrelationAbsRank.Should().Be(3);
        }

        [Fact]
        public async Task GetAsyncOrdersByCorrelationAbsRankAscendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var first = Nm("R1");
            var second = Nm("R2");
            var third = Nm("R3");
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                (third, 0.1, 3), (first, 0.9, 1), (second, -0.5, 2));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(variableId);

            result.Select(r => r.Name).Should().Equal(first, second, third);
            result.Select(r => r.CorrelationAbsRank).Should().Equal(1, 2, 3);
        }

        [Fact]
        public async Task GetAsyncExcludesSoftDeletedCorrelationRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var kept = Nm("Kept");
            var gone = Nm("Gone");
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (kept, 0.5, 1), (gone, 0.9, 2));
            var goneVariableId = await dbContext.ExhaustiveSearchInstanceVariable.Where(w => w.Name == gone)
                .Select(s => s.Id).SingleAsync();
            await dbContext.ExhaustiveSearchInstanceVariableMultiCollinearity
                .Where(w => w.ExhaustiveSearchInstanceVariableId == variableId
                            && w.TestExhaustiveSearchInstanceVariableId == goneVariableId)
                .Set(w => w.Deleted, (byte?)1).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(variableId)).Select(r => r.Name).Should().Equal(kept);
        }

        [Fact]
        public async Task GetAsyncOnlyReturnsRowsForTheRequestedVariableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var mine = Nm("Mine");
            var other = Nm("Other");
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (mine, 0.2, 1));
            await SeedAsync(dbContext, fx.Seed.UserWithPermission, (other, 0.3, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(variableId);

            result.Select(r => r.Name).Should().Equal(mine);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyWhenVariableHasNoCorrelationsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(variableId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownVariableIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantBVariableId = await SeedAsync(dbContext, fx.Seed.UserTenantB, (Nm("B"), 0.7, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(tenantBVariableId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantADataIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAVariableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (Nm("A"), 0.7, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.GetAsync(tenantAVariableId)).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesItsOwnDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var nameA = Nm("A");
            var nameB = Nm("B");
            var variableA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (nameA, 0.1, 1));
            var variableB = await SeedAsync(dbContext, fx.Seed.UserTenantB, (nameB, 0.2, 1));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(variableA)).Select(r => r.Name).Should().Equal(nameA);
            (await serviceB.GetAsync(variableB)).Select(r => r.Name).Should().Equal(nameB);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (Nm("T"), 0.5, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(variableId, cts.Token));
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
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(1));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(1);

            var span = activities.Should()
                .ContainSingle(a => a.OperationName == "ExhaustiveSearchInstanceTrialInstanceVariableVariance.Get")
                .Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
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
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var variableId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, (Nm("T"), 0.5, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync(variableId);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.GetAsync(variableId));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("ExhaustiveSearchInstanceTrialInstanceVariableVarianceGet");
        }
    }
}
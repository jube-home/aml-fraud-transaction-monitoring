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
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceTrialInstanceVariable;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstanceTrialInstanceVariableServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPromotedIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];
        private readonly List<int> createdTrialVariableIds = [];
        private readonly List<int> createdVariableIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(w => createdTrialVariableIds.Contains(w.Id)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => createdPromotedIds.Contains(w.Id)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w => createdTrialInstanceIds.Contains(w.Id)).DeleteAsync();
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
                await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstanceTrialInstanceVariableService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstanceTrialInstanceVariableService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static VariableSpec Spec(string name, int sequence, int? removed = 0) =>
            new(name, 1, 2, 3, 4, 1, 2, 100 + sequence, sequence, removed);

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

        private async Task<int> InsertTrialInstanceAsync(DbContext dbContext, int searchInstanceId)
        {
            var trialInstance = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstanceId,
                    CreatedDate = DateTime.UtcNow
                });
            createdTrialInstanceIds.Add(trialInstance.Id);
            return trialInstance.Id;
        }

        private async Task PromoteAsync(DbContext dbContext, int trialInstanceId, byte active = 1)
        {
            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstanceId,
                    Active = active,
                    Score = 0.5,
                    Json = "{}"
                });
            createdPromotedIds.Add(promoted.Id);
        }

        private async Task AddVariablesAsync(DbContext dbContext, int searchInstanceId, int trialInstanceId,
            params VariableSpec[] variables)
        {
            foreach (var spec in variables)
            {
                var variableId = await dbContext.InsertWithInt32IdentityAsync(
                    new global::Jube.Data.Poco.ExhaustiveSearchInstanceVariable
                    {
                        ExhaustiveSearchInstanceId = searchInstanceId,
                        Name = spec.Name,
                        Mean = spec.Mean,
                        StandardDeviation = spec.StandardDeviation,
                        Maximum = spec.Maximum,
                        Minimum = spec.Minimum,
                        NormalisationTypeId = spec.NormalisationTypeId,
                        ProcessingTypeId = spec.ProcessingTypeId,
                        VariableSequence = spec.VariableSequence
                    });
                createdVariableIds.Add(variableId);

                createdTrialVariableIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new global::Jube.Data.Poco.ExhaustiveSearchInstanceTrialInstanceVariable
                    {
                        ExhaustiveSearchInstanceVariableId = variableId,
                        ExhaustiveSearchInstanceTrialInstanceId = trialInstanceId,
                        Removed = spec.Removed,
                        VariableSequence = spec.TrialSequence
                    }));
            }
        }

        private async Task<int> SeedAsync(DbContext dbContext, string createdUser, params VariableSpec[] variables)
        {
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, createdUser);
            var trialInstanceId = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await PromoteAsync(dbContext, trialInstanceId);
            await AddVariablesAsync(dbContext, searchInstanceId, trialInstanceId, variables);
            return searchInstanceId;
        }

        private static string Nm(string suffix) => $"{DatabaseFixture.Prefix}Var{suffix}{Guid.NewGuid():N}"[..30];

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                ExhaustiveSearchInstanceTrialInstanceVariableService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

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
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(Nm("A"), 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(searchInstanceId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldFromSeededRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = Nm("Full");
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(name, 1.5, 2.5, 30.5, -4.5, 3, 4, 77, 5));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchInstanceId);

            var dto = result.Should().ContainSingle().Subject;
            var expectedId = await dbContext.ExhaustiveSearchInstanceVariable.Where(w => w.Name == name)
                .Select(s => s.Id).SingleAsync();
            dto.Id.Should().Be(expectedId);
            dto.Name.Should().Be(name);
            dto.Mean.Should().Be(1.5);
            dto.StandardDeviation.Should().Be(2.5);
            dto.Maximum.Should().Be(30.5);
            dto.Minimum.Should().Be(-4.5);
            dto.NormalisationTypeId.Should().Be(3);
            dto.ProcessingTypeId.Should().Be(4);
            dto.VariableSequence.Should().Be(77);
            dto.EmptyRange.Should().BeFalse();
        }

        [Fact]
        public async Task EmptyRangeIsTrueWhenMaximumPlusMinimumIsZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("Empty"), 1, 1, 5, -5, 1, 1, 1, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Should().ContainSingle().Which.EmptyRange.Should().BeTrue();
        }

        [Fact]
        public async Task NullStatisticsAndTypesMapToZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("Nulls"), null, null, null, null, null, null, null, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(searchInstanceId)).Should().ContainSingle().Subject;

            dto.Mean.Should().Be(0);
            dto.StandardDeviation.Should().Be(0);
            dto.Maximum.Should().Be(0);
            dto.Minimum.Should().Be(0);
            dto.NormalisationTypeId.Should().Be(0);
            dto.ProcessingTypeId.Should().Be(0);
            dto.VariableSequence.Should().Be(0);
        }

        [Fact]
        public async Task ResultsAreOrderedByTrialInstanceVariableSequenceAscendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var third = Nm("Third");
            var first = Nm("First");
            var second = Nm("Second");
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                Spec(third, 3), Spec(first, 1), Spec(second, 2));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Select(r => r.Name).Should().Equal(first, second, third);
        }

        [Fact]
        public async Task RemovedVariablesAreExcludedButNullRemovedIsIncludedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var kept = Nm("Kept");
            var nullRemoved = Nm("NullRemoved");
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission,
                Spec(kept, 1), Spec(nullRemoved, 2, null), Spec(Nm("Gone"), 3, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Select(r => r.Name).Should().Equal(kept, nullRemoved);
        }

        [Fact]
        public async Task OnlyTheActivePromotedTrialInstanceIsReturnedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var inactiveTrial = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await PromoteAsync(dbContext, inactiveTrial, 0);
            await AddVariablesAsync(dbContext, searchInstanceId, inactiveTrial, Spec(Nm("Inactive"), 1));
            var activeTrial = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await PromoteAsync(dbContext, activeTrial);
            var activeName = Nm("Active");
            await AddVariablesAsync(dbContext, searchInstanceId, activeTrial, Spec(activeName, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Select(r => r.Name).Should().Equal(activeName);
        }

        [Fact]
        public async Task MostRecentlyPromotedActiveTrialInstanceWinsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var olderTrial = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await PromoteAsync(dbContext, olderTrial);
            await AddVariablesAsync(dbContext, searchInstanceId, olderTrial, Spec(Nm("Older"), 1));
            var newerTrial = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await PromoteAsync(dbContext, newerTrial);
            var newerName = Nm("Newer");
            await AddVariablesAsync(dbContext, searchInstanceId, newerTrial, Spec(newerName, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Select(r => r.Name).Should().Equal(newerName);
        }

        [Fact]
        public async Task UnknownSearchInstanceReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task SearchInstanceWithNothingPromotedReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var trial = await InsertTrialInstanceAsync(dbContext, searchInstanceId);
            await AddVariablesAsync(dbContext, searchInstanceId, trial, Spec(Nm("Unpromoted"), 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task PromotedTrialInstanceWithNoVariablesReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBSearchInstanceIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchB = await SeedAsync(dbContext, fx.Seed.UserTenantB, Spec(Nm("SecretB"), 1));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantASearchInstanceIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(Nm("SecretA"), 1));
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var nameA = Nm("OwnA");
            var nameB = Nm("OwnB");
            var searchA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(nameA, 1));
            var searchB = await SeedAsync(dbContext, fx.Seed.UserTenantB, Spec(nameB, 1));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(searchA)).Select(r => r.Name).Should().Equal(nameA);
            (await serviceB.GetAsync(searchB)).Select(r => r.Name).Should().Equal(nameB);
            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
        }

        [Fact]
        public async Task DataQueryForUserWithNoTenantMatchesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var search = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(Nm("A"), 1));
            var trialId = await dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w => w.ExhaustiveSearchInstanceId == search).Select(s => s.Id).SingleAsync();
            var query = new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(
                dbContext, fx.Seed.UserNoTenant);

            (await query.ExecuteByExhaustiveSearchInstanceIdAsync(search)).Should().BeEmpty();
            (await query.ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(trialId)).Should().BeEmpty();
        }

        [Fact]
        public async Task DataQueryByTrialInstanceIdIsTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = Nm("Mine");
            var search = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(name, 1));
            var trialId = await dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(w => w.ExhaustiveSearchInstanceId == search).Select(s => s.Id).SingleAsync();

            (await new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(
                        dbContext, fx.Seed.UserWithPermission)
                    .ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(trialId))
                .Select(r => r.Name).Should().Equal(name);
            (await new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(
                    dbContext, fx.Seed.UserTenantB).ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(trialId))
                .Should().BeEmpty();
            (await new global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(dbContext)
                .ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(trialId)).Should().ContainSingle();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Spec(Nm("C"), 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetAsync(searchInstanceId, cts.Token));
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

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
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
            await service.GetAsync(int.MaxValue);

            activities.Should().ContainSingle(a =>
                    a.OperationName == "ExhaustiveSearchInstanceTrialInstanceVariable.Get")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(int.MaxValue);

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

            await service.GetAsync(int.MaxValue);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersTheUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("ExhaustiveSearchInstanceTrialInstanceVariableGet");
        }
    }
}
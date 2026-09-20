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
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionServiceTests(
        DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPrescriptionIds = [];
        private readonly List<int> createdPromotedIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdSensitivityIds = [];
        private readonly List<int> createdTrialInstanceIds = [];
        private readonly List<int> createdTrialVariableIds = [];
        private readonly List<int> createdVariableIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.ExhaustiveSearchInstanceTrialInstanceVariablePrescription
                .Where(w => createdPrescriptionIds.Contains(w.Id)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                .Where(w => createdSensitivityIds.Contains(w.Id)).DeleteAsync();
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
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService>
            BuildServiceAsync(DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
                IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.CreateAsync(dbContext,
                userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> SeedPromotedAsync(DbContext dbContext, string createdUser,
            params VariableSpec[] variables)
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
                    Active = 1,
                    Score = 0.5,
                    Json = "{}"
                });
            createdPromotedIds.Add(promoted.Id);

            foreach (var spec in variables)
            {
                var variableId = await dbContext.InsertWithInt32IdentityAsync(
                    new global::Jube.Data.Poco.ExhaustiveSearchInstanceVariable
                    {
                        ExhaustiveSearchInstanceId = searchInstance.Id,
                        Name = spec.Name,
                        Mean = spec.Mean,
                        StandardDeviation = spec.StandardDeviation,
                        Maximum = spec.Maximum,
                        Minimum = spec.Minimum
                    });
                createdVariableIds.Add(variableId);

                var trialVariableId = await dbContext.InsertWithInt32IdentityAsync(
                    new global::Jube.Data.Poco.ExhaustiveSearchInstanceTrialInstanceVariable
                    {
                        ExhaustiveSearchInstanceVariableId = variableId,
                        ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                        Removed = spec.Removed
                    });
                createdTrialVariableIds.Add(trialVariableId);

                if (spec.Prescription is { } p)
                {
                    createdPrescriptionIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                        new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                        {
                            ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId,
                            Mean = p.mean,
                            StandardDeviation = p.standardDeviation,
                            Maximum = p.maximum,
                            Minimum = p.minimum
                        }));
                }

                if (spec.Sensitivity is { } s)
                {
                    createdSensitivityIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                        new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                        {
                            ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId,
                            Sensitivity = s
                        }));
                }
            }

            return promoted.Id;
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
                ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.CreateAsync(dbContext,
                    userName, log, localizers, new NullServiceChangeBus(), TestLog.NoOp));

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
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("A"), 1, 1, 1, 1, null, null));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(promotedId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldFromSeededRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = Nm("Full");
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(name, 1.5, 2.5, 30.5, -4.5, (10.25, 11.25, 12.25, 13.25), 0.75));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(promotedId);

            var dto = result.Should().ContainSingle().Subject;
            var expectedId = await dbContext.ExhaustiveSearchInstanceVariable.Where(w => w.Name == name)
                .Select(s => s.Id).SingleAsync();
            dto.Id.Should().Be(expectedId);
            dto.Name.Should().Be(name);
            dto.VariableMean.Should().Be(1.5);
            dto.VariableStandardDeviation.Should().Be(2.5);
            dto.VariableMaximum.Should().Be(30.5);
            dto.VariableMinimum.Should().Be(-4.5);
            dto.PrescriptionMean.Should().Be(10.25);
            dto.PrescriptionStandardDeviation.Should().Be(11.25);
            dto.PrescriptionMaximum.Should().Be(12.25);
            dto.PrescriptionMinimum.Should().Be(13.25);
            dto.Sensitivity.Should().Be(0.75);
        }

        [Fact]
        public async Task MissingPrescriptionAndSensitivityDefaultToZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("Bare"), 1, 2, 3, 4, null, null));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(promotedId)).Should().ContainSingle().Subject;

            dto.PrescriptionMean.Should().Be(0);
            dto.PrescriptionStandardDeviation.Should().Be(0);
            dto.PrescriptionMaximum.Should().Be(0);
            dto.PrescriptionMinimum.Should().Be(0);
            dto.Sensitivity.Should().Be(0);
            dto.VariableMean.Should().Be(1);
        }

        [Fact]
        public async Task ResultsAreOrderedBySensitivityDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("Low"), 1, 1, 1, 1, null, 0.1),
                new VariableSpec(Nm("High"), 1, 1, 1, 1, null, 0.9),
                new VariableSpec(Nm("Mid"), 1, 1, 1, 1, null, 0.5));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(promotedId);

            result.Select(r => r.Sensitivity).Should().Equal(0.9, 0.5, 0.1);
        }

        [Fact]
        public async Task RemovedVariablesAreExcludedButNullRemovedIsIncludedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var kept = Nm("Kept");
            var nullRemoved = Nm("NullRemoved");
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(kept, 1, 1, 1, 1, null, null),
                new VariableSpec(nullRemoved, 1, 1, 1, 1, null, null, null),
                new VariableSpec(Nm("Gone"), 1, 1, 1, 1, null, null, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(promotedId);

            result.Select(r => r.Name).Should().BeEquivalentTo([kept, nullRemoved]);
        }

        [Fact]
        public async Task UnknownPromotedTrialInstanceReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task PromotedTrialInstanceWithNoVariablesReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(promotedId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBPromotedTrialInstanceIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedB = await SeedPromotedAsync(dbContext, fx.Seed.UserTenantB,
                new VariableSpec(Nm("SecretB"), 1, 1, 1, 1, (1, 1, 1, 1), 0.5));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await serviceA.GetAsync(promotedB)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantAPromotedTrialInstanceIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedA = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("SecretA"), 1, 1, 1, 1, (1, 1, 1, 1), 0.5));
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceB.GetAsync(promotedA)).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnPromotedTrialInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var nameA = Nm("OwnA");
            var nameB = Nm("OwnB");
            var promotedA = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(nameA, 1, 1, 1, 1, null, null));
            var promotedB = await SeedPromotedAsync(dbContext, fx.Seed.UserTenantB,
                new VariableSpec(nameB, 2, 2, 2, 2, null, null));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(promotedA)).Select(r => r.Name).Should().Equal(nameA);
            (await serviceB.GetAsync(promotedB)).Select(r => r.Name).Should().Equal(nameB);
        }

        [Fact]
        public async Task DataQueryIsTenantScopedIndependentlyOfTheServiceGuardAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = Nm("Direct");
            var promotedA = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(name, 1, 1, 1, 1, null, null));

            (await new
                    global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(
                        dbContext, fx.Seed.UserWithPermission).ExecuteAsync(promotedA)).Select(r => r.Name).Should()
                .Equal(name);
            (await new
                global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(
                    dbContext, fx.Seed.UserTenantB).ExecuteAsync(promotedA)).Should().BeEmpty();
            (await new
                global::Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(
                    dbContext, fx.Seed.UserNoTenant).ExecuteAsync(promotedA)).Should().BeEmpty();
        }

        [Fact]
        public async Task ForeignTenantAttemptLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedB = await SeedPromotedAsync(dbContext, fx.Seed.UserTenantB,
                new VariableSpec(Nm("SecretB"), 1, 1, 1, 1, null, null));
            var log = new TestLog();
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await serviceA.GetAsync(promotedB);

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var promotedId = await SeedPromotedAsync(dbContext, fx.Seed.UserWithPermission,
                new VariableSpec(Nm("C"), 1, 1, 1, 1, null, null));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(promotedId, cts.Token));
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
                    a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.Get")
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
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionGet");
        }
    }
}
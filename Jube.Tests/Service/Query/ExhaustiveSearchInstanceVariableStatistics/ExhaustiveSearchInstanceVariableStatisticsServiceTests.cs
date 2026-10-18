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
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstanceVariableStatistics
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstanceVariableStatisticsServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly StatisticsSeeder seeder = new(fx);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return seeder.DisposeAsync();
        }

        private static Task<ExhaustiveSearchInstanceVariableStatisticsService> BuildAsync(DbContext db,
            string? user, ILog? log = null, ILog? audit = null, IServiceChangeBus? bus = null)
        {
            return ExhaustiveSearchInstanceVariableStatisticsService.CreateAsync(db, user, log ?? TestLog.NoOp,
                localizers, bus ?? new NullServiceChangeBus(), audit ?? TestLog.NoOp);
        }

        private static string Nm(string suffix)
        {
            return $"{DatabaseFixture.Prefix}St{suffix}{Guid.NewGuid():N}"[..30];
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var db = fx.GetDbContext();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(db, userName));

            ex.Code.Should().Be("NotAuthenticated");
        }

        [Fact]
        public async Task CreateWithTenantlessOrUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var db = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(db, fx.Seed.UserNoTenant));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(db, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task PermissionMissingThrowsForbiddenForBothTheInstanceAndTheSingleVariableCallsAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variableId) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("P"));
            var service = await BuildAsync(db, fx.Seed.UserWithoutPermission);

            var all = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(instance.Id));
            var one = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(instance.Id, variableId));

            all.Code.Should().Be("PermissionDenied");
            all.RequiredSpecifications.Should().BeEquivalentTo([16]);
            one.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task MapsEveryHistogramAndSummaryOfTheVariableAsync()
        {
            await using var db = fx.GetDbContext();
            var name = Nm("Map");
            var (instance, variableId) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, name);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(instance.Id, variableId)).Should().ContainSingle().Subject;

            dto.Id.Should().Be(variableId);
            dto.Name.Should().Be(name);
            dto.Population.Mean.Should().Be(5);
            dto.Population.StandardDeviation.Should().Be(2);
            dto.Population.Minimum.Should().Be(0);
            dto.Population.Maximum.Should().Be(10);
            dto.Population.Iqr.Should().Be(3);
            dto.PopulationBins.Select(b => (b.Start, b.End, b.Frequency)).Should().Equal((0d, 5d, 60), (5d, 10d, 40));
            dto.NegativeBins.Select(b => b.Frequency).Should().Equal(55, 25);
            dto.PositiveBins.Select(b => b.Frequency).Should().Equal(5, 15);
            dto.NegativeBins.Select(b => b.Start).Should().Equal(dto.PopulationBins.Select(b => b.Start));
            dto.ClassificationBins.Select(b => (b.Start, b.Frequency)).Should().Equal((6d, 3), (8d, 9));
            dto.Classification!.Mean.Should().Be(8);
            dto.AnomalyBins.Select(b => b.Frequency).Should().Equal(4, 12);
            dto.Anomaly!.Mean.Should().Be(7.5);

            dto.PrescriptionBins.Select(b => (b.Start, b.End, b.Frequency)).Should()
                .Equal((4d, 6d, 1), (6d, 8d, 6), (8d, 10d, 9), (10d, 12d, 4));

            dto.Prescription!.Mean.Should().Be(7);
            dto.Prescription.StandardDeviation.Should().Be(1.5);
            dto.Prescription.Minimum.Should().Be(4);
            dto.Prescription.Maximum.Should().Be(12);
            dto.Prescription.Iqr.Should().Be(2);
        }

        [Fact]
        public async Task SingleVariableCallReturnsOnlyThatVariableAndTheInstanceCallReturnsAllAsync()
        {
            await using var db = fx.GetDbContext();
            var instance = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission);
            var a = await seeder.VariableAsync(db, instance, Nm("A"));
            var b = await seeder.VariableAsync(db, instance, Nm("B"));
            await seeder.PopulationAsync(db, a, (0, 1, 1));
            await seeder.PopulationAsync(db, b, (0, 1, 2), (1, 2, 3));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var one = await service.GetAsync(instance.Id, b);
            var all = await service.GetAsync(instance.Id);

            one.Should().ContainSingle().Which.PopulationBins.Select(x => x.Frequency).Should().Equal(2, 3);
            all.Select(v => v.Id).Should().Equal(a, b);
            all.Single(v => v.Id == a).PopulationBins.Should().ContainSingle();
        }

        [Fact]
        public async Task VariableFromAnotherInstanceOfTheSameTenantIsNotReturnedAsync()
        {
            await using var db = fx.GetDbContext();
            var (first, firstVariable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("F"));
            var (second, secondVariable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("S"));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            (await service.GetAsync(first.Id, secondVariable)).Should().BeEmpty();
            (await service.GetAsync(second.Id, firstVariable)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantsAreIsolatedInBothDirectionsAndForTheSingleVariableCallAsync()
        {
            await using var db = fx.GetDbContext();
            var (a, aVariable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("TA"));
            var (b, bVariable) = await seeder.FullAsync(db, fx.Seed.UserTenantB, Nm("TB"));
            var serviceA = await BuildAsync(db, fx.Seed.UserWithPermission);
            var serviceB = await BuildAsync(db, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(b.Id)).Should().BeEmpty();
            (await serviceA.GetAsync(b.Id, bVariable)).Should().BeEmpty();
            (await serviceB.GetAsync(a.Id)).Should().BeEmpty();
            (await serviceB.GetAsync(a.Id, aVariable)).Should().BeEmpty();
            (await serviceA.GetAsync(a.Id, aVariable)).Should().ContainSingle();
            (await serviceB.GetAsync(b.Id, bVariable)).Should().ContainSingle();
        }

        [Fact]
        public async Task UnknownAndDeletedInstancesAndVariablesAreEmptyAsync()
        {
            await using var db = fx.GetDbContext();
            var deletedInstance = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission, deleted: 1);
            var variable = await seeder.VariableAsync(db, deletedInstance, Nm("D"));
            var instance = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission);
            var deletedVariable = await seeder.VariableAsync(db, instance, Nm("DV"), 1);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
            (await service.GetAsync(int.MaxValue, int.MaxValue)).Should().BeEmpty();
            (await service.GetAsync(deletedInstance.Id)).Should().BeEmpty();
            (await service.GetAsync(deletedInstance.Id, variable)).Should().BeEmpty();
            (await service.GetAsync(instance.Id, deletedVariable)).Should().BeEmpty();
        }

        [Fact]
        public async Task SoftDeletedClassHistogramRowsAreNotReturnedAsync()
        {
            await using var db = fx.GetDbContext();
            var instance = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission);
            var variable = await seeder.VariableAsync(db, instance, Nm("SD"));
            await seeder.ClassAsync(db, variable, 0, 0, (0, 1, 3));
            await seeder.ClassAsync(db, variable, 1, 1, (0, 1, 9));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(instance.Id, variable)).Single();

            dto.NegativeBins.Should().ContainSingle();
            dto.PositiveBins.Should().BeEmpty();
        }

        [Fact]
        public async Task InstanceWithoutAPromotedTrialHasNoPrescriptionAndOlderInstancesHaveNoClassBinsAsync()
        {
            await using var db = fx.GetDbContext();
            var instance = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission, false);
            var variable = await seeder.VariableAsync(db, instance, Nm("NP"));
            await seeder.PopulationAsync(db, variable, (0, 1, 3));
            await seeder.ClassificationAsync(db, variable, 8, (0, 1, 2));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(instance.Id, variable)).Single();

            dto.PrescriptionBins.Should().BeEmpty();
            dto.Prescription.Should().BeNull();
            dto.NegativeBins.Should().BeEmpty();
            dto.PositiveBins.Should().BeEmpty();
            dto.ClassificationBins.Should().ContainSingle();
            dto.Anomaly.Should().BeNull();
        }

        [Fact]
        public async Task VariableNotUsedByThePromotedTrialHasNoPrescriptionAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("U"));
            var other = await seeder.VariableAsync(db, instance, Nm("Unused"));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(instance.Id, other)).Single();

            dto.Prescription.Should().BeNull();
            dto.PrescriptionBins.Should().BeEmpty();
            (await service.GetAsync(instance.Id, variable)).Single().PrescriptionBins.Should().HaveCount(4);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(1, 1, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndFailureLogsErrorAsync()
        {
            await using var db = fx.GetDbContext();
            var log = new TestLog();
            var forbidden = await BuildAsync(db, fx.Seed.UserWithoutPermission, log);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(1));
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");

            var broken = fx.GetDbContext();
            var failLog = new TestLog();
            var service = await BuildAsync(broken, fx.Seed.UserWithPermission, failLog);
            await broken.DisposeAsync();
            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(1));
            failLog.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanOneAuditLineAndPublishesNothingAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);
            var audit = new TestLog();
            var bus = new CapturingBus();

            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission, audit: audit, bus: bus);
            await service.GetAsync(int.MaxValue);

            activities.Should().ContainSingle(a => a.OperationName == "ExhaustiveSearchInstanceVariableStatistics.Get")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");

            audit.Entries.Should().ContainSingle().Which.Message.Should().Contain("op=Get");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersTheUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("ExhaustiveSearchInstanceVariableStatisticsGet");
        }
    }
}
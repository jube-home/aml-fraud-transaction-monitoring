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
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceVariable;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstanceVariable;
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

namespace Jube.Test.Service.Query.ExhaustiveSearchInstanceVariable
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstanceVariableServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdHistogramIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdVariableIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.ExhaustiveSearchInstanceVariableHistogram
                .Where(w => createdHistogramIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<ExhaustiveSearchInstanceVariableService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstanceVariableService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

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

        private async Task<int> AddVariableAsync(DbContext dbContext, int searchInstanceId, string name,
            byte? normalisationTypeId = 1, params (int? Frequency, double? Start)[] bins)
        {
            var variableId = await dbContext.InsertWithInt32IdentityAsync(
                new global::Jube.Data.Poco.ExhaustiveSearchInstanceVariable
                {
                    ExhaustiveSearchInstanceId = searchInstanceId,
                    Name = name,
                    Mean = 1.5,
                    StandardDeviation = 2.5,
                    Kurtosis = 3.5,
                    Skewness = 4.5,
                    Maximum = 5.5,
                    Minimum = -6.5,
                    Iqr = 7.5,
                    NormalisationTypeId = normalisationTypeId,
                    DistinctValues = 8,
                    Correlation = 0.25,
                    CorrelationAbsRank = 9
                });
            createdVariableIds.Add(variableId);
            var sequence = 0;
            foreach (var (frequency, start) in bins)
            {
                createdHistogramIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariableHistogram
                    {
                        ExhaustiveSearchInstanceVariableId = variableId,
                        BinSequence = sequence++,
                        BinRangeStart = start,
                        BinRangeEnd = start + 1,
                        Frequency = frequency
                    }));
            }

            return variableId;
        }

        private async Task<int> SeedAsync(DbContext dbContext, string createdUser, string name)
        {
            var id = await InsertSearchInstanceAsync(dbContext, createdUser);
            await AddVariableAsync(dbContext, id, name, 1, (10, 0.5), (20, 1.5));
            return id;
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
                ExhaustiveSearchInstanceVariableService.CreateAsync(dbContext, userName, log, localizers,
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
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Nm("A"));
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
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var variableId = await AddVariableAsync(dbContext, searchInstanceId, name, 2, (10, 0.5), (20, 1.5));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(searchInstanceId)).Should().ContainSingle().Subject;

            dto.Id.Should().Be(variableId);
            dto.Name.Should().Be(name);
            dto.Mean.Should().Be(1.5);
            dto.StandardDeviation.Should().Be(2.5);
            dto.Kurtosis.Should().Be(3.5);
            dto.Skewness.Should().Be(4.5);
            dto.Maximum.Should().Be(5.5);
            dto.Minimum.Should().Be(0, "the legacy query never populates Minimum");
            dto.Iqr.Should().Be(7.5);
            dto.NormalisationType.Should().Be("Z Score");
            dto.DistinctValues.Should().Be(8);
            dto.Correlation.Should().Be(0.25);
            dto.CorrelationAbsRank.Should().Be(9);
            dto.HistogramValues.Should().BeEquivalentTo([
                new { Frequency = 10, Bin = 0.5 }, new { Frequency = 20, Bin = 1.5 }
            ]);
        }

        [Theory]
        [InlineData(0, "No")]
        [InlineData(1, "Binary")]
        [InlineData(2, "Z Score")]
        [InlineData(3, "Default")]
        [InlineData(null, "Default")]
        public async Task NormalisationTypeMapsToDisplayNameAsync(int? typeId, string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            await AddVariableAsync(dbContext, searchInstanceId, Nm("Norm"), (byte?)typeId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Should().ContainSingle().Which.NormalisationType
                .Should().Be(expected);
        }

        [Fact]
        public async Task NullStatisticsMapToZeroAndVariableWithoutHistogramHasEmptyBinsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var id = await dbContext.InsertWithInt32IdentityAsync(
                new global::Jube.Data.Poco.ExhaustiveSearchInstanceVariable
                {
                    ExhaustiveSearchInstanceId = searchInstanceId, Name = Nm("Nulls")
                });
            createdVariableIds.Add(id);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(searchInstanceId)).Should().ContainSingle().Subject;

            dto.Mean.Should().Be(0);
            dto.Iqr.Should().Be(0);
            dto.DistinctValues.Should().Be(0);
            dto.CorrelationAbsRank.Should().Be(0);
            dto.HistogramValues.Should().BeEmpty();
        }

        [Fact]
        public async Task HistogramsAreAttachedToTheirOwnVariableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await InsertSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var a = Nm("HA");
            var b = Nm("HB");
            await AddVariableAsync(dbContext, searchInstanceId, a, 1, (1, 1.0));
            await AddVariableAsync(dbContext, searchInstanceId, b, 1, (2, 2.0), (3, 3.0));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchInstanceId);

            result.Single(r => r.Name == a).HistogramValues.Should().ContainSingle().Which.Frequency.Should().Be(1);
            result.Single(r => r.Name == b).HistogramValues.Select(h => h.Frequency).Should()
                .BeEquivalentTo([2, 3]);
        }

        [Fact]
        public async Task OtherSearchInstancesVariablesAreNotReturnedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var mine = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Nm("Mine"));
            await SeedAsync(dbContext, fx.Seed.UserWithPermission, Nm("Other"));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(mine)).Should().ContainSingle();
        }

        [Fact]
        public async Task UnknownSearchInstanceReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBSearchInstanceIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchB = await SeedAsync(dbContext, fx.Seed.UserTenantB, Nm("SecretB"));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantASearchInstanceIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Nm("SecretA"));
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var nameA = Nm("OwnA");
            var nameB = Nm("OwnB");
            var searchA = await SeedAsync(dbContext, fx.Seed.UserWithPermission, nameA);
            var searchB = await SeedAsync(dbContext, fx.Seed.UserTenantB, nameB);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(searchA)).Select(r => r.Name).Should().Equal(nameA);
            (await serviceB.GetAsync(searchB)).Select(r => r.Name).Should().Equal(nameB);
            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Nm("C"));
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
                    a.OperationName == "ExhaustiveSearchInstanceVariable.Get")
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
            names.Should().Contain("ExhaustiveSearchInstanceVariableGet");
        }
    }
}
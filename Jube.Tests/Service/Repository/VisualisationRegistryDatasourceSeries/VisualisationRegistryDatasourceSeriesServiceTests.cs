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
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.VisualisationRegistryDatasourceSeries
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourceSeriesServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdSeriesIds = [];
        private readonly List<int> createdDatasourceIds = [];
        private readonly List<int> createdRegistryIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => createdSeriesIds.Contains(w.Id)).DeleteAsync();
            await dbContext.VisualisationRegistryDatasource
                .Where(w => createdDatasourceIds.Contains(w.Id)).DeleteAsync();
            await dbContext.VisualisationRegistry
                .Where(w => createdRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<VisualisationRegistryDatasourceSeriesService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return VisualisationRegistryDatasourceSeriesService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new Jube.Service.Reactivity.NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateVisualisationRegistryAsync(DbContext dbContext, string createdUser)
        {
            var repository = new Data.Repository.VisualisationRegistryRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Registry{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = 0,
                ShowInDirectory = 0,
            }).ConfigureAwait(false);

            createdRegistryIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task<int> CreateDatasourceAsync(DbContext dbContext, int visualisationRegistryId,
            string createdUser, byte? deleted = 0)
        {
            var repository = new Data.Repository.VisualisationRegistryDatasourceRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.VisualisationRegistryDatasource
            {
                VisualisationRegistryId = visualisationRegistryId,
                Name = $"{DatabaseFixture.Prefix}Datasource{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = deleted,
                Command = "select 1",
            }).ConfigureAwait(false);

            createdDatasourceIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task CreateSeriesAsync(DbContext dbContext, int visualisationRegistryDatasourceId,
            string name, int dataTypeId)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistryDatasourceSeries
            {
                VisualisationRegistryDatasourceId = visualisationRegistryDatasourceId,
                Name = name,
                DataTypeId = dataTypeId,
            }).ConfigureAwait(false);

            createdSeriesIds.Add(id);
        }

        private async Task<int> CreateRegistryDatasourceWithSeriesAsync(DbContext dbContext, string userName,
            byte? datasourceDeleted = 0)
        {
            var registryId = await CreateVisualisationRegistryAsync(dbContext, userName);
            var datasourceId = await CreateDatasourceAsync(dbContext, registryId, userName, datasourceDeleted);
            await CreateSeriesAsync(dbContext, datasourceId, "Frequency", 2);
            await CreateSeriesAsync(dbContext, datasourceId, "ActivationRuleName", 1);
            return datasourceId;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByVisualisationRegistryDatasourceIdAsync(1));
        }

        [Fact]
        public async Task ListByVisualisationRegistryDatasourceIdReturnsSeriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var datasourceId =
                await CreateRegistryDatasourceWithSeriesAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var series = await service.GetByVisualisationRegistryDatasourceIdAsync(datasourceId);

            series.Should().HaveCount(2);
            series.Select(s => s.Name).Should().BeEquivalentTo(["Frequency", "ActivationRuleName"]);
            series.Single(s => s.Name == "Frequency").DataTypeId.Should().Be(2);
        }

        [Fact]
        public async Task ListForUnknownDatasourceIdReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var series = await service.GetByVisualisationRegistryDatasourceIdAsync(int.MaxValue);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var datasourceIdB = await CreateRegistryDatasourceWithSeriesAsync(dbContext, fx.Seed.UserTenantB);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var seriesForA = await serviceA.GetByVisualisationRegistryDatasourceIdAsync(datasourceIdB);

            seriesForA.Should().BeEmpty();

            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var seriesForB = await serviceB.GetByVisualisationRegistryDatasourceIdAsync(datasourceIdB);

            seriesForB.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListExcludesSeriesOfSoftDeletedDatasourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var datasourceId = await CreateRegistryDatasourceWithSeriesAsync(dbContext, fx.Seed.UserWithPermission,
                datasourceDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var series = await service.GetByVisualisationRegistryDatasourceIdAsync(datasourceId);

            series.Should().BeEmpty();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var cancelledToken = cts.Token;
            var act = () => service.GetByVisualisationRegistryDatasourceIdAsync(1, cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ListWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);

            var act = () => service.GetByVisualisationRegistryDatasourceIdAsync(1);
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task ListLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.GetByVisualisationRegistryDatasourceIdAsync(1);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task ListDoesNotPublishChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var datasourceId =
                await CreateRegistryDatasourceWithSeriesAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.GetByVisualisationRegistryDatasourceIdAsync(datasourceId);

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueListsUniqueVisualisationRegistryDatasourceSeriesToolNames()
        {
            var tools = ServiceToolCatalogue.All;
            var names = tools.Where(t => t.Name.StartsWith("VisualisationRegistryDatasourceSeries"))
                .Select(t => t.Name).ToList();

            names.Should().BeEquivalentTo([
                "VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId",
            ]);
            names.Should().OnlyHaveUniqueItems();
        }
    }
}
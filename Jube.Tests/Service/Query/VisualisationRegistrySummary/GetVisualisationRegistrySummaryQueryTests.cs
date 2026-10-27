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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Query.GetVisualisationRegistrySummaryQuery;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Query.VisualisationRegistrySummary
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GetVisualisationRegistrySummaryQueryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> registryIds = [];
        private readonly List<int> datasourceIds = [];
        private readonly List<int> parameterIds = [];
        private readonly DateTime now = DateTime.UtcNow;
        private int tenantRegistryId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            tenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId).FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<VisualisationRegistryDatasourceExecutionLog>()
                .Where(w => w.VisualisationRegistryDatasourceId != null
                            && datasourceIds.Contains(w.VisualisationRegistryDatasourceId.Value))
                .DeleteAsync();
            await dbContext.VisualisationRegistryDatasource.Where(w => datasourceIds.Contains(w.Id)).DeleteAsync();
            await dbContext.VisualisationRegistryParameter.Where(w => parameterIds.Contains(w.Id)).DeleteAsync();
            await dbContext.VisualisationRegistry.Where(w => registryIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> NewRegistryAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Vs{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                ContextKindId = 1,
                TenantRegistryId = tenantRegistryId,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = now
            });
            registryIds.Add(id);

            return id;
        }

        private async Task<int> NewDatasourceAsync(int registryId, byte active = 1, byte deleted = 0,
            int displayKind = 1)
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasource
            {
                Name = $"{DatabaseFixture.Prefix}Ds{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                VisualisationRegistryId = registryId,
                Active = active,
                Locked = 0,
                Deleted = deleted,
                DisplayKindId = displayKind,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = now
            });
            datasourceIds.Add(id);

            return id;
        }

        private async Task NewParameterAsync(int registryId, byte active = 1)
        {
            await using var dbContext = fx.GetDbContext();

            parameterIds.Add(await dbContext.InsertWithInt32IdentityAsync(new VisualisationRegistryParameter
            {
                Name = $"{DatabaseFixture.Prefix}Pm{Guid.NewGuid():N}"[..40],
                VisualisationRegistryId = registryId,
                Active = active,
                Locked = 0,
                Deleted = 0,
                DataTypeId = 1,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = now
            }));
        }

        private async Task LogAsync(int datasourceId, DateTime when, string user, int milliseconds,
            string? error = null)
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasourceExecutionLog
            {
                VisualisationRegistryDatasourceId = datasourceId,
                CreatedDate = when,
                CreatedUser = user,
                ResponseTime = milliseconds,
                Records = 10,
                Error = error
            });
        }

        private async Task<List<GetVisualisationRegistrySummaryQueryDto>> RunAsync(params int[] ids)
        {
            await using var dbContext = fx.GetDbContext();

            return await new GetVisualisationRegistrySummaryQuery(dbContext).ExecuteAsync(ids, now.AddDays(-30));
        }

        [Fact]
        public async Task ARegistryWithNothingRecordedStillGetsARowOfZerosAsync()
        {
            var registry = await NewRegistryAsync();

            var row = (await RunAsync(registry)).Single();

            row.RegistryId.Should().Be(registry);
            row.DatasourceCount.Should().Be(0);
            row.ParameterCount.Should().Be(0);
            row.Executions.Should().Be(0);
            row.LastExecutionDate.Should().BeNull();
            row.LastExecutionUser.Should().BeNull();
            row.AverageResponseMilliseconds.Should().BeNull();
        }

        [Fact]
        public async Task OnlyActiveUndeletedDatasourcesAndParametersAreCountedAsync()
        {
            var registry = await NewRegistryAsync();
            await NewDatasourceAsync(registry, displayKind: 2);
            await NewDatasourceAsync(registry, displayKind: 1);
            await NewDatasourceAsync(registry, displayKind: 2);
            await NewDatasourceAsync(registry, active: 0, displayKind: 9);
            await NewDatasourceAsync(registry, deleted: 1, displayKind: 8);
            await NewParameterAsync(registry);
            await NewParameterAsync(registry);
            await NewParameterAsync(registry, active: 0);

            var row = (await RunAsync(registry)).Single();

            row.DatasourceCount.Should().Be(3);
            row.ParameterCount.Should().Be(2);
            row.DisplayKindIds.Should().Equal(1, 2);
        }

        [Fact]
        public async Task ExecutionsFailuresAndAverageCoverTheWindowButTheLastRunCoversAllTimeAsync()
        {
            var registry = await NewRegistryAsync();
            var datasource = await NewDatasourceAsync(registry);
            var other = await NewRegistryAsync();
            var otherDatasource = await NewDatasourceAsync(other);

            await LogAsync(datasource, now.AddDays(-90), "ancient", 900);
            await LogAsync(datasource, now.AddDays(-5), "alex", 100);
            await LogAsync(datasource, now.AddDays(-2), "sam", 300, "boom");
            await LogAsync(datasource, now.AddHours(-1), "robin", 200);
            await LogAsync(otherDatasource, now.AddMinutes(-1), "someoneElse", 50);

            var row = (await RunAsync(registry, other)).Single(s => s.RegistryId == registry);

            row.Executions.Should().Be(3, "the ninety day old run is outside the window");
            row.Failures.Should().Be(1);
            row.AverageResponseMilliseconds.Should().BeApproximately(200, 0.001);
            row.LastExecutionUser.Should().Be("robin");
            row.LastExecutionDate.Should().BeCloseTo(now.AddHours(-1), TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ARegistryWhoseOnlyRunsAreOlderThanTheWindowStillShowsWhenItLastRanAsync()
        {
            var registry = await NewRegistryAsync();
            var datasource = await NewDatasourceAsync(registry);

            await LogAsync(datasource, now.AddDays(-45), "alex", 120);

            var row = (await RunAsync(registry)).Single();

            row.Executions.Should().Be(0);
            row.AverageResponseMilliseconds.Should().BeNull();
            row.LastExecutionUser.Should().Be("alex");
            row.LastExecutionDate.Should().NotBeNull();
        }

        [Fact]
        public async Task NoRegistriesMeansNoQueryAsync()
        {
            (await RunAsync()).Should().BeEmpty();
        }
    }
}

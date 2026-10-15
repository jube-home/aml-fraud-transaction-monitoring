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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Xunit;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Exhaustive
{
    [Collection("Database")]
    public class DemoDataSourceTests(DatabaseFixture fx)
    {
        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private static Training NewTraining(IReadOnlyDictionary<string, string>? extra = null)
        {
            var values = new Dictionary<string, string> { ["ConnectionString"] = ConnectionString };

            if (extra != null)
            {
                foreach (var pair in extra)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            return new Training(LogManager.GetLogger(typeof(DemoDataSourceTests)),
                TestDynamicEnvironment.Create(values), new JsonSerializationHelper())
            {
                seeded = new Random(1), pollDelay = TimeSpan.FromMilliseconds(20)
            };
        }

        private async Task<(int ModelId, int TenantId, int InstanceId)> NewStoppedInstanceAsync(byte dataSource)
        {
            await using var db = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Dm{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });

            var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Di{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = 0, StatusId = 18,
                    Anomaly = 1, AnomalyProbability = 0.5, Filter = 0, DataSourceId = dataSource
                });

            return (model.Id, model.TenantRegistryId!.Value, instance.Id);
        }

        private async Task<bool> RunAsync(int modelId, int tenantId, int instanceId, byte dataSource,
            IReadOnlyDictionary<string, string>? extra = null)
        {
            await using var db = fx.GetDbContext();

            return await NewTraining(extra).RunOneInstanceAsync(new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = instanceId, EntityAnalysisModelId = modelId, TenantRegistryId = tenantId, Anomaly = true,
                AnomalyProbability = 0.5, DataSourceId = dataSource
            }, db, CancellationToken.None);
        }

        [Fact]
        public async Task DemoDataset_LoadsTheBuiltInRowsOncePerModelIntoMockArchiveAsync()
        {
            var (modelId, tenantId, instanceId) = await NewStoppedInstanceAsync(1);

            (await RunAsync(modelId, tenantId, instanceId, 1)).Should().BeFalse();

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(w => w.EntityAnalysisModelId == modelId)).Should().Be(1827);

            await db.ExhaustiveSearchInstance.Where(w => w.Id == instanceId)
                .Set(s => s.StatusId, (byte)18).UpdateAsync();

            (await RunAsync(modelId, tenantId, instanceId, 1)).Should().BeFalse();
            (await db.MockArchive.CountAsync(w => w.EntityAnalysisModelId == modelId)).Should().Be(1827);
        }

        [Fact]
        public async Task ArchiveDataSource_NeverLoadsTheDemoRowsAsync()
        {
            var (modelId, tenantId, instanceId) = await NewStoppedInstanceAsync(0);

            (await RunAsync(modelId, tenantId, instanceId, 0)).Should().BeFalse();

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(w => w.EntityAnalysisModelId == modelId)).Should().Be(0);
        }

        [Fact]
        public async Task TheRetiredEnvironmentVariable_HasNoEffectOnEitherDataSourceAsync()
        {
            var (modelId, tenantId, instanceId) = await NewStoppedInstanceAsync(0);

            await RunAsync(modelId, tenantId, instanceId, 0,
                new Dictionary<string, string> { ["UseMockDataExhaustive"] = "True" });

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(w => w.EntityAnalysisModelId == modelId)).Should().Be(0);

            var (demoModel, demoTenant, demoInstance) = await NewStoppedInstanceAsync(1);

            await RunAsync(demoModel, demoTenant, demoInstance, 1,
                new Dictionary<string, string> { ["UseMockDataExhaustive"] = "False" });

            (await db.MockArchive.CountAsync(w => w.EntityAnalysisModelId == demoModel)).Should().Be(1827);
        }
    }
}
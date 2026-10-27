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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    public abstract class AdvancedAnalyticsRepositoryTestBase(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> instanceIds = [];
        private readonly List<int> modelIds = [];
        protected DatabaseFixture Fixture { get; } = fx;
        protected DbContext Db { get; private set; } = null!;
        protected int TenantA { get; private set; }
        protected int TenantB { get; private set; }

        public async Task InitializeAsync()
        {
            Db = Fixture.GetDbContext();
            TenantA = await TenantOfAsync(Fixture.Seed.UserWithPermission);
            TenantB = await TenantOfAsync(Fixture.Seed.UserTenantB);
        }

        public async Task DisposeAsync()
        {
            var ids = instanceIds.ToList();

            await Db.AdvancedAnalyticsTrainingRunCurvePoint.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRunMetric.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRunIteration.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRunTrial.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRunVariable.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRunStep.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();

            await Db.AskJoobyAnalyticsTrainedModel.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .Set(s => s.ActiveVersionId, (int?)null)
                .UpdateAsync();

            await Db.AskJoobyAnalyticsTrainedModelVersion
                .Where(w => Db.AskJoobyAnalyticsTrainedModel.Any(m =>
                    m.Id == w.AskJoobyAnalyticsTrainedModelId && ids.Contains(m.AskJoobyAnalyticsInstanceId)))
                .DeleteAsync();

            await Db.AskJoobyAnalyticsTrainedModel.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();
            await Db.AdvancedAnalyticsTrainingRun.Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId))
                .DeleteAsync();

            await Db.GetTable<AskJoobyAnalyticsInstanceVersion>()
                .Where(w => ids.Contains(w.AskJoobyAnalyticsInstanceId)).DeleteAsync();

            await Db.AskJoobyAnalyticsInstance.Where(w => ids.Contains(w.Id)).DeleteAsync();

            var tenants = new List<int> { TenantA, TenantB };
            await Db.AdvancedAnalyticsCache.Where(w => tenants.Contains(w.TenantRegistryId)).DeleteAsync();

            var models = modelIds.ToList();
            await Db.EntityAnalysisModel.Where(w => models.Contains(w.Id)).DeleteAsync();

            await Db.DisposeAsync();
        }

        protected static string UniqueName(string what)
        {
            var name = $"{DatabaseFixture.Prefix}AJ{what}{Guid.NewGuid():N}";

            return name.Length > 48 ? name[..48] : name;
        }

        protected Task<int> TenantOfAsync(string user)
        {
            return Db.UserInTenant.Where(w => w.User == user).Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        protected async Task<int> NewEntityAnalysisModelAsync(int tenantRegistryId)
        {
            var id = await Db.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = UniqueName("Model"),
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            modelIds.Add(id);

            return id;
        }

        protected async Task<AskJoobyAnalyticsInstance> NewInstanceAsync(int tenantRegistryId, int? modelId = null,
            string? name = null)
        {
            var repository = new AskJoobyAnalyticsInstanceRepository(Db, tenantRegistryId, "ZzTestUser");

            var instance = await repository.InsertAsync(new AskJoobyAnalyticsInstance
            {
                Name = name ?? UniqueName("Instance"),
                EntityAnalysisModelId = modelId
            });
            instanceIds.Add(instance.Id);

            return instance;
        }

        protected Task<AdvancedAnalyticsTrainingRun> NewRunAsync(int tenantRegistryId, int instanceId,
            AdvancedAnalyticsKind kind = AdvancedAnalyticsKind.Classifier,
            AdvancedAnalyticsJobMode jobMode = AdvancedAnalyticsJobMode.Online)
        {
            var repository = new AdvancedAnalyticsTrainingRunRepository(Db, tenantRegistryId, "ZzTestUser");

            return repository.InsertAsync(new AdvancedAnalyticsTrainingRun
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                KindId = kind,
                JobMode = jobMode
            });
        }

        protected Task<AskJoobyAnalyticsTrainedModel> NewTrainedModelAsync(int tenantRegistryId,
            int instanceId)
        {
            var repository = new AskJoobyAnalyticsTrainedModelRepository(Db, tenantRegistryId, "ZzTestUser");

            return repository.InsertAsync(new AskJoobyAnalyticsTrainedModel
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                Name = UniqueName("Trained"),
                KindId = AdvancedAnalyticsKind.Classifier,
                SourceId = AdvancedAnalyticsSource.Agent
            });
        }
    }
}
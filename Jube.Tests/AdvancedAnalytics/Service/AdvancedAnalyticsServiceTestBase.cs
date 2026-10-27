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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.AskJoobyAnalyticsInstance;
using Jube.Service.Repository.AskJoobyAnalyticsModel;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    public abstract class AdvancedAnalyticsServiceTestBase(DatabaseFixture fx) : IAsyncLifetime
    {
        protected static readonly IStringLocalizerFactory Localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> entityAnalysisModelIds = [];
        private readonly List<int> instanceIds = [];
        private readonly List<int> roleIds = [];
        private readonly List<string> userNames = [];
        protected DatabaseFixture Fixture { get; } = fx;
        protected DbContext Db { get; private set; } = null!;
        protected int TenantA { get; private set; }
        protected int TenantB { get; private set; }
        protected string Permitted => Fixture.Seed.UserWithPermission;
        protected string Unpermitted => Fixture.Seed.UserWithoutPermission;
        protected string OtherTenant => Fixture.Seed.UserTenantB;

        public async Task InitializeAsync()
        {
            Db = Fixture.GetDbContext();
            TenantA = await TenantOfAsync(Permitted);
            TenantB = await TenantOfAsync(OtherTenant);
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

            var models = entityAnalysisModelIds.ToList();
            await Db.EntityAnalysisModel.Where(w => models.Contains(w.Id)).DeleteAsync();

            var users = userNames.ToList();
            await Db.UserInTenant.Where(w => users.Contains(w.User)).DeleteAsync();
            await Db.UserRegistry.Where(w => users.Contains(w.Name)).DeleteAsync();
            var roles = roleIds.ToList();

            await Db.RoleRegistryPermission.Where(w => roles.Select(r => (int?)r).Contains(w.RoleRegistryId))
                .DeleteAsync();

            await Db.RoleRegistry.Where(w => roles.Contains(w.Id)).DeleteAsync();

            await Db.DisposeAsync();
        }

        protected static string UniqueName(string what)
        {
            var name = $"{DatabaseFixture.Prefix}AJS{what}{Guid.NewGuid():N}";

            return name.Length > 48 ? name[..48] : name;
        }

        protected Task<int> TenantOfAsync(string user)
        {
            return Db.UserInTenant.Where(w => w.User == user).Select(s => s.TenantRegistryId).FirstAsync();
        }

        protected async Task<string> UserWithSpecsAsync(params int[] specs)
        {
            var userName = UniqueName("User");
            var roleGuid = Guid.NewGuid();

            var roleId = await Db.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid,
                Name = UniqueName("Role"),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = TenantA,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            roleIds.Add(roleId);

            foreach (var spec in specs)
            {
                await Db.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            await Db.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await Db.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = TenantA });
            userNames.Add(userName);

            return userName;
        }

        protected async Task<int> NewEntityAnalysisModelAsync(int tenantRegistryId, bool deleted = false)
        {
            var id = await Db.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = UniqueName("Model"),
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = (byte)(deleted ? 1 : 0)
            });
            entityAnalysisModelIds.Add(id);

            return id;
        }

        protected async Task<AskJoobyAnalyticsInstance> NewInstanceAsync(int tenantRegistryId, int? modelId = null)
        {
            var repository = new AskJoobyAnalyticsInstanceRepository(Db, tenantRegistryId, "ZzTestUser");

            var instance = await repository.InsertAsync(new AskJoobyAnalyticsInstance
            {
                Name = UniqueName("Instance"),
                EntityAnalysisModelId = modelId
            });
            TrackInstance(instance.Id);

            return instance;
        }

        protected void TrackInstance(int instanceId)
        {
            instanceIds.Add(instanceId);
        }

        protected Task<AskJoobyAnalyticsInstanceService> InstanceServiceAsync(string? user,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? bus = null, DbContext? db = null)
        {
            return AskJoobyAnalyticsInstanceService.CreateAsync(db ?? Db, user, log ?? TestLog.NoOp,
                Localizers, bus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        protected Task<AdvancedAnalyticsTrainingService> TrainingServiceAsync(string? user,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? bus = null, DbContext? db = null)
        {
            return AdvancedAnalyticsTrainingService.CreateAsync(db ?? Db, user, log ?? TestLog.NoOp,
                Localizers, bus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        protected Task<AskJoobyAnalyticsModelService> ModelServiceAsync(string? user,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? bus = null, DbContext? db = null)
        {
            return AskJoobyAnalyticsModelService.CreateAsync(db ?? Db, user, log ?? TestLog.NoOp,
                Localizers, bus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        protected static SliceDataset Dataset(int seed = 11)
        {
            return SliceData.Logistic(900, seed, -0.4, [1.1, -0.9], 1);
        }

        protected static AdvancedAnalyticsLogisticRegressionTrainingDto TrainingRequest(int instanceId,
            string modelName,
            SliceDataset? data = null, int seed = 1)
        {
            data ??= Dataset();

            return new AdvancedAnalyticsLogisticRegressionTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                Inputs = data.Inputs,
                Labels = data.Labels,
                VariableNames = data.Names,
                ProcessingTypeIds = [.. data.Names.Select((_, i) => i % 2 == 0 ? 1 : 5)],
                Seed = seed
            };
        }

        protected async Task<AdvancedAnalyticsTrainingOutcomeDto> TrainAsync(int instanceId, string modelName,
            int seed = 1, string? user = null)
        {
            var service = await TrainingServiceAsync(user ?? Permitted);

            return await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, modelName, null, seed));
        }

        protected sealed class CapturingBus : IServiceChangeBus
        {
            private readonly List<ServiceChangeEvent> published = [];

            public IReadOnlyList<ServiceChangeEvent> Published
            {
                get
                {
                    lock (published)
                    {
                        return [.. published];
                    }
                }
            }

            public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
            {
                lock (published)
                {
                    published.Add(change);
                }

                return Task.CompletedTask;
            }

            public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
            {
                return new Noop();
            }

            private sealed class Noop : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
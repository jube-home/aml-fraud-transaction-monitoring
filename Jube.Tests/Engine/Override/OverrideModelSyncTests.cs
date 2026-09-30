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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using OverridePoco = Jube.Data.Poco.EntityAnalysisModelOverride;
using ActivationRuleOverridePoco = Jube.Data.Poco.EntityAnalysisModelActivationRuleOverride;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.Engine.Override
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class OverrideModelSyncTests : IAsyncLifetime
    {
        private readonly List<int> createdModelIdsForCleanup = [];
        private readonly List<Guid> createdModelGuidsForCleanup = [];

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            foreach (var modelGuid in createdModelGuidsForCleanup)
            {
                await dbContext.EntityAnalysisModelActivationRuleOverride
                    .Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();
                await dbContext.EntityAnalysisModelOverride
                    .Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();
            }

            foreach (var modelId in createdModelIdsForCleanup)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext)
        {
            var modelGuid = Guid.NewGuid();
            var saved = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = $"ZzTestOverrideModel{Guid.NewGuid():N}"[..40],
                Guid = modelGuid,
                Active = 1,
                Locked = 0,
                Deleted = 0
            });

            createdModelIdsForCleanup.Add(saved);
            createdModelGuidsForCleanup.Add(modelGuid);
            return (saved, modelGuid);
        }

        private static Task SyncAsync(DbContext dbContext, int modelId, Guid modelGuid,
            EntityAnalysisModelDomain model)
        {
            var context = new SyncContext
            {
                Services =
                {
                    DbContext = dbContext,
                    Log = TestLog.NoOp,
                    Parser = new Parser.Parser(TestLog.NoOp, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new Dictionary<int, EntityAnalysisModelDomain> { [modelId] = model }
                }
            };

            model.Instance.Id = modelId;
            model.Instance.Guid = modelGuid;

            return context.SyncOverridesAsync();
        }

        private static Task InsertModelOverrideAsync(DbContext dbContext, Guid modelGuid, string key, string value,
            byte overrideKind = 0)
        {
            return dbContext.InsertAsync(new OverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = key,
                OverrideKeyValue = value,
                OverrideKind = overrideKind,
                Deleted = 0,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "OverrideModelSyncTests",
                Version = 1
            });
        }

        private static Task InsertActivationRuleOverrideAsync(DbContext dbContext, Guid modelGuid, string key,
            string value, string activationRuleName, byte overrideKind = 0)
        {
            return dbContext.InsertAsync(new ActivationRuleOverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = key,
                OverrideKeyValue = value,
                EntityAnalysisModelActivationRuleName = activationRuleName,
                OverrideKind = overrideKind,
                Deleted = 0,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "OverrideModelSyncTests",
                Version = 1
            });
        }

        [Fact]
        public async Task SyncKeepsEverySuppressedValueHeldAgainstOneKeyAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7");
            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "aaaa");
            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "bbbb");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrides = model.Dependencies.EntityAnalysisModelOverrides;

            overrides.Should().ContainKey("CardFingerprint");
            overrides["CardFingerprint"].Keys.Should().BeEquivalentTo(new[] { "cdb7", "aaaa", "bbbb" },
                "every suppressed value against the key must survive synchronisation, not only the last row");
        }

        [Fact]
        public async Task SyncKeepsSuppressedValuesAcrossSeveralKeysAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7");
            await InsertModelOverrideAsync(dbContext, modelGuid, "UserId", "U123");
            await InsertModelOverrideAsync(dbContext, modelGuid, "ProjectCode", "P1");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrides = model.Dependencies.EntityAnalysisModelOverrides;

            overrides.Keys.Should().BeEquivalentTo("CardFingerprint", "UserId", "ProjectCode");
            overrides["CardFingerprint"].Should().ContainKey("cdb7");
            overrides["UserId"].Should().ContainKey("U123");
            overrides["ProjectCode"].Should().ContainKey("P1");
        }

        [Fact]
        public async Task SyncKeepsEveryActivationRuleHeldAgainstOneValueAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7", "RuleA");
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7", "RuleB");
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7", "RuleC");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrides = model.Dependencies.EntityAnalysisModelOverrides;

            overrides["CardFingerprint"]["cdb7"].ActivationRules.Keys.Should()
                .BeEquivalentTo(new[] { "RuleA", "RuleB", "RuleC" },
                    "every activation rule bound to the value must survive, not only the last row");
        }

        [Fact]
        public async Task SyncKeepsEveryValueHeldAgainstOneActivationRuleKeyAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7", "RuleA");
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "aaaa", "RuleA");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrides = model.Dependencies.EntityAnalysisModelOverrides;

            overrides["CardFingerprint"].Keys.Should().BeEquivalentTo("cdb7", "aaaa");
            overrides["CardFingerprint"]["cdb7"].ActivationRules.Should().ContainKey("RuleA");
            overrides["CardFingerprint"]["aaaa"].ActivationRules.Should().ContainKey("RuleA");
        }

        [Fact]
        public async Task SyncMergesModelAndActivationRuleOverridesOntoOneValueAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7");
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7", "RuleA");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrideBinding = model.Dependencies.EntityAnalysisModelOverrides
                ["CardFingerprint"]["cdb7"];

            overrideBinding.AllActivationRules.Should().Be(EntityAnalysisModelOverrideKind.Suppress);
            overrideBinding.ActivationRules.Should().ContainKey("RuleA");
        }

        [Fact]
        public async Task SyncCarriesTheForceOverrideKindAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7",
                "BlacklistCard", (byte)EntityAnalysisModelOverrideKind.Force);
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7",
                "QuietRule");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrideBinding = model.Dependencies.EntityAnalysisModelOverrides
                ["CardFingerprint"]["cdb7"];

            overrideBinding.KindFor("BlacklistCard").Should().Be(EntityAnalysisModelOverrideKind.Force);
            overrideBinding.KindFor("QuietRule").Should().Be(EntityAnalysisModelOverrideKind.Suppress);
        }

        [Fact]
        public async Task SyncLetsForceWinWhereBothKindsAreHeldForOneActivationRuleAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7");
            await InsertActivationRuleOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7",
                "BlacklistCard", (byte)EntityAnalysisModelOverrideKind.Force);

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrideBinding = model.Dependencies.EntityAnalysisModelOverrides
                ["CardFingerprint"]["cdb7"];

            overrideBinding.KindFor("BlacklistCard").Should().Be(EntityAnalysisModelOverrideKind.Force,
                "a force override must not be masked by an all activation rules suppression");
            overrideBinding.KindFor("AnyOtherRule").Should().Be(EntityAnalysisModelOverrideKind.Suppress);
        }

        [Fact]
        public async Task SyncSkipsExpiredAndDeletedOverridesAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "live");

            await dbContext.InsertAsync(new OverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = "CardFingerprint",
                OverrideKeyValue = "expired",
                Deleted = 0,
                DeleteExpiryDate = DateTime.UtcNow.AddMinutes(-5),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "OverrideModelSyncTests",
                Version = 1
            });

            await dbContext.InsertAsync(new OverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = "CardFingerprint",
                OverrideKeyValue = "removed",
                Deleted = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "OverrideModelSyncTests",
                Version = 1
            });

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            var overrides = model.Dependencies.EntityAnalysisModelOverrides;

            overrides["CardFingerprint"].Keys.Should().BeEquivalentTo("live");
        }

        [Fact]
        public async Task SyncReplacesThePreviousOverridesRatherThanAccumulatingAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var (modelId, modelGuid) = await CreateModelAsync(dbContext);

            await InsertModelOverrideAsync(dbContext, modelGuid, "CardFingerprint", "cdb7");

            var model = new EntityAnalysisModelDomain();
            await SyncAsync(dbContext, modelId, modelGuid, model);

            model.Dependencies.EntityAnalysisModelOverrides["CardFingerprint"].Should().ContainKey("cdb7");

            await dbContext.EntityAnalysisModelOverride
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).DeleteAsync();

            await SyncAsync(dbContext, modelId, modelGuid, model);

            model.Dependencies.EntityAnalysisModelOverrides.Should().BeEmpty(
                "a removed override must not survive the next synchronisation");
        }
    }
}
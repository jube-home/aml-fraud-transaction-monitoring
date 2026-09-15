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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Unit")]
    public sealed class CachePruneTaskStarterTests
    {
        private static Context NewContext(CacheService cacheService, TestLog log,
            IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            var context = new Context
            {
                Services =
                {
                    CacheService = cacheService,
                    Log = log,
                    DynamicEnvironment = TestDynamicEnvironment.Create(environmentOverrides)
                }
            };
            return context;
        }

        private static EntityAnalysisModelDomain NewModel(int tenantRegistryId = 1)
        {
            var model = new EntityAnalysisModelDomain
            {
                Instance =
                {
                    TenantRegistryId = tenantRegistryId,
                    Guid = Guid.NewGuid()
                }
            };
            return model;
        }

        [Fact]
        public async Task PruneModelDoesNothingWhenNoReferenceDateIsStoredForTheModelAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var log = new TestLog();
            var context = NewContext(cacheService, log);
            var model = NewModel();
            var starter = new CachePruneTaskStarter(context);

            var act = async () => await starter.PruneModelAsync(model, 100);

            await act.Should().NotThrowAsync();
            log.Entries.Should().Contain(e => e.Message.Contains("the reference date will be looked up"));
            log.Entries.Should().NotContain(e => e.Message.Contains("the threshold reference date for"),
                "the threshold is only computed after a reference date is found");
            log.Entries.Should().NotContain(e => e.Message.Contains("deletion routine has returned"),
                "no deletion should ever be attempted when there is no reference date to base a threshold on");
        }

        [Fact]
        public async Task PruneModelLooksUpTheReferenceDateUsingTheModelsOwnTenantAndGuidAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var log = new TestLog();
            var context = NewContext(cacheService, log);
            var model = NewModel(7);

            await cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                99, model.Instance.Guid, DateTime.UtcNow);

            var starter = new CachePruneTaskStarter(context);
            var act = async () => await starter.PruneModelAsync(model, 100);

            await act.Should().NotThrowAsync();
            log.Entries.Should().NotContain(e => e.Message.Contains("the threshold reference date for"),
                "the reference date was stored under a different tenant, so this model must still see a miss");
        }

        [Fact]
        public void GetDeletionLimitOrDefaultIfNullUsesTheShippedDefaultOfOneThousandWhenNotOverridden()
        {
            var cacheService = TestCacheService.Create(out _);
            var context = NewContext(cacheService, TestLog.NoOp);
            var starter = new CachePruneTaskStarter(context);

            starter.GetDeletionLimitOrDefaultIfNull().Should().Be(1000);
        }

        [Fact]
        public void GetDeletionLimitOrDefaultIfNullUsesTheConfiguredOverrideWhenSet()
        {
            var cacheService = TestCacheService.Create(out _);
            var context = NewContext(cacheService, TestLog.NoOp,
                new Dictionary<string, string> { ["CacheTtlDeleteLimit"] = "250" });
            var starter = new CachePruneTaskStarter(context);

            starter.GetDeletionLimitOrDefaultIfNull().Should().Be(250);
        }
    }
}
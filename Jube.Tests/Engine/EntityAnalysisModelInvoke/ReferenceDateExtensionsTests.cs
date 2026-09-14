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
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ReferenceDateExtensionsTests
    {
        private static Context NewContext(DateTime referenceDate, int tenantRegistryId = 7, int modelId = 999)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                Instance =
                {
                    Id = modelId,
                    TenantRegistryId = tenantRegistryId,
                    Guid = Guid.NewGuid()
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    ReferenceDate = referenceDate,
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        [Fact]
        public async Task FirstEverCallForAModelUpsertsSinceNoReferenceDateIsStoredYetAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var context = NewContext(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            await context.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            var stored = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid);
            stored.Should().Be(context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate);
        }

        [Fact]
        public async Task ANewerReferenceDateOverwritesTheStoredOneAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var tenantRegistryId = 7;
            var modelGuid = Guid.NewGuid();

            var first = NewContext(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), tenantRegistryId);
            first.EntityAnalysisModel.Instance.Guid = modelGuid;
            await first.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(first.PendingWriteTasks);

            var second = NewContext(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), tenantRegistryId,
                111);
            second.EntityAnalysisModel.Instance.Guid = modelGuid;
            await second.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(second.PendingWriteTasks);

            var stored = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(
                tenantRegistryId, modelGuid);
            stored.Should().Be(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task AnOlderOutOfOrderReferenceDateIsRejectedAndDoesNotOverwriteTheStoredOneAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var tenantRegistryId = 7;
            var modelGuid = Guid.NewGuid();

            var newer = NewContext(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), tenantRegistryId);
            newer.EntityAnalysisModel.Instance.Guid = modelGuid;
            await newer.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(newer.PendingWriteTasks);

            var older = NewContext(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), tenantRegistryId,
                222);
            older.EntityAnalysisModel.Instance.Guid = modelGuid;
            await older.CheckIntegrityAndUpsertAsync(cacheService);
            older.PendingWriteTasks.Should().BeEmpty(
                "an older reference date must be rejected, not queued for upsert");

            var stored = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(
                tenantRegistryId, modelGuid);
            stored.Should().Be(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                "the newer, already-stored reference date must survive an older out-of-order write attempt");
        }

        [Fact]
        public async Task DifferentTenantsWithTheSameModelGuidDoNotCollideAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var modelGuid = Guid.NewGuid();

            var tenantA = NewContext(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1);
            tenantA.EntityAnalysisModel.Instance.Guid = modelGuid;
            await tenantA.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(tenantA.PendingWriteTasks);

            var tenantB = NewContext(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), 2);
            tenantB.EntityAnalysisModel.Instance.Guid = modelGuid;
            await tenantB.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(tenantB.PendingWriteTasks);

            var storedForTenantA = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(1, modelGuid);
            var storedForTenantB = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(2, modelGuid);

            storedForTenantA.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            storedForTenantB.Should().Be(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task AFutureReferenceDateThrowsAndDoesNotUpsertAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var context = NewContext(DateTime.UtcNow.AddDays(1));

            var act = async () => await context.CheckIntegrityAndUpsertAsync(cacheService);

            await act.Should().ThrowAsync<ReferenceDateInFutureException>();
            context.PendingWriteTasks.Should().BeEmpty();
        }

        [Fact]
        public async Task AReferenceDateEqualToTheStoredOneIsAcceptedAndUpsertedAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var tenantRegistryId = 7;
            var modelGuid = Guid.NewGuid();
            var sameDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var first = NewContext(sameDate, tenantRegistryId);
            first.EntityAnalysisModel.Instance.Guid = modelGuid;
            await first.CheckIntegrityAndUpsertAsync(cacheService);
            await Task.WhenAll(first.PendingWriteTasks);

            var second = NewContext(sameDate, tenantRegistryId, 333);
            second.EntityAnalysisModel.Instance.Guid = modelGuid;
            await second.CheckIntegrityAndUpsertAsync(cacheService);

            second.PendingWriteTasks.Should().NotBeEmpty();
        }
    }
}
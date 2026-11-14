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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    [Collection("Database")]
    public sealed class SanctionExemptionEngineTests : IDisposable
    {
        private const int Tenant = 987001;

        public SanctionExemptionEngineTests()
        {
            SanctionExemptionSet.Shared.Clear();
        }

        public void Dispose()
        {
            SanctionExemptionSet.Shared.Clear();
        }

        private static EntityAnalysisModelSanction Check()
        {
            return new EntityAnalysisModelSanction
            {
                Name = "SDN",
                MultipartStringDataName = "JoinedName",
                Distance = 0,
                AggregationTypeId = 3,
                CacheInterval = 'h',
                CacheValue = 1,
                MaxDistanceRatio = 0.5,
                MaxCoverageRatio = 5.0
            };
        }

        private static Context NewContext(EntityAnalysisModelSanction check, params (int Id, string Reference)[] entries)
        {
            var model = new EntityAnalysisModel();
            model.Instance.TenantRegistryId = Tenant;
            model.Snapshot.EntityAnalysisModelSanctions.Add(check);
            model.Services.CacheService = TestCacheService.Create(out _);

            foreach (var (id, reference) in entries)
            {
                model.Dependencies.SanctionsEntries.TryAdd(id, new SanctionEntry
                {
                    SanctionEntryId = id,
                    SanctionEntrySourceId = 3,
                    SanctionEntryReference = reference,
                    SanctionElementValue = ["ivan", "petrov"]
                });
            }

            var context = new Context
            {
                EntityAnalysisModel = model,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Sanction = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };

            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Ivan Petrov");

            return context;
        }

        private static ApprovedSanctionExemption Approved(string reference, int id = 1)
        {
            return new ApprovedSanctionExemption(id, Tenant, SanctionExemptionSet.NameKey("Ivan Petrov"), 3, reference,
                null);
        }

        [Fact]
        public async Task AnExemptedEntryIsLeftOutOfTheMatchesAndTheAggregateAsync()
        {
            SanctionExemptionSet.Shared.Replace([Approved("R1")]);
            var context = NewContext(Check(), (1, "R1"), (2, "R2"));

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(1, "only R2 still matches");
            context.EntityAnalysisModelInstanceEntryPayload.SanctionMatch!["SDN"].Select(m => m.Reference)
                .Should().Equal("R2");
        }

        [Fact]
        public async Task WhenEveryMatchIsExemptedThereIsNoSanctionResultAsync()
        {
            SanctionExemptionSet.Shared.Replace([Approved("R1")]);
            var context = NewContext(Check(), (1, "R1"));

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().NotContainKey("SDN");
            context.EntityAnalysisModelInstanceEntryPayload.SanctionMatch.Should().BeNull();
        }

        [Fact]
        public async Task AnExemptionForAnotherTenantChangesNothingAsync()
        {
            SanctionExemptionSet.Shared.Replace([
                new ApprovedSanctionExemption(1, Tenant + 1, SanctionExemptionSet.NameKey("Ivan Petrov"), 3, "R1", null)
            ]);
            var context = NewContext(Check(), (1, "R1"), (2, "R2"));

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(2);
        }

        [Fact]
        public async Task ApprovingAnExemptionStopsAResultCachedBeforeItFromBeingServedAsync()
        {
            var check = Check();
            var before = NewContext(check, (1, "R1"), (2, "R2"));
            var variantBefore = SanctionsExtensions.BuildCacheVariant(before, check);

            SanctionExemptionSet.Shared.Replace([Approved("R1")]);
            var variantAfter = SanctionsExtensions.BuildCacheVariant(before, check);

            variantAfter.Should().NotBe(variantBefore);
        }
    }
}

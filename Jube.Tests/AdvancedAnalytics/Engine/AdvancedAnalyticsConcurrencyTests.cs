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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.AdvancedAnalytics.Extensions;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsConcurrencyTests
    {
        private static readonly TrainedFixture old = EngineModelFactory.Trained(41, ["Field"]);
        private static readonly TrainedFixture @new = EngineModelFactory.Trained(42, ["Field"]);

        [Fact]
        public async Task ManyInvocationsWhileTheCollectionIsSwappedSeeEitherTheOldOrTheNewModelAndNeverFailAsync()
        {
            var entityModel = new EntityAnalysisModel();
            var guid = Guid.NewGuid();
            var oldSet = new List<AdvancedAnalyticsModel> { EngineModelFactory.Model("M", old, 5, 1, guid) };
            var newSet = new List<AdvancedAnalyticsModel> { EngineModelFactory.Model("M", @new, 5, 2, guid) };
            var oldSnapshot = new ModelSnapshot { AdvancedAnalyticsModels = oldSet };
            var newSnapshot = new ModelSnapshot { AdvancedAnalyticsModels = newSet };
            entityModel.PublishSnapshot(oldSnapshot);

            var expectedOld = old.Recall.Predict([0.5])[0];
            var expectedNew = @new.Recall.Predict([0.5])[0];
            expectedOld.Should().NotBe(expectedNew);

            using var stop = new CancellationTokenSource();
            var stopToken = stop.Token;

            var swapper = Task.Run(() =>
            {
                var flip = false;

                while (!stopToken.IsCancellationRequested)
                {
                    entityModel.PublishSnapshot(flip ? oldSnapshot : newSnapshot);
                    flip = !flip;
                }
            }, stopToken);

            var results = new ConcurrentBag<double>();
            var failures = new ConcurrentBag<Exception>();

            var workers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 1500; i++)
                {
                    try
                    {
                        var context = NewContext(entityModel);
                        context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Field"] = 0.5;
                        context.ExecuteAdvancedAnalyticsAdaptation();
                        results.Add(context.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation["M"]);

                        results.Add(AdvancedAnalyticsExtensions.Recall([entityModel], guid,
                            JObject.Parse("{\"Field\":0.5}")));
                    }
                    catch (Exception ex)
                    {
                        failures.Add(ex);
                    }
                }
            }, stop.Token)).ToArray();

            await Task.WhenAll(workers);
            await stop.CancelAsync();
            await swapper;

            failures.Should().BeEmpty();
            results.Should().NotBeEmpty();
            results.Should().OnlyContain(o => Math.Abs(o - expectedOld) < 0.0001 || Math.Abs(o - expectedNew) < 0.0001);
            results.Should().Contain(expectedOld);
            results.Should().Contain(expectedNew);
        }

        [Fact]
        public async Task TheSharedImmutableRecallGivesIdenticalResultsFromManyThreadsAsync()
        {
            var expected = old.Recall.Predict([0.5])[0];
            var model = EngineModelFactory.Model("M", old, 5, 1);

            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
                Enumerable.Range(0, 2000).Select(_ => model.Recall([0.5])[0]).Distinct().ToArray())));

            results.SelectMany(s => s).Distinct().Should().Equal(expected);
        }

        private static Context NewContext(EntityAnalysisModel entityAnalysisModel)
        {
            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptation = new PooledDictionary<string, double>(),
                    AdvancedAnalyticsAdaptation = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = false
            };
        }
    }
}
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
using System.Linq;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Test.AdvancedAnalytics.Slice;
using Newtonsoft.Json;
using Jube.Engine.AdvancedAnalytics.Recall.Models;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    public sealed record TrainedFixture(
        string SnapshotJson,
        string[] Names,
        string VariablesJson,
        AdvancedAnalyticsRecallModel Recall);

    public static class EngineModelFactory
    {
        private static readonly ConcurrentDictionary<string, TrainedFixture> cache = new();

        public static string[] Names(params string[] names)
        {
            return names;
        }

        public static TrainedFixture Trained(int seed, string[] names, int[]? processingTypes = null)
        {
            var key = $"{seed}|{string.Join(",", names)}|{string.Join(",", processingTypes ?? [])}";

            return cache.GetOrAdd(key, _ =>
            {
                var slopes = names.Select((_, i) => i % 2 == 0 ? 1.0 + 0.3 * i : -0.6 - 0.2 * i).ToArray();
                var data = SliceData.Logistic(700, seed, -0.4 + 0.1 * seed, slopes, 0, 0.1);
                var named = data with { Names = names };

                var result = LogisticRegressionTrainer.Train(named.Inputs, named.Labels, named.Names,
                    new LogisticRegressionOptions { Seed = seed });

                var types = processingTypes ?? [.. names.Select(_ => 5)];

                return new TrainedFixture(result.SnapshotJson, names, VariablesJson(names, types),
                    AdvancedAnalyticsModelRegistry.Build(result.SnapshotJson));
            });
        }

        public static string VariablesJson(string[] names, int[] processingTypes)
        {
            return JsonConvert.SerializeObject(names.Select((n, i) =>
                new { name = n, processingTypeId = processingTypes[i] }));
        }

        public static AdvancedAnalyticsModel Model(string name, TrainedFixture fixture, int[] processingTypes,
            int id = 7, Guid? guid = null, bool responsePayload = false)
        {
            var model = new AdvancedAnalyticsModel
            {
                Id = id,
                Guid = guid ?? Guid.NewGuid(),
                Name = name,
                Kind = fixture.Recall.Kind,
                ResponsePayload = responsePayload,
                Recall = fixture.Recall.Predict
            };

            for (var i = 0; i < fixture.Names.Length; i++)
            {
                model.VariablesInOrder.Add(new AdvancedAnalyticsModelVariable
                {
                    Name = fixture.Names[i],
                    ProcessingTypeId = processingTypes[i]
                });
            }

            return model;
        }

        public static AdvancedAnalyticsModel IdentityModel(string name,
            VariableTransformationKind transformationId, string? transformationParametersJson,
            int processingTypeId = 1, string variableName = "Field")
        {
            var model = new AdvancedAnalyticsModel
            {
                Id = 1,
                Guid = Guid.NewGuid(),
                Name = name,
                Kind = "Identity",
                Recall = row => row
            };

            model.VariablesInOrder.Add(new AdvancedAnalyticsModelVariable
            {
                Name = variableName,
                ProcessingTypeId = processingTypeId,
                TransformationId = transformationId,
                TransformationParametersJson = transformationParametersJson
            });

            return model;
        }

        public static AdvancedAnalyticsModel Model(string name, TrainedFixture fixture, int processingType = 5,
            int id = 7, Guid? guid = null)
        {
            return Model(name, fixture, [.. fixture.Names.Select(_ => processingType)], id, guid);
        }

        public static AdvancedAnalyticsModel IdentityModel(string name,
            params AdvancedAnalyticsModelVariable[] variables)
        {
            var model = new AdvancedAnalyticsModel
            {
                Id = 1,
                Guid = Guid.NewGuid(),
                Name = name,
                Kind = "Identity",
                Recall = row => row
            };

            model.VariablesInOrder.AddRange(variables);

            return model;
        }
    }
}
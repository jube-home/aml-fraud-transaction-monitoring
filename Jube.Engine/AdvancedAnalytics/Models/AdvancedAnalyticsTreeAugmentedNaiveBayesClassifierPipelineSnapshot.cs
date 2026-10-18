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

using System.Linq;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Models
{
    public sealed class AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] Parents { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Intercept1 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Slope1 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Variance1 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Intercept0 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Slope0 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Variance0 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double PriorLogOdds { get; set; }

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Intercept1), Intercept1, inputCount);
            RequireLength(nameof(Slope1), Slope1, inputCount);
            RequireLength(nameof(Variance1), Variance1, inputCount);
            RequireLength(nameof(Intercept0), Intercept0, inputCount);
            RequireLength(nameof(Slope0), Slope0, inputCount);
            RequireLength(nameof(Variance0), Variance0, inputCount);

            if (Parents == null)
            {
                throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{nameof(Parents)}' is missing.");
            }

            if (Parents.Length != inputCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot array '{nameof(Parents)}' has {Parents.Length} elements but {inputCount} were expected.");
            }

            for (var i = 0; i < Parents.Length; i++)
            {
                if (Parents[i] < -1 || Parents[i] >= inputCount || Parents[i] == i)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot parent of variable {i} is invalid ({Parents[i]}).");
                }
            }

            for (var i = 0; i < Parents.Length; i++)
            {
                var steps = 0;

                for (var p = Parents[i]; p >= 0; p = Parents[p])
                {
                    if (++steps > Parents.Length)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            $"Snapshot parents contain a cycle at variable {i}.");
                    }
                }
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Intercept1), Intercept1),
                         (nameof(Slope1), Slope1), (nameof(Intercept0), Intercept0), (nameof(Slope0), Slope0)
                     })
            {
                if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' must be finite.");
                }
            }

            foreach (var (name, values) in new[] { (nameof(Variance1), Variance1), (nameof(Variance0), Variance0) })
            {
                if (values.Any(v => !double.IsFinite(v) || v <= 0))
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot array '{name}' must be strictly positive and finite.");
                }
            }

            if (!double.IsFinite(PriorLogOdds))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot prior log-odds must be finite.");
            }
        }
    }
}
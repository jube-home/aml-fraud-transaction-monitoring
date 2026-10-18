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
    public sealed class AdvancedAnalyticsNaiveBayesClassifierPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Mean1 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Variance1 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Mean0 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Variance0 { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double PriorLogOdds { get; set; }

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Mean1), Mean1, inputCount);
            RequireLength(nameof(Variance1), Variance1, inputCount);
            RequireLength(nameof(Mean0), Mean0, inputCount);
            RequireLength(nameof(Variance0), Variance0, inputCount);

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Mean1), Mean1), (nameof(Mean0), Mean0)
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
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
    public sealed class AdvancedAnalyticsOneClassSupportVectorAnomalyPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[][] SupportVectors { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Alphas { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double Bias { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double Gamma { get; set; }

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Means), Means, inputCount);
            RequireLength(nameof(StandardDeviations), StandardDeviations, inputCount);

            if (StandardDeviations.Any(v => v < 0))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot standard deviations must not be negative.");
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Means), Means),
                         (nameof(StandardDeviations), StandardDeviations)
                     })
            {
                if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' must be finite.");
                }
            }

            if (SupportVectors == null || SupportVectors.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no stored support vectors.");
            }

            foreach (var row in SupportVectors)
            {
                if (row == null || row.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every stored support vector must have {inputCount} elements.");
                }

                if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot support vectors must be finite.");
                }
            }

            if (Alphas == null || Alphas.Length != SupportVectors.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'Alphas' must list exactly one coefficient per support vector.");
            }

            if (Alphas.Any(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot alphas must be finite and not negative.");
            }

            if (!double.IsFinite(Bias))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot bias must be finite.");
            }

            if (!double.IsFinite(Gamma) || Gamma < 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot gamma must be finite and not negative.");
            }
        }
    }
}
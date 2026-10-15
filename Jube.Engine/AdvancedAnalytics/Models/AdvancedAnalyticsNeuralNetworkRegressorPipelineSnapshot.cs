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
    public sealed class AdvancedAnalyticsNeuralNetworkRegressorPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public bool Standardise { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int HiddenUnits { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[][] InputWeights { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] HiddenBiases { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] OutputWeights { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double OutputBias { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double TargetLow { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double TargetSpan { get; set; }

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

            if (HiddenUnits < 1)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot must have at least one hidden unit.");
            }

            if (InputWeights == null || InputWeights.Length != HiddenUnits)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'InputWeights' must have exactly one row per hidden unit.");
            }

            foreach (var row in InputWeights)
            {
                if (row == null || row.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every input weight row must have {inputCount} elements.");
                }

                if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot input weights must be finite.");
                }
            }

            RequireLength(nameof(HiddenBiases), HiddenBiases, HiddenUnits);
            RequireLength(nameof(OutputWeights), OutputWeights, HiddenUnits);

            if (HiddenBiases.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot hidden biases must be finite.");
            }

            if (OutputWeights.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot output weights must be finite.");
            }

            if (double.IsNaN(OutputBias) || double.IsInfinity(OutputBias))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot output bias must be finite.");
            }

            if (double.IsNaN(TargetLow) || double.IsInfinity(TargetLow))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot target low must be finite.");
            }

            if (!double.IsFinite(TargetSpan) || TargetSpan <= 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot target span must be finite and positive.");
            }
        }
    }
}
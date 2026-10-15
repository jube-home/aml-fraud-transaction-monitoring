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
    public sealed class AdvancedAnalyticsLogisticRegressionPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public bool Standardise { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool RejectMissing { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] IndicatorInputs { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Weights { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double Intercept { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double TrainingPrior { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double BaseRate { get; set; }

        public double?[] Significances { get; set; }

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Means), Means, inputCount);
            RequireLength(nameof(StandardDeviations), StandardDeviations, inputCount);

            if (IndicatorInputs == null)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot array 'IndicatorInputs' is missing.");
            }

            if (IndicatorInputs.Any(i => i < 0 || i >= inputCount) ||
                IndicatorInputs.Distinct().Count() != IndicatorInputs.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'IndicatorInputs' must be distinct input positions.");
            }

            RequireLength(nameof(Weights), Weights, inputCount + IndicatorInputs.Length);

            if (Significances != null)
            {
                if (Significances.Length != inputCount + IndicatorInputs.Length)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot array '{nameof(Significances)}' has length {Significances.Length} but expected {inputCount + IndicatorInputs.Length}.");
                }

                if (Significances.Any(p => p is { } value && (double.IsNaN(value) || value < 0 || value > 1)))
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot array '{nameof(Significances)}' must contain p-values strictly between 0 and 1 where present.");
                }
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Means), Means),
                         (nameof(StandardDeviations), StandardDeviations), (nameof(Weights), Weights)
                     })
            {
                if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot array '{name}' must be finite.");
                }
            }

            if (StandardDeviations.Any(v => v < 0))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot standard deviations must not be negative.");
            }

            if (double.IsNaN(Intercept) || double.IsInfinity(Intercept))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot intercept must be finite.");
            }

            foreach (var rate in new[] { TrainingPrior, BaseRate })
            {
                if (double.IsNaN(rate) || rate <= 0 || rate >= 1)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot priors must be strictly between 0 and 1.");
                }
            }
        }
    }
}
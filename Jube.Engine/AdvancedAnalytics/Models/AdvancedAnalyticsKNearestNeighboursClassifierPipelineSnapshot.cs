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
    public sealed class AdvancedAnalyticsKNearestNeighboursClassifierPipelineSnapshot : AdvancedAnalyticsSnapshot
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
        public double[][] Rows { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] Labels { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int NeighbourCount { get; set; }

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

            if (Rows == null || Rows.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no stored training rows.");
            }

            foreach (var row in Rows)
            {
                if (row == null || row.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException($"Every stored row must have {inputCount} elements.");
                }

                if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot stored rows must be finite.");
                }
            }

            if (Labels == null || Labels.Length != Rows.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'Labels' must list exactly one label per stored row.");
            }

            if (Labels.Any(l => l != 0 && l != 1))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot labels must be 0 or 1.");
            }

            if (NeighbourCount < 1)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot neighbour count must be at least 1.");
            }

            if (NeighbourCount > Rows.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot neighbour count must not exceed the number of stored rows.");
            }
        }
    }
}
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
    public sealed class AdvancedAnalyticsMahalanobisAnomalyPipelineSnapshot : AdvancedAnalyticsSnapshot
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
        public int[] ActiveInputs { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Centre { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[][] InverseCovariance { get; set; } = [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);
            RequireLength(nameof(Means), Means, inputCount);
            RequireLength(nameof(StandardDeviations), StandardDeviations, inputCount);

            if (ActiveInputs == null || ActiveInputs.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot array 'ActiveInputs' is missing or empty.");
            }

            if (ActiveInputs.Any(i => i < 0 || i >= inputCount) ||
                ActiveInputs.Distinct().Count() != ActiveInputs.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'ActiveInputs' must be distinct input positions.");
            }

            var d = ActiveInputs.Length;

            RequireLength(nameof(Centre), Centre, d);

            if (InverseCovariance == null || InverseCovariance.Length != d ||
                InverseCovariance.Any(row => row.Length != d))
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'InverseCovariance' must be a square matrix matching ActiveInputs.");
            }

            foreach (var (name, values) in new[]
                     {
                         (nameof(FillValues), FillValues), (nameof(Means), Means),
                         (nameof(StandardDeviations), StandardDeviations), (nameof(Centre), Centre)
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

            foreach (var row in InverseCovariance)
            {
                if (row.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'InverseCovariance' must be finite.");
                }
            }

            for (var i = 0; i < d; i++)
            for (var j = 0; j < d; j++)
            {
                if (Math.Abs(InverseCovariance[i][j] - InverseCovariance[j][i]) >
                    1e-6 * Math.Max(1d, Math.Abs(InverseCovariance[i][j])))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'InverseCovariance' must be symmetric.");
                }
            }
        }
    }
}
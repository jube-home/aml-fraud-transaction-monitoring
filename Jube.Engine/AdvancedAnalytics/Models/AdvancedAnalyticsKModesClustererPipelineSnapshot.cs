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
    public sealed class AdvancedAnalyticsKModesClustererPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[][] Modes { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public KModesSnapshotCluster[] Clusters { get; set; } = [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);

            if (FillValues.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot array 'FillValues' must be finite.");
            }

            if (FillValues.Any(v => Math.Abs(v - Math.Round(v, MidpointRounding.AwayFromZero)) > 0.0001))
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot array 'FillValues' must hold whole-number category codes.");
            }

            if (Modes == null || Modes.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no modes.");
            }

            foreach (var mode in Modes)
            {
                if (mode == null || mode.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException($"Every mode must have {inputCount} elements.");
                }

                if (mode.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot modes must be finite.");
                }

                if (mode.Any(v => Math.Abs(v - Math.Round(v, MidpointRounding.AwayFromZero)) > 0.0001))
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot modes must hold whole-number category codes.");
                }
            }

            if (Clusters == null || Clusters.Length != Modes.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'Clusters' must list exactly one entry per mode.");
            }

            for (var i = 0; i < Clusters.Length; i++)
            {
                var cluster = Clusters[i];

                if (cluster == null)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'Clusters' contains a missing entry.");
                }

                if (cluster.Index < 0 || cluster.Index >= Clusters.Length)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot cluster index is out of range.");
                }

                if (double.IsNaN(cluster.Proportion) || cluster.Proportion < 0 || cluster.Proportion > 1)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot cluster proportion must be between 0 and 1.");
                }

                if (cluster.Mode == null || cluster.Mode.Length != inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Every cluster mode must have {inputCount} elements.");
                }

                if (cluster.Mode.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot cluster modes must be finite.");
                }

                if (cluster.Mode.Any(v => Math.Abs(v - Math.Round(v, MidpointRounding.AwayFromZero)) > 0.0001))
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot cluster modes must hold whole-number category codes.");
                }
            }

            if (Clusters.Select(c => c.Index).Distinct().Count() != Clusters.Length)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot 'Clusters' indices must be distinct.");
            }
        }
    }
}
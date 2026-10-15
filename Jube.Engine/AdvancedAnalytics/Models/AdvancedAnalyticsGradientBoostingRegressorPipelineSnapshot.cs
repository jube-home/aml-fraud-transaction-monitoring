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
    public sealed class AdvancedAnalyticsGradientBoostingRegressorPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double BaseValue { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double LearningRate { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public GradientBoostingTreeSnapshot[] Trees { get; set; } = [];

        public override void Validate(int inputCount)
        {
            if (!double.IsFinite(BaseValue))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot base value must be finite.");
            }

            if (!double.IsFinite(LearningRate) || LearningRate <= 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot learning rate must be finite and positive.");
            }

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

            if (Trees == null || Trees.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no trees.");
            }

            foreach (var tree in Trees)
            {
                if (tree == null)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'Trees' contains a missing entry.");
                }

                if (tree.FeatureIndex == null || tree.FeatureIndex.Length == 0)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot tree has no nodes.");
                }

                var nodeCount = tree.FeatureIndex.Length;

                if (tree.Threshold == null || tree.Threshold.Length != nodeCount ||
                    tree.Left == null || tree.Left.Length != nodeCount ||
                    tree.Right == null || tree.Right.Length != nodeCount ||
                    tree.Value == null || tree.Value.Length != nodeCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot tree node arrays must all be the same length.");
                }

                for (var i = 0; i < nodeCount; i++)
                {
                    var value = tree.Value[i];

                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            "Snapshot tree node values must be finite.");
                    }

                    var isLeaf = tree.FeatureIndex[i] == -1;

                    if (isLeaf)
                    {
                        continue;
                    }

                    if (tree.FeatureIndex[i] < 0 || tree.FeatureIndex[i] >= inputCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            "Snapshot tree feature index is out of range.");
                    }

                    if (!double.IsFinite(tree.Threshold[i]))
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree threshold must be finite.");
                    }

                    if (tree.Left[i] < 0 || tree.Left[i] >= nodeCount || tree.Right[i] < 0 ||
                        tree.Right[i] >= nodeCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree child index is out of range.");
                    }
                }
            }
        }
    }
}
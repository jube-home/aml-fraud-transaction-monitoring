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
    public sealed class AdvancedAnalyticsIsolationForestAnomalyPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int SubsampleSize { get; set; }

        [JsonProperty(Required = Required.Always)]
        public IsolationForestTreeSnapshot[] Trees { get; set; } = [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);

            if (FillValues.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot array 'FillValues' must be finite.");
            }

            if (SubsampleSize < 1)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot 'SubsampleSize' must be at least 1.");
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

                if (tree.SplitValue == null || tree.SplitValue.Length != nodeCount ||
                    tree.Left == null || tree.Left.Length != nodeCount ||
                    tree.Right == null || tree.Right.Length != nodeCount ||
                    tree.Size == null || tree.Size.Length != nodeCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot tree node arrays must all be the same length.");
                }

                for (var i = 0; i < nodeCount; i++)
                {
                    if (tree.Size[i] < 1)
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree node sizes must be at least 1.");
                    }

                    var isLeaf = tree.FeatureIndex[i] == -1;

                    if (isLeaf)
                    {
                        if (tree.Left[i] != -1 || tree.Right[i] != -1)
                        {
                            throw new AdvancedAnalyticsSnapshotException(
                                $"Snapshot leaf node {i} must have no children.");
                        }

                        continue;
                    }

                    if (tree.FeatureIndex[i] < 0 || tree.FeatureIndex[i] >= inputCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            "Snapshot tree feature index is out of range.");
                    }

                    if (!double.IsFinite(tree.SplitValue[i]))
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree split value must be finite.");
                    }

                    if (tree.Left[i] <= i || tree.Left[i] >= nodeCount ||
                        tree.Right[i] <= i || tree.Right[i] >= nodeCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            $"Snapshot node {i} has a child index that is not a later node.");
                    }
                }
            }
        }
    }
}
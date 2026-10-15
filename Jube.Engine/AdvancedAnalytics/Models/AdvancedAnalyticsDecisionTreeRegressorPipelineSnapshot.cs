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
    public sealed class AdvancedAnalyticsDecisionTreeRegressorPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public bool[] IsLeaf { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] AttributeIndex { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Threshold { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] LeftChild { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public int[] RightChild { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] NodeValue { get; set; } = [];

        public override void Validate(int inputCount)
        {
            RequireLength(nameof(FillValues), FillValues, inputCount);

            if (FillValues.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot fill values must be finite.");
            }

            if (IsLeaf == null || IsLeaf.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no nodes.");
            }

            var nodeCount = IsLeaf.Length;

            if (AttributeIndex == null || AttributeIndex.Length != nodeCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot 'AttributeIndex' must have exactly one entry per node ({nodeCount}).");
            }

            if (Threshold == null || Threshold.Length != nodeCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot 'Threshold' must have exactly one entry per node ({nodeCount}).");
            }

            if (LeftChild == null || LeftChild.Length != nodeCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot 'LeftChild' must have exactly one entry per node ({nodeCount}).");
            }

            if (RightChild == null || RightChild.Length != nodeCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot 'RightChild' must have exactly one entry per node ({nodeCount}).");
            }

            if (NodeValue == null || NodeValue.Length != nodeCount)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Snapshot 'NodeValue' must have exactly one entry per node ({nodeCount}).");
            }

            for (var i = 0; i < nodeCount; i++)
            {
                if (double.IsNaN(NodeValue[i]) || double.IsInfinity(NodeValue[i]))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot node {i} has a non-finite value.");
                }

                if (IsLeaf[i])
                {
                    if (LeftChild[i] != -1 || RightChild[i] != -1)
                    {
                        throw new AdvancedAnalyticsSnapshotException($"Snapshot leaf node {i} must have no children.");
                    }

                    continue;
                }

                if (AttributeIndex[i] < 0 || AttributeIndex[i] >= inputCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot node {i} has an attribute index out of range.");
                }

                if (double.IsNaN(Threshold[i]) || double.IsInfinity(Threshold[i]))
                {
                    throw new AdvancedAnalyticsSnapshotException($"Snapshot node {i} has a non-finite threshold.");
                }

                if (LeftChild[i] <= i || LeftChild[i] >= nodeCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot node {i} has a left child index that is not a later node.");
                }

                if (RightChild[i] <= i || RightChild[i] >= nodeCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        $"Snapshot node {i} has a right child index that is not a later node.");
                }
            }

            if (IsLeaf[0] && nodeCount != 1)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot root is a leaf but other nodes are present.");
            }
        }
    }
}
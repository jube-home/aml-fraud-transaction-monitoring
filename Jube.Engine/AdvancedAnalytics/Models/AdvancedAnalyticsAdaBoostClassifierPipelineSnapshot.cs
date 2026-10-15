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
    public sealed class AdvancedAnalyticsAdaBoostClassifierPipelineSnapshot : AdvancedAnalyticsSnapshot
    {
        [JsonProperty(Required = Required.Always)]
        public double[] FillValues { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Means { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] StandardDeviations { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public AdaBoostTreeSnapshot[] Trees { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public double[] Alphas { get; set; } = [];

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

            if (Trees == null || Trees.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot has no trees.");
            }

            if (Alphas == null || Alphas.Length != Trees.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot 'Alphas' must have one entry for every tree.");
            }

            if (Alphas.Any(a => double.IsNaN(a) || double.IsInfinity(a) || a < 0))
            {
                throw new AdvancedAnalyticsSnapshotException(
                    "Snapshot alpha values must be finite and not negative.");
            }

            if (Alphas.Sum() <= 0)
            {
                throw new AdvancedAnalyticsSnapshotException("Snapshot alpha values must sum to more than zero.");
            }

            foreach (var tree in Trees)
            {
                if (tree == null)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot 'Trees' contains a missing entry.");
                }

                if (tree.IsLeaf == null || tree.IsLeaf.Length == 0)
                {
                    throw new AdvancedAnalyticsSnapshotException("Snapshot tree has no nodes.");
                }

                var nodeCount = tree.IsLeaf.Length;

                if (tree.AttributeIndex == null || tree.AttributeIndex.Length != nodeCount ||
                    tree.Threshold == null || tree.Threshold.Length != nodeCount ||
                    tree.LeftChild == null || tree.LeftChild.Length != nodeCount ||
                    tree.RightChild == null || tree.RightChild.Length != nodeCount ||
                    tree.NodeVote == null || tree.NodeVote.Length != nodeCount)
                {
                    throw new AdvancedAnalyticsSnapshotException(
                        "Snapshot tree node arrays must all be the same length.");
                }

                for (var i = 0; i < nodeCount; i++)
                {
                    var vote = tree.NodeVote[i];

                    if (double.IsNaN(vote) || double.IsInfinity(vote) || vote < 0 || vote > 1)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            "Snapshot tree node votes must be finite and between 0 and 1.");
                    }

                    if (tree.IsLeaf[i])
                    {
                        continue;
                    }

                    if (tree.AttributeIndex[i] < 0 || tree.AttributeIndex[i] >= inputCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException(
                            "Snapshot tree feature index is out of range.");
                    }

                    if (!double.IsFinite(tree.Threshold[i]))
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree threshold must be finite.");
                    }

                    if (tree.LeftChild[i] < 0 || tree.LeftChild[i] >= nodeCount || tree.RightChild[i] < 0 ||
                        tree.RightChild[i] >= nodeCount)
                    {
                        throw new AdvancedAnalyticsSnapshotException("Snapshot tree child index is out of range.");
                    }
                }
            }
        }
    }
}
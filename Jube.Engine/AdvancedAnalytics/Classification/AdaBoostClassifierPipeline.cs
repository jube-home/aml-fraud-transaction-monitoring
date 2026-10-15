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
using Jube.Analytics;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class AdaBoostClassifierPipeline
    {
        private readonly double alphaTotal;
        private readonly double[] alphas;
        private readonly double[] fill;
        private readonly double[] means;
        private readonly double[] standardDeviations;
        private readonly AdaBoostTree[] trees;

        public AdaBoostClassifierPipeline(double[] fill, double[] means, double[] standardDeviations,
            AdaBoostTree[] trees, double[] alphas, Calibrator? calibrator)
        {
            if (trees.Length != alphas.Length)
            {
                throw new ArgumentException("There must be exactly one alpha per tree.");
            }

            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.trees = trees;
            this.alphas = (double[])alphas.Clone();
            alphaTotal = 0d;

            foreach (var alpha in this.alphas)
            {
                alphaTotal += alpha;
            }

            Calibrator = calibrator;
        }

        public int InputCount => fill.Length;
        public int TreeCount => trees.Length;
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[] Alphas => (double[])alphas.Clone();
        public Calibrator? Calibrator { get; }

        public double[] Prepare(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var prepared = new double[count];

            for (var j = 0; j < count; j++)
            {
                prepared[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
            }

            return prepared;
        }

        public double RawScore(double[] row)
        {
            var prepared = Prepare(row);

            if (alphaTotal <= 0)
            {
                return 0d;
            }

            var weighted = 0d;

            for (var t = 0; t < trees.Length; t++)
            {
                weighted += alphas[t] * trees[t].Sign(prepared);
            }

            return weighted / alphaTotal;
        }

        public double FinalProbability(double[] row)
        {
            var raw = RawScore(row);

            return Calibrator is { } calibrator ? calibrator.Apply(raw) : raw;
        }

        public (double RawScore, double BaseValue, double[] Contributions) Explain(double[] row)
        {
            var prepared = Prepare(row);
            var contributions = new double[fill.Length];
            var unattributed = 0d;

            if (alphaTotal <= 0)
            {
                return (0d, 0d, contributions);
            }

            for (var t = 0; t < trees.Length; t++)
            {
                var alpha = alphas[t];
                var path = trees[t].Path(prepared);
                var attributeIndex = trees[t].AttributeIndex;
                var leafVote = trees[t].NodeVote[path[^1]];
                var sign = leafVote >= 0.5 ? 1 : -1;
                var vote = alpha * sign;
                var splitCount = path.Count - 1;

                if (splitCount > 0)
                {
                    var share = vote / splitCount;

                    for (var depth = 0; depth < splitCount; depth++)
                    {
                        contributions[attributeIndex[path[depth]]] += share;
                    }
                }
                else
                {
                    unattributed += vote;
                }
            }

            var rawScore = (unattributed + Sum(contributions)) / alphaTotal;
            var baseValue = unattributed / alphaTotal;

            for (var j = 0; j < contributions.Length; j++)
            {
                contributions[j] /= alphaTotal;
            }

            return (rawScore, baseValue, contributions);
        }

        private static double Sum(double[] values)
        {
            var total = 0d;

            foreach (var value in values)
            {
                total += value;
            }

            return total;
        }
    }
}
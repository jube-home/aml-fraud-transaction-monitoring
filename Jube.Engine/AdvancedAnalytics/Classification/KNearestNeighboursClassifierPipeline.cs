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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class KNearestNeighboursClassifierPipeline
    {
        private readonly double[] fill;
        private readonly int[] labels;
        private readonly double[] means;
        private readonly double[][] rows;
        private readonly double[] standardDeviations;

        public KNearestNeighboursClassifierPipeline(bool standardise, double[] fill, double[] means,
            double[] standardDeviations, double[][] rows, int[] labels, int neighbourCount, Calibrator? calibrator)
        {
            if (rows.Length != labels.Length)
            {
                throw new ArgumentException("There must be exactly one label per stored row.");
            }

            if (neighbourCount < 1)
            {
                throw new ArgumentException("The neighbour count must be at least 1.");
            }

            if (neighbourCount > rows.Length)
            {
                throw new ArgumentException(
                    $"The neighbour count {neighbourCount} is larger than the {rows.Length} training row(s) the " +
                    "model has to search; either lower the neighbour count or train on more rows.");
            }

            Standardise = standardise;
            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.rows = [.. rows.Select(r => (double[])r.Clone())];
            this.labels = (int[])labels.Clone();
            NeighbourCount = neighbourCount;
            Calibrator = calibrator;
        }

        public int InputCount => fill.Length;
        public int NeighbourCount { get; }
        public int StoredRowCount => rows.Length;
        public bool Standardise { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[][] Rows => [.. rows.Select(r => (double[])r.Clone())];
        public int[] Labels => (int[])labels.Clone();
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
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                prepared[j] = Standardise ? Standardisation.ZScore(value, means[j], standardDeviations[j]) : value;
            }

            return prepared;
        }

        public KNearestNeighboursRawExplain Explain(double[] row)
        {
            var prepared = Prepare(row);
            var count = prepared.Length;
            var distancesSquared = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = 0d;
                var stored = rows[i];

                for (var j = 0; j < count; j++)
                {
                    var d = prepared[j] - stored[j];
                    sum += d * d;
                }

                distancesSquared[i] = sum;
            }

            var neighbourIndices = Enumerable.Range(0, rows.Length)
                .OrderBy(i => distancesSquared[i])
                .Take(NeighbourCount)
                .ToArray();

            var neighbourLabels = new int[NeighbourCount];
            var neighbourDistances = new double[NeighbourCount];
            var perVariable = new double[count];
            var total = 0d;

            for (var k = 0; k < NeighbourCount; k++)
            {
                var index = neighbourIndices[k];
                neighbourLabels[k] = labels[index];
                neighbourDistances[k] = Math.Sqrt(distancesSquared[index]);
                var stored = rows[index];

                for (var j = 0; j < count; j++)
                {
                    var d = prepared[j] - stored[j];
                    var squared = d * d;
                    perVariable[j] += squared;
                    total += squared;
                }
            }

            var shares = new double[count];

            for (var j = 0; j < count; j++)
            {
                shares[j] = total > 0 ? perVariable[j] / total : 1d / count;
            }

            var votes = neighbourLabels.Count(l => l == 1);
            var voteFraction = votes / (double)NeighbourCount;

            return new KNearestNeighboursRawExplain(voteFraction, neighbourLabels, neighbourDistances, shares);
        }

        public double VoteFraction(double[] row)
        {
            return Explain(row).VoteFraction;
        }

        public double Probability(double[] row)
        {
            var vote = VoteFraction(row);

            return Calibrator is { } calibrator ? calibrator.Apply(vote) : vote;
        }
    }
}
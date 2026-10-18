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
using Accord.Statistics.Distributions.Multivariate;
using Jube.Analytics;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    internal sealed class GaussianMixtureClustererPipeline
    {
        private readonly double[][,] componentCovariances;
        private readonly double[][] componentMeans;
        private readonly MultivariateNormalDistribution[] components;
        private readonly double[] fill;
        private readonly double[] logMixtureWeights;
        private readonly double[] means;
        private readonly double[] mixtureWeights;
        private readonly double[] standardDeviations;

        public GaussianMixtureClustererPipeline(bool standardise, double[] fill, double[] means,
            double[] standardDeviations, double[] mixtureWeights, double[][] componentMeans,
            double[][,] componentCovariances)
        {
            Standardise = standardise;
            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.mixtureWeights = (double[])mixtureWeights.Clone();
            logMixtureWeights = [.. this.mixtureWeights.Select(v => Math.Log(v))];
            this.componentMeans = [.. componentMeans.Select(m => (double[])m.Clone())];
            this.componentCovariances = [.. componentCovariances.Select(c => (double[,])c.Clone())];

            components =
            [
                .. this.componentMeans
                    .Select((mean, i) => new MultivariateNormalDistribution(mean, this.componentCovariances[i], true))
            ];
        }

        public int InputCount => fill.Length;
        public int ComponentCount => components.Length;
        public bool Standardise { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[] MixtureWeights => (double[])mixtureWeights.Clone();
        public double[][] ComponentMeans => [.. componentMeans.Select(m => (double[])m.Clone())];
        public double[][,] ComponentCovariances => [.. componentCovariances.Select(c => (double[,])c.Clone())];

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

        public double[] PosteriorsByComponentIndex(double[] row)
        {
            var prepared = Prepare(row);
            var jointLog = new double[components.Length];

            for (var c = 0; c < components.Length; c++)
            {
                jointLog[c] = logMixtureWeights[c] + components[c].LogProbabilityDensityFunction(prepared);
            }

            var max = jointLog.Max();

            if (double.IsNegativeInfinity(max))
            {
                return [.. Enumerable.Repeat(1d / jointLog.Length, jointLog.Length)];
            }

            var sumExp = 0d;

            for (var c = 0; c < jointLog.Length; c++)
            {
                sumExp += Math.Exp(jointLog[c] - max);
            }

            var posteriors = new double[jointLog.Length];

            for (var c = 0; c < jointLog.Length; c++)
            {
                posteriors[c] = Math.Exp(jointLog[c] - max) / sumExp;
            }

            return posteriors;
        }
    }
}
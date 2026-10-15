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

using Accord.Statistics;
using Accord.Statistics.Analysis;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    internal sealed class PrincipalComponentFit
    {
        private readonly PrincipalComponentAnalysis analysis;
        private readonly PrincipalComponentBasis basis;
        private readonly double[] signs;

        private PrincipalComponentFit(PrincipalComponentAnalysis analysis, PrincipalComponentBasis basis, int rows,
            double[] reportedStandardDeviations)
        {
            this.analysis = analysis;
            this.basis = basis;
            Rows = rows;
            Means = analysis.Means;
            StandardDeviations = reportedStandardDeviations;

            Scale = basis == PrincipalComponentBasis.Correlation
                ? reportedStandardDeviations
                : Enumerable.Repeat(1.0, analysis.Means.Length).ToArray();

            Eigenvalues = analysis.Eigenvalues;
            SingularValues = analysis.SingularValues;
            Proportions = analysis.ComponentProportions;
            Cumulative = analysis.CumulativeProportions;

            Loadings = analysis.ComponentVectors.Select(v => (double[])v.Clone()).ToArray();
            signs = new double[Loadings.Length];

            for (var c = 0; c < Loadings.Length; c++)
            {
                var largest = 0.0;

                foreach (var value in Loadings[c])
                {
                    if (Math.Abs(value) > Math.Abs(largest))
                    {
                        largest = value;
                    }
                }

                signs[c] = largest < 0 ? -1.0 : 1.0;

                for (var j = 0; j < Loadings[c].Length; j++)
                {
                    Loadings[c][j] *= signs[c];
                }
            }
        }

        public int Rows { get; }
        public double[] Means { get; }
        public double[] StandardDeviations { get; }
        public double[] Scale { get; }
        public double[] Eigenvalues { get; }
        public double[] SingularValues { get; }
        public double[] Proportions { get; }
        public double[] Cumulative { get; }
        public double[][] Loadings { get; }
        public int Available => Eigenvalues.Length;

        public static PrincipalComponentFit Create(double[][] data, string name, PrincipalComponentBasis basis)
        {
            MultivariateGuard.Matrix(data, name, 2, MultivariateLimits.MaximumProjectionRows);

            if (basis == PrincipalComponentBasis.Correlation)
            {
                MultivariateGuard.RequireVariation(data, name);
            }

            var analysis = new PrincipalComponentAnalysis(
                basis == PrincipalComponentBasis.Correlation
                    ? PrincipalComponentMethod.Standardize
                    : PrincipalComponentMethod.Center);
            analysis.Learn(data);

            var standardDeviations = basis == PrincipalComponentBasis.Correlation
                ? analysis.StandardDeviations
                : data.StandardDeviation(analysis.Means);

            return new PrincipalComponentFit(analysis, basis, data.Length, standardDeviations);
        }

        public int ResolveComponents(int requested)
        {
            if (requested == 0)
            {
                return Available;
            }

            return MultivariateGuard.Bounded(requested, "components", 1, Available);
        }

        public double[][] Score(double[][] rows, int components)
        {
            analysis.NumberOfOutputs = components;
            var scores = analysis.Transform(rows);

            for (var i = 0; i < scores.Length; i++)
            {
                for (var c = 0; c < components; c++)
                {
                    scores[i][c] *= signs[c];
                }
            }

            return scores;
        }

        public double[] Communalities(int components)
        {
            var variables = Means.Length;
            var result = new double[variables];

            for (var j = 0; j < variables; j++)
            {
                var partial = 0.0;
                var total = 0.0;

                for (var c = 0; c < Available; c++)
                {
                    var share = Eigenvalues[c] * Loadings[c][j] * Loadings[c][j];
                    total += share;

                    if (c < components)
                    {
                        partial += share;
                    }
                }

                result[j] = total > 0 ? partial / total : 0.0;
            }

            return result;
        }

        public PrincipalComponentAnalysisResult ToResult(double[][] data, int components, bool returnScores)
        {
            var loadings = Loadings.Take(components).Select(v => (double[])v.Clone()).ToArray();

            var scaled = loadings
                .Select((v, c) => v.Select(x => x * Math.Sqrt(Math.Max(0.0, Eigenvalues[c]))).ToArray())
                .ToArray();

            var mean = Eigenvalues.Average();

            return new PrincipalComponentAnalysisResult(
                Rows,
                Means.Length,
                components,
                basis.ToString(),
                Means,
                StandardDeviations,
                Eigenvalues,
                Proportions,
                Cumulative,
                SingularValues,
                loadings,
                scaled,
                Communalities(components),
                returnScores ? Score(data, components) : Array.Empty<double[]>(),
                analysis.GetNumberOfComponents(0.8),
                analysis.GetNumberOfComponents(0.9),
                analysis.GetNumberOfComponents(0.95),
                analysis.GetNumberOfComponents(0.99),
                Eigenvalues.Count(e => e > mean));
        }
    }
}
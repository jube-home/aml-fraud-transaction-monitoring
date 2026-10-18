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

using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Models.Regression.Linear;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal sealed class LinearFit
    {
        public int N { get; private set; }
        public int K { get; private set; }
        public bool Intercept { get; private set; }
        public string[] Terms { get; private set; } = Array.Empty<string>();
        public double[] Beta { get; private set; } = Array.Empty<double>();
        public double[][] XtXInverse { get; private set; } = Array.Empty<double[]>();
        public double[][] Design { get; private set; } = Array.Empty<double[]>();
        public double[] Y { get; private set; } = Array.Empty<double>();
        public double[]? Weights { get; private set; }
        public double[] Fitted { get; private set; } = Array.Empty<double>();
        public double[] Residuals { get; private set; } = Array.Empty<double>();
        public double[] Leverage { get; private set; } = Array.Empty<double>();
        public double Rss { get; private set; }
        public double Tss { get; private set; }
        public double DfResidual { get; private set; }
        public double DfModel { get; private set; }
        public double Sigma2 { get; private set; }
        public double RSquared { get; private set; }
        public double AdjustedRSquared { get; private set; }
        public double FStatistic { get; private set; }
        public double FpValue { get; private set; }
        public double LogLikelihood { get; private set; }
        public double Aic { get; private set; }
        public double Aicc { get; private set; }
        public double Bic { get; private set; }
        public double Sigma => Math.Sqrt(Sigma2);

        public double StandardError(int index)
        {
            return Math.Sqrt(Sigma2 * XtXInverse[index][index]);
        }

        public double[] DesignRow(double[] x)
        {
            return Intercept ? new[] { 1d }.Concat(x).ToArray() : x;
        }

        public double Predict(double[] x)
        {
            var z = DesignRow(x);
            var sum = 0d;

            for (var j = 0; j < z.Length; j++)
            {
                sum += z[j] * Beta[j];
            }

            return sum;
        }

        public double QuadraticForm(double[] x)
        {
            var z = DesignRow(x);
            var sum = 0d;

            for (var a = 0; a < z.Length; a++)
            {
                for (var b = 0; b < z.Length; b++)
                {
                    sum += z[a] * XtXInverse[a][b] * z[b];
                }
            }

            return sum;
        }

        public static void EnsureFullRank(double[][] design, string[] terms)
        {
            var k = design[0].Length;
            var norms = new double[k];

            foreach (var row in design)
            {
                for (var j = 0; j < k; j++)
                {
                    norms[j] += row[j] * row[j];
                }
            }

            for (var j = 0; j < k; j++)
            {
                if (!(norms[j] > 0))
                {
                    throw new ArgumentException(
                        $"Column '{terms[j]}' is all zeros, so its coefficient cannot be estimated. Remove it.",
                        "inputs");
                }

                norms[j] = Math.Sqrt(norms[j]);
            }

            var gram = new double[k][];

            for (var a = 0; a < k; a++)
            {
                gram[a] = new double[k];
            }

            foreach (var row in design)
            {
                for (var a = 0; a < k; a++)
                {
                    var za = row[a] / norms[a];

                    for (var b = a; b < k; b++)
                    {
                        gram[a][b] += za * row[b] / norms[b];
                    }
                }
            }

            for (var a = 0; a < k; a++)
            {
                for (var b = 0; b < a; b++)
                {
                    gram[a][b] = gram[b][a];
                }
            }

            var singular = new JaggedSingularValueDecomposition(gram, false, false).Diagonal;
            var largest = singular.Max();
            var smallest = singular.Min();

            if (!(smallest > largest * 1e-13))
            {
                var culprit = terms.Length > 1
                    ? " Run VarianceInflationFactors to find the redundant column."
                    : string.Empty;

                throw new ArgumentException(
                    "The predictor columns are perfectly (or almost perfectly) collinear, so the coefficients are not identifiable. Remove a redundant or constant column." +
                    culprit,
                    "inputs");
            }
        }

        public static LinearFit Fit(double[][] x, double[] y, double[]? weights, string[] names, bool intercept)
        {
            var n = x.Length;
            var p = x[0].Length;
            var k = p + (intercept ? 1 : 0);

            if (k == 0)
            {
                throw new ArgumentException(
                    "There is nothing to fit: supply at least one predictor or include an intercept.",
                    "inputs");
            }

            if (n <= k)
            {
                throw new ArgumentException(
                    $"There are {n} usable row(s) for {k} coefficient(s). Supply more rows than coefficients so the residual variance can be estimated.",
                    "inputs");
            }

            var terms = intercept ? new[] { "(Intercept)" }.Concat(names).ToArray() : names.ToArray();
            var design = new double[n][];

            for (var i = 0; i < n; i++)
            {
                design[i] = intercept ? new[] { 1d }.Concat(x[i]).ToArray() : (double[])x[i].Clone();
            }

            if (p > 0)
            {
                EnsureFullRank(design, terms);
            }

            var beta = new double[k];
            var inverse = new double[k][];

            for (var a = 0; a < k; a++)
            {
                inverse[a] = new double[k];
            }

            if (p == 0)
            {
                var totalWeight = weights == null ? n : weights.Sum();
                beta[0] = weights == null ? y.Average() : y.Select((v, i) => v * weights[i]).Sum() / totalWeight;
                inverse[0][0] = 1 / totalWeight;
            }
            else
            {
                var ols = new OrdinaryLeastSquares { UseIntercept = intercept };

                var regression = ols.Learn(x.Select(r => (double[])r.Clone()).ToArray(), (double[])y.Clone(),
                    weights == null ? null : (double[])weights.Clone());

                var information = ols.GetInformationMatrix();
                var offset = intercept ? 1 : 0;

                if (intercept)
                {
                    beta[0] = regression.Intercept;
                }

                for (var j = 0; j < p; j++)
                {
                    beta[j + offset] = regression.Weights[j];
                }

                for (var a = 0; a < k; a++)
                {
                    var sa = MapIndex(a, p, intercept);

                    for (var b = 0; b < k; b++)
                    {
                        inverse[a][b] = information[sa][MapIndex(b, p, intercept)];
                    }
                }
            }

            var fitted = new double[n];
            var residuals = new double[n];
            var leverage = new double[n];
            var rss = 0d;
            var sumW = 0d;
            var sumWy = 0d;
            var sumWyy = 0d;

            for (var i = 0; i < n; i++)
            {
                var wi = weights?[i] ?? 1d;
                var f = 0d;

                for (var j = 0; j < k; j++)
                {
                    f += design[i][j] * beta[j];
                }

                fitted[i] = f;
                residuals[i] = y[i] - f;
                rss += wi * residuals[i] * residuals[i];
                sumW += wi;
                sumWy += wi * y[i];
                sumWyy += wi * y[i] * y[i];

                var q = 0d;

                for (var a = 0; a < k; a++)
                {
                    for (var b = 0; b < k; b++)
                    {
                        q += design[i][a] * inverse[a][b] * design[i][b];
                    }
                }

                leverage[i] = wi * q;
            }

            double tss;

            if (intercept)
            {
                var mean = sumWy / sumW;
                tss = 0;

                for (var i = 0; i < n; i++)
                {
                    var d = y[i] - mean;
                    tss += (weights?[i] ?? 1d) * d * d;
                }
            }
            else
            {
                tss = sumWyy;
            }

            var dfResidual = n - k;
            var dfModel = k - (intercept ? 1 : 0);
            var sigma2 = rss / dfResidual;
            var rSquared = tss > 0 ? 1 - rss / tss : double.NaN;

            var adjusted = tss > 0
                ? 1 - (1 - rSquared) * (intercept ? (n - 1d) / dfResidual : (double)n / dfResidual)
                : double.NaN;

            var fStatistic = dfModel > 0 && sigma2 > 0 ? (tss - rss) / dfModel / sigma2 : double.NaN;
            var fp = dfModel > 0 ? RegressionStats.FUpperTail(fStatistic, dfModel, dfResidual) : double.NaN;
            var logSumW = weights == null ? 0d : weights.Sum(Math.Log);

            var logLikelihood = rss > 0
                ? -n / 2d * (Math.Log(2 * Math.PI) + Math.Log(rss / n) + 1) + 0.5 * logSumW
                : double.NaN;

            var parameters = k + 1;
            var aic = -2 * logLikelihood + 2 * parameters;

            var aicc = n - parameters - 1 > 0
                ? aic + 2d * parameters * (parameters + 1) / (n - parameters - 1)
                : double.NaN;

            var bic = -2 * logLikelihood + parameters * Math.Log(n);

            return new LinearFit
            {
                N = n,
                K = k,
                Intercept = intercept,
                Terms = terms,
                Beta = beta,
                XtXInverse = inverse,
                Design = design,
                Y = y,
                Weights = weights,
                Fitted = fitted,
                Residuals = residuals,
                Leverage = leverage,
                Rss = rss,
                Tss = tss,
                DfResidual = dfResidual,
                DfModel = dfModel,
                Sigma2 = sigma2,
                RSquared = rSquared,
                AdjustedRSquared = adjusted,
                FStatistic = fStatistic,
                FpValue = fp,
                LogLikelihood = logLikelihood,
                Aic = aic,
                Aicc = aicc,
                Bic = bic
            };
        }

        private static int MapIndex(int termIndex, int p, bool intercept)
        {
            if (!intercept)
            {
                return termIndex;
            }

            return termIndex == 0 ? p : termIndex - 1;
        }
    }
}
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
using Accord.Math.Optimization;
using Accord.Statistics;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class MultivariateEngine
    {
        public const int MaximumRows = 100000;

        public static double[][] Rows(double[][]? rows, string name, int minimumRows = 1, int? columns = null)
        {
            if (rows == null || rows.Length < minimumRows)
            {
                throw new ArgumentException(
                    $"{name} needs at least {minimumRows} row(s), each an array of numbers (one array per case).",
                    name);
            }

            if (rows.Length > MaximumRows)
            {
                throw new ArgumentException($"{name} has {rows.Length} rows; at most {MaximumRows} are accepted.",
                    name);
            }

            var width = columns ?? (rows[0]?.Length ?? 0);

            if (width < 1)
            {
                throw new ArgumentException($"{name} rows must contain at least one number.", name);
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].Length != width)
                {
                    throw new ArgumentException(
                        $"{name}[{i}] has {rows[i]?.Length ?? 0} entries but {width} were expected: every row must have the same length.",
                        name);
                }

                for (var j = 0; j < width; j++)
                {
                    if (!double.IsFinite(rows[i][j]))
                    {
                        throw new ArgumentException(
                            $"{name}[{i}][{j}] is {rows[i][j]}: remove missing (NaN) or infinite entries first.", name);
                    }
                }
            }

            return rows;
        }

        public static double[] Vector(double[]? vector, string name, int? length = null)
        {
            DistributionGuard.Values(vector, name);

            if (length.HasValue && vector!.Length != length.Value)
            {
                throw new ArgumentException($"{name} has {vector.Length} entries but {length.Value} were expected.",
                    name);
            }

            return vector!;
        }

        public static double[,] Square(double[][]? matrix, string name, bool symmetric)
        {
            if (matrix == null || matrix.Length == 0)
            {
                throw new ArgumentException($"{name} must be a non-empty square matrix given as an array of rows.",
                    name);
            }

            var n = matrix.Length;
            Rows(matrix, name, 1, n);
            var result = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    result[i, j] = matrix[i][j];
                }
            }

            if (symmetric)
            {
                for (var i = 0; i < n; i++)
                {
                    for (var j = i + 1; j < n; j++)
                    {
                        var tolerance = 1e-8 * Math.Max(1.0, Math.Max(Math.Abs(result[i, j]), Math.Abs(result[j, i])));

                        if (Math.Abs(result[i, j] - result[j, i]) > tolerance)
                        {
                            throw new ArgumentException(
                                $"{name} must be symmetric but [{i}][{j}] = {result[i, j]} differs from [{j}][{i}] = {result[j, i]}.",
                                name);
                        }
                    }
                }
            }

            return result;
        }

        public static double[][] ToJagged(double[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new double[cols];

                for (var j = 0; j < cols; j++)
                {
                    result[i][j] = matrix[i, j];
                }
            }

            return result;
        }

        public static MultivariateNormalDistribution Normal(double[] mean, double[][] covariance)
        {
            var cov = Square(covariance, "covariance", true);

            if (mean.Length != cov.GetLength(0))
            {
                throw new ArgumentException(
                    $"mean has {mean.Length} entries but covariance is {cov.GetLength(0)} by {cov.GetLength(0)}; they must match.",
                    "mean");
            }

            var chol = new CholeskyDecomposition(cov);

            if (!chol.IsPositiveDefinite)
            {
                throw new ArgumentException(
                    "covariance is not positive definite (some variable is constant or perfectly correlated with others). Remove redundant variables or add a small value to the diagonal.",
                    "covariance");
            }

            return new MultivariateNormalDistribution(mean, cov);
        }

        public static MultivariateNormalEvaluation NormalDensity(double[] mean, double[][] covariance,
            double[][] points)
        {
            var d = Normal(mean, covariance);
            Rows(points, "points", 1, mean.Length);
            var density = new double[points.Length];
            var log = new double[points.Length];
            var dist = new double[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                density[i] = d.ProbabilityDensityFunction(points[i]);
                log[i] = d.LogProbabilityDensityFunction(points[i]);
                dist[i] = Math.Sqrt(Math.Max(d.Mahalanobis(points[i]), 0));
            }

            return new MultivariateNormalEvaluation(mean, covariance, DistributionGuard.N(density),
                DistributionGuard.N(log), DistributionGuard.N(dist));
        }

        public static MahalanobisResult Mahalanobis(double[] mean, double[][] covariance, double[][] points)
        {
            var d = Normal(mean, covariance);
            Rows(points, "points", 1, mean.Length);

            return MahalanobisFor(d, points, mean.Length);
        }

        public static MahalanobisResult MahalanobisFor(MultivariateNormalDistribution d, double[][] points,
            int dimension)
        {
            var squared = new double[points.Length];
            var tail = new double[points.Length];
            var chi = new ChiSquareDistribution(dimension);

            for (var i = 0; i < points.Length; i++)
            {
                squared[i] = Math.Max(d.Mahalanobis(points[i]), 0);
                tail[i] = chi.ComplementaryDistributionFunction(squared[i]);
            }

            return new MahalanobisResult(squared.Select(Math.Sqrt).ToArray(), squared, tail, dimension);
        }

        public static (MultivariateNormalDistribution Distribution, double[] Mean, double[][] Covariance) Estimate(
            double[][] data)
        {
            var p = data[0].Length;
            var mean = new double[p];

            foreach (var row in data)
            {
                for (var j = 0; j < p; j++)
                {
                    mean[j] += row[j] / data.Length;
                }
            }

            var jagged = data.Covariance(mean);

            return (Normal(mean, jagged), mean, jagged);
        }

        public static MultivariateNormalFitResult FitNormal(double[][] data)
        {
            var (d, mean, cov) = Estimate(data);
            var n = data.Length;
            var p = mean.Length;
            var sd = Enumerable.Range(0, p).Select(i => Math.Sqrt(cov[i][i])).ToArray();
            var corr = new double[p][];

            for (var i = 0; i < p; i++)
            {
                corr[i] = new double[p];

                for (var j = 0; j < p; j++)
                {
                    corr[i][j] = cov[i][j] / (sd[i] * sd[j]);
                }
            }

            var ll = 0.0;

            foreach (var row in data)
            {
                ll += d.LogProbabilityDensityFunction(row);
            }

            return new MultivariateNormalFitResult(n, p, mean, cov, corr, sd, ll);
        }

        public static MultivariateNormalCumulativeResult NormalOrthant(double[] mean, double[][] covariance,
            double[][] points)
        {
            var d = Normal(mean, covariance);
            Rows(points, "points", 1, mean.Length);
            var lower = new double[points.Length];
            var upper = new double[points.Length];

            for (var i = 0; i < points.Length; i++)
            {
                lower[i] = d.DistributionFunction(points[i]);
                upper[i] = d.ComplementaryDistributionFunction(points[i]);
            }

            return new MultivariateNormalCumulativeResult(points, DistributionGuard.N(lower),
                DistributionGuard.N(upper));
        }

        public static MultivariateSampleResult SampleNormal(double[] mean, double[][] covariance, int count, int seed)
        {
            var d = Normal(mean, covariance);
            var rows = d.Generate(count, new Random(seed));

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }

        public static double[] MeanOf(double[][] rows)
        {
            var p = rows[0].Length;
            var mean = new double[p];

            foreach (var row in rows)
            {
                for (var j = 0; j < p; j++)
                {
                    mean[j] += row[j] / rows.Length;
                }
            }

            return mean;
        }

        public static DirichletDistribution Dirichlet(double[] concentrations)
        {
            DistributionGuard.Values(concentrations, "concentrations", 2);

            for (var i = 0; i < concentrations.Length; i++)
            {
                if (!(concentrations[i] > 0))
                {
                    throw new ArgumentException(
                        $"concentrations[{i}] = {concentrations[i]} must be greater than 0.", "concentrations");
                }
            }

            return new DirichletDistribution(concentrations);
        }

        public static DirichletSummary DirichletSummaryOf(double[] concentrations)
        {
            var d = Dirichlet(concentrations);

            return new DirichletSummary(concentrations, d.Mean, d.Variance, ToJagged(d.Covariance),
                concentrations.Sum());
        }

        private static void CheckSimplex(double[][] points, int dimension, string name)
        {
            Rows(points, name, 1, dimension);

            for (var i = 0; i < points.Length; i++)
            {
                var sum = points[i].Sum();

                if (Math.Abs(sum - 1.0) > 1e-6 || points[i].Any(v => !(v > 0) || v >= 1 + 1e-9))
                {
                    throw new ArgumentException(
                        $"{name}[{i}] must be a point on the simplex: every entry strictly between 0 and 1 and the entries summing to 1 (sum is {sum}).",
                        name);
                }
            }
        }

        public static DistributionEvaluation DirichletDensity(double[] concentrations, double[][] points)
        {
            var d = Dirichlet(concentrations);
            CheckSimplex(points, concentrations.Length, "points");
            var values = points.Select(p => d.ProbabilityDensityFunction(p)).ToArray();

            return new DistributionEvaluation("Dirichlet", "ProbabilityDensity", concentrations,
                points.Select(p => p[0]).ToArray(), DistributionGuard.N(values));
        }

        public static DirichletFitResult FitDirichlet(double[][] data)
        {
            var k = data[0].Length;

            if (k < 2 || k > 12)
            {
                throw new ArgumentException($"data rows must have between 2 and 12 components, got {k}.", "data");
            }

            if (data.Length < k)
            {
                throw new ArgumentException($"data needs at least {k} rows to fit {k} concentrations.", "data");
            }

            CheckSimplex(data, k, "data");

            var means = MeanOf(data);
            var variance = data.Select(r => r[0]).ToArray().Variance();
            var alpha0 = variance > 0 ? means[0] * (1 - means[0]) / variance - 1 : 10.0;

            if (!(alpha0 > 0) || !double.IsFinite(alpha0))
            {
                alpha0 = 1.0;
            }

            var start = means.Select(m => Math.Log(Math.Max(m * alpha0, 1e-3))).ToArray();

            double Objective(double[] z)
            {
                var a = z.Select(v => Math.Exp(Math.Clamp(v, -20, 20))).ToArray();

                try
                {
                    var d = new DirichletDistribution(a);
                    var sum = 0.0;

                    foreach (var row in data)
                    {
                        sum += d.LogProbabilityDensityFunction(row);
                    }

                    return double.IsFinite(sum) ? -sum : 1e300;
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                {
                    return 1e300;
                }
            }

            var best = start;
            var bestValue = Objective(best);

            for (var pass = 0; pass < 4; pass++)
            {
                var nm = new NelderMead(k, Objective);

                for (var j = 0; j < k; j++)
                {
                    nm.StepSize[j] = pass == 0 ? 0.3 : 0.05;
                }

                nm.Convergence.MaximumEvaluations = 6000 * k;
                nm.Convergence.RelativeFunctionTolerance = 1e-12;
                nm.Convergence.AbsoluteFunctionTolerance = 1e-12;
                nm.Convergence.RelativeParameterTolerance = 1e-10;
                nm.Minimize((double[])best.Clone());

                if (nm.Value < bestValue)
                {
                    bestValue = nm.Value;
                    best = (double[])nm.Solution.Clone();
                }
            }

            if (bestValue >= 1e300)
            {
                throw new ArgumentException("The Dirichlet concentrations could not be estimated from these rows.",
                    "data");
            }

            var alphas = best.Select(v => Math.Exp(Math.Clamp(v, -20, 20))).ToArray();
            var total = alphas.Sum();

            return new DirichletFitResult(data.Length, alphas, alphas.Select(a => a / total).ToArray(), total,
                -bestValue);
        }

        public static MultivariateSampleResult SampleDirichlet(double[] concentrations, int count, int seed)
        {
            Dirichlet(concentrations);
            var random = new Random(seed);
            var gammas = concentrations.Select(a => new GammaDistribution(1.0, a)).ToArray();
            var rows = new double[count][];

            for (var i = 0; i < count; i++)
            {
                var row = new double[concentrations.Length];
                var sum = 0.0;

                for (var j = 0; j < row.Length; j++)
                {
                    row[j] = gammas[j].Generate(random);
                    sum += row[j];
                }

                for (var j = 0; j < row.Length; j++)
                {
                    row[j] /= sum;
                }

                rows[i] = row;
            }

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }

        public static MultinomialDistribution Multinomial(int trials, double[] probabilities)
        {
            if (trials < 1)
            {
                throw new ArgumentException($"trials must be at least 1, got {trials}.", "trials");
            }

            DistributionGuard.Values(probabilities, "probabilities", 2);

            if (probabilities.Any(p => p < 0 || p > 1) || Math.Abs(probabilities.Sum() - 1.0) > 1e-6)
            {
                throw new ArgumentException(
                    $"probabilities must each be between 0 and 1 and sum to 1 (they sum to {probabilities.Sum()}).",
                    "probabilities");
            }

            return new MultinomialDistribution(trials, probabilities);
        }

        public static MultinomialSummary MultinomialSummaryOf(int trials, double[] probabilities)
        {
            var d = Multinomial(trials, probabilities);

            return new MultinomialSummary(trials, probabilities, d.Mean, d.Variance, ToJagged(d.Covariance));
        }

        public static MultinomialEvaluation MultinomialProbabilityOf(int trials, double[] probabilities,
            double[][] counts)
        {
            var d = Multinomial(trials, probabilities);
            Rows(counts, "counts", 1, probabilities.Length);
            var p = new double?[counts.Length];
            var lp = new double?[counts.Length];

            for (var i = 0; i < counts.Length; i++)
            {
                for (var j = 0; j < counts[i].Length; j++)
                {
                    if (counts[i][j] < 0 || Math.Abs(counts[i][j] - Math.Round(counts[i][j])) > 1e-9)
                    {
                        throw new ArgumentException($"counts[{i}][{j}] must be a whole number of 0 or more.", "counts");
                    }
                }

                var ints = counts[i].Select(c => (int)Math.Round(c)).ToArray();

                if (ints.Sum() != trials)
                {
                    p[i] = 0.0;
                    lp[i] = null;
                    continue;
                }

                p[i] = DistributionGuard.N(d.ProbabilityMassFunction(ints));
                lp[i] = DistributionGuard.N(d.LogProbabilityMassFunction(ints));
            }

            return new MultinomialEvaluation(trials, probabilities, p, lp);
        }

        public static MultivariateSampleResult SampleMultinomial(int trials, double[] probabilities, int count,
            int seed)
        {
            Multinomial(trials, probabilities);
            var random = new Random(seed);
            var rows = new double[count][];

            for (var i = 0; i < count; i++)
            {
                var row = new double[probabilities.Length];
                var remaining = trials;
                var remainingProbability = 1.0;

                for (var j = 0; j < probabilities.Length - 1; j++)
                {
                    if (remaining == 0 || remainingProbability <= 0)
                    {
                        break;
                    }

                    var q = Math.Min(1.0, Math.Max(0.0, probabilities[j] / remainingProbability));
                    var draw = q >= 1.0 ? remaining : new BinomialDistribution(remaining, q).Generate(random);
                    row[j] = draw;
                    remaining -= draw;
                    remainingProbability -= probabilities[j];
                }

                row[^1] += remaining;
                rows[i] = row;
            }

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }

        public static WishartDistribution Wishart(int degrees, double[][] scale)
        {
            var s = Square(scale, "scale", true);
            var p = s.GetLength(0);

            if (degrees < p)
            {
                throw new ArgumentException(
                    $"degreesOfFreedom ({degrees}) must be at least the matrix size ({p}).", "degreesOfFreedom");
            }

            if (!new CholeskyDecomposition(s).IsPositiveDefinite)
            {
                throw new ArgumentException("scale must be positive definite.", "scale");
            }

            return new WishartDistribution(degrees, s);
        }

        public static MatrixDistributionSummary WishartSummaryOf(int degrees, double[][] scale)
        {
            var d = Wishart(degrees, scale);

            return new MatrixDistributionSummary("Wishart", degrees, ToJagged(d.Mean), ToJagged(d.Covariance));
        }

        public static MatrixDensityResult WishartDensityOf(int degrees, double[][] scale, double[][] matrix)
        {
            var d = Wishart(degrees, scale);
            var x = Square(matrix, "matrix", true);

            if (x.GetLength(0) != scale.Length)
            {
                throw new ArgumentException($"matrix must be {scale.Length} by {scale.Length} like scale.", "matrix");
            }

            if (!new CholeskyDecomposition(x).IsPositiveDefinite)
            {
                return new MatrixDensityResult("Wishart", 0.0, null);
            }

            return new MatrixDensityResult("Wishart", DistributionGuard.N(d.ProbabilityDensityFunction(x)),
                DistributionGuard.N(d.LogProbabilityDensityFunction(x)));
        }

        public static MatrixSampleResult SampleWishart(int degrees, double[][] scale, int count, int seed)
        {
            var d = Wishart(degrees, scale);
            var samples = d.Generate(count, new Random(seed));
            var p = scale.Length;
            var mean = new double[p][];

            for (var i = 0; i < p; i++)
            {
                mean[i] = new double[p];

                for (var j = 0; j < p; j++)
                {
                    mean[i][j] = samples.Sum(m => m[i, j]) / count;
                }
            }

            return new MatrixSampleResult("Wishart", seed, samples.Select(ToJagged).ToArray(), mean);
        }

        public static InverseWishartDistribution InverseWishart(double degrees, double[][] inverseScale)
        {
            var s = Square(inverseScale, "inverseScale", true);
            var p = s.GetLength(0);

            if (degrees <= p + 1)
            {
                throw new ArgumentException(
                    $"degreesOfFreedom ({degrees}) must exceed the matrix size plus 1 ({p + 1}) so the mean exists.",
                    "degreesOfFreedom");
            }

            if (!new CholeskyDecomposition(s).IsPositiveDefinite)
            {
                throw new ArgumentException("inverseScale must be positive definite.", "inverseScale");
            }

            return new InverseWishartDistribution(degrees, s);
        }

        public static MatrixDistributionSummary InverseWishartSummaryOf(double degrees, double[][] inverseScale)
        {
            var d = InverseWishart(degrees, inverseScale);

            return new MatrixDistributionSummary("InverseWishart", degrees, ToJagged(d.Mean), ToJagged(d.Covariance));
        }

        public static MatrixDensityResult InverseWishartDensityOf(double degrees, double[][] inverseScale,
            double[][] matrix)
        {
            var d = InverseWishart(degrees, inverseScale);
            var x = Square(matrix, "matrix", true);

            if (x.GetLength(0) != inverseScale.Length)
            {
                throw new ArgumentException(
                    $"matrix must be {inverseScale.Length} by {inverseScale.Length} like inverseScale.",
                    "matrix");
            }

            if (!new CholeskyDecomposition(x).IsPositiveDefinite)
            {
                return new MatrixDensityResult("InverseWishart", 0.0, null);
            }

            return new MatrixDensityResult("InverseWishart", DistributionGuard.N(d.ProbabilityDensityFunction(x)),
                DistributionGuard.N(d.LogProbabilityDensityFunction(x)));
        }

        public static double[] UnitDirection(double[] direction)
        {
            DistributionGuard.Values(direction, "meanDirection", 2);
            var norm = Math.Sqrt(direction.Sum(v => v * v));

            if (!(norm > 0))
            {
                throw new ArgumentException("meanDirection must not be the zero vector.", "meanDirection");
            }

            return direction.Select(v => v / norm).ToArray();
        }

        public static DirectionalEvaluation VonMisesFisherDensity(double[] direction, double concentration,
            double[][] points)
        {
            var mu = UnitDirection(direction);

            if (!(concentration >= 0) || !double.IsFinite(concentration))
            {
                throw new ArgumentException($"concentration must be 0 or more, got {concentration}.", "concentration");
            }

            if (mu.Length % 2 != 0)
            {
                throw new ArgumentException(
                    $"The density is only supported for an even number of dimensions (2, 4, 6 and so on); meanDirection has {mu.Length}. Use VonMisesFisherRandomSample for other dimensions.",
                    "meanDirection");
            }

            Rows(points, "points", 1, mu.Length);
            var d = new VonMisesFisherDistribution(mu, concentration);
            var values = points.Select(p => d.ProbabilityDensityFunction(p)).ToArray();

            return new DirectionalEvaluation(mu, concentration, DistributionGuard.N(values));
        }

        public static MultivariateSampleResult SampleVonMisesFisher(double[] direction, double concentration, int count,
            int seed)
        {
            var mu = UnitDirection(direction);

            if (mu.Length < 3)
            {
                throw new ArgumentException(
                    "Sampling needs at least 3 dimensions. For angles on a circle use DistributionRandomSample with VonMises.",
                    "meanDirection");
            }

            if (!(concentration >= 0) || !double.IsFinite(concentration))
            {
                throw new ArgumentException($"concentration must be 0 or more, got {concentration}.", "concentration");
            }

            var d = new VonMisesFisherDistribution(mu, concentration);
            var rows = d.Generate(count, new Random(seed));

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }

        public static MultivariateSampleResult SampleSphere(double[] center, double radius, int count, int seed)
        {
            DistributionGuard.Values(center, "center", 2);

            if (!(radius > 0) || !double.IsFinite(radius))
            {
                throw new ArgumentException($"radius must be greater than 0, got {radius}.", "radius");
            }

            var rows = UniformSphereDistribution.Random(count, center, radius,
                Enumerable.Range(0, count).Select(_ => new double[center.Length]).ToArray(), new Random(seed));

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }

        public static MultivariateSampleResult SampleBall(double[] center, double radius, int count, int seed)
        {
            DistributionGuard.Values(center, "center", 2);

            if (!(radius > 0) || !double.IsFinite(radius))
            {
                throw new ArgumentException($"radius must be greater than 0, got {radius}.", "radius");
            }

            var d = new UniformBallDistribution(center, radius);
            var rows = d.Generate(count, new Random(seed));

            return new MultivariateSampleResult(seed, rows, MeanOf(rows));
        }
    }
}
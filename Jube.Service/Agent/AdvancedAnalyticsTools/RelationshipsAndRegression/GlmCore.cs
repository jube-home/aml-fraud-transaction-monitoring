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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class GlmCore
    {
        private const double Epsilon = 1e-10;

        public static double Variance(GlmFamily family, double mu)
        {
            return family switch
            {
                GlmFamily.Gaussian => 1,
                GlmFamily.Poisson => mu,
                GlmFamily.Binomial => mu * (1 - mu),
                GlmFamily.Gamma => mu * mu,
                _ => mu * mu * mu
            };
        }

        public static double UnitDeviance(GlmFamily family, double y, double mu)
        {
            switch (family)
            {
                case GlmFamily.Gaussian:
                    return (y - mu) * (y - mu);
                case GlmFamily.Poisson:
                    return 2 * ((y > 0 ? y * Math.Log(y / mu) : 0) - (y - mu));
                case GlmFamily.Binomial:
                    return 2 * ((y > 0 ? y * Math.Log(y / mu) : 0) +
                                (y < 1 ? (1 - y) * Math.Log((1 - y) / (1 - mu)) : 0));
                case GlmFamily.Gamma:
                    return 2 * (-Math.Log(y / mu) + (y - mu) / mu);
                default:
                    return (y - mu) * (y - mu) / (y * mu * mu);
            }
        }

        public static bool ValidMean(GlmFamily family, double mu)
        {
            if (double.IsNaN(mu) || double.IsInfinity(mu))
            {
                return false;
            }

            return family switch
            {
                GlmFamily.Gaussian => true,
                GlmFamily.Binomial => mu > 0 && mu < 1,
                _ => mu > 0
            };
        }

        public static void ValidateOutcome(GlmFamily family, double[] y, string outputsName)
        {
            for (var i = 0; i < y.Length; i++)
            {
                switch (family)
                {
                    case GlmFamily.Poisson when y[i] < 0:
                        throw new ArgumentException(
                            $"outputs[{i}] is {y[i]}; Poisson counts must be zero or more.", outputsName);
                    case GlmFamily.Binomial when y[i] != 0 && y[i] != 1:
                        throw new ArgumentException(
                            $"{outputsName}[{i}] is {y[i]}; the Binomial family here needs 0/1 outcomes. Expand grouped counts into one row per case.",
                            outputsName);
                    case GlmFamily.Gamma when !(y[i] > 0):
                    case GlmFamily.InverseGaussian when !(y[i] > 0):
                        throw new ArgumentException(
                            $"{outputsName}[{i}] is {y[i]}; the {family} family needs strictly positive outcomes.",
                            outputsName);
                }
            }
        }

        public static GlmSolution Fit(double[][] x, double[] y, string[] names, double[]? priorWeights,
            double[]? offset,
            GlmFamily family, GlmLink link, bool estimateDispersion, int maximumIterations, bool computeNull = true,
            string outputsName = "outputs")
        {
            if (!Enum.IsDefined(typeof(GlmFamily), family))
            {
                throw new ArgumentException(
                    "Unknown family. Use Gaussian, Poisson, Binomial, Gamma or InverseGaussian.",
                    nameof(family));
            }

            ValidateOutcome(family, y, outputsName);
            var linkFunction = GlmLinkFunction.For(link);
            var n = y.Length;
            var p = x[0].Length;
            var k = p + 1;

            if (n <= k)
            {
                throw new ArgumentException(
                    $"There are {n} usable row(s) for {k} coefficients. Supply more rows than coefficients.", "inputs");
            }

            var prior = priorWeights ?? Enumerable.Repeat(1d, n).ToArray();
            var off = offset ?? new double[n];
            var totalPrior = prior.Sum();
            var warnings = new List<string>();
            var mu = new double[n];
            var eta = new double[n];

            for (var i = 0; i < n; i++)
            {
                mu[i] = family switch
                {
                    GlmFamily.Poisson => y[i] + 0.1,
                    GlmFamily.Binomial => (y[i] + 0.5) / 2,
                    _ => y[i]
                };

                if (family == GlmFamily.Gaussian && link == GlmLink.Log)
                {
                    mu[i] = Math.Max(y[i], Epsilon);
                }

                eta[i] = linkFunction.Link(mu[i]);

                if (double.IsNaN(eta[i]) || double.IsInfinity(eta[i]))
                {
                    throw new ArgumentException(
                        $"The {link} link cannot be applied to the starting mean {mu[i]:0.####} for row {i}. Use a link that suits the family and the outcome range.",
                        nameof(link));
                }
            }

            var beta = new double[k];
            var deviance = Deviance(family, y, mu, prior);
            var converged = false;
            int iteration;

            for (iteration = 1; iteration <= maximumIterations; iteration++)
            {
                var z = new double[n];
                var w = new double[n];
                var broke = false;

                for (var i = 0; i < n; i++)
                {
                    var d = linkFunction.Derivative(eta[i]);
                    var v = Variance(family, mu[i]);

                    if (family == GlmFamily.Binomial)
                    {
                        d = Math.Sign(d) * Math.Max(Math.Abs(d), 1e-12);
                        v = Math.Max(v, 1e-12);
                    }

                    if (!(Math.Abs(d) > 0) || !(v > 0) || double.IsNaN(d) || double.IsNaN(v))
                    {
                        if (iteration > 1)
                        {
                            broke = true;
                            break;
                        }

                        throw new ArgumentException(
                            $"The iteration broke down at row {i} (zero or undefined derivative or variance). Try another link, or check for perfectly separated or extreme data.",
                            nameof(link));
                    }

                    w[i] = prior[i] * d * d / v;
                    z[i] = eta[i] - off[i] + (y[i] - mu[i]) / d;
                }

                if (broke)
                {
                    warnings.Add(
                        "The iteration stopped because the fit became numerically degenerate (perfect separation is likely).");

                    break;
                }

                LinearFit fit;

                try
                {
                    fit = LinearFit.Fit(x, z, w, names, true);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(
                        "The weighted least squares step failed: " + e.Message, "inputs");
                }

                var proposal = fit.Beta;
                var newEta = Eta(x, proposal, off);
                var newMu = newEta.Select(e => Clamp(family, linkFunction.Inverse(e))).ToArray();
                var halvings = 0;

                while (newMu.Any(m => !ValidMean(family, m)) && halvings < 30)
                {
                    proposal = proposal.Select((b, j) => (b + beta[j]) / 2).ToArray();
                    newEta = Eta(x, proposal, off);
                    newMu = newEta.Select(e => Clamp(family, linkFunction.Inverse(e))).ToArray();
                    halvings++;
                }

                if (newMu.Any(m => !ValidMean(family, m)))
                {
                    throw new ArgumentException(
                        $"The fitted means left the valid range for the {family} family with the {link} link. Choose a link that keeps the mean in range (for example Log for counts and positive outcomes).",
                        nameof(link));
                }

                var newDeviance = Deviance(family, y, newMu, prior);
                beta = proposal;
                eta = newEta;
                mu = newMu;
                var change = Math.Abs(newDeviance - deviance) / (Math.Abs(newDeviance) + 0.1);
                deviance = newDeviance;

                if (change < 1e-10 && iteration > 1)
                {
                    converged = true;
                    break;
                }

                if (change < 1e-10 && deviance == 0)
                {
                    converged = true;
                    break;
                }
            }

            if (!converged)
            {
                warnings.Add(
                    "The fitting did not converge within maximumIterations. The estimates may be unreliable, or the outcome may be perfectly predictable.");

                iteration = Math.Min(iteration, maximumIterations);
            }

            var finalZ = new double[n];
            var finalW = new double[n];

            for (var i = 0; i < n; i++)
            {
                var d = linkFunction.Derivative(eta[i]);
                var v = Variance(family, mu[i]);

                if (family == GlmFamily.Binomial)
                {
                    d = Math.Sign(d) * Math.Max(Math.Abs(d), 1e-12);
                    v = Math.Max(v, 1e-12);
                }

                finalW[i] = prior[i] * d * d / v;
                finalZ[i] = eta[i] - off[i] + (y[i] - mu[i]) / d;
            }

            var final = LinearFit.Fit(x, finalZ, finalW, names, true);
            var pearson = 0d;

            for (var i = 0; i < n; i++)
            {
                pearson += prior[i] * (y[i] - mu[i]) * (y[i] - mu[i]) / Variance(family, mu[i]);
            }

            var fixedDispersion = family == GlmFamily.Poisson || family == GlmFamily.Binomial;
            var estimated = !fixedDispersion || estimateDispersion;
            var dispersion = estimated ? pearson / (n - k) : 1d;
            var covariance = final.XtXInverse.Select(r => r.Select(v => v * dispersion).ToArray()).ToArray();

            var nullDeviance = computeNull
                ? NullDeviance(family, y, prior, off, link, maximumIterations, n)
                : double.NaN;

            if (family == GlmFamily.Binomial && mu.Any(m => m < 1e-8 || m > 1 - 1e-8))
            {
                warnings.Add(
                    "Some fitted probabilities are numerically 0 or 1, which usually means the classes are (nearly) perfectly separated by the predictors; coefficients and standard errors are then unreliable. Consider ridge-penalised TrainLogisticRegression.");
            }

            if (family == GlmFamily.Poisson && y.Any(v => Math.Abs(v - Math.Round(v)) > 1e-9))
            {
                warnings.Add(
                    "Some Poisson outcomes are not whole numbers; the likelihood is then only a quasi-likelihood.");
            }

            if (fixedDispersion && !estimateDispersion && pearson / (n - k) > 1.5)
            {
                warnings.Add(
                    $"The Pearson dispersion is {pearson / (n - k):0.##}, well above 1: the data are overdispersed relative to the {family} model, so standard errors are too small. Refit with estimateDispersion true (quasi-likelihood).");
            }

            var devianceResiduals = new double[n];

            for (var i = 0; i < n; i++)
            {
                devianceResiduals[i] = Math.Sign(y[i] - mu[i]) *
                                       Math.Sqrt(Math.Max(0, prior[i] * UnitDeviance(family, y[i], mu[i])));
            }

            return new GlmSolution
            {
                Beta = final.Beta,
                Covariance = covariance,
                Mean = mu,
                Eta = eta,
                Deviance = deviance,
                NullDeviance = nullDeviance,
                Pearson = pearson,
                Dispersion = dispersion,
                DispersionEstimated = estimated,
                Iterations = iteration,
                Converged = converged,
                N = n,
                K = k,
                LogLikelihood = LogLikelihood(family, y, mu, prior, deviance, totalPrior),
                DevianceResiduals = devianceResiduals,
                Terms = final.Terms,
                Warnings = warnings
            };
        }

        private static double Clamp(GlmFamily family, double mu)
        {
            return family == GlmFamily.Binomial && !double.IsNaN(mu)
                ? Math.Min(Math.Max(mu, 2.220446049250313e-16), 1 - 2.220446049250313e-16)
                : mu;
        }

        private static double[] Eta(double[][] x, double[] beta, double[] offset)
        {
            var eta = new double[x.Length];

            for (var i = 0; i < x.Length; i++)
            {
                var sum = beta[0] + offset[i];

                for (var j = 0; j < x[i].Length; j++)
                {
                    sum += beta[j + 1] * x[i][j];
                }

                eta[i] = sum;
            }

            return eta;
        }

        private static double Deviance(GlmFamily family, double[] y, double[] mu, double[] prior)
        {
            var sum = 0d;

            for (var i = 0; i < y.Length; i++)
            {
                sum += prior[i] * UnitDeviance(family, y[i], mu[i]);
            }

            return sum;
        }

        private static double NullDeviance(GlmFamily family, double[] y, double[] prior, double[] offset,
            GlmLink link, int maximumIterations, int n)
        {
            var hasOffset = offset.Any(o => o != 0);

            if (!hasOffset)
            {
                var mean = y.Select((v, i) => v * prior[i]).Sum() / prior.Sum();

                if (!ValidMean(family, mean))
                {
                    return double.NaN;
                }

                return prior.Select((w, i) => w * UnitDeviance(family, y[i], mean)).Sum();
            }

            try
            {
                var empty = Enumerable.Range(0, n).Select(_ => Array.Empty<double>()).ToArray();

                var solution = Fit(empty, y, Array.Empty<string>(), prior, offset, family, link, false,
                    maximumIterations,
                    false);

                return solution.Deviance;
            }
            catch (ArgumentException)
            {
                return double.NaN;
            }
        }

        private static double LogLikelihood(GlmFamily family, double[] y, double[] mu, double[] prior, double deviance,
            double totalPrior)
        {
            var n = y.Length;
            var phi = deviance / totalPrior;
            var sum = 0d;

            switch (family)
            {
                case GlmFamily.Gaussian:
                    return deviance > 0
                        ? -totalPrior / 2 * (Math.Log(2 * Math.PI * phi) + 1) + 0.5 * prior.Sum(Math.Log)
                        : double.NaN;
                case GlmFamily.Poisson:
                    for (var i = 0; i < n; i++)
                    {
                        sum += prior[i] * (y[i] * Math.Log(mu[i]) - mu[i] - Special.LogFactorial(y[i]));
                    }

                    return sum;
                case GlmFamily.Binomial:
                    for (var i = 0; i < n; i++)
                    {
                        sum += prior[i] * (y[i] * Math.Log(mu[i]) + (1 - y[i]) * Math.Log(1 - mu[i]));
                    }

                    return sum;
                case GlmFamily.Gamma:
                {
                    if (!(phi > 0))
                    {
                        return double.NaN;
                    }

                    var nu = 1 / phi;

                    for (var i = 0; i < n; i++)
                    {
                        sum += prior[i] * (nu * Math.Log(nu * y[i] / mu[i]) - nu * y[i] / mu[i] - Math.Log(y[i]) -
                                           Gamma.Log(nu));
                    }

                    return sum;
                }
                default:
                {
                    if (!(phi > 0))
                    {
                        return double.NaN;
                    }

                    for (var i = 0; i < n; i++)
                    {
                        sum += -0.5 * prior[i] * (Math.Log(2 * Math.PI * phi * y[i] * y[i] * y[i]) +
                                                  (y[i] - mu[i]) * (y[i] - mu[i]) / (phi * y[i] * mu[i] * mu[i]));
                    }

                    return sum;
                }
            }
        }

        public static GeneralisedLinearModelResult Report(GlmSolution s, GlmFamily family, GlmLink link, double level,
            bool includeSeries, int dropped)
        {
            var fixedDispersion = !s.DispersionEstimated;
            var dfResidual = s.N - s.K;

            var quantile = fixedDispersion
                ? RegressionStats.NormalQuantile(0.5 + level / 2)
                : RegressionStats.TQuantile(0.5 + level / 2, dfResidual);

            var rows = new GlmCoefficientRow[s.K];

            for (var j = 0; j < s.K; j++)
            {
                var se = Math.Sqrt(Math.Max(0, s.Covariance[j][j]));
                var stat = s.Beta[j] / se;
                var lower = s.Beta[j] - quantile * se;
                var upper = s.Beta[j] + quantile * se;

                rows[j] = new GlmCoefficientRow
                {
                    Term = s.Terms[j],
                    Estimate = s.Beta[j],
                    StandardError = se,
                    Statistic = stat,
                    PValue = fixedDispersion
                        ? RegressionStats.NormalTwoSided(stat)
                        : RegressionStats.TwoSidedTp(stat, dfResidual),
                    ConfidenceLower = lower,
                    ConfidenceUpper = upper,
                    ExponentiatedEstimate = Math.Exp(s.Beta[j]),
                    ExponentiatedLower = Math.Exp(lower),
                    ExponentiatedUpper = Math.Exp(upper)
                };
            }

            var dispersionParameters = fixedDispersion ? 0 : 1;
            var parameters = s.K + dispersionParameters;
            var aic = -2 * s.LogLikelihood + 2 * parameters;
            var bic = -2 * s.LogLikelihood + parameters * Math.Log(s.N);
            var lr = (s.NullDeviance - s.Deviance) / s.Dispersion;

            return new GeneralisedLinearModelResult
            {
                Family = family.ToString(),
                Link = link.ToString(),
                Coefficients = rows,
                Converged = s.Converged,
                Iterations = s.Iterations,
                Deviance = s.Deviance,
                NullDeviance = s.NullDeviance,
                PseudoRSquared = s.NullDeviance > 0 ? 1 - s.Deviance / s.NullDeviance : double.NaN,
                ResidualDegreesOfFreedom = dfResidual,
                NullDegreesOfFreedom = s.N - 1,
                Dispersion = s.Dispersion,
                PearsonChiSquare = s.Pearson,
                LogLikelihood = s.LogLikelihood,
                Aic = aic,
                Bic = bic,
                LikelihoodRatioStatistic = lr,
                LikelihoodRatioPValue = RegressionStats.ChiSquareUpperTail(lr, s.K - 1),
                Observations = s.N,
                RowsDropped = dropped,
                FittedMeans = includeSeries ? s.Mean : null,
                DevianceResiduals = includeSeries ? s.DevianceResiduals : null,
                Warnings = (dropped > 0
                    ? s.Warnings.Append($"{dropped} row(s) with missing values were dropped before fitting.")
                    : s.Warnings).ToArray()
            };
        }
    }
}
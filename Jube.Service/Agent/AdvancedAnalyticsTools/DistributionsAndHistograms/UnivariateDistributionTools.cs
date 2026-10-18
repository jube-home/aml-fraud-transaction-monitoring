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

using System.ComponentModel;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    [AdvancedAnalyticsToolGroup("DistributionsAndHistograms")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class UnivariateDistributionTools
    {
        private const string ParametersText =
            "The distribution parameters as numbers, in the order ListDistributions gives for that distribution (for example Normal takes mean then standardDeviation). Use an empty array for distributions without parameters.";

        private const string DistributionText =
            "Which distribution to use. Call ListDistributions to see every choice, its parameters and its support.";

        private static readonly double[] summaryProbabilities =
            { 0.001, 0.01, 0.05, 0.25, 0.5, 0.75, 0.95, 0.99, 0.999 };

        [AdvancedAnalyticsTool("ListDistributions")]
        [Description(
            "Lists every univariate distribution the other distribution tools understand, both continuous and discrete. " +
            "For each it returns the name to pass as the distribution argument, whether it is discrete, a plain language description, the support (which values are possible), typical uses, " +
            "the ordered parameter names with a description of each, an example parameter array, and whether it can be fitted to data with FitDistribution and by which method. " +
            "Call this first when you are unsure which distribution suits a problem or what order the parameters go in.")]
        public DistributionInfo[] ListDistributions()
        {
            return DistributionCatalogue.All.Select(e => new DistributionInfo(
                e.Name,
                e.Discrete,
                e.Description,
                e.Support,
                e.Uses,
                e.Variadic ? new[] { "probabilities" } : e.Parameters.Select(p => p.Name).ToArray(),
                e.Variadic ? new[] { e.Parameters[0].Description } : e.Parameters.Select(p => p.Description).ToArray(),
                e.Examples,
                e.Fit != FitMode.None,
                e.Fit switch
                {
                    FitMode.Library =>
                        "Estimated directly from the data (maximum likelihood or moments, whichever suits the distribution).",
                    FitMode.Numeric => "Estimated by numerical maximum likelihood search" +
                                       (e.FitNote.Length > 0 ? ". " + e.FitNote : "."),
                    _ => "Not fittable: supply the parameters directly."
                })).ToArray();
        }

        [AdvancedAnalyticsTool("DescribeDistribution")]
        [Description(
            "Summarises a distribution with fixed parameters: mean, variance, standard deviation, median, mode, entropy (in nats), the lower and upper end of the support, and the values at the 0.1, 1, 5, 25, 50, 75, 95, 99 and 99.9 percent points. " +
            "Use it to understand what a candidate or fitted distribution implies. " +
            "A field is null when it does not exist or is infinite for that distribution (for example the Cauchy has no mean, and unbounded supports have null limits).")]
        public DistributionSummary DescribeDistribution(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            var variance = model.Variance;

            return new DistributionSummary(
                model.Entry.Name,
                model.Discrete,
                ParameterLabels(model),
                model.Parameters,
                DistributionGuard.N(model.Mean),
                DistributionGuard.N(variance),
                double.IsFinite(variance) && variance >= 0 ? Math.Sqrt(variance) : null,
                DistributionGuard.N(model.Median),
                DistributionGuard.N(model.Mode),
                DistributionGuard.N(model.Entropy),
                DistributionGuard.N(model.SupportLower),
                DistributionGuard.N(model.SupportUpper),
                summaryProbabilities,
                DistributionGuard.N(summaryProbabilities.Select(model.Quantile).ToArray()));
        }

        [AdvancedAnalyticsTool("DistributionDensity")]
        [Description(
            "Evaluates the probability density (continuous distributions) or the probability mass (discrete distributions) at each of the given points. " +
            "For a continuous distribution the density is not a probability: it can exceed 1 and only areas under it are probabilities. " +
            "For a discrete distribution the result is the exact probability of that whole number, and a non-integer point has probability 0. " +
            "A value is null where the density is undefined or infinite.")]
        public DistributionEvaluation DistributionDensity(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description(
                "The points at which to evaluate, any real numbers; for a discrete distribution give whole numbers.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, model.Discrete ? "ProbabilityMass" : "ProbabilityDensity", x, model.Pdf);
        }

        [AdvancedAnalyticsTool("DistributionLogDensity")]
        [Description(
            "Evaluates the natural logarithm of the probability density (or probability mass for discrete distributions) at each point. " +
            "Use this instead of DistributionDensity when densities are tiny and would round to zero, or when you want to add log likelihoods yourself. " +
            "A value of negative infinity (returned as null) means the point is impossible under this distribution.")]
        public DistributionEvaluation DistributionLogDensity(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description(
                "The points at which to evaluate, any real numbers; for a discrete distribution give whole numbers.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, "LogDensity", x, model.LogPdf);
        }

        [AdvancedAnalyticsTool("DistributionCumulativeProbability")]
        [Description(
            "Evaluates the cumulative distribution function: for each point x, the probability that a value drawn from the distribution is less than or equal to x. " +
            "The result rises from 0 to 1. Use it to turn a raw value into a percentile under a distribution. " +
            "For a discrete distribution x is rounded down to a whole number first.")]
        public DistributionEvaluation DistributionCumulativeProbability(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The points at which to evaluate, any real numbers including infinities.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, "CumulativeProbability", x, model.Cdf);
        }

        [AdvancedAnalyticsTool("DistributionUpperTailProbability")]
        [Description(
            "Evaluates the complementary cumulative function (also called the survival function): for each point x, the probability that a value drawn from the distribution is strictly greater than x. " +
            "It is computed directly, so it stays accurate far out in the tail where 1 minus the cumulative probability would round to zero. " +
            "Use it for p values, exceedance probabilities and how rare a large value is.")]
        public DistributionEvaluation DistributionUpperTailProbability(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The points at which to evaluate, any real numbers including infinities.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, "UpperTailProbability", x, model.Ccdf);
        }

        [AdvancedAnalyticsTool("DistributionQuantile")]
        [Description(
            "Evaluates the quantile function (inverse cumulative distribution function): for each probability p, the value below which a fraction p of the distribution lies. " +
            "Use it to find thresholds such as the 99th percentile or critical values. " +
            "Probabilities must be between 0 and 1; 0 and 1 give the ends of the support (null when unbounded). For a discrete distribution the result is the smallest whole number whose cumulative probability reaches p.")]
        public DistributionEvaluation DistributionQuantile(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description(
                "Probabilities between 0 and 1 inclusive, for example 0.5 for the median and 0.99 for the 99th percentile.")]
            double[] probabilities)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Probabilities(probabilities, nameof(probabilities));

            return Evaluate(model, "Quantile", probabilities, model.Quantile);
        }

        [AdvancedAnalyticsTool("DistributionHazardRate")]
        [Description(
            "Evaluates the hazard function: the instantaneous rate of failure at x given survival up to x, which is the density divided by the upper tail probability. " +
            "Use it to see whether risk rises, falls or stays constant over time under a lifetime distribution (Weibull, Exponential, Gamma and similar). " +
            "A value is null where undefined or infinite, for example beyond the end of the support.")]
        public DistributionEvaluation DistributionHazardRate(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The points (for example times) at which to evaluate the hazard.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, "HazardRate", x, model.Hazard);
        }

        [AdvancedAnalyticsTool("DistributionCumulativeHazard")]
        [Description(
            "Evaluates the cumulative hazard function H(x), the accumulated risk up to x, which equals the negative natural logarithm of the upper tail probability. " +
            "Use it with lifetime models: the expected number of events by time x under a repairable process, or to compare survival curves on a common scale. " +
            "A value is null where undefined or infinite.")]
        public DistributionEvaluation DistributionCumulativeHazard(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The points (for example times) at which to evaluate the cumulative hazard.")]
            double[] x)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(x, nameof(x));

            return Evaluate(model, "CumulativeHazard", x, model.CumulativeHazard);
        }

        [AdvancedAnalyticsTool("DistributionIntervalProbability")]
        [Description(
            "Returns the probability that a value falls within each interval [lower, upper] under the distribution. " +
            "The lower and upper arrays are paired by position and must be the same length. Use infinities to leave an end open. " +
            "For a discrete distribution both ends are included, so the interval 3 to 5 covers the whole numbers 3, 4 and 5; for a continuous distribution the end points make no difference.")]
        public DistributionEvaluation DistributionIntervalProbability(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("Lower end of each interval; may be negative infinity.")]
            double[] lower,
            [Description("Upper end of each interval, same length as lower; may be positive infinity.")]
            double[] upper)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Points(lower, nameof(lower));
            DistributionGuard.Points(upper, nameof(upper));

            if (lower.Length != upper.Length)
            {
                throw new ArgumentException(
                    $"lower has {lower.Length} entries but upper has {upper.Length}; supply one lower and one upper end per interval.",
                    nameof(upper));
            }

            var values = new double?[lower.Length];

            for (var i = 0; i < lower.Length; i++)
            {
                if (lower[i] > upper[i])
                {
                    throw new ArgumentException($"lower[{i}] = {lower[i]} is above upper[{i}] = {upper[i]}.",
                        nameof(lower));
                }

                values[i] = DistributionGuard.N(model.Between(lower[i], upper[i]));
            }

            return new DistributionEvaluation(model.Entry.Name, "IntervalProbability", model.Parameters, lower, values);
        }

        [AdvancedAnalyticsTool("DistributionCurve")]
        [Description(
            "Generates evenly spaced x values across the useful range of a distribution and returns the density, cumulative probability and upper tail probability at each, ready to plot or to compare with a histogram. " +
            "The range runs from the lowerProbability quantile to the upperProbability quantile, so heavy tails are trimmed. " +
            "Density values are null where undefined or infinite.")]
        public DistributionCurveResult DistributionCurve(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("Number of x values to generate, between 2 and 5000. Default 101.")]
            int pointCount = 101,
            [Description("Quantile at which the curve starts, strictly between 0 and upperProbability. Default 0.001.")]
            double lowerProbability = 0.001,
            [Description("Quantile at which the curve ends, strictly between lowerProbability and 1. Default 0.999.")]
            double upperProbability = 0.999)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.PositiveCount(pointCount, nameof(pointCount), 5000);

            if (pointCount < 2)
            {
                throw new ArgumentException("pointCount must be at least 2 to draw a curve.", nameof(pointCount));
            }

            DistributionGuard.Probability(lowerProbability, nameof(lowerProbability), false);
            DistributionGuard.Probability(upperProbability, nameof(upperProbability), false);

            if (lowerProbability >= upperProbability)
            {
                throw new ArgumentException("lowerProbability must be below upperProbability.",
                    nameof(lowerProbability));
            }

            var lo = model.Quantile(lowerProbability);
            var hi = model.Quantile(upperProbability);

            if (!double.IsFinite(lo) || !double.IsFinite(hi) || hi <= lo)
            {
                lo = double.IsFinite(model.SupportLower) ? model.SupportLower : -1;
                hi = double.IsFinite(model.SupportUpper) ? model.SupportUpper : lo + 1;

                if (hi <= lo)
                {
                    hi = lo + 1;
                }
            }

            var x = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                x[i] = lo + (hi - lo) * i / (pointCount - 1);

                if (model.Discrete)
                {
                    x[i] = Math.Round(x[i]);
                }
            }

            var density = x.Select(model.Pdf).ToArray();
            var cumulative = x.Select(model.Cdf).ToArray();
            var survival = x.Select(model.Ccdf).ToArray();

            return new DistributionCurveResult(
                model.Entry.Name,
                model.Parameters,
                x,
                DistributionGuard.N(density),
                DistributionGuard.N(cumulative),
                DistributionGuard.N(survival));
        }

        [AdvancedAnalyticsTool("DistributionProbabilityTable")]
        [Description(
            "For a discrete distribution, lists the probability of each whole number from first to last, with running cumulative probabilities and the total probability covered. " +
            "Use it to read off exact chances such as the probability of exactly 3 events, or to see how much probability lies outside a range (one minus totalProbability). " +
            "Only discrete distributions are accepted; use DistributionCurve for continuous ones.")]
        public DistributionMassTable DistributionProbabilityTable(
            [Description(
                "Which discrete distribution to use, for example Poisson, Binomial or Geometric. Call ListDistributions for the choices.")]
            DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("Smallest whole number to list.")]
            int first,
            [Description("Largest whole number to list; at most 10000 values from first.")]
            int last)
        {
            var model = UnivariateModel.Create(distribution, parameters);

            if (!model.Discrete)
            {
                throw new ArgumentException(
                    $"{model.Entry.Name} is continuous, so it has no probability table. Use DistributionCurve or DistributionDensity instead.",
                    nameof(distribution));
            }

            if (last < first || (long)last - first >= 10000)
            {
                throw new ArgumentException("last must be at least first and no more than 9999 above it.",
                    nameof(last));
            }

            var n = last - first + 1;
            var values = new int[n];
            var probability = new double[n];
            var cumulative = new double[n];

            for (var i = 0; i < n; i++)
            {
                values[i] = first + i;
                probability[i] = model.Pdf(values[i]);
                cumulative[i] = model.Cdf(values[i]);
            }

            return new DistributionMassTable(model.Entry.Name, model.Parameters, values, probability, cumulative,
                probability.Where(double.IsFinite).Sum());
        }

        [AdvancedAnalyticsTool("DistributionRandomSample")]
        [Description(
            "Draws random values from a distribution with fixed parameters. The same seed always gives the same values, so results are reproducible. " +
            "Use it to simulate, to bootstrap a null distribution or to compare real data with what the model would generate. " +
            "Returns the values plus their sample mean and sample standard deviation as a quick sanity check.")]
        public DistributionSampleResult DistributionRandomSample(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("How many values to draw, between 1 and 100000.")]
            int count,
            [Description("Seed of the random number generator; reuse it to reproduce the same sample. Default 1.")]
            int seed = 1)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.PositiveCount(count, nameof(count));
            double[] values;

            try
            {
                values = model.Generate(count, new Random(seed));
            }
            catch (Exception ex) when (ex is NotSupportedException or NotImplementedException
                                           or InvalidOperationException)
            {
                throw new ArgumentException(
                    $"Random sampling is not available for {model.Entry.Name}. Sample another distribution or use DistributionQuantile on uniform probabilities.",
                    nameof(distribution));
            }

            var mean = values.Average();

            double? sd = values.Length > 1
                ? Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1))
                : null;

            return new DistributionSampleResult(model.Entry.Name, model.Parameters, seed, values, mean,
                sd.HasValue && double.IsFinite(sd.Value) ? sd : null);
        }

        [AdvancedAnalyticsTool("DistributionLogLikelihood")]
        [Description(
            "Computes how well a distribution with fixed, already chosen parameters explains a set of observations: the total log-likelihood (higher is better), the mean log-likelihood per observation, AIC and BIC (lower is better) and how many observations were impossible under the distribution. " +
            "The parameters are NOT estimated here; use FitDistribution to estimate them. " +
            "Use it to compare a proposed model with a fitted one or to score new data against a reference model. " +
            "If any observation has zero probability the log-likelihood is null and zeroLikelihoodCount says how many.")]
        public DistributionLogLikelihoodResult DistributionLogLikelihood(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The observations to score, at least one, no missing values.")]
            double[] values)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Values(values, nameof(values));

            var zero = 0;
            var sum = 0.0;

            foreach (var v in values)
            {
                var l = model.LogPdf(v);

                if (double.IsNaN(l) || double.IsNegativeInfinity(l))
                {
                    zero++;
                }
                else
                {
                    sum += l;
                }
            }

            var k = model.Parameters.Length;
            var n = values.Length;
            var valid = zero == 0;

            return new DistributionLogLikelihoodResult(
                model.Entry.Name,
                model.Parameters,
                n,
                valid ? sum : null,
                valid ? sum / n : null,
                k,
                valid ? 2 * k - 2 * sum : null,
                valid ? k * Math.Log(n) - 2 * sum : null,
                zero);
        }

        [AdvancedAnalyticsTool("DistributionTailScores")]
        [Description(
            "Scores each value by how extreme it is under a distribution with fixed parameters. " +
            "For every value it returns the lower tail probability (probability of a value this small or smaller), the upper tail probability (this large or larger), a two sided tail probability (twice the smaller tail, capped at 1) and a surprise score, which is minus log base 10 of the two sided probability, so 3 means about one in a thousand. " +
            "Use it to flag anomalies against a fitted or assumed reference distribution.")]
        public DistributionTailScoreResult DistributionTailScores(
            [Description(DistributionText)] DistributionKind distribution,
            [Description(ParametersText)] double[] parameters,
            [Description("The values to score, at least one, no missing values.")]
            double[] values)
        {
            var model = UnivariateModel.Create(distribution, parameters);
            DistributionGuard.Values(values, nameof(values));

            return TailScores(model, values);
        }

        internal static DistributionTailScoreResult TailScores(UnivariateModel model, double[] values)
        {
            var lower = new double[values.Length];
            var upper = new double[values.Length];
            var two = new double[values.Length];
            var surprise = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                lower[i] = model.Cdf(values[i]);
                upper[i] = model.Discrete ? model.Ccdf(values[i] - 1) : model.Ccdf(values[i]);
                two[i] = Math.Min(1.0, 2.0 * Math.Min(lower[i], upper[i]));
                surprise[i] = two[i] > 0 ? -Math.Log10(two[i]) : double.PositiveInfinity;
            }

            return new DistributionTailScoreResult(
                model.Entry.Name,
                values,
                DistributionGuard.N(lower),
                DistributionGuard.N(upper),
                DistributionGuard.N(two),
                DistributionGuard.N(surprise));
        }

        private static string[] ParameterLabels(UnivariateModel model)
        {
            return model.Entry.Variadic
                ? model.Parameters.Select((_, i) => $"probabilityOf{i}").ToArray()
                : model.Entry.Parameters.Select(p => p.Name).ToArray();
        }

        internal static string[] LabelsFor(DistributionEntry entry, int count)
        {
            return entry.Variadic
                ? Enumerable.Range(0, count).Select(i => $"probabilityOf{i}").ToArray()
                : entry.Parameters.Select(p => p.Name).ToArray();
        }

        private static DistributionEvaluation Evaluate(UnivariateModel model, string quantity, double[] inputs,
            Func<double, double> f)
        {
            var values = new double?[inputs.Length];

            for (var i = 0; i < inputs.Length; i++)
            {
                values[i] = DistributionGuard.N(f(inputs[i]));
            }

            return new DistributionEvaluation(model.Entry.Name, quantity, model.Parameters, inputs, values);
        }
    }
}
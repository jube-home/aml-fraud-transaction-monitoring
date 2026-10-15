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
    public class DistributionFittingTools
    {
        private const int MaximumValues = 200000;

        private const string DistributionText =
            "Which distribution to fit. Call ListDistributions to see the choices and which can be fitted (canFit).";

        private const string FixedNamesText =
            "Optional names of parameters to hold at a fixed value instead of estimating, using the parameter names from ListDistributions (for example trials for Binomial, or location for GeneralizedPareto). Leave empty to estimate everything.";

        private const string FixedValuesText =
            "The fixed value for each name in fixedParameterNames, in the same order and of the same length. Leave empty when no names are given.";

        [AdvancedAnalyticsTool("FitDistribution")]
        [Description(
            "Estimates the parameters of one chosen distribution from a sample of numbers by maximum likelihood (or the method of moments where that is what the distribution uses) and reports how good the fit is. " +
            "It returns the fitted parameters with their names, the log-likelihood (higher is better), AIC and BIC (lower is better, and only comparable between distributions of the same kind on the same data), mean, variance and median of the fitted distribution, and the Kolmogorov-Smirnov distance between the data and the fit (0 is perfect, values above about 0.1 with a decent sample mean a poor fit). " +
            "Some distributions need the data to be in a certain range (for example the Lognormal needs values above 0); the error message says what is wrong. " +
            "Use RankDistributionFits to compare several candidate distributions at once.")]
        public DistributionFitResult FitDistribution(
            [Description("The observations, at least 2 numbers, no missing values.")]
            double[] values,
            [Description(DistributionText)] DistributionKind distribution,
            [Description(FixedNamesText)] string[]? fixedParameterNames = null,
            [Description(FixedValuesText)] double[]? fixedParameterValues = null)
        {
            DistributionGuard.Values(values, nameof(values), 2);
            Limit(values);
            var entry = DistributionCatalogue.Get(distribution);
            var fixedArray = BuildFixed(entry, fixedParameterNames, fixedParameterValues);
            var outcome = DistributionFitter.Fit(entry, values, fixedArray);

            return ToResult(outcome, values);
        }

        [AdvancedAnalyticsTool("FitDistributionAndScoreValues")]
        [Description(
            "Fits one distribution to a reference sample and then scores other values against the fitted distribution in one step. " +
            "It returns the fit (as FitDistribution does) and, for each value to score, the lower tail probability, the upper tail probability, a two sided tail probability (twice the smaller tail) and the fitted density. " +
            "Use it to ask how unusual new observations are relative to a fitted baseline, for example whether today's transaction amounts are extreme given the last quarter.")]
        public DistributionFitScoreResult FitDistributionAndScoreValues(
            [Description("The reference observations to fit on, at least 2 numbers, no missing values.")]
            double[] values,
            [Description(DistributionText)] DistributionKind distribution,
            [Description("The new values to score against the fitted distribution, at least one, no missing values.")]
            double[] valuesToScore,
            [Description(FixedNamesText)] string[]? fixedParameterNames = null,
            [Description(FixedValuesText)] double[]? fixedParameterValues = null)
        {
            DistributionGuard.Values(values, nameof(values), 2);
            DistributionGuard.Values(valuesToScore, nameof(valuesToScore));
            Limit(values);
            var entry = DistributionCatalogue.Get(distribution);
            var fixedArray = BuildFixed(entry, fixedParameterNames, fixedParameterValues);
            var outcome = DistributionFitter.Fit(entry, values, fixedArray);
            var scores = UnivariateDistributionTools.TailScores(outcome.Model, valuesToScore);

            return new DistributionFitScoreResult(
                ToResult(outcome, values),
                valuesToScore,
                scores.LowerTail,
                scores.UpperTail,
                scores.TwoSided,
                DistributionGuard.N(valuesToScore.Select(outcome.Model.Pdf).ToArray()));
        }

        [AdvancedAnalyticsTool("RankDistributionFits")]
        [Description(
            "Fits many candidate distributions to the same sample and ranks them by goodness of fit, so you do not have to guess which distribution suits the data. " +
            "For each candidate that could be fitted it returns the fitted parameters, log-likelihood, AIC and BIC (lower is better), the Kolmogorov-Smirnov distance and its p value (the p value is optimistic because the parameters were estimated from the same data), the Anderson-Darling statistic (weights the tails, lower is better), the chi-square statistic (lower is better) and the rank of the candidate under each measure. " +
            "Candidates that could not be fitted are listed with the reason. The list is sorted by the criterion you choose; AIC is the default and is the best single guide when candidates have different numbers of parameters. " +
            "Leave candidates empty to try every suitable distribution; with dataType Automatic the values are treated as counts (discrete candidates) when all are whole numbers with at most 50 distinct values. Continuous and discrete candidates cannot be mixed.")]
        public DistributionFitRanking RankDistributionFits(
            [Description("The observations, at least 5 numbers, no missing values, at most 200000.")]
            double[] values,
            [Description(
                "The candidate distributions to try, from ListDistributions. Leave empty or null to try every fittable distribution whose data requirements the values satisfy.")]
            DistributionKind[]? candidates = null,
            [Description(
                "Criterion that orders the result: Aic (default), Bic, KolmogorovSmirnov, AndersonDarling, ChiSquare or AverageRank of the other measures.")]
            FitRankCriterion rankBy = FitRankCriterion.Aic,
            [Description(
                "Continuous or Discrete decides which default candidates are tried; Automatic (default) decides from the data.")]
            FitDataType dataType = FitDataType.Automatic)
        {
            DistributionGuard.Values(values, nameof(values), 5);
            Limit(values);
            var notes = new List<string>();

            var discrete = dataType switch
            {
                FitDataType.Discrete => true,
                FitDataType.Continuous => false,
                _ => values.All(v => Math.Abs(v - Math.Round(v)) < 1e-9) && values.Distinct().Count() <= 50
            };

            List<DistributionEntry> entries;

            if (candidates is { Length: > 0 })
            {
                entries = candidates.Distinct().Select(DistributionCatalogue.Get).ToList();

                if (entries.Select(e => e.Discrete).Distinct().Count() > 1)
                {
                    throw new ArgumentException(
                        "candidates mixes discrete and continuous distributions, whose likelihoods cannot be compared. Give only one kind.",
                        nameof(candidates));
                }

                discrete = entries[0].Discrete;
            }
            else
            {
                entries = DistributionCatalogue.All
                    .Where(e => e.Fit != FitMode.None && e.Discrete == discrete &&
                                e.Kind != DistributionKind.VonMises &&
                                DistributionFitter.DataProblem(e, values) == null)
                    .ToList();

                notes.Add(discrete
                    ? "Values were treated as counts, so only discrete distributions were tried."
                    : "Values were treated as continuous, so only continuous distributions whose data requirements the values meet were tried.");
            }

            var failed = new List<string>();
            var fitted = new List<(FitOutcome Outcome, double? Ks, double? KsP, double? Ad, double? Chi)>();

            foreach (var entry in entries)
            {
                try
                {
                    var outcome = DistributionFitter.Fit(entry, values, null);

                    if (!double.IsFinite(outcome.LogLikelihood))
                    {
                        failed.Add(
                            $"{entry.Name}: the likelihood is undefined at the fitted parameters, usually because observations sit on the edge of the fitted support.");

                        continue;
                    }

                    var (ks, ksP) = FitScoring.KolmogorovSmirnov(values, outcome.Model);

                    fitted.Add((outcome, ks, ksP, FitScoring.AndersonDarling(values, outcome.Model),
                        FitScoring.ChiSquare(values, outcome.Model)));
                }
                catch (ArgumentException ex)
                {
                    failed.Add($"{entry.Name}: {ex.Message}");
                }
            }

            if (fitted.Count == 0)
            {
                return new DistributionFitRanking(values.Length, rankBy.ToString(),
                    Array.Empty<DistributionFitCandidate>(),
                    failed.ToArray(), notes.ToArray());
            }

            var n = values.Length;
            var aic = fitted.Select(f => (double?)AicOf(f.Outcome)).ToArray();
            var bic = fitted.Select(f => (double?)BicOf(f.Outcome, n)).ToArray();

            var aicRank =
                FitScoring.Ranks(aic.Select(v => v.HasValue && double.IsFinite(v.Value) ? v : null).ToArray());
            var bicRank =
                FitScoring.Ranks(bic.Select(v => v.HasValue && double.IsFinite(v.Value) ? v : null).ToArray());

            var ksRank = FitScoring.Ranks(fitted.Select(f => f.Ks).ToArray());
            var adRank = FitScoring.Ranks(fitted.Select(f => f.Ad).ToArray());
            var chiRank = FitScoring.Ranks(fitted.Select(f => f.Chi).ToArray());

            var averages = new double?[fitted.Count];

            for (var i = 0; i < fitted.Count; i++)
            {
                averages[i] = (aicRank[i] + ksRank[i] + adRank[i] + chiRank[i]) / 4.0 + aic[i]!.Value * 1e-12;
            }

            var averageRank = FitScoring.Ranks(averages);

            var keys = rankBy switch
            {
                FitRankCriterion.Bic => bic.Select(v => v.HasValue && double.IsFinite(v.Value) ? v : null).ToArray(),
                FitRankCriterion.KolmogorovSmirnov => fitted.Select(f => f.Ks).ToArray(),
                FitRankCriterion.AndersonDarling => fitted.Select(f => f.Ad).ToArray(),
                FitRankCriterion.ChiSquare => fitted.Select(f => f.Chi).ToArray(),
                FitRankCriterion.AverageRank => averages,
                _ => aic.Select(v => v.HasValue && double.IsFinite(v.Value) ? v : null).ToArray()
            };

            var finalRank = FitScoring.Ranks(keys);

            var rows = new List<DistributionFitCandidate>();

            for (var i = 0; i < fitted.Count; i++)
            {
                var f = fitted[i];

                rows.Add(new DistributionFitCandidate(
                    f.Outcome.Model.Entry.Name,
                    f.Outcome.Model.Discrete,
                    UnivariateDistributionTools.LabelsFor(f.Outcome.Model.Entry, f.Outcome.Parameters.Length),
                    f.Outcome.Parameters,
                    f.Outcome.Estimated.Count(e => e),
                    f.Outcome.LogLikelihood,
                    aic[i]!.Value,
                    bic[i]!.Value,
                    f.Ks,
                    f.KsP,
                    f.Ad,
                    f.Chi,
                    aicRank[i],
                    bicRank[i],
                    ksRank[i],
                    adRank[i],
                    chiRank[i],
                    averageRank[i],
                    finalRank[i]));
            }

            if (fitted.Any(f => f.Ad == null) && !discrete)
            {
                notes.Add(
                    "Some Anderson-Darling or chi-square statistics were unavailable (null) and ranked last on those measures.");
            }

            if (values.Length < 20)
            {
                notes.Add(
                    "With fewer than 20 values the chi-square statistic is not computed and all rankings are unstable.");
            }

            return new DistributionFitRanking(values.Length, rankBy.ToString(),
                rows.OrderBy(r => r.Rank).ToArray(), failed.ToArray(), notes.ToArray());
        }

        [AdvancedAnalyticsTool("QuantileQuantilePlotPoints")]
        [Description(
            "Generates the points of a quantile-quantile (Q-Q) plot of a sample against a distribution. " +
            "The sample is sorted; each order statistic is paired with the theoretical quantile at its plotting position. If the distribution suits the data the points lie on a straight line: slope and intercept of the least squares line (slope is the scale and intercept the location when comparing to a standard shape) and the correlation of the points are returned, and a correlation close to 1 supports the fit. " +
            "Leave parameters empty to fit the distribution to the data first. Curvature at the ends means heavier or lighter tails than the distribution. " +
            "When there are more sample values than maxPoints an evenly spaced subset of points is returned; the correlation and line always use every value.")]
        public ProbabilityPlotResult QuantileQuantilePlotPoints(
            [Description("The observations, at least 3 numbers, no missing values.")]
            double[] values,
            [Description(DistributionText)] DistributionKind distribution,
            [Description(
                "Fixed distribution parameters in ListDistributions order; leave empty or null to estimate the parameters from the values.")]
            double[]? parameters = null,
            [Description(
                "How plotting positions are computed from ranks: Hazen (i-0.5)/n default, Blom, Weibull i/(n+1) or Benard.")]
            PlottingPosition plottingPosition = PlottingPosition.Hazen,
            [Description("Most points to return, between 10 and 20000. Default 500.")]
            int maxPoints = 500)
        {
            return BuildPlot(values, distribution, parameters, plottingPosition, maxPoints, false);
        }

        [AdvancedAnalyticsTool("ProbabilityProbabilityPlotPoints")]
        [Description(
            "Generates the points of a probability-probability (P-P) plot of a sample against a distribution. " +
            "The sample is sorted; for each order statistic the empirical probability (its plotting position) is paired with the theoretical cumulative probability of that value under the distribution. If the fit is good the points lie on the diagonal from (0,0) to (1,1); the correlation of the two probability columns is returned. " +
            "A P-P plot is most sensitive to differences in the centre of the distribution, while a Q-Q plot is more sensitive in the tails. " +
            "Leave parameters empty to fit the distribution to the data first.")]
        public ProbabilityPlotResult ProbabilityProbabilityPlotPoints(
            [Description("The observations, at least 3 numbers, no missing values.")]
            double[] values,
            [Description(DistributionText)] DistributionKind distribution,
            [Description(
                "Fixed distribution parameters in ListDistributions order; leave empty or null to estimate the parameters from the values.")]
            double[]? parameters = null,
            [Description(
                "How plotting positions are computed from ranks: Hazen (i-0.5)/n default, Blom, Weibull i/(n+1) or Benard.")]
            PlottingPosition plottingPosition = PlottingPosition.Hazen,
            [Description("Most points to return, between 10 and 20000. Default 500.")]
            int maxPoints = 500)
        {
            return BuildPlot(values, distribution, parameters, plottingPosition, maxPoints, true);
        }

        private static ProbabilityPlotResult BuildPlot(double[] values, DistributionKind distribution,
            double[]? parameters,
            PlottingPosition plottingPosition, int maxPoints, bool probabilityPlot)
        {
            DistributionGuard.Values(values, nameof(values), 3);
            Limit(values);

            if (maxPoints < 10 || maxPoints > 20000)
            {
                throw new ArgumentException($"maxPoints must be between 10 and 20000, got {maxPoints}.",
                    nameof(maxPoints));
            }

            UnivariateModel model;

            if (parameters == null || parameters.Length == 0)
            {
                var entry = DistributionCatalogue.Get(distribution);

                if (entry.Parameters.Length == 0 && !entry.Variadic)
                {
                    model = UnivariateModel.Create(distribution, Array.Empty<double>());
                }
                else
                {
                    model = DistributionFitter.Fit(entry, values, null).Model;
                }
            }
            else
            {
                model = UnivariateModel.Create(distribution, parameters);
            }

            var sorted = (double[])values.Clone();
            Array.Sort(sorted);
            var positions = FitScoring.Positions(sorted.Length, plottingPosition);
            var theoretical = positions.Select(model.Quantile).ToArray();
            var theoreticalProbabilities = sorted.Select(model.Cdf).ToArray();

            var finiteQ = theoretical.All(double.IsFinite);
            double correlation;
            var slope = double.NaN;
            var intercept = double.NaN;

            if (probabilityPlot)
            {
                correlation = FitScoring.Correlation(theoreticalProbabilities, positions);
            }
            else
            {
                correlation = finiteQ ? FitScoring.Correlation(theoretical, sorted) : double.NaN;

                if (finiteQ)
                {
                    var sx = HistogramEngine.StandardDeviation(theoretical);

                    if (sx > 0)
                    {
                        slope = HistogramEngine.Covariance(theoretical, sorted) / (sx * sx);
                        intercept = HistogramEngine.Mean(sorted) - slope * HistogramEngine.Mean(theoretical);
                    }
                }
            }

            var index = Thinned(sorted.Length, maxPoints);

            return new ProbabilityPlotResult(
                model.Entry.Name,
                plottingPosition.ToString(),
                model.Parameters,
                index.Select(i => sorted[i]).ToArray(),
                index.Select(i => positions[i]).ToArray(),
                DistributionGuard.N(index.Select(i => theoretical[i]).ToArray()),
                DistributionGuard.N(index.Select(i => theoreticalProbabilities[i]).ToArray()),
                DistributionGuard.N(index.Select(i => positions[i]).ToArray()),
                DistributionGuard.N(correlation),
                DistributionGuard.N(slope),
                DistributionGuard.N(intercept));
        }

        internal static int[] Thinned(int count, int maxPoints)
        {
            if (count <= maxPoints)
            {
                return Enumerable.Range(0, count).ToArray();
            }

            var result = new int[maxPoints];

            for (var i = 0; i < maxPoints; i++)
            {
                result[i] = (int)Math.Round((double)i * (count - 1) / (maxPoints - 1));
            }

            return result;
        }

        private static void Limit(double[] values)
        {
            if (values.Length > MaximumValues)
            {
                throw new ArgumentException(
                    $"values has {values.Length} entries; this tool accepts at most {MaximumValues}. Sample the data down first.",
                    nameof(values));
            }
        }

        private static double AicOf(FitOutcome o)
        {
            return 2.0 * o.Estimated.Count(e => e) - 2.0 * o.LogLikelihood;
        }

        private static double BicOf(FitOutcome o, int n)
        {
            return o.Estimated.Count(e => e) * Math.Log(n) - 2.0 * o.LogLikelihood;
        }

        private static double[]? BuildFixed(DistributionEntry entry, string[]? names, double[]? values)
        {
            if (names == null || names.Length == 0)
            {
                if (values is { Length: > 0 })
                {
                    throw new ArgumentException("fixedParameterValues was given without fixedParameterNames.",
                        "fixedParameterNames");
                }

                return null;
            }

            if (values == null || values.Length != names.Length)
            {
                throw new ArgumentException(
                    $"fixedParameterValues must have one value per name in fixedParameterNames ({names.Length}).",
                    "fixedParameterValues");
            }

            var result = Enumerable.Repeat(double.NaN, entry.Parameters.Length).ToArray();

            for (var i = 0; i < names.Length; i++)
            {
                var index = Array.FindIndex(entry.Parameters,
                    p => string.Equals(p.Name, names[i], StringComparison.OrdinalIgnoreCase));

                if (index < 0)
                {
                    throw new ArgumentException(
                        $"'{names[i]}' is not a parameter of {entry.Name}. Its parameters are: {string.Join(", ", entry.Parameters.Select(p => p.Name))}.",
                        "fixedParameterNames");
                }

                if (!double.IsFinite(values[i]))
                {
                    throw new ArgumentException($"The fixed value for {names[i]} must be a finite number.",
                        "fixedParameterValues");
                }

                result[index] = values[i];
            }

            return result;
        }

        private static DistributionFitResult ToResult(FitOutcome outcome, double[] values)
        {
            var model = outcome.Model;
            var n = values.Length;
            var k = outcome.Estimated.Count(e => e);
            var (ks, _) = FitScoring.KolmogorovSmirnov(values, model);
            var notes = new List<string>();

            for (var i = 0; i < outcome.Estimated.Length; i++)
            {
                if (!outcome.Estimated[i])
                {
                    notes.Add(
                        $"{model.Entry.Parameters[i].Name} was held fixed at {outcome.Parameters[i]} and not estimated.");
                }
            }

            if (!string.IsNullOrEmpty(model.Entry.FitNote) && outcome.Estimated.Any(e => !e))
            {
                notes.Add(model.Entry.FitNote);
            }

            if (n < 20)
            {
                notes.Add(
                    "Fewer than 20 values: the estimates are unstable and the goodness of fit measures are weak evidence.");
            }

            return new DistributionFitResult(
                model.Entry.Name,
                model.Discrete,
                outcome.Method,
                UnivariateDistributionTools.LabelsFor(model.Entry, outcome.Parameters.Length),
                outcome.Parameters,
                outcome.Estimated,
                n,
                DistributionGuard.N(outcome.LogLikelihood),
                k,
                DistributionGuard.N(AicOf(outcome)),
                DistributionGuard.N(BicOf(outcome, n)),
                DistributionGuard.N(model.Mean),
                DistributionGuard.N(model.Variance),
                DistributionGuard.N(model.Median),
                ks,
                notes.ToArray());
        }
    }
}
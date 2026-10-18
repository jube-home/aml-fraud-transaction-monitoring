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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SurvivalModelTools
    {
        private const int MaximumCoxRows = 10000;
        private const int MaximumCoxCovariates = 50;
        private const int MaximumConcordanceRows = 20000;

        [AdvancedAnalyticsTool("FitCoxProportionalHazards")]
        [Description(
            "Fits a Cox proportional hazards regression: how numeric covariates multiply the instantaneous risk of an event over time, with censored cases handled correctly. No model is saved. " +
            "The coefficient table gives, per covariate, the log hazard ratio, standard error, Wald z and p-value, the hazard ratio exp(coefficient) and its confidence interval: a hazard ratio of 1.5 means each extra unit of the covariate multiplies the event rate by 1.5 at every time, below 1 lowers it, and an interval containing 1 means no clear effect. " +
            "Also returned are the partial log-likelihood, the likelihood-ratio test of the whole model against no covariates, Harrell's concordance index (0.5 is chance, 1 is perfect ranking of who fails first), the Breslow baseline cumulative hazard and survival at the mean covariate profile, and, if you give new rows and prediction times, each new row's relative hazard and survival probability at those times. " +
            "Tied event times use the Breslow approximation. Covariates must be numeric (encode categories as 0/1 columns) and not constant; rows with NaN are dropped. Warnings flag non-convergence, few events per covariate and suspected separation. Limited to 10000 rows and 50 covariates.")]
        public CoxModelResult FitCoxProportionalHazards(
            [Description(
                "Covariate rows: one array of numbers per case, all the same length and in the same order as the times; no covariate may be constant. NaN rows are dropped.")]
            double[][] covariates,
            [Description(
                "Time to event or censoring for each case, zero or more, in one consistent unit; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description(
                "Optional name of each covariate column in order; empty labels them Covariate1, Covariate2 and so on.")]
            string[]? covariateNames = null,
            [Description("Confidence level for hazard ratio intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Maximum Newton-Raphson iterations, from 1 to 500. Default 50.")]
            int maximumIterations = 50,
            [Description("Convergence tolerance on the relative change of the coefficients, above 0. Default 1e-6.")]
            double tolerance = 1e-6,
            [Description(
                "Optional new covariate rows, same columns as covariates, to score with the fitted model; leave empty for none.")]
            double[][]? newCovariates = null,
            [Description(
                "Optional times at which to predict survival for the new rows; required with newCovariates, ignored otherwise.")]
            double[]? predictionTimes = null,
            [Description("Maximum baseline rows to return, thinned evenly; 0 returns all event times. Default 200.")]
            int maxBaselineRows = 200)
        {
            CategoricalGuards.Required(covariates, nameof(covariates));
            CategoricalGuards.Required(times, nameof(times));
            CategoricalGuards.BinaryLabels(events, nameof(events));
            CategoricalGuards.SameLength(covariates.Length, times.Length, nameof(covariates), nameof(times));
            CategoricalGuards.SameLength(covariates.Length, events.Length, nameof(covariates), nameof(events));
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (maximumIterations < 1 || maximumIterations > 500)
            {
                throw new ArgumentException("maximumIterations must be between 1 and 500.", nameof(maximumIterations));
            }

            CategoricalGuards.PositiveFinite(tolerance, nameof(tolerance));

            if (maxBaselineRows < 0)
            {
                throw new ArgumentException("maxBaselineRows cannot be negative; use 0 for all rows.",
                    nameof(maxBaselineRows));
            }

            if (covariates.Length == 0 || covariates[0] == null || covariates[0].Length == 0)
            {
                throw new ArgumentException("Give at least one case with at least one covariate.", nameof(covariates));
            }

            var columns = covariates[0].Length;

            if (columns > MaximumCoxCovariates)
            {
                throw new ArgumentException($"At most {MaximumCoxCovariates} covariates are supported; select fewer.",
                    nameof(covariates));
            }

            var names = CategoricalGuards.Names(covariateNames, columns, "Covariate", nameof(covariateNames));
            var keep = new List<int>();

            for (var i = 0; i < covariates.Length; i++)
            {
                if (covariates[i] == null || covariates[i].Length != columns)
                {
                    throw new ArgumentException(
                        $"Every row of covariates must have {columns} numbers; row {i} does not.", nameof(covariates));
                }

                if (double.IsInfinity(times[i]) || times[i] < 0d)
                {
                    throw new ArgumentException($"times must be finite and not negative; entry {i} is {times[i]}.",
                        nameof(times));
                }

                if (covariates[i].Any(v => double.IsInfinity(v)))
                {
                    throw new ArgumentException($"covariates must be finite; row {i} contains an infinite value.",
                        nameof(covariates));
                }

                if (!double.IsNaN(times[i]) && covariates[i].All(v => !double.IsNaN(v)))
                {
                    keep.Add(i);
                }
            }

            if (keep.Count == 0)
            {
                throw new ArgumentException("There are no complete rows: every row has a NaN time or covariate.",
                    nameof(covariates));
            }

            if (keep.Count > MaximumCoxRows)
            {
                throw new ArgumentException(
                    $"At most {MaximumCoxRows} rows are supported for a Cox fit; sample or aggregate the data first.",
                    nameof(covariates));
            }

            var x = keep.Select(i => (double[])covariates[i].Clone()).ToArray();
            var t = keep.Select(i => times[i]).ToArray();
            var e = keep.Select(i => events[i]).ToArray();
            var eventCount = e.Sum();

            if (eventCount == 0)
            {
                throw new ArgumentException(
                    "No events were observed among the usable rows, so no hazard can be modelled.",
                    nameof(events));
            }

            for (var c = 0; c < columns; c++)
            {
                var first = x[0][c];

                if (x.All(row => Math.Abs(row[c] - first) < 0.0001))
                {
                    throw new ArgumentException(
                        $"Covariate '{names[c]}' is constant among the usable rows and cannot be estimated; remove it.",
                        nameof(covariates));
                }
            }

            double[][]? fresh = null;
            double[] evaluation = [];

            if (newCovariates is { Length: > 0 })
            {
                if (predictionTimes == null || predictionTimes.Length == 0)
                {
                    throw new ArgumentException("Give predictionTimes when newCovariates are supplied.",
                        nameof(predictionTimes));
                }

                if (predictionTimes.Any(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0d))
                {
                    throw new ArgumentException("predictionTimes must be finite numbers, zero or more.",
                        nameof(predictionTimes));
                }

                for (var i = 0; i < newCovariates.Length; i++)
                {
                    if (newCovariates[i] == null || newCovariates[i].Length != columns ||
                        newCovariates[i].Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                    {
                        throw new ArgumentException(
                            $"newCovariates row {i} must hold {columns} finite numbers.", nameof(newCovariates));
                    }
                }

                fresh = newCovariates;
                evaluation = predictionTimes;
            }

            var ranked = SurvivalModelMath.DenseRanks(t);
            var fit = CategoricalLibrary.FitProportionalHazards(x, ranked, e, maximumIterations, tolerance);
            var nullLikelihood = CategoricalLibrary.NullPartialLogLikelihood(x, ranked, e);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var coefficients = new List<CoxCoefficientRow>();

            for (var c = 0; c < columns; c++)
            {
                var beta = fit.Coefficients[c];
                var se = fit.StandardErrors[c];
                var statistic = se > 0d ? beta / se : double.NaN;

                coefficients.Add(new CoxCoefficientRow
                {
                    Name = names[c],
                    Coefficient = beta,
                    StandardError = se,
                    HazardRatio = Math.Exp(beta),
                    HazardRatioLower = Math.Exp(beta - z * se),
                    HazardRatioUpper = Math.Exp(beta + z * se),
                    ZStatistic = statistic,
                    PValue = double.IsNaN(statistic)
                        ? double.NaN
                        : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic))
                });
            }

            var means = Enumerable.Range(0, columns).Select(c => x.Average(row => row[c])).ToArray();

            double Linear(double[] row)
            {
                var sum = 0d;

                for (var c = 0; c < columns; c++)
                {
                    sum += fit.Coefficients[c] * (row[c] - means[c]);
                }

                return sum;
            }

            var linear = x.Select(Linear).ToArray();
            var concordance = SurvivalModelMath.Concordance(t, e, linear, 0).ConcordanceIndex;
            var relative = linear.Select(Math.Exp).ToArray();
            var baseline = SurvivalModelMath.Breslow(t, e, relative);

            var baselineRows = baseline.Times.Select((time, i) => new CoxBaselineRow
            {
                Time = time,
                CumulativeHazard = baseline.Hazard[i],
                Survival = Math.Exp(-baseline.Hazard[i])
            }).ToArray();

            var predictions = new List<CoxPredictionRow>();

            if (fresh != null)
            {
                foreach (var row in fresh)
                {
                    var lp = Linear(row);
                    var rr = Math.Exp(lp);

                    predictions.Add(new CoxPredictionRow
                    {
                        LinearPredictor = lp,
                        RelativeHazard = rr,
                        SurvivalAtPredictionTimes = evaluation.Select(time =>
                        {
                            var h0 = 0d;

                            for (var i = 0; i < baseline.Times.Length && baseline.Times[i] <= time; i++)
                            {
                                h0 = baseline.Hazard[i];
                            }

                            return Math.Exp(-h0 * rr);
                        }).ToArray()
                    });
                }
            }

            var warnings = new List<string>();

            if (!fit.Converged)
            {
                warnings.Add(
                    "The fit did not converge within the iteration limit; raise maximumIterations or check for collinear covariates.");
            }

            if (eventCount < 10 * columns)
            {
                warnings.Add(
                    $"There are only {eventCount} events for {columns} covariates (less than 10 per covariate), so estimates may be unstable or over-fitted.");
            }

            if (coefficients.Any(c => double.IsNaN(c.Coefficient) || double.IsNaN(c.StandardError) ||
                                      Math.Abs(c.Coefficient) > 15d || c.StandardError > 1000d))
            {
                warnings.Add(
                    "At least one coefficient is non-finite or extreme, which suggests complete separation or collinearity; treat the table with caution.");
            }

            var lr = 2d * (fit.PartialLogLikelihood - nullLikelihood);

            return new CoxModelResult
            {
                Coefficients = coefficients.ToArray(),
                Subjects = t.Length,
                Events = eventCount,
                LogLikelihood = fit.PartialLogLikelihood,
                NullLogLikelihood = nullLikelihood,
                LikelihoodRatioChiSquare = lr,
                LikelihoodRatioDegreesOfFreedom = columns,
                LikelihoodRatioPValue = CategoricalLibrary.ChiSquareSurvival(lr, columns),
                ConcordanceIndex = concordance,
                Converged = fit.Converged,
                Iterations = fit.Iterations,
                TieMethod = "Breslow (every tied event uses the full risk set)",
                CovariateMeans = means,
                BaselineRows = Thin(baselineRows, maxBaselineRows),
                PredictionTimes = evaluation,
                Predictions = predictions.ToArray(),
                Warnings = warnings.ToArray(),
                SkippedRows = times.Length - keep.Count
            };
        }

        [AdvancedAnalyticsTool("CalculateHarrellConcordanceIndex")]
        [Description(
            "Measures how well any risk score ranks cases by time to event, allowing for censoring, with Harrell's concordance index (C-index). " +
            "Among all comparable pairs (the case with the shorter time had the event; a tie in time counts only when exactly one of the pair had the event) it counts how often the higher risk score belongs to the case that failed first. " +
            "0.5 is no better than chance, 1 is perfect, below 0.5 means the score is inverted, and pairs with equal scores count half. It also returns Somers D (2C minus 1) and the pair counts. Use it to judge a Cox model, a fraud score or any risk ranking against time-to-event outcomes. " +
            "Limited to 20000 rows because every pair is examined. Rows with a NaN time or risk score are dropped.")]
        public ConcordanceResult CalculateHarrellConcordanceIndex(
            [Description(
                "Time to event or censoring for each case, zero or more, in one consistent unit; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description(
                "Risk score for each case in the same order, where a higher value means the event is expected sooner.")]
            double[] riskScores)
        {
            CategoricalGuards.Required(riskScores, nameof(riskScores));
            CategoricalGuards.Required(times, nameof(times));
            CategoricalGuards.BinaryLabels(events, nameof(events));
            CategoricalGuards.SameLength(times.Length, riskScores.Length, nameof(times), nameof(riskScores));
            CategoricalGuards.SameLength(times.Length, events.Length, nameof(times), nameof(events));

            for (var i = 0; i < times.Length; i++)
            {
                if (double.IsInfinity(times[i]) || times[i] < 0d)
                {
                    throw new ArgumentException($"times must be finite and not negative; entry {i} is {times[i]}.",
                        nameof(times));
                }
            }

            var keep = Enumerable.Range(0, times.Length)
                .Where(i => !double.IsNaN(times[i]) && !double.IsNaN(riskScores[i])).ToArray();

            if (keep.Length == 0)
            {
                throw new ArgumentException("There are no rows with both a time and a risk score.", nameof(riskScores));
            }

            if (keep.Length > MaximumConcordanceRows)
            {
                throw new ArgumentException(
                    $"At most {MaximumConcordanceRows} rows are supported; sample the data first.", nameof(times));
            }

            return SurvivalModelMath.Concordance(keep.Select(i => times[i]).ToArray(),
                keep.Select(i => events[i]).ToArray(), keep.Select(i => riskScores[i]).ToArray(),
                times.Length - keep.Length);
        }

        [AdvancedAnalyticsTool("FitExponentialSurvivalModel")]
        [Description(
            "Fits a constant-hazard (exponential) survival model to censored time-to-event data by maximum likelihood: the event rate is the number of events divided by the total time at risk. " +
            "Returns the rate with an exact confidence interval, the mean and median lifetime, the survival probability with a band at any times you list, and MaximumAbsoluteDifferenceFromKaplanMeier, the largest gap between this model and the non-parametric Kaplan-Meier curve: a small value (under about 0.05) supports the constant-hazard assumption, a large one means the risk changes over time and a Weibull or piecewise model is needed. " +
            "With no events the rate is 0 and the mean and median lifetime are NaN because they are unbounded. Events are 1 for observed and 0 for censored; NaN time rows are dropped.")]
        public ExponentialSurvivalResult FitExponentialSurvivalModel(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description("Optional times at which to report model survival with a band; leave empty to omit.")]
            double[]? evaluationTimes = null,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (evaluationTimes != null && evaluationTimes.Any(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0d))
            {
                throw new ArgumentException("Every evaluation time must be a finite number, zero or more.",
                    nameof(evaluationTimes));
            }

            var total = t.Sum();

            if (total <= 0d)
            {
                throw new ArgumentException("The total time at risk is zero, so no rate can be estimated.",
                    nameof(times));
            }

            var events1 = e.Sum();
            var interval = CategoricalIntervals.PoissonRate(events1, total, confidenceLevel);
            var rate = interval.Estimate;
            var steps = SurvivalMath.Steps(t, e);
            var maxDifference = 0d;

            foreach (var step in steps.Where(s => s.Events > 0))
            {
                maxDifference = Math.Max(maxDifference, Math.Abs(step.Survival - Math.Exp(-rate * step.Time)));
            }

            return new ExponentialSurvivalResult
            {
                Rate = rate,
                RateLower = interval.Lower,
                RateUpper = interval.Upper,
                MeanLifetime = rate > 0d ? 1d / rate : double.NaN,
                MeanLifetimeLower = interval.Upper > 0d ? 1d / interval.Upper : double.NaN,
                MeanLifetimeUpper = interval.Lower > 0d ? 1d / interval.Lower : double.NaN,
                MedianLifetime = rate > 0d ? Math.Log(2d) / rate : double.NaN,
                Events = events1,
                Subjects = t.Length,
                TotalTimeAtRisk = total,
                SurvivalAtTimes = (evaluationTimes ?? []).Select(time => new ExponentialSurvivalPoint
                {
                    Time = time,
                    Survival = Math.Exp(-rate * time),
                    Lower = Math.Exp(-interval.Upper * time),
                    Upper = Math.Exp(-interval.Lower * time)
                }).ToArray(),
                MaximumAbsoluteDifferenceFromKaplanMeier = maxDifference,
                Fit = "maximum likelihood, constant hazard",
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateIncidenceRatesByGroup")]
        [Description(
            "Computes person-time incidence rates by group: for each group the number of events, the total time at risk, the event rate per unit of time with an exact Poisson confidence interval, and the rate ratio against a reference group with an exact interval and p-value. " +
            "Use it to compare event intensity between segments when follow-up lengths differ, for example chargebacks per account-year. The rate ratio is the group's rate divided by the reference rate (above 1 means higher). " +
            "Leave the reference empty to use the alphabetically first group. Rows with a NaN time or a blank group are dropped; events are 1 for observed and 0 for censored. A group with no time at risk cannot be rated.")]
        public IncidenceRatesResult CalculateIncidenceRatesByGroup(
            [Description("Time at risk for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event occurred, 0 otherwise.")]
            int[] events,
            [Description(
                "Group name for each case in the same order; blank names are dropped. Use one name for every case for a single overall rate.")]
            string[] groups,
            [Description(
                "Optional name of the reference group for the rate ratios; empty uses the alphabetically first group.")]
            string? referenceGroup = null,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.CleanGrouped(times, events, groups, out var t, out var e, out var g, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var names = g.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
            var reference = string.IsNullOrWhiteSpace(referenceGroup) ? names[0] : referenceGroup;

            if (!names.Contains(reference))
            {
                throw new ArgumentException($"referenceGroup '{reference}' does not occur among the groups.",
                    nameof(referenceGroup));
            }

            var totals = names.ToDictionary(n => n, n =>
            {
                var members = Enumerable.Range(0, t.Length).Where(i => g[i] == n).ToArray();

                return (Subjects: members.Length, Events: members.Sum(i => e[i]), Time: members.Sum(i => t[i]));
            });

            var comparer = new RateAndInformationValueTools();
            var rows = new List<IncidenceRateGroupRow>();

            foreach (var name in names)
            {
                rows.Add(Row(name, totals[name], totals[reference], name == reference, confidenceLevel, comparer));
            }

            var overall = (Subjects: t.Length, Events: e.Sum(), Time: t.Sum());

            return new IncidenceRatesResult
            {
                Groups = rows.ToArray(),
                ReferenceGroup = reference,
                Overall = Row("Overall", overall, overall, true, confidenceLevel, comparer),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("TabulateHazardRateByInterval")]
        [Description(
            "Estimates a piecewise-constant hazard: the event rate per unit time within consecutive time intervals since the start, for example the chargeback rate in days 0-30, 30-90 and 90 onward. " +
            "Each interval reports the subjects entering, events, censored cases, the person-time at risk inside the interval, the hazard rate (events over person-time) with an exact Poisson confidence interval, and the conditional probability of the event within the interval implied by that constant rate. " +
            "A falling series of rates means early-life risk, a rising one means wear-out. Breaks define intervals between consecutive values, plus a final open-ended interval after the last break (its End is NaN). Rows with a NaN time are dropped.")]
        public HazardIntervalResult TabulateHazardRateByInterval(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description(
                "Strictly increasing interval boundaries starting at 0 or more, for example 0, 30, 90, 180; a final open interval follows the last value.")]
            double[] breaks,
            [Description("Confidence level for the rate intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckBreaks(breaks);
            var rows = new List<HazardIntervalRow>();

            for (var b = 0; b < breaks.Length; b++)
            {
                var start = breaks[b];
                var end = b + 1 < breaks.Length ? breaks[b + 1] : double.PositiveInfinity;
                var entering = t.Count(v => v >= start);
                var eventCount = 0;
                var censored = 0;
                var exposure = 0d;

                for (var i = 0; i < t.Length; i++)
                {
                    if (t[i] > start)
                    {
                        exposure += Math.Min(t[i], end) - start;
                    }

                    if (t[i] >= start && t[i] < end)
                    {
                        if (e[i] == 1)
                        {
                            eventCount++;
                        }
                        else
                        {
                            censored++;
                        }
                    }
                }

                var interval = exposure > 0d
                    ? CategoricalIntervals.PoissonRate(eventCount, exposure, confidenceLevel)
                    : new CategoricalEstimate { Estimate = double.NaN, Lower = double.NaN, Upper = double.NaN };

                rows.Add(new HazardIntervalRow
                {
                    Start = start,
                    End = double.IsInfinity(end) ? double.NaN : end,
                    EnteringSubjects = entering,
                    Events = eventCount,
                    Censored = censored,
                    PersonTime = exposure,
                    HazardRate = interval.Estimate,
                    HazardRateLower = interval.Lower,
                    HazardRateUpper = interval.Upper,
                    ConditionalEventProbability = double.IsInfinity(end) || double.IsNaN(interval.Estimate)
                        ? double.NaN
                        : 1d - Math.Exp(-interval.Estimate * (end - start))
                });
            }

            return new HazardIntervalResult
            {
                Intervals = rows.ToArray(),
                Subjects = t.Length,
                Events = e.Sum(),
                ConfidenceLevel = confidenceLevel,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("BuildActuarialLifeTable")]
        [Description(
            "Builds a classical actuarial (life table) survival analysis over fixed intervals, the natural choice for grouped or large data. " +
            "For each interval it returns the subjects entering, those withdrawn (censored) during it, the events, the effective number at risk (entering minus half the withdrawals), the conditional probability of the event within the interval and its standard error, the cumulative survival at the interval end with a Greenwood standard error, and the midpoint hazard rate. " +
            "Breaks define intervals between consecutive values plus a final open-ended interval after the last break (End is NaN). Unlike Kaplan-Meier it assumes censoring occurs evenly through each interval. Rows with a NaN time are dropped.")]
        public LifeTableResult BuildActuarialLifeTable(
            [Description(
                "Time to event or censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description("Event indicator per case in the same order: 1 if the event was observed, 0 if censored.")]
            int[] events,
            [Description(
                "Strictly increasing interval boundaries starting at 0 or more, for example 0, 12, 24, 36; a final open interval follows the last value.")]
            double[] breaks)
        {
            SurvivalGuards.Clean(times, events, out var t, out var e, out var skipped);
            CheckBreaks(breaks);
            var rows = new List<LifeTableRow>();
            var survival = 1d;
            var greenwood = 0d;

            for (var b = 0; b < breaks.Length; b++)
            {
                var start = breaks[b];
                var end = b + 1 < breaks.Length ? breaks[b + 1] : double.PositiveInfinity;
                var entering = t.Count(v => v >= start);
                var deaths = 0;
                var withdrawn = 0;

                for (var i = 0; i < t.Length; i++)
                {
                    if (t[i] >= start && t[i] < end)
                    {
                        if (e[i] == 1)
                        {
                            deaths++;
                        }
                        else
                        {
                            withdrawn++;
                        }
                    }
                }

                var effective = entering - withdrawn / 2d;
                var q = effective > 0d ? deaths / effective : double.NaN;
                var p = 1d - q;
                survival *= double.IsNaN(q) ? 1d : p;
                greenwood = double.IsNaN(q) || p <= 0d ? double.NaN : greenwood + q / (effective * p);

                rows.Add(new LifeTableRow
                {
                    Start = start,
                    End = double.IsInfinity(end) ? double.NaN : end,
                    Entering = entering,
                    Withdrawn = withdrawn,
                    Events = deaths,
                    EffectiveAtRisk = effective,
                    ConditionalProbabilityOfEvent = q,
                    ConditionalStandardError = double.IsNaN(q) ? double.NaN : Math.Sqrt(q * p / effective),
                    SurvivalAtEnd = survival,
                    SurvivalStandardError = survival * Math.Sqrt(greenwood),
                    Hazard = double.IsInfinity(end) || double.IsNaN(q)
                        ? double.NaN
                        : 2d * q / ((end - start) * (1d + p))
                });
            }

            return new LifeTableResult
            {
                Intervals = rows.ToArray(),
                Subjects = t.Length,
                Events = e.Sum(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateCumulativeIncidenceCompetingRisks")]
        [Description(
            "Estimates the cumulative incidence of each event type when several mutually exclusive events compete, using the Aalen-Johansen estimator: the probability that a case has experienced event type k by time t, allowing for the chance it was removed first by another type. " +
            "Use it instead of one minus Kaplan-Meier when a case can end in more than one way, for example fraud confirmed versus account closed. The type codes are 0 for censored and 1, 2, 3 and so on for the competing events; each row gives the number at risk, events of each type, the overall event-free survival and the cumulative incidence per type, and the cumulative incidences plus survival always sum to 1. " +
            "Kaplan-Meier on one type alone would overstate its probability. Rows with a NaN time are dropped; no variance is returned.")]
        public CumulativeIncidenceResult CalculateCumulativeIncidenceCompetingRisks(
            [Description(
                "Time to the first event or to censoring for each case, in one consistent unit, zero or more; NaN rows are dropped.")]
            double[] times,
            [Description(
                "Outcome code per case in the same order: 0 for censored, 1 or more for the type of event that ended follow-up.")]
            int[] eventTypes,
            [Description(
                "Maximum rows to return, thinned evenly keeping the first and last; 0 (default) returns every event time.")]
            int maxRows = 0)
        {
            CategoricalGuards.Required(times, nameof(times));
            CategoricalGuards.Required(eventTypes, nameof(eventTypes));
            CategoricalGuards.SameLength(times.Length, eventTypes.Length, nameof(times), nameof(eventTypes));

            if (maxRows < 0)
            {
                throw new ArgumentException("maxRows cannot be negative; use 0 for all rows.", nameof(maxRows));
            }

            for (var i = 0; i < eventTypes.Length; i++)
            {
                if (eventTypes[i] < 0)
                {
                    throw new ArgumentException(
                        $"eventTypes must be 0 (censored) or a positive code; entry {i} is {eventTypes[i]}.",
                        nameof(eventTypes));
                }
            }

            var keep = Enumerable.Range(0, times.Length).Where(i => !double.IsNaN(times[i])).ToArray();

            if (keep.Length == 0)
            {
                throw new ArgumentException("There are no usable rows: the input is empty or every time is NaN.",
                    nameof(times));
            }

            if (keep.Any(i => double.IsInfinity(times[i]) || times[i] < 0d))
            {
                throw new ArgumentException("times must be finite and not negative.", nameof(times));
            }

            var t = keep.Select(i => times[i]).ToArray();
            var type = keep.Select(i => eventTypes[i]).ToArray();
            var codes = type.Where(v => v > 0).Distinct().OrderBy(v => v).ToArray();

            if (codes.Length == 0)
            {
                throw new ArgumentException("No events were observed, so there is nothing to accumulate.",
                    nameof(eventTypes));
            }

            var order = Enumerable.Range(0, t.Length).OrderBy(i => t[i]).ToArray();
            var atRisk = t.Length;
            var survival = 1d;
            var cumulative = new double[codes.Length];
            var rows = new List<CumulativeIncidenceRow>();
            var position = 0;

            while (position < order.Length)
            {
                var time = t[order[position]];
                var counts = new int[codes.Length];
                var censored = 0;

                while (position < order.Length && Math.Abs(t[order[position]] - time) < 0.0001)
                {
                    var code = type[order[position]];

                    if (code == 0)
                    {
                        censored++;
                    }
                    else
                    {
                        counts[Array.IndexOf(codes, code)]++;
                    }

                    position++;
                }

                var deaths = counts.Sum();

                if (deaths > 0)
                {
                    for (var k = 0; k < codes.Length; k++)
                    {
                        cumulative[k] += survival * counts[k] / atRisk;
                    }

                    var before = atRisk;
                    survival *= 1d - deaths / (double)atRisk;

                    rows.Add(new CumulativeIncidenceRow
                    {
                        Time = time,
                        AtRisk = before,
                        EventsByType = counts,
                        OverallSurvival = survival,
                        CumulativeIncidenceByType = (double[])cumulative.Clone()
                    });
                }

                atRisk -= deaths + censored;
            }

            return new CumulativeIncidenceResult
            {
                EventTypes = codes,
                Rows = Thin(rows.ToArray(), maxRows),
                FinalCumulativeIncidenceByType = cumulative,
                FinalOverallSurvival = survival,
                Subjects = t.Length,
                Censored = type.Count(v => v == 0),
                TotalEventsByType = codes.Select(c => type.Count(v => v == c)).ToArray(),
                SkippedRows = times.Length - keep.Length
            };
        }

        private static IncidenceRateGroupRow Row(string name, (int Subjects, int Events, double Time) group,
            (int Subjects, int Events, double Time) reference, bool isReference, double level,
            RateAndInformationValueTools comparer)
        {
            var interval = group.Time > 0d
                ? CategoricalIntervals.PoissonRate(group.Events, group.Time, level)
                : new CategoricalEstimate { Estimate = double.NaN, Lower = double.NaN, Upper = double.NaN };

            var ratio = double.NaN;
            var lower = double.NaN;
            var upper = double.NaN;
            var p = double.NaN;

            if (group.Time > 0d && reference.Time > 0d && !isReference)
            {
                var comparison = comparer.ComparePoissonRates(group.Events, group.Time, reference.Events,
                    reference.Time,
                    level);

                ratio = comparison.RateRatio;
                lower = comparison.RateRatioLower;
                upper = comparison.RateRatioUpper;
                p = comparison.PValue;
            }

            return new IncidenceRateGroupRow
            {
                Group = name,
                Subjects = group.Subjects,
                Events = group.Events,
                PersonTime = group.Time,
                Rate = interval.Estimate,
                RateLower = interval.Lower,
                RateUpper = interval.Upper,
                RateRatioVersusReference = isReference ? 1d : ratio,
                RateRatioLower = isReference ? 1d : lower,
                RateRatioUpper = isReference ? 1d : upper,
                PValueVersusReference = isReference ? double.NaN : p
            };
        }

        private static void CheckBreaks(double[] breaks)
        {
            CategoricalGuards.Required(breaks, nameof(breaks));

            if (breaks.Length == 0)
            {
                throw new ArgumentException("Give at least one break, for example 0.", nameof(breaks));
            }

            for (var i = 0; i < breaks.Length; i++)
            {
                if (double.IsNaN(breaks[i]) || double.IsInfinity(breaks[i]) || breaks[i] < 0d ||
                    (i > 0 && breaks[i] <= breaks[i - 1]))
                {
                    throw new ArgumentException(
                        "breaks must be finite, not negative and strictly increasing, for example 0, 30, 90.",
                        nameof(breaks));
                }
            }
        }

        private static T[] Thin<T>(T[] rows, int maxRows)
        {
            if (maxRows <= 0 || rows.Length <= maxRows)
            {
                return rows;
            }

            if (maxRows == 1)
            {
                return [rows[0]];
            }

            var picked = new SortedSet<int>();

            for (var k = 0; k < maxRows; k++)
            {
                picked.Add((int)Math.Round(k * (rows.Length - 1d) / (maxRows - 1d)));
            }

            return picked.Select(i => rows[i]).ToArray();
        }
    }
}
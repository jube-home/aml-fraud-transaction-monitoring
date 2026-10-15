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
    public class OddsAndRiskTools
    {
        private static void CheckGroups(double exposedEvents, double exposedNonEvents, double unexposedEvents,
            double unexposedNonEvents)
        {
            TwoByTwoMath.ValidateCells(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                ["exposedEvents", "exposedNonEvents", "unexposedEvents", "unexposedNonEvents"]);

            if (exposedEvents + exposedNonEvents <= 0d)
            {
                throw new ArgumentException(
                    "The exposed group is empty (exposedEvents plus exposedNonEvents is zero), so its risk is undefined.",
                    nameof(exposedEvents));
            }

            if (unexposedEvents + unexposedNonEvents <= 0d)
            {
                throw new ArgumentException(
                    "The unexposed group is empty (unexposedEvents plus unexposedNonEvents is zero), so its risk is undefined.",
                    nameof(unexposedEvents));
            }
        }

        [AdvancedAnalyticsTool("CalculateOddsRatio")]
        [Description(
            "Computes the odds ratio comparing an exposed group with an unexposed group from a 2 by 2 table of counts, with a Woolf logit confidence interval, z statistic and two-sided p-value. " +
            "The odds ratio is (exposedEvents times unexposedNonEvents) divided by (exposedNonEvents times unexposedEvents): above 1 means the event is more likely in the exposed group, below 1 less likely, and an interval containing 1 means no clear difference. " +
            "When any cell is zero and the correction is enabled 0.5 is added to every cell so the ratio stays finite, and the result flags it. For rare events the odds ratio approximates the risk ratio.")]
        public OddsRatioResult CalculateOddsRatio(
            [Description("Count of exposed (flagged, treated or in the segment of interest) cases that had the event.")]
            double exposedEvents,
            [Description("Count of exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Count of unexposed (reference) cases that had the event.")]
            double unexposedEvents,
            [Description("Count of unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("True to add 0.5 to every cell when any cell is zero (Haldane-Anscombe); default true.")]
            bool applyZeroCellCorrection = true)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckGroups(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents);

            return TwoByTwoMath.Odds(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                confidenceLevel, applyZeroCellCorrection);
        }

        [AdvancedAnalyticsTool("CalculateRiskRatio")]
        [Description(
            "Computes the risk ratio (relative risk) of the event in an exposed group against an unexposed group from a 2 by 2 table of counts, with a log-scale confidence interval, z statistic and two-sided p-value. " +
            "The risk ratio is the exposed event rate divided by the unexposed event rate: 2 means twice as likely, 0.5 half as likely, and an interval containing 1 means no clear difference. RelativeRiskReduction is 1 minus the ratio. " +
            "If the exposed or unexposed group has no events and the correction is enabled, 0.5 is added to every cell so the ratio and interval stay finite.")]
        public RiskRatioResult CalculateRiskRatio(
            [Description("Count of exposed (flagged, treated or in the segment of interest) cases that had the event.")]
            double exposedEvents,
            [Description("Count of exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Count of unexposed (reference) cases that had the event.")]
            double unexposedEvents,
            [Description("Count of unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("True to add 0.5 to every cell when a group has no events (default true).")]
            bool applyZeroCellCorrection = true)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckGroups(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents);

            return TwoByTwoMath.Risk(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                confidenceLevel, applyZeroCellCorrection);
        }

        [AdvancedAnalyticsTool("CalculateRiskDifference")]
        [Description(
            "Computes the absolute risk difference (exposed event rate minus unexposed event rate) from a 2 by 2 table of counts, with a Wald interval, a Newcombe-Wilson interval that behaves better for rare events, a pooled z test, and the attributable fractions. " +
            "A positive difference means extra events per exposed case; the interval excluding 0 indicates a real difference. " +
            "AttributableFractionAmongExposed is the share of the exposed group's events that would not have happened without exposure, and PopulationAttributableFraction is the share of all events attributable to exposure, both assuming the exposure is causal and the sample mix represents the population.")]
        public RiskDifferenceResult CalculateRiskDifference(
            [Description("Count of exposed (flagged, treated or in the segment of interest) cases that had the event.")]
            double exposedEvents,
            [Description("Count of exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Count of unexposed (reference) cases that had the event.")]
            double unexposedEvents,
            [Description("Count of unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckGroups(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents);

            return TwoByTwoMath.Difference(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                confidenceLevel);
        }

        [AdvancedAnalyticsTool("CalculateNumberNeededToTreat")]
        [Description(
            "Converts the risk difference between an exposed and an unexposed group into a number needed to treat or harm: how many exposed cases must be handled to see one extra event (positive difference) or to prevent one event (negative difference). " +
            "NumberNeeded is one divided by the absolute risk difference, Direction states which reading applies, and Lower and Upper come from the Newcombe interval on the difference. " +
            "When the difference's interval includes zero the number needed is not bounded above, IntervalSpansInfinity is true and Lower and Upper are NaN. A zero difference gives NaN because no finite number of cases produces an extra event.")]
        public NumberNeededResult CalculateNumberNeededToTreat(
            [Description("Count of exposed (treated or flagged) cases that had the event.")]
            double exposedEvents,
            [Description("Count of exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Count of unexposed (control) cases that had the event.")]
            double unexposedEvents,
            [Description("Count of unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckGroups(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents);

            var difference = TwoByTwoMath.Difference(exposedEvents, exposedNonEvents, unexposedEvents,
                unexposedNonEvents, confidenceLevel);

            var rd = difference.RiskDifference;
            var spans = difference.NewcombeLower <= 0d && difference.NewcombeUpper >= 0d;
            var lower = double.NaN;
            var upper = double.NaN;

            if (!spans)
            {
                var first = 1d / Math.Abs(difference.NewcombeLower);
                var second = 1d / Math.Abs(difference.NewcombeUpper);
                lower = Math.Min(first, second);
                upper = Math.Max(first, second);
            }

            return new NumberNeededResult
            {
                ExposedRisk = difference.ExposedRisk,
                UnexposedRisk = difference.UnexposedRisk,
                RiskDifference = rd,
                NumberNeeded = rd == 0d ? double.NaN : 1d / Math.Abs(rd),
                Direction = rd > 0d
                    ? "number needed to harm: exposed cases handled per one extra event"
                    : rd < 0d
                        ? "number needed to treat: exposed cases handled per one event prevented"
                        : "no difference in risk",
                Lower = lower,
                Upper = upper,
                IntervalSpansInfinity = spans,
                ConfidenceLevel = confidenceLevel
            };
        }

        [AdvancedAnalyticsTool("CalculateYuleAssociation")]
        [Description(
            "Computes Yule's Q and Yule's Y association coefficients for a 2 by 2 table, with the standard error and interval for Q, plus phi and the odds ratio. " +
            "Q equals (ad minus bc) over (ad plus bc) and runs from -1 to +1 (0 is independence, +1 or -1 means one off-diagonal cell is empty); Y is the coefficient of colligation, a gentler version. " +
            "Cells are laid out as exposed events (a), exposed non-events (b), unexposed events (c), unexposed non-events (d). NaN means the coefficient is undefined for the given zeros.")]
        public YuleAssociationResult CalculateYuleAssociation(
            [Description("Cell a: exposed cases that had the event.")]
            double exposedEvents,
            [Description("Cell b: exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Cell c: unexposed cases that had the event.")]
            double unexposedEvents,
            [Description("Cell d: unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the Q interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            TwoByTwoMath.ValidateCells(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                ["exposedEvents", "exposedNonEvents", "unexposedEvents", "unexposedNonEvents"]);

            return TwoByTwoMath.Yule(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                confidenceLevel);
        }

        [AdvancedAnalyticsTool("CalculateDiagnosticLikelihoodRatios")]
        [Description(
            "Computes sensitivity, specificity, predictive values and the likelihood ratios of a binary rule or alert from its confusion counts, each with a log-scale confidence interval, plus the diagnostic odds ratio. " +
            "The positive likelihood ratio (sensitivity over one minus specificity) says how much a flag multiplies the odds of the event; the negative likelihood ratio says how much a non-flag multiplies them (values well below 1 are reassuring). " +
            "PostTestProbabilityAfterPositive and After Negative apply Bayes at the pretest probability you give, or at the sample prevalence when you leave it empty. Zero cells make intervals infinite or NaN.")]
        public DiagnosticLikelihoodRatioResult CalculateDiagnosticLikelihoodRatios(
            [Description("True positives: flagged cases that had the event.")]
            double truePositives,
            [Description("False positives: flagged cases that did not have the event.")]
            double falsePositives,
            [Description("True negatives: unflagged cases that did not have the event.")]
            double trueNegatives,
            [Description("False negatives: unflagged cases that had the event.")]
            double falseNegatives,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "Prior probability of the event to use for the post-test probabilities, between 0 and 1 exclusive; leave empty to use the sample prevalence.")]
            double? pretestProbability = null)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            TwoByTwoMath.ValidateCells(truePositives, falsePositives, trueNegatives, falseNegatives,
                ["truePositives", "falsePositives", "trueNegatives", "falseNegatives"]);

            if (pretestProbability.HasValue && (double.IsNaN(pretestProbability.Value) ||
                                                pretestProbability.Value <= 0d || pretestProbability.Value >= 1d))
            {
                throw new ArgumentException("pretestProbability must be strictly between 0 and 1, or left empty.",
                    nameof(pretestProbability));
            }

            return TwoByTwoMath.Diagnostic(truePositives, falsePositives, trueNegatives, falseNegatives,
                confidenceLevel, pretestProbability);
        }

        [AdvancedAnalyticsTool("AnalyseTwoByTwoTable")]
        [Description(
            "Runs every standard analysis of a 2 by 2 table of exposed and unexposed groups against an event in one call: odds ratio, risk ratio, risk difference with Wald and Newcombe intervals, Yule Q and Y, phi, Pearson chi-square, Yates-corrected chi-square with p-values, and the smallest expected count. " +
            "Use it as the first look at whether a segment, rule or channel is associated with an outcome; then read the odds and risk ratios for direction and size and the intervals for certainty. " +
            "If the smallest expected count is below 5 prefer the exact test from the hypothesis-testing tools over the chi-square p-values.")]
        public TwoByTwoAnalysisResult AnalyseTwoByTwoTable(
            [Description("Count of exposed (flagged, treated or in the segment of interest) cases that had the event.")]
            double exposedEvents,
            [Description("Count of exposed cases that did not have the event.")]
            double exposedNonEvents,
            [Description("Count of unexposed (reference) cases that had the event.")]
            double unexposedEvents,
            [Description("Count of unexposed cases that did not have the event.")]
            double unexposedNonEvents,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True to add 0.5 to every cell for the odds and risk ratios when a cell is zero (default true).")]
            bool applyZeroCellCorrection = true)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CheckGroups(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents);

            if (exposedEvents + unexposedEvents <= 0d || exposedNonEvents + unexposedNonEvents <= 0d)
            {
                throw new ArgumentException(
                    "The event column or the non-event column is empty in both groups, so no association can be measured.",
                    nameof(exposedEvents));
            }

            return TwoByTwoMath.Full(exposedEvents, exposedNonEvents, unexposedEvents, unexposedNonEvents,
                confidenceLevel, applyZeroCellCorrection);
        }

        [AdvancedAnalyticsTool("AnalyseBinaryExposureAndOutcome")]
        [Description(
            "Builds the 2 by 2 table from two parallel 0/1 columns (exposure and outcome, one entry per case) and runs the full two by two analysis: odds ratio, risk ratio, risk difference, Yule coefficients, chi-square tests. " +
            "Use it when you have raw flags, for example 'rule fired' against 'confirmed fraud', rather than pre-counted cells. Exposure 1 is the exposed group and outcome 1 is the event. Any entry that is not 0 or 1 is rejected.")]
        public TwoByTwoAnalysisResult AnalyseBinaryExposureAndOutcome(
            [Description("Exposure flag for each case: 1 for exposed (flagged), 0 for unexposed.")]
            int[] exposure,
            [Description(
                "Outcome flag for each case, same order and length as exposure: 1 for the event, 0 otherwise.")]
            int[] outcome,
            [Description("Confidence level for the intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "True to add 0.5 to every cell for the odds and risk ratios when a cell is zero (default true).")]
            bool applyZeroCellCorrection = true)
        {
            CategoricalGuards.BinaryLabels(exposure, nameof(exposure));
            CategoricalGuards.BinaryLabels(outcome, nameof(outcome));
            CategoricalGuards.SameLength(exposure.Length, outcome.Length, nameof(exposure), nameof(outcome));
            double a = 0;
            double b = 0;
            double c = 0;
            double d = 0;

            for (var i = 0; i < exposure.Length; i++)
            {
                if (exposure[i] == 1)
                {
                    if (outcome[i] == 1)
                    {
                        a++;
                    }
                    else
                    {
                        b++;
                    }
                }
                else if (outcome[i] == 1)
                {
                    c++;
                }
                else
                {
                    d++;
                }
            }

            return AnalyseTwoByTwoTable(a, b, c, d, confidenceLevel, applyZeroCellCorrection);
        }

        [AdvancedAnalyticsTool("CalculateMantelHaenszelOddsRatio")]
        [Description(
            "Pools several 2 by 2 tables (strata, for example one per country, channel or month) into a single Mantel-Haenszel common odds ratio with a Robins-Breslow-Greenland confidence interval, and tests it with the Cochran-Mantel-Haenszel chi-square (plain and continuity corrected). " +
            "It also runs the Breslow-Day test, with Tarone's correction, of whether the odds ratio is the same in every stratum: a small p-value means the association differs between strata and the pooled figure should not be quoted alone. " +
            "Use it to check whether an association survives adjustment for a confounding variable (Simpson's paradox). Strata with fewer than two cases are dropped and counted.")]
        public MantelHaenszelResult CalculateMantelHaenszelOddsRatio(
            [Description(
                "One array of four counts per stratum, in the order exposed events, exposed non-events, unexposed events, unexposed non-events.")]
            double[][] strata,
            [Description(
                "Confidence level for the pooled odds ratio interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CategoricalGuards.Required(strata, nameof(strata));

            if (strata.Length == 0)
            {
                throw new ArgumentException("Supply at least one stratum of four counts.", nameof(strata));
            }

            for (var i = 0; i < strata.Length; i++)
            {
                if (strata[i] == null || strata[i].Length != 4)
                {
                    throw new ArgumentException(
                        $"Stratum {i} must hold exactly four counts: exposed events, exposed non-events, unexposed events, unexposed non-events.",
                        nameof(strata));
                }

                foreach (var count in strata[i])
                {
                    CategoricalGuards.NonNegativeCount(count, nameof(strata));
                }
            }

            var result = TwoByTwoMath.MantelHaenszel(strata, confidenceLevel);

            if (double.IsNaN(result.CommonOddsRatio) || double.IsInfinity(result.CommonOddsRatio))
            {
                throw new ArgumentException(
                    "The pooled odds ratio is undefined because no stratum has both an exposed event and an unexposed non-event or the reverse; supply more data.",
                    nameof(strata));
            }

            return result;
        }

        [AdvancedAnalyticsTool("CalculateBinomialProportionConfidenceInterval")]
        [Description(
            "Computes a confidence interval for a proportion such as an alert rate, hit rate or fraud rate from a count of events out of a number of trials. " +
            "Choose the method: Wilson (default, reliable at small samples and near 0 or 1), ClopperPearson (exact and conservative), AgrestiCoull, Jeffreys (Bayesian equal-tailed) or Wald (simple but poor for rare events). " +
            "Returns the observed proportion with its lower and upper limits, all between 0 and 1. Zero trials return NaN.")]
        public CategoricalEstimate CalculateBinomialProportionConfidenceInterval(
            [Description("Number of successes or events observed, between 0 and the number of trials.")]
            double events,
            [Description("Number of trials or cases observed; must be a whole number of at least 0.")]
            double trials,
            [Description("Confidence level, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description("Interval method: Wilson, ClopperPearson, AgrestiCoull, Wald or Jeffreys. Default Wilson.")]
            ProportionIntervalMethod method = ProportionIntervalMethod.Wilson)
        {
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            CategoricalGuards.NonNegativeCount(events, nameof(events));
            CategoricalGuards.NonNegativeCount(trials, nameof(trials));

            if (events > trials)
            {
                throw new ArgumentException("events cannot exceed trials.", nameof(events));
            }

            return CategoricalIntervals.Proportion(events, trials, confidenceLevel, method);
        }
    }
}
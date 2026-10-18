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

using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class TwoByTwoMath
    {
        public static void ValidateCells(double a, double b, double c, double d, string[] names)
        {
            CategoricalGuards.NonNegativeCount(a, names[0]);
            CategoricalGuards.NonNegativeCount(b, names[1]);
            CategoricalGuards.NonNegativeCount(c, names[2]);
            CategoricalGuards.NonNegativeCount(d, names[3]);

            if (a + b + c + d <= 0d)
            {
                throw new ArgumentException("All four counts are zero, so there is nothing to analyse.", names[0]);
            }
        }

        public static OddsRatioResult Odds(double a, double b, double c, double d, double level, bool correct)
        {
            var applied = correct && (a == 0d || b == 0d || c == 0d || d == 0d);

            if (applied)
            {
                a += 0.5;
                b += 0.5;
                c += 0.5;
                d += 0.5;
            }

            var ratio = a * d / (b * c);
            var logOr = Math.Log(ratio);
            var se = Math.Sqrt(1d / a + 1d / b + 1d / c + 1d / d);
            var z = CategoricalGuards.TwoSidedZ(level);
            var statistic = logOr / se;

            return new OddsRatioResult
            {
                OddsRatio = ratio,
                LogOddsRatio = logOr,
                LogStandardError = se,
                Lower = Math.Exp(logOr - z * se),
                Upper = Math.Exp(logOr + z * se),
                ZStatistic = statistic,
                PValue = double.IsNaN(statistic)
                    ? double.NaN
                    : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic)),
                ZeroCellCorrectionApplied = applied,
                ConfidenceLevel = level
            };
        }

        public static RiskRatioResult Risk(double a, double b, double c, double d, double level, bool correct)
        {
            var exposedRisk = a / (a + b);
            var unexposedRisk = c / (c + d);
            var applied = correct && (a == 0d || c == 0d);

            if (applied)
            {
                a += 0.5;
                b += 0.5;
                c += 0.5;
                d += 0.5;
            }

            var ratio = a / (a + b) / (c / (c + d));
            var logRr = Math.Log(ratio);
            var se = Math.Sqrt(1d / a - 1d / (a + b) + 1d / c - 1d / (c + d));
            var z = CategoricalGuards.TwoSidedZ(level);
            var statistic = logRr / se;

            return new RiskRatioResult
            {
                ExposedRisk = exposedRisk,
                UnexposedRisk = unexposedRisk,
                RiskRatio = ratio,
                LogStandardError = se,
                Lower = Math.Exp(logRr - z * se),
                Upper = Math.Exp(logRr + z * se),
                ZStatistic = statistic,
                PValue = double.IsNaN(statistic)
                    ? double.NaN
                    : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic)),
                RelativeRiskReduction = 1d - ratio,
                ZeroCellCorrectionApplied = applied,
                ConfidenceLevel = level
            };
        }

        public static RiskDifferenceResult Difference(double a, double b, double c, double d, double level)
        {
            var n1 = a + b;
            var n2 = c + d;
            var p1 = a / n1;
            var p2 = c / n2;
            var difference = p1 - p2;
            var se = Math.Sqrt(p1 * (1d - p1) / n1 + p2 * (1d - p2) / n2);
            var z = CategoricalGuards.TwoSidedZ(level);
            var w1 = CategoricalIntervals.Proportion(a, n1, level, ProportionIntervalMethod.Wilson);
            var w2 = CategoricalIntervals.Proportion(c, n2, level, ProportionIntervalMethod.Wilson);

            var newcombeLower =
                difference - Math.Sqrt((p1 - w1.Lower) * (p1 - w1.Lower) + (w2.Upper - p2) * (w2.Upper - p2));

            var newcombeUpper =
                difference + Math.Sqrt((w1.Upper - p1) * (w1.Upper - p1) + (p2 - w2.Lower) * (p2 - w2.Lower));

            var pooled = (a + c) / (n1 + n2);
            var pooledSe = Math.Sqrt(pooled * (1d - pooled) * (1d / n1 + 1d / n2));
            var statistic = pooledSe > 0d ? difference / pooledSe : double.NaN;
            var rr = p2 > 0d ? p1 / p2 : double.NaN;
            var exposedShare = n1 / (n1 + n2);

            return new RiskDifferenceResult
            {
                ExposedRisk = p1,
                UnexposedRisk = p2,
                RiskDifference = difference,
                WaldStandardError = se,
                WaldLower = difference - z * se,
                WaldUpper = difference + z * se,
                NewcombeLower = newcombeLower,
                NewcombeUpper = newcombeUpper,
                ZStatistic = statistic,
                PValue = double.IsNaN(statistic)
                    ? double.NaN
                    : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic)),
                AttributableFractionAmongExposed = double.IsNaN(rr) || rr == 0d ? double.NaN : (rr - 1d) / rr,
                PopulationAttributableFraction = double.IsNaN(rr)
                    ? double.NaN
                    : exposedShare * (rr - 1d) / (1d + exposedShare * (rr - 1d)),
                ConfidenceLevel = level
            };
        }

        public static YuleAssociationResult Yule(double a, double b, double c, double d, double level)
        {
            var ad = a * d;
            var bc = b * c;
            var q = ad + bc > 0d ? (ad - bc) / (ad + bc) : double.NaN;

            var se = a > 0d && b > 0d && c > 0d && d > 0d
                ? 0.5 * (1d - q * q) * Math.Sqrt(1d / a + 1d / b + 1d / c + 1d / d)
                : double.NaN;

            var z = CategoricalGuards.TwoSidedZ(level);

            var y = Math.Sqrt(ad) + Math.Sqrt(bc) > 0d
                ? (Math.Sqrt(ad) - Math.Sqrt(bc)) / (Math.Sqrt(ad) + Math.Sqrt(bc))
                : double.NaN;

            var margins = (a + b) * (c + d) * (a + c) * (b + d);

            return new YuleAssociationResult
            {
                YuleQ = q,
                YuleQStandardError = se,
                YuleQLower = Math.Max(-1d, q - z * se),
                YuleQUpper = Math.Min(1d, q + z * se),
                YuleY = y,
                Phi = margins > 0d ? (ad - bc) / Math.Sqrt(margins) : double.NaN,
                OddsRatio = bc > 0d ? ad / bc : double.NaN
            };
        }

        public static DiagnosticLikelihoodRatioResult Diagnostic(double tp, double fp, double tn, double fn,
            double level, double? pretest)
        {
            var positives = tp + fn;
            var negatives = fp + tn;
            var sensitivity = positives > 0d ? tp / positives : double.NaN;
            var specificity = negatives > 0d ? tn / negatives : double.NaN;
            var z = CategoricalGuards.TwoSidedZ(level);
            var lrPositive = sensitivity / (1d - specificity);
            var lrNegative = (1d - sensitivity) / specificity;
            var seLrPositive = Math.Sqrt(1d / tp - 1d / positives + 1d / fp - 1d / negatives);
            var seLrNegative = Math.Sqrt(1d / fn - 1d / positives + 1d / tn - 1d / negatives);
            var dor = lrPositive / lrNegative;
            var seDor = Math.Sqrt(1d / tp + 1d / fp + 1d / tn + 1d / fn);
            var prevalence = positives / (positives + negatives);
            var pre = pretest ?? prevalence;
            var preOdds = pre / (1d - pre);
            var postPositive = preOdds * lrPositive / (1d + preOdds * lrPositive);
            var postNegative = preOdds * lrNegative / (1d + preOdds * lrNegative);

            return new DiagnosticLikelihoodRatioResult
            {
                Sensitivity = sensitivity,
                Specificity = specificity,
                PositivePredictiveValue = tp + fp > 0d ? tp / (tp + fp) : double.NaN,
                NegativePredictiveValue = tn + fn > 0d ? tn / (tn + fn) : double.NaN,
                Prevalence = prevalence,
                PositiveLikelihoodRatio = lrPositive,
                PositiveLikelihoodRatioLower = Math.Exp(Math.Log(lrPositive) - z * seLrPositive),
                PositiveLikelihoodRatioUpper = Math.Exp(Math.Log(lrPositive) + z * seLrPositive),
                NegativeLikelihoodRatio = lrNegative,
                NegativeLikelihoodRatioLower = Math.Exp(Math.Log(lrNegative) - z * seLrNegative),
                NegativeLikelihoodRatioUpper = Math.Exp(Math.Log(lrNegative) + z * seLrNegative),
                DiagnosticOddsRatio = dor,
                DiagnosticOddsRatioLower = Math.Exp(Math.Log(dor) - z * seDor),
                DiagnosticOddsRatioUpper = Math.Exp(Math.Log(dor) + z * seDor),
                PretestProbability = pre,
                PostTestProbabilityAfterPositive = postPositive,
                PostTestProbabilityAfterNegative = postNegative,
                ConfidenceLevel = level
            };
        }

        public static TwoByTwoAnalysisResult Full(double a, double b, double c, double d, double level, bool correct)
        {
            var table = new[] { new[] { a, b }, new[] { c, d } };
            var association = ContingencyMath.Analyse(table, ["Exposed", "Unexposed"], ["Event", "NonEvent"]);

            return new TwoByTwoAnalysisResult
            {
                ExposedEvents = a,
                ExposedNonEvents = b,
                UnexposedEvents = c,
                UnexposedNonEvents = d,
                Odds = Odds(a, b, c, d, level, correct),
                Risk = Risk(a, b, c, d, level, correct),
                Difference = Difference(a, b, c, d, level),
                Yule = Yule(a, b, c, d, level),
                ChiSquare = association.ChiSquare,
                ChiSquarePValue = association.PValue,
                YatesChiSquare = association.YatesChiSquare,
                YatesPValue = association.YatesPValue,
                SmallestExpectedCount = association.SmallestExpectedCount
            };
        }

        public static MantelHaenszelResult MantelHaenszel(double[][] strata, double level)
        {
            double sumR = 0;
            double sumS = 0;
            double sumPr = 0;
            double sumPsQr = 0;
            double sumQs = 0;
            double sumDeviation = 0;
            double sumVariance = 0;
            var kept = new List<double[]>();
            var dropped = 0;

            foreach (var stratum in strata)
            {
                var a = stratum[0];
                var b = stratum[1];
                var c = stratum[2];
                var d = stratum[3];
                var n = a + b + c + d;

                if (n < 2d)
                {
                    dropped++;
                    continue;
                }

                kept.Add(stratum);
                var p = (a + d) / n;
                var q = (b + c) / n;
                var r = a * d / n;
                var s = b * c / n;
                sumR += r;
                sumS += s;
                sumPr += p * r;
                sumPsQr += p * s + q * r;
                sumQs += q * s;
                var expected = (a + b) * (a + c) / n;
                sumDeviation += a - expected;
                sumVariance += (a + b) * (c + d) * (a + c) * (b + d) / (n * n * (n - 1d));
            }

            if (kept.Count == 0)
            {
                throw new ArgumentException(
                    "No stratum has at least two observations; supply strata with real counts.", "strata");
            }

            var ratio = sumR / sumS;

            var logSe = Math.Sqrt(sumPr / (2d * sumR * sumR) + sumPsQr / (2d * sumR * sumS) +
                                  sumQs / (2d * sumS * sumS));

            var z = CategoricalGuards.TwoSidedZ(level);
            var cmh = sumVariance > 0d ? sumDeviation * sumDeviation / sumVariance : double.NaN;
            var corrected = Math.Max(0d, Math.Abs(sumDeviation) - 0.5);
            var cmhCorrected = sumVariance > 0d ? corrected * corrected / sumVariance : double.NaN;

            double breslowDay = 0;
            double sumDifference = 0;
            double sumBreslowVariance = 0;
            var used = 0;
            var stratumRatios = new List<double>();

            foreach (var stratum in kept)
            {
                var a = stratum[0];
                var b = stratum[1];
                var c = stratum[2];
                var d = stratum[3];
                stratumRatios.Add(b * c > 0d ? a * d / (b * c) : double.NaN);
                var r1 = a + b;
                var r2 = c + d;
                var t1 = a + c;
                var lowBound = Math.Max(0d, t1 - r2);
                var highBound = Math.Min(r1, t1);
                double fitted;

                if (Math.Abs(ratio - 1d) < 1e-12)
                {
                    fitted = r1 * t1 / (r1 + r2);
                }
                else
                {
                    var qa = 1d - ratio;
                    var qb = r2 - t1 + ratio * (r1 + t1);
                    var qc = -ratio * r1 * t1;
                    var discriminant = Math.Sqrt(qb * qb - 4d * qa * qc);
                    var root1 = (-qb + discriminant) / (2d * qa);
                    var root2 = (-qb - discriminant) / (2d * qa);
                    fitted = root1 >= lowBound - 1e-9 && root1 <= highBound + 1e-9 ? root1 : root2;
                }

                var cellVariance = 1d / (1d / fitted + 1d / (r1 - fitted) + 1d / (t1 - fitted) +
                                         1d / (r2 - t1 + fitted));

                if (double.IsNaN(cellVariance) || double.IsInfinity(cellVariance) || cellVariance <= 0d)
                {
                    continue;
                }

                used++;
                breslowDay += (a - fitted) * (a - fitted) / cellVariance;
                sumDifference += a - fitted;
                sumBreslowVariance += cellVariance;
            }

            var df = used - 1;

            var tarone = breslowDay -
                         (sumBreslowVariance > 0d ? sumDifference * sumDifference / sumBreslowVariance : 0d);

            return new MantelHaenszelResult
            {
                StrataUsed = kept.Count,
                StrataDropped = dropped,
                CommonOddsRatio = ratio,
                LogStandardError = logSe,
                Lower = Math.Exp(Math.Log(ratio) - z * logSe),
                Upper = Math.Exp(Math.Log(ratio) + z * logSe),
                CochranMantelHaenszelChiSquare = cmh,
                CochranMantelHaenszelPValue = CategoricalLibrary.ChiSquareSurvival(cmh, 1),
                CochranMantelHaenszelCorrectedChiSquare = cmhCorrected,
                CochranMantelHaenszelCorrectedPValue = CategoricalLibrary.ChiSquareSurvival(cmhCorrected, 1),
                BreslowDayChiSquare = df >= 1 ? breslowDay : double.NaN,
                BreslowDayTaroneChiSquare = df >= 1 ? tarone : double.NaN,
                BreslowDayDegreesOfFreedom = Math.Max(0, df),
                BreslowDayPValue = df >= 1 ? CategoricalLibrary.ChiSquareSurvival(breslowDay, df) : double.NaN,
                BreslowDayTaronePValue = df >= 1 ? CategoricalLibrary.ChiSquareSurvival(tarone, df) : double.NaN,
                StratumOddsRatios = stratumRatios.ToArray(),
                ConfidenceLevel = level
            };
        }
    }
}
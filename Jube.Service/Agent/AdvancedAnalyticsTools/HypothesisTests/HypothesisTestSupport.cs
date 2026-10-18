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

using System.Globalization;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests.Models;
using LibraryConvergenceException = Accord.ConvergenceException;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.HypothesisTests
{
    internal static class HypothesisTestSupport
    {
        internal const double MaximumMagnitude = 1e150;

        internal static string Format(double value)
        {
            return value.ToString("G5", CultureInfo.InvariantCulture);
        }

        internal static string Format(double? value)
        {
            return value.HasValue ? Format(value.Value) : "n/a";
        }

        internal static void RequireAlpha(double alpha, string parameterName = "alpha")
        {
            if (double.IsNaN(alpha) || alpha <= 0 || alpha >= 1)
            {
                throw new ArgumentException(
                    "The significance level must be strictly between 0 and 1, for example 0.05.", parameterName);
            }
        }

        internal static void RequireProbability(double value, string parameterName, bool inclusive = false)
        {
            var valid = !double.IsNaN(value) && (inclusive ? value >= 0 && value <= 1 : value > 0 && value < 1);

            if (!valid)
            {
                throw new ArgumentException(
                    inclusive
                        ? "The value must be a probability between 0 and 1 inclusive."
                        : "The value must be a probability strictly between 0 and 1.",
                    parameterName);
            }
        }

        internal static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException("The value must be a finite number, not NaN or infinity.", parameterName);
            }

            if (Math.Abs(value) > MaximumMagnitude)
            {
                throw new ArgumentException(
                    "The value is larger than 1e150 in magnitude, so its square would overflow. Rescale the data, for example to thousands.",
                    parameterName);
            }
        }

        internal static void RequirePositive(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException("The value must be a finite number greater than zero.", parameterName);
            }

            if (value > MaximumMagnitude)
            {
                throw new ArgumentException(
                    "The value is larger than 1e150, so its square would overflow. Rescale the data, for example to thousands.",
                    parameterName);
            }
        }

        internal static void RequireCount(int value, string parameterName, int minimum)
        {
            if (value < minimum)
            {
                throw new ArgumentException(
                    $"The value must be at least {minimum.ToString(CultureInfo.InvariantCulture)}.", parameterName);
            }
        }

        internal static void RequireValues(double[]? values, string parameterName, int minimumCount)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException(
                    "The sample is empty. Supply the numeric observations as a non-empty array.", parameterName);
            }

            if (values.Length < minimumCount)
            {
                throw new ArgumentException(
                    $"At least {minimumCount.ToString(CultureInfo.InvariantCulture)} observations are needed but {values.Length.ToString(CultureInfo.InvariantCulture)} were supplied.",
                    parameterName);
            }

            if (values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                throw new ArgumentException(
                    "The sample contains NaN or infinity. Remove missing and non-finite observations before calling this tool.",
                    parameterName);
            }

            if (values.Any(v => Math.Abs(v) > MaximumMagnitude))
            {
                throw new ArgumentException(
                    "The sample contains values larger than 1e150 in magnitude, whose squares would overflow. Rescale the data, for example to thousands.",
                    parameterName);
            }
        }

        internal static void RequireVariation(double[] values, string parameterName)
        {
            var first = values[0];

            if (values.All(v => v == first))
            {
                throw new ArgumentException(
                    "Every observation is identical, so the variance is zero and the test statistic is undefined. Check the data or use a different question.",
                    parameterName);
            }
        }

        internal static void RequireSameLength(double[] first, string firstName, double[] second, string secondName)
        {
            if (first.Length != second.Length)
            {
                throw new ArgumentException(
                    $"{firstName} has {first.Length.ToString(CultureInfo.InvariantCulture)} values but {secondName} has {second.Length.ToString(CultureInfo.InvariantCulture)}; paired tests need one value in each array per pair, in the same order.",
                    secondName);
            }
        }

        internal static void RequireGroups(double[][]? groups, string parameterName, int minimumGroups,
            int minimumPerGroup)
        {
            if (groups == null || groups.Length < minimumGroups)
            {
                throw new ArgumentException(
                    $"At least {minimumGroups.ToString(CultureInfo.InvariantCulture)} groups are needed, each an array of observations.",
                    parameterName);
            }

            for (var i = 0; i < groups.Length; i++)
            {
                RequireValues(groups[i], parameterName, minimumPerGroup);
            }
        }

        internal static void RequireTable(int[]?[]? table, string parameterName, int minimumRows, int minimumColumns,
            bool square = false)
        {
            if (table == null || table.Length < minimumRows)
            {
                throw new ArgumentException(
                    $"The table needs at least {minimumRows.ToString(CultureInfo.InvariantCulture)} rows of counts.",
                    parameterName);
            }

            var width = table[0]?.Length ?? 0;

            if (width < minimumColumns)
            {
                throw new ArgumentException(
                    $"The table needs at least {minimumColumns.ToString(CultureInfo.InvariantCulture)} columns of counts.",
                    parameterName);
            }

            if (table.Any(r => r == null || r.Length != width))
            {
                throw new ArgumentException("Every row of the table must have the same number of columns.",
                    parameterName);
            }

            if (table.Any(r => r!.Any(c => c < 0)))
            {
                throw new ArgumentException("Counts must not be negative.", parameterName);
            }

            if (square && table.Length != width)
            {
                throw new ArgumentException(
                    "The table must be square: the rows and columns are the same categories rated by two raters or measured twice.",
                    parameterName);
            }

            var total = table.Sum(r => r!.Sum(c => (long)c));

            if (total == 0)
            {
                throw new ArgumentException("The table contains no observations; every count is zero.", parameterName);
            }

            if (total > int.MaxValue)
            {
                throw new ArgumentException(
                    "The counts total more than 2 billion, which the test cannot represent. Divide the counts by a common factor.",
                    parameterName);
            }
        }

        internal static T Guard<T>(Func<T> calculation, string parameterName)
        {
            try
            {
                return calculation();
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArithmeticException
                                                  or LibraryConvergenceException)
            {
                throw new ArgumentException(
                    "The values are so extreme that the test cannot be evaluated numerically. Rescale the data or check for a unit error.",
                    parameterName);
            }
        }

        internal static int[,] ToRectangular(int[][] table)
        {
            var result = new int[table.Length, table[0].Length];

            for (var i = 0; i < table.Length; i++)
            {
                for (var j = 0; j < table[i].Length; j++)
                {
                    result[i, j] = table[i][j];
                }
            }

            return result;
        }

        internal static OneSampleHypothesis ToOneSample(HypothesisTail tail)
        {
            return tail switch
            {
                HypothesisTail.Greater => OneSampleHypothesis.ValueIsGreaterThanHypothesis,
                HypothesisTail.Less => OneSampleHypothesis.ValueIsSmallerThanHypothesis,
                _ => OneSampleHypothesis.ValueIsDifferentFromHypothesis
            };
        }

        internal static TwoSampleHypothesis ToTwoSample(HypothesisTail tail)
        {
            return tail switch
            {
                HypothesisTail.Greater => TwoSampleHypothesis.FirstValueIsGreaterThanSecond,
                HypothesisTail.Less => TwoSampleHypothesis.FirstValueIsSmallerThanSecond,
                _ => TwoSampleHypothesis.ValuesAreDifferent
            };
        }

        internal static KolmogorovSmirnovTestHypothesis ToKolmogorovSmirnov(HypothesisTail tail)
        {
            return tail switch
            {
                HypothesisTail.Greater => KolmogorovSmirnovTestHypothesis.SampleIsGreater,
                HypothesisTail.Less => KolmogorovSmirnovTestHypothesis.SampleIsSmaller,
                _ => KolmogorovSmirnovTestHypothesis.SampleIsDifferent
            };
        }

        internal static TwoSampleKolmogorovSmirnovTestHypothesis ToTwoSampleKolmogorovSmirnov(HypothesisTail tail)
        {
            return tail switch
            {
                HypothesisTail.Greater => TwoSampleKolmogorovSmirnovTestHypothesis.FirstSampleIsLargerThanSecond,
                HypothesisTail.Less => TwoSampleKolmogorovSmirnovTestHypothesis.FirstSampleIsSmallerThanSecond,
                _ => TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal
            };
        }

        internal static HypothesisTail FromTail(DistributionTail tail)
        {
            return tail switch
            {
                DistributionTail.OneUpper => HypothesisTail.Greater,
                DistributionTail.OneLower => HypothesisTail.Less,
                _ => HypothesisTail.TwoSided
            };
        }

        internal static double[] Sorted(double[] values)
        {
            var copy = (double[])values.Clone();
            Array.Sort(copy);

            return copy;
        }

        internal static HypothesisTestResult Build(
            string testName,
            string statisticName,
            double statistic,
            double pValue,
            HypothesisTail tail,
            double alpha,
            string nullText,
            string alternativeText,
            double? degreesOfFreedom = null,
            double? degreesOfFreedom2 = null,
            string detail = "",
            IEnumerable<string>? warnings = null)
        {
            var rejected = !double.IsNaN(pValue) && pValue < alpha;

            var dfText = string.Empty;

            if (degreesOfFreedom.HasValue && degreesOfFreedom2.HasValue)
            {
                dfText = $" with {Format(degreesOfFreedom)} and {Format(degreesOfFreedom2)} degrees of freedom";
            }
            else if (degreesOfFreedom.HasValue)
            {
                dfText = $" with {Format(degreesOfFreedom)} degrees of freedom";
            }

            string sentence;

            if (double.IsNaN(pValue))
            {
                sentence =
                    $"{testName}: {statisticName} = {Format(statistic)}{dfText}, but the p-value could not be computed, so no conclusion is drawn.";
            }
            else
            {
                var decision = rejected
                    ? $"reject the null hypothesis ({nullText}); the data support that {alternativeText}"
                    : $"fail to reject the null hypothesis ({nullText}); there is not enough evidence that {alternativeText}";

                sentence =
                    $"{testName}: {statisticName} = {Format(statistic)}{dfText}, p = {Format(pValue)}. At alpha = {Format(alpha)} {decision}.";
            }

            if (!string.IsNullOrEmpty(detail))
            {
                sentence += " " + detail;
            }

            return new HypothesisTestResult
            {
                TestName = testName,
                StatisticName = statisticName,
                Statistic = statistic,
                PValue = pValue,
                DegreesOfFreedom = degreesOfFreedom,
                DegreesOfFreedom2 = degreesOfFreedom2,
                Tail = tail,
                Alpha = alpha,
                RejectNull = rejected,
                Warnings = warnings?.ToArray() ?? Array.Empty<string>(),
                Interpretation = sentence
            };
        }

        internal static string TailPhrase(HypothesisTail tail, string subject, string twoSided, string greater,
            string less, string reference)
        {
            var relation = tail switch
            {
                HypothesisTail.Greater => greater,
                HypothesisTail.Less => less,
                _ => twoSided
            };

            return string.IsNullOrEmpty(reference) ? $"{subject} {relation}" : $"{subject} {relation} {reference}";
        }

        internal static void RequireSuccesses(int successes, int trials, string parameterName)
        {
            if (successes < 0 || successes > trials)
            {
                throw new ArgumentException(
                    $"The number of successes must be between 0 and the number of trials ({trials.ToString(CultureInfo.InvariantCulture)}).",
                    parameterName);
            }
        }

        internal static HypothesisIntervalResult ProportionInterval(int successes, int trials, double level,
            HypothesisProportionIntervalMethod method)
        {
            var n = (double)trials;
            var p = successes / n;
            var tailArea = (1 - level) / 2;
            var z = NormalDistribution.Standard.InverseDistributionFunction(1 - tailArea);
            double lower;
            double upper;

            switch (method)
            {
                case HypothesisProportionIntervalMethod.Wald:
                    lower = p - z * Math.Sqrt(p * (1 - p) / n);
                    upper = p + z * Math.Sqrt(p * (1 - p) / n);
                    break;
                case HypothesisProportionIntervalMethod.AgrestiCoull:
                    var nTilde = n + z * z;
                    var pTilde = (successes + z * z / 2) / nTilde;
                    var margin = z * Math.Sqrt(pTilde * (1 - pTilde) / nTilde);
                    lower = pTilde - margin;
                    upper = pTilde + margin;
                    break;
                case HypothesisProportionIntervalMethod.ClopperPearson:
                    lower = successes == 0
                        ? 0
                        : new BetaDistribution(successes, trials - successes + 1.0)
                            .InverseDistributionFunction(tailArea);

                    upper = successes == trials
                        ? 1
                        : new BetaDistribution(successes + 1.0, trials - successes)
                            .InverseDistributionFunction(1 - tailArea);

                    break;
                default:
                    var centre = (p + z * z / (2 * n)) / (1 + z * z / n);
                    var half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / (1 + z * z / n);
                    lower = centre - half;
                    upper = centre + half;
                    break;
            }

            lower = Math.Max(0, lower);
            upper = Math.Min(1, upper);

            return new HypothesisIntervalResult
            {
                Quantity = "proportion",
                Method = method.ToString(),
                Estimate = p,
                Lower = lower,
                Upper = upper,
                ConfidenceLevel = level,
                SampleSize = trials
            };
        }

        internal static string Magnitude(double effect)
        {
            var size = Math.Abs(effect);

            if (double.IsNaN(size))
            {
                return "undefined";
            }

            if (size < 0.2)
            {
                return "negligible";
            }

            if (size < 0.5)
            {
                return "small";
            }

            return size < 0.8 ? "medium" : "large";
        }
    }
}
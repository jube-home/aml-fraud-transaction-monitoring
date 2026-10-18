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
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum SeasonalModel
    {
        Additive,
        Multiplicative
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SeriesDependenceTools
    {
        [AdvancedAnalyticsTool("AutocorrelationFunction")]
        [Description(
            "Sample autocorrelation function (ACF) of an evenly spaced series: the correlation of the series with itself shifted by 0, 1, 2 ... maxLag steps. Use it to see whether today's value depends on earlier values, to spot repeating cycles (a peak at lag 7 in daily data means a weekly pattern) and to check whether residuals are white noise. " +
            "Values[k] is the autocorrelation at lag k (Values[0] is always 1). WhiteNoiseBound is the plus or minus band outside which a lag is significantly different from zero for pure noise; BartlettBounds gives a wider lag-specific band that allows for the correlation already seen at shorter lags; SignificantLags lists lags of 1 or more beyond WhiteNoiseBound. " +
            "The series must be finite and in time order with at least 3 values. A constant series has no defined correlation and returns NaN for every lag above 0.")]
        public AutocorrelationResult AutocorrelationFunction(
            [Description("The series in time order, evenly spaced, with no missing values.")]
            double[] values,
            [Description("Largest lag to report. Default 20; it is reduced to the series length minus 1 when longer.")]
            int maxLag = 20,
            [Description("Confidence level for the significance bands, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(maxLag, 1, nameof(maxLag));
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            var acf = TimeSeriesGuard.Autocorrelations(v, maxLag);
            var n = v.Length;
            var bound = z / Math.Sqrt(n);
            var bartlett = new double[acf.Length];
            var cumulative = 0.0;

            for (var k = 0; k < acf.Length; k++)
            {
                bartlett[k] = k == 0 ? 0 : z * Math.Sqrt((1 + 2 * cumulative) / n);

                if (k >= 1 && !double.IsNaN(acf[k]))
                {
                    cumulative += acf[k] * acf[k];
                }
            }

            var significant = Enumerable.Range(1, acf.Length - 1)
                .Where(k => !double.IsNaN(acf[k]) && Math.Abs(acf[k]) > bound)
                .ToArray();

            return new AutocorrelationResult(
                Enumerable.Range(0, acf.Length).ToArray(),
                acf,
                bound,
                bartlett,
                significant,
                n,
                confidenceLevel,
                TimeSeriesGuard.IsConstant(v)
                    ? "The series is constant so autocorrelation above lag 0 is undefined (NaN)."
                    : "Biased (divide by n) estimator, the standard convention for correlograms.");
        }

        [AdvancedAnalyticsTool("PartialAutocorrelationFunction")]
        [Description(
            "Partial autocorrelation function (PACF) of an evenly spaced series: the correlation between the series and its value k steps earlier after removing the effect of the shorter lags in between. Use it to choose the order of an autoregressive model: for an AR(p) process the PACF cuts off to about zero after lag p, whereas the ACF tails off. " +
            "Values[k-1] is the partial autocorrelation at lag k (lag 0 is not reported); Lags gives the matching lag numbers; WhiteNoiseBound is the plus or minus band beyond which a lag is significant; SignificantLags lists them. " +
            "Computed with the Durbin-Levinson recursion from the sample autocorrelations. Needs at least 4 finite values; a constant series returns NaN.")]
        public AutocorrelationResult PartialAutocorrelationFunction(
            [Description("The series in time order, evenly spaced, with no missing values.")]
            double[] values,
            [Description("Largest lag to report. Default 20; reduced to the series length minus 1 when longer.")]
            int maxLag = 20,
            [Description("Confidence level for the significance band, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.AtLeast(maxLag, 1, nameof(maxLag));
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            var acf = TimeSeriesGuard.Autocorrelations(v, maxLag);
            var pacf = DurbinLevinsonPartials(acf);
            var n = v.Length;
            var bound = z / Math.Sqrt(n);

            var significant = Enumerable.Range(1, pacf.Length)
                .Where(k => !double.IsNaN(pacf[k - 1]) && Math.Abs(pacf[k - 1]) > bound)
                .ToArray();

            return new AutocorrelationResult(
                Enumerable.Range(1, pacf.Length).ToArray(),
                pacf,
                bound,
                Enumerable.Repeat(bound, pacf.Length).ToArray(),
                significant,
                n,
                confidenceLevel,
                "Values start at lag 1.");
        }

        [AdvancedAnalyticsTool("CrossCorrelationFunction")]
        [Description(
            "Cross-correlation between two evenly spaced series of the same length at lags from minus maxLag to plus maxLag. Use it to find whether one series leads another (for example card-present spend leading online spend) and by how many steps. " +
            "Correlations[i] belongs to Lags[i]; a positive lag k is the correlation of x at time t with y at time t+k, so a large value at a positive lag means x leads y by k steps and a large value at a negative lag means y leads x. " +
            "BestLag and BestCorrelation report the lag with the largest absolute correlation; Bound is the plus or minus band for no relationship and SignificantLags lists lags beyond it. " +
            "Both series must be finite and the same length; a constant series has no correlation and returns NaN.")]
        public CrossCorrelationResult CrossCorrelationFunction(
            [Description("The first series (the one that may lead), in time order.")]
            double[] x,
            [Description("The second series, the same length and time grid as x.")]
            double[] y,
            [Description(
                "Largest lag in either direction. Default 10; reduced to the series length minus 1 when longer.")]
            int maxLag = 10,
            [Description("Confidence level for the significance band, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var a = TimeSeriesGuard.Finite(x, nameof(x), 3);
            var b = TimeSeriesGuard.Finite(y, nameof(y), 3);
            TimeSeriesGuard.SameLength(a, b, nameof(x), nameof(y));
            TimeSeriesGuard.AtLeast(maxLag, 1, nameof(maxLag));
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            var n = a.Length;
            var limit = Math.Min(maxLag, n - 1);
            var mx = TimeSeriesGuard.Mean(a);
            var my = TimeSeriesGuard.Mean(b);
            var sx = Math.Sqrt(a.Sum(v => (v - mx) * (v - mx)) / n);
            var sy = Math.Sqrt(b.Sum(v => (v - my) * (v - my)) / n);
            var lags = Enumerable.Range(-limit, 2 * limit + 1).ToArray();
            var correlations = new double[lags.Length];

            for (var i = 0; i < lags.Length; i++)
            {
                var k = lags[i];
                var sum = 0.0;

                for (var t = Math.Max(0, -k); t < Math.Min(n, n - k); t++)
                {
                    sum += (a[t] - mx) * (b[t + k] - my);
                }

                correlations[i] = sx == 0 || sy == 0 ? double.NaN : sum / (n * sx * sy);
            }

            var bound = z / Math.Sqrt(n);
            var bestIndex = -1;

            for (var i = 0; i < correlations.Length; i++)
            {
                if (!double.IsNaN(correlations[i]) &&
                    (bestIndex < 0 || Math.Abs(correlations[i]) > Math.Abs(correlations[bestIndex])))
                {
                    bestIndex = i;
                }
            }

            return new CrossCorrelationResult(
                lags,
                correlations,
                bound,
                bestIndex < 0 ? 0 : lags[bestIndex],
                bestIndex < 0 ? double.NaN : correlations[bestIndex],
                lags.Where((_, i) => !double.IsNaN(correlations[i]) && Math.Abs(correlations[i]) > bound).ToArray(),
                n,
                bestIndex < 0
                    ? "One of the series is constant so the correlation is undefined."
                    : "Positive lag: y follows x. Negative lag: x follows y.");
        }

        [AdvancedAnalyticsTool("LjungBoxAutocorrelationTest")]
        [Description(
            "Ljung-Box portmanteau test of the null hypothesis that a series (or a model's residuals) has no autocorrelation up to a chosen lag. Use it to check that residuals of a fitted or forecasting model are white noise, or to test whether a raw series is predictable from its own past. " +
            "Statistic is Q = n(n+2) times the sum over lags of r-squared divided by (n minus lag); under the null it follows a chi-square distribution with DegreesOfFreedom = lags minus modelDegreesOfFreedom. A small PValue (below your significance level) means autocorrelation is present (RejectsNoAutocorrelation true). " +
            "StatisticByLag and PValueByLag give the running test at every lag from 1 (NaN when degrees of freedom would be 0 or less). Set modelDegreesOfFreedom to the number of fitted ARMA parameters when testing residuals. Requires finite values and lags smaller than the series length. Use BoxPierceAutocorrelationTest for the older, less accurate variant.")]
        public PortmanteauTestResult LjungBoxAutocorrelationTest(
            [Description("The series or model residuals in time order, evenly spaced, no missing values.")]
            double[] values,
            [Description("Number of lags included in the test. Default 10; must be smaller than the series length.")]
            int lags = 10,
            [Description(
                "Number of model parameters already fitted, subtracted from the degrees of freedom. Default 0 for a raw series.")]
            int modelDegreesOfFreedom = 0,
            [Description("Significance level for the RejectsNoAutocorrelation flag. Default 0.05.")]
            double alpha = 0.05)
        {
            return Portmanteau(values, lags, modelDegreesOfFreedom, alpha, true);
        }

        [AdvancedAnalyticsTool("BoxPierceAutocorrelationTest")]
        [Description(
            "Box-Pierce portmanteau test of the null hypothesis that a series has no autocorrelation up to a chosen lag. It is the original form of the Ljung-Box test with Q = n times the sum of squared autocorrelations; it is less accurate in short series, so prefer LjungBoxAutocorrelationTest unless you need this exact statistic. " +
            "Statistic follows a chi-square distribution with DegreesOfFreedom = lags minus modelDegreesOfFreedom under the null; a small PValue means autocorrelation is present. StatisticByLag and PValueByLag give the running test at each lag from 1. " +
            "Requires finite values and lags smaller than the series length.")]
        public PortmanteauTestResult BoxPierceAutocorrelationTest(
            [Description("The series or model residuals in time order, evenly spaced, no missing values.")]
            double[] values,
            [Description("Number of lags included in the test. Default 10; must be smaller than the series length.")]
            int lags = 10,
            [Description(
                "Number of model parameters already fitted, subtracted from the degrees of freedom. Default 0.")]
            int modelDegreesOfFreedom = 0,
            [Description("Significance level for the RejectsNoAutocorrelation flag. Default 0.05.")]
            double alpha = 0.05)
        {
            return Portmanteau(values, lags, modelDegreesOfFreedom, alpha, false);
        }

        [AdvancedAnalyticsTool("DurbinWatsonResidualAutocorrelation")]
        [Description(
            "Durbin-Watson statistic for first-order autocorrelation in an ordered series of residuals: the sum of squared successive differences divided by the sum of squared values. Use it on regression or forecasting residuals to see whether neighbouring errors are correlated. " +
            "Statistic is near 2 when there is no first-order autocorrelation, well below 2 (toward 0) for positive autocorrelation and well above 2 (toward 4) for negative autocorrelation. ImpliedFirstOrderAutocorrelation is approximately 1 minus Statistic divided by 2. Reading is a plain-language label using cut-offs of 1.5 and 2.5. " +
            "Residuals are not centred, which is the textbook definition; supply model residuals rather than a raw series. Returns NaN when every residual is zero.")]
        public DurbinWatsonResult DurbinWatsonResidualAutocorrelation(
            [Description("Model residuals in time order, finite, at least 3 values.")]
            double[] residuals)
        {
            var e = TimeSeriesGuard.Finite(residuals, nameof(residuals), 3);
            var numerator = 0.0;
            var denominator = 0.0;

            for (var i = 0; i < e.Length; i++)
            {
                denominator += e[i] * e[i];

                if (i > 0)
                {
                    numerator += (e[i] - e[i - 1]) * (e[i] - e[i - 1]);
                }
            }

            var d = denominator == 0 ? double.NaN : numerator / denominator;

            var reading = double.IsNaN(d)
                ? "Undefined because every residual is zero."
                : d < 1.5
                    ? "Positive first-order autocorrelation is likely."
                    : d > 2.5
                        ? "Negative first-order autocorrelation is likely."
                        : "No strong first-order autocorrelation.";

            return new DurbinWatsonResult(d, double.IsNaN(d) ? double.NaN : 1 - d / 2, reading, e.Length);
        }

        [AdvancedAnalyticsTool("TurningPointRandomnessTest")]
        [Description(
            "Turning point test of randomness for a series: counts local peaks and troughs and compares them with the count expected if the values were independent, which is 2(n-2)/3 with variance (16n-29)/90. Use it as a quick, distribution-free check that a series (or residuals) is random, without choosing a lag. " +
            "Statistic is the number of turning points, ExpectedUnderRandomness and StandardError are the null mean and standard deviation, ZScore their standardised difference and PValue the two-sided normal-approximation probability. Too few turning points means smooth persistence or trend; too many means alternating (oscillating) behaviour. " +
            "Ties are not turning points. Needs at least 4 finite values; the normal approximation is best from about 10 values upward.")]
        public RandomnessTestResult TurningPointRandomnessTest(
            [Description("The series in time order, finite, at least 4 values.")]
            double[] values,
            [Description("Significance level for the RejectsRandomness flag. Default 0.05.")]
            double alpha = 0.05)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);
            var n = v.Length;
            var turning = 0;

            for (var i = 1; i < n - 1; i++)
            {
                if ((v[i] - v[i - 1]) * (v[i] - v[i + 1]) > 0)
                {
                    turning++;
                }
            }

            var expected = 2.0 * (n - 2) / 3.0;
            var se = Math.Sqrt((16.0 * n - 29) / 90.0);
            var z = (turning - expected) / se;
            var p = TimeSeriesGuard.TwoSidedNormalPValue(z);

            return new RandomnessTestResult("Turning point", turning, expected, se, z, p, p < alpha, n,
                "Two-sided normal approximation.");
        }

        [AdvancedAnalyticsTool("MedianRunsRandomnessTest")]
        [Description(
            "Wald-Wolfowitz runs test of randomness on the sequence of values above and below the series median. A run is an unbroken stretch on one side of the median. Use it to detect trends, clustering (long runs, too few runs) or alternation (too many runs) without assuming any distribution. " +
            "Statistic is the number of runs, ExpectedUnderRandomness and StandardError the null mean and standard deviation, ZScore the standardised difference and PValue the two-sided normal approximation. A large negative ZScore means values cluster or trend; a large positive ZScore means they alternate. " +
            "Values equal to the median are ignored. Needs at least one value strictly above and one strictly below the median.")]
        public RandomnessTestResult MedianRunsRandomnessTest(
            [Description("The series in time order, finite, at least 4 values.")]
            double[] values,
            [Description("Significance level for the RejectsRandomness flag. Default 0.05.")]
            double alpha = 0.05)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);
            var median = TimeSeriesGuard.Median(v);
            var signs = v.Where(x => x != median).Select(x => x > median).ToArray();
            var above = signs.Count(s => s);
            var below = signs.Length - above;

            if (above == 0 || below == 0)
            {
                throw new ArgumentException(
                    "The series has no values on one side of its median (it is constant or nearly so), so a runs test is undefined. Supply a series with variation.",
                    nameof(values));
            }

            var runs = 1;

            for (var i = 1; i < signs.Length; i++)
            {
                if (signs[i] != signs[i - 1])
                {
                    runs++;
                }
            }

            var n = (double)(above + below);
            var expected = 2.0 * above * below / n + 1;
            var variance = 2.0 * above * below * (2.0 * above * below - n) / (n * n * (n - 1));
            var se = Math.Sqrt(variance);
            var z = se == 0 ? double.NaN : (runs - expected) / se;
            var p = TimeSeriesGuard.TwoSidedNormalPValue(z);

            return new RandomnessTestResult("Runs above and below the median", runs, expected, se, z, p,
                !double.IsNaN(p) && p < alpha, v.Length,
                $"{above} values above and {below} below the median {median}.");
        }

        [AdvancedAnalyticsTool("MannKendallTrendTest")]
        [Description(
            "Mann-Kendall non-parametric test for a monotonic trend in a series, together with the Theil-Sen slope. Use it to decide whether a metric (daily volume, average amount, risk score) is drifting up or down over time without assuming a straight line or normal errors. " +
            "S is the sum of the signs of all later-minus-earlier differences (positive means upward); ZScore uses the tie-corrected variance with continuity correction and PValue is two-sided. KendallTau is S scaled to the range -1 to 1. SenSlopePerStep is the median of all pairwise slopes, a robust estimate of change per time step, and SenIntercept the median intercept. " +
            "Trend reads Increasing, Decreasing or NoTrend at the chosen alpha. Assumes independent observations (serial correlation inflates significance). Requires 3 to 4000 finite values.")]
        public MannKendallResult MannKendallTrendTest(
            [Description("The series in time order, finite, between 3 and 4000 values.")]
            double[] values,
            [Description("Significance level used for the Trend label. Default 0.05.")]
            double alpha = 0.05)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);

            if (v.Length > 4000)
            {
                throw new ArgumentException(
                    $"values has {v.Length} values; the trend test is limited to 4000. Aggregate the series to a coarser time step first (for example with ResampleEventsToBuckets).",
                    nameof(values));
            }

            var n = v.Length;
            long s = 0;
            var slopes = new List<double>(n * (n - 1) / 2);

            for (var i = 0; i < n - 1; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    s += Math.Sign(v[j] - v[i]);
                    slopes.Add((v[j] - v[i]) / (j - i));
                }
            }

            var tieTerm = v.GroupBy(x => x).Select(g => (double)g.Count()).Where(t => t > 1)
                .Sum(t => t * (t - 1) * (2 * t + 5));

            var variance = (n * (n - 1.0) * (2.0 * n + 5) - tieTerm) / 18.0;

            var z = variance <= 0 ? double.NaN :
                s > 0 ? (s - 1) / Math.Sqrt(variance) :
                s < 0 ? (s + 1) / Math.Sqrt(variance) : 0;

            var p = TimeSeriesGuard.TwoSidedNormalPValue(z);
            var slope = TimeSeriesGuard.Median(slopes);
            var intercept = TimeSeriesGuard.Median(v.Select((x, i) => x - slope * i).ToArray());
            var pairs = n * (n - 1.0) / 2.0;
            var tau = s / pairs;
            var trend = double.IsNaN(p) || p >= alpha ? "NoTrend" : s > 0 ? "Increasing" : "Decreasing";

            return new MannKendallResult(s, variance, z, p, tau, slope, intercept, trend, n);
        }

        [AdvancedAnalyticsTool("ClassicalSeasonalDecomposition")]
        [Description(
            "Classical decomposition of an evenly spaced series into trend, repeating seasonal pattern and remainder. Use it when a metric has a known cycle (period 7 for daily data with a weekly pattern, 24 for hourly data with a daily pattern, 12 for monthly data) and you want to see the underlying trend or the residual noise. " +
            "The trend is a centred moving average over one full period (NaN for the first and last half period). SeasonalIndices has one value per position in the cycle, position 0 being the first observation; Seasonal repeats them along the series. Additive model: value = trend + seasonal + remainder, with indices summing to 0. Multiplicative model: value = trend x seasonal x remainder, with indices averaging 1 (all values must then be positive). " +
            "SeasonalStrength and TrendStrength are between 0 and 1 (1 minus the ratio of remainder variance to the variance of remainder plus that component; close to 1 means a strong component, close to 0 none). Needs at least two full periods of finite data.")]
        public SeasonalDecompositionResult ClassicalSeasonalDecomposition(
            [Description("The series in time order, evenly spaced, finite, at least two full periods long.")]
            double[] values,
            [Description(
                "Number of observations in one cycle, at least 2, for example 7 for daily data with a weekly pattern.")]
            int period,
            [Description(
                "Additive when the seasonal swing is a constant amount; Multiplicative when it grows with the level (needs all values above 0).")]
            SeasonalModel model = SeasonalModel.Additive)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.AtLeast(period, 2, nameof(period));

            if (v.Length < 2 * period)
            {
                throw new ArgumentException(
                    $"values has {v.Length} observations but two full periods of {period} need at least {2 * period}. Supply more history or a smaller period.",
                    nameof(period));
            }

            if (model == SeasonalModel.Multiplicative && v.Any(x => x <= 0))
            {
                throw new ArgumentException(
                    "A multiplicative model needs every value above 0. Use the Additive model or shift the series.",
                    nameof(model));
            }

            return Decompose(v, period, model);
        }

        [AdvancedAnalyticsTool("SeasonalityStrengthScore")]
        [Description(
            "Scores how strong the seasonal cycle and the trend are in a series, each between 0 and 1, using a classical decomposition at the given period. Use it to decide whether a seasonal forecasting method or seasonal adjustment is worthwhile: above about 0.6 is a strong cycle, below about 0.3 a weak one. " +
            "SeasonalStrength is 1 minus the variance of the remainder over the variance of seasonal plus remainder; TrendStrength is defined the same way for the trend. Reading summarises the seasonal strength in words. " +
            "Needs at least two full periods of finite data. Returns NaN strengths when the de-trended series is constant.")]
        public SeasonalityStrengthResult SeasonalityStrengthScore(
            [Description("The series in time order, evenly spaced, finite, at least two full periods long.")]
            double[] values,
            [Description("Number of observations in one cycle, at least 2.")]
            int period,
            [Description(
                "Additive when the seasonal swing is constant; Multiplicative when it grows with the level (all values above 0).")]
            SeasonalModel model = SeasonalModel.Additive)
        {
            var result = ClassicalSeasonalDecomposition(values, period, model);
            var s = result.SeasonalStrength;

            var reading = double.IsNaN(s) ? "Undefined for this series." :
                s >= 0.6 ? "Strong seasonality." :
                s >= 0.3 ? "Moderate seasonality." : "Weak or no seasonality.";

            return new SeasonalityStrengthResult(s, result.TrendStrength, reading, period);
        }

        [AdvancedAnalyticsTool("EstimateSeasonalPeriodByAutocorrelation")]
        [Description(
            "Estimates the length of the repeating cycle in an evenly spaced series from the peaks of its autocorrelation function. Use it when you do not know the period (for example whether activity repeats weekly or every 30 days). " +
            "The series is optionally de-trended with a straight line first so that a trend does not mask the cycle. Period is the lag of the highest local autocorrelation peak that is above the white-noise Bound, no longer than maxPeriod; CandidatePeriods and CandidateAutocorrelations list every such peak. Found is false (Period 0) when no significant peak exists. " +
            "The answer is in observations, not time units. Needs at least 8 finite values; for an epoch-second event stream, resample to a regular grid first.")]
        public SeasonalPeriodEstimate EstimateSeasonalPeriodByAutocorrelation(
            [Description("The series in time order, evenly spaced, finite, at least 8 values.")]
            double[] values,
            [Description("Longest period considered, in observations. Default 0 means half the series length.")]
            int maxPeriod = 0,
            [Description("True (default) removes a fitted straight line before measuring autocorrelation.")]
            bool detrend = true)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 8);
            var limit = maxPeriod <= 0 ? v.Length / 2 : Math.Min(maxPeriod, v.Length / 2);
            var work = v;

            if (detrend)
            {
                TimeSeriesGuard.LinearFit(v, out var intercept, out var slope);
                work = v.Select((x, i) => x - (intercept + slope * i)).ToArray();
            }

            var acf = TimeSeriesGuard.Autocorrelations(work, limit + 1);
            var bound = 1.96 / Math.Sqrt(v.Length);
            var candidates = new List<int>();

            for (var k = 2; k <= Math.Min(limit, acf.Length - 2); k++)
            {
                if (!double.IsNaN(acf[k]) && acf[k] > bound && acf[k] > acf[k - 1] && acf[k] >= acf[k + 1])
                {
                    candidates.Add(k);
                }
            }

            if (candidates.Count == 0)
            {
                return new SeasonalPeriodEstimate(0, double.NaN, bound, Array.Empty<int>(), Array.Empty<double>(),
                    false,
                    "No autocorrelation peak above the white-noise bound was found.");
            }

            var best = candidates.OrderByDescending(k => acf[k]).First();

            return new SeasonalPeriodEstimate(best, acf[best], bound, candidates.ToArray(),
                candidates.Select(k => acf[k]).ToArray(), true,
                "Period is in observations; multiples of the true period also peak, and the shortest strong peak is usually the fundamental.");
        }

        [AdvancedAnalyticsTool("EngleArchLmTest")]
        [Description(
            "Engle's Lagrange multiplier test for ARCH effects (volatility clustering) in a series of residuals or returns: whether large movements tend to follow large movements regardless of direction. Use it to check whether the variance of a process is changing over time before trusting constant-variance intervals or z-scores. " +
            "The squared values are regressed on their own lags; Statistic is the number of usable rows times the regression R-squared and follows a chi-square distribution with degrees of freedom equal to lags under the null of no ARCH effect. A small PValue means volatility clusters (Significant true). RSquaredOrRestrictedFit is that R-squared. " +
            "Supply mean-removed residuals or returns; if the squared values are constant the test returns NaN. Needs more than 2 times lags plus 2 finite values.")]
        public LagRegressionTestResult EngleArchLmTest(
            [Description("Residuals or returns in time order, finite, mean-removed.")]
            double[] residuals,
            [Description("Number of lags of the squared values in the auxiliary regression. Default 5.")]
            int lags = 5,
            [Description("Significance level for the Significant flag. Default 0.05.")]
            double alpha = 0.05)
        {
            var e = TimeSeriesGuard.Finite(residuals, nameof(residuals), 4);
            TimeSeriesGuard.AtLeast(lags, 1, nameof(lags));
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);

            if (e.Length <= 2 * lags + 2)
            {
                throw new ArgumentException(
                    $"residuals has {e.Length} values but {lags} lags needs more than {2 * lags + 2}. Use fewer lags or a longer series.",
                    nameof(lags));
            }

            var squares = e.Select(x => x * x).ToArray();
            var rows = squares.Length - lags;
            var design = new double[rows, lags + 1];
            var target = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                target[i] = squares[i + lags];
                design[i, 0] = 1;

                for (var j = 1; j <= lags; j++)
                {
                    design[i, j] = squares[i + lags - j];
                }
            }

            var meanTarget = TimeSeriesGuard.Mean(target);
            var total = target.Sum(t => (t - meanTarget) * (t - meanTarget));

            if (total <= 0)
            {
                return new LagRegressionTestResult("Engle ARCH LM", double.NaN, double.NaN, lags, 0, double.NaN, lags,
                    rows,
                    false, "The squared values are constant, so the auxiliary regression has no variation to explain.");
            }

            var coefficients = TimeSeriesGuard.LeastSquares(design, target);
            var rss = TimeSeriesGuard.ResidualSumOfSquares(design, target, coefficients);
            var r2 = Math.Max(0, 1 - rss / total);
            var lm = rows * r2;
            var p = TimeSeriesGuard.ChiSquareUpperTail(lm, lags);

            return new LagRegressionTestResult("Engle ARCH LM", lm, p, lags, 0, r2, lags, rows, p < alpha,
                "Chi-square with degrees of freedom equal to lags.");
        }

        [AdvancedAnalyticsTool("GrangerCausalityTest")]
        [Description(
            "Granger causality F test: whether past values of a candidate cause series improve the prediction of an effect series beyond what the effect series' own past already provides. Use it to test lead-lag relationships (for example whether card-not-present attempts predict later chargebacks). " +
            "Two regressions of the effect on its own lags are compared, with and without the lags of the cause; Statistic is the F ratio, PValue its upper tail with DegreesOfFreedomNumerator (= lags) and DegreesOfFreedomDenominator, and Significant is true when the cause adds predictive power at the chosen alpha. RSquaredOrRestrictedFit is the R-squared of the full model. " +
            "Granger causality is predictive precedence, not true causation, and both series should be stationary (difference trending series first). Series must be finite, the same length and longer than 3 times lags plus 3.")]
        public LagRegressionTestResult GrangerCausalityTest(
            [Description("The candidate cause series in time order, finite.")]
            double[] cause,
            [Description("The effect series, same length and time grid as cause.")]
            double[] effect,
            [Description("Number of past steps of each series used. Default 2.")]
            int lags = 2,
            [Description("Significance level for the Significant flag. Default 0.05.")]
            double alpha = 0.05)
        {
            var x = TimeSeriesGuard.Finite(cause, nameof(cause), 5);
            var y = TimeSeriesGuard.Finite(effect, nameof(effect), 5);
            TimeSeriesGuard.SameLength(x, y, nameof(cause), nameof(effect));
            TimeSeriesGuard.AtLeast(lags, 1, nameof(lags));
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);
            var n = y.Length;
            var rows = n - lags;
            var denominatorDf = rows - 2 * lags - 1;

            if (denominatorDf < 1)
            {
                throw new ArgumentException(
                    $"The series have {n} values which is too few for {lags} lags (need more than {3 * lags + 1}). Use fewer lags or more data.",
                    nameof(lags));
            }

            var restricted = new double[rows, lags + 1];
            var full = new double[rows, 2 * lags + 1];
            var target = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                target[i] = y[i + lags];
                restricted[i, 0] = 1;
                full[i, 0] = 1;

                for (var j = 1; j <= lags; j++)
                {
                    restricted[i, j] = y[i + lags - j];
                    full[i, j] = y[i + lags - j];
                    full[i, lags + j] = x[i + lags - j];
                }
            }

            var rssRestricted =
                TimeSeriesGuard.ResidualSumOfSquares(restricted, target,
                    TimeSeriesGuard.LeastSquares(restricted, target));
            var rssFull =
                TimeSeriesGuard.ResidualSumOfSquares(full, target, TimeSeriesGuard.LeastSquares(full, target));

            var meanTarget = TimeSeriesGuard.Mean(target);
            var total = target.Sum(t => (t - meanTarget) * (t - meanTarget));
            var r2 = total <= 0 ? double.NaN : 1 - rssFull / total;
            double f;
            double p;

            if (rssFull <= 1e-300 * Math.Max(1, total))
            {
                f = rssRestricted > rssFull ? double.PositiveInfinity : double.NaN;
                p = double.IsNaN(f) ? double.NaN : 0;
            }
            else
            {
                f = Math.Max(0, (rssRestricted - rssFull) / lags) / (rssFull / denominatorDf);
                p = new FDistribution(lags, denominatorDf).ComplementaryDistributionFunction(f);
            }

            return new LagRegressionTestResult("Granger causality F", f, p, lags, denominatorDf, r2, lags, rows,
                !double.IsNaN(p) && p < alpha,
                "Null hypothesis: the cause series adds no predictive power for the effect series.");
        }

        internal static double[] DurbinLevinsonPartials(double[] acf)
        {
            var max = acf.Length - 1;
            var pacf = new double[max];

            if (max == 0)
            {
                return pacf;
            }

            if (acf.Skip(1).Any(double.IsNaN))
            {
                Array.Fill(pacf, double.NaN);

                return pacf;
            }

            var previous = new double[max + 1];
            var current = new double[max + 1];

            for (var k = 1; k <= max; k++)
            {
                var numerator = acf[k];
                var denominator = 1.0;

                for (var j = 1; j < k; j++)
                {
                    numerator -= previous[j] * acf[k - j];
                    denominator -= previous[j] * acf[j];
                }

                var phi = Math.Abs(denominator) < 1e-14 ? double.NaN : numerator / denominator;
                current[k] = phi;

                for (var j = 1; j < k; j++)
                {
                    current[j] = previous[j] - phi * previous[k - j];
                }

                pacf[k - 1] = phi;
                Array.Copy(current, previous, max + 1);
            }

            return pacf;
        }

        private static PortmanteauTestResult Portmanteau(double[] values, int lags, int fitted, double alpha,
            bool ljungBox)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(lags, 1, nameof(lags));
            TimeSeriesGuard.AtLeast(fitted, 0, nameof(fitted));
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);

            if (lags >= v.Length)
            {
                throw new ArgumentException(
                    $"lags must be smaller than the series length {v.Length} but was {lags}. Use fewer lags.",
                    nameof(lags));
            }

            var n = v.Length;
            var acf = TimeSeriesGuard.Autocorrelations(v, lags);
            var byLag = new double[lags];
            var pByLag = new double[lags];
            var q = 0.0;

            for (var h = 1; h <= lags; h++)
            {
                var r = acf[h];
                q += ljungBox ? n * (n + 2.0) * r * r / (n - h) : n * r * r;
                byLag[h - 1] = q;
                var df = h - fitted;
                pByLag[h - 1] = df < 1 ? double.NaN : TimeSeriesGuard.ChiSquareUpperTail(q, df);
            }

            var totalDf = lags - fitted;
            var p = totalDf < 1 ? double.NaN : pByLag[lags - 1];

            return new PortmanteauTestResult(
                ljungBox ? "Ljung-Box" : "Box-Pierce",
                byLag[lags - 1],
                totalDf,
                p,
                !double.IsNaN(p) && p < alpha,
                lags,
                n,
                byLag,
                pByLag);
        }

        internal static SeasonalDecompositionResult Decompose(double[] v, int period, SeasonalModel model)
        {
            var n = v.Length;
            var trend = new double[n];
            Array.Fill(trend, double.NaN);
            var half = period / 2;

            if (period % 2 == 1)
            {
                for (var t = half; t < n - half; t++)
                {
                    var sum = 0.0;

                    for (var j = -half; j <= half; j++)
                    {
                        sum += v[t + j];
                    }

                    trend[t] = sum / period;
                }
            }
            else
            {
                for (var t = half; t < n - half; t++)
                {
                    var sum = 0.5 * v[t - half] + 0.5 * v[t + half];

                    for (var j = -half + 1; j <= half - 1; j++)
                    {
                        sum += v[t + j];
                    }

                    trend[t] = sum / period;
                }
            }

            var indices = new double[period];

            for (var p = 0; p < period; p++)
            {
                var parts = new List<double>();

                for (var t = p; t < n; t += period)
                {
                    if (!double.IsNaN(trend[t]))
                    {
                        parts.Add(model == SeasonalModel.Additive ? v[t] - trend[t] : v[t] / trend[t]);
                    }
                }

                indices[p] = parts.Count == 0 ? double.NaN : TimeSeriesGuard.Mean(parts);
            }

            var centre = TimeSeriesGuard.Mean(indices.Where(x => !double.IsNaN(x)).ToArray());

            for (var p = 0; p < period; p++)
            {
                indices[p] = model == SeasonalModel.Additive ? indices[p] - centre : indices[p] / centre;
            }

            var seasonal = new double[n];
            var remainder = new double[n];

            for (var t = 0; t < n; t++)
            {
                seasonal[t] = indices[t % period];

                remainder[t] = double.IsNaN(trend[t])
                    ? double.NaN
                    : model == SeasonalModel.Additive
                        ? v[t] - trend[t] - seasonal[t]
                        : v[t] / (trend[t] * seasonal[t]);
            }

            var rows = Enumerable.Range(0, n).Where(t => !double.IsNaN(trend[t])).ToArray();

            var additiveRemainder = rows
                .Select(t => model == SeasonalModel.Additive ? remainder[t] : v[t] - trend[t] * seasonal[t]).ToArray();

            var seasonalPlusRemainder = rows.Select(t => v[t] - trend[t]).ToArray();

            var trendPlusRemainder = rows.Select(t => v[t] - (model == SeasonalModel.Additive ? seasonal[t] : 0) -
                                                      (model == SeasonalModel.Additive
                                                          ? 0
                                                          : trend[t] * seasonal[t] - trend[t])).ToArray();

            var seasonalStrength = Strength(additiveRemainder, seasonalPlusRemainder);
            var trendStrength = Strength(additiveRemainder, trendPlusRemainder);

            return new SeasonalDecompositionResult(trend, seasonal, remainder, indices, seasonalStrength, trendStrength,
                period, model.ToString());
        }

        private static double Strength(double[] remainder, double[] combined)
        {
            var vr = TimeSeriesGuard.SampleVariance(remainder);
            var vc = TimeSeriesGuard.SampleVariance(combined);

            if (double.IsNaN(vr) || double.IsNaN(vc) || vc <= 0)
            {
                return double.NaN;
            }

            return Math.Max(0, 1 - vr / vc);
        }
    }
}
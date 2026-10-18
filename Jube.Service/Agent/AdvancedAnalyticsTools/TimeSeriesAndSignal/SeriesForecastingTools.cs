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
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum BaselineForecastMethod
    {
        Naive,
        SeasonalNaive,
        Mean,
        Drift,
        MovingAverage
    }

    public enum AutoregressiveEstimation
    {
        YuleWalker,
        LeastSquares
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SeriesForecastingTools
    {
        [AdvancedAnalyticsTool("BaselineSeriesForecast")]
        [Description(
            "Benchmark forecasts that any more elaborate model should beat: Naive (repeat the last value), SeasonalNaive (repeat the value from one cycle earlier), Mean (the overall average), Drift (extend the straight line joining the first and last values) and MovingAverage (average of the last 'window' values). Use it as a quick projection or as a yardstick before trusting a complex model. " +
            "Forecast[h-1] is the point forecast h steps ahead; Lower and Upper are prediction bounds at confidenceLevel built from the in-sample one-step errors with the standard variance formula of each method (Naive sigma x sqrt(h), Drift sigma x sqrt(h(1+h/n)), Mean sigma x sqrt(1+1/n)); Fitted holds the one-step in-sample predictions (NaN where none exists) and ResidualStandardDeviation their spread. " +
            "SeasonalNaive needs period of 2 or more and at least one full cycle. Requires finite values in time order, evenly spaced.")]
        public SeriesForecastResult BaselineSeriesForecast(
            [Description("The history in time order, evenly spaced, finite, at least 3 values.")]
            double[] values,
            [Description("Number of steps ahead to forecast, 1 to 10000.")]
            int horizon,
            [Description("Naive, SeasonalNaive, Mean, Drift or MovingAverage. Default Naive.")]
            BaselineForecastMethod method = BaselineForecastMethod.Naive,
            [Description(
                "Cycle length for SeasonalNaive, for example 7 for daily data with a weekly pattern. Default 1.")]
            int seasonalPeriod = 1,
            [Description("Number of recent values averaged by MovingAverage. Default 3.")]
            int window = 3,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 3);
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));

            if (horizon < 1 || horizon > 10000)
            {
                throw new ArgumentException($"horizon must be between 1 and 10000 but was {horizon}.", nameof(horizon));
            }

            if (method == BaselineForecastMethod.SeasonalNaive)
            {
                TimeSeriesGuard.AtLeast(seasonalPeriod, 2, nameof(seasonalPeriod));

                if (y.Length < seasonalPeriod + 1)
                {
                    throw new ArgumentException(
                        $"values has {y.Length} points; a seasonal naive forecast needs more than one cycle of {seasonalPeriod}.",
                        nameof(values));
                }
            }

            if (method == BaselineForecastMethod.MovingAverage && (window < 1 || window > y.Length))
            {
                throw new ArgumentException(
                    $"window must be between 1 and the series length {y.Length} but was {window}.",
                    nameof(window));
            }

            return Baseline(y, horizon, method, Math.Max(1, seasonalPeriod), Math.Max(1, window), z, confidenceLevel);
        }

        [AdvancedAnalyticsTool("SimpleExponentialSmoothingForecast")]
        [Description(
            "Simple exponential smoothing forecast: a flat forecast equal to an exponentially weighted average of the history, suited to series with no trend or seasonality. Give alpha (between 0 and 1, higher reacts faster) or leave it NaN to have it chosen by minimising the one-step squared error. The level starts at the first value. " +
            "Forecast repeats the final smoothed level; Lower and Upper widen with the horizon as sigma x sqrt(1 + (h-1) alpha squared); Fitted are the one-step predictions (NaN for the first); ParameterNames and ParameterValues report alpha and the final level. " +
            "Requires finite values in time order, evenly spaced.")]
        public SeriesForecastResult SimpleExponentialSmoothingForecast(
            [Description("The history in time order, evenly spaced, finite, at least 3 values.")]
            double[] values,
            [Description("Number of steps ahead, 1 to 10000.")]
            int horizon,
            [Description("Smoothing weight above 0 and at most 1, or NaN (default) to estimate it.")]
            double alpha = double.NaN,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 3);
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            RequireHorizon(horizon);

            if (!double.IsNaN(alpha) && !(alpha > 0 && alpha <= 1))
            {
                throw new ArgumentException(
                    $"alpha must be above 0 and at most 1, or NaN to estimate it, but was {alpha}.",
                    nameof(alpha));
            }

            return Ses(y, horizon, alpha, z, confidenceLevel);
        }

        [AdvancedAnalyticsTool("HoltLinearTrendForecast")]
        [Description(
            "Holt's linear trend exponential smoothing forecast: tracks a level and a slope that both adapt over time, so the forecast continues the recent trend as a straight line (or, with damping below 1, a trend that flattens out). Use it for trending series without a repeating seasonal pattern. " +
            "Leave alpha or beta as NaN to estimate both by minimising the one-step squared error. phi is the damping factor (1 means no damping; 0.8 to 0.98 gives a flattening trend). The level starts at the first value and the slope at the first difference, so a perfectly linear series is reproduced exactly. " +
            "Forecast, Lower and Upper give the projection and prediction bounds; ParameterNames and ParameterValues report alpha, beta, phi and the final level and slope. Requires finite values in time order, evenly spaced.")]
        public SeriesForecastResult HoltLinearTrendForecast(
            [Description("The history in time order, evenly spaced, finite, at least 4 values.")]
            double[] values,
            [Description("Number of steps ahead, 1 to 10000.")]
            int horizon,
            [Description("Level smoothing weight in (0, 1], or NaN (default) to estimate alpha and beta.")]
            double alpha = double.NaN,
            [Description("Trend smoothing weight in (0, 1], or NaN (default) to estimate alpha and beta.")]
            double beta = double.NaN,
            [Description("Damping factor in (0, 1]; 1 (default) keeps the trend undamped.")]
            double phi = 1,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 4);
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            RequireHorizon(horizon);

            if (!double.IsNaN(alpha) && !(alpha > 0 && alpha <= 1))
            {
                throw new ArgumentException($"alpha must be in (0, 1] or NaN but was {alpha}.", nameof(alpha));
            }

            if (!double.IsNaN(beta) && !(beta > 0 && beta <= 1))
            {
                throw new ArgumentException($"beta must be in (0, 1] or NaN but was {beta}.", nameof(beta));
            }

            if (!(phi > 0 && phi <= 1))
            {
                throw new ArgumentException($"phi must be in (0, 1] but was {phi}.", nameof(phi));
            }

            return Holt(y, horizon, alpha, beta, phi, z, confidenceLevel);
        }

        [AdvancedAnalyticsTool("HoltWintersSeasonalForecast")]
        [Description(
            "Holt-Winters exponential smoothing forecast for series with both a trend and a repeating seasonal pattern (weekly cycles in daily counts, daily cycles in hourly counts). Additive seasonality suits a constant seasonal swing, multiplicative one that grows with the level (all values must be above 0). " +
            "Leave alpha, beta or gamma as NaN to estimate all three by minimising the one-step squared error. The level, trend and seasonal indices are initialised from the first two cycles, so at least two full cycles are required. Forecast, Lower and Upper give the projection and prediction bounds (approximate for the multiplicative form); ParameterNames and ParameterValues report the smoothing weights and final state. " +
            "A series that is exactly periodic with no trend is reproduced exactly. Requires finite values in time order, evenly spaced.")]
        public SeriesForecastResult HoltWintersSeasonalForecast(
            [Description("The history in time order, evenly spaced, finite, at least two full cycles long.")]
            double[] values,
            [Description("Number of steps ahead, 1 to 10000.")]
            int horizon,
            [Description("Cycle length, at least 2, for example 7 or 24.")]
            int seasonalPeriod,
            [Description("Additive (default) or Multiplicative seasonality.")]
            SeasonalModel model = SeasonalModel.Additive,
            [Description("Level weight in (0, 1], or NaN (default) to estimate alpha, beta and gamma.")]
            double alpha = double.NaN,
            [Description("Trend weight in (0, 1], or NaN (default) to estimate all three.")]
            double beta = double.NaN,
            [Description("Seasonal weight in (0, 1], or NaN (default) to estimate all three.")]
            double gamma = double.NaN,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 4);
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            RequireHorizon(horizon);
            TimeSeriesGuard.AtLeast(seasonalPeriod, 2, nameof(seasonalPeriod));

            if (y.Length < 2 * seasonalPeriod)
            {
                throw new ArgumentException(
                    $"values has {y.Length} points but two full cycles of {seasonalPeriod} need {2 * seasonalPeriod}.",
                    nameof(seasonalPeriod));
            }

            if (model == SeasonalModel.Multiplicative && y.Any(v => v <= 0))
            {
                throw new ArgumentException("A multiplicative model needs every value above 0. Use Additive.",
                    nameof(model));
            }

            foreach (var (name, weight) in new[]
                         { (nameof(alpha), alpha), (nameof(beta), beta), (nameof(gamma), gamma) })
            {
                if (!double.IsNaN(weight) && !(weight > 0 && weight <= 1))
                {
                    throw new ArgumentException($"{name} must be in (0, 1] or NaN but was {weight}.", name);
                }
            }

            return HoltWinters(y, horizon, seasonalPeriod, model == SeasonalModel.Multiplicative, alpha, beta, gamma, z,
                confidenceLevel);
        }

        [AdvancedAnalyticsTool("LinearTrendForecast")]
        [Description(
            "Extrapolates a straight-line trend fitted by ordinary least squares against the time index 0, 1, 2 and so on, with exact prediction intervals from the t distribution. Use it for a steady, roughly linear drift with noise that does not change over time. " +
            "Forecast continues the line beyond the last point; Lower and Upper are prediction bounds that widen with distance from the data; Fitted is the line at each observed point; ParameterNames and ParameterValues give the intercept and slope per step; ResidualStandardDeviation is the fit's standard error (sum of squared errors over n - 2). " +
            "Ignores seasonality and assumes independent errors. Requires at least 3 finite values.")]
        public SeriesForecastResult LinearTrendForecast(
            [Description("The history in time order, evenly spaced, finite, at least 3 values.")]
            double[] values,
            [Description("Number of steps ahead, 1 to 10000.")]
            int horizon,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            RequireHorizon(horizon);

            return LinearTrend(y, horizon, confidenceLevel);
        }

        [AdvancedAnalyticsTool("AutoregressiveModelFit")]
        [Description(
            "Fits an autoregressive model AR(p), in which each value is a weighted sum of the previous p values plus noise, estimated by Yule-Walker (from the sample autocovariances, always stationary) or ordinary LeastSquares (regression on lagged values with an intercept). Use it to quantify how strongly a series depends on its own past. " +
            "Coefficients[k-1] multiplies the value k steps earlier; Mean is the implied long-run level (NaN if the coefficients sum to 1, a unit root) and Intercept the constant term; InnovationVariance is the estimated variance of the one-step error; Aic and Bic are information criteria (lower is better, comparable only for the same data and method). " +
            "For 1, 2, 3, 4, 5 with order 1 the least-squares fit is exact (coefficient 1, intercept 1, variance 0). Requires finite values.")]
        public AutoregressiveFitResult AutoregressiveModelFit(
            [Description("The series in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Number of lags p, at least 1.")]
            int order,
            [Description("YuleWalker (default) or LeastSquares.")]
            AutoregressiveEstimation estimation = AutoregressiveEstimation.YuleWalker)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 3);
            RequireOrder(y, order, estimation);

            return FitAr(y, order, estimation);
        }

        [AdvancedAnalyticsTool("AutoregressiveOrderSelection")]
        [Description(
            "Chooses the lag order p of an autoregressive model by fitting orders 0 to maxOrder by Yule-Walker and comparing the AIC and BIC information criteria (lower is better; BIC favours smaller models). Use it before AutoregressiveForecast or when deciding how much memory a series has. " +
            "Orders lists the tried orders, Aic and Bic the matching criteria and BestOrderByAic and BestOrderByBic the winners. Order 0 means no dependence on the past. Requires finite values and more than 2 x maxOrder points.")]
        public AutoregressiveOrderResult AutoregressiveOrderSelection(
            [Description("The series in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Largest order to try, at least 1. Default 10.")]
            int maxOrder = 10)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.AtLeast(maxOrder, 1, nameof(maxOrder));

            if (y.Length <= 2 * maxOrder)
            {
                throw new ArgumentException(
                    $"values has {y.Length} points; selecting up to order {maxOrder} needs more than {2 * maxOrder}. Use a smaller maxOrder.",
                    nameof(maxOrder));
            }

            var fits = Enumerable.Range(0, maxOrder + 1)
                .Select(p => p == 0 ? FitAr0(y) : FitAr(y, p, AutoregressiveEstimation.YuleWalker)).ToArray();

            return new AutoregressiveOrderResult(Enumerable.Range(0, maxOrder + 1).ToArray(),
                fits.Select(f => f.Aic).ToArray(),
                fits.Select(f => f.Bic).ToArray(), fits.OrderBy(f => f.Aic).First().Order,
                fits.OrderBy(f => f.Bic).First().Order);
        }

        [AdvancedAnalyticsTool("AutoregressiveForecast")]
        [Description(
            "Forecasts a series with a fitted autoregressive AR(p) model: each future value is predicted from the previous p values (using earlier forecasts once the data run out), so forecasts revert toward the long-run mean for a stationary series. Estimate by YuleWalker (default) or LeastSquares. " +
            "Forecast, Lower and Upper give point forecasts and prediction bounds from the psi (moving-average) weights, so the interval widens with the horizon and levels off; Fitted are the in-sample one-step predictions (NaN for the first p); ParameterNames and ParameterValues report order, intercept and innovation variance. Use AutoregressiveOrderSelection to choose the order. " +
            "For a series that trends, difference it first (DifferenceSeries) or use a trend method. Requires finite values.")]
        public SeriesForecastResult AutoregressiveForecast(
            [Description("The history in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Number of steps ahead, 1 to 10000.")]
            int horizon,
            [Description("Number of lags p, at least 1.")]
            int order,
            [Description("YuleWalker (default) or LeastSquares.")]
            AutoregressiveEstimation estimation = AutoregressiveEstimation.YuleWalker,
            [Description("Coverage of the prediction interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 3);
            var z = TimeSeriesGuard.Confidence(confidenceLevel, nameof(confidenceLevel));
            RequireHorizon(horizon);
            RequireOrder(y, order, estimation);

            return ArForecast(y, horizon, order, estimation, z, confidenceLevel);
        }

        [AdvancedAnalyticsTool("ForecastAccuracyMetrics")]
        [Description(
            "Scores forecasts against what actually happened, with the standard error measures. MeanError is the average of actual minus forecast (positive means under-forecasting); MeanAbsoluteError, MedianAbsoluteError, MeanSquaredError and RootMeanSquaredError measure size in the units of the series; MeanAbsolutePercentageError skips actuals equal to 0 (counted in PercentageErrorsSkipped) and is a fraction, so 0.1 means 10 percent; SymmetricMeanAbsolutePercentageError is the mean of 2|a-f|/(|a|+|f|) (0 when both are 0). " +
            "MeanAbsoluteScaledError divides the mean absolute error by the mean absolute one-step (or seasonal, with seasonalPeriod) naive error on the trainingValues, so values below 1 beat the naive method; it is NaN when trainingValues is empty or constant. " +
            "actual and forecast must be the same length, in the same order, finite.")]
        public ForecastAccuracyResult ForecastAccuracyMetrics(
            [Description("The observed values, finite, at least 1.")]
            double[] actual,
            [Description("The forecast values for the same periods, same length as actual.")]
            double[] forecast,
            [Description(
                "Optional earlier history used only to scale MeanAbsoluteScaledError; leave empty to skip it.")]
            double[]? trainingValues = null,
            [Description("Seasonal lag used for the scaling naive forecast, at least 1. Default 1.")]
            int seasonalPeriod = 1)
        {
            var a = TimeSeriesGuard.Finite(actual, nameof(actual), 1);
            var f = TimeSeriesGuard.Finite(forecast, nameof(forecast), 1);
            TimeSeriesGuard.SameLength(a, f, nameof(actual), nameof(forecast));
            TimeSeriesGuard.AtLeast(seasonalPeriod, 1, nameof(seasonalPeriod));
            var n = a.Length;
            var errors = a.Select((v, i) => v - f[i]).ToArray();
            var absolute = errors.Select(Math.Abs).ToArray();
            var mse = errors.Sum(e => e * e) / n;
            var nonZero = Enumerable.Range(0, n).Where(i => a[i] != 0).ToArray();
            var mape = nonZero.Length == 0 ? double.NaN : nonZero.Average(i => Math.Abs(errors[i] / a[i]));

            var smape = Enumerable.Range(0, n).Average(i =>
                Math.Abs(a[i]) + Math.Abs(f[i]) == 0 ? 0 : 2 * absolute[i] / (Math.Abs(a[i]) + Math.Abs(f[i])));

            var mase = double.NaN;

            if (trainingValues != null && trainingValues.Length > seasonalPeriod)
            {
                TimeSeriesGuard.Finite(trainingValues, nameof(trainingValues), 1);

                var scale = Enumerable.Range(seasonalPeriod, trainingValues.Length - seasonalPeriod)
                    .Average(t => Math.Abs(trainingValues[t] - trainingValues[t - seasonalPeriod]));

                mase = scale > 0 ? absolute.Average() / scale : double.NaN;
            }

            return new ForecastAccuracyResult(n, errors.Average(), absolute.Average(), TimeSeriesGuard.Median(absolute),
                mse, Math.Sqrt(mse), mape, smape, mase, n - nonZero.Length);
        }

        [AdvancedAnalyticsTool("HoldOutForecastComparison")]
        [Description(
            "Compares forecasting methods fairly by hiding the last holdOut points, fitting each method on the earlier history and scoring its forecast of the hidden points. Methods tried are Naive, SeasonalNaive and HoltWintersAdditive (only when seasonalPeriod is 2 or more and there is enough data), Mean, Drift, SimpleExponentialSmoothing, HoltLinearTrend, LinearTrend and an autoregressive model whose order is chosen by BIC. " +
            "Scores has one row per method that could run, sorted by root mean squared error (best first), with MeanAbsoluteError, RootMeanSquaredError, MeanAbsolutePercentageError, SymmetricMeanAbsolutePercentageError, MeanAbsoluteScaledError (below 1 beats the naive method on the training part) and the Forecast itself; BestMethodByRmse names the winner and Skipped explains methods that could not run. " +
            "Use it to pick a method before forecasting the real future. Requires finite values with at least 3 x holdOut and 8 points.")]
        public HoldOutComparisonResult HoldOutForecastComparison(
            [Description("The full history in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Number of final points to hide and forecast, at least 1 and at most a third of the series.")]
            int holdOut,
            [Description("Cycle length for seasonal methods, or 1 (default) if none.")]
            int seasonalPeriod = 1)
        {
            var y = TimeSeriesGuard.Finite(values, nameof(values), 8);
            TimeSeriesGuard.AtLeast(holdOut, 1, nameof(holdOut));
            TimeSeriesGuard.AtLeast(seasonalPeriod, 1, nameof(seasonalPeriod));

            if (holdOut * 3 > y.Length)
            {
                throw new ArgumentException(
                    $"holdOut ({holdOut}) may be at most a third of the series length ({y.Length}).", nameof(holdOut));
            }

            var train = y.Take(y.Length - holdOut).ToArray();
            var actual = y.Skip(y.Length - holdOut).ToArray();
            var z = 1.96;

            var candidates = new List<(string Name, Func<double[]> Run)>
            {
                ("Naive", () => Baseline(train, holdOut, BaselineForecastMethod.Naive, 1, 1, z, 0.95).Forecast),
                ("Mean", () => Baseline(train, holdOut, BaselineForecastMethod.Mean, 1, 1, z, 0.95).Forecast),
                ("Drift", () => Baseline(train, holdOut, BaselineForecastMethod.Drift, 1, 1, z, 0.95).Forecast),
                ("SimpleExponentialSmoothing", () => Ses(train, holdOut, double.NaN, z, 0.95).Forecast),
                ("HoltLinearTrend", () => Holt(train, holdOut, double.NaN, double.NaN, 1, z, 0.95).Forecast),
                ("LinearTrend", () => LinearTrend(train, holdOut, 0.95).Forecast)
            };

            var skipped = new List<string>();

            if (seasonalPeriod >= 2 && train.Length > seasonalPeriod)
            {
                candidates.Add(("SeasonalNaive",
                    () => Baseline(train, holdOut, BaselineForecastMethod.SeasonalNaive, seasonalPeriod, 1, z, 0.95)
                        .Forecast));
            }
            else if (seasonalPeriod >= 2)
            {
                skipped.Add("SeasonalNaive: training data shorter than one cycle.");
            }

            if (seasonalPeriod >= 2 && train.Length >= 2 * seasonalPeriod)
            {
                candidates.Add(("HoltWintersAdditive",
                    () => HoltWinters(train, holdOut, seasonalPeriod, false, double.NaN, double.NaN, double.NaN, z,
                            0.95)
                        .Forecast));
            }
            else if (seasonalPeriod >= 2)
            {
                skipped.Add("HoltWintersAdditive: training data shorter than two cycles.");
            }

            var maxOrder = Math.Min(10, train.Length / 3);

            if (maxOrder >= 1)
            {
                candidates.Add(("Autoregressive", () =>
                {
                    var best = Enumerable.Range(1, maxOrder)
                        .Select(p => FitAr(train, p, AutoregressiveEstimation.YuleWalker)).OrderBy(f => f.Bic).First()
                        .Order;

                    return ArForecast(train, holdOut, best, AutoregressiveEstimation.YuleWalker, z, 0.95).Forecast;
                }));
            }

            var scores = new List<HoldOutMethodScore>();

            foreach (var (name, run) in candidates)
            {
                try
                {
                    var forecast = run();

                    if (forecast.Any(v => !double.IsFinite(v)))
                    {
                        skipped.Add($"{name}: produced a non-finite forecast.");
                        continue;
                    }

                    var m = new SeriesForecastingTools().ForecastAccuracyMetrics(actual, forecast, train,
                        Math.Max(1, seasonalPeriod));

                    scores.Add(new HoldOutMethodScore(name, m.MeanAbsoluteError, m.RootMeanSquaredError,
                        m.MeanAbsolutePercentageError,
                        m.SymmetricMeanAbsolutePercentageError, m.MeanAbsoluteScaledError, forecast));
                }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException)
                {
                    skipped.Add($"{name}: {e.Message}");
                }
            }

            var ordered = scores.OrderBy(s => s.RootMeanSquaredError).ThenBy(s => s.Method, StringComparer.Ordinal)
                .ToArray();

            return new HoldOutComparisonResult(ordered, ordered.Length > 0 ? ordered[0].Method : string.Empty,
                train.Length,
                holdOut, skipped.ToArray());
        }

        private static double Logistic(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }

        private static SeriesForecastResult Build(string method, double[] forecast, double[] fitted, double sigma,
            Func<int, double> varianceFactor,
            double z, double confidence, string[] names, double[] values, string note)
        {
            var lower = new double[forecast.Length];
            var upper = new double[forecast.Length];

            for (var h = 1; h <= forecast.Length; h++)
            {
                var width = z * sigma * Math.Sqrt(varianceFactor(h));
                lower[h - 1] = forecast[h - 1] - width;
                upper[h - 1] = forecast[h - 1] + width;
            }

            return new SeriesForecastResult(method, forecast, lower, upper, fitted, sigma, confidence, names, values,
                note);
        }

        private static double ResidualSigma(double[] y, double[] fitted, int estimated)
        {
            var rss = 0.0;
            var count = 0;

            for (var t = 0; t < y.Length; t++)
            {
                if (!double.IsNaN(fitted[t]))
                {
                    rss += (y[t] - fitted[t]) * (y[t] - fitted[t]);
                    count++;
                }
            }

            return Math.Sqrt(rss / Math.Max(1, count - estimated));
        }

        internal static SeriesForecastResult Baseline(double[] y, int horizon, BaselineForecastMethod method,
            int period,
            int window, double z, double confidence)
        {
            var n = y.Length;
            var fitted = new double[n];
            var forecast = new double[horizon];
            Func<int, double> factor;
            var drift = (y[^1] - y[0]) / (n - 1);
            var k = 0;

            for (var t = 0; t < n; t++)
            {
                switch (method)
                {
                    case BaselineForecastMethod.Naive:
                        fitted[t] = t >= 1 ? y[t - 1] : double.NaN;
                        break;
                    case BaselineForecastMethod.SeasonalNaive:
                        fitted[t] = t >= period ? y[t - period] : double.NaN;
                        break;
                    case BaselineForecastMethod.Mean:
                        fitted[t] = y.Average();
                        break;
                    case BaselineForecastMethod.Drift:
                        fitted[t] = t >= 1 ? y[t - 1] + drift : double.NaN;
                        break;
                    default:
                        fitted[t] = t >= window ? y.Skip(t - window).Take(window).Average() : double.NaN;
                        break;
                }
            }

            for (var h = 1; h <= horizon; h++)
            {
                forecast[h - 1] = method switch
                {
                    BaselineForecastMethod.Naive => y[^1],
                    BaselineForecastMethod.SeasonalNaive => y[n - period + (h - 1) % period],
                    BaselineForecastMethod.Mean => y.Average(),
                    BaselineForecastMethod.Drift => y[^1] + h * drift,
                    _ => y.Skip(n - window).Average()
                };
            }

            switch (method)
            {
                case BaselineForecastMethod.SeasonalNaive:
                    factor = h => (h - 1) / period + 1;
                    break;
                case BaselineForecastMethod.Mean:
                    k = 1;
                    factor = _ => 1 + 1.0 / n;
                    break;
                case BaselineForecastMethod.Drift:
                    k = 1;
                    factor = h => h * (1 + (double)h / n);
                    break;
                case BaselineForecastMethod.MovingAverage:
                    factor = _ => 1 + 1.0 / window;
                    break;
                default:
                    factor = h => h;
                    break;
            }

            var sigma = ResidualSigma(y, fitted, k);

            return Build(method.ToString(), forecast, fitted, sigma, factor, z, confidence,
                new[] { "period", "window" },
                new double[] { period, window },
                "Interval widths use the standard closed-form variances for each benchmark method.");
        }

        private static double[] Optimise(int dimension, Func<double[], double> objective, double[] start)
        {
            var optimiser = new NelderMead(dimension, objective);
            optimiser.Minimize(start);

            return optimiser.Solution;
        }

        internal static SeriesForecastResult Ses(double[] y, int horizon, double alpha, double z, double confidence)
        {
            var estimated = double.IsNaN(alpha);

            Func<double, double[]> run = a =>
            {
                var level = y[0];
                var fitted = new double[y.Length];
                fitted[0] = double.NaN;

                for (var t = 1; t < y.Length; t++)
                {
                    fitted[t] = level;
                    level = a * y[t] + (1 - a) * level;
                }

                return fitted.Append(level).ToArray();
            };

            if (estimated)
            {
                alpha = BrentSearch.Minimize(a =>
                {
                    var r = run(a);

                    return Enumerable.Range(1, y.Length - 1).Sum(t => (y[t] - r[t]) * (y[t] - r[t]));
                }, 0.0001, 0.9999, 1e-8, 200);
            }

            var result = run(alpha);
            var final = result[^1];
            var fittedValues = result.Take(y.Length).ToArray();
            var sigma = ResidualSigma(y, fittedValues, estimated ? 1 : 0);
            var a2 = alpha * alpha;

            return Build("SimpleExponentialSmoothing", Enumerable.Repeat(final, horizon).ToArray(), fittedValues, sigma,
                h => 1 + (h - 1) * a2, z, confidence, new[] { "alpha", "finalLevel" }, new[] { alpha, final },
                estimated ? "alpha was chosen by minimising the one-step squared error." : "alpha was supplied.");
        }

        private static void RequireHorizon(int horizon)
        {
            if (horizon < 1 || horizon > 10000)
            {
                throw new ArgumentException($"horizon must be between 1 and 10000 but was {horizon}.", nameof(horizon));
            }
        }

        internal static SeriesForecastResult Holt(double[] y, int horizon, double alpha, double beta, double phi,
            double z,
            double confidence)
        {
            var estimated = double.IsNaN(alpha) || double.IsNaN(beta);

            Func<double, double, (double[] Fitted, double Level, double Trend)> run = (a, b) =>
            {
                var level = y[0];
                var trend = y[1] - y[0];
                var fitted = new double[y.Length];
                fitted[0] = double.NaN;

                for (var t = 1; t < y.Length; t++)
                {
                    fitted[t] = level + phi * trend;
                    var newLevel = a * y[t] + (1 - a) * (level + phi * trend);
                    trend = b * (newLevel - level) + (1 - b) * phi * trend;
                    level = newLevel;
                }

                return (fitted, level, trend);
            };

            if (estimated)
            {
                var solution = Optimise(2, p =>
                {
                    var r = run(0.0001 + 0.9998 * Logistic(p[0]), 0.0001 + 0.9998 * Logistic(p[1]));

                    return Enumerable.Range(1, y.Length - 1).Sum(t => (y[t] - r.Fitted[t]) * (y[t] - r.Fitted[t]));
                }, new[] { -0.8, -1.5 });

                alpha = 0.0001 + 0.9998 * Logistic(solution[0]);
                beta = 0.0001 + 0.9998 * Logistic(solution[1]);
            }

            var result = run(alpha, beta);
            var forecast = new double[horizon];
            var cumulative = 0.0;
            var power = 1.0;

            for (var h = 1; h <= horizon; h++)
            {
                power *= phi;
                cumulative += power;
                forecast[h - 1] = result.Level + cumulative * result.Trend;
            }

            var sigma = ResidualSigma(y, result.Fitted, estimated ? 2 : 0);
            var betaStar = alpha * beta;

            Func<int, double> factor = h =>
            {
                var sum = 1.0;
                var phiSum = 0.0;
                var p = 1.0;

                for (var j = 1; j <= h - 1; j++)
                {
                    p *= phi;
                    phiSum += p;
                    var c = alpha + betaStar * phiSum;
                    sum += c * c;
                }

                return sum;
            };

            return Build(phi < 1 ? "DampedHoltLinearTrend" : "HoltLinearTrend", forecast, result.Fitted, sigma, factor,
                z,
                confidence,
                new[] { "alpha", "beta", "phi", "finalLevel", "finalTrend" },
                new[] { alpha, beta, phi, result.Level, result.Trend },
                "Level starts at the first value and trend at the first difference.");
        }

        internal static SeriesForecastResult HoltWinters(double[] y, int horizon, int m, bool multiplicative,
            double alpha,
            double beta, double gamma, double z, double confidence)
        {
            var n = y.Length;
            var estimated = double.IsNaN(alpha) || double.IsNaN(beta) || double.IsNaN(gamma);
            var first = y.Take(m).Average();
            var second = y.Skip(m).Take(m).Average();

            Func<double, double, double, (double[] Fitted, double Level, double Trend, double[] Season)> run =
                (a, b, g) =>
                {
                    var level = first;
                    var trend = (second - first) / m;

                    var season = Enumerable.Range(0, m).Select(i => multiplicative ? y[i] / first : y[i] - first)
                        .ToArray();

                    var fitted = new double[n];

                    for (var t = 0; t < n; t++)
                    {
                        var s = season[t % m];
                        var predicted = multiplicative ? (level + trend) * s : level + trend + s;
                        fitted[t] = predicted;

                        var newLevel = multiplicative
                            ? a * (y[t] / s) + (1 - a) * (level + trend)
                            : a * (y[t] - s) + (1 - a) * (level + trend);

                        trend = b * (newLevel - level) + (1 - b) * trend;

                        season[t % m] = multiplicative
                            ? g * (y[t] / newLevel) + (1 - g) * s
                            : g * (y[t] - newLevel) + (1 - g) * s;

                        level = newLevel;
                    }

                    return (fitted, level, trend, season);
                };

            if (estimated)
            {
                var solution = Optimise(3, p =>
                {
                    var r = run(0.0001 + 0.9998 * Logistic(p[0]), 0.0001 + 0.9998 * Logistic(p[1]),
                        0.0001 + 0.9998 * Logistic(p[2]));

                    var sse = Enumerable.Range(0, n).Sum(t => (y[t] - r.Fitted[t]) * (y[t] - r.Fitted[t]));

                    return double.IsNaN(sse) ? double.MaxValue : sse;
                }, new[] { -1.0, -2.0, -1.0 });

                alpha = 0.0001 + 0.9998 * Logistic(solution[0]);
                beta = 0.0001 + 0.9998 * Logistic(solution[1]);
                gamma = 0.0001 + 0.9998 * Logistic(solution[2]);
            }

            var result = run(alpha, beta, gamma);
            var forecast = new double[horizon];

            for (var h = 1; h <= horizon; h++)
            {
                var s = result.Season[(n + h - 1) % m];

                forecast[h - 1] =
                    multiplicative ? (result.Level + h * result.Trend) * s : result.Level + h * result.Trend + s;
            }

            var sigma = ResidualSigma(y, result.Fitted, estimated ? 3 : 0);
            var betaStar = alpha * beta;
            var gammaStar = (1 - alpha) * gamma;

            Func<int, double> factor = h =>
            {
                var sum = 1.0;

                for (var j = 1; j <= h - 1; j++)
                {
                    var c = alpha + betaStar * j + (j % m == 0 ? gammaStar : 0);
                    sum += c * c;
                }

                return sum;
            };

            return Build(multiplicative ? "HoltWintersMultiplicative" : "HoltWintersAdditive", forecast, result.Fitted,
                sigma, factor, z, confidence,
                new[] { "alpha", "beta", "gamma", "period", "finalLevel", "finalTrend" },
                new[] { alpha, beta, gamma, m, result.Level, result.Trend },
                multiplicative
                    ? "Prediction intervals for the multiplicative form are approximate (additive-form variance)."
                    : "Intervals use the additive ETS variance formula.");
        }

        internal static SeriesForecastResult LinearTrend(double[] y, int horizon, double confidence)
        {
            var n = y.Length;
            TimeSeriesGuard.LinearFit(y, out var intercept, out var slope);
            var fitted = Enumerable.Range(0, n).Select(t => intercept + slope * t).ToArray();
            var rss = Enumerable.Range(0, n).Sum(t => (y[t] - fitted[t]) * (y[t] - fitted[t]));
            var s = Math.Sqrt(rss / (n - 2));
            var meanT = (n - 1) / 2.0;
            var sxx = Enumerable.Range(0, n).Sum(t => (t - meanT) * (t - meanT));
            var q = new TDistribution(n - 2).InverseDistributionFunction(0.5 + confidence / 2);
            var forecast = Enumerable.Range(0, horizon).Select(h => intercept + slope * (n + h)).ToArray();

            return Build("LinearTrend", forecast, fitted, s, h => 1 + 1.0 / n + Math.Pow(n + h - 1 - meanT, 2) / sxx, q,
                confidence,
                new[] { "intercept", "slopePerStep" }, new[] { intercept, slope },
                "Intervals use the t distribution with n - 2 degrees of freedom.");
        }

        private static AutoregressiveFitResult FitAr(double[] y, int order, AutoregressiveEstimation estimation)
        {
            var n = y.Length;
            var mean = TimeSeriesGuard.Mean(y);

            if (estimation == AutoregressiveEstimation.YuleWalker)
            {
                var gamma = TimeSeriesGuard.Autocovariances(y, order);
                var phi = Array.Empty<double>();
                var variance = gamma[0];

                if (order > 0)
                {
                    var toeplitz = new double[order, order];
                    var rhs = new double[order];

                    for (var i = 0; i < order; i++)
                    {
                        rhs[i] = gamma[i + 1];

                        for (var j = 0; j < order; j++)
                        {
                            toeplitz[i, j] = gamma[Math.Abs(i - j)];
                        }
                    }

                    phi = gamma[0] <= 0 ? new double[order] : Matrix.Solve(toeplitz, rhs, true);
                    variance = gamma[0] - Enumerable.Range(0, order).Sum(k => phi[k] * gamma[k + 1]);
                }

                variance = Math.Max(variance, 0);
                var aic = variance <= 0 ? double.NegativeInfinity : n * Math.Log(variance) + 2 * (order + 1);
                var bic = variance <= 0 ? double.NegativeInfinity : n * Math.Log(variance) + Math.Log(n) * (order + 1);

                return new AutoregressiveFitResult("YuleWalker", order, mean, mean * (1 - Enumerable.Sum(phi)), phi,
                    variance, aic,
                    bic, n);
            }

            var rows = n - order;
            var design = new double[rows, order + 1];
            var target = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                target[i] = y[i + order];
                design[i, 0] = 1;

                for (var k = 1; k <= order; k++)
                {
                    design[i, k] = y[i + order - k];
                }
            }

            var coefficients = TimeSeriesGuard.LeastSquares(design, target);
            var rss = TimeSeriesGuard.ResidualSumOfSquares(design, target, coefficients);
            var sigma2 = rss / rows;
            var phiLs = coefficients.Skip(1).ToArray();
            var denominator = 1 - Enumerable.Sum(phiLs);
            var lsAic = sigma2 <= 1e-300 ? double.NegativeInfinity : rows * Math.Log(sigma2) + 2 * (order + 1);

            var lsBic = sigma2 <= 1e-300
                ? double.NegativeInfinity
                : rows * Math.Log(sigma2) + Math.Log(rows) * (order + 1);

            return new AutoregressiveFitResult("LeastSquares", order,
                Math.Abs(denominator) < 1e-12 ? double.NaN : coefficients[0] / denominator,
                coefficients[0], phiLs, sigma2, lsAic, lsBic, rows);
        }

        private static void RequireOrder(double[] y, int order, AutoregressiveEstimation estimation)
        {
            TimeSeriesGuard.AtLeast(order, 1, nameof(order));

            if (y.Length < order + (estimation == AutoregressiveEstimation.LeastSquares ? order + 2 : 2))
            {
                throw new ArgumentException(
                    $"values has {y.Length} points, too few for an AR({order}) fit. Use a lower order or more data.",
                    nameof(order));
            }
        }

        private static AutoregressiveFitResult FitAr0(double[] y)
        {
            var n = y.Length;
            var variance = TimeSeriesGuard.Autocovariances(y, 0)[0];
            var mean = TimeSeriesGuard.Mean(y);

            return new AutoregressiveFitResult("YuleWalker", 0, mean, mean, Array.Empty<double>(), variance,
                variance <= 0 ? double.NegativeInfinity : n * Math.Log(variance) + 2,
                variance <= 0 ? double.NegativeInfinity : n * Math.Log(variance) + Math.Log(n), n);
        }

        internal static SeriesForecastResult ArForecast(double[] y, int horizon, int order,
            AutoregressiveEstimation estimation, double z, double confidence)
        {
            var fit = FitAr(y, order, estimation);
            var n = y.Length;
            var extended = y.ToList();
            var forecast = new double[horizon];

            for (var h = 0; h < horizon; h++)
            {
                var value = fit.Intercept;

                for (var k = 1; k <= order; k++)
                {
                    value += fit.Coefficients[k - 1] * extended[extended.Count - k];
                }

                forecast[h] = value;
                extended.Add(value);
            }

            var psi = new double[horizon];
            psi[0] = 1;

            for (var j = 1; j < horizon; j++)
            {
                for (var k = 1; k <= Math.Min(j, order); k++)
                {
                    psi[j] += fit.Coefficients[k - 1] * psi[j - k];
                }
            }

            var fitted = new double[n];

            for (var t = 0; t < n; t++)
            {
                if (t < order)
                {
                    fitted[t] = double.NaN;
                    continue;
                }

                fitted[t] = fit.Intercept + Enumerable.Range(1, order).Sum(k => fit.Coefficients[k - 1] * y[t - k]);
            }

            return Build("Autoregressive" + order + fit.Method, forecast, fitted, Math.Sqrt(fit.InnovationVariance),
                h => psi.Take(h).Sum(p => p * p), z, confidence, new[] { "order", "intercept", "innovationVariance" },
                new[] { order, fit.Intercept, fit.InnovationVariance },
                "Intervals use the moving-average (psi) weights of the fitted AR model.");
        }
    }
}
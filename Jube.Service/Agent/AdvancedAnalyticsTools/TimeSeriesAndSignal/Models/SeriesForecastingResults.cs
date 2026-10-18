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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models
{
    public sealed record SeriesForecastResult(
        string Method,
        double[] Forecast,
        double[] Lower,
        double[] Upper,
        double[] Fitted,
        double ResidualStandardDeviation,
        double ConfidenceLevel,
        string[] ParameterNames,
        double[] ParameterValues,
        string Note);

    public sealed record AutoregressiveFitResult(
        string Method,
        int Order,
        double Mean,
        double Intercept,
        double[] Coefficients,
        double InnovationVariance,
        double Aic,
        double Bic,
        int Observations);

    public sealed record AutoregressiveOrderResult(
        int[] Orders,
        double[] Aic,
        double[] Bic,
        int BestOrderByAic,
        int BestOrderByBic);

    public sealed record ForecastAccuracyResult(
        int Observations,
        double MeanError,
        double MeanAbsoluteError,
        double MedianAbsoluteError,
        double MeanSquaredError,
        double RootMeanSquaredError,
        double MeanAbsolutePercentageError,
        double SymmetricMeanAbsolutePercentageError,
        double MeanAbsoluteScaledError,
        int PercentageErrorsSkipped);

    public sealed record HoldOutMethodScore(
        string Method,
        double MeanAbsoluteError,
        double RootMeanSquaredError,
        double MeanAbsolutePercentageError,
        double SymmetricMeanAbsolutePercentageError,
        double MeanAbsoluteScaledError,
        double[] Forecast);

    public sealed record HoldOutComparisonResult(
        HoldOutMethodScore[] Scores,
        string BestMethodByRmse,
        int TrainingLength,
        int HoldOut,
        string[] Skipped);
}
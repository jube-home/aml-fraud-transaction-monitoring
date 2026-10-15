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
    public sealed record AutocorrelationResult(
        int[] Lags,
        double[] Values,
        double WhiteNoiseBound,
        double[] BartlettBounds,
        int[] SignificantLags,
        int Observations,
        double ConfidenceLevel,
        string Note);

    public sealed record CrossCorrelationResult(
        int[] Lags,
        double[] Correlations,
        double Bound,
        int BestLag,
        double BestCorrelation,
        int[] SignificantLags,
        int Observations,
        string Note);

    public sealed record PortmanteauTestResult(
        string Test,
        double Statistic,
        int DegreesOfFreedom,
        double PValue,
        bool RejectsNoAutocorrelation,
        int Lags,
        int Observations,
        double[] StatisticByLag,
        double[] PValueByLag);

    public sealed record DurbinWatsonResult(
        double Statistic,
        double ImpliedFirstOrderAutocorrelation,
        string Reading,
        int Observations);

    public sealed record RandomnessTestResult(
        string Test,
        double Statistic,
        double ExpectedUnderRandomness,
        double StandardError,
        double ZScore,
        double PValue,
        bool RejectsRandomness,
        int Observations,
        string Note);

    public sealed record MannKendallResult(
        double S,
        double Variance,
        double ZScore,
        double PValue,
        double KendallTau,
        double SenSlopePerStep,
        double SenIntercept,
        string Trend,
        int Observations);

    public sealed record SeasonalDecompositionResult(
        double[] Trend,
        double[] Seasonal,
        double[] Remainder,
        double[] SeasonalIndices,
        double SeasonalStrength,
        double TrendStrength,
        int Period,
        string Model);

    public sealed record SeasonalityStrengthResult(
        double SeasonalStrength,
        double TrendStrength,
        string Reading,
        int Period);

    public sealed record SeasonalPeriodEstimate(
        int Period,
        double AutocorrelationAtPeriod,
        double Bound,
        int[] CandidatePeriods,
        double[] CandidateAutocorrelations,
        bool Found,
        string Note);

    public sealed record LagRegressionTestResult(
        string Test,
        double Statistic,
        double PValue,
        int DegreesOfFreedomNumerator,
        int DegreesOfFreedomDenominator,
        double RSquaredOrRestrictedFit,
        int Lags,
        int Observations,
        bool Significant,
        string Note);
}
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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models
{
    public sealed class DiagnosticLikelihoodRatioResult
    {
        public double Sensitivity { get; init; }
        public double Specificity { get; init; }
        public double PositivePredictiveValue { get; init; }
        public double NegativePredictiveValue { get; init; }
        public double Prevalence { get; init; }
        public double PositiveLikelihoodRatio { get; init; }
        public double PositiveLikelihoodRatioLower { get; init; }
        public double PositiveLikelihoodRatioUpper { get; init; }
        public double NegativeLikelihoodRatio { get; init; }
        public double NegativeLikelihoodRatioLower { get; init; }
        public double NegativeLikelihoodRatioUpper { get; init; }
        public double DiagnosticOddsRatio { get; init; }
        public double DiagnosticOddsRatioLower { get; init; }
        public double DiagnosticOddsRatioUpper { get; init; }
        public double PretestProbability { get; init; }
        public double PostTestProbabilityAfterPositive { get; init; }
        public double PostTestProbabilityAfterNegative { get; init; }
        public double ConfidenceLevel { get; init; }
    }
}
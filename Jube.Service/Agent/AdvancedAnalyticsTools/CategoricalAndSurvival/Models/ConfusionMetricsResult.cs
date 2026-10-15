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
    public sealed class ConfusionMetricsResult
    {
        public double Threshold { get; init; }
        public int TruePositives { get; init; }
        public int FalsePositives { get; init; }
        public int TrueNegatives { get; init; }
        public int FalseNegatives { get; init; }
        public int Total { get; init; }
        public double Prevalence { get; init; }
        public double Accuracy { get; init; }
        public double AccuracyLower { get; init; }
        public double AccuracyUpper { get; init; }
        public double Precision { get; init; }
        public double PrecisionLower { get; init; }
        public double PrecisionUpper { get; init; }
        public double Recall { get; init; }
        public double RecallLower { get; init; }
        public double RecallUpper { get; init; }
        public double Specificity { get; init; }
        public double SpecificityLower { get; init; }
        public double SpecificityUpper { get; init; }
        public double NegativePredictiveValue { get; init; }
        public double FalsePositiveRate { get; init; }
        public double FalseNegativeRate { get; init; }
        public double FalseDiscoveryRate { get; init; }
        public double FalseOmissionRate { get; init; }
        public double Beta { get; init; }
        public double F1 { get; init; }
        public double FBeta { get; init; }
        public double MatthewsCorrelation { get; init; }
        public double CohenKappa { get; init; }
        public double BalancedAccuracy { get; init; }
        public double YoudenJ { get; init; }
        public double Markedness { get; init; }
        public double GeometricMean { get; init; }
        public double PositiveLikelihoodRatio { get; init; }
        public double NegativeLikelihoodRatio { get; init; }
        public double DiagnosticOddsRatio { get; init; }
        public double PredictedPositiveRate { get; init; }
        public double Lift { get; init; }
        public double ExpectedCostPerCase { get; init; }
    }
}
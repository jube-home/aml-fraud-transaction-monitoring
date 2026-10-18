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

using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Regression;
using Newtonsoft.Json;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsTrainingArguments
    {
        public string Family { get; set; } = "LogisticRegression";
        public int Rows { get; set; }
        public string ModelName { get; set; }
        public string CorrelationKey { get; set; }
        public string[] VariableNames { get; set; }
        public int[] ProcessingTypeIds { get; set; }
        public VariableTransformationKind[] TransformationIds { get; set; }
        public string[] TransformationParametersJson { get; set; }
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;
        public double RidgeLambda { get; set; }
        public LogisticClassWeighting ClassWeighting { get; set; } = LogisticClassWeighting.None;

        public LogisticMissingValuePolicy MissingValuePolicy { get; set; } =
            LogisticMissingValuePolicy.ImputeMedianWithIndicators;

        public bool Standardise { get; set; } = true;
        public bool Calibrate { get; set; } = true;
        public double? BaseRateOverride { get; set; }
        public double? CostFalseNegative { get; set; }
        public double? CostFalsePositive { get; set; }
        public double? DecisionThreshold { get; set; }
        public int MaximumIterations { get; set; } = 50;
        public double Tolerance { get; set; } = 1e-8;
        public AdvancedAnalyticsTrainingInput Input { get; set; }

        public LogisticRegressionOptions ToOptions()
        {
            return new LogisticRegressionOptions
            {
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed,
                RidgeLambda = RidgeLambda,
                ClassWeighting = ClassWeighting,
                MissingValuePolicy = MissingValuePolicy,
                Standardise = Standardise,
                Calibrate = Calibrate,
                BaseRateOverride = BaseRateOverride,
                Costs = CostFalseNegative.HasValue
                    ? new CostMatrix(CostFalseNegative.Value, CostFalsePositive!.Value)
                    : null,
                DecisionThreshold = DecisionThreshold,
                MaximumIterations = MaximumIterations,
                Tolerance = Tolerance
            };
        }

        public string ToJson()
        {
            return AdvancedAnalyticsEvidenceJson.Serialise(this);
        }

        public static AdvancedAnalyticsTrainingArguments FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }

            try
            {
                return AdvancedAnalyticsEvidenceJson.Deserialise<AdvancedAnalyticsTrainingArguments>(json)
                       ?? throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }
            catch (JsonException)
            {
                throw new AdvancedAnalyticsTrainingInputException("The run arguments could not be read.");
            }
        }

        public static long InlineSizeBytes(AdvancedAnalyticsTrainingInput input)
        {
            return input.Inputs == null || input.Labels == null
                ? 0
                : AdvancedAnalyticsEvidenceJson.Serialise(input).Length;
        }
    }
}
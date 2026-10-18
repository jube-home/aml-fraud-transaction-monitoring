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

using Jube.Engine.AdvancedAnalytics.Regression;
using Newtonsoft.Json;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsLinearRegressionTrainingArguments
    {
        public string Family { get; set; } = "MultipleLinearRegression";
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

        public LinearMissingValuePolicy MissingValuePolicy { get; set; } =
            LinearMissingValuePolicy.ImputeMedianWithIndicators;

        public bool Standardise { get; set; } = true;
        public double[][] Inputs { get; set; }
        public double[] Outputs { get; set; }

        public LinearRegressionOptions ToOptions()
        {
            return new LinearRegressionOptions
            {
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed,
                MissingValuePolicy = MissingValuePolicy,
                Standardise = Standardise
            };
        }

        public string ToJson()
        {
            return AdvancedAnalyticsEvidenceJson.Serialise(this);
        }

        public static AdvancedAnalyticsLinearRegressionTrainingArguments FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }

            try
            {
                return AdvancedAnalyticsEvidenceJson
                           .Deserialise<AdvancedAnalyticsLinearRegressionTrainingArguments>(json)
                       ?? throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }
            catch (JsonException)
            {
                throw new AdvancedAnalyticsTrainingInputException("The run arguments could not be read.");
            }
        }

        public AdvancedAnalyticsLinearRegressionTrainingArguments WithoutData()
        {
            return new AdvancedAnalyticsLinearRegressionTrainingArguments
            {
                Family = Family,
                Rows = Rows,
                ModelName = ModelName,
                CorrelationKey = CorrelationKey,
                VariableNames = VariableNames,
                ProcessingTypeIds = ProcessingTypeIds,
                TransformationIds = TransformationIds,
                TransformationParametersJson = TransformationParametersJson,
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed,
                MissingValuePolicy = MissingValuePolicy,
                Standardise = Standardise
            };
        }

        public static long InlineSizeBytes(double[][] inputs, double[] outputs)
        {
            return inputs == null || outputs == null
                ? 0
                : AdvancedAnalyticsEvidenceJson.Serialise(new { inputs, outputs }).Length;
        }
    }
}
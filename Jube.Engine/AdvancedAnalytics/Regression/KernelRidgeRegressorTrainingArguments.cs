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

using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Newtonsoft.Json;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public sealed class KernelRidgeRegressorTrainingArguments
    {
        public string Family { get; set; } = "KernelRidgeRegressor";
        public int Rows { get; set; }
        public string ModelName { get; set; }
        public string CorrelationKey { get; set; }
        public string[] VariableNames { get; set; }
        public int[] ProcessingTypeIds { get; set; }
        public VariableTransformationKind[] TransformationIds { get; set; }
        public string[] TransformationParametersJson { get; set; }
        public double[][] Inputs { get; set; }
        public double[] Outputs { get; set; }
        public double Complexity { get; set; } = 1.0;
        public KernelRidgeRegressorKernel Kernel { get; set; } = KernelRidgeRegressorKernel.Gaussian;
        public double Gamma { get; set; }
        public int Degree { get; set; } = 3;
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;

        public KernelRidgeRegressorOptions ToOptions()
        {
            return new KernelRidgeRegressorOptions
            {
                Complexity = Complexity,
                Kernel = Kernel,
                Gamma = Gamma,
                Degree = Degree,
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed
            };
        }

        public string ToJson()
        {
            return AdvancedAnalyticsEvidenceJson.Serialise(this);
        }

        public static KernelRidgeRegressorTrainingArguments FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }

            try
            {
                return AdvancedAnalyticsEvidenceJson.Deserialise<KernelRidgeRegressorTrainingArguments>(json)
                       ?? throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }
            catch (JsonException)
            {
                throw new AdvancedAnalyticsTrainingInputException("The run arguments could not be read.");
            }
        }

        public KernelRidgeRegressorTrainingArguments WithoutData()
        {
            return new KernelRidgeRegressorTrainingArguments
            {
                Family = Family,
                Rows = Rows,
                ModelName = ModelName,
                CorrelationKey = CorrelationKey,
                VariableNames = VariableNames,
                ProcessingTypeIds = ProcessingTypeIds,
                TransformationIds = TransformationIds,
                TransformationParametersJson = TransformationParametersJson,
                Complexity = Complexity,
                Kernel = Kernel,
                Gamma = Gamma,
                Degree = Degree,
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed
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
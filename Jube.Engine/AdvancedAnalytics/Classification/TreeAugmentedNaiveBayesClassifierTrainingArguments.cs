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
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Newtonsoft.Json;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    public sealed class TreeAugmentedNaiveBayesClassifierTrainingArguments
    {
        public string Family { get; set; } = "TreeAugmentedNaiveBayesClassifier";
        public int Rows { get; set; }
        public string ModelName { get; set; }
        public string CorrelationKey { get; set; }
        public string[] VariableNames { get; set; }
        public int[] ProcessingTypeIds { get; set; }
        public VariableTransformationKind[] TransformationIds { get; set; }
        public string[] TransformationParametersJson { get; set; }
        public double[][] Inputs { get; set; }
        public int[] Labels { get; set; }
        public AdvancedAnalyticsTrainingInput Input { get; set; }
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;
        public double Smoothing { get; set; } = 1d;
        public double VarianceRegularisation { get; set; } = 1e-4;
        public double Ridge { get; set; } = 1e-6;
        public bool Calibrate { get; set; } = true;
        public double DecisionThreshold { get; set; } = 0.5;

        public TreeAugmentedNaiveBayesClassifierOptions ToOptions()
        {
            return new TreeAugmentedNaiveBayesClassifierOptions
            {
                ValidationFraction = ValidationFraction,
                TestFraction = TestFraction,
                Seed = Seed,
                Smoothing = Smoothing,
                VarianceRegularisation = VarianceRegularisation,
                Ridge = Ridge,
                Calibrate = Calibrate,
                DecisionThreshold = DecisionThreshold
            };
        }

        public string ToJson()
        {
            return AdvancedAnalyticsEvidenceJson.Serialise(this);
        }

        public static TreeAugmentedNaiveBayesClassifierTrainingArguments FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }

            try
            {
                return AdvancedAnalyticsEvidenceJson
                           .Deserialise<TreeAugmentedNaiveBayesClassifierTrainingArguments>(json)
                       ?? throw new AdvancedAnalyticsTrainingInputException("The run carries no arguments.");
            }
            catch (JsonException)
            {
                throw new AdvancedAnalyticsTrainingInputException("The run arguments could not be read.");
            }
        }

        public TreeAugmentedNaiveBayesClassifierTrainingArguments WithoutData()
        {
            return new TreeAugmentedNaiveBayesClassifierTrainingArguments
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
                Smoothing = Smoothing,
                VarianceRegularisation = VarianceRegularisation,
                Ridge = Ridge,
                Calibrate = Calibrate,
                DecisionThreshold = DecisionThreshold,
                Input = Input
            };
        }
    }
}
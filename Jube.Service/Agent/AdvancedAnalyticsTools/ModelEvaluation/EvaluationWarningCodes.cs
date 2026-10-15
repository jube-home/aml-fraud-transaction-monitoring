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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class EvaluationWarningCodes
    {
        public const string TinyTestSet = "TinyTestSet";
        public const string FewPositives = "FewTrainingPositives";
        public const string SevereImbalance = "SevereImbalanceWithoutHandling";
        public const string CalibrationSkipped = "CalibrationSkipped";
        public const string CalibratorNotIncreasing = "CalibratorNotIncreasing";
        public const string UncalibratedAfterResampling = "ProbabilitiesNotCalibrated";
        public const string Overfit = "TrainTestGap";
        public const string SuspiciouslyPerfect = "SuspiciouslyPerfectScore";
        public const string AntiPredictive = "WorseThanChance";
        public const string NoSignal = "NoDetectableSignal";
        public const string ConstantColumns = "ConstantColumns";
        public const string MissingValues = "MissingValuesImputed";
        public const string LowEventsPerVariable = "LowEventsPerVariable";
        public const string WeightsUnsupported = "ClassWeightsUnsupported";
        public const string Resampled = "TrainingRowsResampled";
        public const string NoValidation = "NoValidationSet";
        public const string SmallSample = "SmallSampleForModel";
        public const string SelectionOnValidation = "SettingChosenOnValidation";
        public const string EdgeOfGrid = "BestAtEdgeOfGrid";
        public const string Optimism = "SelectionOptimism";
        public const string FoldsDisagree = "FoldsDisagree";
        public const string NoTestSet = "NoTestSet";
        public const string Truncated = "SubsampledForImportance";
    }
}
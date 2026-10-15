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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery
{
    [Description("One promoted trial instance of an Exhaustive Search Instance: a trial topology that was " +
                 "promoted out of the search, with its score, complexity and serialised network definition.")]
    public class ExhaustiveSearchInstancePromotedTrialInstanceQueryDto
    {
        [Description("Server-assigned integer identifier of the promoted trial instance.")]
        public int Id { get; set; }

        [Description("Identifier of the Exhaustive Search Instance Trial Instance this row was promoted from.")]
        public int ExhaustiveSearchInstanceTrialInstanceId { get; set; }

        [Description("True when the promoted trial instance is the active one.")]
        public bool Active { get; set; }

        [Description("Score achieved by the trial instance.")]
        public double Score { get; set; }

        [Description("Topology complexity of the trial instance.")]
        public int TopologyComplexity { get; set; }

        [Description("The model kind this trial was: null for a Neural Network topology, or the challenger " +
                     "family name (e.g. RandomForest) otherwise.")]
        public string? Kind { get; set; }

        [Description("Serialised JSON definition of the promoted trial instance.")]
        public string? Json { get; set; }

        [Description("UTC date and time the promoted trial instance was created.")]
        public DateTime CreatedDate { get; set; }

        [Description("Calibration method fitted at promotion: platt, or none when calibration was skipped. Null " +
                     "for a model promoted before calibration was introduced, which serves raw scores.")]
        public string? CalibrationMethod { get; set; }

        [Description("What the Platt calibrator reads: raw (the network output) or logit (the logit of the bounded " +
                     "network output).")]
        public string? CalibrationInput { get; set; }

        [Description("Platt calibrator slope, fitted on the validation split.")]
        public double? CalibrationSlope { get; set; }

        [Description("Platt calibrator intercept, fitted on the validation split.")]
        public double? CalibrationIntercept { get; set; }

        [Description("Clamp applied to the network output before the logit when the calibration input is logit.")]
        public double? CalibrationEpsilon { get; set; }

        [Description("Positive class proportion of the training split the network learned from.")]
        public double? TrainingPrior { get; set; }

        [Description("Positive class proportion of the validation split, the prior at which the calibrator " +
                     "maps before the base rate shift.")]
        public double? CalibrationPrior { get; set; }

        [Description("Deployment base rate the recalled probability is calibrated to.")]
        public double? BaseRate { get; set; }

        [Description("Where the base rate came from: Override, PopulationCount, AnomalyFlaggedFraction or " +
                     "SamplePrior (no shift).")]
        public string? BaseRateSource { get; set; }

        [Description("Number of validation rows the calibrator was fitted on.")]
        public int? CalibrationSampleSize { get; set; }

        [Description("Calibrated, CalibratedLowSample or Skipped; null for a model promoted before calibration " +
                     "was introduced.")]
        public string? CalibrationStatus { get; set; }

        [Description("Why calibration was skipped, or the low sample warning.")]
        public string? CalibrationReason { get; set; }

        [Description("Raw versus calibrated expected calibration error, Brier score and log loss on the " +
                     "validation and test splits, as JSON.")]
        public string? CalibrationQualityJson { get; set; }

        [Description("Isotonic or beta calibrator parameters, as JSON; null for Platt (which uses " +
                     "CalibrationSlope/CalibrationIntercept) or when not calibrated.")]
        public string? CalibrationParametersJson { get; set; }
    }
}
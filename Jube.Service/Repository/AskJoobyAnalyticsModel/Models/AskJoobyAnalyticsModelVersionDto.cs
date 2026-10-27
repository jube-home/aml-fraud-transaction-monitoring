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

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public class AskJoobyAnalyticsModelVersionDto
    {
        [Description("Numeric identifier of the version.")]
        public int Id { get; set; }

        [Description("Globally unique identifier of the version.")]
        public Guid Guid { get; set; }

        [Description("Identifier of the trained model this version belongs to.")]
        public int TrainedModelId { get; set; }

        [Description("Sequence number of the version within its model, starting at 1. A version never changes " +
                     "once stored.")]
        public int VersionNumber { get; set; }

        [Description("True when this is the version the model currently serves.")]
        public bool IsActive { get; set; }

        [Description("Identifier of the training run that produced the version, or null for an uploaded one.")]
        public int? ProducingRunId { get; set; }

        [Description("User who caused the version to be stored.")]
        public string? CreatedUser { get; set; }

        [Description("When the version was stored, in UTC.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Rows in the held-out test split, or null when no test split was kept.")]
        public int? TestRows { get; set; }

        [Description("ROC AUC on the held-out test split, or null when it is undefined.")]
        public double? TestRocAuc { get; set; }

        [Description("Average precision on the held-out test split, or null when it is undefined.")]
        public double? TestAveragePrecision { get; set; }

        [Description("Log loss on the held-out test split, or null when it is undefined.")]
        public double? TestLogLoss { get; set; }

        [Description("Brier score on the held-out test split, or null when it is undefined.")]
        public double? TestBrierScore { get; set; }

        [Description("Expected calibration error on the held-out test split, or null when it is undefined.")]
        public double? TestExpectedCalibrationError { get; set; }

        [Description("Codes of the warnings the trainer raised for this version.")]
        public string[] WarningCodes { get; set; } = [];

        [Description("How many of the version's variables carry a recorded single-variable transformation, as " +
                     "'n of total variables transformed'.")]
        public string? TransformedVariableCount { get; set; }

        [Description("The complete stored evidence, shaped by the model's kind (a LogisticRegression classifier " +
                     "carries coefficients and calibration; other kinds carry their own details). Only filled in " +
                     "when a single version is fetched.")]
        public object? Evidence { get; set; }
    }
}
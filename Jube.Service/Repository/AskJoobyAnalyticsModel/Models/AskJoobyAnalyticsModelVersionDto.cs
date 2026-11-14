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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    [MessagePackObject]
    public class AskJoobyAnalyticsModelVersionDto
    {
        [Description("Numeric identifier of the version.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Globally unique identifier of the version.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("Identifier of the trained model this version belongs to.")]
        [Key(2)]
        public int TrainedModelId { get; set; }

        [Description("Sequence number of the version within its model, starting at 1. A version never changes " +
                     "once stored.")]
        [Key(3)]
        public int VersionNumber { get; set; }

        [Description("True when this is the version the model currently serves.")]
        [Key(4)]
        public bool IsActive { get; set; }

        [Description("Identifier of the training run that produced the version, or null for an uploaded one.")]
        [Key(5)]
        public int? ProducingRunId { get; set; }

        [Description("User who caused the version to be stored.")]
        [Key(6)]
        public string? CreatedUser { get; set; }

        [Description("When the version was stored, in UTC.")]
        [Key(7)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Rows in the held-out test split, or null when no test split was kept.")]
        [Key(8)]
        public int? TestRows { get; set; }

        [Description("ROC AUC on the held-out test split, or null when it is undefined.")]
        [Key(9)]
        public double? TestRocAuc { get; set; }

        [Description("Average precision on the held-out test split, or null when it is undefined.")]
        [Key(10)]
        public double? TestAveragePrecision { get; set; }

        [Description("Log loss on the held-out test split, or null when it is undefined.")]
        [Key(11)]
        public double? TestLogLoss { get; set; }

        [Description("Brier score on the held-out test split, or null when it is undefined.")]
        [Key(12)]
        public double? TestBrierScore { get; set; }

        [Description("Expected calibration error on the held-out test split, or null when it is undefined.")]
        [Key(13)]
        public double? TestExpectedCalibrationError { get; set; }

        [Description("Codes of the warnings the trainer raised for this version.")]
        [Key(14)]
        public string[] WarningCodes { get; set; } = [];

        [Description("How many of the version's variables carry a recorded single-variable transformation, as " +
                     "'n of total variables transformed'.")]
        [Key(15)]
        public string? TransformedVariableCount { get; set; }

        [Description("The complete stored evidence, shaped by the model's kind (a LogisticRegression classifier " +
                     "carries coefficients and calibration; other kinds carry their own details). Only filled in " +
                     "when a single version is fetched.")]
        [Key(16)]
        public object? Evidence { get; set; }
    }
}
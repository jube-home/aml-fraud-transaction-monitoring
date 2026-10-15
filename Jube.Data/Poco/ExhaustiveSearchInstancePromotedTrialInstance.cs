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

using System;
using Jube.Data.Repository.Models;
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public double? Score { get; set; }
        [Column] [Nullable] [Key(2)] public int? TopologyComplexity { get; set; }
        [Column] [Nullable] [Key(3)] public int? TruePositive { get; set; }
        [Column] [Nullable] [Key(4)] public int? TrueNegative { get; set; }
        [Column] [Nullable] [Key(5)] public int? FalsePositive { get; set; }
        [Column] [Nullable] [Key(6)] public int? FalseNegative { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(7)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(8)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId",
            OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(11)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public int? ImportId { get; set; }
        [Column] [Nullable] [Key(15)] public ExhaustiveCalibrationMethod? CalibrationMethodId { get; set; }
        [Column] [Nullable] [Key(16)] public ExhaustiveCalibrationInput? CalibrationInputId { get; set; }
        [Column] [Nullable] [Key(17)] public double? CalibrationSlope { get; set; }
        [Column] [Nullable] [Key(18)] public double? CalibrationIntercept { get; set; }
        [Column] [Nullable] [Key(19)] public double? CalibrationEpsilon { get; set; }
        [Column] [Nullable] [Key(20)] public double? TrainingPrior { get; set; }
        [Column] [Nullable] [Key(21)] public double? CalibrationPrior { get; set; }
        [Column] [Nullable] [Key(22)] public double? BaseRate { get; set; }
        [Column] [Nullable] [Key(23)] public ExhaustiveBaseRateSource? BaseRateSourceId { get; set; }
        [Column] [Nullable] [Key(24)] public int? CalibrationSampleSize { get; set; }
        [Column] [Nullable] [Key(25)] public ExhaustiveCalibrationStatus? CalibrationStatusId { get; set; }
        [Column] [Nullable] [Key(26)] public string CalibrationReason { get; set; }
        [Column] [Nullable] [Key(27)] public string CalibrationQualityJson { get; set; }
        [Column] [Nullable] [Key(28)] public string CalibrationParametersJson { get; set; }
        [Column] [Nullable] [Key(29)] public AdvancedAnalyticsModelKind? KindId { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(30)]
        public string MetricsJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(31)]
        public string VariablesJson { get; set; }
    }
}
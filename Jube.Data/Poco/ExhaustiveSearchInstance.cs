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
using System.Collections.Generic;
using Jube.Data.Repository.Models;
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(7)] public int? ModelsSinceBest { get; set; }
        [Column] [Nullable] [Key(8)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(9)] public string Name { get; set; }
        [Column] [Nullable] [Key(10)] public byte? StatusId { get; set; }
        [Column] [Nullable] [Key(11)] public int? Models { get; set; }
        [Column] [Nullable] [Key(12)] public double? Score { get; set; }
        [Column] [Nullable] [Key(13)] public int? TopologyComplexity { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public byte[] Object { get; set; }
        [Column] [Nullable] [Key(17)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(18)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(20)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(21)] public int? Version { get; set; }
        [Column] [Nullable] [Key(22)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(23)] public byte? Anomaly { get; set; }
        [Column] [Nullable] [Key(24)] public double AnomalyProbability { get; set; }
        [Column] [Nullable] [Key(25)] public byte? Filter { get; set; }
        [Column] [Nullable] [Key(26)] public string FilterSql { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(27)]
        public string FilterJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(28)]
        public string FilterTokens { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(29)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(30)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstance> ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(31)]
        public IEnumerable<ExhaustiveSearchInstanceVariable> ExhaustiveSearchInstanceVariable { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(32)]
        public IEnumerable<ExhaustiveSearchInstanceData> ExhaustiveSearchInstanceData { get; set; }

        [Column] [Nullable] [Key(33)] public int? ImportId { get; set; }
        [Column] [Nullable] [Key(34)] public string CategoricalOptionsJson { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(35)]
        public IEnumerable<ExhaustiveSearchInstanceCategoricalField> ExhaustiveSearchInstanceCategoricalField
        {
            get;
            set;
        }

        [Column] [Nullable] [Key(36)] public int? AnomalyAlgorithmId { get; set; }
        [Column] [Nullable] [Key(37)] public string AnomalyParametersJson { get; set; }
        [Column] [Nullable] [Key(38)] public string AnomalyReportJson { get; set; }
        [Column] [Nullable] [Key(40)] public byte? DataSourceId { get; set; }
        [Column] [Nullable] [Key(41)] public int? ExhaustiveInstanceCsvId { get; set; }
        [Column] [Nullable] [Key(42)] public int? ExhaustiveInstanceJsonId { get; set; }
        [Column] [Nullable] [Key(43)] public double? BaseRate { get; set; }
        [Column] [Nullable] [Key(44)] public ExhaustiveCalibrationMethod? CalibrationMethodId { get; set; }
        [Column] [Nullable] [Key(45)] public byte? Bootstrap { get; set; }
        [Column] [Nullable] [Key(46)] public int? BootstrapRowsAdded { get; set; }
        [Column] [Nullable] [Key(47)] public string BootstrapMethod { get; set; }
        [Column] [Nullable] [Key(48)] public int? BootstrapTo { get; set; }
        [Column] [Nullable] [Key(49)] public double? BootstrapWeightFactor { get; set; }
        [Column] [Nullable] [Key(50)] public double? ChallengerModelBaseProbability { get; set; }
    }
}
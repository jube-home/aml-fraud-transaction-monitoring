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
    public class ExhaustiveSearchInstanceTrialInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(3)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(4)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance> ExhaustiveSearchInstancePromotedTrialInstance
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(5)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActual { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(6)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>
            ExhaustiveSearchInstancePromotedTrialInstanceRoc { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(7)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(8)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(9)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceSensitivity>
            ExhaustiveSearchInstanceTrialInstanceSensitivity { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(10)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceVariable> ExhaustiveSearchInstanceTrialInstanceVariable
        {
            get;
            set;
        }

        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(13)] public int? ImportId { get; set; }
        [Column] [Nullable] [Key(14)] public AdvancedAnalyticsModelKind? KindId { get; set; }
        [Column] [Nullable] [Key(15)] public double? Score { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(16)]
        public string Json { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(17)]
        public string MetricsJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(18)]
        public string VariablesJson { get; set; }
    }
}
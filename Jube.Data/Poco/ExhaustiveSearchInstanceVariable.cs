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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariable
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? ProcessingTypeId { get; set; }
        [Column] [Nullable] [Key(4)] public double? Mode { get; set; }
        [Column] [Nullable] [Key(5)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(6)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(7)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(8)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(9)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(10)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(11)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(12)] public byte? PrescriptionSimulation { get; set; }
        [Column] [Nullable] [Key(13)] public byte? NormalisationTypeId { get; set; }
        [Column] [Nullable] [Key(14)] public int? DistinctValues { get; set; }
        [Column] [Nullable] [Key(15)] public double? Correlation { get; set; }
        [Column] [Nullable] [Key(16)] public int? CorrelationAbsRank { get; set; }
        [Column] [Nullable] [Key(17)] public int? Bins { get; set; }
        [Column] [Nullable] [Key(18)] public int? VariableSequence { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(19)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(20)]
        public IEnumerable<ExhaustiveSearchInstanceVariableAnomaly> ExhaustiveSearchInstanceVariableAnomaly
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(21)]
        public IEnumerable<ExhaustiveSearchInstanceVariableClassification>
            ExhaustiveSearchInstanceVariableClassification { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(22)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogram> ExhaustiveSearchInstanceVariableHistogram
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(23)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogramClassification>
            ExhaustiveSearchInstanceVariableHistogramClassification { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(24)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogramAnomaly>
            ExhaustiveSearchInstanceVariableHistogramAnomaly { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(25)]
        public IEnumerable<ExhaustiveSearchInstanceVariableMultiCollinearity>
            ExhaustiveSearchInstanceVariableMultiCollinearity { get; set; }

        [Column] [Nullable] [Key(26)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(27)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(28)] public int? ImportId { get; set; }
        [Column] [Nullable] [Key(29)] public int? CategoricalFieldId { get; set; }
        [Column] [Nullable] [Key(30)] public byte? CategoricalKind { get; set; }
        [Column] [Nullable] [Key(31)] public string CategoricalLevel { get; set; }
        [Column] [Nullable] [Key(32)] public int? CategoricalIndicatorIndex { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(33)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogramClass>
            ExhaustiveSearchInstanceVariableHistogramClass { get; set; }
    }
}
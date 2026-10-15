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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableAnomaly
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Mode { get; set; }
        [Column] [Nullable] [Key(3)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(4)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(5)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(6)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(7)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(8)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(9)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(10)] public int? DistinctValues { get; set; }
        [Column] [Nullable] [Key(11)] public int? Bins { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public ExhaustiveSearchInstanceVariable ExhaustiveSearchInstanceVariable { get; set; }

        [Column] [Nullable] [Key(15)] public int? ImportId { get; set; }
    }
}
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
    public class ExhaustiveSearchInstanceData
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Dependent { get; set; }
        [Column] [Nullable] [Key(3)] public double? Anomaly { get; set; }
        [Column] [Nullable] [Key(4)] public double[] Independent { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }
}
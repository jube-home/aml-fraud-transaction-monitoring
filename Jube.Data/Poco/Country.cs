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

using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class Country
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public string Alpha2 { get; set; }
        [Column] [Nullable] [Key(3)] public string Alpha3 { get; set; }
        [Column] [Nullable] [Key(4)] public int? Numeric { get; set; }
        [Column] [Nullable] [Key(5)] public double? Latitude { get; set; }
        [Column] [Nullable] [Key(6)] public double? Longitude { get; set; }
    }
}
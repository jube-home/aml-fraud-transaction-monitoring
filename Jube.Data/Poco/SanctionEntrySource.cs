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
    public class SanctionEntrySource
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public byte? Severity { get; set; }
        [Column] [Nullable] [Key(3)] public string DirectoryLocation { get; set; }
        [Column] [Nullable] [Key(4)] public char? Delimiter { get; set; }
        [Column] [Nullable] [Key(5)] public string MultiPartStringIndex { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ReferenceIndex { get; set; }
        [Column] [Nullable] [Key(7)] public byte? EnableDirectoryLocation { get; set; }
        [Column] [Nullable] [Key(8)] public byte? EnableHttpLocation { get; set; }
        [Column] [Nullable] [Key(9)] public string HttpLocation { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Skip { get; set; }
    }
}
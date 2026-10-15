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
    public class AskJoobyRagChunk
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public string Hash { get; set; }
        [Column] [Nullable] [Key(2)] public string ParentHash { get; set; }
        [Column] [Nullable] [Key(3)] public byte? CategoryId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? KindId { get; set; }
        [Column] [Nullable] [Key(5)] public string SourceFile { get; set; }
        [Column] [Nullable] [Key(6)] public int? StartLine { get; set; }
        [Column] [Nullable] [Key(7)] public int? EndLine { get; set; }
        [Column] [Nullable] [Key(8)] public string QualifiedName { get; set; }
        [Column] [Nullable] [Key(9)] public string Content { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? CreatedDate { get; set; }
    }
}
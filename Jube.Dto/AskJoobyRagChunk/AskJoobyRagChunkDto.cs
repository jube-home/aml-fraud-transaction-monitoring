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

namespace Jube.Dto.AskJoobyRagChunk
{
    [Description("One Bite returned from a Pasture search -- a chunk of Jube documentation, rule taxonomy, " +
                 "code or database schema.")]
    [MessagePackObject]
    public class AskJoobyRagChunkDto
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string? Hash { get; set; }
        [Key(2)]
        public string? ParentHash { get; set; }
        [Key(3)]
        public string? Category { get; set; }
        [Key(4)]
        public string? Kind { get; set; }
        [Key(5)]
        public string? SourceFile { get; set; }
        [Key(6)]
        public int? StartLine { get; set; }
        [Key(7)]
        public int? EndLine { get; set; }
        [Key(8)]
        public string? QualifiedName { get; set; }
        [Key(9)]
        public string? Content { get; set; }
        [Key(10)]
        public double Score { get; set; }
    }
}
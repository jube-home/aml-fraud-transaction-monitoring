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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

using MessagePack;

namespace Jube.Dto.Query.Graph
{
    [MessagePackObject]
    public class GraphPatternDto
    {
        [Key(0)]
        public long Id { get; set; }

        [Key(1)]
        public int KindId { get; set; }

        [Key(2)]
        public string? Kind { get; set; }

        [Key(3)]
        public GraphNodeDto? Anchor { get; set; }

        [Key(4)]
        public double Score { get; set; }

        [Key(5)]
        public DateTimeOffset WindowFrom { get; set; }

        [Key(6)]
        public DateTimeOffset WindowTo { get; set; }

        [Key(7)]
        public Dictionary<string, double> Parameters { get; set; } = [];

        [Key(8)]
        public List<long> ArchiveIds { get; set; } = [];

        [Key(9)]
        public int MemberCount { get; set; }

        [Key(10)]
        public List<GraphPatternMemberDto> Members { get; set; } = [];

        [Key(11)]
        public int StateId { get; set; }

        [Key(12)]
        public int? CaseId { get; set; }

        [Key(13)]
        public DateTimeOffset FirstDetectedDate { get; set; }

        [Key(14)]
        public DateTimeOffset LastDetectedDate { get; set; }

        [Key(15)]
        public string? DecidedUser { get; set; }

        [Key(16)]
        public DateTimeOffset? DecidedDate { get; set; }

        [Key(17)]
        public string? Sentence { get; set; }

    }
}

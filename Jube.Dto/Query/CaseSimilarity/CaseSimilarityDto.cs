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

namespace Jube.Dto.Query.CaseSimilarity
{
    [Description("The cases of the same workflow that resemble a case, and how the closed ones were decided. Informational only: similarity never changes a case's ranking.")]
    [MessagePackObject]
    public class CaseSimilarityDto
    {
        [Description("False while the case has not yet been compared, because the similarity sweep has not reached it.")]
        [Key(0)]
        public bool Built { get; set; }

        [Description("When the case was last compared (UTC).")]
        [Key(1)]
        public DateTimeOffset? CheckedDate { get; set; }

        [Description("How many similar cases the caller may see.")]
        [Key(2)]
        public int Similar { get; set; }

        [Description("Of the similar cases, how many are closed with an outcome.")]
        [Key(3)]
        public int Closed { get; set; }

        [Description("Of the closed similar cases, how many were confirmed.")]
        [Key(4)]
        public int Confirmed { get; set; }

        [Description("Of the similar cases, how many are still open.")]
        [Key(5)]
        public int Open { get; set; }

        [Description("True when enough similar cases are closed for the confirmed share to mean something.")]
        [Key(6)]
        public bool EnoughToJudge { get; set; }

        [Description("The most similar cases, most alike first.")]
        [Key(7)]
        public List<CaseSimilarNeighbourDto> Neighbours { get; set; } = [];
    }
}

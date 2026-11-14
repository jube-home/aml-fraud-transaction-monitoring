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
    [Description("A case of the same workflow that resembles the case being viewed.")]
    [MessagePackObject]
    public class CaseSimilarNeighbourDto
    {
        [Description("Identifier of the similar case.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("The key field name of the similar case, for example the account.")]
        [Key(1)]
        public string? CaseKey { get; set; }

        [Description("The key value of the similar case.")]
        [Key(2)]
        public string? CaseKeyValue { get; set; }

        [Description("The name of the status the similar case is in.")]
        [Key(3)]
        public string? Status { get; set; }

        [Description("Closed status of the similar case: 0 open, 1 suspended, 2 reopened, 3 closed, 4 bypassed.")]
        [Key(4)]
        public byte ClosedStatusId { get; set; }

        [Description("Outcome of the status the similar case is in: 0 none, 1 confirmed, 2 not confirmed.")]
        [Key(5)]
        public byte OutcomeId { get; set; }

        [Description("When the similar case was created (UTC).")]
        [Key(6)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When the similar case was closed (UTC), if it is closed.")]
        [Key(7)]
        public DateTimeOffset? ClosedDate { get; set; }

        [Description("How alike the two cases are: 2 Moderate, 3 Strong. A similarity, not a probability.")]
        [Key(8)]
        public byte BandId { get; set; }

        [Description("The similarity behind the band, from 0 (nothing shared) to 1 (identical). A similarity, not a probability.")]
        [Key(9)]
        public double Score { get; set; }

        [Description("True when the two cases share a real-world thing, such as a device or counterparty, or sit in the same network community, rather than only looking alike.")]
        [Key(10)]
        public bool Linked { get; set; }

        [Description("The facts the two cases share, rarest first.")]
        [Key(11)]
        public List<CaseSimilarReasonDto> Reasons { get; set; } = [];
    }
}

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
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.SanctionExemption
{
    [Description("A request to treat one sanction list entry as not matching one screened name, for the signed-in " +
                 "user's tenant. Takes effect only when a second person approves it.")]
    [MessagePackObject]
    public class SanctionExemptionRequestDto
    {
        [Description("The name that was screened and matched, as typed. Required, at most 500 characters.")]
        [Key(0)]
        public string? ScreenedName { get; set; }

        [Description("The identifier of the sanction list the matched entry belongs to.")]
        [Key(1)]
        public int SanctionEntrySourceId { get; set; }

        [Description("The reference of the matched entry within its list. Required, at most 500 characters.")]
        [Key(2)]
        public string? SanctionEntryReference { get; set; }

        [Description("Why the match is judged not to be the sanctioned entity. Required, at most 1000 characters.")]
        [Key(3)]
        public string? Reason { get; set; }

        [Description("When the exemption lapses (UTC). Empty means it does not lapse. Must be in the future.")]
        [Key(4)]
        public DateTimeOffset? ExpiryDate { get; set; }
    }

    [Description("A decision on a pending sanction exemption.")]
    [MessagePackObject]
    public class SanctionExemptionDecisionDto
    {
        [Description("Identifier of the exemption.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Optional note recorded with the decision, at most 1000 characters.")]
        [Key(1)]
        public string? Note { get; set; }
    }

    [Description("A sanction exemption, the screened name it applies to and where it stands.")]
    [MessagePackObject]
    public class SanctionExemptionDto
    {
        [Description("Identifier of the exemption.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The name that was screened, as typed.")]
        [Key(1)]
        public string ScreenedName { get; set; } = string.Empty;

        [Description("The sanction list the exempted entry belongs to.")]
        [Key(2)]
        public int SanctionEntrySourceId { get; set; }

        [Description("The reference of the exempted entry within its list.")]
        [Key(3)]
        public string SanctionEntryReference { get; set; } = string.Empty;

        [Description("Why the match is judged not to be the sanctioned entity.")]
        [Key(4)]
        public string Reason { get; set; } = string.Empty;

        [Description("When the exemption lapses (UTC), if it does.")]
        [Key(5)]
        public DateTimeOffset? ExpiryDate { get; set; }

        [Description("Where it stands, as a SanctionExemptionState: 1 pending approval, 2 approved, 3 rejected, 4 " +
                     "retracted.")]
        [Key(6)]
        public short StateId { get; set; }

        [Description("Who asked for it.")]
        [Key(7)]
        public string RequestedUser { get; set; } = string.Empty;

        [Description("When it was asked for (UTC).")]
        [Key(8)]
        public DateTimeOffset RequestedDate { get; set; }

        [Description("Who approved, rejected or retracted it.")]
        [Key(9)]
        public string? DecidedUser { get; set; }

        [Description("When it was decided (UTC).")]
        [Key(10)]
        public DateTimeOffset? DecidedDate { get; set; }

        [Description("The note recorded with the decision.")]
        [Key(11)]
        public string? DecisionNote { get; set; }
    }
}

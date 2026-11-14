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

namespace Jube.Dto.Query.CaseSupervision
{
    [Description("A supervisor's request to place a case under legal hold, or to lift the hold.")]
    [MessagePackObject]
    public class CaseLegalHoldDto
    {
        [Description("Identifier of the case. Required.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("True to place the case under legal hold, false to lift it. While a case is under hold the " +
                     "retention purge never removes the archived transaction it was raised from.")]
        [Key(1)]
        public bool Hold { get; set; }

        [Description("Why the case is under legal hold. Required when placing a hold, at most 500 characters.")]
        [Key(2)]
        public string? Reason { get; set; }
    }

    [Description("Whether a case is under legal hold, and who placed it and why.")]
    [MessagePackObject]
    public class CaseLegalHoldStateDto
    {
        [Description("True when the case is under legal hold.")]
        [Key(0)]
        public bool Hold { get; set; }

        [Description("Why the case is under hold.")]
        [Key(1)]
        public string? Reason { get; set; }

        [Description("Who last placed or lifted the hold.")]
        [Key(2)]
        public string? User { get; set; }

        [Description("UTC. When the hold was last placed or lifted.")]
        [Key(3)]
        public DateTime? Date { get; set; }
    }
}

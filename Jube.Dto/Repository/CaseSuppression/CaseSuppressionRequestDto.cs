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
using System;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseSuppression
{
    [MessagePackObject]
    public class CaseSuppressionRequestDto
    {
        [Description("Identifier of the case the request is made from. Required.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("How the key was chosen as an integer: 1 CaseKey (the case key and value of the case itself), 2 Transaction (a value picked from the distinct values of the case key journal). Required.")]
        [Key(1)]
        public int PathId { get; set; }

        [Description("The override key: the name of a request XPath flagged Enable Override. Required.")]
        [Key(2)]
        public string OverrideKey { get; set; } = string.Empty;

        [Description("The value, chosen from the distinct values the server listed. Required on the transaction path; on the case key path it is the case key value and may be left empty.")]
        [Key(3)]
        public string? OverrideKeyValue { get; set; }

        [Description("Why the suppression is requested. Required, up to 2000 characters.")]
        [Key(4)]
        public string Reason { get; set; } = string.Empty;

        [Description("When the override created on approval should be removed (UTC). Required and in the future.")]
        [Key(5)]
        public DateTimeOffset? ExpiryDate { get; set; }

        [Description("What to suppress or force. At least one, at most 50.")]
        [Key(6)]
        public List<CaseSuppressionTargetRequestDto> Targets { get; set; } = [];

        [Description("Archive ids of the transactions ticked as evidence. Required on the transaction path. Each is verified to be in the case journal and to carry the value.")]
        [Key(7)]
        public List<long> ArchiveIds { get; set; } = [];

        [Description("The merged journal keys the case page shows, as a JSON array of key and value pairs, at most three. Optional.")]
        [Key(8)]
        public string? Merge { get; set; }
    }
}

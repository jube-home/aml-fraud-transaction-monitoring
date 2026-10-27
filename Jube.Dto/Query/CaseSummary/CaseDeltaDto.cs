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
using Jube.Dto.Query.CaseSearch;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("What changed on a case since the caller last looked at it.")]
    public class CaseDeltaDto
    {
        [Description("True when the caller has never looked at the case before.")]
        public bool FirstLook { get; set; }

        [Description("When the caller last looked (UTC); null on a first look.")]
        public DateTimeOffset? LastViewedDate { get; set; }

        [Description("Priority when the caller last looked; null when no assessment existed then.")]
        public int? PriorityThen { get; set; }

        [Description("Priority now.")]
        public int PriorityNow { get; set; }

        [Description("Changes by other users since the caller last looked.")]
        public List<CaseEventSummaryDto> Changes { get; set; } = [];
    }
}

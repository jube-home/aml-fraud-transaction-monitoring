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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSearch
{
    [Description("Why a case is back in front of the caller, and what changed since they last looked.")]
    public class CaseAttentionDto
    {
        [Description("True when the case needs the caller again: they looked at it or hold it, it is open, and at " +
                     "least one reason applies.")]
        public bool Needed { get; set; }

        [Description("The reasons, most important first.")]
        public List<CaseAttentionReasonDto> Reasons { get; set; } = [];

        [Description("When the caller last looked at the case (UTC); null when they never have.")]
        public DateTimeOffset? LastViewedDate { get; set; }

        [Description("How many changes by other users since the caller last looked.")]
        public int ChangesSince { get; set; }

        [Description("By how many points the Priority has risen since the caller last looked.")]
        public int PriorityRise { get; set; }
    }
}

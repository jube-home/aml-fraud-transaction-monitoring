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
    [Description("One earlier assessment in the risk history of a case.")]
    public class CaseRiskHistoryItemDto
    {
        [Description("Position in the history; higher is newer.")]
        public int Sequence { get; set; }

        [Description("When it was computed (UTC).")]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("What caused the recompute: 1 Initial, 2 Activation attached, 3 Linked key changed, " +
                     "4 Neighbour tagged, 5 Diary or lane threshold, 6 Payment hold, 7 Finding recorded, " +
                     "8 Typology changed, 9 Risk model changed, 10 Scheduled sweep.")]
        public int Trigger { get; set; }

        [Description("Urgency lane then.")]
        public int Lane { get; set; }

        [Description("Likelihood then.")]
        public double Likelihood { get; set; }

        [Description("Impact then.")]
        public int Impact { get; set; }

        [Description("Priority then.")]
        public int Priority { get; set; }
    }
}

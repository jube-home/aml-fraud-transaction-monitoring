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

namespace Jube.Dto.Query.CaseSupervision
{
    [Description("What one analyst is holding and has done over a period, for a supervisor.")]
    public class CaseAnalystWorkloadDto
    {
        [Description("The analyst's user name.")]
        public string User { get; set; } = string.Empty;

        [Description("Open cases locked to the analyst now.")]
        public int Holding { get; set; }

        [Description("Minutes the analyst's longest held case has been locked, null when nothing is held.")]
        public double? OldestHoldMinutes { get; set; }

        [Description("Cases the analyst took in the period by skimming or locking them.")]
        public int Taken { get; set; }

        [Description("Cases the analyst peeked at in the period without taking them.")]
        public int Peeks { get; set; }

        [Description("Cases the analyst closed in the period.")]
        public int Closed { get; set; }

        [Description("Median minutes from creation to closure of the cases the analyst closed in the period, null " +
                     "when none.")]
        public double? MedianResolutionMinutes { get; set; }
    }
}

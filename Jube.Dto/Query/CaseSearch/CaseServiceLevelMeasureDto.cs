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
    [Description("One service level target of a case and where it stands.")]
    public class CaseServiceLevelMeasureDto
    {
        [Description("What is measured: 1 FirstView, 2 Ownership, 3 Resolution, 4 StatusDwell.")]
        public int Dimension { get; set; }

        [Description("English name of the dimension.")]
        public string? DimensionName { get; set; }

        [Description("Where it stands: 1 Met, 2 OnTrack, 3 AtRisk (80 percent of the target elapsed), 4 Breached.")]
        public int State { get; set; }

        [Description("English name of the state.")]
        public string? StateName { get; set; }

        [Description("When the target falls or fell due (UTC).")]
        public DateTimeOffset DueDate { get; set; }

        [Description("Share of the target that has elapsed or was used; one means exactly on the target.")]
        public double ElapsedFraction { get; set; }

        [Description("Minutes taken when the target has been reached; null while it has not.")]
        public double? ActualMinutes { get; set; }

        [Description("The target in minutes.")]
        public int TargetMinutes { get; set; }
    }
}

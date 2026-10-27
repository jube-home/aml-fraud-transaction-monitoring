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
    [Description("The service level position of a case across every target that applies to it.")]
    public class CaseServiceLevelDto
    {
        [Description("The worst state across the measures: 0 NoTarget, 1 Met, 2 OnTrack, 3 AtRisk, 4 Breached.")]
        public int State { get; set; }

        [Description("English name of the state.")]
        public string? StateName { get; set; }

        [Description("The measure that decides the state; null when no target applies.")]
        public CaseServiceLevelMeasureDto? Worst { get; set; }

        [Description("Every measure that applies.")]
        public List<CaseServiceLevelMeasureDto> Measures { get; set; } = [];
    }
}

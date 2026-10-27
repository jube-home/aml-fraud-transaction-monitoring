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
    [Description("The components that produced the impact band of a case.")]
    public class CaseImpactComponentsDto
    {
        [Description("Typology severity from the Case Workflow, 1 to 5.")]
        public int TypologySeverity { get; set; }

        [Description("Money band from the case value, 1 to 5; null when the workflow has no money measure or the " +
                     "case carries no value.")]
        public int? MoneyBand { get; set; }

        [Description("The money involved, when known.")]
        public double? Value { get; set; }

        [Description("Uplift for exposure, 0 or 1.")]
        public int ExposureUplift { get; set; }

        [Description("Uplift for breadth, 0 or 1.")]
        public int BreadthUplift { get; set; }

        [Description("The minimum impact a floor rule imposes.")]
        public int Floor { get; set; }
    }
}

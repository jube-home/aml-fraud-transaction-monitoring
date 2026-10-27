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

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public class AdvancedAnalyticsVersionComparisonDto
    {
        [Description("The first version, with its headline figures and warning codes.")]
        public AskJoobyAnalyticsModelVersionDto VersionA { get; set; } = null!;

        [Description("The second version, with its headline figures and warning codes.")]
        public AskJoobyAnalyticsModelVersionDto VersionB { get; set; } = null!;

        [Description("True when both were trained with the same seed. A difference in metrics is only fair " +
                     "evidence when the rows they were tested on were the same.")]
        public bool SameSeed { get; set; }

        [Description("True when the two split sizes are identical.")]
        public bool SameSplitSizes { get; set; }

        [Description("True when the two use the same predictors in the same order.")]
        public bool SameVariables { get; set; }

        [Description("Held-out metrics side by side with their difference.")]
        public List<AdvancedAnalyticsMetricComparisonDto> Metrics { get; set; } = [];

        [Description("Warning codes only the first version has.")]
        public string[] WarningsOnlyInA { get; set; } = [];

        [Description("Warning codes only the second version has.")]
        public string[] WarningsOnlyInB { get; set; } = [];
    }
}
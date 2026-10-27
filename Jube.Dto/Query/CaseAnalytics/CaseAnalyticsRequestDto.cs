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

namespace Jube.Dto.Query.CaseAnalytics
{
    [Description("Which Case Workflow to analyse and over what period. Only cases the caller's roles may see are " +
                 "counted.")]
    public class CaseAnalyticsRequestDto
    {
        [Description("Guid of the Case Workflow. Required.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Start of the period (UTC). Defaults to thirty days before the end.")]
        public DateTimeOffset? From { get; set; }

        [Description("End of the period (UTC). Defaults to now.")]
        public DateTimeOffset? To { get; set; }
    }
}

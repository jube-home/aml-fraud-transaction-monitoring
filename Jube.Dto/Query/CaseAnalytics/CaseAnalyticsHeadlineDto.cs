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
    [Description("The figures that matter first about a Case Workflow over a period.")]
    public class CaseAnalyticsHeadlineDto
    {
        [Description("Cases created in the period.")]
        public int Opened { get; set; }

        [Description("Cases closed in the period, whenever they were created.")]
        public int Closed { get; set; }

        [Description("Cases open now.")]
        public int OpenNow { get; set; }

        [Description("Opened minus closed in the period; positive means the backlog grew.")]
        public int NetChange { get; set; }

        [Description("Open cases the risk worker has not assessed yet.")]
        public int UnassessedOpen { get; set; }

        [Description("Open cases in the Live money, Mandatory or Deadline near lanes.")]
        public int UrgentOpen { get; set; }

        [Description("Cases closed in the period with a confirmed or not confirmed outcome.")]
        public int Decided { get; set; }

        [Description("Share of decided cases that were confirmed true positives; null when none were decided.")]
        public double? ConfirmedRate { get; set; }

        [Description("Median minutes from creation to first look, for cases created in the period.")]
        public double? MedianFirstViewMinutes { get; set; }

        [Description("Median minutes from creation to closure, for cases created in the period and closed.")]
        public double? MedianResolutionMinutes { get; set; }

        [Description("Open cases currently past a service level target.")]
        public int SlaBreachedOpen { get; set; }

        [Description("Open cases that have used 80 percent or more of a service level target.")]
        public int SlaAtRiskOpen { get; set; }

        [Description("Share of cases created in the period that were reopened at least once.")]
        public double ReopenRate { get; set; }
    }
}

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
using Jube.CaseRisk.Analytics;
using Jube.Dto.Query.CaseAnalytics;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Query.CaseAnalytics
{
    [Description("Service levels, cycle times, flow, bottlenecks, ageing, rule effectiveness, ranking quality, " +
                 "rhythm, workload and handoffs for a Case Workflow over a period.")]
    public class CaseAnalyticsDto
    {
        [Description("Start of the period (UTC).")]
        public DateTimeOffset From { get; set; }

        [Description("End of the period (UTC).")]
        public DateTimeOffset To { get; set; }

        [Description("When the figures were computed (UTC).")]
        public DateTimeOffset GeneratedAt { get; set; }

        [Description("True when there were more cases than could be analysed and the oldest were left out.")]
        public bool Truncated { get; set; }

        [Description("How many cases were analysed.")]
        public int CasesAnalysed { get; set; }

        [Description("Names of the statuses, by Guid, for the flow and dwell figures.")]
        public Dictionary<Guid, string> StatusNames { get; set; } = new();

        [Description("The figures that matter first.")]
        public CaseAnalyticsHeadlineDto Headline { get; set; } = new();

        [Description("How long cases wait to be looked at, owned and resolved, with distributions.")]
        public CycleTimeReport CycleTimes { get; set; } = null!;

        [Description("Opened, closed and backlog by day, with the cases in each status at the end of the day.")]
        public List<FlowDay> Flow { get; set; } = [];

        [Description("Time spent in each status, the bottleneck first.")]
        public List<StatusDwell> StatusDwell { get; set; } = [];

        [Description("Open cases by age and lane, with the oldest.")]
        public AgingReport Aging { get; set; } = null!;

        [Description("Each activation rule's precision and the analyst effort its false positives cost, worst first.")]
        public List<RuleEffectiveness> Rules { get; set; } = [];

        [Description("How well the Priority ordering and the likelihood predict confirmed cases, against working " +
                     "the oldest first, with a calibration table.")]
        public RankingQualityReport Ranking { get; set; } = null!;

        [Description("What the cases picked at random for a quality review show: how many there are, how many " +
                     "have closed with an outcome, and the share confirmed. Because they were picked without " +
                     "regard to the ranking, their confirmed share is an unbiased estimate for the low-priority " +
                     "cases the ranking would otherwise never put in front of an analyst.")]
        public QaSampleReport QaSample { get; set; } = null!;

        [Description("Service level attainment by target, by week, and the cases to watch.")]
        public SlaReport ServiceLevels { get; set; } = null!;

        [Description("When cases arrive by weekday and hour, how long the first look takes by hour, and when cases " +
                     "close.")]
        public RhythmReport Rhythm { get; set; } = null!;

        [Description("The caller's own throughput beside the team median, shown only for a team of three or more.")]
        public WorkloadReport Workload { get; set; } = null!;

        [Description("The caller's own day: cases closed each of the last 14 days, closed today against the " +
                     "workflow's daily goal, the run of consecutive days the goal was met (or one case closed when " +
                     "the workflow sets no goal), the best day and a typical day. Days are UTC calendar days.")]
        public MyDayReport MyDay { get; set; } = null!;

        [Description("How often cases change hands or come back after closing.")]
        public HandoffReport Handoffs { get; set; } = null!;
    }
}

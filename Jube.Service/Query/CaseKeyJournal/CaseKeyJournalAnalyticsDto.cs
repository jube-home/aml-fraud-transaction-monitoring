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
using Jube.CaseRisk.Journal;

namespace Jube.Service.Query.CaseKeyJournal
{
    [Description("A field the analytics worked with.")]
    public class JournalAnalyticsFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Lens { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool RoleInferred { get; set; }

        [Description("measure, category or counterparty.")]
        public string Kind { get; set; } = "measure";
    }

    [Description("How the case key compares with the other case keys of the workflow (its peers), field by field. " +
                 "Only available once the jobs server has built the peer distributions.")]
    public class JournalPeersDto
    {
        [Description("True when at least one field could be compared with its peers.")]
        public bool Available { get; set; }

        [Description("Why peers are not available: NotComputed when no peer distributions have been built for the " +
                     "workflow, NoMeasures when none of the key's fields has one.")]
        public string? Reason { get; set; }

        [Description("When the newest of the distributions used was built (UTC).")]
        public DateTimeOffset? ComputedDate { get; set; }

        [Description("How many days of transactions the distributions were built from.")]
        public int? WindowDays { get; set; }

        [Description("Name of the Case Workflow XPath that splits the peers into segments, when the workflow has one.")]
        public string? Segment { get; set; }

        [Description("The key's value of the segment field, whose peers it was compared with.")]
        public string? SegmentValue { get; set; }

        [Description("True when there were no peers in the key's segment and all keys of the workflow were used.")]
        public bool SegmentFallback { get; set; }

        [Description("One comparison for each field, the most unusual first.")]
        public IReadOnlyList<PeerComparison> Comparisons { get; set; } = [];
    }

    [Description("What was analysed.")]
    public class JournalAnalyticsMetaDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Guid CaseWorkflowGuid { get; set; }
        public int RowCount { get; set; }
        public int Limit { get; set; }
        public bool Truncated { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        [Description("True when there are too few transactions for the statistics to be relied on.")]
        public bool LowConfidence { get; set; }

        public List<string> Sections { get; set; } = [];
    }

    [Description("Statistics computed by the endpoint over the history of a case key. Only the sections asked " +
                 "for are present.")]
    public class CaseKeyJournalAnalyticsDto
    {
        public JournalAnalyticsMetaDto Meta { get; set; } = new();
        public List<JournalAnalyticsFieldDto> Fields { get; set; } = [];
        public JournalSummary? Summary { get; set; }
        public NotableReport? Notable { get; set; }
        public ProgressionReport? Progression { get; set; }
        public IReadOnlyList<DistributionReport>? Distributions { get; set; }
        public VelocityReport? Velocity { get; set; }
        public GapReport? Gaps { get; set; }
        public NoveltyReport? Novelty { get; set; }
        public IReadOnlyList<CounterpartyProfile>? Counterparties { get; set; }
        public CorrelationReport? Correlation { get; set; }
        public DriftReport? Drift { get; set; }
        public JournalPeersDto? Peers { get; set; }
    }
}

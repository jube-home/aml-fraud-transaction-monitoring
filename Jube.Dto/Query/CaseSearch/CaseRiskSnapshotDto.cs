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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSearch
{
    [Description("The latest computed risk of a case: how likely it is real, how bad it would be, how urgent " +
                 "it is, and the Priority that ordering produces. Always read together.")]
    [MessagePackObject]
    public class CaseRiskSnapshotDto
    {
        [Description("True when the risk worker has assessed the case at least once; otherwise every other field " +
                     "is empty or zero.")]
        [Key(0)]
        public bool Assessed { get; set; }

        [Description("Position of this assessment in the case risk history; higher is newer.")]
        [Key(1)]
        public int Sequence { get; set; }

        [Description("Urgency lane: 1 Live money, 2 Mandatory, 3 Deadline near, 4 Standard. Lanes order cases " +
                     "before Priority does.")]
        [Key(2)]
        public int Lane { get; set; }

        [Description("English name of the lane.")]
        [Key(3)]
        public string? LaneName { get; set; }

        [Description("Plain statement of why the case is in its lane.")]
        [Key(4)]
        public string? LaneReason { get; set; }

        [Description("Probability between 0 and 1 that the case will be confirmed a true positive.")]
        [Key(5)]
        public double Likelihood { get; set; }

        [Description("Likelihood band: 1 Unlikely, 2 Possible, 3 Likely, 4 Very likely.")]
        [Key(6)]
        public int LikelihoodBand { get; set; }

        [Description("English name of the likelihood band.")]
        [Key(7)]
        public string? LikelihoodBandName { get; set; }

        [Description("Impact if the case is real: 1 Minimal, 2 Low, 3 Moderate, 4 High, 5 Severe.")]
        [Key(8)]
        public int Impact { get; set; }

        [Description("English name of the impact band.")]
        [Key(9)]
        public string? ImpactName { get; set; }

        [Description("Priority from 0 to 100: expected harm scaled for sorting within a lane. Never meaningful " +
                     "without the likelihood, impact and lane beside it.")]
        [Key(10)]
        public int Priority { get; set; }

        [Description("Likelihood multiplied by the weight of the impact band.")]
        [Key(11)]
        public double ExpectedHarm { get; set; }

        [Description("How the likelihood was produced: 1 Cold start, 2 Learned.")]
        [Key(12)]
        public int Method { get; set; }

        [Description("How many comparable closed cases stand behind the likelihood; zero means it is a cold-start " +
                     "estimate not yet checked against outcomes.")]
        [Key(13)]
        public int EvidenceCount { get; set; }

        [Description("The next obligation the case must meet, when there is one (UTC).")]
        [Key(14)]
        public DateTimeOffset? NextDeadline { get; set; }

        [Description("True when the next deadline has already passed.")]
        [Key(15)]
        public bool DeadlineBreached { get; set; }

        [Description("Label of the evidence that moved the likelihood most, the top driver shown as Why.")]
        [Key(16)]
        public string? TopDriver { get; set; }

        [Description("Family of the top driver, such as Rule or KeyHistory.")]
        [Key(17)]
        public string? TopDriverFamily { get; set; }

        [Description("When the assessment was computed (UTC).")]
        [Key(18)]
        public DateTimeOffset? AssessedDate { get; set; }
    }
}

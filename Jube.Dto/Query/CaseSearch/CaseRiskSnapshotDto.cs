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
    [Description("The latest computed risk of a case: how likely it is real, how bad it would be, how urgent " +
                 "it is, and the Priority that ordering produces. Always read together.")]
    public class CaseRiskSnapshotDto
    {
        [Description("True when the risk worker has assessed the case at least once; otherwise every other field " +
                     "is empty or zero.")]
        public bool Assessed { get; set; }

        [Description("Position of this assessment in the case risk history; higher is newer.")]
        public int Sequence { get; set; }

        [Description("Urgency lane: 1 Live money, 2 Mandatory, 3 Deadline near, 4 Standard. Lanes order cases " +
                     "before Priority does.")]
        public int Lane { get; set; }

        [Description("English name of the lane.")]
        public string? LaneName { get; set; }

        [Description("Plain statement of why the case is in its lane.")]
        public string? LaneReason { get; set; }

        [Description("Probability between 0 and 1 that the case will be confirmed a true positive.")]
        public double Likelihood { get; set; }

        [Description("Likelihood band: 1 Unlikely, 2 Possible, 3 Likely, 4 Very likely.")]
        public int LikelihoodBand { get; set; }

        [Description("English name of the likelihood band.")]
        public string? LikelihoodBandName { get; set; }

        [Description("Impact if the case is real: 1 Minimal, 2 Low, 3 Moderate, 4 High, 5 Severe.")]
        public int Impact { get; set; }

        [Description("English name of the impact band.")]
        public string? ImpactName { get; set; }

        [Description("Priority from 0 to 100: expected harm scaled for sorting within a lane. Never meaningful " +
                     "without the likelihood, impact and lane beside it.")]
        public int Priority { get; set; }

        [Description("Likelihood multiplied by the weight of the impact band.")]
        public double ExpectedHarm { get; set; }

        [Description("How the likelihood was produced: 1 Cold start, 2 Learned.")]
        public int Method { get; set; }

        [Description("How many comparable closed cases stand behind the likelihood; zero means it is a cold-start " +
                     "estimate not yet checked against outcomes.")]
        public int EvidenceCount { get; set; }

        [Description("The next obligation the case must meet, when there is one (UTC).")]
        public DateTimeOffset? NextDeadline { get; set; }

        [Description("True when the next deadline has already passed.")]
        public bool DeadlineBreached { get; set; }

        [Description("Label of the evidence that moved the likelihood most, the top driver shown as Why.")]
        public string? TopDriver { get; set; }

        [Description("Family of the top driver, such as Rule or KeyHistory.")]
        public string? TopDriverFamily { get; set; }

        [Description("When the assessment was computed (UTC).")]
        public DateTimeOffset? AssessedDate { get; set; }
    }
}

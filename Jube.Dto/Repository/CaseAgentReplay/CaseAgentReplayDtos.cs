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

using System;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseAgentReplay
{
    [Description("A request to replay the triage prompt over closed cases and compare it with what people decided.")]
    [MessagePackObject]
    public class CaseAgentReplayRequestDto
    {
        [Description("Cases closed at or after this instant, in UTC. Required.")]
        [Key(0)]
        public DateTime FromDate { get; set; }

        [Description("Cases closed before this instant, in UTC. Required, after FromDate and within 366 days of it.")]
        [Key(1)]
        public DateTime ToDate { get; set; }

        [Description("The most cases to replay, newest closed first; 1 to 200. Defaults to 50.")]
        [Key(2)]
        public int MaximumCases { get; set; } = 50;

        [Description("Minutes after a case was created at which the facts are cut off; 0 to 10080. Defaults to 60. Nothing later is shown to the model.")]
        [Key(3)]
        public int AsOfMinutes { get; set; } = 60;

        [Description("Only cases of this workflow, when given.")]
        [Key(4)]
        public Guid? CaseWorkflowGuid { get; set; }

        [Description("A prompt to test in place of the built-in replay prompt, at most 4000 characters. Replaying the same window with two prompts compares them.")]
        [Key(5)]
        public string? Prompt { get; set; }
    }

    [Description("One replay and, once it has run, how the model's recommendations compare with the outcomes people reached.")]
    [MessagePackObject]
    public class CaseAgentReplayDto
    {
        [Description("The replay's identifier.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("1 Pending, 2 Running, 3 Completed, 4 Failed.")]
        [Key(1)]
        public int StatusId { get; set; }

        [Description("SHA-256 of the prompt used.")]
        [Key(2)]
        public string AgentDefinitionHash { get; set; } = string.Empty;

        [Description("The model id the provider reported.")]
        [Key(3)]
        public string? ModelName { get; set; }

        [Description("Start of the closed-date window.")]
        [Key(4)]
        public DateTime FromDate { get; set; }

        [Description("End of the closed-date window.")]
        [Key(5)]
        public DateTime ToDate { get; set; }

        [Description("Minutes after creation at which facts were cut off.")]
        [Key(6)]
        public int AsOfMinutes { get; set; }

        [Description("How many closed cases were chosen.")]
        [Key(7)]
        public int CasesSelected { get; set; }

        [Description("How many were answered usably.")]
        [Key(8)]
        public int Scored { get; set; }

        [Description("How many answers were unusable.")]
        [Key(9)]
        public int Unusable { get; set; }

        [Description("How many recommendations matched the human outcome.")]
        [Key(10)]
        public int Agreed { get; set; }

        [Description("Missed risk: people confirmed, the model did not.")]
        [Key(11)]
        public int MissedRisk { get; set; }

        [Description("Over-escalation: the model confirmed, people did not.")]
        [Key(12)]
        public int OverEscalation { get; set; }

        [Description("How many the model called inconclusive.")]
        [Key(13)]
        public int Inconclusive { get; set; }

        [Description("Agreed divided by scored; empty when nothing was scored.")]
        [Key(14)]
        public double? AgreementRate { get; set; }

        [Description("A plain reason a failed replay failed.")]
        [Key(15)]
        public string? Error { get; set; }

        [Description("When the replay was requested.")]
        [Key(16)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("The user who requested it.")]
        [Key(17)]
        public string CreatedUser { get; set; } = string.Empty;
    }

    [Description("Two replays over the cases they have in common.")]
    [MessagePackObject]
    public class CaseAgentReplayComparisonDto
    {
        [Description("The first replay.")] [Key(0)] public CaseAgentReplayDto? First { get; set; }

        [Description("The second replay.")] [Key(1)] public CaseAgentReplayDto? Second { get; set; }

        [Description("How many cases both replayed and scored.")]
        [Key(2)]
        public int CommonCases { get; set; }

        [Description("Of those, how many each answered the same.")]
        [Key(3)]
        public int SameRecommendation { get; set; }

        [Description("Of the common cases, how many the first matched the human outcome on.")]
        [Key(4)]
        public int FirstAgreed { get; set; }

        [Description("Of the common cases, how many the second matched the human outcome on.")]
        [Key(5)]
        public int SecondAgreed { get; set; }
    }
}

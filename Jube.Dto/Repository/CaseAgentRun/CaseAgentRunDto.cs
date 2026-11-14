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

namespace Jube.Dto.Repository.CaseAgentRun
{
    [Description("A recommendation the case triage agent made about an open case. It only recommends; nothing here closes a case.")]
    [MessagePackObject]
    public class CaseAgentRunDto
    {
        [Description("The recommendation's identifier, used to accept or override it.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The outcome recommended: 1 Confirmed, 2 Not confirmed, 0 inconclusive.")]
        [Key(1)]
        public int RecommendedOutcomeId { get; set; }

        [Description("The agent's reasoning in plain prose.")]
        [Key(2)]
        public string Rationale { get; set; } = string.Empty;

        [Description("Short citations of the facts the agent relied on.")]
        [Key(3)]
        public List<string> Evidence { get; set; } = [];

        [Description("Where the recommendation stands: 1 Pending, 2 Accepted, 3 Overridden, 4 Superseded.")]
        [Key(4)]
        public int StateId { get; set; }

        [Description("The analyst's reason for overriding, when overridden.")]
        [Key(5)]
        public string? OverrideReason { get; set; }

        [Description("The analyst who decided, when decided.")]
        [Key(6)]
        public string? DecidedUser { get; set; }

        [Description("When it was decided, when decided.")]
        [Key(7)]
        public DateTimeOffset? DecidedDate { get; set; }

        [Description("When the recommendation was made.")]
        [Key(8)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("The model id reported for the run.")]
        [Key(9)]
        public string? ModelName { get; set; }
    }
}

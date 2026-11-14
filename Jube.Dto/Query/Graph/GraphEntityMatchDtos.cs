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

namespace Jube.Dto.Query.Graph
{
    [Description("A candidate that two graph nodes are the same real-world entity, with its evidence and decision.")]
    [MessagePackObject]
    public class GraphEntityMatchDto
    {
        [Description("The candidate's identifier.")] [Key(0)] public long Id { get; set; }

        [Description("The lower node id.")] [Key(1)] public long NodeIdA { get; set; }

        [Description("The key name of the first node, for example Account.")]
        [Key(2)]
        public string KeyA { get; set; } = string.Empty;

        [Description("The value of the first node.")] [Key(3)] public string ValueA { get; set; } = string.Empty;

        [Description("The higher node id.")] [Key(4)] public long NodeIdB { get; set; }

        [Description("The key name of the second node.")]
        [Key(5)]
        public string KeyB { get; set; } = string.Empty;

        [Description("The value of the second node.")] [Key(6)] public string ValueB { get; set; } = string.Empty;

        [Description("Where the candidate came from: 1 shared hard identifier, 2 fuzzy comparison.")]
        [Key(7)]
        public int SourceId { get; set; }

        [Description("Estimated probability they are the same entity; evidence, not a decision.")]
        [Key(8)]
        public double Posterior { get; set; }

        [Description("JSON evidence: what agreed and how much it weighed.")]
        [Key(9)]
        public string? Evidence { get; set; }

        [Description("1 Pending, 2 Confirmed, 3 Rejected.")] [Key(10)] public int DecisionId { get; set; }

        [Description("Who decided.")] [Key(11)] public string? DecidedUser { get; set; }

        [Description("When it was decided.")] [Key(12)] public DateTimeOffset? DecidedDate { get; set; }

        [Description("The reason given with the decision.")] [Key(13)] public string? DecisionNote { get; set; }

        [Description("For a confirmed match, the resolved entity both nodes belong to.")]
        [Key(14)]
        public long? ResolvedEntityId { get; set; }

        [Description("When the candidate was found.")] [Key(15)] public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("A decision on a candidate: confirm it, reject it, or undo an earlier decision back to pending.")]
    [MessagePackObject]
    public class GraphEntityMatchDecideDto
    {
        [Description("The candidate's identifier. Required.")] [Key(0)] public long Id { get; set; }

        [Description("The decision: 2 Confirmed, 3 Rejected, 1 Pending (undo). Required.")]
        [Key(1)]
        public int DecisionId { get; set; }

        [Description("Optional reason, at most 500 characters.")] [Key(2)] public string? Note { get; set; }
    }
}

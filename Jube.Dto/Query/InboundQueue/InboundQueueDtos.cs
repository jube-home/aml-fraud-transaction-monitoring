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

namespace Jube.Dto.Query.InboundQueue
{
    [Description("How many transactions are waiting in the inbound queue, being processed, and dead.")]
    [MessagePackObject]
    public class InboundQueueDepthDto
    {
        [Description("Rows waiting to be claimed (including those delayed for a retry).")]
        [Key(0)]
        public int Pending { get; set; }

        [Description("Rows claimed by an engine node right now.")]
        [Key(1)]
        public int Processing { get; set; }

        [Description("Rows that gave up after the maximum attempts and wait for an operator.")]
        [Key(2)]
        public int Dead { get; set; }
    }

    [Description("A dead inbound queue row. The payload is never returned.")]
    [MessagePackObject]
    public class InboundQueueDeadDto
    {
        [Description("The row's identifier.")] [Key(0)] public long Id { get; set; }

        [Description("The model the transaction was for.")]
        [Key(1)]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("How many times it was tried.")] [Key(2)] public int Attempts { get; set; }

        [Description("A short plain reason it died.")] [Key(3)] public string? LastError { get; set; }

        [Description("The user whose key enqueued it.")] [Key(4)] public string EnqueuedBy { get; set; } = string.Empty;

        [Description("When it was enqueued.")] [Key(5)] public DateTimeOffset CreatedDate { get; set; }
    }
}

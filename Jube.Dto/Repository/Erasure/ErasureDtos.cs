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

namespace Jube.Dto.Repository.Erasure
{
    [Description("A request to erase one subject, with its state and, once run, what was removed.")]
    [MessagePackObject]
    public class ErasureRequestDto
    {
        [Description("The request's identifier.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The archive key name the subject is identified by.")]
        [Key(1)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value of the subject.")]
        [Key(2)]
        public string KeyValue { get; set; } = string.Empty;

        [Description("Why the erasure was requested.")]
        [Key(3)]
        public string Reason { get; set; } = string.Empty;

        [Description("1 Pending approval, 2 Approved, 3 Executed, 4 Rejected, 5 Retracted, 6 Refused.")]
        [Key(4)]
        public int StateId { get; set; }

        [Description("When refused: 1 Legal hold, 2 Open case, 3 Inside the retention minimum, 4 Nothing found.")]
        [Key(5)]
        public int? RefusalReasonId { get; set; }

        [Description("Archived transactions that matched the subject.")]
        [Key(6)]
        public int? ArchiveRowsFound { get; set; }

        [Description("Archived transactions removed.")]
        [Key(7)]
        public int? ArchiveRowsRemoved { get; set; }

        [Description("Graph nodes removed.")]
        [Key(8)]
        public int? GraphNodesRemoved { get; set; }

        [Description("Graph edges removed.")]
        [Key(9)]
        public int? GraphEdgesRemoved { get; set; }

        [Description("Closed cases whose payload copy was cleared.")]
        [Key(10)]
        public int? CasePayloadsNulled { get; set; }

        [Description("Who asked.")]
        [Key(11)]
        public string RequestedUser { get; set; } = string.Empty;

        [Description("UTC. When it was asked.")]
        [Key(12)]
        public DateTime RequestedDate { get; set; }

        [Description("Who decided it.")]
        [Key(13)]
        public string? DecidedUser { get; set; }

        [Description("UTC. When it was decided.")]
        [Key(14)]
        public DateTime? DecidedDate { get; set; }

        [Description("The reason given with the decision.")]
        [Key(15)]
        public string? DecisionNote { get; set; }

        [Description("UTC. When the engine executed or refused it.")]
        [Key(16)]
        public DateTime? ExecutedDate { get; set; }
    }

    [Description("Identifies a subject and why it is to be erased.")]
    [MessagePackObject]
    public class ErasureRequestCreateDto
    {
        [Description("The archive key name the subject is identified by.")]
        [Key(0)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value of the subject.")]
        [Key(1)]
        public string KeyValue { get; set; } = string.Empty;

        [Description("Why the erasure is requested, for example the data subject request reference.")]
        [Key(2)]
        public string Reason { get; set; } = string.Empty;
    }

    [Description("A decision on an erasure request.")]
    [MessagePackObject]
    public class ErasureDecisionDto
    {
        [Description("The request decided.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The reason; required to reject.")]
        [Key(1)]
        public string? Note { get; set; }
    }

    [Description("A subject, named by archive key.")]
    [MessagePackObject]
    public class ErasureSubjectDto
    {
        [Description("The archive key name.")]
        [Key(0)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value.")]
        [Key(1)]
        public string KeyValue { get; set; } = string.Empty;
    }

    [Description("What an erasure of the subject would meet, without doing anything.")]
    [MessagePackObject]
    public class ErasurePreviewDto
    {
        [Description("Archived transactions that match the subject.")]
        [Key(0)]
        public int ArchiveRows { get; set; }

        [Description("When it would be refused: 1 Legal hold, 2 Open case, 3 Inside the retention minimum, 4 Nothing found. Empty when it could proceed.")]
        [Key(1)]
        public int? RefusalReasonId { get; set; }
    }

    [Description("One archived transaction in a subject access export.")]
    [MessagePackObject]
    public class SubjectExportTransactionDto
    {
        [Description("The transaction's entry guid.")]
        [Key(0)]
        public Guid EntryGuid { get; set; }

        [Description("The model that processed it.")]
        [Key(1)]
        public int ModelId { get; set; }

        [Description("UTC. The reference date of the transaction.")]
        [Key(2)]
        public DateTime? ReferenceDate { get; set; }

        [Description("UTC. When it was archived.")]
        [Key(3)]
        public DateTime? CreatedDate { get; set; }

        [Description("The archived record as JSON text: the payload and everything derived from it.")]
        [Key(4)]
        public string Json { get; set; } = string.Empty;
    }

    [Description("Everything archived about one subject. Case and report data is deliberately not included.")]
    [MessagePackObject]
    public class SubjectExportDto
    {
        [Description("The archive key name.")]
        [Key(0)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value.")]
        [Key(1)]
        public string KeyValue { get; set; } = string.Empty;

        [Description("UTC. When the export was made.")]
        [Key(2)]
        public DateTime GeneratedDate { get; set; }

        [Description("Who made it.")]
        [Key(3)]
        public string GeneratedUser { get; set; } = string.Empty;

        [Description("True when there were more transactions than the limit and the rest are not included.")]
        [Key(4)]
        public bool Truncated { get; set; }

        [Description("The archived transactions, oldest first.")]
        [Key(5)]
        public List<SubjectExportTransactionDto> Transactions { get; set; } = [];
    }
}

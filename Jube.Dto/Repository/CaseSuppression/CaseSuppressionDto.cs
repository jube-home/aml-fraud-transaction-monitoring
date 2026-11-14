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
using System;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseSuppression
{
    [MessagePackObject]
    public class CaseSuppressionDto
    {
        [Description("Server-assigned identifier of the request. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Identifier of the case the request was made from.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The case key of the case. Server-assigned and read-only.")]
        [Key(2)]
        public string? CaseKey { get; set; }

        [Description("The case key value of the case. Server-assigned and read-only.")]
        [Key(3)]
        public string? CaseKeyValue { get; set; }

        [Description("The override key: the name of the request XPath, flagged Enable Override, that the suppression matches on.")]
        [Key(4)]
        public string OverrideKey { get; set; } = string.Empty;

        [Description("The value of the override key. Server-resolved from the case journal.")]
        [Key(5)]
        public string OverrideKeyValue { get; set; } = string.Empty;

        [Description("How the key was chosen as an integer: 1 CaseKey, 2 Transaction.")]
        [Key(6)]
        public int PathId { get; set; }

        [Description("Why the suppression is requested.")]
        [Key(7)]
        public string? Reason { get; set; }

        [Description("When the override created on approval is removed by the engine (UTC).")]
        [Key(8)]
        public DateTimeOffset ExpiryDate { get; set; }

        [Description("Where the request is as an integer: 1 Pending, 2 Approved, 3 Rejected, 4 Retracted. Read-only.")]
        [Key(9)]
        public int StateId { get; set; }

        [Description("What the request is now as an integer: 1 Pending, 2 Approved, 3 Rejected, 4 Retracted, 5 Expired, 6 Removed. Expired and Removed are resolved against the override rows. Read-only.")]
        [Key(10)]
        public int StandingId { get; set; }

        [Description("Who raised it as an integer: 1 User, 2 AskJooby. Server-assigned and read-only.")]
        [Key(11)]
        public int SourceId { get; set; }

        [Description("Increases each time the maker resubmits. Read-only.")]
        [Key(12)]
        public int Version { get; set; }

        [Description("How many approvals the setting ApprovalsRequired asks for. Read-only.")]
        [Key(13)]
        public int ApprovalsRequired { get; set; }

        [Description("How many approvals the current version has. Read-only.")]
        [Key(14)]
        public int ApprovalsRecorded { get; set; }

        [Description("True when the caller can approve or reject this request now: holds the approval permission, is not the maker, has not already approved this version, and the request is pending. Read-only.")]
        [Key(15)]
        public bool CanDecide { get; set; }

        [Description("True when the caller is the maker and can retract this pending request. Read-only.")]
        [Key(16)]
        public bool CanRetract { get; set; }

        [Description("True when the caller is the maker and can resubmit this rejected or retracted request. Read-only.")]
        [Key(17)]
        public bool CanResubmit { get; set; }

        [Description("The counts recorded when the request was made. Read-only.")]
        [Key(18)]
        public CaseSuppressionCountsDto Then { get; set; } = new();

        [Description("The counts now. Only filled when a single request is read. Read-only.")]
        [Key(19)]
        public CaseSuppressionCountsDto? Now { get; set; }

        [Description("What the request asks to suppress or force.")]
        [Key(20)]
        public List<CaseSuppressionTargetDto> Targets { get; set; } = [];

        [Description("The transactions ticked as evidence.")]
        [Key(21)]
        public List<CaseSuppressionTransactionDto> Transactions { get; set; } = [];

        [Description("Every decision recorded on the request.")]
        [Key(22)]
        public List<CaseSuppressionApprovalDto> Approvals { get; set; } = [];

        [Description("The maker. For AskJooby, the user it acted for. Read-only.")]
        [Key(23)]
        public string? CreatedUser { get; set; }

        [Description("When it was requested (UTC). Read-only.")]
        [Key(24)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The user whose decision last moved the request. Read-only.")]
        [Key(25)]
        public string? DecidedUser { get; set; }

        [Description("When that decision was made (UTC). Read-only.")]
        [Key(26)]
        public DateTimeOffset? DecidedDate { get; set; }
    }
}

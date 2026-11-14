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

using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseSuppression;

namespace Jube.Service.Repository.CaseSuppression
{
    public static class CaseSuppressionMapper
    {
        private static DateTimeOffset? Utc(DateTime? value)
        {
            return value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;
        }

        public static CaseSuppressionCountsDto ToCounts(Data.Poco.CaseSuppression poco)
        {
            return new CaseSuppressionCountsDto
            {
                CountInCase = poco.CountInCase,
                FirstSeenDate = Utc(poco.FirstSeenDate),
                LastSeenDate = Utc(poco.LastSeenDate),
                WiderCountStateId = poco.WiderCountStateId,
                WiderTransactions = poco.WiderTransactions,
                WiderKeys = poco.WiderKeys,
                WiderCapped = poco.WiderCapped == 1
            };
        }

        public static CaseSuppressionTargetDto ToDto(CaseSuppressionTarget poco, CaseSuppressionStanding standing)
        {
            return new CaseSuppressionTargetDto
            {
                Id = poco.Id,
                EntityAnalysisModelGuid = poco.EntityAnalysisModelGuid,
                EntityAnalysisModelName = poco.EntityAnalysisModelName,
                RuleName = poco.RuleName,
                OverrideKindId = poco.OverrideKindId,
                AppliedOverrideId = poco.AppliedOverrideId,
                AppliedDate = Utc(poco.AppliedDate),
                StandingId = (int)standing
            };
        }

        public static CaseSuppressionTransactionDto ToDto(CaseSuppressionTransaction poco)
        {
            return new CaseSuppressionTransactionDto
            {
                Id = poco.Id,
                ArchiveId = poco.ArchiveId,
                ReferenceDate = Utc(poco.ReferenceDate),
                LaneKey = poco.LaneKey
            };
        }

        public static CaseSuppressionApprovalDto ToDto(CaseSuppressionApproval poco)
        {
            return new CaseSuppressionApprovalDto
            {
                Id = poco.Id,
                SuppressionVersion = poco.SuppressionVersion,
                DecisionId = poco.DecisionId,
                Note = poco.Note,
                CreatedUser = poco.CreatedUser,
                CreatedDate = Utc(poco.CreatedDate)
            };
        }

        public static CaseSuppressionDto ToDto(Data.Poco.CaseSuppression poco, CaseSuppressionStanding standing,
            IReadOnlyCollection<CaseSuppressionTargetDto> targets,
            IReadOnlyCollection<CaseSuppressionTransactionDto> transactions,
            IReadOnlyCollection<CaseSuppressionApprovalDto> approvals, int approvalsRequired, int approvalsRecorded,
            bool canDecide, bool canRetract, bool canResubmit)
        {
            return new CaseSuppressionDto
            {
                Id = poco.Id,
                CaseId = poco.CaseId,
                CaseKey = poco.CaseKey,
                CaseKeyValue = poco.CaseKeyValue,
                OverrideKey = poco.OverrideKey,
                OverrideKeyValue = poco.OverrideKeyValue,
                PathId = poco.PathId,
                Reason = poco.Reason,
                ExpiryDate = new DateTimeOffset(DateTime.SpecifyKind(poco.ExpiryDate, DateTimeKind.Utc)),
                StateId = poco.StateId,
                StandingId = (int)standing,
                SourceId = poco.SourceId,
                Version = poco.Version,
                ApprovalsRequired = approvalsRequired,
                ApprovalsRecorded = approvalsRecorded,
                CanDecide = canDecide,
                CanRetract = canRetract,
                CanResubmit = canResubmit,
                Then = ToCounts(poco),
                Targets = [.. targets],
                Transactions = [.. transactions],
                Approvals = [.. approvals],
                CreatedUser = poco.CreatedUser,
                CreatedDate = Utc(poco.CreatedDate),
                DecidedUser = poco.DecidedUser,
                DecidedDate = Utc(poco.DecidedDate)
            };
        }
    }
}

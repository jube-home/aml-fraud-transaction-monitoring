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
using Jube.Data.Query.GetCaseSuppressionOverrideStateQuery;
using Jube.Dto.Options;

namespace Jube.Service.Repository.CaseSuppression
{
    public static class CaseSuppressionStandingResolver
    {
        public static CaseSuppressionStanding ResolveTarget(CaseSuppressionState state, DateTime requestExpiry,
            CaseSuppressionTarget target, IReadOnlyCollection<GetCaseSuppressionOverrideStateQueryDto> rows,
            DateTime now)
        {
            if (state != CaseSuppressionState.Approved)
            {
                return (CaseSuppressionStanding)(int)state;
            }

            if (target.AppliedOverrideId is null)
            {
                return CaseSuppressionStanding.Approved;
            }

            var isRule = !string.IsNullOrEmpty(target.RuleName);
            var row = rows.FirstOrDefault(r => r.Id == target.AppliedOverrideId && r.IsRule == isRule);

            if (row is null)
            {
                return CaseSuppressionStanding.Removed;
            }

            if (row.DeleteExpiryDate.HasValue && row.DeleteExpiryDate.Value <= now)
            {
                return CaseSuppressionStanding.Expired;
            }

            if (row.Deleted)
            {
                return requestExpiry <= now ? CaseSuppressionStanding.Expired : CaseSuppressionStanding.Removed;
            }

            return CaseSuppressionStanding.Approved;
        }

        public static CaseSuppressionStanding Resolve(CaseSuppressionState state,
            IReadOnlyCollection<CaseSuppressionStanding> targets)
        {
            if (state != CaseSuppressionState.Approved)
            {
                return (CaseSuppressionStanding)(int)state;
            }

            if (targets.Any(t => t == CaseSuppressionStanding.Removed))
            {
                return CaseSuppressionStanding.Removed;
            }

            if (targets.Count > 0 && targets.All(t => t == CaseSuppressionStanding.Expired))
            {
                return CaseSuppressionStanding.Expired;
            }

            return CaseSuppressionStanding.Approved;
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.Options;
using LinqToDB;

namespace Jube.Data.Query.MarkCaseRiskRecheckQuery
{
    public class MarkCaseRiskRecheckQuery(DbContext dbContext)
    {
        public Task<int> ExecuteAsync(IEnumerable<int> caseIds, CaseRiskTrigger trigger,
            CancellationToken token = default)
        {
            var distinct = caseIds.Where(w => w > 0).Distinct().ToList();

            if (distinct.Count == 0)
            {
                return Task.FromResult(0);
            }

            var triggerId = (byte?)trigger;

            return dbContext.Case.Where(w => distinct.Contains(w.Id) && (w.ClosedStatusId == 0 || w.ClosedStatusId == 1))
                .Set(s => s.RiskCheckedDate, (DateTime?)null)
                .Set(s => s.RiskRecheckTriggerId, triggerId)
                .UpdateAsync(token);
        }

        public Task<int> ForOtherOpenCasesOnKeyAsync(Guid caseWorkflowGuid, string caseKey, string caseKeyValue,
            int exceptCaseId, CaseRiskTrigger trigger, CancellationToken token = default)
        {
            var triggerId = (byte?)trigger;

            return dbContext.Case.Where(w => w.CaseWorkflowGuid == caseWorkflowGuid && w.CaseKey == caseKey
                                                                                   && w.CaseKeyValue == caseKeyValue
                                                                                   && w.Id != exceptCaseId
                                                                                   && (w.ClosedStatusId == 0 || w.ClosedStatusId == 1))
                .Set(s => s.RiskCheckedDate, (DateTime?)null)
                .Set(s => s.RiskRecheckTriggerId, triggerId)
                .UpdateAsync(token);
        }

        public Task<int> ForGraphNodesAsync(int tenantRegistryId, IEnumerable<long> nodeIds, CaseRiskTrigger trigger,
            CancellationToken token = default)
        {
            var distinct = nodeIds.Distinct().ToList();

            if (distinct.Count == 0)
            {
                return Task.FromResult(0);
            }

            var triggerId = (byte?)trigger;

            var cases = from c in dbContext.Case
                join n in dbContext.GraphNode on new { Key = c.CaseKey, Value = c.CaseKeyValue }
                    equals new { n.Key, n.Value }
                where n.TenantRegistryId == tenantRegistryId && distinct.Contains(n.Id)
                                                             && (c.ClosedStatusId == 0 || c.ClosedStatusId == 1)
                select c.Id;

            return dbContext.Case.Where(w => cases.Contains(w.Id))
                .Set(s => s.RiskCheckedDate, (DateTime?)null)
                .Set(s => s.RiskRecheckTriggerId, triggerId)
                .UpdateAsync(token);
        }
    }
}

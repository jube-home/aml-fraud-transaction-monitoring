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

namespace Jube.Data.Query.GetCaseEventByCaseKeyValueQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseEventByCaseKeyValueQuery(DbContext dbContext, string userName)
    {
        public async Task<IEnumerable<GetCaseEventByCaseKeyValueQueryDto>> ExecuteAsync(string key, string value,
            CancellationToken token = default)
        {
            var query = from c in dbContext.Case
                from e in dbContext.CaseEvent.InnerJoin(w => w.CaseId == c.Id && w.CaseKey == key && w.CaseKeyValue == value)
                from i in dbContext.CaseWorkflow.InnerJoin(w =>
                    w.Guid == c.CaseWorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                from m in dbContext.EntityAnalysisModel.InnerJoin(w =>
                    w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                from t in dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
                from s in dbContext.CaseWorkflowStatus.InnerJoin(w =>
                    w.Guid == c.CaseWorkflowStatusGuid && (w.Deleted == 0 || w.Deleted == null))
                where c.CaseKey == key
                      && c.CaseKeyValue == value
                      && dbContext.UserInTenant
                          .Any(u => u.TenantRegistryId == t.Id && u.User == userName)
                      && dbContext.CaseWorkflowRole
                          .Where(r => r.CaseWorkflowGuid == i.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                      && dbContext.CaseWorkflowStatusRole
                          .Where(r => r.CaseWorkflowStatusGuid == s.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                orderby e.Id descending
                select e;

            var caseEvents = await query.ToListAsync(token);

            var checklistEventIds = caseEvents
                .Where(e => e.CaseEventTypeId is >= 21 and <= 28 && long.TryParse(e.After, out _))
                .Select(e => long.Parse(e.After))
                .Distinct()
                .ToList();

            var checklistDetails = checklistEventIds.Count == 0
                ? new Dictionary<string, string>()
                : (await dbContext.CaseChecklistEvent
                    .Where(e => checklistEventIds.Contains(e.Id) && e.Detail != null)
                    .Select(e => new { e.Id, e.Detail })
                    .ToListAsync(token))
                .ToDictionary(e => e.Id.ToString(), e => e.Detail);

            var getCaseEventByCaseKeyValueQueryDtos = new List<GetCaseEventByCaseKeyValueQueryDto>();
            foreach (var caseEvent in caseEvents)
            {
                var getCaseEventByCaseKeyValueQueryDto = new GetCaseEventByCaseKeyValueQueryDto
                {
                    Id = caseEvent.Id,
                    CaseId = caseEvent.CaseId.GetValueOrDefault(),
                    CreatedDate =
                        new DateTimeOffset(
                            DateTime.SpecifyKind(caseEvent.CreatedDate.GetValueOrDefault(), DateTimeKind.Utc),
                            TimeSpan.Zero),
                    CaseEventTypeId = caseEvent.CaseEventTypeId.GetValueOrDefault(),
                    CreatedUser = caseEvent.CreatedUser,
                    Before = caseEvent.Before,
                    After = caseEvent.CaseEventTypeId is >= 21 and <= 28 && caseEvent.After != null
                            && checklistDetails.TryGetValue(caseEvent.After, out var detail)
                        ? detail
                        : caseEvent.CaseEventTypeId is >= 21 and <= 28
                            ? null
                            : caseEvent.After,
                    CaseEventType = caseEvent.CaseEventTypeId switch
                    {
                        1 => "Automatic Unlock",
                        2 => "Skim Case",
                        3 => "Automatic Lock",
                        4 => "Full Case Fetch",
                        5 => "Closed",
                        6 => "Manual Lock",
                        7 => "Diary",
                        8 => "Workflow Change",
                        9 => "Status Change",
                        10 => "Diary Date Change",
                        11 => "Rating Change",
                        12 => "Suspend Closed",
                        13 => "Suspend Open",
                        14 => "Allocate Lock",
                        15 => "Process Expired Case",
                        16 => "Peek",
                        17 => "Automatic Escalation",
                        18 => "Lock Broken",
                        19 => "Diary Cleared",
                        20 => "QA Sampled",
                        21 => "Checklist Item Shown",
                        22 => "Checklist Item Hidden",
                        23 => "Checklist Item Checked",
                        24 => "Checklist Item Unchecked",
                        25 => "Checklist Gate Blocked",
                        26 => "Checklist Gate Overridden",
                        27 => "Checklist Gate Bypassed",
                        28 => "Checklist Item SLA Breached",
                        29 => "Triage Recommended",
                        30 => "Triage Accepted",
                        31 => "Triage Overridden",
                        32 => "Legal Hold Placed",
                        33 => "Legal Hold Lifted",
                        34 => "Analyst Assessment Recorded",
                        35 => "Analyst Assessment Withdrawn",
                        36 => "Case Linked",
                        37 => "Case Unlinked",
                        _ => "Unknown"
                    }
                };

                getCaseEventByCaseKeyValueQueryDtos.Add(getCaseEventByCaseKeyValueQueryDto);
            }

            return getCaseEventByCaseKeyValueQueryDtos;
        }
    }
}
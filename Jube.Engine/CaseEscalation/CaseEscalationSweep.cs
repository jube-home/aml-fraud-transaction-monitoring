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

namespace Jube.Engine.CaseEscalation
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Dto.Options;
    using LinqToDB;
    using log4net;

    public sealed record CaseEscalationSweepResult(int Rules, int Skipped, int Examined, int Moved, int Failed);

    public static class CaseEscalationSweep
    {
        public static async Task<CaseEscalationSweepResult> RunAsync(DbContext dbContext, DateTime now, int batchSize,
            ILog log, Func<Case, CaseWorkflowStatus, Task> afterMove = null,
            CancellationToken token = default)
        {
            var sources = await dbContext.CaseWorkflowStatus
                .Where(s => s.Active == 1
                            && (s.Deleted == 0 || s.Deleted == null)
                            && s.EscalationTriggerId != null
                            && s.EscalationTargetCaseWorkflowStatusGuid != null
                            && (s.CaseWorkflow.Deleted == 0 || s.CaseWorkflow.Deleted == null)
                            && (s.CaseWorkflow.EntityAnalysisModel.Deleted == 0
                                || s.CaseWorkflow.EntityAnalysisModel.Deleted == null))
                .ToListAsync(token);

            var rules = 0;
            var skipped = 0;
            var examined = 0;
            var moved = 0;
            var failed = 0;

            foreach (var source in sources)
            {
                token.ThrowIfCancellationRequested();

                var target = await dbContext.CaseWorkflowStatus.FirstOrDefaultAsync(s =>
                    s.Guid == source.EscalationTargetCaseWorkflowStatusGuid
                    && s.CaseWorkflowId == source.CaseWorkflowId
                    && s.Active == 1
                    && (s.Deleted == 0 || s.Deleted == null), token);

                if (target == null || !(target.Priority < source.Priority))
                {
                    skipped++;
                    log.Warn($"Case Escalation: status {source.Name} (id {source.Id}) has an escalation target that " +
                             "is missing, inactive, in another workflow or not a stronger priority; the rule is skipped.");

                    continue;
                }

                rules++;

                var workflow = await dbContext.CaseWorkflow.FirstAsync(w => w.Id == source.CaseWorkflowId, token);
                var candidates = await Matching(dbContext, source, workflow, now)
                    .OrderBy(c => c.CreatedDate).Take(batchSize).ToListAsync(token);

                foreach (var candidate in candidates)
                {
                    token.ThrowIfCancellationRequested();
                    examined++;

                    try
                    {
                        var move = new CaseMove(candidate.Id, candidate.CaseWorkflowGuid, candidate.CaseKey,
                            candidate.CaseKeyValue, source.Guid, source.Name, target.Guid, target.Name,
                            source.EscalationReleaseLock == 1);

                        if (!await CaseStatusMover.MoveAsync(dbContext, move, now, token))
                        {
                            continue;
                        }

                        moved++;

                        if (afterMove != null)
                        {
                            await afterMove(candidate, target);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failed++;
                        log.Error($"Case Escalation: could not escalate case id {candidate.Id}: {ex}");
                    }
                }
            }

            return new CaseEscalationSweepResult(rules, skipped, examined, moved, failed);
        }

        private static IQueryable<Case> Matching(DbContext dbContext, CaseWorkflowStatus source,
            CaseWorkflow workflow, DateTime now)
        {
            var open = dbContext.Case.Where(c => c.CaseWorkflowStatusGuid == source.Guid && c.ClosedStatusId == 0);
            var threshold = source.EscalationThreshold.GetValueOrDefault();

            switch ((CaseEscalationTrigger)source.EscalationTriggerId.GetValueOrDefault())
            {
                case CaseEscalationTrigger.TimeInStatus:
                    var enteredBefore = now.AddMinutes(-threshold);
                    var statusGuid = source.Guid;

                    return open.Where(c => dbContext.CaseStatusTransition.Any(t => t.CaseId == c.Id
                        && t.ExitedDate == null && t.CaseWorkflowStatusGuid == statusGuid
                        && t.EnteredDate <= enteredBefore));

                case CaseEscalationTrigger.ResolutionTargetBreached:
                    if (!workflow.ResolutionTargetMinutes.HasValue)
                    {
                        return open.Where(c => false);
                    }

                    var createdBefore = now.AddMinutes(-workflow.ResolutionTargetMinutes.Value);

                    return open.Where(c => c.CreatedDate <= createdBefore);

                case CaseEscalationTrigger.LaneAtLeastUrgent:
                    var lane = (byte)Math.Clamp(threshold, 1, 3);

                    return open.Where(c => dbContext.CaseRiskAssessment.Any(a => a.CaseId == c.Id
                        && a.Sequence == dbContext.CaseRiskAssessment.Where(x => x.CaseId == c.Id)
                            .Max(x => x.Sequence)
                        && a.LaneId <= lane));

                case CaseEscalationTrigger.PriorityAtLeast:
                    return open.Where(c => dbContext.CaseRiskAssessment.Any(a => a.CaseId == c.Id
                        && a.Sequence == dbContext.CaseRiskAssessment.Where(x => x.CaseId == c.Id)
                            .Max(x => x.Sequence)
                        && a.Priority >= threshold));

                case CaseEscalationTrigger.DeadlineOverdue:
                    return open.Where(c => dbContext.CaseRiskAssessment.Any(a => a.CaseId == c.Id
                        && a.Sequence == dbContext.CaseRiskAssessment.Where(x => x.CaseId == c.Id)
                            .Max(x => x.Sequence)
                        && a.DeadlineBreached == 1));

                default:
                    return open.Where(c => false);
            }
        }
    }
}

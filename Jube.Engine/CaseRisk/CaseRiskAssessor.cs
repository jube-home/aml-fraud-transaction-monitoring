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

namespace Jube.Engine.CaseRisk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Query.GetCaseKeyOutcomeHistoryQuery;
    using Data.Query.GetCaseRiskCandidatesQuery;
    using Data.Query.GetCaseWorkflowOutcomeCountsQuery;
    using Data.Repository;
    using Dto.Options;
    using Jube.CaseRisk;
    using LinqToDB;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public sealed class CaseRiskAssessor(DbContext dbContext, CaseRiskSettings defaults)
    {
        private static readonly JsonSerializerSettings jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly CaseRiskAssessmentRepository assessments = new(dbContext);
        private readonly Dictionary<Guid, GetCaseWorkflowOutcomeCountsQueryDto> outcomes = new();
        private readonly Dictionary<Guid, string> ruleNames = new();
        private readonly Dictionary<Guid, CaseWorkflow> workflows = new();

        public async Task<bool> AssessAsync(GetCaseRiskCandidatesQueryDto candidate, DateTime now,
            CancellationToken token = default)
        {
            var workflow = await GetWorkflowAsync(candidate.CaseWorkflowGuid, token);
            var settings = CaseWorkflowRiskSettings.For(defaults, workflow);
            var workflowOutcomes = await GetOutcomesAsync(candidate.CaseWorkflowGuid, token);

            var evidence = new List<EvidenceItem>();

            if (candidate.EntityAnalysisModelActivationRuleGuid.HasValue)
            {
                var ruleGuid = candidate.EntityAnalysisModelActivationRuleGuid.Value;
                var rule = workflowOutcomes.Rules.FirstOrDefault(r => r.RuleGuid == ruleGuid);

                if (rule is { Closed: > 0 })
                {
                    evidence.Add(new EvidenceItem("Rule", await GetRuleNameAsync(ruleGuid, token),
                        "Closed cases this activation rule created", null, rule.Confirmed, rule.Closed));
                }
            }

            if (!string.IsNullOrEmpty(candidate.CaseKey) && !string.IsNullOrEmpty(candidate.CaseKeyValue))
            {
                var history = await new GetCaseKeyOutcomeHistoryQuery(dbContext).ExecuteAsync(
                    candidate.CaseWorkflowGuid, candidate.CaseKey, candidate.CaseKeyValue, candidate.CaseId, token);

                if (history.Closed > 0)
                {
                    var age = history.NewestClosedDate.HasValue
                        ? Math.Max(0, (now - history.NewestClosedDate.Value).TotalDays)
                        : 0;

                    evidence.Add(new EvidenceItem("KeyHistory",
                        $"{candidate.CaseKey} {candidate.CaseKeyValue}: {history.Closed} earlier closed cases",
                        "Earlier closed cases on the same case key", null, history.Confirmed, history.Closed, age,
                        settings.KeyHistoryHalfLifeDays));
                }
            }

            var value = CaseValueReader.Read(candidate.Json, workflow.ValueField);

            var lane = CaseDiaryLane.Inputs(now, candidate.Diary == 1, candidate.DiaryDate,
                Enum.IsDefined((CaseDiaryType)candidate.DiaryTypeId)
                    ? (CaseDiaryType)candidate.DiaryTypeId
                    : CaseDiaryType.Review);

            var result = CaseRiskCalculator.Assess(new CaseRiskInputs(workflowOutcomes.Confirmed,
                workflowOutcomes.Closed, evidence,
                new ImpactInputs(workflow.TypologySeverityId, value),
                lane), settings);

            await DecideQaSampleAsync(candidate, result, settings, now, token);

            var latest = await assessments.GetLatestAsync(candidate.CaseId, token);
            var written = false;

            if (latest == null || Differs(latest, result))
            {
                await assessments.InsertAsync(ToPoco(candidate, result, TriggerOf(candidate, latest), workflowOutcomes.Closed),
                    token);
                written = true;
            }

            await dbContext.Case.Where(w => w.Id == candidate.CaseId)
                .Set(s => s.RiskCheckedDate, now)
                .Set(s => s.RiskRecheckTriggerId, (byte?)null)
                .UpdateAsync(token);

            return written;
        }

        private async Task DecideQaSampleAsync(GetCaseRiskCandidatesQueryDto candidate, CaseRiskResult result,
            CaseRiskSettings settings, DateTime now, CancellationToken token)
        {
            if (candidate.QaSampleStatusId != 0 || settings.QaSampleRate <= 0
                                                || !QaSampler.Eligible(result.Lane.Lane, result.Priority, settings))
            {
                return;
            }

            var picked = QaSampler.Picked(candidate.CaseWorkflowGuid, candidate.CaseId, settings.QaSampleRate);

            var decided = await dbContext.Case.Where(c => c.Id == candidate.CaseId && c.QaSampleStatusId == 0)
                .Set(s => s.QaSampleStatusId, (byte)(picked ? 2 : 1))
                .Set(s => s.QaSampleDate, picked ? now : null)
                .UpdateAsync(token);

            if (picked && decided > 0)
            {
                await new CaseEventRepository(dbContext, "Administrator").InsertAsync(new CaseEvent
                {
                    CaseId = candidate.CaseId,
                    CaseKey = candidate.CaseKey,
                    CaseKeyValue = candidate.CaseKeyValue,
                    CaseEventTypeId = 20,
                    After = settings.QaSampleRate.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
                }, token);
            }
        }

        private static CaseRiskTrigger TriggerOf(GetCaseRiskCandidatesQueryDto candidate, CaseRiskAssessment latest)
        {
            if (latest == null)
            {
                return CaseRiskTrigger.Initial;
            }

            if (candidate.RiskRecheckTriggerId.HasValue
                && Enum.IsDefined((CaseRiskTrigger)candidate.RiskRecheckTriggerId.Value))
            {
                return (CaseRiskTrigger)candidate.RiskRecheckTriggerId.Value;
            }

            return candidate.RiskCheckedDate == null ? CaseRiskTrigger.ModelChanged : CaseRiskTrigger.ScheduledSweep;
        }

        private static bool Differs(CaseRiskAssessment latest, CaseRiskResult result)
        {
            return latest.LaneId != (byte)result.Lane.Lane
                   || latest.ImpactId != (byte)result.Impact.Band
                   || latest.LikelihoodBandId != (byte)result.Likelihood.Band
                   || latest.Priority != result.Priority
                   || latest.DeadlineBreached != (byte)(result.Lane.Breached ? 1 : 0)
                   || latest.NextDeadlineDate != result.Lane.NextDeadline
                   || Math.Abs(latest.LikelihoodProbability - result.Likelihood.Probability) >= 0.005;
        }

        private static CaseRiskAssessment ToPoco(GetCaseRiskCandidatesQueryDto candidate, CaseRiskResult result,
            CaseRiskTrigger trigger, int closedInWorkflow) => new()
        {
            CaseId = candidate.CaseId,
            TriggerId = (byte)trigger,
            MethodId = (byte)CaseRiskMethod.ColdStart,
            BaseRate = result.Likelihood.BaseRate,
            LikelihoodProbability = result.Likelihood.Probability,
            LikelihoodBandId = (byte)result.Likelihood.Band,
            EvidenceCount = closedInWorkflow,
            ContributionsJson = JsonConvert.SerializeObject(result.Likelihood.Contributions, jsonSettings),
            ImpactId = (byte)result.Impact.Band,
            ImpactComponentsJson = JsonConvert.SerializeObject(result.Impact, jsonSettings),
            LaneId = (byte)result.Lane.Lane,
            LaneReason = result.Lane.Reason,
            NextDeadlineDate = result.Lane.NextDeadline,
            DeadlineBreached = (byte)(result.Lane.Breached ? 1 : 0),
            ExpectedHarm = result.ExpectedHarm,
            Priority = result.Priority
        };

        private async Task<CaseWorkflow> GetWorkflowAsync(Guid guid, CancellationToken token)
        {
            if (!workflows.TryGetValue(guid, out var workflow))
            {
                workflow = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == guid, token);
                workflows[guid] = workflow;
            }

            return workflow;
        }

        private async Task<GetCaseWorkflowOutcomeCountsQueryDto> GetOutcomesAsync(Guid guid,
            CancellationToken token)
        {
            if (!outcomes.TryGetValue(guid, out var counts))
            {
                counts = await new GetCaseWorkflowOutcomeCountsQuery(dbContext).ExecuteAsync(guid, token);
                outcomes[guid] = counts;
            }

            return counts;
        }

        private async Task<string> GetRuleNameAsync(Guid guid, CancellationToken token)
        {
            if (!ruleNames.TryGetValue(guid, out var name))
            {
                name = await dbContext.EntityAnalysisModelActivationRule.Where(w => w.Guid == guid)
                    .Select(s => s.Name).FirstOrDefaultAsync(token) ?? "An activation rule";
                ruleNames[guid] = name;
            }

            return name;
        }
    }
}

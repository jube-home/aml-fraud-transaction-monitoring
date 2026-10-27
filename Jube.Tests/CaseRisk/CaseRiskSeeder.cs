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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;

namespace Jube.Test.CaseRisk
{
    internal sealed class CaseRiskSeeder(DatabaseFixture fx)
    {
        private readonly List<int> caseWorkflowIds = [];
        private readonly List<int> modelIds = [];
        public List<int> CaseIds { get; } = [];
        public List<Guid> WorkflowGuids { get; } = [];

        public sealed record Seeded(Guid WorkflowGuid, Guid Confirmed, Guid NotConfirmed, Guid Neutral);

        public async Task<Seeded> SeedWorkflowAsync(DbContext dbContext, byte typologySeverity = 3,
            string? valueField = null)
        {
            var user = fx.Seed.UserWithPermission;

            var model = await new EntityAnalysisModelRepository(dbContext, user).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}RiskModel{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            modelIds.Add(model.Id);

            var workflowGuid = Guid.NewGuid();

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}RiskWorkflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                TypologySeverityId = typologySeverity,
                ValueField = valueField
            });
            caseWorkflowIds.Add(workflowId);
            WorkflowGuids.Add(workflowGuid);

            return new Seeded(workflowGuid,
                await SeedStatusAsync(dbContext, workflowId, 1),
                await SeedStatusAsync(dbContext, workflowId, 2),
                await SeedStatusAsync(dbContext, workflowId, 0));
        }

        private static async Task<Guid> SeedStatusAsync(DbContext dbContext, int workflowId, byte outcome)
        {
            var guid = Guid.NewGuid();

            await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}St{outcome}{Guid.NewGuid():N}"[..30],
                Guid = guid,
                CaseWorkflowId = workflowId,
                ForeColor = "#112233",
                BackColor = "#445566",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 3,
                OutcomeId = outcome,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });

            return guid;
        }

        public async Task<int> SeedCaseAsync(DbContext dbContext, Seeded workflow, Guid statusGuid,
            byte closedStatusId = 0, string caseKeyValue = "K1", Guid? ruleGuid = null, byte diary = 0,
            DateTime? diaryDate = null, string? json = null, DateTime? riskCheckedDate = null,
            DateTime? closedDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                EntityAnalysisModelActivationRuleGuid = ruleGuid,
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = closedStatusId,
                ClosedDate = closedDate ?? (closedStatusId is 2 or 3 or 4 ? DateTime.UtcNow : null),
                CaseKey = "AccountId",
                CaseKeyValue = caseKeyValue,
                Diary = diary,
                DiaryDate = diaryDate,
                Rating = 0,
                LastClosedStatus = 0,
                Json = json ?? "{}",
                RiskCheckedDate = riskCheckedDate
            });
            CaseIds.Add(id);

            return id;
        }

        public async Task SeedClosedAsync(DbContext dbContext, Seeded workflow, int confirmed, int notConfirmed,
            Guid? ruleGuid = null, string caseKeyValue = "HISTORY")
        {
            for (var i = 0; i < confirmed; i++)
            {
                await SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, caseKeyValue, ruleGuid);
            }

            for (var i = 0; i < notConfirmed; i++)
            {
                await SeedCaseAsync(dbContext, workflow, workflow.NotConfirmed, 3, caseKeyValue, ruleGuid);
            }
        }

        public async Task GrantAsync(DbContext dbContext, Seeded workflow, params string[] users)
        {
            foreach (var user in users)
            {
                var role = await dbContext.UserRegistry.Where(u => u.Name == user).Select(u => u.RoleRegistryGuid)
                    .FirstAsync();

                await dbContext.InsertAsync(new CaseWorkflowRole
                {
                    CaseWorkflowGuid = workflow.WorkflowGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = role,
                    CreatedUser = user,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0
                });

                foreach (var status in new[] { workflow.Confirmed, workflow.NotConfirmed, workflow.Neutral })
                {
                    await dbContext.InsertAsync(new CaseWorkflowStatusRole
                    {
                        CaseWorkflowStatusGuid = status,
                        Guid = Guid.NewGuid(),
                        RoleRegistryGuid = role,
                        CreatedUser = user,
                        CreatedDate = DateTime.UtcNow,
                        Deleted = 0
                    });
                }
            }
        }

        public Task<CaseRiskAssessment> AssessAsync(DbContext dbContext, int caseId, int lane, int priority,
            double likelihood = 0.3, int likelihoodBand = 2, int impact = 3, DateTime? deadline = null,
            string? driver = null, double? value = null) =>
            new CaseRiskAssessmentRepository(dbContext).InsertAsync(new CaseRiskAssessment
            {
                CaseId = caseId,
                TriggerId = 1,
                MethodId = 1,
                BaseRate = 0.08,
                LikelihoodProbability = likelihood,
                LikelihoodBandId = (byte)likelihoodBand,
                EvidenceCount = 5,
                ContributionsJson = driver == null
                    ? "[]"
                    : "[{\"family\":\"Rule\",\"label\":\"" + driver + "\",\"logOdds\":0.9}]",
                ImpactId = (byte)impact,
                ImpactComponentsJson = value.HasValue
                    ? "{\"value\":" + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}"
                    : "{}",
                LaneId = (byte)lane,
                LaneReason = "Seeded lane",
                NextDeadlineDate = deadline,
                ExpectedHarm = likelihood * 10,
                Priority = priority
            });

        public Task<CaseEvent> EventAsync(DbContext dbContext, int caseId, int type, string user,
            DateTime? at = null) => new CaseEventRepository(dbContext, user).InsertAsync(new CaseEvent
        {
            CaseId = caseId,
            CaseEventTypeId = type,
            CaseKey = "AccountId",
            CaseKeyValue = "k"
        }).ContinueWith(async t =>
        {
            if (at.HasValue)
            {
                await dbContext.CaseEvent.Where(w => w.Id == t.Result.Id).Set(s => s.CreatedDate, at.Value)
                    .UpdateAsync();
            }

            return t.Result;
        }).Unwrap();

        public async Task CleanAsync(DbContext dbContext)
        {
            await dbContext.GetTable<CaseWorkflowStatusRole>().Where(w => dbContext.CaseWorkflowStatus
                .Where(x => caseWorkflowIds.Contains(x.CaseWorkflowId ?? 0)).Select(x => x.Guid)
                .Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowRole>().Where(w => WorkflowGuids.Contains(w.CaseWorkflowGuid))
                .DeleteAsync();
            await dbContext.GetTable<CaseEvent>().Where(w => w.CaseId != null && CaseIds.Contains(w.CaseId.Value))
                .DeleteAsync();
            await dbContext.CaseNote.Where(w => w.CaseId != null && CaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => CaseIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowStatus>()
                .Where(w => caseWorkflowIds.Contains(w.CaseWorkflowId ?? 0)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowVersion>()
                .Where(w => caseWorkflowIds.Contains(w.CaseWorkflowId ?? 0)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => caseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }
    }
}

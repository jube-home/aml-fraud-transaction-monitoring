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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;
    using Query.ArchiveKeyLookup;

    public class CaseAgentReplayRepository(DbContext dbContext)
    {
        public Task<int> CountActiveAsync(int tenantRegistryId, CancellationToken token = default)
        {
            var pending = (short)CaseAgentReplayStatus.Pending;
            var running = (short)CaseAgentReplayStatus.Running;

            return dbContext.CaseAgentReplay.CountAsync(w => w.TenantRegistryId == tenantRegistryId
                                                             && (w.StatusId == pending || w.StatusId == running), token);
        }

        public Task InsertAsync(CaseAgentReplay model, CancellationToken token = default)
        {
            return dbContext.InsertAsync(model, token: token);
        }

        public Task<CaseAgentReplay> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseAgentReplay.FirstOrDefaultAsync(w => w.Guid == guid, token);
        }

        public Task<List<CaseAgentReplay>> ListAsync(int tenantRegistryId, int take, CancellationToken token = default)
        {
            return dbContext.CaseAgentReplay.Where(w => w.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<List<CaseAgentReplayResult>> ResultsAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseAgentReplayResult.Where(w => w.ReplayGuid == guid).OrderBy(o => o.CaseId)
                .ToListAsync(token);
        }

        public Task<List<CaseAgentReplay>> ListPendingAsync(int take, CancellationToken token = default)
        {
            var pending = (short)CaseAgentReplayStatus.Pending;

            return dbContext.CaseAgentReplay.Where(w => w.StatusId == pending).OrderBy(o => o.Id).Take(take)
                .ToListAsync(token);
        }

        public async Task<bool> ClaimAsync(Guid guid, CancellationToken token = default)
        {
            var pending = (short)CaseAgentReplayStatus.Pending;
            var running = (short)CaseAgentReplayStatus.Running;

            return await dbContext.CaseAgentReplay.Where(w => w.Guid == guid && w.StatusId == pending)
                .Set(s => s.StatusId, running).Set(s => s.StartedDate, DateTime.UtcNow)
                .UpdateAsync(token).ConfigureAwait(false) == 1;
        }

        public Task CompleteAsync(Guid guid, CaseAgentReplayStatus status, int selected, int scored, string modelName,
            string error, CancellationToken token = default)
        {
            var state = (short)status;

            return dbContext.CaseAgentReplay.Where(w => w.Guid == guid)
                .Set(s => s.StatusId, state).Set(s => s.CasesSelected, selected).Set(s => s.CasesScored, scored)
                .Set(s => s.ModelName, modelName).Set(s => s.Error, error)
                .Set(s => s.CompletedDate, DateTime.UtcNow).UpdateAsync(token);
        }

        public Task InsertResultAsync(CaseAgentReplayResult result, CancellationToken token = default)
        {
            return dbContext.InsertAsync(result, token: token);
        }

        public Task<List<ReplayCaseCandidate>> ListClosedCasesAsync(int tenantRegistryId, DateTime from, DateTime to,
            Guid[] workflowGuids, int take, CancellationToken token = default)
        {
            const string sql = """
                SELECT c."Id" AS "CaseId", c."CaseKey", c."CaseKeyValue", c."CreatedDate", c."CaseWorkflowGuid",
                       m."Id" AS "EntityAnalysisModelId", s."OutcomeId" AS "HumanOutcomeId"
                FROM "Case" c
                JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND (w."Deleted" IS NULL OR w."Deleted" = 0)
                JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId"
                    AND (m."Deleted" IS NULL OR m."Deleted" = 0) AND m."TenantRegistryId" = @tenant
                JOIN "CaseWorkflowStatus" s ON s."Guid" = c."CaseWorkflowStatusGuid"
                    AND (s."Deleted" IS NULL OR s."Deleted" = 0)
                WHERE c."ClosedStatusId" = 3 AND c."ClosedDate" >= @from AND c."ClosedDate" < @to
                  AND c."CaseKey" IS NOT NULL AND c."CaseKeyValue" IS NOT NULL AND s."OutcomeId" IN (1, 2)
                  AND c."CaseWorkflowGuid" = ANY(@workflows)
                ORDER BY c."ClosedDate" DESC, c."Id" DESC
                LIMIT @take
                """;

            return dbContext.QueryToListAsync<ReplayCaseCandidate>(sql, token,
                new DataParameter("tenant", tenantRegistryId), new DataParameter("from", from),
                new DataParameter("to", to), new DataParameter("workflows", workflowGuids),
                new DataParameter("take", take));
        }

        public Task<List<ReplayArchiveRow>> ArchiveBeforeAsync(int entityAnalysisModelId, ArchiveKeyPredicate key,
            DateTime asOf, int take, CancellationToken token = default)
        {
            var parameters = new List<DataParameter>
            {
                new("model", entityAnalysisModelId), new("asof", asOf), new("take", take)
            };

            var sql = ArchiveKeyLookupSql.Select + ArchiveKeyLookupSql.From(key, v =>
            {
                parameters.Add(new DataParameter($"p{parameters.Count}", v));

                return $"@p{parameters.Count - 1}";
            }) + " and a.\"EntityAnalysisModelId\" = @model and a.\"ReferenceDate\" <= @asof" +
                      " order by a.\"ReferenceDate\" desc, a.\"Id\" desc limit @take";

            return dbContext.QueryToListAsync<ReplayArchiveRow>(sql, token, parameters.ToArray());
        }

        public Task<List<ReplayNote>> NotesBeforeAsync(int tenantRegistryId, string caseKey, string caseKeyValue,
            DateTime asOf, int take, CancellationToken token = default)
        {
            const string sql = """
                SELECT n."CreatedDate" AS "CreatedDate", n."Note" AS "Note"
                FROM "CaseNote" n
                WHERE n."CaseKey" = @key AND n."CaseKeyValue" = @value AND n."CreatedDate" <= @asof
                  AND n."SourceId" <> @askJooby AND n."CreatedDate" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM "Case" c
                              JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid"
                              JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId"
                              WHERE c."Id" = n."CaseId" AND m."TenantRegistryId" = @tenant)
                ORDER BY n."CreatedDate"
                LIMIT @take
                """;

            return dbContext.QueryToListAsync<ReplayNote>(sql, token,
                new DataParameter("key", caseKey), new DataParameter("value", caseKeyValue),
                new DataParameter("asof", asOf), new DataParameter("askJooby", (int)CaseNoteSource.AskJooby),
                new DataParameter("tenant", tenantRegistryId), new DataParameter("take", take));
        }

        public Task<List<ReplayPriorOutcome>> PriorOutcomesBeforeAsync(int tenantRegistryId, string caseKey,
            string caseKeyValue, int excludeCaseId, DateTime asOf, CancellationToken token = default)
        {
            const string sql = """
                SELECT s."OutcomeId" AS "OutcomeId", COUNT(*)::int AS "Cases"
                FROM "Case" c
                JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND (w."Deleted" IS NULL OR w."Deleted" = 0)
                JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId" AND m."TenantRegistryId" = @tenant
                JOIN "CaseWorkflowStatus" s ON s."Guid" = c."CaseWorkflowStatusGuid"
                    AND (s."Deleted" IS NULL OR s."Deleted" = 0)
                WHERE c."CaseKey" = @key AND c."CaseKeyValue" = @value AND c."Id" <> @exclude
                  AND c."ClosedStatusId" = 3 AND c."ClosedDate" <= @asof AND s."OutcomeId" IN (1, 2)
                GROUP BY s."OutcomeId"
                """;

            return dbContext.QueryToListAsync<ReplayPriorOutcome>(sql, token,
                new DataParameter("tenant", tenantRegistryId), new DataParameter("key", caseKey),
                new DataParameter("value", caseKeyValue), new DataParameter("exclude", excludeCaseId),
                new DataParameter("asof", asOf));
        }
    }

    public class ReplayCaseCandidate
    {
        public int CaseId { get; set; }
        public string CaseKey { get; set; }
        public string CaseKeyValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public int EntityAnalysisModelId { get; set; }
        public int HumanOutcomeId { get; set; }
    }

    public class ReplayArchiveRow
    {
        public DateTime ReferenceDate { get; set; }
        public string Json { get; set; }
    }

    public class ReplayNote
    {
        public DateTime CreatedDate { get; set; }
        public string Note { get; set; }
    }

    public class ReplayPriorOutcome
    {
        public int OutcomeId { get; set; }
        public int Cases { get; set; }
    }
}

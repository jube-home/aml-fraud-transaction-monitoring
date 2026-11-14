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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Query.Models;
using LinqToDB.Data;

namespace Jube.Data.Query.EraseSubjectQuery
{
    public class EraseSubjectQuery(DbContext dbContext)
    {
        private const string Sql = $"""
            WITH subject AS ({ErasureSubjectSql.SubjectEntries}),
            doomed AS (SELECT a."Id" AS id FROM "Archive" a WHERE a."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)),
            nodes AS (SELECT "Id" AS id FROM "GraphNode" WHERE "TenantRegistryId" = @tenant AND "Key" = @key AND "Value" = @value),
            payloads AS (UPDATE "Case" SET "Json" = NULL WHERE ("EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
                OR ("CaseKey" = @key AND "CaseKeyValue" = @value AND "CaseWorkflowGuid" IN (SELECT "Guid" FROM "CaseWorkflow" WHERE "TenantRegistryId" = @tenant)))
                AND "Json" IS NOT NULL RETURNING 1),
            keys AS (DELETE FROM "ArchiveKey" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject) RETURNING 1),
            key_versions AS (DELETE FROM "ArchiveKeyVersion" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject) RETURNING 1),
            tag_versions AS (DELETE FROM "ArchiveTagVersion" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject) RETURNING 1),
            tags AS (DELETE FROM "ArchiveTag" WHERE "EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject) RETURNING 1),
            versions AS (DELETE FROM "ArchiveVersion" WHERE "ArchiveId" IN (SELECT id FROM doomed) RETURNING 1),
            archive AS (DELETE FROM "Archive" WHERE "Id" IN (SELECT id FROM doomed) RETURNING 1),
            edges AS (DELETE FROM "GraphEdge" WHERE "FromNodeId" IN (SELECT id FROM nodes) OR "ToNodeId" IN (SELECT id FROM nodes) RETURNING 1),
            removed_nodes AS (DELETE FROM "GraphNode" WHERE "Id" IN (SELECT id FROM nodes) RETURNING 1)
            SELECT (SELECT count(*) FROM archive) AS "ArchiveRowsRemoved",
                   (SELECT count(*) FROM removed_nodes) AS "GraphNodesRemoved",
                   (SELECT count(*) FROM edges) AS "GraphEdgesRemoved",
                   (SELECT count(*) FROM payloads) AS "CasePayloadsNulled"
            """;

        private const string LockSql = $"""
            WITH subject AS ({ErasureSubjectSql.SubjectEntries})
            SELECT c."Id" FROM "Case" c
            WHERE c."EntityAnalysisModelInstanceEntryGuid" IN (SELECT guid FROM subject)
               OR (c."CaseKey" = @key AND c."CaseKeyValue" = @value
                   AND c."CaseWorkflowGuid" IN (SELECT "Guid" FROM "CaseWorkflow" WHERE "TenantRegistryId" = @tenant))
            FOR SHARE OF c
            """;

        public async Task LockCasesAsync(int tenantRegistryId, string keyName, string keyValue,
            CancellationToken token = default)
        {
            await dbContext.QueryToListAsync<long>(LockSql, token,
                ErasureSubjectSql.Parameters(tenantRegistryId, keyName, keyValue)).ConfigureAwait(false);
        }

        public async Task<EraseSubjectQueryDto> ExecuteAsync(int tenantRegistryId, string keyName, string keyValue,
            CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<EraseSubjectQueryDto>(Sql, token,
                ErasureSubjectSql.Parameters(tenantRegistryId, keyName, keyValue)).ConfigureAwait(false);

            return rows.First();
        }
    }
}

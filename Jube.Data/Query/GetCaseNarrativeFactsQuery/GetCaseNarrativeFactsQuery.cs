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

namespace Jube.Data.Query.GetCaseNarrativeFactsQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using Context;
    using Dto.Options;
    using LinqToDB.Data;

    public class GetCaseNarrativeFactsQuery(DbContext dbContext)
    {
        public async Task<GetCaseNarrativeFactsQueryDto> ExecuteAsync(int entityAnalysisModelId,
            ArchiveKeyPredicate caseKey, string key, string value, CancellationToken token = default)
        {
            var parameters = new List<DataParameter>
            {
                new("model", entityAnalysisModelId),
                new("key", key),
                new("value", value),
                new("askJooby", (int)CaseNoteSource.AskJooby)
            };

            var archive = caseKey is null
                ? "select 0 as \"ArchiveCount\", 0::bigint as \"ArchiveMaxId\", 0::bigint as \"ActivationCount\""
                : "select count(*) as \"ArchiveCount\", coalesce(max(a.\"Id\"), 0) as \"ArchiveMaxId\", " +
                  "coalesce(sum(a.\"ActivationRuleCount\"), 0) as \"ActivationCount\" " +
                  ArchiveKeyLookupSql.From(caseKey, v =>
                  {
                      parameters.Add(new DataParameter($"p{parameters.Count}", v));

                      return $"@p{parameters.Count - 1}";
                  }) +
                  "and a.\"EntityAnalysisModelId\" = @model";

            const string sameTenant = """
                                      and exists (select 1 from "Case" c
                                          join "CaseWorkflow" w on w."Guid" = c."CaseWorkflowGuid"
                                          join "EntityAnalysisModel" m on m."Id" = w."EntityAnalysisModelId"
                                          where c."Id" = n."CaseId" and m."TenantRegistryId" =
                                              (select t."TenantRegistryId" from "EntityAnalysisModel" t where t."Id" = @model))
                                      """;

            var sql = $"""
                       select f."ArchiveCount", f."ArchiveMaxId", f."ActivationCount",
                           (select count(*) from "CaseNote" n
                             where n."CaseKey" = @key and n."CaseKeyValue" = @value
                               and n."SourceId" <> @askJooby {sameTenant}) as "NoteCount",
                           (select coalesce(max(n."Id"), 0) from "CaseNote" n
                             where n."CaseKey" = @key and n."CaseKeyValue" = @value
                               and n."SourceId" <> @askJooby {sameTenant}) as "NoteMaxId",
                           (select count(*) from "CaseNote" n
                             where n."CaseKey" = @key and n."CaseKeyValue" = @value
                               and n."SourceId" <> @askJooby and n."IsFinding" {sameTenant}) as "FindingCount"
                       from ({archive}) f
                       """;

            var rows = await dbContext.QueryToListAsync<GetCaseNarrativeFactsQueryDto>(sql, token,
                parameters.ToArray()).ConfigureAwait(false);

            return rows.First();
        }
    }
}

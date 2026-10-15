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

namespace Jube.Data.Query.ApproveEntireModelQuery
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB.Data;
    using Repository.Models;

    public class ApproveEntireModelQuery(DbContext dbContext)
    {
        private static readonly Dictionary<string, EntityApprovalKind> byModelId = new()
        {
            { "EntityAnalysisModelRequestXpath", EntityApprovalKind.EntityAnalysisModelRequestXPath },
            { "EntityAnalysisModelInlineScript", EntityApprovalKind.EntityAnalysisModelInlineScript },
            { "EntityAnalysisModelInlineFunction", EntityApprovalKind.EntityAnalysisModelInlineFunction },
            { "EntityAnalysisModelGatewayRule", EntityApprovalKind.EntityAnalysisModelGatewayRule },
            { "EntityAnalysisModelSanction", EntityApprovalKind.EntityAnalysisModelSanction },
            { "EntityAnalysisModelAbstractionRule", EntityApprovalKind.EntityAnalysisModelAbstractionRule },
            {
                "EntityAnalysisModelAbstractionCalculation",
                EntityApprovalKind.EntityAnalysisModelAbstractionCalculation
            },
            { "EntityAnalysisModelTtlCounter", EntityApprovalKind.EntityAnalysisModelTtlCounter },
            { "EntityAnalysisModelHttpAdaptation", EntityApprovalKind.EntityAnalysisModelHttpAdaptation },
            { "ExhaustiveSearchInstance", EntityApprovalKind.ExhaustiveSearchInstance },
            { "EntityAnalysisModelActivationRule", EntityApprovalKind.EntityAnalysisModelActivationRule },
            { "EntityAnalysisModelTag", EntityApprovalKind.EntityAnalysisModelTag },
            { "AdvancedAnalyticsInstance", EntityApprovalKind.AdvancedAnalyticsInstance }
        };

        private static readonly Dictionary<string, EntityApprovalKind> byModelGuid = new()
        {
            { "EntityAnalysisModelList", EntityApprovalKind.EntityAnalysisModelList },
            { "EntityAnalysisModelDictionary", EntityApprovalKind.EntityAnalysisModelDictionary }
        };

        private static readonly Dictionary<string, (string Parent, string ParentId, EntityApprovalKind Kind)>
            byParent = new()
            {
                {
                    "EntityAnalysisModelListValue",
                    ("EntityAnalysisModelList", "EntityAnalysisModelListId",
                        EntityApprovalKind.EntityAnalysisModelListValue)
                },
                {
                    "EntityAnalysisModelDictionaryKvp",
                    ("EntityAnalysisModelDictionary", "EntityAnalysisModelDictionaryId",
                        EntityApprovalKind.EntityAnalysisModelDictionaryKvp)
                }
            };

        public async Task<int> ExecuteAsync(int entityAnalysisModelId, string approvedBy,
            CancellationToken token = default)
        {
            var stamped = await ExecuteSqlAsync($"""
                                                 insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                     "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                                 select m."TenantRegistryId", {(int)EntityApprovalKind.EntityAnalysisModel},
                                                     m."Id", coalesce(m."Version",1), {(int)EntityApprovalState.Approved},
                                                     now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                                 from "EntityAnalysisModel" m
                                                 where m."Id" = @modelId
                                                   and m."TenantRegistryId" is not null
                                                   and not exists (
                                                       select 1 from "EntityApproval" a
                                                       where a."EntityApprovalKindId" = {(int)EntityApprovalKind.EntityAnalysisModel}
                                                         and a."EntityId" = m."Id"
                                                         and a."EntityVersion" = coalesce(m."Version",1)
                                                         and a."CreatedUser" = @approvedBy);
                                                 """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);

            foreach (var (table, kind) in byModelId)
            {
                stamped += await ExecuteSqlAsync($"""
                                                  insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                      "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                                  select m."TenantRegistryId", {(int)kind}, r."Id",
                                                      coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                      now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                                  from "{table}" r
                                                  join "EntityAnalysisModel" m on m."Id" = r."EntityAnalysisModelId"
                                                  where r."EntityAnalysisModelId" = @modelId
                                                    and coalesce(r."Deleted",0) = 0
                                                    and m."TenantRegistryId" is not null
                                                    and not exists (
                                                        select 1 from "EntityApproval" a
                                                        where a."EntityApprovalKindId" = {(int)kind}
                                                          and a."EntityId" = r."Id"
                                                          and a."EntityVersion" = coalesce(r."Version",1)
                                                          and a."CreatedUser" = @approvedBy);
                                                  """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            foreach (var (table, kind) in byModelGuid)
            {
                stamped += await ExecuteSqlAsync($"""
                                                  insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                      "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                                  select m."TenantRegistryId", {(int)kind}, r."Id",
                                                      coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                      now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                                  from "{table}" r
                                                  join "EntityAnalysisModel" m on m."Guid" = r."EntityAnalysisModelGuid"
                                                  where m."Id" = @modelId
                                                    and coalesce(r."Deleted",0) = 0
                                                    and m."TenantRegistryId" is not null
                                                    and not exists (
                                                        select 1 from "EntityApproval" a
                                                        where a."EntityApprovalKindId" = {(int)kind}
                                                          and a."EntityId" = r."Id"
                                                          and a."EntityVersion" = coalesce(r."Version",1)
                                                          and a."CreatedUser" = @approvedBy);
                                                  """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            foreach (var (table, parent) in byParent)
            {
                stamped += await ExecuteSqlAsync($"""
                                                  insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                                      "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                                  select m."TenantRegistryId", {(int)parent.Kind}, r."Id",
                                                      coalesce(r."Version",1), {(int)EntityApprovalState.Approved},
                                                      now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                                  from "{table}" r
                                                  join "{parent.Parent}" p on p."Id" = r."{parent.ParentId}"
                                                  join "EntityAnalysisModel" m on m."Guid" = p."EntityAnalysisModelGuid"
                                                  where m."Id" = @modelId
                                                    and coalesce(r."Deleted",0) = 0
                                                    and m."TenantRegistryId" is not null
                                                    and not exists (
                                                        select 1 from "EntityApproval" a
                                                        where a."EntityApprovalKindId" = {(int)parent.Kind}
                                                          and a."EntityId" = r."Id"
                                                          and a."EntityVersion" = coalesce(r."Version",1)
                                                          and a."CreatedUser" = @approvedBy);
                                                  """, entityAnalysisModelId, approvedBy, token).ConfigureAwait(false);
            }

            return stamped;
        }

        public Task<int> ExecuteForUnallocatedAdvancedAnalyticsAsync(int tenantRegistryId, string approvedBy,
            CancellationToken token = default)
        {
            return dbContext.ExecuteAsync($"""
                                           insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId",
                                               "EntityId","EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                                           select r."TenantRegistryId", {(int)EntityApprovalKind.AdvancedAnalyticsInstance},
                                               r."Id", r."Version", {(int)EntityApprovalState.Approved},
                                               now() at time zone 'utc', @approvedBy, gen_random_uuid()
                                           from "AdvancedAnalyticsInstance" r
                                           where r."TenantRegistryId" = @tenantRegistryId
                                             and r."EntityAnalysisModelId" is null
                                             and r."Deleted" = 0
                                             and not exists (
                                                 select 1 from "EntityApproval" a
                                                 where a."EntityApprovalKindId" = {(int)EntityApprovalKind.AdvancedAnalyticsInstance}
                                                   and a."EntityId" = r."Id"
                                                   and a."EntityVersion" = r."Version"
                                                   and a."CreatedUser" = @approvedBy);
                                           """, token,
                new DataParameter("tenantRegistryId", tenantRegistryId),
                new DataParameter("approvedBy", approvedBy));
        }

        private Task<int> ExecuteSqlAsync(string sql, int modelId, string approvedBy, CancellationToken token)
        {
            return dbContext.ExecuteAsync(sql, token,
                new DataParameter("modelId", modelId),
                new DataParameter("approvedBy", approvedBy));
        }
    }
}
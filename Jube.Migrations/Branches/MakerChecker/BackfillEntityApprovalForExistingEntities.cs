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

namespace Jube.Migrations.Branches.MakerChecker
{
    using FluentMigrator;

    [Migration(20260929120300)]
    public class BackfillEntityApprovalForExistingEntities : Migration
    {
        private const string ApprovedBy = "Migration";

        public override void Up()
        {
            ByEntityAnalysisModelId("EntityAnalysisModelRequestXpath", 6);
            ByEntityAnalysisModelId("EntityAnalysisModelInlineScript", 7);
            ByEntityAnalysisModelId("EntityAnalysisModelInlineFunction", 8);
            ByEntityAnalysisModelId("EntityAnalysisModelGatewayRule", 9);
            ByEntityAnalysisModelId("EntityAnalysisModelSanction", 10);
            ByEntityAnalysisModelId("EntityAnalysisModelAbstractionRule", 11);
            ByEntityAnalysisModelId("EntityAnalysisModelAbstractionCalculation", 12);
            ByEntityAnalysisModelId("EntityAnalysisModelTtlCounter", 13);
            ByEntityAnalysisModelId("EntityAnalysisModelHttpAdaptation", 14);
            ByEntityAnalysisModelId("ExhaustiveSearchInstance", 15);
            ByEntityAnalysisModelId("EntityAnalysisModelActivationRule", 16, "and r.\"ReviewStatusId\" = 4");
            ByEntityAnalysisModelId("EntityAnalysisModelTag", 17);

            ByEntityAnalysisModelGuid("EntityAnalysisModelList", 2);
            ByEntityAnalysisModelGuid("EntityAnalysisModelDictionary", 4);

            ByParent("EntityAnalysisModelListValue", 3, "EntityAnalysisModelList",
                "EntityAnalysisModelListId");
            ByParent("EntityAnalysisModelDictionaryKvp", 5, "EntityAnalysisModelDictionary",
                "EntityAnalysisModelDictionaryId");

            Execute.Sql($"""
                         insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                             "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                         select m."TenantRegistryId", 1, m."Id", coalesce(m."Version",1), 1,
                             now() at time zone 'utc', '{ApprovedBy}', gen_random_uuid()
                         from "EntityAnalysisModel" m
                         where coalesce(m."Deleted",0) = 0
                           and m."TenantRegistryId" is not null;
                         """);
        }

        public override void Down()
        {
        }

        private void ByEntityAnalysisModelId(string table, int kind, string extraFilter = "")
        {
            Execute.Sql($"""
                         insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                             "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                         select m."TenantRegistryId", {kind}, r."Id", coalesce(r."Version",1), 1,
                             now() at time zone 'utc', '{ApprovedBy}', gen_random_uuid()
                         from "{table}" r
                         join "EntityAnalysisModel" m on m."Id" = r."EntityAnalysisModelId"
                         where coalesce(r."Deleted",0) = 0
                           and m."TenantRegistryId" is not null
                           {extraFilter};
                         """);
        }

        private void ByEntityAnalysisModelGuid(string table, int kind)
        {
            Execute.Sql($"""
                         insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                             "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                         select m."TenantRegistryId", {kind}, r."Id", coalesce(r."Version",1), 1,
                             now() at time zone 'utc', '{ApprovedBy}', gen_random_uuid()
                         from "{table}" r
                         join "EntityAnalysisModel" m on m."Guid" = r."EntityAnalysisModelGuid"
                         where coalesce(r."Deleted",0) = 0
                           and m."TenantRegistryId" is not null;
                         """);
        }

        private void ByParent(string table, int kind, string parentTable, string parentColumn)
        {
            Execute.Sql($"""
                         insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                             "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                         select m."TenantRegistryId", {kind}, r."Id", coalesce(r."Version",1), 1,
                             now() at time zone 'utc', '{ApprovedBy}', gen_random_uuid()
                         from "{table}" r
                         join "{parentTable}" p on p."Id" = r."{parentColumn}"
                         join "EntityAnalysisModel" m on m."Guid" = p."EntityAnalysisModelGuid"
                         where coalesce(r."Deleted",0) = 0
                           and coalesce(p."Deleted",0) = 0
                           and m."TenantRegistryId" is not null;
                         """);
        }
    }
}
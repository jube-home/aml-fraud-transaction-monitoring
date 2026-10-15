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

namespace Jube.Migrations.Branches
{
    using FluentMigrator;

    [Migration(20260126115400)]
    public class GitHubIssueBranch104 : Migration
    {
        public override void Up()
        {
            CreateCaseWorkflowRole();
            CreateCaseWorkflowStatusRole();
            CreateCaseWorkflowMacroRole();
            CreateCaseWorkflowFormRole();
            CreateCaseWorkflowActionRole();
            CreateCaseWorkflowDisplayRole();
            CreateCaseWorkflowXPathRole();
            CreateCaseWorkflowFilterRole();
            CreateVisualisationRegistryRole();
            CreateVisualisationRegistryDatasourceRole();
            CreateVisualisationRegistryParameterRole();
            InvalidateSessionCaseSearchCompiledSql();
        }

        public override void Down()
        {
        }

        private void InvalidateSessionCaseSearchCompiledSql()
        {
            Execute.Sql(
                "UPDATE \"SessionCaseSearchCompiledSql\" SET \"Rebuild\" = 1 WHERE \"Rebuild\" = 0 OR \"Rebuild\" IS NULL;");
        }

        private void CreateVisualisationRegistryParameterRole()
        {
            Create.Table("VisualisationRegistryParameterRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("VisualisationRegistryParameterGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("VisualisationRegistryParameterRole").OnColumn("VisualisationRegistryParameterGuid")
                .Ascending().OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "VisualisationRegistryParameterRole"("RoleRegistryGuid", 
                                                                "VisualisationRegistryParameterGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "VisualisationRegistryParameterGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "VisualisationRegistryParameter" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateVisualisationRegistryDatasourceRole()
        {
            Create.Table("VisualisationRegistryDatasourceRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("VisualisationRegistryDatasourceGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("VisualisationRegistryDatasourceRole")
                .OnColumn("VisualisationRegistryDatasourceGuid").Ascending().OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "VisualisationRegistryDatasourceRole"("RoleRegistryGuid", 
                                                                "VisualisationRegistryDatasourceGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "VisualisationRegistryDatasourceGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "VisualisationRegistryDatasource" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateVisualisationRegistryRole()
        {
            Create.Table("VisualisationRegistryRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("VisualisationRegistryGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("VisualisationRegistryRole").OnColumn("VisualisationRegistryGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "VisualisationRegistryRole"("RoleRegistryGuid", 
                                                                "VisualisationRegistryGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "VisualisationRegistryGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "VisualisationRegistry" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowFilterRole()
        {
            Create.Table("CaseWorkflowFilterRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowFilterGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowFilterRole").OnColumn("CaseWorkflowFilterGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowFilterRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowFilterGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowFilterGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowFilter" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowXPathRole()
        {
            Create.Table("CaseWorkflowXPathRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowXPathGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowXPathRole").OnColumn("CaseWorkflowXPathGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowXPathRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowXPathGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowXPathGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowXPath" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowDisplayRole()
        {
            Create.Table("CaseWorkflowDisplayRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowDisplayGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowDisplayRole").OnColumn("CaseWorkflowDisplayGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowDisplayRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowDisplayGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowDisplayGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowDisplay" w
                        where r."Id" = 1
                            and w."Deleted" is null
                           or w."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowActionRole()
        {
            Create.Table("CaseWorkflowActionRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowActionGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowActionRole").OnColumn("CaseWorkflowActionGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowActionRole"("RoleRegistryGuid",
                                                             "CaseWorkflowActionGuid",
                                                             "Guid",
                                                             "CreatedDate",
                                                             "CreatedUser",
                                                             "Version")
                        select r."Guid"          as "RoleRegistryGuid",
                               w."Guid"            as "CaseWorkflowActionGuid",
                               gen_random_uuid() as "Guid",
                               now()             as "CreatedDate",
                               'Administrator'   as "CreatedUser",
                               1                 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowAction" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowFormRole()
        {
            Create.Table("CaseWorkflowFormRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowFormGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowFormRole").OnColumn("CaseWorkflowFormGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowFormRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowFormGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowFormGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowForm" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowMacroRole()
        {
            Create.Table("CaseWorkflowMacroRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowMacroGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowMacroRole").OnColumn("CaseWorkflowMacroGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowMacroRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowMacroGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowMacroGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowMacro" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowStatusRole()
        {
            Create.Table("CaseWorkflowStatusRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowStatusRole").OnColumn("CaseWorkflowStatusGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowStatusRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowStatusGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowStatusGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflowStatus" w
                        where r."Id" = 1
                            and w."Deleted" is null
                           or w."Deleted" = 0
                        """);
        }

        private void CreateCaseWorkflowRole()
        {
            Create.Table("CaseWorkflowRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowRole").OnColumn("CaseWorkflowGuid").Ascending()
                .OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "CaseWorkflowRole"("RoleRegistryGuid", 
                                                                "CaseWorkflowGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "CaseWorkflowGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "CaseWorkflow" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }
    }
}
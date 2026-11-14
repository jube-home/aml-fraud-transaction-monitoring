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

namespace Jube.Migrations.Branches.CasesPage
{
    using FluentMigrator;

    [Migration(20263107850000)]
    public class CreateCaseSearchFilterTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseSearchFilter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("CaseWorkflowId").AsInt32().NotNullable()
                .WithColumn("Name").AsString(256).NotNullable()
                .WithColumn("FilterJson").AsCustom("jsonb").Nullable()
                .WithColumn("SelectJson").AsCustom("jsonb").Nullable()
                .WithColumn("SharedRoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1);

            Create.Index().OnTable("CaseSearchFilter").OnColumn("Guid").Unique();
            Create.Index().OnTable("CaseSearchFilter").OnColumn("CaseWorkflowId");
            Create.Index().OnTable("CaseSearchFilter").OnColumn("CreatedUser");

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_CaseSearchFilter_Owner_Workflow_Name"
                        ON public."CaseSearchFilter" ("CreatedUser", "CaseWorkflowId", lower("Name"));
                        """);

            Create.ForeignKey().FromTable("CaseSearchFilter").ForeignColumn("CaseWorkflowId")
                .ToTable("CaseWorkflow").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseSearchFilter" IS
                        'A search saved by one user for the Cases page: the query-builder selection and filter they composed against one Case Workflow. It is private to its owner unless shared with a role, is never versioned or put through maker-checker (unlike a Case Workflow Filter, which an administrator defines for everyone), and holds only the builder JSON: the SQL is compiled again from it on every run, so it can never reach more cases than the user and the Case Workflow roles already allow.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."Id" IS 'Server-assigned identity of the saved search.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."Guid" IS 'Public identifier of the saved search, used in the address and by the agent tools.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."CaseWorkflowId" IS 'The Case Workflow the search is composed against; the search is removed with the workflow.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."Name" IS 'The owner''s name for the search, unique for that owner within the Case Workflow ignoring case.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."FilterJson" IS 'The query-builder condition tree, as the Case Filter Builder emits it. The SQL is compiled from this on every run and is never stored.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."SelectJson" IS 'The query-builder column selection and ordering, as the Case Filter Builder emits it. This is what lets a saved search work as a report.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."SharedRoleRegistryGuid" IS 'When set, every user holding that role can also run the search; only the owner can change or delete it. Null means private to the owner.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."CreatedUser" IS 'The user that owns the saved search.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."CreatedDate" IS 'When the saved search was created (UTC).';
                        COMMENT ON COLUMN public."CaseSearchFilter"."UpdatedUser" IS 'The user that last changed the saved search.';
                        COMMENT ON COLUMN public."CaseSearchFilter"."UpdatedDate" IS 'When the saved search was last changed (UTC).';
                        COMMENT ON COLUMN public."CaseSearchFilter"."Version" IS 'Optimistic-concurrency version, raised by one on every change.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

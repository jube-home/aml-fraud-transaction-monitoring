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

namespace Jube.Migrations.Branches.CaseChecklists
{
    using FluentMigrator;

    [Migration(20263108410000)]
    public class CreateCaseWorkflowChecklist : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowChecklist")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowChecklist")
                .OnColumn("CaseWorkflowStatusGuid").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Table("CaseWorkflowChecklistVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowChecklistId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowChecklistVersion")
                .OnColumn("CaseWorkflowChecklistId").Ascending();

            Create.Table("CaseWorkflowChecklistItem")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowChecklistGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("ShowSql").AsString(int.MaxValue).Nullable()
                .WithColumn("ValidateSql").AsString(int.MaxValue).Nullable()
                .WithColumn("ValidateMessage").AsString().Nullable()
                .WithColumn("SlaMinutes").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowChecklistItem")
                .OnColumn("CaseWorkflowChecklistGuid").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Table("CaseWorkflowChecklistItemVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowChecklistItemId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowChecklistGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("ShowSql").AsString(int.MaxValue).Nullable()
                .WithColumn("ValidateSql").AsString(int.MaxValue).Nullable()
                .WithColumn("ValidateMessage").AsString().Nullable()
                .WithColumn("SlaMinutes").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowChecklistItemVersion")
                .OnColumn("CaseWorkflowChecklistItemId").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseWorkflowChecklist" IS 'A named checklist carried by one CaseWorkflowStatus; different statuses carry different checklists. Every item that is shown on a case in that status must be ticked before the case can leave the status or be closed. Joins CaseWorkflowStatus by CaseWorkflowStatusGuid on Guid rather than on Id so that a configuration export carries the checklist with the status.';
                        COMMENT ON TABLE public."CaseWorkflowChecklistVersion" IS 'The earlier state of a CaseWorkflowChecklist recorded each time it is updated; the live row is in CaseWorkflowChecklist.';
                        COMMENT ON TABLE public."CaseWorkflowChecklistItem" IS 'One item of a CaseWorkflowChecklist. ShowSql, ValidateSql and SlaMinutes are each optional. Joins CaseWorkflowChecklist by CaseWorkflowChecklistGuid on Guid.';
                        COMMENT ON TABLE public."CaseWorkflowChecklistItemVersion" IS 'The earlier state of a CaseWorkflowChecklistItem recorded each time it is updated; the live row is in CaseWorkflowChecklistItem.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklist"."Priority" IS 'Ordering of the checklist among the checklists of its status; a lower number is listed first, as for CaseWorkflowStatus. Ties are broken by Name.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."Priority" IS 'Ordering of the item within its checklist; a lower number is listed first. Ties are broken by Name.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ShowSql" IS 'Optional. A single SELECT that returns one boolean; the item is shown on the case only while it is true. Blank means the item is always shown. It may use the bound parameters @Id, the case id, and @CaseKeyValue. It runs read only, against the tenant''s own rows, with a short timeout; any failure counts as not shown.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ValidateSql" IS 'Optional. A single SELECT that returns one boolean; the item can be ticked only while it is true. Blank means the item can always be ticked. Same parameters and limits as ShowSql; any failure counts as a failed validation.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ValidateMessage" IS 'Optional text shown to the investigator when ValidateSql fails, in place of the standard message.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."SlaMinutes" IS 'Optional. Minutes allowed from the moment the item is shown on a case to its being ticked; the item is shown in red once breached. Blank means no SLA.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

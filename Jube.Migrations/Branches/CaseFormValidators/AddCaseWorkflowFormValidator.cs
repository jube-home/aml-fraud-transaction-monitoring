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

namespace Jube.Migrations.Branches.CaseFormValidators
{
    using System;
    using FluentMigrator;

    [Migration(20263108130000)]
    public class AddCaseWorkflowFormValidator : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowFormValidator")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowFormGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("FieldName").AsString().Nullable()
                .WithColumn("KindId").AsByte().Nullable()
                .WithColumn("Argument1").AsString().Nullable()
                .WithColumn("Argument2").AsString().Nullable()
                .WithColumn("Sql").AsString(int.MaxValue).Nullable()
                .WithColumn("Message").AsString().Nullable()
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

            Create.Index().OnTable("CaseWorkflowFormValidator")
                .OnColumn("CaseWorkflowFormGuid").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Table("CaseWorkflowFormValidatorVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowFormValidatorId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowFormGuid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("FieldName").AsString().Nullable()
                .WithColumn("KindId").AsByte().Nullable()
                .WithColumn("Argument1").AsString().Nullable()
                .WithColumn("Argument2").AsString().Nullable()
                .WithColumn("Sql").AsString(int.MaxValue).Nullable()
                .WithColumn("Message").AsString().Nullable()
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

            Create.Index().OnTable("CaseWorkflowFormValidatorVersion")
                .OnColumn("CaseWorkflowFormValidatorId").Ascending();

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 67,
                Name = "Author Case Workflow Form SQL Validators"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 67,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });

            Execute.Sql("""
                        UPDATE public."CaseWorkflowForm"
                        SET "Html" = '<form><label for="ExampleTextboxElement">Example textbox element</label><input name="ExampleTextboxElement"></form>'
                        WHERE "Name" = 'ExampleForm' AND "Html" LIKE '%ExampleTextboxElement%' AND "Html" LIKE '%<input id=%';

                        COMMENT ON TABLE public."CaseWorkflowFormValidator" IS 'One server-side validation rule for one field of a CaseWorkflowForm, paired to the HTML element by FieldName. The rules run, in order of Id, when a form entry is submitted and only for fields the template shows for the submitted values. Joins CaseWorkflowForm by CaseWorkflowFormGuid on Guid rather than on Id so that a configuration export carries the rule with the form. A KindId of 9 holds SQL in Sql that must return exactly one row and one column that is boolean true to pass.';
                        COMMENT ON TABLE public."CaseWorkflowFormValidatorVersion" IS 'The earlier state of a CaseWorkflowFormValidator recorded each time it is updated; the live row is in CaseWorkflowFormValidator.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."FieldName" IS 'The name attribute of the form element the rule validates, matched ignoring case. A rule whose field is no longer in the template is ignored when the form is submitted.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."KindId" IS 'The kind of rule: 1 Required, the field must have a value; 2 Required When, the field must have a value when the field named in Argument1 equals Argument2; 3 Range, the value must be between the minimum in Argument1 and the maximum in Argument2 (numbers or ISO dates, either may be blank); 4 Length, the value must be between Argument1 and Argument2 characters (either may be blank); 5 Pattern, the value must match the regular expression in Argument1; 6 Compare, the value must stand in the relation in Argument1 (eq, ne, lt, le, gt, ge) to the field named in Argument2; 7 In List, the value must be in the model list named in Argument1; 8 In Dictionary, the value must be a key of the model dictionary named in Argument1; 9 SQL, the scalar in Sql must be boolean true.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."Argument1" IS 'The first argument of the rule; its meaning depends on KindId.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."Argument2" IS 'The second argument of the rule; its meaning depends on KindId.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."Sql" IS 'For KindId 9, a single SELECT that returns one boolean. It may use the bound parameters @value, the value of the field, @CaseId, @CaseKeyValue and @ followed by the name of any other field of the form. It runs read only, against the tenant''s own rows, with a short timeout; any failure counts as a failed validation.';
                        COMMENT ON COLUMN public."CaseWorkflowFormValidator"."Message" IS 'Optional text shown to the investigator when the rule fails, in place of the standard message for the kind.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

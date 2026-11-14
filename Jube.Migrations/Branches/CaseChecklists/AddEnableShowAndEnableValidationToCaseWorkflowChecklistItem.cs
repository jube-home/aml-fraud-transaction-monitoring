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

    [Migration(20263108460000)]
    public class AddEnableShowAndEnableValidationToCaseWorkflowChecklistItem : Migration
    {
        public override void Up()
        {
            Alter.Table("CaseWorkflowChecklistItem")
                .AddColumn("EnableShow").AsByte().Nullable()
                .AddColumn("EnableValidation").AsByte().Nullable();

            Alter.Table("CaseWorkflowChecklistItemVersion")
                .AddColumn("EnableShow").AsByte().Nullable()
                .AddColumn("EnableValidation").AsByte().Nullable();

            Execute.Sql("""
                        UPDATE public."CaseWorkflowChecklistItem"
                        SET "EnableShow" = CASE WHEN "ShowSql" IS NOT NULL AND btrim("ShowSql") <> '' THEN 1 ELSE 0 END,
                            "EnableValidation" = CASE WHEN "ValidateSql" IS NOT NULL AND btrim("ValidateSql") <> '' THEN 1 ELSE 0 END;

                        UPDATE public."CaseWorkflowChecklistItemVersion"
                        SET "EnableShow" = CASE WHEN "ShowSql" IS NOT NULL AND btrim("ShowSql") <> '' THEN 1 ELSE 0 END,
                            "EnableValidation" = CASE WHEN "ValidateSql" IS NOT NULL AND btrim("ValidateSql") <> '' THEN 1 ELSE 0 END;

                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."EnableShow" IS 'Switch for the show condition: 1 evaluates ShowSql to decide whether the item is shown on a case, 0 means the item is always shown and ShowSql is not run. Turning it off retains ShowSql so it can be switched back on.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."EnableValidation" IS 'Switch for the validation condition: 1 runs ValidateSql when the item is ticked, 0 means the item can always be ticked and ValidateSql is not run. Turning it off retains ValidateSql and ValidateMessage so they can be switched back on.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ShowSql" IS 'A single SELECT that returns one boolean; the item is shown on the case only while it is true. It applies only while EnableShow is 1 and is retained when EnableShow is 0. It may use the bound parameters @Id, the case id, and @CaseKeyValue. It runs read only, against the tenant''s own rows, with a short timeout; any failure counts as not shown.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ValidateSql" IS 'A single SELECT that returns one boolean; the item can be ticked only while it is true. It applies only while EnableValidation is 1 and is retained when EnableValidation is 0. Same parameters and limits as ShowSql; any failure counts as a failed validation.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItem"."ValidateMessage" IS 'Optional text shown to the investigator when ValidateSql fails, in place of the standard message. Applies only while EnableValidation is 1 and is retained when it is 0.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItemVersion"."EnableShow" IS 'The EnableShow switch of the earlier version of the item.';
                        COMMENT ON COLUMN public."CaseWorkflowChecklistItemVersion"."EnableValidation" IS 'The EnableValidation switch of the earlier version of the item.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

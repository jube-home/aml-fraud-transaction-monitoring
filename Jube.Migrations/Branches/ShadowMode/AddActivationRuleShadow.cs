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

namespace Jube.Migrations.Branches.ShadowMode
{
    using FluentMigrator;

    [Migration(2026111014000000)]
    public class AddActivationRuleShadow : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisModelActivationRule")
                .AddColumn("Shadow").AsByte().Nullable();

            Alter.Table("EntityAnalysisModelActivationRuleVersion")
                .AddColumn("Shadow").AsByte().Nullable();

            Execute.Sql("""
                        UPDATE "EntityAnalysisModelActivationRule" SET "Shadow" = 0 WHERE "Shadow" IS NULL;
                        UPDATE "EntityAnalysisModelActivationRuleVersion" SET "Shadow" = 0 WHERE "Shadow" IS NULL;
                        COMMENT ON COLUMN public."EntityAnalysisModelActivationRule"."Shadow" IS 'Flag (1 true, 0 false). When true the rule is evaluated and every match is recorded in the archive under shadowActivation, but the match has no effect: no response elevation, case, hold, notification, webhook, TTL counter increment, activation watcher entry or rule chaining.';
                        COMMENT ON COLUMN public."EntityAnalysisModelActivationRuleVersion"."Shadow" IS 'Flag (1 true, 0 false). Version history of EntityAnalysisModelActivationRule.Shadow.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

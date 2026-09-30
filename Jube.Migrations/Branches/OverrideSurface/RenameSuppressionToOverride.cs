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

namespace Jube.Migrations.Branches.OverrideSurface
{
    using FluentMigrator;

    [Migration(20263107460000)]
    public class RenameSuppressionToOverride : Migration
    {
        private static readonly (string From, string To)[] Tables =
        [
            ("EntityAnalysisModelSuppression", "EntityAnalysisModelOverride"),
            ("EntityAnalysisModelSuppressionVersion", "EntityAnalysisModelOverrideVersion"),
            ("EntityAnalysisModelActivationRuleSuppression", "EntityAnalysisModelActivationRuleOverride"),
            ("EntityAnalysisModelActivationRuleSuppressionVersion",
                "EntityAnalysisModelActivationRuleOverrideVersion")
        ];

        private static readonly string[] EnableTables =
        [
            "EntityAnalysisModelRequestXpath",
            "EntityAnalysisModelRequestXpathVersion",
            "EntityAnalysisModelActivationRule",
            "EntityAnalysisModelActivationRuleVersion"
        ];

        public override void Up()
        {
            foreach (var (from, to) in Tables)
            {
                RenameColumn(from, "SuppressionKey", "OverrideKey");
                RenameColumn(from, "SuppressionKeyValue", "OverrideKeyValue");
                RenameColumn(to, "SuppressionKey", "OverrideKey");
                RenameColumn(to, "SuppressionKeyValue", "OverrideKeyValue");
                RenameTable(from, to);
            }

            foreach (var table in EnableTables)
            {
                RenameColumn(table, "EnableSuppression", "EnableOverride");
            }

            RenameColumn("EntityAnalysisModelOverrideVersion", "EntityAnalysisModelSuppressionId",
                "EntityAnalysisModelOverrideId");
            RenameColumn("EntityAnalysisModelActivationRuleOverrideVersion",
                "EntityAnalysisModelActivationRuleSuppressionId", "EntityAnalysisModelActivationRuleOverrideId");
        }

        public override void Down()
        {
        }

        private void RenameTable(string from, string to)
        {
            Execute.Sql($"""
                         DO $$
                         BEGIN
                             IF EXISTS (SELECT 1 FROM information_schema.tables
                                        WHERE table_schema = current_schema() AND table_name = '{from}')
                                AND NOT EXISTS (SELECT 1 FROM information_schema.tables
                                                WHERE table_schema = current_schema() AND table_name = '{to}') THEN
                                 ALTER TABLE "{from}" RENAME TO "{to}";
                             END IF;
                         END $$;
                         """);
        }

        private void RenameColumn(string table, string from, string to)
        {
            Execute.Sql($"""
                         DO $$
                         BEGIN
                             IF EXISTS (SELECT 1 FROM information_schema.columns
                                        WHERE table_schema = current_schema() AND table_name = '{table}'
                                          AND column_name = '{from}')
                                AND NOT EXISTS (SELECT 1 FROM information_schema.columns
                                                WHERE table_schema = current_schema() AND table_name = '{table}'
                                                  AND column_name = '{to}') THEN
                                 ALTER TABLE "{table}" RENAME COLUMN "{from}" TO "{to}";
                             END IF;
                         END $$;
                         """);
        }
    }
}
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

    [Migration(20263107880000)]
    public class AddRiskInputsToCaseWorkflowAndStatus : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflowStatus", "CaseWorkflowStatusVersion" })
            {
                Alter.Table(table).AddColumn("OutcomeId").AsByte().NotNullable().WithDefaultValue(0);
            }

            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Alter.Table(table).AddColumn("ValueField").AsString().Nullable();
                Alter.Table(table).AddColumn("TypologySeverityId").AsByte().NotNullable().WithDefaultValue(3);
            }

            Alter.Table("Case").AddColumn("EntityAnalysisModelActivationRuleGuid").AsGuid().Nullable();

            Create.Index("IX_Case_CaseWorkflowGuid_ActivationRuleGuid").OnTable("Case")
                .OnColumn("CaseWorkflowGuid").Ascending().OnColumn("EntityAnalysisModelActivationRuleGuid")
                .Ascending();

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."OutcomeId" IS
                        'What closing a case in this status says about the alert. 0 Not an outcome (the case is still in progress or the status says nothing about the alert), 1 Confirmed (the alert was real, a true positive), 2 Not confirmed (a false positive). Closed cases in a Confirmed or Not confirmed status are the outcomes the case risk likelihood is calibrated against; suspend-bypass closures are never counted.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."OutcomeId" IS
                        'Version history copy of CaseWorkflowStatus.OutcomeId. 0 Not an outcome, 1 Confirmed, 2 Not confirmed.';
                        COMMENT ON COLUMN public."CaseWorkflow"."ValueField" IS
                        'Name of the field in the case payload that holds the money involved, such as Amount. The case risk impact reads it to place the case in a money band; null means the workflow has no money measure and impact rests on typology alone.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."ValueField" IS
                        'Version history copy of CaseWorkflow.ValueField.';
                        COMMENT ON COLUMN public."CaseWorkflow"."TypologySeverityId" IS
                        'How severe cases of this workflow are by nature, the typology component of case risk impact. 1 Minimal, 2 Low, 3 Moderate, 4 High, 5 Severe. A terrorist financing workflow is 5, structuring 3, low-value first-party fraud 2.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."TypologySeverityId" IS
                        'Version history copy of CaseWorkflow.TypologySeverityId. 1 Minimal, 2 Low, 3 Moderate, 4 High, 5 Severe.';
                        COMMENT ON COLUMN public."Case"."EntityAnalysisModelActivationRuleGuid" IS
                        'The activation rule whose firing created the case, so that the precision of that rule against closed cases can be measured when the case risk is assessed. Null for cases created before this column existed and for cases not created by an activation rule.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

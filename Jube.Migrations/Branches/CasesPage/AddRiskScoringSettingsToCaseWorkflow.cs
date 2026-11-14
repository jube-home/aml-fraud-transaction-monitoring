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

    [Migration(20263107930000)]
    public class AddRiskScoringSettingsToCaseWorkflow : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Alter.Table(table).AddColumn("RiskKappa").AsDouble().Nullable();
                Alter.Table(table).AddColumn("RiskRho").AsDouble().Nullable();
                Alter.Table(table).AddColumn("RiskPriorStrength").AsDouble().Nullable();
                Alter.Table(table).AddColumn("RiskBaseRatePrior").AsDouble().Nullable();
                Alter.Table(table).AddColumn("RiskDeadlineLaneDays").AsInt32().Nullable();
                Alter.Table(table).AddColumn("RiskKeyHistoryHalfLifeDays").AsDouble().Nullable();
                Alter.Table(table).AddColumn("RiskImpactWeights").AsString(200).Nullable();
                Alter.Table(table).AddColumn("RiskMoneyBands").AsString(200).Nullable();
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskKappa" IS 'Case ranking: damping applied to every piece of evidence moving a case likelihood, between 0 and 1. Null uses the environment default CaseRiskKappa.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskRho" IS 'Case ranking: the weight, between 0 and 1, given to evidence that shares inputs with stronger evidence. Null uses the environment default CaseRiskRho.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskPriorStrength" IS 'Case ranking: how many imaginary cases at the prior base rate pull every estimate towards it. Null uses the environment default CaseRiskPriorStrength.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskBaseRatePrior" IS 'Case ranking: the confirmed rate assumed before the workflow has closed cases with outcomes, between 0.001 and 0.999. Null uses the environment default CaseRiskBaseRatePrior.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskDeadlineLaneDays" IS 'Case ranking: how many days before a diary deadline a case moves into the deadline near lane. Null uses the environment default CaseRiskDeadlineLaneDays.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskKeyHistoryHalfLifeDays" IS 'Case ranking: the age in days at which earlier closed cases on the same case key count for half as much. Null uses the environment default CaseRiskKeyHistoryHalfLifeDays.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskImpactWeights" IS 'Case ranking: five increasing comma separated weights, one per impact band from Minimal to Severe, multiplying likelihood into expected harm. Null uses the environment default CaseRiskImpactWeights.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskMoneyBands" IS 'Case ranking: four increasing comma separated case values that separate the five money bands of impact. Null uses the environment default CaseRiskMoneyBands.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskKappa" IS 'Version history copy of CaseWorkflow.RiskKappa.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskRho" IS 'Version history copy of CaseWorkflow.RiskRho.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskPriorStrength" IS 'Version history copy of CaseWorkflow.RiskPriorStrength.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskBaseRatePrior" IS 'Version history copy of CaseWorkflow.RiskBaseRatePrior.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskDeadlineLaneDays" IS 'Version history copy of CaseWorkflow.RiskDeadlineLaneDays.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskKeyHistoryHalfLifeDays" IS 'Version history copy of CaseWorkflow.RiskKeyHistoryHalfLifeDays.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskImpactWeights" IS 'Version history copy of CaseWorkflow.RiskImpactWeights.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskMoneyBands" IS 'Version history copy of CaseWorkflow.RiskMoneyBands.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

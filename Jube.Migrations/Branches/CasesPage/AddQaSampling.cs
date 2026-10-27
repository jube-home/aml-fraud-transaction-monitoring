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

    [Migration(20263107970000)]
    public class AddQaSampling : Migration
    {
        public override void Up()
        {
            Alter.Table("Case").AddColumn("QaSampleStatusId").AsByte().NotNullable().WithDefaultValue(0);
            Alter.Table("Case").AddColumn("QaSampleDate").AsDateTime().Nullable();

            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Alter.Table(table).AddColumn("RiskQaSampleRate").AsDouble().Nullable();
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."Case"."QaSampleStatusId" IS 'Whether the case was picked at random for a quality review. 0 not yet decided, 1 decided against, 2 picked. The decision is made once, the first time the case is assessed while it is low priority, so the sampled share stays at the configured rate. A picked case is ordered ahead of ordinary cases until it closes.';
                        COMMENT ON COLUMN public."Case"."QaSampleDate" IS 'When the case was picked for a quality review. Null when it was not.';
                        COMMENT ON COLUMN public."CaseWorkflow"."RiskQaSampleRate" IS 'Case ranking: the share, from 0 to 0.5, of the workflow''s low-priority cases picked at random for a quality review. 0 turns sampling off. Null uses the environment default CaseRiskQaSampleRate.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."RiskQaSampleRate" IS 'Version history copy of CaseWorkflow.RiskQaSampleRate.';
                        COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS
                        'What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary (set, or its type changed; After holds the diary type), 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek (the case summary was read without taking the lock), 17=Automatic Escalation (the escalation rule of the status moved the case on), 18=Lock Broken (a supervisor released the lock; Before holds the user who held it and After the reason when one was given), 19=Diary Cleared (Before holds the diary type that was cleared), 20=QA Sampled (the case was picked at random for a quality review; After holds the sample rate). Anything else renders as Unknown. Before and After carry the old and new values for the change the event records. The time to first view of a case is measured from creation to the first event of type 2, 4 or 16, and the time to ownership to the first event of type 2, 3, 6 or 14.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

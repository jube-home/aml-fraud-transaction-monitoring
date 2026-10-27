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

    [Migration(20263107940000)]
    public class AddCaseEscalationRuleToCaseWorkflowStatus : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflowStatus", "CaseWorkflowStatusVersion" })
            {
                Alter.Table(table).AddColumn("EscalationTriggerId").AsByte().Nullable();
                Alter.Table(table).AddColumn("EscalationThreshold").AsInt32().Nullable();
                Alter.Table(table).AddColumn("EscalationTargetCaseWorkflowStatusGuid").AsGuid().Nullable();
                Alter.Table(table).AddColumn("EscalationReleaseLock").AsByte().NotNullable().WithDefaultValue(0);
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."EscalationTriggerId" IS 'Escalation automation: what makes an open case in this status move on by itself. 1 Time in status reaches EscalationThreshold minutes, 2 Case is older than the workflow resolution target, 3 Case lane is at or more urgent than lane EscalationThreshold (1 Live money, 2 Mandatory, 3 Deadline near), 4 Case Priority is at or above EscalationThreshold, 5 The case diary deadline has passed. Null means no automatic escalation.';
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."EscalationThreshold" IS 'Escalation automation: the minutes, lane or Priority that EscalationTriggerId compares against. Unused by triggers 2 and 5.';
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."EscalationTargetCaseWorkflowStatusGuid" IS 'Escalation automation: the status of the same workflow the case moves to. Its priority must be stronger than this status (a lower Priority number), so that automatic escalation can never loop.';
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."EscalationReleaseLock" IS 'Escalation automation: 1 releases the lock held on the case when it moves, so that another analyst may take it. 0 leaves the lock as it is.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."EscalationTriggerId" IS 'Version history copy of CaseWorkflowStatus.EscalationTriggerId.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."EscalationThreshold" IS 'Version history copy of CaseWorkflowStatus.EscalationThreshold.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."EscalationTargetCaseWorkflowStatusGuid" IS 'Version history copy of CaseWorkflowStatus.EscalationTargetCaseWorkflowStatusGuid.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."EscalationReleaseLock" IS 'Version history copy of CaseWorkflowStatus.EscalationReleaseLock.';
                        COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS
                        'What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary, 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek (the case summary was read without taking the lock), 17=Automatic Escalation (the escalation rule of the status moved the case on). Anything else renders as Unknown. Before and After carry the old and new values for the change the event records. The time to first view of a case is measured from creation to the first event of type 2, 4 or 16, and the time to ownership to the first event of type 2, 3, 6 or 14.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

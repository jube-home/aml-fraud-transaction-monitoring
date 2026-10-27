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

    [Migration(20263107960000)]
    public class AddDiaryTypeToCase : Migration
    {
        public override void Up()
        {
            Alter.Table("Case").AddColumn("DiaryTypeId").AsByte().NotNullable().WithDefaultValue(1);
            Alter.Table("Case").AddColumn("RiskRecheckTriggerId").AsByte().Nullable();

            Execute.Sql("""
                        COMMENT ON COLUMN public."Case"."DiaryTypeId" IS 'What the diary on an open case is for, read only while Diary is 1. 1 Review, an ordinary reminder; 2 Customer response, a deadline for the customer to answer; 3 Regulatory deadline, a mandatory clock that puts the case in the Mandatory lane; 4 Payment hold, a held payment that decides at the diary date and puts the case in the Live money lane.';
                        COMMENT ON COLUMN public."Case"."RiskRecheckTriggerId" IS 'Why the case ranking must be recomputed ahead of the next sweep, using the same codes as CaseRiskAssessment.TriggerId: 5 Diary or lane threshold, 9 Risk model changed. Set by whatever asks for the recompute and cleared by the case risk worker when it has assessed the case. Null means no recompute is waiting.';
                        COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS
                        'What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary (set, or its type changed; After holds the diary type), 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek (the case summary was read without taking the lock), 17=Automatic Escalation (the escalation rule of the status moved the case on), 18=Lock Broken (a supervisor released the lock; Before holds the user who held it and After the reason when one was given), 19=Diary Cleared (Before holds the diary type that was cleared). Anything else renders as Unknown. Before and After carry the old and new values for the change the event records. The time to first view of a case is measured from creation to the first event of type 2, 4 or 16, and the time to ownership to the first event of type 2, 3, 6 or 14.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

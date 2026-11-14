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

    [Migration(20263107920000)]
    public class DescribePeekCaseEvent : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS
                        'What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary, 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek (the case summary was read without taking the lock). Anything else renders as Unknown. Before and After carry the old and new values for the change the event records. The time to first view of a case is measured from creation to the first event of type 2, 4 or 16, and the time to ownership to the first event of type 2, 3, 6 or 14.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

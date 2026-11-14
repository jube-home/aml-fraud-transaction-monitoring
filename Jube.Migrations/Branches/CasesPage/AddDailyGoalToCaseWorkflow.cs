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

    [Migration(20263107980000)]
    public class AddDailyGoalToCaseWorkflow : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Alter.Table(table).AddColumn("DailyGoal").AsInt32().Nullable();
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflow"."DailyGoal" IS
                        'How many cases each analyst aims to close in a UTC day, from 1 to 1000. It drives the daily progress ring and the streak of days the goal was met on the Cases page. Null means no goal, and a streak is then a run of days with at least one case closed.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."DailyGoal" IS
                        'Version history copy of CaseWorkflow.DailyGoal.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

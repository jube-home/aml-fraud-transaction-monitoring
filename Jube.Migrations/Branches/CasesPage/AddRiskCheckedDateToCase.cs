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

    [Migration(20263107890000)]
    public class AddRiskCheckedDateToCase : Migration
    {
        public override void Up()
        {
            Alter.Table("Case").AddColumn("RiskCheckedDate").AsDateTime2().Nullable();

            Execute.Sql("""
                        CREATE INDEX "IX_Case_RiskCheckedDate_Open" ON public."Case" ("RiskCheckedDate" NULLS FIRST)
                        WHERE "ClosedStatusId" IN (0, 1);
                        COMMENT ON COLUMN public."Case"."RiskCheckedDate" IS
                        'When the case risk worker last evaluated this case (UTC), whether or not that evaluation changed anything and so wrote a new assessment. Null means the case has not been evaluated since it was created or changed, which is what makes the worker pick it up first. Closed cases are never evaluated again.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

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

    [Migration(20263107910000)]
    public class AddServiceLevelTargets : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Alter.Table(table).AddColumn("FirstViewTargetMinutes").AsInt32().Nullable();
                Alter.Table(table).AddColumn("OwnershipTargetMinutes").AsInt32().Nullable();
                Alter.Table(table).AddColumn("ResolutionTargetMinutes").AsInt32().Nullable();
            }

            foreach (var table in new[] { "CaseWorkflowStatus", "CaseWorkflowStatusVersion" })
            {
                Alter.Table(table).AddColumn("MaxDwellMinutes").AsInt32().Nullable();
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseWorkflow"."FirstViewTargetMinutes" IS 'Service level: the most minutes a new case may wait before anyone first looks at it. Null means no target. Measured from case creation to the first peek, skim or fetch.';
                        COMMENT ON COLUMN public."CaseWorkflow"."OwnershipTargetMinutes" IS 'Service level: the most minutes a new case may wait before someone takes ownership by locking it. Null means no target. Measured from case creation to the first lock.';
                        COMMENT ON COLUMN public."CaseWorkflow"."ResolutionTargetMinutes" IS 'Service level: the most minutes a case may stay open from creation to closure. Null means no target.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."FirstViewTargetMinutes" IS 'Version history copy of CaseWorkflow.FirstViewTargetMinutes.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."OwnershipTargetMinutes" IS 'Version history copy of CaseWorkflow.OwnershipTargetMinutes.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."ResolutionTargetMinutes" IS 'Version history copy of CaseWorkflow.ResolutionTargetMinutes.';
                        COMMENT ON COLUMN public."CaseWorkflowStatus"."MaxDwellMinutes" IS 'Service level: the most minutes a case may stay in this status before it counts as overdue there. Null means no target.';
                        COMMENT ON COLUMN public."CaseWorkflowStatusVersion"."MaxDwellMinutes" IS 'Version history copy of CaseWorkflowStatus.MaxDwellMinutes.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

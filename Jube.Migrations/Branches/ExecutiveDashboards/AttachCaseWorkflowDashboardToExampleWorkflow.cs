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

namespace Jube.Migrations.Branches.ExecutiveDashboards
{
    using FluentMigrator;

    [Migration(2026111793000001)]
    public class AttachCaseWorkflowDashboardToExampleWorkflow : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        UPDATE "CaseWorkflow" w SET "VisualisationRegistryGuid" = d."Guid", "EnableVisualisation" = 1
                        FROM "VisualisationRegistry" d, "VisualisationRegistry" e
                        WHERE d."Name" = 'Case: Workflow Dashboard' AND d."TenantRegistryId" = 1 AND coalesce(d."Deleted", 0) = 0
                          AND e."Name" = 'ExampleEmbeddedVisualisation' AND e."TenantRegistryId" = 1 AND coalesce(e."Deleted", 0) = 0
                          AND w."VisualisationRegistryGuid" = e."Guid" AND coalesce(w."Deleted", 0) = 0;
                        """);
        }

        public override void Down()
        {
        }
    }
}

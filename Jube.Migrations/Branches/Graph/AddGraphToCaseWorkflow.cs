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

namespace Jube.Migrations.Branches.Graph
{
    using FluentMigrator;

    [Migration(20263108320000)]
    public class AddGraphToCaseWorkflow : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseWorkflow" ADD COLUMN IF NOT EXISTS "GraphRegistryGuid" uuid NULL;
                        ALTER TABLE public."CaseWorkflow" ADD COLUMN IF NOT EXISTS "EnableGraph" smallint NULL;
                        ALTER TABLE public."CaseWorkflowVersion" ADD COLUMN IF NOT EXISTS "GraphRegistryGuid" uuid NULL;
                        ALTER TABLE public."CaseWorkflowVersion" ADD COLUMN IF NOT EXISTS "EnableGraph" smallint NULL;

                        COMMENT ON COLUMN public."CaseWorkflow"."GraphRegistryGuid" IS 'The GraphRegistry whose network is shown on cases of this workflow, seeded by the case key. Required when EnableGraph is 1.';
                        COMMENT ON COLUMN public."CaseWorkflow"."EnableGraph" IS '1 when the case page shows the Network section for cases of this workflow.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."GraphRegistryGuid" IS 'Version history copy of CaseWorkflow.GraphRegistryGuid.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."EnableGraph" IS 'Version history copy of CaseWorkflow.EnableGraph.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

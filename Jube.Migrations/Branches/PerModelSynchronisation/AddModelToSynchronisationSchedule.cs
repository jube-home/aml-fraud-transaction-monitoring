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

namespace Jube.Migrations.Branches.PerModelSynchronisation
{
    using FluentMigrator;

    [Migration(20263107830000)]
    public class AddModelToSynchronisationSchedule : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisModelSynchronisationSchedule")
                .AddColumn("EntityAnalysisModelId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSynchronisationSchedule")
                .ForeignColumn("EntityAnalysisModelId").ToTable("EntityAnalysisModel").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);

            Execute.Sql("""
                        COMMENT ON COLUMN public."EntityAnalysisModelSynchronisationSchedule"."EntityAnalysisModelId" IS
                        'The single model this schedule row asks the engine to synchronise. Null means every model in the tenant, which is what every row meant before this column existed. A row for one model leaves the other models of the tenant loaded as they are; the row is removed with the model.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

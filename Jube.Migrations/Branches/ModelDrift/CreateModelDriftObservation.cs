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

namespace Jube.Migrations.Branches.ModelDrift
{
    using FluentMigrator;

    [Migration(2026111023000000)]
    public class CreateModelDriftObservation : Migration
    {
        public override void Up()
        {
            Create.Table("ModelDriftObservation")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().NotNullable()
                .WithColumn("SourceId").AsInt16().NotNullable()
                .WithColumn("ScoreName").AsString(500).NotNullable()
                .WithColumn("ObservedDate").AsDateTime().NotNullable()
                .WithColumn("BaselineCount").AsInt32().NotNullable()
                .WithColumn("CurrentCount").AsInt32().NotNullable()
                .WithColumn("Psi").AsDouble().NotNullable()
                .WithColumn("StatusId").AsInt16().NotNullable();

            Create.Index("IX_ModelDriftObservation_Score")
                .OnTable("ModelDriftObservation")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("SourceId").Ascending()
                .OnColumn("ScoreName").Ascending()
                .OnColumn("ObservedDate").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."ModelDriftObservation" IS 'One drift check of one live score of one model: how far the distribution of the score in the recent window has moved from the distribution in the baseline window before it, measured as the population stability index (PSI).';
                        COMMENT ON COLUMN public."ModelDriftObservation"."SourceId" IS 'Enumeration (ModelDriftSource): 1 Exhaustive adaptation score, 2 Advanced Analytics adaptation score.';
                        COMMENT ON COLUMN public."ModelDriftObservation"."ScoreName" IS 'The name of the model score as archived with each transaction.';
                        COMMENT ON COLUMN public."ModelDriftObservation"."BaselineCount" IS 'How many archived scores made up the baseline window.';
                        COMMENT ON COLUMN public."ModelDriftObservation"."CurrentCount" IS 'How many archived scores made up the recent window.';
                        COMMENT ON COLUMN public."ModelDriftObservation"."Psi" IS 'Population stability index of the recent window against the baseline, over equal-frequency bins of the baseline. Under 0.1 is stable, 0.1 to 0.25 moderate drift, over 0.25 significant drift.';
                        COMMENT ON COLUMN public."ModelDriftObservation"."StatusId" IS 'Enumeration (ModelDriftStatus): 1 Stable, 2 Moderate, 3 Significant.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

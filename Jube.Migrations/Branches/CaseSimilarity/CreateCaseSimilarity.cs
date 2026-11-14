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

namespace Jube.Migrations.Branches.CaseSimilarity
{
    using FluentMigrator;

    [Migration(2026111410000000)]
    public class CreateCaseSimilarity : Migration
    {
        public override void Up()
        {
            CreateComponentTable();
            CreateFrequencyTable();
            AddCheckedDateToCase();
        }

        public override void Down()
        {
        }

        private void CreateComponentTable()
        {
            Create.Table("CaseSimilarityComponent")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().NotNullable()
                .WithColumn("KindId").AsInt16().NotNullable()
                .WithColumn("Component").AsString(300).NotNullable();

            Create.Index("IX_CaseSimilarityComponent_Case")
                .OnTable("CaseSimilarityComponent")
                .OnColumn("CaseId").Ascending()
                .OnColumn("KindId").Ascending()
                .OnColumn("Component").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        CREATE INDEX "IX_CaseSimilarityComponent_Lookup" ON public."CaseSimilarityComponent" ("CaseWorkflowGuid", "KindId", "Component") INCLUDE ("CaseId");
                        COMMENT ON TABLE public."CaseSimilarityComponent" IS 'One fact about a case used to find similar cases in the same case workflow. The set of rows for a case is its similarity vector. Rebuilt by the similarity sweep while the case is open and left as it stood once the case is closed. Informational only: nothing here affects ranking.';
                        COMMENT ON COLUMN public."CaseSimilarityComponent"."CaseId" IS 'The case the fact describes.';
                        COMMENT ON COLUMN public."CaseSimilarityComponent"."CaseWorkflowGuid" IS 'The case workflow the case belongs to. Cases are only compared within one workflow.';
                        COMMENT ON COLUMN public."CaseSimilarityComponent"."KindId" IS 'The kind of fact: 1 Rule, 2 Model, 3 Tag, 4 Entity, 5 Category, 6 Measure, 7 Feature, 8 Band, 9 Network.';
                        COMMENT ON COLUMN public."CaseSimilarityComponent"."Component" IS 'The fact itself. Rule, model and tag names are stored as they are. Entity and Category facts are the field name and a keyed hash of the value, never the value. Measure, Feature and Band facts are a name and a size band.';
                        """);
        }

        private void CreateFrequencyTable()
        {
            Create.Table("CaseSimilarityComponentFrequency")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowGuid").AsGuid().NotNullable()
                .WithColumn("KindId").AsInt16().NotNullable()
                .WithColumn("Component").AsString(300).NotNullable()
                .WithColumn("CaseCount").AsInt32().NotNullable();

            Create.Index("IX_CaseSimilarityComponentFrequency_Component")
                .OnTable("CaseSimilarityComponentFrequency")
                .OnColumn("CaseWorkflowGuid").Ascending()
                .OnColumn("KindId").Ascending()
                .OnColumn("Component").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseSimilarityComponentFrequency" IS 'How many cases of a case workflow carry each similarity component, kept in step with CaseSimilarityComponent by the similarity sweep. A fact many cases share weighs little when cases are compared; a rare one weighs a lot.';
                        COMMENT ON COLUMN public."CaseSimilarityComponentFrequency"."CaseWorkflowGuid" IS 'The case workflow the count belongs to.';
                        COMMENT ON COLUMN public."CaseSimilarityComponentFrequency"."KindId" IS 'The kind of fact, as on CaseSimilarityComponent. Kind 0 with an empty component is the number of cases of the workflow that have a similarity vector.';
                        COMMENT ON COLUMN public."CaseSimilarityComponentFrequency"."Component" IS 'The fact, as on CaseSimilarityComponent.';
                        COMMENT ON COLUMN public."CaseSimilarityComponentFrequency"."CaseCount" IS 'The number of cases of the workflow carrying the fact.';
                        """);
        }

        private void AddCheckedDateToCase()
        {
            Alter.Table("Case").AddColumn("SimilarityCheckedDate").AsDateTime2().Nullable();

            Execute.Sql("""
                        CREATE INDEX "IX_Case_SimilarityDue" ON public."Case" ("SimilarityCheckedDate" NULLS FIRST, "Id") WHERE "SimilarityCheckedDate" IS NULL OR COALESCE("ClosedStatusId", 0) IN (0, 1) OR "ClosedDate" > "SimilarityCheckedDate";
                        COMMENT ON COLUMN public."Case"."SimilarityCheckedDate" IS 'When the similarity sweep last rebuilt the similarity vector of the case, in UTC. Null means it has never been built. An open case is rebuilt again once this is old; a closed case is rebuilt once after it closes and then left alone.';
                        """);
        }
    }
}

/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260916092505)]
    public class AddAskJoobyMemoryAndHistorySearch : Migration
    {
        public override void Up()
        {
            CreateAskJoobyMemory();
            AddAskJoobyMessageEmbeddingAndCompactFlag();
            AddAskJoobyConversationTitleSearch();
        }

        private void CreateAskJoobyMemory()
        {
            Create.Table("AskJoobyMemory")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("ScopeId").AsByte().NotNullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("Content").AsString().NotNullable()
                .WithColumn("Embedding").AsCustom("vector(1024)").Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyMemory").OnColumn("TenantRegistryId").Ascending()
                .OnColumn("ScopeId").Ascending()
                .OnColumn("CreatedUser").Ascending();

            Create.ForeignKey().FromTable("AskJoobyMemory").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyMemory_Content_Tsv" ON "AskJoobyMemory"
                USING gin (to_tsvector('english', "Content"));
                """);

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyMemory_Embedding_Hnsw" ON "AskJoobyMemory"
                USING hnsw ("Embedding" vector_cosine_ops)
                WHERE "Embedding" IS NOT NULL;
                """);
        }

        private void AddAskJoobyMessageEmbeddingAndCompactFlag()
        {
            Alter.Table("AskJoobyMessage")
                .AddColumn("Embedding").AsCustom("vector(1024)").Nullable()
                .AddColumn("IsCompact").AsBoolean().Nullable();

            Create.Index().OnTable("AskJoobyMessage").OnColumn("IsCompact").Ascending();

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyMessage_Content_Tsv" ON "AskJoobyMessage"
                USING gin (to_tsvector('english', "Content"));
                """);

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyMessage_Embedding_Hnsw" ON "AskJoobyMessage"
                USING hnsw ("Embedding" vector_cosine_ops)
                WHERE "Embedding" IS NOT NULL;
                """);
        }

        private void AddAskJoobyConversationTitleSearch()
        {
            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyConversation_Title_Tsv" ON "AskJoobyConversation"
                USING gin (to_tsvector('english', coalesce("Title", '')));
                """);
        }

        public override void Down()
        {
        }
    }
}
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
    [Migration(20263107610000)]
    public class CreateAskJoobyCompactionTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyCompaction")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("AskJoobyMessageId").AsInt32().Nullable()
                .WithColumn("CompactedToAskJoobyMessageId").AsInt32().Nullable()
                .WithColumn("MessageCount").AsInt32().Nullable()
                .WithColumn("BeforeCharacters").AsInt32().Nullable()
                .WithColumn("AfterCharacters").AsInt32().Nullable()
                .WithColumn("Before").AsCustom("text").Nullable()
                .WithColumn("After").AsCustom("text").Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("Embedding").AsCustom("vector(1024)").Nullable();

            Create.Index().OnTable("AskJoobyCompaction").OnColumn("Guid").Unique();
            Create.Index().OnTable("AskJoobyCompaction").OnColumn("AskJoobyConversationId").Ascending();
            Create.Index().OnTable("AskJoobyCompaction").OnColumn("TenantRegistryId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyCompaction").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("AskJoobyCompaction").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyCompaction_Before_Tsv" ON "AskJoobyCompaction"
                USING gin (to_tsvector('english', "Before"));
                """);

            Execute.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AskJoobyCompaction_Embedding_Hnsw" ON "AskJoobyCompaction"
                USING hnsw ("Embedding" vector_cosine_ops)
                WHERE "Embedding" IS NOT NULL;
                """);
        }

        public override void Down()
        {
        }
    }
}
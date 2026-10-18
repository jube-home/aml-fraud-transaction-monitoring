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

using System;
using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260916092500)]
    public class CreateAskJoobyChatTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            CreateAskJoobyConversation();
            CreateAskJoobyMessage();
            CreateAskJoobyRun();
            CreateAskJoobyToolCall();
            CreateAskJoobyModelCall();
            CreateAskJoobyRagChunk();
            GrantAskJoobyPermission();
        }

        private void CreateAskJoobyConversation()
        {
            Create.Table("AskJoobyConversation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Title").AsString(255).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString(255).Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(255).Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("AskJoobyConversation").OnColumn("TenantRegistryId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyConversation").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");
        }

        private void CreateAskJoobyMessage()
        {
            Create.Table("AskJoobyMessage")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("RoleId").AsByte().Nullable()
                .WithColumn("Content").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyMessage").OnColumn("AskJoobyConversationId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyMessage").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");

            TrigramIndex("AskJoobyMessage", "Content");
        }

        private void CreateAskJoobyRun()
        {
            Create.Table("AskJoobyRun")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("ParentAskJoobyRunId").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("AgentType").AsString(255).Nullable()
                .WithColumn("Depth").AsInt32().Nullable()
                .WithColumn("Task").AsString().Nullable()
                .WithColumn("Result").AsString().Nullable()
                .WithColumn("StatusId").AsByte().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyRun").OnColumn("AskJoobyConversationId").Ascending();
            Create.Index().OnTable("AskJoobyRun").OnColumn("ParentAskJoobyRunId").Ascending();
            Create.Index().OnTable("AskJoobyRun").OnColumn("TenantRegistryId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyRun").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("AskJoobyRun").ForeignColumn("ParentAskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");
        }

        private void CreateAskJoobyToolCall()
        {
            Create.Table("AskJoobyToolCall")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("ToolName").AsString(255).Nullable()
                .WithColumn("Arguments").AsCustom("jsonb").Nullable()
                .WithColumn("Result").AsString().Nullable()
                .WithColumn("StatusId").AsByte().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyToolCall").OnColumn("AskJoobyRunId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyToolCall").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");
        }

        private void CreateAskJoobyModelCall()
        {
            Create.Table("AskJoobyModelCall")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("ModelTypeId").AsByte().Nullable()
                .WithColumn("InputTokens").AsInt32().Nullable()
                .WithColumn("OutputTokens").AsInt32().Nullable()
                .WithColumn("FinishReasonId").AsByte().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("FirstChunkMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyModelCall").OnColumn("AskJoobyRunId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyModelCall").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");

            Execute.Sql(
                "COMMENT ON COLUMN \"AskJoobyModelCall\".\"ModelTypeId\" IS " +
                "'See Jube.Data.Repository.Models.AskJoobyModelType. 0=Unknown (the provider reported a model " +
                "string this deployment does not recognise), 1=DeepSeekV41Flash, 2=DeepSeekV4Pro, " +
                "3=BgeM3, 4=BgeRerankerV2M3. NULL behaves as 0.';");
        }

        private void CreateAskJoobyRagChunk()
        {
            Create.Table("AskJoobyRagChunk")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Hash").AsString(64).NotNullable()
                .WithColumn("ParentHash").AsString(64).Nullable()
                .WithColumn("CategoryId").AsByte().Nullable()
                .WithColumn("KindId").AsByte().Nullable()
                .WithColumn("SourceFile").AsString(1024).Nullable()
                .WithColumn("StartLine").AsInt32().Nullable()
                .WithColumn("EndLine").AsInt32().Nullable()
                .WithColumn("QualifiedName").AsString(1024).Nullable()
                .WithColumn("Content").AsString().Nullable()
                .WithColumn("Embedding").AsCustom("vector(1024)").Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index("IX_AskJoobyRagChunk_Hash").OnTable("AskJoobyRagChunk")
                .OnColumn("Hash").Ascending()
                .WithOptions().Unique();

            Create.Index().OnTable("AskJoobyRagChunk").OnColumn("ParentHash").Ascending();

            Create.Index().OnTable("AskJoobyRagChunk").OnColumn("CategoryId").Ascending()
                .OnColumn("KindId").Ascending();

            Execute.Sql("""
                        CREATE INDEX IF NOT EXISTS "IX_AskJoobyRagChunk_Content_Tsv" ON "AskJoobyRagChunk"
                        USING gin (to_tsvector('english', "Content"));
                        """);

            Execute.Sql("""
                        CREATE INDEX IF NOT EXISTS "IX_AskJoobyRagChunk_Embedding_Hnsw" ON "AskJoobyRagChunk"
                        USING hnsw ("Embedding" vector_cosine_ops)
                        WHERE "Embedding" IS NOT NULL;
                        """);
        }

        private void GrantAskJoobyPermission()
        {
            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 65,
                Name = "Access AskJooby"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 65,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private void TrigramIndex(string table, string column)
        {
            Execute.Sql(
                $"CREATE INDEX IF NOT EXISTS \"IX_{table}_{column}_Trgm\" ON \"{table}\" USING gin (lower(\"{column}\") gin_trgm_ops);");
        }

        public override void Down()
        {
        }
    }
}
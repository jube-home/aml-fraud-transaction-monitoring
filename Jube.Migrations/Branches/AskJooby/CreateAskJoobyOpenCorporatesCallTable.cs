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
    [Migration(20263107660200)]
    public class CreateAskJoobyOpenCorporatesCallTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyOpenCorporatesCall")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyRunId").AsInt32().Nullable()
                .WithColumn("Operation").AsString(255).Nullable()
                .WithColumn("Query").AsString().Nullable()
                .WithColumn("ResponseJson").AsCustom("jsonb").Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable();

            Create.Index().OnTable("AskJoobyOpenCorporatesCall").OnColumn("AskJoobyRunId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyOpenCorporatesCall").ForeignColumn("AskJoobyRunId")
                .ToTable("AskJoobyRun").PrimaryColumn("Id");

            CommentTable("AskJoobyOpenCorporatesCall",
                "One OpenCorporatesTool call made against the configured KYB/UBO provider (see "
                + "IOpenCorporatesClient, EnableOpenCorporates) -- Query is exactly what was sent (a name, a "
                + "jurisdiction/company number pair, or a UBO search term), ResponseJson is the provider's own "
                + "response verbatim. Distinct from AskJoobyToolCall, which already logs every tool call's "
                + "formatted text result generically: this table exists specifically so the raw provider "
                + "payload survives AgentGuard screening and the tool's own prose summary, neither of which is "
                + "the verbatim response.");

            CommentColumn("AskJoobyOpenCorporatesCall", "Operation",
                "Which of OpenCorporatesTool's three methods made this call: SearchCompany, GetCompanyDetails, "
                + "or SearchBeneficialOwners.");

            CommentColumn("AskJoobyOpenCorporatesCall", "Query",
                "The identifying input exactly as sent to the provider, after AgentGuard's own tool-call and "
                + "input-injection screening has already run against it -- see OpenCorporatesTool and "
                + "ToolCallAndResultGuardChatClient.");
        }

        private void CommentTable(string table, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.tables
                                      WHERE table_schema = 'public' AND table_name = '{table}') THEN
                             EXECUTE format('COMMENT ON TABLE public."{table}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }

        private void CommentColumn(string table, string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.columns
                                      WHERE table_schema = 'public' AND table_name = '{table}'
                                        AND column_name = '{column}') THEN
                             EXECUTE format('COMMENT ON COLUMN public."{table}"."{column}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }

        public override void Down()
        {
        }
    }
}
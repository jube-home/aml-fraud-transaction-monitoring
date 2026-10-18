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
    [Migration(20263107690000)]
    public class CreateAskJoobyErrataTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyErrata")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Affects").AsString(255).Nullable()
                .WithColumn("Body").AsString(4000).NotNullable()
                .WithColumn("Active").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Comment("AskJoobyErrata",
                "Errata for this running instance: known errors, omissions and footguns that every agent must "
                + "be told about until the underlying problem is fixed. Written by hand from the back end, never "
                + "by an agent or from the chat. Each active row is rendered into the shared prompt preamble in "
                + "Id order. Retire a row by setting Active to false rather than deleting it, so the record of "
                + "what the instance was told survives.");

            Comment("AskJoobyErrata", "Affects",
                "The agent type this erratum is for, matched exactly against the agent's own system name such as "
                + "model-admin. Null means every agent receives it.");

            Comment("AskJoobyErrata", "Body",
                "The erratum itself, written as plain prompt text addressed to the model: what is wrong, what to "
                + "do instead, and anything it must not assume.");

            Comment("AskJoobyErrata", "Active",
                "True while the erratum is still in force and is rendered into every agent's prompt; false once "
                + "the underlying problem is fixed.");
        }

        public override void Down()
        {
        }

        private void Comment(string table, string text)
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

        private void Comment(string table, string column, string text)
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
    }
}
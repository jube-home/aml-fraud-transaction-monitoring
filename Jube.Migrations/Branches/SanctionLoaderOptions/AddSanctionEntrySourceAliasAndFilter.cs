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

namespace Jube.Migrations.Branches.SanctionLoaderOptions
{
    using FluentMigrator;

    [Migration(2026111018000000)]
    public class AddSanctionEntrySourceAliasAndFilter : Migration
    {
        public override void Up()
        {
            Alter.Table("SanctionEntrySource")
                .AddColumn("AliasIndex").AsInt16().Nullable()
                .AddColumn("AliasSeparator").AsString().Nullable()
                .AddColumn("FilterIndex").AsInt16().Nullable()
                .AddColumn("FilterValues").AsString().Nullable();

            Execute.Sql("""
                        COMMENT ON COLUMN public."SanctionEntrySource"."AliasIndex" IS 'Zero based column holding other names of the same entity, separated by AliasSeparator. Each alias is loaded as its own sanction entry with the same reference as the entity. Null when the file has no alias column.';
                        COMMENT ON COLUMN public."SanctionEntrySource"."AliasSeparator" IS 'The text that separates the aliases inside the AliasIndex column, for example a semicolon. Defaults to a semicolon when AliasIndex is set and this is null.';
                        COMMENT ON COLUMN public."SanctionEntrySource"."FilterIndex" IS 'Zero based column to filter rows on, for example the entity type. Null loads every row.';
                        COMMENT ON COLUMN public."SanctionEntrySource"."FilterValues" IS 'Comma separated values of the FilterIndex column that are loaded, compared ignoring case. Rows with any other value are not loaded, and entries previously loaded for them are removed at the next load. Null or empty loads every row.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

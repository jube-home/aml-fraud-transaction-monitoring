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
    [Migration(20263107620000)]
    public class AddSchemaColumnCommentsForFlags : Migration
    {
        private const string Deleted =
            "Soft-delete flag. Nullable smallint, not a boolean: a live row usually has NULL here rather " +
            "than 0, and only a deleted row has 1. Always test (\"Deleted\" = 0 OR \"Deleted\" IS NULL) " +
            "for live rows; testing \"Deleted\" = 0 alone silently returns nothing on live data.";

        private const string Active =
            "Whether the entry participates at runtime. Nullable smallint, not a boolean: 1 is active, 0 " +
            "is inactive, and NULL means it was never set and is treated as inactive. Independent of " +
            "\"Deleted\" -- an active row can still be deleted, and a live row can be inactive.";

        private const string Locked =
            "Whether the entry is locked against editing in the user interface. Nullable smallint, not a " +
            "boolean: 1 is locked, 0 or NULL is editable. Has no effect on runtime behaviour.";

        private const string ResponsePayload =
            "Whether this entry's value is returned in the real-time response payload of an invocation. " +
            "Nullable smallint, not a boolean: 1 returns it, 0 or NULL withholds it.";

        private const string ReportTable =
            "Whether this entry's value is written to the reporting/archive table for later analysis. " +
            "Nullable smallint, not a boolean: 1 writes it, 0 or NULL does not.";

        private const string SearchKey =
            "Whether this field is a search key, meaning values are indexed and cached for lookup by that " +
            "key. Nullable smallint, not a boolean: 1 is a search key, 0 or NULL is not.";

        private const string Cache =
            "Whether values for this field are cached. Nullable smallint, not a boolean: 1 caches, 0 or " +
            "NULL does not.";

        public override void Up()
        {
            Comment("Deleted", Deleted);
            Comment("Active", Active);
            Comment("Locked", Locked);
            Comment("ResponsePayload", ResponsePayload);
            Comment("ReportTable", ReportTable);
            Comment("SearchKey", SearchKey);
            Comment("Cache", Cache);
        }

        public override void Down()
        {
        }

        private void Comment(string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         DECLARE r record;
                         BEGIN
                           FOR r IN SELECT table_name FROM information_schema.columns
                                    WHERE table_schema = 'public' AND column_name = '{column}'
                           LOOP
                             EXECUTE format('COMMENT ON COLUMN public.%I."{column}" IS %L', r.table_name,
                                            '{quoted}');
                           END LOOP;
                         END $$;
                         """);
        }
    }
}
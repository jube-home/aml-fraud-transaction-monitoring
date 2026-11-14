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

namespace Jube.Migrations.Branches.AskJoobyAnalytics
{
    using FluentMigrator;

    [Migration(20263107840000)]
    public class RenameAdvancedAnalyticsStateToAskJoobyAnalytics : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE
                            r record;
                            pattern text := 'AdvancedAnalytics(Instance|TrainedModel)';
                            replacement text := 'AskJoobyAnalytics\1';
                        BEGIN
                            FOR r IN
                                SELECT c.relname
                                FROM pg_class c
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind = 'r' AND c.relname ~ pattern
                            LOOP
                                EXECUTE format('ALTER TABLE public.%I RENAME TO %I', r.relname,
                                    regexp_replace(r.relname, pattern, replacement, 'g'));
                            END LOOP;

                            FOR r IN
                                SELECT c.relname AS table_name, a.attname AS column_name
                                FROM pg_attribute a
                                JOIN pg_class c ON c.oid = a.attrelid
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind = 'r' AND a.attnum > 0
                                  AND NOT a.attisdropped AND a.attname ~ pattern
                            LOOP
                                EXECUTE format('ALTER TABLE public.%I RENAME COLUMN %I TO %I', r.table_name,
                                    r.column_name, regexp_replace(r.column_name, pattern, replacement, 'g'));
                            END LOOP;

                            FOR r IN
                                SELECT c.relname AS table_name, k.conname
                                FROM pg_constraint k
                                JOIN pg_class c ON c.oid = k.conrelid
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND k.conname ~ pattern
                            LOOP
                                EXECUTE format('ALTER TABLE public.%I RENAME CONSTRAINT %I TO %I', r.table_name,
                                    r.conname, regexp_replace(r.conname, pattern, replacement, 'g'));
                            END LOOP;

                            FOR r IN
                                SELECT c.relname, c.relkind
                                FROM pg_class c
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind IN ('i', 'S') AND c.relname ~ pattern
                            LOOP
                                IF r.relkind = 'i' THEN
                                    EXECUTE format('ALTER INDEX public.%I RENAME TO %I', r.relname,
                                        regexp_replace(r.relname, pattern, replacement, 'g'));
                                ELSE
                                    EXECUTE format('ALTER SEQUENCE public.%I RENAME TO %I', r.relname,
                                        regexp_replace(r.relname, pattern, replacement, 'g'));
                                END IF;
                            END LOOP;

                            FOR r IN
                                SELECT c.relname AS table_name, d.description
                                FROM pg_description d
                                JOIN pg_class c ON c.oid = d.objoid AND d.objsubid = 0
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind = 'r' AND d.description ~ pattern
                            LOOP
                                EXECUTE format('COMMENT ON TABLE public.%I IS %L', r.table_name,
                                    regexp_replace(r.description, pattern, replacement, 'g'));
                            END LOOP;

                            FOR r IN
                                SELECT c.relname AS table_name, a.attname AS column_name, d.description
                                FROM pg_description d
                                JOIN pg_class c ON c.oid = d.objoid AND d.objsubid > 0
                                JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.objsubid
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind = 'r' AND d.description ~ pattern
                            LOOP
                                EXECUTE format('COMMENT ON COLUMN public.%I.%I IS %L', r.table_name,
                                    r.column_name, regexp_replace(r.description, pattern, replacement, 'g'));
                            END LOOP;
                        END
                        $$;
                        """);
        }

        public override void Down()
        {
        }
    }
}

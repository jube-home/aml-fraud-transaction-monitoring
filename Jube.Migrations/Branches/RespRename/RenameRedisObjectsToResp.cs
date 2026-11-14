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

namespace Jube.Migrations.Branches.RespRename
{
    using FluentMigrator;

    [Migration(2026110612000000)]
    public class RenameRedisObjectsToResp : Migration
    {
        public override void Up()
        {
            RenameTablesColumnsConstraintsIndexesAndSequences();
            RenameCompoundIdentifiersInComments();
        }

        public override void Down()
        {
        }

        private void RenameTablesColumnsConstraintsIndexesAndSequences()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE
                            r record;
                            known text[] := ARRAY['RedisCallCounter', 'RedisConnectionEvent', 'RedisConnectionMultiplexerMetric',
                                'RedisMetric', 'RedisSentinelEvent', 'RedisSentinelStatus', 'RedisSlowOperation'];
                            old_table text;
                            current_table text;
                            new_name text;
                        BEGIN
                            FOREACH old_table IN ARRAY known
                            LOOP
                                new_name := replace(old_table, 'Redis', 'Resp');
                                IF to_regclass(format('public.%I', old_table)) IS NOT NULL
                                   AND to_regclass(format('public.%I', new_name)) IS NULL THEN
                                    EXECUTE format('ALTER TABLE public.%I RENAME TO %I', old_table, new_name);
                                END IF;
                            END LOOP;

                            FOREACH old_table IN ARRAY known
                            LOOP
                                current_table := replace(old_table, 'Redis', 'Resp');

                                IF to_regclass(format('public.%I', current_table)) IS NULL THEN
                                    CONTINUE;
                                END IF;

                                FOR r IN
                                    SELECT a.attname
                                    FROM pg_attribute a
                                    WHERE a.attrelid = format('public.%I', current_table)::regclass AND a.attnum > 0
                                      AND NOT a.attisdropped AND a.attname LIKE '%Redis%'
                                LOOP
                                    new_name := replace(r.attname, 'Redis', 'Resp');
                                    IF NOT EXISTS (SELECT 1 FROM pg_attribute x
                                                   WHERE x.attrelid = format('public.%I', current_table)::regclass
                                                     AND x.attname = new_name AND NOT x.attisdropped) THEN
                                        EXECUTE format('ALTER TABLE public.%I RENAME COLUMN %I TO %I', current_table, r.attname, new_name);
                                    END IF;
                                END LOOP;

                                FOR r IN
                                    SELECT k.conname
                                    FROM pg_constraint k
                                    WHERE k.conrelid = format('public.%I', current_table)::regclass AND k.conname LIKE '%Redis%'
                                LOOP
                                    new_name := replace(r.conname, 'Redis', 'Resp');
                                    IF NOT EXISTS (SELECT 1 FROM pg_constraint x
                                                   WHERE x.conrelid = format('public.%I', current_table)::regclass AND x.conname = new_name)
                                       AND to_regclass(format('public.%I', new_name)) IS NULL THEN
                                        EXECUTE format('ALTER TABLE public.%I RENAME CONSTRAINT %I TO %I', current_table, r.conname, new_name);
                                    END IF;
                                END LOOP;

                                FOR r IN
                                    SELECT c.relname
                                    FROM pg_index i
                                             JOIN pg_class c ON c.oid = i.indexrelid
                                    WHERE i.indrelid = format('public.%I', current_table)::regclass AND c.relname LIKE '%Redis%'
                                LOOP
                                    new_name := replace(r.relname, 'Redis', 'Resp');
                                    IF to_regclass(format('public.%I', new_name)) IS NULL THEN
                                        EXECUTE format('ALTER INDEX public.%I RENAME TO %I', r.relname, new_name);
                                    END IF;
                                END LOOP;

                                FOR r IN
                                    SELECT c.relname
                                    FROM pg_class c
                                             JOIN pg_depend d ON d.objid = c.oid AND d.classid = 'pg_class'::regclass
                                                 AND d.refobjid = format('public.%I', current_table)::regclass
                                    WHERE c.relkind = 'S' AND c.relname LIKE '%Redis%'
                                LOOP
                                    new_name := replace(r.relname, 'Redis', 'Resp');
                                    IF to_regclass(format('public.%I', new_name)) IS NULL THEN
                                        EXECUTE format('ALTER SEQUENCE public.%I RENAME TO %I', r.relname, new_name);
                                    END IF;
                                END LOOP;
                            END LOOP;
                        END
                        $$;
                        """);
        }

        private void RenameCompoundIdentifiersInComments()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE
                            r record;
                        BEGIN
                            FOR r IN
                                SELECT d.objoid, d.classoid, d.objsubid, d.description
                                FROM pg_description d
                                         JOIN pg_class c ON c.oid = d.objoid AND d.classoid = 'pg_class'::regclass
                                         JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p')
                                  AND d.description ~ 'Redis(?=[A-Z_])'
                            LOOP
                                IF r.objsubid = 0 THEN
                                    EXECUTE format('COMMENT ON TABLE %s IS %L', r.objoid::regclass,
                                                   regexp_replace(r.description, 'Redis(?=[A-Z_])', 'Resp', 'g'));
                                ELSE
                                    EXECUTE format('COMMENT ON COLUMN %s.%I IS %L', r.objoid::regclass,
                                                   (SELECT attname FROM pg_attribute WHERE attrelid = r.objoid AND attnum = r.objsubid),
                                                   regexp_replace(r.description, 'Redis(?=[A-Z_])', 'Resp', 'g'));
                                END IF;
                            END LOOP;
                        END
                        $$;
                        """);
        }
    }
}

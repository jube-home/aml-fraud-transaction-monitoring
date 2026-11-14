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


using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Reporting;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class PostgresSqlValidatorCorpusTests
    {
        private static readonly IReadOnlyDictionary<string, string> allowed =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Archive"] =
                    "SELECT t0.* FROM \"Archive\" t0 WHERE EXISTS (SELECT 1 FROM \"UserInTenant\" uit WHERE uit.\"User\" = 'u')",
                ["Reference"] = "SELECT r0.* FROM \"Reference\" r0"
            };

        private static readonly string[] statements =
        [
            "INSERT INTO \"Case\" (\"Id\") VALUES (1)", "UPDATE \"Case\" SET \"Id\" = 1", "DELETE FROM \"Case\"",
            "TRUNCATE \"Case\"", "DROP TABLE \"Case\"", "CREATE TABLE x (a int)", "CREATE TABLE x AS SELECT 1",
            "ALTER TABLE \"Case\" ADD COLUMN x int", "GRANT ALL ON \"Case\" TO PUBLIC", "REVOKE ALL ON \"Case\" FROM PUBLIC",
            "SELECT 1 INTO x", "SELECT * INTO TEMP x FROM \"Case\"", "SELECT * INTO UNLOGGED x FROM \"Case\"",
            "COPY \"Case\" TO STDOUT", "COPY (SELECT 1) TO PROGRAM 'id'", "COPY x FROM PROGRAM 'id'",
            "COPY (SELECT 1) TO '/tmp/x'", "EXPLAIN SELECT 1", "EXPLAIN ANALYZE SELECT 1",
            "EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM \"Case\"", "SELECT 1 FROM \"Case\" FOR UPDATE",
            "SELECT 1 FROM \"Case\" FOR SHARE", "SELECT 1 FROM \"Case\" FOR NO KEY UPDATE",
            "SELECT 1 FROM \"Case\" FOR KEY SHARE", "SELECT 1 FROM \"Case\" FOR UPDATE NOWAIT",
            "SELECT 1 FROM \"Case\" FOR UPDATE SKIP LOCKED", "SELECT 1 FROM \"Case\" FOR UPDATE OF \"Case\"",
            "SELECT * FROM (SELECT 1 FROM \"Case\" FOR UPDATE) s",
            "WITH x AS (SELECT 1 FROM \"Case\" FOR UPDATE) SELECT * FROM x",
            "PREPARE p AS SELECT 1", "EXECUTE p", "DEALLOCATE p", "DO $$ BEGIN PERFORM 1; END $$",
            "SET ROLE postgres", "SET SESSION AUTHORIZATION postgres", "SET search_path TO x",
            "SET LOCAL statement_timeout = 0", "SET default_transaction_read_only = off", "RESET ALL",
            "RESET ROLE", "SHOW ALL", "SHOW server_version", "CALL x()", "VACUUM", "ANALYZE", "LOCK TABLE \"Case\"",
            "LISTEN x", "NOTIFY x", "UNLISTEN *", "BEGIN", "COMMIT", "ROLLBACK", "START TRANSACTION READ WRITE",
            "SAVEPOINT x", "DECLARE c CURSOR FOR SELECT 1", "FETCH 1 FROM c", "CLOSE c", "DISCARD ALL",
            "REFRESH MATERIALIZED VIEW x", "REINDEX TABLE \"Case\"", "CLUSTER \"Case\"", "COMMENT ON TABLE \"Case\" IS 'x'",
            "CREATE EXTENSION dblink", "CREATE FUNCTION f() RETURNS int AS 'select 1' LANGUAGE sql",
            "CREATE VIEW v AS SELECT 1", "CREATE ROLE r SUPERUSER", "ALTER ROLE postgres PASSWORD 'x'",
            "ALTER SYSTEM SET work_mem = '1GB'", "SECURITY LABEL ON TABLE x IS 'y'", "IMPORT FOREIGN SCHEMA s FROM SERVER v INTO public",
            "MERGE INTO \"Case\" t USING \"Case\" s ON true WHEN MATCHED THEN DELETE",
            "WITH x AS (INSERT INTO \"Case\" (\"Id\") VALUES (1) RETURNING \"Id\") SELECT * FROM x",
            "WITH x AS (UPDATE \"Case\" SET \"Id\" = 1 RETURNING \"Id\") SELECT * FROM x",
            "WITH x AS (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM x",
            "WITH x AS MATERIALIZED (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM x",
            "WITH x AS NOT MATERIALIZED (UPDATE \"Case\" SET \"Id\" = 1 RETURNING \"Id\") SELECT * FROM x",
            "WITH RECURSIVE x AS (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM x",
            "WITH a AS (SELECT 1), x AS (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM a, x",
            "SELECT * FROM (WITH x AS (DELETE FROM \"Case\" RETURNING \"Id\") SELECT * FROM x) s",
            "SELECT 1 WHERE EXISTS (WITH x AS (INSERT INTO \"Case\" (\"Id\") VALUES (1) RETURNING 1) SELECT * FROM x)",
            "SELECT 1; SELECT 2", "SELECT 1; DROP TABLE \"Case\"", "SELECT 1;;DELETE FROM \"Case\"",
            "SELECT 1 -- x\n; DELETE FROM \"Case\"", "/* ; */ DELETE FROM \"Case\"", "SELECT 1/**/;/**/SELECT 2",
            "SELECT $$a$$; DROP TABLE \"Case\"", "SELECT 'a'; DROP TABLE \"Case\"", "SELECT E'\\\\'; DROP TABLE \"Case\"; --'",
            "SELECT 1 /* nested /* comment */ ; DELETE FROM \"Case\" */ ; DELETE FROM \"Case\"",
            "VALUES (1); DELETE FROM \"Case\"", "TABLE \"Case\"; DELETE FROM \"Case\"",
            "", "   ", "\n\t", ";", ";;", "SELECT FROM WHERE", "SELEC 1"
        ];

        private static readonly string[] deniedFunctions =
        [
            "pg_read_file('/etc/passwd')", "pg_read_binary_file('/etc/passwd')", "pg_ls_dir('/')",
            "pg_stat_file('/etc/passwd')", "pg_ls_logdir()", "pg_ls_waldir()", "lo_import('/etc/passwd')",
            "lo_export(1, '/tmp/x')", "lo_get(1)", "lo_from_bytea(0, 'x')", "lo_unlink(1)", "lo_open(1, 1)",
            "loread(1, 1)", "lowrite(1, 'x')", "dblink('host=x', 'select 1')", "dblink_connect('host=x')",
            "dblink_exec('host=x', 'select 1')", "dblink_open('c', 'select 1')", "query_to_xml('select 1', true, true, '')",
            "table_to_xml('x', true, true, '')", "schema_to_xml('public', true, true, '')",
            "database_to_xml(true, true, '')", "cursor_to_xml('c', 1, true, true, '')", "ts_stat('select 1')",
            "pg_sleep(10)", "pg_sleep_for('10 seconds')", "pg_sleep_until(now())", "set_config('a', 'b', false)",
            "current_setting('server_version')", "pg_terminate_backend(1)", "pg_cancel_backend(1)", "pg_reload_conf()",
            "pg_rotate_logfile()", "pg_advisory_lock(1)", "pg_advisory_xact_lock(1)", "pg_try_advisory_lock(1)",
            "pg_advisory_unlock_all()", "txid_current()", "txid_current_snapshot()", "nextval('x')", "setval('x', 1)",
            "currval('x')", "lastval()", "version()", "current_database()", "current_catalog()", "current_schema()",
            "current_schemas(true)", "inet_server_addr()", "inet_server_port()", "inet_client_addr()",
            "to_regclass('x')", "to_regproc('x')", "to_regnamespace('x')", "format('%1000000000s', 'x')",
            "repeat('x', 1000000000)", "lpad('x', 1000000000)", "rpad('x', 1000000000)", "array_fill(1, ARRAY[1000000000])",
            "crypt('x', gen_salt('bf', 31))", "gen_salt('bf', 31)", "current_query()", "has_table_privilege('x', 'select')",
            "has_database_privilege('x', 'connect')", "has_any_column_privilege('x', 'select')", "row_security_active('x')",
            "obj_description(1)", "col_description(1, 1)", "shobj_description(1, 'pg_database')",
            "pg_get_functiondef(1)", "pg_get_viewdef(1)", "pg_backend_pid()", "pg_postmaster_start_time()",
            "pg_catalog.pg_sleep(1)", "pg_catalog.set_config('a', 'b', false)", "pg_read_file('x') || ''",
            "get_raw_page('x', 0)", "heap_page_items('x')", "bt_page_stats('x', 1)", "pgstattuple('x')",
            "pgp_pub_decrypt('x', 'y')", "binary_upgrade_set_next_pg_enum_oid(1)", "gin_clean_pending_list('x')",
            "brin_summarize_new_values('x')", "hash_page_stats('x', 1)", "pgrowlocks('x')"
        ];

        private static readonly string[] functionSpellings =
        [
            "SELECT {0}", "SELECT * FROM {0}", "SELECT * FROM ROWS FROM({0})", "SELECT * FROM (SELECT {0}) s",
            "SELECT 1 WHERE {0} IS NOT NULL", "SELECT (SELECT {0})", "SELECT 1 ORDER BY {0}", "SELECT 1 GROUP BY {0}",
            "SELECT 1 LIMIT ({0})::int", "SELECT ARRAY[{0}]", "SELECT coalesce(({0})::text, 'x')",
            "WITH c AS (SELECT {0}) SELECT * FROM c", "WITH c AS MATERIALIZED (SELECT {0}) SELECT * FROM c",
            "WITH RECURSIVE c AS (SELECT {0}) SELECT * FROM c", "SELECT * FROM generate_series(1,1) g, LATERAL (SELECT {0}) l",
            "SELECT 1 UNION ALL SELECT {0}", "SELECT CASE WHEN {0} IS NULL THEN 1 ELSE 2 END",
            "SELECT 1 FROM generate_series(1,1) g WHERE EXISTS (SELECT {0})", "SELECT x FROM (VALUES ({0})) v(x)",
            "SELECT {0} AS \"pg_sleep\"", "SELECT {0} OVER ()", "SELECT /* c */ {0} -- t"
        ];

        private static readonly string[] valueFunctions =
        [
            "current_user", "session_user", "user", "current_role", "current_catalog", "current_schema"
        ];

        private static readonly string[] protectedTables =
        [
            "UserRegistry", "UserRegistryVersion", "UserRegistryApiKey", "UserLogin", "UserLogout", "UserInTenant",
            "TenantRegistry", "RoleRegistry", "RoleRegistryPermission", "EntityAnalysisModelHttpAdaptation",
            "ApplicationLogEntry", "CacheSanction", "WafAttack"
        ];

        private static readonly string[] tableSpellings =
        [
            "SELECT * FROM \"{0}\"", "SELECT * FROM public.\"{0}\"", "SELECT * FROM somedb.public.\"{0}\"",
            "SELECT * FROM \"{1}\"", "SELECT * FROM public.\"{1}\"", "SELECT * FROM \"{2}\"", "SELECT * FROM ONLY \"{0}\"",
            "SELECT * FROM \"{0}\" *", "SELECT * FROM \"{0}\" AS a", "SELECT * FROM \"{0}\" a (x, y)",
            "SELECT a.* FROM \"{0}\" a JOIN \"Case\" c ON true", "SELECT * FROM \"Case\" c LEFT JOIN \"{0}\" a ON true",
            "SELECT * FROM \"Case\" c, \"{0}\" a", "SELECT * FROM \"Case\" c CROSS JOIN \"{0}\"",
            "SELECT * FROM \"Case\" NATURAL JOIN \"{0}\"", "TABLE \"{0}\"", "TABLE ONLY \"{0}\"",
            "SELECT * FROM (SELECT * FROM \"{0}\") s", "SELECT * FROM (TABLE \"{0}\") s",
            "SELECT * FROM generate_series(1,1) g, LATERAL (SELECT * FROM \"{0}\") l",
            "SELECT 1 UNION SELECT 1 FROM \"{0}\"", "SELECT 1 UNION ALL SELECT 1 FROM \"{0}\"",
            "SELECT 1 INTERSECT SELECT 1 FROM \"{0}\"", "SELECT 1 EXCEPT SELECT 1 FROM \"{0}\"",
            "(SELECT 1) UNION (SELECT 1 FROM \"{0}\")", "SELECT 1 WHERE EXISTS (SELECT 1 FROM \"{0}\")",
            "SELECT 1 WHERE 1 IN (SELECT 1 FROM \"{0}\")", "SELECT 1 WHERE 1 = ANY (SELECT 1 FROM \"{0}\")",
            "SELECT (SELECT 1 FROM \"{0}\" LIMIT 1)", "SELECT ARRAY(SELECT 1 FROM \"{0}\")",
            "SELECT 1 ORDER BY (SELECT 1 FROM \"{0}\" LIMIT 1)", "SELECT 1 GROUP BY (SELECT 1 FROM \"{0}\" LIMIT 1)",
            "SELECT 1 HAVING (SELECT 1 FROM \"{0}\" LIMIT 1) = 1", "SELECT count(*) FILTER (WHERE EXISTS (SELECT 1 FROM \"{0}\")) FROM \"Case\"",
            "SELECT sum(1) OVER (PARTITION BY (SELECT 1 FROM \"{0}\" LIMIT 1)) FROM \"Case\"",
            "SELECT * FROM (VALUES ((SELECT 1 FROM \"{0}\" LIMIT 1))) v(x)",
            "WITH c AS (SELECT * FROM \"{0}\") SELECT * FROM c", "WITH c AS MATERIALIZED (SELECT * FROM \"{0}\") SELECT * FROM c",
            "WITH c AS NOT MATERIALIZED (SELECT * FROM \"{0}\") SELECT * FROM c",
            "WITH RECURSIVE c AS (SELECT * FROM \"{0}\") SELECT * FROM c",
            "WITH a AS (SELECT 1), c AS (SELECT * FROM \"{0}\") SELECT * FROM a, c",
            "WITH a AS (WITH b AS (SELECT * FROM \"{0}\") SELECT * FROM b) SELECT * FROM a",
            "SELECT * FROM (WITH b AS (SELECT * FROM \"{0}\") SELECT * FROM b) s",
            "SELECT 1 WHERE EXISTS (WITH b AS (SELECT * FROM \"{0}\") SELECT * FROM b)",
            "WITH \"{0}\" AS (SELECT 1) SELECT * FROM public.\"{0}\"",
            "WITH \"{0}\" AS (SELECT 1) SELECT * FROM somedb.public.\"{0}\"",
            "WITH RECURSIVE \"{0}\" AS (SELECT 1) SELECT * FROM public.\"{0}\"",
            "WITH \"{0}\" AS MATERIALIZED (SELECT 1) SELECT * FROM public.\"{0}\"",
            "WITH \"{1}\" AS (SELECT 1) SELECT * FROM \"{0}\"", "WITH \"{2}\" AS (SELECT 1) SELECT * FROM \"{0}\"",
            "WITH a AS (SELECT 1), \"{1}\" AS (SELECT 1) SELECT * FROM \"{0}\"",
            "WITH a AS (SELECT * FROM \"{0}\"), \"{0}\" AS (SELECT 1) SELECT * FROM a",
            "SELECT * FROM (WITH \"{1}\" AS (SELECT 1) SELECT 1) s, \"{0}\"",
            "SELECT * FROM \"{0}\" TABLESAMPLE SYSTEM (100)",
            "SELECT * FROM \"{0}\" /* c */", "SELECT * FROM/**/\"{0}\"", "SELECT * FROM -- c\n\"{0}\"",
            "SELECT * FROM U&\"{4}\""
        ];

        private static readonly string[] catalogueSpellings =
        [
            "SELECT * FROM pg_class", "SELECT * FROM PG_CLASS", "SELECT * FROM \"pg_class\"", "SELECT * FROM pg_catalog.pg_class",
            "SELECT * FROM PG_CATALOG.pg_class", "SELECT * FROM pg_catalog.\"pg_class\"", "SELECT * FROM \"pg_catalog\".\"pg_class\"",
            "SELECT * FROM somedb.pg_catalog.pg_class", "SELECT * FROM information_schema.tables",
            "SELECT * FROM INFORMATION_SCHEMA.TABLES", "SELECT * FROM information_schema.columns",
            "SELECT * FROM information_schema.role_table_grants", "SELECT * FROM information_schema.routines",
            "SELECT * FROM pg_toast.pg_toast_1", "SELECT * FROM pg_temp.x", "SELECT * FROM pg_temp_1.x",
            "SELECT * FROM pg_shadow", "SELECT * FROM pg_authid", "SELECT * FROM pg_user", "SELECT * FROM pg_roles",
            "SELECT * FROM pg_stat_activity", "SELECT * FROM pg_settings", "SELECT * FROM pg_file_settings",
            "SELECT * FROM pg_hba_file_rules", "SELECT * FROM pg_largeobject", "SELECT * FROM pg_proc",
            "SELECT * FROM pg_namespace", "SELECT * FROM pg_database", "SELECT * FROM pg_stat_replication",
            "SELECT * FROM pg_tables", "SELECT * FROM pg_views", "SELECT * FROM pg_indexes", "SELECT * FROM pg_sequences",
            "TABLE pg_shadow", "SELECT * FROM (SELECT * FROM pg_shadow) s", "WITH c AS (SELECT * FROM pg_shadow) SELECT * FROM c",
            "WITH pg_shadow AS (SELECT 1) SELECT * FROM pg_catalog.pg_shadow",
            "WITH pg_shadow AS (SELECT 1) SELECT * FROM information_schema.pg_shadow",
            "SELECT 1 UNION SELECT usename::int FROM pg_shadow", "SELECT * FROM pg_stat_statements"
        ];

        private static readonly string[] rejectedByRewrite =
        [
            "SELECT * FROM \"Secret\"", "SELECT * FROM public.\"Secret\"", "SELECT * FROM somedb.public.\"Secret\"",
            "SELECT * FROM other.\"Secret\"", "SELECT * FROM other.\"Archive\" x JOIN other.\"Secret\" y ON true",
            "TABLE \"Secret\"", "SELECT * FROM ONLY \"Secret\"", "SELECT * FROM \"Archive\" a, \"Secret\" s",
            "SELECT * FROM (SELECT * FROM \"Secret\") s", "SELECT * FROM \"Archive\" UNION SELECT * FROM \"Secret\"",
            "SELECT * FROM generate_series(1,1) g, LATERAL (SELECT * FROM \"Secret\") l",
            "SELECT 1 WHERE EXISTS (SELECT 1 FROM \"Secret\")", "SELECT (SELECT 1 FROM \"Secret\" LIMIT 1)",
            "SELECT 1 WHERE 1 IN (SELECT 1 FROM \"Secret\")", "SELECT ARRAY(SELECT 1 FROM \"Secret\")",
            "SELECT 1 ORDER BY (SELECT 1 FROM \"Secret\" LIMIT 1)",
            "WITH c AS (SELECT * FROM \"Secret\") SELECT * FROM c",
            "WITH c AS MATERIALIZED (SELECT * FROM \"Secret\") SELECT * FROM c",
            "WITH RECURSIVE c AS (SELECT * FROM \"Secret\") SELECT * FROM c",
            "WITH a AS (WITH b AS (SELECT * FROM \"Secret\") SELECT * FROM b) SELECT * FROM a",
            "WITH \"Secret\" AS (SELECT 1) SELECT * FROM public.\"Secret\"",
            "WITH \"Secret\" AS (SELECT 1) SELECT * FROM somedb.public.\"Secret\"",
            "WITH RECURSIVE \"Secret\" AS (SELECT 1) SELECT * FROM public.\"Secret\"",
            "WITH \"secret\" AS (SELECT 1) SELECT * FROM \"Secret\"", "WITH \"SECRET\" AS (SELECT 1) SELECT * FROM \"Secret\"",
            "WITH secret AS (SELECT 1) SELECT * FROM \"Secret\"", "WITH a AS (SELECT 1), \"secret\" AS (SELECT 1) SELECT * FROM \"Secret\"",
            "WITH a AS (SELECT * FROM \"Secret\"), \"Secret\" AS (SELECT 1) SELECT * FROM a",
            "SELECT * FROM (WITH \"secret\" AS (SELECT 1) SELECT 1) s, \"Secret\"",
            "WITH \"Archive\" AS (SELECT 1) SELECT * FROM \"Archive\"", "WITH \"ARCHIVE\" AS (SELECT 1) SELECT * FROM \"Archive\"",
            "WITH archive AS (SELECT 1) SELECT * FROM archive", "WITH \"Reference\" AS (SELECT 1) SELECT * FROM \"Reference\"",
            "WITH \"Archive\" AS (SELECT 1), \"Reference\" AS (SELECT 1) SELECT 1",
            "SELECT * FROM pg_class", "SELECT * FROM pg_catalog.pg_class", "SELECT * FROM information_schema.tables",
            "WITH pg_class AS (SELECT 1) SELECT * FROM pg_catalog.pg_class",
            "SELECT * FROM \"Archive\", \"UserRegistry\"", "SELECT * FROM \"Archive\" WHERE \"Id\" IN (SELECT \"Id\" FROM \"TenantRegistry\")",
            "SELECT * FROM \"Archive\" TABLESAMPLE SYSTEM (10), \"Secret\"", "SELECT * FROM \"Secret\" TABLESAMPLE SYSTEM (10)",
            "SELECT * FROM ROWS FROM(generate_series(1,2)), \"Secret\"", "SELECT * FROM xmltable('/a' PASSING (SELECT x FROM \"Secret\") COLUMNS a int)",
            "SELECT * FROM \"Archive\" FOR UPDATE", "SELECT * FROM \"Secret\"; SELECT 1", "SELECT * FROM \"Secret\" -- c\n",
            "SELECT * FROM/**/\"Secret\"", "SELECT * FROM U&\"\\0053ecret\"", "SELECT * FROM \"Secret\" AS \"Archive\"",
            "SELECT * FROM \"Archive\" AS \"Secret\", \"Secret\"",
            "SELECT * FROM (SELECT * FROM \"Archive\") \"Secret\", \"Secret\"",
            "SELECT * FROM generate_series(1,1) AS \"Secret\", \"Secret\""
        ];

        private static readonly string[] stillRewritten =
        [
            "SELECT * FROM \"Archive\"", "SELECT * FROM public.\"Archive\"", "SELECT * FROM \"ARCHIVE\"",
            "SELECT * FROM ONLY \"Archive\"", "TABLE \"Archive\"", "SELECT * FROM \"Archive\" a JOIN \"Reference\" r ON true",
            "SELECT * FROM \"Archive\" AS \"UserInTenant\"", "SELECT * FROM \"Archive\" AS \"Archive\"",
            "SELECT * FROM \"Archive\" AS uit, \"Archive\" AS t0", "SELECT * FROM \"Archive\" a (x, y)",
            "SELECT * FROM (SELECT * FROM \"Archive\") s", "SELECT * FROM \"Archive\" UNION ALL SELECT * FROM \"Archive\"",
            "WITH \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "WITH \"userintenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "WITH RECURSIVE \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "WITH \"UserInTenant\" AS MATERIALIZED (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "WITH \"UserInTenant\" AS NOT MATERIALIZED (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "WITH a AS (SELECT 1), \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\"",
            "SELECT * FROM (WITH \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\") s",
            "WITH a AS (WITH \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\") SELECT * FROM a",
            "SELECT 1 WHERE EXISTS (WITH \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM \"Archive\")",
            "WITH \"UserInTenant\" AS (SELECT 'u'::text AS \"User\") SELECT * FROM generate_series(1,1) g, LATERAL (SELECT * FROM \"Archive\") l",
            "WITH c AS (SELECT * FROM \"Archive\") SELECT * FROM c", "WITH pg_sleep AS (SELECT 1) SELECT * FROM pg_sleep",
            "WITH a AS (SELECT 1), a2 AS (SELECT * FROM a) SELECT * FROM a2",
            "WITH a AS (SELECT 1) SELECT * FROM (WITH a AS (SELECT 2) SELECT * FROM a) s",
            "SELECT $$ ; DROP TABLE \"Secret\" $$ FROM \"Archive\"", "SELECT '-- ' FROM \"Archive\"", "SELECT '/*' FROM \"Archive\" /* */"
        ];

        public static IEnumerable<object[]> StatementCases()
        {
            return statements.Select(sql => new object[] { sql });
        }

        public static IEnumerable<object[]> FunctionCases()
        {
            foreach (var function in deniedFunctions)
            {
                var name = function[..function.IndexOf('(')];
                var shapes = new List<string> { function, function.ToUpperInvariant() };

                shapes.Add(name.StartsWith("pg_", StringComparison.Ordinal)
                    ? "pg_catalog." + function
                    : "public." + function);

                shapes.Add("\"" + name + "\"" + function[name.Length..]);

                foreach (var shape in shapes)
                {
                    foreach (var spelling in functionSpellings)
                    {
                        yield return [string.Format(spelling, shape)];
                    }
                }
            }

            foreach (var spelling in functionSpellings)
            {
                yield return [string.Format(spelling, "U&\"\\0070g_sleep\"(1)")];
                yield return [string.Format(spelling, "U&\"\\0073et_config\"('a', 'b', false)")];
                yield return [string.Format(spelling, "\"pg_sleep\"(1)")];
                yield return [string.Format(spelling, "pg_catalog.\"pg_sleep\"(1)")];
                yield return [string.Format(spelling, "pg_sleep/**/(1)")];
            }
        }

        public static IEnumerable<object[]> ValueFunctionCases()
        {
            foreach (var value in valueFunctions)
            {
                foreach (var spelling in functionSpellings.Where(s => !s.Contains("ROWS FROM") && !s.Contains("OVER")
                             && !s.Contains("AS \"pg_sleep\"") && !s.StartsWith("SELECT * FROM {0}")))
                {
                    yield return [string.Format(spelling, value)];
                    yield return [string.Format(spelling, value.ToUpperInvariant())];
                }
            }
        }

        public static IEnumerable<object[]> ProtectedTableCases()
        {
            foreach (var table in protectedTables)
            {
                foreach (var spelling in tableSpellings)
                {
                    yield return
                    [
                        string.Format(spelling, table, table.ToLowerInvariant(), table.ToUpperInvariant(),
                            string.Empty, string.Concat(table.Select(c => $"\\{(int)c:x4}")))
                    ];
                }
            }
        }

        public static IEnumerable<object[]> CatalogueCases()
        {
            return catalogueSpellings.Select(sql => new object[] { sql });
        }

        public static IEnumerable<object[]> RejectedByRewriteCases()
        {
            return rejectedByRewrite.Select(sql => new object[] { sql });
        }

        public static IEnumerable<object[]> StillRewrittenCases()
        {
            return stillRewritten.Select(sql => new object[] { sql });
        }

        [Theory]
        [MemberData(nameof(StatementCases))]
        public void EveryNonSelectStatementAndEveryStackedOrMalformedSpellingIsRefused(string sql)
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(FunctionCases))]
        public void EveryDeniedFunctionInEverySpellingAndPositionIsRefused(string sql)
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(FunctionCases))]
        public void EveryDeniedFunctionIsAlsoRefusedOnTheRewritePath(string sql)
        {
            var act = () => PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(ValueFunctionCases))]
        public void EveryIdentityRevealingValueFunctionIsRefused(string sql)
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(ProtectedTableCases))]
        public void EveryProtectedTableInEveryPositionAndSpellingIsRefusedInDenyMode(string sql)
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(ProtectedTableCases))]
        public void EveryProtectedTableInEveryPositionAndSpellingIsRefusedByTheAllowList(string sql)
        {
            var act = () => PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(CatalogueCases))]
        public void CatalogueAndInformationSchemaReachIsRefusedInBothModes(string sql)
        {
            var deny = () => PostgresSqlValidator.AssertSelectOnly(sql, true);
            var plain = () => PostgresSqlValidator.AssertSelectOnly(sql);
            var rewrite = () => PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            deny.Should().Throw<InvalidOperationException>(sql);
            plain.Should().Throw<InvalidOperationException>(sql);
            rewrite.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(RejectedByRewriteCases))]
        public void TheAllowListRefusesEveryUnregisteredReachWhateverTheShadowingOrQualification(string sql)
        {
            var act = () => PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            act.Should().Throw<InvalidOperationException>(sql);
        }

        [Theory]
        [MemberData(nameof(StillRewrittenCases))]
        public void LegitimateStatementsRewriteAndKeepTheTenancyCheckOfEveryTemplate(string sql)
        {
            var rewritten = PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            if (sql.Contains("\"Archive\"", StringComparison.OrdinalIgnoreCase))
            {
                rewritten.Should().Contain("public.\"UserInTenant\"",
                    "the template's own tenancy relation is qualified so no expression can capture it");
            }

            rewritten.Should().NotContain("FROM \"UserInTenant\"");
        }

        [Fact]
        public void TheCollectedReferencesOfEveryRejectedShadowIncludeTheRealTable()
        {
            foreach (var sql in rejectedByRewrite.Where(s => s.Contains("WITH ", StringComparison.Ordinal)
                                                              && s.Contains("\"Secret\"", StringComparison.Ordinal)
                                                              && !s.Contains("\"Archive\"", StringComparison.Ordinal)))
            {
                PostgresSqlValidator.CollectTableReferences(sql).Should().Contain("Secret", sql);
            }
        }

        [Theory]
        [InlineData("SELECT $$ ; DROP TABLE x; $$")]
        [InlineData("SELECT $tag$ ; -- /* $$ $tag$")]
        [InlineData("SELECT 'a;b'")]
        [InlineData("SELECT 'it''s; fine'")]
        [InlineData("SELECT E'\\'; still a string'")]
        [InlineData("SELECT U&'\\0041;'")]
        [InlineData("SELECT 1 /* ; DROP TABLE x */")]
        [InlineData("SELECT 1 -- ; DROP TABLE x")]
        [InlineData("SELECT /* a */ 1 /* b */ FROM /* c */ \"Case\" /* d */")]
        [InlineData("SELECT 1 /* nested /* comment */ still comment */")]
        [InlineData("SELECT 1;")]
        [InlineData("VALUES (1), (2)")]
        [InlineData("TABLE \"Case\"")]
        [InlineData("SELECT \"a;b\" FROM \"Case\"")]
        [InlineData("SELECT 1 AS \"--\"")]
        public void CommentsQuotesAndDollarStringsThatHideSemicolonsAreNotStackedStatements(string sql)
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().NotThrow(sql);
        }

        [Fact]
        public void ASqlStatementOverTheLengthLimitIsRefused()
        {
            var sql = "SELECT " + string.Join(",", Enumerable.Repeat("1", 40000));

            var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ANulCharacterIsRefused()
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly("SELECT 1\0; DROP TABLE x", true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(65)]
        [InlineData(500)]
        [InlineData(5000)]
        public void DeeplyParenthesisedSqlIsRefusedWithoutExhaustingTheStack(int depth)
        {
            var sql = "SELECT " + new string('(', depth) + "1" + new string(')', depth);

            var act = () => PostgresSqlValidator.RewriteTableReferences(sql, allowed);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(5000)]
        [InlineData(9000)]
        public void LongOperatorAndSetOperationChainsAreRefusedBeforeTheNativeParserRecurses(int terms)
        {
            var arithmetic = "SELECT " + string.Join("+", Enumerable.Repeat("1", terms));
            var unions = string.Join(" UNION ALL ", Enumerable.Repeat("SELECT 1", terms));

            foreach (var sql in new[] { arithmetic, unions })
            {
                var act = () => PostgresSqlValidator.AssertSelectOnly(sql, true);

                act.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [InlineData(200)]
        [InlineData(1500)]
        public void ModerateChainsEitherRewriteOrAreRefusedButNeverCrash(int terms)
        {
            var arithmetic = "SELECT " + string.Join("+", Enumerable.Repeat("1", terms));
            var unions = string.Join(" UNION ALL ", Enumerable.Repeat("SELECT 1", terms));
            var ors = "SELECT 1 FROM \"Archive\" WHERE " + string.Join(" OR ", Enumerable.Repeat("1=1", terms));

            foreach (var sql in new[] { arithmetic, unions, ors })
            {
                var thrown = Record.Exception(() => PostgresSqlValidator.RewriteTableReferences(sql, allowed));

                if (thrown != null)
                {
                    thrown.Should().BeOfType<InvalidOperationException>();
                }
            }
        }

        [Theory]
        [InlineData("SELECT $1")]
        [InlineData("SELECT $12")]
        [InlineData("SELECT '$1'")]
        [InlineData("SELECT \"$1\" FROM \"Archive\"")]
        [InlineData("SELECT @a, $1")]
        [InlineData("SELECT /* $2 */ 1")]
        [InlineData("SELECT 1 -- $3")]
        public void PositionalParametersAreRefusedSoTheNamedParameterSwapCannotBeSmuggledInto(string sql)
        {
            var act = () => PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql, allowed);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void NamedParametersAreRestoredExactlyEvenWhenInsideStringsAndMoreThanNineAreUsed()
        {
            var names = Enumerable.Range(1, 12).Select(i => "@P" + i).ToArray();
            var sql = "SELECT * FROM \"Archive\" WHERE \"A\" IN (" + string.Join(",", names) + ") AND \"B\" = 'x@P1y'";

            var rewritten = PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql, allowed);

            foreach (var name in names)
            {
                rewritten.Should().Contain(name);
            }

            rewritten.Should().Contain("'x@P1y'").And.NotContain("$1");
        }

        [Fact]
        public void ANamedParameterCannotNameAFunctionOrRelationToEscapeTheGate()
        {
            var act = () => PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(
                "SELECT @x FROM @y", allowed);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TheCorpusReallyCoversEveryDeniedFunctionPrefixAndName()
        {
            deniedFunctions.Length.Should().BeGreaterThan(80);
            statements.Length.Should().BeGreaterThan(90);
            FunctionCases().Count().Should().BeGreaterThan(2000);
            ProtectedTableCases().Count().Should().BeGreaterThan(700);
            valueFunctions.Length.Should().Be(6);
        }
    }
}

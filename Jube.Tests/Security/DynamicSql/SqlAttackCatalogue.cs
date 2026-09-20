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
using System.Text;

namespace Jube.Test.Security.DynamicSql;

internal static class SqlAttackCatalogue
{
    private const string Mine = "'ZzTestW12%'";

    public static readonly (string Kind, string Sql)[] WriteSeeds =
    [
        ("INSERT", "INSERT INTO \"VisualisationRegistry\" (\"Name\") VALUES ('ZzW12Inserted')"),
        ("INSERT-SELECT", "INSERT INTO \"VisualisationRegistry\" (\"Name\") SELECT 'ZzW12Inserted'"),
        ("UPDATE", $"UPDATE \"VisualisationRegistry\" SET \"Name\" = 'ZzW12Pwned' WHERE \"Name\" LIKE {Mine}"),
        ("DELETE", $"DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine}"),
        ("MERGE",
            $"MERGE INTO \"VisualisationRegistry\" t USING (SELECT 1 AS x) s ON t.\"Name\" LIKE {Mine} WHEN MATCHED THEN DELETE"),
        ("TRUNCATE", "TRUNCATE TABLE \"ZzW12NoSuchTable\""),
        ("DROP-TABLE", "DROP TABLE \"ZzW12NoSuchTable\""),
        ("DROP-TABLE-IF", "DROP TABLE IF EXISTS \"ZzW12NoSuchTable\" CASCADE"),
        ("DROP-SCHEMA", "DROP SCHEMA IF EXISTS zzw12noschema CASCADE"),
        ("ALTER-TABLE", "ALTER TABLE \"ZzW12NoSuchTable\" ADD COLUMN x int"),
        ("ALTER-SYSTEM", "ALTER SYSTEM SET work_mem = '64MB'"),
        ("CREATE-TABLE", "CREATE TABLE zzw12new (a int)"),
        ("CREATE-TABLE-AS", "CREATE TABLE zzw12copy AS SELECT 1 AS a"),
        ("CREATE-TEMP", "CREATE TEMP TABLE zzw12temp (a int)"),
        ("CREATE-VIEW", "CREATE VIEW zzw12view AS SELECT 1"),
        ("CREATE-FUNCTION", "CREATE FUNCTION zzw12fn() RETURNS int LANGUAGE sql AS 'select 1'"),
        ("CREATE-ROLE", "CREATE ROLE zzw12role SUPERUSER"),
        ("CREATE-USER", "CREATE USER zzw12user PASSWORD 'x'"),
        ("CREATE-EXTENSION", "CREATE EXTENSION IF NOT EXISTS dblink"),
        ("CREATE-INDEX", "CREATE INDEX zzw12idx ON \"ZzW12NoSuchTable\" (a)"),
        ("CREATE-SCHEMA", "CREATE SCHEMA zzw12schema"),
        ("CREATE-TRIGGER", "CREATE TRIGGER zzw12trg BEFORE INSERT ON \"ZzW12NoSuchTable\" EXECUTE FUNCTION zzw12fn()"),
        ("GRANT", "GRANT ALL ON \"ZzW12NoSuchTable\" TO zzw12nobody"),
        ("GRANT-ROLE", "GRANT zzw12nobody TO zzw12other"),
        ("REVOKE", "REVOKE ALL ON \"ZzW12NoSuchTable\" FROM zzw12nobody"),
        ("COPY-TO-FILE", "COPY (SELECT 1) TO '/tmp/zzw12_copy.csv'"),
        ("COPY-FROM-FILE", "COPY \"ZzW12NoSuchTable\" FROM '/etc/passwd'"),
        ("COPY-FROM-PROGRAM", "COPY \"ZzW12NoSuchTable\" FROM PROGRAM 'touch /tmp/zzw12_pwned'"),
        ("COPY-TO-PROGRAM", "COPY (SELECT 1) TO PROGRAM 'touch /tmp/zzw12_pwned'"),
        ("COPY-STDOUT", "COPY (SELECT 1) TO STDOUT"),
        ("CALL", "CALL zzw12_proc()"),
        ("DO", "DO $$ BEGIN PERFORM 1; END $$"),
        ("DO-TAGGED", "DO $zz$ BEGIN PERFORM pg_sleep(1); END $zz$"),
        ("SELECT-INTO", "SELECT * INTO zzw12copy FROM \"VisualisationRegistry\""),
        ("SELECT-INTO-TEMP", "SELECT 1 AS a INTO TEMP zzw12temp"),
        ("SELECT-INTO-UNLOGGED", "SELECT 1 AS a INTO UNLOGGED zzw12unlogged"),
        ("SELECT-FOR-UPDATE", "SELECT * FROM \"VisualisationRegistry\" FOR UPDATE"),
        ("SELECT-FOR-SHARE", "SELECT * FROM \"VisualisationRegistry\" FOR SHARE NOWAIT"),
        ("CTE-DELETE",
            $"WITH x AS (DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING *) SELECT * FROM x"),
        ("CTE-UPDATE",
            $"WITH x AS (UPDATE \"VisualisationRegistry\" SET \"Name\" = 'ZzW12Pwned' WHERE \"Name\" LIKE {Mine} RETURNING *) SELECT * FROM x"),
        ("CTE-INSERT",
            "WITH x AS (INSERT INTO \"VisualisationRegistry\" (\"Name\") VALUES ('ZzW12Inserted') RETURNING *) SELECT * FROM x"),
        ("CTE-NESTED-DELETE",
            $"WITH a AS (SELECT 1), b AS (DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING 1) SELECT * FROM a, b"),
        ("CTE-IN-SUBQUERY",
            $"SELECT * FROM (WITH x AS (DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING 1) SELECT * FROM x) s"),
        ("CTE-IN-UNION",
            $"SELECT 1 UNION ALL (WITH x AS (DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING 1) SELECT * FROM x)"),
        ("CTE-IN-WHERE",
            $"SELECT 1 WHERE EXISTS (WITH x AS (DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING 1) SELECT * FROM x)"),
        ("STACKED-DELETE", $"SELECT 1; DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine}"),
        ("STACKED-DROP", "SELECT 1; DROP TABLE \"ZzW12NoSuchTable\""),
        ("STACKED-SELECTS", "SELECT 1; SELECT 2"),
        ("STACKED-TRAILING", "SELECT 1;;; DROP TABLE \"ZzW12NoSuchTable\";"),
        ("STACKED-LEADING", $"DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine}; SELECT 1"),
        ("SET", "SET search_path TO zzw12noschema"),
        ("SET-LOCAL", "SET LOCAL statement_timeout = 0"),
        ("SET-ROLE", "SET ROLE zzw12role"),
        ("SET-SESSION-AUTH", "SET SESSION AUTHORIZATION zzw12role"),
        ("RESET", "RESET ALL"),
        ("SET-CONFIG", "SELECT set_config('search_path', 'zzw12noschema', false)"),
        ("SET-CONFIG-LOCAL", "SELECT set_config('statement_timeout', '0', true)"),
        ("SET-CONFIG-READONLY", "SELECT set_config('default_transaction_read_only', 'off', false)"),
        ("SHOW", "SHOW all"),
        ("COMMIT", "COMMIT"),
        ("ROLLBACK", "ROLLBACK"),
        ("BEGIN", "BEGIN"),
        ("BEGIN-RW", "BEGIN READ WRITE"),
        ("START-TRANSACTION", "START TRANSACTION"),
        ("END", "END"),
        ("SAVEPOINT", "SAVEPOINT zzw12"),
        ("RELEASE", "RELEASE SAVEPOINT zzw12"),
        ("SET-TRANSACTION", "SET TRANSACTION READ WRITE"),
        ("EXPLAIN-ANALYZE-DELETE", $"EXPLAIN ANALYZE DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine}"),
        ("EXPLAIN-ANALYZE-SELECT", "EXPLAIN ANALYZE SELECT 1"),
        ("EXPLAIN", "EXPLAIN SELECT 1"),
        ("LISTEN", "LISTEN zzw12"),
        ("NOTIFY", "NOTIFY zzw12, 'x'"),
        ("UNLISTEN", "UNLISTEN *"),
        ("VACUUM", "VACUUM"),
        ("VACUUM-FULL", "VACUUM FULL \"VisualisationRegistry\""),
        ("ANALYZE", "ANALYZE"),
        ("LOCK", "LOCK TABLE \"VisualisationRegistry\" IN ACCESS EXCLUSIVE MODE"),
        ("PREPARE", "PREPARE zzw12 AS SELECT 1"),
        ("PREPARE-DELETE", $"PREPARE zzw12 AS DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine}"),
        ("EXECUTE", "EXECUTE zzw12"),
        ("DEALLOCATE", "DEALLOCATE ALL"),
        ("DECLARE", "DECLARE zzw12 CURSOR FOR SELECT 1"),
        ("FETCH", "FETCH ALL FROM zzw12"),
        ("CLOSE", "CLOSE ALL"),
        ("DISCARD", "DISCARD ALL"),
        ("CHECKPOINT", "CHECKPOINT"),
        ("REINDEX", "REINDEX DATABASE postgres"),
        ("CLUSTER", "CLUSTER"),
        ("REFRESH", "REFRESH MATERIALIZED VIEW zzw12mv"),
        ("COMMENT", "COMMENT ON TABLE \"ZzW12NoSuchTable\" IS 'x'"),
        ("SECURITY-LABEL", "SECURITY LABEL ON TABLE \"ZzW12NoSuchTable\" IS 'x'"),
        ("REASSIGN", "REASSIGN OWNED BY zzw12nobody TO zzw12other"),
        ("DROP-OWNED", "DROP OWNED BY zzw12nobody"),
        ("IMPORT-FOREIGN", "IMPORT FOREIGN SCHEMA zzw12 FROM SERVER zzw12srv INTO public"),
        ("PREPARE-TRANSACTION", "PREPARE TRANSACTION 'zzw12'"),
        ("LOAD", "LOAD 'zzw12'"),
        ("TABLE-UNLOGGED", "ALTER TABLE \"ZzW12NoSuchTable\" SET UNLOGGED"),
        ("UPDATE-RETURNING",
            $"UPDATE \"VisualisationRegistry\" SET \"Name\" = 'ZzW12Pwned' WHERE \"Name\" LIKE {Mine} RETURNING *"),
        ("DELETE-RETURNING", $"DELETE FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE {Mine} RETURNING *")
    ];

    public static readonly string[] DangerousExpressions =
    [
        "pg_sleep(30)", "pg_sleep_for('30 seconds')", "pg_sleep_until(now() + interval '30 seconds')",
        "pg_catalog.pg_sleep(30)", "PG_SLEEP(30)", "pg_sleep((30))", "coalesce(pg_sleep(30)::text, 'x')",
        "pg_terminate_backend(0)", "pg_cancel_backend(0)", "pg_terminate_backend(pg_backend_pid())",
        "lo_import('/etc/passwd')", "lo_export(0, '/tmp/zzw12_lo')", "lo_from_bytea(0, 'x'::bytea)", "lo_unlink(0)",
        "lo_create(0)", "lowrite(0, 'x'::bytea)", "loread(0, 1)",
        "dblink('host=127.0.0.1', 'select 1')", "dblink_exec('host=127.0.0.1', 'select 1')",
        "dblink_connect('zzw12', 'host=127.0.0.1')",
        "pg_read_file('/etc/passwd')", "pg_read_binary_file('/etc/passwd')", "pg_ls_dir('/')", "pg_stat_file('/etc')",
        "pg_ls_logdir()", "pg_ls_waldir()", "pg_ls_tmpdir()",
        "nextval('zzw12seq')", "setval('zzw12seq', 1)", "currval('zzw12seq')", "lastval()",
        "pg_advisory_lock(1)", "pg_advisory_lock_shared(1)", "pg_try_advisory_lock(1)", "pg_advisory_xact_lock(1)",
        "pg_advisory_unlock_all()", "pg_reload_conf()", "pg_rotate_logfile()", "pg_stat_reset()",
        "pg_stat_reset_shared('bgwriter')", "pg_switch_wal()", "pg_create_restore_point('zzw12')",
        "pg_notify('zzw12', 'x')", "pg_backend_pid()", "pg_current_wal_lsn()", "pg_relation_filepath('pg_class')",
        "pg_get_viewdef('pg_class'::regclass)", "pg_get_functiondef(0)", "pg_table_size('pg_class')",
        "txid_current()", "txid_current_snapshot()", "txid_snapshot_xmin(txid_current_snapshot())",
        "set_config('search_path', 'zzw12', false)", "set_config('role', 'postgres', false)",
        "current_setting('data_directory')", "current_setting('server_version')",
        "query_to_xml('select * from \"UserRegistry\"', true, false, '')",
        "table_to_xml('\"UserRegistry\"'::regclass, true, false, '')",
        "cursor_to_xml('zzw12'::refcursor, 1, true, false, '')", "database_to_xml(true, false, '')",
        "schema_to_xml('public', true, false, '')",
        "ts_stat('select to_tsvector(''x'')')",
        "get_raw_page('pg_class', 0)", "pg_read_server_files()", "pg_export_snapshot()",
        "pg_logical_emit_message(true, 'x', 'y')", "pg_drop_replication_slot('zzw12')"
    ];

    public static IEnumerable<string> ContextsFor(string expression)
    {
        yield return $"SELECT {expression}";
        yield return $"SELECT 1 AS a, {expression} AS b";
        yield return $"SELECT * FROM (SELECT {expression} AS b) s";
        yield return $"SELECT 1 WHERE {expression} IS NULL";
        yield return $"SELECT 1 WHERE ({expression})::text = 'x'";
        yield return $"SELECT * FROM {expression}";
        yield return $"SELECT * FROM generate_series(1, 1) g, LATERAL (SELECT {expression}) l";
        yield return $"WITH c AS (SELECT {expression}) SELECT * FROM c";
        yield return $"SELECT CASE WHEN true THEN {expression} ELSE NULL END";
        yield return $"SELECT 1 UNION ALL SELECT {expression}";
        yield return $"SELECT count(*) FILTER (WHERE {expression} IS NOT NULL) FROM generate_series(1, 1)";
        yield return $"SELECT (SELECT {expression})";
        yield return $"SELECT 1 ORDER BY {expression}";
        yield return $"SELECT 1 GROUP BY {expression}";
        yield return $"SELECT 1 LIMIT ({expression})::int";
        yield return $"SELECT ARRAY[{expression}]";
        yield return $"SELECT coalesce(({expression})::text, 'x')";
    }

    public static readonly string[] ProtectedReads =
    [
        "SELECT * FROM \"UserRegistry\"", "SELECT \"Password\" FROM \"UserRegistry\"",
        "SELECT \"Name\", \"Password\" FROM public.\"UserRegistry\"", "SELECT * FROM \"UserRegistryVersion\"",
        "SELECT * FROM \"UserRegistryApiKey\"", "SELECT * FROM \"UserLogin\"", "SELECT * FROM \"UserLogout\"",
        "SELECT * FROM \"UserInTenant\"",
        "SELECT * FROM \"UserInTenantSwitchLog\"", "SELECT * FROM \"TenantRegistry\"",
        "SELECT * FROM \"TenantRegistryVersion\"", "SELECT * FROM \"RoleRegistry\"",
        "SELECT * FROM \"RoleRegistryPermission\"", "SELECT * FROM \"EntityAnalysisModelHttpAdaptation\"",
        "TABLE \"UserRegistry\"", "SELECT u.* FROM \"UserRegistry\" u",
        "SELECT 1 FROM generate_series(1,1) g WHERE EXISTS (SELECT 1 FROM \"UserRegistry\")",
        "SELECT (SELECT \"Password\" FROM \"UserRegistry\" LIMIT 1)",
        "WITH u AS (SELECT * FROM \"UserRegistry\") SELECT * FROM u",
        "SELECT 1 UNION ALL SELECT length(\"Password\") FROM \"UserRegistry\"",
        "SELECT * FROM generate_series(1,1) g, LATERAL (SELECT \"Password\" FROM \"UserRegistry\") l",
        "SELECT * FROM \"VisualisationRegistry\" v JOIN \"TenantRegistry\" t ON t.\"Id\" = v.\"TenantRegistryId\"",
        "SELECT * FROM pg_shadow", "SELECT * FROM pg_user", "SELECT * FROM pg_authid", "SELECT * FROM pg_roles",
        "SELECT * FROM pg_catalog.pg_shadow", "SELECT * FROM pg_catalog.pg_tables", "SELECT * FROM pg_tables",
        "SELECT * FROM pg_stat_activity", "SELECT query FROM pg_stat_activity", "SELECT * FROM pg_settings",
        "SELECT * FROM pg_stat_statements", "SELECT * FROM pg_class", "SELECT * FROM pg_namespace",
        "SELECT * FROM pg_database", "SELECT * FROM pg_file_settings", "SELECT * FROM pg_hba_file_rules",
        "SELECT * FROM pg_largeobject", "SELECT * FROM pg_attribute", "SELECT * FROM pg_proc",
        "SELECT * FROM information_schema.tables", "SELECT * FROM information_schema.columns",
        "SELECT * FROM information_schema.role_table_grants", "SELECT * FROM pg_toast.pg_toast_1",
        "SELECT * FROM pg_temp.zzw12", "SELECT * FROM pg_stat_replication",
        "SELECT * FROM pg_catalog.pg_class c, pg_catalog.pg_namespace n WHERE n.oid = c.relnamespace"
    ];

    public static readonly string[] LegitimateSelects =
    [
        "SELECT 1", "SELECT 1::integer AS \"Value\"", "SELECT 'a' AS \"Label\", 2.5::double precision AS \"Amount\"",
        "SELECT \"Id\", \"Name\" FROM \"VisualisationRegistry\" WHERE \"Name\" LIKE 'x%' ORDER BY \"Id\" LIMIT 10",
        "SELECT count(*) AS \"Total\" FROM \"Archive\"",
        "SELECT date_trunc('day', \"CreatedDate\") AS \"Day\", count(*) AS \"Count\" FROM \"Archive\" WHERE \"CreatedDate\" >= @DateFrom GROUP BY 1 ORDER BY 1",
        "SELECT a.\"Id\" FROM \"Archive\" a INNER JOIN \"EntityAnalysisModel\" e ON e.\"Id\" = a.\"EntityAnalysisModelId\" WHERE e.\"Id\" = @Model",
        "WITH x AS (SELECT 1 AS a) SELECT * FROM x",
        "WITH RECURSIVE t(n) AS (VALUES (1) UNION ALL SELECT n + 1 FROM t WHERE n < 5) SELECT * FROM t",
        "SELECT 1 UNION ALL SELECT 2", "SELECT 1 UNION SELECT 2 EXCEPT SELECT 3", "(SELECT 1) UNION ALL (SELECT 2)",
        "SELECT * FROM generate_series(1, 10) g",
        "SELECT g, row_number() OVER (ORDER BY g) FROM generate_series(1, 3) g",
        "SELECT sum(x) OVER (PARTITION BY y) FROM (VALUES (1, 2), (3, 4)) v(x, y)",
        "SELECT (\"Json\" -> 'payload' ->> 'Amount')::double precision FROM \"Archive\" LIMIT 5",
        "SELECT jsonb_build_object('a', 1) AS j", "SELECT string_agg(g::text, ',') FROM generate_series(1, 3) g",
        "SELECT CASE WHEN 1 = 1 THEN 'y' ELSE 'n' END", "SELECT now() AS \"Now\", current_date, current_timestamp",
        "SELECT coalesce(NULL, 'x'), nullif(1, 2), greatest(1, 2), least(1, 2), abs(-1), round(1.234, 2)",
        "SELECT * FROM (VALUES (1), (2)) v(x) WHERE x IN (SELECT 1)", "SELECT 1 WHERE EXISTS (SELECT 1)",
        "SELECT lower('A'), upper('a'), length('abc'), substr('abc', 1, 2), concat('a', 'b'), md5('a')",
        "SELECT to_char(now(), 'YYYY-MM-DD'), extract(epoch FROM now()), age(now(), now())",
        "SELECT pg_typeof(1), pg_size_pretty(1024::bigint)",
        "SELECT * FROM \"Archive\" TABLESAMPLE SYSTEM (1) LIMIT 5",
        "SELECT x FROM unnest(ARRAY[1, 2, 3]) x", "SELECT 1 -- trailing comment", "/* lead */ SELECT 1",
        "SELECT * FROM \"Case\" c LEFT JOIN \"CaseWorkflow\" w ON w.\"Guid\" = c.\"CaseWorkflowGuid\" WHERE c.\"Id\" > @Id",
        "SELECT $$dollar$$ AS s", "SELECT 'it''s' AS s", "SELECT E'tab\\t' AS s", "SELECT 1;"
    ];

    public static IReadOnlyList<(string Label, string Sql)> HttpAttacks()
    {
        var attacks = new List<(string, string)>();
        foreach (var (kind, sql) in WriteSeeds)
        {
            foreach (var (name, text, neutral) in Variants(sql)
                         .Where(v => v.Name is "identity" or "alt-case" or "splice-comment"))
            {
                if (!neutral)
                {
                    attacks.Add(($"{kind}/{name}", text));
                }
            }
        }

        foreach (var expression in DangerousExpressions)
        {
            attacks.Add(($"fn {expression}", $"SELECT {expression}"));
            attacks.Add(($"fn-from {expression}", $"SELECT * FROM {expression}"));
            attacks.Add(($"fn-cte {expression}", $"WITH c AS (SELECT {expression}) SELECT * FROM c"));
        }

        foreach (var sql in ProtectedReads)
        {
            attacks.Add(($"read {sql}", sql));
        }

        return attacks;
    }

    public static IEnumerable<(string Name, string Text, bool Neutral)> Variants(string sql)
    {
        yield return ("identity", sql, false);
        yield return ("upper", sql.ToUpperInvariant(), false);
        yield return ("v3", sql.ToLowerInvariant(), false);
        yield return ("alt-case", AlternatingCase(sql, false), false);
        yield return ("v5", AlternatingCase(sql, true), false);
        yield return ("lead-comment", "/* leading */ " + sql, false);
        yield return ("v7", sql + " -- trailing", false);
        yield return ("v8", sql + "\n-- trailing\n", false);
        yield return ("v9", "-- head\n" + sql, false);
        yield return ("v10", "/*x*/" + sql + "/*y*/", false);
        yield return ("splice-comment", SpliceBetweenWords(sql, "/**/"), false);
        yield return ("v12", SpliceBetweenWords(sql, "/*zz*/"), false);
        yield return ("splice-tab", SpliceBetweenWords(sql, "\t"), false);
        yield return ("v14", SpliceBetweenWords(sql, "\n"), false);
        yield return ("v15", SpliceBetweenWords(sql, "\r\n"), false);
        yield return ("v16", SpliceBetweenWords(sql, "\f"), false);
        yield return ("v17", SpliceBetweenWords(sql, "\v"), false);
        yield return ("v18", SpliceBetweenWords(sql, "  "), false);
        yield return ("v19", SpliceBetweenWords(sql, " -- c\n "), false);
        yield return ("v20", SpliceBetweenWords(sql, "/* /* nested */ */"), false);
        yield return ("v21", "  \t\r\n " + sql + " \r\n\t  ", false);
        yield return ("v22", "(" + sql + ")", false);
        yield return ("v23", sql + ";", false);
        yield return ("v24", sql + " ; ", false);
        yield return ("v25", "SELECT 1 /* " + sql + " */", true);
        yield return ("v26", "\ufeff" + sql, false);
        yield return ("v27", "\u00a0" + sql, false);
        yield return ("v28", SpliceBetweenWords(sql, "\u00a0"), false);
        yield return ("v29", SpliceBetweenWords(sql, "\u200b"), false);
        yield return ("v30", SpliceBetweenWords(sql, "\u2028"), false);
        yield return ("v31", FullWidth(sql), false);
        yield return ("v32", sql.Replace(" ", "\u3000"), false);
        yield return ("v33", sql.Replace("'", "\u2019"), false);
        yield return ("v34", sql.Replace("\"", "\u201c"), false);
        yield return ("v35", sql + "\0", false);
        yield return ("v36", "\0" + sql, false);
        yield return ("v37", sql.Replace(" ", "\0 ", StringComparison.Ordinal), false);
        yield return ("v38", Uri.EscapeDataString(sql), false);
        yield return ("v39", Uri.EscapeDataString(Uri.EscapeDataString(sql)), false);
        yield return ("v40", System.Net.WebUtility.HtmlEncode(sql), false);
        yield return ("v41", string.Concat(sql.Select(c => "&#" + (int)c + ";")), false);
        yield return ("v42", string.Concat(sql.Select(c => "\\u" + ((int)c).ToString("x4"))), false);
        yield return ("v43", Convert.ToBase64String(Encoding.UTF8.GetBytes(sql)), false);
        yield return ("v44", "E'" + sql.Replace("'", "\\'") + "'", false);
        yield return ("v45", "SELECT $q$" + sql + "$q$", true);
        yield return ("v46", "SELECT $$" + sql + "$$", true);
        yield return ("v47", "1) b; " + sql + "; SELECT * FROM (SELECT 1", false);
        yield return ("v48", "SELECT 1) b, LATERAL (" + sql + ") c --", false);
        yield return ("v49", "SELECT 1 WHERE (" + sql + ")", true);
        yield return ("stack-prefix", "SELECT 1; " + sql, false);
        yield return ("v51", sql + "; SELECT 1", false);
        yield return ("v52", sql + ";--\nSELECT 1", false);
        yield return ("v53", "SELECT 1 /*" + sql + "*/; " + sql, false);
        yield return ("v54", sql.Replace("'", "$$"), true);
        yield return ("v55", "SELECT 'x'/*'*/; " + sql + " -- '", false);
        yield return ("v56", "SELECT E'\\''; " + sql + "; SELECT E'\\''", false);
        yield return ("v57", "SELECT U&'\\0041'; " + sql, false);
    }

    private static string AlternatingCase(string sql, bool startUpper)
    {
        var builder = new StringBuilder(sql.Length);
        var upper = startUpper;
        var inQuote = false;
        foreach (var c in sql)
        {
            if (c is '\'' or '"')
            {
                inQuote = !inQuote;
            }

            builder.Append(!inQuote && char.IsLetter(c)
                ? upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c)
                : c);
            if (!inQuote && char.IsLetter(c))
            {
                upper = !upper;
            }
        }

        return builder.ToString();
    }

    private static string SpliceBetweenWords(string sql, string filler)
    {
        var builder = new StringBuilder(sql.Length * 2);
        var inQuote = false;
        foreach (var c in sql)
        {
            if (c is '\'' or '"')
            {
                inQuote = !inQuote;
            }

            if (c == ' ' && !inQuote)
            {
                builder.Append(filler);
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string FullWidth(string sql)
    {
        return string.Concat(sql.Select(c => c is > ' ' and < '\u007f' ? (char)(c + 0xFEE0) : c));
    }
}
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using PgSqlParser;

namespace Jube.Data.Reporting
{
    public static class PostgresSqlValidator
    {
        private const int MaximumSqlLength = 65536;
        private const int MaximumNestingDepth = 64;

        private static readonly (Regex Pattern, string Replacement)[] normalisationRules =
        [
            (new Regex(@"@\w+", RegexOptions.Compiled), "1")
        ];

        private static readonly Regex namedParameterPattern = new(@"@\w+", RegexOptions.Compiled);

        private static readonly Regex positionalParameterPattern = new(@"\$\d+", RegexOptions.Compiled);

        private static readonly string[] deniedFunctionPrefixes =
        [
            "pg_", "lo_", "lowrite", "loread", "dblink", "set_config", "current_setting", "nextval", "setval",
            "currval", "lastval", "txid_", "query_to_xml", "table_to_xml", "cursor_to_xml", "database_to_xml",
            "schema_to_xml", "ts_stat", "get_raw_page", "heap_page", "bt_page", "page_header", "pgrowlocks",
            "pgstat", "pgp_pub_decrypt", "binary_upgrade", "gin_", "brin_", "hash_page", "pg_ls", "has_"
        ];

        private static readonly HashSet<string> deniedFunctionNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "repeat", "lpad", "rpad", "array_fill", "version", "current_database", "current_catalog",
            "current_schema", "current_schemas", "inet_server_addr", "inet_server_port", "inet_client_addr",
            "inet_client_port", "to_regclass", "to_regtype", "to_regproc", "to_regprocedure", "to_regoper",
            "to_regoperator", "to_regnamespace", "to_regrole", "to_regcollation"
        };

        private static readonly
            ConcurrentDictionary<MessageDescriptor, (FieldDescriptor[] Plain, OneofDescriptor[] Oneofs)>
            messageFieldsByDescriptor =
                new();

        private static readonly HashSet<string> allowedPgFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "pg_typeof", "pg_size_pretty"
        };

        private static readonly HashSet<string> deniedSchemas = new(StringComparer.OrdinalIgnoreCase)
        {
            "pg_catalog", "information_schema", "pg_toast", "pg_temp", "pg_global"
        };

        private static readonly HashSet<string> deniedRelations = new(StringComparer.OrdinalIgnoreCase)
        {
            "UserRegistry", "UserRegistryVersion", "UserRegistryApiKey", "UserLogin", "UserLogout", "UserInTenant",
            "UserInTenantSwitchLog", "TenantRegistry", "TenantRegistryVersion", "RoleRegistry",
            "RoleRegistryVersion", "RoleRegistryPermission", "RoleRegistryPermissionVersion",
            "EntityAnalysisModelHttpAdaptation", "EntityAnalysisModelHttpAdaptationVersion",
            "ExhaustiveInstanceCsv", "ExhaustiveInstanceCsvRow", "ExhaustiveInstanceJson",
            "ExhaustiveInstanceJsonRow",
            "ApplicationLogEntry", "AskJoobyAgentGuardRejection", "AskJoobyAgentGuardRule",
            "CacheAbstraction", "CachePayload", "CachePayloadLatest",
            "CachePayloadLatestRemovalBatch", "CachePayloadLatestRemovalBatchEntry", "CachePayloadLatestRemovalBatchResponseTime",
            "CachePayloadRemovalBatch", "CachePayloadRemovalBatchEntry", "CachePayloadRemovalBatchResponseTime",
            "CacheReferenceDate", "CacheSanction", "CacheSetHash",
            "CacheSetHash_0", "CacheSetHash_1", "CacheSetHash_2",
            "CacheSetHash_3", "CacheTtlCounter", "CacheTtlCounterEntry",
            "CacheTtlCounterEntryRemovalBatch", "CacheTtlCounterEntryRemovalBatchEntry", "CacheTtlCounterEntryRemovalBatchResponseTime",
            "CaptureQueueHealth", "CaseCreationStagePerformanceCounter", "CaseExecutionLog",
            "ContainerLogEntry", "DockerContainerMetric", "DockerEvent",
            "DockerHostMetric", "DotNetRuntimeMetric", "EntityAnalysisAsynchronousQueueBalance",
            "EntityAnalysisInstance", "EntityAnalysisModelEngineSnapshot", "EntityAnalysisModelSynchronisationError",
            "EtcdClusterEvent", "EtcdMemberStatus", "HAProxyReachabilityProbe",
            "HAProxyServerStatus", "HashCacheAssemblyInstance", "HashCacheAssemblyInstanceEntry",
            "HashCacheAssemblyInstanceJournal", "HttpProcessingCounter", "HttpResponseHeader",
            "LocalCacheInstance", "LocalCacheInstanceKey", "LocalCacheInstanceLru",
            "ModelInvokeWarning", "OpenTelemetryExclude", "OpenTelemetryLogCounter",
            "OpenTelemetryMetric", "OtlpDispatchCounter", "OverlayNetworkTaskDrift",
            "PatroniClusterEvent", "PatroniMemberStatus", "PostgresLogEntry",
            "PostgresMetric", "PostgresReplicationStatus", "RedisCallCounter",
            "RedisConnectionEvent", "RedisConnectionMultiplexerMetric", "RedisMetric",
            "RedisSentinelEvent", "RedisSentinelStatus", "RedisSlowOperation",
            "SessionCaseSearchCompiledSql", "SessionCaseSearchCompiledSqlExecution", "WafAttack",
            "WafException", "WafSignature"
        };

        public static void AssertSelectOnly(string sql)
        {
            AssertSelectOnly(sql, false);
        }

        public static void AssertSelectOnly(string sql, bool blockProtectedRelations)
        {
            AssertSelectOnly(sql, blockProtectedRelations, false);
        }

        public static void AssertSelectOnly(string sql, bool blockProtectedRelations,
            bool allowCatalogueRelations)
        {
            try
            {
                AssertSelectOnlyCore(sql, blockProtectedRelations, allowCatalogueRelations);
            }
            catch (InvalidProtocolBufferException)
            {
                throw new InvalidOperationException("The SQL statement is nested too deeply.");
            }
        }

        public static string EscapeStringLiteral(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        public static string RewriteTableReferences(string sql, IReadOnlyDictionary<string, string> allowedTables)
        {
            AssertSelectOnly(sql, false, true);
            var result = Parser.Parse(sql);

            if (result.Error is not null)
            {
                throw new InvalidOperationException($"SQL parse error: {result.Error.Message}");
            }

            var parsed = result.Value;

            foreach (var rawStmt in parsed.Stmts)
            {
                WalkAndSubstituteTables(rawStmt.Stmt, allowedTables);
            }

            var deparsed = Parser.Deparse(parsed);

            if (deparsed.Error is not null)
            {
                throw new InvalidOperationException(
                    $"Failed to regenerate SQL after table substitution: {deparsed.Error.Message}");
            }

            var rewrittenSql = deparsed.Value;
            AssertSelectOnly(rewrittenSql, false, true);

            return rewrittenSql;
        }

        public static string RewriteTableReferencesPreservingNamedParameters(string sql,
            IReadOnlyDictionary<string, string> allowedTables)
        {
            if (!string.IsNullOrEmpty(sql) && positionalParameterPattern.IsMatch(sql))
            {
                throw new InvalidOperationException(
                    "Positional parameters are not permitted in the SQL statement; use named parameters.");
            }

            var names = new List<string>();

            var substituted = namedParameterPattern.Replace(sql ?? string.Empty, match =>
            {
                names.Add(match.Value);

                return "$" + names.Count;
            });

            var rewrittenSql = RewriteTableReferences(substituted, allowedTables);

            for (var i = names.Count; i >= 1; i--)
            {
                rewrittenSql = rewrittenSql.Replace("$" + i, names[i - 1]);
            }

            return rewrittenSql;
        }

        public static IReadOnlyList<string> CollectTableReferences(string sql)
        {
            AssertSelectOnly(sql, false, true);
            var result = Parser.Parse(sql);

            if (result.Error is not null)
            {
                throw new InvalidOperationException($"SQL parse error: {result.Error.Message}");
            }

            var names = new List<string>();

            foreach (var rawStmt in result.Value.Stmts)
            {
                WalkAndCollectTables(rawStmt.Stmt, names, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            return names;
        }

        private static void AssertSelectOnlyCore(string sql, bool blockProtectedRelations,
            bool allowCatalogueRelations)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new InvalidOperationException("No SQL statement provided.");
            }

            if (sql.Contains('\0'))
            {
                throw new InvalidOperationException("The SQL statement contains a NUL character.");
            }

            if (sql.Length > MaximumSqlLength)
            {
                throw new InvalidOperationException("The SQL statement is too large.");
            }

            if (ExceedsNestingDepth(sql))
            {
                throw new InvalidOperationException("The SQL statement nests expressions too deeply.");
            }

            var normalizedSql = normalisationRules.Aggregate(sql,
                (current, rule) => rule.Pattern.Replace(current, rule.Replacement));

            var result = Parser.Parse(normalizedSql);

            if (result.Error is not null)
            {
                throw new InvalidOperationException(
                    $"SQL parse error: {result.Error.Message}");
            }

            var parsed = result.Value;

            if (parsed.Stmts.Count == 0)
            {
                throw new InvalidOperationException("No SQL statement provided.");
            }

            if (parsed.Stmts.Count != 1)
            {
                throw new InvalidOperationException("Only a single SELECT statement is permitted.");
            }

            var rawStmt = parsed.Stmts[0];

            if (rawStmt.Stmt?.SelectStmt == null)
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. Blocked statement detected in query.");
            }

            AssertTreeIsReadOnly(rawStmt.Stmt.SelectStmt, blockProtectedRelations, allowCatalogueRelations);
        }

        private static bool ExceedsNestingDepth(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return false;
            }

            var parenDepth = 0;
            var i = 0;

            while (i < sql.Length)
            {
                var c = sql[i];

                if (c == '\'')
                {
                    i++;

                    while (i < sql.Length)
                    {
                        if (sql[i] == '\'')
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == '\'')
                            {
                                i += 2;
                                continue;
                            }

                            break;
                        }

                        i++;
                    }

                    i++;
                    continue;
                }

                if (c == '"')
                {
                    i++;

                    while (i < sql.Length)
                    {
                        if (sql[i] == '"')
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == '"')
                            {
                                i += 2;
                                continue;
                            }

                            break;
                        }

                        i++;
                    }

                    i++;
                    continue;
                }

                if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
                {
                    i += 2;

                    while (i < sql.Length && sql[i] != '\n')
                    {
                        i++;
                    }

                    continue;
                }

                if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                {
                    var blockDepth = 1;
                    i += 2;

                    while (i < sql.Length && blockDepth > 0)
                    {
                        if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                        {
                            blockDepth++;
                            i += 2;
                        }
                        else if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/')
                        {
                            blockDepth--;
                            i += 2;
                        }
                        else
                        {
                            i++;
                        }
                    }

                    continue;
                }

                if (c == '(')
                {
                    parenDepth++;

                    if (parenDepth > MaximumNestingDepth)
                    {
                        return true;
                    }

                    i++;
                    continue;
                }

                if (c == ')')
                {
                    if (parenDepth > 0)
                    {
                        parenDepth--;
                    }
                }

                i++;
            }

            return false;
        }

        private static void AssertTreeIsReadOnly(IMessage root, bool blockProtectedRelations,
            bool allowCatalogueRelations)
        {
            var pending = new Stack<IMessage>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var message = pending.Pop();
                var descriptor = message.Descriptor;

                switch (descriptor.Name)
                {
                    case "SelectStmt":
                        break;
                    case "IntoClause":
                        throw new InvalidOperationException(
                            "Only SELECT statements are permitted. SELECT INTO is blocked.");
                    case "LockingClause":
                        throw new InvalidOperationException(
                            "Only SELECT statements are permitted. Row locking is blocked.");
                    case "FuncCall":
                        AssertFunctionAllowed(message);
                        break;
                    case "SQLValueFunction":
                        AssertSqlValueFunctionAllowed(message);
                        break;
                    case "RangeVar":
                        AssertRelationAllowed(message, blockProtectedRelations, allowCatalogueRelations);
                        break;
                    default:
                        if (descriptor.Name.EndsWith("Stmt", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                "Only SELECT statements are permitted. Blocked statement detected in query.");
                        }

                        break;
                }

                foreach (var field in VisitableFieldsOf(message))
                {
                    var value = field.Accessor.GetValue(message);

                    switch (value)
                    {
                        case IMessage child:
                            pending.Push(child);
                            break;
                        case IEnumerable children when !field.IsMap:
                            foreach (var item in children)
                            {
                                if (item is IMessage childMessage)
                                {
                                    pending.Push(childMessage);
                                }
                            }

                            break;
                    }
                }
            }
        }

        private static IEnumerable<FieldDescriptor> VisitableFieldsOf(IMessage message)
        {
            var (plain, oneofs) = messageFieldsByDescriptor.GetOrAdd(message.Descriptor, d =>
                ([.. d.Fields.InFieldNumberOrder().Where(f => f.FieldType == FieldType.Message && f.RealContainingOneof == null)],
                    [.. d.Oneofs.Where(o => !o.IsSynthetic)]));

            foreach (var field in plain)
            {
                yield return field;
            }

            foreach (var oneof in oneofs)
            {
                var set = oneof.Accessor.GetCaseFieldDescriptor(message);

                if (set is { FieldType: FieldType.Message })
                {
                    yield return set;
                }
            }
        }

        private static void AssertSqlValueFunctionAllowed(IMessage function)
        {
            var op = function.Descriptor.FindFieldByName("op")?.Accessor.GetValue(function)?.ToString() ?? string.Empty;

            if (op.Contains("User", StringComparison.OrdinalIgnoreCase) ||
                op.Contains("Catalog", StringComparison.OrdinalIgnoreCase) ||
                op.Contains("Schema", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. A blocked function was detected in the query.");
            }
        }

        private static void AssertFunctionAllowed(IMessage funcCall)
        {
            var name = StringValues(funcCall, "funcname").LastOrDefault();

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (allowedPgFunctions.Contains(name))
            {
                return;
            }

            if (deniedFunctionNames.Contains(name) ||
                deniedFunctionPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. A blocked function was detected in the query.");
            }
        }

        private static void AssertRelationAllowed(IMessage rangeVar, bool blockProtectedRelations,
            bool allowCatalogueRelations)
        {
            var schema = ScalarString(rangeVar, "schemaname");
            var relation = ScalarString(rangeVar, "relname");

            if (!string.IsNullOrEmpty(schema) &&
                (deniedSchemas.Contains(schema) || schema.StartsWith("pg_", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. System catalogues are blocked.");
            }

            if (string.IsNullOrEmpty(relation))
            {
                return;
            }

            if ((!allowCatalogueRelations && relation.StartsWith("pg_", StringComparison.OrdinalIgnoreCase)) ||
                (blockProtectedRelations && deniedRelations.Contains(relation)))
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. A protected table was detected in the query.");
            }
        }

        private static string ScalarString(IMessage message, string fieldName)
        {
            var field = message.Descriptor.FindFieldByName(fieldName);

            return field?.Accessor.GetValue(message) as string;
        }

        private static IEnumerable<string> StringValues(IMessage message, string fieldName)
        {
            var field = message.Descriptor.FindFieldByName(fieldName);

            if (field?.Accessor.GetValue(message) is not IEnumerable nodes)
            {
                yield break;
            }

            foreach (var node in nodes)
            {
                if (node is not IMessage nodeMessage)
                {
                    continue;
                }

                var stringField = nodeMessage.Descriptor.FindFieldByName("string");

                if (stringField?.Accessor.GetValue(nodeMessage) is IMessage stringMessage &&
                    ScalarString(stringMessage, "sval") is { } value)
                {
                    yield return value;
                }
            }
        }

        private static void WalkAndCollectTables(IMessage message, ICollection<string> names,
            ISet<string> cteNames)
        {
            if (message is SelectStmt { WithClause: not null } withSelect)
            {
                var scoped = new HashSet<string>(cteNames, StringComparer.OrdinalIgnoreCase);

                foreach (var cteNode in withSelect.WithClause.Ctes)
                {
                    if (cteNode.CommonTableExpr is not { } cte || string.IsNullOrEmpty(cte.Ctename))
                    {
                        continue;
                    }

                    if (withSelect.WithClause.Recursive)
                    {
                        scoped.Add(cte.Ctename);
                    }

                    WalkAndCollectTables(cte.Ctequery, names, scoped);
                    scoped.Add(cte.Ctename);
                }

                WalkChildrenAndCollect(withSelect, names, scoped, withSelect.WithClause);

                return;
            }

            if (message is Node { RangeVar: not null } node)
            {
                var relname = node.RangeVar.Relname;

                if (!string.IsNullOrEmpty(relname) && !cteNames.Contains(relname) && !names.Contains(relname))
                {
                    names.Add(relname);
                }

                return;
            }

            WalkChildrenAndCollect(message, names, cteNames, null);
        }

        private static void WalkChildrenAndCollect(IMessage message, ICollection<string> names,
            ISet<string> cteNames, IMessage skip)
        {
            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                if (field.FieldType != FieldType.Message)
                {
                    continue;
                }

                if (field.IsRepeated)
                {
                    var list = (IList)field.Accessor.GetValue(message);

                    foreach (var item in list)
                    {
                        if (item is IMessage childMessage && !ReferenceEquals(childMessage, skip))
                        {
                            WalkAndCollectTables(childMessage, names, cteNames);
                        }
                    }

                    continue;
                }

                if (field.Accessor.GetValue(message) is IMessage child && !ReferenceEquals(child, skip))
                {
                    WalkAndCollectTables(child, names, cteNames);
                }
            }
        }

        private static void WalkAndSubstituteTables(IMessage message, IReadOnlyDictionary<string, string> allowedTables)
        {
            if (message is SelectStmt { WithClause: not null } withSelect)
            {
                var scoped = new Dictionary<string, string>(allowedTables, StringComparer.OrdinalIgnoreCase);

                foreach (var cteNode in withSelect.WithClause.Ctes)
                {
                    if (cteNode.CommonTableExpr is not { } cte || string.IsNullOrEmpty(cte.Ctename))
                    {
                        continue;
                    }

                    if (allowedTables.ContainsKey(cte.Ctename))
                    {
                        throw new InvalidOperationException(
                            $"\"{cte.Ctename}\" is a real table and cannot also be the name of a " +
                            "common table expression in the same statement.");
                    }

                    if (withSelect.WithClause.Recursive)
                    {
                        scoped[cte.Ctename] = null;
                    }

                    WalkAndSubstituteTables(cte.Ctequery, scoped);
                    scoped[cte.Ctename] = null;
                }

                WalkChildrenAndSubstitute(withSelect, scoped, withSelect.WithClause);

                return;
            }

            if (message is Node { RangeVar: not null } node)
            {
                var relname = node.RangeVar.Relname;

                if (!allowedTables.TryGetValue(relname, out var template))
                {
                    throw new InvalidOperationException($"Table \"{relname}\" is not permitted in this query.");
                }

                if (template is null)
                {
                    return;
                }

                var aliasName = string.IsNullOrEmpty(node.RangeVar.Alias?.Aliasname)
                    ? relname
                    : node.RangeVar.Alias.Aliasname;

                var templateResult = Parser.Parse(template);

                if (templateResult.Error is not null)
                {
                    throw new InvalidOperationException(
                        $"Internal error: substitution template for \"{relname}\" failed to parse: " +
                        $"{templateResult.Error.Message}");
                }

                node.RangeSubselect = new RangeSubselect
                {
                    Subquery = templateResult.Value.Stmts[0].Stmt,
                    Alias = new Alias { Aliasname = aliasName }
                };

                return;
            }

            WalkChildrenAndSubstitute(message, allowedTables, null);
        }

        private static void WalkChildrenAndSubstitute(IMessage message,
            IReadOnlyDictionary<string, string> allowedTables, IMessage skip)
        {
            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                if (field.FieldType != FieldType.Message)
                {
                    continue;
                }

                if (field.IsRepeated)
                {
                    var list = (IList)field.Accessor.GetValue(message);

                    foreach (var item in list)
                    {
                        if (item is IMessage childMessage && !ReferenceEquals(childMessage, skip))
                        {
                            WalkAndSubstituteTables(childMessage, allowedTables);
                        }
                    }

                    continue;
                }

                if (field.Accessor.GetValue(message) is IMessage child && !ReferenceEquals(child, skip))
                {
                    WalkAndSubstituteTables(child, allowedTables);
                }
            }
        }
    }
}
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

namespace Jube.Data.Reporting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Google.Protobuf;
    using Google.Protobuf.Reflection;
    using PgSqlParser;

    public static class PostgresSqlValidator
    {
        private const int MaximumSqlLength = 65536;

        private static readonly (Regex Pattern, string Replacement)[] normalisationRules =
        [
            (new Regex(@"@\w+", RegexOptions.Compiled), "1")
        ];

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
            "EntityAnalysisModelHttpAdaptation", "EntityAnalysisModelHttpAdaptationVersion"
        };

        public static void AssertSelectOnly(string sql)
        {
            AssertSelectOnly(sql, false);
        }

        public static void AssertSelectOnly(string sql, bool blockProtectedRelations)
        {
            try
            {
                AssertSelectOnlyCore(sql, blockProtectedRelations);
            }
            catch (InvalidProtocolBufferException)
            {
                throw new InvalidOperationException("The SQL statement is nested too deeply.");
            }
        }

        private static void AssertSelectOnlyCore(string sql, bool blockProtectedRelations)
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

            AssertTreeIsReadOnly(rawStmt.Stmt.SelectStmt, blockProtectedRelations);
        }

        private static void AssertTreeIsReadOnly(IMessage root, bool blockProtectedRelations)
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
                        AssertRelationAllowed(message, blockProtectedRelations);
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
                        case System.Collections.IEnumerable children when !field.IsMap:
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
                (d.Fields.InFieldNumberOrder().Where(f => f.FieldType == FieldType.Message && f.RealContainingOneof == null).ToArray(),
                    d.Oneofs.Where(o => !o.IsSynthetic).ToArray()));

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

        private static void AssertRelationAllowed(IMessage rangeVar, bool blockProtectedRelations)
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

            if (relation.StartsWith("pg_", StringComparison.OrdinalIgnoreCase) ||
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
            if (field?.Accessor.GetValue(message) is not System.Collections.IEnumerable nodes)
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
    }
}
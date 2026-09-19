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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using Jube.Test.Infrastructure.ModelScaffolding.Models;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    internal static class ModelGraphCopier
    {
        private const string Root = ModelScaffold.Root;

        private static readonly Regex defaultExcluded = new(ModelScaffold.DefaultExcludedTablePattern,
            RegexOptions.Compiled);

        public static async Task<int> ResolveLiveSourceAsync(NpgsqlConnection connection,
            NpgsqlTransaction? transaction, int requestedModelId)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT live.\"Id\" FROM \"{Root}\" requested JOIN \"{Root}\" live ON live.\"Guid\" = requested.\"Guid\" " +
                "WHERE requested.\"Id\" = @id AND COALESCE(requested.\"Deleted\", 0) = 1 " +
                "AND COALESCE(live.\"Deleted\", 0) = 0 ORDER BY live.\"Id\" DESC LIMIT 1", connection, transaction);
            command.Parameters.AddWithValue("id", requestedModelId);

            return await command.ExecuteScalarAsync().ConfigureAwait(false) is int liveId ? liveId : requestedModelId;
        }

        public static async Task<(int ModelId, IReadOnlyDictionary<string, IReadOnlyDictionary<long, long>> IdMap)>
            CopyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, ModelScaffoldOptions options,
                int tenantRegistryId, Guid modelGuid, string rootName)
        {
            var exampleModelId = await ResolveLiveSourceAsync(connection, transaction, options.SourceModelId)
                .ConfigureAwait(false);
            await using (var exists = new NpgsqlCommand(
                             $"SELECT count(*) FROM \"{Root}\" WHERE \"Id\" = @id", connection, transaction))
            {
                exists.Parameters.AddWithValue("id", exampleModelId);
                if ((long)(await exists.ExecuteScalarAsync().ConfigureAwait(false)).Required() == 0)
                {
                    throw new InvalidOperationException(
                        $"Source model {exampleModelId} does not exist in \"{Root}\", so there is nothing to scaffold.");
                }
            }

            bool Excluded(string table)
            {
                return options.ExcludeTables.Contains(table) ||
                       (defaultExcluded.IsMatch(table) && !options.IncludeTables.Contains(table));
            }

            var columns = new List<Column>();
            await using (var command = new NpgsqlCommand(
                             "SELECT table_name, column_name, data_type, is_nullable, column_default, is_identity " +
                             "FROM information_schema.columns WHERE table_schema = 'public'", connection, transaction))
            await using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var isIdentity = reader.GetString(5) == "YES" ||
                                     (!await reader.IsDBNullAsync(4).ConfigureAwait(false) &&
                                      reader.GetString(4).StartsWith("nextval", StringComparison.OrdinalIgnoreCase));
                    columns.Add(new Column(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                        reader.GetString(3) == "YES", isIdentity));
                }
            }

            var edges = new List<Edge>();
            var compositeEdges = new List<Edge>();
            await using (var command = new NpgsqlCommand(
                             "SELECT cl.relname, a.attname, pl.relname, c.conname, array_length(c.conkey, 1) " +
                             "FROM pg_constraint c JOIN pg_class cl ON cl.oid = c.conrelid " +
                             "JOIN pg_namespace n ON n.oid = cl.relnamespace AND n.nspname = 'public' " +
                             "JOIN pg_class pl ON pl.oid = c.confrelid " +
                             "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1] " +
                             "WHERE c.contype = 'f'", connection, transaction))
            await using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var edge = new Edge(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                        reader.GetString(3));
                    (reader.GetInt32(4) == 1 ? edges : compositeEdges).Add(edge);
                }
            }

            var guidLinked = columns
                .Where(c => c.Name == "EntityAnalysisModelGuid" && c.DataType == "uuid" &&
                            c.Table != Root && c.Table != "EntityAnalysisModelRole" && !Excluded(c.Table))
                .Select(c => c.Table).ToHashSet();

            var set = new HashSet<string> { Root };
            set.UnionWith(guidLinked);
            var queue = new Queue<string>([Root, .. guidLinked]);
            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                foreach (var edge in edges.Where(e => e.Parent == parent && e.Child != parent))
                {
                    if (Excluded(edge.Child) || !set.Add(edge.Child))
                    {
                        continue;
                    }

                    queue.Enqueue(edge.Child);
                }
            }

            foreach (var name in options.IncludeTables.Where(name => !set.Contains(name)))
            {
                throw new InvalidOperationException(
                    $"IncludeTables names \"{name}\", which cannot be reached from \"{Root}\" through a foreign key or " +
                    "an EntityAnalysisModelGuid column (or is also in ExcludeTables), so it cannot be scaffolded.");
            }

            foreach (var edge in compositeEdges.Where(e => set.Contains(e.Child) && set.Contains(e.Parent)))
            {
                throw new InvalidOperationException(
                    $"Table \"{edge.Child}\" has a composite foreign key ({edge.Constraint}) to \"{edge.Parent}\", which the " +
                    "scaffold cannot remap. Add the table to ExcludeTables, or extend the scaffold.");
            }

            var order = new List<string>();
            var remaining = new HashSet<string>(set);
            while (remaining.Count > 0)
            {
                var ready = remaining.Where(t => edges
                        .Where(e => e.Child == t && e.Parent != t && set.Contains(e.Parent))
                        .All(e => !remaining.Contains(e.Parent)))
                    .OrderBy(t => t, StringComparer.Ordinal).ToList();
                if (ready.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The foreign key graph below the model has a cycle between: " + string.Join(", ", remaining));
                }

                foreach (var table in ready)
                {
                    order.Add(table);
                    remaining.Remove(table);
                }
            }

            await ExecuteAsync(connection, transaction,
                "CREATE TEMP TABLE _idmap (tbl text, oldid bigint, newid bigint) ON COMMIT DROP").ConfigureAwait(false);
            await ExecuteAsync(connection, transaction,
                "CREATE TEMP TABLE _guidmap (oldg uuid, newg uuid) ON COMMIT DROP").ConfigureAwait(false);

            foreach (var table in order)
            {
                var tableColumns = columns.Where(c => c.Table == table).ToList();
                var identity = tableColumns.FirstOrDefault(c => c.IsIdentity && c.Name == "Id") ??
                               throw new InvalidOperationException(
                                   $"Table \"{table}\" has no identity \"Id\" column, so its rows cannot be copied with a fresh " +
                                   "identity. Add the table to ExcludeTables, or extend the scaffold.");
                _ = identity;

                var ownEdges = edges.Where(e => e.Child == table).ToList();
                var mappedParents = ownEdges.Where(e => e.Parent != table && set.Contains(e.Parent)).ToList();

                var oldIds = new List<long>();
                if (table == Root)
                {
                    oldIds.Add(exampleModelId);
                }
                else
                {
                    var conditions = mappedParents.Select(e =>
                        $"\"{e.ChildColumn}\" IN (SELECT oldid FROM _idmap WHERE tbl = '{e.Parent}')").ToList();
                    if (guidLinked.Contains(table))
                    {
                        conditions.Add(
                            $"\"EntityAnalysisModelGuid\" IN (SELECT \"Guid\" FROM \"{Root}\" WHERE \"Id\" = {exampleModelId})");
                    }

                    if (conditions.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Table \"{table}\" was discovered below the model but has no foreign key or guid column to select its rows by.");
                    }

                    await using var select = new NpgsqlCommand(
                        $"SELECT \"Id\" FROM \"{table}\" WHERE {string.Join(" OR ", conditions)} ORDER BY \"Id\"",
                        connection, transaction);
                    await using var reader = await select.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        oldIds.Add(Convert.ToInt64(reader.GetValue(0)));
                    }
                }

                var insertColumns = tableColumns.Where(c => c.Name != "Id").ToList();
                var expressions = new List<string>();
                var guards = new List<(string Column, string Sql)>();
                foreach (var column in insertColumns)
                {
                    var edge = ownEdges.FirstOrDefault(e => e.ChildColumn == column.Name);
                    if (edge is { Parent: "TenantRegistry" } || (edge == null && column.Name == "TenantRegistryId"))
                    {
                        expressions.Add("@tenant");
                    }
                    else if (edge != null && edge.Parent != table && set.Contains(edge.Parent))
                    {
                        var lookup =
                            $"SELECT newid FROM _idmap WHERE tbl = '{edge.Parent}' AND oldid = t.\"{column.Name}\"";
                        expressions.Add($"({lookup})");
                        guards.Add((column.Name, column.Nullable
                            ? $"(t.\"{column.Name}\" IS NULL OR EXISTS ({lookup}))"
                            : $"EXISTS ({lookup})"));
                    }
                    else if (edge != null && edge.Parent != table && Excluded(edge.Parent) && !column.Nullable)
                    {
                        throw new InvalidOperationException(
                            $"Column \"{table}\".\"{column.Name}\" is not nullable and refers to \"{edge.Parent}\", which is " +
                            "excluded from the copy, so the row cannot be copied. Include the parent table, or exclude this one.");
                    }
                    else if (edge is { Parent: "Import" } && column.Nullable)
                    {
                        expressions.Add("NULL");
                    }
                    else if (table == Root && column.Name == "Guid")
                    {
                        expressions.Add("@modelGuid");
                    }
                    else if (column.Name == "Guid" && column.DataType == "uuid")
                    {
                        expressions.Add("gen_random_uuid()");
                    }
                    else
                        switch (table)
                        {
                            case Root when column.Name == "Name":
                                expressions.Add("@rootName");
                                break;
                            case Root when column.Name == "Active":
                                expressions.Add("@active");
                                break;
                            case Root when column.Name == "Locked":
                                expressions.Add("@locked");
                                break;
                            default:
                                expressions.Add($"t.\"{column.Name}\"");
                                break;
                        }
                }

                var hasGuid = tableColumns.Any(c => c.Name == "Guid" && c.DataType == "uuid");
                var columnList = string.Join(", ", insertColumns.Select(c => $"\"{c.Name}\""));
                var guardSql = guards.Count == 0
                    ? string.Empty
                    : " AND " + string.Join(" AND ", guards.Select(g => g.Sql));
                var insertSql =
                    $"INSERT INTO \"{table}\" ({columnList}) SELECT {string.Join(", ", expressions)} " +
                    $"FROM \"{table}\" t WHERE t.\"Id\" = @old{guardSql} RETURNING \"Id\"" +
                    (hasGuid ? ", \"Guid\"" : string.Empty);

                foreach (var oldId in oldIds)
                {
                    long newId;
                    Guid? newGuid = null;
                    try
                    {
                        await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
                        insert.Parameters.AddWithValue("old", oldId);
                        insert.Parameters.AddWithValue("tenant", tenantRegistryId);
                        if (table == Root)
                        {
                            insert.Parameters.AddWithValue("modelGuid", modelGuid);
                            insert.Parameters.AddWithValue("rootName", rootName);
                            insert.Parameters.AddWithValue("active", (short)(options.Active ? 1 : 0));
                            insert.Parameters.AddWithValue("locked", (short)(options.Locked ? 1 : 0));
                        }

                        await using var reader = await insert.ExecuteReaderAsync().ConfigureAwait(false);
                        if (!await reader.ReadAsync().ConfigureAwait(false))
                        {
                            throw new InvalidOperationException(
                                $"Row {oldId} of table \"{table}\" was not copied because it refers, through " +
                                $"{string.Join(", ", guards.Select(g => "\"" + g.Column + "\""))}, to a row that was not copied " +
                                "(the parent is excluded, or is not below the model).");
                        }

                        newId = Convert.ToInt64(reader.GetValue(0));
                        if (hasGuid)
                        {
                            newGuid = reader.GetGuid(1);
                        }
                    }
                    catch (PostgresException ex)
                    {
                        throw new InvalidOperationException(
                            $"Could not copy row {oldId} of table \"{table}\"" +
                            (ex.ColumnName != null ? $", column \"{ex.ColumnName}\"" : string.Empty) +
                            (ex.ConstraintName != null ? $", constraint {ex.ConstraintName}" : string.Empty) +
                            $": {ex.MessageText}", ex);
                    }

                    await ExecuteAsync(connection, transaction,
                        "INSERT INTO _idmap (tbl, oldid, newid) VALUES (@t, @o, @n)",
                        ("t", table), ("o", oldId), ("n", newId)).ConfigureAwait(false);

                    if (newGuid.HasValue)
                    {
                        await ExecuteAsync(connection, transaction,
                            $"INSERT INTO _guidmap (oldg, newg) SELECT \"Guid\", @n FROM \"{table}\" WHERE \"Id\" = @o",
                            ("n", newGuid.Value), ("o", oldId)).ConfigureAwait(false);
                    }
                }
            }

            foreach (var table in order)
            {
                foreach (var column in columns.Where(c => c.Table == table && c.DataType == "uuid" && c.Name != "Guid"))
                {
                    await ExecuteAsync(connection, transaction,
                        $"UPDATE \"{table}\" t SET \"{column.Name}\" = g.newg FROM _guidmap g " +
                        $"WHERE t.\"{column.Name}\" = g.oldg AND " +
                        $"t.\"Id\" IN (SELECT newid FROM _idmap WHERE tbl = '{table}')").ConfigureAwait(false);
                }
            }

            var idMap = new Dictionary<string, Dictionary<long, long>>();
            await using (var command =
                         new NpgsqlCommand("SELECT tbl, oldid, newid FROM _idmap", connection, transaction))
            await using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (!idMap.TryGetValue(reader.GetString(0), out var perTable))
                    {
                        idMap[reader.GetString(0)] = perTable = new Dictionary<long, long>();
                    }

                    perTable[reader.GetInt64(1)] = reader.GetInt64(2);
                }
            }

            var modelId = idMap[Root][exampleModelId];
            return ((int)modelId,
                idMap.ToDictionary(kvp => kvp.Key, IReadOnlyDictionary<long, long> (kvp) => kvp.Value));
        }

        public static async Task<List<string>> GuidLinkedTablesAsync(NpgsqlConnection connection)
        {
            var tables = new List<string>();
            await using var command = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.columns WHERE table_schema = 'public' " +
                $"AND column_name = 'EntityAnalysisModelGuid' AND data_type = 'uuid' AND table_name <> '{Root}'",
                connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        public static async Task CascadeDeleteAsync(NpgsqlConnection connection, string table, string whereSql,
            params (string Name, object Value)[] parameters)
        {
            await CascadeChildrenAsync(connection, table, whereSql, parameters, 0).ConfigureAwait(false);
            await ExecuteAsync(connection, $"DELETE FROM \"{table}\" WHERE {whereSql}", parameters)
                .ConfigureAwait(false);
        }

        private static async Task CascadeChildrenAsync(NpgsqlConnection connection, string table, string whereSql,
            (string Name, object Value)[] parameters, int depth)
        {
            if (depth > 8)
            {
                return;
            }

            var children = new List<(string Child, string ChildColumn, string ParentColumn)>();
            await using (var command = new NpgsqlCommand(
                             "SELECT cl.relname, a.attname, af.attname FROM pg_constraint c " +
                             "JOIN pg_class cl ON cl.oid = c.conrelid " +
                             "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1] " +
                             "JOIN pg_attribute af ON af.attrelid = c.confrelid AND af.attnum = c.confkey[1] " +
                             "JOIN pg_class pl ON pl.oid = c.confrelid " +
                             "WHERE c.contype = 'f' AND pl.relname = @table AND cl.relname <> @table", connection))
            {
                command.Parameters.AddWithValue("table", table);
                await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    children.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                }
            }

            foreach (var (child, childColumn, parentColumn) in children)
            {
                var childWhere = $"\"{childColumn}\" IN (SELECT \"{parentColumn}\" FROM \"{table}\" WHERE {whereSql})";
                await CascadeChildrenAsync(connection, child, childWhere, parameters, depth + 1).ConfigureAwait(false);
                await ExecuteAsync(connection, $"DELETE FROM \"{child}\" WHERE {childWhere}", parameters)
                    .ConfigureAwait(false);
            }
        }

        public static Task ExecuteAsync(NpgsqlConnection connection, string sql,
            params (string Name, object Value)[] parameters)
        {
            return ExecuteAsync(connection, null, sql, parameters);
        }

        public static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql,
            params (string Name, object Value)[] parameters)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}
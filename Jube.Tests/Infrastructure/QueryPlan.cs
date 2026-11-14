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
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

namespace Jube.Test.Infrastructure
{
    internal sealed class QueryPlan
    {
        private QueryPlan(List<Node> nodes, double executionMilliseconds, long sharedBuffers)
        {
            Nodes = nodes;
            ExecutionMilliseconds = executionMilliseconds;
            SharedBuffers = sharedBuffers;
        }

        public IReadOnlyList<Node> Nodes { get; }
        public double ExecutionMilliseconds { get; }
        public long SharedBuffers { get; }

        public sealed record Node(string NodeType, string? Relation, string? Index, long Rows, long Loops)
        {
            public long RowsTouched => Rows * Loops;
        }

        public static Task<QueryPlan> ExplainAsync(string connectionString, string sql,
            IReadOnlyList<object> parameters)
        {
            return ExplainAsync(connectionString, sql, parameters.Select((value, index) =>
                new NpgsqlParameter("@" + (index + 1), value is DateTime date
                    ? DateTime.SpecifyKind(date, DateTimeKind.Unspecified)
                    : value)).ToArray());
        }

        public static async Task<QueryPlan> ExplainAsync(string connectionString, string sql,
            params NpgsqlParameter[] parameters)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + sql, connection);
            command.CommandTimeout = 300;
            command.Parameters.AddRange(parameters);

            var json = (string)(await command.ExecuteScalarAsync())!;
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement[0];
            var nodes = new List<Node>();
            long buffers = 0;

            Walk(root.GetProperty("Plan"), nodes, ref buffers, true);

            return new QueryPlan(nodes, root.GetProperty("Execution Time").GetDouble(), buffers);
        }

        private static void Walk(JsonElement plan, List<Node> nodes, ref long buffers, bool top)
        {
            if (top)
            {
                buffers = Long(plan, "Shared Hit Blocks") + Long(plan, "Shared Read Blocks");
            }

            nodes.Add(new Node(plan.GetProperty("Node Type").GetString()!, Text(plan, "Relation Name"),
                Text(plan, "Index Name"), Long(plan, "Actual Rows"), Long(plan, "Actual Loops")));

            if (!plan.TryGetProperty("Plans", out var children))
            {
                return;
            }

            foreach (var child in children.EnumerateArray())
            {
                Walk(child, nodes, ref buffers, false);
            }
        }

        private static string? Text(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) ? value.GetString() : null;

        private static long Long(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) ? (long)Math.Round(value.GetDouble()) : 0;

        public IEnumerable<Node> SeqScans(string relation) =>
            Nodes.Where(n => n.NodeType == "Seq Scan" && n.Relation == relation);

        public bool Uses(string index) => Nodes.Any(n => n.Index == index);

        public long RowsTouched(string relation) =>
            Nodes.Where(n => n.Relation == relation && n.NodeType.Contains("Scan", StringComparison.Ordinal))
                .Sum(n => n.RowsTouched);

        public long MaxLoops(string index) => Nodes.Where(n => n.Index == index).Select(n => n.Loops)
            .DefaultIfEmpty(0).Max();

        public string Witness(string name) => string.Format(CultureInfo.InvariantCulture,
            "{0}: {1:F1} ms, {2} shared buffers", name, ExecutionMilliseconds, SharedBuffers);
    }
}

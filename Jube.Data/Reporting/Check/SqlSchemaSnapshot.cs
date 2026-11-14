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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery.Models;

namespace Jube.Data.Reporting.Check
{
    public sealed class SqlSchemaSnapshot
    {
        private readonly Dictionary<string, SqlRelationInfo> publicRelations = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SqlRelationInfo> catalogueRelations = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SqlRelationInfo> insensitive = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SqlFunctionInfo> functions = new(StringComparer.Ordinal);
        private readonly HashSet<string> typeNames;

        public SqlSchemaSnapshot(PostgresSchemaSnapshotRows rows)
        {
            var primaryKeys = rows.PrimaryKeys
                .GroupBy(r => r.RelationName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (IReadOnlySet<string>)g.Select(r => r.ColumnName).ToHashSet(StringComparer.Ordinal),
                    StringComparer.Ordinal);

            foreach (var relation in rows.Columns.GroupBy(c => (c.SchemaName, c.RelationName)))
            {
                var columns = relation
                    .Select(c => new SqlColumnInfo(c.ColumnName, SqlTypeRules.CategoryOf(c.TypeName, c.TypeCategory),
                        c.TypeName))
                    .ToList();

                var isPublic = relation.Key.SchemaName == "public";

                var info = new SqlRelationInfo(relation.Key.RelationName, columns,
                    isPublic && primaryKeys.TryGetValue(relation.Key.RelationName, out var key)
                        ? key
                        : new HashSet<string>(StringComparer.Ordinal));

                (isPublic ? publicRelations : catalogueRelations)[relation.Key.RelationName] = info;

                insensitive.TryAdd(relation.Key.RelationName, info);
            }

            foreach (var group in rows.Functions.GroupBy(f => f.Name, StringComparer.Ordinal))
            {
                functions[group.Key] = new SqlFunctionInfo(group.Key,
                    group.Select(f => new SqlFunctionOverload(
                        f.Variadic ? Math.Max(0, f.ArgumentCount - 1) : f.ArgumentCount - f.DefaultCount,
                        f.Variadic ? -1 : f.ArgumentCount,
                        f.Kind.Length > 0 ? f.Kind[0] : 'f')).ToList());
            }

            typeNames = rows.TypeNames.ToHashSet(StringComparer.Ordinal);
        }

        public ConcurrentDictionary<string, SqlDerivedColumns> TemplateColumns { get; } = new(StringComparer.Ordinal);

        public IEnumerable<string> RelationNames => publicRelations.Keys.Concat(catalogueRelations.Keys);

        public IEnumerable<string> FunctionNames => functions.Keys;

        public SqlRelationInfo FindRelation(string name)
        {
            if (publicRelations.TryGetValue(name, out var found) || catalogueRelations.TryGetValue(name, out found))
            {
                return found;
            }

            return insensitive.GetValueOrDefault(name);
        }

        public SqlFunctionInfo FindFunction(string name)
        {
            return functions.GetValueOrDefault(name);
        }

        public bool HasType(string name)
        {
            return typeNames.Contains(name);
        }
    }
}

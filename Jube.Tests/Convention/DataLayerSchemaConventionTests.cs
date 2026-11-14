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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery;
using Jube.Data.Reporting.Check;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB.Mapping;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DataLayerSchemaConventionTests(DatabaseFixture fx)
    {
        private static readonly Regex literal = new(
            "\"\"\"(?<raw>.*?)\"\"\"|@\"(?<verbatim>(?:[^\"]|\"\")*)\"|\"(?<plain>(?:[^\"\\\\\\n]|\\\\.)*)\"",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex sqlShape = new(
            @"\b(SELECT|INSERT\s+INTO|DELETE\s+FROM|UPDATE)\b.*\b(FROM|INTO|SET|WHERE)\b",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex quoted = new("\"([A-Za-z][A-Za-z0-9_]*)\"", RegexOptions.Compiled);

        private static readonly Regex alias = new("\\bAS\\s+\"([A-Za-z0-9_]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex derivedColumnList = new(
            "(?:\\bWITH\\s+(?:RECURSIVE\\s+)?|,\\s*|\\bAS\\s+)\\w+\\s*\\(((?:\\s*\"\\w+\"\\s*,?)+)\\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private async Task<SqlSchemaSnapshot> SnapshotAsync()
        {
            var rows = await new GetPostgresSchemaSnapshotQuery(fx.ConnectionString).ExecuteAsync();

            return new SqlSchemaSnapshot(rows);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return directory.Required().FullName;
        }

        private static readonly HashSet<string> preservedLegacyMappings = new(StringComparer.Ordinal)
        {
            "EntityAnalysisModelSuppression",
            "EntityAnalysisModelSuppressionVersion",
            "EntityAnalysisModelActivationRuleSuppression",
            "EntityAnalysisModelActivationRuleSuppressionVersion"
        };

        private static string Identifier(string name)
        {
            return name.Length > 63 ? name[..63] : name;
        }

        private static IEnumerable<Type> MappedTypes()
        {
            return typeof(Data.Poco.TenantRegistry).Assembly.GetTypes()
                .Where(t => t.Namespace == "Jube.Data.Poco" && t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0);
        }

        [Fact]
        public async Task EveryMappedPocoTableAndColumnExistsInTheMigratedSchemaAsync()
        {
            var snapshot = await SnapshotAsync();
            var missing = new List<string>();

            foreach (var type in MappedTypes())
            {
                if (preservedLegacyMappings.Contains(type.Name))
                {
                    continue;
                }

                var entity = MappingSchema.Default.GetEntityDescriptor(type);
                var relation = snapshot.FindRelation(Identifier(entity.Name.Name));

                if (relation == null)
                {
                    missing.Add($"{type.Name}: table {entity.Name.Name}");

                    continue;
                }

                var present = relation.Columns.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

                missing.AddRange(entity.Columns.Where(c => !present.Contains(Identifier(c.ColumnName)))
                    .Select(c => $"{type.Name}: column {entity.Name.Name}.{c.ColumnName}"));
            }

            missing.Should().BeEmpty("every mapped table and column must exist in the migrated template schema");
        }

        [Fact]
        public async Task EveryQuotedIdentifierInAnSqlLiteralOfTheDataLayerExistsInTheMigratedSchemaAsync()
        {
            var snapshot = await SnapshotAsync();
            var known = snapshot.RelationNames.ToHashSet(StringComparer.Ordinal);

            foreach (var name in snapshot.RelationNames)
            {
                foreach (var column in snapshot.FindRelation(name).Columns)
                {
                    known.Add(column.Name);
                }
            }

            var root = Path.Combine(RepositoryRoot(), "Jube.Data");
            var separator = Path.DirectorySeparatorChar;
            var unknown = new List<string>();

            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}")
                                                                                && !p.Contains($"{separator}Check{separator}")
                                                                                && !p.EndsWith("PostgresSqlValidator.cs", StringComparison.Ordinal)))
            {
                var texts = literal.Matches(File.ReadAllText(path))
                    .Select(match => match.Groups["raw"].Success
                        ? match.Groups["raw"].Value
                        : match.Groups["verbatim"].Success
                            ? match.Groups["verbatim"].Value.Replace("\"\"", "\"")
                            : match.Groups["plain"].Value.Replace("\\\"", "\""))
                    .Where(text => sqlShape.IsMatch(text))
                    .ToList();

                var local = new HashSet<string>(StringComparer.Ordinal);

                foreach (var text in texts)
                {
                    foreach (Match match in alias.Matches(text))
                    {
                        local.Add(match.Groups[1].Value);
                    }

                    foreach (Match list in derivedColumnList.Matches(text))
                    {
                        foreach (Match column in quoted.Matches(list.Groups[1].Value))
                        {
                            local.Add(column.Groups[1].Value);
                        }
                    }
                }

                foreach (var text in texts)
                {
                    unknown.AddRange(quoted.Matches(text).Select(m => m.Groups[1].Value)
                        .Where(i => !known.Contains(i) && !local.Contains(i))
                        .Distinct()
                        .Select(i => $"{Path.GetRelativePath(root, path)}: {i}"));
                }
            }

            unknown.Should().BeEmpty("raw SQL in the data layer may name only tables and columns of the migrated schema or its own aliases");
        }
    }
}

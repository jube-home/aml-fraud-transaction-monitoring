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
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Convention")]
    public sealed class SoftDeletedGuidTwinConventionTests
    {
        private const int Window = 900;

        private static readonly string[] Roots = ["Jube.Data", "Jube.Service", "Jube.App", "Jube.Engine"];

        private static readonly Dictionary<string, string> ByDesign = new()
        {
            ["Jube.Data/Query/ApproveEntireModelQuery/ApproveEntireModelQuery.cs"] =
                "the model is pinned by its Id in the same statement, so the twin on Guid cannot multiply rows",
            ["Jube.Data/Query/GetArchiveDistinctEntryKeyValue/GetArchiveDistinctEntryKeyValue.cs"] =
                "archive history deliberately spans every copy of a model that shares the Guid",
            ["Jube.Engine/EntityAnalysisModelManager/EntityAnalysisModel/Context/Extensions/SyncEntityAnalysisModelAbstractionRulesExtensions.cs"] =
                "archive history deliberately spans every copy of a model that shares the Guid",
            ["Jube.Engine/EntityAnalysisModelManager/EntityAnalysisModel/Context/Extensions/SyncEntityAnalysisModelRequestXPathExtensions.cs"] =
                "archive history deliberately spans every copy of a model that shares the Guid",
            ["Jube.Data/Repository/AdvancedAnalyticsPreservationRepository.cs"] =
                "the in-use check must see soft-deleted rows, because a Guid they hold still blocks an import",
            ["Jube.Data/Repository/CaseFileRepository.cs"] =
                "the action is pinned by its Id inside the same predicate",
            ["Jube.Data/Repository/CaseNoteRepository.cs"] =
                "the action is pinned by its Id inside the same predicate",
            ["Jube.Data/Repository/CaseWorkflowFormEntryRepository.cs"] =
                "the form is pinned by its Id inside the same predicate",
            ["Jube.Data/Repository/ModelInvokeWarningRepository.cs"] =
                "only excludes warnings whose model belongs to another tenant, which a same-tenant twin cannot change",
            ["Jube.Service/DynamicSql/DynamicSqlTableAccess.cs"] =
                "the dynamic SQL catalogue lists rows as stored and exposes Deleted as an ordinary column"
        };

        private static readonly string Tables = string.Join("|", SoftDeletableTables());

        private static readonly Regex RawSqlTable = new(
            "\"(?:" + Tables + ")\"\\s+(?:as\\s+)?(?!(?:on|where|join|left|inner|set|using|cross|group|order|limit)\\b)(\\w+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LinqJoin = new(
            "join\\s+(\\w+)\\s+in\\s+dbContext\\.(?:" + Tables + ")\\s+on\\s+[^\\r\\n]*(?:\\r?\\n\\s*equals[^\\r\\n]*)?",
            RegexOptions.Compiled);

        private static readonly Regex LinqLookup = new(
            "dbContext\\.(?:" + Tables + ")\\s*\\.(?:Where|First\\w*|Single\\w*|Any\\w*|Count\\w*)\\(\\s*(\\w+)\\s*=>[^;]{0,300}?\\b(?:\\b\\1\\.Guid\\s*==|Contains\\(\\s*\\1\\.Guid\\s*\\))",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static IEnumerable<string> SoftDeletableTables() =>
            Directory.EnumerateFiles(Path.Combine(SolutionRoot(), "Jube.Data", "Poco"), "*.cs")
                .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Text: File.ReadAllText(path)))
                .Where(f => !f.Name.EndsWith("Version", StringComparison.Ordinal)
                            && Regex.IsMatch(f.Text, "public\\s+Guid\\s+Guid\\s*\\{")
                            && Regex.IsMatch(f.Text, "public\\s+byte\\??\\s+Deleted\\s*\\{"))
                .Select(f => f.Name)
                .OrderBy(n => n, StringComparer.Ordinal);

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return directory.Required().FullName;
        }

        private static IEnumerable<string> Sources()
        {
            var separator = Path.DirectorySeparatorChar;

            return Roots.SelectMany(root => Directory.EnumerateFiles(Path.Combine(SolutionRoot(), root), "*.cs",
                    SearchOption.AllDirectories))
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}")
                                                                      && !p.Contains($"{separator}Migrations{separator}")
                                                                      && !p.Contains($"{separator}Poco{separator}"));
        }

        private static string Normalise(string text) => text.Replace("\\\"", "\"");

        private static string Ahead(string text, int index, int length) =>
            text.Substring(index, Math.Min(length, text.Length - index));

        private static int LineOf(string text, int index) => text.Take(index).Count(c => c == '\n') + 1;

        private static bool Guarded(string alias, string window) =>
            Regex.IsMatch(window, "\\b" + alias + "\\s*\\.\\s*\"?Deleted\\b");

        private static bool JoinsOnGuid(string alias, string window) =>
            Regex.IsMatch(window, "\\b" + alias + "\\.\"Guid\"\\s*=") ||
            Regex.IsMatch(window, "=\\s*" + alias + "\\.\"Guid\"");

        private static List<string> Offenders()
        {
            var offenders = new List<string>();

            foreach (var path in Sources())
            {
                var text = Normalise(File.ReadAllText(path));
                var name = Path.GetRelativePath(SolutionRoot(), path).Replace('\\', '/');

                if (ByDesign.ContainsKey(name))
                {
                    continue;
                }

                foreach (Match match in RawSqlTable.Matches(text))
                {
                    var alias = match.Groups[1].Value;
                    var window = Ahead(text, match.Index, Window);

                    if (JoinsOnGuid(alias, window) && !Guarded(alias, window))
                    {
                        offenders.Add($"{name}:{LineOf(text, match.Index)} raw SQL joins {match.Value.Trim()} on Guid with no {alias}.\"Deleted\" check");
                    }
                }

                foreach (Match match in LinqJoin.Matches(text))
                {
                    if (!match.Value.Contains("Guid", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var alias = match.Groups[1].Value;

                    if (!Guarded(alias, Ahead(text, match.Index, Window)))
                    {
                        offenders.Add($"{name}:{LineOf(text, match.Index)} LINQ join {alias} on Guid with no {alias}.Deleted check");
                    }
                }

                foreach (Match match in LinqLookup.Matches(text))
                {
                    var alias = match.Groups[1].Value;
                    var before = text.Substring(Math.Max(0, match.Index - 400), Math.Min(400, match.Index));

                    if (before.Contains("IncludingDeleted", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!Guarded(alias, Ahead(text, match.Index, match.Length + 3000)))
                    {
                        offenders.Add($"{name}:{LineOf(text, match.Index)} LINQ lookup by Guid on {alias} with no {alias}.Deleted check");
                    }
                }
            }

            return offenders.Distinct().OrderBy(o => o, StringComparer.Ordinal).ToList();
        }

        [Fact]
        public void EveryByDesignExemptionPointsAtARealFileAndStatesItsReason()
        {
            foreach (var (name, reason) in ByDesign)
            {
                File.Exists(Path.Combine(SolutionRoot(), name)).Should().BeTrue("{0} must still exist", name);
                reason.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public void EveryGuidJoinOrLookupOnASoftDeletedTableAlsoFiltersDeleted()
        {
            Offenders().Should().BeEmpty(
                "a preservation import leaves a soft-deleted twin sharing the Guid of the live row, so a join or lookup on Guid alone multiplies rows or reaches the dead one");
        }
    }
}

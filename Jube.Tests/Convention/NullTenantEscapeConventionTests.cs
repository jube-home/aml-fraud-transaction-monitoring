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
    public sealed class NullTenantEscapeConventionTests
    {
        private static readonly Regex escape = new(
            @"[tT]enantRegistryId\w*\s*\|\|\s*!\s*[\w.]*[tT]enantRegistryId\w*\.HasValue|!\s*[\w.]*[tT]enantRegistryId\w*\.HasValue\s*\|\|\s*[\w.]*TenantRegistryId\s*==",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> systemModeFiles = new(StringComparer.Ordinal)
        {
            "Jube.Data/Helpers/EntityAnalysisModelParentGuard.cs",
            "Jube.Data/Repository/EntityAnalysisModelAbstractionCalculationRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelAbstractionRuleRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelActivationRuleOverrideRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelActivationRuleRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelDictionaryKvpRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelDictionaryRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelGatewayRuleRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelHttpAdaptationRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelInlineFunctionRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelInlineScriptRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelListRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelListValueRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelOverrideRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelRequestXpathRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelResponseElevationRangeRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelSanctionRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelTagRepository.cs",
            "Jube.Data/Repository/EntityAnalysisModelTtlCounterRepository.cs",
            "Jube.Data/Repository/EntityApprovalRepository.cs",
            "Jube.Data/Repository/ExhaustiveSearchInstancePromotedTrialInstanceRepository.cs",
            "Jube.Data/Repository/ExhaustiveSearchInstanceRepository.cs",
            "Jube.Data/Repository/ExhaustiveSearchInstanceTrialInstanceRepository.cs",
            "Jube.Data/Repository/UserRegistryApiKeyRepository.cs",
            "Jube.Data/Repository/UserRegistryRepository.cs"
        };

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return directory.Required().FullName;
        }

        private static List<string> FilesWithTheEscape()
        {
            var root = RepositoryRoot();
            var separator = Path.DirectorySeparatorChar;

            return new[] { "Jube.Data", "Jube.Service", "Jube.Engine", "Jube.App" }
                .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), "*.cs",
                    SearchOption.AllDirectories))
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Where(p => escape.IsMatch(File.ReadAllText(p)))
                .Select(p => Path.GetRelativePath(root, p).Replace(separator, '/'))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        [Fact]
        public void OnlyTheNamedSystemModeRepositoriesLetAMissingTenantSeeEveryTenant()
        {
            var offenders = FilesWithTheEscape().Where(p => !systemModeFiles.Contains(p)).ToList();

            offenders.Should().BeEmpty(
                "a missing tenant must refuse, so a repository that widens to every tenant is a system-mode repository named on the allow list");
        }

        [Fact]
        public void EveryNamedSystemModeRepositoryStillCarriesTheEscape()
        {
            var present = FilesWithTheEscape().ToHashSet(StringComparer.Ordinal);

            systemModeFiles.Where(p => !present.Contains(p)).Should().BeEmpty(
                "an allow list entry whose escape has gone must be removed so the list stays exact");
        }

        [Fact]
        public void NoRowLevelEscapeLetsARowWithNoTenantMatchEveryCaller()
        {
            var column = new Regex(@"\|\|\s*!\s*\w+(\.\w+)*\.TenantRegistryId\.HasValue", RegexOptions.CultureInvariant);
            var root = RepositoryRoot();
            var separator = Path.DirectorySeparatorChar;

            var offenders = new[] { "Jube.Data", "Jube.Service", "Jube.Engine", "Jube.App" }
                .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), "*.cs",
                    SearchOption.AllDirectories))
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Where(p => column.IsMatch(File.ReadAllText(p)))
                .Select(p => Path.GetRelativePath(root, p))
                .ToList();

            offenders.Should().BeEmpty("a row with a null tenant belongs to no caller and is never matched by a flag test");
        }
    }
}

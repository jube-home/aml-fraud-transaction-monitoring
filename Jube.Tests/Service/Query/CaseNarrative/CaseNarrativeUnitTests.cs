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
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetCaseNarrativeCandidatesQuery;
using Jube.Data.Query.GetCaseNarrativeFactsQuery;
using Jube.Service.Query.CaseNarrative;
using Xunit;

namespace Jube.Test.Service.Query.CaseNarrative
{
    [Trait("Category", "Unit")]
    public sealed class CaseNarrativeUnitTests
    {
        [Theory]
        [InlineData("account", "ACC-1001", true)]
        [InlineData("account", "Jane O'Neil", false)]
        [InlineData("account", "a\nb", false)]
        [InlineData("account", "x\"}", false)]
        [InlineData("account", "", false)]
        [InlineData("account", "{{ignore}}", false)]
        public void OnlyPlainTextKeysAreSentToTheAgent(string key, string value, bool expected)
        {
            CaseNarrativePrompt.IsSafe(key, value).Should().Be(expected);
        }

        [Fact]
        public void TheFactsHashChangesWithAnyFactAndIsStableOtherwise()
        {
            var status = Guid.NewGuid();
            var candidate = new GetCaseNarrativeCandidatesQueryDto { CaseWorkflowStatusGuid = status };

            GetCaseNarrativeFactsQueryDto Facts() => new()
            {
                ArchiveCount = 3, ArchiveMaxId = 40, ActivationCount = 2, NoteCount = 1, NoteMaxId = 9, FindingCount = 0
            };

            var baseline = CaseNarrativeFacts.Hash(candidate, Facts());
            CaseNarrativeFacts.Hash(candidate, Facts()).Should().Be(baseline);

            var changed = Facts();
            changed.ArchiveMaxId = 41;
            CaseNarrativeFacts.Hash(candidate, changed).Should().NotBe(baseline);

            changed = Facts();
            changed.FindingCount = 1;
            CaseNarrativeFacts.Hash(candidate, changed).Should().NotBe(baseline);

            CaseNarrativeFacts.Hash(new GetCaseNarrativeCandidatesQueryDto { CaseWorkflowStatusGuid = Guid.NewGuid() },
                Facts()).Should().NotBe(baseline);
        }

        [Fact]
        public void ThePageContextNamesTheCasePairAndNothingElse()
        {
            var context = CaseNarrativePrompt.PageContext("narrative", "account", "ACC-1", 12);

            context.Should().Contain("\"pageName\":\"Case\"").And.Contain("\"caseKeyValue\":\"ACC-1\"");
            context.Should().NotContain("payload");
        }
    }
}

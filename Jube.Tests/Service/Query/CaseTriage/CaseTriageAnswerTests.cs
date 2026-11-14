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

using System.Linq;
using FluentAssertions;
using Jube.Service.Query.CaseTriage;
using Xunit;

namespace Jube.Test.Service.Query.CaseTriage
{
    [Trait("Category", "Unit")]
    public sealed class CaseTriageAnswerTests
    {
        [Theory]
        [InlineData("confirmed", 1)]
        [InlineData("NotConfirmed", 2)]
        [InlineData(" inconclusive ", 0)]
        public void TheThreeOutcomesMapToTheirIds(string outcome, int expected)
        {
            var answer = CaseTriageAnswer.TryParse($"{{\"outcome\":\"{outcome}\",\"reason\":\"Because.\"}}");

            answer.Should().NotBeNull();
            answer!.OutcomeId.Should().Be(expected);
            answer.Rationale.Should().Be("Because.");
            answer.Evidence.Should().BeEmpty();
        }

        [Fact]
        public void JsonWrappedInProseOrFencesIsFound()
        {
            var answer = CaseTriageAnswer.TryParse(
                "Here you go:\n```json\n{\"outcome\":\"confirmed\",\"reason\":\"R\",\"evidence\":[\"a\",\"\",\"b\"]}\n```");

            answer!.Evidence.Should().Equal("a", "b");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("no json at all")]
        [InlineData("{\"outcome\":\"maybe\",\"reason\":\"R\"}")]
        [InlineData("{\"outcome\":\"confirmed\"}")]
        [InlineData("{\"outcome\":\"confirmed\",\"reason\":\"  \"}")]
        [InlineData("{\"outcome\":1,\"reason\":\"R\"}")]
        [InlineData("{broken")]
        public void UnusableAnswersAreRefused(string? text)
        {
            CaseTriageAnswer.TryParse(text).Should().BeNull();
        }

        [Fact]
        public void LongTextIsClippedAndEvidenceIsCapped()
        {
            var many = string.Join(",", Enumerable.Range(0, 30).Select(i => $"\"{new string('x', 400)}{i}\""));
            var answer = CaseTriageAnswer.TryParse(
                $"{{\"outcome\":\"confirmed\",\"reason\":\"{new string('r', 5000)}\",\"evidence\":[{many}]}}");

            answer!.Rationale.Should().HaveLength(4000);
            answer.Evidence.Should().HaveCount(10).And.OnlyContain(e => e.Length == 300);
        }

        [Fact]
        public void TheDefinitionHashIsStableAndFullLength()
        {
            CaseTriagePrompt.DefinitionHash().Should().HaveLength(64).And.Be(CaseTriagePrompt.DefinitionHash());
        }
    }
}

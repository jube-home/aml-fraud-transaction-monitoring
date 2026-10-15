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

using FluentAssertions;
using Jube.AskJooby.Validation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class PromptPrefixStabilityTests
    {
        [Fact]
        public void TwoValidationsShareEveryByteOfTheCriteriaBeforeAnythingVolatileAppears()
        {
            var first = ResponseValidator.BuildTask("what is a TTL counter", "a bite", "a reply");
            var second = ResponseValidator.BuildTask(
                "something else entirely", "quite different bites", "a completely different reply");

            var shared = CommonPrefixLength(first, second);

            shared.Should().BeGreaterThan(900,
                "the criteria and output format are identical on every validation ever run, so they "
                + "belong in front of the question and the reply where a cache can actually reach them");

            first[..shared].Should().NotContain("what is a TTL counter",
                "nothing volatile may appear inside the shared prefix");
        }

        [Fact]
        public void TheValidationFormatReminderStillComesLastSoItIsTheModelsFinalInstruction()
        {
            var task = ResponseValidator.BuildTask("q", "docs", "reply");

            task.TrimEnd().Should().EndWith("STATUS, REASON and KIND, and nothing else.",
                "a format instruction immediately before the model answers is the one that gets followed");
        }

        [Fact]
        public void TwoRevisionsShareEveryByteOfTheMethodBeforeAnythingVolatileAppears()
        {
            var first = ResponseReviser.BuildTask("question one", "bites", "draft one", "critique one");
            var second = ResponseReviser.BuildTask("question two", "others", "draft two", "critique two");

            var shared = CommonPrefixLength(first, second);

            shared.Should().BeGreaterThan(1000,
                "how to revise never changes, so it belongs in front of what is being revised");

            first[..shared].Should().NotContain("draft one");
        }

        private static int CommonPrefixLength(string first, string second)
        {
            var length = 0;

            while (length < first.Length && length < second.Length && first[length] == second[length])
            {
                length++;
            }

            return length;
        }
    }
}
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


using FluentAssertions;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class CaseKeyJournalIndexStatementTests
    {
        [Theory]
        [InlineData("AccountId", true)]
        [InlineData("Account Id", true)]
        [InlineData("a.b-c:d_9", true)]
        [InlineData("_hidden", true)]
        [InlineData("", false)]
        [InlineData(" lead", false)]
        [InlineData("it's", false)]
        [InlineData("a\"b", false)]
        [InlineData("a;drop", false)]
        [InlineData("a\nb", false)]
        [InlineData("a\\b", false)]
        public void OnlyOrdinaryFieldNamesAreSafeToPutInAnIndexDefinition(string key, bool safe)
        {
            GetCaseKeyJournalRowsQuery.IsIndexableKey(key).Should().Be(safe);
        }

        [Fact]
        public void AFieldNameOfMoreThanOneHundredAndTwentyEightCharactersIsNotIndexable()
        {
            GetCaseKeyJournalRowsQuery.IsIndexableKey(new string('a', 128)).Should().BeTrue();
            GetCaseKeyJournalRowsQuery.IsIndexableKey(new string('a', 129)).Should().BeFalse();
        }

        [Theory]
        [InlineData("ACC-441", true)]
        [InlineData("123", true)]
        [InlineData("-7", true)]
        [InlineData("007", false)]
        [InlineData("1.50", false)]
        [InlineData("1e3", false)]
        [InlineData("True", false)]
        [InlineData("false", false)]
        [InlineData("1,5", true)]
        [InlineData("NaN", true)]
        public void OnlyValuesThatAreTheSameTextInTheDocumentUseTheIndex(string value, bool textEquality)
        {
            GetCaseKeyJournalRowsQuery.UsesTextEquality(value).Should().Be(textEquality);
        }

        [Fact]
        public void TheIndexExpressionQuotesTheFieldNameAsALiteral()
        {
            GetCaseKeyJournalRowsQuery.IndexExpression("AccountId").Should()
                .Be("(\"Json\" -> 'payload' ->> 'AccountId')");
            GetCaseKeyJournalRowsQuery.IndexExpression("a'b").Should().Be("(\"Json\" -> 'payload' ->> 'a''b')");
        }

        [Fact]
        public void TheIndexIsNamedByAHashOfTheFieldAndTheStatementCoversModelExpressionAndIdentity()
        {
            var name = CaseKeyJournalIndexJob.IndexName("AccountId");

            name.Should().StartWith(CaseKeyJournalIndexJob.IndexPrefix).And.HaveLength(
                CaseKeyJournalIndexJob.IndexPrefix.Length + 16);
            CaseKeyJournalIndexJob.IndexName("AccountId").Should().Be(name);
            CaseKeyJournalIndexJob.IndexName("DeviceId").Should().NotBe(name);
            CaseKeyJournalIndexJob.CreateStatement("AccountId").Should().Be(
                $"create index concurrently if not exists \"{name}\" on \"Archive\" " +
                "(\"EntityAnalysisModelId\", (\"Json\" -> 'payload' ->> 'AccountId'), \"Id\" desc)");
        }
    }
}

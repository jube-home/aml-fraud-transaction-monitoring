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
using FluentAssertions;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.ArchiveKeyLookup
{
    [Trait("Category", "Unit")]
    public sealed class ArchiveKeyLookupSqlTests
    {
        [Theory]
        [InlineData(1, ArchiveKeyValueColumn.String)]
        [InlineData(2, ArchiveKeyValueColumn.Integer)]
        [InlineData(3, ArchiveKeyValueColumn.Float)]
        [InlineData(4, ArchiveKeyValueColumn.Date)]
        [InlineData(5, ArchiveKeyValueColumn.Boolean)]
        [InlineData(6, ArchiveKeyValueColumn.Float)]
        [InlineData(7, ArchiveKeyValueColumn.Float)]
        [InlineData(8, ArchiveKeyValueColumn.String)]
        [InlineData(null, ArchiveKeyValueColumn.String)]
        public void TheTypedColumnFollowsTheFieldDataTypeAsTheEngineWritesIt(int? dataTypeId, ArchiveKeyValueColumn expected)
        {
            ArchiveKeyPredicate.ColumnOf(dataTypeId).Should().Be(expected);
        }

        [Fact]
        public void AnEqualityPredicateBindsTheProcessingTypeTheKeyAndTheValueInOrder()
        {
            var tokens = new List<object> { 7, "me" };
            var predicate = ArchiveKeyPredicate.RequestXPath("AccountId", ArchiveKeyValueColumn.String, "ACC-1");

            var scope = ArchiveKeyLookupSql.Scope(predicate, tokens);

            scope.Should().Contain("from \"ArchiveKey\" k ")
                .And.Contain("inner join \"Archive\" a on a.\"EntityAnalysisModelInstanceEntryGuid\" = k.\"EntityAnalysisModelInstanceEntryGuid\"")
                .And.Contain("k.\"ProcessingTypeId\" = (@3) and k.\"Key\" = (@4) and k.\"KeyValueString\" = (@5)")
                .And.Contain("a.\"EntityAnalysisModelId\" = (@1)")
                .And.Contain("u.\"User\" = (@2)");
            tokens.Should().Equal(7, "me", (short)1, "AccountId", "ACC-1");
        }

        [Theory]
        [InlineData(ArchiveKeyLookupOperator.GreaterThan, ">")]
        [InlineData(ArchiveKeyLookupOperator.GreaterThanOrEqual, ">=")]
        [InlineData(ArchiveKeyLookupOperator.LessThan, "<")]
        [InlineData(ArchiveKeyLookupOperator.LessThanOrEqual, "<=")]
        public void ARangePredicateUsesTheTypedColumnWithTheOperator(ArchiveKeyLookupOperator op, string symbol)
        {
            var predicate = new ArchiveKeyPredicate("Amount", 1, ArchiveKeyValueColumn.Float, op, 500d);
            var tokens = new List<object>();

            var condition = predicate.Condition("k", ArchiveKeyLookupSql.Ordered(tokens));

            condition.Should().EndWith($"k.\"KeyValueFloat\" {symbol} (@3)");
            tokens[2].Should().Be(500d);
        }

        [Fact]
        public void ABetweenPredicateBindsBothBounds()
        {
            var predicate = new ArchiveKeyPredicate("Amount", 1, ArchiveKeyValueColumn.Integer,
                ArchiveKeyLookupOperator.Between, 10, 20);
            var tokens = new List<object>();

            predicate.Condition("k", ArchiveKeyLookupSql.Ordered(tokens))
                .Should().EndWith("k.\"KeyValueInteger\" between (@3) and (@4)");
            tokens.Should().Equal((short)1, "Amount", 10, 20);
        }

        [Fact]
        public void TheEntryKeyPredicatesOnTheArchiveRowItselfWithoutTheKeyTable()
        {
            var predicate = ArchiveKeyPredicate.ForEntryKey("E-1");
            var tokens = new List<object> { 7, "me" };

            var scope = ArchiveKeyLookupSql.Scope(predicate, tokens);

            scope.Should().StartWith("from \"Archive\" a ").And.Contain("a.\"EntryKeyValue\" = (@3)")
                .And.NotContain("\"ArchiveKey\"");
            ArchiveKeyLookupSql.Exists(predicate, "a", ArchiveKeyLookupSql.Ordered(tokens))
                .Should().Be("a.\"EntryKeyValue\" = (@4)");
        }

        [Fact]
        public void TheExistsFormCorrelatesOnTheArchiveEntryGuid()
        {
            var predicate = ArchiveKeyPredicate.RequestXPath("Ip", ArchiveKeyValueColumn.String, "10.0.0.1");

            ArchiveKeyLookupSql.Exists(predicate, "a", ArchiveKeyLookupSql.Ordered([]))
                .Should().StartWith("exists (select 1 from \"ArchiveKey\" k where k.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" and k.\"ProcessingTypeId\" = (@1)");
        }

        [Theory]
        [InlineData(ArchiveKeyValueColumn.String, "o.\"KeyValueString\"")]
        [InlineData(ArchiveKeyValueColumn.Integer, "o.\"KeyValueInteger\"::text")]
        [InlineData(ArchiveKeyValueColumn.Boolean, "case o.\"KeyValueBoolean\" when 1 then 'true' when 0 then 'false' end")]
        [InlineData(ArchiveKeyValueColumn.Date, "to_char(o.\"KeyValueDate\", 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"')")]
        public void TheValueTextOfATypedColumnRoundTripsThroughTheResolver(ArchiveKeyValueColumn column, string expected)
        {
            ArchiveKeyLookupSql.ValueText("o", column).Should().Be(expected);
        }

        [Theory]
        [InlineData(ArchiveKeyValueColumn.Integer, true)]
        [InlineData(ArchiveKeyValueColumn.Float, true)]
        [InlineData(ArchiveKeyValueColumn.Date, true)]
        [InlineData(ArchiveKeyValueColumn.Long, true)]
        [InlineData(ArchiveKeyValueColumn.String, false)]
        [InlineData(ArchiveKeyValueColumn.Boolean, false)]
        public void OnlyNumericAndDateColumnsAllowARange(ArchiveKeyValueColumn column, bool expected)
        {
            ArchiveKeyPredicate.AllowsRange(column).Should().Be(expected);
        }

        [Fact]
        public void AFieldNameThatIsNotAPlainIdentifierSelectsByBracket()
        {
            new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelRequestXPath, Guid.NewGuid(), "My Field", 1,
                    ArchiveKeyValueColumn.String, 1, "payload", true, null).SelectTokenPath
                .Should().Be("payload['My Field']");
            new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelAbstractionRule, Guid.NewGuid(), "Volume", 3,
                    ArchiveKeyValueColumn.Float, 5, "abstraction", true, null).SelectTokenPath
                .Should().Be("abstraction.Volume");
        }
    }
}

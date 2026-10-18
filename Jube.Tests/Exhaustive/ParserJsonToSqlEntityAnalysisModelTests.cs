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
using FluentAssertions;
using Jube.Engine.Exhaustive.Utilities.QueryBuilder;
using Jube.Engine.Exhaustive.Utilities.QueryBuilder.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ParserJsonToSqlEntityAnalysisModelTests
    {
        private static readonly CompletionSqlField[] fields =
        [
            new("Amount", "(\"Payload\"->>'Amount')::double precision"),
            new("Country", "\"Payload\"->>'Country'"),
            new("Risk", "\"Payload\"->>'Risk'")
        ];

        private static Rule Leaf(string id, string type, string op, object value)
        {
            return new Rule
            {
                Id = id, Field = id, Type = type, Operator = op,
                Value = JToken.FromObject(value)
            };
        }

        private static Rule Group(string condition, params Rule[] rules)
        {
            return new Rule { Condition = condition, Rules = [.. rules] };
        }

        [Fact]
        public void SingleEqualRule_ProducesOneParameterisedComparison()
        {
            var rule = Leaf("Amount", "double", "equal", 100.5);

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be("(\"Payload\"->>'Amount')::double precision = (@1)");
        }

        [Theory]
        [InlineData("equal", "=")]
        [InlineData("less", "<")]
        [InlineData("less_or_equal", "<=")]
        [InlineData("greater", ">")]
        [InlineData("greater_or_equal", ">=")]
        [InlineData("like", "like")]
        public void EveryComparisonOperator_MapsToItsSqlToken(string op, string sqlToken)
        {
            var rule = Leaf("Amount", "double", op, 5);

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Contain(sqlToken);
        }

        [Fact]
        public void NotEqual_IsNegated()
        {
            var rule = Leaf("Country", "string", "not_equal", "GB");

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().StartWith("not ").And.Contain("= (@1)");
        }

        [Fact]
        public void NestedGroup_WrapsInParenthesesAndJoinsWithCondition()
        {
            var rule = Group("AND",
                Leaf("Amount", "double", "greater", 100),
                Leaf("Country", "string", "equal", "GB"));

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be(
                "((\"Payload\"->>'Amount')::double precision > (@1) AND \"Payload\"->>'Country' = (@2))");
        }

        [Fact]
        public void InOperator_WithAnArrayValue_ProducesOnePlaceholderPerValue()
        {
            var rule = Leaf("Risk", "string", "in", new[] { "Bad", "Fraud" });

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be("\"Payload\"->>'Risk' in (@1, @2)");
        }

        [Fact]
        public void NotInOperator_IsNegated()
        {
            var rule = Leaf("Risk", "string", "not_in", new[] { "Good" });

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be("not \"Payload\"->>'Risk' in (@1)");
        }

        [Fact]
        public void InOperator_WithASingleScalarValue_StillWorks()
        {
            var rule = Leaf("Risk", "string", "in", "Bad");

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be("\"Payload\"->>'Risk' in (@1)");
        }

        [Fact]
        public void InOperator_WithNoValues_Throws()
        {
            var rule = Leaf("Risk", "string", "in", Array.Empty<string>());

            var act = () => ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MixedGroup_NumbersTokensAcrossAnInRuleAndAScalarRuleCorrectly()
        {
            var rule = Group("AND",
                Leaf("Risk", "string", "in", new[] { "Bad", "Fraud" }),
                Leaf("Amount", "double", "greater", 100));

            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            parser.Sql.Should().Be(
                "(\"Payload\"->>'Risk' in (@1, @2) AND (\"Payload\"->>'Amount')::double precision > (@3))");
        }

        [Fact]
        public void UnknownField_Throws()
        {
            var rule = Leaf("NotAField", "string", "equal", "x");

            var act = () => ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            act.Should().Throw<InvalidOperationException>().WithMessage("*NotAField*");
        }

        [Fact]
        public void UnknownOperator_Throws()
        {
            var rule = Leaf("Amount", "double", "regexp", 1);

            var act = () => ParserJsonToSqlEntityAnalysisModel.CreateForFields(rule, fields);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ANullRule_ProducesNoSql()
        {
            var parser = ParserJsonToSqlEntityAnalysisModel.CreateForFields(null, fields);

            parser.Sql.Should().BeNullOrEmpty();
        }
    }
}
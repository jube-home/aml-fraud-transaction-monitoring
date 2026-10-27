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
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class ArchiveFilterTests
    {
        private static readonly Dictionary<string, ArchiveFilterField> fields = new()
        {
            ["Payload.Name"] = new ArchiveFilterField("Payload.Name", "(\"Json\"-> 'payload' ->> 'Name')", "string"),
            ["Payload.Amount"] = new ArchiveFilterField("Payload.Amount",
                "(\"Json\"-> 'payload' ->> 'Amount')::double precision", "double"),
            ["Payload.Count"] = new ArchiveFilterField("Payload.Count", "(\"Json\"-> 'payload' ->> 'Count')::int",
                "integer"),
            ["Payload.When"] = new ArchiveFilterField("Payload.When", "(\"Json\"-> 'payload' ->> 'When')::timestamp",
                "datetime"),
            ["Payload.Flag"] = new ArchiveFilterField("Payload.Flag", "(\"Json\"-> 'payload' ->> 'Flag')::boolean",
                "boolean")
        };

        private static string Rule(string id, string op, string value)
        {
            return $$"""{ "id": "{{id}}", "operator": "{{op}}", "value": "{{value}}" }""";
        }

        private static string Group(string condition, params string[] rules)
        {
            return $$"""{ "condition": "{{condition}}", "rules": [ {{string.Join(", ", rules)}} ] }""";
        }

        [Fact]
        public void AFilterWithNoRulesIsNoFilter()
        {
            ArchiveFilter.Parse(null, fields).Should().BeNull();
            ArchiveFilter.Parse("  ", fields).Should().BeNull();
            ArchiveFilter.Parse(Group("AND"), fields).Should().BeNull();
        }

        [Fact]
        public void NestedGroupsBecomeParameterisedSqlInOrder()
        {
            var filter = ArchiveFilter.Parse(Group("AND", Rule("Payload.Name", "equal", "Acme"),
                Group("OR", Rule("Payload.Amount", "greater", "100.5"), Rule("Payload.Count", "less_or_equal", "3"))),
                fields)!;

            var tokens = new List<object> { 7, "me" };
            var sql = filter.Apply(tokens);

            sql.Should().Be("((\"Json\"-> 'payload' ->> 'Name') = (@3) and " +
                            "((\"Json\"-> 'payload' ->> 'Amount')::double precision > (@4) or " +
                            "(\"Json\"-> 'payload' ->> 'Count')::int <= (@5)))");
            tokens.Skip(2).Should().Equal("Acme", 100.5, 3);
        }

        [Theory]
        [InlineData("equal", "=")]
        [InlineData("not_equal", "<>")]
        [InlineData("less", "<")]
        [InlineData("less_or_equal", "<=")]
        [InlineData("greater", ">")]
        [InlineData("greater_or_equal", ">=")]
        [InlineData("like", "like")]
        [InlineData("not_like", "not like")]
        public void EveryOperatorMapsToItsSql(string op, string sql)
        {
            var filter = ArchiveFilter.Parse(Group("AND", Rule("Payload.Name", op, "A%")), fields)!;

            filter.Apply([]).Should().Be($"((\"Json\"-> 'payload' ->> 'Name') {sql} (@1))");
        }

        [Fact]
        public void ValuesAreConvertedToTheTypeOfTheField()
        {
            var filter = ArchiveFilter.Parse(Group("AND", Rule("Payload.Count", "equal", "12"),
                Rule("Payload.Amount", "equal", "1.25"), Rule("Payload.When", "greater", "2026-10-05T10:30:00Z"),
                Rule("Payload.Flag", "equal", "True")), fields)!;

            var tokens = new List<object>();

            filter.Apply(tokens);

            tokens[0].Should().BeOfType<int>().Which.Should().Be(12);
            tokens[1].Should().BeOfType<double>().Which.Should().Be(1.25);
            tokens[2].Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2026, 10, 5, 10, 30, 0));
            ((DateTime)tokens[2]).Kind.Should().Be(DateTimeKind.Unspecified);
            tokens[3].Should().BeOfType<bool>().Which.Should().BeTrue();
        }

        [Fact]
        public void AValueNeverReachesTheSql()
        {
            var filter = ArchiveFilter.Parse(Group("AND", Rule("Payload.Name", "equal", "x' or 1=1 --")), fields)!;
            var tokens = new List<object>();

            filter.Apply(tokens).Should().NotContain("1=1");
            tokens.Should().Equal("x' or 1=1 --");
        }

        [Theory]
        [InlineData("Payload.Unknown", "equal", "1")]
        [InlineData("Payload.Amount", "like", "1")]
        [InlineData("Payload.Flag", "less", "True")]
        [InlineData("Payload.Count", "equal", "abc")]
        [InlineData("Payload.Count", "equal", "99999999999")]
        [InlineData("Payload.Amount", "equal", "NaN")]
        [InlineData("Payload.When", "equal", "yesterday")]
        [InlineData("Payload.Flag", "equal", "maybe")]
        [InlineData("Payload.Name", "contains", "a")]
        public void AFilterThatIsNotValidIsRefused(string id, string op, string value)
        {
            var act = () => ArchiveFilter.Parse(Group("AND", Rule(id, op, value)), fields);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("[]")]
        [InlineData("""{ "condition": "XOR", "rules": [] }""")]
        [InlineData("""{ "condition": "AND" }""")]
        [InlineData("""{ "condition": "AND", "rules": [ { "id": "Payload.Name", "operator": "equal" } ] }""")]
        [InlineData("""{ "condition": "AND", "rules": [ { "id": "Payload.Name", "operator": "equal", "value": [1] } ] }""")]
        public void AShapeThatIsNotAFilterIsRefused(string json)
        {
            var act = () => ArchiveFilter.Parse(json, fields);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AFilterThatIsTooBigIsRefused()
        {
            var many = Enumerable.Range(0, ArchiveFilter.MaximumRules + 1)
                .Select(index => Rule("Payload.Count", "equal", index.ToString())).ToArray();
            var deep = Group("AND", Rule("Payload.Count", "equal", "1"));

            for (var level = 0; level <= ArchiveFilter.MaximumDepth; level++)
            {
                deep = Group("AND", deep);
            }

            ((Action)(() => ArchiveFilter.Parse(Group("AND", many), fields))).Should().Throw<ArgumentException>();
            ((Action)(() => ArchiveFilter.Parse(deep, fields))).Should().Throw<ArgumentException>();
            ((Action)(() => ArchiveFilter.Parse(Group("AND", Rule("Payload.Name", "equal",
                new string('x', ArchiveFilter.MaximumLength))), fields))).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ThePersistedTextKeepsOnlyWhatTheServerUnderstood()
        {
            var filter = ArchiveFilter.Parse("""
                { "condition": "AND", "valid": true, "rules": [
                  { "id": "Payload.Name", "field": "\"x\"", "type": "double", "operator": "equal", "value": "A", "extra": 1 }
                ] }
                """, fields)!;

            filter.Text.Should().Be("""{"condition":"AND","rules":[{"id":"Payload.Name","operator":"equal","value":"A"}]}""");
            ArchiveFilter.Parse(filter.Text, fields)!.Text.Should().Be(filter.Text);
        }

        [Fact]
        public void OnlyFieldsThatCanBeFilteredAreOffered()
        {
            var usable = ArchiveFilterFields.From(
            [
                new() { Name = "Payload.A", ValueSqlPath = "x", JQueryBuilderDataType = "string" },
                new() { Name = "Payload.A", ValueSqlPath = "y", JQueryBuilderDataType = "string" },
                new() { Name = "Payload.B", ValueSqlPath = "", JQueryBuilderDataType = "string" },
                new() { Name = "Payload.C", ValueSqlPath = "z", JQueryBuilderDataType = "array" },
                new() { Name = "Payload.D", ValueSqlPath = "w", JQueryBuilderDataType = null }
            ]);

            usable.Keys.Should().Equal("Payload.A");
            usable["Payload.A"].Sql.Should().Be("x");
        }
    }
}

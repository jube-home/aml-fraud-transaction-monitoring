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
using System.ComponentModel;
using System.Linq;
using FluentAssertions;
using Jube.Data.QueryBuilder;
using Jube.Service.Agent;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class BuilderFilterTests
    {
        private static readonly Row[] rows =
        [
            new(1, "Alpha", true, 1.5, 10, 'd', new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Guid.Parse("00000000-0000-0000-0000-000000000001"), []),
            new(2, "Beta", false, 2.5, 20, 'h', new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(),
                []),
            new(3, "Alphabet", true, null, 30, 'd', null, Guid.NewGuid(), []),
            new(4, null, null, 4, 40, 'm', new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), [])
        ];

        private static int[] Ids(string json)
        {
            var parsed = BuilderProfile.Parse(json, BuilderFilter.Catalogue(typeof(Row)));
            parsed.Valid.Should().BeTrue(string.Join("; ", parsed.Errors.Select(e => e.Message)));
            var predicate = BuilderFilter.Compile<Row>(parsed.Group);
            return rows.Where(predicate).Select(r => r.Id).ToArray();
        }

        private static string Rule(string id, string op, string value)
        {
            return "{\"condition\":\"AND\",\"rules\":[{\"id\":\"" + id + "\",\"operator\":\"" + op + "\",\"value\":" +
                   value + "}]}";
        }

        [Fact]
        public void TheCatalogueIsEveryReadableScalarPropertyWithItsTypeAndDescription()
        {
            var fields = BuilderFilter.Fields(typeof(Row)).ToDictionary(f => f.Id);

            fields.Keys.Should().BeEquivalentTo("Id", "Name", "Active", "Weight", "Count", "Interval", "CreatedDate",
                "Guid");
            fields["Id"].Type.Should().Be(BuilderFieldType.Integer);
            fields["Count"].Type.Should().Be(BuilderFieldType.Integer);
            fields["Name"].Type.Should().Be(BuilderFieldType.String);
            fields["Interval"].Type.Should().Be(BuilderFieldType.String);
            fields["Active"].Type.Should().Be(BuilderFieldType.Boolean);
            fields["Weight"].Type.Should().Be(BuilderFieldType.Double);
            fields["CreatedDate"].Type.Should().Be(BuilderFieldType.DateTime);
            fields["Guid"].Type.Should().Be(BuilderFieldType.Guid);
            fields["Name"].Description.Should().Be("The row's name.");
            fields["Count"].Description.Should().BeEmpty();
        }

        [Theory]
        [InlineData("equal", "\"Alpha\"", new[] { 1 })]
        [InlineData("begins_with", "\"Alpha\"", new[] { 1, 3 })]
        [InlineData("contains", "\"et\"", new[] { 2, 3 })]
        [InlineData("ends_with", "\"a\"", new[] { 1, 2 })]
        [InlineData("equal", "\"alpha\"", new int[0])]
        public void StringOperatorsAreOrdinalAndNeverMatchANullValue(string op, string value, int[] expected)
        {
            Ids(Rule("Name", op, value)).Should().Equal(expected);
        }

        [Theory]
        [InlineData("equal", "20", new[] { 2 })]
        [InlineData("less", "20", new[] { 1 })]
        [InlineData("less_or_equal", "20", new[] { 1, 2 })]
        [InlineData("greater", "20", new[] { 3, 4 })]
        [InlineData("greater_or_equal", "20", new[] { 2, 3, 4 })]
        public void IntegerOperatorsCompareNumerically(string op, string value, int[] expected)
        {
            Ids(Rule("Count", op, value)).Should().Equal(expected);
        }

        [Fact]
        public void ANullableNumberWithNoValueNeverMatches()
        {
            Ids(Rule("Weight", "greater", "0")).Should().Equal(1, 2, 4);
            Ids(Rule("Weight", "less", "3")).Should().Equal(1, 2);
        }

        [Fact]
        public void BooleansCompareWithTrueOrFalseAndNullMatchesNeither()
        {
            Ids(Rule("Active", "equal", "\"True\"")).Should().Equal(1, 3);
            Ids(Rule("Active", "equal", "\"False\"")).Should().Equal(2);
        }

        [Fact]
        public void ACharIsFilteredAsText()
        {
            Ids(Rule("Interval", "equal", "\"d\"")).Should().Equal(1, 3);
        }

        [Fact]
        public void DatesCompareAsInstantsInUtc()
        {
            Ids(Rule("CreatedDate", "greater_or_equal", "\"2026-06-01T00:00:00Z\"")).Should().Equal(2, 4);
            Ids(Rule("CreatedDate", "less", "\"2026-06-01T01:00:00+02:00\"")).Should().Equal(1);
            Ids(Rule("CreatedDate", "equal", "\"2026-01-01\"")).Should().Equal(1);
        }

        [Fact]
        public void GuidsCompareByValue()
        {
            Ids(Rule("Guid", "equal", "\"00000000-0000-0000-0000-000000000001\"")).Should().Equal(1);
        }

        [Fact]
        public void GroupsCombineWithOrAndNot()
        {
            const string json = "{\"condition\":\"AND\",\"not\":true,\"rules\":[" +
                                "{\"id\":\"Count\",\"operator\":\"greater\",\"value\":5}," +
                                "{\"condition\":\"OR\",\"rules\":[" +
                                "{\"id\":\"Name\",\"operator\":\"equal\",\"value\":\"Alpha\"}," +
                                "{\"id\":\"Name\",\"operator\":\"equal\",\"value\":\"Beta\"}]}]}";

            Ids(json).Should().Equal(3, 4);
        }

        [Fact]
        public void TheFilterFormatRefusesWhatDoesNotFitTheField()
        {
            var fields = BuilderFilter.Catalogue(typeof(Row));

            BuilderProfile.Parse(Rule("Nope", "equal", "1"), fields).Errors.Should()
                .ContainSingle(e => e.Code == "FieldUnknown");
            BuilderProfile.Parse(Rule("Active", "greater", "\"True\""), fields).Errors.Should()
                .ContainSingle(e => e.Code == "OperatorInvalid");
            BuilderProfile.Parse(Rule("CreatedDate", "equal", "\"yesterday\""), fields).Errors.Should()
                .ContainSingle(e => e.Code == "ValueInvalid");
            BuilderProfile.Parse(Rule("Guid", "equal", "\"1\""), fields).Errors.Should()
                .ContainSingle(e => e.Code == "ValueInvalid");
            BuilderProfile.Parse(Rule("Guid", "contains", "\"1\""), fields).Errors.Should()
                .ContainSingle(e => e.Code == "OperatorInvalid");
        }

        [Fact]
        public void DtoFilterPagesByIdAndSaysWhenThereIsMore()
        {
            var first = DtoFilter.Filter(rows.Reverse(), Rule("Count", "greater", "5"), 2, null, r => r.Id);
            var second = DtoFilter.Filter(rows, Rule("Count", "greater", "5"), 2, first.Items[^1].Id, r => r.Id);

            first.Valid.Should().BeTrue();
            first.Items.Select(r => r.Id).Should().Equal(1, 2);
            first.More.Should().BeTrue();
            second.Items.Select(r => r.Id).Should().Equal(3, 4);
            second.More.Should().BeFalse();
        }

        [Fact]
        public void DtoFilterWithNoJsonSelectsEverythingAndClampsTake()
        {
            var result = DtoFilter.Filter(rows, null, 0, null, r => r.Id);

            result.Valid.Should().BeTrue();
            result.Items.Should().ContainSingle().Which.Id.Should().Be(1);
            result.More.Should().BeTrue();
        }

        [Fact]
        public void DtoFilterReturnsErrorsWithPathsInsteadOfThrowing()
        {
            var result = DtoFilter.Filter(rows, Rule("Name", "greater", "\"A\""), 10, null, r => r.Id);

            result.Valid.Should().BeFalse();
            result.Items.Should().BeEmpty();
            result.Errors.Should().ContainSingle(e =>
                e.ErrorCode == "OperatorInvalid" && e.PropertyName == "$.rules[0].operator");
        }

        [Fact]
        public void DtoCountGroupsByAFieldLargestFirstWithNullAsItsOwnGroup()
        {
            var result = DtoFilter.Count(rows, null, "Active");

            result.Valid.Should().BeTrue();
            result.Count.Should().Be(4);
            result.Groups.Select(g => (g.Value, g.Count)).Should()
                .Equal(("True", 2), (null, 1), ("False", 1));
            result.GroupsTruncated.Should().BeFalse();
        }

        [Fact]
        public void DtoCountFiltersBeforeCountingAndRefusesAnUnknownGroupBy()
        {
            DtoFilter.Count(rows, Rule("Interval", "equal", "\"d\""), null).Count.Should().Be(2);
            DtoFilter.Count(rows, null, "Nope").Errors.Should()
                .ContainSingle(e => e.ErrorCode == "FieldUnknown" && e.PropertyName == "groupBy");
        }

        [Fact]
        public void DtoFilterFieldsListTheOperatorsForEachType()
        {
            var fields = DtoFilter.Fields<Row>().ToDictionary(f => f.Name);

            fields["Guid"].Operators.Should().Equal("equal", "not_equal");
            fields["CreatedDate"].Operators.Should().StartWith([
                "equal", "not_equal", "less", "less_or_equal",
                "greater", "greater_or_equal", "between", "not_between"
            ]).And.Contain([
                "is_weekday", "is_weekend",
                "is_end_of_month", "same_day_as"
            ]);
            fields["Name"].DataType.Should().Be("String");
        }

        [Fact]
        public void TheCatalogueReadsEachFieldsValueFromTheRowAndLeavesOutLists()
        {
            var fields = BuilderFilter.Fields(typeof(Row)).ToDictionary(f => f.Id);

            foreach (var row in rows)
            {
                var expected = new Dictionary<string, object?>
                {
                    ["Id"] = row.Id, ["Name"] = row.Name, ["Active"] = row.Active, ["Weight"] = row.Weight,
                    ["Count"] = row.Count, ["Interval"] = row.Interval, ["CreatedDate"] = row.CreatedDate,
                    ["Guid"] = row.Guid
                };
                foreach (var (name, value) in expected)
                {
                    fields[name].Property.GetValue(row).Should().Be(value, name);
                }

                row.Tags.Should().BeEmpty();
            }

            fields.Should().NotContainKey("Tags");
        }

        private sealed record Row(
            int Id,
            [property: Description("The row's name.")]
            string? Name,
            bool? Active,
            double? Weight,
            long Count,
            char Interval,
            DateTime? CreatedDate,
            Guid Guid,
            string[] Tags);
    }
}
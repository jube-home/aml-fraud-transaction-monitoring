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
using System.Text.Json.Nodes;
using FluentAssertions;
using Jube.RegulatoryReport;
using Xunit;

namespace Jube.Test.RegulatoryReport
{
    [Trait("Category", "Unit")]
    public sealed class TemplateDirectiveTests
    {
        private const string Open = "<r xmlns:jr=\"urn:jube:regulatory-report:template:1\">";

        private static (string? Xml, ReportIssue[] Issues) Run(string body, string data,
            string schema = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:element name=\"r\"/></xs:schema>")
        {
            var profile = ExampleProfile.Create(Open + body + "</r>", schema);
            var template = profile with { SchemaDocuments = new System.Collections.Generic.Dictionary<string, string>() };
            var result = ReportBuilder.Build(template, JsonNode.Parse(data));

            return (result.Xml, result.Issues.Where(i => i.Stage == ReportIssueStage.Mapping).ToArray());
        }

        [Fact]
        public void IfIncludesTheElementOnlyWhenTheValueIsPresentAndTrue()
        {
            var body = "<a jr:if=\"flag\" jr:value=\"x\"/><b jr:if=\"missing\">no</b><c jr:if=\"off\">no</c>";

            var (xml, issues) = Run(body, "{\"flag\":true,\"x\":\"1\",\"off\":false}");

            issues.Should().BeEmpty();
            xml.Should().Contain("<a>1</a>").And.NotContain("<b").And.NotContain("<c");
        }

        [Fact]
        public void DefaultFillsAnEmptyValueAndKeepEmptyKeepsTheElement()
        {
            var body = "<a jr:value=\"missing\" jr:default=\"N/A\"/><b jr:value=\"missing\" jr:keep-empty=\"true\"/>";

            var (xml, _) = Run(body, "{}");

            xml.Should().Contain("<a>N/A</a>").And.Contain("<b />");
        }

        [Fact]
        public void PruneDropsAnElementWhoseContentAllVanished()
        {
            var (xml, _) = Run("<party jr:prune=\"true\"><name jr:value=\"missing\"/></party><keep>x</keep>", "{}");

            xml.Should().NotContain("party").And.Contain("<keep>x</keep>");
        }

        [Fact]
        public void EachRepeatsPerItemWithRelativeAndRootPaths()
        {
            var body = "<row jr:each=\"rows\" jr:attributes=\"id=id;owner=$.owner\"><v jr:value=\"n\"/></row>";

            var (xml, issues) = Run(body, "{\"owner\":\"O\",\"rows\":[{\"id\":1,\"n\":\"a\"},{\"id\":2,\"n\":\"b\"}]}");

            issues.Should().BeEmpty();
            xml.Should().Contain("<row id=\"1\" owner=\"O\">").And.Contain("<row id=\"2\" owner=\"O\">")
                .And.Contain("<v>a</v>").And.Contain("<v>b</v>");
        }

        [Fact]
        public void IndexedPathsAndFormatsWork()
        {
            var body = "<a jr:value=\"list[1].t|trim|upper|truncate:3\"/><n jr:value=\"amt|number:0.0\"/>";

            var (xml, _) = Run(body, "{\"list\":[{\"t\":\"zz\"},{\"t\":\"  hello \"}],\"amt\":2.25}");

            xml.Should().Contain("<a>HEL</a>").And.Contain("<n>2.3</n>");
        }

        [Fact]
        public void FormattingIsCultureInvariant()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            try
            {
                var (xml, _) = Run("<n jr:value=\"amt|number:0.00\"/>", "{\"amt\":\"1234.5\"}");
                xml.Should().Contain("<n>1234.50</n>");
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void AListWhereAValueIsExpectedAndAnObjectWhereAListIsExpectedAreReported()
        {
            var (_, issues) = Run("<a jr:value=\"list\"/><b jr:each=\"obj\"/>", "{\"list\":[1],\"obj\":{\"k\":1}}");

            issues.Select(i => i.Code).Should().BeEquivalentTo("NotAValue", "NotAList");
        }

        [Fact]
        public void UnknownFormatsAndBadAttributeListsAreReported()
        {
            var (_, issues) = Run("<a jr:value=\"x|shout\" jr:attributes=\"oops\"/>", "{\"x\":\"1\"}");

            issues.Select(i => i.Code).Should().BeEquivalentTo("UnknownFormat", "BadAttributes");
        }

        [Fact]
        public void ATemplateNestedTooDeepIsRefused()
        {
            var body = string.Concat(Enumerable.Repeat("<d>", 70)) + "x" + string.Concat(Enumerable.Repeat("</d>", 70));

            var (_, issues) = Run(body, "{}");

            issues.Should().Contain(i => i.Code == "TemplateTooDeep");
        }

        [Fact]
        public void AnEachOverTooManyItemsIsRefused()
        {
            var items = new JsonArray();

            for (var i = 0; i < 100_001; i++)
            {
                items.Add(1);
            }

            var profile = ExampleProfile.Create(Open + "<i jr:each=\"rows\"/></r>");
            var result = ReportBuilder.Build(profile with { SchemaDocuments = new System.Collections.Generic.Dictionary<string, string>() },
                new JsonObject { ["rows"] = items });

            result.Issues.Should().Contain(i => i.Code == "TooManyItems");
        }
    }
}

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
    public sealed class ReportBuilderTests
    {
        private static JsonNode Data(string json) => JsonNode.Parse(json)!;

        private const string Good = """
            {"reference":"REF-1","submitted":"2026-11-10T09:30:00Z","reason":"Structuring",
             "transactions":[{"amount":"1250.5","currency":"GBP","country":"United Kingdom"},
                             {"amount":99,"currency":"KWD","country":"Kuwait"}]}
            """;

        [Fact]
        public void AGoodReportIsBuiltValidatedAndHashed()
        {
            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(Good));

            result.Issues.Should().BeEmpty(string.Join("; ", result.Issues.Select(i => i.Message)));
            result.Succeeded.Should().BeTrue();
            result.Sha256.Should().HaveLength(64);
            result.Xml.Should().Contain("<reference>REF-1</reference>").And.Contain("<amount>1250.50</amount>")
                .And.Contain("<amount>99.00</amount>").And.Contain("<country>GB</country>")
                .And.Contain("currency=\"KWD\"").And.Contain("<submitted>2026-11-10T09:30:00</submitted>");
            result.Xml.Should().NotContain("jr:").And.NotContain("urn:jube:regulatory-report");
        }

        [Fact]
        public void AnOptionalValueThatIsMissingIsLeftOut()
        {
            var json = Good.Replace("\"reason\":\"Structuring\",", string.Empty);

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            result.Succeeded.Should().BeTrue();
            result.Xml.Should().NotContain("reason");
        }

        [Fact]
        public void AMissingRequiredValueNamesTheDataPathAndTheTemplateLine()
        {
            var json = Good.Replace("\"reference\":\"REF-1\",", string.Empty);

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            result.Succeeded.Should().BeFalse();
            result.Xml.Should().BeNull();
            var issue = result.Issues.Should().ContainSingle().Subject;
            (issue.Stage, issue.Code, issue.Path).Should().Be((ReportIssueStage.Mapping, "MissingRequired", "$.reference"));
            issue.Line.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ARequiredValueInsideAListNamesTheItem()
        {
            var json = Good.Replace("\"amount\":99,", string.Empty);

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            result.Issues.Should().ContainSingle().Which.Path.Should().Be("$.transactions[1].amount");
        }

        [Fact]
        public void AnUnknownCodeIsReportedNotGuessed()
        {
            var json = Good.Replace("Kuwait", "Narnia");

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            var issue = result.Issues.Should().ContainSingle().Subject;
            (issue.Code, issue.Path).Should().Be(("UnknownCode", "$.transactions[1].country"));
        }

        [Fact]
        public void ABadNumberOrDateIsReported()
        {
            var json = Good.Replace("1250.5", "lots").Replace("2026-11-10T09:30:00Z", "yesterday");

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            result.Issues.Select(i => i.Code).Should().BeEquivalentTo("NotANumber", "NotADate");
        }

        [Fact]
        public void AnEmptyRequiredListIsReported()
        {
            var json = """{"reference":"R","submitted":"2026-11-10T09:30:00Z","transactions":[]}""";

            var result = ReportBuilder.Build(ExampleProfile.Create(), Data(json));

            result.Issues.Should().Contain(i => i.Code == "MissingRequired" && i.Path == "$.transactions");
        }

        [Fact]
        public void HostileTextIsEscapedAndNeverBecomesMarkup()
        {
            var data = Data(Good);
            data["reason"] = "</reason><evil a='1'/>&amp;";

            var result = ReportBuilder.Build(ExampleProfile.Create(), data);

            result.Succeeded.Should().BeTrue();
            result.Xml.Should().Contain("&lt;/reason&gt;&lt;evil").And.NotContain("<evil");
        }

        [Fact]
        public void TheSchemaNamesTheFieldThatBreaksIt()
        {
            var strict = ExampleProfile.Schema.Replace("name=\"amount\" type=\"xs:decimal\"",
                "name=\"amount\" type=\"xs:integer\"");

            var result = ReportBuilder.Build(ExampleProfile.Create(schema: strict), Data(Good));

            result.Succeeded.Should().BeFalse();
            var issue = result.Issues.First(i => i.Stage == ReportIssueStage.Schema);
            issue.Path.Should().Be("/report/transaction/amount");
            issue.Line.Should().BeGreaterThan(0);
            issue.Position.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ExternallyProducedXmlCanBeValidatedAgainstTheProfile()
        {
            var good = ReportBuilder.Build(ExampleProfile.Create(), Data(Good)).Xml!;

            ReportBuilder.Validate(ExampleProfile.Create(), good).Issues.Should().BeEmpty();
            ReportBuilder.Validate(ExampleProfile.Create(), good.Replace("<reference>", "<ref>")
                .Replace("</reference>", "</ref>")).Issues.Should().NotBeEmpty();
            ReportBuilder.Validate(ExampleProfile.Create(), "<report").Issues
                .Should().ContainSingle().Which.Code.Should().Be("XmlInvalid");
        }

        [Fact]
        public void AProfileWithoutASchemaOrWithABrokenOneSaysSo()
        {
            var none = ExampleProfile.Create() with
            {
                SchemaDocuments = new System.Collections.Generic.Dictionary<string, string>()
            };

            ReportBuilder.Build(none, Data(Good)).Issues.Should().ContainSingle(i => i.Code == "NoSchema");
            ReportBuilder.Build(ExampleProfile.Create(schema: "<xs:schema"), Data(Good)).Issues
                .Should().Contain(i => i.Stage == ReportIssueStage.Profile && i.Code == "SchemaInvalid");
        }

        [Fact]
        public void ABrokenOrDoctypeTemplateIsRefusedBeforeAnythingIsRendered()
        {
            ReportBuilder.Build(ExampleProfile.Create(template: "<report"), Data(Good)).Issues
                .Should().ContainSingle(i => i.Code == "TemplateInvalid");

            ReportBuilder.Build(ExampleProfile.Create(template:
                    "<!DOCTYPE report [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><report>&x;</report>"), Data(Good))
                .Issues.Should().ContainSingle(i => i.Code == "TemplateInvalid");
        }

        [Fact]
        public void NoDataIsReportedNotThrown()
        {
            ReportBuilder.Build(ExampleProfile.Create(), null).Issues.Should().ContainSingle(i => i.Code == "NoData");
        }

        [Fact]
        public void TheSameDataAlwaysGivesTheSameBytesAndHash()
        {
            var one = ReportBuilder.Build(ExampleProfile.Create(), Data(Good));
            var two = ReportBuilder.Build(ExampleProfile.Create(), Data(Good));

            (one.Xml, one.Sha256).Should().Be((two.Xml, two.Sha256));
        }
    }
}

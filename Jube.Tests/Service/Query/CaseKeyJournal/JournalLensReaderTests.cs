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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalLensReaderTests
    {
        private static readonly JObject archived = JObject.Parse("""
            {
              "payload": { "AccountId": "A1", "Amount": 120.5, "Channel": "App", "Fresh": true, "Basket": { "x": 1 } },
              "activation": {
                "Velocity": { "forced": false, "visible": true },
                "Sanctioned": { "forced": true, "visible": true },
                "Internal": { "forced": false, "visible": false }
              },
              "abstraction": { "Volume": 123.45, "Label": "x", "Nested": { "a": 1 } },
              "abstractionCalculation": { "Ratio": 0.5 },
              "ttlCounter": { "All": 3 },
              "dictionary": { "Watch": 1 },
              "exhaustiveAdaptation": { "StructNet": 0.91 },
              "advancedAnalyticsAdaptation": { "Gradient": 0.4 },
              "sanction": { "Fuzzy": 0.82 },
              "httpAdaptation": { "Bureau": 640 }
            }
            """);

        private static Dictionary<string, object?> Read(string lens, out JournalColumnSet columns,
            params GetCaseWorkflowXPathByCaseWorkflowIdQueryDto[] xPaths)
        {
            var row = new Dictionary<string, object?>();
            columns = new JournalColumnSet();

            JournalLensReader.Read(archived, lens, row, columns, xPaths);

            return row;
        }

        [Fact]
        public void OnlyVisibleActivationsAreNamedAndAForcedOneSaysSo()
        {
            JournalLensReader.VisibleActivations(archived).Should().BeEquivalentTo("Velocity", "Sanctioned");

            var row = Read(JournalLensReader.Rules, out var columns);

            row["rule:Velocity"].Should().Be("fired");
            row["rule:Sanctioned"].Should().Be("forced");
            row.Should().NotContainKey("rule:Internal");
            columns.All.Select(column => column.Name).Should().BeEquivalentTo("rule:Velocity", "rule:Sanctioned");
        }

        [Fact]
        public void FeaturesGatherEveryFacetTheRulesSawWithTheirTypes()
        {
            var row = Read(JournalLensReader.Features, out var columns);

            row["abstraction:Volume"].Should().Be(123.45);
            row["abstraction:Label"].Should().Be("x");
            row["calculation:Ratio"].Should().Be(0.5);
            row["counter:All"].Should().Be(3L);
            row["dictionary:Watch"].Should().Be(1L);
            row.Should().NotContainKey("abstraction:Nested");
            columns.All.Single(column => column.Name == "abstraction:Volume").Type.Should().Be("number");
            columns.All.Single(column => column.Name == "counter:All").Group.Should().Be("counter");
        }

        [Fact]
        public void ModelsAndSanctionsKeepTheirOwnFacets()
        {
            var models = Read(JournalLensReader.Models, out _);
            var sanctions = Read(JournalLensReader.Sanctions, out _);

            models["exhaustive:StructNet"].Should().Be(0.91);
            models["advanced:Gradient"].Should().Be(0.4);
            models.Should().NotContainKey("sanction:Fuzzy");
            sanctions["sanction:Fuzzy"].Should().Be(0.82);
            sanctions["http:Bureau"].Should().Be(640L);
        }

        [Fact]
        public void PayloadReadsOnlyTheXPathsItIsGivenAndLeavesObjectsOut()
        {
            var row = Read(JournalLensReader.Payload, out var columns,
                XPath("Amount", "$.payload.Amount"), XPath("Basket", "$.payload.Basket"),
                XPath("Missing", "$.payload.Nope"));

            row["Amount"].Should().Be(120.5);
            row["Basket"].Should().BeNull();
            row["Missing"].Should().BeNull();
            row.Should().NotContainKey("Channel");
            columns.All.Single(column => column.Name == "Amount").Type.Should().Be("number");
        }

        private static GetCaseWorkflowXPathByCaseWorkflowIdQueryDto Formatted(string name, string path,
            string? pattern = "^Ap", bool bold = false)
        {
            return new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
            {
                Name = name,
                XPath = path,
                ConditionalRegularExpressionFormatting = pattern is not null,
                RegularExpression = pattern,
                ConditionalFormatBackColor = "#111111",
                ConditionalFormatForeColor = "#222222",
                ForeRowColorScope = true,
                BoldLineMatched = bold,
                BoldLineFormatBackColor = "#333333",
                BoldLineFormatForeColor = "#444444"
            };
        }

        private static Dictionary<string, object?> Formatting(
            IReadOnlyDictionary<string, string>? reference, params GetCaseWorkflowXPathByCaseWorkflowIdQueryDto[] xPaths)
        {
            var row = new Dictionary<string, object?>();

            JournalLensReader.Format(archived, xPaths, row, reference);

            return row;
        }

        [Fact]
        public void PayloadCarriesTheConfiguredRoleAndDrill()
        {
            Read(JournalLensReader.Payload, out var columns,
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                {
                    Name = "Channel",
                    XPath = "$.payload.Channel",
                    RoleId = 2,
                    Drill = true
                });

            var column = columns.All.Single();

            column.RoleId.Should().Be(2);
            column.Drill.Should().BeTrue();
        }

        [Fact]
        public void APayloadReadNoLongerCarriesFormatting()
        {
            var row = Read(JournalLensReader.Payload, out _, Formatted("Channel", "$.payload.Channel", bold: true));

            row.Should().NotContainKey("CellFormat");
            row.Should().NotContainKey("BoldLine");
        }

        [Fact]
        public void AMatchingPatternFormatsTheCellAndSaysWhichPatternDidIt()
        {
            var row = Formatting(null, Formatted("Channel", "$.payload.Channel"));

            var format = row["CellFormat"].Should().BeAssignableTo<List<Dictionary<string, object?>>>().Which.Single();

            format["CellFormatKey"].Should().Be("Channel");
            format["CellFormatPattern"].Should().Be("^Ap");
            format["CellFormatBackColor"].Should().Be("#111111");
            format["CellFormatForeRow"].Should().Be(true);
        }

        [Fact]
        public void APatternThatDoesNotMatchOrIsBrokenFormatsNothing()
        {
            Formatting(null, Formatted("Channel", "$.payload.Channel", "^Web"))
                .Should().NotContainKey("CellFormat");

            Formatting(null, Formatted("Channel", "$.payload.Channel", "(["))
                .Should().NotContainKey("CellFormat");
        }

        [Fact]
        public void TheBoldLineMatchesOnlyWhenTheRowSharesTheCaseValue()
        {
            var channel = Formatted("Channel", "$.payload.Channel", null, true);

            var same = Formatting(new Dictionary<string, string> { ["Channel"] = "App" }, channel);
            var other = Formatting(new Dictionary<string, string> { ["Channel"] = "Web" }, channel);
            var none = Formatting(null, channel);

            var line = same["BoldLine"].Should().BeAssignableTo<Dictionary<string, object?>>().Which;

            line["BoldLineKey"].Should().Be("Channel");
            line["BoldLineValue"].Should().Be("App");
            line["BoldLineFormatBackColor"].Should().Be("#333333");
            other.Should().NotContainKey("BoldLine");
            none.Should().NotContainKey("BoldLine");
        }

        [Fact]
        public void TheBoldLineComparesNumbersAndBooleansAsText()
        {
            var amount = Formatted("Amount", "$.payload.Amount", null, true);
            var fresh = Formatted("Fresh", "$.payload.Fresh", null, true);

            Formatting(new Dictionary<string, string> { ["Amount"] = "120.5" }, amount)
                .Should().ContainKey("BoldLine");
            Formatting(new Dictionary<string, string> { ["Fresh"] = "true" }, fresh)
                .Should().ContainKey("BoldLine");
            Formatting(new Dictionary<string, string> { ["Amount"] = "120" }, amount)
                .Should().NotContainKey("BoldLine");
        }

        [Fact]
        public void TheReferenceIsTakenFromTheCaseOnlyForBoldLineXPaths()
        {
            var caseJson = JObject.Parse("""{ "payload": { "Channel": "App", "Amount": 5, "Basket": { "x": 1 } } }""");

            var reference = JournalLensReader.BoldReference(caseJson,
            [
                Formatted("Channel", "$.payload.Channel", null, true),
                Formatted("Amount", "$.payload.Amount", null),
                Formatted("Basket", "$.payload.Basket", null, true),
                Formatted("Missing", "$.payload.Nope", null, true)
            ]);

            reference.Should().BeEquivalentTo(new Dictionary<string, string> { ["Channel"] = "App" });
            JournalLensReader.BoldReference(null, [Formatted("Channel", "$.payload.Channel", null, true)])
                .Should().BeEmpty();
        }

        [Fact]
        public void PresenceSaysWhichLensesHaveAnythingToShow()
        {
            var presence = JournalLensReader.Presence(archived);

            presence.Should().OnlyContain(pair => pair.Value);

            var bare = JournalLensReader.Presence(JObject.Parse("{\"payload\":{}}"));

            bare.Values.Should().OnlyContain(present => !present);
        }

        [Theory]
        [InlineData("payload", true)]
        [InlineData("rules", true)]
        [InlineData("features", true)]
        [InlineData("models", true)]
        [InlineData("sanctions", true)]
        [InlineData("Payload", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("everything", false)]
        public void OnlyTheFiveLensesAreAccepted(string? lens, bool expected)
        {
            JournalLensReader.IsLens(lens).Should().Be(expected);
        }

        [Fact]
        public void TheKeyIsMatchedAsATextAndAsAnumberSoANumericIdentifierStillFinds()
        {
            GetCaseKeyJournalRowsQuery.Containment("AccountId", "123").Should()
                .Be("{\"payload\":{\"AccountId\":\"123\"}}");
            GetCaseKeyJournalRowsQuery.TypedContainment("AccountId", "123").Should()
                .Be("{\"payload\":{\"AccountId\":123}}");
            GetCaseKeyJournalRowsQuery.TypedContainment("AccountId", "12.5").Should()
                .Be("{\"payload\":{\"AccountId\":12.5}}");
            GetCaseKeyJournalRowsQuery.TypedContainment("Flag", "true").Should()
                .Be("{\"payload\":{\"Flag\":true}}");
            GetCaseKeyJournalRowsQuery.TypedContainment("AccountId", "A1").Should()
                .Be("{\"payload\":{\"AccountId\":\"A1\"}}");
        }

        [Fact]
        public void AKeyWithQuotesOrBracesCannotEscapeTheContainmentDocument()
        {
            var document = JObject.Parse(GetCaseKeyJournalRowsQuery.Containment("a\"}}, \"x\":{\"", "v\"}"));

            document.Properties().Select(property => property.Name).Should().BeEquivalentTo("payload");
            ((JObject)document["payload"]!).Properties().Should().ContainSingle();
        }

        private static GetCaseWorkflowXPathByCaseWorkflowIdQueryDto XPath(string name, string xPath)
        {
            return new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = name, XPath = xPath };
        }
    }
}

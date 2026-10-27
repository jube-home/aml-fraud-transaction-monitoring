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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Models;
using Jube.Service.Charts.Datasource.Template;
using Xunit;

namespace Jube.Test.Charting.Datasource
{
    [Trait("Category", "Unit")]
    public sealed class DatasourceTemplateTests
    {
        private static DatasourceFrame Frame(params object?[][] rows)
        {
            return new DatasourceFrame(
                [
                    new DatasourceColumn("region", 1, "text", SeriesDataType.Text),
                    new DatasourceColumn("total", 2, "numeric", SeriesDataType.Decimal),
                    new DatasourceColumn("order total", 3, "integer", SeriesDataType.Integer)
                ], rows);
        }

        private static readonly object?[][] Two = [["North", 10.5, 3], ["South", 20.0, 4]];

        private static string Render(string template, params object?[][] rows)
        {
            return DatasourceTemplate.Render(template, Frame(rows.Length == 0 ? Two : rows), false);
        }

        private static DatasourceChartException Fails(string template)
        {
            var act = () => DatasourceTemplate.Render(template, Frame(Two), false);

            return act.Should().Throw<DatasourceChartException>().Which;
        }

        [Fact]
        public void InterpolatesAnEscapedValue()
        {
            Render("<p>[@ rows[0].region @]</p>", ["<b>x</b>", 1.0, 1]).Should()
                .Be("<p>&lt;b&gt;x&lt;/b&gt;</p>");
        }

        [Fact]
        public void LoopsOverRowsWithAnIndex()
        {
            Render("<ul><li v-for=\"(row, i) in rows\">[@ i + 1 @]. [@ row.region @]</li></ul>").Should()
                .Be("<ul><li>1. North</li><li>2. South</li></ul>");
        }

        [Fact]
        public void ReadsAColumnWithASpaceInItsNameByBracket()
        {
            Render("<span>[@ rows[1]['order total'] @]</span>").Should().Be("<span>4</span>");
        }

        [Fact]
        public void ChoosesTheFirstTrueBranch()
        {
            Render("<div v-for=\"row in rows\"><b v-if=\"row.total > 15\">high</b><i v-else>low</i></div>")
                .Should().Be("<div><i>low</i></div><div><b>high</b></div>");
        }

        [Fact]
        public void FollowsAnElseIfChain()
        {
            Render("<p v-for=\"row in rows\"><b v-if=\"row.total > 30\">a</b><i v-else-if=\"row.total > 15\">b</i>" +
                   "<em v-else>c</em></p>").Should().Be("<p><em>c</em></p><p><i>b</i></p>");
        }

        [Fact]
        public void BindsAClassFromAConditional()
        {
            Render("<td v-for=\"row in rows\" class=\"[@ row.total > 15 ? 'text-error' : 'text-base-content' @]\">[@ row.region @]</td>")
                .Should().Be("<td class=\"text-base-content\">North</td><td class=\"text-error\">South</td>");
        }

        [Fact]
        public void MixesTextAndTokensInOneAttribute()
        {
            Render("<td v-for=\"row in rows\" class=\"badge [@ row.region | lower @]-x\">y</td>")
                .Should().Be("<td class=\"badge north-x\">y</td><td class=\"badge south-x\">y</td>");
        }

        [Fact]
        public void RefusesTheOldColonBindingBecauseTokensReplaceIt()
        {
            Fails("<div :class=\"x\">a</div>").Message.Should().Contain(":class");
        }

        [Fact]
        public void IgnoresCaseInNamesMembersAndFilters()
        {
            Render("[@ ROWCOUNT @] [@ Rows[0].REGION | UPPER @] [@ rows.LENGTH @]").Should().Be("2 NORTH 2");
        }

        [Fact]
        public void RefusesAnUnclosedAttributeToken()
        {
            Fails("<div title=\"[@ rowCount\">a</div>").Message.Should().Contain("[@");
        }

        [Fact]
        public void FormatsANumberHeldAsTextButNeverComparesTextAsANumber()
        {
            Render("<p>[@ '12.5' | number(2) @]</p>").Should().Be("<p>12.50</p>");
            Render("<p>[@ '007' == '7' @]</p>").Should().Be("<p>false</p>");
        }

        [Fact]
        public void KeepsAStaticClass()
        {
            Render("<div class=\"badge badge-soft\">x</div>").Should().Be("<div class=\"badge badge-soft\">x</div>");
        }

        [Fact]
        public void OmitsABoundAttributeThatIsFalseOrNull()
        {
            Render("<div title=\"[@ null @]\" class=\"[@ false @]\">x</div>").Should().Be("<div>x</div>");
        }

        [Fact]
        public void RendersATemplateWrapperWithoutATag()
        {
            Render("<template v-for=\"row in rows\"><i>[@ row.region @]</i></template>").Should()
                .Be("<i>North</i><i>South</i>");
        }

        [Fact]
        public void RendersVoidTagsWithoutAClosingTag()
        {
            Render("a<br>b<hr/>").Should().Be("a<br>b<hr>");
        }

        [Fact]
        public void ExposesTheRowCountAndTruncation()
        {
            DatasourceTemplate.Render("[@ rowCount @] [@ truncated @]", Frame(Two), true).Should().Be("2 true");
        }

        [Fact]
        public void ExposesColumnNames()
        {
            Render("<span v-for=\"c in columns\">[@ c.name @];</span>").Should()
                .Be("<span>region;</span><span>total;</span><span>order total;</span>");
        }

        [Theory]
        [InlineData("[@ 1 + 2 * 3 @]", "7")]
        [InlineData("[@ (1 + 2) * 3 @]", "9")]
        [InlineData("[@ 7 % 4 @]", "3")]
        [InlineData("[@ 10 / 4 @]", "2.5")]
        [InlineData("[@ 1 / 0 @]", "")]
        [InlineData("[@ -5 + 2 @]", "-3")]
        [InlineData("[@ 'a' + 'b' @]", "ab")]
        [InlineData("[@ 1 < 2 && 2 < 3 @]", "true")]
        [InlineData("[@ 1 > 2 || 3 > 2 @]", "true")]
        [InlineData("[@ !(1 == 1) @]", "false")]
        [InlineData("[@ 'x' != 'y' @]", "true")]
        [InlineData("[@ 1 == 1.0 @]", "true")]
        [InlineData("[@ null @]", "")]
        [InlineData("[@ missing @]", null)]
        [InlineData("[@ rows.length @]", "2")]
        [InlineData("[@ rows[5].region @]", "")]
        [InlineData("[@ rows[0].nothing.deeper @]", "")]
        [InlineData("[@ 'abc'.length @]", "3")]
        [InlineData("[@ 0 ? 'y' : 'n' @]", "n")]
        [InlineData("[@ '' || 'fallback' @]", "fallback")]
        public void EvaluatesExpressions(string template, string? expected)
        {
            if (expected is null)
            {
                Fails(template).Message.Should().Contain("missing");

                return;
            }

            Render(template).Should().Be(expected);
        }

        [Fact]
        public void FormatsWithFiltersUsingTheCurrentCulture()
        {
            var previous = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentUICulture = new CultureInfo("en-GB");

            try
            {
                Render("[@ 12500.5 | number(2) @]|[@ 0.256 | percent @]|[@ 12500.5 | currency('GBP') @]")
                    .Should().Be("12,500.50|25.6%|GBP 12,500.50");
                Render("[@ '2026-10-20T10:30:00Z' | date @]|[@ '2026-10-20T10:30:00Z' | dateTime @]")
                    .Should().Be("20 Oct 2026|20 Oct 2026 10:30");
                Render("[@ 'abc' | upper @][@ 'ABC' | lower @][@ 'abcdef' | truncate(3) @]")
                    .Should().Be("ABCabcabc\u2026");
                Render("[@ 2.5 | round @][@ 1.234 | round(2) @]").Should().Be("31.23");
                Render("[@ null | default('none') @]|[@ 0 | default('none') @]").Should().Be("none|0");
                Render("[@ rows | join @]").Should().NotBeEmpty();
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public void FormatsNumbersInPolish()
        {
            var previous = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

            try
            {
                System.Net.WebUtility.HtmlDecode(Render("[@ 1234.5 | number(1) @]")).Replace('\u00a0', ' ').Should().Be("1 234,5");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public void IgnoresComments()
        {
            Render("a<!-- hidden [@ nope @] -->b").Should().Be("ab");
        }

        [Fact]
        public void DecodesEntitiesBeforeEscapingSoTheyAreNotDoubled()
        {
            Render("<p>a &amp; b &lt;</p>").Should().Be("<p>a &amp; b &lt;</p>");
        }

        [Fact]
        public void DropsWhitespaceOnlyLinesBetweenElements()
        {
            Render("<ul>\n  <li>a</li>\n  <li>b</li>\n</ul>").Should().Be("<ul><li>a</li><li>b</li></ul>");
        }

        [Theory]
        [InlineData("<script>alert(1)</script>", "script")]
        [InlineData("<a href=\"x\">y</a>", "a")]
        [InlineData("<img src=x>", "img")]
        [InlineData("<iframe></iframe>", "iframe")]
        [InlineData("<style></style>", "style")]
        [InlineData("<svg></svg>", "svg")]
        public void RefusesTagsOutsideTheWhitelist(string template, string tag)
        {
            Fails(template).Message.Should().Contain("<" + tag + ">").And.Contain("not allowed");
        }

        [Theory]
        [InlineData("<div onclick=\"x()\">a</div>", "onclick")]
        [InlineData("<div style=\"color:red\">a</div>", "style")]
        [InlineData("<div id=\"x\">a</div>", "id")]
        [InlineData("<div href=\"x\">a</div>", "href")]
        [InlineData("<div :onclick=\"1\">a</div>", "onclick")]
        [InlineData("<div @click=\"x\">a</div>", "@click")]
        [InlineData("<td data-Bad=\"1\">a</td>", "data-Bad")]
        [InlineData("<template class=\"x\">a</template>", "class")]
        public void RefusesAttributesOutsideTheWhitelist(string template, string attribute)
        {
            Fails(template).Message.Should().Contain(attribute + "\"");
        }

        [Theory]
        [InlineData("<div v-html=\"x\">a</div>")]
        [InlineData("<div v-on:click=\"x\">a</div>")]
        [InlineData("<div v-bind:class=\"x\">a</div>")]
        [InlineData("<div v-else>a</div>")]
        [InlineData("<div v-else-if=\"true\">a</div>")]
        [InlineData("<div v-if=\"true\" v-for=\"r in rows\">a</div>")]
        [InlineData("<div v-if=\"true\" v-else>a</div>")]
        public void RefusesDirectivesThatAreNotValidHere(string template)
        {
            Fails(template).Message.Should().Contain("directive");
        }

        [Fact]
        public void RefusesAnElseAfterAnUnrelatedElement()
        {
            Fails("<p v-if=\"true\">a</p><b>x</b><i v-else>b</i>").Message.Should().Contain("v-else");
        }

        [Theory]
        [InlineData("<p v-for=\"rows\">a</p>")]
        [InlineData("<p v-for=\"row of rows\">a</p>")]
        [InlineData("<p v-for=\"1 in rows\">a</p>")]
        public void RefusesAMalformedLoop(string template)
        {
            Fails(template).Message.Should().Contain("v-for");
        }

        [Theory]
        [InlineData("[@ rows[ @]")]
        [InlineData("[@ 1 + @]")]
        [InlineData("[@ (1 @]")]
        [InlineData("[@ 'open @]")]
        [InlineData("[@ 1 @ 2 @]")]
        [InlineData("[@ a b @]")]
        [InlineData("[@ rows.1 @]")]
        [InlineData("[@ rows[0]( @]")]
        public void RefusesAMalformedExpression(string template)
        {
            var failure = Fails(template);

            failure.Message.Should().StartWith("Template line 1, column 1:");
        }

        [Theory]
        [InlineData("[@ constructor @]", "constructor")]
        [InlineData("[@ window @]", "window")]
        [InlineData("[@ row.region @]", "row")]
        [InlineData("<p v-for=\"r in rows\">[@ row @]</p>", "row")]
        public void RefusesAnUnknownName(string template, string name)
        {
            Fails(template).Message.Should().Contain("\"" + name + "\" is not known");
        }

        [Fact]
        public void KeepsALoopVariableOutOfItsSiblings()
        {
            Fails("<p v-for=\"r in rows\">[@ r.region @]</p><p>[@ r.region @]</p>").Message.Should()
                .Contain("\"r\" is not known");
        }

        [Theory]
        [InlineData("[@ 1 | nothing @]", "does not exist")]
        [InlineData("[@ 1 | upper(2) @]", "does not take")]
        [InlineData("[@ 1 | currency @]", "does not take")]
        public void RefusesAnUnknownFilterOrWrongArguments(string template, string reason)
        {
            Fails(template).Message.Should().Contain(reason);
        }

        [Fact]
        public void ReportsTheLineAndColumnOfTheProblem()
        {
            Fails("<ul>\n  <li>ok</li>\n  <li>[@ nope @]</li>\n</ul>").Message.Should()
                .Contain("line 3, column 7");
        }

        [Theory]
        [InlineData("<p>a")]
        [InlineData("<div><p>a</div>")]
        [InlineData("</p>")]
        [InlineData("<p>[@ rows[0].region </p>")]
        [InlineData("<p class=\"x>a</p>")]
        public void RefusesBrokenStructure(string template)
        {
            var act = () => DatasourceTemplate.Validate(template);

            act.Should().Throw<DatasourceChartException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \n  ")]
        public void RefusesAnEmptyTemplate(string? template)
        {
            var act = () => DatasourceTemplate.Validate(template);

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("empty");
        }

        [Fact]
        public void RefusesATemplateOverTheLengthLimit()
        {
            var act = () => DatasourceTemplate.Validate(new string('a', TemplateLimits.MaximumTextLength + 1));

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("longer than");
        }

        [Fact]
        public void RefusesTemplatesNestedTooDeep()
        {
            var text = string.Concat(Enumerable.Repeat("<div>", TemplateLimits.MaximumElementDepth + 2));

            var act = () => DatasourceTemplate.Validate(text);

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("nests deeper");
        }

        [Fact]
        public void RefusesExpressionsNestedTooDeep()
        {
            var text = "[@ " + string.Concat(Enumerable.Repeat("(", 80)) + "1" +
                       string.Concat(Enumerable.Repeat(")", 80)) + " @]";

            var act = () => DatasourceTemplate.Validate(text);

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("nests deeper");
        }

        [Fact]
        public void StopsALoopOverLoopThatWouldRunForever()
        {
            var rows = Enumerable.Range(0, 400).Select(i => new object?[] { "r" + i, 1.0, 1 }).ToArray();
            var template = "<p v-for=\"a in rows\"><i v-for=\"b in rows\">x</i></p>";

            var act = () => DatasourceTemplate.Render(template, Frame(rows), false);

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("steps");
        }

        [Fact]
        public void StopsOutputOverTheLimit()
        {
            var rows = Enumerable.Range(0, 2000).Select(_ => new object?[] { new string('z', 700), 1.0, 1 }).ToArray();
            var template = "<p v-for=\"r in rows\">[@ r.region @]</p>";

            var act = () => DatasourceTemplate.Render(template, Frame(rows), false);

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("characters of output");
        }

        [Fact]
        public void NeverEmitsAnythingExecutableFromHostileData()
        {
            var html = Render("<p title=\"[@ row.region @]\" v-for=\"row in rows\">[@ row.region @]</p>",
                ["\"><script>alert(1)</script>", 1.0, 1]);

            html.Should().NotContain("<script");
            html.Should().Contain("&lt;script&gt;").And.Contain("&quot;");
        }

        [Fact]
        public void TreatsATooBigNumberAsEmpty()
        {
            Render("[@ 99999999999999999999 * 99999999999999999999 * 99999999999999999999 @]").Should().BeEmpty();
        }

        [Fact]
        public void RendersEveryCellOfATableWithTheColumnsAsHeaders()
        {
            var html = Render("<table><thead><tr><th v-for=\"c in columns\">[@ c.name @]</th></tr></thead>" +
                              "<tbody><tr v-for=\"row in rows\"><td>[@ row.region @]</td><td>[@ row.total | number(1) @]</td></tr></tbody></table>");

            html.Should().Contain("<th>region</th>").And.Contain("<td>North</td>");
        }

        [Fact]
        public void AllowsAriaAndDataAttributes()
        {
            Render("<div aria-label=\"x\" data-kind=\"y\" role=\"status\">z</div>").Should()
                .Be("<div aria-label=\"x\" data-kind=\"y\" role=\"status\">z</div>");
        }

        [Fact]
        public void IsRegisteredAsAnHtmlKindWithNoRoles()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Template);

            kind.Markup.Should().Be(DatasourceMarkupKind.Html);
            kind.Roles.Should().BeEmpty();
            ((int)DatasourceDisplayKind.Template).Should().Be(29);
            new HashSet<string>(TemplateVocabulary.Tags).Should().NotContain(["script", "style", "iframe", "a", "img"]);
        }
    }
}

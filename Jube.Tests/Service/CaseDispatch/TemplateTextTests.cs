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
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Xunit;

namespace Jube.Test.Service.CaseDispatch
{
    [Trait("Category", "Unit")]
    public sealed class TemplateTextTests
    {
        private static readonly Dictionary<string, object?> Roots = new(StringComparer.OrdinalIgnoreCase)
        {
            ["rows"] = new List<object?>
            {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "A <b>&</b> B", ["amount"] = 1234.5m, ["id"] = "x y/z"
                }
            },
            ["rowCount"] = 1,
            ["truncated"] = false
        };

        [Fact]
        public void RendersTokensAndFiltersFromTheRowsWithTheChosenEncoding()
        {
            TemplateText.Render("Hi [@ rows[0].name @], [@ rows[0].amount | number(1) @]", Roots, TemplateText.Plain)
                .Should().Be("Hi A <b>&</b> B, 1,234.5");
            TemplateText.Render("[@ rows[0].name @]", Roots, TemplateText.Html).Should().Be("A &lt;b&gt;&amp;&lt;/b&gt; B");
            TemplateText.Render("[@ rows[0].id @]", Roots, TemplateText.Url).Should().Be("x%20y%2Fz");
        }

        [Fact]
        public void RefusesANameThatIsNotARootOfTheSqlDialect()
        {
            var act = () => TemplateText.Validate("[@ Payload.Amount @]");

            act.Should().Throw<DatasourceChartException>();
            TemplateText.Validate("[@ rows[0].amount @] [@ Form.reason @] [@ rowCount @]");
        }

        [Fact]
        public void RefusesAnUnclosedTokenAndTextThatIsTooLong()
        {
            var open = () => TemplateText.Validate("Total [@ rows[0].amount");
            var long_ = () => TemplateText.Validate(new string('x', TemplateText.MaximumLength + 1));

            open.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("[@");
            long_.Should().Throw<DatasourceChartException>();
        }

        [Fact]
        public void LeavesPlainTextAndAnEmptyTemplateAlone()
        {
            TemplateText.Render("No tokens here.", Roots, TemplateText.Html).Should().Be("No tokens here.");
            TemplateText.Render(null, Roots, TemplateText.Html).Should().BeEmpty();
        }

        [Fact]
        public void TreatsAMissingRowAsEmptyRatherThanAnError()
        {
            TemplateText.Render("[@ rows[5].name @]|", Roots, TemplateText.Plain).Should().Be("|");
        }

        [Fact]
        public void AcceptsTheFormSqlDialectAndRefusesTheLegacyRootsInIt()
        {
            var schema = DatasourceTemplate.CompileForm("<p>[@ rows[0].name @]</p><input name=\"a\">", TemplateDialect.FormSql);

            schema.Fields.Should().ContainSingle();

            var act = () => DatasourceTemplate.CompileForm("<p>[@ Case.Id @]</p><input name=\"a\">", TemplateDialect.FormSql);

            act.Should().Throw<DatasourceChartException>();
        }

        [Fact]
        public void DrawsAFormFromTheRowsInTheSqlDialect()
        {
            var render = DatasourceTemplate.RenderForm("<p>[@ rows[0].name @]</p><input name=\"a\" value=\"[@ rows[0].id @]\">", Roots,
                new Dictionary<string, string?>(), false, TemplateDialect.FormSql);

            render.Html.Should().Contain("<p>A &lt;b&gt;&amp;&lt;/b&gt; B</p>").And.Contain("value=\"x y/z\"");
        }
    }
}

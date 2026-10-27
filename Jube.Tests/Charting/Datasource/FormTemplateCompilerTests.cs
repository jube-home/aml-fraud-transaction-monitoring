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
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Xunit;

namespace Jube.Test.Charting.Datasource
{
    [Trait("Category", "Unit")]
    public sealed class FormTemplateCompilerTests
    {
        private static FormRender Render(string template, params (string Name, string Value)[] values)
        {
            return DatasourceTemplate.RenderForm(template, new Dictionary<string, object?>(),
                values.ToDictionary(v => v.Name, v => (string?)v.Value), false);
        }

        private static FormSubmissionResult Submit(string template, params (string Name, string Value)[] values)
        {
            return DatasourceTemplate.ValidateSubmission(template, new Dictionary<string, object?>(),
                values.ToDictionary(v => v.Name, v => (string?)v.Value));
        }

        private static DatasourceChartException Failing(string template)
        {
            var act = () => DatasourceTemplate.CompileForm(template);

            return act.Should().Throw<DatasourceChartException>().Which;
        }

        [Fact]
        public void CompilesTheFieldsOfAForm()
        {
            var schema = DatasourceTemplate.CompileForm(
                "<form><label for=\"reason\">Why</label><input name=\"reason\"><input name=\"amount\" type=\"number\"><textarea name=\"notes\"></textarea><input name=\"sure\" type=\"checkbox\"></form>");

            schema.Fields.Select(f => f.Name).Should().Equal("reason", "amount", "notes", "sure");
            schema.Fields.Select(f => f.Kind).Should().Equal(FormFieldKind.Text, FormFieldKind.Number,
                FormFieldKind.TextArea, FormFieldKind.Checkbox);
        }

        [Fact]
        public void ReadsTheOptionsOfASelect()
        {
            var schema = DatasourceTemplate.CompileForm(
                "<select name=\"outcome\"><option value=\"a\">A</option><template v-if=\"Case.Id\"><option value=\"b\">B</option></template></select>");

            schema.Fields.Single().Options.Should().Equal("a", "b");
        }

        [Fact]
        public void GathersRadioButtonsIntoOneFieldWithItsOptions()
        {
            var schema = DatasourceTemplate.CompileForm(
                "<input type=\"radio\" name=\"risk\" value=\"low\"><input type=\"radio\" name=\"risk\" value=\"high\">");

            schema.Fields.Should().ContainSingle().Which.Options.Should().Equal("low", "high");
        }

        [Fact]
        public void TreatsFieldNamesAsCaseInsensitive()
        {
            Failing("<input name=\"Reason\"><input name=\"reason\">").Message.Should().Contain("more than once");
        }

        [Fact]
        public void RefusesAFieldWithoutAFixedName()
        {
            Failing("<input type=\"text\">").Message.Should().Contain("needs a name");
            Failing("<input name=\"[@ Case.Id @]\">").Message.Should().Contain("needs a name");
            Failing("<input name=\"1st\">").Message.Should().Contain("must start with a letter");
        }

        [Fact]
        public void RefusesAFieldInsideALoop()
        {
            Failing("<div v-for=\"a in Case.Activations\"><input name=\"x\"></div>").Message.Should()
                .Contain("v-for");
        }

        [Fact]
        public void RefusesATypeOutsideTheList()
        {
            Failing("<input name=\"x\" type=\"hidden\">").Message.Should().Contain("not allowed");
            Failing("<input name=\"x\" type=\"password\">").Message.Should().Contain("not allowed");
        }

        [Fact]
        public void RefusesASelectWithoutFixedOptions()
        {
            Failing("<select name=\"x\"></select>").Message.Should().Contain("fixed options");
            Failing("<select name=\"x\"><option v-for=\"a in Case.Activations\" value=\"1\">a</option></select>")
                .Message.Should().Contain("fixed options");
            Failing("<select name=\"x\"><option value=\"[@ Case.Id @]\">a</option></select>").Message.Should()
                .Contain("fixed options");
        }

        [Fact]
        public void RefusesALabelForAnUnknownField()
        {
            Failing("<label for=\"nope\">x</label><input name=\"y\">").Message.Should().Contain("nope");
        }

        [Fact]
        public void RefusesTheFormTagsInADatasourceTemplate()
        {
            var act = () => DatasourceTemplate.Validate("<input name=\"x\">");

            act.Should().Throw<DatasourceChartException>().Which.Message.Should().Contain("not allowed");
        }

        [Fact]
        public void RefusesIdStyleAndEventAttributesOnAField()
        {
            Failing("<input name=\"x\" id=\"y\">").Message.Should().Contain("not allowed");
            Failing("<input name=\"x\" style=\"color:red\">").Message.Should().Contain("not allowed");
            Failing("<input name=\"x\" onfocus=\"a()\">").Message.Should().Contain("not allowed");
        }

        [Fact]
        public void KnowsTheCaseAndPayloadNamesButNotTheDatasourceOnes()
        {
            DatasourceTemplate.CompileForm("<p>[@ Case.CaseKeyValue @] [@ payload.Amount @]</p><input name=\"x\">")
                .Fields.Should().ContainSingle();

            var act = () => DatasourceTemplate.CompileForm("<p>[@ rows @]</p><input name=\"x\">");

            act.Should().Throw<DatasourceChartException>();
        }

        [Fact]
        public void FillsTheValuesBackIntoTheFields()
        {
            var render = Render("<input name=\"a\"><textarea name=\"b\"></textarea><input type=\"checkbox\" name=\"c\"><input type=\"radio\" name=\"d\" value=\"x\"><input type=\"radio\" name=\"d\" value=\"y\"><select name=\"e\"><option value=\"p\">P</option><option value=\"q\">Q</option></select>",
                ("a", "<hi>"), ("b", "note"), ("c", "true"), ("d", "y"), ("e", "q"));

            render.Html.Should().Contain("value=\"&lt;hi&gt;\"").And.Contain(">note</textarea>")
                .And.Contain("name=\"c\" checked").And.Contain("value=\"y\" checked")
                .And.Contain("<option value=\"q\" selected>");
            render.Html.Should().NotContain("value=\"x\" checked");
        }

        [Fact]
        public void MarksEveryFieldDisabledWhenReadOnly()
        {
            var render = DatasourceTemplate.RenderForm("<input name=\"a\"><textarea name=\"b\"></textarea><select name=\"c\"><option value=\"1\">1</option></select>",
                new Dictionary<string, object?>(), new Dictionary<string, string?>(), true);

            render.Html.Split("disabled").Length.Should().Be(4);
        }

        [Fact]
        public void ReportsOnlyTheFieldsThatAreShown()
        {
            var render = DatasourceTemplate.RenderForm("<select name=\"reason\"><option value=\"other\">o</option><option value=\"fraud\">f</option></select><input v-if=\"Form.reason == 'other'\" name=\"detail\">",
                new Dictionary<string, object?>(), new Dictionary<string, string?> { ["reason"] = "other" }, false);

            render.VisibleFields.Should().Equal("reason", "detail");

            DatasourceTemplate.RenderForm("<select name=\"reason\"><option value=\"other\">o</option></select><input v-if=\"Form.reason == 'other'\" name=\"detail\">",
                new Dictionary<string, object?>(), new Dictionary<string, string?> { ["reason"] = "fraud" }, false)
                .VisibleFields.Should().Equal("reason");
        }

        [Fact]
        public void ReadsTheCaseAndPayloadRootsIgnoringCase()
        {
            var render = DatasourceTemplate.RenderForm("<p>[@ case.Key @]</p><input name=\"a\" value=\"[@ PAYLOAD.amount @]\">",
                new Dictionary<string, object?>
                {
                    ["Case"] = new Dictionary<string, object?> { ["Key"] = "K1" },
                    ["Payload"] = new Dictionary<string, object?> { ["Amount"] = 12 }
                }, new Dictionary<string, string?>(), false);

            render.Html.Should().Contain("<p>K1</p>").And.Contain("value=\"12\"");
        }

        [Fact]
        public void RejectsAKeyThatIsNotAFieldOfTheForm()
        {
            var result = Submit("<input name=\"a\">", ("a", "1"), ("zzz", "2"));

            result.Errors.Should().ContainSingle().Which.Should().Be(new FormFieldError("zzz", "unknown"));
        }

        [Fact]
        public void DropsTheValueOfAHiddenField()
        {
            var result = Submit("<select name=\"r\"><option value=\"other\">o</option><option value=\"fraud\">f</option></select><input v-if=\"Form.r == 'other'\" name=\"d\">",
                ("r", "fraud"), ("d", "left over"));

            result.Valid.Should().BeTrue();
            result.Accepted.Keys.Should().Equal("r");
        }

        [Fact]
        public void ChecksTheTypeOfEachValue()
        {
            Submit("<input type=\"number\" name=\"a\">", ("a", "12.5")).Valid.Should().BeTrue();
            Submit("<input type=\"number\" name=\"a\">", ("a", "twelve")).Errors.Single().Code.Should().Be("type");
            Submit("<input type=\"date\" name=\"a\">", ("a", "2026-10-26")).Valid.Should().BeTrue();
            Submit("<input type=\"date\" name=\"a\">", ("a", "26/10/2026")).Errors.Single().Code.Should().Be("type");
            Submit("<input type=\"datetime-local\" name=\"a\">", ("a", "2026-10-26T10:30")).Valid.Should().BeTrue();
            Submit("<input type=\"email\" name=\"a\">", ("a", "a@b.io")).Valid.Should().BeTrue();
            Submit("<input type=\"email\" name=\"a\">", ("a", "not an email")).Errors.Single().Code.Should().Be("type");
            Submit("<input type=\"tel\" name=\"a\">", ("a", "+44 (0) 20 7946")).Valid.Should().BeTrue();
            Submit("<input type=\"tel\" name=\"a\">", ("a", "call me")).Errors.Single().Code.Should().Be("type");
            Submit("<input type=\"checkbox\" name=\"a\">", ("a", "maybe")).Errors.Single().Code.Should().Be("type");
        }

        [Fact]
        public void EnforcesTheOptionsOfASelectAndARadioGroup()
        {
            Submit("<select name=\"a\"><option value=\"x\">x</option></select>", ("a", "y")).Errors.Single().Code
                .Should().Be("option");
            Submit("<input type=\"radio\" name=\"a\" value=\"x\"><input type=\"radio\" name=\"a\" value=\"z\">", ("a", "y"))
                .Errors.Single().Code.Should().Be("option");
            Submit("<select name=\"a\"><option value=\"x\">x</option></select>", ("a", "x")).Valid.Should().BeTrue();
        }

        [Fact]
        public void LimitsTheLengthOfText()
        {
            Submit("<input name=\"a\">", ("a", new string('x', 4001))).Errors.Single().Code.Should().Be("length");
            Submit("<textarea name=\"a\"></textarea>", ("a", new string('x', 4001))).Valid.Should().BeTrue();
        }

        [Fact]
        public void TreatsAnEmptyValueAsNotSupplied()
        {
            var result = Submit("<input type=\"number\" name=\"a\">", ("a", ""));

            result.Valid.Should().BeTrue();
            result.Accepted.Should().BeEmpty();
        }
    }
}

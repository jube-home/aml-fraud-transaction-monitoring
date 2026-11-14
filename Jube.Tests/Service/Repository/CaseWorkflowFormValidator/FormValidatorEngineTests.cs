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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator.Models;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowFormValidator
{
    [Trait("Category", "Unit")]
    public sealed class FormValidatorEngineTests
    {
        private static FormValidatorRule Rule(CaseWorkflowFormValidatorKind kind, string field, string? argument1 = null,
            string? argument2 = null, string? sql = null, string? message = null, int id = 1)
        {
            return new FormValidatorRule(id, field, kind, argument1, argument2, sql, message);
        }

        private static Task<List<FormFieldError>> Run(string form, FormValidatorRule rule,
            params (string Name, string Value)[] values)
        {
            return Run(form, [rule], new FakeSql(true), values);
        }

        private static Task<List<FormFieldError>> Run(string form, FormValidatorRule rule, FakeSql sql,
            params (string Name, string Value)[] values)
        {
            return Run(form, [rule], sql, values);
        }

        private static async Task<List<FormFieldError>> Run(string form, FormValidatorRule[] rules, FakeSql sql,
            params (string Name, string Value)[] values)
        {
            var submitted = values.ToDictionary(v => v.Name, v => (string?)v.Value);
            var roots = new Dictionary<string, object?>();
            var schema = DatasourceTemplate.CompileForm(form);
            var visible = DatasourceTemplate.RenderForm(form, roots, submitted, false).VisibleFields;
            var accepted = values.ToDictionary(v => v.Name, v => v.Value);

            return await FormValidatorEngine.ValidateAsync(schema, visible, accepted, rules, "K-1", 42,
                new FakeLookups(), sql, CancellationToken.None);
        }

        private sealed class FakeLookups : IFormValidatorLookups
        {
            public Task<bool> ListExistsAsync(string list, CancellationToken token) => Task.FromResult(true);

            public Task<bool> DictionaryExistsAsync(string dictionary, CancellationToken token) => Task.FromResult(true);

            public Task<bool> ListContainsAsync(string list, string value, CancellationToken token) =>
                Task.FromResult(list == "Countries" && value is "GB" or "FR");

            public Task<bool> DictionaryContainsAsync(string dictionary, string key, CancellationToken token) =>
                Task.FromResult(dictionary == "Risk" && key is "high" or "low");
        }

        private sealed class FakeSql(bool answer, Exception? failure = null) : IFormSqlRunner
        {
            public FakeSql(Exception failure) : this(false, failure)
            {
            }

            public IReadOnlyDictionary<string, object> Parameters { get; private set; } =
                new Dictionary<string, object>();

            public Task<bool> PassesAsync(string sql, IReadOnlyDictionary<string, object> parameters,
                CancellationToken token)
            {
                Parameters = parameters;

                return failure != null ? Task.FromException<bool>(failure) : Task.FromResult(answer);
            }
        }

        [Fact]
        public async Task RequiresAValueForARequiredRule()
        {
            var errors = await Run("<input name=\"a\">", Rule(CaseWorkflowFormValidatorKind.Required, "a"), ("b", "x"));

            errors.Should().ContainSingle().Which.Should().Be(new FormFieldError("a", "required"));

            (await Run("<input name=\"a\">", Rule(CaseWorkflowFormValidatorKind.Required, "a"), ("a", "x"))).Should().BeEmpty();
        }

        [Fact]
        public async Task RequiresACheckboxToBeTickedWhenItIsRequired()
        {
            var rule = Rule(CaseWorkflowFormValidatorKind.Required, "ok");

            (await Run("<input type=\"checkbox\" name=\"ok\">", rule, ("ok", "false"))).Should().ContainSingle();
            (await Run("<input type=\"checkbox\" name=\"ok\">", rule, ("ok", "true"))).Should().BeEmpty();
        }

        [Fact]
        public async Task RequiresAValueOnlyWhenTheOtherFieldHasTheNamedValue()
        {
            var rule = Rule(CaseWorkflowFormValidatorKind.RequiredWhen, "detail", "reason", "other");
            const string form = "<input name=\"reason\"><input name=\"detail\">";

            (await Run(form, rule, ("reason", "OTHER"))).Should().ContainSingle().Which.Code.Should().Be("required");
            (await Run(form, rule, ("reason", "fraud"))).Should().BeEmpty();
            (await Run(form, rule, ("reason", "other"), ("detail", "x"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecksANumberAgainstTheRangeAndLeavesAnEmptyBoundOpen()
        {
            const string form = "<input type=\"number\" name=\"a\">";

            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Range, "a", "1", "10"), ("a", "11"))).Should().ContainSingle()
                .Which.Code.Should().Be("range");
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Range, "a", "1", "10"), ("a", "10"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Range, "a", "1", null), ("a", "1000000"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Range, "a", null, "5"), ("a", "-4"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecksADateAgainstTheRange()
        {
            const string form = "<input type=\"date\" name=\"a\">";
            var rule = Rule(CaseWorkflowFormValidatorKind.Range, "a", "2026-01-01", "2026-12-31");

            (await Run(form, rule, ("a", "2025-12-31"))).Should().ContainSingle();
            (await Run(form, rule, ("a", "2026-06-15"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecksTheLengthInCharacters()
        {
            const string form = "<input name=\"a\">";
            var rule = Rule(CaseWorkflowFormValidatorKind.Length, "a", "2", "4");

            (await Run(form, rule, ("a", "x"))).Should().ContainSingle().Which.Code.Should().Be("length");
            (await Run(form, rule, ("a", "xxxxx"))).Should().ContainSingle();
            (await Run(form, rule, ("a", "xxx"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecksAPatternAndTreatsABrokenOneAsAFailure()
        {
            const string form = "<input name=\"a\">";

            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Pattern, "a", "^[A-Z]{2}[0-9]{4}$"), ("a", "AB1234"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Pattern, "a", "^[A-Z]{2}[0-9]{4}$"), ("a", "ab1234"))).Should().ContainSingle()
                .Which.Code.Should().Be("pattern");
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Pattern, "a", "(["), ("a", "x"))).Should().ContainSingle();
        }

        [Fact]
        public async Task CannotBeSlowedByACatastrophicPattern()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();

            var errors = await Run("<input name=\"a\">", Rule(CaseWorkflowFormValidatorKind.Pattern, "a", "^(a+)+$"),
                ("a", new string('a', 40) + "!"));

            clock.ElapsedMilliseconds.Should().BeLessThan(2000);
            errors.Should().ContainSingle();
        }

        [Fact]
        public async Task ComparesTwoFieldsByTheNamedRelation()
        {
            const string form = "<input type=\"number\" name=\"low\"><input type=\"number\" name=\"high\">";

            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Compare, "low", "lt", "high"), ("low", "9"), ("high", "10"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Compare, "low", "lt", "high"), ("low", "10"), ("high", "10"))).Should().ContainSingle()
                .Which.Code.Should().Be("compare");
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Compare, "low", "le", "high"), ("low", "10"), ("high", "10"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Compare, "low", "ne", "high"), ("low", "10"), ("high", "10"))).Should().ContainSingle();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Compare, "low", "lt", "high"), ("low", "5"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ComparesDatesAsDatesAndNumbersAsNumbers()
        {
            (await Run("<input type=\"date\" name=\"a\"><input type=\"date\" name=\"b\">",
                Rule(CaseWorkflowFormValidatorKind.Compare, "a", "lt", "b"), ("a", "2026-02-01"), ("b", "2026-10-01"))).Should().BeEmpty();
            (await Run("<input type=\"number\" name=\"a\"><input type=\"number\" name=\"b\">",
                Rule(CaseWorkflowFormValidatorKind.Compare, "a", "lt", "b"), ("a", "9"), ("b", "10"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecksMembershipOfAListAndADictionary()
        {
            const string form = "<input name=\"a\">";

            (await Run(form, Rule(CaseWorkflowFormValidatorKind.InList, "a", "Countries"), ("a", "GB"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.InList, "a", "Countries"), ("a", "ZZ"))).Should().ContainSingle()
                .Which.Code.Should().Be("list");
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.InDictionary, "a", "Risk"), ("a", "high"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.InDictionary, "a", "Risk"), ("a", "none"))).Should().ContainSingle()
                .Which.Code.Should().Be("dictionary");
        }

        [Fact]
        public async Task PassesASqlRuleOnlyWhenTheScalarIsTrue()
        {
            var rule = Rule(CaseWorkflowFormValidatorKind.Sql, "a", sql: "select true");

            (await Run("<input name=\"a\">", rule, new FakeSql(true), ("a", "x"))).Should().BeEmpty();
            (await Run("<input name=\"a\">", rule, new FakeSql(false), ("a", "x"))).Should().ContainSingle()
                .Which.Code.Should().Be("sql");
        }

        [Fact]
        public async Task FailsClosedWhenTheSqlRaisesOrTimesOut()
        {
            var rule = Rule(CaseWorkflowFormValidatorKind.Sql, "a", sql: "select true");

            (await Run("<input name=\"a\">", rule, new FakeSql(new InvalidOperationException("boom")), ("a", "x")))
                .Should().ContainSingle().Which.Code.Should().Be("sqlFailed");
            (await Run("<input name=\"a\">", rule, new FakeSql(new OperationCanceledException()), ("a", "x")))
                .Should().ContainSingle().Which.Code.Should().Be("sqlFailed");
        }

        [Fact]
        public async Task BindsTheValueTheOtherFieldsAndTheCaseToTheSql()
        {
            var sql = new FakeSql(true);

            await Run("<input name=\"a\"><input name=\"other\">", Rule(CaseWorkflowFormValidatorKind.Sql, "a", sql: "select true"),
                sql, ("a", "x"), ("other", "y"));

            sql.Parameters["value"].Should().Be("x");
            sql.Parameters["other"].Should().Be("y");
            sql.Parameters["CaseId"].Should().Be(42);
            sql.Parameters["CaseKeyValue"].Should().Be("K-1");
        }

        [Fact]
        public async Task SkipsAFieldTheTemplateDoesNotShowOrThatIsNotInTheForm()
        {
            var form = "<select name=\"r\"><option value=\"other\">o</option><option value=\"x\">x</option></select><input v-if=\"Form.r == 'other'\" name=\"d\">";

            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Required, "d"), ("r", "x"))).Should().BeEmpty();
            (await Run(form, Rule(CaseWorkflowFormValidatorKind.Required, "d"), ("r", "other"))).Should().ContainSingle();
            (await Run("<input name=\"a\">", Rule(CaseWorkflowFormValidatorKind.Required, "gone"))).Should().BeEmpty();
        }

        [Fact]
        public async Task ReportsOnlyTheFirstFailureOfAField()
        {
            var errors = await Run("<input name=\"a\">", [Rule(CaseWorkflowFormValidatorKind.Length, "a", "5", null, id: 1),
                Rule(CaseWorkflowFormValidatorKind.Pattern, "a", "^z+$", id: 2)], new FakeSql(true), ("a", "x"));

            errors.Should().ContainSingle().Which.Code.Should().Be("length");
        }

        [Fact]
        public async Task CarriesTheAuthoredMessage()
        {
            var errors = await Run("<input name=\"a\">", Rule(CaseWorkflowFormValidatorKind.Required, "a", message: "Say why."));

            errors.Should().ContainSingle().Which.Message.Should().Be("Say why.");
        }

        [Fact]
        public async Task TestsOneRuleAndTimesIt()
        {
            var schema = DatasourceTemplate.CompileForm("<input name=\"a\">");

            var outcome = await FormValidatorEngine.TestAsync(schema, Rule(CaseWorkflowFormValidatorKind.Length, "a", "3", null),
                new Dictionary<string, string> { ["a"] = "xx" }, "K", 1, new FakeLookups(), new FakeSql(true), default);

            outcome.Passed.Should().BeFalse();
            outcome.Code.Should().Be("length");
            outcome.Milliseconds.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}

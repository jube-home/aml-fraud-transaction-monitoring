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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class RuleRunnerTests
    {
        private static readonly InvocationContextField[] fields =
        [
            new("Payload.Amount", "Payload", "double"),
            new("Payload.Country", "Payload", "string"),
            new("TTLCounter.CountLastHour", "TTLCounter", "double"),
            new("Abstraction.Velocity", "Abstraction", "double"),
            new("Activation.Earlier", "Activation", "boolean")
        ];

        private static RuleParseEnvironmentDto Environment()
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = ["CountLastHour"];
            environment.AbstractionRules = ["Velocity"];
            environment.Sanctions = [];
            environment.AbstractionCalculations = [];
            environment.HttpAdaptations = [];
            environment.ExhaustiveAdaptations = [];
            environment.ActivationRules = ["Earlier"];
            return environment;
        }

        private static RuleRunInputs Inputs(double amount, string country = "GB", double count = 0,
            double velocity = 0, bool earlier = false)
        {
            var context = InvocationContextBuilder.Blank(1, fields, [], false, DateTime.UtcNow);
            InvocationContextBuilder.Overlay(context, new Dictionary<string, string>
            {
                ["Payload.Amount"] = amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Payload.Country"] = country,
                ["TTLCounter.CountLastHour"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Abstraction.Velocity"] = velocity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Activation.Earlier"] = earlier ? "true" : "false"
            }).Should().BeEmpty();
            return RuleRunner.ToInputs(context,
                new Dictionary<string, List<string>> { ["HighRiskTerms"] = ["IR", "KP"] });
        }

        private static RuleParseResult Parse(string text, int type, bool reprocessing = false)
        {
            var parsed = RuleParse.Execute(text, type, Environment(), TestLog.NoOp,
                RuleParse.EngineReferences(type), engineWrap: true, reprocessing: reprocessing);
            parsed.Compiled.Should().BeTrue(parsed.Message);
            return parsed;
        }

        [Theory]
        [InlineData(150, true)]
        [InlineData(50, false)]
        public async Task AGatewayRuleRunsAgainstTheContextAsync(double amount, bool expected)
        {
            var result = await RuleRunner.RunAsync(
                Parse("If (Payload.Amount > 100) Then\n   Return True\nEnd If", RuleParse.GatewayRule),
                RuleParse.GatewayRule, false, Inputs(amount));

            result.Error.Should().BeNull();
            result.Value.Should().Be(expected);
        }

        [Fact]
        public async Task AReprocessingRuleUsesTheReprocessingDelegateAsync()
        {
            var result = await RuleRunner.RunAsync(
                Parse("If (Payload.Amount > 100) Then\n   Return True\nEnd If", RuleParse.GatewayRule, true),
                RuleParse.GatewayRule, true, Inputs(150));

            result.Value.Should().Be(true);
        }

        [Fact]
        public async Task AnAbstractionRuleCompilesAsTheEnginesAbstractionRuleClassAsync()
        {
            var parsed = Parse("If (List.HighRiskTerms.Contains(Payload.Country)) Then\n   Return True\nEnd If",
                RuleParse.AbstractionRule);

            parsed.ClassName.Should().Be("AbstractionRule");
            (await RuleRunner.RunAsync(parsed, RuleParse.AbstractionRule, false, Inputs(1, "IR"))).Value.Should()
                .Be(true);
            (await RuleRunner.RunAsync(parsed, RuleParse.AbstractionRule, false, Inputs(1))).Value.Should()
                .Be(false);
        }

        [Fact]
        public async Task AnActivationRuleSeesCountersAbstractionsAndEarlierActivationsAsync()
        {
            var parsed = Parse(
                "If (TTLCounter.CountLastHour > 2 And Abstraction.Velocity > 5 And Activation.Earlier) Then\n" +
                "   Return True\nEnd If", RuleParse.ActivationRule);

            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false,
                Inputs(1, count: 3, velocity: 6, earlier: true))).Value.Should().Be(true);
            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false,
                Inputs(1, count: 3, velocity: 6, earlier: false))).Value.Should().Be(false);
        }

        [Fact]
        public async Task AnAbstractionCalculationReturnsItsNumberAsync()
        {
            var result = await RuleRunner.RunAsync(Parse("Return Payload.Amount * 2", RuleParse.AbstractionCalculation),
                RuleParse.AbstractionCalculation, false, Inputs(21));

            result.Value.Should().Be(42d);
        }

        [Fact]
        public async Task AnInlineFunctionReturnsItsValueAsync()
        {
            var result = await RuleRunner.RunAsync(Parse("Return Payload.Country", RuleParse.InlineFunction),
                RuleParse.InlineFunction, false, Inputs(1, "FR"));

            result.Value.Should().Be("FR");
        }

        [Fact]
        public async Task ARuntimeErrorIsReportedRatherThanSwallowedAsync()
        {
            var parsed = Parse("If (List.HighRiskTerms.Contains(Payload.Country)) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule);
            var inputsWithoutTheList = RuleRunner.ToInputs(
                InvocationContextBuilder.Blank(1, fields, [], false, DateTime.UtcNow),
                new Dictionary<string, List<string>>());

            var result = await RuleRunner.RunAsync(parsed, RuleParse.GatewayRule, false, inputsWithoutTheList);

            result.Error.Should().BeOfType<KeyNotFoundException>();
            result.Value.Should().BeNull();
            result.TimedOut.Should().BeFalse();
        }
    }
}
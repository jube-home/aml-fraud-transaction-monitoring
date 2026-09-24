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
using Jube.Data.Query.Models;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleParseTests
    {
        private const string ValidGatewayRule = "If (Payload.Amount > 100) Then\n   Return True\nEnd If";

        internal static RuleParseEnvironmentDto Environment()
        {
            return new RuleParseEnvironmentDto
            {
                Tokens = ["Matched"],
                RequestXPaths = new Dictionary<string, RuleParseEnvironmentRequestXPathDto>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true },
                    ["Country"] = new() { DataTypeId = 1, Cache = true },
                    ["Narrative"] = new() { DataTypeId = 1, Cache = false }
                },
                Lists = ["HighRiskTerms"]
            };
        }

        [Fact]
        public void ValidGatewayRuleCompilesAndCarriesTheClassName()
        {
            var result = RuleParse.Execute(ValidGatewayRule, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences());

            result.Message.Should().Be("Compiled");
            result.Compiled.Should().BeTrue();
            result.ErrorSpans.Should().BeEmpty();
            result.ClassName.Should().Be("GatewayRule");
            result.ParsedRuleText.Should().Contain("Public Class GatewayRule");
        }

        [Fact]
        public void UnknownFieldReturnsLocatedErrorSpans()
        {
            var result = RuleParse.Execute("If (Payload.Missing > 100) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule, Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.Message.Should().NotBe("Compiled");
            result.ErrorSpans.Should().NotBeEmpty();
            result.ClassName.Should().BeNull();
        }

        [Fact]
        public void CompilerErrorIsReportedAgainstTheUserLineNotTheWrapper()
        {
            var result = RuleParse.Execute("If (Payload.Amount > ) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule, Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse();
            result.ErrorSpans.Should().Contain(e => e.Line == 0 && e.Message.StartsWith("Line 1:"));
        }

        [Fact]
        public void RuleTextOverTheMaximumLengthIsAnErrorWithoutParsing()
        {
            var result = RuleParse.Execute(new string('a', RuleParse.MaximumRuleTextLength + 1),
                RuleParse.GatewayRule, Environment(), TestLog.NoOp, RuleParse.DefaultReferences());

            result.Message.Should().Be("Error");
            result.ParsedRuleText.Should().BeNull();
        }

        [Fact]
        public void WithoutReferencesTheRuleParsesButIsNotMarkedCompiled()
        {
            var result = RuleParse.Execute(ValidGatewayRule, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                null);

            result.Message.Should().Be("Compiled");
            result.Compiled.Should().BeFalse();
        }

        [Fact]
        public void AbstractionRulesOnlySeeCachedPayloadFields()
        {
            const string rule = "If (Payload.Narrative = \"x\") Then\n   Return True\nEnd If";

            RuleParse.Execute(rule, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences()).Compiled.Should().BeTrue();

            RuleParse.Execute(rule, RuleParse.AbstractionRule, Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences()).Compiled.Should().BeFalse();
        }

        [Fact]
        public void TtlCountersAreNotVisibleBeforeTheirStage()
        {
            var environment = Environment();
            environment.TtlCounters = ["CountLastHour"];
            const string rule = "If (TTLCounter.CountLastHour > 1) Then\n   Return True\nEnd If";

            RuleParse.Execute(rule, RuleParse.ActivationRule, environment, TestLog.NoOp,
                RuleParse.DefaultReferences()).Compiled.Should().BeTrue();

            environment.TtlCounters = null;
            RuleParse.Execute(rule, RuleParse.GatewayRule, environment, TestLog.NoOp,
                RuleParse.DefaultReferences()).Compiled.Should().BeFalse();
        }

        [Fact]
        public void TheEnvironmentTokenListIsNotMutatedByTheParser()
        {
            var environment = Environment();

            RuleParse.Execute(ValidGatewayRule, RuleParse.GatewayRule, environment, TestLog.NoOp, null);

            environment.Tokens.Should().Equal("Matched");
        }

        [Theory]
        [InlineData(RuleParse.InlineFunction, "InlineFunction")]
        [InlineData(RuleParse.GatewayRule, "GatewayRule")]
        [InlineData(RuleParse.AbstractionRule, "GatewayRule")]
        [InlineData(RuleParse.AbstractionCalculation, "CalculationRule")]
        [InlineData(RuleParse.ActivationRule, "ActivationRule")]
        [InlineData(99, null)]
        public void ClassNameFollowsTheParserWrapper(int ruleParseType, string? expected)
        {
            RuleParse.ClassName(ruleParseType).Should().Be(expected);
        }

        [Fact]
        public void CompilingWithoutLoadingKeepsTheBinaryAndLoadsNoAssembly()
        {
            var parsed = RuleParse.Execute(ValidGatewayRule, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                null);

            var compile = new Compile();
            compile.CompileCode(parsed.ParsedRuleText, TestLog.NoOp, RuleParse.DefaultReferences(),
                Compile.Language.Vb, false);

            compile.Success.Should().BeTrue();
            compile.CompiledAssembly.Should().BeNull();
            compile.CompiledAssemblyBinary.Should().NotBeEmpty();
            compile.CompiledAssemblyBytes.Should().Be(compile.CompiledAssemblyBinary.LongLength);
        }

        [Fact]
        public void CompilingWithAndWithoutLoadingReportsTheSameErrors()
        {
            const string broken =
                "Public Class GatewayRule\nPublic Shared Function Match() As Boolean\nReturn 1 +\nEnd Function\nEnd Class";

            var loaded = new Compile();
            loaded.CompileCode(broken, TestLog.NoOp, RuleParse.DefaultReferences(), Compile.Language.Vb);
            var notLoaded = new Compile();
            notLoaded.CompileCode(broken, TestLog.NoOp, RuleParse.DefaultReferences(), Compile.Language.Vb, false);

            loaded.Success.Should().BeFalse();
            notLoaded.Success.Should().BeFalse();
            notLoaded.Errors.Select(e => e.GetMessage()).Should().Equal(loaded.Errors.Select(e => e.GetMessage()));
        }
    }
}
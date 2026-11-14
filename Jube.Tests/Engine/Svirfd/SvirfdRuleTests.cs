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

using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation.Models;
using Jube.Parser;
using Jube.Parser.Models;
using Jube.Svirfd;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Xunit;

namespace Jube.Test.Engine.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdRuleTests
    {
        private static RuleParseEnvironmentDto Environment()
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = [];
            environment.AbstractionRules = [];
            environment.Sanctions = [];
            environment.AbstractionCalculations = [];
            environment.HttpAdaptations = [];
            environment.ExhaustiveAdaptations = [];
            environment.AdvancedAnalyticsAdaptations = [];
            environment.ActivationRules = [];
            environment.Graph = [];
            environment.Svirfd = ["IP"];

            return environment;
        }

        private static RuleParseResult Parse(string text, int type = RuleParse.ActivationRule)
        {
            return RuleParse.Execute(text, type, Environment(), TestLog.NoOp, RuleParse.EngineReferences(type),
                engineWrap: true);
        }

        private static async Task<RuleRunInputs> InputsAsync(string address)
        {
            var inputs = new RuleRunInputs();
            inputs.Svirfd["IP"] = await Jube.Test.Svirfd.SvirfdFixture.RecordAsync(address);

            return inputs;
        }

        [Theory]
        [InlineData("If Svirfd.IP.Asn.Name = \"T-Mobile Polska S.A.\" Then\n   Return True\nEnd If", "37.30.56.54", true)]
        [InlineData("If Svirfd.IP.Asn.Name = \"T-Mobile Polska S.A.\" Then\n   Return True\nEnd If", "185.220.101.151", false)]
        [InlineData("If Svirfd.IP.Privacy.Tor = True Then\n   Return True\nEnd If", "185.220.101.151", true)]
        [InlineData("If Svirfd.IP.Privacy.Tor = True Then\n   Return True\nEnd If", "37.30.56.54", false)]
        [InlineData("If Svirfd.IP.Routing.Origin.Peers > 0 Then\n   Return True\nEnd If", "37.30.56.54", true)]
        [InlineData("If Svirfd.IP.Lists.Count > 0 Then\n   Return True\nEnd If", "185.220.101.151", true)]
        [InlineData("If Svirfd.IP.Bogon = True Then\n   Return True\nEnd If", "10.0.0.1", true)]
        [InlineData("If Svirfd.IP.DistanceKm(52.23, 21.01) < 5 Then\n   Return True\nEnd If", "37.30.56.54", true)]
        [InlineData("If Svirfd.IP.DistanceKm(51.51, -0.13) < 5 Then\n   Return True\nEnd If", "37.30.56.54", false)]
        [InlineData("If Svirfd.IP.WithinKm(52.23, 21.01, 10) = True Then\n   Return True\nEnd If", "37.30.56.54", true)]
        public async Task ActivationRulesReadTheRecordAsync(string rule, string address, bool expected)
        {
            var parsed = Parse(rule);

            parsed.Compiled.Should().BeTrue(parsed.Message);
            parsed.ErrorSpans.Should().BeEmpty();

            var result = await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, await InputsAsync(address));

            result.Error.Should().BeNull();
            (result.Value is true).Should().Be(expected);
        }

        [Fact]
        public async Task AMissingRecordReadsAsNothingAndNeverThrowsAsync()
        {
            var parsed = Parse("If Svirfd.IP.Privacy.Vpn = True Then\n   Return True\nEnd If");
            var inputs = new RuleRunInputs();
            inputs.Svirfd["IP"] = null!;

            var result = await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, inputs);

            result.Error.Should().BeNull();
            (result.Value is true).Should().BeFalse();
        }

        [Theory]
        [InlineData(RuleParse.GatewayRule)]
        [InlineData(RuleParse.AbstractionRule)]
        public async Task GatewayAndAbstractionRulesReadTheRecordAsync(int type)
        {
            var parsed = Parse("If Svirfd.IP.Asn.Asn = \"AS12912\" Then\n   Return True\nEnd If", type);

            parsed.Compiled.Should().BeTrue(parsed.Message);

            var result = await RuleRunner.RunAsync(parsed, type, false, await InputsAsync("37.30.56.54"));

            result.Error.Should().BeNull();
            (result.Value is true).Should().BeTrue();
        }

        [Fact]
        public async Task InlineFunctionsReadTheRecordAsync()
        {
            var parsed = Parse("Return Svirfd.IP.Country", RuleParse.InlineFunction);

            parsed.Compiled.Should().BeTrue(parsed.Message);

            var result = await RuleRunner.RunAsync(parsed, RuleParse.InlineFunction, false,
                await InputsAsync("37.30.56.54"));

            result.Error.Should().BeNull();
            result.Value.Should().Be("PL");
        }

        [Fact]
        public void UnknownEntitiesAndMembersAreErrorSpans()
        {
            var entity = Parse("If Svirfd.Nope.Privacy.Vpn = True Then\n   Return True\nEnd If");
            entity.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("Svirfd entry does not exist for Nope"));

            var member = Parse("If Svirfd.IP.Privacy.Nope = True Then\n   Return True\nEnd If");
            member.ErrorSpans.Should().ContainSingle(e => e.Message.Contains("Svirfd member does not exist for Privacy.Nope"));
        }

        [Fact]
        public void TheVocabularyAndTokensCarryTheCatalogue()
        {
            var parser = new Parser.Parser(TestLog.NoOp, []);

            parser.RuleScriptTokens.Should().Contain(["Svirfd", "Privacy", "Vpn", "DistanceKm", "Origin"]);
            SvirfdMembers.Scalars.Should().HaveCountGreaterThan(100);
        }
    }
}

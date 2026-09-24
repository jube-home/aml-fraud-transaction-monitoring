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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Data.QueryBuilder;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class BuilderRuleTranslatorTests
    {
        private static readonly IReadOnlyDictionary<string, BuilderField> fields = BuilderRuleTranslator.Fields([
            ("Payload.Amount", "double"), ("Payload.Count", "integer"), ("Payload.Country", "string"),
            ("Payload.IP", "string"), ("Payload.ResponseCode", "string"), ("Payload.Flag", "boolean"),
            ("Payload.Opened", "datetime"), ("List.HighRiskTerms", "list"), ("List.IPDenyList", "list"),
            ("TTLCounter.PerAccount", "double")
        ]);

        private static string Translate(string json)
        {
            var parsed = BuilderProfile.Parse(json, fields, BuilderTarget.RuleText);
            parsed.Errors.Should().BeEmpty();
            return BuilderRuleTranslator.Translate(parsed.Group);
        }

        [Theory]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":100}]}",
            "If (Payload.Amount > 100) Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"less_or_equal\",\"value\":2.5}]}",
            "If (Payload.Amount <= 2.5) Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"GB\"}]}",
            "If (Payload.Country = \"GB\") Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"begins_with\",\"value\":\"G\"}]}",
            "If (Payload.Country.StartsWith(\"G\")) Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"List.HighRiskTerms\",\"operator\":\"has\",\"value\":\"Payload.Country\"}]}",
            "If (List.HighRiskTerms.contains(Payload.Country)) Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Flag\",\"operator\":\"equal\",\"value\":\"True\"}]}",
            "If (Payload.Flag = True) Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"O'Brien\"}]}",
            "If (Payload.Country = \"O'Brien\") Then\n  Return True\nEnd If")]
        [InlineData(
            "{\"condition\":\"AND\",\"not\":true,\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":1}," +
            "{\"condition\":\"OR\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"GB\"}," +
            "{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"FR\"}]}]}",
            "If (NOT ( Payload.Amount > 1 AND ( Payload.Country = \"GB\" OR Payload.Country = \"FR\" )  )) Then\n  Return True\nEnd If")]
        public void TheTranslationIsExactlyWhatTheBrowserBuilderProduces(string json, string expected)
        {
            Translate(json).Should().Be(expected);
        }

        [Fact]
        public void TheBrowsersOwnJsonShapeIsAcceptedIncludingFieldTypeInputAndValidKeys()
        {
            Translate(
                    "{\"rules\":[{\"id\":\"TTLCounter.PerAccount\",\"type\":\"double\",\"field\":\"ttlcounter.PerAccount\"," +
                    "\"input\":\"number\",\"value\":5,\"operator\":\"greater\"}],\"valid\":true,\"condition\":\"AND\"}")
                .Should().Be("If (TTLCounter.PerAccount > 5) Then\n  Return True\nEnd If");
        }

        [Theory]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Nope\",\"operator\":\"equal\",\"value\":\"x\"}]}",
            "FieldUnknown")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Opened\",\"operator\":\"contains\",\"value\":\"x\"}]}",
            "OperatorInvalid")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Opened\",\"operator\":\"equal\",\"value\":\"x\"}]}",
            "ValueInvalid")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"greater\",\"value\":\"x\"}]}",
            "OperatorInvalid")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Count\",\"operator\":\"equal\",\"value\":1.5}]}",
            "ValueInvalid")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Flag\",\"operator\":\"equal\",\"value\":\"yes\"}]}",
            "ValueInvalid")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"List.HighRiskTerms\",\"operator\":\"has\",\"value\":\"Payload.Amount\"}]}",
            "ValueInvalid")]
        [InlineData(
            "{\"condition\":\"XOR\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":1}]}",
            "ConditionInvalid")]
        [InlineData("{\"condition\":\"AND\",\"rules\":[]}", "RulesEmpty")]
        [InlineData(
            "{\"condition\":\"AND\",\"sql\":\"drop\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":1}]}",
            "KeyUnknown")]
        [InlineData(
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"field\":\"Payload.Count\",\"operator\":\"greater\",\"value\":1}]}",
            "FieldMismatch")]
        [InlineData("not json", "BuilderJsonInvalid")]
        public void AnythingOutsideTheProfileIsRefusedWithACodeAndPath(string json, string code)
        {
            var parsed = BuilderProfile.Parse(json, fields, BuilderTarget.RuleText);

            parsed.Valid.Should().BeFalse();
            parsed.Errors.Should().Contain(e => e.Code == code && e.Path.StartsWith("$"));
        }

        [Fact]
        public void NestingDeeperThanTheLimitIsRefused()
        {
            var json =
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":1}]}";
            for (var i = 0; i < BuilderProfile.MaximumDepth; i++)
            {
                json = "{\"condition\":\"AND\",\"rules\":[" + json + "]}";
            }

            BuilderProfile.Parse(json, fields, BuilderTarget.RuleText).Errors.Should()
                .Contain(e => e.Code == "GroupTooDeep");
        }

        public static IEnumerable<object[]> Seeds()
        {
            yield return
            [
                "{\"rules\":[{\"id\":\"List.IPDenyList\",\"type\":\"string\",\"field\":\"List.IPDenyList\",\"input\":\"select\"," +
                "\"value\":\"Payload.IP\",\"operator\":\"has\"}],\"valid\":true,\"condition\":\"AND\"}",
                "If (( List.IPDenyList.contains(Payload.IP))) Then\n  Return True\nEnd If"
            ];
            yield return
            [
                "{\"not\":false,\"rules\":[{\"id\":\"Payload.Amount\",\"type\":\"double\",\"field\":\"Payload.Amount\"," +
                "\"input\":\"number\",\"value\":0,\"operator\":\"greater\"}],\"valid\":true,\"condition\":\"AND\"}",
                "If (Payload.Amount > 0) Then \n   Return True \nEnd If"
            ];
            yield return
            [
                "{\"not\":true,\"rules\":[{\"id\":\"Payload.ResponseCode\",\"type\":\"string\",\"field\":\"Payload.ResponseCode\"," +
                "\"input\":\"text\",\"value\":\"0\",\"operator\":\"equal\"}],\"valid\":true,\"condition\":\"AND\"}",
                "If (NOT ( Payload.ResponseCode = \"0\" )) Then \n   Return True\nEnd If"
            ];
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public async Task TranslatedSeedJsonBehavesLikeTheStoredSeedScriptAsync(string json, string storedScript)
        {
            var environment = RuleParseTests.Environment();
            environment.RequestXPaths["IP"] = new RuleParseEnvironmentRequestXPathDto { DataTypeId = 1 };
            environment.RequestXPaths["ResponseCode"] = new RuleParseEnvironmentRequestXPathDto { DataTypeId = 1 };
            environment.Lists.Add("IPDenyList");
            var translated = Translate(json);

            var ours = RuleParse.Execute(translated, RuleParse.GatewayRule, environment, TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.GatewayRule), engineWrap: true);
            var theirs = RuleParse.Execute(storedScript, RuleParse.GatewayRule, environment, TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.GatewayRule), engineWrap: true);
            ours.Compiled.Should().BeTrue(ours.Message);
            theirs.Compiled.Should().BeTrue(theirs.Message);

            InvocationContextField[] contextFields =
            [
                new("Payload.Amount", "Payload", "double"), new("Payload.IP", "Payload", "string"),
                new("Payload.ResponseCode", "Payload", "string")
            ];
            foreach (var (amount, ip, code) in new[] { ("0", "1.1.1.1", "0"), ("5", "9.9.9.9", "1") })
            {
                var context = InvocationContextBuilder.Blank(1, contextFields, [], false, DateTime.UtcNow);
                InvocationContextBuilder.Overlay(context, new Dictionary<string, string>
                    { ["Payload.Amount"] = amount, ["Payload.IP"] = ip, ["Payload.ResponseCode"] = code });
                var inputs = RuleRunner.ToInputs(context,
                    new Dictionary<string, List<string>> { ["IPDenyList"] = ["9.9.9.9"], ["HighRiskTerms"] = [] });

                var ourResult = await RuleRunner.RunAsync(ours, RuleParse.GatewayRule, false, inputs);
                var theirResult = await RuleRunner.RunAsync(theirs, RuleParse.GatewayRule, false, inputs);

                ourResult.Value.Should().Be(theirResult.Value, $"amount {amount}, ip {ip}, code {code}");
            }
        }

        [Fact]
        public void EveryTranslationParsesAndCompilesAgainstTheModel()
        {
            var environment = RuleParseTests.Environment();
            environment.RequestXPaths["Flag"] = new RuleParseEnvironmentRequestXPathDto { DataTypeId = 5 };
            environment.RequestXPaths["Count"] = new RuleParseEnvironmentRequestXPathDto { DataTypeId = 2 };
            environment.Tokens.AddRange(["StartsWith", "EndsWith"]);
            var texts = new[]
            {
                "{\"condition\":\"OR\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"ends_with\",\"value\":\"B\"}," +
                "{\"id\":\"Payload.Count\",\"operator\":\"greater_or_equal\",\"value\":3}," +
                "{\"id\":\"Payload.Flag\",\"operator\":\"equal\",\"value\":\"False\"}," +
                "{\"id\":\"List.HighRiskTerms\",\"operator\":\"has\",\"value\":\"Payload.Country\"}]}"
            }.Select(Translate);

            foreach (var text in texts)
            {
                RuleParse.Execute(text, RuleParse.GatewayRule, environment, TestLog.NoOp,
                    RuleParse.DefaultReferences()).Compiled.Should().BeTrue(text);
            }
        }

        [Fact]
        public void BeginsWithAndEndsWithCompileOnADefaultInstallation()
        {
            var text = Translate(
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"ends_with\",\"value\":\"B\"}," +
                "{\"id\":\"Payload.Country\",\"operator\":\"not_begins_with\",\"value\":\"G\"}]}");

            RuleParse.Execute(text, RuleParse.GatewayRule, RuleParseTests.Environment(), TestLog.NoOp,
                RuleParse.DefaultReferences()).Compiled.Should().BeTrue(text);
        }

        [Fact]
        public void TheBrowsersJsonForEveryKindOfArgumentTranslatesToTheBrowsersRuleText()
        {
            var browserFields = BuilderRuleTranslator.Fields([
                ("Payload.Amount", "double"), ("Payload.Country", "string"), ("Payload.Email", "string"),
                ("Payload.Opened", "datetime"), ("Payload.Flag", "boolean"), ("TTLCounter.PerAccount", "double"),
                ("List.HighRisk", "list")
            ]);
            const string json =
                "{\"condition\":\"AND\",\"rules\":[" +
                "{\"id\":\"Payload.Amount\",\"field\":\"Payload.Amount\",\"type\":\"double\",\"input\":\"number\",\"operator\":\"greater\",\"value\":100}," +
                "{\"id\":\"Payload.Amount\",\"field\":\"Payload.Amount\",\"type\":\"double\",\"input\":\"number\",\"operator\":\"between\",\"value\":[\"10\",\"20\"]}," +
                "{\"id\":\"Payload.Amount\",\"field\":\"Payload.Amount\",\"type\":\"double\",\"input\":\"number\",\"operator\":\"ratio_of_above\",\"value\":[\"TTLCounter.PerAccount\",\"2\"]}," +
                "{\"id\":\"Payload.Country\",\"field\":\"Payload.Country\",\"type\":\"string\",\"input\":\"text\",\"operator\":\"in_list\",\"value\":\"List.HighRisk\"}," +
                "{\"id\":\"Payload.Country\",\"field\":\"Payload.Country\",\"type\":\"string\",\"input\":\"text\",\"operator\":\"in\",\"value\":\"GB, FR\"}," +
                "{\"id\":\"Payload.Opened\",\"field\":\"Payload.Opened\",\"type\":\"datetime\",\"input\":\"text\",\"operator\":\"less\",\"value\":\"2026-01-31T00:00:00\"}," +
                "{\"id\":\"Payload.Email\",\"field\":\"Payload.Email\",\"type\":\"string\",\"input\":\"text\",\"operator\":\"equal_field\",\"value\":\"Payload.Country\"}," +
                "{\"id\":\"Payload.Flag\",\"field\":\"Payload.Flag\",\"type\":\"string\",\"input\":\"radio\",\"operator\":\"equal\",\"value\":\"True\"}," +
                "{\"id\":\"List.HighRisk\",\"field\":\"List.HighRisk\",\"type\":\"string\",\"input\":\"select\",\"operator\":\"has\",\"value\":\"Payload.Country\"}," +
                "{\"id\":\"Payload.Amount\",\"field\":\"Payload.Amount\",\"type\":\"double\",\"input\":\"number\",\"operator\":\"is_just_below_any_threshold\",\"value\":[\"10\",\"1000, 10000\"]}" +
                "],\"valid\":true}";
            var parsed = BuilderProfile.Parse(json, browserFields, BuilderTarget.RuleText);
            parsed.Errors.Should().BeEmpty();

            BuilderRuleTranslator.Translate(parsed.Group).Should().Be(
                "If (Payload.Amount > 100 AND Payload.Amount.MatchInRange(10, 20).ToBoolean() AND " +
                "Payload.Amount.MatchRatioOfAbove(TTLCounter.PerAccount, 2).ToBoolean() AND " +
                "Payload.Country.MatchInList(List.HighRisk).ToBoolean() AND " +
                "Payload.Country.MatchIn(\"GB\", \"FR\").ToBoolean() AND " +
                "Payload.Opened < \"2026-01-31T00:00:00Z\".ToIsoDateTime() AND Payload.Email = Payload.Country AND " +
                "Payload.Flag = True AND List.HighRisk.contains(Payload.Country) AND " +
                "Payload.Amount.MatchIsJustBelowAnyThreshold(10, 1000, 10000).ToBoolean()) Then\n  Return True\nEnd If");
        }

        [Fact]
        public void AFieldReferenceIsOnlyAcceptedWhereTheArgumentAllowsItAndNeverInMemory()
        {
            var inMemory = BuilderProfile.Parse(
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"ratio_of_above\"," +
                "\"value\":[\"Payload.Amount\",\"2\"]}]}", fields);
            var textField = BuilderProfile.Parse(
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\"," +
                "\"value\":\"Payload.IP\"}]}", fields, BuilderTarget.RuleText);

            inMemory.Errors.Should().ContainSingle(e => e.Code == "ValueInvalid");
            Translate("{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\"," +
                      "\"value\":\"Payload.IP\"}]}").Should().Contain("Payload.Country = \"Payload.IP\"");
            textField.Errors.Should().BeEmpty();
        }

        [Fact]
        public void AQuoteInATextValueIsDoubledAndAControlCharacterIsRefused()
        {
            Translate(
                    "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"a\\\"b\"}]}")
                .Should().Be("If (Payload.Country = \"a\"\"b\") Then\n  Return True\nEnd If");
            BuilderProfile.Parse(
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Country\",\"operator\":\"equal\",\"value\":\"a\\nb\"}]}",
                fields, BuilderTarget.RuleText).Errors.Should().ContainSingle(e => e.Code == "ValueInvalid");
        }
    }
}
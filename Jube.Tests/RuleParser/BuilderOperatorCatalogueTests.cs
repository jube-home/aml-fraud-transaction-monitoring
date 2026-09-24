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
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Data.QueryBuilder;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class BuilderOperatorCatalogueTests
    {
        private static readonly IReadOnlyDictionary<string, BuilderField> fields = BuilderRuleTranslator.Fields([
            ("Payload.Amount", "double"), ("Payload.Limit", "double"), ("Payload.Count", "integer"),
            ("Payload.Other", "integer"), ("Payload.Country", "string"), ("Payload.Email", "string"),
            ("Payload.Opened", "datetime"), ("Payload.Closed", "datetime"), ("Payload.Flag", "boolean"),
            ("Payload.Shadow", "boolean"), ("List.HighRiskTerms", "list")
        ]);

        private static readonly Dictionary<BuilderFieldType, (string Field, string Other)> subjects = new()
        {
            [BuilderFieldType.String] = ("Payload.Country", "Payload.Email"),
            [BuilderFieldType.Integer] = ("Payload.Count", "Payload.Other"),
            [BuilderFieldType.Double] = ("Payload.Amount", "Payload.Limit"),
            [BuilderFieldType.DateTime] = ("Payload.Opened", "Payload.Closed"),
            [BuilderFieldType.Boolean] = ("Payload.Flag", "Payload.Shadow"),
            [BuilderFieldType.List] = ("List.HighRiskTerms", "Payload.Country")
        };

        private static readonly Dictionary<BuilderFieldType, string[]> payloads = new()
        {
            [BuilderFieldType.String] = ["GB", "gb-FRANCE 12345"],
            [BuilderFieldType.Integer] = ["3", "-7"],
            [BuilderFieldType.Double] = ["10", "999.5"],
            [BuilderFieldType.DateTime] = ["2026-01-03T10:00:00", "2026-01-31T23:30:00"],
            [BuilderFieldType.Boolean] = ["True", "False"]
        };

        private static readonly Dictionary<string, List<string>> lists = new()
        {
            ["HighRiskTerms"] = ["GB", "FR", "gb-france 12345"]
        };

        public static IEnumerable<object[]> RuleTextCases()
        {
            return BuilderOperators.All
                .SelectMany(o => o.RuleTextTypes.Select(t => new object[] { o.Type, t }));
        }

        public static IEnumerable<object[]> InMemoryCases()
        {
            return BuilderOperators.All
                .Where(o => o.InMemoryCapable && o.Template != null)
                .SelectMany(o => o.RuleTextTypes.Where(t => o.Evaluators.ContainsKey(t))
                    .Select(t => new object[] { o.Type, t }));
        }

        private static RuleParseEnvironmentDto Environment()
        {
            var environment = RuleParseTests.Environment();
            foreach (var (name, type) in new[]
                     {
                         ("Limit", 3), ("Count", 2), ("Other", 2), ("Email", 1), ("Opened", 4), ("Closed", 4),
                         ("Flag", 5), ("Shadow", 5)
                     })
            {
                environment.RequestXPaths[name] = new RuleParseEnvironmentRequestXPathDto { DataTypeId = type };
            }

            return environment;
        }

        private static JToken Sample(BuilderArgument argument, BuilderFieldType type)
        {
            var kind = argument.Kind;
            if (kind == BuilderArgumentKind.Value)
            {
                kind = type switch
                {
                    BuilderFieldType.Integer => BuilderArgumentKind.Integer,
                    BuilderFieldType.Double => BuilderArgumentKind.Number,
                    BuilderFieldType.DateTime => BuilderArgumentKind.Date,
                    BuilderFieldType.String => BuilderArgumentKind.Text,
                    _ => BuilderArgumentKind.Value
                };
            }

            var name = argument.Name.ToLowerInvariant();
            string one = kind switch
            {
                BuilderArgumentKind.Integer => name.Contains("hour") ? name.Contains("end") ? "17" : "9"
                    : name.Contains("to") || name.Contains("maximum") || name.Contains("upper") ? "12" : "3",
                BuilderArgumentKind.Number => name.Contains("to") || name.Contains("maximum") || name.Contains("upper")
                    ? "1000"
                    : name.Contains("threshold") && !name.Contains("margin")
                        ? "1000"
                        : name.Contains("percent") || name.Contains("margin")
                            ? "20"
                            : name.Contains("latitude") || name.Contains("longitude")
                                ? "51.5"
                                : "0.5",
                BuilderArgumentKind.Date => name.Contains("end") || name.Contains("to")
                    ? "2026-02-01T00:00:00Z"
                    : "2026-01-01T00:00:00Z",
                BuilderArgumentKind.Text => name.Contains("zone") ? "Europe/London"
                    : name.Contains("options") ? "lev=1"
                    : name.Contains("pattern") ? "^G"
                    : name.Contains("domain") ? "example.com"
                    : name.Contains("cidr") ? "10.0.0.0/8"
                    : name.Contains("path") ? "$.a" : "GB",
                BuilderArgumentKind.List => "List.HighRiskTerms",
                BuilderArgumentKind.Field => subjects[type].Other,
                _ => type == BuilderFieldType.Boolean ? "True" : "x"
            };

            if (!argument.Many)
            {
                return new JValue(one);
            }

            var two = kind switch
            {
                BuilderArgumentKind.Integer => "5",
                BuilderArgumentKind.Number => "10000",
                BuilderArgumentKind.Text => "FR",
                BuilderArgumentKind.Date => "2026-01-31T23:30:00Z",
                _ => one
            };
            return new JValue(one + ", " + two);
        }

        private static string Json(BuilderOperator definition, BuilderFieldType type)
        {
            JToken value = definition.Inputs switch
            {
                0 => JValue.CreateNull(),
                1 => Sample(definition.Arguments[0], type),
                _ => new JArray(definition.Arguments.Select(a => Sample(a, type)))
            };

            return new JObject
            {
                ["condition"] = "AND",
                ["rules"] = new JArray(new JObject
                {
                    ["id"] = subjects[type].Field, ["operator"] = definition.Type, ["value"] = value
                })
            }.ToString(Formatting.None);
        }

        private static string Translate(BuilderOperator definition, BuilderFieldType type)
        {
            var parsed = BuilderProfile.Parse(Json(definition, type), fields, BuilderTarget.RuleText);
            parsed.Errors.Should().BeEmpty(Json(definition, type));
            return BuilderRuleTranslator.Translate(parsed.Group);
        }

        [Fact]
        public void EveryPipelineMatchStepOnAPlainValueIsAnOperator()
        {
            var plain = new[] { typeof(string), typeof(int), typeof(double), typeof(DateTime), typeof(bool) };
            var steps = typeof(global::Jube.Dictionary.Extensions.Extensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name.Length > 5 && m.Name.StartsWith("Match") && char.IsUpper(m.Name[5]) &&
                            m.Name != "Matches" && m.GetParameters().Length > 0 &&
                            plain.Contains(m.GetParameters()[0].ParameterType) &&
                            m.GetParameters().All(p =>
                                p.ParameterType != typeof(System.Text.RegularExpressions.RegexOptions)))
                .Select(m => m.Name)
                .Distinct()
                .ToList();

            var coveredByBasic = new Dictionary<string, string>
            {
                ["MatchEqual"] = "equal", ["MatchNotEqual"] = "not_equal", ["MatchGreater"] = "greater",
                ["MatchGreaterOrEqual"] = "greater_or_equal", ["MatchLess"] = "less",
                ["MatchLessOrEqual"] = "less_or_equal", ["MatchOutsideRange"] = "not_between",
                ["MatchContains"] = "contains", ["MatchStartsWith"] = "begins_with", ["MatchEndsWith"] = "ends_with"
            };
            var templates = BuilderOperators.All.Select(o => o.Template ?? string.Empty).ToList();

            var missing = steps.Where(step => !templates.Any(t => t.Contains("." + step + "(")) &&
                                              !(coveredByBasic.TryGetValue(step, out var basic) &&
                                                BuilderOperators.ByType.ContainsKey(basic)))
                .ToList();

            missing.Should().BeEmpty("every step is offered, directly or by the basic operator that means the same");
            steps.Count.Should().BeGreaterThan(250);
        }

        [Fact]
        public void KeysAreUniqueAndGroupsAreInOrderWithLabelsAlphabeticalOutsideBasic()
        {
            BuilderOperators.All.Select(o => o.Type).Should().OnlyHaveUniqueItems();
            BuilderOperators.All.Select(o => BuilderOperators.GroupOrder.ToList().IndexOf(o.Group)).Should()
                .BeInAscendingOrder().And.NotContain(-1);
            foreach (var group in BuilderOperators.All.Where(o => o.Group != BuilderOperators.Basic)
                         .GroupBy(o => o.Group))
            {
                group.Select(o => o.Label).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase, group.Key);
            }
        }

        [Theory]
        [InlineData("equal", " = ?")]
        [InlineData("less", " < ?")]
        [InlineData("less_or_equal", " <= ?")]
        [InlineData("greater", " > ?")]
        [InlineData("greater_or_equal", " >= ?")]
        [InlineData("begins_with", ".StartsWith(?)")]
        [InlineData("contains", ".Contains(?)")]
        [InlineData("ends_with", ".EndsWith(?)")]
        [InlineData("has", ".contains(?)")]
        public void TheOperatorsSavedRulesAlreadyUseKeepTheirRuleText(string type, string template)
        {
            BuilderOperators.ByType[type].Template.Should().Be(template);
        }

        [Theory]
        [MemberData(nameof(RuleTextCases))]
        public void EveryOperatorCompilesInTheEngineWrapperForEveryTypeItAppliesTo(string type,
            BuilderFieldType fieldType)
        {
            var text = Translate(BuilderOperators.ByType[type], fieldType);

            var result = RuleParse.Execute(text, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.GatewayRule), engineWrap: true);

            result.Compiled.Should().BeTrue(text + " " + result.Message);
        }

        [Theory]
        [MemberData(nameof(InMemoryCases))]
        public async Task EveryOperatorBehavesInTheEngineExactlyAsTheLibraryDoesAsync(string type,
            BuilderFieldType fieldType)
        {
            var definition = BuilderOperators.ByType[type];
            var text = Translate(definition, fieldType);
            var parsed = RuleParse.Execute(text, RuleParse.GatewayRule, Environment(), TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.GatewayRule), engineWrap: true);
            parsed.Compiled.Should().BeTrue(parsed.Message);

            var filter = BuilderProfile.Parse(Json(definition, fieldType), fields);
            filter.Errors.Should().BeEmpty();
            var rule = (BuilderRule)filter.Group.Rules[0];
            var evaluate = BuilderFilter.CompileValues(filter.Group);
            var subject = subjects[fieldType].Field;
            var dataType = fieldType.ToString().ToLowerInvariant();

            foreach (var payload in payloads[fieldType])
            {
                var context = InvocationContextBuilder.Blank(1,
                [
                    new InvocationContextField(subject, "Payload", dataType == "string" ? "string" : dataType)
                ], [], false, DateTime.UtcNow);
                InvocationContextBuilder.Overlay(context, new Dictionary<string, string> { [subject] = payload });
                var inputs = RuleRunner.ToInputs(context, lists);
                InvocationContextBuilder.TryConvertText(dataType, payload, out var typed);

                var engine = await RuleRunner.RunAsync(parsed, RuleParse.GatewayRule, false, inputs);
                var library = evaluate(new Dictionary<string, object> { [subject] = typed });

                engine.Error.Should().BeNull(text);
                (engine.Value is true).Should().Be(library, $"{text} with {subject} = {payload}; {rule.Operator}");
            }
        }
    }
}
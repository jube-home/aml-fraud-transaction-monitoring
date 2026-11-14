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
using FluentAssertions;
using Jube.Dto.Forms;
using Jube.Dto.Options;
using Jube.Dto.Query.FormModel;
using Jube.Parser;
using Jube.Parser.Dependency.Models;
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelEditorTests
    {
        private static readonly string[] BuilderRoles = ["scriptType", "builder", "builderJson", "coder"];

        private static readonly string[] KnownFieldEditors = ["ruleBuilder", "caseFilter", "exhaustiveDataset", "exhaustiveFilter", "datasource", "caseSql", "aggregationParameters"];

        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        public static IEnumerable<object[]> WithEditors()
        {
            return Registry.Areas.Order().Where(area =>
            {
                Registry.TryGet(area, out var model);

                return model!.Editors.Count > 0;
            }).Select(area => new object[] { area });
        }

        private static FormModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        [Fact]
        public void TheDatasourceEditorStandsInForTheFivePropertiesThatMakeUpADatasourceDisplay()
        {
            var model = Model("VisualisationRegistryDatasource");
            var editor = model.Editors.Should().ContainSingle(e => e.Kind == "datasource").Subject;

            editor.Roles.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["command"] = "command",
                ["displayKind"] = "displayKindId",
                ["grid"] = "includeGrid",
                ["template"] = "templateText",
                ["mapping"] = "mapping"
            });
            model.Fields.Where(field => field.Editor == "datasource").Select(field => field.Name)
                .Should().BeEquivalentTo("command", "includeGrid", "displayKindId", "templateText", "mapping");
            model.Fields.Single(field => field.Name == "mapping").Type.Should().Be("json");
        }

        [Fact]
        public void TheLegacyKendoDisplayPropertiesAreNoLongerFormFields()
        {
            var names = Model("VisualisationRegistryDatasource").Fields.Select(field => field.Name).ToList();

            names.Should().NotContain(["includeDisplay", "visualisationText", "visualisationTypeId"]);
        }

        [Fact]
        public void EveryRuleParseTypeEqualsTheParserConstantOfTheSameName()
        {
            foreach (var member in Enum.GetValues<RuleParseType>())
            {
                typeof(RuleParse).GetField(member.ToString())!.GetRawConstantValue().Should().Be((int)member,
                    member.ToString());
            }
        }

        [Fact]
        public void TheRuleEditorsReachTheModelWithTheirParseTypeAndRoles()
        {
            var gateway = Model("EntityAnalysisModelGatewayRule").Editors.Single();

            gateway.Kind.Should().Be("ruleBuilder");
            gateway.ParseType.Should().Be((int)RuleParseType.GatewayRule);
            gateway.Roles.Should().Equal(new Dictionary<string, string>
            {
                ["scriptType"] = "ruleScriptTypeId",
                ["builder"] = "builderRuleScript",
                ["builderJson"] = "json",
                ["coder"] = "coderRuleScript"
            });

            Model("EntityAnalysisModelReprocessingRule").Editors.Single().ParseType.Should()
                .Be((int)RuleParseType.GatewayRule);
            Model("EntityAnalysisModelActivationRule").Editors.Single(editor => editor.Kind == "ruleBuilder").ParseType.Should()
                .Be((int)RuleParseType.ActivationRule);
            Model("EntityAnalysisModelAbstractionRule").Editors.Single(editor => editor.Kind == "ruleBuilder").ParseType.Should()
                .Be((int)RuleParseType.AbstractionRule);
        }

        [Fact]
        public void TheRuleToolsEditorSitsOnTheActivationRuleAndClaimsNoField()
        {
            var model = Model("EntityAnalysisModelActivationRule");
            var tools = model.Editors.Should().ContainSingle(editor => editor.Kind == "ruleTools").Subject;

            tools.ParseType.Should().Be((int)RuleToolsType.ActivationRule);
            tools.Roles.Should().BeEmpty();
            tools.Reads.Should().BeEmpty();
            model.Editors.Should().ContainSingle(editor => editor.Kind == "ruleBuilder");
        }

        [Fact]
        public void OnlyTheActivationRuleCarriesTheRuleToolsEditor()
        {
            Registry.Areas.Where(area => Model(area).Editors.Any(editor => editor.Kind == "ruleTools"))
                .Should().Equal("EntityAnalysisModelActivationRule");
        }

        [Fact]
        public void EveryRuleToolsTypeNamesAModelEntityKindTheDependencyServiceUnderstands()
        {
            foreach (var type in Enum.GetValues<RuleToolsType>())
            {
                Enum.TryParse<ModelEntityKind>(type.ToString(), false, out _).Should().BeTrue();
            }
        }

        [Fact]
        public void ACoderOnlyRuleEditorNamesOnlyTheCoderProperty()
        {
            var calculation = Model("EntityAnalysisModelAbstractionCalculation").Editors.Single();
            var function = Model("EntityAnalysisModelInlineFunction").Editors.Single();

            calculation.Roles.Should().Equal(new Dictionary<string, string> { ["coder"] = "functionScript" });
            function.Roles.Should().Equal(new Dictionary<string, string> { ["coder"] = "functionScript" });
            calculation.ParseType.Should().Be((int)RuleParseType.AbstractionCalculation);
            function.ParseType.Should().Be((int)RuleParseType.InlineFunction);
        }

        [Fact]
        public void TheCaseFilterEditorClaimsItsSelectAndFilterJson()
        {
            var editor = Model("CaseWorkflowFilter").Editors.Single();

            editor.Kind.Should().Be("caseFilter");
            editor.ParseType.Should().BeNull();
            editor.Roles.Should().Equal(new Dictionary<string, string>
            {
                ["select"] = "selectJson",
                ["filter"] = "filterJson"
            });
        }

        [Fact]
        public void TheExhaustiveDatasetEditorClaimsBothDatasetIdsAndOnlyReadsTheSourceAndStatus()
        {
            var editor = Model("ExhaustiveSearchInstance").Editors.Single(candidate => candidate.Kind == "exhaustiveDataset");

            editor.ParseType.Should().BeNull();
            editor.Roles.Should().Equal(new Dictionary<string, string>
            {
                ["csv"] = "exhaustiveInstanceCsvId",
                ["json"] = "exhaustiveInstanceJsonId"
            });
            editor.Reads.Should().Equal(new Dictionary<string, string>
            {
                ["source"] = "dataSourceId",
                ["status"] = "statusId"
            });
        }

        [Fact]
        public void TheExhaustiveFilterEditorClaimsTheThreeFilterPropertiesAndReadsTheDataSource()
        {
            var editor = Model("ExhaustiveSearchInstance").Editors.Single(candidate => candidate.Kind == "exhaustiveFilter");

            editor.Roles.Should().Equal(new Dictionary<string, string>
            {
                ["filterJson"] = "filterJson",
                ["filterSql"] = "filterSql",
                ["filterTokens"] = "filterTokens"
            });
            editor.Reads.Should().Equal(new Dictionary<string, string>
            {
                ["source"] = "dataSourceId",
                ["csv"] = "exhaustiveInstanceCsvId",
                ["json"] = "exhaustiveInstanceJsonId"
            });
        }

        [Fact]
        public void TheDataSourceAndStatusAnEditorReadsStayOrdinaryFormFields()
        {
            var model = Model("ExhaustiveSearchInstance");
            var claimed = model.Editors.SelectMany(editor => editor.Roles.Values).ToList();

            foreach (var read in model.Editors.SelectMany(editor => editor.Reads.Values).Distinct())
            {
                model.Fields.Should().Contain(field => field.Name == read);
            }

            claimed.Should().NotContain(["dataSourceId", "statusId"]);
            model.Fields.Single(field => field.Name == "dataSourceId").Editor.Should().BeNull();
        }

        [Fact]
        public void EveryEditorRegistersWithoutAFailure()
        {
            Registry.Failures.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(WithEditors))]
        public void EveryClaimedFieldIsMarkedWithItsEditorAndNoFieldIsClaimedTwice(string area)
        {
            var model = Model(area);
            var claimed = model.Editors.SelectMany(editor => editor.Roles.Values).ToList();

            claimed.Should().OnlyHaveUniqueItems();

            foreach (var editor in model.Editors)
            {
                foreach (var property in editor.Roles.Values)
                {
                    model.Fields.Single(field => field.Name == property).Editor.Should().Be(editor.Kind);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WithEditors))]
        public void ABuilderCapableRuleTypeCarriesAllFourRolesAndACoderOnlyTypeJustTheCoder(string area)
        {
            foreach (var editor in Model(area).Editors.Where(editor => editor.Kind == "ruleBuilder"))
            {
                var builderCapable = (RuleParseType)editor.ParseType!.Value is RuleParseType.GatewayRule
                    or RuleParseType.AbstractionRule or RuleParseType.ActivationRule;

                editor.Roles.Keys.Should().Contain("coder");
                editor.Roles.Keys.OrderBy(role => role).Should().Equal(
                    (builderCapable ? BuilderRoles : ["coder"]).OrderBy(role => role));
            }
        }

        [Theory]
        [MemberData(nameof(WithEditors))]
        public void AnEditorIsGivenItsContextByTheParentKeyOfItsRecord(string area)
        {
            var model = Model(area);

            foreach (var editor in model.Editors)
            {
                model.Keys.Parent.Should().Be(editor.Kind switch
                {
                    "ruleBuilder" or "ruleTools" or "exhaustiveDataset" or "exhaustiveFilter" or "aggregationParameters" =>
                        "entityAnalysisModelId",
                    "datasource" => "visualisationRegistryId",
                    _ => "caseWorkflowId"
                });
            }
        }

        [Fact]
        public void TheAbstractionRuleCarriesTheParametersOfEveryCatalogueFunction()
        {
            var model = Model("EntityAnalysisModelAbstractionRule");
            var editor = model.Editors.Should().ContainSingle(e => e.Kind == "aggregationParameters").Subject;

            editor.Roles.Should().Equal(new Dictionary<string, string>
                { ["parameters"] = "searchFunctionParameters" });
            editor.Reads.Should().Equal(new Dictionary<string, string> { ["function"] = "searchFunctionTypeId" });

            model.Fields.Single(field => field.Name == "searchFunctionParameters").Editor.Should()
                .Be("aggregationParameters");
            model.Fields.Single(field => field.Name == "searchFunctionTypeId").Editor.Should().BeNull();

            var offered = model.Fields.Single(field => field.Name == "searchFunctionTypeId").Options
                .Select(option => Convert.ToInt32(option.Value)).ToList();

            editor.ParameterSets.Select(set => set.Value).Should().BeSubsetOf(offered)
                .And.BeEquivalentTo(Jube.Analytics.Aggregation.AggregationCatalogue.Entries.Select(e => e.Id));

            var trimmed = editor.ParameterSets.Single(set => set.Value == 19);

            trimmed.Parameters.Should().ContainSingle().Which.Should().BeEquivalentTo(new
            {
                Name = "trimFraction", Kind = "number", Default = 0.1, Minimum = 0d, Maximum = 0.5,
                MinimumIsExclusive = false, MaximumIsExclusive = true
            }, options => options.ExcludingMissingMembers());

            editor.ParameterSets.Single(set => set.Value == 23).Parameters.Single().Kind.Should().Be("boolean");
            editor.ParameterSets.Single(set => set.Value == 45).Parameters.Single().Kind.Should().Be("integer");
            editor.ParameterSets.Single(set => set.Value == 24).Parameters.Should().BeEmpty();
        }

        [Fact]
        public void OnlyTheAggregationParametersEditorCarriesParameterSets()
        {
            Registry.Areas.SelectMany(area => Model(area).Editors)
                .Where(editor => editor.Kind != "aggregationParameters")
                .Should().OnlyContain(editor => editor.ParameterSets.Count == 0);
        }

        [Fact]
        public void AFieldLevelEditorAttributeNamesOnlyAComponentTheFormCannotDraw()
        {
            var named = typeof(FormEndpointAttribute).Assembly.GetTypes()
                .SelectMany(type => type.GetProperties())
                .Select(property => (Property: property, Editor: property
                    .GetCustomAttributes(typeof(EditorAttribute), false)
                    .Cast<EditorAttribute>().FirstOrDefault()))
                .Where(pair => pair.Editor != null)
                .Select(pair => $"{pair.Property.DeclaringType!.Name}.{pair.Property.Name}={pair.Editor!.Component}")
                .ToList();

            named.Should().BeEmpty(
                "a component that edits several properties is declared once on the class, not on each property");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryFieldEditorIsOneTheRendererKnows(string area)
        {
            Model(area).Fields.Where(field => field.Editor != null)
                .Select(field => field.Editor!)
                .Where(editor => !KnownFieldEditors.Contains(editor))
                .Should().BeEmpty($"{area} must not name an editor nobody renders");
        }
    }
}

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
using Jube.Dto.Query.FormModel;
using Jube.Service.Query.FormModel;
using Jube.Test.Infrastructure.EndpointCatalogue;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelChildTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static FormModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        public static IEnumerable<object[]> ParentAreas()
        {
            return Registry.Areas.Order()
                .Where(area => Model(area).Children.Count > 0)
                .Select(area => new object[] { area });
        }

        [Fact]
        public void TheModelOffersItsChildrenInTheGroupsTheMenuUses()
        {
            var children = Model("EntityAnalysisModel").Children;

            string[] Within(string hub)
            {
                return children.Where(child => child.Hub == hub).Select(child => child.Label).ToArray();
            }

            Within("References").Should().Equal("Request XPath", "Inline Functions", "Inline Scripts",
                "Gateway Rules", "Sanctions", "Tags", "Lists", "Dictionary");
            Within("Abstraction").Should().Equal("TTL Counters", "Abstraction Rules", "Abstraction Calculation");
            Within("Machine Learning").Should().Equal("HTTP Adaptation", "Exhaustive Adaptation");
            Within("").Should().Equal("Response Elevation Ranges", "Activation Rules", "Cases Workflows", "Reprocessing");
        }

        [Fact]
        public void ACaseWorkflowOffersTheKindsThatSitUnderIt()
        {
            Model("CaseWorkflow").Children.Select(child => child.Area).Should().Equal("CaseWorkflowStatus",
                "CaseWorkflowXPath", "CaseWorkflowForm", "CaseWorkflowAction", "CaseWorkflowDisplay",
                "CaseWorkflowMacro", "CaseWorkflowFilter", "CaseWorkflowFormValidator", "CaseWorkflowReleaseHook");
        }

        [Theory]
        [MemberData(nameof(ParentAreas))]
        public void EveryChildIsAKnownAreaKeyedToItsParentAndListedByARouteThatExists(string area)
        {
            var unreachable = new List<string>();

            foreach (var child in Model(area).Children)
            {
                Registry.TryGet(child.Area, out var childModel).Should().BeTrue($"{area} names {child.Area}");

                childModel!.Keys.Parent.Should().Be(child.ParentKey);
                childModel.Operations.Should().Contain("list");

                if (!EndpointCatalogue.Instance.Routes.Any(route => route.Method == "GET" &&
                                                                    route.Pattern.Trim('/').StartsWith(
                                                                        child.ListRoute.Trim('/') + "/{",
                                                                        StringComparison.OrdinalIgnoreCase)))
                    unreachable.Add($"{child.Area} by {child.ListRoute}/{{id}}");
            }

            unreachable.Should().BeEmpty($"{area} names a list route that does not exist: set FormChild Route");
        }

        [Fact]
        public void TheHierarchyHasNoCycleAndNoAreaHasTwoParents()
        {
            var parentOf = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var area in Registry.Areas)
            {
                foreach (var child in Model(area).Children)
                {
                    parentOf.ContainsKey(child.Area).Should().BeFalse($"{child.Area} has more than one parent");

                    parentOf[child.Area] = area;
                }
            }

            foreach (var start in parentOf.Keys)
            {
                var seen = new HashSet<string> { start };
                var at = start;

                while (parentOf.TryGetValue(at, out var parent))
                {
                    seen.Add(parent).Should().BeTrue($"{start} sits under itself");

                    at = parent;
                }
            }
        }

        [Fact]
        public void TheModelIsTheRootOfTheDeepestChainAndNoChainIsDeeperThanThreeLevels()
        {
            var depth = new Dictionary<string, int>(StringComparer.Ordinal);

            int DepthOf(string area)
            {
                if (depth.TryGetValue(area, out var known)) return known;

                var below = Model(area).Children.Select(child => DepthOf(child.Area)).DefaultIfEmpty(0).Max();

                return depth[area] = below + 1;
            }

            DepthOf("EntityAnalysisModel").Should().Be(3);
            Registry.Areas.Max(DepthOf).Should().Be(3);
        }

        [Fact]
        public void TheChildLabelsAndHubsAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise()
        {
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                [FormModelKeys.Child("EntityAnalysisModel", "EntityAnalysisModelTag")] = "Znaczniki",
                [FormModelKeys.Hub("EntityAnalysisModel", "References")] = "Odniesienia"
            });

            var localised = FormModelLocaliser.Localise(Model("EntityAnalysisModel"), strings);

            var tag = localised.Children.Single(child => child.Area == "EntityAnalysisModelTag");

            tag.Label.Should().Be("Znaczniki");
            tag.Hub.Should().Be("Odniesienia");
            localised.Children.Single(child => child.Area == "EntityAnalysisModelInlineScript").Label
                .Should().Be("Inline Scripts");
            localised.Children.Single(child => child.Area == "EntityAnalysisModelActivationRule").Hub.Should().BeEmpty();
        }
    }
}

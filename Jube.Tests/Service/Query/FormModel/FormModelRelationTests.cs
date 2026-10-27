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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Data.Repository.Models;
using Jube.Dto.Forms;
using Jube.Dto.Query.FormModel;
using Jube.Service.Query.FormModel;
using Jube.Test.Infrastructure.EndpointCatalogue;
using Jube.Validations.EntityAnalysisModelTag;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelRelationTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        private static IEnumerable<FormModelDto> Models()
        {
            return Registry.Areas.Select(area =>
            {
                Registry.TryGet(area, out var model);

                return model!;
            });
        }

        public static IEnumerable<object[]> Approved()
        {
            return Models().Where(model => model.Approval != null).OrderBy(model => model.Area)
                .Select(model => new object[] { model.Area });
        }

        public static IEnumerable<object[]> WithRoles()
        {
            return Models().Where(model => model.Roles != null).OrderBy(model => model.Area)
                .Select(model => new object[] { model.Area });
        }

        public static IEnumerable<object[]> WithValues()
        {
            return Models().Where(model => model.Values != null).OrderBy(model => model.Area)
                .Select(model => new object[] { model.Area });
        }

        private static FormModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        private static string Capitalise(string name)
        {
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        [Fact]
        public void EveryRelationResolvesWithoutAFailure()
        {
            Registry.Failures.Should().BeEmpty();
        }

        [Fact]
        public void AnApprovalTakesItsKindFromTheAreaNameAndItsHistoryFromTheKind()
        {
            var tag = Model("EntityAnalysisModelTag").Approval!;

            tag.Kind.Should().Be((int)EntityApprovalKind.EntityAnalysisModelTag);
            tag.HistoryRoute.Should().Be("/api/EntityAnalysisModelTagVersionHistory");
        }

        [Fact]
        public void TheHttpAdaptationAreaIsNamedDifferentlyFromItsApprovalKindAndSaysSo()
        {
            var adaptation = Model("EntityAnalysisModelAdaptation").Approval!;

            adaptation.Kind.Should().Be((int)EntityApprovalKind.EntityAnalysisModelHttpAdaptation);
            adaptation.HistoryRoute.Should().Be("/api/EntityAnalysisModelHttpAdaptationVersionHistory");
        }

        [Fact]
        public void RolesFollowTheAreaNameForTheEndpointAndTheParentKey()
        {
            var roles = Model("CaseWorkflowStatus").Roles!;

            roles.Endpoint.Should().Be("/api/CaseWorkflowStatusRole");
            roles.ParentKey.Should().Be("caseWorkflowStatusGuid");
        }

        [Fact]
        public void ValuesNameTheValueAreaTheParentFieldTheEditableValuesAndTheParentApprovalKind()
        {
            var dictionary = Model("EntityAnalysisModelDictionary").Values!;

            dictionary.Area.Should().Be("EntityAnalysisModelDictionaryKvp");
            dictionary.Endpoint.Should().Be("/api/EntityAnalysisModelDictionaryKvp");
            dictionary.ParentField.Should().Be("entityAnalysisModelDictionaryId");
            dictionary.ParentKind.Should().Be((int)EntityApprovalKind.EntityAnalysisModelDictionary);
            dictionary.Fields.Select(field => (field.Field, field.Type)).Should()
                .Equal(("kvpKey", "string"), ("kvpValue", "number"));

            var list = Model("EntityAnalysisModelList").Values!;

            list.Fields.Select(field => field.Field).Should().Equal("listValue");
            list.ParentKind.Should().Be((int)EntityApprovalKind.EntityAnalysisModelList);
        }

        [Fact]
        public void NoApprovalKindIsUsedByTwoAreas()
        {
            Models().Where(model => model.Approval != null).Select(model => model.Approval!.Kind)
                .Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [MemberData(nameof(Approved))]
        public void EveryApprovedAreaHasItsVersionHistoryAndItsRetractAndResubmitRoutes(string area)
        {
            var model = Model(area);
            var catalogue = EndpointCatalogue.Instance;
            var history = model.Approval!.HistoryRoute;

            var missing = new List<string>();

            if (!catalogue.Has("GET", $"{history}/ByParent/{{parentId:int}}")) missing.Add($"GET {history}/ByParent");
            if (!catalogue.Has("GET", $"{history}/Compare")) missing.Add($"GET {history}/Compare");
            if (!catalogue.Has("POST", $"{model.Endpoint}/{{id:int}}/Resubmit")) missing.Add("POST Resubmit");
            if (!catalogue.Has("POST", $"{model.Endpoint}/{{id:int}}/Retract")) missing.Add("POST Retract");

            missing.Should().BeEmpty($"{area} approval needs these routes");
        }

        [Fact]
        public void EveryApprovalKindWithAVersionHistoryIsMarkedForApprovalOrIsTheRowOfAValuesGrid()
        {
            var covered = Models().Where(model => model.Approval != null).Select(model => model.Approval!.Kind)
                .ToHashSet();
            var rows = Models().Where(model => model.Values != null).Select(model => model.Values!.Area).ToList();

            foreach (var kind in Enum.GetValues<EntityApprovalKind>())
            {
                var name = kind.ToString();

                if (!EndpointCatalogue.Instance.Has("GET", $"/api/{name}VersionHistory/ByParent/{{parentId:int}}"))
                {
                    continue;
                }

                (covered.Contains((int)kind) || rows.Contains(name)).Should()
                    .BeTrue($"{name} has a version history, so it must carry FormApproval or be a values row");
            }
        }

        [Theory]
        [MemberData(nameof(WithRoles))]
        public void EveryRolesAreaHasItsRoleRoutes(string area)
        {
            var roles = Model(area).Roles!;
            var catalogue = EndpointCatalogue.Instance;
            var missing = new List<string>();

            if (!catalogue.Has("GET", $"{roles.Endpoint}/By{Capitalise(roles.ParentKey)}/{{guid:guid}}"))
                missing.Add("GET by parent");
            if (!catalogue.Has("POST", roles.Endpoint)) missing.Add("POST");
            if (!catalogue.Has("DELETE", $"{roles.Endpoint}/{{id:int}}")) missing.Add("DELETE");

            missing.Should().BeEmpty($"{area} role allocation needs these routes");
        }

        [Fact]
        public void EveryRoleRouteBelongsToAnAreaMarkedForRoles()
        {
            var unmarked = EndpointCatalogue.Instance.Routes
                .Where(route => route.Method == "GET")
                .Select(route => Regex.Match(route.Pattern, @"^/api/(\w+)Role/By(\w+)Guid/\{guid:guid\}$"))
                .Where(match => match.Success && match.Groups[1].Value == match.Groups[2].Value)
                .Select(match => match.Groups[1].Value)
                .Where(area => !Registry.TryGet(area, out var model) || model?.Roles == null)
                .ToList();

            unmarked.Should().BeEmpty("a role endpoint exists for these areas, which must carry FormRoles");
        }

        [Theory]
        [MemberData(nameof(WithValues))]
        public void EveryValuesAreaReadsAndWritesItsRowsThroughRoutesThatExist(string area)
        {
            var values = Model(area).Values!;
            var catalogue = EndpointCatalogue.Instance;
            var missing = new List<string>();

            if (!catalogue.Has("GET", $"{values.Endpoint}/By{Capitalise(values.ParentField)}"))
                missing.Add("GET by parent");
            if (!catalogue.Has("POST", values.Endpoint)) missing.Add("POST");
            if (!catalogue.Has("PUT", values.Endpoint)) missing.Add("PUT");
            if (!catalogue.HasUnder("DELETE", values.Endpoint)) missing.Add("DELETE");
            if (!catalogue.Has("POST", "/api/EntityApproval/ValueStatus")) missing.Add("POST value status");

            missing.Should().BeEmpty($"{area} values need these routes");
            values.Fields.Should().NotBeEmpty();
        }
    }
}

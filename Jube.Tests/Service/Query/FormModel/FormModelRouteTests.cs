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
using Jube.Service.Query.FormModel;
using Jube.Validations.EntityAnalysisModelTag;
using Jube.Test.Infrastructure.EndpointCatalogue;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class FormModelRouteTests
    {
        private static readonly FormModelRegistry Registry =
            FormModelRegistry.Build(typeof(FormEndpointAttribute).Assembly,
                typeof(EntityAnalysisModelTagDtoValidator).Assembly);

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        [Fact]
        public void EveryEndpointMapperRegistersWithoutNeedingAnyService()
        {
            EndpointCatalogue.Instance.Failures.Should().BeEmpty();
            EndpointCatalogue.Instance.Routes.Should().NotBeEmpty();
        }

        [Fact]
        public void TheCatalogueSeesTheFormModelRoutesItself()
        {
            EndpointCatalogue.Instance.Has("GET", "/api/FormModel/{area}").Should().BeTrue();
            EndpointCatalogue.Instance.Has("GET", "/api/FormModel").Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryFormAreaHasTheRoutesForTheOperationsItDeclares(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var catalogue = EndpointCatalogue.Instance;
            var route = model!.Endpoint;
            var missing = new List<string>();

            if (model.Operations.Contains("list") && !catalogue.Has("GET", route)) missing.Add($"GET {route}");
            if (model.Operations.Contains("read") && !catalogue.HasUnder("GET", route)) missing.Add($"GET {route}/...");
            if (model.Operations.Contains("create") && !catalogue.Has("POST", route)) missing.Add($"POST {route}");
            if (model.Operations.Contains("update") && !catalogue.Has("PUT", route)) missing.Add($"PUT {route}");
            if (model.Operations.Contains("delete") && !catalogue.HasUnder("DELETE", route)) missing.Add($"DELETE {route}");

            missing.Should().BeEmpty($"{area} must expose a route for every operation it declares");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryFormAreaDeclaresAtLeastOneOperation(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            model!.Operations.Should().NotBeEmpty();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryLookupSourceIsARouteThatExists(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var sources = model!.Fields.Where(field => field.Lookup != null)
                .Select(field => field.Lookup!.Source)
                .ToList();

            sources.Where(source => !source.StartsWith("/api/", StringComparison.Ordinal)).Should().BeEmpty();
            sources.Where(source => !EndpointCatalogue.Instance.HasUnder("GET", source)).Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryLookupWithAParentTakesItAsTheLastSegmentOfARouteAndNamesAFieldOfTheArea(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            var names = model!.Fields.Select(field => field.Name)
                .Append(model.Keys.Parent)
                .Where(name => name != null)
                .ToHashSet(StringComparer.Ordinal);

            var withParent = model.Fields.Where(field => field.Lookup?.ParentField != null)
                .Select(field => field.Lookup!)
                .ToList();

            withParent.Where(lookup => !names.Contains(lookup.ParentField!)).Select(lookup => lookup.Source)
                .Should().BeEmpty();

            withParent.Where(lookup => !EndpointCatalogue.Instance.Routes.Any(route =>
                    route.Method == "GET" && TakesOneTrailingParameter(route.Pattern, lookup.Source)))
                .Select(lookup => lookup.Source)
                .Should().BeEmpty($"{area} must name a route that takes the parent value as its last segment");
        }

        private static bool TakesOneTrailingParameter(string pattern, string source)
        {
            var prefix = source.Trim('/') + "/{";
            var route = pattern.Trim('/');

            return route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   !route[prefix.Length..].Contains('/');
        }
    }
}

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
using Jube.Dto.Query.GridModel;
using Jube.Service.Query.GridModel;
using Jube.Test.Infrastructure.EndpointCatalogue;
using Jube.Test.Service.Query.FormModel;
using Xunit;

namespace Jube.Test.Service.Query.GridModel
{
    [Trait("Category", "Unit")]
    public sealed class GridModelRegistryTests
    {
        private static readonly GridModelRegistry Registry =
            GridModelRegistry.Build(typeof(GridEndpointAttribute).Assembly);

        private static GridModelDto Model(string area)
        {
            Registry.TryGet(area, out var model).Should().BeTrue();

            return model!;
        }

        public static IEnumerable<object[]> Areas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        [Fact]
        public void EveryDecoratedGridRegistersWithoutAFailure()
        {
            Registry.Areas.Should().NotBeEmpty();
            Registry.Failures.Should().BeEmpty();
        }

        [Fact]
        public void TheArchiverStageGridCarriesItsTimeWindowStageFilterRatioAndOrder()
        {
            var model = Model("ArchiverStagePerformanceCounter");

            model.Endpoint.Should().Be("/api/ArchiverStagePerformanceCounter");
            model.TimeField.Should().Be("createdDate");
            model.WindowMinutes.Should().Be(60);
            model.SortField.Should().Be("createdDate");
            model.Snapshot.Should().BeFalse();

            var stage = model.Filters.Single();

            stage.Param.Should().Be("stageId");
            stage.Kind.Should().Be("select");
            stage.Options.Select(option => option.Label).Should().Equal("Build archive JSON",
                "Case creation dispatch", "RDBMS archive write", "Bulk copy archive buffer");

            var average = model.Columns.Single(column => column.Field == "averageMicroseconds");

            average.Numerator.Should().Be("totalMicroseconds");
            average.Denominator.Should().Be("invokeCount");
            average.Unit.Should().Be("µs");

            model.Columns.Where(column => !column.Hidden).Select(column => column.Field).Should().Equal(
                "entityAnalysisModelName", "stageName", "createdDate", "totalMicroseconds", "minMicroseconds",
                "maxMicroseconds", "invokeCount", "averageMicroseconds", "instance");
            model.Columns.Where(column => column.Hidden).Select(column => column.Field).Should().BeEquivalentTo(
                "id", "entityAnalysisModelGuid", "stageId");
        }

        [Fact]
        public void ASearchOnlyGridHasASearchBoxAndAWindow()
        {
            var model = Model("ApplicationLogEntry");

            model.Search!.Param.Should().Be("search");
            model.Search.Label.Should().Be("Message contains");
            model.Filters.Should().BeEmpty();
            model.TimeField.Should().Be("occurredDate");
        }

        [Fact]
        public void ASnapshotGridHasNoWindowAndNamesItsSort()
        {
            var model = Model("PostgresActivity");

            model.Snapshot.Should().BeTrue();
            model.TimeField.Should().BeNull();
            model.SortField.Should().Be("queryDurationSeconds");
            model.Columns.Single(column => column.Field == "queryDurationSeconds").Unit.Should().Be("s");
            model.Columns.Single(column => column.Field == "pid").Format.Should().Be("code");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryGridEndpointIsARouteThatTakesTheParametersTheGridWillSend(string area)
        {
            var model = Model(area);
            var route = EndpointCatalogue.Instance.Routes.SingleOrDefault(entry =>
                entry.Method == "GET" && string.Equals(entry.Pattern, model.Endpoint, StringComparison.OrdinalIgnoreCase));

            route.Should().NotBeNull($"{area} names {model.Endpoint}");

            var sent = new List<string> { "sortField", "sortDirection" };

            if (!model.Snapshot) sent.AddRange(["take", "from", "to"]);
            if (model.Search != null) sent.Add(model.Search.Param);

            sent.AddRange(model.Filters.Select(filter => filter.Param));

            sent.Where(name => !route!.Parameters.Contains(name, StringComparer.OrdinalIgnoreCase)).Should()
                .BeEmpty($"{area}'s endpoint must accept every parameter its grid sends");
        }

        [Theory]
        [MemberData(nameof(Areas))]
        public void EveryGridColumnHasATitleAndNoTwoColumnsShareAField(string area)
        {
            var model = Model(area);

            model.Columns.Should().OnlyContain(column => column.Title.Length > 0);
            model.Columns.Select(column => column.Field).Should().OnlyHaveUniqueItems();
            model.Columns.Count(column => !column.Hidden).Should().BeGreaterThan(0);
        }

        [Fact]
        public void TheGridTitlesColumnsAndFiltersAreTranslatedWhenARuleSaysSoAndKeepTheEnglishOtherwise()
        {
            var strings = new DictionaryLocalizer(new Dictionary<string, string>
            {
                ["ArchiverStagePerformanceCounter.title"] = "Wydajność",
                ["ArchiverStagePerformanceCounter.column.stageName"] = "Etap",
                ["ArchiverStagePerformanceCounter.filter.stageId"] = "Etap filtra"
            });

            var localised = GridModelLocaliser.Localise(Model("ArchiverStagePerformanceCounter"), strings);

            localised.Title.Should().Be("Wydajność");
            localised.Columns.Single(column => column.Field == "stageName").Title.Should().Be("Etap");
            localised.Columns.Single(column => column.Field == "instance").Title.Should().Be("Instance");
            localised.Filters.Single().Label.Should().Be("Etap filtra");
        }
    }
}

/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasource;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.Repository.VisualisationRegistryDatasource
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourcePreviewServiceTests(DatabaseFixture fx)
        : VisualisationRegistryDatasourceServiceTestBase(fx)
    {
        private const string FiveDays =
            "SELECT d::text AS \"Day\", (d * 2)::numeric AS \"Total\" FROM generate_series(1, 5) d";

        private static DatasourcePreviewRequestDto Request(int registryId, string? command,
            DatasourceDisplayKind kind = 0, bool grid = false)
        {
            return new DatasourcePreviewRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = command,
                DisplayKindId = (int)kind,
                IncludeGrid = grid
            };
        }

        [Fact]
        public async Task AChartIsReturnedAsAnSvgWithTheStylesheetAndNoGridWhenTheGridIsOffAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Chart");

            var card = await service.PreviewAsync(Request(registryId, FiveDays, DatasourceDisplayKind.Column));

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.Message.Should().BeNull();
            card.RowCount.Should().Be(5);
            card.Truncated.Should().BeFalse();
            card.Display.Should().NotBeNull();
            card.Display!.KindId.Should().Be((int)DatasourceDisplayKind.Column);
            card.Display.Markup.Should().StartWith("<svg");
            card.Grid.Should().BeNull();
            card.Style.ModeId.Should().Be((int)DatasourceStyleMode.External);
            card.Style.Version.Should().NotBeNullOrEmpty();
            card.Style.Css.Should().NotBeNullOrEmpty();
            card.Id.Should().Be(0);
            card.Guid.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task WithTheGridOnTheRowsComeAlongAlignedToTheColumnsInSelectOrderAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Grid");

            var card = await service.PreviewAsync(Request(registryId, FiveDays, DatasourceDisplayKind.Column, true));

            card.Grid.Should().NotBeNull();
            card.Grid!.Columns.Select(c => c.Name).Should().Equal("Day", "Total");
            card.Grid.Columns.Select(c => c.Ordinal).Should().Equal(1, 2);
            card.Grid.Columns.Select(c => c.DataTypeId).Should().Equal(1, 3);
            card.Grid.Rows.Should().HaveCount(5);
            card.Grid.Rows[0][0].Should().Be("1");
            Convert.ToDouble(card.Grid.Rows[0][1]).Should().Be(2);
        }

        [Fact]
        public async Task AGridWithNoChartReturnsTheRowsAndNoDisplayAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("GridOnly");

            var card = await service.PreviewAsync(Request(registryId, FiveDays, grid: true));

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.Display.Should().BeNull();
            card.Grid!.Rows.Should().HaveCount(5);
            card.Style.Css.Should().BeNull();
        }

        [Fact]
        public async Task TheStylesheetIsNotResentWhenTheCallerAlreadyHoldsItsVersionAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Css");
            var first = await service.PreviewAsync(Request(registryId, FiveDays, DatasourceDisplayKind.Line));

            var again = Request(registryId, FiveDays, DatasourceDisplayKind.Line);
            again.Card.KnownStylesheetVersion = first.Style.Version;

            var second = await service.PreviewAsync(again);

            second.Style.Version.Should().Be(first.Style.Version);
            second.Style.Css.Should().BeNull();
            second.Display!.Markup.Should().StartWith("<svg");
        }

        [Fact]
        public async Task AStaleStylesheetVersionGetsTheCurrentStylesheetAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("CssStale");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Line);
            request.Card.KnownStylesheetVersion = "00000000";

            var card = await service.PreviewAsync(request);

            card.Style.Css.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InlineStyleIsSelfContainedAndCarriesNoStylesheetAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Inline");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            request.Card.StyleModeId = (int)DatasourceStyleMode.Inline;

            var card = await service.PreviewAsync(request);

            card.Style.ModeId.Should().Be((int)DatasourceStyleMode.Inline);
            card.Style.Css.Should().BeNull();
            card.Style.Version.Should().BeEmpty();
            card.Display!.Markup.Should().Contain("<style");
        }

        [Fact]
        public async Task TheDarkThemeIsAppliedToTheSvgAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Dark");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            request.Card.ThemeId = (int)DatasourceTheme.Dark;

            var card = await service.PreviewAsync(request);

            card.Display!.Markup.Should().Contain("data-theme=\"dark\"");
        }

        [Fact]
        public async Task ARequestedSizeBecomesTheCanvasAndIsClampedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Size");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            request.Card.Width = 900;
            request.Card.Height = 10;

            var card = await service.PreviewAsync(request);

            card.Display!.Markup.Should().Contain("0 0 900 160");
        }

        [Fact]
        public async Task AQueryWithNoRowsIsEmptyWithAReasonAndKeepsTheGridColumnsAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Empty");

            var card = await service.PreviewAsync(Request(registryId, "SELECT 1 AS \"A\" WHERE false",
                DatasourceDisplayKind.Column, true));

            card.StateId.Should().Be((int)DatasourceCardState.Empty);
            card.Message.Should().Contain("no rows");
            card.Display.Should().BeNull();
            card.Grid!.Columns.Should().ContainSingle();
            card.Grid.Rows.Should().BeEmpty();
        }

        [Theory]
        [InlineData("SELECT * FROM \"NoSuchTable\"")]
        [InlineData("SELEC 1")]
        [InlineData("DELETE FROM \"TenantRegistry\"")]
        public async Task AQueryThatFailsIsReportedInTheCardNotThrownAsync(string command)
        {
            var (_, service, registryId) = await ArrangeAsync("Failing");

            var card = await service.PreviewAsync(Request(registryId, command, DatasourceDisplayKind.Column, true));

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().NotBeNullOrWhiteSpace();
            card.Display.Should().BeNull();
            card.Grid.Should().BeNull();
        }

        [Fact]
        public async Task AChartThatCannotBeDrawnIsAnErrorButTheGridStillComesAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Undrawable");

            var card = await service.PreviewAsync(Request(registryId, "SELECT 'a' AS \"T\"",
                DatasourceDisplayKind.Pie, true));

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().Contain("numeric");
            card.Display.Should().BeNull();
            card.Grid!.Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task RowsOverTheCardLimitAreDroppedAndFlaggedNotFailedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Truncate");

            var card = await service.PreviewAsync(Request(registryId,
                "SELECT g AS \"N\" FROM generate_series(1, 6000) g", grid: true));

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.RowCount.Should().Be(5000);
            card.Truncated.Should().BeTrue();
            card.Grid!.Rows.Should().HaveCount(5000);
        }

        [Fact]
        public async Task AnExplicitMappingChoosesTheSeriesAndLeavesTheOtherColumnsForTheGridAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Mapping");
            var request = Request(registryId,
                "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\", 'n' AS \"Notes\", 7 AS \"Beta\"",
                DatasourceDisplayKind.Column, true);

            request.Mapping =
            [
                new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["Beta"] }
            ];

            var card = await service.PreviewAsync(request);

            card.Display!.Markup.Should().Contain("Beta").And.NotContain("Alpha");
            card.Grid!.Columns.Should().HaveCount(4);
        }

        [Fact]
        public async Task ParametersAreBoundAtTheirDeclaredTypesAndTheSuppliedValueWinsAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Parameters");
            await AddParameterAsync(dbContext, registryId, "Threshold", 2, "5", createdUserNames[^1]);
            var id = createdParameterIds[^1];
            const string sql = "SELECT @Threshold::integer AS \"T\"";

            var byDefault = await service.PreviewAsync(Request(registryId, sql, grid: true));
            byDefault.Grid!.Rows[0][0].Should().Be(5);

            var request = Request(registryId, sql, grid: true);
            request.Card.Parameters = [new DatasourceCardParameterDto { Id = id, Value = "9" }];

            var supplied = await service.PreviewAsync(request);
            supplied.Grid!.Rows[0][0].Should().Be(9);
        }

        [Fact]
        public async Task AParameterValueOfTheWrongTypeIsAValidationErrorAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("BadParameter");
            await AddParameterAsync(dbContext, registryId, "Threshold", 2, "5", createdUserNames[^1]);
            var request = Request(registryId, "SELECT @Threshold::integer AS \"T\"");
            request.Card.Parameters = [new DatasourceCardParameterDto { Id = createdParameterIds[^1], Value = "abc" }];

            var act = () => service.PreviewAsync(request);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Threshold"));
        }

        [Fact]
        public async Task AParameterOfAnotherRegistryIsRefusedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("ForeignParameter");
            var request = Request(registryId, "SELECT 1 AS \"A\"");
            request.Card.Parameters = [new DatasourceCardParameterDto { Id = int.MaxValue, Value = "1" }];

            var act = () => service.PreviewAsync(request);

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task SpansAndPriorityAreCarriedThroughToTheCardAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Layout");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            request.ColumnSpan = 3;
            request.RowSpan = 2;
            request.Priority = 7;

            var card = await service.PreviewAsync(request);

            card.ColumnSpan.Should().Be(3);
            card.RowSpan.Should().Be(2);
            card.Priority.Should().Be(7);
        }

        [Fact]
        public async Task ValuesAreNormalisedForJsonDatesAsIsoTextAndNonFiniteNumbersAsNullAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Normalise");

            var card = await service.PreviewAsync(Request(registryId,
                "SELECT TIMESTAMP '2026-01-02 03:04:05' AS \"At\", 'NaN'::double precision AS \"N\", " +
                "ARRAY[1, 2] AS \"Arr\", NULL::text AS \"Nothing\", 'abc'::bytea AS \"Raw\"", grid: true));

            var row = card.Grid!.Rows[0];
            row[0].Should().Be("2026-01-02T03:04:05Z");
            row[1].Should().BeNull();
            ((IEnumerable<object?>)row[2]!).Should().Equal(1, 2);
            row[3].Should().BeNull();
            row[4].Should().BeNull();

            var json = JsonSerializer.Serialize(card);
            json.Should().NotContain("NaN");
        }

        [Fact]
        public async Task ALongFormResultIsSplitIntoSeriesByTheSeriesRoleAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Split");
            var request = Request(registryId,
                "SELECT * FROM (VALUES ('Mon','north',1),('Mon','south',2),('Tue','north',3),('Tue','south',4)) v(\"Day\",\"Region\",\"Sales\")",
                DatasourceDisplayKind.Column);

            request.Mapping =
            [
                new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Series, Columns = ["Region"] }
            ];

            var card = await service.PreviewAsync(request);

            card.StateId.Should().Be((int)DatasourceCardState.Ready, card.Message);
            card.Display!.Markup.Should().Contain("north").And.Contain("south");
        }

        [Fact]
        public async Task AForeignRegistryIsNotFoundAsync()
        {
            var dbContext = OpenContext();
            var (_, mine) = await CreateTenantWithPermissionAsync(dbContext, "PreviewMine", [DatasourcePermissionSpec]);
            var (otherTenant, otherUser) =
                await CreateTenantWithPermissionAsync(dbContext, "PreviewTheirs", [DatasourcePermissionSpec]);

            var theirs = await CreateRegistryAsync(dbContext, otherTenant, otherUser);
            var service = await BuildServiceAsync(dbContext, mine);

            var act = () => service.PreviewAsync(Request(theirs, "SELECT 1 AS \"A\""));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task PreviewRequiresTheDatasourcePermissionAsync()
        {
            var dbContext = OpenContext();
            var (tenantId, _) = await CreateTenantWithPermissionAsync(dbContext, "PreviewOwner", [DatasourcePermissionSpec]);
            var (_, without) = await CreateTenantWithPermissionAsync(dbContext, "PreviewWithout", []);
            var registryId = await CreateRegistryAsync(dbContext, tenantId, without);
            var service = await BuildServiceAsync(dbContext, without);

            var act = () => service.PreviewAsync(Request(registryId, "SELECT 1 AS \"A\""));

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Theory]
        [InlineData(0, "SELECT 1 AS \"A\"", 0, "VisualisationRegistryId")]
        [InlineData(1, "  ", 0, "Command")]
        [InlineData(1, "SELECT 1 AS \"A\"", 999, "DisplayKindId")]
        public async Task AnIncompletePreviewRequestIsAValidationErrorAsync(int registryId, string command, int kind,
            string property)
        {
            var (_, service, _) = await ArrangeAsync("PreviewIncomplete");

            var request = Request(registryId, command);
            request.DisplayKindId = kind;

            var act = () => service.PreviewAsync(request);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == property);
        }

        [Fact]
        public async Task AMappingNamingAnUnknownRoleIsAValidationErrorAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("UnknownRole");
            var request = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            request.Mapping = [new DatasourceMappingEntryDto { RoleId = 999, Columns = ["Day"] }];

            var act = () => service.PreviewAsync(request);

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task AnEmptyMappingEntryOrParameterIsAValidationErrorNotACrashAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("NullEntries");

            var mapping = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            mapping.Mapping = [null!];

            var parameter = Request(registryId, FiveDays, DatasourceDisplayKind.Column);
            parameter.Card.Parameters = [null!];

            await ((Func<Task>)(() => service.PreviewAsync(mapping))).Should().ThrowAsync<DtoValidationException>();
            await ((Func<Task>)(() => service.PreviewAsync(parameter))).Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task ChartKindsListsEveryKindWithItsRolesAsync()
        {
            var (_, service, _) = await ArrangeAsync("Kinds");

            var kinds = await service.ChartKindsAsync();

            kinds.Select(k => k.KindId).Should().BeEquivalentTo(
                Jube.Service.Charts.Datasource.DatasourceChartKinds.All.Select(k => (int)k.Kind));

            var column = kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Column);
            column.Roles.Select(r => r.RoleId).Should().Equal(
                (int)DatasourceChartRole.Category, (int)DatasourceChartRole.Value, (int)DatasourceChartRole.Series);
            column.Roles.Single(r => r.RoleId == (int)DatasourceChartRole.Value).Many.Should().BeTrue();
            column.Roles.Single(r => r.RoleId == (int)DatasourceChartRole.Series).Optional.Should().BeTrue();
        }

        [Fact]
        public async Task ChartKindsRequiresTheDatasourcePermissionAsync()
        {
            var dbContext = OpenContext();
            var (_, without) = await CreateTenantWithPermissionAsync(dbContext, "KindsWithout", []);
            var service = await BuildServiceAsync(dbContext, without);

            var act = () => service.ChartKindsAsync();

            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}

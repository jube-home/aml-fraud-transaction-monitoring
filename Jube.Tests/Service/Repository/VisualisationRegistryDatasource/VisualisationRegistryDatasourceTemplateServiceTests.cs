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
using System.Linq;
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
    public sealed class VisualisationRegistryDatasourceTemplateServiceTests(DatabaseFixture fx)
        : VisualisationRegistryDatasourceServiceTestBase(fx)
    {
        private const string TwoRows =
            "SELECT * FROM (VALUES ('North', 10.5), ('South', 20)) AS t(\"Region\", \"Total\")";

        private const string Good =
            "<ul><li v-for=\"row in rows\">[@ row.Region @]: [@ row.Total | number(1) @]</li></ul>";

        private static DatasourcePreviewRequestDto Preview(int registryId, string? template, bool grid = false)
        {
            return new DatasourcePreviewRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = TwoRows,
                DisplayKindId = (int)DatasourceDisplayKind.Template,
                TemplateText = template,
                IncludeGrid = grid
            };
        }

        private static VisualisationRegistryDatasourceDto NewDatasource(int registryId, string? template)
        {
            return new VisualisationRegistryDatasourceDto
            {
                VisualisationRegistryId = registryId,
                Name = $"{DatabaseFixture.Prefix}Tmpl{Guid.NewGuid():N}"[..40],
                Active = true,
                Command = TwoRows,
                Priority = 1,
                RowSpan = 1,
                ColumnSpan = 1,
                IncludeGrid = false,
                DisplayKindId = (int)DatasourceDisplayKind.Template,
                TemplateText = template
            };
        }

        [Fact]
        public async Task APreviewRendersTheTemplateAsEscapedHtmlWithNoChartStylesheetAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplPreview");

            var card = await service.PreviewAsync(Preview(registryId, Good));

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.Display!.KindId.Should().Be((int)DatasourceDisplayKind.Template);
            card.Display.MarkupKindId.Should().Be((int)DatasourceMarkupKind.Html);
            card.Display.Markup.Should().Be("<ul><li>North: 10.5</li><li>South: 20.0</li></ul>");
            card.Style.Css.Should().BeNull();
            card.Grid.Should().BeNull();
        }

        [Fact]
        public async Task ATemplateAndTheGridComeBackTogetherWhenTheGridIsOnAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplGrid");

            var card = await service.PreviewAsync(Preview(registryId, Good, true));

            card.Display!.Markup.Should().Contain("North");
            card.Grid!.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ABrokenTemplateIsAnErrorCardWithLineAndColumnButTheGridStillComesAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplBroken");

            var card = await service.PreviewAsync(Preview(registryId, "<p>\n  [@ nope @]\n</p>", true));

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().Contain("line 2").And.Contain("nope");
            card.Display.Should().BeNull();
            card.Grid.Should().NotBeNull();
        }

        [Fact]
        public async Task ATemplateKindWithNoTextIsAnErrorCardAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplEmpty");

            var card = await service.PreviewAsync(Preview(registryId, null));

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().Contain("empty");
        }

        [Fact]
        public async Task HostileRowDataIsEscapedInThePreviewAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplHostile");
            var request = Preview(registryId, "<p>[@ rows[0].Region @]</p>");
            request.Command = "SELECT '<script>alert(1)</script>' AS \"Region\"";

            var card = await service.PreviewAsync(request);

            card.Display!.Markup.Should().Be("<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>");
        }

        [Fact]
        public async Task IntrospectionReportsATemplateErrorOnlyWhenTheTemplateKindIsAskedAboutAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplIntro");

            var bad = await service.IntrospectAsync(new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = TwoRows,
                DisplayKindId = (int)DatasourceDisplayKind.Template,
                TemplateText = "<p>[@ rows[ @]</p>"
            });

            var good = await service.IntrospectAsync(new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = TwoRows,
                DisplayKindId = (int)DatasourceDisplayKind.Template,
                TemplateText = Good
            });

            var unasked = await service.IntrospectAsync(new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = TwoRows,
                TemplateText = "<p>[@ rows[ @]</p>"
            });

            bad.Valid.Should().BeTrue();
            bad.TemplateError.Should().Contain("line 1");
            good.TemplateError.Should().BeNull();
            good.MappingError.Should().BeNull();
            unasked.TemplateError.Should().BeNull();
        }

        [Fact]
        public async Task TheTemplateKindAlwaysFitsByPositionAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplFits");

            var result = await service.IntrospectAsync(new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = "SELECT 1 AS \"Only\""
            });

            result.Kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Template).FitsByPosition.Should()
                .BeTrue();
        }

        [Fact]
        public async Task ChartKindsListsTheTemplateKindWithNoRolesAsync()
        {
            var (_, service, _) = await ArrangeAsync("TmplKinds");

            var kinds = await service.ChartKindsAsync();

            var template = kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Template);

            template.Roles.Should().BeEmpty();
            template.Name.Should().Be("Template");
        }

        [Fact]
        public async Task AValidTemplateIsSavedAndReadBackAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplSave");

            var saved = await service.InsertAsync(NewDatasource(registryId, Good));
            createdDatasourceIds.Add(saved.Id);

            var read = await service.GetByIdAsync(saved.Id);

            read!.DisplayKindId.Should().Be((int)DatasourceDisplayKind.Template);
            read.TemplateText.Should().Be(Good);
            read.Mapping.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("<script>x</script>")]
        [InlineData("<p>[@ nope @]</p>")]
        public async Task AnInvalidTemplateIsRefusedOnSaveAsync(string? template)
        {
            var (_, service, registryId) = await ArrangeAsync("TmplRefuse");

            var act = () => service.InsertAsync(NewDatasource(registryId, template));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == "TemplateText");
        }

        [Fact]
        public async Task ATemplateWithAMappingRoleIsRefusedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TmplMapping");
            var dto = NewDatasource(registryId, Good);
            dto.Mapping = [new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["Total"] }];

            var act = () => service.InsertAsync(dto);

            await act.Should().ThrowAsync<DtoValidationException>();
        }
    }
}

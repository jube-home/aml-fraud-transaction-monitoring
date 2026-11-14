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
using System.Globalization;
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
    public sealed class VisualisationRegistryDatasourceLocalisationServiceTests(DatabaseFixture fx)
        : VisualisationRegistryDatasourceServiceTestBase(fx)
    {
        private static async Task<T> InAsync<T>(string culture, Func<Task<T>> action)
        {
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(culture);

                return await action();
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        [Theory]
        [InlineData("pl", "Wykres kolumnowy", "Kategoria")]
        [InlineData("ar", "مخطط أعمدة", "الفئة")]
        [InlineData("en", "Column chart", "Category")]
        public async Task ChartKindsAreListedInThePageLanguageAsync(string culture, string columnName, string categoryName)
        {
            var (_, service, _) = await ArrangeAsync("KindsLanguage");

            var kinds = await InAsync(culture, () => service.ChartKindsAsync());

            var column = kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Column);
            column.Name.Should().Be(columnName);
            column.Roles.First().Name.Should().Be(categoryName);
        }

        [Fact]
        public async Task AFailedPreviewExplainsItselfInThePageLanguageAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("PreviewLanguage");

            var card = await InAsync("pl", () => service.PreviewAsync(new DatasourcePreviewRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = "SELECT 1 AS \"A\" WHERE false",
                DisplayKindId = (int)DatasourceDisplayKind.Column,
                IncludeGrid = true
            }));

            card.StateId.Should().Be((int)DatasourceCardState.Empty);
            card.Message.Should().Be("Zapytanie nie zwróciło wierszy.");
        }

        [Fact]
        public async Task AChartThatCannotBeDrawnExplainsItselfInThePageLanguageWithTranslatedTermsAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("UndrawableLanguage");

            var card = await InAsync("pl", () => service.PreviewAsync(new DatasourcePreviewRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = "SELECT 'a' AS \"T\"",
                DisplayKindId = (int)DatasourceDisplayKind.Pie,
                IncludeGrid = true
            }));

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().Contain("Wykres kołowy").And.Contain("kolumna liczbowa");
            card.Message.Should().NotContain("numeric");
        }

        [Fact]
        public async Task ADuplicateColumnIsReportedByIntrospectionInThePageLanguageAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("IntrospectLanguage");

            var result = await InAsync("pl", () => service.IntrospectAsync(new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = "SELECT 1 AS \"Amount\", 2 AS \"Amount\""
            }));

            result.Valid.Should().BeFalse();
            result.Error.Should().Be("Nazwa kolumny „Amount” występuje więcej niż raz. Nadaj każdej kolumnie własny alias.");
        }

        [Fact]
        public async Task AMappingThatNamesAMissingColumnIsRefusedInThePageLanguageAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("MappingLanguage");

            var dto = new VisualisationRegistryDatasourceDto
            {
                VisualisationRegistryId = registryId,
                Name = $"{DatabaseFixture.Prefix}Lang{Guid.NewGuid():N}"[..40],
                Active = true,
                Command = "SELECT 'a' AS \"Day\", 1 AS \"Alpha\"",
                IncludeGrid = true,
                DisplayKindId = (int)DatasourceDisplayKind.Column,
                Mapping = [new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["Gamma"] }]
            };

            var act = () => InAsync("pl", () => service.InsertAsync(dto));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Mapowanie wskazuje kolumnę „Gamma”"));
        }
    }
}

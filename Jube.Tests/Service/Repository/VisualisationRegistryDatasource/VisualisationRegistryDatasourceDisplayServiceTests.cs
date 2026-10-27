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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasource;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Repository.VisualisationRegistryDatasource
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourceDisplayServiceTests(DatabaseFixture fx)
        : VisualisationRegistryDatasourceServiceTestBase(fx)
    {
        private const string ThreeColumns =
            "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\", 7 AS \"Beta\", 'n' AS \"Notes\"";

        private static VisualisationRegistryDatasourceDto NewDatasource(int registryId, string command,
            DatasourceDisplayKind kind = 0)
        {
            return new VisualisationRegistryDatasourceDto
            {
                VisualisationRegistryId = registryId,
                Name = $"{DatabaseFixture.Prefix}Disp{Guid.NewGuid():N}"[..40],
                Active = true,
                Command = command,
                Priority = 1,
                RowSpan = 1,
                ColumnSpan = 1,
                IncludeGrid = true,
                DisplayKindId = (int)kind
            };
        }

        private static DatasourceMappingEntryDto Role(DatasourceChartRole role, params string[] columns)
        {
            return new DatasourceMappingEntryDto { RoleId = (int)role, Columns = [.. columns] };
        }

        private async Task<Data.Poco.VisualisationRegistryDatasource> StoredAsync(DbContext dbContext,
            int id)
        {
            return await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().FirstAsync(d => d.Id == id);
        }

        [Fact]
        public async Task TheChartKindIsStoredAndComesBackOnAReadAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Kind");

            var saved = await service.InsertAsync(NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column));
            createdDatasourceIds.Add(saved.Id);

            (await StoredAsync(dbContext, saved.Id)).DisplayKindId.Should().Be((int)DatasourceDisplayKind.Column);
            (await service.GetByIdAsync(saved.Id))!.DisplayKindId.Should().Be((int)DatasourceDisplayKind.Column);
        }

        [Fact]
        public async Task NoChartKindIsStoredAsNullAndReadAsZeroAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("NoKind");

            var saved = await service.InsertAsync(NewDatasource(registryId, ThreeColumns));
            createdDatasourceIds.Add(saved.Id);

            (await StoredAsync(dbContext, saved.Id)).DisplayKindId.Should().BeNull();
            (await service.GetByIdAsync(saved.Id))!.DisplayKindId.Should().Be(0);
        }

        [Fact]
        public async Task TheTemplateTextIsStoredAndReturnedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Template");
            var dto = NewDatasource(registryId, ThreeColumns);
            dto.TemplateText = "[@ row.Alpha @]";

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            (await service.GetByIdAsync(saved.Id))!.TemplateText.Should().Be("[@ row.Alpha @]");
        }

        [Fact]
        public async Task SeriesAreStoredInSelectOrderWithTheirOrdinalsAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Ordinals");

            var saved = await service.InsertAsync(NewDatasource(registryId, ThreeColumns));
            createdDatasourceIds.Add(saved.Id);

            var series = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(s => s.VisualisationRegistryDatasourceId == saved.Id).OrderBy(s => s.Ordinal).ToListAsync();

            series.Select(s => s.Name).Should().Equal("Day", "Alpha", "Beta", "Notes");
            series.Select(s => s.Ordinal).Should().Equal(1, 2, 3, 4);
            series.Should().OnlyContain(s => s.RoleId == null && s.RoleOrder == null);
        }

        [Fact]
        public async Task AnExplicitMappingIsStoredOnTheSeriesRowsAndReadBackAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Mapping");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);

            dto.Mapping =
            [
                Role(DatasourceChartRole.Category, "Day"),
                Role(DatasourceChartRole.Value, "Beta", "Alpha")
            ];

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var series = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(s => s.VisualisationRegistryDatasourceId == saved.Id).OrderBy(s => s.Ordinal).ToListAsync();

            series.Single(s => s.Name == "Beta").RoleId.Should().Be((int)DatasourceChartRole.Value);
            series.Single(s => s.Name == "Beta").RoleOrder.Should().Be(1);
            series.Single(s => s.Name == "Alpha").RoleOrder.Should().Be(2);
            series.Single(s => s.Name == "Notes").RoleId.Should().BeNull();

            var read = await service.GetByIdAsync(saved.Id);
            read!.Mapping.Select(m => m.RoleId).Should().Equal((int)DatasourceChartRole.Category,
                (int)DatasourceChartRole.Value);
            read.Mapping.Single(m => m.RoleId == (int)DatasourceChartRole.Value).Columns.Should()
                .Equal("Beta", "Alpha");
        }

        [Fact]
        public async Task TheMappingIsAlsoReadByGuidAndLeftOutOfListsAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("MappingGuid");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Beta")];

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            (await service.GetByGuidAsync(saved.Guid))!.Mapping.Should().ContainSingle();
            (await service.GetByVisualisationRegistryIdAsync(registryId)).Single().Mapping.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateReplacesTheMappingAndTheVersionKeepsThePreviousKindAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Replace");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Alpha")];

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var update = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Line);
            update.Id = saved.Id;
            update.Name = saved.Name;
            update.Version = saved.Version.GetValueOrDefault();
            update.Mapping = [Role(DatasourceChartRole.Value, "Beta")];

            await service.UpdateAsync(update);

            var read = await service.GetByIdAsync(saved.Id);
            read!.DisplayKindId.Should().Be((int)DatasourceDisplayKind.Line);
            read.Mapping.Should().ContainSingle().Which.Columns.Should().Equal("Beta");

            var version = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceVersion>()
                .FirstAsync(v => v.VisualisationRegistryDatasourceId == saved.Id);

            version.DisplayKindId.Should().Be((int)DatasourceDisplayKind.Column);
        }

        [Fact]
        public async Task ClearingTheMappingOnUpdateReturnsTheColumnsToPositionalAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Clear");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Alpha")];

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var update = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            update.Id = saved.Id;
            update.Name = saved.Name;
            update.Version = saved.Version.GetValueOrDefault();

            await service.UpdateAsync(update);

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .CountAsync(s => s.VisualisationRegistryDatasourceId == saved.Id && s.RoleId != null)).Should().Be(0);
        }

        [Theory]
        [InlineData(999)]
        [InlineData(-1)]
        public async Task AnUnknownChartKindIsAValidationErrorAsync(int kind)
        {
            var (_, service, registryId) = await ArrangeAsync("BadKind");
            var dto = NewDatasource(registryId, ThreeColumns);
            dto.DisplayKindId = kind;

            var act = () => service.InsertAsync(dto);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == "DisplayKindId");
        }

        [Fact]
        public async Task AnUnknownRoleAndADuplicateRoleAreValidationErrorsAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("BadRole");
            var unknown = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            unknown.Mapping = [new DatasourceMappingEntryDto { RoleId = 999, Columns = ["Alpha"] }];

            var duplicate = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            duplicate.Mapping = [Role(DatasourceChartRole.Value, "Alpha"), Role(DatasourceChartRole.Value, "Beta")];

            await ((Func<Task>)(() => service.InsertAsync(unknown))).Should().ThrowAsync<DtoValidationException>();
            await ((Func<Task>)(() => service.InsertAsync(duplicate))).Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task AColumnInTwoRolesIsAValidationErrorAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("TwoRoles");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Category, "Alpha"), Role(DatasourceChartRole.Value, "Alpha")];

            var act = () => service.InsertAsync(dto);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == "Mapping");
        }

        [Fact]
        public async Task AMappingNamingAColumnTheSqlDoesNotReturnIsRefusedWithTheNameAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Missing");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Gamma")];

            var act = () => service.InsertAsync(dto);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Gamma"));
        }

        [Fact]
        public async Task AMappingNamingAMissingColumnIsRefusedEvenWithNoChartKindAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("MissingNoKind");
            var dto = NewDatasource(registryId, ThreeColumns);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Gamma")];

            var act = () => service.InsertAsync(dto);

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task AChartThatTheColumnsCannotSatisfyIsRefusedAtSaveWithAReasonAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Unsatisfied");
            var dto = NewDatasource(registryId, "SELECT 'a' AS \"OnlyText\"", DatasourceDisplayKind.Pie);

            var act = () => service.InsertAsync(dto);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.ErrorMessage.Contains("numeric"));
        }

        [Fact]
        public async Task ARefusedMappingStoresNoDatasourceAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Atomic");
            var dto = NewDatasource(registryId, ThreeColumns, DatasourceDisplayKind.Column);
            dto.Mapping = [Role(DatasourceChartRole.Value, "Gamma")];

            await ((Func<Task>)(() => service.InsertAsync(dto))).Should().ThrowAsync<DtoValidationException>();

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .CountAsync(d => d.VisualisationRegistryId == registryId)).Should().Be(0);
        }

        [Fact]
        public async Task ATemplateOverTheMaximumLengthIsAValidationErrorAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("LongTemplate");
            var dto = NewDatasource(registryId, ThreeColumns);
            dto.TemplateText = new string('x', 65537);

            var act = () => service.InsertAsync(dto);

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == "TemplateText");
        }

        [Fact]
        public async Task ANullMappingFromAClientIsTreatedAsNoMappingAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("NullMapping");
            var dto = NewDatasource(registryId, ThreeColumns);
            dto.Mapping = null!;

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            (await service.GetByIdAsync(saved.Id))!.Mapping.Should().BeEmpty();
        }
    }
}

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
    public sealed class VisualisationRegistryDatasourceIntrospectionServiceTests(DatabaseFixture fx)
        : VisualisationRegistryDatasourceServiceTestBase(fx)
    {
        private static VisualisationRegistryDatasourceIntrospectionRequestDto Request(int registryId, string? command)
        {
            return new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = command
            };
        }

        private static VisualisationRegistryDatasourceDto NewDatasource(int registryId, string command)
        {
            return new VisualisationRegistryDatasourceDto
            {
                VisualisationRegistryId = registryId,
                Name = $"{DatabaseFixture.Prefix}Intro{Guid.NewGuid():N}"[..40],
                Active = true,
                Locked = false,
                Command = command,
                Priority = 1,
                RowSpan = 1,
                ColumnSpan = 1,
                IncludeGrid = true
            };
        }

        [Fact]
        public async Task ARejectedCommandSaysWhereTheDatabaseFoundTheProblemAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Position");

            var result = await service.IntrospectAsync(Request(registryId,
                "SELECT 1 AS \"A\",\n  nope AS \"B\" FROM generate_series(1, 2)"));

            result.Valid.Should().BeFalse();
            result.ErrorLine.Should().Be(2);
            result.ErrorColumn.Should().Be(3);
        }

        [Fact]
        public async Task AnAcceptedCommandHasNoErrorPositionAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("NoPosition");

            var result = await service.IntrospectAsync(Request(registryId, "SELECT 1 AS \"A\""));

            result.Valid.Should().BeTrue();
            result.ErrorLine.Should().BeNull();
            result.ErrorColumn.Should().BeNull();
        }

        [Fact]
        public async Task IntrospectReturnsEachColumnInSelectOrderWithItsClassifiedTypeAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Order");

            var result = await service.IntrospectAsync(Request(registryId,
                "SELECT 1::numeric AS \"Total\", 'x'::text AS \"Label\", now() AS \"At\", true AS \"Flag\", " +
                "3::smallint AS \"Small\", 2::bigint AS \"Count\", 1.5::real AS \"Rate\""));

            result.Valid.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Columns.Select(c => c.Name).Should()
                .Equal("Total", "Label", "At", "Flag", "Small", "Count", "Rate");
            result.Columns.Select(c => c.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6, 7);
            result.Columns.Select(c => c.DataTypeId).Should().Equal(3, 1, 4, 5, 5, 2, 3);
            result.Columns.Should().OnlyContain(c => !string.IsNullOrEmpty(c.DataTypeName));
        }

        [Fact]
        public async Task IntrospectKeepsSelectOrderWhenItDiffersFromAlphabeticalOrderAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Alpha");

            var result = await service.IntrospectAsync(Request(registryId,
                "SELECT 1 AS \"Zebra\", 2 AS \"Apple\", 3 AS \"Mango\""));

            result.Columns.Select(c => c.Name).Should().Equal("Zebra", "Apple", "Mango");
        }

        [Fact]
        public async Task IntrospectReportsADuplicateColumnNameAsInvalidWithAHintAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Duplicate");

            var result = await service.IntrospectAsync(Request(registryId,
                "SELECT 1 AS \"Amount\", 2 AS \"Amount\""));

            result.Valid.Should().BeFalse();
            result.Columns.Should().BeEmpty();
            result.Error.Should().Contain("Amount").And.Contain("more than once");
        }

        [Fact]
        public async Task IntrospectAcceptsASingleUnnamedColumnUnderThePostgresDefaultNameAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Unnamed");

            var result = await service.IntrospectAsync(Request(registryId, "SELECT 1 AS \"Named\", 2"));

            result.Valid.Should().BeTrue(result.Error);
            result.Columns.Select(c => c.Name).Should().Equal("Named", "?column?");
        }

        [Fact]
        public async Task IntrospectReportsMoreThanOneUnnamedColumnAsInvalidWithAnAliasHintAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Unnamed2");

            var result = await service.IntrospectAsync(Request(registryId, "SELECT 1, 2"));

            result.Valid.Should().BeFalse();
            result.Error.Should().Contain("no name").And.Contain("alias");
        }

        [Fact]
        public async Task IntrospectReportsADatabaseSyntaxErrorAsInvalidNotAsAnExceptionAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Syntax");

            var result = await service.IntrospectAsync(Request(registryId, "SELEC 1"));

            result.Valid.Should().BeFalse();
            result.Columns.Should().BeEmpty();
            result.Error.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData("DELETE FROM \"TenantRegistry\"")]
        [InlineData("SELECT 1; SELECT 2")]
        [InlineData("SELECT \"Password\" FROM \"UserRegistry\"")]
        public async Task IntrospectRefusesAnythingButASingleAllowedSelectAsync(string command)
        {
            var (_, service, registryId) = await ArrangeAsync("Refuse");

            var result = await service.IntrospectAsync(Request(registryId, command));

            result.Valid.Should().BeFalse();
            result.Error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task IntrospectBindsTheRegistryParametersAtTheirTypesAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Parameters");

            var user = createdUserNames[^1];
            await AddParameterAsync(dbContext, registryId, "Threshold", 2, "5", user);
            await AddParameterAsync(dbContext, registryId, "Minimum Spend", 3, "1.5", user);
            await AddParameterAsync(dbContext, registryId, "Switch", 5, "1", user);
            await AddParameterAsync(dbContext, registryId, "Since", 4, "7", user);

            var result = await service.IntrospectAsync(Request(registryId,
                "SELECT @Threshold::integer AS \"T\", @Minimum_Spend::double precision AS \"M\" " +
                "WHERE 1::smallint = @Switch AND now() > @Since"));

            result.Valid.Should().BeTrue(result.Error);
            result.Columns.Select(c => c.Name).Should().Equal("T", "M");
            result.Columns.Select(c => c.DataTypeId).Should().Equal(2, 3);
        }

        private static VisualisationRegistryDatasourceIntrospectionRequestDto Shape(int registryId, string command,
            DatasourceDisplayKind kind = 0, params DatasourceMappingEntryDto[] mapping)
        {
            return new VisualisationRegistryDatasourceIntrospectionRequestDto
            {
                VisualisationRegistryId = registryId,
                Command = command,
                DisplayKindId = (int)kind,
                Mapping = [.. mapping]
            };
        }

        [Fact]
        public async Task IntrospectSaysForEveryKindWhetherItFitsTheColumnsByPositionAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Fits");

            var result = await service.IntrospectAsync(Shape(registryId, "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\""));

            result.Kinds.Select(k => k.KindId).Should().BeEquivalentTo(
                Jube.Service.Charts.Datasource.DatasourceChartKinds.All.Select(k => (int)k.Kind));

            result.Kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Column).FitsByPosition.Should().BeTrue();
            result.Kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Column).Reason.Should().BeNull();

            var candlestick = result.Kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Candlestick);
            candlestick.FitsByPosition.Should().BeFalse();
            candlestick.Reason.Should().Contain("High");
        }

        [Fact]
        public async Task IntrospectLeavesTheShapeEmptyWhenTheSqlIsRejectedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("FitsInvalid");

            var result = await service.IntrospectAsync(Shape(registryId, "SELEC 1", DatasourceDisplayKind.Column));

            result.Valid.Should().BeFalse();
            result.Kinds.Should().BeEmpty();
            result.ResolvedMapping.Should().BeEmpty();
        }

        [Fact]
        public async Task IntrospectResolvesTheAskedAboutKindByPositionWhenNothingIsAssignedAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Resolve");

            var result = await service.IntrospectAsync(Shape(registryId,
                "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\", 2 AS \"Beta\"", DatasourceDisplayKind.Column));

            result.MappingError.Should().BeNull();
            result.ResolvedMapping.Select(r => r.RoleId).Should().Equal((int)DatasourceChartRole.Category,
                (int)DatasourceChartRole.Value);

            result.ResolvedMapping.Single(r => r.RoleId == (int)DatasourceChartRole.Value).Columns.Should()
                .Equal("Alpha", "Beta");

            result.ResolvedMapping.Should().OnlyContain(r => !r.Explicit);
        }

        [Fact]
        public async Task IntrospectMarksTheRolesTheCallerAssignedAsExplicitAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Explicit");

            var result = await service.IntrospectAsync(Shape(registryId,
                "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\", 2 AS \"Beta\"", DatasourceDisplayKind.Column,
                new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["Beta"] }));

            var value = result.ResolvedMapping.Single(r => r.RoleId == (int)DatasourceChartRole.Value);
            value.Columns.Should().Equal("Beta");
            value.Explicit.Should().BeTrue();
            result.ResolvedMapping.Single(r => r.RoleId == (int)DatasourceChartRole.Category).Explicit.Should()
                .BeFalse();
        }

        [Theory]
        [InlineData(999)]
        [InlineData(-3)]
        public async Task IntrospectReportsAnUnknownKindAsAMappingErrorNotACrashAsync(int kind)
        {
            var (_, service, registryId) = await ArrangeAsync("FitsUnknownKind");
            var request = Shape(registryId, "SELECT 1 AS \"A\"");
            request.DisplayKindId = kind;

            var result = await service.IntrospectAsync(request);

            result.Valid.Should().BeTrue();
            result.MappingError.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task IntrospectReportsAChartTheColumnsCannotSatisfyAsAMappingErrorAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("FitsUnsatisfied");

            var result = await service.IntrospectAsync(Shape(registryId, "SELECT 'a' AS \"T\"",
                DatasourceDisplayKind.Pie));

            result.Valid.Should().BeTrue();
            result.MappingError.Should().Contain("numeric");
            result.ResolvedMapping.Should().BeEmpty();
        }

        [Fact]
        public async Task IntrospectReportsABadMappingEntryAsAMappingErrorNotACrashAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("FitsBadEntry");

            var unknownRole = await service.IntrospectAsync(Shape(registryId, "SELECT 1 AS \"A\"",
                DatasourceDisplayKind.Column, new DatasourceMappingEntryDto { RoleId = 999, Columns = ["A"] }));

            var twice = await service.IntrospectAsync(Shape(registryId, "SELECT 1 AS \"A\", 2 AS \"B\"",
                DatasourceDisplayKind.Column,
                new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["A"] },
                new DatasourceMappingEntryDto { RoleId = (int)DatasourceChartRole.Value, Columns = ["B"] }));

            var empty = Shape(registryId, "SELECT 1 AS \"A\"", DatasourceDisplayKind.Column);
            empty.Mapping = [null!];

            unknownRole.MappingError.Should().NotBeNullOrWhiteSpace();
            twice.MappingError.Should().NotBeNullOrWhiteSpace();
            (await service.IntrospectAsync(empty)).MappingError.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task IntrospectExplainsWhyAKindDoesNotFitInThePageLanguageAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("FitsLanguage");
            var previous = System.Globalization.CultureInfo.CurrentUICulture;

            try
            {
                System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("pl");

                var result = await service.IntrospectAsync(Shape(registryId, "SELECT 'a' AS \"Day\""));

                var candlestick = result.Kinds.Single(k => k.KindId == (int)DatasourceDisplayKind.Candlestick);
                candlestick.Reason.Should().Contain("Wykres świecowy").And.NotContain("Candlestick");
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public async Task IntrospectRefusesARegistryOfAnotherTenantAsNotFoundAsync()
        {
            var dbContext = OpenContext();

            var (_, userName) = await CreateTenantWithPermissionAsync(dbContext, "Mine", [DatasourcePermissionSpec]);
            var (otherTenantId, otherUser) =
                await CreateTenantWithPermissionAsync(dbContext, "Theirs", [DatasourcePermissionSpec]);

            var theirRegistryId = await CreateRegistryAsync(dbContext, otherTenantId, otherUser);
            var service = await BuildServiceAsync(dbContext, userName);

            var act = () => service.IntrospectAsync(Request(theirRegistryId, "SELECT 1 AS \"A\""));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task IntrospectRefusesAnUnknownRegistryAsNotFoundAsync()
        {
            var (_, service, _) = await ArrangeAsync("Unknown");

            var act = () => service.IntrospectAsync(Request(int.MaxValue, "SELECT 1 AS \"A\""));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task IntrospectRequiresTheDatasourcePermissionAsync()
        {
            var dbContext = OpenContext();

            var (tenantId, _) = await CreateTenantWithPermissionAsync(dbContext, "Owner", [DatasourcePermissionSpec]);
            var (_, userWithout) = await CreateTenantWithPermissionAsync(dbContext, "Without", []);

            var registryId = await CreateRegistryAsync(dbContext, tenantId, userWithout);
            var service = await BuildServiceAsync(dbContext, userWithout);

            var act = () => service.IntrospectAsync(Request(registryId, "SELECT 1 AS \"A\""));

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task IntrospectRefusesACallerWhoIsNotSignedInAsync()
        {
            var dbContext = OpenContext();

            var act = () => BuildServiceAsync(dbContext, null);

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Theory]
        [InlineData(0, "SELECT 1 AS \"A\"", "VisualisationRegistryId")]
        [InlineData(1, null, "Command")]
        [InlineData(1, "   ", "Command")]
        public async Task IntrospectRejectsAnIncompleteRequestAsync(int registryId, string? command, string property)
        {
            var (_, service, _) = await ArrangeAsync("Incomplete");

            var act = () => service.IntrospectAsync(Request(registryId, command));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == property);
        }

        [Fact]
        public async Task IntrospectRejectsACommandOverTheMaximumLengthAsync()
        {
            var (_, service, registryId) = await ArrangeAsync("Long");

            var act = () => service.IntrospectAsync(Request(registryId, "SELECT 1 AS \"A\" -- " + new string('x', 70000)));

            var thrown = await act.Should().ThrowAsync<DtoValidationException>();
            thrown.Which.Result.Errors.Should().Contain(e => e.PropertyName == "Command");
        }

        [Fact]
        public async Task IntrospectStoresNothingAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("Stateless");

            await service.IntrospectAsync(Request(registryId, "SELECT 1 AS \"A\""));

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .CountAsync(w => w.VisualisationRegistryId == registryId)).Should().Be(0);
        }

        [Fact]
        public async Task SaveStoresTheSeriesInSelectOrderWithTheClassifiedTypesAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("SaveOrder");

            var saved = await service.InsertAsync(NewDatasource(registryId,
                "SELECT 1::numeric AS \"Zebra\", 'a'::text AS \"Apple\", now() AS \"Mango\""));
            createdDatasourceIds.Add(saved.Id);

            var series = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => w.VisualisationRegistryDatasourceId == saved.Id).OrderBy(o => o.Id).ToListAsync();

            series.Select(s => s.Name).Should().Equal("Zebra", "Apple", "Mango");
            series.Select(s => s.DataTypeId).Should().Equal(3, 1, 4);
        }

        [Fact]
        public async Task UpdateReplacesTheSeriesWithTheNewColumnsInOrderAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("SaveReplace");

            var saved = await service.InsertAsync(NewDatasource(registryId, "SELECT 1 AS \"One\", 2 AS \"Two\""));
            createdDatasourceIds.Add(saved.Id);

            var update = NewDatasource(registryId, "SELECT 'b'::text AS \"Second\", 1.5::real AS \"First\"");
            update.Id = saved.Id;
            update.Name = saved.Name;
            update.Version = saved.Version.GetValueOrDefault();

            await service.UpdateAsync(update);

            var series = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => w.VisualisationRegistryDatasourceId == saved.Id).OrderBy(o => o.Id).ToListAsync();

            series.Select(s => s.Name).Should().Equal("Second", "First");
            series.Select(s => s.DataTypeId).Should().Equal(1, 3);
        }

        [Fact]
        public async Task SaveWithADuplicateColumnNameIsRefusedAndStoresNoDatasourceAsync()
        {
            var (dbContext, service, registryId) = await ArrangeAsync("SaveDuplicate");

            var dto = NewDatasource(registryId, "SELECT 1 AS \"Amount\", 2 AS \"Amount\"");

            var act = () => service.InsertAsync(dto);

            await act.Should().ThrowAsync<SqlValidationFailedException>();

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .CountAsync(w => w.VisualisationRegistryId == registryId)).Should().Be(0);
        }
    }
}

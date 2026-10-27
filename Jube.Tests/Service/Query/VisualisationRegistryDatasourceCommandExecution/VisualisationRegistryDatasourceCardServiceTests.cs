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
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Exceptions.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Reactivity;
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourceCardServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string FiveDays =
            "SELECT d::text AS \"Day\", (d * 2)::numeric AS \"Total\" FROM generate_series(1, 5) d";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdDatasourceIds = [];
        private readonly List<int> createdParameterIds = [];
        private readonly List<int> createdRegistryIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<DbContext> openContexts = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var logIds = dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).Select(w => w.Id);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .Where(w => logIds.Contains(w.VisualisationRegistryDatasourceExecutionLogId ?? 0)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();

            var datasourceGuids = dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(d => createdDatasourceIds.Contains(d.Id)).Select(d => d.Guid);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceRole>()
                .Where(w => datasourceGuids.Contains(w.VisualisationRegistryDatasourceGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(w => createdDatasourceIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .Where(w => createdParameterIds.Contains(w.Id)).DeleteAsync();

            var registryGuids = dbContext.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(r => createdRegistryIds.Contains(r.Id)).Select(r => r.Guid);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryRole>()
                .Where(w => registryGuids.Contains(w.VisualisationRegistryGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(w => createdRegistryIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.Case>().Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflow>().Where(w => createdWorkflowIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModel>().Where(w => createdModelIds.Contains(w.Id))
                .DeleteAsync();

            foreach (var openContext in openContexts)
            {
                await openContext.DisposeAsync();
            }
        }

        private DbContext OpenContext()
        {
            var dbContext = fx.GetDbContext();
            openContexts.Add(dbContext);

            return dbContext;
        }

        private static Task<VisualisationRegistryDatasourceCommandExecutionService> BuildServiceAsync(
            DbContext dbContext, string? userName)
        {
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ReportConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                             ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty
            });

            return VisualisationRegistryDatasourceCommandExecutionService.CreateAsync(dbContext, userName,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<int> TenantOfAsync(DbContext dbContext, string userName)
        {
            return (await dbContext.GetTable<Data.Poco.UserInTenant>().FirstAsync(u => u.User == userName))
                .TenantRegistryId;
        }

        private async Task<(Data.Poco.VisualisationRegistryDatasource Datasource, int RegistryId)> CreateAsync(
            DbContext dbContext, int tenantId, string command, DatasourceDisplayKind kind, bool grid,
            bool active = true, params string[] grantedUsers)
        {
            var registryGuid = Guid.NewGuid();

            var registryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Reg{Guid.NewGuid():N}"[..40],
                Guid = registryGuid,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });
            createdRegistryIds.Add(registryId);

            var datasource = new Data.Poco.VisualisationRegistryDatasource
            {
                Name = $"{DatabaseFixture.Prefix}Card{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                VisualisationRegistryId = registryId,
                Command = command,
                Active = (byte)(active ? 1 : 0),
                Locked = 0,
                Deleted = 0,
                Version = 1,
                IncludeGrid = (byte)(grid ? 1 : 0),
                DisplayKindId = kind == 0 ? null : (int)kind,
                ColumnSpan = 3,
                RowSpan = 2,
                Priority = 4,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            };

            datasource.Id = await dbContext.InsertWithInt32IdentityAsync(datasource);
            createdDatasourceIds.Add(datasource.Id);

            foreach (var user in grantedUsers)
            {
                var roleGuid = (await dbContext.UserRegistry.FirstAsync(u => u.Name == user)).RoleRegistryGuid;

                await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryRole
                {
                    Guid = Guid.NewGuid(),
                    VisualisationRegistryGuid = registryGuid,
                    RoleRegistryGuid = roleGuid,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                    Version = 1
                });

                await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryDatasourceRole
                {
                    Guid = Guid.NewGuid(),
                    VisualisationRegistryDatasourceGuid = datasource.Guid,
                    RoleRegistryGuid = roleGuid,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                    Version = 1
                });
            }

            return (datasource, registryId);
        }

        private async Task<(DbContext DbContext, VisualisationRegistryDatasourceCommandExecutionService Service,
            Data.Poco.VisualisationRegistryDatasource Datasource, int RegistryId)> ArrangeAsync(string command,
            DatasourceDisplayKind kind, bool grid)
        {
            var dbContext = OpenContext();
            var user = fx.Seed.UserWithPermission;
            var tenantId = await TenantOfAsync(dbContext, user);
            var (datasource, registryId) = await CreateAsync(dbContext, tenantId, command, kind, grid, true, user);
            var service = await BuildServiceAsync(dbContext, user);

            return (dbContext, service, datasource, registryId);
        }

        private async Task<int> CreateCaseOfTenantAsync(DbContext dbContext, int tenantId)
        {
            var model = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            createdModelIds.Add(model);

            var workflowGuid = Guid.NewGuid();

            createdWorkflowIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            }));

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}"[..30],
                CreatedDate = DateTime.UtcNow
            });
            createdCaseIds.Add(id);

            return id;
        }

        private async Task<int> AddParameterAsync(DbContext dbContext, int registryId, string name, int dataTypeId,
            string? defaultValue)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistryParameter
            {
                VisualisationRegistryId = registryId,
                Name = name,
                DataTypeId = dataTypeId,
                DefaultValue = defaultValue,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });

            createdParameterIds.Add(id);

            return id;
        }

        [Fact]
        public async Task ASavedChartComesBackAsAnSvgWithTheCardIdentityAndNoGridWhenTheGridIsOffAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync(FiveDays, DatasourceDisplayKind.Column, false);

            var card = await service.CardAsync(datasource.Id, null);

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.Id.Should().Be(datasource.Id);
            card.Guid.Should().Be(datasource.Guid);
            card.Name.Should().Be(datasource.Name);
            card.ColumnSpan.Should().Be(3);
            card.RowSpan.Should().Be(2);
            card.Priority.Should().Be(4);
            card.Display!.KindId.Should().Be((int)DatasourceDisplayKind.Column);
            card.Display.Markup.Should().StartWith("<svg");
            card.Grid.Should().BeNull();
            card.Style.Css.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WithTheGridOnTheRowsComeAlongAndWithNoChartKindThereIsNoDisplayAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync(FiveDays, 0, true);

            var card = await service.CardAsync(datasource.Id, new DatasourceCardRequestDto());

            card.Display.Should().BeNull();
            card.Grid!.Rows.Should().HaveCount(5);
            card.Grid.Columns.Select(c => c.Name).Should().Equal("Day", "Total");
        }

        [Fact]
        public async Task StoredColumnRolesChooseTheSeriesAsync()
        {
            var (dbContext, service, datasource, _) = await ArrangeAsync(
                "SELECT 'a' AS \"Day\", 1.5 AS \"Alpha\", 7 AS \"Beta\"", DatasourceDisplayKind.Column, true);

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryDatasourceSeries
            {
                VisualisationRegistryDatasourceId = datasource.Id, Name = "Day", DataTypeId = 1, Ordinal = 1
            });

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryDatasourceSeries
            {
                VisualisationRegistryDatasourceId = datasource.Id, Name = "Alpha", DataTypeId = 3, Ordinal = 2
            });

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryDatasourceSeries
            {
                VisualisationRegistryDatasourceId = datasource.Id, Name = "Beta", DataTypeId = 2, Ordinal = 3,
                RoleId = (int)DatasourceChartRole.Value, RoleOrder = 1
            });

            var card = await service.CardAsync(datasource.Id, null);

            card.Display!.Markup.Should().Contain("Beta").And.NotContain("Alpha");
            card.Grid!.Columns.Should().HaveCount(3);
        }

        [Fact]
        public async Task TheStylesheetIsOnlySentToACallerWhoDoesNotAlreadyHoldItsVersionAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync(FiveDays, DatasourceDisplayKind.Line, false);

            var first = await service.CardAsync(datasource.Id, null);
            var second = await service.CardAsync(datasource.Id,
                new DatasourceCardRequestDto { KnownStylesheetVersion = first.Style.Version });

            first.Style.Css.Should().NotBeNullOrEmpty();
            second.Style.Css.Should().BeNull();
            second.Style.Version.Should().Be(first.Style.Version);
        }

        [Fact]
        public async Task ARegistryShownInACaseReceivesTheCaseIdAsItsIdAsync()
        {
            var (dbContext, service, datasource, registryId) = await ArrangeAsync(
                "SELECT @Id::integer AS \"I\"", 0, true);

            await dbContext.GetTable<Data.Poco.VisualisationRegistry>().Where(r => r.Id == registryId)
                .Set(r => r.ContextKindId, (byte?)2).UpdateAsync();

            var caseId = await CreateCaseOfTenantAsync(dbContext, await TenantOfAsync(dbContext,
                fx.Seed.UserWithPermission));

            var card = await service.CardAsync(datasource.Id, new DatasourceCardRequestDto { ContextId = caseId });

            card.StateId.Should().Be((int)DatasourceCardState.Ready);
            card.Grid!.Rows.Should().HaveCount(1);
            card.Grid.Rows[0].Should().Equal(caseId);
        }

        [Fact]
        public async Task ACaseOfAnotherTenantIsNotReachableThroughTheIdAsync()
        {
            var (dbContext, service, datasource, registryId) = await ArrangeAsync(
                "SELECT @Id::integer AS \"I\"", 0, true);

            await dbContext.GetTable<Data.Poco.VisualisationRegistry>().Where(r => r.Id == registryId)
                .Set(r => r.ContextKindId, (byte?)2).UpdateAsync();

            var other = await CreateCaseOfTenantAsync(dbContext, await TenantOfAsync(dbContext,
                fx.Seed.UserTenantB));

            var card = await service.CardAsync(datasource.Id, new DatasourceCardRequestDto { ContextId = other });

            card.Grid!.Rows[0].Should().Equal(new object?[] { null });
        }

        [Fact]
        public async Task TheDirectoryHasNoIdWhateverIsSuppliedAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync("SELECT @Id::integer AS \"I\"", 0, true);

            var card = await service.CardAsync(datasource.Id, new DatasourceCardRequestDto { ContextId = 1 });

            card.StateId.Should().Be((int)DatasourceCardState.Error);
        }

        [Fact]
        public async Task EveryCardIsWrittenToTheExecutionLogWithItsRowsAndParametersAsync()
        {
            var (dbContext, service, datasource, registryId) = await ArrangeAsync(
                "SELECT @Threshold::integer AS \"T\"", 0, true);

            var parameterId = await AddParameterAsync(dbContext, registryId, "Threshold", 2, "5");

            await service.CardAsync(datasource.Id, new DatasourceCardRequestDto
            {
                Parameters = [new DatasourceCardParameterDto { Id = parameterId, Value = "9" }]
            });

            var log = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == datasource.Id);

            log.Records.Should().Be(1);
            log.Error.Should().BeNull();
            log.CreatedUser.Should().Be(fx.Seed.UserWithPermission);

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .SingleAsync(p => p.VisualisationRegistryDatasourceExecutionLogId == log.Id)).Value.Should().Be("9");
        }

        [Fact]
        public async Task AFailingQueryIsAnErrorCardAndIsLoggedWithItsErrorAsync()
        {
            var (dbContext, service, datasource, _) = await ArrangeAsync("SELECT * FROM \"NoSuchTable\"",
                DatasourceDisplayKind.Column, true);

            var card = await service.CardAsync(datasource.Id, null);

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().NotBeNullOrWhiteSpace();
            card.Display.Should().BeNull();
            card.Grid.Should().BeNull();

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == datasource.Id)).Error.Should()
                .NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task AQueryWithNoRowsIsEmptyAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync("SELECT 1 AS \"A\" WHERE false",
                DatasourceDisplayKind.Column, true);

            var card = await service.CardAsync(datasource.Id, null);

            card.StateId.Should().Be((int)DatasourceCardState.Empty);
            card.Grid!.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task RowsOverTheLimitAreFlaggedAndTheLogRecordsWhatWasReadAsync()
        {
            var (dbContext, service, datasource, _) = await ArrangeAsync(
                "SELECT g AS \"N\" FROM generate_series(1, 6000) g", 0, true);

            var card = await service.CardAsync(datasource.Id, null);

            card.Truncated.Should().BeTrue();
            card.RowCount.Should().Be(5000);

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == datasource.Id)).Records.Should().Be(5000);
        }

        [Fact]
        public async Task TheCardNeverCarriesTheSqlAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync(FiveDays, DatasourceDisplayKind.Column, true);

            var card = await service.CardAsync(datasource.Id, null);

            JsonSerializer.Serialize(card).Should().NotContain("generate_series");
        }

        [Fact]
        public async Task ADatasourceTheCallerCannotSeeIsNotFoundAndNotLoggedAsync()
        {
            var dbContext = OpenContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (datasource, _) = await CreateAsync(dbContext, tenantId, FiveDays, 0, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.CardAsync(datasource.Id, null);

            await act.Should().ThrowAsync<NotFoundException>();

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == datasource.Id)).Should().Be(0);
        }

        [Fact]
        public async Task AnInactiveDatasourceIsNotFoundAsync()
        {
            var dbContext = OpenContext();
            var user = fx.Seed.UserWithPermission;
            var tenantId = await TenantOfAsync(dbContext, user);
            var (datasource, _) = await CreateAsync(dbContext, tenantId, FiveDays, 0, true, false, user);
            var service = await BuildServiceAsync(dbContext, user);

            var act = () => service.CardAsync(datasource.Id, null);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AnUnknownDatasourceIsNotFoundAsync()
        {
            var dbContext = OpenContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.CardAsync(int.MaxValue, null);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ACallerWithoutThePermissionIsForbiddenAsync()
        {
            var dbContext = OpenContext();
            var user = fx.Seed.UserWithoutPermission;
            var tenantId = await TenantOfAsync(dbContext, user);
            var (datasource, _) = await CreateAsync(dbContext, tenantId, FiveDays, 0, true, true, user);
            var service = await BuildServiceAsync(dbContext, user);

            var act = () => service.CardAsync(datasource.Id, null);

            var thrown = await act.Should().ThrowAsync<ForbiddenException>();
            thrown.Which.RequiredSpecifications.Should().BeEquivalentTo([28, 1]);

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == datasource.Id)).Should().Be(0);
        }

        [Fact]
        public async Task AParameterValueOfTheWrongTypeOrOfAnotherRegistryOrTwiceIsRefusedAsync()
        {
            var (dbContext, service, datasource, registryId) = await ArrangeAsync("SELECT @Threshold::integer AS \"T\"", 0, true);
            var parameterId = await AddParameterAsync(dbContext, registryId, "Threshold", 2, "5");

            var wrongType = () => service.CardAsync(datasource.Id, new DatasourceCardRequestDto
            {
                Parameters = [new DatasourceCardParameterDto { Id = parameterId, Value = "abc" }]
            });

            var foreign = () => service.CardAsync(datasource.Id, new DatasourceCardRequestDto
            {
                Parameters = [new DatasourceCardParameterDto { Id = int.MaxValue, Value = "1" }]
            });

            var twice = () => service.CardAsync(datasource.Id, new DatasourceCardRequestDto
            {
                Parameters =
                [
                    new DatasourceCardParameterDto { Id = parameterId, Value = "1" },
                    new DatasourceCardParameterDto { Id = parameterId, Value = "2" }
                ]
            });

            await wrongType.Should().ThrowAsync<InvalidParametersException>().WithMessage("*Threshold*");
            await foreign.Should().ThrowAsync<InvalidParametersException>();
            await twice.Should().ThrowAsync<InvalidParametersException>();
        }

        [Fact]
        public async Task AnEmptyParameterEntryIsRefusedNotACrashAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync(FiveDays, 0, true);

            var act = () => service.CardAsync(datasource.Id, new DatasourceCardRequestDto { Parameters = [null!] });

            await act.Should().ThrowAsync<InvalidParametersException>();
        }

        [Fact]
        public async Task ACardExplainsItselfInThePageLanguageAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync("SELECT 1 AS \"A\" WHERE false",
                DatasourceDisplayKind.Column, true);

            var previous = System.Globalization.CultureInfo.CurrentUICulture;

            try
            {
                System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("pl");

                var card = await service.CardAsync(datasource.Id, null);

                card.Message.Should().Be("Zapytanie nie zwróciło wierszy.");
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentUICulture = previous;
            }
        }

        [Fact]
        public async Task AChartThatCannotBeDrawnIsAnErrorCardButTheGridStillComesAsync()
        {
            var (_, service, datasource, _) = await ArrangeAsync("SELECT 'a' AS \"T\"", DatasourceDisplayKind.Pie, true);

            var card = await service.CardAsync(datasource.Id, null);

            card.StateId.Should().Be((int)DatasourceCardState.Error);
            card.Message.Should().Contain("numeric");
            card.Grid!.Rows.Should().ContainSingle();
        }
    }
}

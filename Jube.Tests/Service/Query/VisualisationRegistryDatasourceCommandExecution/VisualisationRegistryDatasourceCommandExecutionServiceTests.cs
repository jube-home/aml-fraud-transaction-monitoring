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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Observability;
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution.Models;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.VisualisationRegistryDatasourceCommandExecution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourceCommandExecutionServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private const string TypeAndValueCommand = "SELECT pg_typeof(@P)::text AS t, (@P)::text AS v";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdDatasourceIds = [];
        private readonly List<int> createdParameterIds = [];
        private readonly List<int> createdRegistryIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var logIds = dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0))
                .Select(w => w.Id);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .Where(w => logIds.Contains(w.VisualisationRegistryDatasourceExecutionLogId ?? 0)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
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
        }

        private static Task<VisualisationRegistryDatasourceCommandExecutionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null, bool assumeLocal = false)
        {
            var realConnectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ?? string.Empty;
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ReportConnectionString"] = realConnectionString,
                ["AssumeLocalDateInPayloadExtraction"] = assumeLocal ? "True" : "False"
            });

            return VisualisationRegistryDatasourceCommandExecutionService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                dynamicEnvironment, auditLog ?? TestLog.NoOp);
        }

        private async Task<int> TenantOfAsync(DbContext dbContext, string userName)
        {
            return (await dbContext.GetTable<Data.Poco.UserInTenant>().FirstAsync(u => u.User == userName))
                .TenantRegistryId;
        }

        private async Task<(int DatasourceId, int RegistryId)> CreateDatasourceAsync(DbContext dbContext,
            int tenantId, string command, params string[] grantedUsers)
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

            var datasourceGuid = Guid.NewGuid();
            var datasourceId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.VisualisationRegistryDatasource
                {
                    Name = $"{DatabaseFixture.Prefix}Ds{Guid.NewGuid():N}"[..40],
                    Guid = datasourceGuid,
                    VisualisationRegistryId = registryId,
                    Command = command,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow
                });
            createdDatasourceIds.Add(datasourceId);

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
                    VisualisationRegistryDatasourceGuid = datasourceGuid,
                    RoleRegistryGuid = roleGuid,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                    Version = 1
                });
            }

            return (datasourceId, registryId);
        }

        private async Task<int> CreateParameterAsync(DbContext dbContext, int registryId, string name,
            int dataTypeId, string? defaultValue = null)
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

        private static JArray Params(params (int Id, object? Value)[] items)
        {
            return new JArray(items.Select(i =>
            {
                var o = new JObject { ["id"] = i.Id };
                if (i.Value != null)
                {
                    o["value"] = JToken.FromObject(i.Value);
                }

                return o;
            }));
        }

        private async Task<(int DatasourceId, int ParameterId)> SingleParameterAsync(DbContext dbContext,
            int dataTypeId, string? defaultValue = null, string? user = null)
        {
            user ??= fx.Seed.UserWithPermission;
            var tenantId = await TenantOfAsync(dbContext, user);
            var (dsId, regId) = await CreateDatasourceAsync(dbContext, tenantId, TypeAndValueCommand, user);
            var pId = await CreateParameterAsync(dbContext, regId, "P", dataTypeId, defaultValue);
            return (dsId, pId);
        }

        private async Task<(string Type, string Value)> RunSingleAsync(int dataTypeId, object? value,
            string? defaultValue = null, bool assumeLocal = false)
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, dataTypeId, defaultValue);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, assumeLocal: assumeLocal);
            var rows = await service.ExecuteAsync(dsId, Params((pId, value)));
            rows.Should().ContainSingle();
            return ((string)rows[0]["t"], (string)rows[0]["v"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserThrowsNotAuthenticatedAsync(string? user)
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, user));
        }

        [Fact]
        public async Task UnmappedAndNoTenantUsersThrowNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task MissingPermissionThrowsForbiddenWithSpecsAndCodeAndExecutesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithoutPermission);
            var (dsId, regId) = await CreateDatasourceAsync(dbContext, tenantId, TypeAndValueCommand,
                fx.Seed.UserWithoutPermission);
            var pId = await CreateParameterAsync(dbContext, regId, "P", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ExecuteAsync(dsId, Params((pId, "x"))));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([28, 1]);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(0);
        }

        [Fact]
        public async Task StringIsPassedThroughAsTextAsync()
        {
            var (type, value) = await RunSingleAsync(1, "hello world");
            type.Should().Be("text");
            value.Should().Be("hello world");
        }

        [Fact]
        public async Task IntegerIsParsedToInt32Async()
        {
            var (type, value) = await RunSingleAsync(2, "42");
            type.Should().Be("integer");
            value.Should().Be("42");
        }

        [Fact]
        public async Task IntegerJsonNumberIsParsedTooAsync()
        {
            var (type, value) = await RunSingleAsync(2, 7);
            type.Should().Be("integer");
            value.Should().Be("7");
        }

        [Fact]
        public async Task DoubleIsParsedToFloat8Async()
        {
            var (type, value) = await RunSingleAsync(3, "3.5");
            type.Should().Be("double precision");
            value.Should().Be("3.5");
        }

        [Fact]
        public async Task ByteIsParsedAsync()
        {
            var (_, value) = await RunSingleAsync(5, "200");
            value.Should().Be("200");
        }

        [Fact]
        public async Task DateWithoutLocalAssumptionIsTreatedAsUtcAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 4);
            const string command =
                "SELECT pg_typeof(@P)::text AS t, to_char(@P, 'YYYY-MM-DD\"T\"HH24:MI:SS') AS v";
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Command, command).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "2024-03-05T10:20:30")));

            rows.Should().ContainSingle();
            ((string)rows[0]["t"]).Should().Be("timestamp without time zone");
            ((string)rows[0]["v"]).Should().Be("2024-03-05T10:20:30");
        }

        [Fact]
        public async Task DateWithLocalAssumptionUsesLocalOffsetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 4);
            const string command =
                "SELECT to_char(@P, 'YYYY-MM-DD\"T\"HH24:MI:SS') AS v";
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Command, command).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, assumeLocal: true);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "2024-07-05T10:20:30")));

            var expected = DateTimeOffset.Parse("2024-07-05T10:20:30", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            ((string)rows[0]["v"]).Should().Be(expected);
        }

        [Fact]
        public async Task DateWithExplicitOffsetIsNormalisedToUtcRegardlessOfAssumptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 4);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Command,
                    "SELECT to_char(@P, 'YYYY-MM-DD\"T\"HH24:MI:SS') AS v").UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, assumeLocal: true);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "2024-03-05T10:20:30+02:00")));

            ((string)rows[0]["v"]).Should().Be("2024-03-05T08:20:30");
        }

        [Fact]
        public async Task UnparseableDateFallsBackToNowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 4);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Command, "SELECT @P AS v").UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var before = DateTime.UtcNow.AddSeconds(-5);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "not a date")));

            ((DateTime)rows[0]["v"]).Should().BeAfter(before).And
                .BeBefore(DateTime.UtcNow.AddSeconds(5));
        }

        [Fact]
        public async Task MissingValueFallsBackToDefaultPerTypeAsync()
        {
            (await RunSingleAsync(1, null, "dflt")).Value.Should().Be("dflt");
            (await RunSingleAsync(2, null, "9")).Value.Should().Be("9");
            (await RunSingleAsync(3, null, "1.25")).Value.Should().Be("1.25");
            (await RunSingleAsync(5, null, "3")).Value.Should().Be("3");
        }

        [Fact]
        public async Task MissingDateValueDefaultsToDaysAgoFromNowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 4, "10");
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Command, "SELECT @P AS v").UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, Params((pId, null)));

            ((DateTime)rows[0]["v"]).Should()
                .BeCloseTo(DateTime.UtcNow.AddDays(-10), TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task UnknownDataTypeIdIsTreatedAsStringAsync()
        {
            var (type, value) = await RunSingleAsync(99, "abc");
            type.Should().Be("text");
            value.Should().Be("abc");
        }

        [Fact]
        public async Task ParameterNameSpacesBecomeUnderscoresAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, regId) = await CreateDatasourceAsync(dbContext, tenantId, "SELECT @My_Name AS v",
                fx.Seed.UserWithPermission);
            var pId = await CreateParameterAsync(dbContext, regId, "My Name", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "spaced")));

            rows[0]["v"].Should().Be("spaced");
        }

        [Theory]
        [InlineData(2, "abc")]
        [InlineData(2, "1.5")]
        [InlineData(3, "abc")]
        [InlineData(5, "256")]
        [InlineData(5, "-1")]
        public async Task UnparseableNumericValueThrowsFormatOrOverflowAndWritesNoLogAsync(int dataTypeId,
            string value)
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, dataTypeId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync(dsId, Params((pId, value))));

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(0);
        }

        [Fact]
        public async Task NullParametersThrowsArgumentNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var execute = typeof(VisualisationRegistryDatasourceCommandExecutionService)
                .GetMethod(nameof(VisualisationRegistryDatasourceCommandExecutionService.ExecuteAsync)).Required();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                (Task<List<IDictionary<string, object>>>)execute.Invoke(service, [1, null, CancellationToken.None])
                    .Required());
        }

        [Fact]
        public async Task DuplicateParameterIdsThrowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.ExecuteAsync(dsId, Params((pId, "a"), (pId, "b"))));
        }

        [Fact]
        public async Task ParameterWithoutIdThrowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, _) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.ExecuteAsync(dsId, new JArray(new JObject { ["value"] = "x" })));
        }

        [Fact]
        public async Task ReturnsDynamicColumnsAndRowsWithRawValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantId,
                "SELECT g AS \"Num\", 'r' || g AS \"Txt\", g * 1.5 AS \"Dbl\", NULL::text AS \"Nul\" FROM generate_series(1,3) g ORDER BY g",
                fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, []);

            rows.Should().HaveCount(3);
            rows[0].Keys.Should().BeEquivalentTo("Num", "Txt", "Dbl", "Nul");
            rows[1]["Num"].Should().Be(2);
            rows[1]["Txt"].Should().Be("r2");
            Convert.ToDouble(rows[2]["Dbl"]).Should().Be(4.5);
            rows[0]["Nul"].Should().BeNull();
        }

        [Fact]
        public async Task EmptyResultReturnsEmptyListAndLogsZeroRecordsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantId, "SELECT 1 AS a WHERE 1 = 0",
                fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, []);

            rows.Should().BeEmpty();
            var log = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == dsId);
            log.Records.Should().Be(0);
            log.Error.Should().BeNull();
        }

        [Fact]
        public async Task SuccessWritesExecutionLogAndParameterRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, regId) = await CreateDatasourceAsync(dbContext, tenantId,
                "SELECT @A AS a, @B AS b", fx.Seed.UserWithPermission);
            var pA = await CreateParameterAsync(dbContext, regId, "A", 2);
            var pB = await CreateParameterAsync(dbContext, regId, "B", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.ExecuteAsync(dsId, Params((pA, "5"), (pB, "bee")));

            var log = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == dsId);
            log.Records.Should().Be(1);
            log.Error.Should().BeNull();
            log.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            log.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
            log.ResponseTime.Should().BeGreaterThanOrEqualTo(0);
            var logParams = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .Where(p => p.VisualisationRegistryDatasourceExecutionLogId == log.Id).ToListAsync();
            logParams.Should().HaveCount(2);
            logParams.Single(p => p.VisualisationRegistryParameterId == pA).Value.Should().Be("5");
            logParams.Single(p => p.VisualisationRegistryParameterId == pB).Value.Should().Be("bee");
        }

        [Fact]
        public async Task FailingCommandIsCapturedAsErrorLoggedAndReturnsNoRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, regId) = await CreateDatasourceAsync(dbContext, tenantId,
                "SELECT * FROM jube_table_that_does_not_exist_xyz WHERE a = @P", fx.Seed.UserWithPermission);
            var pId = await CreateParameterAsync(dbContext, regId, "P", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, Params((pId, "v")));

            rows.Should().BeEmpty();
            var log = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == dsId);
            log.Error.Should().NotBeNullOrWhiteSpace();
            log.Records.Should().Be(0);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .CountAsync(p => p.VisualisationRegistryDatasourceExecutionLogId == log.Id)).Should().Be(1);
        }

        [Fact]
        public async Task NonSelectCommandIsRejectedByParserAndLoggedAsErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantId,
                "DELETE FROM \"UserRegistry\"", fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, []);

            rows.Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task ParameterWithoutValueIsLoggedWithNullValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1, "dflt");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.ExecuteAsync(dsId, Params((pId, null)));

            var log = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .SingleAsync(l => l.VisualisationRegistryDatasourceId == dsId);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLogParameter>()
                .SingleAsync(p => p.VisualisationRegistryDatasourceExecutionLogId == log.Id)).Value.Should().BeNull();
        }

        [Fact]
        public async Task EachExecutionWritesItsOwnLogRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.ExecuteAsync(dsId, Params((pId, "1")));
            await service.ExecuteAsync(dsId, Params((pId, "2")));

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(2);
        }

        [Fact]
        public async Task TenantBUserCannotExecuteTenantADatasourceEvenWithRoleGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantA, "SELECT 'secretA' AS s",
                fx.Seed.UserWithPermission, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var rows = await service.ExecuteAsync(dsId, []);

            rows.Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(0);
        }

        [Fact]
        public async Task TenantAUserCannotExecuteTenantBDatasourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantB, "SELECT 'secretB' AS s",
                fx.Seed.UserWithPermission, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ExecuteAsync(dsId, [])).Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(0);

            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            (await serviceB.ExecuteAsync(dsId, [])).Should().ContainSingle()
                .Which["s"].Should().Be("secretB");
        }

        [Fact]
        public async Task ForeignTenantParameterIdsAreIgnoredForCoercionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantA, "SELECT 1 AS one",
                fx.Seed.UserWithPermission);
            var (_, regBId) = await CreateDatasourceAsync(dbContext, tenantB, "SELECT 1", fx.Seed.UserTenantB);
            var foreignParam = await CreateParameterAsync(dbContext, regBId, "P", 2);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.ExecuteAsync(dsId, Params((foreignParam, "abc")));

            rows.Should().ContainSingle();
        }

        [Fact]
        public async Task DatasourceWithoutRoleGrantForUserReturnsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantA, "SELECT 1 AS one");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ExecuteAsync(dsId, [])).Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
                .CountAsync(l => l.VisualisationRegistryDatasourceId == dsId)).Should().Be(0);
        }

        [Fact]
        public async Task InactiveDatasourceReturnsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var (dsId, _) = await CreateDatasourceAsync(dbContext, tenantA, "SELECT 1 AS one",
                fx.Seed.UserWithPermission);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(d => d.Id == dsId)
                .Set(d => d.Active, (byte)0).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ExecuteAsync(dsId, [])).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExecuteAsync(dsId, Params((pId, "x")), cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(1, []));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task BadParameterLogsErrorWithExceptionAndPropagatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 2);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await Assert.ThrowsAnyAsync<FormatException>(() => service.ExecuteAsync(dsId, Params((pId, "zz"))));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Which.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.ExecuteAsync(dsId, Params((pId, "x")));

            activities.Should().ContainSingle(a =>
                    a.OperationName == "VisualisationRegistryDatasourceCommandExecution.Execute")
                .Which.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.ExecuteAsync(dsId, Params((pId, "x")));

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Execute");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.ExecuteAsync(dsId, Params((pId, "x")));

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Execute");
        }

        [Fact]
        public async Task ExecutionPublishesNoChangeEventAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var (dsId, pId) = await SingleParameterAsync(dbContext, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.ExecuteAsync(dsId, Params((pId, "x")));
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.ExecuteAsync(dsId, []));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueDescriptorHasUniqueNameAndOperationAttributeMatches()
        {
            var tools = new List<ServiceToolDescriptor>();
            typeof(ServiceToolCatalogue).GetMethod("AddVisualisationRegistryDatasourceCommandExecution",
                BindingFlags.NonPublic | BindingFlags.Static).Required().Invoke(null, [tools]);

            tools.Should().ContainSingle().Which.Name.Should()
                .Be("VisualisationRegistryDatasourceCommandExecutionExecute");
            tools[0].Kind.Should().Be(OperationKind.Read);
            tools[0].Name.Should().NotContain("_");
            ServiceToolCatalogue.All.Select(t => t.Name).Should().OnlyHaveUniqueItems();

            typeof(VisualisationRegistryDatasourceCommandExecutionService)
                .GetMethod(nameof(VisualisationRegistryDatasourceCommandExecutionService.ExecuteAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>().Required().Name
                .Should().Be("VisualisationRegistryDatasourceCommandExecutionExecute");
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");
                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(1, []));

                ex.Message.Should().Be("Vous n'avez pas la permission d'effectuer cette action.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}
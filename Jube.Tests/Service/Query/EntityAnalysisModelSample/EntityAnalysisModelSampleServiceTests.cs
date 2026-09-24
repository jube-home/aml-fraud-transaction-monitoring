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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Query.EntityAnalysisModelSample;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSample;
using Jube.Service.Query.EntityAnalysisModelSample;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelSample
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSampleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string realConnectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private static readonly DateTimeOffset windowFrom = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset windowTo = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        private const string StaticHeader =
            "\"EntityAnalysisModelInstanceEntryGuid\",\"ResponseElevation\",\"PrevailingEntityAnalysisModelActivationRuleId\"," +
            "\"EntityAnalysisModelGuid\",\"EntityAnalysisModelActivationRuleCount\",\"CreatedDate\",\"ReferenceDate\"";

        private readonly List<long> createdArchiveIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleIds = createdRoleRegistryIds.Select(id => (int?)id).ToList();
            var modelIds = createdModelIds.Select(id => (int?)id).ToList();

            await dbContext.Archive.Where(w => createdArchiveIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => modelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => roleIds.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Jube.DynamicEnvironment.DynamicEnvironment Env(string? reportConnectionString = null) =>
            TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = realConnectionString,
                ["ReportConnectionString"] = reportConnectionString ?? realConnectionString
            });

        private static Task<EntityAnalysisModelSampleService> BuildServiceAsync(DbContext dbContext,
            string? userName, ILog? log = null, ILog? auditLog = null, IServiceChangeBus? serviceChangeBus = null,
            Jube.DynamicEnvironment.DynamicEnvironment? env = null)
        {
            return EntityAnalysisModelSampleService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), env ?? Env(), auditLog ?? TestLog.NoOp);
        }

        private static EntityAnalysisModelSampleOptionsDto Opts(Guid modelGuid, double sample = 1.0) => new()
        {
            EntityAnalysisModelGuid = modelGuid,
            DateFrom = windowFrom,
            DateTo = windowTo,
            Sample = sample
        };

        private async Task<(string UserName, int TenantId)> CreateUserWithPermissionAsync(DbContext dbContext,
            params int[] permissionSpecificationIds)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}SmpTenant{suffix}",
                Active = 1, Locked = 0, Deleted = 0, Landlord = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantRegistryIds.Add(tenantRegistryId);

            var roleRegistryGuid = Guid.NewGuid();
            var roleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleRegistryGuid,
                Name = $"{DatabaseFixture.Prefix}SmpRole{suffix}",
                Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantRegistryId, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleRegistryId);

            foreach (var permissionSpecificationId in permissionSpecificationIds)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = permissionSpecificationId,
                    RoleRegistryId = roleRegistryId,
                    Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}SmpUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantRegistryId
            });

            return (userName, tenantRegistryId);
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext, int tenantRegistryId,
            byte deleted = 0, params (string Name, int DataTypeId)[] xpaths)
        {
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}SmpModel{Guid.NewGuid():N}"[..40],
                Guid = guid,
                TenantRegistryId = tenantRegistryId,
                Active = 1, Locked = 0, Deleted = deleted, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdModelIds.Add(id);

            foreach (var (name, dataTypeId) in xpaths)
            {
                await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = id,
                    Name = name,
                    DataTypeId = dataTypeId,
                    XPath = $"$.{name}",
                    Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    Guid = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            return (id, guid);
        }

        private static readonly (string Name, int DataTypeId)[] fiveTypes =
        [
            ("aString", 1), ("bInt", 2), ("cFloat", 3), ("dDate", 4), ("eBool", 5)
        ];

        private async Task InsertArchiveAsync(DbContext dbContext, int modelId, Guid modelGuid, string payloadJson,
            Guid? entryGuid = null, DateTime? referenceDate = null)
        {
            var entry = entryGuid ?? Guid.NewGuid();
            var reference = referenceDate ?? new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            var json =
                $"{{\"entityAnalysisModelInstanceEntryGuid\":\"{entry}\",\"responseElevation\":{{\"value\":12.5}}," +
                $"\"prevailingEntityAnalysisModelActivationRuleId\":7,\"entityAnalysisModelGuid\":\"{modelGuid}\"," +
                "\"entityAnalysisModelActivationRuleCount\":3,\"createdDate\":\"2026-03-01T10:00:00Z\"," +
                $"\"referenceDate\":\"{reference:yyyy-MM-ddTHH:mm:ssZ}\",\"payload\":{payloadJson}}}";

            var id = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json,
                EntityAnalysisModelInstanceEntryGuid = entry,
                EntityAnalysisModelId = modelId,
                CreatedDate = DateTime.UtcNow,
                ReferenceDate = reference
            });
            createdArchiveIds.Add(id);
        }

        private static string Text(EntityAnalysisModelSampleFileDto file) => Encoding.UTF8.GetString(file.Content);

        private static string[] Lines(EntityAnalysisModelSampleFileDto file) =>
            Text(file).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        private static string ExpectedRow(Guid entry, Guid modelGuid, string payloadCells) =>
            $"\"{entry}\",12.5,7,\"{modelGuid}\",3,2026-03-01T10:00:00.0000000Z,2026-03-01T10:00:00.0000000Z,{payloadCells}";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, userName, log));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task ExecuteThrowsForbiddenWhenPermissionMissingAndWritesNoAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, tenantId) = await CreateUserWithPermissionAsync(dbContext, 1);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var (userWithoutForty, _) = await CreateUserWithPermissionAsync(dbContext, 1, 2, 3);
            var service = await BuildServiceAsync(dbContext, userWithoutForty);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(Opts(modelGuid)));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([40]);
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                    .CountAsync(w => w.EntityAnalysisModelId == modelId))
                .Should().Be(0);
        }

        [Fact]
        public async Task ExecuteThrowsForbiddenForUserWithoutAnyPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(Opts(Guid.NewGuid())));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(Opts(Guid.NewGuid())));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task PermissionIsCheckedBeforeValidationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ExecuteAsync(new EntityAnalysisModelSampleOptionsDto()));
        }

        public static IEnumerable<object[]> InvalidOptions()
        {
            var ok = Guid.NewGuid();
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = Guid.Empty, DateFrom = windowFrom, DateTo = windowTo, Sample = 0.5 },
                "An Entity Analysis Model must be selected."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = null, DateTo = windowTo, Sample = 0.5 },
                "A start date is required."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = windowFrom, DateTo = null, Sample = 0.5 },
                "An end date is required."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = windowTo, DateTo = windowFrom, Sample = 0.5 },
                "End date must be after the start date."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = windowFrom, DateTo = windowFrom, Sample = 0.5 },
                "End date must be after the start date."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = windowFrom, DateTo = windowTo, Sample = 1.01 },
                "Sample rate must be between 0 and 1."
            ];
            yield return
            [
                new EntityAnalysisModelSampleOptionsDto
                    { EntityAnalysisModelGuid = ok, DateFrom = windowFrom, DateTo = windowTo, Sample = -0.01 },
                "Sample rate must be between 0 and 1."
            ];
        }

        [Theory]
        [MemberData(nameof(InvalidOptions))]
        public async Task ExecuteThrowsDtoValidationExceptionWithLegacyMessagesAndWritesNoAuditRowAsync(
            EntityAnalysisModelSampleOptionsDto options, string expectedMessage)
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, _) = await CreateUserWithPermissionAsync(dbContext, 40);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);
            var before = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .CountAsync(w => w.CreatedUser == userName);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.ExecuteAsync(options));

            ex.Code.Should().Be("ValidationFailed");
            ex.Result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedMessage);
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                    .CountAsync(w => w.CreatedUser == userName))
                .Should().Be(before);
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SampleBoundariesZeroAndOneAreValidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (_, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName);

            await service.ExecuteAsync(Opts(modelGuid, 0.0));
            await service.ExecuteAsync(Opts(modelGuid));
        }

        [Fact]
        public async Task ExecuteThrowsNotFoundForUnknownModelGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, _) = await CreateUserWithPermissionAsync(dbContext, 40);
            var service = await BuildServiceAsync(dbContext, userName);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Opts(Guid.NewGuid())));
            ex.Code.Should().Be("NotFound");
        }

        [Fact]
        public async Task ExecuteThrowsNotFoundForSoftDeletedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 1, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Opts(modelGuid)));
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                    .CountAsync(w => w.EntityAnalysisModelId == modelId))
                .Should().Be(0);
        }

        [Fact]
        public async Task TenantBCannotSampleTenantAModelAndViceVersaAndNoAuditRowIsWrittenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userA, tenantA) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (userB, tenantB) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelAId, modelAGuid) = await CreateModelAsync(dbContext, tenantA, 0, fiveTypes);
            var (modelBId, modelBGuid) = await CreateModelAsync(dbContext, tenantB, 0, fiveTypes);
            await InsertArchiveAsync(dbContext, modelAId, modelAGuid,
                "{\"aString\":\"secretA\",\"bInt\":1,\"cFloat\":1.5,\"dDate\":\"2026-02-01T08:30:00Z\",\"eBool\":true}");
            await InsertArchiveAsync(dbContext, modelBId, modelBGuid,
                "{\"aString\":\"secretB\",\"bInt\":1,\"cFloat\":1.5,\"dDate\":\"2026-02-01T08:30:00Z\",\"eBool\":true}");

            var serviceA = await BuildServiceAsync(dbContext, userA);
            var serviceB = await BuildServiceAsync(dbContext, userB);

            await Assert.ThrowsAsync<NotFoundException>(() => serviceB.ExecuteAsync(Opts(modelAGuid)));
            await Assert.ThrowsAsync<NotFoundException>(() => serviceA.ExecuteAsync(Opts(modelBGuid)));

            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>().CountAsync(w =>
                w.EntityAnalysisModelId == modelAId || w.EntityAnalysisModelId == modelBId)).Should().Be(0);

            var ownA = await serviceA.ExecuteAsync(Opts(modelAGuid));
            Text(ownA).Should().Contain("secretA").And.NotContain("secretB");
            var ownB = await serviceB.ExecuteAsync(Opts(modelBGuid));
            Text(ownB).Should().Contain("secretB").And.NotContain("secretA");
        }

        [Fact]
        public async Task CsvContainsHeaderAndTypeCoercedRowForStringIntFloatDateBooleanAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid,
                "{\"aString\":\"plain\",\"bInt\":42,\"cFloat\":3.5,\"dDate\":\"2026-02-01T08:30:00Z\",\"eBool\":true}",
                entry);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            file.ContentType.Should().Be("text/csv");
            var lines = Lines(file);
            lines.Should().HaveCount(2);
            lines[0].Should().Be(StaticHeader +
                                 ",\"Payload.aString\",\"Payload.bInt\",\"Payload.cFloat\",\"Payload.dDate\",\"Payload.eBool\"");
            lines[1].Should().Be(ExpectedRow(entry, modelGuid,
                "\"plain\",42,3.5,2026-02-01T08:30:00.0000000Z,1"));
            Text(file).Should().EndWith("\n");
        }

        [Fact]
        public async Task CsvQuotesAndEscapesEmbeddedQuotesAndCommasInStringsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("aString", 1));
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"he said \\\"hi\\\", ok\"}", entry);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Lines(file)[1].Should().Be(ExpectedRow(entry, modelGuid, "\"he said \"\"hi\"\", ok\""));
        }

        [Fact]
        public async Task CsvUsesLegacyDefaultsForMissingPayloadFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{}", entry);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Lines(file)[1].Should().Be(ExpectedRow(entry, modelGuid, "\"\",0,0,,0"));
        }

        [Fact]
        public async Task CsvFallsBackToZeroOrEmptyStringWhenCoercionFailsAndLogsInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid,
                "{\"aString\":{\"nested\":1},\"bInt\":\"abc\",\"cFloat\":\"xyz\",\"dDate\":\"not-a-date\",\"eBool\":true}",
                entry);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Lines(file)[1].Should().Be(ExpectedRow(entry, modelGuid, "\"\",0,0,0,1"));
            log.Entries.Count(e => e.Level == "INFO" && e.Message.Contains("Type coercion failed")).Should().Be(4);
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CsvTreatsBooleanFalseAsOneBecauseLegacyDoesNotInspectTheValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("eBool", 5));
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"eBool\":false}", entry);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Lines(file)[1].Should().Be(ExpectedRow(entry, modelGuid, "1"));
        }

        [Fact]
        public async Task CsvHandlesCurrencyAndDoubleAliasDataTypesSixAndSevenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("fSix", 6), ("gSeven", 7));
            var entry = Guid.NewGuid();
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"fSix\":10.25,\"gSeven\":2}", entry);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Lines(file)[1].Should().Be(ExpectedRow(entry, modelGuid, "10.25,2"));
        }

        [Fact]
        public async Task CsvIncludesOneLinePerSampledRowAndHonoursTheDateWindowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("aString", 1));
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"in1\"}");
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"in2\"}");
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"tooEarly\"}",
                referenceDate: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"tooLate\"}",
                referenceDate: new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            var text = Text(file);
            Lines(file).Should().HaveCount(3);
            text.Should().Contain("\"in1\"").And.Contain("\"in2\"")
                .And.NotContain("tooEarly").And.NotContain("tooLate");
        }

        [Fact]
        public async Task SampleZeroReturnsHeaderOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("aString", 1));
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"in1\"}");
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid, 0.0));

            Lines(file).Should().Equal(StaticHeader + ",\"Payload.aString\"");
        }

        [Fact]
        public async Task CsvOnlyContainsArchiveRowsOfTheRequestedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("aString", 1));
            var (otherId, otherGuid) = await CreateModelAsync(dbContext, tenantId, 0, ("aString", 1));
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"mine\"}");
            await InsertArchiveAsync(dbContext, otherId, otherGuid, "{\"aString\":\"theirs\"}");
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            Text(file).Should().Contain("mine").And.NotContain("theirs");
        }

        [Fact]
        public async Task FileNameFollowsSampleModelIdTimestampPatternAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName);

            var file = await service.ExecuteAsync(Opts(modelGuid));

            file.FileName.Should().MatchRegex($"^sample_{modelId}_\\d{{14}}\\.csv$");
            file.ContentType.Should().Be("text/csv");
            var stamp = Regex.Match(file.FileName, @"_(\d{14})\.csv$").Groups[1].Value;
            DateTime.ParseExact(stamp, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
                .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task SuccessWritesOneExecutionLogRowWithRowCountAndParametersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"a\"}");
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"b\"}");
            await InsertArchiveAsync(dbContext, modelId, modelGuid, "{\"aString\":\"c\"}");
            var service = await BuildServiceAsync(dbContext, userName);

            await service.ExecuteAsync(Opts(modelGuid));

            var rows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .Where(w => w.EntityAnalysisModelId == modelId).ToListAsync();
            var row = rows.Should().ContainSingle().Subject;
            row.InError.Should().Be(0);
            row.ErrorStack.Should().BeNull();
            row.RowCount.Should().Be(3);
            row.Sample.Should().Be(1.0);
            row.CreatedUser.Should().Be(userName);
            row.DateFrom.Should().BeCloseTo(windowFrom.UtcDateTime, TimeSpan.FromSeconds(1));
            row.DateTo.Should().BeCloseTo(windowTo.UtcDateTime, TimeSpan.FromSeconds(1));
            row.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            row.ResponseTime.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task SuccessWithZeroRowsStillWritesAuditRowWithRowCountZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName);

            await service.ExecuteAsync(Opts(modelGuid, 0.25));

            var row = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .SingleAsync(w => w.EntityAnalysisModelId == modelId);
            row.InError.Should().Be(0);
            row.RowCount.Should().Be(0);
            row.Sample.Should().Be(0.25);
        }

        [Fact]
        public async Task FailureWritesAuditRowInErrorWithStackAndPropagatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var log = new TestLog();
            var env = Env("this is not a valid connection string");
            var service = await BuildServiceAsync(dbContext, userName, log, env: env);

            await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync(Opts(modelGuid, 0.5)));

            var row = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .SingleAsync(w => w.EntityAnalysisModelId == modelId);
            row.InError.Should().Be(1);
            row.ErrorStack.Should().NotBeNullOrWhiteSpace();
            row.RowCount.Should().BeNull();
            row.Sample.Should().Be(0.5);
            row.CreatedUser.Should().Be(userName);
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Exception != null);
        }

        [Fact]
        public async Task ReportConnectionStringTakesPrecedenceOverConnectionStringAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);

            var env = Env("this is not a valid connection string");
            env.AppSettings("ConnectionString").Should().Be(realConnectionString);
            var failing = await BuildServiceAsync(dbContext, userName, env: env);
            await Assert.ThrowsAnyAsync<Exception>(() => failing.ExecuteAsync(Opts(modelGuid)));

            var working = await BuildServiceAsync(dbContext, userName);
            await working.ExecuteAsync(Opts(modelGuid));

            var rows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                .Where(w => w.EntityAnalysisModelId == modelId).OrderBy(o => o.Id).ToListAsync();
            rows.Select(r => r.InError).Should().Equal((byte?)1, (byte?)0);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndWritesNoAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (modelId, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExecuteAsync(Opts(modelGuid), cts.Token));
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelSampleExecutionLog>()
                    .CountAsync(w => w.EntityAnalysisModelId == modelId, token: CancellationToken.None))
                .Should().Be(0);
        }

        [Fact]
        public async Task ExactlyOneAuditLogLineOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (_, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, auditLog: auditLog);

            await service.ExecuteAsync(Opts(modelGuid));

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Execute");
        }

        [Fact]
        public async Task ReadPublishesNoChangeEventOnSuccessOrFailureAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var (userName, tenantId) = await CreateUserWithPermissionAsync(dbContext, 40);
            var (_, modelGuid) = await CreateModelAsync(dbContext, tenantId, 0, fiveTypes);
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: bus);

            await service.ExecuteAsync(Opts(modelGuid));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ExecuteAsync(Opts(Guid.NewGuid())));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void ExecuteIsDeclaredAsAnIdempotentReadServiceOperationNamedEntityAnalysisModelSampleExecute()
        {
            var attribute =
                typeof(EntityAnalysisModelSampleService).GetMethod(
                        nameof(EntityAnalysisModelSampleService.ExecuteAsync)).Required()
                    .GetCustomAttribute<ServiceOperationAttribute>();
            attribute = attribute.Required();

            attribute.Should().NotBeNull();
            attribute.Name.Should().Be("EntityAnalysisModelSampleExecute");
            attribute.Kind.Should().Be(OperationKind.Read);
            attribute.Idempotent.Should().BeTrue();
        }
    }
}
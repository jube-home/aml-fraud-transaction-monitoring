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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.SessionCaseJournal;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.SessionCaseJournal;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SessionCaseJournal;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.SessionCaseJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SessionCaseJournalServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<Guid> createdCaseWorkflowGuids = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.SessionCaseJournal
                .Where(w => w.CaseWorkflowGuid != null && createdCaseWorkflowGuids.Contains(w.CaseWorkflowGuid.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<SessionCaseJournalService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return SessionCaseJournalService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<Guid> CreateWorkflowAsync(DbContext dbContext, string userName)
        {
            var repository = new Data.Repository.EntityAnalysisModelRepository(dbContext, userName);
            var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
            });
            createdModelIds.Add(model.Id);

            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(id);
            createdCaseWorkflowGuids.Add(guid);
            return guid;
        }

        private static Task<int> InsertJournalDirectAsync(DbContext dbContext, string createdUser, Guid guid,
            string json)
        {
            return dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SessionCaseJournal
            {
                Json = json,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow,
                CaseWorkflowGuid = guid,
            });
        }

        [Fact]
        public async Task CreateServiceWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
        }

        [Fact]
        public async Task CreateServiceWithBlankUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, "  "));
        }

        [Fact]
        public async Task CreateServiceWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task CreateServiceWithUserInNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task GetWithoutPermissionThrowsForbiddenWithCodeAndSpecsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.GetByCaseWorkflowGuidAsync(Guid.NewGuid());
            var thrown = (await act.Should().ThrowAsync<ForbiddenException>()).Which;
            thrown.Code.Should().Be("PermissionDenied");
            thrown.RequiredSpecifications.Should().Equal(1);
        }

        [Fact]
        public async Task CreateWithoutPermissionThrowsForbiddenAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithoutPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "[]" });
            var thrown = (await act.Should().ThrowAsync<ForbiddenException>()).Which;
            thrown.Code.Should().Be("PermissionDenied");
            thrown.RequiredSpecifications.Should().Equal(1);

            (await dbContext.SessionCaseJournal.CountAsync(w => w.CaseWorkflowGuid == guid)).Should().Be(0);
        }

        [Fact]
        public async Task CreateWithNullModelThrowsArgumentNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.CreateAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CreatePersistsAndReturnsServerAssignedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.CreateAsync(new SessionCaseJournalDto
            {
                CaseWorkflowGuid = guid,
                Json = "[{\"field\":\"A\",\"width\":10}]",
            });

            saved.Id.Should().BeGreaterThan(0);
            saved.CaseWorkflowGuid.Should().Be(guid);
            saved.Json.Should().Be("[{\"field\":\"A\",\"width\":10}]");
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30));

            var row = await dbContext.SessionCaseJournal.SingleAsync(w => w.Id == saved.Id);
            row.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            row.CaseWorkflowGuid.Should().Be(guid);
            row.Json.Should().Contain("\"field\"");
        }

        [Fact]
        public async Task CreateIgnoresClientSuppliedOwnerAndIdOnInsertAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.CreateAsync(new SessionCaseJournalDto
            {
                Id = 999999,
                CaseWorkflowGuid = guid,
                Json = "[]",
                CreatedUser = "someone-else",
                CreatedDate = DateTimeOffset.UtcNow.AddYears(-5),
            });

            saved.Id.Should().NotBe(999999);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30));
            (await dbContext.SessionCaseJournal.CountAsync(w => w.CreatedUser == "someone-else")).Should().Be(0);
        }

        [Fact]
        public async Task CreateTwiceReplacesRatherThanDuplicatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{\"v\":1}" });
            await service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{\"v\":2}" });

            var rows = await dbContext.SessionCaseJournal.Where(w => w.CaseWorkflowGuid == guid).ToListAsync();
            rows.Should().ContainSingle();
            rows[0].Json.Should().Contain("2");

            var read = await service.GetByCaseWorkflowGuidAsync(guid);
            read.Required().Json.Should().Contain("2");
        }

        [Fact]
        public async Task CreateDoesNotPublishChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{}" });
            await service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{\"a\":1}" });

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsOwnJournalWithExactFieldMappingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var id = await InsertJournalDirectAsync(dbContext, fx.Seed.UserWithPermission, guid, "{\"k\":\"v\"}");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetByCaseWorkflowGuidAsync(guid);
            dto = dto.Required();

            dto.Should().NotBeNull();
            dto.Id.Should().Be(id);
            dto.CaseWorkflowGuid.Should().Be(guid);
            dto.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            dto.Json.Should().Contain("\"k\"");
            dto.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30));
            dto.CreatedDate.Required().Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public async Task GetWithNoJournalReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByCaseWorkflowGuidAsync(guid)).Should().BeNull();
            (await service.GetByCaseWorkflowGuidAsync(Guid.NewGuid())).Should().BeNull();
        }

        [Fact]
        public async Task GetDoesNotReturnAnotherUsersJournalInSameTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            await InsertJournalDirectAsync(dbContext, fx.Seed.UserWithoutPermission, guid, "{\"private\":true}");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByCaseWorkflowGuidAsync(guid)).Should().BeNull();
        }

        [Fact]
        public async Task CreateDoesNotOverwriteAnotherUsersJournalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var otherId = await InsertJournalDirectAsync(dbContext, fx.Seed.UserWithoutPermission, guid,
                "{\"private\":true}");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{\"mine\":true}" });

            (await dbContext.SessionCaseJournal.SingleAsync(w => w.Id == otherId)).Json.Should().Contain("private");
            (await dbContext.SessionCaseJournal.CountAsync(w => w.CaseWorkflowGuid == guid)).Should().Be(2);
        }

        [Fact]
        public async Task TenantBJournalIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guidA = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var guidB = await CreateWorkflowAsync(dbContext, fx.Seed.UserTenantB);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await serviceA.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guidA, Json = "{\"t\":\"A\"}" });
            await serviceB.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guidB, Json = "{\"t\":\"B\"}" });

            (await serviceA.GetByCaseWorkflowGuidAsync(guidA)).Required().Json.Should().Contain("A");
            (await serviceB.GetByCaseWorkflowGuidAsync(guidB)).Required().Json.Should().Contain("B");
            (await serviceA.GetByCaseWorkflowGuidAsync(guidB)).Should().BeNull();
            (await serviceB.GetByCaseWorkflowGuidAsync(guidA)).Should().BeNull();
        }

        [Fact]
        public async Task GetHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;
            var act = () => service.GetByCaseWorkflowGuidAsync(Guid.NewGuid(), token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task CreateHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;
            var act = () => service.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{}" },
                token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            (await dbContext.SessionCaseJournal.CountAsync(w => w.CaseWorkflowGuid == guid,
                    token: CancellationToken.None)).Should()
                .Be(0);
        }

        [Fact]
        public async Task ForbiddenWarnsNotErrorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, capturingLog);
            var act = () => service.GetByCaseWorkflowGuidAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task EachOperationLogsExactlyOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);

            var createAudit = new TestLog();
            var createService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: createAudit);
            await createService.CreateAsync(new SessionCaseJournalDto { CaseWorkflowGuid = guid, Json = "{}" });
            createAudit.Entries.Should().ContainSingle();
            createAudit.Entries[0].Message.Should().Contain("area=SessionCaseJournal op=Create");

            var getAudit = new TestLog();
            var getService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: getAudit);
            await getService.GetByCaseWorkflowGuidAsync(guid);
            getAudit.Entries.Should().ContainSingle();
            getAudit.Entries[0].Message.Should().Contain("op=GetByCaseWorkflowGuid");
        }

        [Fact]
        public void ToolCatalogueListsBothUniqueToolNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().ContainSingle(n => n == "SessionCaseJournalGetByCaseWorkflowGuid");
            names.Should().ContainSingle(n => n == "SessionCaseJournalCreate");
        }
    }
}
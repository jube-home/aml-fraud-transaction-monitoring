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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.Exceptions.Repository.CaseFile;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseFile;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseFile
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseFileServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseFileIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseFile>().Where(w => createdCaseFileIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids1 = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids1.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids2 = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids2.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseFileService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseFileService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task<(Guid CaseWorkflowGuid, Guid CaseWorkflowStatusGuid)> CreateWorkflowWithRoleAsync(
            DbContext dbContext, string userName)
        {
            var modelId = await CreateModelAsync(dbContext, userName);

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            var caseWorkflowGuid = Guid.NewGuid();
            var caseWorkflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowGuid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(caseWorkflowId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            var caseWorkflowStatusGuid = Guid.NewGuid();
            var caseWorkflowStatusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowStatusGuid,
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(caseWorkflowStatusId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return (caseWorkflowGuid, caseWorkflowStatusGuid);
        }

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid caseWorkflowGuid,
            Guid caseWorkflowStatusGuid)
        {
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                CaseKey = "account",
                CaseKeyValue = $"acc-{Guid.NewGuid():N}"[..20],
                Json = "{}",
                Rating = 1,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdCaseIds.Add(caseId);
            return caseId;
        }

        private async Task<int> CreateCaseFileAsync(DbContext dbContext, int caseId, string caseKey,
            string caseKeyValue, string createdUser, byte[]? content = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseFile
            {
                Object = content ?? Encoding.UTF8.GetBytes("ZzTest file content"),
                CaseId = caseId,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
                Name = $"{DatabaseFixture.Prefix}File{Guid.NewGuid():N}.txt",
                Extension = ".txt",
                ContentType = "text/plain",
                Size = 20,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = createdUser,
            }).ConfigureAwait(false);

            createdCaseFileIds.Add(id);
            return id;
        }

        private static UploadedFileContent NonEmptyFile(string name = "test.txt", string content = "hello") =>
            new(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content)), name, "text/plain",
                Encoding.UTF8.GetByteCount(content));

        private static UploadedFileContent EmptyFile(string name = "empty.txt") =>
            new(new System.IO.MemoryStream(), name, "text/plain", 0);

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, null);
            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task CreateWithUnknownTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, fx.Seed.UnknownUser);
            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task UploadWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.UploadAsync([NonEmptyFile()], "account", "value", caseId);
            await act.Should().ThrowAsync<ForbiddenException>();

            var files = await dbContext.GetTable<Data.Poco.CaseFile>().Where(w => w.CaseId == caseId).ToListAsync();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", "value",
                fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.RemoveAsync(fileId);
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.GetByCaseKeyValueAsync("account", "value");
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task UploadPersistsFileAndReturnsDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var result = await service.UploadAsync([NonEmptyFile("report.pdf", "%PDF-1.4 test")], "account",
                "acc-value", caseId);
            result = result.Required();
            createdCaseFileIds.Add(result.Id);

            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("report.pdf");
            result.Extension.Should().Be(".pdf");
            result.ContentType.Should().Be("text/plain");
            result.CaseId.Should().Be(caseId);
            result.CreatedUser.Should().Be(fx.Seed.UserWithPermission);

            var saved = await dbContext.GetTable<Data.Poco.CaseFile>().FirstAsync(w => w.Id == result.Id);
            saved.Object.Should().Equal(Encoding.UTF8.GetBytes("%PDF-1.4 test"));

            bus.Published.Should().ContainSingle(e => e.EntityId == result.Id && e.Kind == ServiceChangeKind.Created);
        }

        [Fact]
        public async Task UploadSkipsEmptyFilesAndStoresFirstNonEmptyOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.UploadAsync(
                [EmptyFile(), NonEmptyFile("first.txt"), NonEmptyFile("second.txt")], "account", "value", caseId);
            createdCaseFileIds.Add(result.Required().Id);

            result.Required().Name.Should().Be("first.txt");
            var files = await dbContext.GetTable<Data.Poco.CaseFile>().Where(w => w.CaseId == caseId).ToListAsync();
            files.Should().HaveCount(1);
        }

        [Fact]
        public async Task UploadWithNoNonEmptyFileReturnsNullAndStoresNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.UploadAsync([EmptyFile()], "account", "value", caseId);

            result.Should().BeNull();
            var files = await dbContext.GetTable<Data.Poco.CaseFile>().Where(w => w.CaseId == caseId).ToListAsync();
            files.Should().BeEmpty();
        }

        [Theory]
        [InlineData("", "value")]
        [InlineData("   ", "value")]
        [InlineData(null, "value")]
        public async Task UploadWithBlankCaseKeyThrowsValidationFailedAsync(string? caseKey, string caseKeyValue)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UploadAsync([NonEmptyFile()], caseKey, caseKeyValue, 1));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == "caseKey");
        }

        [Fact]
        public async Task UploadWithBlankCaseKeyValueThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UploadAsync([NonEmptyFile()], "account", "", 1));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == "caseKeyValue");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task UploadWithInvalidCaseIdThrowsValidationFailedAsync(int caseId)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UploadAsync([NonEmptyFile()], "account", "value", caseId));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == "caseId");
        }

        [Fact]
        public async Task UploadWithUnknownCaseThrowsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UploadAsync([NonEmptyFile()], "account", "value", 2_000_000_000);
            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task UploadOfCaseInAnotherTenantThrowsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.UploadAsync([NonEmptyFile()], "account", "value", caseId);
            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task UploadDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UploadAsync([NonEmptyFile()], "", "value", 1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveSoftDeletesFileAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", "value",
                fx.Seed.UserWithPermission);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            await service.RemoveAsync(fileId);

            var saved = await dbContext.GetTable<Data.Poco.CaseFile>().FirstAsync(w => w.Id == fileId);
            saved.Deleted.Should().Be(1);

            bus.Published.Should().ContainSingle(e => e.EntityId == fileId && e.Kind == ServiceChangeKind.Deleted);
        }

        [Fact]
        public async Task RemoveWithUnknownIdThrowsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.RemoveAsync(2_000_000_000);
            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task RemoveOfFileInAnotherTenantThrowsNotVisibleAndLeavesRowUntouchedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", "value", fx.Seed.UserTenantB);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.RemoveAsync(fileId);
            await act.Should().ThrowAsync<NotVisibleException>();

            var saved = await dbContext.GetTable<Data.Poco.CaseFile>().FirstAsync(w => w.Id == fileId);
            saved.Deleted.Should().NotBe(1);
        }

        [Fact]
        public async Task GenerateReturnsFileContentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var content = Encoding.UTF8.GetBytes("ZzTest binary content");
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", "value",
                fx.Seed.UserWithPermission, content);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GenerateAsync(fileId);

            result.Content.Should().Equal(content);
            result.ContentType.Should().Be("text/plain");
        }

        [Fact]
        public async Task GenerateWithUnknownIdThrowsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.GenerateAsync(2_000_000_000);
            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task GenerateOfFileInAnotherTenantThrowsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", "value", fx.Seed.UserTenantB);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.GenerateAsync(fileId);
            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task GetByCaseKeyValueReturnsMatchingFilesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", caseKeyValue,
                fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseKeyValueAsync("account", caseKeyValue);

            result.Should().ContainSingle(f => f.Id == fileId);
        }

        [Fact]
        public async Task GetByCaseKeyValueIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateCaseFileAsync(dbContext, caseId, "account", caseKeyValue, fx.Seed.UserTenantB);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseKeyValueAsync("account", caseKeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueRequiresCaseWorkflowRoleNotJustTenantPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateCaseFileAsync(dbContext, caseId, "account", caseKeyValue,
                fx.Seed.UserWithPermissionNoApproveByReview);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseKeyValueAsync("account", caseKeyValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueExcludesRemovedFilesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var fileId = await CreateCaseFileAsync(dbContext, caseId, "account", caseKeyValue,
                fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.RemoveAsync(fileId);

            var result = await service.GetByCaseKeyValueAsync("account", caseKeyValue);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.GetByCaseKeyValueAsync("account", "no-such-value");

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueWithCancelledTokenThrowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new System.Threading.CancellationTokenSource();
            await cts.CancelAsync();

            var cancelledToken = cts.Token;
            var act = () => service.GetByCaseKeyValueAsync("account", "value", cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task UploadWithoutPermissionLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, testLog);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UploadAsync([NonEmptyFile()], "account", "value", 1));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UploadUnexpectedFailureLogsErrorWithExceptionAttachedAsync()
        {
            var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            // Disposed deliberately (only once) to force an unexpected failure inside the service.
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.UploadAsync([NonEmptyFile()], "account", "value", caseId));

            var errorEntry = testLog.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadSuccessLogsExactlyOneInfoEntryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid);
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            var result = await service.UploadAsync([NonEmptyFile()], "account", "value", caseId);
            createdCaseFileIds.Add(result.Required().Id);

            testLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public async Task GetByCaseKeyValueWithGatesDisabledRecordsNoEntriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog(enabled: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            await service.GetByCaseKeyValueAsync("account", "no-such-value");

            testLog.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueEmitsExactlyOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetByCaseKeyValueAsync("account", "no-such-value");

            var entry = auditLog.Entries.Should().ContainSingle().Subject;
            entry.Message.Should().Contain("area=CaseFile").And.Contain("op=GetByCaseKeyValue")
                .And.Contain("outcome=ok");
        }

        [Fact]
        public async Task UploadWithBlankCaseKeyUsesFrenchMessageUnderFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.UploadAsync([NonEmptyFile()], "", "value", 1));

                ex.Result.Errors.First(e => e.PropertyName == "caseKey").ErrorMessage
                    .Should().Be("La clé de dossier est requise.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void ServiceToolCatalogueRegistersCaseFileOperationsWithGloballyUniqueNames()
        {
            var names = Jube.Service.Agent.ServiceToolCatalogue.ServiceToolCatalogue.All
                .Select(t => t.Name).ToList();

            names.Should().Contain("CaseFileRemove").And.Contain("CaseFileListByCaseKeyValue");
            names.Should().OnlyHaveUniqueItems();
            names.Should().NotContain("CaseFileUpload");
        }
    }
}
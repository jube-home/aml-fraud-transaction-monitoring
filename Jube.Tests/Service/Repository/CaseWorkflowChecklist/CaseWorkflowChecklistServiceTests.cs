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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowChecklist;
using Jube.Service.Exceptions.Repository.CaseWorkflowChecklist;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowChecklist;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowChecklistServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> checklistIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> statusIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistVersion>()
                .Where(w => w.CaseWorkflowChecklistId != null && checklistIds.Contains(w.CaseWorkflowChecklistId.Value))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklist>().Where(w => checklistIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowChecklistService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseWorkflowChecklistService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private async Task<(Guid StatusGuid, int StatusId)> CreateStatusAsync(DbContext dbContext, string userName)
        {
            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, userName).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            modelIds.Add(model.Id);

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            workflowIds.Add(workflowId);

            var statusGuid = Guid.NewGuid();

            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 3,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            statusIds.Add(statusId);

            return (statusGuid, statusId);
        }

        private static CaseWorkflowChecklistDto Checklist(Guid status, string name, int? priority = 0)
        {
            return new CaseWorkflowChecklistDto
            {
                CaseWorkflowStatusGuid = status,
                Name = name,
                Priority = priority,
                Active = true
            };
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var act = () => BuildServiceAsync(dbContext, null);

            (await act.Should().ThrowAsync<NotAuthenticatedException>()).Which.Code.Should().Be("NotAuthenticated");
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.GetAsync();

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(19);
        }

        [Fact]
        public async Task InsertThenListAndGetRoundTripAChecklistUnderItsStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Checklist(status, "Initial triage", 1));
            checklistIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CaseWorkflowStatusId.Should().Be(statusId);
            saved.CaseWorkflowStatusGuid.Should().Be(status);
            saved.Priority.Should().Be(1);

            (await service.GetByCaseWorkflowStatusIdAsync(statusId)).Should().ContainSingle(c => c.Id == saved.Id);
            (await service.GetByIdAsync(saved.Id))!.Name.Should().Be("Initial triage");
        }

        [Fact]
        public async Task InsertResolvesTheStatusGuidFromTheStatusIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var model = Checklist(Guid.Empty, "By identifier");
            model.CaseWorkflowStatusId = statusId;

            var saved = await service.InsertAsync(model);
            checklistIds.Add(saved.Id);

            saved.CaseWorkflowStatusGuid.Should().Be(status);
        }

        [Fact]
        public async Task ABlankPriorityIsStoredAsZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, _) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Checklist(status, "No priority", null));
            checklistIds.Add(saved.Id);

            saved.Priority.Should().Be(0);
        }

        [Fact]
        public async Task ListIsOrderedByPriorityAscendingThenNameThenIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var names = new[] { ("Zulu", 1), ("Alpha", 2), ("Bravo", 1), ("Alpha", 1), ("Charlie", 0) };

            foreach (var (name, priority) in names)
            {
                checklistIds.Add((await service.InsertAsync(Checklist(status, name, priority))).Id);
            }

            var listed = await service.GetByCaseWorkflowStatusIdAsync(statusId);

            listed.Select(c => $"{c.Priority}:{c.Name}").Should()
                .Equal("0:Charlie", "1:Alpha", "1:Bravo", "1:Zulu", "2:Alpha");
        }

        [Fact]
        public async Task InsertRefusesMissingNameNegativePriorityAndUnknownStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, _) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var noName = () => service.InsertAsync(Checklist(status, string.Empty));
            var negative = () => service.InsertAsync(Checklist(status, "Negative", -1));
            var unknown = () => service.InsertAsync(Checklist(Guid.NewGuid(), "Orphan"));

            (await noName.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "NameNotEmpty");
            (await negative.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "PriorityGreaterThanOrEqualTo");
            (await unknown.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "CaseWorkflowStatusGuidNotFound");
        }

        [Fact]
        public async Task ValidateReportsFailuresWithoutStoringAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ValidateAsync(Checklist(status, string.Empty));

            result.Errors.Should().NotBeEmpty();
            (await service.GetByCaseWorkflowStatusIdAsync(statusId)).Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, _) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Checklist(status, "Original"));
            checklistIds.Add(saved.Id);

            saved.Name = "Renamed";
            saved.Priority = 7;

            var updated = await service.UpdateAsync(saved);

            updated.Version.Should().Be(2);
            updated.Name.Should().Be("Renamed");
            updated.Priority.Should().Be(7);

            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistVersion>()
                .CountAsync(v => v.CaseWorkflowChecklistId == saved.Id)).Should().Be(1);

            var stale = () => service.UpdateAsync(saved);

            await stale.Should().ThrowAsync<VersionConflictException>();
        }

        [Fact]
        public async Task UpdateCannotMoveAChecklistToAnotherStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var (other, _) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Checklist(status, "Stays put"));
            checklistIds.Add(saved.Id);

            saved.CaseWorkflowStatusGuid = other;
            var updated = await service.UpdateAsync(saved);

            updated.CaseWorkflowStatusGuid.Should().Be(status);
            (await service.GetByCaseWorkflowStatusIdAsync(statusId)).Should().ContainSingle(c => c.Id == saved.Id);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Checklist(status, "Doomed"));
            checklistIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            (await service.GetByCaseWorkflowStatusIdAsync(statusId)).Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklist>().FirstAsync(c => c.Id == saved.Id))
                .Deleted.Should().Be(1);

            var again = () => service.DeleteAsync(saved.Id);

            await again.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AnotherTenantCannotSeeChangeOrDeleteAChecklistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, statusId) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var saved = await owner.InsertAsync(Checklist(status, "Private"));
            checklistIds.Add(saved.Id);

            (await outsider.GetAsync()).Should().NotContain(c => c.Id == saved.Id);
            (await outsider.GetByCaseWorkflowStatusIdAsync(statusId)).Should().BeEmpty();
            (await outsider.GetByIdAsync(saved.Id)).Should().BeNull();

            var update = () => outsider.UpdateAsync(saved);
            var delete = () => outsider.DeleteAsync(saved.Id);
            var insert = () => outsider.InsertAsync(Checklist(status, "Planted"));

            await update.Should().ThrowAsync<NotFoundException>();
            await delete.Should().ThrowAsync<NotFoundException>();
            (await insert.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "CaseWorkflowStatusGuidNotFound");
        }

        [Fact]
        public async Task ActiveByStatusGuidExcludesInactiveChecklistsAndKeepsOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (status, _) = await CreateStatusAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var second = await service.InsertAsync(Checklist(status, "Second", 2));
            var first = await service.InsertAsync(Checklist(status, "First", 1));
            var off = Checklist(status, "Off", 0);
            off.Active = false;
            var inactive = await service.InsertAsync(off);
            checklistIds.AddRange([second.Id, first.Id, inactive.Id]);

            var active = await new Data.Repository.CaseWorkflowChecklistRepository(dbContext,
                    fx.Seed.UserWithPermission)
                .GetActiveByCaseWorkflowStatusGuidAsync(status);

            active.Select(c => c.Name).Should().Equal("First", "Second");
        }
    }
}

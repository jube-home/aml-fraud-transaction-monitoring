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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflowXPath;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseWorkflowXPath;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowXPath;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowXPath
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowXPathServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int XPathPermissionSpecification = 20;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdXPathIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        private string userXPathA = string.Empty;
        private string userXPathB = string.Empty;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var tenantAId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            var tenantBId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserTenantB)
                .Select(w => w.TenantRegistryId).FirstAsync();

            userXPathA = await SeedActorWithXPathPermissionAsync(dbContext, tenantAId, "A");
            userXPathB = await SeedActorWithXPathPermissionAsync(dbContext, tenantBId, "B");
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var caseWorkflowXPathGuids1 = dbContext.CaseWorkflowXPath.Where(s => createdXPathIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => caseWorkflowXPathGuids1.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId != null && createdXPathIds.Contains(w.CaseWorkflowXPathId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowGuids2 = dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids2.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();

            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<string> SeedActorWithXPathPermissionAsync(DbContext dbContext, int tenantRegistryId,
            string label)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RoleXPath{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdRoleRegistryIds.Add(roleId);

            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                PermissionSpecificationId = XPathPermissionSpecification,
                RoleRegistryId = roleId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });

            var userName = $"{DatabaseFixture.Prefix}UserXPath{label}{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });

            await dbContext.InsertAsync(new Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantRegistryId,
            });

            createdUserNames.Add(userName);
            return userName;
        }

        private static Task<CaseWorkflowXPathService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowXPathService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
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

        private async Task<int> CreateWorkflowAsync(DbContext dbContext, int entityAnalysisModelId,
            string createdUser)
        {
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = entityAnalysisModelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdWorkflowIds.Add(workflowId);
            return workflowId;
        }

        private async Task GrantWorkflowRoleAsync(DbContext dbContext, int caseWorkflowId, string userName)
        {
            var caseWorkflowGuid = await dbContext.CaseWorkflow.Where(w => w.Id == caseWorkflowId)
                .Select(w => w.Guid).FirstAsync();
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private async Task GrantXPathRoleAsync(DbContext dbContext, Guid caseWorkflowXPathGuid, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
            {
                CaseWorkflowXPathGuid = caseWorkflowXPathGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private static CaseWorkflowXPathDto ValidDto(int caseWorkflowId, string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}XPath{Guid.NewGuid():N}"[..40],
            CaseWorkflowId = caseWorkflowId,
            Active = true,
            Locked = false,
            XPath = "payload.AmountUSD",
            Drill = false,
            BoldLineMatched = false,
            ConditionalRegularExpressionFormatting = false,
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}XPath{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(workflowId, name)));

            var stillExists = await dbContext.CaseWorkflowXPath.AnyAsync(w => w.Name == name);
            stillExists.Should().BeFalse();
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should()
                .Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(userXPathA);
            saved.CreatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId, "");

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userXPathA);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(-1)));
        }

        [Fact]
        public async Task InsertWithCaseWorkflowUnderSoftDeletedModelThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId)
                .Set(w => w.Deleted, (byte)1).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, userXPathA);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(workflowId)));
        }

        [Fact]
        public async Task InsertWithBlankXPathThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.XPath = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var name = $"{DatabaseFixture.Prefix}XPath{Guid.NewGuid():N}"[..40];

            var first = await service.InsertAsync(ValidDto(workflowId, name));
            createdXPathIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(workflowId, name)));
        }

        [Fact]
        public async Task InsertWithSameNameInDifferentWorkflowSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowOneId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var workflowTwoId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var name = $"{DatabaseFixture.Prefix}XPath{Guid.NewGuid():N}"[..40];

            var first = await service.InsertAsync(ValidDto(workflowOneId, name));
            createdXPathIds.Add(first.Id);
            var second = await service.InsertAsync(ValidDto(workflowTwoId, name));
            createdXPathIds.Add(second.Id);

            second.Id.Should().NotBe(first.Id);
        }

        [Fact]
        public async Task InsertWithBoldLineMatchedRequiresColorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.BoldLineMatched = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Select(e => e.PropertyName).Should()
                .Contain([
                    nameof(CaseWorkflowXPathDto.BoldLineFormatForeColor),
                    nameof(CaseWorkflowXPathDto.BoldLineFormatBackColor)
                ]);
        }

        [Fact]
        public async Task InsertWithConditionalFormattingRequiresRegexAndColorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.ConditionalRegularExpressionFormatting = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Select(e => e.PropertyName).Should()
                .Contain([
                    nameof(CaseWorkflowXPathDto.RegularExpression),
                    nameof(CaseWorkflowXPathDto.ConditionalFormatForeColor),
                    nameof(CaseWorkflowXPathDto.ConditionalFormatBackColor)
                ]);
        }

        [Fact]
        public async Task InsertIgnoresDisplayNameDeadFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.DisplayName = "should be dropped";

            var saved = await service.InsertAsync(dto);
            createdXPathIds.Add(saved.Id);

            saved.DisplayName.Should().BeNull();
        }

        [Fact]
        public async Task InsertIgnoresTamperedIdentityAndAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Id = 999_999;
            dto.Guid = Guid.NewGuid();
            dto.CreatedUser = "tampered";
            dto.CreatedDate = DateTimeOffset.UtcNow.AddYears(-5);
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdXPathIds.Add(saved.Id);

            saved.Id.Should().NotBe(999_999);
            saved.Guid.Should().NotBe(dto.Guid);
            saved.CreatedUser.Should().Be(userXPathA);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);
            var originalCreatedUser = created.CreatedUser;
            var originalCreatedDate = created.CreatedDate;

            await Task.Delay(50);

            var toUpdate = ValidDto(workflowId, created.Name);
            toUpdate.Id = created.Id;
            var updated = await service.UpdateAsync(toUpdate);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(originalCreatedUser);
            updated.CreatedDate.Should().BeCloseTo(originalCreatedDate.Required(), TimeSpan.FromSeconds(1));
            updated.UpdatedUser.Should().Be(userXPathA);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateWritesAVersionAuditRowCarryingTheDeletedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var toUpdate = ValidDto(workflowId, created.Name);
            toUpdate.Id = created.Id;
            await service.UpdateAsync(toUpdate);

            var audit = await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId == created.Id).FirstAsync();

            audit.Version.Should().Be(created.Version);
            audit.Name.Should().Be(created.Name);
            (audit.Deleted is null or 0).Should().BeTrue();
            audit.DeletedUser.Should().BeNull();
            audit.DeletedDate.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 1,
                CreatedUser = userXPathA,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.CaseWorkflowXPath.Where(w => w.Id == created.Id)
                .Set(w => w.ImportId, importId).UpdateAsync();

            try
            {
                var toUpdate = ValidDto(workflowId, created.Name);
                toUpdate.Id = created.Id;
                await service.UpdateAsync(toUpdate);

                var persistedImportId = await dbContext.CaseWorkflowXPath.Where(w => w.Id == created.Id)
                    .Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.CaseWorkflowXPath.Where(w => w.Id == created.Id)
                    .Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Data.Poco.Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task UpdateOfUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Id = int.MaxValue;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);
            await dbContext.CaseWorkflowXPath.Where(w => w.Id == created.Id)
                .Set(w => w.Locked, (byte)1).UpdateAsync();

            var toUpdate = ValidDto(workflowId, created.Name);
            toUpdate.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(toUpdate));
        }

        [Fact]
        public async Task UpdateOfCrossTenantRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var otherModelId = await CreateModelAsync(dbContext, userXPathB);
            var otherWorkflowId = await CreateWorkflowAsync(dbContext, otherModelId, userXPathB);
            var otherTenantService = await BuildServiceAsync(dbContext, userXPathB);
            var dto = ValidDto(otherWorkflowId, created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenantService.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, userXPathB);
            var dto = ValidDto(workflowId, created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<DtoValidationException>(() => otherTenantService.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateIgnoresTamperedIdentityAndAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var toUpdate = ValidDto(workflowId, created.Name);
            toUpdate.Id = created.Id;
            toUpdate.Guid = Guid.NewGuid();
            toUpdate.CreatedUser = "tampered";
            toUpdate.Version = 999;

            var updated = await service.UpdateAsync(toUpdate);

            updated.Guid.Should().Be(created.Guid);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userXPathA, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);
            bus.Published.Clear();

            await service.DeleteAsync(created.Id);

            var stillVisible = await service.GetByIdAsync(created.Id);
            stillVisible.Should().BeNull();
            bus.Published.Should().ContainSingle(e => e.Kind == ServiceChangeKind.Deleted);
        }

        [Fact]
        public async Task DeleteOfUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userXPathA);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue));
        }

        [Fact]
        public async Task DeleteOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);
            await dbContext.CaseWorkflowXPath.Where(w => w.Id == created.Id)
                .Set(w => w.Locked, (byte)1).UpdateAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task ListByWorkflowIdIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, userXPathB);
            var results = await otherTenantService.GetByCaseWorkflowIdAsync(workflowId);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByWorkflowIdActiveDrillOnlyRequiresBothWorkflowAndXPathRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Drill = true;
            var created = await ownerService.InsertAsync(dto);
            createdXPathIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, userXPathA);

            var beforeAnyGrant = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            beforeAnyGrant.Should().BeEmpty();

            await GrantWorkflowRoleAsync(dbContext, workflowId, userXPathA);
            var withOnlyWorkflowGrant = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            withOnlyWorkflowGrant.Should().BeEmpty();

            await GrantXPathRoleAsync(dbContext, created.Guid, userXPathA);
            var withBothGrants = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            withBothGrants.Should().ContainSingle(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListByWorkflowIdActiveDrillOnlyExcludesNonDrillRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Drill = false;
            var created = await ownerService.InsertAsync(dto);
            createdXPathIds.Add(created.Id);

            await GrantWorkflowRoleAsync(dbContext, workflowId, userXPathA);
            await GrantXPathRoleAsync(dbContext, created.Guid, userXPathA);

            var service = await BuildServiceAsync(dbContext, userXPathA);
            var results = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByWorkflowGuidActiveDrillOnlyReturnsMatchingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Id == workflowId)
                .Select(w => w.Guid).FirstAsync();
            var ownerService = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Drill = true;
            var created = await ownerService.InsertAsync(dto);
            createdXPathIds.Add(created.Id);

            await GrantWorkflowRoleAsync(dbContext, workflowId, userXPathA);
            await GrantXPathRoleAsync(dbContext, created.Guid, userXPathA);

            var service = await BuildServiceAsync(dbContext, userXPathA);
            var results = await service.GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid);

            results.Should().ContainSingle(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userXPathA);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task RoundTripPreservesAllExtractionAndColorFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var dto = ValidDto(workflowId);
            dto.Drill = true;
            dto.BoldLineMatched = true;
            dto.BoldLineFormatForeColor = "#111111";
            dto.BoldLineFormatBackColor = "#222222";
            dto.ConditionalRegularExpressionFormatting = true;
            dto.RegularExpression = "^[0-9]+$";
            dto.ConditionalFormatForeColor = "#333333";
            dto.ConditionalFormatBackColor = "#444444";
            dto.ForeRowColorScope = true;
            dto.BackRowColorScope = true;

            var saved = await service.InsertAsync(dto);
            createdXPathIds.Add(saved.Id);
            var fetched = await service.GetByIdAsync(saved.Id);
            fetched = fetched.Required();

            fetched.Should().NotBeNull();
            fetched.Drill.Should().BeTrue();
            fetched.BoldLineMatched.Should().BeTrue();
            fetched.BoldLineFormatForeColor.Should().Be("#111111");
            fetched.BoldLineFormatBackColor.Should().Be("#222222");
            fetched.ConditionalRegularExpressionFormatting.Should().BeTrue();
            fetched.RegularExpression.Should().Be("^[0-9]+$");
            fetched.ConditionalFormatForeColor.Should().Be("#333333");
            fetched.ConditionalFormatBackColor.Should().Be("#444444");
            fetched.ForeRowColorScope.Should().BeTrue();
            fetched.BackRowColorScope.Should().BeTrue();
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userXPathA, log);

            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, userXPathA, log);

            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userXPathA, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
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
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "CaseWorkflowXPath.Insert").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);
            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdXPathIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Insert");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, userXPathA, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userXPathA, serviceChangeBus: bus);

            await service.GetAsync();

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task FailedInsertPublishesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userXPathA, serviceChangeBus: bus);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(-1)));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var modelId = await CreateModelAsync(dbContext, userXPathA);
                var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
                var service = await BuildServiceAsync(dbContext, userXPathA);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.InsertAsync(ValidDto(workflowId, "")));

                ex.Result.Errors.First(e => e.PropertyName == nameof(CaseWorkflowXPathDto.Name)).ErrorMessage
                    .Should().Be("Le nom est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void CatalogueRegistersExpectedToolNamesForThisArea()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().Contain("CaseWorkflowXPathList");
            names.Should().Contain("CaseWorkflowXPathListByWorkflowIdActiveDrillOnly");
            names.Should().Contain("CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly");
            names.Should().Contain("CaseWorkflowXPathGet");
            names.Should().Contain("CaseWorkflowXPathCreate");
            names.Should().Contain("CaseWorkflowXPathUpdate");
            names.Should().Contain("CaseWorkflowXPathDelete");
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, userXPathA);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userXPathA);
            var service = await BuildServiceAsync(dbContext, userXPathA);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(ValidDto(workflowId));
                createdXPathIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }
    }
}
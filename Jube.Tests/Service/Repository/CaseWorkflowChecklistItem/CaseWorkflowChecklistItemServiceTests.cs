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
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using Jube.Service.Exceptions.Repository.CaseWorkflowChecklistItem;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowChecklistItem;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseWorkflowChecklistItem
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowChecklistItemServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Connection(),
                ["ReportConnectionString"] = Connection(),
            });

        private readonly List<int> checklistIds = [];
        private readonly List<int> itemIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> statusIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItemVersion>()
                .Where(w => w.CaseWorkflowChecklistItemId != null && itemIds.Contains(w.CaseWorkflowChecklistItemId.Value))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>().Where(w => itemIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowChecklist>().Where(w => checklistIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private static string Connection()
        {
            return Environment.GetEnvironmentVariable("JubeTestConnectionString")
                   ?? Environment.GetEnvironmentVariable("ConnectionString")
                   ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;";
        }

        private static Task<CaseWorkflowChecklistItemService> BuildServiceAsync(DbContext dbContext,
            string? userName)
        {
            return CaseWorkflowChecklistItemService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<(Guid ChecklistGuid, int WorkflowId)> CreateChecklistAsync(DbContext dbContext,
            string userName)
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

            statusIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
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
            }));

            var checklistGuid = Guid.NewGuid();

            checklistIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowChecklist
            {
                Name = $"{DatabaseFixture.Prefix}Checklist{Guid.NewGuid():N}"[..40],
                Guid = checklistGuid,
                CaseWorkflowStatusGuid = statusGuid,
                Priority = 0,
                Active = 1,
                Deleted = 0,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            }));

            return (checklistGuid, workflowId);
        }

        private static CaseWorkflowChecklistItemDto Item(Guid checklist, string name = "Item", int priority = 0)
        {
            return new CaseWorkflowChecklistItemDto
            {
                CaseWorkflowChecklistGuid = checklist,
                Name = name,
                Priority = priority,
                Active = true
            };
        }

        private static CaseWorkflowChecklistItemDto WithSql(Guid checklist, string? show, string? validate)
        {
            var model = Item(checklist);

            model.ShowSql = show;
            model.ValidateSql = validate;

            return model;
        }

        private async Task<CaseWorkflowChecklistItemDto> SaveAsync(CaseWorkflowChecklistItemService service,
            CaseWorkflowChecklistItemDto model)
        {
            var saved = await service.InsertAsync(model);

            itemIds.Add(saved.Id);

            return saved;
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
        public async Task AnItemWithNoSqlAndNoSlaIsAcceptedAndStoredAsNullsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, workflowId) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = WithSql(checklist, "   ", "");

            model.ValidateMessage = " ";

            var saved = await SaveAsync(service, model);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CaseWorkflowId.Should().Be(workflowId);
            saved.CaseWorkflowChecklistGuid.Should().Be(checklist);
            saved.Priority.Should().Be(0);
            saved.ShowSql.Should().BeNull();
            saved.ValidateSql.Should().BeNull();
            saved.ValidateMessage.Should().BeNull();
            saved.SlaMinutes.Should().BeNull();

            (await service.GetByIdAsync(saved.Id))!.Name.Should().Be("Item");
            (await service.GetByCaseWorkflowChecklistGuidAsync(checklist)).Should().ContainSingle(i => i.Id == saved.Id);
            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().ContainSingle(i => i.Id == saved.Id);
        }

        [Fact]
        public async Task AnItemCarriesShowSqlValidateSqlAndSlaWhenTheyAreSuppliedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = WithSql(checklist, "select @Id = 0", "select @CaseKeyValue = ''");

            model.SlaMinutes = 90;
            model.ValidateMessage = "Note first.";

            var saved = await SaveAsync(service, model);

            saved.ShowSql.Should().Be("select @Id = 0");
            saved.ValidateSql.Should().Be("select @CaseKeyValue = ''");
            saved.SlaMinutes.Should().Be(90);
            saved.ValidateMessage.Should().Be("Note first.");
        }

        [Fact]
        public async Task ItemsAreOrderedByPriorityThenNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await SaveAsync(service, Item(checklist, "Zulu", 1));
            await SaveAsync(service, Item(checklist, "Bravo", 2));
            await SaveAsync(service, Item(checklist, "Alpha", 2));
            await SaveAsync(service, Item(checklist, "Last", 9));
            await SaveAsync(service, Item(checklist, "First"));

            (await service.GetByCaseWorkflowChecklistGuidAsync(checklist)).Select(i => i.Name)
                .Should().Equal("First", "Zulu", "Alpha", "Bravo", "Last");
        }

        [Fact]
        public async Task InsertChecksNamePriorityAndSlaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            async Task<string[]> Codes(Action<CaseWorkflowChecklistItemDto> shape)
            {
                var model = Item(checklist);

                shape(model);

                var act = () => service.InsertAsync(model);

                return (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                    .Select(e => e.ErrorCode).ToArray();
            }

            (await Codes(m => m.Name = null)).Should().Contain("NameNotEmpty");
            (await Codes(m => m.Name = new string('x', 101))).Should().Contain("NameMaximumLength");
            (await Codes(m => m.Priority = -1)).Should().Contain("PriorityInvalid");
            (await Codes(m => m.SlaMinutes = 0)).Should().Contain("SlaMinutesInvalid");
            (await Codes(m => m.SlaMinutes = -5)).Should().Contain("SlaMinutesInvalid");
            (await Codes(m => m.ValidateMessage = new string('x', 501))).Should().Contain("ValidateMessageMaximumLength");
            (await Codes(m => m.CaseWorkflowChecklistGuid = Guid.Empty)).Should()
                .Contain("CaseWorkflowChecklistGuidNotEmpty");
            (await Codes(m => m.CaseWorkflowChecklistGuid = Guid.NewGuid())).Should()
                .Contain("CaseWorkflowChecklistGuidNotFound");
        }

        [Fact]
        public async Task InsertOfAnItemWithSqlNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var showOnly = () => service.InsertAsync(WithSql(checklist, "select true", null));
            var validateOnly = () => service.InsertAsync(WithSql(checklist, null, "select true"));

            (await showOnly.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(67);
            (await validateOnly.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should()
                .Equal(67);

            var plain = await SaveAsync(service, Item(checklist));

            plain.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task InsertRefusesUnknownParametersBlockedTablesAndWritesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var unknown = () => service.InsertAsync(WithSql(checklist, "select @nothing is null", null));
            var caseId = () => service.InsertAsync(WithSql(checklist, null, "select @CaseId = 1"));
            var blocked = () => service.InsertAsync(WithSql(checklist, null, "select count(*) > 0 from \"UserRegistry\""));
            var write = () => service.InsertAsync(WithSql(checklist, "delete from \"Case\" returning true", null));
            var garbage = () => service.InsertAsync(WithSql(checklist, "this is not sql", null));

            (await unknown.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlParameterUnknown" && e.PropertyName == "ShowSql");
            (await caseId.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlParameterUnknown" && e.PropertyName == "ValidateSql");
            (await blocked.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlRejected");
            (await write.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlRejected");
            (await garbage.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlRejected");
        }

        [Fact]
        public async Task UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await SaveAsync(service, WithSql(checklist, "select true", null));

            saved.Name = "Renamed";
            saved.SlaMinutes = 30;

            var updated = await service.UpdateAsync(saved);

            updated.Version.Should().Be(2);
            updated.Name.Should().Be("Renamed");
            updated.SlaMinutes.Should().Be(30);

            var version = await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItemVersion>()
                .SingleAsync(v => v.CaseWorkflowChecklistItemId == saved.Id);

            version.Name.Should().Be("Item");
            version.ShowSql.Should().Be("select true");
            version.SlaMinutes.Should().BeNull();

            var stale = () => service.UpdateAsync(saved);

            await stale.Should().ThrowAsync<VersionConflictException>();
        }

        [Fact]
        public async Task UpdateCanClearTheSqlAndSlaBackToNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = WithSql(checklist, "select true", "select true");

            model.SlaMinutes = 10;

            var saved = await SaveAsync(service, model);

            saved.ShowSql = null;
            saved.ValidateSql = "";
            saved.SlaMinutes = null;

            var updated = await service.UpdateAsync(saved);

            updated.ShowSql.Should().BeNull();
            updated.ValidateSql.Should().BeNull();
            updated.SlaMinutes.Should().BeNull();
        }

        [Fact]
        public async Task ChangingTheSqlOfAnItemNeedsThePermissionButRenamingItDoesNotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(author, WithSql(checklist, "select true", null));

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            saved.Name = "Renamed by someone without SQL permission";

            var renamed = await other.UpdateAsync(saved);

            renamed.Name.Should().Be("Renamed by someone without SQL permission");
            renamed.ShowSql.Should().Be("select true");

            renamed.ShowSql = "select false";

            var changed = () => other.UpdateAsync(renamed);

            (await changed.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(67);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, Item(checklist));

            await service.DeleteAsync(saved.Id);

            (await service.GetByCaseWorkflowChecklistGuidAsync(checklist)).Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>().FirstAsync(v => v.Id == saved.Id))
                .Deleted.Should().Be(1);

            var again = () => service.DeleteAsync(saved.Id);

            await again.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeletingAnItemWithSqlNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(author, WithSql(checklist, null, "select true"));
            var plain = await SaveAsync(author, Item(checklist, "Plain"));

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var act = () => other.DeleteAsync(saved.Id);

            await act.Should().ThrowAsync<ForbiddenException>();
            await other.DeleteAsync(plain.Id);
        }

        [Fact]
        public async Task ATenantCannotSeeOrChangeAnotherTenantsItemsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, workflowId) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(owner, Item(checklist));

            var stranger = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await stranger.GetByIdAsync(saved.Id)).Should().BeNull();
            (await stranger.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();
            (await stranger.GetByCaseWorkflowChecklistGuidAsync(checklist)).Should().BeEmpty();

            var update = () => stranger.UpdateAsync(saved);
            var delete = () => stranger.DeleteAsync(saved.Id);
            var insert = () => stranger.InsertAsync(Item(checklist));
            var test = () => stranger.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ShowCondition
            });

            await update.Should().ThrowAsync<NotFoundException>();
            await delete.Should().ThrowAsync<NotFoundException>();
            await test.Should().ThrowAsync<NotFoundException>();
            (await insert.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "CaseWorkflowChecklistGuidNotFound");
        }

        [Fact]
        public async Task TestRunsEachConditionAgainstTheRealDatabaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = WithSql(checklist, "select @Id = 0", "select @CaseKeyValue = 'x'");

            model.ValidateMessage = "Case key must be x.";

            var saved = await SaveAsync(service, model);

            var show = await service.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ShowCondition
            });

            var validate = await service.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ValidateCondition
            });

            show.Passed.Should().BeTrue();
            show.Code.Should().BeNull();
            validate.Passed.Should().BeFalse();
            validate.Code.Should().Be("sql");
            validate.Message.Should().Be("Case key must be x.");
        }

        [Fact]
        public async Task TestOfABlankConditionPassesWithoutNeedingTheSqlPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var saved = await SaveAsync(service, Item(checklist));

            var result = await service.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ValidateCondition
            });

            result.Passed.Should().BeTrue();
        }

        [Fact]
        public async Task TestOfAConditionWithSqlNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(author, WithSql(checklist, "select true", null));

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var act = () => other.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ShowCondition
            });

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task TestReportsAFailureOfTheSqlItselfWithoutRaisingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, WithSql(checklist, "select (1 / (@Id::int)) > 0", null));

            var result = await service.TestAsync(new CaseWorkflowChecklistItemTestDto
            {
                Id = saved.Id, ConditionId = CaseWorkflowChecklistItemService.ShowCondition
            });

            result.Passed.Should().BeFalse();
            result.Code.Should().Be("sqlFailed");
        }

        [Fact]
        public async Task TestRefusesAnUnknownConditionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, Item(checklist));

            var act = () => service.TestAsync(new CaseWorkflowChecklistItemTestDto { Id = saved.Id, ConditionId = 3 });

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "ConditionInvalid");
        }

        [Fact]
        public async Task ValidateReportsFailuresWithoutStoringAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ValidateAsync(Item(checklist, ""));

            result.IsValid.Should().BeFalse();
            (await service.GetByCaseWorkflowChecklistGuidAsync(checklist)).Should().BeEmpty();
        }

        private static CaseWorkflowChecklistItemDto Flagged(Guid checklist, bool enableShow, string? show,
            bool enableValidation, string? validate)
        {
            var model = Item(checklist);

            model.EnableShow = enableShow;
            model.ShowSql = show;
            model.EnableValidation = enableValidation;
            model.ValidateSql = validate;

            return model;
        }

        [Fact]
        public async Task ASwitchedOnConditionWithoutSqlIsInvalidAndAnEmptyOneSwitchedOffIsNotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ValidateAsync(Flagged(checklist, true, null, true, "  "));

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorCode).Should().Contain(["ShowSqlRequired", "ValidateSqlRequired"]);

            var off = await service.ValidateAsync(Flagged(checklist, false, null, false, null));

            off.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task SwitchingAConditionOffRetainsItsSqlAndMessageAndSwitchingItOnAgainKeepsThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var model = Flagged(checklist, true, "select true", true, "select false");
            model.ValidateMessage = "Not yet";

            var saved = await SaveAsync(service, model);

            saved.EnableShow.Should().BeTrue();
            saved.EnableValidation.Should().BeTrue();

            saved.EnableShow = false;
            saved.EnableValidation = false;

            var off = await service.UpdateAsync(saved);

            off.EnableShow.Should().BeFalse();
            off.EnableValidation.Should().BeFalse();
            off.ShowSql.Should().Be("select true");
            off.ValidateSql.Should().Be("select false");
            off.ValidateMessage.Should().Be("Not yet");

            off.EnableShow = true;

            var on = await service.UpdateAsync(off);

            on.EnableShow.Should().BeTrue();
            on.ShowSql.Should().Be("select true");
            on.ValidateSql.Should().Be("select false");
        }

        [Fact]
        public async Task ARetainedSqlOfASwitchedOffConditionIsStillCheckedForSafetyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var unknownParameter = await service.ValidateAsync(
                Flagged(checklist, false, "select @Anything is not null", false, null));
            var write = await service.ValidateAsync(
                Flagged(checklist, false, null, false, "delete from \"Case\""));

            unknownParameter.IsValid.Should().BeFalse();
            unknownParameter.Errors.Select(e => e.ErrorCode).Should().Contain("SqlParameterUnknown");
            write.IsValid.Should().BeFalse();
            write.Errors.Select(e => e.ErrorCode).Should().Contain("SqlRejected");

            var insert = () => service.InsertAsync(Flagged(checklist, false, "select @Anything is not null", false,
                null));

            await insert.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task SwitchingAConditionOnNeedsTheSqlPermissionButSwitchingItOffAndRenamingDoNotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var disabled = await SaveAsync(author, Flagged(checklist, false, "select true", false, "select true"));

            disabled.EnableShow = true;

            var enableShow = () => other.UpdateAsync(disabled);

            (await enableShow.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should()
                .Equal(67);

            disabled.EnableShow = false;
            disabled.EnableValidation = true;

            var enableValidation = () => other.UpdateAsync(disabled);

            (await enableValidation.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should()
                .Equal(67);

            var enabled = await SaveAsync(author, Flagged(checklist, true, "select true", true, "select true"));

            enabled.Name = "Renamed and switched off without SQL permission";
            enabled.EnableShow = false;
            enabled.EnableValidation = false;

            var saved = await other.UpdateAsync(enabled);

            saved.Name.Should().Be("Renamed and switched off without SQL permission");
            saved.EnableShow.Should().BeFalse();
            saved.EnableValidation.Should().BeFalse();
            saved.ShowSql.Should().Be("select true");
            saved.ValidateSql.Should().Be("select true");
        }

        [Fact]
        public async Task InsertWithAConditionSwitchedOnNeedsTheSqlPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var model = Item(checklist);
            model.EnableShow = true;

            var act = () => service.InsertAsync(model);

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(67);
        }

        [Fact]
        public async Task TheVersionRowOfAnUpdateCarriesTheSwitchesAsTheyWereAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await SaveAsync(service, Flagged(checklist, true, "select true", false, "select true"));

            saved.EnableShow = false;
            saved.EnableValidation = true;

            await service.UpdateAsync(saved);

            var version = await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItemVersion>()
                .FirstAsync(w => w.CaseWorkflowChecklistItemId == saved.Id);

            version.EnableShow.Should().Be(1);
            version.EnableValidation.Should().Be(0);
        }

        [Fact]
        public async Task TestStillRunsTheSqlOfASwitchedOffConditionSoItCanBeTriedBeforeSwitchingItOnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (checklist, _) = await CreateChecklistAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await SaveAsync(service, Flagged(checklist, false, "select false", false, null));

            var result = await service.TestAsync(new CaseWorkflowChecklistItemTestDto { Id = saved.Id, ConditionId = 1 });

            result.Passed.Should().BeFalse();
        }
    }
}

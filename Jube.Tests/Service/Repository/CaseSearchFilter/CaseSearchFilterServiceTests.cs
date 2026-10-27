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
using Jube.Dto.Repository.CaseSearchFilter;
using Jube.Dto.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseSearchFilter;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.CaseSearchFilter;
using Jube.Service.Repository.SessionCaseSearchCompiledSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseSearchFilter
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseSearchFilterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                                          ?? Environment.GetEnvironmentVariable("ConnectionString")
                                                          ??
                                                          "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Pooling=true;MinPoolSize=1;MaxPoolSize=50;";

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<Guid> createdWorkflowGuids = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseSearchFilter.Where(w => createdCaseWorkflowIds.Contains(w.CaseWorkflowId))
                .DeleteAsync();
            await dbContext.SessionCaseSearchCompiledSql
                .Where(w => createdWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => createdWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseSearchFilterService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseSearchFilterService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static Task<Guid> RoleOfAsync(DbContext dbContext, string userName)
        {
            return dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid)
                .FirstAsync();
        }

        private async Task<Guid> SeedWorkflowAsync(DbContext dbContext, string ownerUser,
            params string[] grantedUsers)
        {
            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            createdModelIds.Add(model.Id);

            var workflowGuid = Guid.NewGuid();

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow
            });
            createdCaseWorkflowIds.Add(workflowId);
            createdWorkflowGuids.Add(workflowGuid);

            foreach (var user in grantedUsers.Prepend(ownerUser).Distinct())
            {
                await GrantAsync(dbContext, workflowGuid, user);
            }

            return workflowGuid;
        }

        private static async Task GrantAsync(DbContext dbContext, Guid workflowGuid, string user)
        {
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleOfAsync(dbContext, user),
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            });
        }

        private static string SelectJson() => JsonConvert.SerializeObject(new
        {
            condition = "AND",
            rules = new[]
            {
                new
                {
                    id = "CaseKeyValue", field = "\"Case\".\"CaseKeyValue\"", type = "string", @operator = "order",
                    value = "ASC"
                }
            }
        });

        private static string FilterJson(string caseKeyValue = "x") => JsonConvert.SerializeObject(new
        {
            condition = "AND",
            rules = new[]
            {
                new
                {
                    id = "CaseKeyValue", field = "CaseKeyValue", type = "string", @operator = "equal",
                    value = caseKeyValue
                }
            }
        });

        private static CaseSearchFilterDto Request(Guid workflowGuid, string name = "My search") => new()
        {
            CaseWorkflowGuid = workflowGuid,
            Name = name,
            SelectJson = SelectJson(),
            FilterJson = FilterJson()
        };

        [Fact]
        public async Task CreateWithBlankUserOrUserWithoutTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, "  "));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task EveryOperationWithoutPermissionThrowsForbiddenAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await SeedWorkflowAsync(dbContext, fx.Seed.UserWithPermission,
                fx.Seed.UserWithoutPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(Request(workflow)));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().Equal(1);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByGuidAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(Request(workflow)));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(Guid.NewGuid()));

            (await dbContext.CaseSearchFilter.CountAsync(w => w.CaseWorkflow.Guid == workflow)).Should().Be(0);
        }

        [Fact]
        public async Task InsertStoresTheSearchForTheCallerMapsEveryFieldAndPublishesACreatedEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, user);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, user, serviceChangeBus: bus);
            var before = DateTimeOffset.UtcNow.AddSeconds(-5);

            var dto = await service.InsertAsync(Request(workflow, "  Large cash  "));

            dto.Guid.Should().NotBeEmpty();
            dto.CaseWorkflowGuid.Should().Be(workflow);
            dto.Name.Should().Be("Large cash");
            dto.Mine.Should().BeTrue();
            dto.CreatedUser.Should().Be(user);
            dto.CreatedDate.Should().BeAfter(before);
            dto.UpdatedUser.Should().BeNull();
            dto.Version.Should().Be(1);
            dto.SharedRoleRegistryGuid.Should().BeNull();

            var stored = await dbContext.CaseSearchFilter.FirstAsync(w => w.Guid == dto.Guid);
            stored.CreatedUser.Should().Be(user);

            bus.Published.Should().ContainSingle(e => e.Area == "CaseSearchFilter"
                                                      && e.Kind == ServiceChangeKind.Created
                                                      && e.Actor == user);
        }

        [Fact]
        public async Task InsertIgnoresAGuidSuppliedByTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);
            var chosen = Guid.NewGuid();
            var request = Request(workflow);
            request.Guid = chosen;

            var dto = await service.InsertAsync(request);

            dto.Guid.Should().NotBe(chosen);
        }

        [Fact]
        public async Task InsertRefusesBadInputWithAValidationFailurePerFieldAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, user);
            var service = await BuildServiceAsync(dbContext, user);

            async Task<List<string>> CodesAsync(CaseSearchFilterDto dto)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToList();
            }

            var blank = Request(workflow, " ");
            (await CodesAsync(blank)).Should().Contain("NameNotEmpty");

            var longName = Request(workflow, new string('n', 257));
            (await CodesAsync(longName)).Should().Contain("NameMaximumLength");

            var badFilter = Request(workflow);
            badFilter.FilterJson = "{not json";
            (await CodesAsync(badFilter)).Should().Contain("FilterJsonInvalid");

            var noSelect = Request(workflow);
            noSelect.SelectJson = null;
            (await CodesAsync(noSelect)).Should().Contain("SelectJsonNotEmpty");

            var noWorkflow = Request(Guid.Empty);
            (await CodesAsync(noWorkflow)).Should().Contain("CaseWorkflowGuidNotEmpty");

            var unknownWorkflow = Request(Guid.NewGuid());
            (await CodesAsync(unknownWorkflow)).Should().Contain("CaseWorkflowGuidNotFound");

            var unknownRole = Request(workflow);
            unknownRole.SharedRoleRegistryGuid = Guid.NewGuid();
            (await CodesAsync(unknownRole)).Should().Contain("SharedRoleRegistryGuidNotFound");

            (await dbContext.CaseSearchFilter.CountAsync(w => w.CaseWorkflow.Guid == workflow)).Should().Be(0);
        }

        [Fact]
        public async Task InsertRefusesAWorkflowTheCallersRoleIsNotGrantedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await SeedWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(Request(workflow)));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseWorkflowGuidNotFound");
        }

        [Fact]
        public async Task InsertRefusesAWorkflowAndARoleFromAnotherTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await SeedWorkflowAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(Request(workflow)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseWorkflowGuidNotFound");

            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var request = Request(workflow);
            request.SharedRoleRegistryGuid = await RoleOfAsync(dbContext, fx.Seed.UserTenantB);

            var shareEx = await Assert.ThrowsAsync<DtoValidationException>(() => ownerService.InsertAsync(request));
            shareEx.Result.Errors.Should().Contain(e => e.ErrorCode == "SharedRoleRegistryGuidNotFound");
        }

        [Fact]
        public async Task NamesAreUniquePerOwnerAndWorkflowIgnoringCaseButNotAcrossOwnersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var other = fx.Seed.UserWithPermissionNoApproval;
            var workflow = await SeedWorkflowAsync(dbContext, owner, other);
            var ownerService = await BuildServiceAsync(dbContext, owner);
            var otherService = await BuildServiceAsync(dbContext, other);
            var secondWorkflow = await SeedWorkflowAsync(dbContext, owner);

            await ownerService.InsertAsync(Request(workflow, "Cash"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                ownerService.InsertAsync(Request(workflow, "CASH")));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            await otherService.InsertAsync(Request(workflow, "Cash"));
            await ownerService.InsertAsync(Request(secondWorkflow, "Cash"));
        }

        [Fact]
        public async Task ValidateReportsEveryFailureWithoutStoringAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            await service.InsertAsync(Request(workflow, "Taken"));

            var clean = await service.ValidateAsync(Request(workflow, "Fresh"));
            clean.IsValid.Should().BeTrue();

            var request = Request(workflow, "taken");
            request.FilterJson = "{broken";
            var result = await service.ValidateAsync(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Select(e => e.ErrorCode).Should().Contain(["NameDuplicate", "FilterJsonInvalid"]);

            (await dbContext.CaseSearchFilter.CountAsync(w => w.CaseWorkflow.Guid == workflow)).Should().Be(1);
        }

        [Fact]
        public async Task ListReturnsOwnAndSharedSearchesOrderedByNameAndNeverAnotherUsersPrivateOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var other = fx.Seed.UserWithPermissionNoApproval;
            var workflow = await SeedWorkflowAsync(dbContext, owner, other);
            var otherWorkflow = await SeedWorkflowAsync(dbContext, owner, other);
            var ownerService = await BuildServiceAsync(dbContext, owner);
            var otherService = await BuildServiceAsync(dbContext, other);

            var shared = Request(workflow, "B shared");
            shared.SharedRoleRegistryGuid = await RoleOfAsync(dbContext, other);

            await ownerService.InsertAsync(shared);
            await ownerService.InsertAsync(Request(workflow, "A private"));
            await ownerService.InsertAsync(Request(otherWorkflow, "C elsewhere"));
            await otherService.InsertAsync(Request(workflow, "D theirs"));

            var forOther = await otherService.ListAsync(workflow);
            forOther.Select(s => s.Name).Should().Equal("B shared", "D theirs");
            forOther.Single(s => s.Name == "B shared").Mine.Should().BeFalse();
            forOther.Single(s => s.Name == "D theirs").Mine.Should().BeTrue();

            var forOwner = await ownerService.ListAsync(workflow);
            forOwner.Select(s => s.Name).Should().Equal("A private", "B shared");

            var everything = (await ownerService.ListAsync()).Where(s =>
                s.CaseWorkflowGuid == workflow || s.CaseWorkflowGuid == otherWorkflow);
            everything.Select(s => s.Name).Should().Equal("A private", "B shared", "C elsewhere");
        }

        [Fact]
        public async Task ListHidesSearchesOfAWorkflowTheCallersRoleNoLongerHoldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            await service.InsertAsync(Request(workflow));

            (await service.ListAsync(workflow)).Should().HaveCount(1);

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>().Where(w => w.CaseWorkflowGuid == workflow)
                .Set(w => w.Deleted, (byte)1).UpdateAsync();

            (await service.ListAsync(workflow)).Should().BeEmpty();
        }

        [Fact]
        public async Task ListNeverCrossesTenantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await SeedWorkflowAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var role = await RoleOfAsync(dbContext, fx.Seed.UserTenantB);
            var request = Request(workflow);
            await service.InsertAsync(request);

            await dbContext.CaseSearchFilter.Where(w => w.CaseWorkflow.Guid == workflow)
                .Set(w => w.SharedRoleRegistryGuid, role).UpdateAsync();

            var tenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantB.ListAsync(workflow)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetReturnsOwnAndSharedSearchesAndNotFoundForAnotherUsersPrivateOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var other = fx.Seed.UserWithPermissionNoApproval;
            var workflow = await SeedWorkflowAsync(dbContext, owner, other);
            var ownerService = await BuildServiceAsync(dbContext, owner);
            var otherService = await BuildServiceAsync(dbContext, other);

            var privateOne = await ownerService.InsertAsync(Request(workflow, "Private"));
            var shared = Request(workflow, "Shared");
            shared.SharedRoleRegistryGuid = await RoleOfAsync(dbContext, other);
            var sharedOne = await ownerService.InsertAsync(shared);

            (await ownerService.GetByGuidAsync(privateOne.Guid)).Mine.Should().BeTrue();
            (await otherService.GetByGuidAsync(sharedOne.Guid)).Mine.Should().BeFalse();

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => otherService.GetByGuidAsync(privateOne.Guid));
            ex.Code.Should().Be("NotFound");
            await Assert.ThrowsAsync<NotFoundException>(() => otherService.GetByGuidAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateByTheOwnerChangesTheSearchRaisesTheVersionAndPublishesAnUpdatedEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, owner, serviceChangeBus: bus);
            var created = await service.InsertAsync(Request(workflow, "First"));

            created.Name = "Second";
            created.FilterJson = FilterJson("changed");
            created.SharedRoleRegistryGuid = await RoleOfAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var updated = await service.UpdateAsync(created);

            updated.Name.Should().Be("Second");
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(owner);
            updated.UpdatedDate.Should().NotBeNull();
            updated.SharedRoleRegistryGuid.Should().Be(created.SharedRoleRegistryGuid);
            updated.CreatedUser.Should().Be(owner);
            updated.CaseWorkflowGuid.Should().Be(workflow);

            var stored = await dbContext.CaseSearchFilter.FirstAsync(w => w.Guid == created.Guid);
            stored.Name.Should().Be("Second");
            stored.FilterJson.Should().Contain("changed");

            bus.Published.Should().Contain(e => e.Area == "CaseSearchFilter" && e.Kind == ServiceChangeKind.Updated
                                                                          && e.Version == 2);
        }

        [Fact]
        public async Task OnlyTheOwnerCanUpdateOrDeleteEvenWhenTheSearchIsSharedWithTheirRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var other = fx.Seed.UserWithPermissionNoApproval;
            var workflow = await SeedWorkflowAsync(dbContext, owner, other);
            var ownerService = await BuildServiceAsync(dbContext, owner);
            var otherService = await BuildServiceAsync(dbContext, other);
            var request = Request(workflow, "Shared");
            request.SharedRoleRegistryGuid = await RoleOfAsync(dbContext, other);
            var created = await ownerService.InsertAsync(request);

            created.Name = "Hijacked";

            await Assert.ThrowsAsync<NotFoundException>(() => otherService.UpdateAsync(created));
            await Assert.ThrowsAsync<NotFoundException>(() => otherService.DeleteAsync(created.Guid));

            var stored = await dbContext.CaseSearchFilter.FirstAsync(w => w.Guid == created.Guid);
            stored.Name.Should().Be("Shared");
            stored.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            var created = await service.InsertAsync(Request(workflow, "Versioned"));

            created.Name = "Newer";
            await service.UpdateAsync(created);

            created.Name = "Stale";
            var ex = await Assert.ThrowsAsync<VersionConflictException>(() => service.UpdateAsync(created));
            ex.SuppliedVersion.Should().Be(1);
            ex.CurrentVersion.Should().Be(2);

            created.Version = 0;
            created.Name = "Unchecked";
            (await service.UpdateAsync(created)).Version.Should().Be(3);
        }

        [Fact]
        public async Task UpdateCannotMoveASearchToAnotherWorkflowOrOntoAnExistingNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var second = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            var created = await service.InsertAsync(Request(workflow, "Mine"));
            await service.InsertAsync(Request(workflow, "Taken"));

            var moved = Request(second, "Mine");
            moved.Guid = created.Guid;
            var moveEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(moved));
            moveEx.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseWorkflowGuidCannotChange");

            created.Name = "taken";
            var nameEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(created));
            nameEx.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task DeleteRemovesTheSearchPublishesADeletedEventAndAMissingOneIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, owner, serviceChangeBus: bus);
            var created = await service.InsertAsync(Request(workflow));

            await service.DeleteAsync(created.Guid);

            (await dbContext.CaseSearchFilter.CountAsync(w => w.Guid == created.Guid)).Should().Be(0);
            bus.Published.Should().Contain(e => e.Area == "CaseSearchFilter" && e.Kind == ServiceChangeKind.Deleted);
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Guid));
        }

        [Fact]
        public async Task DeletingTheWorkflowRemovesItsSavedSearchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            var created = await service.InsertAsync(Request(workflow));

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>().Where(w => w.CaseWorkflowGuid == workflow)
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow).DeleteAsync();

            (await dbContext.CaseSearchFilter.CountAsync(w => w.Guid == created.Guid)).Should().Be(0);
        }

        [Fact]
        public async Task ASavedSearchRoundTripsThroughStorageIntoTheCompilerWithTheCallersRightsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = fx.Seed.UserWithPermission;
            var workflow = await SeedWorkflowAsync(dbContext, owner);
            var service = await BuildServiceAsync(dbContext, owner);
            var created = await service.InsertAsync(Request(workflow));
            var read = await service.GetByGuidAsync(created.Guid);

            var compiler = await SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, owner, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);

            var compiled = await compiler.InsertAsync(new SessionCaseSearchCompiledSqlDto
            {
                CaseWorkflowGuid = read.CaseWorkflowGuid,
                FilterJson = read.FilterJson,
                SelectJson = read.SelectJson
            });

            compiled.Prepared.Should().Be(1, compiled.Error);
        }

        [Fact]
        public void EveryOperationIsRegisteredAsAnAgentToolWithTheCasesPermission()
        {
            var names = new[]
            {
                "CaseSearchFilterList", "CaseSearchFilterGet", "CaseSearchFilterCreate", "CaseSearchFilterValidate",
                "CaseSearchFilterUpdate", "CaseSearchFilterDelete"
            };

            var tools = ServiceToolCatalogue.All.Where(t => names.Contains(t.Name)).ToList();

            tools.Select(t => t.Name).Should().BeEquivalentTo(names);
            tools.Should().OnlyContain(t => t.RequiredPermissions.SequenceEqual(new[] { 1 }));
            tools.Single(t => t.Name == "CaseSearchFilterDelete").Kind.Should().Be(OperationKind.Delete);
        }
    }
}

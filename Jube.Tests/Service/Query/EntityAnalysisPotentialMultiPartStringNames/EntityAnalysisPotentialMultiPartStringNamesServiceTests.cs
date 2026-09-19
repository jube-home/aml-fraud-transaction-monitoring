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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisPotentialMultiPartStringNames;
using Jube.Service.Observability;
using Jube.Service.Query.EntityAnalysisPotentialMultiPartStringNames;
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

namespace Jube.Test.Service.Query.EntityAnalysisPotentialMultiPartStringNames
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisPotentialMultiPartStringNamesServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdInlineFunctionIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdXpathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelInlineFunction.Where(w => createdInlineFunctionIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            var roleIds = createdRoleRegistryIds.Select(id => (int?)id).ToList();
            await dbContext.RoleRegistryPermission.Where(w => roleIds.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisPotentialMultiPartStringNamesService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisPotentialMultiPartStringNamesService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static string UniqueName(string label) =>
            $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[
                ..Math.Min(40, DatabaseFixture.Prefix.Length + label.Length + 32)];

        private async Task<Data.Poco.EntityAnalysisModel> CreateModelAsync(DbContext dbContext, string ownerUser)
        {
            var tenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == ownerUser)
                .Select(s => s.TenantRegistryId).FirstAsync();
            var model = new Data.Poco.EntityAnalysisModel
            {
                Name = UniqueName("Model"),
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            createdModelIds.Add(model.Id);
            return model;
        }

        private async Task AddXpathAsync(DbContext dbContext, int modelId, string name, int dataTypeId,
            byte? deleted = 0)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                DataTypeId = dataTypeId,
                XPath = "$.x",
                Active = 1,
                Locked = 0,
                Deleted = deleted,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdXpathIds.Add(id);
        }

        private async Task AddInlineFunctionAsync(DbContext dbContext, int modelId, string name, int returnDataTypeId,
            byte? deleted = 0)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelInlineFunction
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                ReturnDataTypeId = returnDataTypeId,
                FunctionScript = "Return 1",
                Active = 1,
                Locked = 0,
                Deleted = deleted,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdInlineFunctionIds.Add(id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisPotentialMultiPartStringNamesService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

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
        public async Task BothOverloadsThrowForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var byId = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
            byId.Code.Should().Be("PermissionDenied");
            byId.RequiredSpecifications.Should().BeEquivalentTo([11, 17, 4, 1]);

            var byGuid = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByGuidAsync(Guid.NewGuid()));
            byGuid.RequiredSpecifications.Should().BeEquivalentTo([11, 17, 4, 1]);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(17)]
        [InlineData(4)]
        [InlineData(1)]
        public async Task AnyOneOfTheFourPermissionsIsSufficientAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, spec);
            var service = await BuildServiceAsync(dbContext, userName);

            (await service.GetByIdAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task UnrelatedPermissionAloneIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, 36);
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
        }

        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        private async Task<string> CreateUserWithExactPermissionAsync(DbContext dbContext, int spec)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}MpTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantRegistryIds.Add(tenantRegistryId);
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}MpRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantRegistryId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleId);
            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            var userName = $"{DatabaseFixture.Prefix}MpUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used", Active = 1, PasswordLocked = 0,
                Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);
            await dbContext.InsertAsync(new Data.Poco.UserInTenant
                { User = userName, TenantRegistryId = tenantRegistryId });
            return userName;
        }

        [Fact]
        public async Task ReturnsStringXpathsAndStringInlineFunctionsSortedAndDistinctForBothOverloadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var nameZ = UniqueName("Z");
            var nameA = UniqueName("A");
            var nameM = UniqueName("M");
            var shared = UniqueName("S");

            await AddXpathAsync(dbContext, model.Id, nameZ, 1);
            await AddXpathAsync(dbContext, model.Id, shared, 1);
            await AddInlineFunctionAsync(dbContext, model.Id, nameA, 1);
            await AddInlineFunctionAsync(dbContext, model.Id, shared, 1);
            await AddXpathAsync(dbContext, model.Id, nameM, 1, null);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var expected = new[] { nameA, nameM, shared, nameZ }.OrderBy(s => s, StringComparer.Ordinal).ToList();
            var byId = await service.GetByIdAsync(model.Id);
            var byGuid = await service.GetByGuidAsync(model.Guid);

            byId.Count.Should().Be(4);
            byId.Should().BeEquivalentTo(expected);
            byGuid.Should().Equal(byId);
        }

        [Fact]
        public async Task InactiveXpathsAndFunctionsAreNotListedAndReactivationListsThemAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var other = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var xpath = UniqueName("X");
            var function = UniqueName("F");
            await AddXpathAsync(dbContext, model.Id, xpath, 1);
            await AddInlineFunctionAsync(dbContext, model.Id, function, 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var expected = new[] { xpath, function }.OrderBy(s => s, StringComparer.Ordinal).ToList();

            (await service.GetByIdAsync(model.Id)).Should().Equal(expected);

            async Task SetActiveAsync(DbContext context, byte active)
            {
                await context.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id))
                    .Set(w => w.Active, active).UpdateAsync();
                await context.EntityAnalysisModelInlineFunction.Where(w => createdInlineFunctionIds.Contains(w.Id))
                    .Set(w => w.Active, active).UpdateAsync();
            }

            await SetActiveAsync(dbContext, 0);
            (await service.GetByIdAsync(model.Id)).Should().BeEmpty();
            (await service.GetByGuidAsync(model.Guid)).Should().BeEmpty();

            await SetActiveAsync(dbContext, 1);
            (await service.GetByIdAsync(model.Id)).Should().Equal(expected);
            (await service.GetByGuidAsync(model.Guid)).Should().Equal(expected);
            (await service.GetByIdAsync(other.Id)).Should().BeEmpty();
        }

        [Fact]
        public async Task ExcludesNonStringDeletedAndOtherModelRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var otherModel = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var kept = UniqueName("Kept");
            var wrongType = UniqueName("Int");
            var deletedX = UniqueName("DelX");
            var deletedF = UniqueName("DelF");
            var wrongTypeF = UniqueName("IntF");
            var otherName = UniqueName("Other");

            await AddXpathAsync(dbContext, model.Id, kept, 1);
            await AddXpathAsync(dbContext, model.Id, wrongType, 2);
            await AddXpathAsync(dbContext, model.Id, deletedX, 1, 1);
            await AddInlineFunctionAsync(dbContext, model.Id, deletedF, 1, 1);
            await AddInlineFunctionAsync(dbContext, model.Id, wrongTypeF, 3);
            await AddXpathAsync(dbContext, otherModel.Id, otherName, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByIdAsync(model.Id)).Should().Equal(kept);
            (await service.GetByGuidAsync(model.Guid)).Should().Equal(kept);
        }

        [Fact]
        public async Task EmptyResultForModelWithNoRowsAndForUnknownModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByIdAsync(model.Id)).Should().BeEmpty();
            (await service.GetByGuidAsync(model.Guid)).Should().BeEmpty();
            (await service.GetByIdAsync(int.MaxValue)).Should().BeEmpty();
            (await service.GetByGuidAsync(Guid.NewGuid())).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBModelIsInvisibleToTenantAAndViceVersaForBothOverloadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var nameA = UniqueName("TenA");
            var nameB = UniqueName("TenB");
            await AddXpathAsync(dbContext, modelA.Id, nameA, 1);
            await AddInlineFunctionAsync(dbContext, modelA.Id, nameA + "f", 1);
            await AddXpathAsync(dbContext, modelB.Id, nameB, 1);
            await AddInlineFunctionAsync(dbContext, modelB.Id, nameB + "f", 1);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetByIdAsync(modelB.Id)).Should().BeEmpty();
            (await serviceA.GetByGuidAsync(modelB.Guid)).Should().BeEmpty();
            (await serviceB.GetByIdAsync(modelA.Id)).Should().BeEmpty();
            (await serviceB.GetByGuidAsync(modelA.Guid)).Should().BeEmpty();

            (await serviceA.GetByIdAsync(modelA.Id)).Should().BeEquivalentTo([nameA, nameA + "f"]);
            (await serviceB.GetByGuidAsync(modelB.Guid)).Should().BeEquivalentTo([nameB, nameB + "f"]);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetByIdAsync(1, cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.GetByGuidAsync(Guid.NewGuid(), cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetByIdAsync(1));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAndExactlyOneAuditLineAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);
            await service.GetByGuidAsync(Guid.NewGuid());

            activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisPotentialMultiPartStringNames.GetByGuid")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=GetByGuid");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetByIdAsync(1);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetByIdAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersBothUniqueToolNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("EntityAnalysisPotentialMultiPartStringNamesGetById");
            names.Should().Contain("EntityAnalysisPotentialMultiPartStringNamesGetByGuid");
        }
    }
}
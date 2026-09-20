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
using Jube.Dto.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType;
using Jube.Service.Observability;
using Jube.Service.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType;
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

namespace Jube.Test.Service.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeServiceTests(
        DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private const string ScriptCode =
            "public class Fields { public string ScriptString { get; set; } " +
            "[SearchKey] public double ScriptFloat { get; set; } public int ScriptInt { get; set; } " +
            "private string Hidden { get; set; } }";

        private readonly List<int> functionIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> modelScriptIds = [];
        private readonly List<int> scriptIds = [];
        private readonly List<int> xpathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelInlineScript.Where(w => modelScriptIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisInlineScript.Where(w => scriptIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelInlineFunction.Where(w => functionIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xpathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
            var roleIdsNullable = createdRoleIds.Select(id => (int?)id).ToList();
            await dbContext.RoleRegistryPermission.Where(w => roleIdsNullable.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService>
            BuildServiceAsync(
                DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
                IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService.CreateAsync(dbContext,
                userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private Task<int> TenantOfAsync(DbContext dbContext, string userName)
        {
            return dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstAsync();
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, int tenantRegistryId)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            modelIds.Add(id);
            return id;
        }

        private async Task AddXpathAsync(DbContext dbContext, int modelId, string name, int dataTypeId,
            byte searchKey, byte deleted = 0)
        {
            xpathIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                DataTypeId = dataTypeId,
                XPath = "$." + name,
                SearchKey = searchKey,
                Active = 1,
                Locked = 0,
                Deleted = deleted,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            }));
        }

        private async Task AddFunctionAsync(DbContext dbContext, int modelId, string name, int returnDataTypeId)
        {
            functionIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelInlineFunction
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                FunctionScript = "return 1;",
                ReturnDataTypeId = returnDataTypeId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            }));
        }

        private async Task AddScriptAsync(DbContext dbContext, int modelId)
        {
            var scriptId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisInlineScript
            {
                Name = $"{DatabaseFixture.Prefix}Script{Guid.NewGuid():N}"[..40],
                Code = ScriptCode,
                LanguageId = 2,
                CreatedDate = DateTime.UtcNow
            });
            scriptIds.Add(scriptId);
            modelScriptIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelInlineScript
                {
                    EntityAnalysisModelId = modelId,
                    EntityAnalysisInlineScriptId = scriptId,
                    Name = "ModelScript",
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    Guid = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                }));
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
                EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeService.CreateAsync(dbContext,
                    userName, log, localizers,
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
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([11, 17, 4, 12]);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(17)]
        [InlineData(4)]
        [InlineData(12)]
        public async Task AnyOneOfTheFourPermissionsSufficesAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, spec);
            var service = await BuildServiceAsync(dbContext, userName);

            var result = await service.GetAsync(-1);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UnrelatedPermissionAloneIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, 36);
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(-1));
        }

        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<string> createdUserNames = [];

        private async Task<string> CreateUserWithExactPermissionAsync(DbContext dbContext,
            int permissionSpecificationId)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}XqTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}XqRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);
            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = permissionSpecificationId,
                RoleRegistryId = roleId, Active = 1, Locked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            var userName = $"{DatabaseFixture.Prefix}XqUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });
            return userName;
        }

        [Fact]
        public async Task GetAsyncReturnsXpathsScriptPropertiesAndFunctionsMappedFieldByFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var modelId = await CreateModelAsync(dbContext, tenantId);
            await AddXpathAsync(dbContext, modelId, "XpStringKey", 1, 1);
            await AddXpathAsync(dbContext, modelId, "XpFloatPlain", 3, 0);
            await AddXpathAsync(dbContext, modelId, "XpIntExcluded", 2, 1);
            await AddXpathAsync(dbContext, modelId, "XpDeletedExcluded", 1, 1, 1);
            await AddScriptAsync(dbContext, modelId);
            await AddFunctionAsync(dbContext, modelId, "FnString", 1);
            await AddFunctionAsync(dbContext, modelId, "FnFloatExcluded", 3);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(modelId);

            result.Select(r => r.Name).Should().Equal("FnString", "ScriptFloat", "ScriptString", "XpFloatPlain",
                "XpStringKey");
            Map(result, "XpStringKey").Should().BeEquivalentTo(
                new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
                    { Name = "XpStringKey", DataTypeId = 1, SearchKey = true });
            Map(result, "XpFloatPlain").Should().BeEquivalentTo(
                new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
                    { Name = "XpFloatPlain", DataTypeId = 3, SearchKey = false });
            Map(result, "ScriptString").Should().BeEquivalentTo(
                new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
                    { Name = "ScriptString", DataTypeId = 1, SearchKey = false });
            Map(result, "ScriptFloat").Should().BeEquivalentTo(
                new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
                    { Name = "ScriptFloat", DataTypeId = 3, SearchKey = true });
            Map(result, "FnString").Should().BeEquivalentTo(
                new EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
                    { Name = "FnString", DataTypeId = 1, SearchKey = false });
        }

        private async Task SetActiveAsync(DbContext dbContext, byte active)
        {
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xpathIds.Contains(w.Id))
                .Set(w => w.Active, active).UpdateAsync();
            await dbContext.EntityAnalysisModelInlineFunction.Where(w => functionIds.Contains(w.Id))
                .Set(w => w.Active, active).UpdateAsync();
            await dbContext.EntityAnalysisModelInlineScript.Where(w => modelScriptIds.Contains(w.Id))
                .Set(w => w.Active, active).UpdateAsync();
        }

        [Fact]
        public async Task InactiveXpathsScriptsAndFunctionsAreNotListedAndReactivationListsThemAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var modelId = await CreateModelAsync(dbContext, tenantId);
            var otherModelId = await CreateModelAsync(dbContext, tenantId);
            await AddXpathAsync(dbContext, modelId, "XpActive", 1, 1);
            await AddScriptAsync(dbContext, modelId);
            await AddFunctionAsync(dbContext, modelId, "FnActive", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var active = (await service.GetAsync(modelId)).Select(r => r.Name).ToList();
            active.Should().Equal("FnActive", "ScriptFloat", "ScriptString", "XpActive");

            await SetActiveAsync(dbContext, 0);
            (await service.GetAsync(modelId)).Should().BeEmpty();

            await SetActiveAsync(dbContext, 1);
            (await service.GetAsync(modelId)).Select(r => r.Name).Should().Equal(active);

            (await service.GetAsync(otherModelId)).Should().BeEmpty();
        }

        private static EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto Map(
            List<EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto> result, string name) =>
            result.Single(r => r.Name == name);

        [Fact]
        public async Task GetAsyncReturnsEmptyForModelWithNoFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var modelId = await CreateModelAsync(dbContext, tenantId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(modelId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var modelA = await CreateModelAsync(dbContext, tenantA);
            var modelB = await CreateModelAsync(dbContext, tenantB);
            await AddXpathAsync(dbContext, modelA, "OnlyA", 1, 0);
            await AddFunctionAsync(dbContext, modelA, "FnOnlyA", 1);
            await AddXpathAsync(dbContext, modelB, "OnlyB", 1, 0);
            await AddFunctionAsync(dbContext, modelB, "FnOnlyB", 1);
            await AddScriptAsync(dbContext, modelB);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(modelB)).Should().BeEmpty();
            (await serviceB.GetAsync(modelA)).Should().BeEmpty();
            (await serviceA.GetAsync(modelA)).Select(r => r.Name).Should().Equal("FnOnlyA", "OnlyA");
            (await serviceB.GetAsync(modelB)).Select(r => r.Name).Should()
                .Equal("FnOnlyB", "OnlyB", "ScriptFloat", "ScriptString");
        }

        [Fact]
        public async Task OtherTenantInlineFunctionsDoNotLeakThroughTheUnscopedDataQueryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var modelB = await CreateModelAsync(dbContext, tenantB);
            await AddFunctionAsync(dbContext, modelB, "SecretFunction", 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(modelB);

            result.Should().NotContain(r => r.Name == "SecretFunction");
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(1, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

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

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(1));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(-1);

            activities.Should().ContainSingle(a =>
                    a.OperationName == "EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.Get")
                .Subject
                .GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(-1);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(-1);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(-1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeGet");
        }
    }
}
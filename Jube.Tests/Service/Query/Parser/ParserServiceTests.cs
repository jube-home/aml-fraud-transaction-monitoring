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
using Jube.Dto.Query.Parser;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Reactivity;
using Jube.Service.Exceptions.Query.Parser;
using Jube.Service.Query.Parser;
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

namespace Jube.Test.Service.Query.Parser
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ParserServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private const string ValidGatewayRule = "If (Payload.ParserAmount > 0) Then\n   Return True\nEnd If";

        private readonly List<int> createdExhaustiveIds = [];
        private readonly List<int> createdHttpAdaptationIds = [];
        private readonly List<int> createdInlineFunctionIds = [];
        private readonly List<int> createdInlineScriptIds = [];
        private readonly List<int> createdModelInlineScriptIds = [];
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

            await dbContext.EntityAnalysisModelInlineScript.Where(w => createdModelInlineScriptIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisInlineScript>()
                .Where(w => createdInlineScriptIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelInlineFunction>()
                .Where(w => createdInlineFunctionIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelHttpAdaptation>()
                .Where(w => createdHttpAdaptationIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>()
                .Where(w => createdExhaustiveIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => modelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => roleIds.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<ParserService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null, ILog? auditLog = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return ParserService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static ParseRuleRequestDto Req(int modelId, string? text, int type = 2) => new()
        {
            EntityAnalysisModelId = modelId, RuleText = text, RuleParseType = type
        };

        private async Task<(string UserName, int TenantId)> CreateUserAsync(DbContext dbContext,
            params int[] permissionSpecificationIds)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}PrsTenant{suffix}",
                Active = 1, Locked = 0, Deleted = 0, Landlord = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantRegistryIds.Add(tenantRegistryId);

            var roleRegistryGuid = Guid.NewGuid();
            var roleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleRegistryGuid,
                Name = $"{DatabaseFixture.Prefix}PrsRole{suffix}",
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

            var userName = $"{DatabaseFixture.Prefix}PrsUser{suffix}";
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
                { User = userName, TenantRegistryId = tenantRegistryId });

            return (userName, tenantRegistryId);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, int tenantRegistryId,
            params (string Name, int DataTypeId)[] xpaths)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}PrsModel{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1, Locked = 0, Deleted = 0, Version = 1,
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

            return id;
        }

        private async Task<(string User, int Model)> PermittedUserWithModelAsync(DbContext dbContext,
            params (string Name, int DataTypeId)[] xpaths)
        {
            var (user, tenant) = await CreateUserAsync(dbContext, 26);
            return (user, await CreateModelAsync(dbContext, tenant, xpaths));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserIsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName, log));
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UnmappedAndNoTenantUsersAreNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, $"{DatabaseFixture.Prefix}NoSuchUser"));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task MissingPermissionIsForbiddenWithCodeAndSpecsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ParseAsync(Req(1, "Return True")));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().Equal(8, 10, 13, 14, 16, 17, 25, 26);
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Theory]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(25)]
        [InlineData(26)]
        public async Task AnySingleSpecificationOfTheSetPermitsAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, spec);
            var modelId = await CreateModelAsync(dbContext, tenant, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.ParseAsync(Req(modelId, ValidGatewayRule));

            result.Message.Should().Be("Compiled");
        }

        [Fact]
        public async Task UnrelatedSpecificationDoesNotPermitAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, _) = await CreateUserAsync(dbContext, 36);
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ParseAsync(Req(1, "Return True")));
        }

        [Fact]
        public async Task ValidRuleCompilesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.ParseAsync(Req(modelId, ValidGatewayRule));

            result.Message.Should().Be("Compiled");
            result.ErrorSpans.Should().BeNull();
        }

        [Fact]
        public async Task UnknownFieldReturnsErrorWithLocatedSpansAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, user);

            var result =
                await service.ParseAsync(Req(modelId, "If (Payload.NoSuchField > 0) Then\n   Return True\nEnd If"));

            result.Message.Should().Contain("Request XPath does not exist for NoSuchField");
            result.ErrorSpans.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CompilerErrorsAreReturnedWithMessageAndSpansAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.ParseAsync(Req(modelId,
                "If (Payload.ParserAmount > 0 Then\n   Return Nonsense(\nEnd If"));

            result.Message.Should().NotBe("Compiled");
            result.Message.Should().NotBeNullOrWhiteSpace();
            result.ErrorSpans.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task EmptyRuleTextIsHandledWithoutThrowingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.ParseAsync(Req(modelId, ""));

            result.Message.Should().NotBeNull();
        }

        [Fact]
        public async Task NullRuleTextIsSwallowedIntoErrorResultAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, user, log);

            var result = await service.ParseAsync(Req(modelId, null));

            result.Message.Should().Be("Error");
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task ForeignTenantModelFieldsAreNotVisibleToTheParserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userA, _) = await CreateUserAsync(dbContext, 26);
            var (_, tenantB) = await CreateUserAsync(dbContext, 26);
            var foreignModel = await CreateModelAsync(dbContext, tenantB, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, userA);

            var result = await service.ParseAsync(Req(foreignModel, ValidGatewayRule));

            result.Message.Should().Contain("Request XPath does not exist for ParserAmount");
        }

        [Fact]
        public async Task SameRuleCompilesForOwnerTenantButNotForeignTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userA, tenantA) = await CreateUserAsync(dbContext, 26);
            var (userB, _) = await CreateUserAsync(dbContext, 26);
            var modelA = await CreateModelAsync(dbContext, tenantA, ("ParserAmount", 3));
            var serviceA = await BuildServiceAsync(dbContext, userA);
            var serviceB = await BuildServiceAsync(dbContext, userB);

            (await serviceA.ParseAsync(Req(modelA, ValidGatewayRule))).Message.Should().Be("Compiled");
            (await serviceB.ParseAsync(Req(modelA, ValidGatewayRule))).Message.Should()
                .Contain("Request XPath does not exist for ParserAmount");
        }

        [Fact]
        public async Task ForeignTenantInlineFunctionIsNotVisibleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (userA, _) = await CreateUserAsync(dbContext, 26);
            var (_, tenantB) = await CreateUserAsync(dbContext, 26);
            var foreignModel = await CreateModelAsync(dbContext, tenantB);
            await CreateInlineFunctionAsync(dbContext, foreignModel, "ParserFn", 3);
            var service = await BuildServiceAsync(dbContext, userA);

            var result =
                await service.ParseAsync(Req(foreignModel, "If (Payload.ParserFn > 0) Then\n   Return True\nEnd If"));

            result.Message.Should().Contain("Request XPath does not exist for ParserFn");
        }

        private async Task CreateInlineFunctionAsync(DbContext dbContext, int modelId, string name, int returnType)
        {
            createdInlineFunctionIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelInlineFunction
                {
                    EntityAnalysisModelId = modelId, Name = name, ReturnDataTypeId = returnType,
                    FunctionScript = "Return 1", Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                }));
        }

        [Fact]
        public async Task InlineFunctionIsResolvedByTheParserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext);
            await CreateInlineFunctionAsync(dbContext, modelId, "ParserFn", 3);
            var service = await BuildServiceAsync(dbContext, user);

            var result =
                await service.ParseAsync(Req(modelId, "If (Payload.ParserFn > 0) Then\n   Return True\nEnd If"));

            result.Message.Should().Be("Compiled");
        }

        [Fact]
        public async Task InlineScriptPublicPropertiesAreResolvedByTheParserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext);
            var scriptId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisInlineScript
            {
                Name = $"{DatabaseFixture.Prefix}PrsScript",
                Code = "public class ParserScript { public double ScriptAmount { get; set; } }",
                LanguageId = 2, Compiled = 1, CreatedDate = DateTime.UtcNow
            });
            createdInlineScriptIds.Add(scriptId);
            createdModelInlineScriptIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelInlineScript
                {
                    EntityAnalysisModelId = modelId, EntityAnalysisInlineScriptId = scriptId,
                    Name = $"{DatabaseFixture.Prefix}PrsModelScript", Active = 1, Locked = 0, Deleted = 0,
                    Version = 1, Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                }));
            var service = await BuildServiceAsync(dbContext, user);

            var result =
                await service.ParseAsync(Req(modelId, "If (Payload.ScriptAmount > 0) Then\n   Return True\nEnd If"));

            result.Message.Should().Be("Compiled");
        }

        [Fact]
        public async Task HttpAndExhaustiveAdaptationsAreVisibleForActivationRulesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext);
            createdHttpAdaptationIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelHttpAdaptation
                {
                    EntityAnalysisModelId = modelId, Name = "ParserHttp", Active = 1, Locked = 0, Deleted = 0,
                    Version = 1, Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                }));
            createdExhaustiveIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    EntityAnalysisModelId = modelId, Name = "ParserExh", Active = 1, Locked = 0, Deleted = 0,
                    Version = 1, Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                }));
            var service = await BuildServiceAsync(dbContext, user);

            var known = await service.ParseAsync(Req(modelId,
                "If (HttpAdaptation.ParserHttp.Value > 0 And ExhaustiveAdaptation.ParserExh.Value > 0) Then\n   Return True\nEnd If",
                5));
            var unknown = await service.ParseAsync(Req(modelId,
                "If (HttpAdaptation.NoHttp.Value > 0 And ExhaustiveAdaptation.NoExh.Value > 0) Then\n   Return True\nEnd If",
                5));

            known.Message.Should().NotContain("does not exist");
            unknown.Message.Should().Contain("NoHttp").And.Contain("NoExh");
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext, ("ParserAmount", 3));
            var service = await BuildServiceAsync(dbContext, user);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ParseAsync(Req(modelId, ValidGatewayRule), cts.Token));
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAndNoChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, modelId) = await PermittedUserWithModelAsync(dbContext, ("ParserAmount", 3));
            var auditLog = new TestLog();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, user, auditLog: auditLog, serviceChangeBus: bus);

            await service.ParseAsync(Req(modelId, ValidGatewayRule));

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Parse");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void ToolCatalogueListsParserParseOnce()
        {
            ServiceToolCatalogue.All.Count(t => t.Name == "ParserParse").Should().Be(1);
        }
    }
}
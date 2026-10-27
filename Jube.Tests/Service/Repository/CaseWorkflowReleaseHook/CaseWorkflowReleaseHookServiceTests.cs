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
using Jube.Case.Outbound;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Exceptions.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowReleaseHook;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowReleaseHook
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowReleaseHookServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> hookIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseWorkflowReleaseHookVersion
                .Where(w => w.CaseWorkflowReleaseHookId != null && hookIds.Contains(w.CaseWorkflowReleaseHookId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowReleaseHook.Where(w => hookIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowReleaseHookService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseWorkflowReleaseHookService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp, TestDynamicEnvironment.Create());
        }

        private async Task<int> CreateWorkflowAsync(DbContext dbContext, string userName)
        {
            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, userName).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                }).ConfigureAwait(false);
            modelIds.Add(model.Id);

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = 0, CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });
            workflowIds.Add(workflowId);

            return workflowId;
        }

        private static CaseWorkflowReleaseHookDto Hook(int workflowId, string name = "Faster payments")
        {
            return new CaseWorkflowReleaseHookDto
            {
                CaseWorkflowId = workflowId, Name = name, Active = true, HttpEndpointTypeId = 1,
                ReleaseUrl = "https://payments.example.com/release/[@Payload.PaymentId@]"
            };
        }

        private async Task<CaseWorkflowReleaseHookDto> SaveAsync(CaseWorkflowReleaseHookService service,
            CaseWorkflowReleaseHookDto model)
        {
            var saved = await service.InsertAsync(model);
            hookIds.Add(saved.Id);

            return saved;
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            var act = async () =>
            {
                await using var dbContext = fx.GetDbContext();

                await BuildServiceAsync(dbContext, null);
            };

            (await act.Should().ThrowAsync<NotAuthenticatedException>()).Which.Code.Should().Be("NotAuthenticated");
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenNamingPermission69Async()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.GetAsync();

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(69);
        }

        [Fact]
        public async Task InsertThenListAndGetRoundTripAHookUnderItsWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await SaveAsync(service, Hook(workflowId));

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CaseWorkflowId.Should().Be(workflowId);
            saved.Guid.Should().NotBeEmpty();

            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().ContainSingle(v => v.Id == saved.Id);
            (await service.GetByIdAsync(saved.Id))!.ReleaseUrl.Should().Contain("payments.example.com");
        }

        [Fact]
        public async Task UpdateKeepsTheEarlierStateAsAVersionAndChecksTheVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, Hook(workflowId));

            saved.ReleaseUrl = "https://payments.example.com/v2/release/[@Payload.PaymentId@]";
            var updated = await service.UpdateAsync(saved);

            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);

            await using var read = fx.GetDbContext();
            var versions = await read.CaseWorkflowReleaseHookVersion.Where(v => v.CaseWorkflowReleaseHookId == saved.Id)
                .ToListAsync();
            versions.Should().ContainSingle().Which.ReleaseUrl.Should().Contain("payments.example.com/release");

            saved.Version = 1;
            var stale = () => service.UpdateAsync(saved);
            await stale.Should().ThrowAsync<VersionConflictException>();
        }

        [Fact]
        public async Task ALockedHookCannotBeUpdatedOrDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, Hook(workflowId));

            await dbContext.CaseWorkflowReleaseHook.Where(w => w.Id == saved.Id).Set(s => s.Locked, (byte)1)
                .UpdateAsync();

            var update = () => service.UpdateAsync(saved);
            await update.Should().ThrowAsync<NotFoundException>();

            var delete = () => service.DeleteAsync(saved.Id);
            await delete.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteIsSoftAndTheHookNoLongerListsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(service, Hook(workflowId));

            await service.DeleteAsync(saved.Id);

            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();

            await using var read = fx.GetDbContext();
            var row = await read.CaseWorkflowReleaseHook.SingleAsync(w => w.Id == saved.Id);
            row.Deleted.Should().Be(1);
            row.DeletedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task AnotherTenantCannotSeeUpdateOrDeleteTheHookAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await SaveAsync(owner, Hook(workflowId));

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await other.GetByIdAsync(saved.Id)).Should().BeNull();
            (await other.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();
            await ((Func<Task>)(() => other.UpdateAsync(saved))).Should().ThrowAsync<NotFoundException>();
            await ((Func<Task>)(() => other.DeleteAsync(saved.Id))).Should().ThrowAsync<NotFoundException>();
            await ((Func<Task>)(() => other.InsertAsync(Hook(workflowId, "Intruder"))))
                .Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task TheNameMustBeUniqueWithinTheWorkflowIgnoringCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await SaveAsync(service, Hook(workflowId, "Faster"));

            var act = () => service.InsertAsync(Hook(workflowId, "FASTER"));

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "NameAlreadyExists");

            var otherWorkflow = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            await SaveAsync(service, Hook(otherWorkflow, "Faster"));
        }

        [Theory]
        [InlineData("https://[@Payload.PaymentId@]/release", "ReleaseUrlInvalid")]
        [InlineData("https://payments.example.com[@Payload.PaymentId@]", "ReleaseUrlInvalid")]
        [InlineData("payments.example.com/release", "ReleaseUrlInvalid")]
        [InlineData("ftp://payments.example.com/release", "ReleaseUrlInvalid")]
        [InlineData("https://payments.example.com/[@Payload.A@]/[@Secret.Anything@]", "ReleaseUrlInvalid")]
        [InlineData("", "ReleaseUrlNotEmpty")]
        public async Task AReleaseUrlWhoseHostIsNotLiteralOrWithAnUnknownTokenIsRefusedAsync(string url, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Hook(workflowId);
            model.ReleaseUrl = url;

            var act = () => service.InsertAsync(model);

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == code);
        }

        [Theory]
        [InlineData("http://169.254.169.254/latest/meta-data/[@Payload.PaymentId@]")]
        [InlineData("http://10.0.0.5/release")]
        [InlineData("http://127.0.0.1/release")]
        [InlineData("http://localhost:8080/release")]
        [InlineData("http://[::1]/release")]
        [InlineData("http://[::ffff:192.168.1.1]/release")]
        [InlineData("http://2130706433/release")]
        public async Task AReleaseOrRefuseUrlOnAnInternalHostIsRefusedAtSaveTimeAsync(string url)
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var release = Hook(workflowId);
            release.ReleaseUrl = url;
            var refuse = Hook(workflowId);
            refuse.RefuseUrl = url;

            (await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(release))).Result.Errors
                .Should().Contain(e => e.ErrorCode == "ReleaseUrlHostBlocked");
            (await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(refuse))).Result.Errors
                .Should().Contain(e => e.ErrorCode == "RefuseUrlHostBlocked");
        }

        [Fact]
        public async Task AnAllowListedInternalHostOrRangeIsAcceptedAtSaveTimeAndNothingElseInternalIsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                [OutboundHostPolicy.AllowedHostsKey] = "hooks.internal",
                [OutboundHostPolicy.AllowedCidrsKey] = "10.20.0.0/16"
            });

            var service = await CaseWorkflowReleaseHookService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp, environment);

            var byHost = Hook(workflowId);
            byHost.ReleaseUrl = "https://hooks.internal/release";
            await SaveAsync(service, byHost);

            var byRange = Hook(workflowId, "Ranged");
            byRange.ReleaseUrl = "http://10.20.4.4/release";
            await SaveAsync(service, byRange);

            var outside = Hook(workflowId, "Outside");
            outside.ReleaseUrl = "http://10.21.4.4/release";

            (await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(outside))).Result.Errors
                .Should().Contain(e => e.ErrorCode == "ReleaseUrlHostBlocked");
        }

        [Fact]
        public async Task TheRefuseUrlFollowsTheSameRulesAndMayBeLeftOutAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var bad = Hook(workflowId);
            bad.RefuseUrl = "https://[@Payload.PaymentId@]/refuse";

            (await ((Func<Task>)(() => service.InsertAsync(bad))).Should().ThrowAsync<DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.ErrorCode == "RefuseUrlInvalid");

            var good = Hook(workflowId);
            good.RefuseUrl = "https://payments.example.com/refuse/[@Payload.PaymentId@]";

            (await SaveAsync(service, good)).RefuseUrl.Should().Contain("/refuse/");
        }

        [Theory]
        [InlineData("Authorization", "", "CredentialVariableNotEmpty")]
        [InlineData("", "JUBE_HOOK_PAYMENTS", "HeaderNameNotEmpty")]
        [InlineData("Authorization", "ConnectionString", "CredentialVariableInvalid")]
        [InlineData("Authorization", "JWTKey", "CredentialVariableInvalid")]
        [InlineData("Host", "JUBE_HOOK_PAYMENTS", "HeaderNameInvalid")]
        [InlineData("Content-Length", "JUBE_HOOK_PAYMENTS", "HeaderNameInvalid")]
        public async Task TheHeaderAndTheCredentialVariableComeTogetherAndNeverNameAnyOtherVariableAsync(
            string header, string secret, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Hook(workflowId);
            model.HeaderName = header;
            model.CredentialVariable = secret;

            var act = () => service.InsertAsync(model);

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == code);
        }

        [Fact]
        public async Task AValidHeaderAndCredentialVariableAreStoredAsNamesOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Hook(workflowId);
            model.HeaderName = "X-Api-Key";
            model.CredentialVariable = "JUBE_HOOK_PAYMENTS";

            var saved = await SaveAsync(service, model);

            saved.HeaderName.Should().Be("X-Api-Key");
            saved.CredentialVariable.Should().Be("JUBE_HOOK_PAYMENTS");
        }

        [Theory]
        [InlineData("SourceSystem", "", "SourceValueNotEmpty")]
        [InlineData("", "Faster", "SourceValueWithoutField")]
        [InlineData("bad;field", "Faster", "SourceFieldInvalid")]
        public async Task TheSourceFieldAndValueComeTogetherAsync(string field, string value, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Hook(workflowId);
            model.SourceField = field;
            model.SourceValue = value;

            var act = () => service.InsertAsync(model);

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == code);
        }

        [Fact]
        public async Task ABodyIsOnlyForPostAndTakesOnlyKnownTokensAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var get = Hook(workflowId);
            get.HttpEndpointTypeId = 2;
            get.Body = "{}";
            (await ((Func<Task>)(() => service.InsertAsync(get))).Should().ThrowAsync<DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.ErrorCode == "BodyOnlyForPost");

            var unknown = Hook(workflowId);
            unknown.Body = "{\"x\":\"[@Environment.JWTKey@]\"}";
            (await ((Func<Task>)(() => service.InsertAsync(unknown))).Should().ThrowAsync<DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.PropertyName == "Body");

            var known = Hook(workflowId);
            known.Body = "{\"why\":\"[@Decision.Reason@]\",\"id\":\"[@Payload.PaymentId@]\"}";
            (await SaveAsync(service, known)).Body.Should().Contain("Decision.Reason");
        }

        [Fact]
        public async Task ValidateReportsTheSameFailuresWithoutSavingAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Hook(workflowId);
            model.ReleaseUrl = "https://[@Payload.PaymentId@]/x";

            var result = await service.ValidateAsync(model);

            result.IsValid.Should().BeFalse();
            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TheCaseWorkflowMustExistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(Hook(int.MaxValue));

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "CaseWorkflowIdInvalid");
        }
    }
}

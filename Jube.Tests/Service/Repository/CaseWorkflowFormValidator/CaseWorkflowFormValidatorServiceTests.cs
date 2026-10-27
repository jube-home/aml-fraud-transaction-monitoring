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
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormValidator;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseWorkflowFormValidator
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFormValidatorServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

        private readonly List<int> formIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> validatorIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowFormValidatorVersion>()
                .Where(w => w.CaseWorkflowFormValidatorId != null && validatorIds.Contains(w.CaseWorkflowFormValidatorId.Value))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowFormValidator>().Where(w => validatorIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => formIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<CaseWorkflowFormValidatorService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseWorkflowFormValidatorService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<(Guid FormGuid, int WorkflowId)> CreateFormAsync(DbContext dbContext, string userName)
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

            var formGuid = Guid.NewGuid();

            formIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
            {
                Name = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..40],
                Guid = formGuid,
                CaseWorkflowId = workflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Html = "<input type=\"number\" name=\"amount\"><textarea name=\"note\"></textarea>",
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            }));

            return (formGuid, workflowId);
        }

        private static CaseWorkflowFormValidatorDto Required(Guid form, string field)
        {
            return new CaseWorkflowFormValidatorDto
            {
                CaseWorkflowFormGuid = form,
                Name = "Rule",
                FieldName = field,
                KindId = (int)CaseWorkflowFormValidatorKind.Required,
                Active = true
            };
        }

        private static CaseWorkflowFormValidatorDto Sql(Guid form, string field, string sql)
        {
            var model = Required(form, field);

            model.KindId = (int)CaseWorkflowFormValidatorKind.Sql;
            model.Sql = sql;

            return model;
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

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(21);
        }

        [Fact]
        public async Task InsertThenListAndGetRoundTripAValidatorUnderItsWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, workflowId) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Required(form, "amount"));
            validatorIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CaseWorkflowId.Should().Be(workflowId);
            saved.CaseWorkflowFormGuid.Should().Be(form);

            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().ContainSingle(v => v.Id == saved.Id);
            (await service.GetByIdAsync(saved.Id))!.FieldName.Should().Be("amount");
        }

        [Fact]
        public async Task InsertRefusesAFieldTheFormTemplateDoesNotDeclareAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(Required(form, "nothere"));

            var failure = (await act.Should().ThrowAsync<DtoValidationException>()).Which;

            failure.Result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be("FieldNotInForm");
        }

        [Fact]
        public async Task InsertRefusesARuleThatDoesNotApplyToTheTypeOfTheFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var model = Required(form, "note");

            model.KindId = (int)CaseWorkflowFormValidatorKind.Range;
            model.Argument1 = "1";

            var act = () => service.InsertAsync(model);

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "KindNotForField");
        }

        [Fact]
        public async Task InsertChecksTheArgumentsOfEachKindAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            async Task<string[]> Codes(Action<CaseWorkflowFormValidatorDto> shape)
            {
                var model = Required(form, "note");

                shape(model);

                var act = () => service.InsertAsync(model);

                return (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                    .Select(e => e.ErrorCode).ToArray();
            }

            (await Codes(m => { m.KindId = 5; m.Argument1 = "(["; })).Should().Contain("PatternInvalid");
            (await Codes(m => { m.KindId = 6; m.Argument1 = "huh"; m.Argument2 = "amount"; })).Should().Contain("Argument1Relation");
            (await Codes(m => { m.KindId = 6; m.Argument1 = "eq"; m.Argument2 = "missing"; })).Should().Contain("OtherFieldNotInForm");
            (await Codes(m => { m.KindId = 4; m.Argument1 = "9"; m.Argument2 = "2"; })).Should().Contain("LengthOrder");
            (await Codes(m => { m.KindId = 2; m.Argument1 = null; m.Argument2 = "x"; })).Should().Contain("Argument1NotEmpty");
            (await Codes(m => { m.KindId = 7; m.Argument1 = "NoSuchList"; })).Should().Contain("ListNotFound");
            (await Codes(m => { m.KindId = 8; m.Argument1 = "NoSuchDictionary"; })).Should().Contain("DictionaryNotFound");
            (await Codes(m => { m.KindId = 99; })).Should().Contain("KindIdInvalid");
        }

        [Fact]
        public async Task InsertOfASqlRuleNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var act = () => service.InsertAsync(Sql(form, "amount", "select @value::numeric > 0"));

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(67);
        }

        [Fact]
        public async Task InsertOfASqlRuleRefusesUnknownParametersAndBlockedTablesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var unknown = () => service.InsertAsync(Sql(form, "amount", "select @nothing is null"));
            var blocked = () => service.InsertAsync(Sql(form, "amount", "select count(*) > 0 from \"UserRegistry\""));
            var write = () => service.InsertAsync(Sql(form, "amount", "delete from \"Case\" returning true"));

            (await unknown.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlParameterUnknown");
            (await blocked.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlRejected");
            (await write.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "SqlRejected");
        }

        [Fact]
        public async Task UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Required(form, "amount"));
            validatorIds.Add(saved.Id);

            saved.Name = "Renamed";

            var updated = await service.UpdateAsync(saved);

            updated.Version.Should().Be(2);
            updated.Name.Should().Be("Renamed");

            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormValidatorVersion>()
                .CountAsync(v => v.CaseWorkflowFormValidatorId == saved.Id)).Should().Be(1);

            var stale = () => service.UpdateAsync(saved);

            await stale.Should().ThrowAsync<VersionConflictException>();
        }

        [Fact]
        public async Task DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, workflowId) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Required(form, "amount"));
            validatorIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormValidator>().FirstAsync(v => v.Id == saved.Id))
                .Deleted.Should().Be(1);

            var again = () => service.DeleteAsync(saved.Id);

            await again.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeletingASqlRuleNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await author.InsertAsync(Sql(form, "amount", "select @value::numeric > 0"));
            validatorIds.Add(saved.Id);

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var act = () => other.DeleteAsync(saved.Id);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ATenantCannotSeeOrChangeAnotherTenantsValidatorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, workflowId) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(Required(form, "amount"));
            validatorIds.Add(saved.Id);

            var stranger = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await stranger.GetByIdAsync(saved.Id)).Should().BeNull();
            (await stranger.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();

            var update = () => stranger.UpdateAsync(saved);
            var insert = () => stranger.InsertAsync(Required(form, "amount"));

            await update.Should().ThrowAsync<NotFoundException>();
            (await insert.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "CaseWorkflowFormGuidNotFound");
        }

        [Fact]
        public async Task TestRunsASavedSqlRuleAgainstTheRealDatabaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Sql(form, "amount", "select @value::numeric > 100"));
            validatorIds.Add(saved.Id);

            var passed = await service.TestAsync(new CaseWorkflowFormValidatorTestDto
            {
                Id = saved.Id, Values = new Dictionary<string, string?> { ["amount"] = "250" }
            });

            var failed = await service.TestAsync(new CaseWorkflowFormValidatorTestDto
            {
                Id = saved.Id, Values = new Dictionary<string, string?> { ["amount"] = "5" }
            });

            passed.Passed.Should().BeTrue();
            failed.Passed.Should().BeFalse();
            failed.Code.Should().Be("sql");
            failed.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task TestReportsAFailureOfTheSqlItselfWithoutRaisingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, _) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(Sql(form, "amount", "select @value::numeric > 100"));
            validatorIds.Add(saved.Id);

            var result = await service.TestAsync(new CaseWorkflowFormValidatorTestDto
            {
                Id = saved.Id, Values = new Dictionary<string, string?> { ["amount"] = "not a number" }
            });

            result.Passed.Should().BeFalse();
            result.Code.Should().Be("sqlFailed");
        }

        [Fact]
        public async Task ValidateReportsFailuresWithoutStoringAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (form, workflowId) = await CreateFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ValidateAsync(Required(form, "nothere"));

            result.IsValid.Should().BeFalse();
            (await service.GetByCaseWorkflowIdAsync(workflowId)).Should().BeEmpty();
        }
    }
}

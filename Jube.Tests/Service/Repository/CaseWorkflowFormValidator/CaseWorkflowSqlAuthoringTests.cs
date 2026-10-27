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
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowForm;
using Jube.Dto.Repository.CaseWorkflowMacro;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowForm;
using Jube.Service.Repository.CaseWorkflowMacro;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowFormValidator
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowSqlAuthoringTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> formIds = [];
        private readonly List<int> macroIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowFormVersion>()
                .Where(w => w.CaseWorkflowFormId != null && formIds.Contains(w.CaseWorkflowFormId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowMacroVersion>()
                .Where(w => w.CaseWorkflowMacroId != null && macroIds.Contains(w.CaseWorkflowMacroId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowMacro.Where(w => workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowFormService> FormsAsync(DbContext dbContext, string user)
        {
            return CaseWorkflowFormService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static Task<CaseWorkflowMacroService> MacrosAsync(DbContext dbContext, string user)
        {
            return CaseWorkflowMacroService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private async Task<int> CreateWorkflowAsync(DbContext dbContext, string userName)
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

            return workflowId;
        }

        private static CaseWorkflowFormDto Form(int workflowId, string? sql)
        {
            return new CaseWorkflowFormDto
            {
                CaseWorkflowId = workflowId,
                Name = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..30],
                Html = "<input name=\"a\">",
                Active = true,
                Sql = sql
            };
        }

        private static CaseWorkflowMacroDto Macro(int workflowId, string? sql)
        {
            return new CaseWorkflowMacroDto
            {
                CaseWorkflowId = workflowId,
                Name = $"{DatabaseFixture.Prefix}Macro{Guid.NewGuid():N}"[..30],
                Javascript = "x",
                Active = true,
                Sql = sql
            };
        }

        [Fact]
        public async Task AFormWithoutSqlNeedsNoSqlPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var service = await FormsAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var saved = await service.InsertAsync(Form(workflowId, null));
            formIds.Add(saved.Id);

            saved.Sql.Should().BeNull();
        }

        [Fact]
        public async Task InsertingFormSqlNeedsTheSqlAuthoringPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var plain = await FormsAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var denied = () => plain.InsertAsync(Form(workflowId, "select 1 as n"));

            (await denied.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowForm.ForbiddenException>())
                .Which.RequiredSpecifications.Should().Equal(67);

            var author = await FormsAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await author.InsertAsync(Form(workflowId, "select 1 as n"));
            formIds.Add(saved.Id);

            saved.Sql.Should().Be("select 1 as n");
        }

        [Fact]
        public async Task FormSqlThatReadsABlockedTableOrAnUnknownParameterIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await FormsAsync(dbContext, fx.Seed.UserWithPermission);

            var blocked = () => author.InsertAsync(Form(workflowId, "select count(*) from \"UserRegistry\""));
            var unknown = () => author.InsertAsync(Form(workflowId, "select @nothing"));
            var write = () => author.InsertAsync(Form(workflowId, "delete from \"Case\" returning 1"));

            (await blocked.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowForm.DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.PropertyName == "Sql" && e.ErrorCode == "SqlRejected");
            (await unknown.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowForm.DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.ErrorCode == "SqlParameterUnknown");
            (await write.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowForm.DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.ErrorCode == "SqlRejected");
        }

        [Fact]
        public async Task UpdatingAFormMayLeaveItsSqlAloneButNeedsThePermissionToChangeItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermission);
            var author = await FormsAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await author.InsertAsync(Form(workflowId, "select 1 as n"));
            formIds.Add(saved.Id);

            var editor = await FormsAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            saved.Name = "Renamed form";

            var renamed = await editor.UpdateAsync(saved);

            renamed.Sql.Should().Be("select 1 as n");

            renamed.Sql = "select 2 as n";

            var changed = () => editor.UpdateAsync(renamed);

            await changed.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowForm.ForbiddenException>();
        }

        [Fact]
        public async Task AMacroNeedsTheSqlAuthoringPermissionToHaveSqlAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflowId = await CreateWorkflowAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            var plain = await MacrosAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);

            var denied = () => plain.InsertAsync(Macro(workflowId, "select 1 as n"));

            await denied.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseWorkflowMacro.ForbiddenException>();

            var author = await MacrosAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await author.InsertAsync(Macro(workflowId, "select 1 as n"));
            macroIds.Add(saved.Id);

            saved.Sql.Should().Be("select 1 as n");
        }
    }
}

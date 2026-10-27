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
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Exceptions.Query.EntityVersionHistory;
using Jube.Service.Query.CaseWorkflowReleaseHookVersionHistory;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowReleaseHook;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.CaseWorkflowReleaseHookVersionHistory
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowReleaseHookVersionHistoryServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

        private static Task<CaseWorkflowReleaseHookVersionHistoryService> HistoryAsync(DbContext dbContext,
            string? user) =>
            CaseWorkflowReleaseHookVersionHistoryService.CreateAsync(dbContext, user, TestLog.NoOp, localizers);

        private async Task<CaseWorkflowReleaseHookDto> SeedAsync(DbContext dbContext, string user)
        {
            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, user).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });
            modelIds.Add(model.Id);

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = 0, CreatedUser = user,
                CreatedDate = DateTime.UtcNow
            });
            workflowIds.Add(workflowId);

            var service = await CaseWorkflowReleaseHookService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp, TestDynamicEnvironment.Create());

            var saved = await service.InsertAsync(new CaseWorkflowReleaseHookDto
            {
                CaseWorkflowId = workflowId, Name = "Faster", Active = true, HttpEndpointTypeId = 1,
                ReleaseUrl = "https://payments.example.com/v1/release"
            });
            hookIds.Add(saved.Id);

            saved.ReleaseUrl = "https://payments.example.com/v2/release";
            saved = await service.UpdateAsync(saved);
            saved.ReleaseUrl = "https://payments.example.com/v3/release";

            return await service.UpdateAsync(saved);
        }

        [Fact]
        public async Task EveryUpdateLeavesTheEarlierStateAsAVersionOldestFirstAndTheLatestIsTheLastAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hook = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var history = await HistoryAsync(dbContext, fx.Seed.UserWithPermission);

            var versions = await history.GetByParentIdAsync(hook.Id);

            versions.Select(s => s.ReleaseUrl).Should().BeEquivalentTo(
                ["https://payments.example.com/v1/release", "https://payments.example.com/v2/release"]);

            (await history.GetLatestAsync(hook.Id))!.ReleaseUrl.Should().Be("https://payments.example.com/v2/release");
            (await history.GetByIdAsync(versions[0].Id))!.CaseWorkflowReleaseHookId.Should().Be(hook.Id);
        }

        [Fact]
        public async Task CompareReportsTheFieldsThatChangedBetweenTwoVersionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hook = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var history = await HistoryAsync(dbContext, fx.Seed.UserWithPermission);
            var versions = (await history.GetByParentIdAsync(hook.Id)).OrderBy(o => o.Version).ToList();

            var changes = await history.CompareAsync(versions[0].Id, versions[1].Id);

            changes.Should().Contain(c => c.PropertyName == nameof(Data.Poco.CaseWorkflowReleaseHookVersion.ReleaseUrl));
        }

        [Fact]
        public async Task WithoutPermission69TheHistoryIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hook = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var history = await HistoryAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => history.GetByParentIdAsync(hook.Id);

            (await act.Should().ThrowAsync<ForbiddenException>()).Which.RequiredSpecifications.Should().Equal(69);
        }

        [Fact]
        public async Task AnotherTenantSeesNoVersionsAndCannotFetchOneByIdOrCompareAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hook = await SeedAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await HistoryAsync(dbContext, fx.Seed.UserWithPermission);
            var versions = await owner.GetByParentIdAsync(hook.Id);

            var other = await HistoryAsync(dbContext, fx.Seed.UserTenantB);

            (await other.GetByParentIdAsync(hook.Id)).Should().BeEmpty();
            (await other.GetLatestAsync(hook.Id)).Should().BeNull();
            (await other.GetByIdAsync(versions[0].Id)).Should().BeNull();
            (await other.GetByDateRangeAsync(hook.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)))
                .Should().BeEmpty();
        }

        [Fact]
        public async Task ABlankUserIsNotAuthenticatedAsync()
        {
            var act = async () =>
            {
                await using var dbContext = fx.GetDbContext();

                await HistoryAsync(dbContext, " ");
            };

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }
    }
}

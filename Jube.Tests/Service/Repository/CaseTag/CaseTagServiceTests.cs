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
using Jube.Data.Poco;
using Jube.Dto.Repository.CaseTag;
using Jube.Service.Exceptions.Repository.CaseTag;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseTag;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseTag
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseTagServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<Data.Poco.CaseTag>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseTagService> BuildAsync(DbContext dbContext, string? user) =>
            CaseTagService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        [Fact]
        public async Task MergingTagsIsRefusedOnACaseLockedToAnotherUserAndWritesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Me);
            var held = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var mine = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var free = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            await dbContext.Case.Where(w => w.Id == held).Set(s => s.Locked, (byte?)1)
                .Set(s => s.LockedUser, fx.Seed.UserWithoutPermission).UpdateAsync();
            await dbContext.Case.Where(w => w.Id == mine).Set(s => s.Locked, (byte?)1)
                .Set(s => s.LockedUser, Me).UpdateAsync();

            var service = await BuildAsync(dbContext, Me);

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.MergeAsync(new CaseTagMergeRequestDto { CaseId = held, Tags = [] }));

            (await service.MergeAsync(new CaseTagMergeRequestDto { CaseId = mine, Tags = [] })).Should().BeEmpty();
            (await service.MergeAsync(new CaseTagMergeRequestDto { CaseId = free, Tags = [] })).Should().BeEmpty();

            (await dbContext.GetTable<Data.Poco.CaseTag>().CountAsync(w => w.CaseId == held)).Should().Be(0);
        }
    }
}

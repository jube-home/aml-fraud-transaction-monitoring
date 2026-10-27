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
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseViewRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> caseIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}View{Guid.NewGuid():N}",
                Diary = 0,
                Rating = 0,
                LastClosedStatus = 0,
                Json = "{}"
            });
            caseIds.Add(id);

            return id;
        }

        [Fact]
        public async Task TheFirstViewInsertsAndLaterViewsReplaceTheSameRowAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseViewRepository(dbContext, fx.Seed.UserWithPermission);
            var before = DateTime.UtcNow.AddSeconds(-5);

            (await repository.GetAsync(caseId)).Should().BeNull();

            await repository.UpsertAsync(caseId, 10, 1);
            var first = await repository.GetAsync(caseId);
            first.LastViewedCaseEventId.Should().Be(10);
            first.LastViewedRiskSequence.Should().Be(1);
            first.LastViewedDate.Should().BeAfter(before);

            await repository.UpsertAsync(caseId, 25, null);
            var second = await repository.GetAsync(caseId);
            second.Id.Should().Be(first.Id);
            second.LastViewedCaseEventId.Should().Be(25);
            second.LastViewedRiskSequence.Should().BeNull();
            second.LastViewedDate.Should().BeOnOrAfter(first.LastViewedDate);

            (await dbContext.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
        }

        [Fact]
        public async Task EachUserHasTheirOwnViewOfACaseAndNeverSeesAnothersAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var mine = new CaseViewRepository(dbContext, fx.Seed.UserWithPermission);
            var theirs = new CaseViewRepository(dbContext, fx.Seed.UserWithPermissionNoApproval);

            await mine.UpsertAsync(caseId, 5, 2);

            (await theirs.GetAsync(caseId)).Should().BeNull();
            (await theirs.GetForCasesAsync([caseId])).Should().BeEmpty();

            await theirs.UpsertAsync(caseId, 9, 3);

            (await mine.GetAsync(caseId)).LastViewedCaseEventId.Should().Be(5);
            (await theirs.GetAsync(caseId)).LastViewedCaseEventId.Should().Be(9);
            (await dbContext.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(2);
        }

        [Fact]
        public async Task ViewsForManyCasesComeBackKeyedByCaseAndOnlyForTheCallerAsync()
        {
            var seen = await SeedCaseAsync();
            var unseen = await SeedCaseAsync();
            var seenByOther = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var mine = new CaseViewRepository(dbContext, fx.Seed.UserWithPermission);
            var theirs = new CaseViewRepository(dbContext, fx.Seed.UserWithPermissionNoApproval);

            await mine.UpsertAsync(seen, 1, 1);
            await theirs.UpsertAsync(seenByOther, 1, 1);

            var views = await mine.GetForCasesAsync([seen, unseen, seenByOther]);

            views.Keys.Should().BeEquivalentTo([seen]);
        }

        [Fact]
        public async Task ConcurrentViewsOfTheSameCaseByTheSameUserLeaveOneRowAsync()
        {
            var caseId = await SeedCaseAsync();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
            {
                await using var dbContext = fx.GetDbContext();
                await new CaseViewRepository(dbContext, fx.Seed.UserWithPermission).UpsertAsync(caseId, i, i);
            }));

            await using var check = fx.GetDbContext();
            (await check.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
        }

        [Fact]
        public async Task ViewsAreRemovedWithTheirCaseAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            await new CaseViewRepository(dbContext, fx.Seed.UserWithPermission).UpsertAsync(caseId, 1, 1);

            await dbContext.Case.Where(w => w.Id == caseId).DeleteAsync();

            (await dbContext.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }
    }
}

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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserNotificationRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly string marker = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith(marker)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private UserNotification Row(int tenant, string user, string key, int? caseId = null, DateTime? at = null)
        {
            return new UserNotification
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenant,
                RecipientUser = user,
                KindId = 1,
                PriorityId = 2,
                CaseId = caseId,
                DedupeKey = $"{marker}:{key}",
                CreatedDate = at ?? DateTime.UtcNow
            };
        }

        [Fact]
        public async Task InsertIfNewIsIdempotentOnTheDedupeKeyForOneRecipientAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new UserNotificationRepository(dbContext);

            (await repository.InsertIfNewAsync(Row(1, "alice", "a"))).Should().BeTrue();
            (await repository.InsertIfNewAsync(Row(1, "alice", "a"))).Should().BeFalse();
            (await repository.InsertIfNewAsync(Row(1, "bob", "a"))).Should().BeTrue("the key is per recipient");
        }

        [Fact]
        public async Task AResolvedNotificationFreesItsDedupeKeyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new UserNotificationRepository(dbContext);
            var caseId = 987654;

            (await repository.InsertIfNewAsync(Row(1, "alice", "r", caseId))).Should().BeTrue();
            await repository.ResolveForCaseAsync(caseId);

            (await repository.ListActiveAsync(1, "alice", 50)).Where(n => n.CaseId == caseId).Should().BeEmpty();
            (await repository.InsertIfNewAsync(Row(1, "alice", "r", caseId))).Should().BeTrue();
        }

        [Fact]
        public async Task ListActiveReturnsOnlyTheCallersRowsInTheirTenantNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new UserNotificationRepository(dbContext);

            await repository.InsertIfNewAsync(Row(1, "alice", "old", at: DateTime.UtcNow.AddMinutes(-10)));
            await repository.InsertIfNewAsync(Row(1, "alice", "new", at: DateTime.UtcNow));
            await repository.InsertIfNewAsync(Row(1, "bob", "other"));
            await repository.InsertIfNewAsync(Row(2, "alice", "othertenant"));

            var listed = (await repository.ListActiveAsync(1, "alice", 50))
                .Where(n => n.DedupeKey.StartsWith(marker)).ToList();

            listed.Select(n => n.DedupeKey).Should().Equal($"{marker}:new", $"{marker}:old");
        }

        [Fact]
        public async Task MarkReadOnlyAffectsTheCallersOwnRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new UserNotificationRepository(dbContext);
            var mine = Row(1, "alice", "m");
            var theirs = Row(1, "bob", "t");
            await repository.InsertIfNewAsync(mine);
            await repository.InsertIfNewAsync(theirs);

            (await repository.MarkReadAsync(1, "alice", theirs.Guid)).Should().Be(0, "another user's row is untouchable");
            (await repository.MarkReadAsync(2, "alice", mine.Guid)).Should().Be(0, "another tenant's row is untouchable");
            (await repository.MarkReadAsync(1, "alice", mine.Guid)).Should().Be(1);

            (await dbContext.UserNotification.SingleAsync(w => w.Guid == mine.Guid)).ReadDate.Should().NotBeNull();
            (await dbContext.UserNotification.SingleAsync(w => w.Guid == theirs.Guid)).ReadDate.Should().BeNull();
        }

        [Fact]
        public async Task MarkAllReadMarksOnlyTheCallersUnreadRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new UserNotificationRepository(dbContext);
            await repository.InsertIfNewAsync(Row(1, "alice", "1"));
            await repository.InsertIfNewAsync(Row(1, "alice", "2"));
            var bobs = Row(1, "bob", "3");
            await repository.InsertIfNewAsync(bobs);

            await repository.MarkAllReadAsync(1, "alice");

            (await dbContext.UserNotification.Where(w => w.RecipientUser == "alice" && w.DedupeKey.StartsWith(marker))
                .AllAsync(w => w.ReadDate != null)).Should().BeTrue();
            (await dbContext.UserNotification.SingleAsync(w => w.Guid == bobs.Guid)).ReadDate.Should().BeNull();
        }

        [Fact]
        public async Task VisibleIdsAreLimitedToCasesTheUserHoldsTheWorkflowAndStatusRolesForAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var granted = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, granted);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var forGranted = await new CaseRepository(dbContext, granted)
                .GetVisibleIdsActiveOnlyAsync([caseId, int.MaxValue]);
            var forOther = await new CaseRepository(dbContext, fx.Seed.UserTenantB)
                .GetVisibleIdsActiveOnlyAsync([caseId]);

            forGranted.Should().Equal(caseId);
            forOther.Should().BeEmpty();
        }
    }
}

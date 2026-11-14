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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseChecklist;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistHistoryTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private const string Unconditional = "Review the triggering transactions";

        [Fact]
        public async Task TheHistoryListsTheAuditNewestFirstWithItemNamesAndActors()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var clock = new ManualUtcTimeProvider(Start);
            var service = await ChecklistAsync(dbContext, Users.Holder, clock);
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, Unconditional);

            clock.Advance(TimeSpan.FromMinutes(5));
            await service.CheckAsync(caseId, itemId);
            await service.UncheckAsync(caseId, itemId);

            var history = await service.GetHistoryAsync(caseId);

            history.Should().NotBeEmpty();
            history.Select(h => h.Id).Should().BeInDescendingOrder();
            history.First().ActionId.Should().Be((int)CaseChecklistAction.Unchecked);
            history.First().ItemName.Should().Be(Unconditional);
            history.First().Actor.Should().Be(Users.Holder);

            var shown = history.Where(h => h.ActionId == (int)CaseChecklistAction.Shown).ToList();
            shown.Should().NotBeEmpty();
            shown.Should().OnlyContain(h => h.Actor == CaseChecklistService.SystemActor);
        }

        [Fact]
        public async Task TheHistoryIncludesGateEntriesThatAreNotAboutOneItem()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var holder = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await holder.GetAsync(caseId);
            var admin = await ChecklistAsync(dbContext, Administrator, new ManualUtcTimeProvider(Start));

            await admin.OverrideAsync(caseId, new CaseChecklistOverrideRequestDto { Reason = "Verified by phone." });

            var entry = (await admin.GetHistoryAsync(caseId))
                .First(h => h.ActionId == (int)CaseChecklistAction.GateOverridden);

            entry.ItemName.Should().BeNull();
            entry.Actor.Should().Be(Administrator);
            entry.Detail.Should().Contain("Verified by phone.");
        }

        [Fact]
        public async Task TheSnapshotCarriesTheOpenOverrideUntilTheStatusChanges()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var holder = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            var admin = await ChecklistAsync(dbContext, Administrator, new ManualUtcTimeProvider(Start));

            (await holder.GetAsync(caseId)).Override.Should().BeNull();

            await admin.OverrideAsync(caseId, new CaseChecklistOverrideRequestDto { Reason = "Verified by phone." });

            var snapshot = await holder.GetAsync(caseId);

            snapshot.Override.Should().NotBeNull();
            snapshot.Override!.GrantedUser.Should().Be(Administrator);
            snapshot.Override.Reason.Should().Be("Verified by phone.");
            snapshot.Override.OpenItems.Should().Contain(Unconditional);
        }

        [Fact]
        public async Task TheHistoryIsCappedAtTwoHundredEntries()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);

            for (var i = 0; i < 205; i++)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseChecklistEvent
                {
                    CaseId = caseId,
                    ActionId = (byte)CaseChecklistAction.GateBlocked,
                    Actor = Users.Holder,
                    CreatedDate = Start.AddSeconds(i),
                    Detail = "Refused."
                });
            }

            (await service.GetHistoryAsync(caseId)).Should().HaveCount(200);
        }

        [Fact]
        public async Task AnotherTenantAnUnknownCaseAndAnAnonymousCallerCannotReadTheHistory()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var stranger = await ChecklistAsync(dbContext, Fx.Seed.UserTenantB, new ManualUtcTimeProvider(Start));
            var admin = await ChecklistAsync(dbContext, Administrator, new ManualUtcTimeProvider(Start));

            await FluentActions.Awaiting(() => stranger.GetHistoryAsync(caseId)).Should()
                .ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => admin.GetHistoryAsync(int.MaxValue)).Should()
                .ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => ChecklistAsync(dbContext, string.Empty,
                new ManualUtcTimeProvider(Start))).Should().ThrowAsync<NotAuthenticatedException>();
        }
    }
}

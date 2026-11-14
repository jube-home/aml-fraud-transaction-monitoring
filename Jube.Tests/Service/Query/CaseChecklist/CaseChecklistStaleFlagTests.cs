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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistStaleFlagTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private static CaseEvent EventOf(int caseId, int typeId)
        {
            return new CaseEvent
            {
                CaseId = caseId,
                CaseEventTypeId = typeId,
                CaseKey = "AccountId",
                CaseKeyValue = "n/a",
                CreatedUser = Administrator,
                CreatedDate = DateTime.UtcNow
            };
        }

        private static Task<DateTime?> StaleAsync(Jube.Data.Context.DbContext dbContext, int caseId)
        {
            return dbContext.Case.Where(w => w.Id == caseId).Select(s => s.ChecklistStaleDate).FirstAsync();
        }

        [Fact]
        public async Task ADecideEventMarksTheCaseStale()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            (await StaleAsync(dbContext, caseId)).Should().BeNull();

            var before = DateTime.UtcNow.AddSeconds(-1);
            await new CaseEventRepository(dbContext, Administrator).InsertAsync(EventOf(caseId, 3));

            (await StaleAsync(dbContext, caseId)).Should().BeOnOrAfter(before);
        }

        [Fact]
        public async Task EveryChecklistEventTypeLeavesTheCaseNotStale()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var repository = new CaseEventRepository(dbContext, Administrator);

            foreach (var typeId in Enumerable.Range(21, 8))
            {
                await repository.InsertAsync(EventOf(caseId, typeId));
            }

            await repository.BulkInsertAsync(Enumerable.Range(21, 8).Select(t => EventOf(caseId, t)).ToList());

            (await StaleAsync(dbContext, caseId)).Should().BeNull();
        }

        [Fact]
        public async Task TheNeighbouringEventTypesStillMarkTheCaseStale()
        {
            await using var dbContext = Fx.GetDbContext();
            var below = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var above = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var repository = new CaseEventRepository(dbContext, Administrator);

            await repository.InsertAsync(EventOf(below, 20));
            await repository.InsertAsync(EventOf(above, 29));

            (await StaleAsync(dbContext, below)).Should().NotBeNull();
            (await StaleAsync(dbContext, above)).Should().NotBeNull();
        }

        [Fact]
        public async Task ABulkInsertAcrossTwoCasesMarksBothAndNoOtherCase()
        {
            await using var dbContext = Fx.GetDbContext();
            var first = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var second = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var untouched = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);

            await new CaseEventRepository(dbContext, Administrator).BulkInsertAsync(new List<CaseEvent>
            {
                EventOf(first, 6), EventOf(first, 11), EventOf(second, 10), EventOf(second, 23)
            });

            (await StaleAsync(dbContext, first)).Should().NotBeNull();
            (await StaleAsync(dbContext, second)).Should().NotBeNull();
            (await StaleAsync(dbContext, untouched)).Should().BeNull();
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == first || w.CaseId == second)).Should().Be(4);
        }

        [Fact]
        public async Task ABatchOfOnlyChecklistEventsMarksNothingEvenAcrossCases()
        {
            await using var dbContext = Fx.GetDbContext();
            var first = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var second = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);

            await new CaseEventRepository(dbContext, Administrator).BulkInsertAsync(new List<CaseEvent>
            {
                EventOf(first, 21), EventOf(second, 28)
            });

            (await StaleAsync(dbContext, first)).Should().BeNull();
            (await StaleAsync(dbContext, second)).Should().BeNull();
        }

        [Fact]
        public async Task AnEventWithNoCaseIsStoredAndMarksNothing()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var orphan = EventOf(caseId, 3);
            orphan.CaseId = null;

            var stored = await new CaseEventRepository(dbContext, Administrator).InsertAsync(orphan);

            stored.Id.Should().BeGreaterThan(0);
            (await StaleAsync(dbContext, caseId)).Should().BeNull();
            await dbContext.CaseEvent.Where(w => w.Id == stored.Id).DeleteAsync();
        }

        [Fact]
        public async Task ReconcilingWritesItsOwnTimelineEventsWithoutLeavingTheCaseStale()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100);
            var service = await ChecklistAsync(dbContext, Users.Holder, TimeProvider.System);

            await service.GetAsync(caseId);

            (await TimelineCountAsync(dbContext, caseId, Jube.Service.Query.CaseChecklist.CaseChecklistService.ShownEventTypeId)).Should().BeGreaterThan(0);
            (await StaleAsync(dbContext, caseId)).Should().BeNull();
        }

        [Fact]
        public async Task ADecideActionThroughTheCaseServiceMarksTheCaseStale()
        {
            await using var dbContext = Fx.GetDbContext();
            var caseId = await CreateCaseAsync(dbContext, FirstLine, UniqueKey(), 100, Users.Holder);
            var current = await StatusGuidAsync(dbContext, FirstLine);
            var cases = await CasesAsync(dbContext, Users.Holder);
            var move = Move(caseId, current, 0, Users.Holder);
            move.Rating = 3;

            await cases.UpdateAsync(move);

            (await StaleAsync(dbContext, caseId)).Should().NotBeNull();
        }
    }
}

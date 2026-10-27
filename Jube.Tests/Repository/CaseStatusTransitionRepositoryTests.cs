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
using Jube.Dto.Options;
using Jube.Migrations.Branches.CasesPage;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseStatusTransitionRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime start = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        private readonly List<int> caseIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseEvent>()
                .Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedCaseAsync(Guid workflow, Guid status, byte closedStatus = 0,
            DateTime? closedDate = null)
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = status,
                CreatedDate = start,
                Locked = 0,
                ClosedStatusId = closedStatus,
                ClosedDate = closedDate,
                ClosedUser = closedDate.HasValue ? "closer" : null,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}Tr{Guid.NewGuid():N}",
                Diary = 0,
                Rating = 0,
                LastClosedStatus = 0,
                Json = "{}"
            });
            caseIds.Add(id);

            return id;
        }

        [Fact]
        public async Task EnteringAStatusOpensAStayAndMovingOnClosesItWithTheTimeSpentAsync()
        {
            var workflow = Guid.NewGuid();
            var one = Guid.NewGuid();
            var two = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, one);
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseStatusTransitionRepository(dbContext);

            await repository.EnterAsync(caseId, workflow, one, CaseTransitionEnterReason.Created, null, start);
            await repository.EnterAsync(caseId, workflow, two, CaseTransitionEnterReason.StatusChanged, "alice",
                start.AddSeconds(90));

            var stays = await repository.ListForCaseAsync(caseId);
            stays.Should().HaveCount(2);
            stays[0].DurationSeconds.Should().Be(90);
            stays[0].ExitReasonId.Should().Be((byte)CaseTransitionExitReason.StatusChanged);
            stays[0].ExitedUser.Should().Be("alice");
            stays[0].EnteredUser.Should().BeNull();
            stays[1].PreviousCaseWorkflowStatusGuid.Should().Be(one);
            stays[1].ExitedDate.Should().BeNull();
            stays[1].DurationSeconds.Should().BeNull();
        }

        [Fact]
        public async Task EnteringTheStatusTheCaseIsAlreadyInChangesNothingAsync()
        {
            var workflow = Guid.NewGuid();
            var status = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, status);
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseStatusTransitionRepository(dbContext);

            await repository.EnterAsync(caseId, workflow, status, CaseTransitionEnterReason.Created, null, start);
            await repository.EnterAsync(caseId, workflow, status, CaseTransitionEnterReason.StatusChanged, "alice",
                start.AddMinutes(5));

            (await repository.ListForCaseAsync(caseId)).Should().ContainSingle();
        }

        [Fact]
        public async Task ClosingEndsTheOpenStayAndClosingACaseWithNoOpenStayIsHarmlessAsync()
        {
            var workflow = Guid.NewGuid();
            var status = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, status);
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseStatusTransitionRepository(dbContext);

            await repository.CloseAsync(caseId, start, "x");
            (await repository.ListForCaseAsync(caseId)).Should().BeEmpty();

            await repository.EnterAsync(caseId, workflow, status, CaseTransitionEnterReason.Created, null, start);
            await repository.CloseAsync(caseId, start.AddHours(2), "bob");
            await repository.CloseAsync(caseId, start.AddHours(3), "bob");

            var stay = (await repository.ListForCaseAsync(caseId)).Single();
            stay.ExitReasonId.Should().Be((byte)CaseTransitionExitReason.Closed);
            stay.DurationSeconds.Should().Be(7200);
            stay.ExitedUser.Should().Be("bob");
        }

        [Fact]
        public async Task ConcurrentEntriesIntoTheSameStatusLeaveExactlyOneOpenStayAsync()
        {
            var workflow = Guid.NewGuid();
            var status = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, status);

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var dbContext = fx.GetDbContext();
                await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow, status,
                    CaseTransitionEnterReason.StatusChanged, "u", start);
            }));

            await using var check = fx.GetDbContext();
            (await check.CaseStatusTransition.CountAsync(w => w.CaseId == caseId && w.ExitedDate == null))
                .Should().Be(1);
        }

        [Fact]
        public async Task TheDatabaseRefusesASecondOpenStayForACaseAsync()
        {
            var workflow = Guid.NewGuid();
            var status = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, status);
            await using var dbContext = fx.GetDbContext();
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow, status,
                CaseTransitionEnterReason.Created, null, start);

            Exception? thrown = null;

            try
            {
                await dbContext.InsertAsync(new CaseStatusTransition
                {
                    CaseId = caseId,
                    CaseWorkflowGuid = workflow,
                    CaseWorkflowStatusGuid = Guid.NewGuid(),
                    EnterReasonId = 2,
                    EnteredDate = start
                });
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            thrown.Should().NotBeNull();
            thrown!.ToString().Should().Contain("23505");
        }

        [Fact]
        public async Task TheBackfillRebuildsStaysFromTheCaseEventsOfAnExistingClosedCaseAsync()
        {
            var workflow = Guid.NewGuid();
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            var c = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, c, 3, start.AddHours(5));
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(new CaseEvent
            {
                CaseEventTypeId = 9, CaseId = caseId, CaseKey = "AccountId", CaseKeyValue = "k",
                Before = a.ToString(), After = b.ToString(), CreatedDate = start.AddHours(1), CreatedUser = "alice"
            });
            await dbContext.InsertAsync(new CaseEvent
            {
                CaseEventTypeId = 9, CaseId = caseId, CaseKey = "AccountId", CaseKeyValue = "k",
                Before = b.ToString(), After = c.ToString(), CreatedDate = start.AddHours(3), CreatedUser = "bob"
            });
            await dbContext.InsertAsync(new CaseEvent
            {
                CaseEventTypeId = 9, CaseId = caseId, CaseKey = "AccountId", CaseKeyValue = "k",
                Before = "not a guid", After = "also not", CreatedDate = start.AddHours(4), CreatedUser = "x"
            });

            await dbContext.ExecuteAsync(CreateCaseStatusTransitionTable.BackfillSql($"c.\"Id\" = {caseId}"));

            var stays = await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(caseId);

            stays.Select(s => s.CaseWorkflowStatusGuid).Should().Equal(a, b, c);
            stays.Select(s => s.DurationSeconds).Should().Equal(3600, 7200, 7200);
            stays.Select(s => s.EnterReasonId).Should().Equal(1, 2, 2);
            stays.Select(s => s.EnteredUser).Should().Equal(null, "alice", "bob");
            stays.Select(s => s.ExitReasonId).Should().Equal(1, 1, 2);
            stays[2].ExitedUser.Should().Be("closer");
            stays[1].PreviousCaseWorkflowStatusGuid.Should().Be(a);

            await dbContext.ExecuteAsync(CreateCaseStatusTransitionTable.BackfillSql($"c.\"Id\" = {caseId}"));
            (await dbContext.CaseStatusTransition.CountAsync(w => w.CaseId == caseId)).Should().Be(3);
        }

        [Fact]
        public async Task TheBackfillOfAnOpenCaseWithNoMovesLeavesOneOpenStayInItsCurrentStatusAsync()
        {
            var workflow = Guid.NewGuid();
            var status = Guid.NewGuid();
            var caseId = await SeedCaseAsync(workflow, status);
            await using var dbContext = fx.GetDbContext();

            await dbContext.ExecuteAsync(CreateCaseStatusTransitionTable.BackfillSql($"c.\"Id\" = {caseId}"));

            var stay = (await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(caseId)).Single();
            stay.CaseWorkflowStatusGuid.Should().Be(status);
            stay.EnteredDate.Should().Be(start);
            stay.ExitedDate.Should().BeNull();
            stay.EnterReasonId.Should().Be((byte)CaseTransitionEnterReason.Created);
        }
    }
}

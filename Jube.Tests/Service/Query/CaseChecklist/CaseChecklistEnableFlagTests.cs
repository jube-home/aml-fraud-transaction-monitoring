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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistEnableFlagTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private const string Restricted = "Restricted";
        private const string NoteExists = "select exists (select 1 from \"CaseNote\" n where n.\"CaseId\" = @Id)";

        private static (CaseChecklistReconciler Reconciler, CountingSqlRunner Runner) Reconciler(
            DbContext dbContext)
        {
            var runner = new CountingSqlRunner(new FormSqlRunner(Connection(), Administrator, TestLog.NoOp));

            return (new CaseChecklistReconciler(dbContext, runner, new ManualUtcTimeProvider(Start), TestLog.NoOp),
                runner);
        }

        [Fact]
        public async Task ASwitchedOffShowConditionShowsTheItemWithoutRunningItsSql()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var own = new CaseChecklistOwnItems(dbContext);
            var itemGuid = await own.AddAsync(await StatusGuidAsync(dbContext, Restricted), "FlagShowOff", false,
                "select false", false, null);
            var caseId = await CreateCaseAsync(dbContext, Restricted, UniqueKey(), 100);
            var (reconciler, runner) = Reconciler(dbContext);

            var changes = await reconciler.ReconcileAsync(caseId, "System", "Test");

            changes.Should().Be(1);
            runner.Calls.Should().Be(0);

            var row = await dbContext.CaseChecklistItem.SingleAsync(w => w.CaseId == caseId);

            row.CaseWorkflowChecklistItemGuid.Should().Be(itemGuid);
            row.StateId.Should().Be((byte)CaseChecklistItemState.Shown);
        }

        [Fact]
        public async Task ASwitchedOnShowConditionRunsItsSqlAndKeepsAFailingItemOff()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var own = new CaseChecklistOwnItems(dbContext);
            await own.AddAsync(await StatusGuidAsync(dbContext, Restricted), "FlagShowOn", true, "select false",
                false, null);
            var caseId = await CreateCaseAsync(dbContext, Restricted, UniqueKey(), 100);
            var (reconciler, runner) = Reconciler(dbContext);

            var changes = await reconciler.ReconcileAsync(caseId, "System", "Test");

            changes.Should().Be(0);
            runner.Calls.Should().Be(1);
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task SwitchingTheShowConditionOffReShowsAHiddenItemAndSwitchingItOnEvaluatesItAgain()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var own = new CaseChecklistOwnItems(dbContext);
            var itemGuid = await own.AddAsync(await StatusGuidAsync(dbContext, Restricted), "FlagShowFlip", true,
                NoteExists, false, null);
            var caseId = await CreateCaseAsync(dbContext, Restricted, UniqueKey(), 100);
            var (reconciler, runner) = Reconciler(dbContext);

            await reconciler.ReconcileAsync(caseId, "System", "Test");
            (await dbContext.CaseChecklistItem.CountAsync(w => w.CaseId == caseId)).Should().Be(0);

            await AddNoteAsync(dbContext, caseId, Users.Holder);
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            (await dbContext.CaseChecklistItem.SingleAsync(w => w.CaseId == caseId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Shown);

            await dbContext.CaseNote.Where(w => w.CaseId == caseId).DeleteAsync();
            await reconciler.ReconcileAsync(caseId, "System", "Test");
            (await dbContext.CaseChecklistItem.SingleAsync(w => w.CaseId == caseId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Hidden);

            var callsBeforeSwitchOff = runner.Calls;

            await own.SetEnableShowAsync(itemGuid, false);
            await reconciler.ReconcileAsync(caseId, "System", "Test");

            (await dbContext.CaseChecklistItem.SingleAsync(w => w.CaseId == caseId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Shown);
            runner.Calls.Should().Be(callsBeforeSwitchOff);
            (await dbContext.GetTable<Data.Poco.CaseWorkflowChecklistItem>().SingleAsync(w => w.Guid == itemGuid))
                .ShowSql.Should().Be(NoteExists);

            await own.SetEnableShowAsync(itemGuid, true);
            await reconciler.ReconcileAsync(caseId, "System", "Test");

            (await dbContext.CaseChecklistItem.SingleAsync(w => w.CaseId == caseId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Hidden);
            runner.Calls.Should().Be(callsBeforeSwitchOff + 1);
        }

        [Fact]
        public async Task ASwitchedOffValidationLetsTheItemBeTickedWithoutRunningItsSql()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var own = new CaseChecklistOwnItems(dbContext);
            await own.AddAsync(await StatusGuidAsync(dbContext, Restricted), "FlagValidationOff", false, null, false,
                "select false");
            var caseId = await CreateCaseAsync(dbContext, Restricted, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, "FlagValidationOff");

            await service.CheckAsync(caseId, itemId);

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Done);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.ValidationFailed)).Should().Be(0);
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.Checked)).Should().Be(1);
        }

        [Fact]
        public async Task ASwitchedOnValidationRunsItsSqlAndRefusesTheTickWithTheMessage()
        {
            await using var dbContext = Fx.GetDbContext();
            await using var own = new CaseChecklistOwnItems(dbContext);
            var itemGuid = await own.AddAsync(await StatusGuidAsync(dbContext, Restricted), "FlagValidationOn",
                false, null, true, "select false");
            var caseId = await CreateCaseAsync(dbContext, Restricted, UniqueKey(), 100, Users.Holder);
            var service = await ChecklistAsync(dbContext, Users.Holder, new ManualUtcTimeProvider(Start));
            await service.GetAsync(caseId);
            var itemId = await ItemIdAsync(dbContext, caseId, "FlagValidationOn");

            var failure = (await FluentActions.Awaiting(() => service.CheckAsync(caseId, itemId)).Should()
                .ThrowAsync<DtoValidationException>()).Which;

            failure.Result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Flag test message");
            (await AuditCountAsync(dbContext, caseId, CaseChecklistAction.ValidationFailed)).Should().Be(1);

            await own.SetEnableValidationAsync(itemGuid, false);
            await service.CheckAsync(caseId, itemId);

            (await dbContext.CaseChecklistItem.FirstAsync(w => w.Id == itemId)).StateId
                .Should().Be((byte)CaseChecklistItemState.Done);
        }
    }
}

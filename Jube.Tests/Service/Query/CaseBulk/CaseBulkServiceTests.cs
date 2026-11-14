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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseBulk;
using Jube.Service.Query.CaseBulk;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Reactivity;
using Jube.Service.Repository.Case;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Query.CaseBulk
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseBulkServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=x;"
            });

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Supervisor => fx.Seed.UserWithPermission;
        private string Analyst => fx.Seed.UserWithPermissionNoApproval;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static async Task<CaseBulkService> BuildAsync(DbContext dbContext, string user)
        {
            var caseService = await CaseService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, jsonSerializationHelper, TestLog.NoOp);
            var supervision = await CaseSupervisionService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());

            return new CaseBulkService(caseService, supervision, TestLog.NoOp);
        }

        private async Task<(CaseRiskSeeder.Seeded Workflow, List<int> Cases)> OpenCasesAsync(DbContext dbContext,
            int count)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Supervisor, Analyst);
            var cases = new List<int>();

            for (var i = 0; i < count; i++)
            {
                cases.Add(await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral));
            }

            return (workflow, cases);
        }

        [Fact]
        public async Task CloseClosesEveryOpenCaseWithTheReasonAndNoteAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, cases) = await OpenCasesAsync(dbContext, 3);
            var service = await BuildAsync(dbContext, Supervisor);

            var result = await service.CloseAsync(new CaseBulkCloseDto
            {
                CaseIds = cases, ClosedReasonId = 2, ClosedReasonNote = "  Rule too noisy.  "
            });

            result.Done.Should().Be(3);
            result.Skipped.Should().Be(0);
            result.Items.Select(i => i.CaseId).Should().Equal(cases);

            var closed = await dbContext.Case.Where(w => cases.Contains(w.Id)).ToListAsync();
            closed.Should().OnlyContain(c => c.ClosedStatusId == 3 && c.ClosedReasonId == 2
                                                                   && c.ClosedReasonNote == "Rule too noisy."
                                                                   && c.ClosedUser == Supervisor);
        }

        [Fact]
        public async Task CloseSkipsAClosedCaseAnUnknownCaseAndIgnoresDuplicatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, cases) = await OpenCasesAsync(dbContext, 1);
            var alreadyClosed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3);
            var service = await BuildAsync(dbContext, Supervisor);

            var result = await service.CloseAsync(new CaseBulkCloseDto
            {
                CaseIds = [cases[0], cases[0], alreadyClosed, int.MaxValue]
            });

            result.Items.Should().HaveCount(3);
            result.Items.Single(i => i.CaseId == cases[0]).Done.Should().BeTrue();
            result.Items.Single(i => i.CaseId == alreadyClosed).SkipReasonId
                .Should().Be((byte)CaseBulkSkipReason.AlreadyClosed);
            result.Items.Single(i => i.CaseId == int.MaxValue).SkipReasonId
                .Should().Be((byte)CaseBulkSkipReason.NotFound);
        }

        [Fact]
        public async Task CloseSkipsACaseHeldByAnotherUserAndLeavesItOpenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, cases) = await OpenCasesAsync(dbContext, 2);
            await dbContext.Case.Where(w => w.Id == cases[0])
                .Set(s => s.Locked, (byte)1).Set(s => s.LockedUser, Analyst).UpdateAsync();
            var service = await BuildAsync(dbContext, Supervisor);

            var result = await service.CloseAsync(new CaseBulkCloseDto { CaseIds = cases });

            result.Items.Single(i => i.CaseId == cases[0]).SkipReasonId.Should().Be((byte)CaseBulkSkipReason.Conflict);
            result.Items.Single(i => i.CaseId == cases[1]).Done.Should().BeTrue();
            (await dbContext.Case.SingleAsync(w => w.Id == cases[0])).ClosedStatusId.Should().Be(0);
        }

        [Fact]
        public async Task CloseWithoutTheCasePermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, cases) = await OpenCasesAsync(dbContext, 1);
            var service = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.CloseAsync(new CaseBulkCloseDto { CaseIds = cases });

            await act.Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            (await dbContext.Case.SingleAsync(w => w.Id == cases[0])).ClosedStatusId.Should().Be(0);
        }

        [Fact]
        public async Task CloseRefusesAnEmptyListTooManyCasesAndAnUnknownReasonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Supervisor);

            await ((Func<Task>)(() => service.CloseAsync(new CaseBulkCloseDto()))).Should()
                .ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.CloseAsync(new CaseBulkCloseDto
                { CaseIds = Enumerable.Range(1, CaseBulkService.MaximumCases + 1).ToList() }))).Should()
                .ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.CloseAsync(new CaseBulkCloseDto
                { CaseIds = [1], ClosedReasonId = 9 }))).Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.CloseAsync(new CaseBulkCloseDto
                { CaseIds = [1], ClosedReasonNote = new string('x', 1001) }))).Should()
                .ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ReassignHandsEveryEligibleCaseToTheAnalystAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, cases) = await OpenCasesAsync(dbContext, 3);
            var service = await BuildAsync(dbContext, Supervisor);

            var result = await service.ReassignAsync(new CaseBulkReassignDto { CaseIds = cases, ToUser = Analyst });

            result.Done.Should().Be(3);
            (await dbContext.Case.Where(w => cases.Contains(w.Id)).ToListAsync())
                .Should().OnlyContain(c => c.Locked == 1 && c.LockedUser == Analyst);
        }

        [Fact]
        public async Task ReassignSkipsAClosedCaseAnUnknownCaseAndAnIneligibleAnalystAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, cases) = await OpenCasesAsync(dbContext, 1);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3);
            var service = await BuildAsync(dbContext, Supervisor);

            var result = await service.ReassignAsync(new CaseBulkReassignDto
            {
                CaseIds = [cases[0], closed, int.MaxValue], ToUser = Analyst
            });

            result.Items.Single(i => i.CaseId == cases[0]).Done.Should().BeTrue();
            result.Items.Single(i => i.CaseId == closed).SkipReasonId.Should().Be((byte)CaseBulkSkipReason.Conflict);
            result.Items.Single(i => i.CaseId == int.MaxValue).SkipReasonId
                .Should().Be((byte)CaseBulkSkipReason.NotFound);

            var other = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var ineligible = await service.ReassignAsync(new CaseBulkReassignDto
            {
                CaseIds = [other], ToUser = fx.Seed.UserTenantB
            });

            ineligible.Items.Single().SkipReasonId.Should().Be((byte)CaseBulkSkipReason.Invalid);
            (await dbContext.Case.SingleAsync(w => w.Id == other)).LockedUser.Should().BeNull();
        }

        [Fact]
        public async Task ReassignRequiresAnAnalystAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Supervisor);

            await ((Func<Task>)(() => service.ReassignAsync(new CaseBulkReassignDto { CaseIds = [1] }))).Should()
                .ThrowAsync<ArgumentException>();
        }
    }
}

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
using Jube.Dto.Repository.CaseAgentReplay;
using Jube.Service.Query.CaseTriage;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseAgentReplay;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseAgentReplay
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseAgentReplayServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<Guid> replays = [];

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseAgentReplayResult.Where(w => replays.Contains(w.ReplayGuid)).DeleteAsync();
            await dbContext.CaseAgentReplay.Where(w => replays.Contains(w.Guid)).DeleteAsync();
        }

        private static Task<CaseAgentReplayService> BuildAsync(DbContext dbContext, string user) =>
            CaseAgentReplayService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private static CaseAgentReplayRequestDto Request() => new()
        {
            FromDate = DateTime.UtcNow.AddDays(-10), ToDate = DateTime.UtcNow, MaximumCases = 10, AsOfMinutes = 30
        };

        private async Task<Guid> StartAsync(CaseAgentReplayService service, CaseAgentReplayRequestDto? request = null)
        {
            var guid = await service.StartAsync(request ?? Request());
            replays.Add(guid);

            return guid;
        }

        [Fact]
        public async Task AStartedReplayIsPendingHashedByItsPromptAndListedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);

            var builtIn = await StartAsync(service);
            var request = Request();
            request.Prompt = " My prompt. ";
            var custom = await StartAsync(service, request);

            var one = await service.GetAsync(builtIn);
            var two = await service.GetAsync(custom);
            (one.StatusId, one.AgentDefinitionHash).Should().Be(((int)CaseAgentReplayStatus.Pending, CaseTriagePrompt.DefinitionHash(CaseTriagePrompt.Replay)));
            two.AgentDefinitionHash.Should().Be(CaseTriagePrompt.DefinitionHash("My prompt."));
            (await service.ListAsync()).Select(r => r.Guid).Should().Contain([builtIn, custom]);
        }

        [Fact]
        public async Task BadWindowsCountsCutOffsPromptsAndWorkflowsAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);
            var now = DateTime.UtcNow;

            foreach (var bad in new Action<CaseAgentReplayRequestDto>[]
                     {
                         r => r.ToDate = r.FromDate,
                         r => r.FromDate = now.AddDays(-400),
                         r => r.MaximumCases = 0,
                         r => r.MaximumCases = 201,
                         r => r.AsOfMinutes = -1,
                         r => r.AsOfMinutes = 10081,
                         r => r.Prompt = new string('x', 4001)
                     })
            {
                var request = Request();
                bad(request);
                await ((Func<Task>)(() => service.StartAsync(request))).Should().ThrowAsync<ArgumentException>();
            }

            var unknown = Request();
            unknown.CaseWorkflowGuid = Guid.NewGuid();
            await ((Func<Task>)(() => service.StartAsync(unknown))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }

        [Fact]
        public async Task ComparingTwoReplaysCountsAgreementOverTheirCommonCasesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);
            var first = await StartAsync(service);
            var second = await StartAsync(service);

            async Task AddAsync(Guid replay, int caseId, short human, short? recommended)
            {
                await dbContext.InsertAsync(new CaseAgentReplayResult
                {
                    ReplayGuid = replay, CaseId = caseId, HumanOutcomeId = human, RecommendedOutcomeId = recommended,
                    AsOfDate = DateTime.UtcNow
                });
            }

            await AddAsync(first, 900001, 1, 1);
            await AddAsync(first, 900002, 2, 1);
            await AddAsync(first, 900003, 1, 2);
            await AddAsync(second, 900001, 1, 1);
            await AddAsync(second, 900002, 2, 2);
            await AddAsync(second, 900004, 1, 1);

            var comparison = await service.CompareAsync(first, second);

            (comparison.CommonCases, comparison.SameRecommendation, comparison.FirstAgreed, comparison.SecondAgreed)
                .Should().Be((2, 1, 1, 2));
            (comparison.First!.MissedRisk, comparison.First.OverEscalation, comparison.First.Agreed).Should().Be((1, 1, 1));
        }

        [Fact]
        public async Task PermissionAndTenantAreEnforcedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await StartAsync(await BuildAsync(dbContext, User));

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.StartAsync(Request()))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.ListAsync())).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.GetAsync(guid))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => other.GetAsync(guid))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => other.CompareAsync(guid, guid))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            (await other.ListAsync()).Select(r => r.Guid).Should().NotContain(guid);
        }
    }
}

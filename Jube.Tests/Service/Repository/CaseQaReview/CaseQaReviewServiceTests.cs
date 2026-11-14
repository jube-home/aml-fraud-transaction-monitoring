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
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseQaReview;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseQaReview;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseQaReview
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseQaReviewServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseQaReview.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseAgentRun.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseQaReviewService> BuildAsync(DbContext dbContext, string user) =>
            CaseQaReviewService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<int> SampledClosedCaseAsync(DbContext dbContext, byte closedStatus = 3, byte qa = 2,
            string closedBy = "someone.else")
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, User);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, closedStatus);

            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.QaSampleStatusId, qa)
                .Set(s => s.ClosedUser, closedBy).UpdateAsync();

            return caseId;
        }

        [Fact]
        public async Task AReviewerJudgesASampledClosedCaseAndItIsListedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await SampledClosedCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.SubmitAsync(new CaseQaReviewDto
                { CaseId = caseId, AnalystVerdictId = (int)CaseQaVerdict.Pass, Comment = " Good work. " });

            var state = await service.ListAsync(caseId);
            state.Eligible.Should().BeTrue();
            var listed = state.Reviews.Should().ContainSingle().Subject;
            (listed.AnalystVerdictId, listed.AgentVerdictId, listed.Comment, listed.ReviewerUser)
                .Should().Be((1, null, "Good work.", User));
        }

        [Fact]
        public async Task TheAgentRecommendationCanBeJudgedOnlyWhenThereWasOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await SampledClosedCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.SubmitAsync(new CaseQaReviewDto
                    { CaseId = caseId, AnalystVerdictId = 1, AgentVerdictId = 2 })))
                .Should().ThrowAsync<ArgumentException>();

            var run = Guid.NewGuid();
            await dbContext.InsertAsync(new Data.Poco.CaseAgentRun
            {
                Guid = run, TenantRegistryId = (await Data.Repository.UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, User))!.Value,
                CaseId = caseId, RecommendedOutcomeId = 1, Rationale = "R", FactsHash = new string('a', 64),
                AgentDefinitionHash = new string('b', 64), StateId = (short)CaseAgentRunState.Accepted,
                CreatedDate = DateTime.UtcNow
            });

            await service.SubmitAsync(new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 1, AgentVerdictId = 2 });

            var stored = await dbContext.CaseQaReview.SingleAsync(w => w.CaseId == caseId);
            (stored.AgentVerdictId, stored.CaseAgentRunGuid).Should().Be(((short?)2, (Guid?)run));
        }

        [Fact]
        public async Task OnlyAPickedClosedCaseNotClosedByTheReviewerCanBeReviewedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var notPicked = await SampledClosedCaseAsync(dbContext, qa: 1);
            var open = await SampledClosedCaseAsync(dbContext, closedStatus: 0);
            var ownClose = await SampledClosedCaseAsync(dbContext, closedBy: User);
            var service = await BuildAsync(dbContext, User);
            var review = (int id) => new CaseQaReviewDto { CaseId = id, AnalystVerdictId = 1 };

            await ((Func<Task>)(() => service.SubmitAsync(review(notPicked)))).Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.SubmitAsync(review(open)))).Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.SubmitAsync(review(ownClose)))).Should().ThrowAsync<CaseExceptions.ConflictException>();
            (await dbContext.CaseQaReview.CountAsync(w => seeder.CaseIds.Contains(w.CaseId))).Should().Be(0);

            (await service.ListAsync(notPicked)).Eligible.Should().BeFalse();
            (await service.ListAsync(open)).Eligible.Should().BeFalse();
            (await service.ListAsync(ownClose)).Eligible.Should().BeFalse();
        }

        [Fact]
        public async Task BadVerdictsAndLongCommentsAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await SampledClosedCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            foreach (var bad in new[]
                     {
                         new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 0 },
                         new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 4 },
                         new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 1, AgentVerdictId = 9 },
                         new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 1, Comment = new string('x', 1001) },
                         new CaseQaReviewDto { CaseId = 0, AnalystVerdictId = 1 }
                     })
            {
                await ((Func<Task>)(() => service.SubmitAsync(bad))).Should().ThrowAsync<ArgumentException>();
            }
        }

        [Fact]
        public async Task PermissionAndTenantAreEnforcedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var caseId = await SampledClosedCaseAsync(dbContext);
            var review = new CaseQaReviewDto { CaseId = caseId, AnalystVerdictId = 1 };

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.SubmitAsync(review))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => otherTenant.SubmitAsync(review))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => otherTenant.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }
    }
}

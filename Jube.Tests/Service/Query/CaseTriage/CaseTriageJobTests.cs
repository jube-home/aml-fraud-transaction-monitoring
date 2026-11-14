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
using Jube.Dto.Options;
using Jube.Service.Query.CaseTriage;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.CaseNarrative;
using Jube.Test.Service.Repository.CaseNote;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.CaseTriage
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseTriageJobTests(DatabaseFixture fx)
    {
        private const string Reply =
            "{\"outcome\":\"confirmed\",\"reason\":\"Funds moved through three new payees.\",\"evidence\":[\"Three new payees in one day\"]}";

        private CaseTriageJob Job(FakeCaseNarrativeAgent agent, params Guid[] workflows) =>
            new(fx.ConnectionString, TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = fx.ConnectionString,
                ["CaseTriageUsers"] = fx.Seed.UserWithPermission,
                ["CaseTriageWorkflows"] = string.Join(",", workflows),
                ["CaseTriageMaximumCases"] = "25"
            }), agent, TestLog.NoOp);

        private async Task CleanAsync(int caseId)
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.CaseAgentRun.Where(w => w.CaseId == caseId).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId == caseId && w.CaseEventTypeId == 29).DeleteAsync();
        }

        [Fact]
        public async Task AnOpenCaseInAListedWorkflowGetsAPendingRecommendationWithGovernanceEvidenceAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var agent = new FakeCaseNarrativeAgent { Reply = Reply };

            try
            {
                await Job(agent, seed.CaseWorkflowGuid).RunAsync();

                await using var dbContext = fx.GetDbContext();
                var run = await dbContext.CaseAgentRun.SingleAsync(w => w.CaseId == seed.CaseId);
                run.StateId.Should().Be((short)CaseAgentRunState.Pending);
                run.RecommendedOutcomeId.Should().Be(1);
                run.Rationale.Should().Contain("three new payees");
                run.Evidence.Should().Contain("Three new payees");
                run.AskJoobyConversationId.Should().Be(77);
                run.FactsHash.Should().HaveLength(64);
                run.AgentDefinitionHash.Should().Be(CaseTriagePrompt.DefinitionHash());
                agent.Calls.Should().ContainSingle(c => c.Prompt == CaseTriagePrompt.Triage
                                                        && c.PageContext.Contains("triage"));

                var recommended = await dbContext.CaseEvent.SingleAsync(w => w.CaseId == seed.CaseId
                                                                           && w.CaseEventTypeId == 29);
                (recommended.After, recommended.CreatedUser).Should().Be(("1", fx.Seed.UserWithPermission));
            }
            finally
            {
                await CleanAsync(seed.CaseId);
            }
        }

        [Fact]
        public async Task AWorkflowThatIsNotListedIsNeverTriagedAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var agent = new FakeCaseNarrativeAgent { Reply = Reply };

            await Job(agent, Guid.NewGuid()).RunAsync();
            await Job(agent).RunAsync();

            agent.Calls.Should().NotContain(c => c.PageContext.Contains(seed.CaseKeyValue));
            await using var dbContext = fx.GetDbContext();
            (await dbContext.CaseAgentRun.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task ARecommendationIsNotRemadeWhileTheFactsAreUnchangedAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var agent = new FakeCaseNarrativeAgent { Reply = Reply };
            var job = Job(agent, seed.CaseWorkflowGuid);

            try
            {
                await job.RunAsync();
                await job.RunAsync();

                agent.Calls.Count(c => c.PageContext.Contains(seed.CaseKeyValue)).Should().Be(1);
            }
            finally
            {
                await CleanAsync(seed.CaseId);
            }
        }

        [Fact]
        public async Task ANewNoteChangesTheFactsAndSupersedesThePendingRecommendationAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var agent = new FakeCaseNarrativeAgent { Reply = Reply };
            var job = Job(agent, seed.CaseWorkflowGuid);

            try
            {
                await job.RunAsync();
                await seed.CreateNoteAsync(fx.Seed.UserWithPermission, text: "Called the customer.");
                agent.Reply = "{\"outcome\":\"notconfirmed\",\"reason\":\"Customer explained.\"}";
                await job.RunAsync();

                await using var dbContext = fx.GetDbContext();
                var runs = await dbContext.CaseAgentRun.Where(w => w.CaseId == seed.CaseId).OrderBy(w => w.Id)
                    .ToListAsync();

                runs.Select(r => (r.StateId, r.RecommendedOutcomeId)).Should().Equal(
                    ((short)CaseAgentRunState.Superseded, (short)1), ((short)CaseAgentRunState.Pending, (short)2));
            }
            finally
            {
                await CleanAsync(seed.CaseId);
            }
        }

        [Fact]
        public async Task AnUnusableOrSilentAgentWritesNothingAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);

            await Job(new FakeCaseNarrativeAgent { Reply = "I cannot say." }, seed.CaseWorkflowGuid).RunAsync();
            await Job(new FakeCaseNarrativeAgent { Silent = true }, seed.CaseWorkflowGuid).RunAsync();

            await using var dbContext = fx.GetDbContext();
            (await dbContext.CaseAgentRun.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }
    }
}

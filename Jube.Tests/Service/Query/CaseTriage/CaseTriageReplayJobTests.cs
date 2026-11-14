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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Service.Query.CaseNarrative.Models;
using Jube.Service.Query.CaseTriage;
using Jube.Service.Query.CaseTriage.Interfaces;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Service.Query.CaseTriage
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseTriageReplayJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly string key = "K-" + Guid.NewGuid().ToString("N")[..10];
        private readonly List<Guid> replays = [];
        private static readonly DateTime created = DateTime.UtcNow.AddDays(-3);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ExecuteAsync("DELETE FROM \"Archive\" WHERE \"EntryKeyValue\" = @k", new DataParameter("k", key));
            await dbContext.CaseNote.Where(w => w.CaseKeyValue == key).DeleteAsync();
            await dbContext.CaseAgentReplayResult.Where(w => replays.Contains(w.ReplayGuid)).DeleteAsync();
            await dbContext.CaseAgentReplay.Where(w => replays.Contains(w.Guid)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private sealed class Recorder(string reply) : ICaseTriageReplayAgent
        {
            public List<(string Prompt, string Facts)> Calls { get; } = [];

            public Task<CaseNarrativeAnswer?> AskAsync(string prompt, string facts, CancellationToken token = default)
            {
                Calls.Add((prompt, facts));

                return Task.FromResult<CaseNarrativeAnswer?>(new CaseNarrativeAnswer(reply, null, "model-y"));
            }
        }

        private async Task<(int CaseId, int Tenant)> SeedAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, fx.Seed.UserWithPermission);
            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Select(s => s.EntityAnalysisModelId).SingleAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).Set(s => s.EntryName, "AccountId").UpdateAsync();

            var earlier = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, key);
            await dbContext.Case.Where(w => w.Id == earlier).Set(s => s.CreatedDate, created.AddDays(-30))
                .Set(s => s.ClosedDate, created.AddDays(-20)).UpdateAsync();
            var later = await seeder.SeedCaseAsync(dbContext, workflow, workflow.NotConfirmed, 3, key);
            await dbContext.Case.Where(w => w.Id == later).Set(s => s.CreatedDate, created.AddDays(5))
                .Set(s => s.ClosedDate, created.AddDays(6)).UpdateAsync();

            var target = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, key);
            await dbContext.Case.Where(w => w.Id == target).Set(s => s.CreatedDate, created)
                .Set(s => s.ClosedDate, DateTime.UtcNow.AddDays(-1)).UpdateAsync();

            foreach (var (minutes, mark) in new[] { (10, "BEFOREMARK"), (120, "AFTERMARK") })
            {
                await dbContext.ExecuteAsync(
                    "INSERT INTO \"Archive\" (\"Json\",\"EntityAnalysisModelInstanceEntryGuid\",\"EntryKeyValue\",\"EntityAnalysisModelId\",\"CreatedDate\",\"ReferenceDate\") VALUES (@j::jsonb,@g,@k,@m,@d,@d)",
                    new DataParameter("j", $"{{\"note\":\"{mark}\"}}"), new DataParameter("g", Guid.NewGuid()),
                    new DataParameter("k", key), new DataParameter("m", modelId),
                    new DataParameter("d", created.AddMinutes(minutes)));
            }

            foreach (var (minutes, text, source) in new[]
                     {
                         (5, "NOTEBEFORE", 1), (200, "NOTEAFTER", 1), (6, "NOTEFROMAI", 3)
                     })
            {
                await dbContext.InsertAsync(new CaseNote
                {
                    CaseKey = "AccountId", CaseKeyValue = key, Note = text, CreatedDate = created.AddMinutes(minutes),
                    SourceId = source, KindId = 1, CreatedUser = fx.Seed.UserWithPermission, CaseId = target
                });
            }

            var tenant = (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission))!.Value;

            return (target, tenant);
        }

        private async Task<Guid> RequestAsync(DbContext dbContext, int tenant, string? prompt = null)
        {
            var guid = Guid.NewGuid();
            replays.Add(guid);

            await new CaseAgentReplayRepository(dbContext).InsertAsync(new CaseAgentReplay
            {
                Guid = guid, TenantRegistryId = tenant, StatusId = (short)CaseAgentReplayStatus.Pending,
                PromptText = prompt, AgentDefinitionHash = CaseTriagePrompt.DefinitionHash(prompt ?? CaseTriagePrompt.Replay),
                FromDate = DateTime.UtcNow.AddDays(-2), ToDate = DateTime.UtcNow.AddMinutes(5), MaximumCases = 50,
                AsOfMinutes = 60, CreatedUser = fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
            });

            return guid;
        }

        private CaseTriageReplayJob Job(ICaseTriageReplayAgent agent) =>
            new(fx.ConnectionString, TestDynamicEnvironment.Create(new Dictionary<string, string>
                { ["ConnectionString"] = fx.ConnectionString }), agent, TestLog.NoOp);

        [Fact]
        public async Task TheModelSeesOnlyTheFactsFromBeforeTheCutOffAndNeverTheOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (target, tenant) = await SeedAsync(dbContext);
            var replay = await RequestAsync(dbContext, tenant);
            var agent = new Recorder("{\"outcome\":\"confirmed\",\"reason\":\"Looks bad.\"}");

            await Job(agent).RunPendingAsync();

            var call = agent.Calls.Single(c => c.Facts.Contains(key) && c.Facts.Contains("BEFOREMARK"));
            call.Facts.Should().Contain("BEFOREMARK").And.Contain("NOTEBEFORE");
            call.Facts.Should().NotContain("AFTERMARK").And.NotContain("NOTEAFTER").And.NotContain("NOTEFROMAI");
            call.Facts.Should().Contain("\"confirmed\":1,\"notConfirmed\":0");
            call.Prompt.Should().Be(CaseTriagePrompt.Replay);

            var result = await dbContext.CaseAgentReplayResult.SingleAsync(w => w.ReplayGuid == replay && w.CaseId == target);
            (result.HumanOutcomeId, result.RecommendedOutcomeId).Should().Be(((short)1, (short?)1));

            var header = await dbContext.CaseAgentReplay.SingleAsync(w => w.Guid == replay);
            (header.StatusId, header.ModelName).Should().Be(((short)CaseAgentReplayStatus.Completed, "model-y"));
        }

        [Fact]
        public async Task AnUnusableAnswerIsRecordedAsUnscoredAndAReplayRunsOnlyOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (target, tenant) = await SeedAsync(dbContext);
            var replay = await RequestAsync(dbContext, tenant, "Custom prompt text.");
            var agent = new Recorder("no idea");

            await Job(agent).RunPendingAsync();
            var calls = agent.Calls.Count;
            await Job(agent).RunPendingAsync();

            agent.Calls.Count.Should().Be(calls);
            agent.Calls.Should().Contain(c => c.Prompt == "Custom prompt text." && c.Facts.Contains(key));
            var result = await dbContext.CaseAgentReplayResult.SingleAsync(w => w.ReplayGuid == replay && w.CaseId == target);
            result.RecommendedOutcomeId.Should().BeNull();
            (await dbContext.CaseAgentReplay.SingleAsync(w => w.Guid == replay)).CasesScored.Should().Be(0);
        }

        [Fact]
        public async Task AReplayWithNoWorkflowCoversOnlyTheWorkflowsTheRequesterCanAccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (target, tenant) = await SeedAsync(dbContext);

            var hidden = await seeder.SeedWorkflowAsync(dbContext);
            var hiddenCase = await seeder.SeedCaseAsync(dbContext, hidden, hidden.Confirmed, 3, key);
            await dbContext.Case.Where(w => w.Id == hiddenCase).Set(s => s.CreatedDate, created)
                .Set(s => s.ClosedDate, DateTime.UtcNow.AddDays(-1)).UpdateAsync();

            var replay = await RequestAsync(dbContext, tenant);

            await Job(new Recorder("{\"outcome\":\"confirmed\",\"reason\":\"x\"}")).RunPendingAsync();

            var covered = await dbContext.CaseAgentReplayResult.Where(w => w.ReplayGuid == replay)
                .Select(s => s.CaseId).ToListAsync();

            covered.Should().Contain(target).And.NotContain(hiddenCase);
        }
    }
}

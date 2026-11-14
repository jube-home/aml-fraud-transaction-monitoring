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
using Jube.Data.Query.GetCaseSimilarityDueQuery;
using Jube.Dto.Options;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseSimilarity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.CaseKeyJournal;
using LinqToDB;
using LinqToDB.Async;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseSimilarity
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseSimilarityJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string CaseKey = "AccountId";
        private const string HashingKey = "unit-test-CaseSimilarityHashingKey";

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private readonly List<int> cases = [];
        private readonly JournalJobsSeed seed = new(fx);
        private readonly List<Guid> workflows = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseSimilarityComponent.Where(w => workflows.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseSimilarityComponentFrequency.Where(w => workflows.Contains(w.CaseWorkflowGuid))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseTag>().Where(w => cases.Contains(w.CaseId)).DeleteAsync();
            await dbContext.Case.Where(w => cases.Contains(w.Id)).DeleteAsync();
            await seed.CleanupAsync(dbContext);
        }

        private static CaseSimilarityJob Job(TimeSpan? refreshAfter = null)
        {
            return JobWithKey(HashingKey, refreshAfter);
        }

        private static CaseSimilarityJob JobWithKey(string? hashingKey, TimeSpan? refreshAfter = null)
        {
            var environment = hashingKey is null
                ? TestDynamicEnvironment.Create()
                : TestDynamicEnvironment.Create(new Dictionary<string, string>
                {
                    [CaseSimilarityHash.Setting] = hashingKey
                });

            return new CaseSimilarityJob(connectionString, environment, TestLog.NoOp,
                new CaseSimilarityJobSettings { RefreshAfter = refreshAfter ?? TimeSpan.FromMinutes(30) });
        }

        private async Task<(int Model, Guid Workflow)> WorkflowAsync(DbContext dbContext)
        {
            var (model, _, workflow) = await seed.WorkflowAsync(dbContext);

            workflows.Add(workflow);
            await seed.RequestXPathAsync(dbContext, model, "Channel", 1, false);

            return (model, workflow);
        }

        private async Task<int> CaseAsync(DbContext dbContext, Guid workflow, string account, byte closedStatusId = 0,
            DateTime? closedDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                Locked = 0,
                ClosedStatusId = closedStatusId,
                ClosedDate = closedDate,
                CaseKey = CaseKey,
                CaseKeyValue = account,
                Json = "{}"
            });

            cases.Add(id);

            return id;
        }

        private async Task TransactionAsync(DbContext dbContext, int model, string account, string device,
            double amount, params string[] rules)
        {
            var payload = new JObject
            {
                [CaseKey] = account, ["Device"] = device, ["Channel"] = "Card", ["Amount"] = amount
            };

            var guid = await seed.ArchiveAsync(dbContext, model, payload, DateTime.UtcNow.AddMinutes(-1));
            var activation = new JObject();

            foreach (var rule in rules)
            {
                activation[rule] = new JObject { ["visible"] = true };
            }

            var json = new JObject { ["payload"] = payload, ["activation"] = activation, ["tag"] = new JArray() };

            await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid)
                .Set(a => a.Json, json.ToString(Newtonsoft.Json.Formatting.None)).UpdateAsync();
        }

        private static async Task<GetCaseSimilarityDueQueryDto?> DueAsync(DbContext dbContext, int caseId,
            TimeSpan? refreshAfter = null)
        {
            return (await new GetCaseSimilarityDueQuery(dbContext)
                    .ExecuteAsync(DateTime.UtcNow - (refreshAfter ?? TimeSpan.FromMinutes(30)), 1_000_000))
                .SingleOrDefault(row => row.CaseId == caseId);
        }

        private static async Task RebuildAsync(DbContext dbContext, int caseId)
        {
            var due = await DueAsync(dbContext, caseId, TimeSpan.FromMinutes(-5));

            due.Should().NotBeNull();
            await Job().RebuildAsync(dbContext, new ArchiveKeyFieldResolver(dbContext), due!);
        }

        private static Task<List<Data.Poco.CaseSimilarityComponent>> VectorAsync(DbContext dbContext, int caseId)
        {
            return dbContext.CaseSimilarityComponent.Where(c => c.CaseId == caseId)
                .OrderBy(c => c.KindId).ThenBy(c => c.Component).ToListAsync();
        }

        private static async Task FrequenciesShouldEqualARecountAsync(DbContext dbContext, Guid workflow)
        {
            var recount = (await dbContext.CaseSimilarityComponent.Where(c => c.CaseWorkflowGuid == workflow)
                    .GroupBy(c => new { c.KindId, c.Component })
                    .Select(g => new { g.Key.KindId, g.Key.Component, Count = g.Count() }).ToListAsync())
                .ToDictionary(g => (g.KindId, g.Component), g => g.Count);

            recount[(0, string.Empty)] = await dbContext.CaseSimilarityComponent
                .Where(c => c.CaseWorkflowGuid == workflow).Select(c => c.CaseId).Distinct().CountAsync();

            var stored = (await dbContext.CaseSimilarityComponentFrequency
                    .Where(f => f.CaseWorkflowGuid == workflow && f.CaseCount != 0).ToListAsync())
                .ToDictionary(f => (f.KindId, f.Component), f => f.CaseCount);

            stored.Should().BeEquivalentTo(recount.Where(pair => pair.Value != 0)
                .ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        [Fact]
        public async Task TheVectorIsBuiltFromEveryTransactionOnTheKeyWithNoConfigurationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflow) = await WorkflowAsync(dbContext);
            var id = await CaseAsync(dbContext, workflow, "ACC-1");

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-8831A", 600, "Structuring");
            await TransactionAsync(dbContext, model, "ACC-1", "DEV-8831A", 3000, "Velocity");
            await TransactionAsync(dbContext, model, "ACC-2", "DEV-OTHER", 1, "Unrelated");

            await RebuildAsync(dbContext, id);

            var vector = await VectorAsync(dbContext, id);

            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Rule).Select(c => c.Component)
                .Should().Equal("Structuring", "Velocity");
            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Measure).Select(c => c.Component)
                .Should().Equal("Amount:max=+e3.0", "Amount:mean=+e3.0", "Amount:sum=+e3.5");
            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Entity).Select(c => c.Component)
                .Should().HaveCount(2).And.OnlyContain(c => c.StartsWith("AccountId=") || c.StartsWith("Device="));
            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Category).Select(c => c.Component)
                .Should().ContainSingle().Which.Should().StartWith("Channel=");
            vector.Should().OnlyContain(c => c.CaseWorkflowGuid == workflow);
            vector.Should().NotContain(c => c.Component.Contains("8831") || c.Component.Contains("ACC-1")
                                                                       || c.Component.Contains("Card"));

            (await dbContext.Case.Where(c => c.Id == id).Select(c => c.SimilarityCheckedDate).SingleAsync())
                .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            await FrequenciesShouldEqualARecountAsync(dbContext, workflow);
        }

        [Fact]
        public async Task ARebuildFollowsNewTransactionsAndTagsAndKeepsTheFrequenciesEqualToARecountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflow) = await WorkflowAsync(dbContext);
            var first = await CaseAsync(dbContext, workflow, "ACC-1");
            var second = await CaseAsync(dbContext, workflow, "ACC-2");

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-SHARED", 600, "Structuring");
            await TransactionAsync(dbContext, model, "ACC-2", "DEV-SHARED", 700, "Structuring");
            await RebuildAsync(dbContext, first);
            await RebuildAsync(dbContext, second);

            var device = (await VectorAsync(dbContext, first)).Single(c => c.Component.StartsWith("Device=")).Component;

            (await dbContext.CaseSimilarityComponentFrequency
                .Where(f => f.CaseWorkflowGuid == workflow && f.Component == device).Select(f => f.CaseCount)
                .SingleAsync()).Should().Be(2);

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-NEW", 90000, "Velocity");

            await dbContext.InsertAsync(new Data.Poco.CaseTag
            {
                CaseId = first, Name = "Mule", Version = 1, CreatedUser = seed.Me, CreatedDate = DateTime.UtcNow
            });

            await RebuildAsync(dbContext, first);

            var vector = await VectorAsync(dbContext, first);

            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Rule).Select(c => c.Component)
                .Should().Equal("Structuring", "Velocity");
            vector.Where(c => c.KindId == (short)CaseSimilarityKind.Tag).Select(c => c.Component)
                .Should().Equal("Mule");
            vector.Count(c => c.Component.StartsWith("Device=")).Should().Be(2);
            vector.Should().Contain(c => c.Component == "Amount:max=+e4.5");
            vector.Should().NotContain(c => c.Component == "Amount:max=+e2.5");
            await FrequenciesShouldEqualARecountAsync(dbContext, workflow);

            await dbContext.Case.Where(c => c.Id == second).Set(c => c.CaseKeyValue, (string?)null).UpdateAsync();
            await RebuildAsync(dbContext, second);

            (await VectorAsync(dbContext, second)).Should().BeEmpty();
            await FrequenciesShouldEqualARecountAsync(dbContext, workflow);
        }

        [Fact]
        public async Task AnOpenCaseIsDueAgainOnlyOnceItsLastCheckIsOldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, workflow) = await WorkflowAsync(dbContext);
            var id = await CaseAsync(dbContext, workflow, "ACC-1");

            (await DueAsync(dbContext, id)).Should().NotBeNull();

            await RebuildAsync(dbContext, id);

            (await DueAsync(dbContext, id)).Should().BeNull();

            await dbContext.Case.Where(c => c.Id == id)
                .Set(c => c.SimilarityCheckedDate, DateTime.UtcNow.AddHours(-1)).UpdateAsync();

            (await DueAsync(dbContext, id)).Should().NotBeNull();
        }

        [Fact]
        public async Task AClosedCaseIsRebuiltOnceAfterItClosesAndThenLeftAsItStoodAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflow) = await WorkflowAsync(dbContext);
            var id = await CaseAsync(dbContext, workflow, "ACC-1");

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-1", 600, "Structuring");
            await RebuildAsync(dbContext, id);

            await dbContext.Case.Where(c => c.Id == id).Set(c => c.ClosedStatusId, (byte?)3)
                .Set(c => c.ClosedDate, DateTime.UtcNow).UpdateAsync();

            (await DueAsync(dbContext, id)).Should().NotBeNull();

            await RebuildAsync(dbContext, id);

            var frozen = (await VectorAsync(dbContext, id)).Select(c => c.Component).ToList();

            await dbContext.Case.Where(c => c.Id == id)
                .Set(c => c.SimilarityCheckedDate, DateTime.UtcNow.AddDays(1)).UpdateAsync();
            await TransactionAsync(dbContext, model, "ACC-1", "DEV-2", 90000, "Velocity");

            (await DueAsync(dbContext, id, TimeSpan.FromDays(-30))).Should().BeNull();
            (await VectorAsync(dbContext, id)).Select(c => c.Component).Should().Equal(frozen);
        }

        [Fact]
        public async Task ACaseWhoseWorkflowIsGoneIsStampedWithAnEmptyVectorSoItNeverBlocksTheSweepAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var orphan = Guid.NewGuid();

            workflows.Add(orphan);

            var id = await CaseAsync(dbContext, orphan, "ACC-1");
            var due = await DueAsync(dbContext, id);

            due!.EntityAnalysisModelId.Should().BeNull();

            await Job().RebuildAsync(dbContext, new ArchiveKeyFieldResolver(dbContext), due);

            (await VectorAsync(dbContext, id)).Should().BeEmpty();
            (await DueAsync(dbContext, id)).Should().BeNull();
        }

        [Fact]
        public async Task WithoutAHashingKeyNoVectorIsBuiltAndTheCaseStaysDueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflow) = await WorkflowAsync(dbContext);
            var id = await CaseAsync(dbContext, workflow, "ACC-1");

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-1", 600, "Structuring");

            var due = await DueAsync(dbContext, id);
            var job = JobWithKey(null);

            job.HasHashingKey.Should().BeFalse();
            (await job.RebuildAsync(dbContext, new ArchiveKeyFieldResolver(dbContext), due!)).Should().Be(0);
            (await job.RunOnceAsync()).Checked.Should().Be(0);

            (await VectorAsync(dbContext, id)).Should().BeEmpty();
            (await DueAsync(dbContext, id)).Should().NotBeNull();
        }

        [Fact]
        public async Task BuildingAVectorWritesNothingElseAboutTheCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflow) = await WorkflowAsync(dbContext);
            var id = await CaseAsync(dbContext, workflow, "ACC-1");

            await TransactionAsync(dbContext, model, "ACC-1", "DEV-1", 600, "Structuring");

            var before = await dbContext.Case.SingleAsync(c => c.Id == id);

            await RebuildAsync(dbContext, id);

            var after = await dbContext.Case.SingleAsync(c => c.Id == id);

            after.Should().BeEquivalentTo(before, options => options.Excluding(c => c.SimilarityCheckedDate)
                .Excluding(c => c.CaseWorkflow).Excluding(c => c.CaseWorkflowStatus));
            (await dbContext.GetTable<Data.Poco.CaseEvent>().CountAsync(e => e.CaseId == id)).Should().Be(0);
            (await dbContext.GetTable<Data.Poco.CaseRiskAssessment>().CountAsync(a => a.CaseId == id)).Should().Be(0);
            (await dbContext.CaseLink.CountAsync(l => l.CaseIdLow == id || l.CaseIdHigh == id)).Should().Be(0);
        }
    }
}

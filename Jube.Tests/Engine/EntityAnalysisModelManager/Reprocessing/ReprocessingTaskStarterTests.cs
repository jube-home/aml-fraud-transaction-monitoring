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
using Jube.Dictionary;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using LinqToDB.Data;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ReprocessingTaskStarterTests(ReprocessingModelFixture fixture)
        : IClassFixture<ReprocessingModelFixture>, IAsyncLifetime
    {
        private const string AmountRule = "If (Payload.CurrencyAmount > 50) Then\n   Return True\nEnd If";

        public Task InitializeAsync() => ClearAsync();

        public Task DisposeAsync() => ClearAsync();

        private static DbContext Db()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ModelScaffold.ConnectionString,
                TestLog.NoOp);
        }

        private KeyValuePair<int,
                global::Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel>
            Model()
        {
            var model = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid)
                        ?? throw new InvalidOperationException("The scaffolded model is not loaded.");
            model.Started.Should().BeTrue();
            return new KeyValuePair<int,
                global::Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel>(
                model.Instance.Id, model);
        }

        private ReprocessingTaskStarter Starter(TimeSpan? progressInterval = null)
        {
            return new ReprocessingTaskStarter(fixture.Engine.Engine.Context.Tasks.EntityAnalysisModelManager.Context)
            {
                ProgressInterval = progressInterval ?? TimeSpan.FromSeconds(10)
            };
        }

        private async Task ClearAsync()
        {
            await using var dbContext = Db();
            var modelId = fixture.Shared.ModelId;
            await dbContext.ExecuteAsync(
                "delete from \"ArchiveKeyVersion\" v using \"ArchiveKey\" k, \"Archive\" a where v.\"ArchiveKeyId\" = k.\"Id\" and k.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" and a.\"EntityAnalysisModelId\" = @id",
                new DataParameter("id", modelId));
            await dbContext.ExecuteAsync(
                "delete from \"ArchiveKey\" k using \"Archive\" a where k.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" and a.\"EntityAnalysisModelId\" = @id",
                new DataParameter("id", modelId));
            await dbContext.ExecuteAsync(
                "delete from \"ArchiveVersion\" v using \"Archive\" a where v.\"ArchiveId\" = a.\"Id\" and a.\"EntityAnalysisModelId\" = @id",
                new DataParameter("id", modelId));
            await dbContext.ExecuteAsync("delete from \"Archive\" where \"EntityAnalysisModelId\" = @id",
                new DataParameter("id", modelId));
            await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(w => w.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.EntityAnalysisModelReprocessingRule.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
        }

        private async Task SeedAsync(int from, int count, Func<string, string> referenceDate,
            string? createdDate = null)
        {
            await using var dbContext = Db();
            await dbContext.ExecuteAsync(
                "insert into \"Archive\" (\"Json\", \"EntityAnalysisModelInstanceEntryGuid\", \"EntryKeyValue\", " +
                "\"ResponseElevation\", \"EntityAnalysisModelId\", \"ActivationRuleCount\", \"CreatedDate\", \"ReferenceDate\") " +
                "select jsonb_build_object('payload', jsonb_build_object('AccountId', 'Acc' || (i % 7), 'TxnId', 'Txn' || i, " +
                "'CurrencyAmount', i % 100, 'TxnDateTime', to_char(" + referenceDate("i") +
                ", 'YYYY-MM-DD\"T\"HH24:MI:SS'))), " +
                "gen_random_uuid(), 'Txn' || i, 0, @model, 0, " +
                (createdDate ?? "(now() at time zone 'utc') - interval '1 hour'") +
                ", " + referenceDate("i") + " from generate_series(@from, @to) i",
                new DataParameter("model", fixture.Shared.ModelId), new DataParameter("from", from),
                new DataParameter("to", from + count - 1));
        }

        private static string SecondsAfterBase(string i, int divisor = 1)
        {
            return $"timestamp '2024-01-01 00:00:00' + ({i} / {divisor}) * interval '1 second'";
        }

        private async Task<int> CreateInstanceAsync(string coderRule, double sample = 100, string interval = "y",
            int value = 10)
        {
            await using var dbContext = Db();
            var ruleId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelReprocessingRule
            {
                EntityAnalysisModelId = fixture.Shared.ModelId, Name = "Reprocess" + Guid.NewGuid().ToString("N")[..8],
                RuleScriptTypeId = 2, CoderRuleScript = coderRule, BuilderRuleScript = "", Json = "{}",
                ReprocessingInterval = interval, ReprocessingValue = value, ReprocessingSample = sample, Active = 1,
                Deleted = 0, Priority = 1, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "Test"
            });
            return await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelReprocessingRuleInstance
            {
                EntityAnalysisModelReprocessingRuleId = ruleId, StatusId = 0, CreatedDate = DateTime.UtcNow,
                CreatedUser = "Test", Deleted = 0, Version = 1
            });
        }

        private static async Task<EntityAnalysisModelReprocessingRuleInstance> InstanceAsync(int id)
        {
            await using var dbContext = Db();
            return await dbContext.EntityAnalysisModelReprocessingRuleInstance.SingleAsync(w => w.Id == id);
        }

        private async Task<List<Archive>> ArchiveAsync()
        {
            await using var dbContext = Db();
            return await dbContext.Archive.Where(w => w.EntityAnalysisModelId == fixture.Shared.ModelId).ToListAsync();
        }

        private async Task<ReprocessingRunResult?> TryRunAsync(ReprocessingTaskStarter? starter = null)
        {
            await using var dbContext = Db();
            return await (starter ?? Starter()).RunNextAsync(dbContext, Model(), CancellationToken.None);
        }

        private async Task<ReprocessingRunResult> RunAsync(ReprocessingTaskStarter? starter = null)
        {
            return (await TryRunAsync(starter))!;
        }

        [Fact]
        public async Task WithNothingWaitingNothingIsClaimedAsync()
        {
            await using var dbContext = Db();

            (await Starter().RunNextAsync(dbContext, Model(), CancellationToken.None)).Should().BeNull();
        }

        [Fact]
        public async Task EveryDocumentInTheWindowIsProcessedExactlyOnceAcrossPagesAndTiesAsync()
        {
            await SeedAsync(1, 250, i => SecondsAfterBase(i, 3));
            var instanceId = await CreateInstanceAsync(AmountRule);

            var result = await RunAsync();

            var expectedMatched = Enumerable.Range(1, 250).Count(i => i % 100 > 50);
            result.Outcome.Should().Be(ReprocessingRunOutcome.Completed, result.Failure);
            result.Processed.Should().Be(250);
            result.Sampled.Should().Be(250);
            result.Matched.Should().Be(expectedMatched);
            result.Errors.Should().Be(0);
            result.Pages.Should()
                .Be((250 + ReprocessingModelFixture.BulkLimit - 1) / ReprocessingModelFixture.BulkLimit);

            var archive = await ArchiveAsync();
            var reprocessed = archive.Where(a => a.EntityAnalysisModelsReprocessingRuleInstanceId == instanceId)
                .ToList();
            reprocessed.Should().HaveCount(expectedMatched);
            reprocessed.Should().OnlyContain(a => a.Version == 1);
            archive.Except(reprocessed).Should().OnlyContain(a => a.Version == null);

            await using var dbContext = Db();
            var versions = await dbContext.ArchiveVersion
                .Where(v => reprocessed.Select(r => r.Id).Contains(v.ArchiveId)).ToListAsync();
            versions.Should().HaveCount(expectedMatched);
            versions.Select(v => v.ArchiveId).Should().OnlyHaveUniqueItems();

            var instance = await InstanceAsync(instanceId);
            instance.StatusId.Should().Be(4);
            instance.CompletedDate.Should().NotBeNull();
            instance.AvailableCount.Should().Be(250);
            instance.ProcessedCount.Should().Be(250);
            instance.SampledCount.Should().Be(250);
            instance.MatchedCount.Should().Be(expectedMatched);
            instance.ErrorCount.Should().Be(0);
        }

        [Fact]
        public async Task TheReprocessedArchiveCarriesTheNewModelOutputAsync()
        {
            await SeedAsync(1, 5, i => SecondsAfterBase(i));
            var instanceId = await CreateInstanceAsync("Return True");

            (await RunAsync()).Matched.Should().Be(5);

            var archive = await ArchiveAsync();
            archive.Should().OnlyContain(a => a.EntityAnalysisModelsReprocessingRuleInstanceId == instanceId);
            archive.Should().OnlyContain(a => a.Json.Contains("\"payload\""));
            archive.Should().OnlyContain(a => a.CreatedDate > DateTime.UtcNow.AddMinutes(-5));
        }

        [Fact]
        public async Task OnlyDocumentsWithinTheIntervalOfTheLatestAreProcessedAsync()
        {
            await SeedAsync(1, 300, i => $"timestamp '2024-01-01 00:00:00' + {i} * interval '1 minute'");
            await CreateInstanceAsync("Return True", interval: "h", value: 1);

            var result = await RunAsync();

            result.Processed.Should().Be(61);
            result.Matched.Should().Be(61);
        }

        [Fact]
        public async Task DocumentsArchivedAfterTheRunStartedAreNotProcessedAsync()
        {
            await SeedAsync(1, 30, i => SecondsAfterBase(i));
            await SeedAsync(31, 10, i => SecondsAfterBase(i), "(now() at time zone 'utc') + interval '1 day'");
            await CreateInstanceAsync("Return True");

            var result = await RunAsync();

            result.Processed.Should().Be(30);
            (await ArchiveAsync()).Count(a => a.Version == 1).Should().Be(30);
        }

        [Fact]
        public async Task ASampleOfNothingProcessesButMatchesNothingAsync()
        {
            await SeedAsync(1, 50, i => SecondsAfterBase(i));
            await CreateInstanceAsync("Return True", 0);

            var result = await RunAsync();

            result.Processed.Should().Be(50);
            result.Sampled.Should().Be(0);
            result.Matched.Should().Be(0);
            (await ArchiveAsync()).Should().OnlyContain(a => a.Version == null);
        }

        [Fact]
        public async Task AHalfSampleIsAPercentageNotAFractionAsync()
        {
            await SeedAsync(1, 400, i => SecondsAfterBase(i));
            await CreateInstanceAsync("Return False", 50);

            var result = await RunAsync();

            result.Processed.Should().Be(400);
            result.Sampled.Should().BeInRange(120, 280);
        }

        [Fact]
        public async Task ARuleThatDoesNotParseFailsTheInstanceAndDoesNotBlockTheNextAsync()
        {
            await SeedAsync(1, 10, i => SecondsAfterBase(i));
            var instanceId = await CreateInstanceAsync("If (Payload.NoSuchField ??? Then");

            var result = await RunAsync();

            result.Outcome.Should().Be(ReprocessingRunOutcome.Failed);
            result.Failure.Should().NotBeNullOrEmpty();
            result.Processed.Should().Be(0);
            var instance = await InstanceAsync(instanceId);
            instance.StatusId.Should().Be(5);
            (await ArchiveAsync()).Should().OnlyContain(a => a.Version == null);

            await using var dbContext = Db();
            var ruleId = instance.EntityAnalysisModelReprocessingRuleId!.Value;
            await new Jube.Data.Repository.EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext,
                    fixture.Shared.UserName)
                .InsertAsync(new EntityAnalysisModelReprocessingRuleInstance
                    { EntityAnalysisModelReprocessingRuleId = ruleId });
        }

        [Fact]
        public async Task AModelWithNoArchiveCompletesAtOnceAsync()
        {
            var instanceId = await CreateInstanceAsync("Return True");

            var result = await RunAsync();

            result.Outcome.Should().Be(ReprocessingRunOutcome.Completed);
            result.Processed.Should().Be(0);
            (await InstanceAsync(instanceId)).StatusId.Should().Be(4);
        }

        [Fact]
        public async Task DeletingTheInstanceDuringTheRunStopsItAndLeavesItDeletedAsync()
        {
            await SeedAsync(1, 600, i => SecondsAfterBase(i));
            var instanceId = await CreateInstanceAsync("Return True");
            var starter = Starter(TimeSpan.Zero);

            var run = RunAsync(starter);
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while ((await InstanceAsync(instanceId)).ProcessedCount.GetValueOrDefault() == 0 &&
                   DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            await using (var dbContext = Db())
            {
                await dbContext.EntityAnalysisModelReprocessingRuleInstance.Where(w => w.Id == instanceId)
                    .Set(s => s.Deleted, (byte)1).UpdateAsync();
            }

            var result = await run;

            result.Outcome.Should().Be(ReprocessingRunOutcome.Stopped);
            result.Processed.Should().BeLessThan(600);
            var instance = await InstanceAsync(instanceId);
            instance.StatusId.Should().NotBe(4);
            instance.CompletedDate.Should().BeNull();
        }

        private async Task<Dictionary<string, byte[]>> CacheSnapshotAsync(IDatabase database, IServer server)
        {
            var tenant = fixture.Shared.TenantRegistryId;
            var snapshot = new Dictionary<string, byte[]>();
            await foreach (var key in server.KeysAsync(pattern: $"*{tenant}*{fixture.Shared.ModelGuid:N}*"))
            {
                if (!key.ToString().StartsWith("LruJournal:", StringComparison.Ordinal))
                {
                    snapshot[key!] = (await database.KeyDumpAsync(key))!;
                }
            }

            snapshot["ReferenceDate"] = (await database.HashGetAsync($"ReferenceDate:{tenant}",
                fixture.Shared.ModelGuid.ToString("N")))!;
            return snapshot;
        }

        [Fact]
        public async Task ReprocessingLeavesTheCacheExactlyAsLiveTrafficLeftItAsync()
        {
            await SeedAsync(1, 30, i => SecondsAfterBase(i));
            await CreateInstanceAsync("Return True");
            var model = Model().Value;
            var cache = model.Services.CacheService;
            var tenant = fixture.Shared.TenantRegistryId;
            var modelGuid = fixture.Shared.ModelGuid;

            await using var multiplexer =
                await ConnectionMultiplexer.ConnectAsync(ModelEngineHost.RedisConnectionString + ",allowAdmin=true");
            var database = multiplexer.GetDatabase();
            var server = multiplexer.GetServer(multiplexer.GetEndPoints()[0]);
            await foreach (var key in server.KeysAsync(pattern: $"*{tenant}*{modelGuid:N}*"))
            {
                await database.KeyDeleteAsync(key);
            }

            foreach (var archive in (await ArchiveAsync()).OrderBy(a => a.ReferenceDate).Take(10))
            {
                var payload = new DictionaryNoBoxing<int>();
                payload.TryAdd(-1, archive.ReferenceDate!.Value);
                await cache.CachePayloadRepository.InsertAsync(tenant, modelGuid, payload,
                    archive.ReferenceDate.Value, archive.EntityAnalysisModelInstanceEntryGuid);
                await cache.CachePayloadRepository.InsertPayloadJournalAndLedgerAsync(tenant, modelGuid, "AccountId",
                    archive.EntryKeyValue, archive.ReferenceDate.Value, archive.EntityAnalysisModelInstanceEntryGuid);
            }

            var latest = DateTime.UtcNow.AddMinutes(-1);
            foreach (var account in Enumerable.Range(0, 7).Select(n => "Acc" + n))
            {
                await cache.CachePayloadLatestRepository.UpsertAsync(tenant, modelGuid, latest, Guid.NewGuid(),
                    "AccountId", account);
            }

            await cache.CacheReferenceDateRepository.UpsertReferenceDateAsync(tenant, modelGuid, latest);
            var before = await CacheSnapshotAsync(database, server);

            var result = await RunAsync();

            result.Matched.Should().Be(30);
            var after = await CacheSnapshotAsync(database, server);
            after.Keys.Should().BeEquivalentTo(before.Keys);
            foreach (var (key, value) in before)
            {
                after[key].Should().Equal(value, key);
            }
        }

        [Fact]
        public async Task TwoStartersNeverClaimTheSameInstanceAsync()
        {
            await SeedAsync(1, 20, i => SecondsAfterBase(i));
            var first = await CreateInstanceAsync("Return False");
            var second = await CreateInstanceAsync("Return False");

            var results = await Task.WhenAll(TryRunAsync(), TryRunAsync());
            var third = await TryRunAsync();

            var claimed = results.Where(r => r != null).Select(r => r!.EntityAnalysisModelsReprocessingRuleInstanceId)
                .Concat(third == null ? [] : [third.EntityAnalysisModelsReprocessingRuleInstanceId]).ToList();
            claimed.Should().BeEquivalentTo([first, second]);
            (await InstanceAsync(first)).StatusId.Should().Be(4);
            (await InstanceAsync(second)).StatusId.Should().Be(4);
        }
    }
}
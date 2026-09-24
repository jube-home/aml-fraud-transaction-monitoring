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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using LinqToDB.Data;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Load
{
    [Trait("Category", "Load")]
    [Collection("Database")]
    public sealed class ReprocessingLoadTests(ReprocessingLoadFixture fixture, ITestOutputHelper output)
        : IClassFixture<ReprocessingLoadFixture>, IAsyncLifetime
    {
        private const long MaxHeapGrowthBytes = 128L * 1024 * 1024;
        private const int CommandTimeoutSeconds = 3600;
        private const int Threshold = 949;

        private static readonly DateTime start = new(2026, 1, 1, 0, 0, 0);

        public Task InitializeAsync() => ClearAsync();

        public Task DisposeAsync() => ClearAsync();

        private static DbContext Db()
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(ModelScaffold.ConnectionString,
                TestLog.NoOp);
            dbContext.CommandTimeout = CommandTimeoutSeconds;
            return dbContext;
        }

        private static long Setting(string name, long fallback)
        {
            return long.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
                ? value
                : fallback;
        }

        private static long Heap()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return GC.GetTotalMemory(true);
        }

        private async Task ClearAsync()
        {
            await using var dbContext = Db();
            var modelId = new DataParameter("m", fixture.Shared.ModelId);
            await dbContext.ExecuteAsync(
                "DELETE FROM \"ArchiveKeyVersion\" v USING \"ArchiveKey\" k, \"Archive\" a WHERE v.\"ArchiveKeyId\" = " +
                "k.\"Id\" AND k.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                "AND a.\"EntityAnalysisModelId\" = @m", modelId);
            await dbContext.ExecuteAsync(
                "DELETE FROM \"ArchiveKey\" k USING \"Archive\" a WHERE k.\"EntityAnalysisModelInstanceEntryGuid\" = " +
                "a.\"EntityAnalysisModelInstanceEntryGuid\" AND a.\"EntityAnalysisModelId\" = @m", modelId);
            await dbContext.ExecuteAsync(
                "DELETE FROM \"ArchiveVersion\" v USING \"Archive\" a WHERE v.\"ArchiveId\" = a.\"Id\" AND " +
                "a.\"EntityAnalysisModelId\" = @m", modelId);
            while (await dbContext.ExecuteAsync(
                       "DELETE FROM \"Archive\" WHERE \"Id\" IN (SELECT \"Id\" FROM \"Archive\" WHERE " +
                       "\"EntityAnalysisModelId\" = @m LIMIT 250000)", modelId) > 0)
            {
            }

            await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(w => w.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId == fixture.Shared.ModelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModelReprocessingRule
                .Where(w => w.EntityAnalysisModelId == fixture.Shared.ModelId).DeleteAsync();
        }

        private async Task SeedAsync(DbContext dbContext, long count)
        {
            const long batch = 500_000;
            for (long from = 1; from <= count; from += batch)
            {
                var to = Math.Min(count, from + batch - 1);
                await dbContext.ExecuteAsync(
                    "INSERT INTO \"Archive\" (\"Json\", \"EntityAnalysisModelInstanceEntryGuid\", \"EntryKeyValue\", " +
                    "\"EntityAnalysisModelId\", \"CreatedDate\", \"ReferenceDate\", \"ActivationRuleCount\", " +
                    "\"ResponseElevation\") " +
                    "SELECT jsonb_build_object('payload', jsonb_build_object('AccountId', 'Acc' || (g % 5000), " +
                    "'TxnId', 'TX' || g, 'CurrencyAmount', (hashint4(g::int) & 1023) % 1000)), gen_random_uuid(), " +
                    "'TX' || g, @m, (now() at time zone 'utc') - interval '1 hour', @start + (g / 2) * interval '1 second', " +
                    "0, 0 FROM generate_series(@from, @to) g",
                    new DataParameter("m", fixture.Shared.ModelId), new DataParameter("start", start),
                    new DataParameter("from", from), new DataParameter("to", to));
            }

            await dbContext.ExecuteAsync("ANALYZE \"Archive\"");
        }

        [Fact]
        public async Task ReprocessingProcessesEveryArchivedDocumentOnceUpdatesEveryMatchInBoundedMemoryAsync()
        {
            var count = Setting("JubeReprocessingLoadRows", 1_000_000);
            var minimumRowsPerSecond = Setting("JubeReprocessingLoadMinRowsPerSecond", 2_000);

            await using var dbContext = Db();
            var seeding = Stopwatch.StartNew();
            await SeedAsync(dbContext, count);
            seeding.Stop();
            var expectedMatched = await dbContext.ExecuteAsync<long>(
                "SELECT COUNT(*) FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m AND " +
                "(\"Json\"->'payload'->>'CurrencyAmount')::double precision > @t",
                new DataParameter("m", fixture.Shared.ModelId), new DataParameter("t", Threshold));

            var ruleId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelReprocessingRule
            {
                EntityAnalysisModelId = fixture.Shared.ModelId, Name = "Load", RuleScriptTypeId = 2,
                CoderRuleScript = $"If (Payload.CurrencyAmount > {Threshold}) Then\n   Return True\nEnd If",
                BuilderRuleScript = "", Json = "{}", ReprocessingInterval = "y", ReprocessingValue = 100,
                ReprocessingSample = 100, Active = 1, Deleted = 0, Priority = 1, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = "Load"
            });
            var instanceId = await dbContext.InsertWithInt32IdentityAsync(
                new EntityAnalysisModelReprocessingRuleInstance
                {
                    EntityAnalysisModelReprocessingRuleId = ruleId, StatusId = 0, CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Load", Deleted = 0, Version = 1
                });

            var model = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid)!;
            var fetchTimes = new List<double>();
            var processTimes = new List<double>();
            var pageSizes = new List<int>();
            var heapSamples = new List<long>();
            var baseline = Heap();
            var starter = new ReprocessingTaskStarter(
                fixture.Engine.Engine.Context.Tasks.EntityAnalysisModelManager.Context)
            {
                PageCompleted = page =>
                {
                    fetchTimes.Add(page.Fetch.TotalMilliseconds);
                    processTimes.Add(page.Process.TotalMilliseconds);
                    pageSizes.Add(page.Documents);
                    if (page.Page % 20 == 0)
                    {
                        heapSamples.Add(Heap());
                    }
                }
            };

            var stopwatch = Stopwatch.StartNew();
            var result = await starter.RunNextAsync(dbContext,
                new KeyValuePair<int, global::Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.
                    EntityAnalysisModel>(
                    model.Instance.Id, model), CancellationToken.None);
            stopwatch.Stop();
            var peak = Math.Max(heapSamples.DefaultIfEmpty(baseline).Max(), Heap());

            var decile = Math.Max(1, fetchTimes.Count / 10);
            var earlyFetch = fetchTimes.Skip(1).Take(decile).DefaultIfEmpty(0).Average();
            var lateFetch = fetchTimes.Skip(fetchTimes.Count - decile).DefaultIfEmpty(0).Average();
            var rowsPerSecond = count / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            var updated = await dbContext.ExecuteAsync<long>(
                "SELECT COUNT(*) FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m AND " +
                "\"EntityAnalysisModelsReprocessingRuleInstanceId\" = @i AND \"Version\" = 1",
                new DataParameter("m", fixture.Shared.ModelId), new DataParameter("i", instanceId));
            var overUpdated = await dbContext.ExecuteAsync<long>(
                "SELECT COUNT(*) FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m AND \"Version\" > 1",
                new DataParameter("m", fixture.Shared.ModelId));

            output.WriteLine($"Seeded {count:N0} rows in {seeding.Elapsed}; reprocessing read {result!.Processed:N0} " +
                             $"and re-invoked {result.Matched:N0} in {stopwatch.Elapsed} ({rowsPerSecond:N0} rows/s, " +
                             $"{result.Matched / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001):N0} invokes/s) over " +
                             $"{result.Pages:N0} pages, heap growth {(peak - baseline) / 1024 / 1024:N0} MB, early " +
                             $"page fetch {earlyFetch:N0} ms, late page fetch {lateFetch:N0} ms, mean page process " +
                             $"{processTimes.DefaultIfEmpty(0).Average():N0} ms.");

            foreach (var (name, stage) in model.StagePerformanceCounters.Stages
                         .Concat(model.ArchiverStagePerformanceCounters.Stages)
                         .OrderByDescending(s => s.Value.TotalMicroseconds))
            {
                output.WriteLine($"Stage {name}: {stage.InvokeCount:N0} calls, " +
                                 $"{stage.TotalMicroseconds / Math.Max(stage.InvokeCount, 1):N0} µs mean, " +
                                 $"{stage.TotalMicroseconds / 1000:N0} ms total.");
            }

            result.Outcome.Should().Be(ReprocessingRunOutcome.Completed, result.Failure);
            result.Processed.Should().Be((int)count);
            result.Sampled.Should().Be((int)count);
            result.Matched.Should().Be((int)expectedMatched);
            result.Errors.Should().Be(0);
            result.Pages.Should()
                .Be((int)((count + ReprocessingLoadFixture.BulkLimit - 1) / ReprocessingLoadFixture.BulkLimit));
            pageSizes.Should().HaveCount(result.Pages);
            pageSizes.Sum().Should().Be((int)count);
            pageSizes.SkipLast(1).Should().OnlyContain(size => size == ReprocessingLoadFixture.BulkLimit,
                "every page but the last must be a full page, or keyset paging is skipping or repeating rows");
            updated.Should().Be(expectedMatched);
            overUpdated.Should().Be(0);
            (peak - baseline).Should().BeLessThan(MaxHeapGrowthBytes);
            rowsPerSecond.Should().BeGreaterThanOrEqualTo(minimumRowsPerSecond);
            lateFetch.Should().BeLessThanOrEqualTo(Math.Max(earlyFetch * 3, 50),
                "reading a later page of the archive must not get slower as reprocessing goes deeper");

            var instance =
                await dbContext.EntityAnalysisModelReprocessingRuleInstance.SingleAsync(w => w.Id == instanceId);
            instance.StatusId.Should().Be(4);
            instance.ProcessedCount.Should().Be(count);
            instance.MatchedCount.Should().Be(expectedMatched);
        }
    }
}
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.QueryBuilder;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.RuleParser;
using LinqToDB;
using LinqToDB.Data;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Jube.Test.Load.Models;

namespace Jube.Test.Load
{
    [Trait("Category", "Load")]
    [Collection("Database")]
    public sealed class BacktestLoadTests(DatabaseFixture fx, ITestOutputHelper output) : IAsyncLifetime
    {
        private const int PageSize = 5000;
        private const long MaxHeapGrowthBytes = 128L * 1024 * 1024;
        private const int CommandTimeoutSeconds = 3600;

        private const string Rule = "If (Payload.Amount > 500) Then\n   Return True\nEnd If";

        private const string Fraud =
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Tag.Fraud\",\"operator\":\"equal\",\"value\":\"True\"}]}";

        private static readonly DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private int modelId;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (modelId == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();
            dbContext.CommandTimeout = CommandTimeoutSeconds;
            await dbContext.ExecuteAsync(
                "DELETE FROM \"ArchiveTag\" WHERE \"EntityAnalysisModelInstanceEntryGuid\" IN " +
                "(SELECT \"EntityAnalysisModelInstanceEntryGuid\" FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m)",
                new DataParameter("m", modelId));
            while (await dbContext.ExecuteAsync(
                       "DELETE FROM \"Archive\" WHERE \"Id\" IN (SELECT \"Id\" FROM \"Archive\" WHERE " +
                       "\"EntityAnalysisModelId\" = @m LIMIT 250000)", new DataParameter("m", modelId)) > 0)
            {
            }

            await dbContext.GetTable<EntityAnalysisModelTag>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
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

        private static RuleParseResult Parse(string text)
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = [];
            environment.AbstractionRules = [];
            environment.Sanctions = [];
            environment.AbstractionCalculations = [];
            environment.HttpAdaptations = [];
            environment.ExhaustiveAdaptations = [];
            environment.ActivationRules = [];
            var parsed = RuleParse.Execute(text, RuleParse.ActivationRule, environment, TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.ActivationRule), engineWrap: true);
            parsed.Compiled.Should().BeTrue(parsed.Message);
            return parsed;
        }

        private static IEnumerable<List<BacktestRow>> Pages(long count, Random random, BacktestLoadExpected expected)
        {
            var page = new List<BacktestRow>(PageSize);
            for (long i = 0; i < count; i++)
            {
                var amount = random.Next(0, 1000);
                var fraud = random.NextDouble() < (amount > 500 ? 0.3 : 0.02);
                var reviewed = random.NextDouble() < 0.05;
                var archive = new JObject
                {
                    ["entityInstanceEntryId"] = "TX" + i,
                    ["payload"] = new JObject
                    {
                        ["Amount"] = amount,
                        ["Country"] = random.Next(0, 4) switch { 0 => "GB", 1 => "FR", 2 => "US", _ => "DE" },
                        ["Narrative"] = "payment " + random.Next(0, 100_000).ToString(CultureInfo.InvariantCulture)
                    }
                };
                var tags = new List<string>(2);
                if (fraud)
                {
                    tags.Add("Fraud");
                }

                if (reviewed)
                {
                    tags.Add("Reviewed");
                }

                expected.Add(amount > 500, fraud);
                page.Add(new BacktestRow(Guid.NewGuid(), "TX" + i, start.AddSeconds(i), archive, tags));
                if (page.Count == PageSize)
                {
                    yield return page;
                    page = new List<BacktestRow>(PageSize);
                }
            }

            if (page.Count > 0)
            {
                yield return page;
            }
        }

        [Fact]
        public async Task TheBacktestCountsMillionsOfGeneratedRowsExactlyInBoundedMemoryAsync()
        {
            var count = Setting("JubeBacktestLoadRows", 1_000_000);
            var minimumRowsPerSecond = Setting("JubeBacktestLoadMinRowsPerSecond", 5_000);
            var catalogue = new Dictionary<string, BuilderField>
            {
                ["Tag.Fraud"] = new("Tag.Fraud", BuilderFieldType.Boolean)
            };
            var parsedClass = BuilderProfile.Parse(Fraud, catalogue);
            var backtest = new RuleBacktest(new BacktestPlan(1, Parse(Rule), RuleParse.ActivationRule, false, null,
                BuilderFilter.CompileValues(parsedClass.Group), ["Fraud", "Reviewed"],
                [new InvocationContextField("Payload.Amount", "Payload", "double")], [], 10));
            var expected = new BacktestLoadExpected();

            var baseline = Heap();
            var peak = baseline;
            var pages = 0;
            var stopwatch = Stopwatch.StartNew();
            foreach (var page in Pages(count, new Random(42), expected))
            {
                (await backtest.AddAsync(page)).Should().BeTrue();
                if (++pages % 100 == 0)
                {
                    peak = Math.Max(peak, Heap());
                }
            }

            stopwatch.Stop();
            peak = Math.Max(peak, Heap());
            var rowsPerSecond = count / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            output.WriteLine($"In memory: {count:N0} rows in {stopwatch.Elapsed} ({rowsPerSecond:N0} rows/s), " +
                             $"heap growth {(peak - baseline) / 1024 / 1024:N0} MB over {pages:N0} pages.");

            var result = backtest.Result;
            result.Scanned.Should().Be(count);
            result.Evaluated.Should().Be(count);
            (result.TruePositives, result.FalsePositives, result.FalseNegatives, result.TrueNegatives).Should()
                .Be((expected.TruePositives, expected.FalsePositives, expected.FalseNegatives, expected.TrueNegatives));
            result.TruePositiveSamples.Should().HaveCount(10);
            result.TagsInSample.Keys.Should().BeEquivalentTo("Fraud", "Reviewed");
            (peak - baseline).Should().BeLessThan(MaxHeapGrowthBytes);
            rowsPerSecond.Should().BeGreaterThanOrEqualTo(minimumRowsPerSecond);
        }

        [Fact]
        public async Task TheBacktestReadsTheArchivePageByPageAndCountsExactlyInBoundedMemoryAsync()
        {
            var count = Setting("JubeBacktestLoadDatabaseRows", 200_000);
            var minimumRowsPerSecond = Setting("JubeBacktestLoadDatabaseMinRowsPerSecond", 2_000);

            await using var dbContext = fx.GetDbContext();
            dbContext.CommandTimeout = CommandTimeoutSeconds;
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Load{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            modelId = model.Id;
            foreach (var (name, dataTypeId) in new[] { ("Amount", 3), ("Country", 1) })
            {
                await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = modelId, Name = name, DataTypeId = dataTypeId, XPath = $"$.{name}",
                    Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            }

            await dbContext.InsertAsync(new EntityAnalysisModelTag
            {
                EntityAnalysisModelId = modelId, Name = "Fraud", Active = 1, Deleted = 0, Guid = Guid.NewGuid()
            });

            var seeding = Stopwatch.StartNew();
            await SeedAsync(dbContext, count);
            seeding.Stop();
            var positives = await dbContext.ArchiveTag.CountAsync(t =>
                t.Name == "Fraud" && dbContext.Archive.Any(a =>
                    a.EntityAnalysisModelId == modelId &&
                    a.EntityAnalysisModelInstanceEntryGuid == t.EntityAnalysisModelInstanceEntryGuid));
            var fired = await dbContext.ExecuteAsync<long>(
                "SELECT COUNT(*) FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m " +
                "AND (\"Json\"->'payload'->>'Amount')::double precision > 500", new DataParameter("m", modelId));
            var truePositives = await dbContext.ExecuteAsync<long>(
                "SELECT COUNT(*) FROM \"Archive\" a JOIN \"ArchiveTag\" t ON t.\"EntityAnalysisModelInstanceEntryGuid\" " +
                "= a.\"EntityAnalysisModelInstanceEntryGuid\" AND t.\"Name\" = 'Fraud' WHERE a.\"EntityAnalysisModelId\" " +
                "= @m AND (a.\"Json\"->'payload'->>'Amount')::double precision > 500", new DataParameter("m", modelId));

            var baseline = Heap();
            var peak = baseline;
            var pages = 0;
            var pageTimes = new List<double>();
            var stopwatch = Stopwatch.StartNew();
            var lastPage = TimeSpan.Zero;
            var execution = await BacktestExecutor.ExecuteAsync(dbContext, model.TenantRegistryId!.Value,
                new BacktestSpecification(modelId, "ActivationRule", Rule, null, null, Fraud, null, null, count, 10),
                PageSize, _ =>
                {
                    var now = stopwatch.Elapsed;
                    pageTimes.Add((now - lastPage).TotalMilliseconds);
                    if (++pages % 20 == 0)
                    {
                        peak = Math.Max(peak, Heap());
                    }

                    lastPage = stopwatch.Elapsed;
                    return Task.FromResult(true);
                });
            stopwatch.Stop();
            var decile = Math.Max(1, pageTimes.Count / 10);
            var earlyPages = pageTimes.Skip(1).Take(decile).DefaultIfEmpty(0).Average();
            var latePages = pageTimes.Skip(pageTimes.Count - decile).DefaultIfEmpty(0).Average();
            peak = Math.Max(peak, Heap());
            var rowsPerSecond = count / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            output.WriteLine($"Database: seeded {count:N0} rows in {seeding.Elapsed}; backtest read {count:N0} rows " +
                             $"in {stopwatch.Elapsed} ({rowsPerSecond:N0} rows/s) over {pages:N0} pages, heap " +
                             $"growth {(peak - baseline) / 1024 / 1024:N0} MB, early pages {earlyPages:N0} ms, " +
                             $"late pages {latePages:N0} ms.");

            execution.Valid.Should().BeTrue(string.Join("; ", execution.Errors.Select(e => e.Message)));
            var result = execution.Result;
            result.Scanned.Should().Be(count);
            pages.Should().Be((int)((count + PageSize - 1) / PageSize));
            result.Fired.Should().Be(fired);
            result.Positives.Should().Be(positives);
            result.TruePositives.Should().Be(truePositives);
            (result.TruePositives + result.FalsePositives + result.FalseNegatives + result.TrueNegatives).Should()
                .Be(count);
            (peak - baseline).Should().BeLessThan(MaxHeapGrowthBytes);
            rowsPerSecond.Should().BeGreaterThanOrEqualTo(minimumRowsPerSecond);
            latePages.Should().BeLessThanOrEqualTo(Math.Max(earlyPages * 3, 50),
                "reading a later page of the archive must not get slower as the backtest goes deeper");
        }

        private async Task SeedAsync(Data.Context.DbContext dbContext, long count)
        {
            const long batch = 500_000;
            for (long from = 1; from <= count; from += batch)
            {
                var to = Math.Min(count, from + batch - 1);
                await dbContext.ExecuteAsync(
                    "INSERT INTO \"Archive\" (\"Json\", \"EntityAnalysisModelInstanceEntryGuid\", \"EntryKeyValue\", " +
                    "\"EntityAnalysisModelId\", \"CreatedDate\", \"ReferenceDate\", \"ActivationRuleCount\", " +
                    "\"ResponseElevation\") " +
                    "SELECT jsonb_build_object('entityInstanceEntryId', 'TX' || g, 'payload', jsonb_build_object(" +
                    "'Amount', (hashint4(g::int) & 1023) % 1000, 'Country', (ARRAY['GB','FR','US','DE'])[1 + " +
                    "(hashint4(g::int + 7) & 3)])), gen_random_uuid(), 'TX' || g, @m, now() at time zone 'utc', " +
                    "@start + g * interval '1 second', 0, 0 FROM generate_series(@from, @to) g",
                    new DataParameter("m", modelId), new DataParameter("start", start),
                    new DataParameter("from", from), new DataParameter("to", to));
            }

            await dbContext.ExecuteAsync(
                "INSERT INTO \"ArchiveTag\" (\"EntityAnalysisModelInstanceEntryGuid\", \"Name\", \"Version\", " +
                "\"CreatedUser\", \"CreatedDate\", \"Deleted\") SELECT \"EntityAnalysisModelInstanceEntryGuid\", " +
                "'Fraud', 1, 'load', now() at time zone 'utc', 0 FROM \"Archive\" WHERE \"EntityAnalysisModelId\" = @m " +
                "AND (hashint4(\"Id\"::int) & 15) < CASE WHEN (\"Json\"->'payload'->>'Amount')::int > 500 THEN 5 ELSE 1 END",
                new DataParameter("m", modelId));
            await dbContext.ExecuteAsync("ANALYZE \"Archive\"");
        }
    }
}
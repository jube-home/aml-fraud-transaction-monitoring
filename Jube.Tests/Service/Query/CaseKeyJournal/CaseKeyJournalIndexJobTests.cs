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
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Dto.Options;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseKeyJournalIndexJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private readonly JournalJobsSeed seed = new(fx);
        private readonly List<string> keys = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var recorded = await dbContext.CaseKeyJournalKeyIndex.Select(w => w.KeyName).ToListAsync();

            foreach (var key in keys.Concat(recorded).Distinct())
            {
                var name = CaseKeyJournalIndexJob.IndexName(key);

                await dbContext.ExecuteAsync(
                    "do $$ begin " +
                    $"if exists (select 1 from pg_class where relname = '{name}' and relkind = 'r') then drop table \"{name}\"; " +
                    $"elsif exists (select 1 from pg_class where relname = '{name}' and relkind = 'i') then drop index \"{name}\"; " +
                    "end if; end $$");
                await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == key).DeleteAsync();
            }

            await seed.CleanupAsync(dbContext);
        }

        private static CaseKeyJournalIndexJob Job(int minimumRows = 0, int maximum = 1000)
        {
            return new CaseKeyJournalIndexJob(connectionString, null, TestLog.NoOp, minimumRows, maximum);
        }

        private async Task<string> SeedKeyAsync(DbContext dbContext, string? key = null, bool drill = false)
        {
            key ??= $"{DatabaseFixture.Prefix}Idx{Guid.NewGuid():N}"[..32];
            keys.Add(key);

            var (_, workflowId, workflow) = await seed.WorkflowAsync(dbContext);

            if (drill)
            {
                await seed.XPathAsync(dbContext, workflowId, key, true);
            }
            else
            {
                await seed.CaseAsync(dbContext, workflow, key);
            }

            return key;
        }

        private static Task<bool> IndexExistsAsync(DbContext dbContext, string key)
        {
            return dbContext.QueryToArrayAsync<bool>(
                    $"select exists (select 1 from pg_indexes where indexname = '{CaseKeyJournalIndexJob.IndexName(key)}' " +
                    "and indexdef like '%EntityAnalysisModelId%')")
                .ContinueWith(task => task.Result[0]);
        }

        [Fact]
        public async Task BuildsTheIndexForACaseKeyAndRecordsItAsReadyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);

            var result = await Job().RunOnceAsync();

            result.Created.Should().Contain(key);
            result.Skipped.Should().BeNull();
            (await IndexExistsAsync(dbContext, key)).Should().BeTrue();

            var row = await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == key);

            row.StateId.Should().Be((byte)CaseKeyJournalKeyIndexState.Ready);
            row.IndexName.Should().Be(CaseKeyJournalIndexJob.IndexName(key));
            row.AttemptCount.Should().Be(1);
            row.ReadyDate.Should().NotBeNull();
            row.Error.Should().BeNull();
        }

        [Fact]
        public async Task DrillFieldsOfAWorkflowAreIndexedToo()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext, drill: true);

            (await Job().RunOnceAsync()).Created.Should().Contain(key);
            (await IndexExistsAsync(dbContext, key)).Should().BeTrue();
        }

        [Fact]
        public async Task ARunAgainBuildsNothingNewAndKeepsTheIndexAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);

            await Job().RunOnceAsync();
            var again = await Job().RunOnceAsync();

            again.Created.Should().NotContain(key);
            again.Failed.Should().NotContain(key);
            again.Lost.Should().NotContain(key);
            (await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == key)).AttemptCount.Should().Be(1);
        }

        [Fact]
        public async Task NothingIsBuiltBeforeTheArchiveIsLargeEnoughAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);

            var result = await Job(minimumRows: int.MaxValue).RunOnceAsync();

            result.Skipped.Should().Be("BelowMinimumRows");
            result.Created.Should().BeEmpty();
            (await IndexExistsAsync(dbContext, key)).Should().BeFalse();
            (await dbContext.CaseKeyJournalKeyIndex.CountAsync(w => w.KeyName == key)).Should().Be(0);
        }

        [Fact]
        public async Task KeysThatAreNotSafeInAnIndexDefinitionAreNeverIndexedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var unsafeKey = await SeedKeyAsync(dbContext, "it's;drop table \"Archive\"");

            var result = await Job().RunOnceAsync();

            result.Created.Should().NotContain(unsafeKey);
            result.Failed.Should().NotContain(unsafeKey);
            (await dbContext.CaseKeyJournalKeyIndex.CountAsync(w => w.KeyName == unsafeKey)).Should().Be(0);
        }

        [Fact]
        public async Task NoMoreThanTheMaximumNumberOfIndexesIsHeldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedKeyAsync(dbContext, $"{DatabaseFixture.Prefix}IdxA{Guid.NewGuid():N}"[..32]);
            await SeedKeyAsync(dbContext, $"{DatabaseFixture.Prefix}IdxB{Guid.NewGuid():N}"[..32]);
            var held = await dbContext.CaseKeyJournalKeyIndex.CountAsync(
                w => w.StateId == (byte)CaseKeyJournalKeyIndexState.Ready);

            var result = await Job(maximum: held + 1).RunOnceAsync();

            result.Skipped.Should().Be("MaximumIndexes");
            result.Created.Should().HaveCount(1);
        }

        [Fact]
        public async Task AnIndexThatHasBeenDroppedIsReportedLostAndRebuiltOnceTheRetryTimeHasPassedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);

            await Job().RunOnceAsync();
            await dbContext.ExecuteAsync($"drop index \"{CaseKeyJournalIndexJob.IndexName(key)}\"");

            var lost = await Job().RunOnceAsync();

            lost.Lost.Should().Contain(key);
            lost.Created.Should().NotContain(key);
            (await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == key)).StateId.Should()
                .Be((byte)CaseKeyJournalKeyIndexState.Failed);

            await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == key)
                .Set(w => w.UpdatedDate, DateTime.UtcNow.AddHours(-2)).UpdateAsync();

            (await Job().RunOnceAsync()).Created.Should().Contain(key);
            (await IndexExistsAsync(dbContext, key)).Should().BeTrue();
        }

        [Fact]
        public async Task ABuildLeftBuildingIsRetriedOnlyOnceItLooksAbandonedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);
            await new Jube.Data.Repository.CaseKeyJournalKeyIndexRepository(dbContext).MarkAsync(key,
                CaseKeyJournalIndexJob.IndexName(key), CaseKeyJournalKeyIndexState.Building, null, true);

            (await Job().RunOnceAsync()).Created.Should().NotContain(key);

            await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == key)
                .Set(w => w.UpdatedDate, DateTime.UtcNow.AddDays(-2)).UpdateAsync();

            (await Job().RunOnceAsync()).Created.Should().Contain(key);
            (await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == key)).AttemptCount.Should().Be(2);
        }

        [Fact]
        public async Task ABuildThatCannotRunIsRecordedAsFailedWithWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = await SeedKeyAsync(dbContext);
            await dbContext.ExecuteAsync($"create table \"{CaseKeyJournalIndexJob.IndexName(key)}\" (\"x\" int)");

            var result = await Job().RunOnceAsync();

            result.Failed.Should().Contain(key);

            var row = await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == key);

            row.StateId.Should().Be((byte)CaseKeyJournalKeyIndexState.Failed);
            row.Error.Should().NotBeNullOrEmpty();
            (await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == key).Select(w => w.ReadyDate)
                .FirstAsync()).Should().BeNull();
        }

        [Theory]
        [InlineData("ACC-1")]
        [InlineData("123")]
        [InlineData("007")]
        [InlineData("1.50")]
        [InlineData("True")]
        [InlineData("missing")]
        public async Task TheLookupFindsTheSameTransactionsWithAndWithoutTheIndexAsync(string value)
        {
            await using var dbContext = fx.GetDbContext();
            var (model, _, _) = await seed.WorkflowAsync(dbContext);
            var key = "AccountId";
            var payloads = new object[] { "ACC-1", "123", 123, "007", 7, 1.5, "1.50", true, "True", "other" };

            foreach (var payload in payloads)
            {
                await seed.ArchiveAsync(dbContext, model, new JObject { [key] = JToken.FromObject(payload) });
            }

            var query = new GetCaseKeyJournalRowsQuery(connectionString, TestLog.NoOp);
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow.AddMinutes(5);

            var plain = await query.ExecuteAsync(model, seed.Me, key, value, from, to, 0, 0, 100);
            var indexed = await query.ExecuteAsync(model, seed.Me, key, value, from, to, 0, 0, 100,
                keyIndexed: true);

            indexed.Select(row => row.Id).Should().Equal(plain.Select(row => row.Id));
        }

        [Fact]
        public async Task TextEqualityAndContainmentFindTheSameStringAndWholeNumberRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, _, _) = await seed.WorkflowAsync(dbContext);

            foreach (var payload in new object[] { "ACC-1", "123", 123, "other" })
            {
                await seed.ArchiveAsync(dbContext, model, new JObject { ["AccountId"] = JToken.FromObject(payload) });
            }

            var query = new GetCaseKeyJournalRowsQuery(connectionString, TestLog.NoOp);
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow.AddMinutes(5);

            (await query.ExecuteAsync(model, seed.Me, "AccountId", "ACC-1", from, to, 0, 0, 100, keyIndexed: true))
                .Should().ContainSingle();
            (await query.ExecuteAsync(model, seed.Me, "AccountId", "123", from, to, 0, 0, 100, keyIndexed: true))
                .Should().HaveCount(2);
            (await query.ExecuteAsync(model, seed.Me, "AccountId", "nope", from, to, 0, 0, 100, keyIndexed: true))
                .Should().BeEmpty();
        }
    }
}

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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.CaseKeyJournal;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Newtonsoft.Json.Linq;
using Npgsql;
using Xunit;

namespace Jube.Test.Migrations
{
    [Collection("Database")]
    [Trait("Category", "Service")]
    public sealed class MigrateCaseKeyJournalIndexesToArchiveKeyScriptTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string ScriptPath = "Jube.Migrations/Scripts/MigrateCaseKeyJournalIndexesToArchiveKey.sql";
        private readonly JournalJobsSeed seed = new(fx);
        private int model;
        private string accountKey = null!;
        private string journalIndexName = null!;
        private Guid full;
        private Guid badCount;
        private Guid alreadyKeyed;
        private Guid duplicated;
        private Guid young;
        private Guid stringFlag;
        private long alreadyKeyedId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ExecuteAsync($"drop index if exists \"{ArchiveKeyRepository.RequestKeyIndexName}\"");
            (model, _, _) = await seed.WorkflowAsync(dbContext);
            accountKey = JournalJobsSeed.NewKey();

            await seed.RequestXPathAsync(dbContext, model, accountKey, 1, true);
            await seed.RequestXPathAsync(dbContext, model, "Amount", 3, true);
            await seed.RequestXPathAsync(dbContext, model, "Count", 2, true);
            await seed.RequestXPathAsync(dbContext, model, "When", 4, true);
            await seed.RequestXPathAsync(dbContext, model, "Flag", 5, true);
            await seed.RequestXPathAsync(dbContext, model, "NotReported", 1, false);

            var old = DateTime.UtcNow.AddDays(-1);

            full = await seed.ArchiveAsync(dbContext, model, Payload("ACC-1", 12.5, "7", true), old, false);
            badCount = await seed.ArchiveAsync(dbContext, model, Payload("ACC-2", 1, "abc", false), old, false);
            alreadyKeyed = await seed.ArchiveAsync(dbContext, model, Payload("ACC-3", 2, "1", true), old, false);
            duplicated = await seed.ArchiveAsync(dbContext, model, Payload("ACC-4", 3, "2", true), old, false);

            var flagged = Payload("ACC-6", 5, "4", true);
            flagged["Flag"] = "True";
            stringFlag = await seed.ArchiveAsync(dbContext, model, flagged, old, false);
            young = await seed.ArchiveAsync(dbContext, model, Payload("ACC-5", 4, "3", true), null, false);

            alreadyKeyedId = await dbContext.InsertWithInt64IdentityAsync(Key(alreadyKeyed, accountKey, "ACC-3"));
            await dbContext.InsertWithInt64IdentityAsync(Key(duplicated, accountKey, "ACC-4"));
            await dbContext.InsertWithInt64IdentityAsync(Key(duplicated, accountKey, "ACC-4"));

            journalIndexName = "IX_Archive_Journal_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(accountKey)))[..16].ToLowerInvariant();
            await dbContext.ExecuteAsync(
                $"create index if not exists \"{journalIndexName}\" on \"Archive\" (\"EntityAnalysisModelId\", " +
                $"((\"Json\" -> 'payload' ->> '{accountKey}')), \"Id\" desc)");
            await new CaseKeyJournalKeyIndexRepository(dbContext).MarkAsync(accountKey, journalIndexName,
                CaseKeyJournalKeyIndexState.Ready, null, true);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ExecuteAsync($"drop index if exists \"{journalIndexName}\"");
            await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == accountKey).DeleteAsync();
            await dbContext.ExecuteAsync(
                "delete from \"ArchiveKeyBackfillKey\" where \"RunId\" in (select \"Id\" from \"ArchiveKeyBackfillRun\" where \"ModelIds\" = @m)",
                new DataParameter("m", model.ToString(), DataType.Text));
            await dbContext.ExecuteAsync("delete from \"ArchiveKeyBackfillRun\" where \"ModelIds\" = @m",
                new DataParameter("m", model.ToString(), DataType.Text));
            await seed.CleanupAsync(dbContext);
        }

        [Fact]
        public async Task TheScriptBackfillsValidatesAndRetiresWithoutTouchingWhatTheEngineWroteAsync()
        {
            await RunScriptAsync();

            await using var dbContext = fx.GetDbContext();
            var seeded = new List<Guid> { full, badCount, alreadyKeyed, duplicated, young, stringFlag };
            var keys = await dbContext.ArchiveKey
                .Where(k => seeded.Contains(k.EntityAnalysisModelInstanceEntryGuid))
                .ToListAsync();

            keys.Should().OnlyContain(k => k.ProcessingTypeId == 1 && k.Version == 1);
            keys.Should().NotContain(k => k.Key == "NotReported");
            keys.Should().NotContain(k => k.EntityAnalysisModelInstanceEntryGuid == young);

            var fullKeys = keys.Where(k => k.EntityAnalysisModelInstanceEntryGuid == full)
                .ToDictionary(k => k.Key);
            fullKeys.Should().HaveCount(5);
            fullKeys[accountKey].KeyValueString.Should().Be("ACC-1");
            fullKeys["Amount"].KeyValueFloat.Should().Be(12.5);
            fullKeys["Count"].KeyValueInteger.Should().Be(7);
            fullKeys["Flag"].KeyValueBoolean.Should().Be(1);
            fullKeys["When"].KeyValueDate.Should()
                .BeCloseTo(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567), TimeSpan.FromMilliseconds(1));

            keys.Where(k => k.EntityAnalysisModelInstanceEntryGuid == badCount).Select(k => k.Key)
                .Should().BeEquivalentTo(accountKey, "Amount", "When", "Flag");

            var kept = keys.Single(k => k.EntityAnalysisModelInstanceEntryGuid == alreadyKeyed && k.Key == accountKey);
            kept.Id.Should().Be(alreadyKeyedId);
            kept.KeyValueString.Should().Be("ACC-3");

            keys.Count(k => k.EntityAnalysisModelInstanceEntryGuid == duplicated && k.Key == accountKey).Should().Be(1);

            keys.Single(k => k.EntityAnalysisModelInstanceEntryGuid == stringFlag && k.Key == "Flag").KeyValueBoolean
                .Should().Be(1);

            var register = await dbContext.CaseKeyJournalKeyIndex.SingleAsync(w => w.KeyName == accountKey);
            register.StateId.Should().Be((byte)CaseKeyJournalKeyIndexState.Retired);

            var indexState = await dbContext.QueryToListAsync<IndexState>(
                "select i.indisvalid as \"Valid\", obj_description(c.oid, 'pg_class') as \"Comment\" " +
                "from pg_class c join pg_index i on i.indexrelid = c.oid where c.relname = @name",
                new DataParameter("name", journalIndexName, DataType.Text));
            indexState.Should().ContainSingle().Which.Comment.Should().StartWith("Retired ");

            var unique = await dbContext.QueryToListAsync<IndexState>(
                "select i.indisvalid as \"Valid\", null::text as \"Comment\" from pg_class c " +
                "join pg_index i on i.indexrelid = c.oid where c.relname = @name",
                new DataParameter("name", ArchiveKeyRepository.RequestKeyIndexName, DataType.Text));
            unique.Should().ContainSingle().Which.Valid.Should().BeTrue();

            var progress = await dbContext.QueryToListAsync<KeyProgress>(
                "select k.\"Key\" as \"KeyName\", k.\"CastFailed\", k.\"ArchiveWithKeyCount\", k.\"ArchiveKeyCount\", " +
                "k.\"SampleMismatched\", k.\"StateId\" from \"ArchiveKeyBackfillKey\" k " +
                "join \"ArchiveKeyBackfillRun\" r on r.\"Id\" = k.\"RunId\" where r.\"ModelIds\" = @m",
                new DataParameter("m", model.ToString(), DataType.Text));
            progress.Should().HaveCount(5);
            progress.Should().OnlyContain(p => p.StateId == 3 && p.SampleMismatched == 0
                                              && p.ArchiveWithKeyCount - p.CastFailed == p.ArchiveKeyCount);
            progress.Single(p => p.KeyName == "Count").CastFailed.Should().Be(1);

            var runState = await dbContext.ExecuteAsync<short>(
                "select \"StateId\" from \"ArchiveKeyBackfillRun\" where \"ModelIds\" = @m order by \"Id\" desc limit 1",
                new DataParameter("m", model.ToString(), DataType.Text));
            runState.Should().Be(4);

            var before = keys.Count;
            await RunScriptAsync();
            (await dbContext.ArchiveKey.CountAsync(k => seeded.Contains(k.EntityAnalysisModelInstanceEntryGuid)))
                .Should().Be(before);
        }

        private async Task RunScriptAsync()
        {
            var script = await File.ReadAllTextAsync(Path.Combine(SqlScriptSteps.SolutionDirectory(), ScriptPath));

            await using var connection = new NpgsqlConnection(fx.ConnectionString);
            await connection.OpenAsync();

            await using (var settings = new NpgsqlCommand(
                             "select set_config('jube.backfill_model_ids', @m, false), " +
                             "set_config('jube.backfill_settle', '1 hour', false), " +
                             "set_config('jube.backfill_batch_size', '1000', false), " +
                             "set_config('jube.backfill_pause_seconds', '0', false), " +
                             "set_config('jube.backfill_sample_percent', '100', false)", connection))
            {
                settings.Parameters.AddWithValue("m", model.ToString());
                await settings.ExecuteNonQueryAsync();
            }

            foreach (var (_, sql) in SqlScriptSteps.Split(script))
            {
                await using var command = new NpgsqlCommand(sql, connection);
                command.CommandTimeout = 0;
                await command.ExecuteNonQueryAsync();
            }
        }

        private JObject Payload(string account, double amount, string count, bool flag)
        {
            return new JObject
            {
                [accountKey] = account,
                ["Amount"] = amount,
                ["Count"] = int.TryParse(count, out var parsed) ? parsed : count,
                ["When"] = "2026-01-02T03:04:05.1234567Z",
                ["Flag"] = flag,
                ["NotReported"] = "x"
            };
        }

        private static ArchiveKey Key(Guid guid, string key, string value)
        {
            return new ArchiveKey
            {
                ProcessingTypeId = 1, Key = key, KeyValueString = value, EntityAnalysisModelInstanceEntryGuid = guid,
                Version = 1
            };
        }

        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private sealed class IndexState
        {
            public bool Valid { get; set; }
            public string? Comment { get; set; }
        }

        private sealed class KeyProgress
        {
            public string KeyName { get; set; } = null!;
            public long CastFailed { get; set; }
            public long ArchiveWithKeyCount { get; set; }
            public long ArchiveKeyCount { get; set; }
            public int SampleMismatched { get; set; }
            public short StateId { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}

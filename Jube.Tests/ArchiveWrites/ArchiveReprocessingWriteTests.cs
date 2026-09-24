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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.ArchiveWrites
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ArchiveReprocessingWriteTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<Guid> guids = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            foreach (var guid in guids)
            {
                var keyIds = await dbContext.ArchiveKey.Where(k => k.EntityAnalysisModelInstanceEntryGuid == guid)
                    .Select(k => k.Id).ToListAsync();
                await dbContext.ArchiveKeyVersion.Where(v => keyIds.Contains(v.ArchiveKeyId)).DeleteAsync();
                await dbContext.ArchiveKey.Where(k => k.EntityAnalysisModelInstanceEntryGuid == guid).DeleteAsync();
                var archiveIds = await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid)
                    .Select(a => a.Id).ToListAsync();
                await dbContext.ArchiveVersion.Where(v => archiveIds.Contains(v.ArchiveId)).DeleteAsync();
                await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid).DeleteAsync();
            }
        }

        private async Task<Guid> ArchiveAsync(params ArchiveKey[] keys)
        {
            var guid = Guid.NewGuid();
            guids.Add(guid);
            await using var dbContext = fx.GetDbContext();
            await dbContext.InsertAsync(new Archive
            {
                Json = "{\"payload\":{\"a\":1}}", EntityAnalysisModelInstanceEntryGuid = guid, EntryKeyValue = "E1",
                ResponseElevation = 1, ActivationRuleCount = 1, CreatedDate = DateTime.UtcNow.AddDays(-1),
                ReferenceDate = DateTime.UtcNow.AddDays(-1)
            });
            foreach (var key in keys)
            {
                key.EntityAnalysisModelInstanceEntryGuid = guid;
                key.Version = 1;
                await dbContext.InsertAsync(key);
            }

            return guid;
        }

        private static ArchiveKey Key(string name, string value, byte type = 1)
        {
            return new ArchiveKey { ProcessingTypeId = type, Key = name, KeyValueString = value };
        }

        private async Task<List<ArchiveKey>> KeysAsync(Guid guid)
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.ArchiveKey.Where(k => k.EntityAnalysisModelInstanceEntryGuid == guid)
                .OrderBy(k => k.Key).ToListAsync();
        }

        private async Task<List<ArchiveKeyVersion>> KeyVersionsAsync(Guid guid)
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.ArchiveKeyVersion.Where(k => k.EntityAnalysisModelInstanceEntryGuid == guid)
                .ToListAsync();
        }

        private async Task ReplaceAsync(Guid guid, params ArchiveKey[] keys)
        {
            await using var dbContext = fx.GetDbContext();
            await new ArchiveKeyRepository(dbContext).ReplaceAsync(guid, keys, 42);
        }

        [Fact]
        public async Task UnchangedKeysAreLeftAloneWithoutAVersionAsync()
        {
            var guid = await ArchiveAsync(Key("A", "1"), Key("B", "2"));
            var before = await KeysAsync(guid);

            await ReplaceAsync(guid, Key("A", "1"), Key("B", "2"));

            var after = await KeysAsync(guid);
            after.Select(k => (k.Id, k.Version, k.KeyValueString)).Should()
                .Equal(before.Select(k => (k.Id, k.Version, k.KeyValueString)));
            (await KeyVersionsAsync(guid)).Should().BeEmpty();
        }

        [Fact]
        public async Task AChangedKeyIsVersionedAndUpdatedInPlaceAsync()
        {
            var guid = await ArchiveAsync(Key("A", "1"), Key("B", "2"));
            var original = (await KeysAsync(guid)).Single(k => k.Key == "A");

            await ReplaceAsync(guid, Key("A", "9"), Key("B", "2"));

            var updated = (await KeysAsync(guid)).Single(k => k.Key == "A");
            updated.Id.Should().Be(original.Id);
            updated.KeyValueString.Should().Be("9");
            updated.Version.Should().Be(2);
            updated.EntityAnalysisModelsReprocessingRuleInstanceId.Should().Be(42);
            var version = (await KeyVersionsAsync(guid)).Should().ContainSingle().Subject;
            version.ArchiveKeyId.Should().Be(original.Id);
            version.KeyValueString.Should().Be("1");
            version.Version.Should().Be(1);
        }

        [Fact]
        public async Task NewKeysAreAddedAndKeysNoLongerProducedAreRemovedWithTheirVersionsAsync()
        {
            var guid = await ArchiveAsync(Key("A", "1"), Key("Gone", "x"));
            await ReplaceAsync(guid, Key("A", "1"), Key("Gone", "y"));
            (await KeyVersionsAsync(guid)).Should().ContainSingle();

            await ReplaceAsync(guid, Key("A", "1"), Key("New", "z", 2));

            var keys = await KeysAsync(guid);
            keys.Select(k => (k.Key, k.KeyValueString, k.ProcessingTypeId)).Should()
                .Equal(("A", "1", (byte?)1), ("New", "z", (byte?)2));
            keys.Single(k => k.Key == "New").Version.Should().Be(1);
            (await KeyVersionsAsync(guid)).Should().BeEmpty();
        }

        [Fact]
        public async Task AnotherRecordsKeysAreNeverTouchedAsync()
        {
            var guid = await ArchiveAsync(Key("A", "1"));
            var other = await ArchiveAsync(Key("A", "1"), Key("B", "2"));

            await ReplaceAsync(guid);

            (await KeysAsync(guid)).Should().BeEmpty();
            (await KeysAsync(other)).Should().HaveCount(2);
        }

        [Fact]
        public async Task TheArchiveRecordIsVersionedAndUpdatedAsync()
        {
            var guid = await ArchiveAsync();
            await using var dbContext = fx.GetDbContext();
            var original = await dbContext.Archive.SingleAsync(a => a.EntityAnalysisModelInstanceEntryGuid == guid);

            await new ArchiveRepository(dbContext).UpdateAsync(new Archive
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Json = "{\"payload\":{\"a\":2}}", EntryKeyValue = "E1",
                ResponseElevation = 5, ActivationRuleCount = 3, EntityAnalysisModelsReprocessingRuleInstanceId = 42
            });
            await new ArchiveRepository(dbContext).UpdateAsync(new Archive
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Json = "{\"payload\":{\"a\":3}}", EntryKeyValue = "E1",
                ResponseElevation = 6, ActivationRuleCount = 3, EntityAnalysisModelsReprocessingRuleInstanceId = 43
            });

            var updated = await dbContext.Archive.SingleAsync(a => a.EntityAnalysisModelInstanceEntryGuid == guid);
            updated.Id.Should().Be(original.Id);
            updated.Json.Should().Contain("3");
            updated.ResponseElevation.Should().Be(6);
            updated.Version.Should().Be(2);
            updated.EntityAnalysisModelsReprocessingRuleInstanceId.Should().Be(43);
            updated.ReferenceDate.Should().Be(original.ReferenceDate);
            var versions = await dbContext.ArchiveVersion.Where(v => v.ArchiveId == original.Id)
                .OrderBy(v => v.Id).ToListAsync();
            versions.Select(v => (v.Version, v.ResponseElevation, v.EntityAnalysisModelsReprocessingRuleInstanceId))
                .Should().Equal((null, 1d, null), (1, 5d, 42));
        }

        [Fact]
        public async Task ABatchUpdatesEveryRecordInOneGoAndReportsTheOnesThatAreGoneAsync()
        {
            var first = await ArchiveAsync(Key("A", "1"));
            var second = await ArchiveAsync(Key("A", "1"), Key("B", "2"));
            var gone = Guid.NewGuid();
            await using var dbContext = fx.GetDbContext();

            var updated = await new ArchiveRepository(dbContext).UpdateBatchAsync([
                new Archive
                {
                    EntityAnalysisModelInstanceEntryGuid = first, Json = "{\"n\":1}", EntryKeyValue = "E1",
                    ResponseElevation = 7, ActivationRuleCount = 2, EntityAnalysisModelsReprocessingRuleInstanceId = 9
                },
                new Archive
                {
                    EntityAnalysisModelInstanceEntryGuid = second, Json = "{\"n\":2}", EntryKeyValue = "E2",
                    ResponseElevation = 8, EntityAnalysisModelActivationRuleId = 3, ActivationRuleCount = 1,
                    EntityAnalysisModelsReprocessingRuleInstanceId = 9
                },
                new Archive { EntityAnalysisModelInstanceEntryGuid = gone, Json = "{}" }
            ]);
            await new ArchiveKeyRepository(dbContext).ReplaceBatchAsync(
                [(first, [Key("A", "5")]), (second, [Key("A", "1")])], 9);

            updated.Should().BeEquivalentTo([first, second]);
            var rows = await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == first ||
                                                          a.EntityAnalysisModelInstanceEntryGuid == second)
                .ToListAsync();
            rows.Should().OnlyContain(r => r.Version == 1 && r.EntityAnalysisModelsReprocessingRuleInstanceId == 9);
            rows.Single(r => r.EntityAnalysisModelInstanceEntryGuid == second).EntityAnalysisModelActivationRuleId
                .Should().Be(3);
            rows.Single(r => r.EntityAnalysisModelInstanceEntryGuid == first).Json.Should().Contain("\"n\": 1");
            (await dbContext.ArchiveVersion.CountAsync(v => v.EntityAnalysisModelInstanceEntryGuid == first ||
                                                            v.EntityAnalysisModelInstanceEntryGuid == second))
                .Should().Be(2);
            (await KeysAsync(first)).Single().KeyValueString.Should().Be("5");
            (await KeyVersionsAsync(first)).Should().ContainSingle();
            (await KeysAsync(second)).Select(k => k.Key).Should().Equal("A");
            (await KeyVersionsAsync(second)).Should().BeEmpty();
        }

        [Fact]
        public async Task UpdatingAnArchiveRecordThatDoesNotExistThrowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var thrown = false;
            try
            {
                await new ArchiveRepository(dbContext).UpdateAsync(new Archive
                    { EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(), Json = "{}" });
            }
            catch (KeyNotFoundException)
            {
                thrown = true;
            }

            thrown.Should().BeTrue();
        }
    }
}
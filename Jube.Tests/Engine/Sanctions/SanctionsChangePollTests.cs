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
using Jube.Data.Poco;
using Jube.Engine.BackgroundTasks.TaskStarters;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionsChangePollTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> sourceIds = [];
        private ModelEngineHost? host;

        public async Task InitializeAsync()
        {
            SanctionsTaskStarter.ChangeMarkerReader = DefaultReaderAsync;
            host = await ModelEngineHost.StartAsync(new Dictionary<string, string>
            {
                ["SanctionLoaderChangePoll"] = "200"
            });
        }

        public async Task DisposeAsync()
        {
            SanctionsTaskStarter.ChangeMarkerReader = DefaultReaderAsync;
            if (host != null)
            {
                await host.DisposeAsync();
            }

            await CleanUpAsync(fx, sourceIds);
        }

        private static Task<string> DefaultReaderAsync(Data.Context.DbContext dbContext,
            CancellationToken token) =>
            new Jube.Data.Repository.SanctionEntryImportRepository(dbContext).GetChangeMarkerAsync(token);

        private ModelEngineHost Host => host ?? throw new InvalidOperationException("The engine is not started.");

        private static async Task CleanUpAsync(DatabaseFixture fx, List<int> sourceIds)
        {
            await using var dbContext = fx.GetDbContext();
            foreach (var sourceId in sourceIds)
            {
                await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
            }
        }

        private static string Unique(string tag) => $"{DatabaseFixture.Prefix}{tag}{Guid.NewGuid():N}"[..40];

        private static async Task<int> InsertSourceAsync(DatabaseFixture fx, List<int> sourceIds)
        {
            await using var dbContext = fx.GetDbContext();
            var id = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = Unique("PollSource"),
                Severity = 2,
                Delimiter = ',',
                MultiPartStringIndex = "0",
                ReferenceIndex = 0,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 0,
                DirectoryLocation = "/tmp/does-not-matter",
                Skip = 0
            });
            sourceIds.Add(id);
            return id;
        }

        internal static async Task<List<int>> ImportAsync(DatabaseFixture fx, int sourceId, params string[] values)
        {
            await using var dbContext = fx.GetDbContext();
            var ids = new List<int>();
            foreach (var value in values)
            {
                ids.Add(await dbContext.InsertWithInt32IdentityAsync(new SanctionEntry
                {
                    SanctionEntryElementValue = value,
                    SanctionEntryReference = "REF-" + value,
                    SanctionEntrySourceId = sourceId,
                    SanctionEntryHash = Guid.NewGuid().ToString("N"),
                    SanctionPayload = "{}",
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "PollTest",
                    Deleted = 0
                }));
            }

            await dbContext.InsertWithInt32IdentityAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = sourceId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                TotalRows = values.Length,
                InsertedCount = values.Length,
                RevivedCount = 0,
                UnchangedCount = 0,
                RemovedCount = 0,
                RejectedCount = 0,
                Successful = 1,
                CreatedUser = "PollTest",
                CreatedDate = DateTime.UtcNow
            });

            return ids;
        }

        private static async Task<bool> WaitAsync(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(50);
            }

            return condition();
        }

        private bool Cached(int id) => Host.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(id);

        [Fact]
        public async Task ANewImport_BecomesVisibleInTheCache_WithoutWaitingForTheLoaderIntervalAsync()
        {
            var sourceId = await InsertSourceAsync(fx, sourceIds);
            await WaitAsync(() => Host.Engine.Context.Sanctions.SanctionsLoadedForStartup, TimeSpan.FromSeconds(30));
            var before = SanctionsTaskStarter.LightRefreshCount;

            var ids = await ImportAsync(fx, sourceId, "zzpollalpha zzpollbeta", "zzpollgamma zzpolldelta");

            (await WaitAsync(() => ids.All(Cached), TimeSpan.FromSeconds(15))).Should().BeTrue(
                "the change poll must see the new import long before SanctionLoaderWait (an hour) elapses");
            (await WaitAsync(() => SanctionsTaskStarter.LightRefreshCount > before, TimeSpan.FromSeconds(5)))
                .Should().BeTrue("the refresh is counted once the entries are cached");
            Host.Engine.Context.Sanctions.SanctionsEntries[ids[0]].SanctionEntryReference.Should()
                .Be("REF-zzpollalpha zzpollbeta");
            Host.Engine.Context.Sanctions.SanctionsEntries[ids[0]].SanctionEntrySourceId.Should().Be(sourceId);
        }

        [Fact]
        public async Task EntriesRemovedByALaterImport_LeaveTheCacheAsync()
        {
            var sourceId = await InsertSourceAsync(fx, sourceIds);
            var ids = await ImportAsync(fx, sourceId, "zzremovealpha zzremovebeta", "zzremovegamma zzremovedelta");
            (await WaitAsync(() => ids.All(Cached), TimeSpan.FromSeconds(15))).Should().BeTrue();

            await using (var dbContext = fx.GetDbContext())
            {
                await dbContext.SanctionEntry.Where(w => w.Id == ids[0]).Set(s => s.Deleted, (byte?)1).UpdateAsync();
            }

            await ImportAsync(fx, sourceId);

            (await WaitAsync(() => !Cached(ids[0]), TimeSpan.FromSeconds(15))).Should().BeTrue(
                "an entry soft deleted by an import is removed from memory too, it no longer waits for a restart");
            Cached(ids[1]).Should().BeTrue("the entry that is still live stays cached");
        }

        [Fact]
        public async Task WhenNothingHasBeenImported_TheCacheIsNotReloadedAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await WaitAsync(() => Host.Engine.Context.Sanctions.SanctionsLoadedForStartup, TimeSpan.FromSeconds(30));
            var before = SanctionsTaskStarter.LightRefreshCount;

            await Task.Delay(TimeSpan.FromSeconds(2));

            SanctionsTaskStarter.LightRefreshCount.Should().Be(before);
        }

        [Fact]
        public async Task AFailingFingerprintQuery_DoesNotEndTheLoop_AndTheNextPollRecoversAsync()
        {
            var sourceId = await InsertSourceAsync(fx, sourceIds);
            var failures = 0;
            SanctionsTaskStarter.ChangeMarkerReader = (_, _) =>
            {
                failures++;
                throw new InvalidOperationException("the fingerprint query is down");
            };

            var ids = await ImportAsync(fx, sourceId, "zzfailalpha zzfailbeta");
            (await WaitAsync(() => failures >= 3, TimeSpan.FromSeconds(10))).Should().BeTrue(
                "the loop keeps polling while the query fails");
            Cached(ids[0]).Should().BeFalse("nothing can be refreshed while the fingerprint is unreadable");
            Host.Engine.Context.Tasks.SanctionsTask.IsCompleted.Should().BeFalse("the loop survives a failing query");

            SanctionsTaskStarter.ChangeMarkerReader = DefaultReaderAsync;

            (await WaitAsync(() => Cached(ids[0]), TimeSpan.FromSeconds(15))).Should().BeTrue(
                "once the query works again the import that was missed is picked up");
        }

        [Fact]
        public async Task ACancellation_EndsTheLoopPromptlyAsync()
        {
            var task = Host.Engine.Context.Tasks.SanctionsTask;
            var stopwatch = Stopwatch.StartNew();

            await Host.DisposeAsync();
            host = null;

            (await WaitAsync(() => task.IsCompleted, TimeSpan.FromSeconds(10))).Should().BeTrue(
                "the wait is sliced, so a shutdown does not wait out the loader interval");
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        }
    }
}
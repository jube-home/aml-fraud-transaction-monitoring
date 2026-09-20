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
    public sealed class SanctionsChangePollConfigurationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> sourceIds = [];
        private ModelEngineHost? host;
        private int markerReads;

        public Task InitializeAsync()
        {
            SanctionsTaskStarter.ChangeMarkerReader = (dbContext, token) =>
            {
                Interlocked.Increment(ref markerReads);
                return new Jube.Data.Repository.SanctionEntryImportRepository(dbContext).GetChangeMarkerAsync(token);
            };
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            SanctionsTaskStarter.ChangeMarkerReader = (dbContext, token) =>
                new Jube.Data.Repository.SanctionEntryImportRepository(dbContext).GetChangeMarkerAsync(token);
            if (host != null)
            {
                await host.DisposeAsync();
            }

            await using var dbContext = fx.GetDbContext();
            foreach (var sourceId in sourceIds)
            {
                await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
            }
        }

        private ModelEngineHost Engine => host ?? throw new InvalidOperationException("The engine is not started.");

        private async Task StartAsync(params (string Key, string Value)[] settings)
        {
            host = await ModelEngineHost.StartAsync(settings.ToDictionary(s => s.Key, s => s.Value));
        }

        private async Task<int> NewSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}PollCfg{Guid.NewGuid():N}"[..40], Severity = 2, Delimiter = ',',
                MultiPartStringIndex = "0", ReferenceIndex = 0, EnableDirectoryLocation = 0, EnableHttpLocation = 0,
                DirectoryLocation = "/tmp/does-not-matter", Skip = 0
            });
            sourceIds.Add(id);
            return id;
        }

        [Theory]
        [InlineData(null, 60000)]
        [InlineData("", 60000)]
        [InlineData("garbage", 60000)]
        [InlineData("0", 60000)]
        [InlineData("-1", 60000)]
        [InlineData("-2147483648", 60000)]
        [InlineData("1.5", 60000)]
        [InlineData("99999999999", 60000)]
        [InlineData("1", 1)]
        [InlineData("250", 250)]
        [InlineData("60000", 60000)]
        public void TheIntervalFallsBackToTheDefault_WhenItIsMissingGarbageZeroOrNegative(string? setting, int expected)
        {
            SanctionsTaskStarter.ResolveChangePollMilliseconds(setting).Should().Be(expected);
        }

        [Fact]
        public async Task WhenTheFlagIsFalse_NothingIsPolled_AndANewImportIsNotNoticedAsync()
        {
            await StartAsync(("EnableSanctionLoaderChangePoll", "False"), ("SanctionLoaderChangePoll", "100"));
            var sourceId = await NewSourceAsync();
            var readsAtStart = Volatile.Read(ref markerReads);
            var before = SanctionsTaskStarter.LightRefreshCount;

            var ids = await SanctionsChangePollTests.ImportAsync(fx, sourceId, "zzflagoffalpha zzflagoffbeta");
            await Task.Delay(TimeSpan.FromSeconds(3));

            Engine.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(ids[0]).Should().BeFalse();
            Volatile.Read(ref markerReads).Should().Be(readsAtStart, "with the flag off there is no polling at all");
            SanctionsTaskStarter.LightRefreshCount.Should().Be(before);
        }

        [Fact]
        public async Task WhenTheFlagIsNotSet_TheDefaultPollsAndPicksUpANewImportAsync()
        {
            var defaults = ModelEngineHost.CreateEnvironment();
            defaults.AppSettings("EnableSanctionLoaderChangePoll").Should().Be("True");
            defaults.AppSettings("SanctionLoaderChangePoll").Should().Be("60000");
            SanctionsTaskStarter.ResolveChangePollMilliseconds(defaults.AppSettings("SanctionLoaderChangePoll"))
                .Should().Be(60000);

            await StartAsync(("SanctionLoaderChangePoll", "200"));
            var sourceId = await NewSourceAsync();

            var ids = await SanctionsChangePollTests.ImportAsync(fx, sourceId, "zzdefaultalpha zzdefaultbeta");

            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline &&
                   !Engine.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(ids[0]))
            {
                await Task.Delay(100);
            }

            Engine.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(ids[0]).Should().BeTrue(
                "an explicit interval is honoured and the flag defaults to True");
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("garbage")]
        public async Task AZeroNegativeOrGarbageInterval_FallsBackToTheDefault_AndDoesNotSpinAsync(string interval)
        {
            await StartAsync(("SanctionLoaderChangePoll", interval));
            var readsAtStart = Volatile.Read(ref markerReads);

            await Task.Delay(TimeSpan.FromSeconds(3));

            (Volatile.Read(ref markerReads) - readsAtStart).Should().BeLessThanOrEqualTo(1,
                "the fallback of 60 seconds allows no more than one read in 3 seconds, never a tight loop");
        }

        [Fact]
        public async Task AnIntervalAtOrAboveTheWait_IsOneSliceThatStillChecksAtTheEndOfTheWaitAsync()
        {
            await StartAsync(("SanctionLoaderWait", "1500"), ("SanctionLoaderChangePoll", "3600000"));
            var sourceId = await NewSourceAsync();
            var readsAtStart = Volatile.Read(ref markerReads);

            var ids = await SanctionsChangePollTests.ImportAsync(fx, sourceId, "zzsliceonealpha zzsliceonebeta");

            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline &&
                   !Engine.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(ids[0]))
            {
                await Task.Delay(100);
            }

            Engine.Engine.Context.Sanctions.SanctionsEntries.ContainsKey(ids[0]).Should().BeTrue(
                "one slice of the wait ends with the fingerprint check");
            Volatile.Read(ref markerReads).Should().BeGreaterThan(readsAtStart);
        }
    }
}
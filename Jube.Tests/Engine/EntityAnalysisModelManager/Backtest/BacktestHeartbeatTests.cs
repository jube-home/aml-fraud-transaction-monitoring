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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.Backtest
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class BacktestHeartbeatTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly TimeSpan interval = TimeSpan.FromMilliseconds(250);

        private int instanceId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var now = DateTime.UtcNow;
            instanceId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelBacktestInstance
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 0,
                EntityAnalysisModelId = 0,
                RuleType = "ActivationRule",
                Request = "{}",
                Status = (short)BacktestInstanceStatus.Running,
                CreatedDate = now,
                StartedDate = now,
                HeartbeatDate = now,
                CreatedUser = DatabaseFixture.Prefix
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelBacktestInstance.Where(w => w.Id == instanceId).DeleteAsync();
        }

        private async Task<EntityAnalysisModelBacktestInstance> InstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.EntityAnalysisModelBacktestInstance.SingleAsync(w => w.Id == instanceId);
        }

        private async Task SetStatusAsync(BacktestInstanceStatus status)
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelBacktestInstance.Where(w => w.Id == instanceId)
                .Set(s => s.Status, (short)status).UpdateAsync();
        }

        private async Task RecoverStaleAsync(TimeSpan stale)
        {
            await using var dbContext = fx.GetDbContext();
            await EntityAnalysisModelBacktestInstanceRepository.RecoverStaleAsync(dbContext, stale);
        }

        private BacktestHeartbeat Start()
        {
            return new BacktestHeartbeat(fx.GetDbContext, instanceId, interval, TestLog.NoOp);
        }

        [Theory]
        [InlineData(120, 10_000)]
        [InlineData(20, 5_000)]
        [InlineData(8, 2_000)]
        [InlineData(2, 1_000)]
        [InlineData(0, 1_000)]
        public void TheIntervalIsAQuarterOfTheStaleWindowBetweenOneAndTenSeconds(int staleSeconds, int expectedMs)
        {
            BacktestHeartbeat.IntervalFor(TimeSpan.FromSeconds(staleSeconds))
                .Should().Be(TimeSpan.FromMilliseconds(expectedMs));
        }

        [Fact]
        public async Task APageLongerThanTheStaleWindowNoLongerFailsARunningInstanceAsync()
        {
            await using (Start())
            {
                await Task.Delay(TimeSpan.FromSeconds(2.5));

                await RecoverStaleAsync(TimeSpan.FromSeconds(1));

                var instance = await InstanceAsync();
                instance.Status.Should().Be((short)BacktestInstanceStatus.Running);
                instance.HeartbeatDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task AnInstanceWhoseHeartbeatHasStoppedIsStillRecoveredAsync()
        {
            await using (Start())
            {
                await Task.Delay(interval * 2);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
            await RecoverStaleAsync(TimeSpan.FromSeconds(1));

            (await InstanceAsync()).Status.Should().Be((short)BacktestInstanceStatus.Failed);
        }

        [Fact]
        public async Task ReportedProgressIsWrittenOnTheNextBeatAsync()
        {
            await using var heartbeat = Start();
            heartbeat.Report(50, 40, 100);

            await Task.Delay(interval * 3);

            var instance = await InstanceAsync();
            instance.Scanned.Should().Be(50);
            instance.Evaluated.Should().Be(40);
            instance.Progress.Should().BeApproximately(0.5, 0.0001);
        }

        [Theory]
        [InlineData(BacktestInstanceStatus.Cancelling)]
        [InlineData(BacktestInstanceStatus.Failed)]
        [InlineData(BacktestInstanceStatus.Cancelled)]
        public async Task AStopOrARecoveryIsSeenWithinABeatEvenMidPageAsync(BacktestInstanceStatus status)
        {
            await using var heartbeat = Start();
            await Task.Delay(interval);
            heartbeat.StopRequested.Should().BeFalse();

            await SetStatusAsync(status);
            await Task.Delay(interval * 3);

            heartbeat.StopRequested.Should().BeTrue();
        }

        [Fact]
        public async Task APageRecordsProgressAndCarriesOnUntilAStopIsSeenAsync()
        {
            await using var heartbeat = Start();

            (await heartbeat.OnPageAsync(new BacktestProgress(10, 8, 100))).Should().BeTrue();
            heartbeat.Scanned.Should().Be(10);
            heartbeat.Evaluated.Should().Be(8);
            heartbeat.Stopped.Should().BeFalse();

            await SetStatusAsync(BacktestInstanceStatus.Cancelling);
            await Task.Delay(interval * 3);
            heartbeat.StopRequested.Should().BeTrue();
            heartbeat.Stopped.Should().BeFalse();

            (await heartbeat.OnPageAsync(new BacktestProgress(20, 16, 100))).Should().BeFalse();
            heartbeat.Stopped.Should().BeTrue();
            heartbeat.Scanned.Should().Be(20);
        }

        [Fact]
        public async Task ADisposedHeartbeatWritesNothingMoreAsync()
        {
            await using (Start())
            {
                await Task.Delay(interval);
            }

            var before = (await InstanceAsync()).HeartbeatDate;
            await Task.Delay(interval * 3);

            (await InstanceAsync()).HeartbeatDate.Should().Be(before);
        }

        [Fact]
        public async Task AFailedBeatIsRetriedRatherThanEndingTheHeartbeatAsync()
        {
            var calls = 0;
            await using (new BacktestHeartbeat(() =>
                         {
                             if (++calls == 1)
                             {
                                 throw new InvalidOperationException("database unavailable");
                             }

                             return fx.GetDbContext();
                         }, instanceId, interval, TestLog.NoOp))
            {
                await Task.Delay(interval * 3);
            }

            calls.Should().BeGreaterThan(1);
            (await InstanceAsync()).HeartbeatDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}
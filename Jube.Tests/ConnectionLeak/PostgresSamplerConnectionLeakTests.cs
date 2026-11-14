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


using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Test.Infrastructure.ConnectionLeak;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.ConnectionLeak
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PostgresSamplerConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int SamplerCount = 6;
        private const int TicksPerSampler = 3;
        private readonly string isolated = PoolHeadroom.CreateIsolatedConnectionString(fx.ConnectionString);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            PoolHeadroom.ClearPool(isolated);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task SamplingMetricsOnSeveralLiveSamplersNeverPinsAPooledConnectionAsync()
        {
            var samplers = new PostgresMetricSampler[SamplerCount];

            for (var i = 0; i < SamplerCount; i++)
            {
                samplers[i] = new PostgresMetricSampler(isolated);

                for (var tick = 0; tick < TicksPerSampler; tick++)
                {
                    await samplers[i].SampleAsync();
                    await samplers[i].SampleReplicationAsync();
                }
            }

            var serverConnections = await PoolHeadroom.CountServerConnectionsAsync(fx.ConnectionString, isolated);

            serverConnections.Should().BeLessThanOrEqualTo(1,
                $"{SamplerCount} live metric samplers sampled sequentially must share one idle pooled connection "
                + "and never pin one each");

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "Live PostgresMetricSampler instances");

            foreach (var sampler in samplers)
            {
                await sampler.DisposeAsync();
            }
        }

        [Fact]
        public async Task SamplingTheLogOnSeveralLiveSamplersNeverPinsAPooledConnectionAsync()
        {
            var samplers = new PostgresLogSampler[SamplerCount];

            for (var i = 0; i < SamplerCount; i++)
            {
                samplers[i] = new PostgresLogSampler(isolated);

                for (var tick = 0; tick < TicksPerSampler; tick++)
                {
                    await samplers[i].SampleAsync();
                }
            }

            var serverConnections = await PoolHeadroom.CountServerConnectionsAsync(fx.ConnectionString, isolated);

            serverConnections.Should().BeLessThanOrEqualTo(1,
                $"{SamplerCount} live log samplers sampled sequentially must share one idle pooled connection "
                + "and never pin one each");

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "Live PostgresLogSampler instances");

            foreach (var sampler in samplers)
            {
                await sampler.DisposeAsync();
            }
        }

        [Fact]
        public async Task TheNumberOfServerConnectionsDoesNotGrowWithTheNumberOfTicksAsync()
        {
            var metricSampler = new PostgresMetricSampler(isolated);
            var logSampler = new PostgresLogSampler(isolated);

            for (var tick = 0; tick < 2; tick++)
            {
                await metricSampler.SampleAsync();
                await logSampler.SampleAsync();
            }

            var afterFewTicks = await PoolHeadroom.CountServerConnectionsAsync(fx.ConnectionString, isolated);

            for (var tick = 0; tick < 20; tick++)
            {
                await metricSampler.SampleAsync();
                await logSampler.SampleAsync();
            }

            var afterManyTicks = await PoolHeadroom.CountServerConnectionsAsync(fx.ConnectionString, isolated);

            afterManyTicks.Should().BeLessThanOrEqualTo(afterFewTicks,
                "server connections must stay flat however many sampling ticks have run");

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "A metric and a log sampler after many ticks");

            await metricSampler.DisposeAsync();
            await logSampler.DisposeAsync();
        }
    }
}

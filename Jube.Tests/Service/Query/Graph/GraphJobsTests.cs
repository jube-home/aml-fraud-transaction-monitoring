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
using Jube.Service.Query.Graph.Jobs;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Service.Query.Graph
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphJobsTests(DatabaseFixture fx)
    {
        [Fact]
        public async Task TheRetentionJobCreatesTheMonthlyPartitionsAheadAndNeverDropsCurrentOnesAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                   ?? fx.ConnectionString;
            var job = new GraphRetentionJob(connectionString, null, TestLog.NoOp, 365);

            await job.RunOnceAsync();
            var second = await job.RunOnceAsync();
            second.PartitionsCreated.Should().Be(0);
            second.PartitionsDropped.Should().Be(0);

            await using var dbContext = fx.GetDbContext();
            var partitions = await dbContext.QueryToListAsync<string>(
                "select c.relname from pg_inherits i join pg_class c on c.oid = i.inhrelid join pg_class p on p.oid = i.inhparent where p.relname = 'GraphEdgeEvent'");
            var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            for (var ahead = 0; ahead <= GraphRetentionJob.MonthsAhead; ahead++)
            {
                partitions.Should().Contain(GraphRetentionJob.PartitionName(month.AddMonths(ahead)));
            }

            partitions.Should().Contain("GraphEdgeEventDefault");
        }

        [Fact]
        public async Task TheArchiveKeyIndexJobLeavesAValidIndexAndIsIdempotentAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                   ?? fx.ConnectionString;
            var job = new GraphArchiveKeyIndexJob(connectionString, null, TestLog.NoOp);

            var result = await job.RunOnceAsync();
            result.Valid.Should().BeTrue();
            var again = await job.RunOnceAsync();
            again.Valid.Should().BeTrue();
            again.Created.Should().BeFalse();

            await using var dbContext = fx.GetDbContext();
            (await GraphBuildJob.IndexIsValidAsync(dbContext, default)).Should().BeTrue();
        }
    }
}

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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Reporting.Check;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using Npgsql;
using Xunit;

namespace Jube.Test.Reporting.Check
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SqlSchemaSnapshotInvalidationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string tableName = $"{DatabaseFixture.Prefix}SchemaInvalidation{Guid.NewGuid():N}"[..40];

        public Task InitializeAsync()
        {
            SqlSchemaSnapshotCache.Invalidate();

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await ExecuteAsync($"DROP TABLE IF EXISTS public.\"{tableName}\"");
            SqlSchemaSnapshotCache.Invalidate();
        }

        private async Task ExecuteAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(fx.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static ServiceChangeEvent EventIn(string area)
        {
            return new ServiceChangeEvent(area, 0, ServiceChangeKind.Updated, null, null, "test",
                DateTimeOffset.UtcNow, "correlation");
        }

        [Fact]
        public async Task ASchemaChangeEventDropsTheCachedSnapshotSoANewTableIsSeenAsync()
        {
            var bus = new InProcessServiceChangeBus();
            using var subscription = SqlSchemaSnapshotInvalidation.Attach(bus);

            var before = await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString);
            before!.FindRelation(tableName).Should().BeNull();

            await ExecuteAsync($"CREATE TABLE public.\"{tableName}\" (\"Id\" integer)");

            (await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString)).Should().BeSameAs(before);

            await bus.PublishAsync(EventIn(SqlSchemaSnapshotInvalidation.SchemaChangeArea));

            var after = await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString);
            after.Should().NotBeSameAs(before);
            after!.FindRelation(tableName).Should().NotBeNull();
        }

        [Fact]
        public async Task AnUnrelatedChangeEventLeavesTheCachedSnapshotAloneAsync()
        {
            var bus = new InProcessServiceChangeBus();
            using var subscription = SqlSchemaSnapshotInvalidation.Attach(bus);

            var before = await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString);

            await bus.PublishAsync(EventIn("EntityAnalysisModelTag"));

            (await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString)).Should().BeSameAs(before);
        }

        [Fact]
        public async Task AnnouncingASchemaChangeInvalidatesThisNodeAndTellsTheBusAsync()
        {
            var bus = new InProcessServiceChangeBus();
            var seen = new System.Collections.Generic.List<ServiceChangeEvent>();
            using var listener = bus.Subscribe(change =>
            {
                seen.Add(change);

                return Task.CompletedTask;
            });
            var before = await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString);

            await SqlSchemaSnapshotInvalidation.AnnounceAsync(bus, "Migration");

            seen.Should().ContainSingle(c => c.Area == SqlSchemaSnapshotInvalidation.SchemaChangeArea
                                             && c.Actor == "Migration");
            (await SqlSchemaSnapshotCache.GetAsync(fx.ConnectionString)).Should().NotBeSameAs(before);
        }
    }
}

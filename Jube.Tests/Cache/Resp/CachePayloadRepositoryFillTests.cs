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
using Jube.Cache.Resp;
using Jube.Cache.Resp.Serialization;
using Jube.Cache.Resp.Serialization.DictionaryNoBoxing.MessagePack.Models;
using Jube.Data.Context;
using Jube.Dictionary;
using Jube.Test.Infrastructure;
using LinqToDB.Async;
using MessagePack;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache.Resp
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CachePayloadRepositoryFillTests
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        [Fact]
        public async Task TheStartupFillStoresEachPayloadUnderTheKeyThatALookupAsksForAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var model = await dbContext.EntityAnalysisModel
                .Where(w => w.Deleted == null || w.Deleted == 0).FirstOrDefaultAsync();

            model.Should().NotBeNull("the fill walks the models held in the database");

            var db = new FakeHybridResilientRespDatabase();
            var payloadGuid = $"{Guid.NewGuid():N}";
            var keyPayload = $"Payload:{model!.TenantRegistryId}:{model.Guid:N}";
            var keyLruJournal = $"LruJournal:{model.TenantRegistryId}:{model.Guid:N}";
            var keyJournal = $"Journal:{model.TenantRegistryId}:{model.Guid:N}:{Guid.NewGuid():N}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var payload = new DictionaryNoBoxing<int>();
            payload.Add(1, "from-the-fill");

            var envelope = new EnvelopeDictionaryNoBoxing<int> { Version = 1, Data = payload };

            await db.HashSetAsync(keyPayload, payloadGuid, MessagePackSerializer.Serialize(envelope,
                MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(false)));

            await db.SortedSetAddAsync(keyLruJournal, keyJournal, now);
            await db.SortedSetAddAsync(keyJournal, payloadGuid, now);

            using var repository = await CachePayloadRepository.CreateAsync(inertMultiplexer, db, ConnectionString,
                TestLog.NoOp, true, true, 10_000_000, false, false, false, TimeSpan.FromHours(1), false);

            await repository.FillTask;

            var entry = repository.GetLocalCacheEntry(keyPayload);

            var result = await repository.GetPayloadBatchAsync(model.TenantRegistryId.GetValueOrDefault(),
                model.Guid, [payloadGuid], Guid.NewGuid());

            result.Should().ContainKey(payloadGuid);
            result[payloadGuid][1].AsString().Should().Be("from-the-fill");
            entry.Misses.Should().Be(0, "the fill keyed the payload by its element alone, as the lookup does");
        }
    }
}

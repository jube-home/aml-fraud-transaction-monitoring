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

namespace Jube.ResilientRedisConnection
{
    using Interfaces;
    using log4net;
    using Polly;
    using StackExchange.Redis;

    public class ResilientRedisConnection(
        IConnectionMultiplexer multiplexer,
        string connectionString,
        bool hsetOffload,
        ILog log)
        : IDisposable
    {
        private static readonly IAsyncPolicy noOpPolicy = Policy.NoOpAsync();

        private readonly string connectionString =
            connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        private readonly IConnectionMultiplexer multiplexer =
            multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

        public void Dispose()
        {
            multiplexer.Dispose();
        }

        public IHybridResilientRedisDatabase GetDatabase(int db = -1)
        {
            return new ResilientRedisDatabase(multiplexer.GetDatabase(db), noOpPolicy, noOpPolicy, connectionString,
                log, hsetOffload);
        }
    }
}
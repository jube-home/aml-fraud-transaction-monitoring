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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jube.Svirfd;
using Microsoft.Extensions.Logging.Abstractions;
using Svirfd;

namespace Jube.Test.Svirfd
{
    public static class SvirfdFixture
    {
        public static string Directory => Path.Combine(AppContext.BaseDirectory, "Svirfd", "Fixtures");

        public static SvirfdSettings DirectorySettings => new()
        {
            Mode = SvirfdMode.Directory,
            Directory = Directory,
            BackgroundStart = false
        };

        public static async Task<SvirfdHost> StartDirectoryHostAsync()
        {
            var host = new SvirfdHost(DirectorySettings, new HttpClient(), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            return host;
        }

        public static async Task<IpResponse> RecordAsync(string address)
        {
            await using var host = await StartDirectoryHostAsync();

            return host.TryLookup(address, out var record) && record != null
                ? record
                : throw new InvalidOperationException($"The fixture has no record for {address}.");
        }
    }
}

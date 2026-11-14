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
using System.Threading;
using System;
using System.Threading.Tasks;
using Jube.Svirfd.Interfaces;
using Svirfd;

namespace Jube.Svirfd
{
    public sealed class SvirfdNullLookup : ISvirfdLookup
    {
        public bool Enabled => false;
        public bool Ready => false;
        public bool RemoteEngineLookup => false;
        public SvirfdMode Mode => SvirfdMode.Auto;
        public SvirfdState State => SvirfdState.Failed;
        public string LastError => string.Empty;
        public DateTime? NextRetryAt => null;
        public TimeSpan? Staleness => null;
        public long Seq => -1;
        public string Attribution => SvirfdCredit.Line;
        public string LicenceUrl => SvirfdCredit.LicenceUrl;

        public bool TryLookup(string? address, out IpResponse? record)
        {
            record = null;

            return false;
        }

        public Task<IpResponse?> LookupAsync(string? address, CancellationToken token)
        {
            return Task.FromResult<IpResponse?>(null);
        }

        public Task<IpResponse?> LookupFreshAsync(string? address, CancellationToken token)
        {
            return Task.FromResult<IpResponse?>(null);
        }

        public Task<IpResponse?> LookupAsync(string? address, bool remoteAllowed, CancellationToken token)
        {
            return Task.FromResult<IpResponse?>(null);
        }

        public Task<IpResponse?> LookupFreshAsync(string? address, bool remoteAllowed, CancellationToken token)
        {
            return Task.FromResult<IpResponse?>(null);
        }
    }
}

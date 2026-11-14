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
using Svirfd;

namespace Jube.Svirfd.Interfaces
{
    public interface ISvirfdLookup
    {
        bool Enabled { get; }
        bool Ready { get; }
        bool RemoteEngineLookup { get; }
        SvirfdMode Mode { get; }
        SvirfdState State { get; }
        string LastError { get; }
        DateTime? NextRetryAt { get; }
        TimeSpan? Staleness { get; }
        long Seq { get; }
        string Attribution { get; }
        string LicenceUrl { get; }
        bool TryLookup(string? address, out IpResponse? record);
        Task<IpResponse?> LookupAsync(string? address, CancellationToken token);
        Task<IpResponse?> LookupFreshAsync(string? address, CancellationToken token);
        Task<IpResponse?> LookupAsync(string? address, bool remoteAllowed, CancellationToken token);
        Task<IpResponse?> LookupFreshAsync(string? address, bool remoteAllowed, CancellationToken token);
    }
}

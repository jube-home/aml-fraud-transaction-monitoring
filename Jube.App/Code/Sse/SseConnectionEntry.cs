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
using System.Threading.Channels;

namespace Jube.App.Code.Sse
{
    internal sealed class SseConnectionEntry(
        string userName,
        string issuedMilliseconds,
        int tenantRegistryId,
        Channel<string> channel,
        CancellationTokenSource cancellationTokenSource)
    {
        public string UserName { get; } = userName;
        public string IssuedMilliseconds { get; } = issuedMilliseconds;
        public int TenantRegistryId { get; } = tenantRegistryId;
        public Channel<string> Channel { get; } = channel;
        public bool Revoked { get; private set; }

        public void Abort()
        {
            Revoked = true;

            try
            {
                cancellationTokenSource.Cancel();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (System.Exception)
            {
            }
        }
    }
}
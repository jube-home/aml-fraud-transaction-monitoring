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
using System.Threading.Tasks;

namespace Jube.Engine.CaseEscalation
{
    internal sealed class LegacyCaseStatusDispatch(CaseStatusDispatchRequest request) : ICaseStatusDispatch
    {
        public async Task<string> NotifyAsync(CancellationToken token = default)
        {
            return request.Status.EnableNotification == 1 && request.Payload != null
                ? await CaseStatusNotifier.SendNotificationAsync(request.Environment, request.Log, request.Status,
                    request.Payload, token).ConfigureAwait(false)
                : null;
        }

        public async Task<string> CallAsync(CancellationToken token = default)
        {
            return request.Status.EnableHttpEndpoint == 1 && request.Payload != null
                ? await CaseStatusNotifier.CallHttpEndpointAsync(request.Log, request.Status, request.Case,
                    request.Serializer, request.Payload).ConfigureAwait(false)
                : null;
        }
    }
}

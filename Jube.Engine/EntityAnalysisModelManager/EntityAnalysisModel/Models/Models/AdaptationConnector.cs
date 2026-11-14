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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Jube.Case.Outbound;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    public static class AdaptationConnector
    {
        public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context,
            bool allowInsecureTargets, CancellationToken token)
        {
            var host = context.DnsEndPoint.Host;

            IReadOnlyList<IPAddress> addresses = HostSpelling.TryParseAddress(host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(host, token).ConfigureAwait(false);

            var refusal = TenantBaseUrlPolicy.CheckResolved(addresses, allowInsecureTargets, out var permitted);

            if (refusal != null)
            {
                throw new HttpRequestException($"Refused by the outbound host policy ({refusal}).", null);
            }

            Exception last = null;

            foreach (var address in permitted)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token)
                        .ConfigureAwait(false);

                    return new NetworkStream(socket, true);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    last = ex;
                }
            }

            throw new HttpRequestException("The host could not be reached.", last);
        }
    }
}

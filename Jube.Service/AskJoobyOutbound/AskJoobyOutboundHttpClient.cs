/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Net;
using System.Net.Http;

namespace Jube.Service.AskJoobyOutbound
{
    public static class AskJoobyOutboundHttpClient
    {
        public const long MaxResponseBytes = 8 * 1024 * 1024;

        public static HttpClient Create()
        {
            var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromSeconds(10)
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60),
                MaxResponseContentBufferSize = MaxResponseBytes
            };
        }
    }
}

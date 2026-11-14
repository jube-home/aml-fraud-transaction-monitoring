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
using Jube.Case.Outbound;

namespace Jube.Engine.Sanctions
{
    public static class SanctionListDownloader
    {
        public const int MaximumRedirects = 5;
        public const long MaximumBytes = 128L * 1024 * 1024;
        public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

        public static async Task<MemoryStream> DownloadAsync(GuardedHttpEndpoint endpoint, string location,
            long maximumBytes, TimeSpan timeout, CancellationToken token)
        {
            if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
            {
                throw new OutboundPolicyException(PolicyCode.NotAbsolute);
            }

            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(timeout);

            for (var hop = 0; hop <= MaximumRedirects; hop++)
            {
                using var response = await endpoint.OpenGetAsync(uri, timed.Token).ConfigureAwait(false);

                var code = (int)response.StatusCode;

                if (code is >= 300 and < 400)
                {
                    var next = response.Headers.Location;

                    if (next == null)
                    {
                        throw new HttpRequestException($"Redirect {code} without a Location header.");
                    }

                    uri = next.IsAbsoluteUri ? next : new Uri(uri, next);

                    continue;
                }

                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength > maximumBytes)
                {
                    throw new InvalidDataException($"The list is larger than {maximumBytes} bytes.");
                }

                await using var source = await response.Content.ReadAsStreamAsync(timed.Token)
                    .ConfigureAwait(false);

                var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int read;

                while ((read = await source.ReadAsync(chunk, timed.Token).ConfigureAwait(false)) > 0)
                {
                    if (buffer.Length + read > maximumBytes)
                    {
                        throw new InvalidDataException($"The list is larger than {maximumBytes} bytes.");
                    }

                    buffer.Write(chunk, 0, read);
                }

                buffer.Position = 0;

                return buffer;
            }

            throw new HttpRequestException($"More than {MaximumRedirects} redirects.");
        }
    }
}

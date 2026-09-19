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

namespace Jube.App.Middlewares
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class RequestTrackingMiddleware(RequestDelegate next)
    {
        private static int activeRequests;

        public async Task InvokeAsync(HttpContext context)
        {
            Interlocked.Increment(ref activeRequests);
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref activeRequests);
            }
        }

        public static async Task WaitForRequestsToDrainAsync(CancellationToken cancellationToken = default)
        {
            while (activeRequests > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            }
        }
    }
}

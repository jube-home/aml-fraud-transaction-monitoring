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

using System.Net;
using System.Net.Sockets;
using Polly.Timeout;

namespace Jube.ResilientHttp
{
    public static class HttpFailureClassification
    {
        public static bool IsTransient(Exception exception)
        {
            return exception switch
            {
                TimeoutRejectedException => true,
                TimeoutException => true,
                TaskCanceledException { InnerException: TimeoutException } => true,
                OperationCanceledException => false,
                SocketException => true,
                HttpRequestException { StatusCode: { } status } => IsTransientStatus(status),
                HttpRequestException => true,
                IOException => true,
                _ => false
            };
        }

        public static bool IsTransientStatus(HttpStatusCode status)
        {
            var code = (int)status;

            return code >= 500 || code is 408 or 429;
        }
    }
}

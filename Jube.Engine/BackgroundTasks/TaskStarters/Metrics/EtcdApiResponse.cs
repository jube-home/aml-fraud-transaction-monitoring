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
using System.Net;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public static class EtcdApiResponse
    {
        public static bool TryRead(HttpStatusCode statusCode, string body, out JObject payload, out string error)
        {
            payload = null;

            var status = $"HTTP {(int)statusCode} {statusCode}";

            if (string.IsNullOrWhiteSpace(body))
            {
                error = $"{status} with an empty response body";
                return false;
            }

            JObject parsed;
            try
            {
                parsed = JObject.Parse(body);
            }
            catch (Exception)
            {
                error = $"{status} with a response body that is not a JSON object";
                return false;
            }

            var reported = (string)parsed["error"] ?? (string)parsed["message"];
            if (!string.IsNullOrWhiteSpace(reported))
            {
                var code = (int?)parsed["code"];
                error = code.HasValue
                    ? $"{status}: {reported} (etcd code {code.Value})"
                    : $"{status}: {reported}";
                return false;
            }

            if (statusCode is < HttpStatusCode.OK or > (HttpStatusCode)299)
            {
                error = status;
                return false;
            }

            payload = parsed;
            error = null;
            return true;
        }
    }
}
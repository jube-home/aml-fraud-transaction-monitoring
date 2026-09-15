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
using System.Diagnostics.Tracing;

namespace Jube.Data.Observability.NpgsqlEventCounterBridge
{
    internal sealed class Listener : EventListener
    {
        private const string NpgsqlEventSourceName = "Npgsql";

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (string.Equals(eventSource.Name, NpgsqlEventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All,
                    new Dictionary<string, string> { ["EventCounterIntervalSec"] = "15" });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters" || eventData.Payload == null)
            {
                return;
            }

            foreach (var payload in eventData.Payload)
            {
                if (payload is not IDictionary<string, object> data)
                {
                    continue;
                }

                if (!data.TryGetValue("Name", out var nameObj) || nameObj is not string name)
                {
                    continue;
                }

                if (data.TryGetValue("Mean", out var mean))
                {
                    NpgsqlEventCounterBridge.Record(name, Convert.ToDouble(mean));
                }
                else if (data.TryGetValue("Increment", out var increment))
                {
                    NpgsqlEventCounterBridge.Record(name, Convert.ToDouble(increment));
                }
            }
        }
    }
}
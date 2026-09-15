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

using Jube.Data.Poco;

namespace Jube.Monitoring
{
    public sealed class DockerEventSampler(DockerApiClient client)
    {
        private long? lastNanos;

        public async Task<List<DockerEvent>> SampleAsync()
        {
            var results = new List<DockerEvent>();
            var nowNanos = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L;

            if (lastNanos == null)
            {
                lastNanos = nowNanos;
                return results;
            }

            var sinceNanos = lastNanos.Value + 1;
            var since = $"{sinceNanos / 1_000_000_000}.{sinceNanos % 1_000_000_000:D9}";
            var until = $"{nowNanos / 1_000_000_000}.{nowNanos % 1_000_000_000:D9}";

            var events = await client.GetEventsAsync(since, until).ConfigureAwait(false);
            if (events is not { Count: > 0 })
            {
                lastNanos = nowNanos;
                return results;
            }

            var maxNanos = lastNanos.Value;
            foreach (var e in events)
            {
                var time = (long?)e["time"];
                var timeNano = (long?)e["timeNano"] ?? time * 1_000_000_000L;
                if (timeNano is { } t && t > maxNanos)
                {
                    maxNanos = t;
                }

                var actor = e["Actor"];

                results.Add(new DockerEvent
                {
                    OccurredDate = time.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(time.Value).UtcDateTime
                        : null,
                    EventType = (string?)e["Type"],
                    Action = (string?)e["Action"],
                    ActorId = (string?)actor?["ID"],
                    ActorName = (string?)actor?["Attributes"]?["name"],
                    Scope = (string?)e["scope"]
                });
            }

            lastNanos = maxNanos;
            return results;
        }
    }
}
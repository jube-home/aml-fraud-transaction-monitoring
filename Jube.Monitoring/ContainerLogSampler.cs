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
    public sealed class ContainerLogSampler(DockerApiClient client)
    {
        private readonly Dictionary<string, long> lastNanosByContainerId = new();

        public async Task<List<ContainerLogEntry>> SampleAsync()
        {
            var results = new List<ContainerLogEntry>();

            var containers = await client.GetContainersAsync().ConfigureAwait(false);
            if (containers == null)
            {
                return results;
            }

            foreach (var container in containers)
            {
                var containerId = (string?)container["Id"];
                if (containerId == null)
                {
                    continue;
                }

                var name = ((string?)container["Names"]?[0])?.TrimStart('/');

                if (!lastNanosByContainerId.TryGetValue(containerId, out var lastNanos))
                {
                    lastNanos = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L;
                    lastNanosByContainerId[containerId] = lastNanos;
                    continue;
                }

                var sinceNanos = lastNanos + 1;
                var since = $"{sinceNanos / 1_000_000_000}.{sinceNanos % 1_000_000_000:D9}";

                var lines = await client.GetContainerLogsAsync(containerId, since).ConfigureAwait(false);
                if (lines is not { Count: > 0 })
                {
                    continue;
                }

                var maxNanos = lastNanos;
                foreach (var (streamType, line) in lines)
                {
                    var (occurredDate, nanos, message) = ParseLine(line);
                    if (nanos > maxNanos)
                    {
                        maxNanos = nanos;
                    }

                    results.Add(new ContainerLogEntry
                    {
                        OccurredDate = occurredDate,
                        ContainerName = name,
                        StreamTypeId = (int)streamType,
                        Message = message
                    });
                }

                lastNanosByContainerId[containerId] = maxNanos;
            }

            return results;
        }

        private static (DateTime? OccurredDate, long Nanos, string Message) ParseLine(string line)
        {
            var separatorIndex = line.IndexOf(' ');
            var timestampText = separatorIndex >= 0 ? line[..separatorIndex] : line;

            if (!DateTimeOffset.TryParse(timestampText, out var parsed))
            {
                return (null, 0, line);
            }

            var totalNanos = (parsed.UtcTicks - DateTimeOffset.UnixEpoch.Ticks) * 100;
            var message = separatorIndex >= 0 && separatorIndex + 1 < line.Length
                ? line[(separatorIndex + 1)..]
                : string.Empty;

            return (parsed.UtcDateTime, totalNanos, message);
        }
    }
}
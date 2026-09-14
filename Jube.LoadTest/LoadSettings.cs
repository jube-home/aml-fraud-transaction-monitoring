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

namespace Jube.LoadTest
{
    public sealed record LoadSettings
    {
        public static readonly IReadOnlyList<ContainerRole> DefaultContainerRoles =
        [
            new("Postgres", ["postgres", "patroni"]),
            new("Redis", ["redis"]),
            new("Sentinel", ["sentinel"]),
            new("Etcd", ["etcd"]),
            new("HAProxy", ["haproxy"]),
            new("Jube", ["jube.webapi", "jube-api", "jube-ui", "jube"])
        ];

        public required Uri Uri { get; init; }
        public int HttpTimeoutMilliseconds { get; init; } = 500;
        public int TargetRequestsPerSecond { get; init; } = 174;
        public int DurationSeconds { get; init; } = 86400;
        public int MaxConnectionsPerServer { get; init; } = 500;
        public int MaxConcurrentInFlight { get; init; } = 500;
        public int TimeDriftMs { get; init; } = 172;
        public required string ApiKey { get; init; }
        public string MockTemplatePath { get; init; } = "Mock.json";
        public string OutputPath { get; init; } = "WriteLinesTpsSnapshot.txt";
        public double ResponseSampleRate { get; init; } = 0.02;
        public string ResponseSampleOutputPath { get; init; } = "ResponseTimeSample.csv";
        public long KeyPoolSize { get; init; } = 1_000_000;
        public double KeySkew { get; init; } = 0.8;
        public bool UseUniformKeyDistribution { get; init; }
        public IReadOnlyList<ContainerRole> ContainerRoles { get; init; } = DefaultContainerRoles;
        public bool ContainerNamePatternsAreRegex { get; init; }
        public bool SampleContainerStats { get; init; } = true;
        public bool SampleHostStats { get; init; } = true;
        public string? PostgresConnectionString { get; init; }
        public string? RedisConnectionString { get; init; }
    }
}
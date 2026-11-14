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

namespace Jube.ResilientHttp
{
    public sealed record HttpResilienceOptions
    {
        public const int DefaultMaxRetries = 0;

        public TimeSpan Timeout { get; init; } = TimeSpan.FromMilliseconds(1000);
        public double FailureRatio { get; init; } = 0.5;
        public int MinimumThroughput { get; init; } = 5;
        public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromSeconds(30);
        public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; init; } = DefaultMaxRetries;
        public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    }
}

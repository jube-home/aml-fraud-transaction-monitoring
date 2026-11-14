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

using System.Collections.Concurrent;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Jube.ResilientHttp
{
    public sealed class ResilientHttpExecutor(TimeProvider timeProvider)
    {
        private readonly ConcurrentDictionary<(string Key, HttpResilienceOptions Options), ResiliencePipeline>
            pipelines = new();

        public static ResilientHttpExecutor Shared { get; } = new(TimeProvider.System);

        public int PipelineCount => pipelines.Count;

        public async Task<T> ExecuteAsync<T>(string breakerKey, HttpResilienceOptions options,
            Func<CancellationToken, Task<T>> action, CancellationToken token = default)
        {
            var pipeline = pipelines.GetOrAdd((breakerKey, options), static (k, tp) => Build(k.Options, tp),
                timeProvider);

            return await pipeline.ExecuteAsync(async ct => await action(ct).ConfigureAwait(false), token)
                .ConfigureAwait(false);
        }

        public static bool IsCircuitOpen(Exception exception)
        {
            return exception is BrokenCircuitException or IsolatedCircuitException;
        }

        private static ResiliencePipeline Build(HttpResilienceOptions options, TimeProvider tp)
        {
            var builder = new ResiliencePipelineBuilder { TimeProvider = tp };

            if (options.MaxRetries > 0)
            {
                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetries,
                    Delay = options.RetryDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(HttpFailureClassification.IsTransient)
                });
            }

            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.FailureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = options.SamplingDuration,
                BreakDuration = options.BreakDuration,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(HttpFailureClassification.IsTransient)
            });

            builder.AddTimeout(options.Timeout);

            return builder.Build();
        }
    }
}

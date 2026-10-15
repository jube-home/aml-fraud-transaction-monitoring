/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Jube.AskJooby.Observability
{
    public static class PromptCacheUsage
    {
        private static readonly string[] cacheHitPaths =
        [
            "$.usage.prompt_cache_hit_tokens",
            "$.usage.cache_read_input_tokens",
            "$.usage.prompt_tokens_details.cached_tokens"
        ];

        private static readonly string[] promptDetailPaths =
        [
            "$.usage.prompt_tokens_details.cached_tokens"
        ];

        public static long? CachedInputTokens(UsageDetails? usage, object? raw = null)
        {
            var counts = usage?.AdditionalCounts;

            if (counts is null || counts.Count == 0)
            {
                return FromRaw(raw, cacheHitPaths);
            }

            foreach (var key in new[]
                     {
                         "cache_read_input_tokens", "CachedTokenCount", "prompt_cache_hit_tokens",
                         "prompt_tokens_details.cached_tokens"
                     })
            {
                if (counts.TryGetValue(key, out var exact))
                {
                    return exact;
                }
            }

            var match = counts.Keys.Where(IsCacheRead).OrderBy(key => key, StringComparer.Ordinal)
                .FirstOrDefault();

            return match is null ? FromRaw(raw, cacheHitPaths) : counts[match];
        }

        public static long? PromptDetailCachedTokens(UsageDetails? usage, object? raw = null)
        {
            var counts = usage?.AdditionalCounts;

            if (counts is null || counts.Count == 0)
            {
                return FromRaw(raw, promptDetailPaths);
            }

            foreach (var key in new[]
                     {
                         "prompt_tokens_details.cached_tokens", "cached_tokens",
                         "InputTokenDetails.CachedTokenCount"
                     })
            {
                if (counts.TryGetValue(key, out var exact))
                {
                    return exact;
                }
            }

            return FromRaw(raw, promptDetailPaths);
        }

        public static string? DescribeCounts(UsageDetails? usage)
        {
            var counts = usage?.AdditionalCounts;

            return counts is null || counts.Count == 0
                ? null
                : JsonSerializer.Serialize(counts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        }

        private static long? FromRaw(object? raw, string[] paths)
        {
            if (raw is null)
            {
                return null;
            }

            foreach (var path in paths)
            {
                var bytes = Encoding.UTF8.GetBytes(path);

#pragma warning disable SCME0001

                switch (raw)
                {
                    case StreamingChatCompletionUpdate streaming
                        when streaming.Patch.TryGetValue(bytes, out string? streamed)
                             && long.TryParse(streamed, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                 out var streamedValue):
                        return streamedValue;
                    case ChatCompletion completion
                        when completion.Patch.TryGetValue(bytes, out string? completed)
                             && long.TryParse(completed, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                 out var completedValue):
                        return completedValue;
                }
#pragma warning restore SCME0001
            }

            return null;
        }

        private static bool IsCacheRead(string key)
        {
            if (key.Contains("write", StringComparison.OrdinalIgnoreCase)
                || key.Contains("creat", StringComparison.OrdinalIgnoreCase)
                || key.Contains("miss", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return (key.Contains("cache", StringComparison.OrdinalIgnoreCase)
                    || key.Contains("cached", StringComparison.OrdinalIgnoreCase))
                   && key.Contains("token", StringComparison.OrdinalIgnoreCase);
        }
    }
}
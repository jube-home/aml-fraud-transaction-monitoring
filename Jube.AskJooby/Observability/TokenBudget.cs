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

using Jube.AskJooby.Observability.Interfaces;

namespace Jube.AskJooby.Observability
{
    public sealed class TokenBudget(
        long limit,
        double overrunMultiple = 1.5,
        double cachedInputWeight = 1.0) : ITokenBudget
    {
        private long spent;

        private long HardLimit { get; } = overrunMultiple > 1.0
            ? (long)(limit * overrunMultiple)
            : limit;

        public long Limit { get; } = limit;
        public long Spent => Interlocked.Read(ref spent);
        public bool IsExhausted => Limit > 0 && Interlocked.Read(ref spent) >= Limit;
        public bool IsOverrun => Limit > 0 && Interlocked.Read(ref spent) >= HardLimit;

        public void Record(long inputTokens, long outputTokens, long cachedInputTokens = 0)
        {
            var input = inputTokens > 0 ? inputTokens : 0;
            var output = outputTokens > 0 ? outputTokens : 0;

            var cached = cachedInputTokens > 0 && cachedInputTokens <= input ? cachedInputTokens : 0;
            var weight = cachedInputWeight is > 0 and < 1.0 ? cachedInputWeight : 1.0;

            var chargeableInput = cached == 0
                ? input
                : (long)Math.Round(input - cached + cached * weight, MidpointRounding.AwayFromZero);

            var total = chargeableInput + output;

            if (total > 0)
            {
                Interlocked.Add(ref spent, total);
            }
        }
    }
}
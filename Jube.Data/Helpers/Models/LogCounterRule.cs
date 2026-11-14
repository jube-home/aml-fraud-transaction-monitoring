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

using System.Text.RegularExpressions;
using System.Threading;

namespace Jube.Data.Helpers.Models
{
    public sealed class LogCounterRule(string name, string pattern, Regex regex)
    {
        public const int MaxConsecutiveTimeouts = 5;
        private int consecutiveTimeouts;
        public string Name { get; } = name;
        public string Pattern { get; } = pattern;
        public Regex Regex { get; } = regex;

        public bool IsSuspended => Volatile.Read(ref consecutiveTimeouts) >= MaxConsecutiveTimeouts;

        public int RecordTimeout()
        {
            return Interlocked.Increment(ref consecutiveTimeouts);
        }

        public void RecordCompletion()
        {
            if (Volatile.Read(ref consecutiveTimeouts) != 0)
            {
                Volatile.Write(ref consecutiveTimeouts, 0);
            }
        }

        public void Resume()
        {
            Volatile.Write(ref consecutiveTimeouts, 0);
        }
    }
}

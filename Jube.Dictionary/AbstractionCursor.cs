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

using System.ComponentModel;

namespace Jube.Dictionary
{
    public sealed class AbstractionCursor
    {
        [Description("Zero-based position of the transaction being tested; always one less than Position.")]
        public int Index { get; private set; }

        [Description("One-based position of the transaction being tested. 1 is the first transaction in iteration order.")]
        public int Position => Index + 1;

        [Description("How many transactions the walk covers: the history inside the rule's search window, including the transaction being processed.")]
        public int Size { get; private set; }

        [Description("How many transactions this rule has accepted so far in the walk.")]
        public int Matches { get; private set; }

        [Description("True on the first transaction of the walk.")]
        public bool IsFirst => Index == 0;

        [Description("True on the last transaction of the walk.")]
        public bool IsLast => Index == Size - 1;

        [Description("True when the transaction being tested is the one being processed now, rather than one from the history.")]
        public bool IsCurrent { get; private set; }

        public bool Stopped { get; private set; }

        [Description("A number the rule can set and read as the walk proceeds. The Cursor Result function returns its final value as the abstraction value.")]
        public double Result { get; set; }

        public object? State { get; set; }

        public void Begin(int size)
        {
            Size = size;
            Index = 0;
            Matches = 0;
            IsCurrent = false;
            Stopped = false;
            Result = 0;
            State = null;
        }

        public void MoveTo(int index, bool isCurrent)
        {
            Index = index;
            IsCurrent = isCurrent;
        }

        public void Accept()
        {
            Matches++;
        }

        public void Stop()
        {
            Stopped = true;
        }
    }
}

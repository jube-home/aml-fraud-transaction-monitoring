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
using Svirfd;

namespace Jube.Svirfd
{
    public static class SvirfdDrift
    {
        public static IReadOnlyList<SvirfdDriftItem> Compare(IpResponse? stored, IpResponse? fresh)
        {
            var items = new List<SvirfdDriftItem>();

            if (stored == null || fresh == null)
            {
                return items;
            }

            foreach (var member in SvirfdMembers.Scalars)
            {
                if (member.Volatile)
                {
                    continue;
                }

                var before = member.Read(stored);
                var after = member.Read(fresh);

                if (Same(before, after))
                {
                    continue;
                }

                items.Add(new SvirfdDriftItem(member.Name, SvirfdMembers.ToText(before),
                    SvirfdMembers.ToText(after)));
            }

            return items;
        }

        private static bool Same(object? before, object? after)
        {
            if (before == null && after == null)
            {
                return true;
            }

            if (before == null || after == null)
            {
                return false;
            }

            if (before is double a && after is double b)
            {
                return Math.Abs(a - b) < 0.000001;
            }

            if (before is string left && after is string right)
            {
                return string.Equals(left, right, StringComparison.Ordinal);
            }

            return Equals(before, after);
        }
    }
}

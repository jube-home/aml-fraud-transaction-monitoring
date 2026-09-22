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

using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static Flow<int> ParseInteger(this Flow<string?> flow)
        {
            if (flow.IsDecided)
            {
                return flow.Carry(0);
            }

            return int.TryParse(flow.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? flow.Carry(parsed)
                : flow.Carry(0).Decide(FlowOutcome.Errored, "Value could not be parsed as an integer.");
        }

        public static Flow<int> ParseInteger(this string? value)
        {
            return value.Start().ParseInteger();
        }
    }
}

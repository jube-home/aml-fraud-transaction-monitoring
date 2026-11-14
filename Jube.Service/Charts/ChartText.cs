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
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    internal static class ChartText
    {
        public static string Of(IStringLocalizer? strings, string key, string english, params object[] arguments)
        {
            var found = strings?[key];
            var format = found == null || found.ResourceNotFound || string.IsNullOrWhiteSpace(found.Value)
                ? english
                : found.Value;

            return arguments.Length == 0
                ? format
                : string.Format(CultureInfo.InvariantCulture, format,
                    arguments.Select(a => Convert.ToString(a, CultureInfo.InvariantCulture) ?? string.Empty)
                        .Cast<object>().ToArray());
        }
    }
}

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

using System.Collections;
using System.Globalization;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceCardValue
    {
        public static object? Normalise(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case double d:
                    return double.IsFinite(d) ? d : null;
                case float f:
                    return float.IsFinite(f) ? f : null;
                case DateTime t:
                    return DateTime.SpecifyKind(t, t.Kind == DateTimeKind.Local ? DateTimeKind.Local : DateTimeKind.Utc)
                        .ToUniversalTime()
                        .ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
                case DateTimeOffset o:
                    return o.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
                case TimeSpan span:
                    return span.ToString("c", CultureInfo.InvariantCulture);
                case Guid guid:
                    return guid.ToString();
                case byte[]:
                    return null;
                case string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or decimal:
                    return value;
                case IEnumerable items:
                    return items.Cast<object?>().Select(Normalise).ToList();
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }
    }
}

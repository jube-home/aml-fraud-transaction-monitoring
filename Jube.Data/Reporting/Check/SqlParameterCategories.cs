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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Jube.Data.Reporting.Check
{
    public static class SqlParameterCategories
    {
        public static IReadOnlyDictionary<string, SqlTypeCategory> Of(IReadOnlyDictionary<string, object> values)
        {
            return values.ToDictionary(v => v.Key, v => CategoryOf(v.Value), StringComparer.Ordinal);
        }

        public static SqlTypeCategory CategoryOf(object value)
        {
            return value switch
            {
                string => SqlTypeCategory.Text,
                int or long or short or byte or double or float or decimal => SqlTypeCategory.Numeric,
                DateTime or DateTimeOffset => SqlTypeCategory.DateTime,
                bool => SqlTypeCategory.Bool,
                Guid => SqlTypeCategory.Uuid,
                _ => SqlTypeCategory.Unknown
            };
        }
    }
}

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

namespace Jube.Data.Query.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public static class RowReader
    {
        public static string Str(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : null;

        public static int Int(IDictionary<string, object> row, string name) => IntN(row, name) ?? 0;

        public static int? IntN(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null
                ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : null;

        public static long Long(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null
                ? Convert.ToInt64(value, CultureInfo.InvariantCulture)
                : 0;

        public static double Dbl(IDictionary<string, object> row, string name) => DblN(row, name) ?? 0;

        public static double? DblN(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null
                ? Convert.ToDouble(value, CultureInfo.InvariantCulture)
                : null;

        public static DateTime? DtN(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null
                ? DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)
                : null;

        public static DateTime Dt(IDictionary<string, object> row, string name) => DtN(row, name) ?? default;

        public static bool Bool(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value != null && Convert.ToBoolean(value,
                CultureInfo.InvariantCulture);

        public static Guid Guid(IDictionary<string, object> row, string name) =>
            row.TryGetValue(name, out var value) && value is Guid g ? g : default;
    }
}

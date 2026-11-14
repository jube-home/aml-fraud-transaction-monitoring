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
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public sealed class DatasourceFrame(IReadOnlyList<DatasourceColumn> columns, IReadOnlyList<object?[]> rows)
    {
        public IReadOnlyList<DatasourceColumn> Columns { get; } = columns;
        public IReadOnlyList<object?[]> Rows { get; } = rows;

        public int IndexOf(string name)
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                if (string.Equals(Columns[i].Name, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            throw new DatasourceChartException(DatasourceMessages.ColumnMissing, name);
        }

        public double[] Doubles(string column)
        {
            var index = IndexOf(column);

            return Rows.Select(row => ToDouble(row[index])).ToArray();
        }

        public string[] Texts(string column)
        {
            var index = IndexOf(column);

            return Rows.Select(row => ToText(row[index])).ToArray();
        }

        public DateTime[] Times(string column)
        {
            var index = IndexOf(column);

            return Rows.Select(row => ToTime(row[index]) ?? throw new DatasourceChartException(
                DatasourceMessages.ColumnNotATime, column)).ToArray();
        }

        public static double ToDouble(object? value)
        {
            return value switch
            {
                null => double.NaN,
                double d => double.IsFinite(d) ? d : double.NaN,
                float f => float.IsFinite(f) ? f : double.NaN,
                decimal m => (double)m,
                byte or sbyte or short or ushort or int or uint or long or ulong => Convert.ToDouble(value,
                    CultureInfo.InvariantCulture),
                bool b => b ? 1 : 0,
                _ => double.NaN
            };
        }

        public static DateTime? ToTime(object? value)
        {
            return value switch
            {
                DateTime t => t,
                DateTimeOffset o => o.UtcDateTime,
                _ => null
            };
        }

        public static string ToText(object? value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string s:
                    return s;
                case DateTime t:
                    return t.TimeOfDay == TimeSpan.Zero
                        ? t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                case DateTimeOffset o:
                    return ToText(o.UtcDateTime);
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                case byte[]:
                    return string.Empty;
                case IEnumerable items:
                    return string.Join(", ", items.Cast<object?>().Select(ToText));
                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}

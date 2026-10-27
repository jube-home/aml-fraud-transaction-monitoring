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

namespace Jube.Service.Charts.Datasource.Template
{
    public static class TemplateValues
    {
        public static decimal? AsNumber(object? value)
        {
            try
            {
                return value switch
                {
                    byte or sbyte or short or ushort or int or uint or long or ulong => Convert.ToDecimal(value,
                        CultureInfo.InvariantCulture),
                    decimal d => d,
                    double d when double.IsFinite(d) => (decimal)d,
                    float f when float.IsFinite(f) => (decimal)f,
                    _ => null
                };
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        public static decimal? AsFormattable(object? value)
        {
            return AsNumber(value) ?? (value is string text && text.Length <= 64 &&
                                       decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
                                           out var parsed)
                ? parsed
                : null);
        }

        public static DateTime? AsTime(object? value)
        {
            return value switch
            {
                DateTime t => t,
                string s when DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed) => parsed,
                _ => null
            };
        }

        public static bool Truthy(object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                string s => s.Length > 0,
                IList list => list.Count > 0,
                _ => AsNumber(value) is not { } n || n != 0
            };
        }

        public static string Display(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string s => s,
                bool b => b ? "true" : "false",
                decimal d => d.ToString("#,0.##########", CultureInfo.CurrentUICulture),
                double d => d.ToString("#,0.##########", CultureInfo.CurrentUICulture),
                float f => f.ToString("#,0.##########", CultureInfo.CurrentUICulture),
                IFormattable f => f.ToString(null, CultureInfo.CurrentUICulture),
                IEnumerable items => string.Join(", ", items.Cast<object?>().Select(Display)),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        public static bool Equal(object? left, object? right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            if (AsNumber(left) is { } a && AsNumber(right) is { } b)
            {
                return a == b;
            }

            return left is bool lb && right is bool rb
                ? lb == rb
                : string.Equals(Display(left), Display(right), StringComparison.Ordinal);
        }

        public static int? Compare(object? left, object? right)
        {
            if (AsNumber(left) is { } a && AsNumber(right) is { } b)
            {
                return a.CompareTo(b);
            }

            return left is string x && right is string y ? string.CompareOrdinal(x, y) : null;
        }
    }
}

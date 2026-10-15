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

using System.Globalization;

namespace Jube.Service.Agent.RuleExtensionTools
{
    public static class RuleExtensionVisualBasicTypes
    {
        private static readonly Dictionary<Type, string> keywords = new()
        {
            [typeof(string)] = "String",
            [typeof(bool)] = "Boolean",
            [typeof(byte)] = "Byte",
            [typeof(sbyte)] = "SByte",
            [typeof(char)] = "Char",
            [typeof(short)] = "Short",
            [typeof(ushort)] = "UShort",
            [typeof(int)] = "Integer",
            [typeof(uint)] = "UInteger",
            [typeof(long)] = "Long",
            [typeof(ulong)] = "ULong",
            [typeof(float)] = "Single",
            [typeof(double)] = "Double",
            [typeof(decimal)] = "Decimal",
            [typeof(DateTime)] = "DateTime",
            [typeof(TimeSpan)] = "TimeSpan",
            [typeof(object)] = "Object",
            [typeof(void)] = "Void"
        };

        public static string Describe(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying is not null)
            {
                return Describe(underlying) + "?";
            }

            if (type.IsArray)
            {
                return Describe(type.GetElementType()!) + "()";
            }

            if (keywords.TryGetValue(type, out var keyword))
            {
                return keyword;
            }

            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments().Select(Describe);
                var name = type.Name[..type.Name.IndexOf('`')];

                return $"{name}(Of {string.Join(", ", arguments)})";
            }

            return type.Name;
        }

        public static string? DescribeDefault(object? value)
        {
            return value switch
            {
                null => "Nothing",
                bool flag => flag ? "True" : "False",
                string text => "\"" + text.Replace("\"", "\"\"") + "\"",
                char character => "\"" + character + "\"c",
                DateTime moment => "#" + moment.ToString("yyyy-MM-dd HH:mm:ss") + "#",
                double number => number.ToString("R"),
                float number => number.ToString("R"),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}
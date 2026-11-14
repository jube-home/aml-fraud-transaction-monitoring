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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jube.Data.Reporting.Check
{
    internal static class SqlTypeRules
    {
        private static readonly HashSet<string> booleanWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "t", "tr", "tru", "true", "f", "fa", "fal", "fals", "false", "y", "ye", "yes", "n", "no", "on", "of",
            "off", "1", "0"
        };

        private static readonly HashSet<string> dateTimeWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "now", "today", "tomorrow", "yesterday", "infinity", "-infinity", "epoch", "allballs"
        };

        private static readonly Regex integerPattern = new(@"^[+-]?\d+$", RegexOptions.Compiled);

        private static readonly Regex numberPattern =
            new(@"^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$", RegexOptions.Compiled);

        private static readonly Regex hexPattern = new("^[0-9a-fA-F]{32}$", RegexOptions.Compiled);

        public static SqlTypeCategory CategoryOf(string typeName, string typeCategory)
        {
            switch (typeName)
            {
                case "bool":
                    return SqlTypeCategory.Bool;
                case "uuid":
                    return SqlTypeCategory.Uuid;
                case "json":
                case "jsonb":
                    return SqlTypeCategory.Json;
                case "money":
                    return SqlTypeCategory.Other;
            }

            return typeCategory switch
            {
                "N" => SqlTypeCategory.Numeric,
                "S" => SqlTypeCategory.Text,
                "D" => SqlTypeCategory.DateTime,
                "A" => SqlTypeCategory.Array,
                _ => SqlTypeCategory.Other
            };
        }

        public static SqlTypeCategory CategoryOfTypeName(string typeName)
        {
            return typeName switch
            {
                "int2" or "int4" or "int8" or "numeric" or "float4" or "float8" or "oid" => SqlTypeCategory.Numeric,
                "text" or "varchar" or "bpchar" or "name" => SqlTypeCategory.Text,
                "bool" => SqlTypeCategory.Bool,
                "timestamp" or "timestamptz" or "date" or "time" or "timetz" => SqlTypeCategory.DateTime,
                "uuid" => SqlTypeCategory.Uuid,
                "json" or "jsonb" => SqlTypeCategory.Json,
                _ => SqlTypeCategory.Other
            };
        }

        public static string DisplayName(string typeName)
        {
            return typeName switch
            {
                "int2" => "smallint",
                "int4" => "integer",
                "int8" => "bigint",
                "float4" => "real",
                "float8" => "double precision",
                "bool" => "boolean",
                "timestamptz" => "timestamp with time zone",
                _ => typeName
            };
        }

        public static string Name(SqlTypeCategory category)
        {
            return category switch
            {
                SqlTypeCategory.Numeric => "number",
                SqlTypeCategory.Text => "text",
                SqlTypeCategory.Bool => "boolean",
                SqlTypeCategory.DateTime => "date and time",
                SqlTypeCategory.Uuid => "uuid",
                SqlTypeCategory.Json => "json",
                SqlTypeCategory.Array => "array",
                SqlTypeCategory.UntypedString => "text",
                _ => "unknown"
            };
        }

        public static bool Clash(SqlExprType left, SqlExprType right)
        {
            return left.IsKnownScalar && right.IsKnownScalar && left.Category != right.Category;
        }

        public static bool LiteralInvalid(string literal, string typeName)
        {
            var text = literal.Trim();

            switch (typeName)
            {
                case "int2":
                    return !FitsInteger(text, short.MinValue, short.MaxValue);
                case "int4":
                    return !FitsInteger(text, int.MinValue, int.MaxValue);
                case "int8":
                case "oid":
                    return !FitsInteger(text, long.MinValue, long.MaxValue);
                case "numeric":
                    return !(numberPattern.IsMatch(text) ||
                             text.Equals("NaN", StringComparison.OrdinalIgnoreCase));
                case "float4":
                case "float8":
                    return !(numberPattern.IsMatch(text) || text.ToLowerInvariant() is "nan" or "infinity"
                        or "-infinity" or "+infinity" or "inf" or "-inf" or "+inf");
                case "bool":
                    return !booleanWords.Contains(text);
                case "uuid":
                    return !hexPattern.IsMatch(text.Trim('{', '}').Replace("-", string.Empty));
                case "date":
                case "timestamp":
                case "timestamptz":
                    return text.Length > 0 && !HasDigit(text) && !dateTimeWords.Contains(text);
                default:
                    return false;
            }
        }

        private static bool HasDigit(string text)
        {
            foreach (var c in text)
            {
                if (char.IsDigit(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FitsInteger(string text, long minimum, long maximum)
        {
            return integerPattern.IsMatch(text) && long.TryParse(text, NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var value) && value >= minimum && value <= maximum;
        }
    }
}

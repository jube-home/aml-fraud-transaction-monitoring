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
using System.Text.RegularExpressions;
using Jube.Dto.Options;

namespace Jube.Data.Reporting
{
    public static class PostgresDataTypeClassifier
    {
        private static readonly Regex modifierPattern = new(@"\([^)]*\)", RegexOptions.Compiled);
        private static readonly Regex whitespacePattern = new(@"\s+", RegexOptions.Compiled);

        private static readonly HashSet<string> integerTypes = new(StringComparer.Ordinal)
        {
            "integer", "int", "int4", "bigint", "int8"
        };

        private static readonly HashSet<string> decimalTypes = new(StringComparer.Ordinal)
        {
            "double precision", "real", "numeric", "decimal", "float4", "float8"
        };

        private static readonly HashSet<string> flagTypes = new(StringComparer.Ordinal)
        {
            "smallint", "int2", "boolean", "bool"
        };

        public static SeriesDataType Classify(string dataTypeName)
        {
            if (string.IsNullOrWhiteSpace(dataTypeName))
            {
                return SeriesDataType.Text;
            }

            var name = Normalise(dataTypeName);

            if (name.EndsWith("[]", StringComparison.Ordinal))
            {
                return decimalTypes.Contains(name[..^2]) ? SeriesDataType.DecimalArray : SeriesDataType.Array;
            }

            if (name.Contains("timestamp", StringComparison.Ordinal) || name == "date")
            {
                return SeriesDataType.DateTime;
            }

            if (integerTypes.Contains(name))
            {
                return SeriesDataType.Integer;
            }

            if (decimalTypes.Contains(name))
            {
                return SeriesDataType.Decimal;
            }

            return flagTypes.Contains(name) ? SeriesDataType.Flag : SeriesDataType.Text;
        }

        private static string Normalise(string dataTypeName)
        {
            var withoutModifiers = modifierPattern.Replace(dataTypeName, string.Empty);

            return whitespacePattern.Replace(withoutModifiers.Trim().ToLowerInvariant(), " ");
        }
    }
}

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
using Npgsql;

namespace Jube.Data.Reporting
{
    public static class PostgresErrorPosition
    {
        public const string DataKey = "Jube.SqlPosition";
        public const int WrapPrefixLength = 15;

        public static bool Annotate(Exception exception, string original, string submitted)
        {
            for (var inner = exception; inner != null; inner = inner.InnerException)
            {
                if (inner is not PostgresException { Position: > 0 } postgres)
                {
                    continue;
                }

                var position = InOriginal(original, submitted, postgres.Position - WrapPrefixLength);

                if (position != null)
                {
                    exception.Data[DataKey] = position.Value;
                }

                break;
            }

            return false;
        }

        public static int? InOriginal(string original, string submitted, int position)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(submitted) || position < 1 ||
                position > submitted.Length)
            {
                return null;
            }

            var prefix = 0;

            while (prefix < original.Length && prefix < submitted.Length && original[prefix] == submitted[prefix])
            {
                prefix++;
            }

            if (position <= prefix)
            {
                return position;
            }

            var suffix = 0;

            while (suffix < original.Length - prefix && suffix < submitted.Length - prefix &&
                   original[original.Length - 1 - suffix] == submitted[submitted.Length - 1 - suffix])
            {
                suffix++;
            }

            var fromEnd = submitted.Length - position;

            return fromEnd < suffix ? original.Length - fromEnd : null;
        }

        public static (int Line, int Column) LineAndColumn(string text, int position)
        {
            var line = 1;
            var lineStart = 0;
            var end = Math.Min(position - 1, text.Length);

            for (var i = 0; i < end; i++)
            {
                if (text[i] != '\n')
                {
                    continue;
                }

                line++;
                lineStart = i + 1;
            }

            return (line, position - lineStart);
        }
    }
}

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

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Jube.Data.Reporting.Check
{
    internal sealed class SqlParameterSubstitution
    {
        private static readonly Regex namedParameterPattern = new(@"@\w+", RegexOptions.Compiled);

        private SqlParameterSubstitution(string text, IReadOnlyList<string> names,
            IReadOnlyList<SqlParameterReplacement> replacements)
        {
            Text = text;
            Names = names;
            Replacements = replacements;
        }

        public string Text { get; }
        public IReadOnlyList<string> Names { get; }
        public IReadOnlyList<SqlParameterReplacement> Replacements { get; }

        public static SqlParameterSubstitution Apply(string sql)
        {
            var names = new List<string>();
            var replacements = new List<SqlParameterReplacement>();
            var builder = new StringBuilder();
            var cursor = 0;

            foreach (Match match in namedParameterPattern.Matches(sql))
            {
                names.Add(match.Value[1..]);
                var replacement = "$" + names.Count;

                builder.Append(sql, cursor, match.Index - cursor).Append(replacement);
                replacements.Add(new SqlParameterReplacement(match.Index, match.Length, replacement.Length));
                cursor = match.Index + match.Length;
            }

            builder.Append(sql, cursor, sql.Length - cursor);

            return new SqlParameterSubstitution(builder.ToString(), names, replacements);
        }
    }
}

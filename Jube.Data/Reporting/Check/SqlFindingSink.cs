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
using System.Linq;

namespace Jube.Data.Reporting.Check
{
    internal sealed class SqlFindingSink(string original, SqlPositionMap map, bool silent)
    {
        private readonly List<SqlFinding> findings = [];

        public IReadOnlyList<SqlFinding> Findings => findings;

        public void Add(string code, int byteLocation, params object[] arguments)
        {
            if (silent)
            {
                return;
            }

            var position = byteLocation >= 0 && map != null ? map.ToOriginalPosition(byteLocation) : null;
            int? line = null;
            int? column = null;

            if (position.HasValue)
            {
                var located = PostgresErrorPosition.LineAndColumn(original, position.Value);
                line = located.Line;
                column = located.Column;
            }

            var suggestion = arguments.Length > 1 && code.EndsWith("Suggest", System.StringComparison.Ordinal)
                ? arguments[^1]?.ToString()
                : null;

            if (findings.Any(f => f.Code == code && f.Position == position &&
                                  f.Arguments.SequenceEqual(arguments)))
            {
                return;
            }

            findings.Add(new SqlFinding(code, arguments, position, line, column, suggestion));
        }
    }
}

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

namespace Jube.Data.Reporting.Check
{
    internal sealed class SqlScope(SqlScope parent)
    {
        public SqlScope Parent { get; } = parent;
        public List<SqlSource> Sources { get; } = [];
        public Dictionary<string, SqlDerivedColumns> CommonTableExpressions { get; } = new(StringComparer.Ordinal);
        public HashSet<string> MergedNames { get; } = new(StringComparer.Ordinal);
        public HashSet<string> OutputNames { get; } = new(StringComparer.Ordinal);
        public List<SqlColumnUse> Uses { get; } = [];
        public bool Natural { get; set; }
        public bool HasAggregate { get; set; }

        public SqlDerivedColumns FindCommonTableExpression(string name)
        {
            for (var scope = this; scope != null; scope = scope.Parent)
            {
                if (scope.CommonTableExpressions.TryGetValue(name, out var found))
                {
                    return found;
                }
            }

            return null;
        }
    }
}

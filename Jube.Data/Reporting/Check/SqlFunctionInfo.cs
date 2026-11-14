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
    public sealed class SqlFunctionInfo(string name, IReadOnlyList<SqlFunctionOverload> overloads)
    {
        public string Name { get; } = name;
        public IReadOnlyList<SqlFunctionOverload> Overloads { get; } = overloads;
        public bool IsAggregate => Overloads.All(o => o.Kind == 'a');
        public bool IsWindow => Overloads.All(o => o.Kind == 'w');
        public bool IsWindowCapable => Overloads.All(o => o.Kind is 'a' or 'w');

        public bool AcceptsArguments(int count)
        {
            return Overloads.Any(o => count >= o.MinimumArguments &&
                                      (o.MaximumArguments < 0 || count <= o.MaximumArguments));
        }

        public string ExpectedArguments()
        {
            var minimum = Overloads.Min(o => o.MinimumArguments);

            return Overloads.Any(o => o.MaximumArguments < 0)
                ? minimum + "+"
                : minimum == Overloads.Max(o => o.MaximumArguments)
                    ? minimum.ToString()
                    : minimum + "-" + Overloads.Max(o => o.MaximumArguments);
        }
    }
}

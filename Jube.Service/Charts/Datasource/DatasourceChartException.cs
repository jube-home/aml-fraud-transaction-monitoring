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

using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public sealed class DatasourceChartException : ArgumentException
    {
        public DatasourceChartException(string code, params object[] arguments)
            : this([new DatasourceChartProblem(code, arguments)])
        {
        }

        public DatasourceChartException(IReadOnlyList<DatasourceChartProblem> problems)
            : base(string.Join(" ", problems.Select(p => DatasourceMessages.Format(p.Code, p.Arguments))))
        {
            Problems = problems;
        }

        public DatasourceChartException(string message, Exception inner)
            : base(message, inner)
        {
            Problems = [];
        }

        public IReadOnlyList<DatasourceChartProblem> Problems { get; }
    }
}

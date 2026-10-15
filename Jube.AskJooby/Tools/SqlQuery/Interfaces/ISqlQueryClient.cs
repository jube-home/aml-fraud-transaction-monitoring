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

using Jube.AskJooby.SqlQuery.Models;

namespace Jube.AskJooby.Tools.SqlQuery.Interfaces
{
    public interface ISqlQueryClient
    {
        Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteAsync(string sql,
            CancellationToken token = default);

        Task<SqlValidation> ValidateAsync(string sql, CancellationToken token = default);
        Task<SqlPlanSummary> ExplainAsync(string sql, CancellationToken token = default);

        Task<IReadOnlyList<SqlTableStatistic>> TableStatisticsAsync(string sql,
            CancellationToken token = default);

        Task<SqlQueryHistory> QueryHistoryAsync(string sql, CancellationToken token = default);
        IReadOnlyList<string> ListQueryableTables();

        Task<IReadOnlyDictionary<string, string>> TableCommentsAsync(IReadOnlyList<string> tables,
            CancellationToken token = default);
    }
}
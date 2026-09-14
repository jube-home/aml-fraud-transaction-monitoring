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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddPostgresStatementStatistics(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "PostgresStatementStatisticsList", OperationKind.Read, true, false,
                    "Lists per-query-shape aggregate statistics from pg_stat_statements -- calls, rows, " +
                    "execution time min/mean/max/stddev, cache-hit vs disk-read bytes, temp-file spill bytes " +
                    "and WAL bytes, accumulated across all history since the extension was created or last " +
                    "reset. Complements PostgresActivityList: that is 'what is running right now', this is " +
                    "'which query shape is worst overall'. Optional case-insensitive substring search against " +
                    "username, database name or query text. Read-only."));
        }
    }
}
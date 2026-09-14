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
        static partial void AddPostgresActivity(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "PostgresActivityList", OperationKind.Read, true, false,
                    "Lists every backend currently known to PostgreSQL (pg_stat_activity), live -- not a stored " +
                    "snapshot, so results reflect only the instant this is called. Includes each row's blocking " +
                    "chain (pg_blocking_pids) and current query/transaction duration. Optional case-insensitive " +
                    "substring search against username, application name or query text. Read-only."));
        }
    }
}
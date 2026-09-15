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
        static partial void AddPatroniMemberStatus(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "PatroniMemberStatusList", OperationKind.Read, true, false,
                    "Lists per-minute Patroni-managed Postgres member snapshots, one row per " +
                    "discovered/configured member, most recent first, capped at 100000. Covers role/state, " +
                    "Patroni's own computed replication lag, timeline, pending restarts, and " +
                    "version/identity fields useful for spotting a diverged member. Supports optional " +
                    "date-range and substring search (Name/Host/Role/State/Scope) filters. Only populated " +
                    "when Patroni is configured or discoverable, not scoped to any tenant. Also accepts " +
                    "samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of " +
                    "the most recent ones."));
        }
    }
}
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

namespace Jube.Service.Query.EntityAnalysisModelBacktest
{
    public static class TuneGate
    {
        public const int MaximumPerTenant = 2;

        private static readonly Lock gate = new();
        private static readonly Dictionary<int, int> active = new();

        public static bool TryEnter(int tenantRegistryId)
        {
            lock (gate)
            {
                active.TryGetValue(tenantRegistryId, out var current);

                if (current >= MaximumPerTenant)
                {
                    return false;
                }

                active[tenantRegistryId] = current + 1;

                return true;
            }
        }

        public static void Leave(int tenantRegistryId)
        {
            lock (gate)
            {
                active.TryGetValue(tenantRegistryId, out var current);
                active[tenantRegistryId] = Math.Max(0, current - 1);
            }
        }
    }
}

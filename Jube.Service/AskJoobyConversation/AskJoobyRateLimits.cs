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

namespace Jube.Service.AskJoobyConversation
{
    public sealed record AskJoobyRateLimits(
        int WindowSeconds,
        int MaxMessagesPerUserPerWindow,
        int MaxMessagesPerTenantPerWindow,
        int MaxConcurrentTurnsPerUser,
        int MaxConcurrentTurnsPerTenant)
    {
        public static AskJoobyRateLimits From(DynamicEnvironment.DynamicEnvironment? dynamicEnvironment)
        {
            return new AskJoobyRateLimits(
                Read(dynamicEnvironment, "AskJoobyRateWindowSeconds", 60),
                Read(dynamicEnvironment, "AskJoobyMaxMessagesPerUserPerWindow", 20),
                Read(dynamicEnvironment, "AskJoobyMaxMessagesPerTenantPerWindow", 120),
                Read(dynamicEnvironment, "AskJoobyMaxConcurrentTurnsPerUser", 3),
                Read(dynamicEnvironment, "AskJoobyMaxConcurrentTurnsPerTenant", 20));
        }

        private static int Read(DynamicEnvironment.DynamicEnvironment? dynamicEnvironment, string key, int fallback)
        {
            return int.TryParse(dynamicEnvironment?.AppSettings(key), out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }
    }
}

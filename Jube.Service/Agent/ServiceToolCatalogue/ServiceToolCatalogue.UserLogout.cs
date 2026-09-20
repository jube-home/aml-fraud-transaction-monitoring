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
        static partial void AddUserLogout(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "UserLogoutList", OperationKind.Read, true, false,
                    "Lists the Logout Audit Trail -- every time a session was cut (the user logged out, an " +
                    "administrator revoked the user's tokens, or a password change revoked the earlier sessions), " +
                    "most recent first, capped at 100000, with optional date-range and substring search " +
                    "(CreatedUser, CutByUser, RemoteIp, UserAgent, Message) filters, scoped to the caller's own " +
                    "tenant (a landlord sees every tenant). Also accepts samplePercentage (0-100) to draw an " +
                    "unbiased random subset of matching rows instead of the most recent ones."));
        }
    }
}
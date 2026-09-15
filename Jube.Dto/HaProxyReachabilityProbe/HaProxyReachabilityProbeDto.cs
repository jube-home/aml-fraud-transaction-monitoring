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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.HaProxyReachabilityProbe
{
    public sealed record HaProxyReachabilityProbeDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this probe was attempted.")]
        DateTime? OccurredDate,
        [property:
            Description(
                "What was probed: JubeUi and JubeApi are a real GET /api/ready routed through HAProxy; PostgresPrimary/PostgresReplica are a bare TCP connect only.")]
        string Target,
        [property:
            Description(
                "The specific HAProxy replica IP:port probed -- resolved via tasks.haproxy (Swarm's per-task DNS, bypassing the VIP) so a problem specific to one node's HAProxy is attributable rather than averaged away by load balancing.")]
        string HaProxyAddress,
        [property: Description("Whether this probe succeeded.")]
        bool? Success,
        [property: Description("Microseconds from starting the connection attempt to it succeeding or failing.")]
        long? ConnectMicroseconds,
        [property: Description("The HTTP status code returned, for JubeUi/JubeApi probes only.")]
        int? HttpStatusCode,
        [property: Description("The exception message on failure; null on success.")]
        string ErrorMessage,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that ran this probe -- one row per HAProxy replica per target per minute per capturing instance.")]
        string Instance);
}
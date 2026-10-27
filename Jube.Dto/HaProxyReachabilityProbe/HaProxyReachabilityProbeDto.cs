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
using Jube.Dto.Forms;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.HaProxyReachabilityProbe
{
    [GridEndpoint("HaProxyReachabilityProbe", Title = "Ha proxy reachability probe", DefaultSort = "createdDate")]
    [GridSearch(Label = "Target or HAProxy address contains")]
    public sealed record HaProxyReachabilityProbeDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this probe was attempted.")]
        [property: ListColumn(Hidden = true)]
        DateTime? OccurredDate,
        [property:
            Description(
                "What was probed: JubeUi and JubeApi are a real GET /api/ready routed through HAProxy; PostgresPrimary/PostgresReplica are a bare TCP connect only.")]
        [property: ListColumn(Order = 30)]
        string Target,
        [property:
            Description(
                "The specific HAProxy replica IP:port probed -- resolved via tasks.haproxy (Swarm's per-task DNS, bypassing the VIP) so a problem specific to one node's HAProxy is attributable rather than averaged away by load balancing.")]
        [property: ListColumn(Order = 40, Title = "HAProxy Address")]
        string HaProxyAddress,
        [property: Description("Whether this probe succeeded.")]
        [property: ListColumn(Order = 50)]
        bool? Success,
        [property: Description("Microseconds from starting the connection attempt to it succeeding or failing.")]
        [property: ListColumn(Order = 60, Title = "Connect", Unit = "µs", Format = GridFormat.Integer)]
        long? ConnectMicroseconds,
        [property: Description("The HTTP status code returned, for JubeUi/JubeApi probes only.")]
        [property: ListColumn(Order = 70, Title = "HTTP Status", Format = GridFormat.Integer)]
        int? HttpStatusCode,
        [property: Description("The exception message on failure; null on success.")]
        [property: ListColumn(Order = 80)]
        string ErrorMessage,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        [property: ListColumn(Order = 10, Title = "Sampled Date")]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that ran this probe -- one row per HAProxy replica per target per minute per capturing instance.")]
        [property: ListColumn(Order = 20)]
        string Instance);
}
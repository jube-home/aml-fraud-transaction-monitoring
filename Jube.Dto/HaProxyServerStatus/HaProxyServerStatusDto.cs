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

namespace Jube.Dto.HaProxyServerStatus
{
    [GridEndpoint("HaProxyServerStatus", Title = "Ha proxy server status", DefaultSort = "createdDate")]
    [GridSearch(Label = "Proxy, server or status contains")]
    public sealed record HaProxyServerStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this row's HAProxy stats snapshot was scraped.")]
        [property: ListColumn(Hidden = true)]
        DateTime? OccurredDate,
        [property: Description("HAProxy proxy/backend name (e.g. jube_ui, jube_api, postgres_primary).")]
        [property: ListColumn(Order = 30, Title = "Proxy")]
        string PxName,
        [property:
            Description(
                "HAProxy server slot name within that backend (e.g. jube-ui-1, patroni1). Excludes the FRONTEND/BACKEND aggregate rows.")]
        [property: ListColumn(Order = 40, Title = "Server")]
        string SvName,
        [property: Description("HAProxy's current status for this slot (UP, DOWN, MAINT, NOLB, and so on).")]
        [property: ListColumn(Order = 50)]
        string Status,
        [property:
            Description(
                "The IP:port HAProxy currently has resolved for this slot -- the field that proves or disproves HAProxy routing to an address nothing is actually listening on anymore.")]
        [property: ListColumn(Order = 60, Title = "Address")]
        string Addr,
        [property: Description("The last health check's result string (e.g. L7OK, L4CON).")]
        [property: ListColumn(Order = 70)]
        string CheckStatus,
        [property: Description("The HTTP status code returned by the last health check, for HTTP-mode backends.")]
        [property: ListColumn(Order = 80, Format = GridFormat.Integer)]
        int? CheckCode,
        [property: Description("Cumulative number of failed health checks for this slot.")]
        [property: ListColumn(Order = 90, Title = "Check Failures", Format = GridFormat.Integer)]
        int? ChkFail,
        [property: Description("Cumulative number of times this slot has transitioned to DOWN.")]
        [property: ListColumn(Order = 100, Title = "Down Transitions", Format = GridFormat.Integer)]
        int? ChkDown,
        [property:
            Description(
                "Seconds since this slot's status last changed -- a low value on a flapping slot indicates instability rather than a single clean failure.")]
        [property: ListColumn(Order = 110, Title = "Seconds Since Change", Format = GridFormat.Integer)]
        int? LastChg,
        [property: Description("Current number of active sessions on this slot.")]
        [property: ListColumn(Order = 120, Title = "Current Sessions", Format = GridFormat.Integer)]
        int? Scur,
        [property: Description("Current number of queued requests on this slot.")]
        [property: ListColumn(Order = 130, Title = "Current Queued", Format = GridFormat.Integer)]
        int? Qcur,
        [property: Description("This slot's configured load-balancing weight.")]
        [property: ListColumn(Order = 140, Format = GridFormat.Integer)]
        int? Weight,
        [property: Description("Non-zero when this slot is an active (non-backup) server.")]
        [property: ListColumn(Order = 150, Title = "Active", Format = GridFormat.Integer)]
        int? Act,
        [property: Description("Non-zero when this slot is a backup server.")]
        [property: ListColumn(Order = 160, Title = "Backup", Format = GridFormat.Integer)]
        int? Bck,
        [property: Description("Cumulative count of HTTP 2xx responses from this slot, for HTTP-mode backends.")]
        [property: ListColumn(Order = 170, Title = "HTTP 2xx", Format = GridFormat.Integer)]
        long? Hrsp2Xx,
        [property: Description("Cumulative count of HTTP 5xx responses from this slot, for HTTP-mode backends.")]
        [property: ListColumn(Order = 180, Title = "HTTP 5xx", Format = GridFormat.Integer)]
        long? Hrsp5Xx,
        [property: Description("The backend's mode (http or tcp).")]
        [property: ListColumn(Order = 190)]
        string Mode,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        [property: ListColumn(Order = 10, Title = "Sampled Date")]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that captured this sample -- one row per HAProxy server slot per minute per capturing instance.")]
        [property: ListColumn(Order = 20)]
        string Instance);
}
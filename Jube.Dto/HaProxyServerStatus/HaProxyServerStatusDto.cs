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

namespace Jube.Dto.HaProxyServerStatus
{
    public sealed record HaProxyServerStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this row's HAProxy stats snapshot was scraped.")]
        DateTime? OccurredDate,
        [property: Description("HAProxy proxy/backend name (e.g. jube_ui, jube_api, postgres_primary).")]
        string PxName,
        [property:
            Description(
                "HAProxy server slot name within that backend (e.g. jube-ui-1, patroni1). Excludes the FRONTEND/BACKEND aggregate rows.")]
        string SvName,
        [property: Description("HAProxy's current status for this slot (UP, DOWN, MAINT, NOLB, and so on).")]
        string Status,
        [property:
            Description(
                "The IP:port HAProxy currently has resolved for this slot -- the field that proves or disproves HAProxy routing to an address nothing is actually listening on anymore.")]
        string Addr,
        [property: Description("The last health check's result string (e.g. L7OK, L4CON).")]
        string CheckStatus,
        [property: Description("The HTTP status code returned by the last health check, for HTTP-mode backends.")]
        int? CheckCode,
        [property: Description("Cumulative number of failed health checks for this slot.")]
        int? ChkFail,
        [property: Description("Cumulative number of times this slot has transitioned to DOWN.")]
        int? ChkDown,
        [property:
            Description(
                "Seconds since this slot's status last changed -- a low value on a flapping slot indicates instability rather than a single clean failure.")]
        int? LastChg,
        [property: Description("Current number of active sessions on this slot.")]
        int? Scur,
        [property: Description("Current number of queued requests on this slot.")]
        int? Qcur,
        [property: Description("This slot's configured load-balancing weight.")]
        int? Weight,
        [property: Description("Non-zero when this slot is an active (non-backup) server.")]
        int? Act,
        [property: Description("Non-zero when this slot is a backup server.")]
        int? Bck,
        [property: Description("Cumulative count of HTTP 2xx responses from this slot, for HTTP-mode backends.")]
        long? Hrsp2Xx,
        [property: Description("Cumulative count of HTTP 5xx responses from this slot, for HTTP-mode backends.")]
        long? Hrsp5Xx,
        [property: Description("The backend's mode (http or tcp).")]
        string Mode,
        [property: Description("UTC timestamp this sample was captured and flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that captured this sample -- one row per HAProxy server slot per minute per capturing instance.")]
        string Instance);
}
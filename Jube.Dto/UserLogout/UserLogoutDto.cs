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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.UserLogout
{
    [GridEndpoint("UserLogout", Title = "User logout", DefaultSort = "createdDate")]
    [GridSearch(Label = "User, cut by, IP, User-Agent, message contains")]
    [MessagePackObject]
    public sealed record UserLogoutDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp the session was cut.")]
        [property: ListColumn(Order = 10, Title = "Logged Out")]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Key(2)] [property: Description("Username whose session was cut.")]
        [property: ListColumn(Order = 20, Title = "User")]
        string CreatedUser,
        [property: Key(3)] [property: Description("Tenant of the user whose session was cut; null when the user resolved to no tenant.")]
        [property: ListColumn(Hidden = true)]
        int? TenantRegistryId,
        [property: Key(4)] [property:
            Description(
                "Why the session was cut, as a raw integer: 1 = the user logged out, 2 = an administrator revoked the user's tokens, 3 = a password change revoked the earlier sessions.")]
        [property: ListColumn(Hidden = true)]
        int ReasonId,
        [property: Key(5)] [property:
            Description(
                "Human-readable form of ReasonId: 'User logout', 'Tokens revoked by an administrator', 'Password change', or '(unknown)'.")]
        [property: ListColumn(Order = 30)]
        string Reason,
        [property: Key(6)] [property:
            Description(
                "What happened to the session, as a raw integer: 1 = revoked, 2 = no session was presented (only the cookies were cleared), 3 = the revocation failed (the cookies were still cleared).")]
        [property: ListColumn(Hidden = true)]
        int OutcomeId,
        [property: Key(7)] [property:
            Description(
                "Human-readable form of OutcomeId: 'Revoked', 'No session (cookies cleared only)', 'Revocation failed (cookies cleared)', or '(unknown)'.")]
        [property: ListColumn(Order = 40)]
        string Outcome,
        [property: Key(8)] [property: Description("Remote (caller) IP address recorded for the request, when available.")]
        [property: ListColumn(Order = 90)]
        string? RemoteIp,
        [property: Key(9)] [property: Description("Local (server-side) IP address the request was received on, when available.")]
        [property: ListColumn(Order = 100)]
        string? LocalIp,
        [property: Key(10)] [property: Description("User-Agent header of the request, when available.")]
        [property: ListColumn(Order = 110)]
        string? UserAgent,
        [property: Key(11)] [property:
            Description(
                "UTC start of the session that was cut, taken from the session-start claim of the presented token; null when no valid token was presented.")]
        [property: ListColumn(Order = 50, Title = "Session Started")]
        DateTime? SessionStartDate,
        [property: Key(12)] [property:
            Description(
                "How long the session lived in whole seconds (CreatedDate minus SessionStartDate); null when the start is unknown.")]
        [property: ListColumn(Order = 60, Title = "Session", Unit = "s", Format = GridFormat.Integer)]
        long? SessionSeconds,
        [property: Key(13)] [property: Description("The administrator who revoked the tokens, for a revocation by an administrator only.")]
        [property: ListColumn(Order = 70, Title = "Cut By")]
        string? CutByUser,
        [property: Key(14)] [property:
            Description(
                "Short failure text when the revocation failed; never a stack trace or a secret. Null otherwise.")]
        [property: ListColumn(Order = 80)]
        string? Message);
}
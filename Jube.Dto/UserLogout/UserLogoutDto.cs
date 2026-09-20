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
namespace Jube.Dto.UserLogout
{
    public sealed record UserLogoutDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp the session was cut.")]
        DateTime CreatedDate,
        [property: Description("Username whose session was cut.")]
        string CreatedUser,
        [property: Description("Tenant of the user whose session was cut; null when the user resolved to no tenant.")]
        int? TenantRegistryId,
        [property:
            Description(
                "Why the session was cut, as a raw integer: 1 = the user logged out, 2 = an administrator revoked the user's tokens, 3 = a password change revoked the earlier sessions.")]
        int ReasonId,
        [property:
            Description(
                "Human-readable form of ReasonId: 'User logout', 'Tokens revoked by an administrator', 'Password change', or '(unknown)'.")]
        string Reason,
        [property:
            Description(
                "What happened to the session, as a raw integer: 1 = revoked, 2 = no session was presented (only the cookies were cleared), 3 = the revocation failed (the cookies were still cleared).")]
        int OutcomeId,
        [property:
            Description(
                "Human-readable form of OutcomeId: 'Revoked', 'No session (cookies cleared only)', 'Revocation failed (cookies cleared)', or '(unknown)'.")]
        string Outcome,
        [property: Description("Remote (caller) IP address recorded for the request, when available.")]
        string? RemoteIp,
        [property: Description("Local (server-side) IP address the request was received on, when available.")]
        string? LocalIp,
        [property: Description("User-Agent header of the request, when available.")]
        string? UserAgent,
        [property:
            Description(
                "UTC start of the session that was cut, taken from the session-start claim of the presented token; null when no valid token was presented.")]
        DateTime? SessionStartDate,
        [property:
            Description(
                "How long the session lived in whole seconds (CreatedDate minus SessionStartDate); null when the start is unknown.")]
        long? SessionSeconds,
        [property: Description("The administrator who revoked the tokens, for a revocation by an administrator only.")]
        string? CutByUser,
        [property:
            Description(
                "Short failure text when the revocation failed; never a stack trace or a secret. Null otherwise.")]
        string? Message);
}

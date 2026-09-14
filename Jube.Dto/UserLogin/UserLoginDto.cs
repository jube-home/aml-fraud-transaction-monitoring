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

namespace Jube.Dto.UserLogin
{
    public sealed record UserLoginDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this sign-in attempt occurred.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Username the sign-in attempt was for; may be the raw supplied username even when it matched no User Registry (FailureTypeId 1).")]
        string CreatedUser,
        [property: Description("1 if this sign-in attempt failed, 0 if it succeeded.")]
        byte Failed,
        [property:
            Description(
                "Which authentication scheme was used, as a raw integer: 1 = Username and Password, 2 = Negotiate (Windows/Kerberos), 3 = OAuth / OpenID Connect. Null if not recorded.")]
        int? AuthenticationTypeId,
        [property:
            Description(
                "Human-readable form of AuthenticationTypeId: 'Username and Password', 'Negotiate (Windows/Kerberos)', 'OAuth / OpenID Connect', or '(unknown)'.")]
        string AuthenticationScheme,
        [property:
            Description(
                "Only meaningful when Failed is 1. Raw failure reason code -- 1 = no User Registry found, 2 = User Registry not Active, 3 = User Registry Password Locked, 4 = password expired and must be changed (Username/Password only), 5 = bad credentials (Username/Password only), 6 = no password supplied (Username/Password only), 7 = OAuth: no usable identity claim, 8 = OAuth: remote/identity-provider failure, 9 = OAuth: local token validation failed, 10 = OAuth: internal error during processing.")]
        int FailureTypeId,
        [property:
            Description(
                "Human-readable form of FailureTypeId (see docs/Concepts/Authentication/index.md's Login Audit Trail legend for the full list); empty when Failed is 0.")]
        string FailureReason,
        [property:
            Description(
                "Free-text detail for OAuth failures only (FailureTypeId 7-10) -- the identity provider's own error message or a token-validation exception message. Null for Username/Password and Negotiate failures, which already have a small fixed FailureReason enum that needs no free text.")]
        string? FailureMessage,
        [property: Description("Remote (caller) IP address recorded for this sign-in attempt, when available.")]
        string? RemoteIp,
        [property: Description("Local (server-side) IP address the request was received on, when available.")]
        string? LocalIp,
        [property: Description("User-Agent header of the sign-in request, when available.")]
        string? UserAgent);
}
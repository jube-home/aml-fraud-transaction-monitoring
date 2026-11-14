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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Authentication
{
    [Description("What a user needs to set up an authenticator app: the otpauth link for a QR code and the same " +
                 "secret as text. Returned once, only to a user who has just proved their password and has not yet " +
                 "completed set up.")]
    [MessagePackObject]
    public class TotpEnrolmentDto
    {
        [Description("The otpauth:// link to render as a QR code.")]
        [Key(0)]
        public string ProvisioningUri { get; init; } = string.Empty;

        [Description("The secret in base32, for typing into an authenticator app that cannot scan.")]
        [Key(1)]
        public string Secret { get; init; } = string.Empty;
    }
}

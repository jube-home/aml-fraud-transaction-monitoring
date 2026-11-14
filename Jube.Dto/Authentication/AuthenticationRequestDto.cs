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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty
// ReSharper disable UnusedMember.Global

using MessagePack;

namespace Jube.Dto.Authentication
{
    [MessagePackObject]
    public class AuthenticationRequestDto
    {
        [Key(0)]
        public string? UserName { get; set; }
        [Key(1)]
        public string? Password { get; set; }
        [Key(2)]
        public string? NewPassword { get; set; }
        [Key(3)]
        public string? RepeatNewPassword { get; set; }
        [Key(4)]
        public string? RemoteIp { get; set; }
        [Key(5)]
        public string? LocalIp { get; set; }
        [Key(6)]
        public string? UserAgent { get; set; }
        [Key(7)]
        public bool PasswordChangeState { get; set; }
        [Key(8)]
        public string? Mfa { get; set; }
    }
}
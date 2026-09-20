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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.UserRegistry
{
    public class UserRegistryPasswordResponseDto
    {
        [Description("Newly-issued temporary password, returned in clear text exactly once so the caller " +
                     "can communicate it to the user. Never stored or retrievable again; only its Argon2 " +
                     "hash is persisted.")]
        public string? Password { get; set; }

        [Description("Timestamp (UTC) after which the temporary password expires and must be changed.")]
        public DateTimeOffset? PasswordExpiryDate { get; set; }
    }
}
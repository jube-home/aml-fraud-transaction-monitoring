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
    public class UserRegistryPasswordResetDto
    {
        [Description("Identifier of the user whose password is being reset.")]
        public int Id { get; set; }

        [Description("When true, the newly-issued temporary password is additionally salted with the " +
                     "user's Name and SHA-256 hashed before Argon2 hashing.")]
        public bool WirePasswordHash { get; set; }
    }
}
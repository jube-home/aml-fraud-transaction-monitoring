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

namespace Jube.Dto.Validation
{
    [Description("The outcome of validating an entity without saving it: whether it would be accepted, and every " +
                 "failure that would stop it being saved.")]
    public class ValidationResultDto
    {
        [Description("True when the entity would be accepted by a create or update; false when any failure is " +
                     "listed.")]
        public bool IsValid { get; set; }

        [Description("Every failure found, in the order the checks ran. Empty when IsValid is true.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
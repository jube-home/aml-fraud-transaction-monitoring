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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.PermissionSpecification
{
    [FormEndpoint("PermissionSpecification")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    public class PermissionSpecificationDto
    {
        [Description("Server-assigned integer identifier of this permission specification -- the same integer " +
                     "literal every service's own permission checks are hardcoded against. Read-only: the " +
                     "catalogue is seeded by database migration, not created or edited through this API.")]
        [FormField(Group = "Identity", Order = 10, ReadOnly = true)]
        [ListColumn(Order = 10, Title = "Id")]
        public int Id { get; set; }

        [Description("Display name of the permission specification, shown in the Permission dropdown when " +
                     "granting a Role's permissions. Read-only: the catalogue is seeded by database migration, " +
                     "not created or edited through this API.")]
        [FormField(Group = "Identity", Order = 20, ReadOnly = true)]
        [ListColumn(Order = 20, Title = "Name")]
        public string? Name { get; set; }
    }
}
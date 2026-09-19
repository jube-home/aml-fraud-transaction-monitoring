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
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.RoleRegistryPermission
{
    [FormEndpoint("RoleRegistryPermission")]
    [FormKeys(Id = nameof(Id), Parent = nameof(RoleRegistryId), NaturalKey = nameof(PermissionSpecificationId))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class RoleRegistryPermissionDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Role Registry this permission grant belongs to. Set from the parent " +
                     "Role Registry context; not user-editable.")]
        public int RoleRegistryId { get; set; }

        [Description("Identifier of the Permission Specification being granted to the Role -- the same " +
                     "integer literal every service's own permission check is hardcoded against. Must reference " +
                     "an existing row in the Permission Specification catalogue.")]
        [FormField(Group = "Identity", Order = 10, Widget = "dropdown")]
        [Lookup("PermissionSpecification", TextField = "name", ValueField = "id")]
        [ListColumn(Order = 10, Title = "Permission")]
        public int PermissionSpecificationId { get; set; }

        [Description("When true, this permission grant is enforced. When false, it is ignored and the Role " +
                     "does not receive the permission.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, this grant is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        public int Id { get; set; }

        [Description("User who created this row. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 10)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this row was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this row. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this row was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number. Read-only.")]
        [FormField(Group = "Audit", Order = 50)]
        public int Version { get; set; }

        [Description("User who removed this row, if soft-deleted. Server-assigned. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this row was removed, if applicable. Server-assigned. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
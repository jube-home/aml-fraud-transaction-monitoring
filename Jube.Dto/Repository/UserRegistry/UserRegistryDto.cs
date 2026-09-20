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
namespace Jube.Dto.Repository.UserRegistry
{
    [FormEndpoint("UserRegistry")]
    [FormKeys(Id = nameof(Id), Parent = nameof(RoleRegistryGuid), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Security", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class UserRegistryDto : IActivatable, ITreeChild
    {
        [Description("Sign-in name of the user, matching the Name claim of the JSON Web Token issued at " +
                     "authentication. Unique within the tenant.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Identifier of the Role Registry this user is assigned to. Determines the user's " +
                     "tenant and the permissions the user holds.")]
        [FormField(Group = "Identity", Order = 20, Widget = "dropdown")]
        [Lookup("RoleRegistry", TextField = "name", ValueField = "guid")]
        public Guid RoleRegistryGuid { get; set; }

        [Description("Email address of the user, used to identify the account holder. Not used to deliver " +
                     "the temporary password, which is returned once to the caller resetting it.")]
        [FormField(Group = "Identity", Order = 30)]
        [ListColumn(Order = 20, Title = "Email")]
        public string? Email { get; set; }

        [Description("When true, the user may authenticate. When false, sign-in is refused regardless of " +
                     "password correctness.")]
        [FormField(Group = "Identity", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 30, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, a temporary password issued by the password reset action is additionally " +
                     "salted with the user's Name and SHA-256 hashed before Argon2 hashing (the 'wire' " +
                     "password format some client integrations expect). Does not change how the password is " +
                     "displayed to the caller performing the reset.")]
        [FormField(Group = "Security", Order = 10, Widget = "switch")]
        [NewDefault(true)]
        public bool WirePasswordHash { get; set; }

        [Description("When true, the account is locked out following repeated failed password attempts and " +
                     "cannot authenticate until a password reset clears the lock. Read-only here; cleared " +
                     "only by the password reset action.")]
        [FormField(Group = "Security", Order = 20, Widget = "switch", ReadOnly = true)]
        [NewDefault(true)]
        public bool? PasswordLocked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this account. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this account was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Identifier carried over from a preservation import of a previous installation's row, " +
                     "if this account originated from one. Not otherwise populated or used. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int InheritedId { get; set; }
    }
}
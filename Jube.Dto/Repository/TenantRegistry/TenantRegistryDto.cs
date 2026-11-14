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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.TenantRegistry
{
    [FormEndpoint("TenantRegistry")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("HttpAdaptation", Order = 40)]
    [FormGroup("DataProtection", Order = 50)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class TenantRegistryDto : IUpdated, IActivatable, ILockable
    {
        [Description("Legacy free-text description field carried over from the original API response shape. " +
                     "It is not backed by a database column on this entity -- always null, and any value sent " +
                     "on Create/Update is discarded.")]
        [Key(0)]
        public string? Description { get; set; }

        [Description("Display name of the Tenant, shown in the tenant switcher and Administration > Security > " +
                     "Tenants. Unique across every Tenant on the installation (this table has no parent scope " +
                     "of its own to be unique within).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Acme Bank")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("When true, the Tenant is available for use (e.g. selectable via the tenant switcher).")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [Key(2)]
        public bool Active { get; set; }

        [Description("When true, the Tenant is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [Key(3)]
        public bool Locked { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Tenant.")]
        [Key(4)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(5)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Tenant. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(6)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Tenant. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(7)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(8)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(9)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Tenant, if any. Read-only.")]
        [Key(10)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Tenant was soft-deleted, if any. Read-only.")]
        [Key(11)]
        public DateTimeOffset? DeletedDate { get; set; }

        [Description("Landlord-only. Base URL this Tenant's HTTP Adaptation endpoints are appended to. Leave empty " +
                     "to use the installation-wide HttpAdaptationUrl. Must be an https URL to a public host with no " +
                     "credentials, query string or fragment.")]
        [FormField(Group = "HttpAdaptation", Order = 10, Placeholder = "https://scoring.example.com")]
        [Key(12)]
        public string? HttpAdaptationUrl { get; set; }

        [Description("Landlord-only. Timeout in milliseconds for this Tenant's HTTP Adaptation calls, between 100 and " +
                     "30000. Zero uses the installation-wide HttpAdaptationTimeout.")]
        [FormField(Group = "HttpAdaptation", Order = 20, Widget = "number")]
        [NewDefault(0)]
        [Key(13)]
        public int HttpAdaptationTimeoutMilliseconds { get; set; }

        [Description("Landlord-only. When true the TLS certificate of this Tenant's HTTP Adaptation endpoint is " +
                     "validated. Only applies when an HttpAdaptationUrl is set for the Tenant.")]
        [FormField(Group = "HttpAdaptation", Order = 30, Widget = "switch")]
        [NewDefault(true)]
        [Key(14)]
        public bool HttpAdaptationValidateSsl { get; set; } = true;

        [Description("Landlord-only data-protection switch. When true (the default) the engine may send the IP addresses " +
                     "carried by this Tenant's transactions to the remote Svirfd IP intelligence service (svirfd.com) " +
                     "for lookups. When false no remote Svirfd lookup is made for this Tenant: nothing leaves the " +
                     "installation, while local and directory database lookups are unaffected.")]
        [FormField(Group = "DataProtection", Order = 10, Widget = "switch")]
        [NewDefault(true)]
        [Key(15)]
        public bool SvirfdRemoteLookupsEnabled { get; set; } = true;
    }
}
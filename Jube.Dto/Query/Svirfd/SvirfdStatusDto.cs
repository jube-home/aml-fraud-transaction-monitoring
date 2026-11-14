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

using System;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.Svirfd
{
    [Description("The state of the Svirfd IP intelligence client.")]
    [MessagePackObject]
    public class SvirfdStatusDto
    {
        [Description("True when Svirfd is enabled on this installation.")]
        [Key(0)]
        public bool Enabled { get; set; }

        [Description("True when lookups answer: the index is loaded or the mode is Remote.")]
        [Key(1)]
        public bool Ready { get; set; }

        [Description("The mode in use: Local, Directory or Remote.")]
        [Key(2)]
        public string Mode { get; set; } = string.Empty;

        [Description("The change-log sequence the local index is at, or -1.")]
        [Key(3)]
        public long Seq { get; set; }

        [Description("True when transactions are looked up through the service in Remote mode.")]
        [Key(4)]
        public bool RemoteEngineLookup { get; set; }

        [Description("The credit that must be shown wherever the data is shown.")]
        [Key(5)]
        public string Attribution { get; set; } = string.Empty;

        [Description("Where the data licence is published.")]
        [Key(6)]
        public string LicenceUrl { get; set; } = string.Empty;

        [Description("The client state: Starting, Remote, LocalReady, FellBackToRemote, Stale or Failed. FellBackToRemote means the Local snapshot could not be loaded and lookups are answered by the public API while Local is retried.")]
        [Key(7)]
        public string State { get; set; } = string.Empty;

        [Description("The most recent start or refresh error, or empty.")]
        [Key(8)]
        public string LastError { get; set; } = string.Empty;

        [Description("When the Local load is next retried, in UTC, or empty when no retry is scheduled.")]
        [Key(9)]
        public DateTime? NextRetryAt { get; set; }

        [Description("Seconds since the feed last answered, or empty when there is no local index.")]
        [Key(10)]
        public long? StalenessSeconds { get; set; }

        [Description("False when the platform operator has switched remote Svirfd lookups off for the caller's tenant: no IP address of the tenant's transactions or lookups is sent to the Svirfd service, while local and directory lookups are unaffected.")]
        [Key(11)]
        public bool TenantRemoteLookupsEnabled { get; set; } = true;
    }
}

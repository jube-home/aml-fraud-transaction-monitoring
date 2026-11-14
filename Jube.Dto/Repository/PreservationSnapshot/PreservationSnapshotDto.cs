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
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.PreservationSnapshot
{
    [MessagePackObject]
    public class PreservationSnapshotDto
    {
        [Description("Server-assigned snapshot identifier. This is the value to quote to roll back to this " +
                     "snapshot, by importing it.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Globally unique identifier of the snapshot.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("What caused the snapshot to be taken: the Preservation page, a model synchronisation, " +
                     "or AskJooby acting on the configuration.")]
        [Key(2)]
        public PreservationSnapshotSource SnapshotSourceId { get; set; }

        [Description("Optional label given when the snapshot was taken, for example the reason for the change " +
                     "that followed it.")]
        [Key(3)]
        public string? Name { get; set; }

        [Description("Version of the export body format held in the snapshot.")]
        [Key(4)]
        public int? ExportVersion { get; set; }

        [Description("Identifier carried inside the snapshot body, matching the wrapper it was taken from.")]
        [Key(5)]
        public Guid? ExportGuid { get; set; }

        [Description("How many Entity Analysis Models the snapshot body holds.")]
        [Key(6)]
        public int? EntityAnalysisModelCount { get; set; }

        [Description("Size of the snapshot body in bytes.")]
        [Key(7)]
        public long? Bytes { get; set; }

        [Description("True when the snapshot failed to complete and is not importable.")]
        [Key(8)]
        public bool InError { get; set; }

        [Description("User the snapshot was taken by. Server-assigned.")]
        [Key(9)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) the snapshot was started. Server-assigned.")]
        [Key(10)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Timestamp (UTC) the snapshot body was written. Null while incomplete or in error.")]
        [Key(11)]
        public DateTimeOffset? CompletedDate { get; set; }
    }
}
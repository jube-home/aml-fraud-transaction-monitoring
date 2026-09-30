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

namespace Jube.Dto.Repository.PreservationSnapshot
{
    public class PreservationSnapshotDto
    {
        [Description("Server-assigned snapshot identifier. This is the value to quote to roll back to this " +
                     "snapshot, by importing it.")]
        public int Id { get; set; }

        [Description("Globally unique identifier of the snapshot.")]
        public Guid Guid { get; set; }

        [Description("What caused the snapshot to be taken: the Preservation page, a model synchronisation, " +
                     "or AskJooby acting on the configuration.")]
        public PreservationSnapshotSource SnapshotSourceId { get; set; }

        [Description("Optional label given when the snapshot was taken, for example the reason for the change " +
                     "that followed it.")]
        public string? Name { get; set; }

        [Description("Version of the export body format held in the snapshot.")]
        public int? ExportVersion { get; set; }

        [Description("Identifier carried inside the snapshot body, matching the wrapper it was taken from.")]
        public Guid? ExportGuid { get; set; }

        [Description("How many Entity Analysis Models the snapshot body holds.")]
        public int? EntityAnalysisModelCount { get; set; }

        [Description("Size of the snapshot body in bytes.")]
        public long? Bytes { get; set; }

        [Description("True when the snapshot failed to complete and is not importable.")]
        public bool InError { get; set; }

        [Description("User the snapshot was taken by. Server-assigned.")]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) the snapshot was started. Server-assigned.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Timestamp (UTC) the snapshot body was written. Null while incomplete or in error.")]
        public DateTimeOffset? CompletedDate { get; set; }
    }
}
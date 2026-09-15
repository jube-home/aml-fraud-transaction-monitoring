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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.ArchiverWarning
{
    public sealed record ArchiverWarningDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp the stage's warn threshold was crossed.")]
        DateTime OccurredDate,
        [property: Description("GUID of the Model this warning belongs to.")]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the Model this warning belongs to.")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Guid of the specific invocation this item was archived for; null for BulkCopyArchiveBuffer, which operates on a batch of buffered items rather than one.")]
        Guid? EntityAnalysisModelInstanceEntryGuid,
        [property:
            Description("Which archiver stage was slow -- see StageName for the human-readable form.")]
        int StageId,
        [property:
            Description(
                "Name of the archiver stage that was slow -- BuildArchiveJson, CaseCreationDispatch, RdbmsArchiveWrite, or BulkCopyArchiveBuffer.")]
        string StageName,
        [property: Description("How long this stage actually took, in microseconds.")]
        long DurationMicroseconds,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this warning.")]
        string Instance);
}
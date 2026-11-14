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
using Jube.Dto.Options;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ArchiverWarning
{
    [GridEndpoint("ArchiverWarning", Title = "Archiver warning", DefaultSort = "occurredDate")]
    [GridFilter(Param = "stageId", Label = "Stage", Choices = ["1=BuildArchiveJson", "2=CaseCreationDispatch", "3=RdbmsArchiveWrite", "4=BulkCopyArchiveBuffer"])]
    [MessagePackObject]
    public sealed record ArchiverWarningDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp the stage's warn threshold was crossed.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Key(2)] [property: Description("GUID of the Model this warning belongs to.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelGuid,
        [property: Key(3)] [property: Description("Name of the Model this warning belongs to.")]
        [property: ListColumn(Order = 20, Title = "Entity Analysis Model")]
        string EntityAnalysisModelName,
        [property: Key(4)] [property:
            Description(
                "Guid of the specific invocation this item was archived for; null for BulkCopyArchiveBuffer, which operates on a batch of buffered items rather than one.")]
        [property: ListColumn(Order = 50, Title = "Invocation Entry")]
        Guid? EntityAnalysisModelInstanceEntryGuid,
        [property: Key(5)] [property:
            Description("Which archiver stage was slow -- see StageName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int StageId,
        [property: Key(6)] [property:
            Description(
                "Name of the archiver stage that was slow -- BuildArchiveJson, CaseCreationDispatch, RdbmsArchiveWrite, or BulkCopyArchiveBuffer.")]
        [property: ListColumn(Order = 30, Title = "Stage")]
        string StageName,
        [property: Key(7)] [property: Description("How long this stage actually took, in microseconds.")]
        [property: ListColumn(Order = 40, Title = "Duration", Unit = "µs", Format = GridFormat.Integer)]
        long DurationMicroseconds,
        [property: Key(8)] [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 70, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Key(9)] [property: Description("Hostname of the Jube node that captured this warning.")]
        [property: ListColumn(Order = 60)]
        string Instance);
}
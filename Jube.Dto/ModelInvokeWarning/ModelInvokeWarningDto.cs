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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ModelInvokeWarning
{
    [GridEndpoint("ModelInvokeWarning", Title = "Model invoke warning", DefaultSort = "occurredDate")]
    [GridFilter(Param = "entityAnalysisModelGuid", Label = "Model", Lookup = "EntityAnalysisModel")]
    [GridSearch(Label = "Model or message contains")]
    public sealed record ModelInvokeWarningDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp the warn threshold was crossed during the invocation.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("Guid of the model that raised this warning.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the model that raised this warning, at the time it was raised.")]
        [property: ListColumn(Order = 20, Title = "Model")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Guid of the specific invocation (EntityAnalysisModelInstanceEntry) this warning belongs to -- correlates back to the same invocation's own logs/archive if still retained.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelInstanceEntryGuid,
        [property:
            Description(
                "The trace point's message text, identical to what would appear in the invocation's own captured logs.")]
        [property: ListColumn(Order = 30)]
        string Message,
        [property: Description("Microseconds elapsed in the invocation when this trace point was reached.")]
        [property: ListColumn(Order = 50, Title = "Elapsed", Unit = "µs", Format = GridFormat.Integer)]
        long ElapsedMicroseconds,
        [property:
            Description(
                "Microseconds since the previously captured trace point for this invocation -- the gap that crossed Warn Threshold Milliseconds.")]
        [property: ListColumn(Order = 40, Title = "Gap", Unit = "µs", Format = GridFormat.Integer)]
        long SinceLastEntryMicroseconds,
        [property: Description("Managed thread ID the trace point ran on.")]
        [property: ListColumn(Order = 60, Title = "Thread")]
        int ThreadId,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 80, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this warning.")]
        [property: ListColumn(Order = 70)]
        string Instance);
}
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

namespace Jube.Dto.ModelInvokeWarning
{
    public sealed record ModelInvokeWarningDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp the warn threshold was crossed during the invocation.")]
        DateTime OccurredDate,
        [property: Description("Guid of the model that raised this warning.")]
        Guid EntityAnalysisModelGuid,
        [property: Description("Name of the model that raised this warning, at the time it was raised.")]
        string EntityAnalysisModelName,
        [property:
            Description(
                "Guid of the specific invocation (EntityAnalysisModelInstanceEntry) this warning belongs to -- correlates back to the same invocation's own logs/archive if still retained.")]
        Guid EntityAnalysisModelInstanceEntryGuid,
        [property:
            Description(
                "The trace point's message text, identical to what would appear in the invocation's own captured logs.")]
        string Message,
        [property: Description("Microseconds elapsed in the invocation when this trace point was reached.")]
        long ElapsedMicroseconds,
        [property:
            Description(
                "Microseconds since the previously captured trace point for this invocation -- the gap that crossed Warn Threshold Milliseconds.")]
        long SinceLastEntryMicroseconds,
        [property: Description("Managed thread ID the trace point ran on.")]
        int ThreadId,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this warning.")]
        string Instance);
}
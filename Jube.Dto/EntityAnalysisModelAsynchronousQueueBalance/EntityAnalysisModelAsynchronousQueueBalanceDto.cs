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

namespace Jube.Dto.EntityAnalysisModelAsynchronousQueueBalance
{
    public sealed record EntityAnalysisModelAsynchronousQueueBalanceDto(
        [property: Description("Name of the Entity Analysis Model this row's queue balance belongs to.")]
        string Name,
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTimeOffset? CreatedDate,
        [property: Description("Count of items queued for asynchronous archiving in this interval.")]
        int Archive,
        [property: Description("Count of items queued for the asynchronous activation watcher in this interval.")]
        int ActivationWatcher,
        [property: Description("Identifier of the Entity Analysis Model this row's queue balance belongs to.")]
        Guid EntityAnalysisModelGuid);
}
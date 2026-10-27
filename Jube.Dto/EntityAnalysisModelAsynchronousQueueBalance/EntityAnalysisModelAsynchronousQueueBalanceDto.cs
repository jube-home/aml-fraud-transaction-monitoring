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

namespace Jube.Dto.EntityAnalysisModelAsynchronousQueueBalance
{
    [GridEndpoint("EntityAnalysisModelAsynchronousQueueBalance", Title = "Model asynchronous queue balance", DefaultSort = "createdDate")]
    [GridSearch(Label = "Model name contains")]
    public sealed record EntityAnalysisModelAsynchronousQueueBalanceDto(
        [property: Description("Name of the Entity Analysis Model this row's queue balance belongs to.")]
        [property: ListColumn(Order = 10, Title = "Entity Analysis Model")]
        string Name,
        [property: Description("Hostname of the node that wrote this row.")]
        [property: ListColumn(Order = 20)]
        string Instance,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 30)]
        [property: GridTimeRange]
        DateTimeOffset? CreatedDate,
        [property: Description("Count of items queued for asynchronous archiving in this interval.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        int Archive,
        [property: Description("Count of items queued for the asynchronous activation watcher in this interval.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        int ActivationWatcher,
        [property: Description("Identifier of the Entity Analysis Model this row's queue balance belongs to.")]
        [property: ListColumn(Hidden = true)]
        Guid EntityAnalysisModelGuid);
}
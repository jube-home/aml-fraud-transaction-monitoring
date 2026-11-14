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

namespace Jube.Dto.EntityAnalysisAsynchronousQueueBalance
{
    [GridEndpoint("EntityAnalysisAsynchronousQueueBalance", Title = "Entity analysis asynchronous queue balance", DefaultSort = "createdDate")]
    [MessagePackObject]
    public sealed record EntityAnalysisAsynchronousQueueBalanceDto(
        [property: Key(0)] [property: Description(
            "Always null -- carried forward unchanged from the legacy DTO, which never had a source " +
            "column mapped to this property.")]
        [property: ListColumn(Hidden = true)]
        string? Name,
        [property: Key(1)] [property: Description("Hostname of the node that wrote this row.")]
        [property: ListColumn(Order = 10)]
        string Instance,
        [property: Key(2)] [property: Description("UTC timestamp this row's flush interval was written.")]
        [property: ListColumn(Order = 20)]
        [property: GridTimeRange]
        DateTimeOffset? CreatedDate,
        [property: Key(3)] [property: Description(
            "Always 0 -- carried forward unchanged from the legacy DTO, whose mapping never matched the " +
            "underlying AsynchronousInvoke column name.")]
        [property: ListColumn(Order = 60, Title = "Asynchronous Model Invoke", Format = GridFormat.Integer)]
        int AsynchronousEntityInvoke,
        [property: Key(4)] [property: Description("Count of items queued for asynchronous Case Creation in this interval.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        int CaseCreation,
        [property: Key(5)] [property: Description("Count of items queued for asynchronous Tagging in this interval.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        int Tagging,
        [property: Key(6)] [property: Description("Count of items queued for asynchronous Notification in this interval.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        int Notification,
        [property: Key(7)] [property: Description("Count of items queued for asynchronous Web hook dispatch in this interval.")]
        [property: ListColumn(Order = 55, Format = GridFormat.Integer)]
        int Webhook);
}
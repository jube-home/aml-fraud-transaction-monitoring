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

namespace Jube.Dto.EntityAnalysisAsynchronousQueueBalance
{
    public sealed record EntityAnalysisAsynchronousQueueBalanceDto(
        [property: Description(
            "Always null -- carried forward unchanged from the legacy DTO, which never had a source " +
            "column mapped to this property.")]
        string? Name,
        [property: Description("Hostname of the node that wrote this row.")]
        string Instance,
        [property: Description("UTC timestamp this row's flush interval was written.")]
        DateTimeOffset? CreatedDate,
        [property: Description(
            "Always 0 -- carried forward unchanged from the legacy DTO, whose mapping never matched the " +
            "underlying AsynchronousInvoke column name.")]
        int AsynchronousEntityInvoke,
        [property: Description("Count of items queued for asynchronous Case Creation in this interval.")]
        int CaseCreation,
        [property: Description("Count of items queued for asynchronous Tagging in this interval.")]
        int Tagging,
        [property: Description("Count of items queued for asynchronous Notification in this interval.")]
        int Notification);
}
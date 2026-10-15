/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Preservation
{
    public sealed record PreservationSnapshotDto(
        [property: Description("Server-assigned identifier of this snapshot.")]
        int Id,
        [property: Description("Identifying Guid of this snapshot -- pass this to PreservationRecover to " +
                               "restore it.")]
        Guid Guid,
        [property: Description("Size of the stored snapshot body in bytes, or null if it did not complete.")]
        long? Bytes,
        [property: Description("UTC timestamp the snapshot was taken.")]
        DateTimeOffset TakenAtUtc);
}
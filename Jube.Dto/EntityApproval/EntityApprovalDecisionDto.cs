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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

using MessagePack;

namespace Jube.Dto.EntityApproval
{
    using System.Collections.Generic;

    [MessagePackObject]
    public sealed record EntityApprovalDecisionDto([property: Key(0)] int Kind, [property: Key(1)] int EntityId, [property: Key(2)] int Version, [property: Key(3)] string? Note);

    [MessagePackObject]
    public sealed record EntityApprovalBulkDto([property: Key(0)] int Kind, [property: Key(1)] int EntityId);

    [MessagePackObject]
    public sealed record EntityApprovalBulkResultDto([property: Key(0)] int Approved, [property: Key(1)] IReadOnlyList<string> Refusals);

    [MessagePackObject]
    public sealed record EntityApprovalActionRequestDto([property: Key(0)] string? Note);
}
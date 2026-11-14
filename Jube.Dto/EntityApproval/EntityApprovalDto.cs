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
    using System;

    [MessagePackObject]
    public sealed record EntityApprovalDto(
        [property: Key(0)] int Id,
        [property: Key(1)] int Kind,
        [property: Key(2)] int EntityId,
        [property: Key(3)] int EntityVersion,
        [property: Key(4)] string State,
        [property: Key(5)] string? Note,
        [property: Key(6)] string? CreatedUser,
        [property: Key(7)] DateTime? CreatedDate,
        [property: Key(8)] DateTime? ReturnedDate,
        [property: Key(9)] string? ReturnedUser,
        [property: Key(10)] string? ReturnedNote,
        [property: Key(11)] int? ReturnKindId,
        [property: Key(12)] int? CarriedForwardFromVersion);

    [MessagePackObject]
    public sealed record EntityApprovalStatusDto(
        [property: Key(0)] int Kind,
        [property: Key(1)] int EntityId,
        [property: Key(2)] string Name,
        [property: Key(3)] int CurrentVersion,
        [property: Key(4)] int? EffectiveVersion,
        [property: Key(5)] bool Deleted,
        [property: Key(6)] bool Pending,
        [property: Key(7)] bool Rejected,
        [property: Key(8)] string? MakerUser,
        [property: Key(9)] int ApprovalsRecorded,
        [property: Key(10)] int ApprovalsRequired,
        [property: Key(11)] bool CanApprove,
        [property: Key(12)] string? Note,
        [property: Key(13)] string? ReturnedNote);

    [MessagePackObject]
    public sealed record PendingApprovalDto(
        [property: Key(0)] int Kind,
        [property: Key(1)] int EntityId,
        [property: Key(2)] string Name,
        [property: Key(3)] int Version,
        [property: Key(4)] bool Deleted,
        [property: Key(5)] string? MakerUser,
        [property: Key(6)] int ModelId,
        [property: Key(7)] int? ParentId);
}
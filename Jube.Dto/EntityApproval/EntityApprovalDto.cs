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

namespace Jube.Dto.EntityApproval
{
    using System;

    public sealed record EntityApprovalDto(
        int Id,
        int Kind,
        int EntityId,
        int EntityVersion,
        string State,
        string? Note,
        string? CreatedUser,
        DateTime? CreatedDate);

    public sealed record EntityApprovalStatusDto(
        int Kind,
        int EntityId,
        string Name,
        int CurrentVersion,
        int? EffectiveVersion,
        bool Deleted,
        bool Pending,
        bool Rejected,
        string? MakerUser,
        int ApprovalsRecorded,
        int ApprovalsRequired,
        bool CanApprove);

    public sealed record PendingApprovalDto(
        int Kind,
        int EntityId,
        string Name,
        int Version,
        bool Deleted,
        string? MakerUser,
        int ModelId,
        int? ParentId);
}
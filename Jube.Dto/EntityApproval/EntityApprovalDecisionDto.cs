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
    using System.Collections.Generic;
    public sealed record EntityApprovalDecisionDto(int Kind, int EntityId, int Version, string? Note);
    public sealed record EntityApprovalBulkDto(int Kind, int EntityId);
    public sealed record EntityApprovalBulkResultDto(int Approved, IReadOnlyList<string> Refusals);
}
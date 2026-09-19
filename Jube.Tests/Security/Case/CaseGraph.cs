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

using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Test.Security.Case;

public sealed class CaseGraph
{
    public int ModelId { get; init; }
    public int WorkflowId { get; init; }
    public Guid WorkflowGuid { get; init; }
    public int StatusId { get; init; }
    public Guid StatusGuid { get; init; }
    public int Status2Id { get; init; }
    public Guid Status2Guid { get; init; }
    public int ActionId { get; init; }
    public Guid ActionGuid { get; init; }
    public int DisplayId { get; init; }
    public Guid DisplayGuid { get; init; }
    public int FilterId { get; init; }
    public Guid FilterGuid { get; init; }
    public int CaseId { get; init; }
    public string CaseKey { get; init; } = string.Empty;
    public string CaseKeyValue { get; init; } = string.Empty;
    public Guid EntryGuid { get; init; }
    public int FileId { get; init; }
    public int NoteId { get; init; }
    public int ActionRoleId { get; init; }
    public int DisplayRoleId { get; init; }
    public int FilterRoleId { get; init; }
    public Guid RoleGuid { get; init; }
}
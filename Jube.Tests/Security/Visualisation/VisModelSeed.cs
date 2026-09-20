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
using System.Collections.Generic;

namespace Jube.Test.Security.Visualisation;

internal sealed class VisModelSeed
{
    public int ModelId { get; init; }
    public Guid ModelGuid { get; init; }
    public required string XPathName { get; init; }
    public required string TagName { get; init; }
    public int WorkflowId { get; init; }
    public Guid WorkflowGuid { get; init; }
    public required string WorkflowName { get; init; }

    public required Dictionary<string, string> WorkflowChildren { get; init; }
}
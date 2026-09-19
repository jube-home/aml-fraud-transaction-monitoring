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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Service.Query.TreeChildren;

namespace Jube.Test.Service.Query.TreeChildren.Models
{
    internal sealed record NodeCase(
        TreeChildrenNode Node,
        ParentKind Kind,
        string DtoShape,
        Func<TreeChildrenService, Parents, CancellationToken, Task<List<Child>>> Invoke,
        Func<TreeChildrenServiceTests, DbContext, string, Parents, string, byte, byte, Task<Seeded>> Seed);
}
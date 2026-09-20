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
using Fixture = Jube.Test.Infrastructure.DatabaseFixture.DatabaseFixture;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed class ModelScaffoldOptions
    {
        public int SourceModelId { get; init; } = ModelScaffold.ExampleModelId;

        public int? TenantRegistryId { get; init; }

        public Guid? ModelGuid { get; init; } = ModelScaffold.ExampleModelGuid;

        public string NamePrefix { get; init; } = Fixture.Prefix;

        public IReadOnlyCollection<string> IncludeTables { get; init; } = [];

        public IReadOnlyCollection<string> ExcludeTables { get; init; } = [];

        public bool Active { get; init; } = true;

        public bool Locked { get; init; }

        public bool CreateCallers { get; init; } = true;

        public static ModelScaffoldOptions Isolated => new() { ModelGuid = null };
    }
}
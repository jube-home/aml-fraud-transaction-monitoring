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

namespace Jube.Test.Security.Visualisation;

internal sealed class VisSeed
{
    public int TenantAId { get; init; }
    public int TenantBId { get; init; }
    public Guid RoleAGuid { get; init; }
    public Guid RoleBGuid { get; init; }
    public Guid RoleA2Guid { get; init; }
    public int RoleAId { get; init; }
    public int RoleBId { get; init; }
    public required VisRegistryRow RegistryA { get; init; }
    public required VisRegistryRow RegistryB { get; init; }
    public required VisParameterRow ParameterA { get; init; }
    public required VisParameterRow ParameterB { get; init; }
    public required VisDatasourceRow DatasourceA { get; init; }
    public required VisDatasourceRow DatasourceB { get; init; }
    public required string SeriesNameA { get; init; }
    public required string SeriesNameB { get; init; }
}
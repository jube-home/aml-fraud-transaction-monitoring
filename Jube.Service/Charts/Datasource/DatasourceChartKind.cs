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

using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Models;

namespace Jube.Service.Charts.Datasource
{
    public sealed class DatasourceChartKind(
        DatasourceDisplayKind kind,
        string name,
        string description,
        IReadOnlyList<DatasourceChartRoleSpec> roles,
        Func<DatasourceChartInput, string> render,
        Func<DatasourceChartMapping, DatasourceChartException?>? check = null,
        DatasourceMarkupKind markup = DatasourceMarkupKind.Svg)
    {
        public DatasourceDisplayKind Kind { get; } = kind;
        public string Name { get; } = name;
        public string Description { get; } = description;
        public IReadOnlyList<DatasourceChartRoleSpec> Roles { get; } = roles;
        public Func<DatasourceChartInput, string> Render { get; } = render;
        public Func<DatasourceChartMapping, DatasourceChartException?>? Check { get; } = check;
        public DatasourceMarkupKind Markup { get; } = markup;
    }
}

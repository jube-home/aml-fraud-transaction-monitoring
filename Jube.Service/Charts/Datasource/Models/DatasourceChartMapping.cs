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

using Jube.Dto.Options;

namespace Jube.Service.Charts.Datasource.Models
{
    public sealed class DatasourceChartMapping(
        IReadOnlyDictionary<DatasourceChartRole, IReadOnlyList<string>> columns)
    {
        public IReadOnlyDictionary<DatasourceChartRole, IReadOnlyList<string>> Columns { get; } = columns;

        public bool Has(DatasourceChartRole role)
        {
            return Columns.TryGetValue(role, out var names) && names.Count > 0;
        }

        public string One(DatasourceChartRole role)
        {
            return Columns[role][0];
        }

        public string? OneOrNull(DatasourceChartRole role)
        {
            return Has(role) ? Columns[role][0] : null;
        }

        public IReadOnlyList<string> Many(DatasourceChartRole role)
        {
            return Columns.TryGetValue(role, out var names) ? names : [];
        }
    }
}

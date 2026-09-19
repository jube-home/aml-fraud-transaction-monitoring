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

using System.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.VisualisationRegistryDatasourceSeries
{
    public class VisualisationRegistryDatasourceSeriesDto
    {
        [Description("Server-assigned identifier of the series definition row. Read-only.")]
        public int Id { get; set; }

        [Description("Name of the series, taken verbatim from the column name returned by the Visualisation " +
                     "Registry Datasource's SQL command. Used as the field name when building a Kendo Chart or " +
                     "Grid data source in the browser. Server-derived; not user-editable.")]
        public string? Name { get; set; }

        [Description("Data type of the series column, inferred from the underlying SQL result column type when " +
                     "the Visualisation Registry Datasource is created or updated: 1=string/text, 2=integer, " +
                     "3=double precision/numeric, 4=date, 5=boolean, 6=other/string. Server-derived; not " +
                     "user-editable.")]
        public int DataTypeId { get; set; }
    }
}
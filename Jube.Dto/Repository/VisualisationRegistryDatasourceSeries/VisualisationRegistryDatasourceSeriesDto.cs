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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.VisualisationRegistryDatasourceSeries
{
    public class VisualisationRegistryDatasourceSeriesDto
    {
        [Description("Server-assigned identifier of the series definition row. Read-only.")]
        public int Id { get; set; }

        [Description("Name of the series, taken verbatim from the column name returned by the Visualisation " +
                     "Registry Datasource's SQL command. Server-derived; not user-editable.")]
        public string? Name { get; set; }

        [Description("Data type of the series column, inferred from the underlying SQL result column type when " +
                     "the Visualisation Registry Datasource is created or updated: 1=text, 2=integer, " +
                     "3=decimal (double precision, real, numeric), 4=date and time, 5=flag (smallint or boolean), " +
                     "6=array of decimals, 7=other array. Series are listed in the order of the SELECT columns. " +
                     "Server-derived; not user-editable.")]
        public int DataTypeId { get; set; }

        [Description("Position of the column in the SELECT list, counting from 1. Charts take their series from the " +
                     "columns in this order. Server-derived; not user-editable.")]
        public int Ordinal { get; set; }

        [Description("The chart role this column has been explicitly assigned to, from DatasourceChartRole. Null " +
                     "when the column has no explicit role and is given one by position when the chart is drawn.")]
        public int? RoleId { get; set; }

        [Description("Order among the columns assigned to the same role, counting from 1. Null when RoleId is null.")]
        public int? RoleOrder { get; set; }
    }
}
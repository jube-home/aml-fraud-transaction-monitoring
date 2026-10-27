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

namespace Jube.Dto.Repository.VisualisationRegistryDatasource
{
    public class VisualisationRegistryDatasourceIntrospectionColumnDto
    {
        [Description("Name of the column exactly as the SQL Command returns it. Unique within the result.")]
        public string Name { get; set; } = string.Empty;

        [Description("Position of the column in the SELECT list, counting from 1. Charts take their series from " +
                     "the columns in this order.")]
        public int Ordinal { get; set; }

        [Description("The reporting database's own name for the column type, for example 'integer', 'numeric' or " +
                     "'timestamp without time zone'.")]
        public string DataTypeName { get; set; } = string.Empty;

        [Description("Jube's classification of the column type: 1=text, 2=integer, 3=decimal, 4=date and time, " +
                     "5=flag, 6=array of decimals, 7=other array.")]
        public int DataTypeId { get; set; }
    }
}

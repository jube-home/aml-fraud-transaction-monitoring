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
    public class VisualisationRegistryDatasourceIntrospectionDto
    {
        [Description("True when the SQL Command is accepted and its columns could be read. False when it was " +
                     "rejected, in which case Error says why and Columns is empty.")]
        public bool Valid { get; set; }

        [Description("The columns the SQL Command returns, in SELECT order. Empty when Valid is false.")]
        public List<VisualisationRegistryDatasourceIntrospectionColumnDto> Columns { get; set; } = [];

        [Description("Why the SQL Command was rejected. Null when Valid is true.")]
        public string? Error { get; set; }

        [Description("The line, counting from 1, of the SQL Command where the database says the problem is. Null " +
                     "when the SQL was accepted or the database gave no usable position.")]
        public int? ErrorLine { get; set; }

        [Description("The column, counting from 1, on ErrorLine. Null when ErrorLine is null.")]
        public int? ErrorColumn { get; set; }

        [Description("For every chart kind, whether it can be drawn from these columns by position alone and, if " +
                     "not, why. Empty when Valid is false.")]
        public List<DatasourceKindFitDto> Kinds { get; set; } = [];

        [Description("When a chart kind was asked about and its roles could be satisfied, the columns that fill each " +
                     "role, marking which were assigned explicitly and which were taken by position.")]
        public List<DatasourceResolvedRoleDto> ResolvedMapping { get; set; } = [];

        [Description("Why the asked-about chart kind cannot be drawn with the given mapping, in plain language. " +
                     "Null when it can, or when no chart kind was asked about.")]
        public string? MappingError { get; set; }

        [Description("Why the template text is not valid, with its line and column, in plain language. Null when " +
                     "it is valid, or when the Template kind was not asked about.")]
        public string? TemplateError { get; set; }
    }
}

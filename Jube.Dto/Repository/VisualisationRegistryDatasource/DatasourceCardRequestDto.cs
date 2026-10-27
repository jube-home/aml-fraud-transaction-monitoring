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
    public class DatasourceCardRequestDto
    {
        [Description("Values for the registry parameters; any parameter not listed takes its default.")]
        public List<DatasourceCardParameterDto> Parameters { get; set; } = [];

        [Description("The id of the Case the visualisation is embedded in, supplied to the datasource SQL as @Id " +
                     "when the registry is shown in a Case. Ignored in the Directory, which has no @Id.")]
        public int? ContextId { get; set; }

        [Description("Palette the SVG is drawn in, from DatasourceTheme: 1=auto (the stylesheet follows the page), 2=light, 3=dark. 0 means auto.")]
        public int ThemeId { get; set; }

        [Description("From DatasourceStyleMode: 1=external (the SVG relies on the stylesheet in the response), 2=inline (self contained). 0 means external.")]
        public int StyleModeId { get; set; }

        [Description("The stylesheet version the caller already holds. When it matches, the stylesheet is not sent again.")]
        public string? KnownStylesheetVersion { get; set; }

        [Description("Width of the chart canvas in pixels. 0 uses the default.")]
        public double Width { get; set; }

        [Description("Height of the chart canvas in pixels. 0 uses the default.")]
        public double Height { get; set; }
    }
}

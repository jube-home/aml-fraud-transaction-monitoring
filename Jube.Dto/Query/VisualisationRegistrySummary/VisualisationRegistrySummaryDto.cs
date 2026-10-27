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

namespace Jube.Dto.Query.VisualisationRegistrySummary
{
    public class VisualisationRegistrySummaryDto
    {
        [Description("Identifier of the Visualisation Registry this summary describes.")]
        public int RegistryId { get; set; }

        [Description("Number of active datasources, one per tile on the dashboard.")]
        public int DatasourceCount { get; set; }

        [Description("Number of active parameters the dashboard asks for.")]
        public int ParameterCount { get; set; }

        [Description("The distinct display kind identifiers of the active datasources, in ascending order.")]
        public int[] DisplayKindIds { get; set; } = [];

        [Description("Datasource executions recorded in the window. One dashboard run executes every tile, so " +
                     "this counts tile executions, not dashboard runs.")]
        public int ExecutionsInWindow { get; set; }

        [Description("Datasource executions in the window that recorded an error.")]
        public int FailuresInWindow { get; set; }

        [Description("Mean response time in milliseconds of the executions in the window, null when there were none.")]
        public double? AverageResponseMilliseconds { get; set; }

        [Description("When a datasource of this dashboard was last executed, over all time. Null when never.")]
        public DateTimeOffset? LastExecutionDate { get; set; }

        [Description("Who triggered that last execution.")]
        public string? LastExecutionUser { get; set; }

        [Description("The length in days of the window the counts and the average cover.")]
        public int WindowDays { get; set; }
    }
}

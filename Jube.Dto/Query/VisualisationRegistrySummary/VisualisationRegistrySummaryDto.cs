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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.VisualisationRegistrySummary
{
    [MessagePackObject]
    public class VisualisationRegistrySummaryDto
    {
        [Description("Identifier of the Visualisation Registry this summary describes.")]
        [Key(0)]
        public int RegistryId { get; set; }

        [Description("Number of active datasources, one per tile on the dashboard.")]
        [Key(1)]
        public int DatasourceCount { get; set; }

        [Description("Number of active parameters the dashboard asks for.")]
        [Key(2)]
        public int ParameterCount { get; set; }

        [Description("The distinct display kind identifiers of the active datasources, in ascending order.")]
        [Key(3)]
        public int[] DisplayKindIds { get; set; } = [];

        [Description("Datasource executions recorded in the window. One dashboard run executes every tile, so " +
                     "this counts tile executions, not dashboard runs.")]
        [Key(4)]
        public int ExecutionsInWindow { get; set; }

        [Description("Datasource executions in the window that recorded an error.")]
        [Key(5)]
        public int FailuresInWindow { get; set; }

        [Description("Mean response time in milliseconds of the executions in the window, null when there were none.")]
        [Key(6)]
        public double? AverageResponseMilliseconds { get; set; }

        [Description("When a datasource of this dashboard was last executed, over all time. Null when never.")]
        [Key(7)]
        public DateTimeOffset? LastExecutionDate { get; set; }

        [Description("Who triggered that last execution.")]
        [Key(8)]
        public string? LastExecutionUser { get; set; }

        [Description("The length in days of the window the counts and the average cover.")]
        [Key(9)]
        public int WindowDays { get; set; }
    }
}

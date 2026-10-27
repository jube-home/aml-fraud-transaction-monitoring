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

namespace Jube.Data.Query.GetVisualisationRegistrySummaryQuery
{
    using System;

    public class GetVisualisationRegistrySummaryQueryDto
    {
        public int RegistryId { get; set; }
        public int DatasourceCount { get; set; }
        public int ParameterCount { get; set; }
        public int[] DisplayKindIds { get; set; } = [];
        public int Executions { get; set; }
        public int Failures { get; set; }
        public double? AverageResponseMilliseconds { get; set; }
        public DateTime? LastExecutionDate { get; set; }
        public string LastExecutionUser { get; set; }
    }
}

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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    public class Flags
    {
        public bool EnableCache { get; set; }
        public bool EnableSanctionCache { get; set; }
        public bool EnableTtlCounter { get; set; }
        public bool EnableActivationArchive { get; set; }
        public bool EnableRdbmsArchive { get; set; }
        public bool EnableActivationWatcher { get; set; }
        public bool EnableResponseElevationLimit { get; set; }
        public bool EnableImplicitAsync { get; set; }
        public int? ImplicitAsyncTimeoutMilliseconds { get; set; }
        public bool EnableTrace { get; set; }
        public bool EnableLogs { get; set; }
        public bool EnableLogsInfo { get; set; }
        public bool EnableLogsWarnThreshold { get; set; }
        public int? LogsWarnThresholdMilliseconds { get; set; }
        public bool EnableLogsInResponse { get; set; }
        public bool EnableSampling { get; set; }
        public double? SamplePercentage { get; set; }
        public string FallbackResponseElevationRedirect { get; set; }
    }
}
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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaptureQueueHealth(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "CaptureQueueHealthList", OperationKind.Read, true, false,
                    "Lists per-cycle depth and dropped-count snapshots for every bounded in-memory capture " +
                    "queue in the observability suite (ModelInvokeWarning, CaseCreationWarning, " +
                    "ArchiverWarning, RedisSentinelEvent, RedisConnectionEvent, OpenTelemetryMetric), one row " +
                    "per queue every ~60 seconds regardless of activity. Any non-zero DroppedCount means real " +
                    "data was silently discarded because a queue hit capacity -- the first thing to check when " +
                    "an expected event or warning seems to be missing. Not scoped to any tenant. Also accepts " +
                    "samplePercentage (0-100) to draw an unbiased random subset of matching rows instead of the " +
                    "most recent ones."));
        }
    }
}
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Jube.Test.Security.Observability;

public static class ObservabilityAreas
{
    private static readonly int[] monitoring = [27];

    public static IReadOnlyList<ObservabilityArea> All { get; } =
    [
        new("ApplicationLogEntry", monitoring, false, [], true),
        new("ArchiverStagePerformanceCounter", monitoring, true, ["stageId"], false),
        new("ArchiverWarning", monitoring, true, ["stageId"], false),
        new("CaptureQueueHealth", monitoring, false, ["queueId"], false),
        new("CaseCreationStagePerformanceCounter", monitoring, false, ["stageId"], false),
        new("CaseCreationWarning", monitoring, true, ["stageId"], true),
        new("ContainerLogEntry", monitoring, false, ["streamTypeId"], true),
        new("DockerContainerMetric", monitoring, false, [], true),
        new("DockerEvent", monitoring, false, [], true),
        new("DockerHostMetric", monitoring, false, [], true),
        new("DotNetRuntimeMetric", monitoring, false, [], true),
        new("EntityAnalysisAsynchronousQueueBalance", monitoring, false, [], false),
        new("EntityAnalysisModelAsynchronousQueueBalance", monitoring, true, [], true),
        new("EntityAnalysisModelProcessingCounter", monitoring, true, [], true),
        new("EntityAnalysisModelResponseTimePipelineCounter", monitoring, true, ["stageId"], false),
        new("EntityAnalysisModelStagePerformanceCounter", monitoring, true, ["stageId"], false),
        new("EntityAnalysisModelTaskPerformanceCounter", monitoring, true, ["directionId", "taskTypeId"], false),
        new("EtcdClusterEvent", monitoring, false, ["eventTypeId"], true),
        new("EtcdMemberStatus", monitoring, false, [], true),
        new("HAProxyReachabilityProbe", monitoring, false, [], true),
        new("HAProxyServerStatus", monitoring, false, [], true),
        new("HttpProcessingCounter", [5, 9], false, [], false),
        new("ModelInvokeWarning", monitoring, false, [], true, "entityAnalysisModelGuid")
    ];

    public static IEnumerable<object[]> Names() => All.Select(a => new object[] { a.Name });

    public static IEnumerable<object[]> SearchNames() =>
        All.Where(a => a.HasSearch).Select(a => new object[] { a.Name });

    public static IEnumerable<object[]> TenantScopedNames() =>
        All.Where(a => a.TenantScoped).Select(a => new object[] { a.Name });

    public static ObservabilityArea Get(string name) =>
        All.Single(a => a.Name.Equals(name, StringComparison.Ordinal));
}
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
using System.Reflection;
using FluentAssertions;
using Jube.Dto.Forms;
using Jube.Service.Query.GridModel;
using Xunit;

namespace Jube.Test.Service.Query.GridModel
{
    [Trait("Category", "Unit")]
    public sealed class GridModelAccessTests
    {
        private static readonly string[] landlordOnlyAreas =
        [
            "ApplicationLogEntry", "ArchiverStagePerformanceCounter", "ArchiverWarning", "CaptureQueueHealth",
            "CaseCreationStagePerformanceCounter", "CaseCreationWarning", "ContainerLogEntry", "DockerContainerMetric",
            "DockerEvent", "DockerHostMetric", "DotNetRuntimeMetric", "EntityAnalysisAsynchronousQueueBalance",
            "EntityAnalysisModelAsynchronousQueueBalance", "EntityAnalysisModelProcessingCounter",
            "EntityAnalysisModelResponseTimePipelineCounter", "EntityAnalysisModelStagePerformanceCounter",
            "EntityAnalysisModelTaskPerformanceCounter", "EtcdClusterEvent", "EtcdMemberStatus",
            "HaProxyReachabilityProbe", "HaProxyServerStatus", "HttpProcessingCounter", "ModelInvokeWarning",
            "OpenTelemetryMetric", "OtlpDispatchCounter", "OverlayNetworkTaskDrift", "PatroniClusterEvent",
            "PatroniMemberStatus", "PostgresActivity", "PostgresIndexStatistics", "PostgresLogEntry",
            "PostgresMetric", "PostgresReplicationStatus", "PostgresStatementStatistics", "PostgresTableStatistics",
            "RespCallCounter", "RespConnectionEvent", "RespConnectionMultiplexerMetric", "RespMetric",
            "RespSentinelEvent", "RespSentinelStatus", "RespSlowOperation", "WafAttack"
        ];

        private static readonly string[] permissionedAreas = ["UserLogin", "UserLogout"];

        private static readonly GridModelRegistry Registry =
            GridModelRegistry.Build(typeof(GridEndpointAttribute).Assembly);

        public static IEnumerable<object[]> LandlordAreas()
        {
            return landlordOnlyAreas.Select(area => new object[] { area });
        }

        public static IEnumerable<object[]> PermissionedAreas()
        {
            return permissionedAreas.Select(area => new object[] { area });
        }

        public static IEnumerable<object[]> RegistryAreas()
        {
            return Registry.Areas.Order().Select(area => new object[] { area });
        }

        [Fact]
        public void EveryGridAreaIsPinnedHereSoANewAreaForcesADecisionAboutWhoMaySeeIt()
        {
            Registry.Areas.Should().BeEquivalentTo(landlordOnlyAreas.Concat(permissionedAreas));
        }

        [Theory]
        [MemberData(nameof(RegistryAreas))]
        public void EveryGridAreaResolvesToExactlyOneRealServiceSoARenamedServiceCannotSilentlyBecomeLandlordOnly(
            string area)
        {
            GridModelAccess.IsKnown(area).Should().BeTrue();

            typeof(GridModelAccess).Assembly.GetTypes().Count(t =>
                string.Equals(t.Name, area + "Service", StringComparison.OrdinalIgnoreCase) &&
                t.GetProperty("RequiredPermissions", BindingFlags.Public | BindingFlags.Static) != null)
                .Should().Be(1);
        }

        [Theory]
        [MemberData(nameof(LandlordAreas))]
        public void AMonitoringAreaIsLandlordOnlyWithNoTenantPermission(string area)
        {
            var access = GridModelAccess.Resolve(area);

            access.LandlordOnly.Should().BeTrue();
            access.Permissions.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(PermissionedAreas))]
        public void ALoginAuditAreaNeedsPermission35AndIsNotLandlordOnly(string area)
        {
            var access = GridModelAccess.Resolve(area);

            access.LandlordOnly.Should().BeFalse();
            access.Permissions.Should().Equal(35);
        }

        [Fact]
        public void AnUnknownAreaIsNotKnownAndFailsClosedToLandlordOnly()
        {
            GridModelAccess.IsKnown("NoSuchArea").Should().BeFalse();
            GridModelAccess.Resolve("NoSuchArea").LandlordOnly.Should().BeTrue();
        }
    }
}

/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.DockerAdministrator;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Docker
{
    public sealed class DockerAdministratorService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly string? dockerMonitoringProxyUrl;
        private readonly ILog log;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private DockerAdministratorService(string? dockerMonitoringProxyUrl, string userName,
            int tenantRegistryId, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dockerMonitoringProxyUrl = dockerMonitoringProxyUrl;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static bool RequiresLandlord => true;

        public static Task<DockerAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, dynamicEnvironment, log, stringLocalizerFactory,
                serviceChangeBus, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<DockerAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(DockerAdministratorResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("DockerAdministrator.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[DockerAdministratorResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DockerAdministrator.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[DockerAdministratorResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            if (!permissionValidation.Landlord)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DockerAdministrator.Create: permission denied (landlord only) user={userName}");
                }

                throw new ForbiddenException(strings[DockerAdministratorResources.PermissionDenied], permissions);
            }

            return new DockerAdministratorService(dynamicEnvironment.AppSettings("DockerMonitoringProxyUrl"),
                userName, resolvedTenantRegistryId.Value, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every container Docker currently knows about RIGHT NOW (running or not), with " +
                     "id, name, image, state and status -- not the background sampler's last-stored row " +
                     "(see DockerContainerMetricList), which can be up to a minute stale. Read-only; never " +
                     "execs into, stops or removes anything.")]
        [ServiceOperation("DockerContainerListNow", OperationKind.Read, Idempotent = true)]
        public async Task<List<string>> GetContainerSummariesNowAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("DockerAdministrator", "ContainerListNow", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (string.IsNullOrWhiteSpace(dockerMonitoringProxyUrl))
            {
                op.Rows(0);

                return [];
            }

            using var client = BuildClient();
            var containers = await client.GetContainersAsync(token).ConfigureAwait(false);

            var summaries = (containers ?? [])
                .Select(c => $"{(string?)c["Id"]} {((string?)c["Names"]?[0])?.TrimStart('/')} " +
                             $"{(string?)c["Image"]} {(string?)c["State"]} {(string?)c["Status"]}")
                .ToList();

            op.Rows(summaries.Count);

            return summaries;
        }

        [Description("Reads the Docker host's own live /info and /version -- container/image counts, CPU, " +
                     "memory, kernel, OS, Docker/API version -- RIGHT NOW, not the background sampler's " +
                     "last-stored row (see DockerHostMetricList). Read-only.")]
        [ServiceOperation("DockerHostInfoNow", OperationKind.Read, Idempotent = true)]
        public async Task<string?> GetHostInfoNowAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("DockerAdministrator", "HostInfoNow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (string.IsNullOrWhiteSpace(dockerMonitoringProxyUrl))
            {
                op.Rows(0);

                return null;
            }

            using var client = BuildClient();
            var info = await client.GetInfoAsync(token).ConfigureAwait(false);
            op.Rows(info != null ? 1 : 0);

            return info?.ToString();
        }

        private DockerProxyClient BuildClient()
        {
            return new DockerProxyClient(new HttpClient
                { BaseAddress = new Uri(dockerMonitoringProxyUrl!), Timeout = TimeSpan.FromSeconds(10) });
        }
    }
}
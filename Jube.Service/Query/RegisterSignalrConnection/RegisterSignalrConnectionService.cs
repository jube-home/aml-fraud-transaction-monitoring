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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.RegisterSignalrConnection;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.RegisterSignalrConnection
{
    public sealed partial class RegisterSignalrConnectionService
    {
        private const int MaxConnectionIdLength = 128;

        private static readonly int[] permissions = [30];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ISignalrGroupRegistrar registrar;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private RegisterSignalrConnectionService(string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ISignalrGroupRegistrar registrar, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.registrar = registrar;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static string GroupName(int tenantRegistryId) => TenantGroup.Name(tenantRegistryId);

        [GeneratedRegex("^[A-Za-z0-9_\\-+/=]+$")]
        private static partial Regex ConnectionIdShape();

        private static bool IsValidConnectionId([NotNullWhen(true)] string? connectionId)
        {
            return !string.IsNullOrEmpty(connectionId) && connectionId.Length <= MaxConnectionIdLength &&
                   ConnectionIdShape().IsMatch(connectionId);
        }

        public static Task<RegisterSignalrConnectionService> CreateAsync(DbContext dbContext, string? userName,
            ISignalrGroupRegistrar registrar, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, registrar, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RegisterSignalrConnectionService> CreateAsync(DbContext dbContext,
            string? userName, ISignalrGroupRegistrar registrar, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RegisterSignalrConnectionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RegisterSignalrConnection.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RegisterSignalrConnectionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"RegisterSignalrConnection.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RegisterSignalrConnectionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RegisterSignalrConnectionService(userName, resolvedTenantRegistryId.Value,
                permissionValidation, registrar, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Subscribes a live SignalR connection to the calling user's tenant notification group " +
                     "(named Tenant_<tenantRegistryId>) so that it receives that tenant's real-time events. " +
                     "The group is derived from the caller, never from the arguments. The connection must have been opened " +
                     "by the caller, and is removed from any other tenant group. Idempotent.")]
        [ServiceOperation("RegisterSignalrConnectionRegister", OperationKind.Write, Idempotent = true)]
        public async Task RegisterAsync(
            [Description("Identifier of the SignalR connection to subscribe; must be one the caller opened.")]
            string connectionId,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var op = OperationScope.Start("RegisterSignalrConnection", "Register", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            const string name = "RegisterSignalrConnection.Register";
            if (log.IsDebugEnabled)
            {
                log.Debug($"{name}: entry user={userName} connectionId={connectionId}");
            }

            try
            {
                EnsurePermitted(name);
                EnsureConnectionOwned(name, connectionId);

                var groupName = GroupName(tenantRegistryId);
                foreach (var stale in registrar.TenantGroupsOf(connectionId).Where(g => g != groupName).ToList())
                {
                    await registrar.RemoveFromGroupAsync(connectionId, stale, token).ConfigureAwait(false);
                }

                await registrar.AddToGroupAsync(connectionId, groupName, token).ConfigureAwait(false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (InvalidConnectionIdException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (ConnectionNotOwnedException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"{name}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsureConnectionOwned(string op, string? connectionId)
        {
            if (!IsValidConnectionId(connectionId))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{op}: malformed connection id user={userName}");
                }

                throw new InvalidConnectionIdException(strings[RegisterSignalrConnectionResources.InvalidConnectionId]);
            }

            if (registrar.IsOwnedBy(connectionId, userName))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: connection {connectionId} is not owned by user={userName}; refusing.");
            }

            throw new ConnectionNotOwnedException(strings[RegisterSignalrConnectionResources.ConnectionNotOwned]);
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[RegisterSignalrConnectionResources.PermissionDenied], permissions);
        }
    }
}
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
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Payload;
using Jube.Dto.UserLogin;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.UserLogin;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.UserLogin
{
    public sealed class UserLoginService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [35];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly UserLoginRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private UserLoginService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new UserLoginRepository(dbContext);
        }

        public static Task<UserLoginService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<UserLoginService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(UserLoginResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserLogin.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserLoginResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"UserLogin.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserLoginResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new UserLoginService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Login Audit Trail -- every sign-in attempt, successful or not, across all " +
                     "three schemes (Username/Password, Negotiate, OAuth/OpenID Connect) -- ordered per " +
                     "sortField/sortDirection (most recent first by default), capped at 'take' rows (max " +
                     "100000, default 100000). Each of from/to defaults independently to the last hour when " +
                     "omitted. Not scoped to any Model or tenant. Optionally restrict to a date range (by " +
                     "CreatedDate) and/or a case-insensitive substring search against CreatedUser, RemoteIp, " +
                     "UserAgent or FailureMessage. Optionally apply samplePercentage on top of every other " +
                     "filter to draw a random subset instead of the most recent rows. Statistics is always " +
                     "empty for this DTO -- Failed, AuthenticationTypeId and FailureTypeId are categorical " +
                     "codes/flags, not continuous/measured quantities.")]
        [ServiceOperation("UserLoginList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<UserLoginDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with CreatedDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with CreatedDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description(
                "Case-insensitive substring match against CreatedUser, RemoteIp, UserAgent or FailureMessage; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random one-tenth of matching rows, not the newest tenth. Omit for the normal most-recent-first behaviour.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by: createdDate, createdUser, failed, authenticationTypeId, failureTypeId, failureMessage, remoteIp, localIp or userAgent. Unrecognised or omitted falls back to Id descending.")]
            string? sortField = null,
            [Description(
                "Sort direction, case-insensitive: 'asc' for ascending, anything else (including omitted) for descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserLogin", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserLogin.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("UserLogin.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, search, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);
                var total = await repository.CountAsync(from, to, search, clampedSamplePercentage, token)
                    .ConfigureAwait(false);
                var statistics = await repository.GetStatisticsAsync(from, to, search, clampedSamplePercentage,
                    MaxListTake, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new UserLoginDto(
                    r.Id, r.CreatedDate.GetValueOrDefault(), r.CreatedUser, r.Failed, r.AuthenticationTypeId,
                    DescribeAuthenticationScheme(r.AuthenticationTypeId), r.FailureTypeId,
                    r.Failed == 1 ? DescribeFailureReason(r.FailureTypeId) : string.Empty, r.FailureMessage,
                    r.RemoteIp, r.LocalIp, r.UserAgent)).ToList();

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserLogin.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<UserLoginDto>(dtos, total, statistics);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserLogin.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserLogin.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static string DescribeAuthenticationScheme(int? authenticationTypeId)
        {
            return authenticationTypeId switch
            {
                1 => "Username and Password",
                2 => "Negotiate (Windows/Kerberos)",
                3 => "OAuth / OpenID Connect",
                _ => "(unknown)"
            };
        }

        private static string DescribeFailureReason(int failureTypeId)
        {
            return failureTypeId switch
            {
                1 => "No User Registry found matching the supplied username",
                2 => "User Registry matched is not Active",
                3 => "User Registry matched is Password Locked",
                4 => "Password has expired and must be changed",
                5 => "Bad credentials (password did not match)",
                6 => "No password was supplied",
                7 => "OAuth: no usable identity claim in principal payload",
                8 => "OAuth: remote/identity-provider failure",
                9 => "OAuth: local token validation failed",
                10 => "OAuth: internal error during processing",
                _ => "(unknown)"
            };
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

            throw new ForbiddenException(strings[UserLoginResources.PermissionDenied], permissions);
        }
    }
}
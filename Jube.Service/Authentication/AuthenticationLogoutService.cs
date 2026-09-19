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

using System.Diagnostics;
using Jube.Data.Context;
using Jube.Data.Security;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.UserLogout;
using log4net;

namespace Jube.Service.Authentication
{
    public sealed class AuthenticationLogoutService(
        Func<DbContext> dbContextFactory,
        ILog log,
        Action<string>? abortConnections = null,
        TimeProvider? timeProvider = null,
        ILog? auditLog = null)
    {
        private const string Area = "Authentication";
        private static readonly TimeSpan revokeTimeout = TimeSpan.FromSeconds(10);

        private readonly ILog auditLog = auditLog ?? LogManager.GetLogger("Jube.Audit");
        private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

        public async Task<LogoutOutcome> LogoutAsync(AuthenticationLogoutRequest request)
        {
            var identity = request.Identity;
            var remoteIp = request.RemoteIp;
            var userName = identity is { IsAuthenticated: true, Name: { Length: > 0 } name } ? name : null;
            using var op = OperationScope.Start(Area, "Logout", userName == null ? null : Safe(userName), null,
                auditLog, log, new NullServiceChangeBus());
            op.Outcome("no-session");
            Activity.Current?.SetTag("jube.remote.ip", remoteIp ?? "-");

            if (userName == null)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Logout: no session was presented, the cookies are cleared. ip={Safe(remoteIp)}");
                }

                await UserLogoutRecorder.RecordAsync(dbContextFactory, log,
                    new UserLogoutEntry(null, UserLogoutReason.UserLogout, UserLogoutOutcome.NoSession, remoteIp,
                        request.LocalIp, request.UserAgent), timeProvider).ConfigureAwait(false);

                return LogoutOutcome.NoSession;
            }

            var outcome = LogoutOutcome.Revoked;
            try
            {
                using var timeout = new CancellationTokenSource(revokeTimeout);
                await using var dbContext = dbContextFactory();
                await TokenValidity.RevokeUserBeforeAsync(dbContext, userName, timeProvider.GetUtcNow(),
                    timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                outcome = LogoutOutcome.RevokeFailed;
                log.Error(
                    $"Logout: could not revoke the tokens of user={Safe(userName)} ip={Safe(remoteIp)}; the cookies are cleared but a copied token stays valid until it expires.",
                    ex);
            }

            try
            {
                abortConnections?.Invoke(userName);
            }
            catch (Exception ex)
            {
                log.Error($"Logout: could not close the hub connections of user={Safe(userName)}.", ex);
            }

            op.Outcome(outcome == LogoutOutcome.Revoked ? "revoked" : "revoke-failed");
            await UserLogoutRecorder.RecordAsync(dbContextFactory, log,
                new UserLogoutEntry(userName, UserLogoutReason.UserLogout,
                    outcome == LogoutOutcome.Revoked ? UserLogoutOutcome.Revoked : UserLogoutOutcome.RevokeFailed,
                    remoteIp, request.LocalIp, request.UserAgent, request.SessionStartUtc,
                    Message: outcome == LogoutOutcome.Revoked ? null : "The tokens could not be revoked."),
                timeProvider).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"Logout: user={Safe(userName)} outcome={outcome} ip={Safe(remoteIp)}");
            }

            return outcome;
        }

        private static string Safe(string? value)
        {
            if (value == null)
            {
                return "(null)";
            }

            return string.Create(value.Length, value, static (span, v) =>
            {
                for (var i = 0; i < v.Length; i++)
                {
                    span[i] = char.IsControl(v[i]) ? '?' : v[i];
                }
            });
        }
    }
}
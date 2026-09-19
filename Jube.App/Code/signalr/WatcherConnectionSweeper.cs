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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Security;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code.signalr
{
    public sealed class WatcherConnectionSweeper(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        WatcherConnectionRegistry registry) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!int.TryParse(dynamicEnvironment.AppSettings("HubRevocationCheckSeconds"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await SweepAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.Error("WatcherConnectionSweeper: could not check open connections.", ex);
                }
            }
        }

        public async Task<int> SweepAsync(CancellationToken token = default)
        {
            var sessions = registry.Tracked()
                .Where(t => !string.IsNullOrEmpty(t.UserName))
                .GroupBy(t => (t.UserName, t.IssuedMilliseconds))
                .ToList();
            if (sessions.Count == 0)
            {
                return 0;
            }

            var aborted = 0;
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            foreach (var session in sessions)
            {
                token.ThrowIfCancellationRequested();
                var refuse = AbsoluteSessionLifetime.IsExpired(dynamicEnvironment, session.Key.IssuedMilliseconds,
                    DateTimeOffset.UtcNow);
                if (!refuse)
                {
                    var (validFrom, blocked) = await TokenValidity
                        .GetSessionStateAsync(dbContext, session.Key.UserName).ConfigureAwait(false);
                    refuse = blocked
                             || (validFrom.HasValue
                                 && long.TryParse(session.Key.IssuedMilliseconds, NumberStyles.Integer,
                                     CultureInfo.InvariantCulture, out var issued)
                                 && issued < validFrom.Value);
                }

                if (!refuse)
                {
                    continue;
                }

                foreach (var connection in session)
                {
                    registry.Abort(connection.ConnectionId);
                    aborted++;
                }
            }

            return aborted;
        }
    }
}
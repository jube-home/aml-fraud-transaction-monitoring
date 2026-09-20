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

namespace Jube.App.Code.signalr
{
    using System;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Security;
    using DynamicEnvironment;
    using Jube.Service.Exceptions.Query.RegisterSignalrConnection;
    using Jube.Service.Query.RegisterSignalrConnection;
    using Jube.Service.Reactivity.Interfaces;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Localization;

    public class WatcherHub(
        DynamicEnvironment dynamicEnvironment,
        IStringLocalizerFactory stringLocalizerFactory,
        IServiceChangeBus serviceChangeBus,
        ILog log) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name;
            WatcherConnectionRegistry.Instance.Track(Context.ConnectionId, userName, Context.Abort,
                Context.User?.FindFirst(TokenValidity.IssuedMillisecondsClaim)?.Value);

            if (!string.IsNullOrWhiteSpace(userName))
            {
                try
                {
                    await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        dynamicEnvironment.AppSettings("ConnectionString"), log);
                    var service = await RegisterSignalrConnectionService.CreateAsync(dbContext, userName,
                        new SignalrGroupRegistrar(Groups, WatcherConnectionRegistry.Instance), log,
                        stringLocalizerFactory, serviceChangeBus);
                    await service.RegisterAsync(Context.ConnectionId);
                }
                catch (Exception ex) when (ex is NotAuthenticatedException or ForbiddenException)
                {
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"WatcherHub.OnConnected: could not join tenant group user={userName}", ex);
                }
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            WatcherConnectionRegistry.Instance.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
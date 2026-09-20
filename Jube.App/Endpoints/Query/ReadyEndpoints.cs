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
using Jube.App.Code.WatcherDispatch;
using Jube.Service.Query.Ready;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Jube.App.Endpoints.Query.Models;

namespace Jube.App.Endpoints.Query
{
    public static class ReadyEndpoints
    {
        private const string Base = "/api/Ready";

        public static void MapReadyEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(Base, Get)
                .AllowAnonymous()
                .WithTags("Ready")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status503ServiceUnavailable)
                .WithName("ReadyGet");
        }

        private static IResult Get(HttpContext httpContext, ILog log, IHostApplicationLifetime lifetime,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            var engine = httpContext.RequestServices.GetService<Jube.Engine.Engine>();
            var relay = httpContext.RequestServices.GetService<Relay>();

            var service = new ReadyService(
                new RuntimeReadinessSignals(lifetime, engine, relay),
                dynamicEnvironment.AppSettings("EnableEngine").Equals("True", StringComparison.OrdinalIgnoreCase),
                dynamicEnvironment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase));

            var ready = service.IsReady();
            if (!ready && log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}: 503 not ready");
            }

            return ready ? TypedResults.Ok() : TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
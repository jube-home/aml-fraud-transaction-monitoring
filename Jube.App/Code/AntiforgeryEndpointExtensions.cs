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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Code
{
    public static class AntiforgeryEndpointExtensions
    {
        public static RouteHandlerBuilder ValidateAntiforgeryForCookieSessions(this RouteHandlerBuilder builder)
        {
            return builder.DisableAntiforgery().AddEndpointFilter(ValidateForCookieSessionsAsync);
        }

        private static async ValueTask<object> ValidateForCookieSessionsAsync(
            EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;

            if (CookieSessionRequest.IsCookieSessionOnly(httpContext.Request))
            {
                if (!httpContext.Request.Headers.ContainsKey(CookieSessionRequest.AntiforgeryHeaderName))
                {
                    return Results.Json("Missing antiforgery token.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                try
                {
                    await httpContext.RequestServices.GetRequiredService<IAntiforgery>()
                        .ValidateRequestAsync(httpContext).ConfigureAwait(false);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.Json("Invalid antiforgery token.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            return await next(context).ConfigureAwait(false);
        }
    }
}
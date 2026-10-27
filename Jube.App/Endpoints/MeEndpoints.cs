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
using System.Linq;
using System.Threading.Tasks;
using Jube.App.Code;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints
{
    public static class MeEndpoints
    {
        public static void MapMeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/Me", GetAsync)
                .RequireAuthorization()
                .Produces<MeResponse>()
                .WithTags("Me")
                .WithName("MeGet");
        }

        private static async Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            var userName = httpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return TypedResults.Unauthorized();
            }

            var permissions = await new Data.Security.PermissionValidation()
                .GetPermissionsAsync(dynamicEnvironment.AppSettings("ConnectionString"), userName, log)
                .ConfigureAwait(false);

            var externalAuthentication = IsEnabled(dynamicEnvironment, "OAuthAuthentication")
                                         || IsEnabled(dynamicEnvironment, "NegotiateAuthentication");

            return TypedResults.Ok(new MeResponse
            {
                UserName = userName,
                Landlord = permissions.Landlord,
                Permissions = permissions.Permissions.Distinct().Order().ToArray(),
                TenantRegistryId = permissions.TenantRegistryId,
                CanChangePassword = !externalAuthentication,
                AskJoobyInference = IsEnabled(dynamicEnvironment, "EnableAskJooby"),
                SessionExpiry = SessionExpiry.Resolve(httpContext)
            });
        }

        private static bool IsEnabled(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string key)
        {
            return (dynamicEnvironment.AppSettings(key) ?? "False").Equals("True",
                StringComparison.OrdinalIgnoreCase);
        }

        public class MeResponse
        {
            public string UserName { get; init; }
            public bool Landlord { get; init; }
            public int[] Permissions { get; init; }
            public int TenantRegistryId { get; init; }
            public bool CanChangePassword { get; init; }
            public bool AskJoobyInference { get; init; }
            public DateTime? SessionExpiry { get; init; }
        }
    }
}

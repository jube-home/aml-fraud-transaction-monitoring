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

using System.Threading;
using System.Threading.Tasks;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Security;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jube.App.Pages
{
    using AskJoobyPermissions = AskJoobyPermissions;
    using NotAuthenticatedException = NotAuthenticatedException;
    using PermissionValidation = PermissionValidation;

    [Authorize]
    public class AskJooby(
        ILog log,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        IHttpContextAccessor httpContextAccessor)
        : PageModel
    {
        private static readonly int[] permissions = [AskJoobyPermissions.Access];
        public bool IsLandlord { get; private set; }

        public async Task<ActionResult> OnGetAsync(CancellationToken token)
        {
            var userName = httpContextAccessor.HttpContext?.User.Identity?.Name;

            if (userName is null)
            {
                return Forbid();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                    .ConfigureAwait(false);

                if (!permissionValidation.Validate(permissions))
                {
                    return Forbid();
                }

                IsLandlord = permissionValidation.Landlord;

                return new PageResult();
            }
            catch (NotAuthenticatedException)
            {
                return Forbid();
            }
        }
    }
}
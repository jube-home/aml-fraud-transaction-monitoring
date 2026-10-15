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

using Jube.App.Code;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jube.App.Pages.Model
{
    [Authorize]
    public class AdvancedAnalytics : PageModel
    {
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public AdvancedAnalytics(ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            permissionValidation =
                new PermissionValidation(dynamicEnvironment.AppSettings("ConnectionString"), userName, log);
        }

        public bool CanDelete { get; private set; }
        public bool CanDeleteAllocated { get; private set; }
        public bool CanSetActive { get; private set; }

        public ActionResult OnGet()
        {
            if (!permissionValidation.Validate([
                    45, 46, 47, 48
                ]))
            {
                return Forbid();
            }

            CanDelete = permissionValidation.Validate([
                48
            ]);

            CanDeleteAllocated = CanDelete && permissionValidation.Validate([
                47
            ]);

            CanSetActive = permissionValidation.Validate([
                47
            ]);

            return new PageResult();
        }
    }
}
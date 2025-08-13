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
using System.Collections.Generic;
using Jube.App.Code;
using Jube.App.Dto;
using Jube.Data.Context;
using Jube.Engine.Helpers;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jube.App.Controllers.Repository
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowPriorityController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly PermissionValidation _permissionValidation;
        private readonly string _userName;

        public CaseWorkflowPriorityController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Close();
                _dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpGet("ByCasesWorkflowIdActiveOnly/{id:int}")]
        public ActionResult<List<CaseWorkflowPriorityDto>> ByCasesWorkflowIdActiveOnly(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(CaseWorkflowPriorities());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowGuidActiveOnly/{guid:guid}")]
        public ActionResult<List<CaseWorkflowPriorityDto>> ByCasesWorkflowGuidActiveOnly(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(CaseWorkflowPriorities());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        private static List<CaseWorkflowPriorityDto> CaseWorkflowPriorities()
        {
            return new List<CaseWorkflowPriorityDto>
            {
                new()
                {
                    Id = 1,
                    Name = "High"
                },
                new()
                {
                    Id = 2,
                    Name = "Medium"
                },
                new()
                {
                    Id = 3,
                    Name = "Low"
                }
            };
        }
    }
}
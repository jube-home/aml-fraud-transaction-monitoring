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
using System.Linq;
using Jube.App.Code;
using Jube.App.Dto.Requests;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Engine.Helpers;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jube.App.Controllers.Helper
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CompletionsController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly PermissionValidation _permissionValidation;
        private readonly string _userName;

        public CompletionsController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
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

        [HttpGet("ByCaseWorkflowId")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowId(int caseWorkflowId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 25, 17, 20 })) return Forbid();

                var caseWorkflowRepository = new CaseWorkflowRepository(_dbContext, _userName);
                var entityAnalysisModelId = caseWorkflowRepository.GetById(caseWorkflowId).EntityAnalysisModelId;

                if (entityAnalysisModelId == null) return NotFound();

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, _userName, true);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowGuidIncludingDeleted")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowGuidIncludingDeleted(Guid caseWorkflowGuid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                var caseWorkflowRepository = new CaseWorkflowRepository(_dbContext, _userName);
                var entityAnalysisModelId =
                    caseWorkflowRepository.GetByGuidIncludingDeleted(caseWorkflowGuid).EntityAnalysisModelId;

                if (entityAnalysisModelId == null) return NotFound();

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, _userName, true);

                return Ok(completionDtos);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowIdIncludingDeleted")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowIdIncludingDeleted(int caseWorkflowId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                var caseWorkflowRepository = new CaseWorkflowRepository(_dbContext, _userName);
                var entityAnalysisModelId =
                    caseWorkflowRepository.GetByIdIncludingDeleted(caseWorkflowId).EntityAnalysisModelId;

                if (entityAnalysisModelId == null) return NotFound();

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, _userName, true);

                return Ok(completionDtos);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId")]
        public ActionResult<List<CompletionDto>> GetByEntityAnalysisModelId(int entityAnalysisModelId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 16 })) return Forbid();

                var completionDtos = CompletionDtos(entityAnalysisModelId, 6, _userName, true);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                _log.Error(e);
                throw;
            }
        }

        [HttpGet("ByEntityAnalysisModelIdParseTypeId")]
        public ActionResult<List<CompletionDto>> GetByEntityAnalysisModelIdParseTypeId(int entityAnalysisModelId,
            int parseTypeId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 8, 10, 13, 14, 17, 20, 26 })) return Forbid();

                var completionDtos = CompletionDtos(entityAnalysisModelId, parseTypeId, _userName, false);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                _log.Error(e);
                throw;
            }
        }

        private List<CompletionDto> CompletionDtos(int entityAnalysisModelId, int parseTypeId, string userName,
            bool reporting)
        {
            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(_dbContext, userName);

            var completionDtos = getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .Execute(entityAnalysisModelId, parseTypeId, reporting)
                .Select(field => new CompletionDto
                {
                    Score = 1000,
                    Name = field.Name,
                    Value = field.Value,
                    Field = field.ValueSqlPath,
                    Meta = $"{field.Name}:{field.JQueryBuilderDataType}",
                    Group = field.Group,
                    DataType = field.JQueryBuilderDataType,
                    XPath = field.ValueJsonPath
                })
                .ToList();

            return completionDtos;
        }
    }
}
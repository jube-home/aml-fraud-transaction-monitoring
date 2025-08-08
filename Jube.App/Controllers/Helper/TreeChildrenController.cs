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
using Jube.App.Dto;
using Jube.App.Dto.TreeChildren;
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
    [Route("api/TreeChildren")]
    [Authorize]
    public class EntityAnalysisModelTreeChildrenController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly PermissionValidation _permissionValidation;
        private readonly string _userName;

        public EntityAnalysisModelTreeChildrenController(ILog log,
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

        [HttpGet]
        [Route("RequestXPath")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetRequestXPath(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 7 })) return Forbid();

                var repository = new EntityAnalysisModelRequestXPathRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("VisualisationRegistryDatasource")]
        public ActionResult<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryDatasource(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 33 })) return Forbid();

                var repository = new VisualisationRegistryDatasourceRepository(_dbContext, _userName);
                return Ok(repository.GetByVisualisationRegistryIdOrderById(id)
                    .Select(entry => new VisualisationRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, VisualisationRegistryId = entry.VisualisationRegistryId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("UserRegistry")]
        public ActionResult<List<RoleRegistryTreeChildDto>> GetUserRegistry(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 35 })) return Forbid();

                var repository = new UserRegistryRepository(_dbContext, _userName);
                return Ok(repository.GetByRoleRegistryId(id)
                    .Select(entry => new RoleRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, RoleRegistryId = entry.RoleRegistryId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("RoleRegistryPermission")]
        public ActionResult<List<RoleRegistryTreeChildDto>> GetRolePermissionRegistry(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 36 })) return Forbid();

                var getRoleRegistryPermissionByRoleRegistryId =
                    new GetRoleRegistryPermissionByRoleRegistryIdQuery(_dbContext, _userName);

                return getRoleRegistryPermissionByRoleRegistryId.Execute(id).Select(s
                    => new RoleRegistryTreeChildDto
                    {
                        Key = s.Id,
                        Name = s.Name,
                        Color = s.Active ? "green" : "red",
                        RoleRegistryId = s.RoleRegistryId
                    }).ToList();
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("VisualisationRegistryParameter")]
        public ActionResult<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryParameter(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 32 })) return Forbid();

                var repository = new VisualisationRegistryParameterRepository(_dbContext, _userName);
                return Ok(repository.GetByVisualisationRegistryIdOrderById(id)
                    .Select(entry => new VisualisationRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, VisualisationRegistryId = entry.VisualisationRegistryId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("InlineFunction")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetInlineFunction(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 8 })) return Forbid();

                var repository = new EntityAnalysisModelInlineFunctionRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Tag")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetTag(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 37 })) return Forbid();

                var repository = new EntityAnalysisModelTagRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("GatewayRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetGatewayRule(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 10 })) return Forbid();

                var repository = new EntityAnalysisModelGatewayRuleRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Exhaustive")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetExhaustive(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 16 })) return Forbid();

                var repository = new ExhaustiveSearchInstanceRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Reprocessing")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetReprocessing(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 26 })) return Forbid();

                var repository = new EntityAnalysisModelReprocessingRuleRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelId(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Adaptation")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAdaptation(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 15 })) return Forbid();

                var repository = new EntityAnalysisModelHttpAdaptationRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflow")]
        public ActionResult<List<CaseWorkflowDto>> GetCaseWorkflows(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18, 19, 20, 21, 22, 23, 24, 25 })) return Forbid();

                var repository = new CaseWorkflowRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("AbstractionCalculation")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAbstractionCalculation(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 14 })) return Forbid();

                var repository = new EntityAnalysisModelAbstractionCalculationRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("AbstractionRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAbstractionRule(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 13, 14 })) return Forbid();

                var repository = new EntityAnalysisModelAbstractionRuleRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("ActivationRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetActivationRule(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 17 })) return Forbid();

                var repository = new EntityAnalysisModelActivationRuleRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }


        [HttpGet]
        [Route("TTLCounter")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetTtlCounter(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 12 })) return Forbid();

                var repository = new EntityAnalysisModelTtlCounterRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("InlineScript")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetInlineScript(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 9 })) return Forbid();

                var repository = new EntityAnalysisModelInlineScriptRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Sanctions")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetSanction(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 11 })) return Forbid();

                var repository = new EntityAnalysisModelSanctionRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("List")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetList(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 3 })) return Forbid();

                var repository = new EntityAnalysisModelListRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelGuid(guid)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelGuid = entry.EntityAnalysisModelGuid
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Dictionary")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetDictionary(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 4 })) return Forbid();

                var repository = new EntityAnalysisModelDictionaryRepository(_dbContext, _userName);
                return Ok(repository.GetByEntityAnalysisModelGuid(guid)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, EntityAnalysisModelGuid = entry.EntityAnalysisModelGuid
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowXPath")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowXPath(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 20 })) return Forbid();

                var repository = new CaseWorkflowXPathRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderByIdDesc(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowForm")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowForm(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 })) return Forbid();

                var repository = new CaseWorkflowFormRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowAction")]
        public ActionResult<List<CaseWorkflowActionDto>> GetCaseWorkflowAction(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 22 })) return Forbid();

                var repository = new CaseWorkflowActionRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowMacro")]
        public ActionResult<List<CaseWorkflowMacroDto>> GetCaseWorkflowMacro(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 24 })) return Forbid();

                var repository = new CaseWorkflowMacroRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowFilter")]
        public ActionResult<List<CaseWorkflowFilterDto>> GetCaseWorkflowFilter(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 25 })) return Forbid();

                var repository = new CaseWorkflowFilterRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowDisplay")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowDisplay(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 23 })) return Forbid();

                var repository = new CaseWorkflowDisplayRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowStatus")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowStatus(int key)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 19 })) return Forbid();

                var repository = new CaseWorkflowStatusRepository(_dbContext, _userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red", Key = entry.Id,
                        Name = entry.Name, CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
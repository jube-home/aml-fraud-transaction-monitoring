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
using System.Net;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Jube.App.Code;
using Jube.App.Dto;
using Jube.App.Validators;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
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
    public class CaseWorkflowController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly CaseWorkflowRepository _repository;
        private readonly string _userName;
        private readonly IValidator<CaseWorkflowDto> _validator;

        public CaseWorkflowController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseWorkflow, CaseWorkflowDto>();
                cfg.CreateMap<CaseWorkflowDto, CaseWorkflow>();
                cfg.CreateMap<List<CaseWorkflow>, List<CaseWorkflowDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            _mapper = new Mapper(config);
            _repository = new CaseWorkflowRepository(_dbContext, _userName);
            _validator = new CaseWorkflowDtoValidator();
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
        public ActionResult<List<CaseWorkflowDto>> Get()
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowDto>>(_repository.Get()));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}")]
        public ActionResult<List<CaseWorkflowDto>> GetByEntityAnalysisModelId(int entityAnalysisModelId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 17, 18 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowDto>>(
                    _repository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelIdActiveOnly")]
        public ActionResult<List<CaseWorkflowDto>> GetByEntityAnalysisModelIdActiveOnly(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18, 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowDto>>(_repository.GetByEntityAnalysisModelIdActiveOnly(id)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelGuidActiveOnly")]
        public ActionResult<List<CaseWorkflowDto>> GetByEntityAnalysisModelGuidActiveOnly(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18, 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowDto>>(_repository.GetByEntityAnalysisModelGuidActiveOnly(guid)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<CaseWorkflowDto> GetById(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18 })) return Forbid();

                return Ok(_mapper.Map<CaseWorkflowDto>(_repository.GetById(id)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowDto> Create([FromBody] CaseWorkflowDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18 }, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid) return Ok(_repository.Insert(_mapper.Map<CaseWorkflow>(model)));

                return BadRequest(results);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(CaseWorkflowDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowDto> Update([FromBody] CaseWorkflowDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18 }, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid) return Ok(_repository.Update(_mapper.Map<CaseWorkflow>(model)));

                return BadRequest(results);
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

        [HttpDelete]
        [Route("{id:int}")]
        public ActionResult<List<CaseWorkflowDto>> Get(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 18 }, true)) return Forbid();

                _repository.Delete(id);
                return Ok();
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
    }
}
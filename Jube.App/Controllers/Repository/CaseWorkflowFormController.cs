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
    public class CaseWorkflowFormController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly CaseWorkflowFormRepository _repository;
        private readonly string _userName;
        private readonly IValidator<CaseWorkflowFormDto> _validator;

        public CaseWorkflowFormController(ILog log, IHttpContextAccessor httpContextAccessor,
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
                cfg.CreateMap<CaseWorkflowForm, CaseWorkflowFormDto>();
                cfg.CreateMap<CaseWorkflowFormDto, CaseWorkflowForm>();
                cfg.CreateMap<List<CaseWorkflowForm>, List<CaseWorkflowFormDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            _mapper = new Mapper(config);
            _repository = new CaseWorkflowFormRepository(_dbContext, _userName);
            _validator = new CaseWorkflowFormDtoValidator();
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
        public ActionResult<List<CaseWorkflowFormDto>> Get()
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowFormDto>>(_repository.Get()));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowIdActiveOnly/{id:int}")]
        public ActionResult<List<CaseWorkflowFormDto>> ByCasesWorkflowIdActiveOnly(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21, 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowFormDto>>(_repository.GetByCasesWorkflowIdActiveOnly(id)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowGuidActiveOnly/{guid:guid}")]
        public ActionResult<List<CaseWorkflowFormDto>> ByCasesWorkflowGuidActiveOnly(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21, 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowFormDto>>(_repository.GetByCasesWorkflowGuidActiveOnly(guid)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowId/{casesWorkflowId:int}")]
        public ActionResult<List<CaseWorkflowFormDto>> GetByEntityAnalysisModelId(int casesWorkflowId)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 })) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowFormDto>>(
                    _repository.GetByCasesWorkflowIdOrderById(casesWorkflowId)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<CaseWorkflowFormDto> GetById(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 })) return Forbid();

                return Ok(_mapper.Map<CaseWorkflowFormDto>(_repository.GetById(id)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowFormDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowFormDto> Create([FromBody] CaseWorkflowFormDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 }, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid) return Ok(_repository.Insert(_mapper.Map<CaseWorkflowForm>(model)));

                return BadRequest(results);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(CaseWorkflowFormDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowFormDto> Update([FromBody] CaseWorkflowFormDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 }, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid) return Ok(_repository.Update(_mapper.Map<CaseWorkflowForm>(model)));

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
        public ActionResult<List<CaseWorkflowFormDto>> Get(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 21 }, true)) return Forbid();

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
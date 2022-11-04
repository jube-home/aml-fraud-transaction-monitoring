﻿/* Copyright (C) 2022-present Jube Holdings Limited.
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
using Newtonsoft.Json.Linq;

namespace Jube.App.Controllers.Repository
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowFormEntryController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly CaseWorkflowFormEntryRepository _repository;
        private readonly string _userName;
        private readonly IValidator<CaseWorkflowFormEntryDto> _validator;

        public CaseWorkflowFormEntryController(ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;
            
            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);
            
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseWorkflowFormEntry, CaseWorkflowFormEntryDto>();
                cfg.CreateMap<CaseWorkflowFormEntryDto, CaseWorkflowFormEntry>();
                cfg.CreateMap<List<CaseWorkflowFormEntry>, List<CaseWorkflowFormEntryDto>>();
            });
            _mapper = new Mapper(config);
            _repository = new CaseWorkflowFormEntryRepository(_dbContext, _userName);
            _validator = new CaseWorkflowFormEntryDtoValidator();
            _dynamicEnvironment = dynamicEnvironment;
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
        
        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowFormEntryDto), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowFormEntryDto> Create([FromBody] CaseWorkflowFormEntryDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] {1}, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid)
                {
                    var entry = _repository.Insert(_mapper.Map<CaseWorkflowFormEntry>(model));

                    if (model.Payload != null)
                    {
                        var repositoryCaseWorkflowFormEntryValue =
                            new CaseWorkflowFormEntryValueRepository(_dbContext, _userName);

                        var jObject = JObject.Parse(model.Payload);

                        var values = new Dictionary<string, string>();
                        foreach (var (key, value) in jObject)
                        {
                            if (value != null)
                            {
                                values.Add(key, value.ToString());

                                if (key != "CaseKey")
                                {
                                    var caseWorkflowFormEntryValue = new CaseWorkflowFormEntryValue
                                    {
                                        CaseWorkflowFormEntryId = entry.Id,
                                        Name = key,
                                        Value = value.ToString()
                                    };

                                    repositoryCaseWorkflowFormEntryValue.Insert(caseWorkflowFormEntryValue);
                                }
                            }
                        }

                        var caseWorkflowFormRepository = new CaseWorkflowFormRepository(_dbContext, _userName);

                        var caseWorkflowForm = caseWorkflowFormRepository.GetById(model.CaseWorkflowFormId);

                        if (caseWorkflowForm.EnableNotification == 1 || caseWorkflowForm.EnableHttpEndpoint == 1)
                        {
                            if (caseWorkflowForm.EnableNotification == 1)
                            {
                                var notification = new Notification(_log, _dynamicEnvironment);
                                notification.Send(caseWorkflowForm.NotificationTypeId ?? 1,
                                    caseWorkflowForm.NotificationDestination,
                                    caseWorkflowForm.NotificationSubject,
                                    caseWorkflowForm.NotificationBody, values);
                            }

                            if (caseWorkflowForm.EnableHttpEndpoint == 1)
                            {
                                var sendHttpEndpoint = new SendHttpEndpoint();
                                if (caseWorkflowForm.HttpEndpointTypeId != null)
                                    sendHttpEndpoint.Send(caseWorkflowForm.HttpEndpoint,
                                        caseWorkflowForm.HttpEndpointTypeId.Value
                                        , values);
                            }
                        }
                    }

                    return Ok(entry);
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseKeyValue")]
        public ActionResult<List<CaseWorkflowFormEntryDto>> GetByCaseKeyValue(string key, string value)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] {1})) return Forbid();

                return Ok(_mapper.Map<List<CaseWorkflowFormEntry>>(_repository.GetByCaseKeyValue(key, value)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
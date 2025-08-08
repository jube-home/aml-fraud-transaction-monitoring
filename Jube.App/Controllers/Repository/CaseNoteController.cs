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
using AutoMapper;
using FluentValidation;
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
    public class CaseNoteController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly CaseNoteRepository _repository;
        private readonly string _userName;
        private readonly IValidator<CaseNoteDto> _validator;

        public CaseNoteController(ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment
            , IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseNote, CaseNoteDto>();
                cfg.CreateMap<CaseNoteDto, CaseNote>();
                cfg.CreateMap<List<CaseNote>, List<CaseNoteDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            _mapper = new Mapper(config);
            _repository = new CaseNoteRepository(_dbContext, _userName);
            _validator = new CaseNoteDtoValidator();
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
        public ActionResult<CaseNoteDto> Insert([FromBody] CaseNoteDto model)
        {
            if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

            var results = _validator.Validate(model);

            if (!results.IsValid) return Ok(_repository.Insert(_mapper.Map<CaseNote>(model)));

            var jObject = JObject.Parse(model.Payload);

            var values = new Dictionary<string, string>();
            foreach (var (key, value) in jObject)
                if (value != null)
                    values.Add(key, value.ToString());

            var caseWorkflowActionRepository = new CaseWorkflowActionRepository(_dbContext, _userName);

            var caseWorkflowAction = caseWorkflowActionRepository.GetById(model.ActionId);

            if (caseWorkflowAction.EnableNotification != 1 && caseWorkflowAction.EnableHttpEndpoint != 1)
                return Ok(_repository.Insert(_mapper.Map<CaseNote>(model)));

            if (caseWorkflowAction.EnableNotification == 1)
            {
                var notification = new Notification(_log, _dynamicEnvironment);
                notification.Send(caseWorkflowAction.NotificationTypeId ?? 1,
                    caseWorkflowAction.NotificationDestination,
                    caseWorkflowAction.NotificationSubject,
                    caseWorkflowAction.NotificationBody, values);
            }

            if (caseWorkflowAction.EnableHttpEndpoint != 1)
                return Ok(_repository.Insert(_mapper.Map<CaseNote>(model)));

            var sendHttpEndpoint = new SendHttpEndpoint();
            if (caseWorkflowAction.HttpEndpointTypeId != null)
                sendHttpEndpoint.Send(caseWorkflowAction.HttpEndpoint,
                    caseWorkflowAction.HttpEndpointTypeId.Value
                    , values);

            return Ok(_repository.Insert(_mapper.Map<CaseNote>(model)));
        }

        [HttpGet("ByCaseKeyValue")]
        public ActionResult<List<CaseNoteDto>> GetByCaseKeyValue(string key, string value)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseNote>>(_repository.GetByCaseKeyValue(key, value)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
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
using System.Globalization;
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
    public class CaseController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly CaseRepository _repositoryCase;
        private readonly CaseEventRepository _repositoryCaseEvent;
        private readonly string _userName;
        private readonly IValidator<CaseDto> _validator;

        public CaseController(ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment
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
                cfg.CreateMap<Case, CaseDto>();
                cfg.CreateMap<CaseDto, Case>();
                cfg.CreateMap<List<Case>, List<CaseDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            _mapper = new Mapper(config);
            _repositoryCase = new CaseRepository(_dbContext, _userName);
            _repositoryCaseEvent = new CaseEventRepository(_dbContext, _userName);
            _validator = new CaseDtoValidator();
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

        [HttpGet]
        public ActionResult<List<CaseDto>> Get()
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(_mapper.Map<List<CaseDto>>(_repositoryCase.Get()));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public ActionResult<CaseDto> GetByCaseId(int id)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(_mapper.Map<CaseDto>(
                    _repositoryCase.GetById(id)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseDto> CreateCase([FromBody] CaseDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 }, true)) return Forbid();

                var results = _validator.Validate(model);
                if (results.IsValid) return Ok(_repositoryCase.Insert(_mapper.Map<Case>(model)));

                return BadRequest(results);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(CaseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseDto> UpdateCase([FromBody] CaseDto model)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 }, true)) return Forbid();

                var results = _validator.Validate(model);

                if (!results.IsValid) return BadRequest(results);

                var existing = _repositoryCase.GetById(model.Id);

                var caseEvents = new List<CaseEvent>();

                if (existing.ClosedStatusId is 0 or 1 or 2)
                    switch (model.ClosedStatusId)
                    {
                        case 3:
                            existing.ClosedUser = _userName;
                            existing.ClosedDate = DateTime.Now;

                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 5,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = existing.ClosedDate,
                                    CreatedUser = _userName
                                }
                            );
                            break;
                        case 2:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 12,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = _userName
                                }
                            );
                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 13,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = _userName
                                }
                            );
                            break;
                    }
                else
                    switch (model.ClosedStatusId)
                    {
                        case 2:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 12,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = _userName
                                }
                            );
                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 13,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = _userName
                                }
                            );
                            break;
                    }

                existing.ClosedStatusId = model.ClosedStatusId;

                if (existing.LockedUser != model.LockedUser)
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 14,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.CaseKeyValue,
                            Before = existing.LockedUser,
                            After = model.LockedUser,
                            CreatedDate = DateTime.Now,
                            CreatedUser = _userName
                        }
                    );

                existing.LockedUser = model.LockedUser;

                if (existing.Locked is 0 or null)
                {
                    if (existing.Locked == 1)
                    {
                        caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 6,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                CreatedDate = DateTime.Now,
                                CreatedUser = _userName
                            }
                        );

                        existing.LockedDate = DateTime.Now;
                        existing.LockedUser = _userName;
                    }
                    else
                    {
                        existing.LockedDate = null;
                    }
                }

                existing.Locked = (byte)(model.Locked ? 1 : 0);

                if (existing.DiaryDate != model.DiaryDate)
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 10,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.DiaryDate.ToString(),
                            After = model.DiaryDate.ToString(CultureInfo.InvariantCulture),
                            CreatedDate = DateTime.Now,
                            CreatedUser = _userName
                        }
                    );

                existing.DiaryDate = model.DiaryDate;

                if (existing.Diary is 0 or null)
                    if (model.Diary)
                    {
                        caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 7,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.LockedUser,
                                CreatedDate = DateTime.Now,
                                CreatedUser = _userName
                            }
                        );

                        existing.DiaryUser = _userName;
                    }

                existing.Diary = (byte)(model.Diary ? 1 : 0);

                if (existing.CaseWorkflowStatusGuid != model.CaseWorkflowStatusGuid)
                {
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 9,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.CaseWorkflowStatusGuid.ToString(),
                            After = model.CaseWorkflowStatusGuid.ToString(),
                            CreatedDate = DateTime.Now,
                            CreatedUser = _userName
                        }
                    );

                    if (model.Payload != null)
                    {
                        var jObject = JObject.Parse(model.Payload);

                        var values = new Dictionary<string, string>();
                        foreach (var (key, value) in jObject)
                            if (value != null)
                                values.Add(key, value.ToString());

                        var caseWorkflowStatusRepository =
                            new CaseWorkflowStatusRepository(_dbContext, _userName);

                        var caseWorkflowStatus =
                            caseWorkflowStatusRepository.GetByGuid(model.CaseWorkflowStatusGuid);

                        if (caseWorkflowStatus.EnableNotification == 1 ||
                            caseWorkflowStatus.EnableHttpEndpoint == 1)
                        {
                            if (caseWorkflowStatus.EnableNotification == 1)
                            {
                                var notification = new Notification(_log, _dynamicEnvironment);
                                notification.Send(caseWorkflowStatus.NotificationTypeId ?? 1,
                                    caseWorkflowStatus.NotificationDestination,
                                    caseWorkflowStatus.NotificationSubject,
                                    caseWorkflowStatus.NotificationBody, values);
                            }

                            if (caseWorkflowStatus.EnableHttpEndpoint == 1)
                            {
                                var sendHttpEndpoint = new SendHttpEndpoint();
                                if (caseWorkflowStatus.HttpEndpointTypeId != null)
                                    sendHttpEndpoint.Send(caseWorkflowStatus.HttpEndpoint,
                                        caseWorkflowStatus.HttpEndpointTypeId.Value
                                        , values);
                            }
                        }
                    }
                }

                existing.CaseWorkflowStatusGuid = model.CaseWorkflowStatusGuid;

                if (existing.Rating != model.Rating)
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 11,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.Rating.ToString(),
                            After = model.Rating.ToString(),
                            CreatedDate = DateTime.Now,
                            CreatedUser = _userName
                        }
                    );

                existing.Rating = model.Rating;

                existing = _repositoryCase.Update(existing);

                _repositoryCaseEvent.BulkInsert(caseEvents);

                return Ok(existing);
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
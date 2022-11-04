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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Jube.App.Dto;
using Jube.Data.Cache;
using Jube.Data.Extension;
using Jube.Engine.Invoke;
using Jube.Engine.Model;
using Jube.Engine.Model.Archive;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;

namespace Jube.App.Controllers.Invoke
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvokeController : Controller
    {
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly Jube.Engine.Program _engine;
        private readonly ILog _log;
        private readonly ConcurrentQueue<EntityAnalysisModelInvoke> _pendingEntityInvoke;
        private readonly IModel _rabbitMqChannel;
        private readonly Random _seeded;

        public InvokeController(ILog log,
            Random seeded, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke,
            Jube.Engine.Program engine = null,
            IModel rabbitMqChannel = null)
        {
            _engine = engine;
            _log = log;
            _seeded = seeded;
            _dynamicEnvironment = dynamicEnvironment;
            _pendingEntityInvoke = pendingEntityInvoke;
            _rabbitMqChannel = rabbitMqChannel;
            if (_engine != null) _engine.HttpCounterAllRequests += 1;
        }

        [HttpGet("EntityAnalysisModel/Callback/{guid:Guid}")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public Task<ActionResult> EntityAnalysisModelCallback(Guid guid,int? timeout)
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult<ActionResult>(Forbid());
                
                timeout ??= 3000;
                
                _engine.HttpCounterCallback += 1;
                
                var sw = new Stopwatch();
                sw.Start();
                
                var spinWait = new SpinWait();
                while (true)
                {
                    _engine.EntityAnalysisModelManager.PendingCallbacks.TryGetValue(guid,out var value);

                    if (value != null)
                    {
                        var cacheCallbackRepository = new CacheCallbackRepository(_dynamicEnvironment.AppSettings(new[] {"CacheConnectionString", "ConnectionString"}),
                            _log);
                        
                        cacheCallbackRepository.Delete(guid);

                        Response.ContentType = "application/json";
                        Response.ContentLength = value.Payload.Length;
                        return Task.FromResult<ActionResult>(Ok(value.Payload));
                    }

                    if (sw.ElapsedMilliseconds > timeout)
                    {
                        return Task.FromResult<ActionResult>(NotFound());
                    }
                    
                    spinWait.SpinOnce();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Callback Fetch: Has seen an error as {ex}. Returning 500.");

                _engine.HttpCounterCallback += 1;
                return Task.FromResult<ActionResult>(StatusCode(500));
            }
        }
        
        [HttpGet("Sanction")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public ActionResult<List<SanctionEntryDto>> Sanction(string multiPartString, int distance)
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (!_engine.SanctionsHasLoadedForStartup) return NotFound();

                _engine.HttpCounterSanction += 1;

                _log.Info(
                    $"Sanction Fetch: Reached Sanction Get controller with distance of {distance} and string of {multiPartString}.");

                return _engine.HttpHandlerSanctions(multiPartString, distance)
                    .Select(sanctionEntryReturn => new SanctionEntryDto
                    {
                        Reference = sanctionEntryReturn.SanctionEntryDto.SanctionEntryReference,
                        Value = string.Join(' ', sanctionEntryReturn.SanctionEntryDto.SanctionElementValue),
                        Source = _engine.SanctionSources.ContainsKey(sanctionEntryReturn.SanctionEntryDto
                            .SanctionEntrySourceId)
                            ? _engine.SanctionSources[sanctionEntryReturn.SanctionEntryDto.SanctionEntrySourceId].Name
                            : "Missing",
                        Distance = sanctionEntryReturn.LevenshteinDistance,
                        Id = sanctionEntryReturn.SanctionEntryDto.SanctionEntryId
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _log.Error($"Sanction Fetch: Has seen an error as {ex}. Returning 500.");

                _engine.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }

        [HttpPut("Archive/Tag")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public ActionResult EntityAnalysisModelInstanceEntryGuid([FromBody] TagRequestDto model)
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (!_engine.EntityModelsHasLoadedForStartup) return NotFound();

                _log.Info(
                    $"Tagging: Controller has Put request with guid {model.EntityAnalysisModelInstanceEntryGuid}," +
                    $" name {model.Name} and value {model.Value}.");

                _engine.HttpCounterTag += 1;
                
                var entityAnalysisModelGuid = Guid.Parse(model.EntityAnalysisModelGuid);
                foreach (var (_, value) in
                         from modelKvp in _engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels
                         where entityAnalysisModelGuid == modelKvp.Value.Guid
                         select modelKvp)
                {
                    if (value.EntityAnalysisModelTags.Find(w => w.Name == model.Name) == null) return BadRequest();

                    var tag = new Tag
                    {
                        Name = model.Name,
                        Value = model.Value,
                        EntityAnalysisModelInstanceEntryGuid = Guid.Parse(model.EntityAnalysisModelInstanceEntryGuid),
                        EntityAnalysisModelId = value.Id
                    };
                
                    _log.Info(
                        "HTTP Handler Entity: GUID matched for Requested Model GUID " +
                        $"{tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.");
                    
                    _engine.PendingTagging.Enqueue(tag);

                    _log.Info(
                        "Tagging: Controller has put tag in queue with guid " +
                        $"{tag.EntityAnalysisModelInstanceEntryGuid}, model {tag.EntityAnalysisModelId}, " +
                        $"name {model.Name} and value {model.Value}.  Returning Ok.");
                    
                    return Ok();
                }
                
                return NotFound();
            }
            catch (Exception ex)
            {
                _log.Error(
                    "Tagging: An error has been created while tagging guid " +
                    $"{model.EntityAnalysisModelInstanceEntryGuid} " +
                    $"and model {model.EntityAnalysisModelGuid} as {ex}.");

                _engine.HttpCounterAllError += 1;
                
                return StatusCode(500);
            }
        }

        [HttpPost("EntityAnalysisModel/{guid}")]
        [HttpPost("EntityAnalysisModel/{guid}/{async}")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult> EntityAnalysisModelGuid()
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (_engine == null) return NotFound();

                if (!_engine.EntityModelsHasLoadedForStartup) return NotFound();

                _engine.HttpCounterModel += 1;

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                try
                {
                    var guid = Guid.Parse(Request.RouteValues["guid"].AsString());
                    
                    var async = false;
                    if (Request.RouteValues.ContainsKey("async"))
                    {
                        async = Request.RouteValues["async"].AsString().Equals("Async",StringComparison.OrdinalIgnoreCase);
                        _engine.HttpCounterModelAsync += 1;   
                    }

                    EntityAnalysisModel entityAnalysisModel = null;
                    foreach (var (_, value) in
                             from modelKvp in _engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels
                             where guid == modelKvp.Value.Guid
                             select modelKvp)
                    {
                        entityAnalysisModel = value;

                        _log.Info(
                            $"HTTP Handler Entity: GUID matched for Requested Model GUID {guid}.  Model id is {entityAnalysisModel.Id}.");

                        break;
                    }

                    if (entityAnalysisModel != null)
                    {
                        if (!entityAnalysisModel.Started) return StatusCode(204);
                        
                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {guid} model id is {entityAnalysisModel.Id} will now begin payload parsing.");

                        var entityModelInvoke = new EntityAnalysisModelInvoke(_log, _dynamicEnvironment,
                            _rabbitMqChannel,_engine.PendingNotification, _seeded,
                            _engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels);
                        
                        if (Request.ContentLength != null)
                        {
                            if (Request.ContentLength > 0)
                            {
                                entityModelInvoke.ParseAndInvoke(entityAnalysisModel, ms, async,
                                    Request.ContentLength.Value,
                                    _pendingEntityInvoke);

                                if (entityModelInvoke.InError) return BadRequest(entityModelInvoke.ErrorMessage);

                                /*
                                Response.ContentLength = entityModelInvoke.ResponseJson.Length;
                                entityModelInvoke.ResponseJson.CopyToAsync(Response.Body).Wait();*/
                                
                                Response.ContentType = "application/json";
                                Response.ContentLength = entityModelInvoke.ResponseJson.Length;
                                return Ok(entityModelInvoke.ResponseJson);
                            }

                            return BadRequest("Content body is zero length.");
                        }

                        _log.Info(
                            "HTTP Handler Entity: Json content body is zero.");

                        return BadRequest("Content body is zero length.");
                    }

                    _log.Info(
                        $"HTTP Handler Entity: Could not locate the model for Guid {guid}.");

                    return NotFound();
                }
                catch
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"HTTP Handler Entity: Error as {ex}.  Returning 500.");

                if (_engine != null) _engine.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }

        [HttpPost("ExhaustiveSearchInstance/{guid}")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult<double>> ExhaustiveSearchInstance()
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (!_engine.EntityModelsHasLoadedForStartup) return NotFound();

                _engine.HttpCounterExhaustive += 1;

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                var guid = Request.RouteValues["guid"].AsString();

                _log.Info($"Exhaustive Recall:  Recall received for {guid}.  Invoking handler.");

                var value = Math.Round(_engine.ThreadPoolCallBackHttpHandlerExhaustive(
                    Guid.Parse(guid),
                    JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()))), 2);

                _log.Info($"Exhaustive Recall:  Has invoked the handler and returned a value of {value}.  Returning.");

                return value;
            }
            catch (Exception ex)
            {
                _log.Error($"Exhaustive Recall:  An error has been raised as {ex}.  Returning 500.");

                _engine.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }

        [HttpPost("ExampleFraudScoreLocalEndpoint")]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult<double>> ExampleFraudScoreLocalEndpoint()
        {
            try
            {
                if (!_dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True",StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                _log.Info("Example FraudScore Local Endpoint Recall:  Recall received.");

                var jObject = JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()));

                var responseCodeVolumeRatio = jObject.SelectToken("$.ResponseCodeEqual0Volume");

                _log.Info($"Example FraudScore Local Endpoint Recall:  Json parsed as {jObject}.  " +
                          "This endpoint will just echo back the sqrt of the ResponseCodeVolumeRatio element." +
                          " More typically this would be an R endpoint and it would recall a variety of models.");

                if (responseCodeVolumeRatio != null)
                {
                    return Math.Sqrt(responseCodeVolumeRatio.ToObject<double>());
                }

                return 0;
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Example FraudScore Local Endpoint Recall:  An error has been raised as {ex}.  Returning 500.");

                return StatusCode(500);
            }
        }
    }
}
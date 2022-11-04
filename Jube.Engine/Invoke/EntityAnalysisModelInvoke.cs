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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Jube.Data.Cache;
using Jube.Data.Extension;
using Jube.Data.Messaging;
using Jube.Data.Poco;
using Jube.Engine.Helpers;
using Jube.Engine.Invoke.Abstraction;
using Jube.Engine.Invoke.Reflect;
using Jube.Engine.Model.Processing;
using Jube.Engine.Model.Processing.CaseManagement;
using Jube.Engine.Model.Processing.Payload;
using Jube.Engine.Sanctions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using EntityAnalysisModel = Jube.Engine.Model.EntityAnalysisModel;
using EntityAnalysisModelActivationRule = Jube.Engine.Model.EntityAnalysisModelActivationRule;

namespace Jube.Engine.Invoke
{
    public class EntityAnalysisModelInvoke
    {
        private readonly DynamicEnvironment.DynamicEnvironment _jubeEnvironment;
        private readonly ILog _log;
        private readonly IModel _rabbitMqChannel;
        private ConcurrentQueue<Notification> _pendingNotification;
        
        public EntityAnalysisModelInvoke(ILog log, DynamicEnvironment.DynamicEnvironment jubeEnvironment,
            IModel rabbitMqChannel,
            ConcurrentQueue<Notification> pendingNotification,
            Random seeded,
            Dictionary<int, EntityAnalysisModel> models)
        {
            _log = log;
            _jubeEnvironment = jubeEnvironment;
            _rabbitMqChannel = rabbitMqChannel;
            _pendingNotification = pendingNotification;
            Seeded = seeded;
            Models = models;
        }

        public MemoryStream ResponseJson { get; set; }
        public Dictionary<string, object> CachePayloadDocumentStore { get; set; }
        public Dictionary<string, object> CachePayloadDocumentResponse { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayloadStore { get; set; }
        private Dictionary<int, List<Dictionary<string, object>>> AbstractionRuleMatches { get; } = new();

        public Dictionary<string, Dictionary<string, double>> EntityInstanceEntryAdaptationResponses
        {
            get;
            set;
        } = new();

        public Dictionary<string, double> EntityInstanceEntryAbstractionResponse { get; } = new();
        public Dictionary<string, int> EntityInstanceEntryTtlCountersResponse { get; } = new();
        private Dictionary<string, double> EntityInstanceEntrySanctions { get; } = new();
        public Dictionary<string, double> EntityInstanceEntrySanctionsResponse { get; } = new();
        private Dictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; } = new();
        public Dictionary<string, double> EntityInstanceEntryDictionaryKvPsResponse { get; } = new();
        public Dictionary<int, EntityAnalysisModelActivationRule> EntityInstanceEntryActivationResponse { get; } =
            new();
        public Dictionary<string, double> EntityInstanceEntryAbstractionCalculationResponse { get; } =
            new();
        public Dictionary<int, EntityAnalysisModel> Models { get; set; }
        private bool Finished { get; set; }
        public Random Seeded { get; set; }
        public List<ArchiveKey> ReportDatabaseValues { get; set; } = new();
        public Stopwatch Stopwatch { get; set; } = new();
        public bool Reprocess { get; set; }
        public bool InError { get; set; }
        public string ErrorMessage { get; set; }
        public bool AsyncEnableCallback { get; set; }
        
        public void ParseAndInvoke(EntityAnalysisModel entityAnalysisModel, MemoryStream inputStream, 
            bool async, long inputLength,
            ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke)
        {
            EntityAnalysisModel = entityAnalysisModel;
            
            var entityInstanceEntryPayloadStore = new EntityAnalysisModelInstanceEntryPayload
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                EntityAnalysisModelGuid = entityAnalysisModel.Guid
            };
            
            var cachePayloadDocumentStore = new Dictionary<string, object>();
            var cachePayloadDocumentResponse = new Dictionary<string, object>();
            var reportDatabaseValues = new List<ArchiveKey>();
            
            try
            {
                JObject json = null;
                var modelEntryValue = "";
                var referenceDateValue = default(DateTime);

                JToken jToken = null;
                if (inputLength > 0)
                {
                    json = JObject.Parse(Encoding.UTF8.GetString(inputStream.ToArray()));

                    _log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} JSON has been parsed.  The outer JSON is {json}");
                }
                else
                {
                    _log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} is JSON but has zero content length.");
                }

                try
                {
                    if (json != null) jToken = json.SelectToken(entityAnalysisModel.EntryXPath);
                    if (jToken != null) modelEntryValue = jToken.ToString();

                    _log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for ENTRY identifier {entityAnalysisModel.EntryXPath} with value of {modelEntryValue}.");
                }
                catch (Exception ex)
                {
                    InError = true;

                    _log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} could not extract the JSON Path or Querystring for ENTRY identifier {entityAnalysisModel.EntryXPath} with exception message of {ex.Message}.");

                    ErrorMessage = "Could not locate model entry value.";
                }

                try
                {
                    switch (entityAnalysisModel.ReferenceDatePayloadLocationTypeId)
                    {
                        case 3:
                            referenceDateValue = DateTime.Now;

                            _log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with the system time.");

                            break;
                        default:
                        {
                            if (json != null) jToken = json.SelectToken(entityAnalysisModel.ReferenceDateXpath);
                            if (jToken is {Type: JTokenType.Date})
                            {
                                referenceDateValue = Convert.ToDateTime(jToken);
                            }
                            else
                            {
                                if (jToken != null && !DateTime.TryParse(jToken.ToString(),
                                    out referenceDateValue))
                                {
                                    referenceDateValue = DateTime.Now;

                                    _log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with a promiscuous parse, but it failed,  so it has been set to the system time.");
                                }
                                else
                                {
                                    _log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with a promiscuous parse.");
                                }
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    InError = true;

                    _log.Error($"Could not locate model date value {ex}.");

                    ErrorMessage = "Could not locate model date value.";
                }

                if (!InError)
                {
                    entityInstanceEntryPayloadStore.CreatedDate = DateTime.Now;
                    entityInstanceEntryPayloadStore.R = Seeded.NextDouble();
                    entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceName = Dns.GetHostName();
                    entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceGuid =
                        entityAnalysisModel.EntityAnalysisInstanceGuid;
                    entityInstanceEntryPayloadStore.EntityAnalysisModelName =
                        entityAnalysisModel.Name;

                    _log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} had added the cache db GUID _id to the entry as {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} which is the same as the main GUID.");

                    entityInstanceEntryPayloadStore.ReferenceDate = referenceDateValue;
                    if (!cachePayloadDocumentStore.ContainsKey(entityAnalysisModel.ReferenceDateName))
                    {
                        cachePayloadDocumentStore.Add(entityAnalysisModel.ReferenceDateName,
                            entityInstanceEntryPayloadStore.ReferenceDate);
                        cachePayloadDocumentResponse.Add(entityAnalysisModel.ReferenceDateName,
                            entityInstanceEntryPayloadStore.ReferenceDate);
                        
                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} added reference date with the field name {entityAnalysisModel.ReferenceDateName} and value {referenceDateValue}.");
                    }
                    else
                    {
                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} already contains a reference date with the field name {entityAnalysisModel.ReferenceDateName}. This is not ideal,  try and make distinct.");
                    }

                    entityInstanceEntryPayloadStore.EntityInstanceEntryId = modelEntryValue;
                    if (!cachePayloadDocumentStore.ContainsKey(entityAnalysisModel.EntryName))
                    {
                        cachePayloadDocumentStore.Add(entityAnalysisModel.EntryName,
                            entityInstanceEntryPayloadStore.EntityInstanceEntryId);
                        cachePayloadDocumentResponse.Add(entityAnalysisModel.EntryName,
                            entityInstanceEntryPayloadStore.EntityInstanceEntryId);

                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} added field with the name {entityAnalysisModel.EntryName} and value {modelEntryValue} when adding the Entry.");
                    }
                    else
                    {
                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} already contains a field with the name {entityAnalysisModel.EntryName} when adding the Entry. This is not ideal,  try and make distinct.");
                    }

                    if (!InError)
                    {
                        _log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} is now going to loop around all of the XPath Requests specified to perform extractions of data.");

                        foreach (var xPath in entityAnalysisModel.EntityAnalysisModelRequestXPaths)
                        {
                            _log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} evaluating {xPath.Name}.");

                            if (!cachePayloadDocumentStore.ContainsKey(xPath.Name))
                            {
                                _log.Info(
                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} no duplication on {xPath.Name}.");

                                string value;
                                var defaultFallback = false;
                                try
                                {
                                    _log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name} is in the body of the POST.");

                                    value = json.SelectToken(xPath.XPath).ToString();

                                    _log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}JSON Path {xPath.XPath} has extracted value {value}.");
                                }
                                catch (Exception ex)
                                {
                                    value = xPath.DefaultValue;
                                    defaultFallback = true;

                                    _log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name} Querystring {xPath.XPath} has caused an error of {ex.Message} and has fallen back to default value of {xPath.DefaultValue}.");
                                }

                                try
                                {
                                    if (value != null)
                                        switch (xPath.DataTypeId)
                                        {
                                            //'String
                                            case 1:
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, value);

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as a string.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, value);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueString = value,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            entityInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            //'Integer
                                            case 2:
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, int.Parse(value));

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an integer.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, int.Parse(value));

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueInteger = int.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                _log.Info(
                                                    $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");

                                                break;
                                            }
                                            //'Float
                                            case 3:
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, double.Parse(value));

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as Float.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, double.Parse(value));

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueFloat = double.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            //'Date Fallback
                                            case 4 when defaultFallback:
                                            {
                                                var fallbackDate =
                                                    DateTime.Now.AddDays(int.Parse(xPath.DefaultValue) * -1);

                                                cachePayloadDocumentStore.Add(xPath.Name,
                                                    fallbackDate);

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an Date.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, fallbackDate);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = fallbackDate,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            //'Date Parse
                                            case 4 when DateTime.TryParse(value, out var dateValue):
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, dateValue);

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an Date.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, dateValue);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = dateValue,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            case 4:
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, DateTime.Now);
                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, DateTime.Now);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = DateTime.Now,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            //'Boolean
                                            case 5:
                                            {
                                                var valueBoolean = value.Equals("True",StringComparison.OrdinalIgnoreCase) || value == "1";
                                                cachePayloadDocumentStore.Add(xPath.Name, valueBoolean);

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {valueBoolean} and been typed in the BSON document as an Boolean.");

                                                if (xPath.ResponsePayload)
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, valueBoolean);

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueBoolean = (byte) (valueBoolean ? 1 : 0),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {valueBoolean} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }
                                            case 6:
                                            {
                                                cachePayloadDocumentStore.Add(xPath.Name, double.Parse(value));

                                                _log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as Lat or Long.");

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.Add(
                                                        xPath.Name, double.Parse(value));

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueFloat = double.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    _log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                if (xPath.DataTypeId == 6)
                                                {
                                                    Latitude = double.Parse(value);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and has set this as the prevailing Latitude.");
                                                }
                                                else
                                                {
                                                    Longitude = double.Parse(value);

                                                    _log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and has set this as the prevailing Longitude.");
                                                }

                                                break;
                                            }
                                        }
                                }
                                catch (Exception ex)
                                {
                                    if (!cachePayloadDocumentStore.ContainsKey(xPath.Name))
                                    {
                                        cachePayloadDocumentStore.Add(xPath.Name, value);

                                        _log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as DEFAULT string on error as {ex}.");

                                        if (xPath.ResponsePayload)
                                        {
                                            cachePayloadDocumentResponse.Add(xPath.Name, value);

                                            _log.Info(
                                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                        }

                                        if (xPath.ReportTable && !Reprocess)
                                        {
                                            reportDatabaseValues.Add(new ArchiveKey
                                            {
                                                ProcessingTypeId = 1,
                                                Key = xPath.Name,
                                                KeyValueString = value,
                                                EntityAnalysisModelInstanceEntryGuid =
                                                    entityInstanceEntryPayloadStore
                                                        .EntityAnalysisModelInstanceEntryGuid
                                            });

                                            _log.Info(
                                                $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} duplication on {xPath.Name}, stepped over.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InError = true;
                ErrorMessage =
                    "A fatal error has occured in processing.  Please check the logs for more information.";

                _log.Error(
                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has yielded an error in the XPath and Model parsing as {ex}.");
            }

            if (!InError)
            {
                _log.Info(
                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} activation promotion is set to {entityAnalysisModel.EnableActivationArchive}.");

                CachePayloadDocumentStore = cachePayloadDocumentStore;
                entityInstanceEntryPayloadStore.Payload = cachePayloadDocumentStore;
                    
                CachePayloadDocumentResponse = cachePayloadDocumentResponse;
                ReportDatabaseValues = reportDatabaseValues;

                var stopwatch = new Stopwatch();
                Stopwatch = stopwatch;

                EntityAnalysisModelInstanceEntryPayloadStore = entityInstanceEntryPayloadStore;

                _log.Info(
                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has instantiated a model invocation object and is launching it.");

                if (async)
                {
                    if (pendingEntityInvoke.Count >=
                        int.Parse(_jubeEnvironment.AppSettings("MaximumModelInvokeAsyncQueue")))
                    {
                        InError = true;
                        ErrorMessage =
                            "Maximum Queue threshold has been reached.  Please wait and retry.";
                    }
                    else
                    {
                        AsyncEnableCallback = _jubeEnvironment.AppSettings("EnableCallback").Equals("True",StringComparison.OrdinalIgnoreCase);
                        pendingEntityInvoke.Enqueue(this);
                        
                        var payloadJsonResponse = new EntityAnalysisModelInstanceEntryPayloadJson();
                        ResponseJson = payloadJsonResponse.BuildJson(EntityAnalysisModelInstanceEntryPayloadStore,entityAnalysisModel.ContractResolver);
                    }
                }
                else
                {
                    Start();
                }

                _log.Info(
                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has finished model invocation.");
            }
        }

        public void Start()
        {
            try
            {
                Stopwatch.Start();

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has started invocation timer.");

                var matchedGateway = false;
                double maxGatewayResponseElevation = 0;
                EntityAnalysisModel.ModelInvokeCounter += 1;
                EntityAnalysisModelInstanceEntryPayloadStore.Reprocess = Reprocess;

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $"has configured startup options.  The model invocation counter is {EntityAnalysisModel.ModelInvokeCounter} and " +
                    $"reprocessing is set to {EntityAnalysisModelInstanceEntryPayloadStore.Reprocess}.  Will now proceed" +
                    "to execute inline functions.");

                ExecuteInlineFunctions();
                ExecuteInlineScripts();
                ExecuteGatewayRules(ref maxGatewayResponseElevation, ref matchedGateway);

                if (matchedGateway)
                {
                    ExecuteDictionaryKvPs();
                    ExecuteTtlCounters();
                    var cachePayloadRepository =
                        new CachePayloadRepository(_jubeEnvironment.AppSettings(
                            new []{"CacheConnectionString","ConnectionString"}),_log);
                    ExecuteAbstractionRulesWithSearchKeys(cachePayloadRepository);
                    ExecuteAbstractionRulesWithoutSearchKeys();
                    ExecuteAbstractionCalculations();
                    ExecuteExhaustiveModels();
                    ExecuteAdaptations();
                    ExecuteSanctions();
                    ExecuteActivations(maxGatewayResponseElevation);
                    ExecuteCacheDbStorage(cachePayloadRepository, EntityAnalysisModelInstanceEntryPayloadStore);
                }

                QueueAsynchronousResponseMessage();
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has created a general error as {ex}.");
            }
            finally
            {
                Finished = true;

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} all model invocation processing has completed.");
            }
        }

        private void QueueAsynchronousResponseMessage()
        {
            if (EntityAnalysisModel.OutputTransform) //'Output Transform Script
            {
                var activationsForTransform = new Dictionary<int, string>();
                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has an outbound transformation routine and the transformation will now begin.");

                ResponseJson = EntityAnalysisModel.OutputTransformDelegate(
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect,
                    CachePayloadDocumentResponse,
                    EntityInstanceEntryAbstractionResponse,
                    EntityInstanceEntryTtlCountersResponse, activationsForTransform,
                    EntityInstanceEntryAbstractionCalculationResponse, null, _log);

                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has completed the outbound transformation and the memory stream has {ResponseJson.Length} bytes.");
            }
            else
            {
                var payloadJsonResponse = new EntityAnalysisModelInstanceEntryPayloadJson();
                ResponseJson = payloadJsonResponse.BuildJson(EntityAnalysisModelInstanceEntryPayloadStore,EntityAnalysisModel.ContractResolver);
            }

            if (_jubeEnvironment.AppSettings("AMQP").Equals("True",StringComparison.OrdinalIgnoreCase) && !Reprocess)
            {
                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} is about to publish the response to the Outbound Exchange.");

                var props = _rabbitMqChannel.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>();

                ResponseJson.Position = 0;
                _rabbitMqChannel.BasicPublish("jubeOutbound", "", props, ResponseJson.ToArray());

                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has published the response to the Outbound Exchange.");
            }
            else
            {
                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} does not have AMQP configured to dispatch messages to an exchange.");
            }
            
            if (AsyncEnableCallback) {
                var cacheCallbackRepository =
                    new CacheCallbackRepository(_jubeEnvironment.AppSettings(
                        new []{"CacheConnectionString","ConnectionString"}),_log);
                
                cacheCallbackRepository.Insert(ResponseJson.ToArray(),EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid);
                
                _log.Info(
                    $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} will store the callback in the database.");
            }
        }

        private void ExecuteCacheDbStorage(CachePayloadRepository cachePayloadRepository,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            InsertOrReplaceCacheEntries(cachePayloadRepository, entityAnalysisModelInstanceEntryPayload);
        }

        private void InsertOrReplaceCacheEntries(CachePayloadRepository cachePayloadRepository,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            if (EntityAnalysisModel.EnableCache)
            {
                if (Reprocess)
                {
                    cachePayloadRepository.Upsert(entityAnalysisModelInstanceEntryPayload.TenantRegistryId,
                        entityAnalysisModelInstanceEntryPayload.Payload, entityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid);                    
                    
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has replaced the entity into the cache db serially.");
                }
                else
                {
                    cachePayloadRepository.Insert(entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelId,
                        entityAnalysisModelInstanceEntryPayload.Payload, entityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has inserted the entity into the cache db serially.");
                }
            }
            else
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} does not allow entity storage in the cache.");
            }
        }

        private bool CheckSuppressedResponseElevation()
        {
            if (EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count > EntityAnalysisModel.ResponseElevationFrequencyLimitCounter)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an activation balance of {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} and has exceeded threshold.");

                return true;
            }

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an activation balance of {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} and has not exceeded threshold.");

            return false;
        }

        private void ExecuteActivations(double maxGatewayResponseElevation)
        {
            var suppressedActivationRules = new List<string>();
            CreateCase createCase = null;
            int iActivationRule;
            int? prevailingActivationRuleId = null;
            string prevailingActivationRuleName = default;
            var activationRuleCount = 0;
            double responseElevationHighWaterMark = 0;
            double responseElevationNotAdjustedHighWaterMark = 0;

            var suppressedModel = ActivationRuleGetSuppressedModel(ref suppressedActivationRules);
            var suppressedResponseElevation = CheckSuppressedResponseElevation();

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now process {EntityAnalysisModel.ModelActivationRules.Count} Activation Rules .");
    
            for (iActivationRule = 0; iActivationRule < EntityAnalysisModel.ModelActivationRules.Count; iActivationRule++)
                try
                {
                    var evaluateActivationRule = EntityAnalysisModel.ModelActivationRules[iActivationRule];
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating activation rule {evaluateActivationRule.Id}.");

                    var suppressed = false;
                    if (suppressedModel || suppressedResponseElevation)
                    {
                        suppressed = true;

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} activation rule {evaluateActivationRule.Id} is suppressed at the model level or has exceeded response elevation counter at {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} or {EntityAnalysisModel.BillingResponseElevationBalance}.");
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the model level, will test at rule level.");

                        if (!evaluateActivationRule.EnableReprocessing && Reprocess)
                        {
                            suppressed = true;

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level because of reprocessing.");
                        }
                        else if (suppressedActivationRules != null)
                        {
                            suppressed = suppressedActivationRules.Contains(evaluateActivationRule.Name);

                            _log.Info(
                                suppressedModel
                                    ? $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level."
                                    : $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the activation rule level.");
                        }
                    }

                    var activationSample = false;
                    if (evaluateActivationRule.ActivationSample >= Seeded.NextDouble())
                    {
                        activationSample = true;

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id}  has passed sampling and is eligible for activation.");
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id}  has failed in sampling so certain activations will not take place even if there is a match on the activation rule.");
                    }

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id}  is starting to test the activation rule match.");

                    var matched = ReflectRule.Execute(evaluateActivationRule, EntityAnalysisModel,
                        EntityAnalysisModelInstanceEntryPayloadStore,
                        EntityInstanceEntryDictionaryKvPs, _log);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id}  has finished testing the activation rule and it has a matched status of {matched}.");

                    if (matched)
                    {
                        EntityAnalysisModelInstanceEntryPayloadStore.Activation.Add(
                            evaluateActivationRule.Name,
                            new EntityModelActivationRulePayload
                            {
                                Visible = evaluateActivationRule.Visible
                            });

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} and has added the activation rule {evaluateActivationRule.Id} flag to the activation buffer for processing.");

                        if (evaluateActivationRule.ResponsePayload)
                        {
                            EntityInstanceEntryActivationResponse.Add(
                                evaluateActivationRule.Id,
                                evaluateActivationRule);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }

                        if (evaluateActivationRule.ReportTable && !Reprocess)
                        {
                            ReportDatabaseValues.Add(new ArchiveKey
                            {
                                ProcessingTypeId = 11,
                                Key = evaluateActivationRule.Name,
                                KeyValueBoolean = 1,
                                EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                    .EntityAnalysisModelInstanceEntryGuid
                            });

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }

                        ActivationRuleGetResponseElevationHighWaterMark(evaluateActivationRule,
                            ref responseElevationHighWaterMark);
                        ActivationRuleResponseElevationHighest(evaluateActivationRule,
                            responseElevationHighWaterMark, suppressed, activationSample,
                            maxGatewayResponseElevation);
                        //ActivationRuleTagging(evaluateActivationRule, suppressed, activationSample);
                        ActivationRuleNotification(evaluateActivationRule, suppressed, activationSample);
                        ActivationRuleCountsAndArchiveHighWatermark(iActivationRule, evaluateActivationRule,
                            suppressed, activationSample, ref activationRuleCount, ref prevailingActivationRuleId,
                            ref prevailingActivationRuleName);
                        ActivationRuleActivationWatcher(evaluateActivationRule, suppressed, activationSample,
                            responseElevationNotAdjustedHighWaterMark);

                        createCase ??= ActivationRuleCreateCaseObject(evaluateActivationRule, suppressed,
                            activationSample);
                        
                        ActivationRuleTtlCounter(evaluateActivationRule);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} error in TTL Counter processing as {ex} .");
                }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Activation", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has added the response elevation for use in bidding against other models if called by model inheritance.");

            ActivationRuleFinishResponseElevation(EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value);
            ActivationRuleResponseElevationAddToCounters();
            ActivationRuleBuildArchivePayload(activationRuleCount, prevailingActivationRuleId,
                createCase);
        }

        private void ActivationRuleResponseElevationAddToCounters()
        {
            if (EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value > 0)
            {
                EntityAnalysisModel.BillingResponseElevationCount += 1;
                EntityAnalysisModel.BillingResponseElevationJournal.Enqueue(DateTime.Now);

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation is greater than 0 and has incremented counters for throttling.  The Billing Response Elevation Count is {EntityAnalysisModel.BillingResponseElevationCount}.");
            }
        }

        private void ActivationRuleBuildArchivePayload(int activationRuleCount, int? prevailingActivationRuleId, CreateCase createCase)
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has been selected for sampling or case creation is been specified. Is building the XML payload from the payload created.");

            EntityAnalysisModelInstanceEntryPayloadStore.TenantRegistryId = EntityAnalysisModel.TenantRegistryId;
            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelId = EntityAnalysisModel.Id;
            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelName = EntityAnalysisModel.Name;
            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelActivationRuleCount = activationRuleCount;
            EntityAnalysisModelInstanceEntryPayloadStore.PrevailingEntityAnalysisModelActivationRuleId = prevailingActivationRuleId;
            EntityAnalysisModelInstanceEntryPayloadStore.ReportDatabaseValues = ReportDatabaseValues;
            EntityAnalysisModelInstanceEntryPayloadStore.Payload = CachePayloadDocumentStore;
            EntityAnalysisModelInstanceEntryPayloadStore.ArchiveEnqueueDate = DateTime.Now;
            EntityAnalysisModelInstanceEntryPayloadStore.CreateCasePayload = createCase;
            EntityAnalysisModelInstanceEntryPayloadStore.StoreInRdbms = EntityAnalysisModel.EnableRdbmsArchive;

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been created for archive.");

            if (Reprocess)
            {
                EntityAnalysisModel.CaseCreationAndArchiver(EntityAnalysisModelInstanceEntryPayloadStore,
                    null); //It does not matter that the insert buffer is nothing,  as reprocess will always Upsert for SQL Server.

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been added for archive synchronously as it is set for reprocessing.");
            }
            else
            {
                EntityAnalysisModel.PersistToDatabaseAsync.Enqueue(EntityAnalysisModelInstanceEntryPayloadStore);

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been added for archive asynchronously.");
            }
        }

        private void ActivationRuleFinishResponseElevation(double responseElevation)
        {
            if (responseElevation > 0)
            {
                EntityAnalysisModel.ModelResponseElevationCounter += 1;
                EntityAnalysisModel.ModelResponseElevationSum += responseElevation;

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation is greater than zero and has incremented Response Elevation Counter which has a value of {EntityAnalysisModel.ModelResponseElevationCounter} and Model Response Elevation Sum which has a value of {EntityAnalysisModel.ModelResponseElevationSum}.");
            }
        }

        private void ActivationRuleTtlCounter(EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            if (!evaluateActivationRule.EnableTtlCounter || Reprocess) return;
            
            var cacheTtlCounterRepository = new CacheTtlCounterRepository(
                _jubeEnvironment.AppSettings(
                    new []{"CacheConnectionString","ConnectionString"}),_log);

            var cacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(
                _jubeEnvironment.AppSettings(
                    new []{"CacheConnectionString","ConnectionString"}),_log);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is incrementing TTL counter {evaluateActivationRule.EntityAnalysisModelTtlCounterId} as this is enabled in the activation rule.");

            var found = false;
            foreach (var (key, value) in
                     from targetTtlCounterModelKvp in Models
                     where evaluateActivationRule.EntityAnalysisModelIdTtlCounter ==
                           targetTtlCounterModelKvp.Value.Id
                     select targetTtlCounterModelKvp)
            {
                foreach (var foundTtlCounter in value.ModelTtlCounters)
                {
                    if (evaluateActivationRule.EntityAnalysisModelTtlCounterId == foundTtlCounter.Id)
                    {
                        try
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has matched the name in the activation rule to the TTL counters loaded for {EntityAnalysisModel.Name} in model id {value.Id}.");

                            if (CachePayloadDocumentStore.ContainsKey(foundTtlCounter.TtlCounterDataName))
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} found a value a value for TTL counter name {foundTtlCounter.Name} as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]}.");
                                    
                                if (value.EnableTtlCounter)
                                {
                                    if (evaluateActivationRule.EntityAnalysisModelIdTtlCounter ==
                                        key)
                                    {
                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]}.  Is about to insert the entry.");

                                        if (!foundTtlCounter.EnableLiveForever)
                                        {
                                            cacheTtlCounterEntryRepository.Insert(
                                                EntityAnalysisModel.Id,
                                                foundTtlCounter.TtlCounterDataName,
                                                CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]
                                                    .AsString(),
                                                foundTtlCounter.Id,
                                                EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate);
                                        }
                                        else
                                        {
                                            _log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]} is set to live forever so no entry has been made to wind back counters.");
                                        }

                                        cacheTtlCounterRepository.Upsert(
                                            EntityAnalysisModel.Id,
                                            foundTtlCounter.TtlCounterDataName,
                                            CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName].AsString(),
                                            foundTtlCounter.Id,
                                            EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate
                                        );
                                    }
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} cannot create a TTL counter for name {value.Name} as TTL Counter Storage is disabled for the model id {value.Id}.");
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} could not find a value for TTL counter name {foundTtlCounter.Name}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} error performing insertion on match for a TTL Counter by name of {foundTtlCounter.Name} and id of {foundTtlCounter.Id} with exception message of {ex.Message}.");
                        }

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has matched the name in the activation rule to the TTL counters loaded for {EntityAnalysisModel.Name} and has finished processing.");

                        found = true;
                    }

                    if (found) break;
                }

                if (found) break;
            }
        }

        private void ActivationRuleResponseElevationHighest(
            EntityAnalysisModelActivationRule evaluateActivationRule, double responseElevationHighWaterMark, bool suppressed,
            bool activationSample, double maxGatewayResponseElevation)
        {
            if (responseElevationHighWaterMark > EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value &&
                !suppressed &&
                activationSample && evaluateActivationRule.EnableResponseElevation)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is the current largest Response Elevation {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation} but is less than the new value of {responseElevationHighWaterMark} so it will be elevated.  Some integrity checks will also be performed.");

                if (responseElevationHighWaterMark > EntityAnalysisModel.MaxResponseElevation)
                {
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value = EntityAnalysisModel.MaxResponseElevation;
                    EntityAnalysisModel.ResponseElevationValueLimitCounter += 1;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation exceeds the maximum allowed in the model of {EntityAnalysisModel.MaxResponseElevation}, so has been truncated to {EntityAnalysisModel.MaxResponseElevation} and the Response Elevation Value Limit Counter incremented.");
                }
                else
                {
                    if (responseElevationHighWaterMark > maxGatewayResponseElevation)
                    {
                        EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value = maxGatewayResponseElevation;
                        EntityAnalysisModel.ResponseElevationValueGatewayLimitCounter += 1;

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation exceeds the maximum allowed in the gateway rule of {maxGatewayResponseElevation}, so has been truncated to {maxGatewayResponseElevation} and the Response Elevation Value Gateway Limit counter incremented.");
                    }
                    else
                    {
                        EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value = responseElevationHighWaterMark;

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation has tested the limits and the response elevation is being carried forward as {responseElevationHighWaterMark}.");
                    }
                }

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is being tested against the current limits and cap to zero if exceeded.");

                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content = evaluateActivationRule.ResponseElevationContent;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect = evaluateActivationRule.ResponseElevationRedirect;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor =
                    evaluateActivationRule.ResponseElevationBackColor;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor =
                    evaluateActivationRule.ResponseElevationForeColor;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content = evaluateActivationRule.ResponseElevationContent;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect = evaluateActivationRule.ResponseElevationRedirect;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor =
                    evaluateActivationRule.ResponseElevationForeColor;
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor =
                    evaluateActivationRule.ResponseElevationBackColor;

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} updated the response elevation to {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation}.");

                if (EntityAnalysisModel.EnableResponseElevationLimit)
                {
                    EntityAnalysisModel.BillingResponseElevationBalanceEntries.Enqueue(new ResponseElevation
                    {
                        CreatedDate = DateTime.Now,
                        Value = EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value
                    });

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has noted the response elevation date on the counter queue. There are {EntityAnalysisModel.ActivationWatcherCountJournal.Count} in queue.");
                }
                else
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} does not have response elevation limit enabled,  so has not noted the response elevation date.");
                }
            }
        }

        private void ActivationRuleNotification(EntityAnalysisModelActivationRule evaluateActivationRule, bool suppressed,
            bool activationSample)
        {
            if (_jubeEnvironment.AppSettings("EnableNotification").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                if (!suppressed && activationSample && evaluateActivationRule.EnableNotification && !Reprocess)
                {
                    var notification = new Notification
                    {
                        NotificationBody = ReplaceTokens(evaluateActivationRule.NotificationBody),
                        NotificationDestination = ReplaceTokens(evaluateActivationRule.NotificationDestination),
                        NotificationSubject = ReplaceTokens(evaluateActivationRule.NotificationSubject),
                        NotificationTypeId= evaluateActivationRule.NotificationTypeId
                    };
                
                    if (_jubeEnvironment.AppSettings("AMQP").Equals("True",StringComparison.OrdinalIgnoreCase))
                    {

                        var jsonString = JsonConvert.SerializeObject(notification);
                        var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                        _rabbitMqChannel.BasicPublish("", "jubeNotifications", null, bodyBytes);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has sent a message to the notification dispatcher as {jsonString}.");
                    }
                    else
                    {
                        _pendingNotification.Enqueue(notification);
                    
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has not sent a message to the internal notification dispatcher because AMQP is not enabled.");
                    }
                }
            }
            else
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has not sent a message as notification disabled.");
            }
        }

        private string ReplaceTokens(string message)
        {
            var notificationTokenizationList = NotificationTokenization.ReturnTokens(message);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has found {notificationTokenizationList.Count} tokens in message {message}.");

            foreach (var notificationToken in notificationTokenizationList)
            {
                var notificationTokenValue = "";
                if (CachePayloadDocumentStore.ContainsKey(notificationToken))
                    notificationTokenValue = CachePayloadDocumentStore[notificationToken].ToString();
                else if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.ContainsKey(notificationToken))
                    notificationTokenValue = EntityAnalysisModelInstanceEntryPayloadStore
                        .Abstraction[notificationToken].ToString(CultureInfo.InvariantCulture);
                else if (EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.ContainsKey(notificationToken))
                    notificationTokenValue = EntityAnalysisModelInstanceEntryPayloadStore
                        .TtlCounter[notificationToken].ToString();
                else if (
                    EntityAnalysisModelInstanceEntryPayloadStore.AbstractionCalculation.ContainsKey(
                        notificationToken))
                    notificationTokenValue = EntityAnalysisModelInstanceEntryPayloadStore
                        .AbstractionCalculation[notificationToken]
                        .ToString(CultureInfo.InvariantCulture);
                var notificationReplaceToken = $"[@{notificationToken}@]";
                message = message.Replace(notificationReplaceToken, notificationTokenValue);

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finalized notification message {message}.");
            }

            return message;
        }

        private void ActivationRuleGetResponseElevationHighWaterMark(EntityAnalysisModelActivationRule evaluateActivationRule,
            ref double responseElevationHighWaterMark)
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                $"and model {EntityAnalysisModel.Id} will begin processing of response elevation for activation " +
                $"rule {evaluateActivationRule.Id}. " +
                $"Current high water mark on response elevation is {responseElevationHighWaterMark}.");

            responseElevationHighWaterMark = evaluateActivationRule.ResponseElevation;

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is the current largest Response Elevation {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation} and will be tested against the new one of {responseElevationHighWaterMark} .");
        }

        private bool ActivationRuleGetSuppressedModel(ref List<string> suppressedActivationRules)
        {
            var suppressedModelValue = false;
            foreach (var xpath in EntityAnalysisModel.EntityAnalysisModelRequestXPaths.Where(w => w.EnableSuppression).ToList())
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name}.  Will now check to see if has a suppressed value.");

                if (CachePayloadDocumentStore.ContainsKey(xpath.Name))
                {
                    if (EntityAnalysisModel.EntityAnalysisModelSuppressionModels.ContainsKey(xpath.Name))
                    {
                        suppressedModelValue =
                            EntityAnalysisModel.EntityAnalysisModelSuppressionModels[xpath.Name].Contains(
                                CachePayloadDocumentStore[xpath.Name].AsString());
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but it has no keys.");
                    }

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");

                    if (EntityAnalysisModel.EntityAnalysisModelSuppressionRules.ContainsKey(xpath.Name))
                    {
                        if (EntityAnalysisModel.EntityAnalysisModelSuppressionRules[xpath.Name].ContainsKey(
                            CachePayloadDocumentStore[xpath.Name].AsString()))
                        {
                            suppressedActivationRules =
                                EntityAnalysisModel.EntityAnalysisModelSuppressionRules[xpath.Name][
                                    CachePayloadDocumentStore[xpath.Name].AsString()];

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");
                        }
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but it has no keys.");
                    }
                }
                else
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but could not locate the value in the data payload.");
                }
            }

            return suppressedModelValue;
        }

        private CreateCase ActivationRuleCreateCaseObject(EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, bool activationSample)
        {
            CreateCase createCase = null;
            if (evaluateActivationRule.EnableCaseWorkflow && !suppressed && activationSample)
            {
                createCase = new CreateCase
                {
                    CaseEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid,
                    CaseWorkflowId = evaluateActivationRule.CaseWorkflowId,
                    CaseWorkflowStatusId = evaluateActivationRule.CaseWorkflowStatusId
                };

                if (evaluateActivationRule.BypassSuspendSample > Seeded.NextDouble())
                {
                    createCase.SuspendBypass = true;
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for bypass.");

                    switch (evaluateActivationRule.BypassSuspendInterval)
                    {
                        case 'n':
                            createCase.SuspendBypassDate =
                                DateTime.Now.AddMinutes(evaluateActivationRule.BypassSuspendValue);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of n to create a date of {createCase.SuspendBypassDate}.");

                            break;
                        case 'h':
                            createCase.SuspendBypassDate =
                                DateTime.Now.AddHours(evaluateActivationRule.BypassSuspendValue);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of h to create a date of {createCase.SuspendBypassDate}.");

                            break;
                        case 'd':
                            createCase.SuspendBypassDate =
                                DateTime.Now.AddDays(evaluateActivationRule.BypassSuspendValue);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of d to create a date of {createCase.SuspendBypassDate}.");

                            break;
                        case 'm':
                            createCase.SuspendBypassDate =
                                DateTime.Now.AddMonths(evaluateActivationRule.BypassSuspendValue);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of m to create a date of {createCase.SuspendBypassDate}.");

                            break;
                    }
                }
                else
                {
                    createCase.SuspendBypass = false;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for open.");

                    createCase.SuspendBypassDate = DateTime.Now;
                }

                _log.Info(
                     string.IsNullOrEmpty(evaluateActivationRule.CaseKey)
                        ? $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is an entry foreign key."
                        : $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is not a entry foreign key.");

                if (evaluateActivationRule.CaseKey != null && CachePayloadDocumentStore.ContainsKey(evaluateActivationRule.CaseKey))
                {
                    createCase.CaseKey = evaluateActivationRule.CaseKey;
                    createCase.CaseKeyValue = CachePayloadDocumentStore[evaluateActivationRule.CaseKey].ToString();

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} and case key value is {CachePayloadDocumentStore[evaluateActivationRule.CaseKey]}.");
                }
                else
                {
                    createCase.CaseKeyValue = EntityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId;
                    createCase.CaseKey = null;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} does not have a value,  has fallen back to the entity id of {EntityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId}.");
                }

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has flagged that a case needs to be created for case workflow id {createCase.CaseWorkflowId} and case status id {createCase.CaseWorkflowStatusId}.  The case will be queued later after the archive XML has been created.");
            }

            return createCase;
        }

        private void ActivationRuleActivationWatcher(EntityAnalysisModelActivationRule evaluateActivationRule, bool suppressed,
            bool activationSample, double responseElevationNotAdjustedHighWaterMark)
        {
            if (evaluateActivationRule.SendToActivationWatcher && !suppressed && activationSample && !Reprocess &&
                EntityAnalysisModel.EnableActivationWatcher)
                try
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the current activation watch count is {EntityAnalysisModel.ActivationWatcherCount} which will be tested against the threshold {EntityAnalysisModel.MaxActivationWatcherThreshold}.");

                    if (EntityAnalysisModel.ActivationWatcherCount < EntityAnalysisModel.MaxActivationWatcherThreshold &&
                        EntityAnalysisModel.ActivationWatcherSample >= Seeded.NextDouble())
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the current activation watch count is {EntityAnalysisModel.ActivationWatcherCount} which will be tested against the threshold {EntityAnalysisModel.MaxActivationWatcherThreshold} and selected via random sampling.");

                        var activationWatcher = new ActivationWatcher
                        {
                            BackColor = evaluateActivationRule.ResponseElevationBackColor,
                            ForeColor = evaluateActivationRule.ResponseElevationForeColor,
                            ResponseElevation = responseElevationNotAdjustedHighWaterMark,
                            ResponseElevationContent = evaluateActivationRule.ResponseElevationContent,
                            ActivationRuleSummary = evaluateActivationRule.Name,
                            TenantRegistryId = EntityAnalysisModel.TenantRegistryId,
                            CreatedDate = DateTime.Now,
                            Longitude = Longitude,
                            Latitude = Latitude,
                            Key = "",
                            KeyValue = ""
                        };

                        if (CachePayloadDocumentStore.ContainsKey(evaluateActivationRule
                            .ResponseElevationKey))
                        {
                            activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                            activationWatcher.KeyValue =
                                CachePayloadDocumentStore[evaluateActivationRule
                                    .ResponseElevationKey].AsString();

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} found key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                        }
                        else
                        {
                            activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                            activationWatcher.KeyValue = "Missing";

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} fallen back to key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                        }
                        
                        var jsonString = JsonConvert.SerializeObject(activationWatcher, new JsonSerializerSettings
                        {
                            ContractResolver = EntityAnalysisModel.ContractResolver 
                        });
                        
                        var bodyBytes = Encoding.UTF8.GetBytes(jsonString);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has serialized the Activation Watcher Object to be dispatched.");

                        if (_jubeEnvironment.AppSettings("ActivationWatcherAllowPersist").Equals("True",StringComparison.OrdinalIgnoreCase))
                        {
                            EntityAnalysisModel.PersistToActivationWatcherAsync.Enqueue(activationWatcher);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} replay is  allowed so it has been sent to the database. {EntityAnalysisModel.ActivationWatcherCount}.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} replay is not allowed so it has not been sent to the database. {EntityAnalysisModel.ActivationWatcherCount}.");
                        }

                        if (_jubeEnvironment.AppSettings("StreamingActivationWatcher").Equals("True",StringComparison.OrdinalIgnoreCase))
                        {
                            var messaging = new Messaging(_jubeEnvironment.AppSettings("ConnectionString"),_log);
                            
                            messaging.SendActivation(bodyBytes);
                            
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} streaming is allowed so it has been sent to the database as a notification in the activation channel. {EntityAnalysisModel.ActivationWatcherCount}.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} streaming is not allowed so it has not been sent to the database as a notification in the activation channel. {EntityAnalysisModel.ActivationWatcherCount}.");
                        }
                        
                        EntityAnalysisModel.ActivationWatcherCount += 1;

                        if (_jubeEnvironment.AppSettings("AMQP").Equals("True",StringComparison.OrdinalIgnoreCase))
                        {
                            var properties = _rabbitMqChannel.CreateBasicProperties();

                            _rabbitMqChannel.BasicPublish("jubeActivations", "", properties, bodyBytes);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} AMQP is  allowed so it has been published to the RabbitMQ.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} AMQP is not allowed, so publish has been stepped over.");
                        }

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has sent a message to the watcher as {jsonString} the activation watcher counter has been incremented and is currently {EntityAnalysisModel.ActivationWatcherCount}.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} there has been an error in Activation Watcher processing {ex}.");
                }
        }

        private void ActivationRuleCountsAndArchiveHighWatermark(int iActivationRule,
            EntityAnalysisModelActivationRule evaluateActivationRule, bool suppressed, bool activationSample,
            ref int activationRuleCount, ref int? prevailingActivationRuleId, ref string prevailingActivationRuleName)
        {
            if (!suppressed && activationSample)
            {
                EntityAnalysisModel.ModelActivationRules[iActivationRule].Counter += 1;

                if (EntityAnalysisModel.ModelActivationRules[iActivationRule].Visible)
                {
                    prevailingActivationRuleId = EntityAnalysisModel.ModelActivationRules[iActivationRule].Id;
                    prevailingActivationRuleName = EntityAnalysisModel.ModelActivationRules[iActivationRule].Name;
                                        
                    activationRuleCount += 1;
                }
                else
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} has not been included in the local count as the rule is not set to visible{prevailingActivationRuleId}.  This activation rule count is {activationRuleCount}.");

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} activation counter has been incremented and the prevailing activation rule has been set to {prevailingActivationRuleId}.  This activation rule count is {activationRuleCount}.");
            }
        }

        private void ExecuteSanctions()
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is starting Sanctions processing.");

            var cacheSanctionRepository = new CacheSanctionRepository(_jubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),_log);

            double sumLevenshteinDistance = 0;
            foreach (var entityAnalysisModelSanction in EntityAnalysisModel.EntityAnalysisModelSanctions)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name}.");

                try
                {
                    if (CachePayloadDocumentStore.ContainsKey(entityAnalysisModelSanction.MultipartStringDataName))
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to look for Sanctions Match in the Cache.");

                        if (CachePayloadDocumentStore[entityAnalysisModelSanction.MultipartStringDataName] == null)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an entry but is a null value.");
                        }
                        else
                        {
                            var multiPartStringValue = CachePayloadDocumentStore
                                [entityAnalysisModelSanction.MultipartStringDataName].AsString();

                            var sanction = cacheSanctionRepository.GetByMultiPartStringDistanceThreshold(
                                EntityAnalysisModel.Id, multiPartStringValue,
                                entityAnalysisModelSanction.Distance
                            );

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} and has found sanction as {sanction != null}.");

                            var foundCacheSanctions = false;
                            if (sanction != null)
                            {
                                var deleteLineCacheKeys = sanction.CreatedDate;

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} has a cache interval of {entityAnalysisModelSanction.CacheInterval} and value of {entityAnalysisModelSanction.CacheValue}.");

                                deleteLineCacheKeys = entityAnalysisModelSanction.CacheInterval switch
                                {
                                    's' => deleteLineCacheKeys.AddSeconds(
                                        entityAnalysisModelSanction.CacheValue),
                                    'n' => deleteLineCacheKeys.AddMinutes(
                                        entityAnalysisModelSanction.CacheValue),
                                    'h' => deleteLineCacheKeys.AddHours(entityAnalysisModelSanction.CacheValue),
                                    'd' => deleteLineCacheKeys.AddDays(entityAnalysisModelSanction.CacheValue),
                                    _ => deleteLineCacheKeys.AddDays(entityAnalysisModelSanction.CacheValue)
                                };

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} has an expiry date of {deleteLineCacheKeys}");

                                if (deleteLineCacheKeys <= DateTime.Now)
                                {
                                    foundCacheSanctions = false;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available because of expiration.");
                                }
                                else
                                {
                                    foundCacheSanctions = true;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is available.");
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available.");
                            }

                            if (foundCacheSanctions)
                            {
                                if (!EntityInstanceEntrySanctions.ContainsKey(entityAnalysisModelSanction.Name))
                                {
                                    if (sanction.Value.HasValue)
                                    {
                                        EntityInstanceEntrySanctions.Add(entityAnalysisModelSanction.Name, sanction.Value.Value);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} is adding cache value of {sanction.Value} to processing. Reprocessing will not take place.");

                                        if (entityAnalysisModelSanction.ResponsePayload)
                                        {
                                            EntityInstanceEntrySanctionsResponse.Add(entityAnalysisModelSanction.Name,
                                                sanction.Value.Value);

                                            _log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {sanction.Value} to the response payload.");
                                        }   
                                    }
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} is not adding cache value of {sanction.Value} to processing as is a duplicate. Reprocessing will not take place.");
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and is about to execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance}.");

                                var sanctionEntryReturns = LevenshteinDistance.CheckMultipartString(
                                    multiPartStringValue,
                                    entityAnalysisModelSanction.Distance, EntityAnalysisModel.SanctionsEntries);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} and found {sanctionEntryReturns.Count} matches.");

                                double? averageLevenshteinDistance = null;
                                if (sanctionEntryReturns.Count == 0)
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches average distance set to 5.");
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the average.");

                                    sumLevenshteinDistance = sanctionEntryReturns.Aggregate(sumLevenshteinDistance,
                                        (current, sanctionEntryReturn) =>
                                            current + sanctionEntryReturn.LevenshteinDistance);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance}.");

                                    if (!((sumLevenshteinDistance == 0) | double.IsNaN(sumLevenshteinDistance)))
                                    {
                                        averageLevenshteinDistance =
                                            sumLevenshteinDistance / sanctionEntryReturns.Count;

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} and calculated average as {averageLevenshteinDistance?? null}.");
                                    }
                                    else
                                    {
                                        averageLevenshteinDistance = 0;
                                        
                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} but is an invalid number.");
                                    }
                                    
                                    if (!EntityInstanceEntrySanctions.ContainsKey(entityAnalysisModelSanction.Name))
                                    {
                                        if (averageLevenshteinDistance.HasValue)
                                        {
                                            EntityInstanceEntrySanctions.Add(entityAnalysisModelSanction.Name,
                                                averageLevenshteinDistance.Value);

                                            _log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance?? null} to the payload.");

                                            if (entityAnalysisModelSanction.ResponsePayload)
                                            {
                                                EntityInstanceEntrySanctionsResponse.Add(entityAnalysisModelSanction.Name,
                                                    averageLevenshteinDistance.Value);

                                                _log.Info(
                                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance?? null} to the response payload.");
                                            }   
                                        }
                                    }
                                    else
                                    {
                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance?? null} but has not been added to payload as it is a duplicate.");
                                    }
                                }

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has constructed a cache payload as Distance of {averageLevenshteinDistance?? null}, MultiPartString of {multiPartStringValue} and a created date of now.");

                                if (sanction == null)
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to insert cache payload.");

                                    cacheSanctionRepository.Insert(EntityAnalysisModel.Id, multiPartStringValue,
                                        entityAnalysisModelSanction.Distance,averageLevenshteinDistance);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has inserted cache payload.");
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to update cache payload for {sanction.Id}.");

                                    cacheSanctionRepository.Update(sanction.Id,averageLevenshteinDistance);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has updated cache payload.");
                                }
                            }
                        }
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} but could not find it in the payload.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has seen an error in sanctions checking as {ex}.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.Sanction = EntityInstanceEntrySanctions;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Sanction", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finished sanctions processing.");
        }

        private void ExecuteAdaptations()
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will begin processing adaptations.");

            foreach (var (adaptationKey, modelAdaptation) in EntityAnalysisModel.EntityAnalysisModelAdaptations)
                try
                {
                    var jsonForPlumber = new Dictionary<string, object>();

                    /*_log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} is starting to prepare R Plumber post.");

                    foreach (var (key, value) in CachePayloadDocumentStore)
                        if (!jsonForPlumber.ContainsKey(key))
                            jsonForPlumber.Add(key, value);*/

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has finished allocating the Data collection for R Plumber POST.");

                    foreach (var (key, value) in EntityAnalysisModelInstanceEntryPayloadStore
                        .Abstraction.Where(jsonForPlumberCacheElement => !jsonForPlumber.ContainsKey(jsonForPlumberCacheElement.Key)))
                        jsonForPlumber.Add(key, value);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has finished allocating the Abstractions for R Plumber POST.");

                    foreach (var (key, value) in
                        from jsonForPlumberKvpInteger in EntityAnalysisModelInstanceEntryPayloadStore
                            .TtlCounter
                        where !jsonForPlumber.ContainsKey(jsonForPlumberKvpInteger.Key)
                        select jsonForPlumberKvpInteger)
                        jsonForPlumber.Add(key, value);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has finished allocating the TTL Counters for R Plumber POST.");

                    foreach (var (key, value) in
                        from jsonForPlumberKvpDouble in EntityAnalysisModelInstanceEntryPayloadStore
                            .AbstractionCalculation
                        where !jsonForPlumber.ContainsKey(jsonForPlumberKvpDouble.Key)
                        select jsonForPlumberKvpDouble)
                        jsonForPlumber.Add(key, value);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has finished allocating the Abstraction Calculations for R Plumber POST.");

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has finished allocating and created JSON for R Plumber:{jsonForPlumber}.");

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} is about to post to R Plumber.");

                    var adaptationSimulation = modelAdaptation.Post(jsonForPlumber, _log);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation}.");

                    EntityAnalysisModelInstanceEntryPayloadStore.HttpAdaptation.Add(
                        modelAdaptation.Name, adaptationSimulation.Result);

                    if (modelAdaptation.ResponsePayload)
                    {
                        var simulations = new Dictionary<string, double>
                        {
                            {"1", adaptationSimulation.Result}
                        };
                        EntityInstanceEntryAdaptationResponses.Add(modelAdaptation.Name,
                            simulations);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation} and added it to the response payload.");
                    }

                    if (modelAdaptation.ReportTable && !Reprocess)
                    {
                        ReportDatabaseValues.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 9,
                            Key = modelAdaptation.Name,
                            KeyValueFloat = adaptationSimulation.Result,
                            EntityAnalysisModelInstanceEntryGuid =
                                EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                        });

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation} and has added it to the SQL report payload.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating {adaptationKey} produced an error {ex}.");
                }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Adaptation", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Adaptations have concluded.");
        }


        private void ExecuteExhaustiveModels()
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now perform Exhaustive and will loop through each.");
            
            foreach (var exhaustive in EntityAnalysisModel.ExhaustiveModels)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}.");
                
                try
                {
                    var data = new double[exhaustive.NetworkVariablesInOrder.Count];
                    for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                    {
                        var cleanName = exhaustive.NetworkVariablesInOrder[i].Name.Contains('.') 
                            ? exhaustive.NetworkVariablesInOrder[i].Name.Split(".")[1] 
                            : exhaustive.NetworkVariablesInOrder[i].Name;
                        
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                            $"  Will look up {cleanName} for processing type id {exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId}.");
                        
                        switch (exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId)
                        {
                            case 1:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for payload.");
                                
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Payload.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Payload[cleanName].AsDouble());
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                        
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }
                                
                                break;
                            case 2:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for KVP.");
                            
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Dictionary.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Dictionary[cleanName]);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                        
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                            case 3:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for Ttl Counter.");
                            
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .TtlCounter.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter[cleanName]);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                        
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                            case 4:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for Ttl Counter.");
                                
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Sanction.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Sanction[cleanName]);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                            case 5:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for Abstraction.");
                                
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Abstraction[cleanName]);
                                        
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                            case 6:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for Abstraction Calculation.");
                            
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .AbstractionCalculation.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.AbstractionCalculation[cleanName]);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                            default:
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                    $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                    $" will look up {cleanName} for Abstraction as the default.");
                                
                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Abstraction[cleanName]);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} found value {data[i]}.");
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);
                                    
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" {cleanName} fall back value {data[i]}.");
                                }

                                break;
                        }
                    }
                    
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} is about to recall model with {data.Length} variables.");
                    
                    var value = exhaustive.TopologyNetwork.Compute(data)[0];
                    
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has recalled a score of {value}.  Will proceed to add the value to payload collection.");
                    
                    EntityAnalysisModelInstanceEntryPayloadStore.ExhaustiveAdaptation.Add(exhaustive.Name,value);
                    
                }
                catch (Exception ex)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has exception {ex}.");
                }
                
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has concluded exhaustive recall.");
            }
        }
        
        private void ExecuteAbstractionCalculations()
        {
            double calculationDouble = 0;
            
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now perform entity analysis abstractions calculations and will loop through each.");

            foreach (var entityAnalysisModelAbstractionCalculation in EntityAnalysisModel.EntityAnalysisModelAbstractionCalculations)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id}.");

                try
                {
                    if (entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId == 5)
                    {
                        calculationDouble = ReflectRule.Execute(entityAnalysisModelAbstractionCalculation, EntityAnalysisModel,
                            EntityAnalysisModelInstanceEntryPayloadStore, EntityInstanceEntryDictionaryKvPs, _log);
                    }
                    else
                    {
                        try
                        {
                            double leftDouble = 0;
                            double rightDouble = 0;

                            var cleanAbstractionNameLeft = entityAnalysisModelAbstractionCalculation
                                .EntityAnalysisModelAbstractionNameLeft.Replace(" ", "_");
                            var cleanAbstractionNameRight = entityAnalysisModelAbstractionCalculation
                                .EntityAnalysisModelAbstractionNameRight.Replace(" ", "_");

                            if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.ContainsKey(
                                cleanAbstractionNameLeft))
                            {
                                leftDouble = EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction[cleanAbstractionNameLeft];

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has extracted left value of {leftDouble}.");
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} but it does not contain a left value.");
                            }

                            if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.ContainsKey(
                                cleanAbstractionNameRight))
                            {
                                rightDouble = EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction[cleanAbstractionNameRight];

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and extracted right value of {rightDouble}.");
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} but it does not contain a right value.");
                            }

                            switch (entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId)
                            {
                                case 1:
                                    calculationDouble = leftDouble + rightDouble;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} addition, produces value of {calculationDouble}.");

                                    break;
                                case 2:
                                    calculationDouble = leftDouble - rightDouble;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} subtraction, produces value of {calculationDouble}.");

                                    break;
                                case 3:
                                    calculationDouble = leftDouble / rightDouble;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} divide, produces value of {calculationDouble}.");

                                    break;
                                case 4:
                                    calculationDouble = leftDouble * rightDouble;

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} multiply, produces value of {calculationDouble}.");

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            calculationDouble = 0;

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced an error in calculation and has been set to zero with exception message of {ex.Message}.");
                        }

                        if (double.IsNaN(calculationDouble) | double.IsInfinity(calculationDouble))
                        {
                            calculationDouble = 0;

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced IsNaN or IsInfinity and has been set to zero.");
                        }
                    }

                    EntityAnalysisModelInstanceEntryPayloadStore.AbstractionCalculation.Add(
                        entityAnalysisModelAbstractionCalculation.Name, calculationDouble);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to abstractions for processing.");

                    if (entityAnalysisModelAbstractionCalculation.ResponsePayload)
                    {
                        EntityInstanceEntryAbstractionCalculationResponse.Add(
                            entityAnalysisModelAbstractionCalculation.Name, calculationDouble);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to response payload also.");
                    }

                    if (entityAnalysisModelAbstractionCalculation.ReportTable && !Reprocess)
                    {
                        ReportDatabaseValues.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 6,
                            Key = entityAnalysisModelAbstractionCalculation.Name,
                            KeyValueFloat = calculationDouble,
                            EntityAnalysisModelInstanceEntryGuid =
                                EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                        });

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to report payload also with a column name of {entityAnalysisModelAbstractionCalculation.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced an error as {ex}.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Calculation", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Abstraction Calculations have concluded in {Stopwatch.ElapsedMilliseconds} ms.");
        }

        private void ExecuteAbstractionRulesWithoutSearchKeys()
        {
            //var reflectionAbstractionRule = new ReflectRule();
            foreach (var evaluateAbstractionRule in
                from evaluateAbstractionRuleLinq in EntityAnalysisModel.ModelAbstractionRules
                where !evaluateAbstractionRuleLinq.Search
                select evaluateAbstractionRuleLinq)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} is being processed as a basic rule.");

                double abstractionValue;

                if (ReflectRule.Execute(evaluateAbstractionRule, EntityAnalysisModel, CachePayloadDocumentStore,
                    EntityInstanceEntryTtlCountersResponse, EntityInstanceEntryDictionaryKvPs, _log))
                {
                    abstractionValue = 1;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} has returned true and set abstraction value to {abstractionValue}.");
                }
                else
                {
                    abstractionValue = 0;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} has returned false and set abstraction value to {abstractionValue}.");
                }

                EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.Add(evaluateAbstractionRule.Name,
                    abstractionValue);

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to processing.");

                if (evaluateAbstractionRule.ResponsePayload)
                {
                    EntityInstanceEntryAbstractionResponse.Add(evaluateAbstractionRule.Name, abstractionValue);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to response payload.");
                }

                if (evaluateAbstractionRule.ReportTable && !Reprocess)
                {
                    ReportDatabaseValues.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 5,
                        Key = evaluateAbstractionRule.Name,
                        KeyValueFloat = abstractionValue,
                        EntityAnalysisModelInstanceEntryGuid =
                            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                    });

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to report payload with a column name of {evaluateAbstractionRule.Name}.");
                }

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} finished basic abstraction rule {evaluateAbstractionRule.Id}.");
            }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Abstraction", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Abstraction has concluded in {Stopwatch.ElapsedMilliseconds} ms.");
        }

        private void ExecuteAbstractionRulesWithSearchKeys(CachePayloadRepository cachePayloadRepository)
        {
            var activeExecutionThreads = new List<Execute>();
            if (EntityAnalysisModel.EnableCache)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Entity cache storage is enabled so will now proceed to loop through the distinct grouping keys for this model.");

                foreach (var (key, value) in EntityAnalysisModel.DistinctSearchKeys)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating grouping key {key}.");

                    try
                    {
                        if (value.SearchKeyCache)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} grouping key {key} is a search key,  so the values will be fetched from the cache later on.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} checking if grouping key {key} exists in the current payload data.");

                            if (CachePayloadDocumentStore.ContainsKey(key))
                            {
                                var execute = new Execute
                                {
                                    EntityInstanceEntryDictionaryKvPs = EntityInstanceEntryDictionaryKvPs,
                                    AbstractionRuleGroupingKey = key,
                                    CachePayloadDocument = CachePayloadDocumentStore,
                                    EntityAnalysisModelInstanceEntryPayload = EntityAnalysisModelInstanceEntryPayloadStore,
                                    AbstractionRuleMatches = AbstractionRuleMatches,
                                    EntityAnalysisModel = EntityAnalysisModel,
                                    Log = _log,
                                    CachePayloadRepository = cachePayloadRepository
                                };
                                activeExecutionThreads.Add(execute);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has created a execute object to run all the abstraction rules rolling up to the grouping key.  It has been added to a collection to track it when multi threaded abstraction rules are enabled.");

                                switch (_jubeEnvironment.AppSettings("ForkAbstractionRuleSearchKeys"))
                                {
                                    case "True":
                                        ThreadPool.QueueUserWorkItem(ThreadPoolCallBackExecute, execute);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} execute object is set to fork, so it is being launched in its own thread pool thread.");

                                        break;
                                    default:
                                        ThreadPoolCallBackExecute(execute);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} execute object is set be serial, so it is being launched now in this thread.");

                                        break;
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} grouping key {key} does not exist in the current transaction data being processed.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} checking if grouping key {key} has created an error as {ex}.");
                    }
                }

                if (_jubeEnvironment.AppSettings("ForkAbstractionRuleSearchKeys").Equals("True",StringComparison.OrdinalIgnoreCase))
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} execute object is set to fork, looping through the register and waiting to merge all of these threads on their completion.");

                    var spin = new SpinWait();
                    do
                    {
                        var countFinished =
                            (from execute in activeExecutionThreads where execute.Finished select execute).Count();
                        if (countFinished == activeExecutionThreads.Count)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has merged all abstraction rule execution objects.");

                            break;
                        }
                        spin.SpinOnce();
                    } while (true);
                }

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now loop around all of the Abstraction rules for the purposes of performing the aggregations.");

                var cacheAbstractionRepository = new CacheAbstractionRepository(_jubeEnvironment.AppSettings("ConnectionString"),_log);

                foreach (var abstractionRule in EntityAnalysisModel.ModelAbstractionRules)
                    try
                    {
                        if (abstractionRule.Search)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating abstraction rule {abstractionRule.Id}.");

                            double abstractionValue;
                            if (EntityAnalysisModel.DistinctSearchKeys.FirstOrDefault(x =>
                                    x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache).Value != null)
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {abstractionRule.Id} has its values in the cache.");

                                abstractionValue = cacheAbstractionRepository.GetByNameSearchNameSearchValueReturnValueOnly(EntityAnalysisModel.Id,
                                    abstractionRule.Name,abstractionRule.SearchKey,
                                    CachePayloadDocumentStore[abstractionRule.SearchKey].AsString());
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} using documents in the entities collection of the cache.");

                                abstractionValue = EntityAnalysisModelAbstractionRuleAggregator.Aggregate(EntityAnalysisModelInstanceEntryPayloadStore,
                                    AbstractionRuleMatches, abstractionRule, _log);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} has finished and the value is {abstractionValue}.");
                            }

                            EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.Add(abstractionRule.Name,
                                abstractionValue);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {abstractionValue} to processing.");

                            if (abstractionRule.ResponsePayload)
                            {
                                EntityInstanceEntryAbstractionResponse.Add(abstractionRule.Name, abstractionValue);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {abstractionValue} to response payload.");
                            }

                            if (abstractionRule.ReportTable && !Reprocess)
                            {
                                ReportDatabaseValues.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 5,
                                    Key = abstractionRule.Name,
                                    KeyValueFloat = abstractionValue,
                                    EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                        .EntityAnalysisModelInstanceEntryGuid
                                });

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {abstractionValue} to report payload with a column name of {abstractionRule.Name}.");
                            }

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} finished aggregating abstraction rule {abstractionRule.Id}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} but has created an error as {ex}.");
                    }
            }
            else
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Entity cache storage is not enabled so it cannot fetch anything relating to Abstraction Rules.");
            }

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} all abstraction aggregation has finished, basic rules will now be processed.");
        }

        private void ExecuteTtlCounters()
        {
            try
            {
                if (EntityAnalysisModel.EnableTtlCounter)
                {
                    var cacheTtlCounterRepository = new CacheTtlCounterRepository(
                        _jubeEnvironment.AppSettings(
                            new []{"CacheConnectionString","ConnectionString"}),_log);

                    var cacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(
                        _jubeEnvironment.AppSettings(
                            new []{"CacheConnectionString","ConnectionString"}),_log);

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter cache storage is enabled so it will now proceed to return the TTL Counters via MongoDB aggregate query.");

                    if (EntityAnalysisModel.ModelTtlCounters.FindAll(x => x.OnlineAggregation).Count > 0)
                    {
                        var getByNameDataNameDataValueParams =
                            new List<CacheTtlCounterEntryRepository.GetByNameDataNameDataValueParams>();
                        foreach (var ttlCounter in EntityAnalysisModel.ModelTtlCounters)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} is online aggregation.");

                            if (CachePayloadDocumentStore.ContainsKey(ttlCounter.TtlCounterDataName))
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} which has an interval type of {ttlCounter.TtlCounterInterval} and interval value of {ttlCounter.TtlCounterValue}.");

                                var getByNameDataNameDataValueParam =
                                    new CacheTtlCounterEntryRepository.GetByNameDataNameDataValueParams();

                                var adjustedTtlCounterDate = ttlCounter.TtlCounterInterval switch
                                {
                                    "d" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddDays(
                                        ttlCounter.TtlCounterValue * -1),
                                    "h" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddHours(
                                        ttlCounter.TtlCounterValue * -1),
                                    "n" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddMinutes(
                                        ttlCounter.TtlCounterValue * -1),
                                    "s" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddSeconds(
                                        ttlCounter.TtlCounterValue * -1),
                                    "m" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddMonths(
                                        ttlCounter.TtlCounterValue * -1),
                                    "y" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddYears(
                                        ttlCounter.TtlCounterValue * -1),
                                    _ => default
                                };
                                getByNameDataNameDataValueParam.ReferenceDateFrom = adjustedTtlCounterDate;
                                getByNameDataNameDataValueParam.ReferenceDateTo =
                                    EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate;
                                getByNameDataNameDataValueParam.DataName = ttlCounter.TtlCounterDataName;
                                getByNameDataNameDataValueParam.DataValue =
                                    CachePayloadDocumentStore[ttlCounter.TtlCounterDataName].AsString();

                                getByNameDataNameDataValueParams.Add(getByNameDataNameDataValueParam);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} the date threshold from is {adjustedTtlCounterDate} to {EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate}, the TTL Counter Name is {ttlCounter.Name}, the TTL Counter Data Name is {ttlCounter.TtlCounterDataName} and the TTL Counter Data Name Value is {CachePayloadDocumentStore[ttlCounter.TtlCounterDataName]}.");
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} was unable to fine a value for TTL Counter Data Name {ttlCounter.TtlCounterDataName} and TTL Counter Name {ttlCounter.Name}.");
                            }
                        }

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finalized the predicate.");

                        var counts =
                            cacheTtlCounterEntryRepository.GetByNameDataNameDataValue(EntityAnalysisModel.Id,
                                getByNameDataNameDataValueParams
                                    .ToArray());

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has executed the group by query for the TTL Counters from the cache.  A loop of all TTL Counter results will now be performed to retrieve the for processing.");

                        foreach (var (key, value) in counts)
                            try
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key}.");

                                var modelTtlCounter = EntityAnalysisModel.ModelTtlCounters.Find(x =>
                                    x.Name == key);
                                if (modelTtlCounter != null)
                                {
                                    EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.Add(
                                        modelTtlCounter.Name, value);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key} has been located and the value {value} has been added to processing with the name of {modelTtlCounter.Name}.");

                                    if (modelTtlCounter.ResponsePayload)
                                    {
                                        EntityInstanceEntryTtlCountersResponse.Add(modelTtlCounter.Name,
                                            value);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key} has been located and the value {value} has been added to the response payload with the name of {modelTtlCounter.Name}.");
                                    }

                                    if (modelTtlCounter.ReportTable && !Reprocess)
                                    {
                                        ReportDatabaseValues.Add(new ArchiveKey
                                        {
                                            ProcessingTypeId = 5,
                                            Key = modelTtlCounter.Name,
                                            KeyValueInteger = value,
                                            EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                        });

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key} has been located and the value {value} has been added to the report payload with the name of {modelTtlCounter.Name}.");
                                    }
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key} but it could not be located,  returning nothing.");
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Error(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} matching TTL Counter  {key} has created an error as {ex}");
                            }

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will loop through the TTL counter values to make sure that there is at least a zero present for missing values.");

                        foreach (var ttlCounter in EntityAnalysisModel.ModelTtlCounters)
                            try
                            {
                                if (!EntityInstanceEntryTtlCountersResponse.ContainsKey(ttlCounter.Name))
                                {
                                    EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.Add(ttlCounter.Name,
                                        0);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  so will add this as name {ttlCounter.Name} with value of zero.");

                                    if (ttlCounter.ResponsePayload)
                                    {
                                        EntityInstanceEntryTtlCountersResponse.Add(ttlCounter.Name, 0);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of zero to the response payload also.");
                                    }

                                    if (ttlCounter.ReportTable && !Reprocess)
                                    {
                                        ReportDatabaseValues.Add(new ArchiveKey
                                        {
                                            ProcessingTypeId = 5,
                                            Key = ttlCounter.Name,
                                            KeyValueInteger = 0,
                                            EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                        });

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of zero to the report payload also.");
                                    }
                                }
                                else
                                {
                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} exists already,  so nothing more added.");
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Error(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} has created an error as {ex}.");
                            }
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Does not have any online TTL Counters.");
                    }

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now look for TTL Counters from the cache.");

                    foreach (var ttlCounter in EntityAnalysisModel.ModelTtlCounters.FindAll(x => x.OnlineAggregation == false))
                    {
                        try
                        {
                            var ttlCounterValue = cacheTtlCounterRepository.GetByNameDataNameDataValue(
                                EntityAnalysisModel.Id,
                                ttlCounter.Id,
                                ttlCounter.TtlCounterDataName,
                                CachePayloadDocumentStore[ttlCounter.TtlCounterDataName].AsString());

                            if (!EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.ContainsKey(
                                ttlCounter.Name))
                            {
                                EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.Add(ttlCounter.Name,
                                    ttlCounterValue);

                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  so will add this as name {ttlCounter.Name} with value of {ttlCounterValue}.");

                                if (ttlCounter.ResponsePayload)
                                {
                                    EntityInstanceEntryTtlCountersResponse.Add(ttlCounter.Name, ttlCounterValue);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of {ttlCounterValue} to the response payload also.");
                                }

                                if (ttlCounter.ReportTable && !Reprocess)
                                {
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 5,
                                        Key = ttlCounter.Name,
                                        KeyValueInteger = ttlCounterValue,
                                        EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                            .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of {ttlCounterValue} to the report payload also.");
                                }
                            }
                            else
                            {
                                _log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  so will add this as name {ttlCounter.Name} with value of {ttlCounterValue}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} has thrown an error as {ex}.");
                        }
                    }

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counters have concluded in{Stopwatch.ElapsedMilliseconds} ms.");
                }
                else
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter cache storage is not enabled so it cannot fetch TTL Counter Aggregation.");
                }

                EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("TTLC", (int) Stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has caused an error in TTL Counters as {ex}.");
            }
        }

        private void ExecuteDictionaryKvPs()
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Will now look for KVP Dictionary Values.");

            foreach (var (i, kvpDictionary) in EntityAnalysisModel.KvpDictionaries)
                try
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i}.");

                    double value;
                    if (CachePayloadDocumentStore.ContainsKey(kvpDictionary.DataName))
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload.");

                        var key = (string) CachePayloadDocumentStore[kvpDictionary.DataName];

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, which will be used for the lookup.");

                        if (kvpDictionary.KvPs.ContainsKey(key))
                        {
                            value = kvpDictionary.KvPs[key];

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, found a lookup value.  The dictionary value has been set to {value}.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, does not contain a lookup value.  The dictionary value has been set to zero.");

                            value = 0;
                        }
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} but the payload does not have such a value,  so have set the value to zero.");

                        value = 0;
                    }

                    if (!EntityInstanceEntryDictionaryKvPs.ContainsKey(kvpDictionary.Name))
                    {
                        EntityInstanceEntryDictionaryKvPs.Add(kvpDictionary.Name, value);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} for processing.");

                        if (kvpDictionary.ResponsePayload)
                        {
                            EntityInstanceEntryDictionaryKvPsResponse.Add(kvpDictionary.Name, value);

                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} to response payload.");
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has not added the name of {kvpDictionary.Name} and value of {value} to response payload.");
                        }
                    }
                    else
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has already added the name of {kvpDictionary.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has caused an error in Dictionary KVP as {ex}.");
                }

            EntityAnalysisModelInstanceEntryPayloadStore.Dictionary = EntityInstanceEntryDictionaryKvPs;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("KVP", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finished looking for Dictionary KVP values.");
        }

        private void ExecuteInlineFunctions()
        {
            try
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to check for inline functions.");

                foreach (var inlineFunction in EntityAnalysisModel.EntityAnalysisModelInlineFunctions)
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke inline function {inlineFunction.Id}.");

                    try
                    {
                        var output = ReflectRule.Execute(inlineFunction, EntityAnalysisModel, EntityAnalysisModelInstanceEntryPayloadStore,
                            EntityInstanceEntryDictionaryKvPs, _log);

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} and returned a value of {output}.");

                        if (!CachePayloadDocumentStore.ContainsKey(inlineFunction.Name))
                            switch (inlineFunction.ReturnDataTypeId)
                            {
                                case 1:
                                    CachePayloadDocumentStore.Add(inlineFunction.Name, output);

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as string.");

                                    break;
                                case 2:
                                    CachePayloadDocumentStore.Add(inlineFunction.Name, Convert.ToInt32(output));

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as integer.");

                                    break;
                                case 3:
                                    CachePayloadDocumentStore.Add(inlineFunction.Name, Convert.ToDouble(output));

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as double.");

                                    break;
                                case 4:
                                    CachePayloadDocumentStore.Add(inlineFunction.Name, Convert.ToDateTime(output));

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as date.");

                                    break;
                                case 5:
                                    CachePayloadDocumentStore.Add(inlineFunction.Name, Convert.ToBoolean(output));

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as boolean.");

                                    break;
                            }
                        else
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has not added to payload as name {inlineFunction.Name} already exists.");


                        if (inlineFunction.ResponsePayload)
                        {
                            if (!CachePayloadDocumentResponse.ContainsKey(inlineFunction.Name))
                                switch (inlineFunction.ReturnDataTypeId)
                                {
                                    case 1:
                                        CachePayloadDocumentResponse.Add(inlineFunction.Name, output);

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as string.");

                                        break;
                                    case 2:
                                        CachePayloadDocumentResponse.Add(inlineFunction.Name, Convert.ToInt32(output));

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as integer.");

                                        break;
                                    case 3:
                                        CachePayloadDocumentResponse.Add(inlineFunction.Name, Convert.ToDouble(output));

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as double.");

                                        break;
                                    case 4:
                                        CachePayloadDocumentResponse.Add(inlineFunction.Name,
                                            Convert.ToDateTime(output));

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as date.");

                                        break;
                                    case 5:
                                        CachePayloadDocumentResponse.Add(inlineFunction.Name, Convert.ToBoolean(output));

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as boolean.");

                                        break;
                                }
                        }
                        else
                        {
                            _log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has not added to response payload as name {inlineFunction.Name} already exists.");
                        }

                        if (inlineFunction.ReportTable && !Reprocess)
                        {
                            switch (inlineFunction.ReturnDataTypeId)
                            {
                                case 1:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueString = output == null ? null : Convert.ToString(output),
                                        EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                            .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as string.");

                                    break;
                                case 2:
                                    if (output != null)
                                    {
                                        ReportDatabaseValues.Add(new ArchiveKey
                                        {
                                            ProcessingTypeId = 3,
                                            Key = inlineFunction.Name,
                                            KeyValueInteger = (int) output,
                                            EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                        });

                                        _log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as integer.");
                                    }

                                    break;
                                case 3:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueFloat = Convert.ToDouble(output),
                                        EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                            .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as double.");

                                    break;
                                case 4:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueDate = Convert.ToDateTime(output),
                                        EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                            .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as date.");

                                    break;
                                case 5:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueBoolean = Convert.ToByte(output),
                                        EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                                            .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    _log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as boolean.");

                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} has created an error as {ex}.");
                    }
                }

                EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Functions", (int) Stopwatch.ElapsedMilliseconds);

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has passed inline functions.");
            }
            catch (Exception ex)
            {
                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has experienced an error invoking inline functions as {ex}.");
            }
        }

        private void ExecuteGatewayRules(ref double maxGatewayResponseElevation, ref bool matchedGateway)
        {
            var gatewaySample = Seeded.NextDouble();
            Seeded.NextDouble();

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke Gateway Rules with a gateway sample of {gatewaySample}.");

            foreach (var entityModelGatewayRule in EntityAnalysisModel.ModelGatewayRules)
                try
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke Gateway Rule {entityModelGatewayRule.EntityAnalysisModelGatewayRuleId} with a gateway sample of {gatewaySample}.  The models Gateway Sample is {entityModelGatewayRule.GatewaySample} to be tested against {gatewaySample} .");

                    if (entityModelGatewayRule.GatewayRuleCompileDelegate(CachePayloadDocumentStore,
                            EntityAnalysisModel.EntityAnalysisModelLists, EntityInstanceEntryDictionaryKvPs, _log) &&
                        gatewaySample < entityModelGatewayRule.GatewaySample)
                    {
                        matchedGateway = true;
                        maxGatewayResponseElevation = entityModelGatewayRule.MaxResponseElevation;
                        EntityAnalysisModel.ModelInvokeGatewayCounter += 1;
                        entityModelGatewayRule.Counter += 1;

                        _log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke Gateway Rule {entityModelGatewayRule.EntityAnalysisModelGatewayRuleId} as it has matched. The max response elevation has been set to {maxGatewayResponseElevation} and Model Invoke Gateway Counter has been set to {EntityAnalysisModel.ModelInvokeGatewayCounter}. The Entity Model Gateway Rule Counter has been set to {entityModelGatewayRule.Counter}.");

                        break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has tried to invoke Gateway Rule {entityModelGatewayRule.EntityAnalysisModelGatewayRuleId} but it has caused an error as {ex}.");
                }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Gateway", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Gateway Rules have concluded {Stopwatch.ElapsedMilliseconds} ms and has returned {matchedGateway} to continue processing.");
        }

        private void ExecuteInlineScripts()
        {
            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model{EntityAnalysisModel.Id}.  Model Invocation Counter is now {EntityAnalysisModel.ModelInvokeCounter}.");

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to search for inline scripts to be invoked.");

            foreach (var inlineScript in EntityAnalysisModel.EntityAnalysisModelInlineScripts)
                try
                {
                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke {inlineScript.InlineScriptCode}.");

                    var tempVarCachePayloadDocumentStore = CachePayloadDocumentStore;
                    var tempVarCachePayloadDocumentResponse = CachePayloadDocumentResponse;
                    var tempVarReportDatabaseValues = ReportDatabaseValues;
                    ReflectInlineScript.Execute(inlineScript, ref tempVarCachePayloadDocumentStore,
                        ref tempVarCachePayloadDocumentResponse, _log);
                    ReportDatabaseValues = tempVarReportDatabaseValues;
                    CachePayloadDocumentResponse = tempVarCachePayloadDocumentResponse;
                    CachePayloadDocumentStore = tempVarCachePayloadDocumentStore;

                    _log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has invoked{inlineScript.InlineScriptCode}.");
                }
                catch (Exception ex)
                {
                    _log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has tried to invoke inline script {inlineScript.InlineScriptCode} but it has produced an error as {ex}.");
                }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseTime.Add("Scripts", (int) Stopwatch.ElapsedMilliseconds);

            _log.Info(
                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} inline script invocation has concluded {Stopwatch.ElapsedMilliseconds} ms.");
        }

        private void ThreadPoolCallBackExecute(object threadContext)
        {
            try
            {
                var execute = (Execute) threadContext;

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Call Back Triggered for Execute on grouping key {execute.AbstractionRuleGroupingKey}.");

                execute.Start();

                _log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Call Back Completed for Execute on grouping key {execute.AbstractionRuleGroupingKey}.");
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Execute model Call Back has created an error as {ex}.");
            }
        }
    }
}
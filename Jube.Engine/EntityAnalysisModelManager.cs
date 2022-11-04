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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using Accord.Neuro;
using Jube.Data.Cache;
using Jube.Data.Context;
using Jube.Data.Extension;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Engine.Helpers;
using Jube.Engine.Invoke;
using Jube.Engine.Model;
using Jube.Engine.Model.Archive;
using Jube.Engine.Model.Compiler;
using Jube.Engine.Model.Processing;
using Jube.Engine.Model.Processing.Payload;
using Jube.Engine.Sanctions;
using Jube.Parser;
using log4net;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using EntityAnalysisModel = Jube.Engine.Model.EntityAnalysisModel;
using EntityAnalysisModelAbstractionCalculation = Jube.Engine.Model.EntityAnalysisModelAbstractionCalculation;
using EntityAnalysisModelAbstractionRule = Jube.Engine.Model.EntityAnalysisModelAbstractionRule;
using EntityAnalysisModelActivationRule = Jube.Engine.Model.EntityAnalysisModelActivationRule;
using EntityAnalysisModelDictionary = Jube.Engine.Model.EntityAnalysisModelDictionary;
using EntityAnalysisModelHttpAdaptation = Jube.Engine.Model.EntityAnalysisModelHttpAdaptation;
using EntityAnalysisModelInlineFunction = Jube.Engine.Model.EntityAnalysisModelInlineFunction;
using EntityAnalysisModelInlineScript = Jube.Engine.Model.EntityAnalysisModelInlineScript;
using EntityAnalysisModelSanction = Jube.Engine.Model.EntityAnalysisModelSanction;
using EntityAnalysisModelTag = Jube.Engine.Model.EntityAnalysisModelTag;
using EntityAnalysisModelTtlCounter = Jube.Engine.Model.EntityAnalysisModelTtlCounter;
using ExhaustiveSearchInstance = Jube.Engine.Model.Exhaustive.ExhaustiveSearchInstance;
using ExhaustiveSearchInstancePromotedTrialInstanceVariable =
    Jube.Engine.Model.Exhaustive.ExhaustiveSearchInstancePromotedTrialInstanceVariable;

namespace Jube.Engine
{
    public class EntityAnalysisModelManager
    {
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Thread> _activationWatcherThreads = new();

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Thread> _archiverThreads = new();
        private readonly List<ArchiverThreadStarter> _archiverThreadStarters = new();
        private readonly List<EntityAnalysisModelInlineScript> _inlineScripts = new();

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Thread> _reprocessingThreads = new();
        private Thread _cacheThread;
        private Guid _entityAnalysisInstanceGuid;
        private Thread _modelSyncThread;
        private bool _stopping;
        private Thread _tTtlCThread;
        public ILog Log { get; set; }
        public ConcurrentQueue<Tag> PendingTagging { get; set; } = new();
        public Dictionary<int, EntityAnalysisModel> ActiveEntityAnalysisModels { get; } = new();
        private ConcurrentQueue<ActivationWatcher> PersistToActivationWatcherAsync { get; } = new();
        public Dictionary<string, Assembly> HashCacheAssembly { get; set; }
        public Dictionary<int, SanctionEntryDto> SanctionsEntries { get; set; } = new();
        public Random Seeded { get; set; }
        public DynamicEnvironment.DynamicEnvironment JubeEnvironment { get; set; }
        public IModel RabbitMqChannel { get; set; }
        public ConcurrentQueue<Notification> PendingNotification { get; set; }
        public bool EntityModelsHasLoadedForStartup { get; set; }
        public ConcurrentDictionary<Guid,Callback> PendingCallbacks { get; set; }
        public DefaultContractResolver ContractResolver;
        
        public void StopMe()
        {
            _stopping = true;

            foreach (var archiverThreadStarter in _archiverThreadStarters) archiverThreadStarter.StopMe();
        }

        public void Start()
        {
            StartCallbackListener();
            LogStartInstance();
            StartModelSync();
            StartArchiver();
            StartActivationWatcherArchive();
            StartSearchKeyCacheServer();
            StartTtlCounterServer();
            StartReprocessing();
        }
        
        private void StartCallbackListener()
        {
            if (JubeEnvironment.AppSettings("EnableCallback").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                PendingCallbacks = new ConcurrentDictionary<Guid, Callback>();
                
                var cacheCallbackRepository = new CacheCallbackRepository(JubeEnvironment.AppSettings(
                    new[] {"CacheConnectionString", "ConnectionString"}), Log,PendingCallbacks);
                    
                var startCallbackThread = new ThreadStart(cacheCallbackRepository.ListenForCallbacks);
                var callbackThread = new Thread(startCallbackThread)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Normal
                };
                callbackThread.Start();
            }
        }

        private void StartReprocessing()
        {
            if (JubeEnvironment.AppSettings("EnableReprocessing").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                int i;
                var tempVar = int.Parse(JubeEnvironment.AppSettings("ReprocessingThreads"));
                for (i = 1; i <= tempVar; i++)
                {
                    Log.Debug($"Entity Start: Starting Reprocessing routine for thread {i}.");

                    ThreadStart startReprocessingThread = EntityReprocessing;
                    var reprocessingThread = new Thread(startReprocessingThread)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Normal
                    };
                    reprocessingThread.Start();
                    _reprocessingThreads.Add(reprocessingThread);

                    Log.Debug($"Entity Start: Started Reprocessing in start routine for thread {i}.");
                }
            }
            else
            {
                Log.Debug("Entity Start: Has not started reprocessing as it is disabled.");
            }
        }

        private Parser.Parser ConfigureTokenParserForSecurity(DbContext dbContext)
        {
            Log.Info("Entity Start: Starting soft code parser.");

            var repository = new RuleScriptTokenRepository(dbContext);
            var tokens = repository.Get().Select(s => s.Token).ToList();

            Log.Info($"Entity Start: Has fetched {tokens.Count} tokens.  Will construct and return the parser.");

            return new Parser.Parser(Log, tokens);
        }

        private void StartTtlCounterServer()
        {
            Log.Debug(
                $"Entity Start: TTL Counter Administration is set to be {JubeEnvironment.AppSettings("EnableTtlCounter")} on this node.");

            if (JubeEnvironment.AppSettings("EnableTtlCounter").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug($"Entity Start: Starting the TTL Counter Administration with a polling rate of {JubeEnvironment.AppSettings("WaitTtlCounterDecrement")}.");
                
                ThreadStart tsTtlC = TtlCounterAdministration;
                _tTtlCThread = new Thread(tsTtlC)
                {
                    IsBackground = false,
                    Priority = ThreadPriority.Normal
                };

                _tTtlCThread.Start();

                Log.Debug("Entity Start: TTL Counter Administration.");
            }
        }

        private void StartSearchKeyCacheServer()
        {
            Log.Debug(
                $"Entity Start: The Rule Cache Server is set to be {JubeEnvironment.AppSettings("EnableSearchKeyCache")} on this node.");

            if (JubeEnvironment.AppSettings("EnableSearchKeyCache").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Entity Start: Starting the Rule Cache Engine.");

                ThreadStart tsCache = AbstractionRuleCaching;
                _cacheThread = new Thread(tsCache)
                {
                    IsBackground = false,
                    Priority = ThreadPriority.Normal
                };

                _cacheThread.Start();

                Log.Debug("Entity Start: Started the Rule Cache Engine.");
            }
        }

        private void StartActivationWatcherArchive()
        {
            if (JubeEnvironment.AppSettings("ActivationWatcherAllowPersist").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                int i;
                var tempVar = int.Parse(JubeEnvironment.AppSettings("ActivationWatcherPersistThreads"));
                for (i = 1; i <= tempVar; i++)
                {
                    ThreadStart tsActivationWatcher = PersistToActivationWatcher;
                    var activationWatcherThread = new Thread(tsActivationWatcher)
                    {
                        IsBackground = false,
                        Priority = ThreadPriority.Normal
                    };
                    activationWatcherThread.Start();
                    _activationWatcherThreads.Add(activationWatcherThread);

                    Log.Debug(string.Format("Entity Start: Started Activation Watcher Persist Thread " + i + "."));
                }
            }
        }

        private void StartArchiver()
        {
            int i;

            Log.Debug(
                $"Entity Start: There are {JubeEnvironment.AppSettings("ArchiverPersistThreads")} SQL Persist threads about to start.");

            var tempVar = int.Parse(JubeEnvironment.AppSettings("ArchiverPersistThreads"));
            for (i = 1; i <= tempVar; i++)
            {
                var persistToDatabaseAcrossAllModels = new ArchiverThreadStarter
                {
                    Log = Log,
                    ThreadSequence = i,
                    ActiveModels = ActiveEntityAnalysisModels
                };

                ThreadStart tsSql = persistToDatabaseAcrossAllModels.Start;
                var archiverThread = new Thread(tsSql)
                {
                    IsBackground = false,
                    Priority = ThreadPriority.Normal
                };
                archiverThread.Start();
                _archiverThreads.Add(archiverThread);
                _archiverThreadStarters.Add(persistToDatabaseAcrossAllModels);

                Log.Debug($"Entity Start: Started Database Persist Thread {i}.");
            }
        }

        private void StartModelSync()
        {
            Log.Debug("Entity Start: Starting Model Sync.");

            ThreadStart startModelSyncThread = ModelSync;
            _modelSyncThread = new Thread(startModelSyncThread)
            {
                IsBackground = false,
                Priority = ThreadPriority.Normal
            };
            _modelSyncThread.Start();

            Log.Debug("Entity Start: Started Model Sync in start routine.");
        }

        private void LogStartInstance()
        {
            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));

            try
            {
                Log.Debug("Entity Start: Trying to establish a connection to the Database database.");

                var repository = new EntityAnalysisInstanceRepository(dbContext);

                Log.Debug("Entity Start: Established a connection to the Database database.");

                _entityAnalysisInstanceGuid = Guid.NewGuid();

                Log.Debug(
                    $"Entity Start: A GUID for this instance has been created and is {_entityAnalysisInstanceGuid}.");
                
                var model = new EntityAnalysisInstance
                {
                    Guid = _entityAnalysisInstanceGuid,
                    Instance = Dns.GetHostName(),
                    CreatedDate = DateTime.Now
                };

                Log.Debug(
                    $"Entity Start: Passing values to record the entity instance starting Entity_Analysis_Instance_GUID {_entityAnalysisInstanceGuid}; Node {model.Instance};.");

                repository.Insert(model);

                Log.Debug(
                    $"Entity Start: Recorded the entity instance starting in the Database database with a GUID of {_entityAnalysisInstanceGuid}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Entity Start: {ex}");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                Log.Debug("Entity Start: Closed the Database Connection Finally.");
            }
        }

        private void ModelSync()
        {
            var startupTenantRegistrySchedule = true;
            try
            {
                while (!_stopping)
                    try
                    {
                        var codeBase = Assembly.GetExecutingAssembly().Location;

                        Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");

                        var strPathBinary = Path.GetDirectoryName(codeBase);
                        var strPathFramework = Path.GetDirectoryName(typeof(object).Assembly.Location);

                        Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");

                        Log.Debug("Entity Model Sync: Making a connection to the Database database.");

                        var dbContext =
                            DataConnectionDbContext.GetDbContextDataConnection(
                                JubeEnvironment.AppSettings("ConnectionString"));

                        Log.Debug("Entity Model Sync: Connected to the Database database.");
                        try
                        {
                            var tenantRegistrySchedules = GetTenantRegistrySchedule(dbContext);

                            CompileInlineScripts(strPathBinary, strPathFramework, dbContext);

                            CreateDataTableBuffersIfNotExist();

                            foreach (var tenantRegistrySchedule in tenantRegistrySchedules)
                            {
                                if (tenantRegistrySchedule.SynchronisationPending || startupTenantRegistrySchedule)
                                {
                                    if (startupTenantRegistrySchedule) startupTenantRegistrySchedule = false;

                                    var parser = ConfigureTokenParserForSecurity(dbContext);
                                    SyncEntityAnalysisModels(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                    SyncEntityAnalysisModelRequestXPath(dbContext);
                                    SyncEntityAnalysisModelGatewayRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelSanctions(dbContext);
                                    SyncEntityAnalysisModelAbstractionRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelActivationRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelInlineFunctions(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelAbstractionCalculations(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelTtlCounters(dbContext);
                                    SyncEntityAnalysisModelInlineScripts(dbContext);
                                    SyncEntityAnalysisModelHttpAdaptation(dbContext);
                                    SyncEntityAnalysisModelTags(dbContext);
                                    ConfirmSync(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                }
                                else
                                {
                                    HeartbeatThisModel(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                }

                                SyncExhaustiveSearchInstances(dbContext);
                                StoreRuleCounterValues(dbContext);
                                SyncEntityAnalysisModelLists(dbContext);
                                SyncEntityAnalysisModelDictionaries(dbContext);
                                SyncSuppression(dbContext);
                                SyncActivationRuleSuppression(dbContext);
                                StartupModel(dbContext);
                                EntityModelsHasLoadedForStartup = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Entity Start: Has produced an error {ex}");
                        }
                        finally
                        {
                            dbContext.Close();
                            dbContext.Dispose();

                            Log.Debug("Entity Start: Closing the database connection.");

                            Thread.Sleep(int.Parse(JubeEnvironment.AppSettings("ModelSynchronisationWait")));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Entity Start: Error in licence and file path check as {ex}");
                    }
            }
            catch (Exception ex)
            {
                Log.Error($"Fatal Failure of Model Sync.{ex}");
            }
        }

        private void ConfirmSync(DbContext dbContext, int tenantRegistryId)
        {
            try
            {
                var repository = new EntityAnalysisModelSyncronisationNodeStatusEntryRepository(dbContext);

                var upsert = new EntityAnalysisModelSynchronisationNodeStatusEntry
                {
                    Instance = Dns.GetHostName(),
                    TenantRegistryId = tenantRegistryId
                };

                repository.UpsertSynchronisation(upsert);
            }
            catch (Exception ex)
            {
                Log.Error($"Entity Start: Error in model sync as {ex}");
            }
        }

        private void StoreRuleCounterValues(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose Synchronisation of the model counters.  Will now start with the Gateway Rule Counters.");

                foreach (var gatewayRule in value.ModelGatewayRules)
                    if (gatewayRule.Counter > 0)
                    {
                        Log.Debug(
                            $"Entity Start: Checking if model {key} is about to update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");

                        UpdateGatewayRuleCounter(dbContext, gatewayRule);

                        Log.Debug(
                            $"Entity Start: Checking if model {key} has finished processing updating gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");
                    }
                    else
                    {
                        Log.Debug(
                            $"Entity Start: Checking if model {key} will not update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} as counter is 0.");
                    }

                foreach (var activationRule in value.ModelActivationRules)
                    if (activationRule.Counter > 0)
                    {
                        Log.Debug(
                            $"Entity Start: Checking if model {key} is about to update activation rule id {activationRule.Id} and counter of {activationRule.Counter}.");

                        UpdateActivationRuleCounter(dbContext, activationRule);

                        Log.Debug(
                            $"Entity Start: Checking if model {key} has finished processing updating activation rule id {activationRule.Id} and counter of {activationRule.Counter}.");
                    }
                    else
                    {
                        Log.Debug(
                            $"Entity Start: Checking if model {key} will not update activation rule id {activationRule.Id} as counter is 0.");
                    }

                Log.Debug(
                    $"Entity Start: Checking if model {key} is finished Synchronisation of the model counters.");
            }
        }

        private void StartupModel(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    "Entity Start: About to perform index synchronisation with model.");

                value.MountCollectionsAndSyncCacheDbIndex();

                Log.Debug(
                    "Entity Start: Has finished index synchronisation with model.");

                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose of starting the thread.");

                if (!ActiveEntityAnalysisModels[key].Started)
                {
                    Log.Debug($"Entity Start: Checking if model {key} is not started.");

                    value.EntityAnalysisModelInstanceGuid = Guid.NewGuid();

                    Log.Debug($"Entity Start: Checking if model {key} has now been started.");

                    var repository = new EntityAnalysisModelInstanceRepository(dbContext);

                    var model = new EntityAnalysisModelInstance
                    {
                        CreatedDate = DateTime.Now,
                        EntityAnalysisInstanceGuid = ActiveEntityAnalysisModels[key].EntityAnalysisInstanceGuid,
                        EntityAnalysisModelInstanceGuid = value.EntityAnalysisModelInstanceGuid,
                        EntityAnalysisModelId = ActiveEntityAnalysisModels[key].Id
                    };

                    Log.Debug(
                        $"Entity Start: Recording that model {key} with Created Date {model.CreatedDate}, Entity_Analysis_Model_GUID {ActiveEntityAnalysisModels[key].Guid}, Entity_Analysis_Model_Instance_GUID {ActiveEntityAnalysisModels[key].EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");

                    repository.Insert(model);

                    Log.Debug(
                        $"Entity Start: Started model {key} with Started_Date {model.CreatedDate}, Entity_Analysis_Model_GUID {ActiveEntityAnalysisModels[key].Guid}, Entity_Analysis_Model_Instance_GUID {ActiveEntityAnalysisModels[key].EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");

                    value.Started = true;
                    Log.Info($"Entity Start: has started {value.Id}.");
                }
            }
        }

        private void CreateDataTableBuffersIfNotExist()
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
                try
                {
                    int i;
                    var tempVar = int.Parse(JubeEnvironment.AppSettings("ArchiverPersistThreads"));
                    for (i = 1; i <= tempVar; i++)
                        if (!value.BulkInsertMessageBuffers.ContainsKey(i))
                            value.BulkInsertMessageBuffers.Add(i, new ArchiveBuffer());
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Create table process has created an error as {ex} for model {key}.");
                }
        }

        private void SyncActivationRuleSuppression(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Suppression Activation Rules.");

                var repository = new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelActivationRuleSuppressionRepository.GetByEntityAnalysisModelId for and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelSuppressionDictionary =
                    new Dictionary<string, Dictionary<string, List<string>>>();
                foreach (var record in records)
                    try
                    {
                        Log.Info(
                            $"Entity Start: Suppression ID {record.Id} returned for model {key}.");

                        if (record.SuppressionKey != null)
                        {
                            var suppressionsDictionary = new Dictionary<string, List<string>>();
                            if (record.SuppressionKeyValue != null)
                            {
                                Log.Info(
                                    $"Entity Start: Checking to see if there is a shadow collection for suppression value {record.Id} for Entity Model ID {key}.");

                                if (!suppressionsDictionary.ContainsKey(record.SuppressionKeyValue ?? string.Empty))
                                {
                                    Log.Info(
                                        $"Entity Start: No shadow collection for suppression value {record.Id} for Entity Model ID {key} so it is being created.");

                                    suppressionsDictionary.Add(record.SuppressionKeyValue ?? string.Empty,
                                        new List<string>());

                                    Log.Info(
                                        $"Entity Start: checking for collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");

                                    if (!suppressionsDictionary[
                                                record.SuppressionKeyValue ?? string.Empty]
                                            .Contains(record.EntityAnalysisModelActivationRuleName))
                                    {
                                        suppressionsDictionary[
                                                record.SuppressionKeyValue ?? string.Empty]
                                            .Add(record.EntityAnalysisModelActivationRuleName);

                                        Log.Info(
                                            $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                    }
                                    else
                                    {
                                        Log.Info(
                                            $"Entity Start: was not added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} as it already exists.");
                                    }
                                }
                                else
                                {
                                    if (!suppressionsDictionary[
                                                record.SuppressionKeyValue ?? string.Empty]
                                            .Contains(record.EntityAnalysisModelActivationRuleName))
                                    {
                                        suppressionsDictionary[
                                                record.SuppressionKeyValue ?? string.Empty]
                                            .Add(record.EntityAnalysisModelActivationRuleName);

                                        Log.Info(
                                            $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                    }
                                }

                                if (shadowEntityAnalysisModelSuppressionDictionary.ContainsKey(record.SuppressionKey))
                                {
                                    shadowEntityAnalysisModelSuppressionDictionary[record.SuppressionKey] =
                                        suppressionsDictionary;

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                                }
                                else
                                {
                                    shadowEntityAnalysisModelSuppressionDictionary.Add(record.SuppressionKey,
                                        suppressionsDictionary);

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                    }

                value.EntityAnalysisModelSuppressionRules = shadowEntityAnalysisModelSuppressionDictionary;

                Log.Debug(
                    $"Entity Start: Model {key} and suppression Model  {key} added to collection.");
            }
        }

        private void SyncSuppression(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose adding suppression.");

                var repository = new EntityAnalysisModelSuppressionRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelSuppressionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelSuppressionList = new Dictionary<string, List<string>>();
                foreach (var record in records)
                    try
                    {
                        Log.Info(
                            $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key}.");

                        var suppressionsDictionary = new List<string>();
                        if (record.SuppressionKeyValue != null)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Value as {record.SuppressionKeyValue} also checking to see if it is already added.");

                            if (!suppressionsDictionary.Contains(record.SuppressionKeyValue))
                            {
                                suppressionsDictionary.Add(record.SuppressionKeyValue);

                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression ID  {record.Id} set Value as {record.SuppressionKeyValue} has been added to a shadow list of suppression.");
                            }

                            if (shadowEntityAnalysisModelSuppressionList.ContainsKey(record.SuppressionKey))
                            {
                                shadowEntityAnalysisModelSuppressionList[record.SuppressionKey] =
                                    suppressionsDictionary;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                            }
                            else
                            {
                                {
                                    shadowEntityAnalysisModelSuppressionList.Add(record.SuppressionKey,
                                        suppressionsDictionary);

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and does not exist in collection,  created key.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                    }

                value.EntityAnalysisModelSuppressionModels = shadowEntityAnalysisModelSuppressionList;

                Log.Debug(
                    $"Entity Start: Model {key} and Activation Rule Suppression ID added to collection.");
            }
        }

        private void SyncEntityAnalysisModelDictionaries(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Dictionary.");

                var repositoryDictionary = new EntityAnalysisModelDictionaryRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelDictionaryRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var recordsDictionary = repositoryDictionary.GetByEntityAnalysisModelId(key);

                var shadowKvpDictionary = new Dictionary<int, EntityAnalysisModelDictionary>();
                foreach (var recordDictionary in recordsDictionary)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key}.");

                        if (recordDictionary.Active.Value == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is active.");

                            var kvpDictionary = new EntityAnalysisModelDictionary();

                            if (recordDictionary.DataName != null)
                            {
                                kvpDictionary.DataName = recordDictionary.DataName.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Data Name as {kvpDictionary.DataName}.");
                            }

                            if (recordDictionary.ResponsePayload.HasValue)
                            {
                                kvpDictionary.ResponsePayload = recordDictionary.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Response Payload as {kvpDictionary.ResponsePayload}.");
                            }
                            else
                            {
                                kvpDictionary.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} found empty and set Response Payload as {kvpDictionary.ResponsePayload}.");
                            }

                            if (recordDictionary.Name != null)
                            {
                                kvpDictionary.Name = recordDictionary.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Name as {kvpDictionary.Name}.");

                                shadowKvpDictionary.Add(recordDictionary.Id, kvpDictionary);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is in error with {ex}.");
                    }

                foreach (var (i, kvpDictionary) in shadowKvpDictionary)
                {
                    var repositoryDictionaryKvp = new EntityAnalysisModelDictionaryKvpRepository(dbContext);

                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelDictionaryKvpRepository.GetByEntityAnalysisModelDictionaryId for entity model key of {key} and Dictionary id {i}.");

                    var recordsDictionaryKvp = repositoryDictionaryKvp.GetByEntityAnalysisModelDictionaryId(i);

                    Log.Debug("Returned all Dictionary KVP from the database.");

                    foreach (var recordDictionaryKvp in recordsDictionaryKvp)
                        try
                        {
                            if (recordDictionaryKvp.KvpKey != null)
                            {
                                var kvpKey = recordDictionaryKvp.KvpKey;

                                Log.Debug(
                                    $"Entity Start: Dictionary KVP entity model key of {key} and Dictionary KVP id {recordDictionaryKvp.KvpKey} has found value of {kvpKey} .");

                                if (recordDictionaryKvp.KvpValue.HasValue)
                                {
                                    var kvpValue = recordDictionaryKvp.KvpValue.Value;

                                    if (!kvpDictionary.KvPs.ContainsKey(kvpKey))
                                        kvpDictionary.KvPs.Add(kvpKey, kvpValue);
                                    else
                                        Log.Debug(
                                            $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has already added the KVP value.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                                }
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(
                                $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} is in error with {ex}.");
                        }

                    Log.Debug(
                        $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {i} has added all Dictionary values.");
                }

                value.KvpDictionaries = shadowKvpDictionary;
            }
        }

        private void SyncEntityAnalysisModelLists(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Lists.");

                var repositoryList = new EntityAnalysisModelListRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelListRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var recordsList = repositoryList.GetByEntityAnalysisModelId(key);

                var listIdName = new Dictionary<int, string>();
                var shadowEntityAnalysisModelLists = new Dictionary<string, List<string>>();
                foreach (var recordList in recordsList)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: List ID ID {recordList.Id} returned for model {key}.");

                        if (recordList.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: List ID ID {recordList.Id} returned for model {key} is active.");

                            if (recordList.Name != null)
                            {
                                var name = recordList.Name.Replace(" ", "_");
                                listIdName.Add(recordList.Id, recordList.Name);

                                Log.Debug(
                                    $"Entity Start: Model  {key} and List {recordList.Id} set Name as {name}.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: List ID ID {recordList.Id} returned for model {key} is in error with {ex}.");
                    }


                foreach (var (i, s) in
                         from listIdNameKvp in listIdName
                         where !shadowEntityAnalysisModelLists.ContainsKey(listIdNameKvp.Value)
                         select listIdNameKvp)
                {
                    shadowEntityAnalysisModelLists.Add(s, new List<string>());

                    var repositoryListValues = new EntityAnalysisModelListValueRepository(dbContext);

                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelListValueRepository.GetByEntityAnalysisModelListId for entity model key of {key} and list id {i}.");

                    var recordsListValues = repositoryListValues.GetByEntityAnalysisModelListId(i);

                    Log.Debug("Returned all ListValues from the database.");

                    foreach (var recordListValues in recordsListValues)
                        try
                        {
                            if (recordListValues.ListValue != null)
                            {
                                shadowEntityAnalysisModelLists[s].Add(recordListValues.ListValue);

                                Log.Debug(
                                    $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found value of {recordListValues.ListValue} .");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found null value.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(
                                $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} is in error with {ex}.");
                        }

                    Log.Debug(
                        $"Entity Start: List Value entity model key of {key} and list id {i} has added all list values.");
                }

                value.EntityAnalysisModelLists = shadowEntityAnalysisModelLists;

                Log.Debug($"Model {key} and List ID shadow copy has been over written");
            }
        }

        private void SyncExhaustiveSearchInstances(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the Exhaustive Algorithm.");

                var repository = new ExhaustiveSearchInstanceRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing select from Exhaustive Search Instance Repository and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelExhaustive = new List<ExhaustiveSearchInstance>();
                foreach (var record in records.ToList())
                    try
                    {
                        Log.Debug($"Entity Start: Exhaustive ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            var exhaustive = new ExhaustiveSearchInstance
                            {
                                Id = record.Id,
                                Guid = record.Guid
                            };

                            if (record.Name == null)
                            {
                                exhaustive.Name =
                                    $"Exhaustive_{exhaustive.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Exhaustive_{exhaustive.Id} set DEFAULT Name as {exhaustive.Name}.");
                            }
                            else
                            {
                                exhaustive.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.Name}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                exhaustive.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ResponsePayload}.");
                            }
                            else
                            {
                                exhaustive.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                exhaustive.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ReportTable}.");
                            }
                            else
                            {
                                exhaustive.ReportTable = record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ReportTable}.");
                            }

                            var getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                = new GetExhaustiveSearchInstancePromotedTrialInstanceByLastActiveQuery(dbContext)
                                    .Execute(exhaustive.Id);

                            try
                            {
                                var topologyNetworkBytes = getExhaustiveSearchInstancePromotedTrialInstanceQuery.Object;

                                Stream topologyNetworkStream = new MemoryStream(topologyNetworkBytes);

                                Log.Debug(
                                    $"Exhaustive, Balance and Currencies: Exhaustive GUID {exhaustive.Id} has received the byte array from the database and will now start to load the byte array to Accord.");

                                var topologyNetwork = (ActivationNetwork) Network.Load(topologyNetworkStream);
                                exhaustive.TopologyNetwork = topologyNetwork;

                                var getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                    = new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(dbContext);

                                foreach (var exhaustiveVariable in
                                         getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                             .ExecuteByExhaustiveSearchInstanceTrialInstanceId(
                                                 getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                                     .ExhaustiveSearchInstanceTrialInstanceId).ToList().Select(
                                                 variable =>
                                                     new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                                                     {
                                                         Name = variable.Name,
                                                         ProcessingTypeId = variable.ProcessingTypeId,
                                                         Mean = variable.Mean,
                                                         Sd = variable.StandardDeviation,
                                                         NormalisationTypeId = variable.NormalisationTypeId
                                                     }))
                                    exhaustive.NetworkVariablesInOrder.Add(exhaustiveVariable);

                                Log.Debug(
                                    $"Entity Start: Exhaustive GUID {exhaustive.Id} has loaded the byte array to Accord.");
                            }
                            catch (Exception ex)
                            {
                                Log.Debug(
                                    $"Entity Start: Exhaustive GUID {exhaustive.Id} has created an error during loading as {ex}.");
                            }

                            shadowEntityAnalysisModelExhaustive.Add(exhaustive);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Exhaustive Id {record.Id} returned for model {key} as created an error as {ex}.");
                    }

                value.ExhaustiveModels = shadowEntityAnalysisModelExhaustive;
            }
        }

        private void SyncEntityAnalysisModelTags(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and adding Tags.");

                var repository = new EntityAnalysisModelTagRepository(dbContext);

                Log.Debug($"Entity Start: Executing fetch of all Tags for entity model {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelTags = new List<EntityAnalysisModelTag>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Tag {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Tag ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelTag = new EntityAnalysisModelTag
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelTag.Name =
                                    $"Tag_{entityAnalysisModelTag.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag_{entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelTag.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.Name}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelTag.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelTag.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelTag.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelTag.ReportTable = record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ReportTable}.");
                            }
                            
                            shadowEntityAnalysisModelTags.Add(entityAnalysisModelTag);

                            Log.Debug(
                                $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} has been added to a shadow list of Tags.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Tag ID {record.Id} returned for model {key} as created an error as {ex}.");
                    }

                Log.Debug(
                    $"Model {key} and Tag Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");

                value.EntityAnalysisModelTags = shadowEntityAnalysisModelTags;

                Log.Debug(
                    $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
            }

            Log.Debug("Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
        }
                
        private void SyncEntityAnalysisModelHttpAdaptation(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the HttP Adaptation.");

                var repository = new EntityAnalysisModelHttpAdaptationRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelHttpAdaptationRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelAdaptations = new Dictionary<int, EntityAnalysisModelHttpAdaptation>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Adaptation ID ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelAdaptation = new EntityAnalysisModelHttpAdaptation(10,false,1000)
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelAdaptation.Name =
                                    $"Adaptation_{entityAnalysisModelAdaptation.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation{entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.Name}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelAdaptation.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelAdaptation.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelAdaptation.ReportTable = record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ReportTable}.");
                            }

                            if (record.HttpEndpoint == null)
                            {
                                entityAnalysisModelAdaptation.HttpEndpoint = "";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set DEFAULT Http Endpoint  as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                            }
                            else
                            {
                                var validHost = JubeEnvironment.AppSettings("HttpAdaptationUrl").EndsWith("/") 
                                    ? JubeEnvironment.AppSettings("HttpAdaptationUrl") : 
                                    JubeEnvironment.AppSettings("HttpAdaptationUrl") + "/" ;
                
                                var validUrl = record.HttpEndpoint.StartsWith("/")
                                    ? record.HttpEndpoint.Remove(0,1)
                                    : record.HttpEndpoint;
                            
                                var rPlumberEndpoint = $"{validHost}{validUrl}";
                            
                                entityAnalysisModelAdaptation.HttpEndpoint = rPlumberEndpoint;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set Http Endpoint as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                            }

                            shadowEntityAnalysisModelAdaptations.Add(
                                entityAnalysisModelAdaptation.Id,
                                entityAnalysisModelAdaptation);

                            Log.Debug(
                                $"Entity Start: Model {key} and Exhaustive Search Instance Trial Instance ID  {entityAnalysisModelAdaptation.Id} has been added to a shadow list of Adaptations.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} as created an error as {ex}.");
                    }

                Log.Debug(
                    $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");

                value.EntityAnalysisModelAdaptations = shadowEntityAnalysisModelAdaptations;

                Log.Debug(
                    $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
            }

            Log.Debug("Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
        }

        private void SyncEntityAnalysisModelInlineScripts(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Scripts.");

                var repository = new EntityAnalysisModelInlineScriptRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                Log.Debug("Returned all inline scripts from the database.");

                var shadowEntityAnalysisModelInlineScripts = new List<EntityAnalysisModelInlineScript>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key} is Active.");

                            foreach (var addInlineScriptWithinLoop in _inlineScripts)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID {record.EntityAnalysisInlineScriptId} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId}.");

                                if (record.EntityAnalysisInlineScriptId.HasValue)
                                {
                                    Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} checking to see if matched to this model.");

                                    if (addInlineScriptWithinLoop.InlineScriptId ==
                                        record.EntityAnalysisInlineScriptId.Value)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is matched to this model.  Will now check if there are grouping keys for this inline script that need to be attached to the model.");

                                        foreach (var searchKey in addInlineScriptWithinLoop.GroupingKeys)
                                        {
                                            Log.Debug(
                                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} grouping ket {searchKey.SearchKey}.");

                                            if (!value.DistinctSearchKeys.ContainsKey(
                                                    searchKey.SearchKey))
                                            {
                                                value.DistinctSearchKeys.Add(searchKey.SearchKey,
                                                    searchKey);

                                                Log.Debug(
                                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} grouping key {searchKey.SearchKey} has been matched.");
                                            }
                                            else
                                            {
                                                value.DistinctSearchKeys[searchKey.SearchKey] =
                                                    searchKey;
                                            }
                                        }

                                        Log.Debug(
                                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is in the cache.");

                                        if (addInlineScriptWithinLoop != null)
                                        {
                                            shadowEntityAnalysisModelInlineScripts.Add(addInlineScriptWithinLoop);
                                            Log.Debug(
                                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is in the cache and has been added to a shadow list of inline scripts for this model.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} has created an error as {ex}.");
                    }

                value.EntityAnalysisModelInlineScripts = shadowEntityAnalysisModelInlineScripts;

                Log.Debug(
                    "Entity Start: Inline Scripts have overwritten the main copy with the shadow copy for model {ModelKVP.Key}.");
            }

            Log.Debug("Entity Start: Completed adding Inline Scripts to entity models.");
        }

        private void SyncEntityAnalysisModelTtlCounters(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding TTL Counters.");

                var repository = new EntityAnalysisModelTtlCounterRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelTtlCounterRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelTtlCounters = new List<EntityAnalysisModelTtlCounter>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: TTL Counter ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: TTL Counter ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelTtlCounter = new EntityAnalysisModelTtlCounter
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelTtlCounter.Name =
                                    $"TTL_Counter_{entityAnalysisModelTtlCounter.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.Name}.");
                            }

                            if (record.TtlCounterDataName == null)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterDataName =
                                    record.TtlCounterDataName.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                            }

                            if (record.TtlCounterInterval == null)
                            {
                                entityAnalysisModelTtlCounter.TtlCounterInterval = "d";

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterInterval = record.TtlCounterInterval;

                                Log.Debug(
                                    $"Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                            }

                            if (!record.TtlCounterValue.HasValue)
                            {
                                entityAnalysisModelTtlCounter.TtlCounterValue = 0;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterValue = record.TtlCounterValue.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelTtlCounter.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.ResponsePayload = record.ResponsePayload.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelTtlCounter.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.ReportTable = record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                            }

                            if (!record.OnlineAggregation.HasValue)
                            {
                                entityAnalysisModelTtlCounter.OnlineAggregation = true;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set DEFAULT Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.OnlineAggregation = record.OnlineAggregation == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                            }

                            if (!record.EnableLiveForever.HasValue)
                            {
                                entityAnalysisModelTtlCounter.EnableLiveForever = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set DEFAULT Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.EnableLiveForever = record.EnableLiveForever.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                            }

                            shadowEntityAnalysisModelTtlCounters.Add(entityAnalysisModelTtlCounter);

                            Log.Debug(
                                $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} has been added to a shadow collection of TTL Counters.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: TTL Counter ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                value.ModelTtlCounters = shadowEntityAnalysisModelTtlCounters;

                Log.Debug(
                    $"Entity Start: Model {key} and TTL Counter Interval has been added to a shadow collection of TTL Counters.");
            }

            Log.Debug("Entity Start: Completed adding TTL Counters to entity models.");
        }

        private void SyncEntityAnalysisModelSanctions(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Sanctions.");

                var repository = new EntityAnalysisModelSanctionRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelSanctionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                Log.Debug("Returned all Sanctions from the database.");

                var shadowEntityAnalysisModelSanctions = new List<EntityAnalysisModelSanction>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Sanctions ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Sanctions ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelSanctions = new EntityAnalysisModelSanction
                            {
                                EntityAnalysisModelSanctionsId = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelSanctions.Name =
                                    $"Sanctions_Name_{entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {record.Id} set DEFAULT Name as {entityAnalysisModelSanctions.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.Name =
                                    record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Name as {entityAnalysisModelSanctions.Name}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelSanctions.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.ResponsePayload =
                                    record.ResponsePayload.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelSanctions.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.ReportTable = record.ReportTable.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                            }

                            if (!record.CacheValue.HasValue)
                            {
                                entityAnalysisModelSanctions.CacheValue = 0;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.CacheValue = record.CacheValue.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                            }

                            if (!record.Distance.HasValue)
                            {
                                entityAnalysisModelSanctions.Distance = 0;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.Distance}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.Distance = record.Distance.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.Distance}.");
                            }

                            if (record.CacheInterval == null)
                            {
                                entityAnalysisModelSanctions.CacheInterval = 'd';

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                            }
                            else
                            {
                                entityAnalysisModelSanctions.CacheInterval = record.CacheInterval.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                            }

                            if (record.MultipartStringDataName != null)
                            {
                                entityAnalysisModelSanctions.MultipartStringDataName =
                                    record.MultipartStringDataName.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Multipart String Data Name as {entityAnalysisModelSanctions.MultipartStringDataName}.");
                            }

                            shadowEntityAnalysisModelSanctions.Add(entityAnalysisModelSanctions);

                            Log.Debug(
                                $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} has been added to a shadow list of Sanctions.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Sanctions ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }


                value.EntityAnalysisModelSanctions = shadowEntityAnalysisModelSanctions;

                Log.Debug(
                    $"Model {key} has overwritten the current Sanctions with the shadow list of Sanctions.");
            }

            Log.Debug("Entity Start: Completed adding Sanctions to entity models.");
        }

        private void SyncEntityAnalysisModelAbstractionCalculations(DbContext dbContext, string strPath,
            Parser.Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Abstraction Calculations.");

                var repository = new EntityAnalysisModelAbstractionCalculationRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelAbstractionCalculationRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                Log.Debug("Returned all Calculation Field from the database.");

                var shadowEntityAnalysisModelAbstractionCalculation =
                    new List<EntityAnalysisModelAbstractionCalculation>();

                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelAbstractionCalculation =
                                new EntityAnalysisModelAbstractionCalculation
                                {
                                    Id = record.Id
                                };

                            if (record.Name == null)
                            {
                                entityAnalysisModelAbstractionCalculation.Name =
                                    $"s{entityAnalysisModelAbstractionCalculation.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                            }

                            if (record.EntityAnalysisModelAbstractionNameLeft != null)
                            {
                                entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft =
                                    record.EntityAnalysisModelAbstractionNameLeft;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                            }

                            if (record.EntityAnalysisModelAbstractionNameRight != null)
                            {
                                entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight =
                                    record.EntityAnalysisModelAbstractionNameRight;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.ReportTable =
                                    record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                            }

                            if (!record.AbstractionCalculationTypeId.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId = 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId =
                                    record.AbstractionCalculationTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                            }

                            var hasRuleScript = false;
                            if (record.FunctionScript != null)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.FunctionScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    entityAnalysisModelAbstractionCalculation.FunctionScript =
                                        parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelAbstractionCalculation.Id} set  script as {entityAnalysisModelAbstractionCalculation.FunctionScript}.");
                                }
                            }

                            if (hasRuleScript)
                            {
                                var activationRuleScript = new StringBuilder();
                                activationRuleScript.Append("Imports System.IO\r\n");
                                activationRuleScript.Append("Imports log4net\r\n");
                                activationRuleScript.Append("Imports System.Net\r\n");
                                activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                                activationRuleScript.Append("Imports System\r\n");
                                activationRuleScript.Append("Public Class CalculationRule\r\n");
                                activationRuleScript.Append(
                                    "Public Shared Function Match(Data As Dictionary(Of string,object),TTLCounter As Dictionary(Of String, Integer),Abstraction As Dictionary(Of string,double),List as Dictionary(Of String,List(Of String)),KVP As Dictionary(Of String, Double),Log as ILog) As Double\r\n");
                                activationRuleScript.Append("Dim Matched as Double\r\n");
                                activationRuleScript.Append("Try\r\n");
                                activationRuleScript.Append(entityAnalysisModelAbstractionCalculation.FunctionScript +
                                                            "\r\n");
                                activationRuleScript.Append("Catch ex As Exception\r\n");
                                activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                activationRuleScript.Append("End Try\r\n");
                                activationRuleScript.Append("Return Matched\r\n");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("End Function\r\n");
                                activationRuleScript.Append("End Class\r\n");

                                Log.Debug(
                                    $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} class wrapped as {activationRuleScript}.");

                                var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                                Log.Debug(
                                    $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");

                                if (HashCacheAssembly.ContainsKey(activationRuleScriptHash))
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");

                                    entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                        HashCacheAssembly[activationRuleScriptHash];

                                    var classType =
                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile.GetType(
                                            "CalculationRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                        (EntityAnalysisModelAbstractionCalculation.Match) Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                    Log.Debug(
                                        $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");

                                    var compile = new Compile();
                                    compile.CompileCode(activationRuleScript.ToString(), Log,
                                        new[] {Path.Combine(strPath, "log4net.dll")});

                                    Log.Debug(
                                        $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");

                                    if (compile.Errors == 0)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");

                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                            compile.CompiledAssembly;

                                        var classType =
                                            entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile
                                                .GetType("CalculationRule");
                                        var methodInfo = classType.GetMethod("Match");
                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                            (EntityAnalysisModelAbstractionCalculation.Match)
                                            Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                        HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);

                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Calculations.");
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                    }
                                }
                            }

                            shadowEntityAnalysisModelAbstractionCalculation.Add(
                                entityAnalysisModelAbstractionCalculation);

                            Log.Debug(
                                $"Entity Start: Model {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been added to a shadow list of Abstraction Calculations.");
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Calculation {record.Id} has not been added to a shadow list of Abstraction Calculations as it is not active.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                value.EntityAnalysisModelAbstractionCalculations =
                    shadowEntityAnalysisModelAbstractionCalculation;

                Log.Debug(
                    $"Model {key} has overwritten the current Abstraction Calculations with the shadow list of Abstraction Calculations.");
            }

            Log.Debug("Entity Start: Completed adding Abstraction Calculations to entity models.");
        }

        private void SyncEntityAnalysisModelInlineFunctions(DbContext dbContext, string strPath, Parser.Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Functions.");

                var repository = new EntityAnalysisModelInlineFunctionRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelInlineFunctionRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                Log.Debug("Returned all Inline Functions from the database.");

                var shadowEntityAnalysisModelInlineFunctions = new List<EntityAnalysisModelInlineFunction>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Inline Function ID {record.Id} returned for model {key}.");

                        if (record.Active.Value == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Inline_Function ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelInlineFunction = new EntityAnalysisModelInlineFunction
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelInlineFunction.Name =
                                    $"Inline_Function_{entityAnalysisModelInlineFunction.Id}";

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Name as {entityAnalysisModelInlineFunction.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Inline Function {entityAnalysisModelInlineFunction.Id} set Name as {entityAnalysisModelInlineFunction.Name}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ResponsePayload = record.ResponsePayload.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ReportTable = record.ReportTable.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                            }

                            if (!record.ReturnDataTypeId.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ReturnDataTypeId = 1;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ReturnDataTypeId = record.ReturnDataTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                            }

                            var hasRuleScript = false;
                            if (record.FunctionScript != null)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.FunctionScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    entityAnalysisModelInlineFunction.FunctionScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set  script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set soft parse security error for script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                }
                            }

                            if (hasRuleScript)
                            {
                                var activationRuleScript = new StringBuilder();
                                activationRuleScript.Append("Imports System.IO\r\n");
                                activationRuleScript.Append("Imports log4net\r\n");
                                activationRuleScript.Append("Imports System.Net\r\n");
                                activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                                activationRuleScript.Append("Imports System\r\n");
                                activationRuleScript.Append("Public Class InlineFunction\r\n");
                                activationRuleScript.Append(
                                    "Public Shared Function Match(Data As Dictionary(Of string,object),List As Dictionary(Of String, List(Of String)),KVP As Dictionary(Of String, Double),Log as ILog) As Object\r\n");
                                activationRuleScript.Append("Dim Matched as Object = Nothing");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("Try\r\n");
                                activationRuleScript.Append(entityAnalysisModelInlineFunction.FunctionScript + "\r\n");
                                activationRuleScript.Append("Catch ex As Exception\r\n");
                                activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                activationRuleScript.Append("End Try\r\n");
                                activationRuleScript.Append("Return Matched\r\n");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("End Function\r\n");
                                activationRuleScript.Append("End Class\r\n");

                                Log.Debug(
                                    $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} class wrapped as {activationRuleScript}.");

                                var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                                Log.Debug(
                                    $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");

                                if (HashCacheAssembly.ContainsKey(activationRuleScriptHash))
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");

                                    entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                        HashCacheAssembly[activationRuleScriptHash];

                                    var classType =
                                        entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                            "InlineFunction");
                                    var methodInfo = classType.GetMethod("Match");
                                    entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                        (EntityAnalysisModelInlineFunction.Match) Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelInlineFunction.Match),
                                            methodInfo);

                                    shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Inline Functions.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");

                                    var compile = new Compile();
                                    compile.CompileCode(activationRuleScript.ToString(), Log,
                                        new[] {Path.Combine(strPath, "log4net.dll")});

                                    Log.Debug(
                                        $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");

                                    if (compile.Errors == 0)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");

                                        entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                            compile.CompiledAssembly;

                                        var classType =
                                            entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                                "InlineFunction");
                                        var methodInfo = classType.GetMethod("Match");
                                        entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                            (EntityAnalysisModelInlineFunction.Match) Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelInlineFunction.Match),
                                                methodInfo);

                                        HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);
                                        shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Inline Functions.");
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                    }
                                }
                            }

                            Log.Debug(
                                $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} has been added to a shadow list of Inline Functions.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Inline Function ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                value.EntityAnalysisModelInlineFunctions = shadowEntityAnalysisModelInlineFunctions;

                Log.Debug(
                    $"Model {key} has overwritten the current Inline Functions with the shadow list of Inline Functions.");
            }

            Log.Debug("Entity Start: Completed adding Inline Functions to entity models.");
        }

        private void SyncEntityAnalysisModelGatewayRules(DbContext dbContext, string strPath, Parser.Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Checking if model {key} is started for the purpose determining compiling the Gateway rules.");

                var repository = new EntityAnalysisModelGatewayRuleRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityModelGatewayRule = new List<EntityModelGatewayRule>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key} is active.");

                            var modelGatewayRule = new EntityModelGatewayRule
                            {
                                EntityAnalysisModelGatewayRuleId = record.Id
                            };

                            if (record.Name != null)
                            {
                                modelGatewayRule.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule value set as {modelGatewayRule.Name}.");
                            }
                            else
                            {
                                modelGatewayRule.Name =
                                    $"Gateway_Rule_{modelGatewayRule.EntityAnalysisModelGatewayRuleId}";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule value set as {modelGatewayRule.Name}.");
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelGatewayRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                            }
                            else
                            {
                                modelGatewayRule.RuleScriptTypeId = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                            }

                            if (record.MaxResponseElevation.HasValue)
                            {
                                modelGatewayRule.MaxResponseElevation = record.MaxResponseElevation.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                            }
                            else
                            {
                                modelGatewayRule.MaxResponseElevation = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                            }

                            if (record.GatewaySample.HasValue)
                            {
                                modelGatewayRule.GatewaySample = record.GatewaySample.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                            }
                            else
                            {
                                modelGatewayRule.GatewaySample = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                            }

                            var hasRuleScript = false;
                            if (record.BuilderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 1)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.BuilderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set builder script as {modelGatewayRule.GatewayRuleScript}.");
                                }
                            }
                            else if (record.CoderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 2)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.CoderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set coder script as {modelGatewayRule.GatewayRuleScript}.");
                                }
                            }

                            if (hasRuleScript)
                            {
                                var gatewayRuleScript = new StringBuilder();
                                gatewayRuleScript.Append("Imports System.IO\r\n");
                                gatewayRuleScript.Append("Imports log4net\r\n");
                                gatewayRuleScript.Append("Imports System.Net\r\n");
                                gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                                gatewayRuleScript.Append("Imports System\r\n");
                                gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                                gatewayRuleScript.Append(
                                    "Public Shared Function Match(Data As Dictionary(Of string,object),List As Dictionary(Of String, List(Of String)),KVP As Dictionary(Of String, Double),Log as ILog) As Boolean\r\n");
                                gatewayRuleScript.Append("Dim Matched as Boolean\r\n");
                                gatewayRuleScript.Append("Try\r\n");
                                gatewayRuleScript.Append(modelGatewayRule.GatewayRuleScript + "\r\n");
                                gatewayRuleScript.Append("Catch ex As Exception\r\n");
                                gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                gatewayRuleScript.Append("End Try\r\n");
                                gatewayRuleScript.Append("Return Matched\r\n");
                                gatewayRuleScript.Append("\r\n");
                                gatewayRuleScript.Append("End Function\r\n");
                                gatewayRuleScript.Append("End Class\r\n");

                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set class wrap as {gatewayRuleScript}.");

                                var gatewayRuleScriptHash = Hash.GetHash(gatewayRuleScript.ToString());

                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");

                                if (HashCacheAssembly.ContainsKey(gatewayRuleScriptHash))
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");

                                    modelGatewayRule.GatewayRuleCompile = HashCacheAssembly[gatewayRuleScriptHash];

                                    var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    modelGatewayRule.GatewayRuleCompileDelegate =
                                        (EntityModelGatewayRule.Match) Delegate.CreateDelegate(
                                            typeof(EntityModelGatewayRule.Match), methodInfo);

                                    shadowEntityModelGatewayRule.Add(modelGatewayRule);

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");

                                    var compile = new Compile();
                                    compile.CompileCode(gatewayRuleScript.ToString(), Log,
                                        new[] {Path.Combine(strPath, "log4net.dll")});

                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");

                                    if (compile.Errors == 0)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");

                                        modelGatewayRule.GatewayRuleCompile = compile.CompiledAssembly;

                                        var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                                        var methodInfo = classType.GetMethod("Match");
                                        modelGatewayRule.GatewayRuleCompileDelegate =
                                            (EntityModelGatewayRule.Match) Delegate.CreateDelegate(
                                                typeof(EntityModelGatewayRule.Match), methodInfo);
                                        shadowEntityModelGatewayRule.Add(modelGatewayRule);
                                        HashCacheAssembly.Add(gatewayRuleScriptHash, compile.CompiledAssembly);

                                        Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                Log.Debug(
                    $"Entity Start: {key} is being finished,  proceeding to update gateway rules and close off the cursor.");

                value.ModelGatewayRules = shadowEntityModelGatewayRule;

                Log.Debug($"Entity Start: {key} replaced Gateway Rule List with shadow gateway rules.");
            }

            Log.Debug("Entity Start: Completed adding Gateway Rules to entity models.");
        }

        private void UpdateGatewayRuleCounter(DbContext dbContext, EntityModelGatewayRule gatewayRule)
        {
            try
            {
                var repository = new EntityAnalysisModelGatewayRuleRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");

                repository.UpdateCounter(gatewayRule.EntityAnalysisModelGatewayRuleId, gatewayRule.Counter);

                Log.Debug(
                    $"Entity Start: Finished EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and has reset counter of {gatewayRule.Counter}.");
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Entity Start: Gateway Rule ID {gatewayRule.EntityAnalysisModelGatewayRuleId} has created an error as {ex} on update counter.");
            }
            finally
            {
                gatewayRule.Counter = 0;
            }
        }

        private void SyncEntityAnalysisModelActivationRules(DbContext dbContext, string strPath, Parser.Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Looping through active models {key} is started for the purpose adding the Activation Rules.");

                var repository = new EntityAnalysisModelActivationRuleRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.GetByEntityAnalysisModelIdInPriorityOrder for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelIdInPriorityOrder(key);

                var shadowEntityModelActivationRule = new List<EntityAnalysisModelActivationRule>();
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");

                        bool active;
                        if (record.Active == 1)
                        {
                            active = true;

                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Active value set as {active}.");
                        }
                        else
                        {
                            active = false;

                            Log.Debug(
                                $"Entity Start: Entity Model {key} and DEFAULT Activation Rule {record.Id} set Active value set as {active}.");
                        }

                        bool approval;
                        if (record.ReviewStatusId.HasValue)
                        {
                            switch (record.ReviewStatusId.Value)
                            {
                                case 0:
                                    approval = false;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                    break;
                                case 1:
                                    approval = false;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 1 set as {approval}.");
                                    break;
                                case 2:
                                    approval = false;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 2 set as {approval}.");
                                    break;
                                case 3:
                                    approval = false;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 3 set as {approval}.");
                                    break;
                                case 4:
                                    approval = true;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 4 set as {approval}.");
                                    break;
                                default:
                                    approval = false;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                    break;
                            }
                        }
                        else
                        {
                            approval = false;

                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set DEFAULT Approval value set as {approval}.");
                        }

                        if (active && approval)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} is Active and Approved. Proceeding to build Activation Rule.");

                            var modelActivationRule = new EntityAnalysisModelActivationRule
                            {
                                Id = record.Id
                            };

                            if (record.ReportTable.HasValue)
                            {
                                modelActivationRule.ReportTable =
                                    record.ReportTable.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Promote Report Table value set as {modelActivationRule.ReportTable}.");
                            }
                            else
                            {
                                modelActivationRule.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Promote Report Table value set as {modelActivationRule.ReportTable}.");
                            }

                            if (record.Name != null)
                            {
                                modelActivationRule.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Name value set as {modelActivationRule.Name}.");
                            }
                            else
                            {
                                modelActivationRule.Name =
                                    $"Activation_Rule_{modelActivationRule.Id}";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Name value set as {modelActivationRule.Name}.");
                            }

                            if (record.ResponsePayload.HasValue)
                            {
                                modelActivationRule.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Payload value set as {modelActivationRule.ResponsePayload}.");
                            }
                            else
                            {
                                modelActivationRule.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Payload value set as {modelActivationRule.ResponsePayload}.");
                            }

                            if (record.Visible.HasValue)
                            {
                                modelActivationRule.Visible = record.Visible.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Visible value set as {modelActivationRule.Visible}.");
                            }
                            else
                            {
                                modelActivationRule.Visible = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Visible value set as {modelActivationRule.Visible}.");
                            }

                            if (record.EnableReprocessing.HasValue)
                            {
                                modelActivationRule.EnableReprocessing = record.EnableReprocessing == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                            }
                            else
                            {
                                modelActivationRule.EnableReprocessing = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                            }

                            if (record.ResponseElevationForeColor != null)
                            {
                                modelActivationRule.ResponseElevationForeColor = record.ResponseElevationForeColor;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationForeColor = "#000000";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                            }

                            if (record.BypassSuspendInterval != null)
                            {
                                modelActivationRule.BypassSuspendInterval = record.BypassSuspendInterval.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendInterval = 'd';

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                            }

                            if (record.BypassSuspendValue.HasValue)
                            {
                                modelActivationRule.BypassSuspendValue = record.BypassSuspendValue.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendValue = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                            }

                            if (record.BypassSuspendSample.HasValue)
                            {
                                modelActivationRule.BypassSuspendSample = record.BypassSuspendSample.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                            }
                            else
                            {
                                modelActivationRule.BypassSuspendSample = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                            }

                            if (record.ResponseElevationBackColor != null)
                            {
                                modelActivationRule.ResponseElevationBackColor = record.ResponseElevationBackColor;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationBackColor = "#ffffff";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                            }

                            if (record.EnableCaseWorkflow.HasValue)
                            {
                                modelActivationRule.EnableCaseWorkflow = record.EnableCaseWorkflow.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                            }
                            else
                            {
                                modelActivationRule.EnableCaseWorkflow = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                            }

                            if (record.EnableNotification.HasValue)
                            {
                                modelActivationRule.EnableNotification = record.EnableNotification == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Notification value set as {modelActivationRule.EnableNotification}.");
                            }
                            else
                            {
                                modelActivationRule.EnableNotification = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Notification value set as {modelActivationRule.EnableNotification}.");
                            }

                            if (record.NotificationTypeId.HasValue)
                            {
                                modelActivationRule.NotificationTypeId = record.NotificationTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                            }
                            else
                            {
                                modelActivationRule.NotificationTypeId = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                            }

                            if (record.CaseKey != null)
                            {
                                modelActivationRule.CaseKey = record.CaseKey;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Case Key value set as {modelActivationRule.CaseKey}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Case Key value set as {modelActivationRule.CaseKey}.");
                            }

                            if (record.ResponseElevationKey != null)
                            {
                                modelActivationRule.ResponseElevationKey = record.ResponseElevationKey;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationKey = value.EntryName;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                            }

                            if (record.NotificationDestination != null)
                            {
                                modelActivationRule.NotificationDestination = record.NotificationDestination;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                            }
                            else
                            {
                                modelActivationRule.NotificationDestination = "";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                            }

                            if (record.NotificationSubject != null)
                            {
                                modelActivationRule.NotificationSubject = record.NotificationSubject;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                            }
                            else
                            {
                                modelActivationRule.NotificationSubject = "";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                            }

                            if (record.NotificationBody != null)
                            {
                                modelActivationRule.NotificationBody =
                                    HttpUtility.HtmlDecode(record.NotificationBody);

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Body value set as {modelActivationRule.NotificationBody}.");
                            }
                            else
                            {
                                modelActivationRule.NotificationBody = "";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Body value set as {modelActivationRule.NotificationBody}.");
                            }

                            if (record.SendToActivationWatcher.HasValue)
                            {
                                modelActivationRule.SendToActivationWatcher = record.SendToActivationWatcher.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                            }
                            else
                            {
                                modelActivationRule.SendToActivationWatcher = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                            }

                            if (record.ResponseElevationContent != null)
                            {
                                modelActivationRule.ResponseElevationContent = record.ResponseElevationContent;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationContent = "";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                            }

                            if (record.ResponseElevationRedirect != null)
                            {
                                try
                                {
                                    var uri = new Uri(record.ResponseElevationRedirect);

                                    modelActivationRule.ResponseElevationRedirect = record.ResponseElevationRedirect;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set ResponseRedirect value set as {uri.AbsoluteUri} from original {modelActivationRule.ResponseElevationRedirect}.");
                                }
                                catch (Exception ex)
                                {
                                    modelActivationRule.ResponseElevationRedirect =
                                        value.FallbackResponseElevationRedirect;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set FAILED PARSE fallback, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect} with exception message of {ex.Message}.");
                                }

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Redirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevationRedirect = value.FallbackResponseElevationRedirect;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                            }

                            if (record.ActivationSample.HasValue)
                            {
                                modelActivationRule.ActivationSample = record.ActivationSample.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Activation Sample value set as {modelActivationRule.ActivationSample}.");
                            }
                            else
                            {
                                modelActivationRule.ActivationSample = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Activation Sample value set as {modelActivationRule.ActivationSample}.");
                            }

                            if (record.EnableTtlCounter.HasValue)
                            {
                                modelActivationRule.EnableTtlCounter = record.EnableTtlCounter == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                            }
                            else
                            {
                                modelActivationRule.EnableTtlCounter = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                            }

                            if (record.EnableResponseElevation.HasValue)
                            {
                                modelActivationRule.EnableResponseElevation = record.EnableResponseElevation == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                            }
                            else
                            {
                                modelActivationRule.EnableResponseElevation = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                            }

                            if (record.EntityAnalysisModelTtlCounterId.HasValue)
                            {
                                modelActivationRule.EntityAnalysisModelTtlCounterId =
                                    record.EntityAnalysisModelTtlCounterId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set TTL Counter Name value set as {modelActivationRule.EntityAnalysisModelTtlCounterId}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterId}.");
                            }

                            if (record.EntityAnalysisModelIdTtlCounter.HasValue)
                            {
                                modelActivationRule.EntityAnalysisModelIdTtlCounter =
                                    record.EntityAnalysisModelIdTtlCounter.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Entity Analysis Model Id Ttl Counter value set as {modelActivationRule.EntityAnalysisModelIdTtlCounter}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterId}.");
                            }

                            if (record.ResponseElevation.HasValue)
                            {
                                modelActivationRule.ResponseElevation = record.ResponseElevation.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                            }
                            else
                            {
                                modelActivationRule.ResponseElevation = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                            }

                            if (record.CaseWorkflowId.HasValue)
                            {
                                modelActivationRule.CaseWorkflowId = record.CaseWorkflowId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow ID value set as {modelActivationRule.CaseWorkflowId}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow ID, not supplied, value set as {modelActivationRule.CaseWorkflowId}.");
                            }

                            if (record.CaseWorkflowStatusId.HasValue)
                            {
                                modelActivationRule.CaseWorkflowStatusId = record.CaseWorkflowStatusId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow Status Name value set as {modelActivationRule.CaseWorkflowStatusId}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow Status Name, not supplied, value set as {modelActivationRule.CaseWorkflowStatusId}.");
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelActivationRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Rule Script Type ID value set as {modelActivationRule.RuleScriptTypeId}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Rule Script Type ID, not supplied, value set as {modelActivationRule.RuleScriptTypeId}.");
                            }

                            var hasRuleScript = false;
                            if (record.BuilderRuleScript != null && modelActivationRule.RuleScriptTypeId == 1)
                            {
                                /*var rule = Parser.TranslateFromDotNotation(record.BuilderRuleScript,value.EntityAnalysisModelRequestXPaths);
                                if (Parser.Parse(rule))*/

                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.BuilderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set builder script as {modelActivationRule.ActivationRuleScript}.");
                                }
                            }
                            else if (record.CoderRuleScript != null && modelActivationRule.RuleScriptTypeId == 2)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.CoderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set coder script as {modelActivationRule.ActivationRuleScript}.");
                                    hasRuleScript = true;
                                }
                            }

                            if (hasRuleScript)
                            {
                                var activationRuleScript = new StringBuilder();
                                activationRuleScript.Append("Imports System.IO\r\n");
                                activationRuleScript.Append("Imports log4net\r\n");
                                activationRuleScript.Append("Imports System.Net\r\n");
                                activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                                activationRuleScript.Append("Imports System\r\n");
                                activationRuleScript.Append("Public Class ActivationRule\r\n");
                                activationRuleScript.Append(
                                    "Public Shared Function Match(Data As Dictionary(Of string,object),TTLCounter As Dictionary(Of String, Integer),Abstraction As Dictionary(Of string,double),HttpAdaptation As Dictionary(Of String, Double),ExhaustiveAdaptation As Dictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As Dictionary(Of String, Double),Sanctions As Dictionary(Of String, Double),KVP As Dictionary(Of String, Double),Log as ILog) As Boolean\r\n");
                                activationRuleScript.Append("Dim Matched as Boolean\r\n");
                                activationRuleScript.Append("Try\r\n");
                                activationRuleScript.Append(modelActivationRule.ActivationRuleScript + "\r\n");
                                activationRuleScript.Append("Catch ex As Exception\r\n");
                                activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                activationRuleScript.Append("End Try\r\n");
                                activationRuleScript.Append("Return Matched\r\n");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("End Function\r\n");
                                activationRuleScript.Append("End Class\r\n");

                                Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} class wrapped as {activationRuleScript}.");

                                var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                                Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");

                                if (HashCacheAssembly.ContainsKey(activationRuleScriptHash))
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");

                                    modelActivationRule.ActivationRuleCompile =
                                        HashCacheAssembly[activationRuleScriptHash];

                                    var classType = modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    modelActivationRule.ActivationRuleCompileDelegate =
                                        (EntityAnalysisModelActivationRule.Match) Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelActivationRule.Match), methodInfo);

                                    shadowEntityModelActivationRule.Add(modelActivationRule);

                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");

                                    var compile = new Compile();
                                    compile.CompileCode(activationRuleScript.ToString(), Log,
                                        new[] {Path.Combine(strPath, "log4net.dll")});

                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");

                                    if (compile.Errors == 0)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");

                                        modelActivationRule.ActivationRuleCompile = compile.CompiledAssembly;

                                        var classType =
                                            modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                                        var methodInfo = classType.GetMethod("Match");
                                        modelActivationRule.ActivationRuleCompileDelegate =
                                            (EntityAnalysisModelActivationRule.Match) Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelActivationRule.Match), methodInfo);
                                        shadowEntityModelActivationRule.Add(modelActivationRule);
                                        HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);

                                        Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Activation Rules.");
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Activation Rule Model {record.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                Log.Debug($"Entity Start: {key} replaced Activation Rule List with shadow activation rules.");

                value.ModelActivationRules = shadowEntityModelActivationRule;

                Log.Debug($"Entity Start: {key} finished updating the counters for the Activation Rules.");
            }

            Log.Debug("Entity Start: finished loading the Activation Rules.");
        }

        private void UpdateActivationRuleCounter(DbContext dbContext, EntityAnalysisModelActivationRule activationRule)
        {
            try
            {
                var repository = new EntityAnalysisModelActivationRuleRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and counter of {activationRule.Counter}.");

                repository.UpdateCounter(activationRule.Id, activationRule.Counter);

                Log.Debug(
                    $"Entity Start: Finished Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and has reset counter of {activationRule.Counter}.");
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Entity Start: Activation Rule ID {activationRule.Id} has created an error as {ex} on update counter.");
            }
            finally
            {
                activationRule.Counter = 0;
            }
        }

        private void SyncEntityAnalysisModelAbstractionRules(DbContext dbContext, string strPath, Parser.Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Looping through active models {key} is started for the purpose adding the Abstraction Rules.");

                var repository = new EntityAnalysisModelAbstractionRuleRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelAbstractionRuleRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityModelAbstractionRule = new List<EntityAnalysisModelAbstractionRule>();

                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} is active.");

                            var modelAbstractionRule = new EntityAnalysisModelAbstractionRule
                            {
                                Id = record.Id
                            };

                            if (record.Name != null)
                            {
                                modelAbstractionRule.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Name value as {modelAbstractionRule.Name}.");
                            }
                            else
                            {
                                modelAbstractionRule.Name =
                                    $"Abstraction_Rule_{modelAbstractionRule.Id}";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Name value as {modelAbstractionRule.Name}.");
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelAbstractionRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                            }
                            else
                            {
                                modelAbstractionRule.RuleScriptTypeId = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                            }

                            if (record.SearchKey != null)
                            {
                                modelAbstractionRule.SearchKey = record.SearchKey;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Key value as {modelAbstractionRule.SearchKey}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Key value as {modelAbstractionRule.SearchKey}.");
                            }

                            if (record.ReportTable.HasValue)
                            {
                                modelAbstractionRule.ReportTable = record.ReportTable == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                            }
                            else
                            {
                                modelAbstractionRule.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                            }

                            if (record.ResponsePayload.HasValue)
                            {
                                modelAbstractionRule.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                            }
                            else
                            {
                                modelAbstractionRule.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                            }

                            if (record.SearchFunctionKey != null)
                            {
                                modelAbstractionRule.SearchFunctionKey = record.SearchFunctionKey;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                            }

                            if (record.Search.HasValue)
                            {
                                modelAbstractionRule.Search = record.Search.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value as {modelAbstractionRule.Search}.");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Checking to see if already added to the distinct search keys available to this abstraction rule.");

                                var distinctSearchKey = new DistinctSearchKey();
                                foreach (var requestXPath in value.EntityAnalysisModelRequestXPaths)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} checking {requestXPath.Name}.");

                                    if (requestXPath.Name == modelAbstractionRule.SearchKey)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name}.");

                                        distinctSearchKey.SearchKeyCacheIntervalType =
                                            requestXPath.SearchKeyCacheInterval;
                                        distinctSearchKey.SearchKeyCacheIntervalValue =
                                            requestXPath.SearchKeyCacheValue;
                                        distinctSearchKey.SearchKeyCacheTtlIntervalValue =
                                            requestXPath.SearchKeyCacheTtlValue;
                                        distinctSearchKey.SearchKeyCache = requestXPath.SearchKeyCache;
                                        distinctSearchKey.SearchKeyCacheFetchLimit =
                                            requestXPath.SearchKeyCacheFetchLimit;
                                        distinctSearchKey.SearchKey = modelAbstractionRule.SearchKey;
                                        distinctSearchKey.SearchKeyCacheSample = requestXPath.SearchKeyCacheSample;

                                        if (!value.DistinctSearchKeys.ContainsKey(modelAbstractionRule
                                                .SearchKey))
                                        {
                                            value.DistinctSearchKeys.Add(distinctSearchKey.SearchKey,
                                                distinctSearchKey);

                                            Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Not added to distinct search keys,  adding.");
                                        }
                                        else
                                        {
                                            value.DistinctSearchKeys[distinctSearchKey.SearchKey] =
                                                distinctSearchKey;

                                            Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Not added to distinct search keys,  merging.");
                                        }

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name} and added the grouping key to the distinct list being used by the rule.  Will not check any further in the available XPath.");

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                modelAbstractionRule.Search = true;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Search Extrapolation Type value as {modelAbstractionRule.Search}.");
                            }

                            if (record.Offset.HasValue)
                            {
                                modelAbstractionRule.EnableOffset = record.Offset.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                            }
                            else
                            {
                                modelAbstractionRule.EnableOffset = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                            }

                            if (record.OffsetTypeId.HasValue)
                            {
                                modelAbstractionRule.OffsetType = record.OffsetTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset Type value as {modelAbstractionRule.OffsetType}.");
                            }
                            else
                            {
                                modelAbstractionRule.OffsetType = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset Type value as {modelAbstractionRule.OffsetType}.");
                            }

                            if (record.OffsetValue.HasValue)
                            {
                                modelAbstractionRule.OffsetValue = record.OffsetValue.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset value as {modelAbstractionRule.OffsetValue}.");
                            }
                            else
                            {
                                modelAbstractionRule.OffsetValue = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset value as {modelAbstractionRule.OffsetValue}.");
                            }

                            if (record.SearchInterval != null)
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType =
                                    record.SearchInterval;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType = "d";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }

                            if (record.SearchInterval != null)
                            {
                                modelAbstractionRule.AbstractionHistoryIntervalValue =
                                    record.SearchValue;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionHistoryIntervalValue = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }

                            if (record.SearchFunctionTypeId.HasValue)
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionType =
                                    record.SearchFunctionTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Type Value as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionType = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction History Search Function Type Id as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                            }

                            var hasRuleScript = false;
                            if (record.BuilderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 1)
                            {
                                /*var rule = Parser.TranslateFromDotNotation(record.BuilderRuleScript,value.EntityAnalysisModelRequestXPaths);
                                if (Parser.Parse(rule))*/
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.BuilderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set builder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                }
                            }
                            else if (record.CoderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 2)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.CoderRuleScript,
                                    ErrorSpans = new List<ErrorSpan>()
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set coder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                    hasRuleScript = true;
                                }
                            }

                            if (hasRuleScript)
                            {
                                var abstractionRuleScript = new StringBuilder();
                                abstractionRuleScript.Append("Imports System.IO\r\n");
                                abstractionRuleScript.Append("Imports log4net\r\n");
                                abstractionRuleScript.Append("Imports System.Net\r\n");
                                abstractionRuleScript.Append("Imports System.Collections.Generic\r\n");
                                abstractionRuleScript.Append("Imports System\r\n");
                                abstractionRuleScript.Append("Public Class AbstractionRule\r\n");
                                abstractionRuleScript.Append(
                                    "Public Shared Function Match(Data As Dictionary(Of string,object),TTLCounter as Dictionary(Of String,Integer),List as Dictionary(Of String,List(Of String)),KVP as Dictionary(of String,Double),Log as ILog) As Boolean\r\n");
                                abstractionRuleScript.Append("Dim Matched as Boolean\r\n");
                                abstractionRuleScript.Append("Try\r\n");
                                abstractionRuleScript.Append(modelAbstractionRule.AbstractionRuleScript + "\r\n");
                                abstractionRuleScript.Append("Catch ex As Exception\r\n");
                                abstractionRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                abstractionRuleScript.Append("End Try\r\n");
                                abstractionRuleScript.Append("Return Matched\r\n");
                                abstractionRuleScript.Append("\r\n");
                                abstractionRuleScript.Append("End Function\r\n");
                                abstractionRuleScript.Append("End Class\r\n");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set class as {abstractionRuleScript}.");

                                var abstractionRuleScriptHash = Hash.GetHash(abstractionRuleScript.ToString());

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} calculated hash as {abstractionRuleScriptHash}.  Checking if in hash cache.");

                                if (HashCacheAssembly.ContainsKey(abstractionRuleScriptHash))
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is in the hash cache,  will create the delegate from this.");

                                    modelAbstractionRule.AbstractionRuleCompile =
                                        HashCacheAssembly[abstractionRuleScriptHash];
                                    var classType =
                                        modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    modelAbstractionRule.AbstractionRuleCompileDelegate =
                                        (EntityAnalysisModelAbstractionRule.Match) Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);
                                    shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} created delegate.");
                                }
                                else
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is not in the hash cache,  will proceed to compile.");

                                    var compile = new Compile();
                                    compile.CompileCode(abstractionRuleScript.ToString(), Log,
                                        new[] {Path.Combine(strPath, "log4net.dll")});

                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled with {compile.Errors}.");

                                    if (compile.Errors == 0)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled without error and will proceed to create delegate.");

                                        modelAbstractionRule.AbstractionRuleCompile = compile.CompiledAssembly;

                                        var classType =
                                            modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                                        var methodInfo = classType.GetMethod("Match");

                                        modelAbstractionRule.AbstractionRuleCompileDelegate =
                                            (EntityAnalysisModelAbstractionRule.Match) Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);

                                        HashCacheAssembly.Add(abstractionRuleScriptHash, compile.CompiledAssembly);

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has created delegate and added it to the hash cache.");

                                        shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is being added to the shadow list of Abstraction Rules.");
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has failed to load.");
                                    }
                                }

                                modelAbstractionRule.LogicHash =
                                    Hash.GetHash(modelAbstractionRule.AbstractionRuleScript);

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {modelAbstractionRule.LogicHash} has been attached to the rule to avoid duplication in execution of abstraction rules.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                value.ModelAbstractionRules = shadowEntityModelAbstractionRule;

                Log.Debug(
                    $"Entity Start: Entity Model {key} and Abstraction Rule Model has replaced the Abstraction Rule list with the shadow values and closed the reader.");
            }

            Log.Debug("Entity Start: Completed adding Abstraction Rules to entity models.");
        }

        private void SyncEntityAnalysisModelRequestXPath(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                Log.Debug(
                    $"Entity Start: Looping through active models {key} is started for the purpose adding the XPath.");

                var repository = new EntityAnalysisModelRequestXPathRepository(dbContext);

                Log.Debug(
                    $"Entity Start: Executing EntityAnalysisModelRequestXPathRepository.GetByEntityAnalysisModelId for entity model key of {key}.");

                var records =
                    repository.GetByEntityAnalysisModelId(
                        key);

                var shadowEntityAnalysisModelRequestXPath = new List<EntityAnalysisModelRequestXPath>();
                var cachePayloadSql = $"select \"CreatedDate\",\"ReferenceDate\" AS \"{value.ReferenceDateName}\"";
                var archivePayloadSql = "select \"EntityAnalysisModelInstanceEntryGuid\"," +
                                        $"\"CreatedDate\",\"ReferenceDate\" AS \"{value.ReferenceDateName}\"";
                foreach (var record in records)
                    try
                    {
                        Log.Debug(
                            $"Entity Start: XPath ID {record.Id} returned for model {key}.");

                        if (record.Active == 1)
                        {
                            Log.Debug(
                                $"Entity Start: XPath ID {record.Id} returned for model {key} is active.");

                            var entityAnalysisModelRequestXPath = new EntityAnalysisModelRequestXPath
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelRequestXPath.Name =
                                    $"XPath_Name_{entityAnalysisModelRequestXPath.Id}";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Name value as {entityAnalysisModelRequestXPath.Name}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.Name = record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Name value as {entityAnalysisModelRequestXPath.Name}.");
                            }

                            if (!record.DataTypeId.HasValue)
                            {
                                entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelRequestXPath.ReportTable = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.ReportTable = record.ReportTable.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                            }

                            if (record.XPath == null)
                            {
                                entityAnalysisModelRequestXPath.XPath = "$." + record.Name.Replace(" ", "_");

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.XPath = record.XPath;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                            }

                            if (record.DefaultValue == null)
                            {
                                switch (entityAnalysisModelRequestXPath.DataTypeId)
                                {
                                    case 1:
                                        entityAnalysisModelRequestXPath.DefaultValue = "default";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default String value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 2:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Integer value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 3:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Float value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 4:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Date value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 5:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Boolean value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 6:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Latitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                    case 7:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Longitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");

                                        break;
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.DefaultValue = record.DefaultValue;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Default value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                            }

                            if (!record.SearchKey.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKey = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKey = record.SearchKey.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelRequestXPath.ResponsePayload = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.ResponsePayload = record.ResponsePayload == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                            }

                            if (!record.EnableSuppression.HasValue)
                            {
                                entityAnalysisModelRequestXPath.EnableSuppression = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.EnableSuppression = record.EnableSuppression == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                            }

                            if (!record.SearchKeyCache.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCache = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCache = record.SearchKeyCache.Value == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                            }

                            if (!record.SearchKeyCacheSample.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheSample = false;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheSample = record.SearchKeyCacheSample == 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                            }

                            if (record.SearchKeyCacheInterval == null)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheInterval = "h";

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheInterval =
                                    record.SearchKeyCacheInterval;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                            }

                            if (!record.SearchKeyCacheValue.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheValue = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheValue =
                                    record.SearchKeyCacheValue.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                            }

                            if (!record.SearchKeyCacheTtlValue.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue = 1;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue =
                                    record.SearchKeyCacheTtlValue.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                            }

                            if (!record.SearchKeyCacheFetchLimit.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit = 0;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit =
                                    record.SearchKeyCacheFetchLimit.Value;

                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                            }

                            var databaseType = entityAnalysisModelRequestXPath.DataTypeId switch
                            {
                                2 => "::int",
                                3 => "::float8",
                                4 => "::timestamp",
                                5 => "::boolean",
                                6 => "::float8",
                                7 => "::float8",
                                _ => ""
                            };

                            cachePayloadSql +=
                                $",(\"Json\" ->> '{entityAnalysisModelRequestXPath.Name}'){databaseType} AS \"{entityAnalysisModelRequestXPath.Name}\"";

                            archivePayloadSql +=
                                $",(\"Json\" -> 'payload' ->> '{entityAnalysisModelRequestXPath.Name}'){databaseType} AS \"{entityAnalysisModelRequestXPath.Name}\"";

                            shadowEntityAnalysisModelRequestXPath.Add(entityAnalysisModelRequestXPath);

                            Log.Debug(
                                $"Entity Start: XPath ID {entityAnalysisModelRequestXPath.Id} added to shadow list for model {key}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: XPath ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }

                value.CachePayloadSql = cachePayloadSql + " From \"CachePayload\" where \"EntityAnalysisModelId\" = "
                                                        + value.Id +
                                                        " and \"Json\" ->> (@key) = (@value) "
                                                        + " order by (@order) limit (@limit);";

                value.ArchivePayloadSql = archivePayloadSql + " From \"Archive\" where \"EntityAnalysisModelId\" = "
                                                            + value.Id
                                                            + " and \"ReferenceDate\" >= (@adjustedStartDate) " +
                                                            "order by \"Id\" asc limit (@limit) offset (@skip);";

                value.EntityAnalysisModelRequestXPaths = shadowEntityAnalysisModelRequestXPath;

                Log.Debug(
                    $"Entity Start: Shadow XPath list set to list of xpath for model {key} and reader closed.");
            }

            Log.Debug("Entity Start: Completed adding XPath to entity models.");
        }

        private void SyncEntityAnalysisModels(DbContext dbContext, int tenantRegistryId)
        {
            Log.Debug("Entity Start: Getting all Entity Models from Database.");

            var repository = new EntityAnalysisModelRepository(dbContext, tenantRegistryId);

            Log.Debug(
                "Entity Start: Executing EntityAnalysisModelRepository.Get.");

            var records = repository.Get();

            foreach (var record in records)
                try
                {
                    Log.Debug(
                        $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active.");

                    if (record.Active == 1)
                    {
                        Log.Debug(
                            $"Entity Start: Model {record.Id} has been returned, is active. Proceeding to build model.");

                        EntityAnalysisModel entityAnalysisModel;

                        Log.Debug(
                            $"Entity Start: Checking to see if Model {record.Id} exists in the list of Active Models.");

                        if (!ActiveEntityAnalysisModels.ContainsKey(record.Id))
                        {
                            Log.Debug(
                                $"Entity Start: Model {record.Id} does not exist in the list of Active Models and is being created.");

                            entityAnalysisModel = new EntityAnalysisModel
                            {
                                EntityAnalysisInstanceGuid = _entityAnalysisInstanceGuid,
                                SanctionsEntries = SanctionsEntries,
                                Log = Log,
                                PersistToActivationWatcherAsync = PersistToActivationWatcherAsync,
                                PendingTagging = PendingTagging,
                                JubeEnvironment = JubeEnvironment,
                                ContractResolver =  ContractResolver
                            };
                        }
                        else
                        {
                            entityAnalysisModel = ActiveEntityAnalysisModels[record.Id];

                            Log.Debug(
                                $"Entity Start: Model {record.Id} does exist in the list of Active Models and is being updated.");
                        }

                        entityAnalysisModel.Id = record.Id;
                        Log.Debug(
                            $"Entity Start: Model {record.Id} with Entity Analysis Model ID value {entityAnalysisModel.Id}.");

                        if (record.Name == null)
                        {
                            entityAnalysisModel.Name = "";

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Name value of {entityAnalysisModel.Name}.");
                        }
                        else
                        {
                            entityAnalysisModel.Name = record.Name.Replace(" ", "_");

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Name value of {entityAnalysisModel.Name}.");
                        }

                        if (record.EntryXPath == null)
                        {
                            entityAnalysisModel.EntryXPath = "";

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Entry XPath value of {entityAnalysisModel.EntryXPath}.");
                        }
                        else
                        {
                            entityAnalysisModel.EntryXPath = record.EntryXPath;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Entry XPath value of {entityAnalysisModel.EntryXPath}.");
                        }

                        if (record.ReferenceDateXPath == null)
                        {
                            entityAnalysisModel.ReferenceDateXpath = "";

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Date XPath value of {entityAnalysisModel.ReferenceDateXpath}.");
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDateXpath = record.ReferenceDateXPath;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date XPath value of {entityAnalysisModel.ReferenceDateXpath}.");
                        }

                        if (record.EntryName == null)
                        {
                            entityAnalysisModel.EntryName = "";
                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Entry Name value of {entityAnalysisModel.EntryName}.");
                        }
                        else
                        {
                            entityAnalysisModel.EntryName = record.EntryName;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Entry Name value of {entityAnalysisModel.EntryName}.");
                        }

                        if (record.ReferenceDateName == null)
                        {
                            entityAnalysisModel.ReferenceDateName = "";

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Data Name value of {entityAnalysisModel.ReferenceDateName}.");
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDateName = record.ReferenceDateName;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date Name value of {entityAnalysisModel.ReferenceDateName}.");
                        }

                        if (record.ReferenceDatePayloadLocationTypeId.HasValue)
                        {
                            entityAnalysisModel.ReferenceDatePayloadLocationTypeId =
                                record.ReferenceDatePayloadLocationTypeId.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date Payload Location of {entityAnalysisModel.ReferenceDatePayloadLocationTypeId}.");
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDatePayloadLocationTypeId = 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Date Payload Location of {entityAnalysisModel.ReferenceDatePayloadLocationTypeId}.");
                        }

                        if (record.EnableCache.HasValue)
                        {
                            entityAnalysisModel.EnableCache = record.EnableCache == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Allow Entity Cache of {entityAnalysisModel.EnableCache}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableCache = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Allow Entity Cache of {entityAnalysisModel.EnableCache}.");
                        }

                        if (record.EnableActivationWatcher.HasValue)
                        {
                            entityAnalysisModel.EnableActivationWatcher = record.EnableActivationWatcher == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Activation Watcher of {entityAnalysisModel.EnableActivationWatcher}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableActivationWatcher = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Activation Watcher of {entityAnalysisModel.EnableActivationWatcher}.");
                        }

                        if (record.EnableResponseElevationLimit.HasValue)
                        {
                            entityAnalysisModel.EnableResponseElevationLimit = record.EnableResponseElevationLimit == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Response Elevation Limit of {entityAnalysisModel.EnableResponseElevationLimit}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableResponseElevationLimit = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Response Elevation Limit of {entityAnalysisModel.EnableResponseElevationLimit}.");
                        }

                        if (record.EnableTtlCounter.HasValue)
                        {
                            entityAnalysisModel.EnableTtlCounter = record.EnableTtlCounter == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Ttl Counter cache of {entityAnalysisModel.EnableTtlCounter}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableTtlCounter = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Ttl Counter cache of {entityAnalysisModel.EnableTtlCounter}.");
                        }

                        if (record.EnableSanctionCache.HasValue)
                        {
                            entityAnalysisModel.EnableSanctionCache = record.EnableSanctionCache == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Sanction Cache cache of {entityAnalysisModel.EnableSanctionCache}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableSanctionCache = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Sanction Cache of {entityAnalysisModel.EnableSanctionCache}.");
                        }

                        if (record.CacheFetchLimit.HasValue)
                        {
                            entityAnalysisModel.CacheTtlLimit = record.CacheFetchLimit.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Case TTL Limit of {entityAnalysisModel.CacheTtlLimit}.");
                        }
                        else
                        {
                            entityAnalysisModel.CacheTtlLimit = 100;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Case TTL Limit of {entityAnalysisModel.CacheTtlLimit}.");
                        }

                        if (record.MaxResponseElevation.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevation = record.MaxResponseElevation.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with Max Response Elevation of {entityAnalysisModel.MaxResponseElevation}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevation = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Max Response Elevation of {entityAnalysisModel.MaxResponseElevation}.");
                        }

                        if (record.Guid != Guid.Empty)
                        {
                            entityAnalysisModel.Guid = record.Guid;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Entity Analysis Model GUID value {entityAnalysisModel.Guid}.");
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Entity Analysis Model GUID is empty.");
                        }

                        if (record.TenantRegistryId.HasValue)
                        {
                            entityAnalysisModel.TenantRegistryId = record.TenantRegistryId.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Tenant Registry ID value {entityAnalysisModel.TenantRegistryId}.");
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Tenant Registry ID is empty,  which it cannot be.");
                        }

                        if (record.MaxResponseElevationInterval.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationInterval =
                                record.MaxResponseElevationInterval.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Interval value {entityAnalysisModel.MaxResponseElevationInterval}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationInterval = 'n';

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Interval value {entityAnalysisModel.MaxResponseElevationInterval}.");
                        }

                        if (record.MaxResponseElevationValue.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationValue = record.MaxResponseElevationValue.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Value {entityAnalysisModel.MaxResponseElevationValue}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationValue = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Value {entityAnalysisModel.MaxResponseElevationValue}.");
                        }

                        if (record.MaxResponseElevationThreshold.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationThreshold =
                                record.MaxResponseElevationThreshold.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Threshold Value {entityAnalysisModel.MaxResponseElevationThreshold}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationThreshold = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Threshold Value {entityAnalysisModel.MaxResponseElevationThreshold}.");
                        }

                        if (record.MaxActivationWatcherInterval.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherInterval =
                                record.MaxActivationWatcherInterval.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher Interval value {entityAnalysisModel.MaxActivationWatcherInterval}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherInterval = 'n';

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher Interval value {entityAnalysisModel.MaxActivationWatcherInterval}.");
                        }

                        if (record.MaxActivationWatcherValue.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherValue = record.MaxActivationWatcherValue.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher value {entityAnalysisModel.MaxActivationWatcherValue}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherValue = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher value {entityAnalysisModel.MaxActivationWatcherValue}.");
                        }

                        if (record.MaxActivationWatcherThreshold.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherThreshold =
                                record.MaxActivationWatcherThreshold.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher Threshold value {entityAnalysisModel.MaxActivationWatcherThreshold}.");
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherThreshold = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher Threshold value {entityAnalysisModel.MaxActivationWatcherThreshold}.");
                        }

                        if (record.ActivationWatcherSample.HasValue)
                        {
                            entityAnalysisModel.ActivationWatcherSample = record.ActivationWatcherSample.Value;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Activation Watcher Sample value {entityAnalysisModel.ActivationWatcherSample}.");
                        }
                        else
                        {
                            entityAnalysisModel.ActivationWatcherSample = 0;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Activation Watcher Sample value {entityAnalysisModel.ActivationWatcherSample}.");
                        }

                        if (record.EnableActivationArchive.HasValue)
                        {
                            entityAnalysisModel.EnableActivationArchive = record.EnableActivationArchive.Value == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Promote Activation Archive value {entityAnalysisModel.EnableActivationArchive}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableActivationArchive = false;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Promote Activation Archive value {entityAnalysisModel.EnableActivationArchive}.");
                        }

                        if (record.EnableRdbmsArchive.HasValue)
                        {
                            entityAnalysisModel.EnableRdbmsArchive = record.EnableRdbmsArchive.Value == 1;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with Enable Database value {entityAnalysisModel.EnableRdbmsArchive}.");
                        }
                        else
                        {
                            entityAnalysisModel.EnableRdbmsArchive = true;

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Enable Database value {entityAnalysisModel.EnableRdbmsArchive}.");
                        }

                        if (!ActiveEntityAnalysisModels.ContainsKey(entityAnalysisModel.Id))
                        {
                            ActiveEntityAnalysisModels.Add(entityAnalysisModel.Id, entityAnalysisModel);

                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} does not exist in the list of active models,  hence it has just been added.");
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Start: Model {entityAnalysisModel.Id} already exists,  hence it has just been updated.");
                        }
                    }
                    else
                    {
                        if (ActiveEntityAnalysisModels.ContainsKey(record.Id))
                        {
                            ActiveEntityAnalysisModels.Remove(record.Id);

                            Log.Debug(
                                $"Entity Start: Model {record.Id} already exists but is marked as inactive,  hence it has just been removed from the list of active models.");
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Start: Model {record.Id} is marked as inactive but it does not exist in the list in of Active Models.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active has created an error as {ex}.");
                }

            Log.Debug("Entity Start: Executed database procedures to get all models.");
        }

        private void HeartbeatThisModel(DbContext dbContext, int tenantRegistryId)
        {
            var repository = new EntityAnalysisModelSyncronisationNodeStatusEntryRepository(dbContext);

            var upsert = new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                TenantRegistryId = tenantRegistryId,
                Instance = Dns.GetHostName()
            };

            repository.UpsertHeartbeat(upsert);
        }

        private void CompileInlineScripts(string strPathBinary, string strPathFramework, DbContext dbContext)
        {
            Log.Debug("Entity Start: Getting all Inline Scripts from Database.");

            var repository = new EntityAnalysisInlineScriptRepository(dbContext);

            Log.Debug(
                "Entity Start: Executing EntityAnalysisInlineScriptRepository.Get.");

            var records = repository.Get();

            foreach (var record in records)
                try
                {
                    Log.Debug(
                        $"Entity Start: Found an inline script with the id of {record.Id} and will proceed to check if already have this inline script available.");

                    var inlineScript = _inlineScripts.Find(x => x.InlineScriptId == record.Id);
                    if (inlineScript == null)
                    {
                        inlineScript = new EntityAnalysisModelInlineScript();

                        Log.Debug(
                            $"Entity Start: Have not found an inline script in the available inline scripts, with the id of {record.Id} hence a new one will be created.");
                    }
                    else
                    {
                        Log.Debug(
                            $"Entity Start: Found an inline script in the available inline scripts, with the id of {record.Id} and this will be used.");
                    }

                    Log.Debug(
                        $"Entity Start: Found an inline script {record.Id} has Created Date: {inlineScript.CreatedDate.HasValue} of {inlineScript.CreatedDate}. A check will be made to see if it has changed recently");

                    if (inlineScript.CreatedDate.HasValue &&
                        Convert.ToDateTime(record.CreatedDate) > inlineScript.CreatedDate ||
                        !inlineScript.CreatedDate.HasValue)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has changed recently or is new,  setting created date.");

                        inlineScript.CreatedDate = record.CreatedDate;
                        inlineScript.InlineScriptId = record.Id;
                        inlineScript.InlineScriptCode = record.Code + "\r";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public Class SearchKey\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public CacheKey As Boolean\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public CacheKeyIntervalType As String\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public CacheKeyIntervalValue As Integer\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public FetchLimit As Integer\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public CacheKeyIntervalTTLType As String\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public CacheKeyIntervalTTLValue As Integer\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public Class ResponsePayload\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}Public Class Latitude\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public Class Longitude\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public Class EncryptAtRest\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Public Class ReportTable\r\n";
                        inlineScript.InlineScriptCode =
                            $"{inlineScript.InlineScriptCode}Inherits System.Attribute\r\n";
                        inlineScript.InlineScriptCode = $"{inlineScript.InlineScriptCode}End Class\r\n";

                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");

                        inlineScript.MethodName = record.MethodName;

                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.MethodName}.");

                        inlineScript.ClassName = record.ClassName;

                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.ClassName}.");

                        inlineScript.Name = record.Name;

                        Log.Debug($"Entity Start: Inline Script {record.Id} has name of {inlineScript.Name}.");

                        var dependencyArray = new string[1];
                        dependencyArray[0] = Path.Combine(strPathBinary, "log4net.dll");

                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} is being checked for dll dependencies.");

                        if (!String.IsNullOrEmpty(record.Dependency))
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has dll dependency specification of {record.Dependency}.");

                            inlineScript.Dependencies = record.Dependency;
                            
                            foreach (var file in inlineScript.Dependencies.Split(",".ToCharArray()))
                            {
                                Array.Resize(ref dependencyArray, dependencyArray.Length + 1);
                                if (File.Exists(Path.Combine(strPathBinary, file)))
                                {
                                    dependencyArray[^1] = Path.Combine(strPathBinary, file);
                                    Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at binary level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                                else
                                {
                                    dependencyArray[^1] = Path.Combine(strPathFramework, file);
                                    Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at framework level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                            }
                        }

                        var inlineScriptHash = Hash.GetHash(inlineScript.InlineScriptCode);

                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and the hash cache will now be checked.");

                        if (HashCacheAssembly.ContainsKey(inlineScriptHash))
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache,  this will be used.  Creating a delegate.");

                            inlineScript.InlineScriptCompile = HashCacheAssembly[inlineScriptHash];
                            inlineScript.InlineScriptType =
                                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                            inlineScript.PreProcessingMethodInfo =
                                inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                    new[] {typeof(Dictionary<string, object>), typeof(ILog)});

                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache and allocated to a delegate.");
                        }
                        else
                        {
                            bool compiled;

                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} but has not been located in the hash cache.  The inline script will now be compiled.");

                            var compile = new Compile();
                            compile.CompileCode(inlineScript.InlineScriptCode, Log, dependencyArray);

                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");

                            if (compile.Errors == 0)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled with no errors and will now be allocated to a delegate.");
                                inlineScript.InlineScriptCompile = compile.CompiledAssembly;
                                inlineScript.InlineScriptType =
                                    inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                                inlineScript.PreProcessingMethodInfo =
                                    inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                        new[] {typeof(Dictionary<string, object>), typeof(ILog)});

                                _inlineScripts.Add(inlineScript);
                                HashCacheAssembly.Add(inlineScriptHash, compile.CompiledAssembly);
                                compiled = true;

                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled and allocated to a delegate.");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Entity Start: Could not compile inline script: {inlineScript.InlineScriptCode}.");

                                compiled = false;
                            }

                            if (compiled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled will now proceed to inspect the properties exposed by the class.");

                                var searchKeyAttributesPropertyInfo = inlineScript.InlineScriptCompile.GetTypes()
                                    .SelectMany(t => t.GetProperties()).ToArray();
                                foreach (var propertyInfoWithinLoop in searchKeyAttributesPropertyInfo)
                                {
                                    Log.Debug(
                                        $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and looking for custom attributes.");

                                    foreach (var customAttributeDataWithinLoop in propertyInfoWithinLoop
                                                 .CustomAttributes)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and is inspecting custom attribute {customAttributeDataWithinLoop.AttributeType.Name}.");

                                        switch (customAttributeDataWithinLoop.AttributeType.Name)
                                        {
                                            case "SearchKey":
                                            {
                                                Log.Debug(
                                                    $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key.");

                                                var groupingKey = new DistinctSearchKey
                                                    {SearchKey = propertyInfoWithinLoop.Name};

                                                foreach (var customAttributeNamedArgument in
                                                         customAttributeDataWithinLoop
                                                             .NamedArguments)
                                                {
                                                    var customAttributeTypedArgument =
                                                        customAttributeNamedArgument.TypedValue;
                                                    switch (customAttributeNamedArgument.MemberName)
                                                    {
                                                        case "CacheKey":
                                                            Log.Debug(
                                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKey with value of {customAttributeTypedArgument.Value}.");

                                                            groupingKey.SearchKeyCache =
                                                                Convert.ToBoolean(customAttributeTypedArgument.Value);
                                                            break;
                                                        case "CacheKeyIntervalType":
                                                            Log.Debug(
                                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalType with value of {customAttributeTypedArgument.Value}.");

                                                            groupingKey.SearchKeyCacheIntervalType =
                                                                customAttributeTypedArgument.Value.ToString();
                                                            break;
                                                        case "CacheKeyIntervalValue":
                                                            Log.Debug(
                                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalValue with value of {customAttributeTypedArgument.Value}.");

                                                            groupingKey.SearchKeyCacheIntervalValue =
                                                                Convert.ToInt32(customAttributeTypedArgument.Value);
                                                            break;
                                                    }
                                                }

                                                inlineScript.GroupingKeys.Add(groupingKey);

                                                Log.Debug(
                                                    $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key and is adding the grouping key to the model.");

                                                break;
                                            }
                                            case "ReportTable":
                                            {
                                                var columnName = propertyInfoWithinLoop.Name;
                                                int columnType;

                                                if (propertyInfoWithinLoop.PropertyType == typeof(string))
                                                    columnType = 1;

                                                else if (propertyInfoWithinLoop.PropertyType == typeof(int))
                                                    columnType = 2;

                                                else if (propertyInfoWithinLoop.PropertyType == typeof(double))
                                                    columnType = 3;

                                                else if (propertyInfoWithinLoop.PropertyType ==
                                                         typeof(DateTime))
                                                    columnType = 4;

                                                else if (propertyInfoWithinLoop.PropertyType == typeof(bool))
                                                    columnType = 5;

                                                else
                                                    columnType = 1;

                                                if (!inlineScript.PromoteReportTableColumns.ContainsKey(
                                                        "ColumnName"))
                                                    inlineScript.PromoteReportTableColumns.Add(columnName,
                                                        columnType);

                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                inlineScript.ActivatedObject =
                                    Activator.CreateInstance(inlineScript.InlineScriptType, Log);

                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been created and the method referenced.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");
                }

            Log.Debug("Entity Start:  Completed creating Inline Scripts and closed the reader.");
        }

        private List<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.Dto> GetTenantRegistrySchedule(
            DbContext dbContext)
        {
            Log.Debug("Entity Start: Executing a fetch of all tenant schedules for the entity sub system using GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");

            var query = new GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(dbContext);
            var values = query.Execute(Dns.GetHostName()).ToList();

            Log.Debug(
                "Entity Start: Executed GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");

            return values;
        }

        private DateTime AddSearchKeyCacheServerTime(DateTime currentDate)
        {
            var value = JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalType") switch
            {
                "n" => currentDate.AddMinutes(
                    int.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "h" => currentDate.AddHours(
                    int.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "d" => currentDate.AddDays(int.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "m" => currentDate.AddMonths(
                    int.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                _ => currentDate.AddHours(1)
            };

            return value;
        }

        private void AbstractionRuleCaching()
        {
            while (!_stopping)
                try
                {
                    foreach (var (key, value) in
                             from modelEntityKvp in ActiveEntityAnalysisModels
                             where modelEntityKvp.Value.Started
                             where AddSearchKeyCacheServerTime(modelEntityKvp.Value.LastModelSearchKeyCacheWritten) <
                                   DateTime.Now
                             select modelEntityKvp)
                    {
                        if (!value.HasCheckedDatabaseForLastSearchKeyCacheDates)
                        {
                            Log.Debug(
                                $"Entity Abstraction Rule Caching: The startup routine has not run for model {key} and the last dates will be fetched from the database.");

                            var dbContext =
                                DataConnectionDbContext.GetDbContextDataConnection(
                                    JubeEnvironment.AppSettings("ConnectionString"));
                            try
                            {
                                var query =
                                    new GetEntityAnalysisModelsSearchKeyCalculationInstancesLastSearchKeyDates(
                                        dbContext);

                                Log.Debug(
                                    $"Entity Abstraction Rule Caching: Is about to lookup the last date the search keys were run for model {key}.");

                                var records = query.Execute(key);

                                Log.Debug(
                                    $"Entity Abstraction Rule Caching: Has executed {key} to look up search keys last executed date.");

                                foreach (var record in records)
                                    if (record.SearchKey != null)
                                    {
                                        if (record.DistinctFetchToDate.HasValue)
                                        {
                                            value.LastAbstractionRuleCache.Add(
                                                record.SearchKey ?? string.Empty, record.DistinctFetchToDate.Value);

                                            Log.Debug(
                                                $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} has been added with a Distinct_Fetch_To_Date of {record.DistinctFetchToDate.Value}.");
                                        }
                                        else
                                        {
                                            Log.Debug(
                                                $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} is null and being stepped over during last distinct fetch dates.");
                                        }
                                    }
                                    else
                                    {
                                        Log.Debug(
                                            "Entity Abstraction Rule Caching: Search Key grouped null and is being stepped over during last distinct fetch dates.");
                                    }

                                Log.Debug(
                                    $"Entity Abstraction Rule Caching: Has finished searching for last date the search keys were run for model {key}.");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(
                                    $"Entity Abstraction Rule Caching: Error while fetching the last search key cache dates as {ex}.");
                            }
                            finally
                            {
                                value.HasCheckedDatabaseForLastSearchKeyCacheDates = true;

                                dbContext.Close();
                                dbContext.Dispose();
                            }
                        }
                        else
                        {
                            Log.Debug(
                                $"Entity Abstraction Rule Caching: The startup of has run before for model {key} and the database is not being checked for the last date on search key cache.");
                        }

                        Log.Debug(
                            $"Entity Abstraction Rule Caching: Entity Model {key} is being started.");

                        value.AbstractionRuleCaching();

                        Log.Debug($"Entity Abstraction Rule Caching: Entity Model {key} has finished.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                finally
                {
                    Thread.Sleep(10000);
                }
        }

        private void EntityReprocessing()
        {
            try
            {
                var lastReferenceDate = default(DateTime);
                long allCount = 0;
                var adjustedStartDate = default(DateTime);
                var lastUpdated = default(DateTime);

                while (!_stopping)
                {
                    var dbContext =
                        DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
                    try
                    {
                        Log.Debug("Entity Reprocessing:  About to make a database connection.");
                        
                        Log.Debug(
                            "Entity Reprocessing:  Has made a database connection.  Will now proceed to loop around the models and see if there are any reprocessing requests.");

                        foreach (var modelKvp in ActiveEntityAnalysisModels)
                            try
                            {
                                Log.Debug(
                                    $"Entity Reprocessing:  Has found model id {modelKvp.Key}.  Will now check to see if the model has been started.");

                                if (modelKvp.Value.Started)
                                {
                                    GetEntityAnalysisModelRuleReprocessingInstance(dbContext, modelKvp,
                                        out var entityAnalysisModelRuleReprocessingInstance, out var foundInstance);

                                    if (foundInstance)
                                    {
                                        Log.Info(
                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} using created date.");

                                        var documentsInitialCounts =
                                            GetInitialCounts(entityAnalysisModelRuleReprocessingInstance);

                                        EstablishProcessingDateRange(entityAnalysisModelRuleReprocessingInstance,
                                            documentsInitialCounts, ref lastReferenceDate, ref allCount,
                                            ref adjustedStartDate);

                                        UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCount(dbContext,
                                            entityAnalysisModelRuleReprocessingInstance, lastReferenceDate, allCount);

                                        var limit = int.Parse(JubeEnvironment.AppSettings("ReprocessingBulkLimit"));

                                        Log.Info(
                                            $"Entity Reprocessing:  Is about to build up the cache filter for instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} reprocessing bulk limit has been set to {limit}.");

                                        Log.Info(
                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created a filter between {adjustedStartDate} and {lastReferenceDate}.");

                                        var sampled = 0;
                                        var matched = 0;
                                        var processed = 0;
                                        var errors = 0;
                                        var deleted = false;

                                        var archiveDatabase =
                                            new Postgres(JubeEnvironment.AppSettings("ConnectionString"));
                                        do
                                        {
                                            Log.Info(
                                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to run a query on cache to bring back all document for filter,  skipping {processed} and limiting {limit}.");

                                            var documents =
                                                archiveDatabase.ExecuteReturnPayloadFromArchiveWithSkipLimit(
                                                    modelKvp.Value.ArchivePayloadSql, adjustedStartDate, processed,
                                                    limit);

                                            if (documents.Count == 0)
                                            {
                                                break;
                                            }

                                            foreach (var entry in documents)
                                            {
                                                try
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to process document {processed}.");

                                                    if (entityAnalysisModelRuleReprocessingInstance
                                                            .ReprocessingSample >= Seeded.NextDouble())
                                                    {
                                                        Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and it has passed a random sample.  Will now test the rule.");

                                                        sampled += 1;

                                                        var entityInstanceEntryDictionaryKvPs =
                                                            new Dictionary<string, double>();

                                                        if (entityAnalysisModelRuleReprocessingInstance
                                                            .ReprocessingRuleCompileDelegate(entry,
                                                                modelKvp.Value.EntityAnalysisModelLists,
                                                                entityInstanceEntryDictionaryKvPs, Log))
                                                        {
                                                            lastReferenceDate =
                                                                Convert.ToDateTime(
                                                                    entry[modelKvp.Value.ReferenceDateName]);

                                                            InvokeReprocessingForDocument(modelKvp,
                                                                entityAnalysisModelRuleReprocessingInstance, processed,
                                                                entry, ref matched);
                                                        }
                                                        else
                                                        {
                                                            Log.Info(
                                                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has not passed the rule.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has failed to obtain a random digit and as been sampled out.");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    errors += 1;

                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and has had an error {ex}.");
                                                }
                                                finally
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has finished processing {processed}.");

                                                    processed += 1;
                                                }

                                                if (lastUpdated <= DateTime.Now.AddSeconds(-10))
                                                {
                                                    if (LogAndGetTerminate(dbContext,
                                                            entityAnalysisModelRuleReprocessingInstance,
                                                            processed, sampled, matched, errors, lastReferenceDate))
                                                    {
                                                        Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been removed, stopping process.");

                                                        deleted = true;

                                                        break;
                                                    }

                                                    lastUpdated = DateTime.Now;
                                                }
                                                else
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} not updated database as time interval not passed.");
                                                }
                                            }

                                            deleted = LogAndGetTerminate(dbContext,
                                                entityAnalysisModelRuleReprocessingInstance,
                                                processed, sampled, matched, errors, lastReferenceDate);
                                        } while (!deleted);

                                        FinishReprocessBatchChunk(dbContext,
                                            entityAnalysisModelRuleReprocessingInstance);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Entity Reprocessing: {ex}");
                            }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Entity Reprocessing: {ex}");
                    }
                    finally
                    {
                        Log.Info(
                            "Entity Reprocessing: Has finished a cycle and will now sleep for 20 seconds,  the database connection to Database will also be closed.");

                        dbContext.Close();
                        dbContext.Dispose();

                        Thread.Sleep(20000);

                        Log.Info("Entity Reprocessing: Awake again.");
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Entity Reprocessing: Error outside of loop {ex}");
            }
        }

        private bool LogAndGetTerminate(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            int sampled, int matched, int errors, DateTime referenceDate)
        {
            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to report back status to the database.");

            var deleted = false;
            try
            {
                var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

                repository.UpdateCounts(
                    entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                    sampled, matched, processed, errors, referenceDate);
            }
            catch
            {
                deleted = true;
            }

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated database with processed {processed}, Sampled {sampled}, Matched {matched}, Errors{errors}.");

            return deleted;
        }

        private void InvokeReprocessingForDocument(KeyValuePair<int, EntityAnalysisModel> modelKvp,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            IDictionary<string, object> entry, ref int matched)
        {
            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and matched the rule and will now invoke.");

            matched += 1;

            var stopwatch = new Stopwatch();

            ExtractModelFieldsForInvocation(entityAnalysisModelRuleReprocessingInstance, entry,
                out var entityInstanceEntryPayloadStore, modelKvp, out var cachePayloadDocumentStore,
                out var entityModelInvoke, out var cachePayloadDocumentResponse);

            ExtractRequestXPathForInvocation(entityAnalysisModelRuleReprocessingInstance, entry, modelKvp,
                cachePayloadDocumentStore);

            FinaliseAndInvokeForReprocess(entityAnalysisModelRuleReprocessingInstance, entityInstanceEntryPayloadStore,
                modelKvp, cachePayloadDocumentStore, entityModelInvoke,
                cachePayloadDocumentResponse, stopwatch);
        }

        private void FinaliseAndInvokeForReprocess(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayloadStore,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            Dictionary<string, object> cachePayloadDocumentStore,
            EntityAnalysisModelInvoke entityAnalysisModelInvoke,
            Dictionary<string, object> cachePayloadDocumentResponse,
            Stopwatch stopwatch)
        {
            var (_, value) = modelKvp;
            entityAnalysisModelInvoke.CachePayloadDocumentStore = cachePayloadDocumentStore;
            entityAnalysisModelInvoke.CachePayloadDocumentResponse = cachePayloadDocumentResponse;
            entityAnalysisModelInvoke.EntityAnalysisModel = value;
            entityAnalysisModelInvoke.Stopwatch = stopwatch;
            entityAnalysisModelInvoke.EntityAnalysisModelInstanceEntryPayloadStore =
                entityAnalysisModelInstanceEntryPayloadStore;
            entityAnalysisModelInvoke.Reprocess = true;
            entityAnalysisModelInvoke.Start();

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has completed the invoke.");
        }

        private void ExtractRequestXPathForInvocation(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            IDictionary<string, object> entry,
            KeyValuePair<int, EntityAnalysisModel> modelKvp, IDictionary<string, object> cachePayloadDocumentStore)
        {
            foreach (var xPath in
                     from xPathLinq in modelKvp.Value.EntityAnalysisModelRequestXPaths
                     where !cachePayloadDocumentStore.ContainsKey(xPathLinq.Name)
                     select xPathLinq)
                if (!entry.ContainsKey(xPath.Name))
                {
                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  XPath {xPath.Name} was not in the original payload.");
                }
                else
                {
                    cachePayloadDocumentStore.Add(xPath.Name, entry[xPath.Name]);

                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  Added {xPath.Name} with value {entry[xPath.Name]} as a report column.");
                }

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has added all request XPath fields,  will now start the invoke.");
        }

        private void ExtractModelFieldsForInvocation(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            IDictionary<string, object> entry,
            out EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayloadStore,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            out Dictionary<string, object> cachePayloadDocumentStore,
            out EntityAnalysisModelInvoke entityAnalysisModelInvoke,
            out Dictionary<string, object> cachePayloadDocumentResponse)
        {
            entityAnalysisModelInstanceEntryPayloadStore = new EntityAnalysisModelInstanceEntryPayload
                {EntityAnalysisModelInstanceEntryGuid = entry["EntityAnalysisModelInstanceEntryGuid"].AsGuid()};

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid}.");

            entityAnalysisModelInvoke = new EntityAnalysisModelInvoke(Log, JubeEnvironment, RabbitMqChannel,PendingNotification, Seeded,
                ActiveEntityAnalysisModels);
            cachePayloadDocumentStore = new Dictionary<string, object>();
            cachePayloadDocumentResponse = new Dictionary<string, object>();

            var (_, value) = modelKvp;
            var modelEntryValue = entry[value.EntryName].ToString();

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. ModelEntryValue is {modelEntryValue}.");

            var referenceDateValue = Convert.ToDateTime(entry[value.ReferenceDateName]);

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. ReferenceDateValue is {referenceDateValue}.");

            entityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId = modelEntryValue;
            entityAnalysisModelInstanceEntryPayloadStore.ReferenceDate = referenceDateValue;

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  Will now add the XPath values by looping through the XPath values configured for this model.");
        }

        private void FinishReprocessBatchChunk(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance)
        {
            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} no documents returned so the work is done.  Is about to update the database to show that the process has completed.");

            var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

            repository.UpdateCompleted(entityAnalysisModelRuleReprocessingInstance
                .EntityAnalysisModelsReprocessingRuleInstanceId);

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} processing completed and database updated.");
        }

        private void UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCount(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            DateTime lastReferenceDate, long allCount)
        {
            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to update initial counts for monitoring as Reference_Date {lastReferenceDate} and Available_Count {allCount}.");

            var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

            repository.UpdateReferenceDateCount(
                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                allCount, lastReferenceDate);

            Log.Info(
                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated initial counts for monitoring as Reference Date {lastReferenceDate} and Available Count {allCount}.");
        }

        private void EstablishProcessingDateRange(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            IEnumerable<Dictionary<string, object>> documents, ref DateTime lastReferenceDate, ref long allCount,
            ref DateTime adjustedStartDate)
        {
            foreach (var cacheDocumentLast in documents)
                if (cacheDocumentLast.Any())
                {
                    lastReferenceDate = Convert.ToDateTime(cacheDocumentLast["Max"]);

                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a last reference date of {lastReferenceDate}.");

                    var firstReferenceDate = Convert.ToDateTime(cacheDocumentLast["Min"]);

                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a first reference date of {firstReferenceDate}.");

                    allCount = cacheDocumentLast["Count"].AsLong();

                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found counts of  {allCount}.  Will now proceed to adjust the date to create a between range.");

                    switch (entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType)
                    {
                        case "d":
                            adjustedStartDate =
                                lastReferenceDate.AddDays(entityAnalysisModelRuleReprocessingInstance
                                    .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched d as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                        case "h":
                            adjustedStartDate =
                                lastReferenceDate.AddHours(entityAnalysisModelRuleReprocessingInstance
                                    .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched h as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                        case "n":
                            adjustedStartDate = lastReferenceDate.AddMinutes(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched n as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                        case "s":
                            adjustedStartDate = lastReferenceDate.AddSeconds(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched s as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                        case "m":
                            adjustedStartDate =
                                lastReferenceDate.AddMonths(entityAnalysisModelRuleReprocessingInstance
                                    .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched m as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                        case "y":
                            adjustedStartDate =
                                lastReferenceDate.AddYears(entityAnalysisModelRuleReprocessingInstance
                                    .ReprocessingIntervalValue * -1);

                            Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched y as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");

                            break;
                    }

                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Lower Date for range is {adjustedStartDate}.  Finished getting initial counts.");

                    break;
                }
                else
                {
                    Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has executed the condition to get counts. Bypassed counts as no records returned.");
                }
        }

        private IEnumerable<Dictionary<string, object>> GetInitialCounts(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance)
        {
            var cachePayloadRepository = new CachePayloadRepository(JubeEnvironment.AppSettings(
                new[] {"CacheConnectionString", "ConnectionString"}), Log);

            return cachePayloadRepository
                .GetInitialCounts(entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelId);
        }

        private void GetEntityAnalysisModelRuleReprocessingInstance(DbContext dbContext,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            out EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            out bool foundInstance)
        {
            var (key, _) = modelKvp;

            Log.Debug(
                $"Entity Reprocessing:  Has found model id {key}.  The model has been started,  so we will check to see if there is a reprocessing instance for the model.");

            var query = new GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(dbContext);

            Log.Debug(
                $"Entity Reprocessing:  Has found model id {key}.  Has executed the reader to find reprocessing instances.");

            var record = query.Execute(key);

            entityAnalysisModelRuleReprocessingInstance = new EntityAnalysisModelRuleReprocessingInstance();
            foundInstance = false;

            if (record != null)
            {
                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelId = record.EntityAnalysisModelId;

                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId
                    = record.Id;

                Log.Debug(
                    $"Entity Reprocessing:  Has found model id {key}.  Has found a reprocessing instance id of {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.");

                if (record.ReprocessingIntervalValue.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue =
                        record.ReprocessingIntervalValue.Value;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue = 1;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                }

                if (record.ReprocessingIntervalType != null)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType =
                        record.ReprocessingIntervalType;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType = "d";

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                }

                if (record.ReprocessingSample.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingSample = record.ReprocessingSample.Value;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to {entityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingSample = 0;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                }

                if (record.RuleScriptTypeId.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to {entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = 1;

                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to DEFAULT {entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                }

                Log.Debug(
                    $"Entity Reprocessing:  Has found model id {key}.  Is loading the rule parser and tokens.");

                var parser = ConfigureTokenParserForSecurity(dbContext);

                Log.Debug(
                    $"Entity Reprocessing:  Has found model id {key}.  Has loaded the rule parser and tokens.");

                if (record.BuilderRuleScript != null &&
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 1)
                {
                    var parsedRule = new ParsedRule
                    {
                        OriginalRuleText = record.BuilderRuleScript,
                        ErrorSpans = new List<ErrorSpan>()
                    };
                    parsedRule = parser.TranslateFromDotNotation(parsedRule);
                    parsedRule = parser.Parse(parsedRule);

                    if (parsedRule.ErrorSpans.Count == 0)
                    {
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                        Log.Debug(
                            $"Entity Reprocessing: {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set builder script as {entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                    }
                }
                else if (record.CoderRuleScript != null &&
                         entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 2)
                {
                    var parsedRule = new ParsedRule
                    {
                        OriginalRuleText = record.CoderRuleScript,
                        ErrorSpans = new List<ErrorSpan>()
                    };
                    parsedRule = parser.TranslateFromDotNotation(parsedRule);
                    parsedRule = parser.Parse(parsedRule);

                    if (parsedRule.ErrorSpans.Count == 0)
                    {
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                        Log.Debug(
                            $"Entity Reprocessing: {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set coder script as {entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                    }
                }

                var gatewayRuleScript = new StringBuilder();
                gatewayRuleScript.Append("Imports System.IO\r\n");
                gatewayRuleScript.Append("Imports log4net\r\n");
                gatewayRuleScript.Append("Imports System.Net\r\n");
                gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                gatewayRuleScript.Append("Imports System\r\n");
                gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                gatewayRuleScript.Append(
                    "Public Shared Function Match(Data As IDictionary(Of string,object), List As Dictionary(Of String, List(Of String)),KVP As Dictionary(Of String, Double),Log As ILog) As Boolean\r\n");
                gatewayRuleScript.Append("Dim Matched As Boolean\r\n");
                gatewayRuleScript.Append("Try\r\n");
                gatewayRuleScript.Append(entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript + "\r\n");
                gatewayRuleScript.Append("Catch ex As Exception\r\n");
                gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                gatewayRuleScript.Append("End Try\r\n");
                gatewayRuleScript.Append("Return Matched\r\n");
                gatewayRuleScript.Append("\r\n");
                gatewayRuleScript.Append("End Function\r\n");
                gatewayRuleScript.Append("End Class\r\n");

                Log.Debug(
                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set class wrap as {gatewayRuleScript}.");

                var gatewayRuleScriptHash = Hash.GetHash(gatewayRuleScript.ToString());

                Log.Debug(
                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");

                if (HashCacheAssembly.ContainsKey(gatewayRuleScriptHash))
                {
                    Log.Debug(
                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");

                    entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile =
                        HashCacheAssembly[gatewayRuleScriptHash];
                    var classType =
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");

                    var methodInfo = classType?.GetMethod("Match");
                    if (methodInfo != null)
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                            (EntityAnalysisModelRuleReprocessingInstance.Match) Delegate.CreateDelegate(
                                typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);

                    Log.Debug(
                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");

                    foundInstance = true;
                }
                else
                {
                    Log.Debug(
                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");

                    var codeBase = Assembly.GetExecutingAssembly().Location;

                    Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");

                    var strPathBinary = Path.GetDirectoryName(codeBase);

                    Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");

                    var compile = new Compile();
                    compile.CompileCode(gatewayRuleScript.ToString(), Log,
                        new[] {Path.Combine(strPathBinary ?? throw new InvalidOperationException(), "log4net.dll")});

                    Log.Debug(
                        $"Entity Start: Model {key} and Gateway Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");

                    if (compile.Errors == 0)
                    {
                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");

                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile = compile.CompiledAssembly;

                        var classType =
                            entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");
                        var methodInfo = classType?.GetMethod("Match");
                        if (methodInfo != null)
                            entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                                (EntityAnalysisModelRuleReprocessingInstance.Match) Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);

                        HashCacheAssembly.Add(gatewayRuleScriptHash, compile.CompiledAssembly);

                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");

                        foundInstance = true;
                    }
                    else
                    {
                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                    }
                }
            }
            
            Log.Debug(
                $"Entity Reprocessing:  Has finished loading reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.  Will now proceed to select the counts and date ranges.");
        }

        private void PersistToActivationWatcher()
        {
            var activationWatchers = new List<ActivationWatcher>();
            while (!_stopping)
                try
                {
                    PersistToActivationWatcherAsync.TryDequeue(out var payload);

                    if (payload != null)
                    {
                        Log.Info(
                            "Database Activation Watcher Persist: a message has been received to be persisted to the Database database Activation Watcher.");

                        try
                        {
                            activationWatchers.Add(payload);

                            Log.Info(
                                $"Database Persist: Added record to the data table pending SQL Bulk insert Tenant_Registry_ID {payload.TenantRegistryId},Symbol_Entity_Key {payload.Key},Longitude {payload.Longitude},Latitude {payload.Latitude}, Activation_Rule_Summary {payload.ActivationRuleSummary}, Response_Elevation_Content {payload.ResponseElevationContent}, Response_Elevation {payload.ResponseElevation}, Back_Color {payload.BackColor}, Fore_Color {payload.ForeColor}.");

                            Log.Info(
                                $"Database Activation Watcher Persist: The table count threshold has been set to {activationWatchers.Count} and the bulk copy threshold is {JubeEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")}.");

                            if (activationWatchers.Count >=
                                int.Parse(JubeEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")))
                            {
                                var sw = new Stopwatch();
                                sw.Start();

                                Log.Info(
                                    "Database Activation Watcher Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");

                                var dbContext =
                                    DataConnectionDbContext.GetDbContextDataConnection(
                                        JubeEnvironment.AppSettings("ConnectionString"));

                                Log.Info("Database Activation Watcher Persist: Opened an SQL Bulk Collection.");

                                var repository = new ActivationWatcherRepository(dbContext);

                                Log.Info("Database Activation Watcher Persist: Will proceed to bulk copy.");

                                try
                                {
                                    repository.BulkCopy(activationWatchers);

                                    Log.Info(
                                        $"Database Activation Watcher Persist: The bulk copy has inserted {activationWatchers.Count} Activation Watcher records and cleared the data table.  The time taken is {sw.ElapsedMilliseconds} in ms.");

                                    sw.Reset();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());

                                    sw.Reset();
                                }
                                finally
                                {
                                    activationWatchers.Clear();

                                    dbContext.Close();
                                    dbContext.Dispose();

                                    Log.Info("Database Activation Watcher Persist: Closed an SQL Bulk Collection.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}");
                }
        }

        private void TtlCounterAdministration()
        {
            while (!_stopping)
                try
                {
                    var activeModelsForLoopWithoutEnumError = ActiveEntityAnalysisModels.ToList();
                    foreach (var (key, value) in
                             from modelEntityKvp in activeModelsForLoopWithoutEnumError
                             where modelEntityKvp.Value.Started
                             select modelEntityKvp)
                    {
                        Log.Debug(
                            $"Entity TTL Counter Administration: Entity Model {key} is being started.");

                        value.TtlCounterServer();

                        Log.Debug(
                            $"Entity TTL Counter Administration: Entity Model {key} has finished will wait for {JubeEnvironment.AppSettings("WaitTtlCounterDecrement")} miliseconds.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                finally
                {
                    Thread.Sleep(int.Parse(JubeEnvironment.AppSettings("WaitTtlCounterDecrement")));
                }
        }
    }
}
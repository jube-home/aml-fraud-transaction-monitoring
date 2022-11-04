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
using System.Text;
using Jube.Data.Cache;
using Jube.Data.Context;
using Jube.Data.Extension;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Engine.Helpers;
using Jube.Engine.Invoke.Abstraction;
using Jube.Engine.Invoke.Reflect;
using Jube.Engine.Model.Archive;
using Jube.Engine.Model.Processing;
using Jube.Engine.Model.Processing.Payload;
using Jube.Engine.Sanctions;
using log4net;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Serialization;
using ExhaustiveSearchInstance = Jube.Engine.Model.Exhaustive.ExhaustiveSearchInstance;
using Tag = Jube.Engine.Model.Archive.Tag;

namespace Jube.Engine.Model
{
    public class EntityAnalysisModel
    {
        public delegate MemoryStream Transform(string foreColor, string backColor, double responseElevation,
            string responseContent, string responseRedirect, Dictionary<string,object> entityInstanceEntryPayloadCache,
            Dictionary<string,double> entityInstanceEntryAbstraction, Dictionary<string, int> entityInstanceEntryTtlCounters,
            Dictionary<int, string> entityInstanceEntryActivation,
            Dictionary<string, double> entityInstanceEntryAbstractionCalculations,
            Dictionary<string, int> responseTimes, ILog log);

        public string CachePayloadSql { get; set; }
        public string ArchivePayloadSql { get; set; }
        public DateTime LastModelSearchKeyCacheWritten;
        public ILog Log { get; init; }
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public int Id { get; set; }
        public ConcurrentQueue<Tag> PendingTagging { get; init; } = new();
        public List<EntityAnalysisModelAbstractionRule> ModelAbstractionRules { get; set; } = new();
        public List<EntityAnalysisModelTtlCounter> ModelTtlCounters { get; set; } = new();
        public List<EntityAnalysisModelSanction> EntityAnalysisModelSanctions { get; set; } = new();
        public List<EntityAnalysisModelActivationRule> ModelActivationRules { get; set; } = new();
        public List<EntityModelGatewayRule> ModelGatewayRules { get; set; } = new();
        public List<ExhaustiveSearchInstance> ExhaustiveModels { get; set; } = new();
        public Dictionary<string,DistinctSearchKey> DistinctSearchKeys { get; } = new();
        public string EntryXPath { get; set; }
        public string EntryName { get; set; }
        public string ReferenceDateXpath { get; set; }
        public string ReferenceDateName { get; set; }
        public List<EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths { get; set; } = new();
        public List<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculations { get; set; }
            = new();
        public List<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunctions { get; set; } = new();
        public Dictionary<int, EntityAnalysisModelHttpAdaptation> EntityAnalysisModelAdaptations { get; set; } = new();
        public List<EntityAnalysisModelTag> EntityAnalysisModelTags { get; set; } = new();
        public ConcurrentQueue<ActivationWatcher> PersistToActivationWatcherAsync { get; init; } = new();
        public bool Started { get; set; }
        public Guid EntityAnalysisInstanceGuid { get; set; }
        public Guid EntityAnalysisModelInstanceGuid { get; set; }
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; set; } = new();
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; set; } = new();
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; set; } = new();
        public int CacheTtlLimit { get; set; }
        public Dictionary<string,List<string>> EntityAnalysisModelSuppressionModels { get; set; }
        public Dictionary<string,Dictionary<string, List<string>>> EntityAnalysisModelSuppressionRules { get; set; } = new();
        public byte ReferenceDatePayloadLocationTypeId { get; set; }
        public double MaxResponseElevation { get; set; }
        public int TenantRegistryId { get; set; }
        public double BillingResponseElevationBalance { get; set; }
        public ConcurrentQueue<ResponseElevation> BillingResponseElevationBalanceEntries { get; } =
            new();
        public int ActivationWatcherCount { get; set; }
        public ConcurrentQueue<DateTime> ActivationWatcherCountJournal { get; } = new();
        public double BillingResponseElevationCount { get; set; }
        public ConcurrentQueue<DateTime> BillingResponseElevationJournal { get; } = new();
        public char MaxResponseElevationInterval { get; set; }
        public int MaxResponseElevationValue { get; set; }
        public int MaxResponseElevationThreshold { get; set; }
        public char MaxActivationWatcherInterval { get; set; }
        public int MaxActivationWatcherValue { get; set; }
        public double MaxActivationWatcherThreshold { get; set; }
        public double ActivationWatcherSample { get; set; }
        public DateTime LastCountersChecked { get; set; }
        public DateTime LastCountersWritten { get; set; }
        public int ModelInvokeCounter { get; set; }
        public int ModelInvokeGatewayCounter { get; set; }
        public int ModelResponseElevationCounter { get; set; }
        public double ModelResponseElevationSum { get; set; }
        public int BalanceLimitCounter { get; set; }
        public int ResponseElevationValueLimitCounter { get; set; }
        public int ResponseElevationFrequencyLimitCounter { get; set; }
        public int ResponseElevationValueGatewayLimitCounter { get; set; }
        public int ResponseElevationBillingSumLimitCounter { get; set; }
        public int ParentResponseElevationValueLimitCounter { get; set; }
        public int ParentBalanceLimitCounter { get; set; }
        public DateTime LastModelInvokeCountersWritten { get; set; }
        public bool OutputTransform { get; set; }
        public string FallbackResponseElevationRedirect { get; set; }
        public Transform OutputTransformDelegate { get; set; }
        public DynamicEnvironment.DynamicEnvironment JubeEnvironment { get; init; }
        public bool HasCheckedDatabaseForLastSearchKeyCacheDates { get; set; }
        public Dictionary<string, DateTime> LastAbstractionRuleCache { get; } = new();
        public bool EnableCache { get; set; }
        public bool EnableSanctionCache { get; set; }
        public bool EnableTtlCounter { get; set; }
        public bool EnableActivationArchive { get; set; }
        public bool EnableRdbmsArchive { get; set; }
        public Dictionary<int, SanctionEntryDto> SanctionsEntries { get; init; } = new();
        public ConcurrentQueue<EntityAnalysisModelInstanceEntryPayload> PersistToDatabaseAsync { get; } = new();
        public Dictionary<int, ArchiveBuffer> BulkInsertMessageBuffers { get; } = new();
        public bool EnableActivationWatcher { get; set; } 
        public bool EnableResponseElevationLimit { get; set; } 
        
        public DefaultContractResolver ContractResolver;
        
        public void AbstractionRuleCaching()
        {
            Log.Info(
                "Entity Start: Will try and make a connection to the Database to create the Search Key Cache.");
                
            var dbContext = DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
            
            try
            {                
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} starting to loop around all of the Grouping keys that have been synchronise.");

                var processedGroupingValues = 0;
                foreach (var (key, value) in DistinctSearchKeys)
                {
                    var ready = IsSearchKeyReady(value);
                    if (ready)
                    {
                        var toDate = DateTime.Now;
                        var entityAnalysisModelsSearchKeyCalculationInstanceId =
                            InsertEntityAnalysisModelsSearchKeyCalculationInstances(dbContext, value, toDate);
                        var groupingValues = GetDistinctListOfGroupingValues(value, toDate);

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {groupingValues.Count} grouping values.");

                        UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValues(dbContext,
                            entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValues.Count);
                        
                        var expires = GetExpiredCacheKeys(value);

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {expires.Count} expires values.");

                        UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCount(dbContext,
                            entityAnalysisModelsSearchKeyCalculationInstanceId, expires.Count);
                        groupingValues = AddExpiredToGroupingValues(value, expires, groupingValues);

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {expires.Count} grouping values in total including expires.");

                        if (groupingValues.Count > 0)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {key} has {groupingValues.Count} values.  For each value,  records will be returned using it as key and rules executed against the returned records.");

                            foreach (var groupingValue in groupingValues)
                            {
                                var entityInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload();
                                var abstractionRuleMatches = new Dictionary<int, List<Dictionary<string,object>>>();

                                Log.Info(
                                    $"Abstraction Rule Caching: For model {Id} and grouping key {key} is processing grouping value {groupingValue}.");

                                if (!string.IsNullOrEmpty(groupingValue))
                                {
                                    var entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId =
                                        InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstances(dbContext,
                                            entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValue);

                                    var documents = GetAllForKey(value, groupingValue);

                                    UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCount(
                                        dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                        documents.Count);

                                    abstractionRuleMatches =
                                        ProcessAbstractionRules(value, documents, abstractionRuleMatches);

                                    UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatches(
                                        dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);

                                    Log.Info(
                                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key}, the matches will now be aggregated by looping through each abstraction rule.");

                                    foreach (var abstractionRuleMatch in abstractionRuleMatches)
                                    {
                                        Log.Info(
                                            $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} , the matches will now be aggregated by looping through each abstraction rule.");

                                        try
                                        {
                                            var abstractionRule = ModelAbstractionRules.Find(x =>
                                                x.Id == abstractionRuleMatch.Key);

                                            if (abstractionRule != null)
                                            {
                                                var abstractionValue = GetAggregateValue(value, groupingValue,
                                                    abstractionRuleMatches, abstractionRuleMatch, abstractionRule,
                                                    entityInstanceEntryPayload);
                                                
                                                UpsertOrDeleteSearchKeyCacheValue(value, groupingValue,
                                                    abstractionRuleMatch, abstractionRule, abstractionValue);

                                                if (EnableRdbmsArchive)
                                                {
                                                    ReplicateToDatabase(dbContext,
                                                        entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                                        value, groupingValue, abstractionRule, abstractionValue);   
                                                }
                                            }
                                            else
                                            {
                                                Log.Error(
                                                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key}  could not find full details of the abstraction rule.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(
                                                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} is in error as {ex}.");
                                        }
                                    }

                                    UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompleted(
                                        dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
                                }
                                else
                                {
                                    Log.Error(
                                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} is empty.");
                                }

                                processedGroupingValues += 1;
                                UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCount(
                                    dbContext, entityAnalysisModelsSearchKeyCalculationInstanceId,
                                    processedGroupingValues);
                            }
                        }
                        else
                        {
                            Log.Error(
                                $"Abstraction Rule Caching: For model {Id} and Grouping Key {key} is empty.");
                        }

                        UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompleted(dbContext,
                            entityAnalysisModelsSearchKeyCalculationInstanceId);
                    }
                }

                LastModelSearchKeyCacheWritten = DateTime.Now;

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} has recorded the date of last rule cache.");
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Abstraction Rule Caching: For model {Id} has produced an error as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();
            }
        }
        
        private static int InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstances(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId, string groupingValue)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);
         
            var model = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
                {
                    EntityAnalysisModelSearchKeyCalculationInstanceId = entityAnalysisModelsSearchKeyCalculationInstanceId,
                    SearchKeyValue = groupingValue,
                    CreatedDate = DateTime.Now
                };

            model = repository.Insert(model);
            
            return model.Id;   
        }

        private int InsertEntityAnalysisModelsSearchKeyCalculationInstances(DbContext dbContext, DistinctSearchKey distinctSearchKey,
            DateTime toDate)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            
            var model = new EntityAnalysisModelSearchKeyCalculationInstance
            {
                SearchKey = distinctSearchKey.SearchKey,
                EntityAnalysisModelId = Id,
                DistinctFetchToDate = toDate,
                CreatedDate = DateTime.Now
            };

            repository.Insert(model);
            
            return model.Id;
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValues(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            
            repository.UpdateDistinctValuesCount(entityAnalysisModelsSearchKeyCalculationInstanceId,count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);
            
            repository.UpdateEntriesCount(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatches(
            DbContext dbContext, int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            repository.UpdateAbstractionRuleMatches(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompleted(DbContext dbContext,
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            repository.UpdateCompleted(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
        }
        
        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateExpiredSearchKeyCacheCount(entityAnalysisModelsSearchKeyCalculationInstanceId,count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyCalculationInstanceId,
            int distinctValuesProcessedValuesCount)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateDistinctValuesProcessedValuesCount(entityAnalysisModelsSearchKeyCalculationInstanceId,distinctValuesProcessedValuesCount);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompleted(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateCompleted(entityAnalysisModelsSearchKeyCalculationInstanceId);
        }
        
        private void UpsertOrDeleteSearchKeyCacheValue(DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<Dictionary<string,object>>> abstractionRuleMatch, EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var cacheAbstractionRepository =
                new CacheAbstractionRepository(JubeEnvironment.AppSettings(
                    new []{"CacheConnectionString","ConnectionString"}), Log);
            
            var document = FindCacheKeyValueEntry(cacheAbstractionRepository,
                distinctSearchKey, groupingValue, abstractionRuleMatch, abstractionRule,
                abstractionValue);
            
            if (document == null)
            {
                if (abstractionValue > 0)
                {
                    InsertSearchKeyCacheValue(cacheAbstractionRepository,
                        distinctSearchKey, groupingValue, 
                       abstractionRuleMatch, abstractionRule,
                       abstractionValue);   
                }
            }
            else
            {
                UpdateOrDeleteSearchKeyCacheValue(cacheAbstractionRepository,distinctSearchKey, groupingValue, abstractionRule,
                    abstractionValue, document);
            }
        }

        private static void ReplicateToDatabase(DbContext dbContext,
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, DistinctSearchKey distinctSearchKey,
            string groupingValue, EntityAnalysisModelAbstractionRule abstractionRule, double abstractionValue)
        {
            var repository = new ArchiveEntityAnalysisModelAbstractionEntryRepository(dbContext);

            var model = new ArchiveEntityAnalysisModelAbstractionEntry
                {
                    EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceId = entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                    SearchKey = distinctSearchKey.SearchKey,
                    SearchValue = groupingValue,
                    Value = abstractionValue,
                    EntityAnalysisModelAbstractionRuleId = abstractionRule.Id,
                    CreatedDate = DateTime.Now
                };

            repository.Insert(model);
        }

        private void UpdateOrDeleteSearchKeyCacheValue(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue, EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue, CacheAbstractionRepository.CacheAbstractionIdValueDto document)
        {
            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it will be updated or deleted.");
            
            if (abstractionValue == 0)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it will be deleted to save storage.");
                
                cacheAbstractionRepository.Delete(document.Id);

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it has been deleted to save storage.");
            }
            else
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it will be updated.");

                cacheAbstractionRepository.Update(document.Id,abstractionValue);
                
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it has been updated.");
            }
        }

        private void InsertSearchKeyCacheValue(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<Dictionary<string, object>>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var (key, _) = abstractionRuleMatch;
            Log.Info(
        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}. As there are no existing cache values, it will be inserted.");
        
        cacheAbstractionRepository.Insert(Id,distinctSearchKey.SearchKey,
            groupingValue,abstractionRule.Name,abstractionValue);
        
        Log.Info(
            $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it has been updated.");
        }

        private CacheAbstractionRepository.CacheAbstractionIdValueDto FindCacheKeyValueEntry(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<Dictionary<string,object>>> abstractionRuleMatch, EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var document = cacheAbstractionRepository.GetByNameSearchNameSearchValue(
                Id, abstractionRule.Name, distinctSearchKey.SearchKey, groupingValue);

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRuleMatch.Key}  has returned aggregated value of {abstractionValue}.  The cache has returned for rule name {abstractionRule.Name} with {document == null} null document.  An upsert will now take place.");

            return document;
        }

        private double GetAggregateValue(DistinctSearchKey distinctSearchKey, string groupingValue,
            Dictionary<int, List<Dictionary<string,object>>> abstractionRuleMatches,
            KeyValuePair<int, List<Dictionary<string,object>>> abstractionRuleMatch, EntityAnalysisModelAbstractionRule abstractionRule,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            var (key, _) = abstractionRuleMatch;

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned full details of the abstraction rule.");

            var abstractionValue = EntityAnalysisModelAbstractionRuleAggregator.Aggregate(entityAnalysisModelInstanceEntryPayload, abstractionRuleMatches,
                abstractionRule, Log);

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.");

            return abstractionValue;
        }

        private Dictionary<int, List<Dictionary<string,object>>> ProcessAbstractionRules(DistinctSearchKey distinctSearchKey,
            List<Dictionary<string,object>> documents,
            Dictionary<int, List<Dictionary<string,object>>> abstractionRuleMatches)
        {
            var logicHashMatches = new Dictionary<string, List<Dictionary<string,object>>>();

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} will step through all abstraction rules.");

            foreach (var evaluateAbstractionRule in ModelAbstractionRules)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} has a search type > 1 and matches on teh current grouping key.  The search type is {evaluateAbstractionRule.Search} and the rule grouping key is{evaluateAbstractionRule.SearchKey}.");

                if (evaluateAbstractionRule.Search &&
                    evaluateAbstractionRule.SearchKey == distinctSearchKey.SearchKey)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  As some rule logic can be common across a number of rules,  a check will be made to see if this logic has already been executed using the hash of the rule logic as {evaluateAbstractionRule.LogicHash}.");

                    List<Dictionary<string,object>> matches;
                    if (logicHashMatches.ContainsKey(evaluateAbstractionRule.LogicHash))
                    {
                        matches = logicHashMatches[evaluateAbstractionRule.LogicHash];

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has already been executed so it will simply return the {matches.Count} records already having been matched on this logic.");
                    }
                    else
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has not already been executed so it be filtered using the rule logic.");

                        matches = documents.FindAll(x =>
                            ReflectRule.Execute(evaluateAbstractionRule, this, x, null, null, Log));
                        logicHashMatches.Add(evaluateAbstractionRule.LogicHash, matches);

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has been executed for the first time and returned {matches.Count} records already.  It has been added to the cache using the logic hash as a key.");
                    }

                    var historyThresholdDate =
                        DateAndTime.DateAdd(evaluateAbstractionRule.AbstractionRuleAggregationFunctionIntervalType,
                            evaluateAbstractionRule.AbstractionHistoryIntervalValue * -1, DateTime.Now);

                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash}, {matches.Count} records will be filtered based on the history criteria.  It has been added to the cache using the logic hash as a key.  The interval for the rule logic is {evaluateAbstractionRule.AbstractionHistoryIntervalType} and the value is {evaluateAbstractionRule.AbstractionHistoryIntervalValue}.  Records will be return where the date is between {historyThresholdDate} and now.");

                    var finalMatches = matches.FindAll(x =>
                        x["CreatedDate"].AsDateTime() >= historyThresholdDate && x["CreatedDate"].AsDateTime() <= DateTime.Now);
                    
                    abstractionRuleMatches.Add(evaluateAbstractionRule.Id, new List<Dictionary<string,object>>());
                    abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} and abstraction rule {evaluateAbstractionRule.Id}  has a final number of matches of {finalMatches.Count} and has been added to the list of matches");
                }

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} is moving to the next grouping value.");
            }

            return abstractionRuleMatches;
        }

        private List<Dictionary<string,object>> GetAllForKey(DistinctSearchKey distinctSearchKey, string groupingValue)
        {
           Log.Info(
                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is processing grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit}.");
           
            var cachePayloadRepository = new CachePayloadRepository(JubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),Log);

            List<Dictionary<string,object>> documents;
            if (distinctSearchKey.SearchKeyCacheSample)
            {
                documents = cachePayloadRepository.GetSqlByKeyValueLimit(CachePayloadSql,distinctSearchKey.SearchKey,
                    groupingValue,"RANDOM()",distinctSearchKey.SearchKeyCacheFetchLimit);
                
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {documents.Count} ordered randomly.");
            }
            else
            {
                documents = cachePayloadRepository.GetSqlByKeyValueLimit(CachePayloadSql,distinctSearchKey.SearchKey,
                    groupingValue,"CreatedDate",distinctSearchKey.SearchKeyCacheFetchLimit);

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {documents.Count} ordered by CreatedDate desc.");

                documents.Reverse();
            }

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {documents.Count} having been finalised.");

            return documents;
        }

        private List<string> AddExpiredToGroupingValues(DistinctSearchKey distinctSearchKey, IReadOnlyCollection<string> expires,
            List<string> groupingValues)
        {
            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has returned {expires.Count} cache keys.  These keys will now be added to the list distinct wise.");

            foreach (var expired in expires)
                if (!groupingValues.Contains(expired))
                {
                    groupingValues.Add(expired);

                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has added the value {expired}.");
                }
                else
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has not added the value {expired} as it is a duplicate of one already added.");
                }

            return groupingValues;
        }

        private List<string> GetExpiredCacheKeys(DistinctSearchKey distinctSearchKey)
        {
            if (LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
            {
                var cachePayloadRepository =
                    new CachePayloadRepository(JubeEnvironment.AppSettings(
                        new []{"CacheConnectionString","ConnectionString"}),Log);

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has a interval value of {distinctSearchKey.SearchKeyCacheTtlIntervalValue} and an interval of {distinctSearchKey.SearchKeyCacheIntervalType}.  Calculating the threshold for grouping keys that have expired.");

                var deleteLineCacheKeys = distinctSearchKey.SearchKeyCacheIntervalType switch
                {
                    "s" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                        .AddSeconds(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                    "n" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                        .AddMinutes(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                    "h" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                        .AddHours(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                    "d" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                        .AddDays(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                    _ => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                        .AddDays(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1)
                };

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} the date threshold for cache keys that have expired is {deleteLineCacheKeys}.");

                return cachePayloadRepository.GetDistinctKeys(Id, distinctSearchKey.SearchKey,
                    deleteLineCacheKeys);
            }

            return new List<string>();
        }

        private List<string> GetDistinctListOfGroupingValues(DistinctSearchKey distinctSearchKey, DateTime toDate)
        {
            List<string> value;

            var cachePayloadRepository = new CachePayloadRepository(JubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),Log);
            
            if (LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and will now check if there has been any new records since it was last calculated on {LastAbstractionRuleCache[distinctSearchKey.SearchKey]} then bring back a distinct list of all grouping keys.");
                
                value = cachePayloadRepository.GetDistinctKeys(Id,distinctSearchKey.SearchKey,
                    LastAbstractionRuleCache[distinctSearchKey.SearchKey],toDate);

                LastAbstractionRuleCache[distinctSearchKey.SearchKey] = toDate;

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been added to a collection for grouping key {distinctSearchKey.SearchKey}.");
            }
            else
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and is now bringing back a distinct list of values for the grouping key.");
                
                value = cachePayloadRepository.GetDistinctKeys(Id,distinctSearchKey.SearchKey);
                
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been updated to a collection for grouping key {distinctSearchKey.SearchKey}.");
            }

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has returned {value.Count} records when looking for distinct values for this grouping key.  There is a list of grouping keys to be evaluated,  now a check will be made for those that need to be evaluated again.");

            return value;
        }

        private bool IsSearchKeyReady(DistinctSearchKey distinctSearchKey)
        {
            bool ready;

            Log.Info(
                $"Abstraction Rule Caching: For model {Id} Checking to see if grouping key {distinctSearchKey.SearchKey} is a search key.  It has a search key value of {distinctSearchKey.SearchKeyCache}.");

            if (distinctSearchKey.SearchKeyCache)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} grouping key {distinctSearchKey.SearchKey} is a search key. A check will now be performed to understand when this abstraction rule key was last calculated.");

                if (LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} will calculate the date threshold for this grouping key to be run.  It was last run on {LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey)} and the SearchKey Cache Interval Type is {distinctSearchKey.SearchKeyCacheIntervalType} and the Search Key Cache Interval Value{distinctSearchKey.SearchKeyCacheIntervalValue}.");

                    var dateThreshold = DateAndTime.DateAdd(distinctSearchKey.SearchKeyCacheIntervalType,
                        distinctSearchKey.SearchKeyCacheIntervalValue, LastAbstractionRuleCache[distinctSearchKey.SearchKey]);

                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} should next run on {dateThreshold}.");

                    if (DateTime.Now > dateThreshold)
                    {
                        ready = true;

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as the threshold has lapsed.");
                    }
                    else
                    {
                        ready = false;

                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a unready state as the threshold has not lapsed.");
                    }
                }
                else
                {
                    ready = true;
                }
            }
            else
            {
                ready = false;

                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as it has never been run.");
            }

            return ready;
        }
        
        public void TtlCounterServer()
        {
            try
            {
                Log.Info(
                    $"TTL Counter Administration: has started for {Id}.  Is about to loop around all TTL Counters.");

                foreach (var ttlCounterWithinLoop in ModelTtlCounters)
                    try
                    {
                        Log.Info(
                            $"TTL Counter Administration: has started for {Id} is about to process TTL Counter {ttlCounterWithinLoop.Name} and data name {ttlCounterWithinLoop.TtlCounterDataName}.");

                        //var referenceDate = GetMostRecentFromTtlCounterCache(ttlCounterWithinLoop);

                        var referenceDate = DateTime.Now;

                        var adjustedTtlCounterDate = GetAdjustedTtlCounterDate(ttlCounterWithinLoop, referenceDate);
                            
                        var aggregateList = GetExpiredTtlCounterCacheCounts(ttlCounterWithinLoop,
                            adjustedTtlCounterDate);

                        foreach (var (key, value) in aggregateList)
                        {
                            DecrementTtlCounterCache(ttlCounterWithinLoop, referenceDate,
                                key, value);
                        }
                            
                        DeleteTtlCounterEntries(ttlCounterWithinLoop, adjustedTtlCounterDate);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"TTL Counter Administration: has produced an error for {ttlCounterWithinLoop.Name} and Data Name {ttlCounterWithinLoop.TtlCounterDataName} as {ex}.");
                    }
            }
            catch (Exception ex)
            {
                Log.Error($"TTL Counter Administration: has produced an error as {ex}");
            }
            finally
            {
                Log.Info(
                    $"TTL Counter Administration: Model TTL Counter processing for model id {Id} has finished.");
            }
        }

        private void DeleteTtlCounterEntries(EntityAnalysisModelTtlCounter ttlCounter, DateTime adjustedTtlCounterDate)
        {
            var cacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(JubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),Log);
            cacheTtlCounterEntryRepository.DeleteAfterDecremented(Id,
                ttlCounter.Id, ttlCounter.TtlCounterDataName, adjustedTtlCounterDate);
            
            Log.Info(
                $"TTL Counter Administration: has finished aggregation for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName} and has also deleted the values from the entries table where records are less than {adjustedTtlCounterDate}.");
        }

        private void DecrementTtlCounterCache(EntityAnalysisModelTtlCounter ttlCounter, DateTime referenceDate,
            string value, int decrement)
        {
            var cacheTtlCounterRepository = new CacheTtlCounterRepository(JubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),Log);
            cacheTtlCounterRepository.DecrementTtlCounterCache(Id,ttlCounter.Id,
                ttlCounter.TtlCounterDataName,value,decrement,referenceDate);
 
            Log.Info(
                $"TTL Counter Administration: has finished aggregation for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName} and has also decremented value {value} by {decrement} in the TTL counter cache.  Will now use the same date criteria to delete the records from the entries table.");
        }

        private Dictionary<string,int> GetExpiredTtlCounterCacheCounts(EntityAnalysisModelTtlCounter ttlCounter,
            DateTime adjustedTtlCounterDate)
        {
            var cacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(JubeEnvironment.AppSettings(
                new []{"CacheConnectionString","ConnectionString"}),Log);
            return cacheTtlCounterEntryRepository.GetExpiredTtlCounterCacheCounts(Id,
                ttlCounter.Id, ttlCounter.TtlCounterDataName, adjustedTtlCounterDate);
        }

        private DateTime GetAdjustedTtlCounterDate(EntityAnalysisModelTtlCounter ttlCounter, DateTime referenceDate)
        {
            try
            {
                Log.Info(
                    $"TTL Counter Administration: has found a reference date of {referenceDate} for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}.");

                return ttlCounter.TtlCounterInterval switch
                {
                    "d" => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1),
                    "h" => referenceDate.AddHours(ttlCounter.TtlCounterValue * -1),
                    "n" => referenceDate.AddMinutes(ttlCounter.TtlCounterValue * -1),
                    "s" => referenceDate.AddSeconds(ttlCounter.TtlCounterValue * -1),
                    "m" => referenceDate.AddMonths(ttlCounter.TtlCounterValue * -1),
                    "y" => referenceDate.AddYears(ttlCounter.TtlCounterValue * -1),
                    _ => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1)
                };
            }
            catch (Exception ex)
            {
                Log.Info(
                    $"TTL Counter Administration: has found a reference date of {referenceDate}" +
                    $" for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}." +
                    $" Error of {ex} returning reference date as default.");
                
                return referenceDate;
            }
        }

        private DateTime? GetMostRecentFromTtlCounterCache(EntityAnalysisModelTtlCounter ttlCounter)
        {
            var cacheTtlCounterRepository = new CacheTtlCounterRepository(JubeEnvironment.AppSettings(
                    new []{"CacheConnectionString","ConnectionString"}),Log);
            return cacheTtlCounterRepository.GetMostRecentFromTtlCounterCache(Id, ttlCounter.Id,
                ttlCounter.TtlCounterDataName);
        }
        
        public bool TryProcessSingleDequeueForCaseCreationAndArchiver(int threadSequence)
        {
            var found = false;
            try
            {
                if (BulkInsertMessageBuffers.ContainsKey(threadSequence))
                {
                    var bulkInsertMessageBuffer = BulkInsertMessageBuffers[threadSequence];
                    PersistToDatabaseAsync.TryDequeue(out var payload);

                    if (payload != null)
                    {
                        bulkInsertMessageBuffer.LastMessage = DateTime.Now;
                        
                        found = true;

                        CaseCreationAndArchiver(payload, bulkInsertMessageBuffer);
                    }
                    else
                    {
                        if (bulkInsertMessageBuffer.LastMessage.AddSeconds(10) <= DateTime.Now &&
                            bulkInsertMessageBuffer.Archive.Count > 0)
                        {
                            WriteToDatabase(bulkInsertMessageBuffer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has occurred as {ex}");
            }

            return found;
        }

        public void CaseCreationAndArchiver(EntityAnalysisModelInstanceEntryPayload payload,
            ArchiveBuffer bulkInsertMessageBuffer)
        {
            var payloadJsonStore = new EntityAnalysisModelInstanceEntryPayloadJson();
            var json = payloadJsonStore.BuildJson(payload,ContractResolver);
            
            string jsonString = null;
            if (payload.CreateCasePayload != null)
            {
                jsonString = Encoding.UTF8.GetString(json.ToArray());
                CreateCase(payload, jsonString);
            }

            if (payload.StoreInRdbms)
            {
                if (string.IsNullOrEmpty(jsonString)) jsonString = Encoding.UTF8.GetString(json.ToArray());
                
                if (payload.Reprocess)
                {
                    TransactionalUpdate(payload, jsonString);   
                }
                else if (bulkInsertMessageBuffer is null)
                {
                    Log.Error("Database Persist: Not implemented bulkInsertMessageBuffer is null.");
                }
                else
                {
                    DataTableInsertToBuffer(bulkInsertMessageBuffer, payload, jsonString);

                    if (bulkInsertMessageBuffer.Archive.Count >= int.Parse(JubeEnvironment.AppSettings("BulkCopyThreshold")))
                    {
                        WriteToDatabase(bulkInsertMessageBuffer);
                    }
                }
            }
        }

        private void WriteToDatabase(ArchiveBuffer bulkInsertMessageBuffer)
        {
            var sw = new Stopwatch();
            sw.Start();

            Log.Info(
                "Database Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");
            
            var dbContext = DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));

            Log.Info("Database Persist: Opened an SQL Bulk Collection via repository.");

            var repositoryArchive = new ArchiveRepository(dbContext);
            var repositoryArchiveKeys = new ArchiveKeyRepository(dbContext);

            try
            {
                repositoryArchive.BulkCopy(bulkInsertMessageBuffer.Archive);
                repositoryArchiveKeys.BulkCopy(bulkInsertMessageBuffer.ArchiveKeys);
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has been created on build insert as {ex}");
            }
            finally
            {
                bulkInsertMessageBuffer.Archive.Clear();
                bulkInsertMessageBuffer.ArchiveKeys.Clear();
                
                dbContext.Close();
                dbContext.Dispose();
                
                Log.Info("Database Persist: Closed an SQL Bulk Collection.");
            }
        }

        private void DataTableInsertToBuffer(ArchiveBuffer bulkInsertMessageBuffer,
            EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            Log.Info(
                $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for bulk insert.");
            
            try
            {
                Log.Info(
                    "Database Persist: The flag to promote report table has been set for this model,  will now check columns are available and add the record to the data table.");
                
                var model = new Data.Poco.Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.Now
                };
                
                bulkInsertMessageBuffer.Archive.Add(model);

                foreach (var reportDatabaseValue in payload.ReportDatabaseValues)
                {
                    bulkInsertMessageBuffer.ArchiveKeys.Add(reportDatabaseValue);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has occurred as {ex}");
            }
        }

        private void TransactionalUpdate(EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            Log.Info(
                $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for update as it is reprocess.");
            
            var dbContext = DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                var repository = new ArchiveRepository(dbContext);

                var model = new Data.Poco.Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.Now
                };

                repository.Update(model);
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: error processing payload as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                Log.Info(
                    $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  Has finished reprocess ");
            }
        }

        private void CreateCase(EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            var dbContext = DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                Log.Info(
                    $"Case Creation: has received a case creation message with case entry GUID of {payload.CreateCasePayload.CaseEntryGuid}.");

                var repositoryCase = new CaseRepository(dbContext);
                var query = new GetExistingCasePriorityQuery(dbContext);

                Log.Info(
                    $"Case Creation: connection to the database established for case entry GUID of {payload.CreateCasePayload.CaseEntryGuid}.");

                var model = new Case
                {
                    EntityAnalysisModelInstanceEntryGuid = payload.CreateCasePayload.CaseEntryGuid,
                    CaseWorkflowId = payload.CreateCasePayload.CaseWorkflowId,
                    CaseWorkflowStatusId = payload.CreateCasePayload.CaseWorkflowStatusId,
                    CaseKey = payload.CreateCasePayload.CaseKey,
                    CaseKeyValue = payload.CreateCasePayload.CaseKeyValue,
                    Locked = 0,
                    Rating = 0,
                    CreatedDate = DateTime.Now
                };

                if (payload.CreateCasePayload.SuspendBypass)
                {
                    model.Diary = 1;
                    model.DiaryDate = payload.CreateCasePayload.SuspendBypassDate;
                    model.ClosedStatusId = 4;
                }
                else
                {
                    model.Diary = 0;
                    model.DiaryDate = payload.CreateCasePayload.SuspendBypassDate;
                    model.ClosedStatusId = 0;
                    model.DiaryDate = DateTime.Now;
                }
                
                model.Json = jsonString;

                Log.Info(
                    $"Case Creation: Have created a case creation SQL command with Case Entry GUID of {payload.CreateCasePayload.CaseEntryGuid}, Case Workflow ID of {payload.CreateCasePayload.CaseWorkflowId}, Case Workflow Status ID of {payload.CreateCasePayload.CaseWorkflowStatusId}, Case Key of {payload.CreateCasePayload.CaseKeyValue}, Case XML Bytes {jsonString.Length}");

                var existing = query.Execute(model.CaseWorkflowId.Value,model.CaseKey,model.CaseKeyValue);

                if (existing == null)
                {
                    repositoryCase.Insert(model);
                }
                else
                {
                    var repositoryCasesWorkflowsStatus =
                        new CaseWorkflowStatusRepository(dbContext, TenantRegistryId);

                    var recordCasesWorkflowsStatus =
                        repositoryCasesWorkflowsStatus.GetById(model.CaseWorkflowStatusId);
                    
                    if (recordCasesWorkflowsStatus.Priority < existing.Priority)
                    {
                        model.Id = existing.CaseId;
                        model.Locked = 0;
                        model.CaseWorkflowStatusId = payload.CreateCasePayload.CaseWorkflowStatusId;
                        repositoryCase.Update(model);
                    }
                }
                
                Log.Info(
                    $"Case Creation: Executed Case Entry GUID of {payload.CreateCasePayload.CaseEntryGuid}, Case Workflow ID of {payload.CreateCasePayload.CaseWorkflowId}, Case Workflow Status ID of {payload.CreateCasePayload.CaseWorkflowStatusId}, Case Key of {payload.CreateCasePayload.CaseKeyValue}, Case JSON Bytes {jsonString.Length}");
                
            }
            catch (Exception ex)
            {
                Log.Error($"Case Creation: error processing payload as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                Log.Info("Case Creation: closed the database connection.");
            }
        }
        
        public void MountCollectionsAndSyncCacheDbIndex()
        {
            if (JubeEnvironment.AppSettings("EnableCacheIndex").Equals("True",StringComparison.OrdinalIgnoreCase))
            {
                BuildGroupingKeyIndex();
            }
        }
        
        private void BuildGroupingKeyIndex()
        {
            try
            {
                var cachePayloadRepository = new CachePayloadRepository(JubeEnvironment.AppSettings(
                    new []{"CacheConnectionString","ConnectionString"}),Log);
                var indexes = cachePayloadRepository.GetIndexes();
                
                Log.Debug(
                    "Cache Indexing: Retrieved a list of indexes on the Entries collection for model " +
                    Id +
                    ",  will now check to see if the Grouping Keys have already been added.");

                foreach (var (key, _) in DistinctSearchKeys)
                {
                    Log.Debug(
                        "Cache Indexing: Retrieved a list of indexes on the Entries collection " +
                        "will now check to see if the Grouping Key " +
                        key + " has already been added.");
                    try
                    {
                        var compoundIndexSearchKeyNameReferenceDateName = "IX_CachePayload_EntityAnalysisModelId_" + key + "_ReferenceDate";
                        if (!indexes.Contains(compoundIndexSearchKeyNameReferenceDateName))
                            try
                            {
                                Log.Debug("Cache Indexing: Entries index does not exist for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                 + key + " it is being created.");
                                
                                cachePayloadRepository.CreateIndex(compoundIndexSearchKeyNameReferenceDateName,
                                    "\"ReferenceDate\"",
                                    "(\"Json\"->>'" + key + "')",
                                    Id);
                                
                                Log.Debug("Cache Indexing: Entries index has been created for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                          + key + ".");
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Cache Indexing: Entries index for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                          + key + " has created an error as " + ex + ".");
                            }
                        
                        var compoundIndexSearchKeyNameCreatedDate = "IX_CachePayload_EntityAnalysisModelId_" + key + "_CreatedDate";
                        if (!indexes.Contains(compoundIndexSearchKeyNameCreatedDate))
                            try
                            {
                                Log.Debug("Cache Indexing: Entries index does not exist for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                          + key + " it is being created.");

                                cachePayloadRepository.CreateIndex(compoundIndexSearchKeyNameCreatedDate,
                                    "\"CreatedDate\"",
                                    "(\"Json\"->>'" + key + "')",
                                    Id);
                                
                                Log.Debug("Cache Indexing: Entries index has been created for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                          + key + ".");
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Cache Indexing: Entries index for " + compoundIndexSearchKeyNameReferenceDateName + " and grouping key "
                                          + key + " has created an error as " + ex + ".");
                            }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Cache Indexing: Entries index failure for " + key + " " +
                                  ex + ".");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Cache Indexing: Entries index failure for EntityAnalysisModel " + Id + " " + ex + ".");
            }
        }
    }
}
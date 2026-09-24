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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Archiver;
    using Context;
    using Data.Context;
    using Data.Query;
    using Data.Reporting;
    using Data.Repository;
    using Data.SyntaxTree;
    using Dictionary;
    using EntityAnalysisModel;
    using EntityAnalysisModelInvoke;
    using Helpers;
    using Models;
    using Parser;
    using Parser.Compiler;
    using Reprocessing;

    public class ReprocessingTaskStarter(Context context)
    {
        private readonly Random random = new(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
        public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromSeconds(10);
        public Action<ReprocessingPageTiming> PageCompleted { get; init; }

        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                    try
                    {
                        foreach (var modelKvp in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                        {
                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                            if (!modelKvp.Value.Started)
                            {
                                continue;
                            }

                            try
                            {
                                await RunNextAsync(dbContext, modelKvp,
                                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error($"Entity Reprocessing: {ex}");
                            }
                        }

                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);

                        await Task.Delay(20000, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);

                        throw;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);

                        context.Services.Log.Error($"Entity Reprocessing: {ex}. Waiting.");

                        await Task.Delay(20000, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation EntityReprocessingAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"EntityReprocessingAsync: Error outside of loop {ex}");
            }
        }

        public async Task<ReprocessingRunResult> RunNextAsync(DbContext dbContext,
            KeyValuePair<int, EntityAnalysisModel> modelKvp, CancellationToken token = default)
        {
            var (instance, found) = await GetEntityAnalysisModelRuleReprocessingInstanceAsync(dbContext, modelKvp,
                token).ConfigureAwait(false);

            if (instance.EntityAnalysisModelsReprocessingRuleInstanceId == 0)
            {
                return null;
            }

            var instanceId = instance.EntityAnalysisModelsReprocessingRuleInstanceId;
            var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

            if (!found)
            {
                var failure = instance.Failure ?? "The reprocessing rule could not be loaded.";
                await repository.UpdateFailedAsync(instanceId, token).ConfigureAwait(false);

                context.Services.Log.Error(
                    $"Entity Reprocessing: Reprocessing instance {instanceId} has failed as {failure}");

                return new ReprocessingRunResult(instanceId, ReprocessingRunOutcome.Failed, 0, 0, 0, 0, 0, failure);
            }

            var model = modelKvp.Value;
            var ranges = await new GetArchiveRangeAndCountsQuery(dbContext)
                .ExecuteAsync(model.Instance.Guid, token).ConfigureAwait(false);

            if (ranges?.Max == null)
            {
                await repository.UpdateReferenceDateCountAsync(instanceId, 0, DateTime.UtcNow, token)
                    .ConfigureAwait(false);

                await repository.UpdateCompletedAsync(instanceId, token).ConfigureAwait(false);
                return new ReprocessingRunResult(instanceId, ReprocessingRunOutcome.Completed, 0, 0, 0, 0, 0, null);
            }

            var snapshotDate = DateTime.UtcNow;
            var lastReferenceDate = DateTime.SpecifyKind(ranges.Max.Value, DateTimeKind.Unspecified);
            var startDate = ReprocessingDateRange.StartDate(lastReferenceDate, instance.ReprocessingIntervalType,
                instance.ReprocessingIntervalValue);

            if (startDate == null)
            {
                var failure =
                    $"The interval {instance.ReprocessingIntervalValue}{instance.ReprocessingIntervalType} is not valid.";
                await repository.UpdateFailedAsync(instanceId, token).ConfigureAwait(false);

                context.Services.Log.Error(
                    $"Entity Reprocessing: Reprocessing instance {instanceId} has failed as {failure}");

                return new ReprocessingRunResult(instanceId, ReprocessingRunOutcome.Failed, 0, 0, 0, 0, 0, failure);
            }

            var availableCount = await new ArchiveRepository(dbContext)
                .GetCountsByReferenceDateAsync(model.Instance.Guid, startDate.Value, token).ConfigureAwait(false);

            await repository.UpdateReferenceDateCountAsync(instanceId, availableCount, startDate.Value, token)
                .ConfigureAwait(false);

            var limit = int.Parse(context.Services.DynamicEnvironment.AppSettings("ReprocessingBulkLimit"));

            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {instanceId} will process {availableCount} documents between {startDate} and {lastReferenceDate} created before {snapshotDate}, {limit} at a time.");
            }

            var sampled = 0;
            var matched = 0;
            var processed = 0;
            var errors = 0;
            var pages = 0;
            var stopped = false;
            var progressDate = startDate.Value;
            var afterReferenceDate = startDate.Value;
            var afterGuid = Guid.Empty;
            var lastProgress = DateTime.UtcNow;
            var sql = model.References.ArchivePayloadSqlSelect + " " + model.References.ArchivePayloadSqlBody;

            using var archiveDatabase = new Postgres(
                context.Services.ReportConnectionString ?? dbContext.Connection.ConnectionString,
                context.Services.Log,
                context.Services.DynamicEnvironment.ParserAssertSelectOnly());

            while (!stopped)
            {
                var fetchStarted = Stopwatch.GetTimestamp();
                var documents = await archiveDatabase.ExecuteReturnPayloadFromArchiveAfterAsync(sql,
                    startDate.Value, lastReferenceDate, snapshotDate, afterReferenceDate, afterGuid, limit,
                    token).ConfigureAwait(false);
                var fetch = Stopwatch.GetElapsedTime(fetchStarted);

                if (documents.Count == 0)
                {
                    break;
                }

                pages += 1;
                var processStarted = Stopwatch.GetTimestamp();
                var batch = new ReprocessingArchiveBatch(instanceId);

                foreach (var entry in documents)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        if (ReprocessingDateRange.Sampled(instance.ReprocessingSample, random.NextDouble()))
                        {
                            sampled += 1;

                            if (instance.ReprocessingRuleCompileDelegate(entry,
                                    model.Dependencies.EntityAnalysisModelLists,
                                    new PooledDictionary<string, DictionaryNoBoxing<string>>(),
                                    context.Services.Log))
                            {
                                await EntityAnalysisModelInvoke.InvokeAsync(model, entry, instanceId, batch)
                                    .ConfigureAwait(false);

                                matched += 1;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        errors += 1;

                        context.Services.Log.Error(
                            $"Entity Reprocessing: Reprocessing instance {instanceId} has had an error on document {processed} as {ex}.");
                    }

                    processed += 1;
                    progressDate = entry[model.References.ReferenceDateName];

                    if (DateTime.UtcNow - lastProgress < ProgressInterval)
                    {
                        continue;
                    }

                    stopped = await ReportProgressAsync(repository, instanceId, processed, sampled, matched, errors,
                        progressDate, token).ConfigureAwait(false);
                    lastProgress = DateTime.UtcNow;

                    if (stopped)
                    {
                        break;
                    }
                }

                var written = batch.Count;
                try
                {
                    var missing = await batch.FlushAsync(dbContext, token).ConfigureAwait(false);
                    matched -= missing;
                    errors += missing;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    matched -= written;
                    errors += written;

                    context.Services.Log.Error(
                        $"Entity Reprocessing: Reprocessing instance {instanceId} could not write page {pages} to the archive as {ex}.");
                }

                PageCompleted?.Invoke(new ReprocessingPageTiming(pages, documents.Count, fetch,
                    Stopwatch.GetElapsedTime(processStarted)));

                var last = documents[^1];
                afterReferenceDate = last[model.References.ReferenceDateName];
                afterGuid = last["EntityAnalysisModelInstanceEntryGuid"];
            }

            if (!stopped)
            {
                stopped = await ReportProgressAsync(repository, instanceId, processed, sampled, matched, errors,
                    progressDate, token).ConfigureAwait(false);
            }

            if (stopped)
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {instanceId} has been removed, stopping process.");
                }

                return new ReprocessingRunResult(instanceId, ReprocessingRunOutcome.Stopped, processed, sampled,
                    matched, errors, pages, null);
            }

            await repository.UpdateCompletedAsync(instanceId, token).ConfigureAwait(false);

            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {instanceId} has completed with processed {processed}, sampled {sampled}, matched {matched} and errors {errors}.");
            }

            return new ReprocessingRunResult(instanceId, ReprocessingRunOutcome.Completed, processed, sampled,
                matched, errors, pages, null);
        }

        private async Task<bool> ReportProgressAsync(EntityAnalysisModelReprocessingRuleInstanceRepository repository,
            int instanceId, int processed, int sampled, int matched, int errors, DateTime referenceDate,
            CancellationToken token)
        {
            try
            {
                await repository.UpdateCountsAsync(instanceId, sampled, matched, processed, errors, referenceDate,
                    token).ConfigureAwait(false);
                return false;
            }
            catch (KeyNotFoundException)
            {
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Entity Reprocessing: Reprocessing instance {instanceId} could not report progress as {ex}.");

                return false;
            }
        }

        private async
            Task<(EntityAnalysisModelRuleReprocessingInstance EntityAnalysisModelRuleReprocessingInstance, bool
                FoundInstance)> GetEntityAnalysisModelRuleReprocessingInstanceAsync(DbContext dbContext,
                KeyValuePair<int, EntityAnalysisModel> modelKvp, CancellationToken token = default)
        {
            var returnTuple = (
                EntityAnalysisModelRuleReprocessingInstance: new EntityAnalysisModelRuleReprocessingInstance(),
                FoundInstance: false);

            try
            {
                var (key, entityAnalysisModel) = modelKvp;
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  The model has been started,  so we will check to see if there is a reprocessing instance for the model.");
                }

                var query = new GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has executed the reader to find reprocessing instances.");
                }

                var record = await query.ExecuteAsync(key, token).ConfigureAwait(false);

                if (record != null)
                {
                    returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelId =
                        record.EntityAnalysisModelId;

                    returnTuple.EntityAnalysisModelRuleReprocessingInstance
                            .EntityAnalysisModelsReprocessingRuleInstanceId
                        = record.Id;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has found a reprocessing instance id of {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.");
                    }

                    if (record.ReprocessingIntervalValue.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue =
                            record.ReprocessingIntervalValue.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue = 1;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                        }
                    }

                    if (record.ReprocessingIntervalType != null)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType =
                            record.ReprocessingIntervalType;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType = "d";

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                        }
                    }

                    if (record.ReprocessingSample.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample =
                            record.ReprocessingSample.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample = 0;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                        }
                    }

                    if (record.RuleScriptTypeId.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId =
                            record.RuleScriptTypeId.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = 1;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Is loading the rule parser and tokens.");
                    }

                    var parser = new Parser(context.Services.Log, [])
                    {
                        EntityAnalysisModelRequestXPaths = [],
                        EntityAnalysisModelInlineScriptProperties = []
                    };

                    foreach (var entityAnalysisModelRequestXPath in
                             entityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths
                                 .Where(entityAnalysisModelRequestXPath =>
                                     !parser.EntityAnalysisModelRequestXPaths.ContainsKey(
                                         entityAnalysisModelRequestXPath.Name)))
                    {
                        parser.EntityAnalysisModelRequestXPaths.Add(entityAnalysisModelRequestXPath.Name,
                            new EntityAnalysisModelRequestXPath
                            {
                                DataTypeId = entityAnalysisModelRequestXPath.DataTypeId,
                                DefaultValue = entityAnalysisModelRequestXPath.DefaultValue,
                                Cache = entityAnalysisModelRequestXPath.Cache
                            });
                    }

                    foreach (var publicProperty in
                             entityAnalysisModel.Collections.EntityAnalysisModelInlineScripts
                                 .SelectMany(entityAnalysisModelInlineScript
                                     => SyntaxTreeHelpers.GetPublicProperties(
                                         entityAnalysisModelInlineScript.InlineScriptCode,
                                         entityAnalysisModelInlineScript.LanguageId == 2)))
                    {
                        parser.EntityAnalysisModelInlineScriptProperties.TryAdd(publicProperty.Key,
                            publicProperty.Value.DataTypeId);
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has loaded the rule parser and tokens.");
                    }

                    if (record.BuilderRuleScript != null &&
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 1)
                    {
                        var parsedRule = new ParsedRule
                        {
                            OriginalRuleText = record.BuilderRuleScript,
                            ErrorSpans = []
                        };

                        parsedRule = parser.TranslateFromDotNotation(parsedRule);
                        parsedRule = parser.Parse(parsedRule);

                        if (parsedRule.ErrorSpans.Count == 0)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript =
                                parsedRule.ParsedRuleText;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set builder script as {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                            }
                        }
                    }
                    else if (record.CoderRuleScript != null &&
                             returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 2)
                    {
                        var parsedRule = new ParsedRule
                        {
                            OriginalRuleText = record.CoderRuleScript,
                            ErrorSpans = []
                        };
                        parsedRule = parser.TranslateFromDotNotation(parsedRule);
                        parsedRule = parser.Parse(parsedRule);

                        if (parsedRule.ErrorSpans.Count == 0)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript =
                                parsedRule.ParsedRuleText;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set coder script as {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                            }
                        }
                    }

                    if (returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript == null)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.Failure =
                            "The reprocessing rule is empty or does not parse.";
                        return returnTuple;
                    }

                    var gatewayRuleScript = new StringBuilder(
                        EngineRuleWrapper.ReprocessingRule(returnTuple.EntityAnalysisModelRuleReprocessingInstance
                            .ReprocessingRuleScript).Text);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set class wrap as {gatewayRuleScript}.");
                    }

                    var gatewayRuleScriptHash = HashHelper.GetHash(gatewayRuleScript.ToString());

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");
                    }

                    if (context.Caching.HashCacheAssembly.TryGetValue(gatewayRuleScriptHash, out var value))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");
                        }

                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile =
                            value;
                        var classType =
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType(
                                "GatewayRule");

                        var methodInfo = classType?.GetMethod("Match");
                        if (methodInfo != null)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                                (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                        }

                        returnTuple.FoundInstance = true;
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");
                        }

                        var codeBase = Assembly.GetExecutingAssembly().Location;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        var strPathBinary = Path.GetDirectoryName(codeBase);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        var compile = new Compile();
                        compile.CompileCode(gatewayRuleScript.ToString(), context.Services.Log,
                        [
                            Path.Combine(strPathBinary ?? throw new InvalidOperationException(), "log4net.dll"),
                            Path.Combine(strPathBinary, "Jube.Dictionary.dll")
                        ], Compile.Language.Vb);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Model {key} and Gateway Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == null)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");
                            }

                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile =
                                compile.CompiledAssembly;

                            var classType =
                                returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType(
                                    "GatewayRule");
                            var methodInfo = classType?.GetMethod("Match");
                            if (methodInfo != null)
                            {
                                returnTuple.EntityAnalysisModelRuleReprocessingInstance
                                        .ReprocessingRuleCompileDelegate =
                                    (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                            }

                            context.Caching.HashCacheAssembly.TryAdd(gatewayRuleScriptHash, compile.CompiledAssembly);
                            context.Caching.HashCacheAssemblyMetadata.TryAdd(gatewayRuleScriptHash,
                                new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes,
                                    compile.CompiledAssemblyBinary, gatewayRuleScript.ToString()));

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                            }

                            returnTuple.FoundInstance = true;
                        }
                        else
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.Failure =
                                "The reprocessing rule does not compile: " + string.Join("; ",
                                    compile.Errors.Select(e => e.GetMessage()));

                            foreach (var compileError in compile.Errors)
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} compile error {compileError.GetMessage()}.");
                                }
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                            }
                        }
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has finished loading reprocessing instance {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.  Will now proceed to select the counts and date ranges.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                returnTuple.FoundInstance = false;
                returnTuple.EntityAnalysisModelRuleReprocessingInstance.Failure = ex.Message;
                context.Services.Log.Error($"EntityAnalysisModelRuleReprocessingInstance: has produced an error {ex}");
            }

            if (returnTuple.FoundInstance &&
                returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate == null)
            {
                returnTuple.FoundInstance = false;
                returnTuple.EntityAnalysisModelRuleReprocessingInstance.Failure =
                    "The compiled reprocessing rule does not expose a Match function.";
            }

            return returnTuple;
        }
    }
}
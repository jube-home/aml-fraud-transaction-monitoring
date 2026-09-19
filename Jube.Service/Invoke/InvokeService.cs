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

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Jube.Data.Context;
using Jube.Dto.Invoke;
using Jube.Dto.Repository.Archive;
using Jube.Engine.BackgroundTasks.TaskStarters.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Sanctions;
using Jube.Service.Exceptions.Invoke;
using Jube.Validations.Invoke;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Invoke
{
    public sealed class InvokeService
    {
        private readonly global::Jube.DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly global::Jube.Engine.Engine? engine;
        private readonly Func<DbContext>? dbContextFactory;
        private readonly ILog log;
        private readonly string? userName;
        private readonly IStringLocalizer localiser;

        public InvokeService(global::Jube.Engine.Engine? engine,
            global::Jube.DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log, string? userName,
            IStringLocalizer localiser, Func<DbContext>? dbContextFactory = null)
        {
            this.localiser = localiser;
            this.engine = engine;
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
            this.userName = userName;
            this.dbContextFactory = dbContextFactory;

            if (this.engine != null)
            {
                Interlocked.Increment(ref this.engine.Context.Counters.HttpCounterAllRequests);
            }
        }

        private bool PublicInvokeEnabled()
        {
            return dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                .Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<InvokeResult> CallbackAsync(Guid guid, int? timeout, CancellationToken token = default)
        {
            try
            {
                if (!PublicInvokeEnabled())
                {
                    return InvokeResult.Empty(404);
                }

                if (engine?.Context == null)
                {
                    return InvokeResult.Empty(503);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterCallback);

                var waited = Stopwatch.StartNew();
                var timeoutSeconds = Math.Clamp(timeout ?? 30000, 0, 30000);
                var tcs = engine.Context.Services.CacheService.CacheCallbackPublishSubscribe.Callbacks.GetOrAdd(guid,
                    // ReSharper disable once RedundantNameQualifier
                    _ => new TaskCompletionSource<global::Jube.Cache.Redis.Callback.Callback>(
                        TaskCreationOptions.RunContinuationsAsynchronously));
                var callback = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), token);

                if (!await CallerBelongsToTenantOfCallbackAsync(callback.TenantRegistryId, token)
                        .ConfigureAwait(false))
                {
                    var callbackTimeoutMilliseconds = int.TryParse(
                        dynamicEnvironment.AppSettings("CallbackTimeout"), out var configured)
                        ? configured
                        : 1000;
                    var remaining = TimeSpan.FromMilliseconds(
                        Math.Min(timeoutSeconds * 1000.0, callbackTimeoutMilliseconds)) - waited.Elapsed;
                    if (remaining > TimeSpan.Zero)
                    {
                        await Task.Delay(remaining, token).ConfigureAwait(false);
                    }

                    Interlocked.Increment(ref engine.Context.Counters.HttpCounterCallbackTimeout);
                    return InvokeResult.Empty(408);
                }

                await engine.Context.Services.CacheService
                    .CacheCallbackPublishSubscribe
                    .DeleteAsync(guid, token).ConfigureAwait(false);

                return InvokeResult.Content(callback.Payload, "application/json");
            }
            catch (TimeoutException)
            {
                if (engine?.Context is { } timeoutContext)
                {
                    Interlocked.Increment(ref timeoutContext.Counters.HttpCounterCallbackTimeout);
                }

                return InvokeResult.Empty(408);
            }
            catch (Exception ex)
            {
                log.Error($"Callback Fetch: Has seen an error as {ex}. Returning 500.");

                if (engine?.Context != null)
                {
                    Interlocked.Increment(ref engine.Context.Counters.HttpCounterCallback);
                }

                return InvokeResult.Empty(500);
            }
        }

        private async Task<bool> CallerBelongsToTenantOfCallbackAsync(int tenantRegistryId,
            CancellationToken token)
        {
            if (dbContextFactory == null || string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            await using var dbContext = dbContextFactory();
            return await dbContext.UserInTenant.AnyAsync(
                    w => w.User == userName && w.TenantRegistryId == tenantRegistryId, token)
                .ConfigureAwait(false);
        }

        public InvokeResult Sanction(SanctionSearchRequestDto request)
        {
            try
            {
                if (!PublicInvokeEnabled()
                    || !dynamicEnvironment.AppSettings("EnableEngine")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeResult.Empty(404);
                }

                if (engine?.Context is not { Ready: true })
                {
                    return InvokeResult.Empty(503);
                }

                var validationResult = new SanctionSearchRequestDtoValidator(localiser).Validate(request);
                if (!validationResult.IsValid)
                {
                    throw new DtoValidationException(validationResult);
                }

                var multiPartString = request.MultiPartString ?? string.Empty;
                var distance = string.IsNullOrWhiteSpace(request.Distance)
                    ? 0
                    : int.Parse(request.Distance, CultureInfo.InvariantCulture);
                var maxDistanceRatio = ParseOptionalDouble(request.MaxDistanceRatio);
                var maxCoverageRatio = ParseOptionalDouble(request.MaxCoverageRatio);

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterSanction);

                var effectiveMaxDistanceRatio = maxDistanceRatio ??
                                                LevenshteinDistance.ParseNullableDistanceRatio(
                                                    dynamicEnvironment.AppSettings(
                                                        "SanctionsLevenshteinMaxDistanceRatio"));

                var effectiveMaxCoverageRatio = maxCoverageRatio ??
                                                LevenshteinDistance.ParseNullableCoverageRatio(
                                                    dynamicEnvironment.AppSettings(
                                                        "SanctionsLevenshteinMaxCoverageRatio"));

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Sanction Fetch: Reached Sanction Get controller with distance of {distance}, max distance ratio of {effectiveMaxDistanceRatio}, max coverage ratio of {effectiveMaxCoverageRatio} and string of {multiPartString}.");
                }

                var sanctionEntryReturns = new LevenshteinDistance(effectiveMaxDistanceRatio, effectiveMaxCoverageRatio)
                    .CheckMultipartString(multiPartString, distance, engine.Context.Sanctions.SanctionsEntries,
                        engine.Context.Sanctions.SanctionsStopTokens);

                var entries = sanctionEntryReturns
                    .Select(sanctionEntryReturn => new SanctionEntryDto
                    {
                        Reference = sanctionEntryReturn.SanctionEntry.SanctionEntryReference,
                        Value = string.Join(' ', sanctionEntryReturn.SanctionEntry.SanctionElementValue),
                        SanctionEntrySourceId = sanctionEntryReturn.SanctionEntry.SanctionEntrySourceId,
                        Source = engine.Context.Sanctions.SanctionsSources.TryGetValue(sanctionEntryReturn.SanctionEntry
                            .SanctionEntrySourceId, out var source)
                            ? source.Name
                            : "Missing",
                        Distance = sanctionEntryReturn.LevenshteinDistance,
                        Id = sanctionEntryReturn.SanctionEntry.SanctionEntryId
                    })
                    .ToList();

                var groupBySource = sanctionEntryReturns
                    .GroupBy(sanctionEntryReturn => sanctionEntryReturn.SanctionEntry.SanctionEntrySourceId)
                    .Select(sourceGroup =>
                    {
                        var sourceMatches = sourceGroup.ToList();

                        return new SanctionSourceAggregationDto
                        {
                            SourceId = sourceGroup.Key,
                            SourceName =
                                engine.Context.Sanctions.SanctionsSources.TryGetValue(sourceGroup.Key, out var source)
                                    ? source.Name
                                    : "Missing",
                            Sum = SanctionAggregationCalculator.CalculateSum(sourceMatches),
                            Average = SanctionAggregationCalculator.CalculateAverage(sourceMatches),
                            Count = SanctionAggregationCalculator.CalculateCount(sourceMatches),
                            Max = SanctionAggregationCalculator.CalculateMax(sourceMatches),
                            Min = SanctionAggregationCalculator.CalculateMin(sourceMatches),
                            First = SanctionAggregationCalculator.CalculateFirst(sourceMatches),
                            Last = SanctionAggregationCalculator.CalculateLast(sourceMatches),
                            Confidence = SanctionAggregationCalculator.CalculateConfidence(sourceMatches)
                        };
                    })
                    .OrderBy(sourceAggregation => sourceAggregation.SourceName)
                    .ToList();

                var total = new SanctionAggregationDto
                {
                    Sum = SanctionAggregationCalculator.CalculateSum(sanctionEntryReturns),
                    Average = SanctionAggregationCalculator.CalculateAverage(sanctionEntryReturns),
                    Count = SanctionAggregationCalculator.CalculateCount(sanctionEntryReturns),
                    Max = SanctionAggregationCalculator.CalculateMax(sanctionEntryReturns),
                    Min = SanctionAggregationCalculator.CalculateMin(sanctionEntryReturns),
                    First = SanctionAggregationCalculator.CalculateFirst(sanctionEntryReturns),
                    Last = SanctionAggregationCalculator.CalculateLast(sanctionEntryReturns),
                    Confidence = SanctionAggregationCalculator.CalculateConfidence(sanctionEntryReturns)
                };

                return InvokeResult.Json(200, new SanctionSearchResponseDto
                {
                    Aggregations = new SanctionAggregationsDto
                    {
                        Total = total,
                        BySource = groupBySource
                    },
                    Entries = entries
                });
            }
            catch (DtoValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Error($"Sanction Fetch: Has seen an error as {ex}. Returning 500.");

                if (engine?.Context != null)
                {
                    Interlocked.Increment(ref engine.Context.Counters.HttpCounterAllError);
                }

                return InvokeResult.Empty(500);
            }
        }

        private static double? ParseOptionalDouble(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : double.Parse(value, CultureInfo.InvariantCulture);
        }

        public InvokeResult Tag(ArchiveTagDto? model)
        {
            try
            {
                if (!PublicInvokeEnabled())
                {
                    return InvokeResult.Empty(404);
                }

                if (engine?.Context is not { Ready: true })
                {
                    return InvokeResult.Empty(503);
                }

                if (model == null)
                {
                    return InvokeResult.Empty(400);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Tagging: Controller has Put request with guid {model.EntityAnalysisModelInstanceEntryGuid}," +
                        $" name {model.Tag}.");
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterTag);

                var tag = new TagMessage
                {
                    Tag = model.Tag,
                    EntityAnalysisModelInstanceEntryGuid = model.EntityAnalysisModelInstanceEntryGuid,
                    UserName = userName
                };

                engine.Context.ConcurrentQueues.PendingTagging.Enqueue(tag);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Tagging: Controller has put tag in queue with guid " +
                        $"{tag.EntityAnalysisModelInstanceEntryGuid} and tags {model.Tag}.  Returning Ok.");
                }

                return InvokeResult.Empty(200);
            }
            catch (Exception ex)
            {
                log.Error(
                    "Tagging: An error has been created while tagging guid " +
                    $"{model?.EntityAnalysisModelInstanceEntryGuid} as {ex}.");

                if (engine?.Context != null)
                {
                    engine.Context.Counters.HttpCounterAllError += 1;
                }

                return InvokeResult.Empty(500);
            }
        }

        public async Task<InvokeResult> InvokeModelAsync(string? guidRouteValue, string? asyncRouteValue,
            Func<Task<MemoryStream>> readBodyAsync, bool hasContentLength)
        {
            try
            {
                if (!PublicInvokeEnabled())
                {
                    return InvokeResult.Empty(404);
                }

                if (engine?.Context is not { Ready: true })
                {
                    return InvokeResult.Empty(503);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterModel);

                var ms = await readBodyAsync().ConfigureAwait(false);

                try
                {
                    if (!Guid.TryParse(guidRouteValue, out var guid))
                    {
                        return InvokeResult.Empty(404);
                    }

                    var async = false;
                    if (asyncRouteValue != null)
                    {
                        async = asyncRouteValue.Equals("Async", StringComparison.OrdinalIgnoreCase);
                        Interlocked.Increment(ref engine.Context.Counters.HttpCounterModelAsync);
                    }

                    var foundModels = engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                        .ActiveEntityAnalysisModels
                        .Where(modelKvp => guid == modelKvp.Value.Instance.Guid).ToList();

                    if (!foundModels.Any())
                    {
                        return InvokeResult.Empty(404);
                    }

                    foreach (var (_, value) in foundModels)
                    {
                        if (!value.Collections.Users.Contains(userName))
                        {
                            continue;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID matched for Requested Model GUID {guid}.  Model id is {value.Instance.Id}.");
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {guid} model id is {value.Instance.Id} will now begin payload parsing.");
                        }

                        if (hasContentLength)
                        {
                            try
                            {
                                // ReSharper disable once RedundantNameQualifier
                                var context = await global::Jube.Engine.EntityAnalysisModelInvoke
                                    .EntityAnalysisModelInvoke.InvokeAsync(
                                        value,
                                        ms,
                                        int.Parse(dynamicEnvironment.AppSettings("MaxInvokeControllerRequestBytes")),
                                        async).ConfigureAwait(false);

                                var bytes = context.ImplicitAsyncTimedOut
                                    ? context.ImplicitAsyncTimeoutResponseJson
                                    : context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0
                                        ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson
                                        : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson;

                                if (bytes == null || bytes.Length == 0)
                                {
                                    log.Error(
                                        $"HTTP Handler Entity: The engine processed the request for model id {value.Instance.Id} but produced no response as it hit an error (see the engine log). Returning 500.");

                                    Interlocked.Increment(ref engine.Context.Counters.HttpCounterAllError);
                                    return InvokeResult.Empty(500);
                                }

                                return InvokeResult.Content(bytes, "application/json");
                            }
                            catch (ExceededBytesException)
                            {
                                return InvokeResult.Json(400, "Exceeded the maximum allowed bytes in POST body.");
                            }
                            catch (ExceededQueueLengthException)
                            {
                                return InvokeResult.Json(429, "Too many asynchronous requests in the queue.");
                            }
                            catch (ReferenceDateInFutureException)
                            {
                                return InvokeResult.Json(400, "Reference Date can't be in the future.");
                            }
                            catch (ZeroBytesException)
                            {
                                return InvokeResult.Json(400, "Empty POST body.");
                            }
                            catch (Newtonsoft.Json.JsonException ex)
                            {
                                if (log.IsWarnEnabled)
                                {
                                    log.Warn(
                                        $"HTTP Handler Entity: The POST body is not a valid JSON object: {ex.Message}");
                                }

                                return InvokeResult.Json(400, "Malformed JSON in POST body.");
                            }
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                "HTTP Handler Entity: Json content body is zero.");
                        }

                        return InvokeResult.Json(400, "Content body is zero length.");
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: Could not locate the model for Guid {guid}.");
                    }

                    return InvokeResult.Forbidden();
                }
                catch (Exception ex)
                {
                    log.Error($"HTTP Handler Entity: Error as {ex}.  Returning 500.");
                    return InvokeResult.Empty(500);
                }
            }
            catch (ClientRequestException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"HTTP Handler Entity: Client did not complete the request body as {ex.Message}.  " +
                        $"Returning {ex.StatusCode}.");
                }

                return InvokeResult.Empty(ex.StatusCode);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"HTTP Handler Entity: Error as {ex}.  Returning 500.");

                if (engine != null)
                {
                    engine.Context.Counters.HttpCounterAllError += 1;
                }

                return InvokeResult.Empty(500);
            }
        }

        private static bool RecallBodyHasValidTypes(
            global::Jube.Engine.Exhaustive.Models.ExhaustiveSearchInstance exhaustive, JObject recallBody)
        {
            foreach (var variable in exhaustive.NetworkVariablesInOrder)
            {
                if (!recallBody.TryGetValue(variable.Name, StringComparison.Ordinal, out var token))
                {
                    continue;
                }

                var valid = token.Type switch
                {
                    JTokenType.Integer => true,
                    JTokenType.Float => double.IsFinite(token.Value<double>()),
                    JTokenType.Boolean or JTokenType.Null => true,
                    JTokenType.String => double.TryParse(token.Value<string>(),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed),
                    _ => false
                };

                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<InvokeResult> ExhaustiveSearchInstanceAsync(string? guidRouteValue,
            Func<Task<MemoryStream>> readBodyAsync)
        {
            try
            {
                if (!PublicInvokeEnabled())
                {
                    return InvokeResult.Empty(404);
                }

                if (engine?.Context is not { Ready: true })
                {
                    return InvokeResult.Empty(503);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterExhaustive);

                var ms = await readBodyAsync().ConfigureAwait(false);

                if (!Guid.TryParse(guidRouteValue, out var guid))
                {
                    return InvokeResult.Empty(404);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Recall:  Recall received for {guid}.  Invoking handler.");
                }

                var foundExhaustive = engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                    .ActiveEntityAnalysisModels
                    .Where(w => w.Value.Collections.ExhaustiveModels.Any(a => a.Guid == guid)).ToList();

                if (foundExhaustive.Count == 0)
                {
                    return InvokeResult.Empty(404);
                }

                foreach (var (_, value) in foundExhaustive)
                {
                    if (!value.Collections.Users.Contains(userName))
                    {
                        continue;
                    }

                    JObject recallBody;
                    try
                    {
                        recallBody = JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()));
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        return InvokeResult.Json(400, "Malformed JSON in POST body.");
                    }

                    var exhaustive = value.Collections.ExhaustiveModels.First(a => a.Guid == guid);
                    if (!RecallBodyHasValidTypes(exhaustive, recallBody))
                    {
                        return InvokeResult.Json(400, "Exhaustive recall input has an invalid shape or types.");
                    }

                    var recalled = engine.Context.RecallExhaustive(guid, recallBody);
                    if (!double.IsFinite(recalled))
                    {
                        log.Error($"Exhaustive Recall: The recall for {guid} was not a finite number.  Returning 500.");

                        Interlocked.Increment(ref engine.Context.Counters.HttpCounterAllError);
                        return InvokeResult.Empty(500);
                    }

                    var response = Math.Round(recalled, 2);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Exhaustive Recall:  Has invoked the handler and returned a value of {value}.  Returning.");
                    }

                    return InvokeResult.Json(200, response);
                }

                return InvokeResult.Forbidden();
            }
            catch (ClientRequestException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"Exhaustive Recall: Client did not complete the request body as {ex.Message}.  " +
                        $"Returning {ex.StatusCode}.");
                }

                return InvokeResult.Empty(ex.StatusCode);
            }
            catch (Exception ex)
            {
                log.Error($"Exhaustive Recall:  An error has been raised as {ex}.  Returning 500.");

                if (engine?.Context != null)
                {
                    engine.Context.Counters.HttpCounterAllError += 1;
                }

                return InvokeResult.Empty(500);
            }
        }
    }
}
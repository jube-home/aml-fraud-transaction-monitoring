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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.QueryBuilder;
using Jube.Data.Repository;
using Jube.Dto.Filter;
using Jube.Dto.Query.EntityAnalysisModelBacktest;
using Jube.Dto.Validation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelBacktest;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.EntityAnalysisModelBacktest
{
    public sealed class EntityAnalysisModelBacktestService
    {
        private static readonly int[] permissions = [61];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string? reportConnectionString;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelBacktestService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings, string? reportConnectionString)
        {
            this.dbContext = dbContext;
            this.reportConnectionString = reportConnectionString;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelBacktestService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), null, token);
        }

        public static Task<EntityAnalysisModelBacktestService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, string? reportConnectionString, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), reportConnectionString, token);
        }

        internal static async Task<EntityAnalysisModelBacktestService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, string? reportConnectionString,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelBacktestResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelBacktest.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelBacktestResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelBacktest.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelBacktestResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelBacktestService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings,
                reportConnectionString);
        }

        [Description("Lists the fields a backtest's filter and class may use: FilterFields for FilterJson (the " +
                     "fields of a reprocessing rule) and ClassFields for ClassJson (everything an activation rule " +
                     "can see, plus Tag.<Name> as True or False for each of the model's tags).")]
        [ServiceOperation("EntityAnalysisModelBacktestFields", OperationKind.Read, Idempotent = true)]
        public Task<BacktestFieldsDto> FieldsAsync(
            [Description("The model.")] int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("Fields", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                return new BacktestFieldsDto
                {
                    FilterFields = ToFieldDtos(BuilderTarget.RuleText, await BacktestExecutor.FilterFieldsAsync(
                        dbContext, tenantRegistryId,
                        model.Id, token).ConfigureAwait(false)),
                    ClassFields = ToFieldDtos(BuilderTarget.InMemory, await BacktestExecutor.ClassFieldsAsync(dbContext,
                        tenantRegistryId,
                        model.Id, token).ConfigureAwait(false))
                };
            }, token);
        }

        [Description("Backtests a rule immediately, while the caller waits: reads a model's archived " +
                     "transactions newest first (up to Limit, at most BacktestOnlineMaxRows, optionally between " +
                     "From and To), keeps those the filter (FilterJson or FilterRuleText, run as a reprocessing " +
                     "rule) keeps, instances the rule exactly as the engine compiles and calls it on each with every " +
                     "value as it was when invoked, and with a class (ClassJson, which may use Tag.<Name>) counts " +
                     "the four raw confusion matrix cells with examples of each. No rates are worked out and " +
                     "nothing is stored. Submit larger backtests with EntityAnalysisModelBacktestSubmit. Invalid " +
                     "input is not an error: Valid is false and Errors says why.")]
        [ServiceOperation("EntityAnalysisModelBacktestRunRule", OperationKind.Read, Idempotent = true)]
        public Task<BacktestResultDto> RunRuleAsync(
            [Description("The filter, rule, class, range and limit.")]
            BacktestRequestDto? request,
            CancellationToken token = default)
        {
            return RunAsync("RunRule", async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var model = await RequireModelAsync(request.EntityAnalysisModelId, token).ConfigureAwait(false);
                var onlineMax = Setting("BacktestOnlineMaxRows", 10_000);
                if (request.Limit > onlineMax)
                {
                    return new BacktestResultDto
                    {
                        Errors =
                        [
                            Error("Limit", "LimitTooLarge",
                                $"An immediate backtest reads at most {onlineMax} transactions; submit it with " +
                                "EntityAnalysisModelBacktestSubmit to read more.")
                        ]
                    };
                }

                await using var archive = ReportDbContext();
                var execution = await BacktestExecutor.ExecuteAsync(dbContext, tenantRegistryId,
                    ToSpecification(request, model.Id, request.Limit), (int)Setting("BacktestPageSize", 5000), null,
                    token, archive).ConfigureAwait(false);
                return execution.Valid
                    ? ToDto(execution.Result)
                    : new BacktestResultDto { Errors = execution.Errors.Select(ToDto).ToList() };
            }, token);
        }

        [Description("Submits a backtest to run in the background on the engine's backtest threads and returns at " +
                     "once with the instance, which starts Pending; poll it with EntityAnalysisModelBacktestStatus and " +
                     "read the counts with EntityAnalysisModelBacktestResult when it has Succeeded. Takes the same " +
                     "request as EntityAnalysisModelBacktestRunRule, with Limit up to BacktestMaxRows (millions " +
                     "are practical). The request is checked and compiled first; an invalid one is refused with " +
                     "Errors and nothing is submitted.")]
        [ServiceOperation("EntityAnalysisModelBacktestSubmit", OperationKind.Write, Idempotent = false)]
        public Task<BacktestSubmitResultDto> SubmitAsync(
            [Description("The filter, rule, class, range and limit.")]
            BacktestRequestDto? request,
            CancellationToken token = default)
        {
            return RunAsync("Submit", async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var model = await RequireModelAsync(request.EntityAnalysisModelId, token).ConfigureAwait(false);
                var specification = ToSpecification(request, model.Id,
                    Math.Min(request.Limit, Setting("BacktestMaxRows", 1_000_000)));
                var (plan, errors) = await BacktestExecutor.PrepareAsync(dbContext, tenantRegistryId, specification,
                    token).ConfigureAwait(false);
                if (plan == null)
                {
                    return new BacktestSubmitResultDto { Errors = errors.Select(ToDto).ToList() };
                }

                var instance = await new EntityAnalysisModelBacktestInstanceRepository(dbContext, tenantRegistryId)
                    .InsertAsync(
                        new Data.Poco.EntityAnalysisModelBacktestInstance
                        {
                            EntityAnalysisModelId = model.Id,
                            RuleType = specification.RuleType,
                            RuleId = request.RuleId,
                            Request = JsonConvert.SerializeObject(specification),
                            Status = (short)BacktestInstanceStatus.Pending,
                            CreatedUser = userName
                        }, token).ConfigureAwait(false);
                return new BacktestSubmitResultDto { Valid = true, Instance = ToDto(instance, request.RuleId) };
            }, token);
        }

        [Description("Returns a submitted backtest's status and progress, and when it has finished its headline " +
                     "counts.")]
        [ServiceOperation("EntityAnalysisModelBacktestStatus", OperationKind.Read, Idempotent = true)]
        public Task<BacktestInstanceDto> StatusAsync(
            [Description("The instance's id.")] int id,
            CancellationToken token = default)
        {
            return RunAsync("Status", async () => ToDto(await RequireInstanceAsync(id, token).ConfigureAwait(false)),
                token);
        }

        [Description("Lists a model's submitted backtests, newest first, optionally only those of one kind of " +
                     "rule or one saved rule.")]
        [ServiceOperation("EntityAnalysisModelBacktestList", OperationKind.Read, Idempotent = true)]
        public Task<List<BacktestInstanceDto>> ListAsync(
            [Description("The model.")] int entityAnalysisModelId,
            [Description("Only backtests of this kind of rule: GatewayRule, ActivationRule or ReprocessingRule.")]
            string? ruleType = null,
            [Description("Only backtests started from this saved rule.")]
            int? ruleId = null,
            [Description("The most instances to return; at most 100.")]
            int take = 20,
            CancellationToken token = default)
        {
            return RunAsync("List", async () =>
            {
                var model = await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                return (await new EntityAnalysisModelBacktestInstanceRepository(dbContext, tenantRegistryId)
                        .GetByRuleAsync(model.Id, ruleType, ruleId, Math.Clamp(take, 1, 100), token)
                        .ConfigureAwait(false))
                    .Select(r => ToDto(r)).ToList();
            }, token);
        }

        [Description("Returns a finished backtest's full result: every count and the examples of each outcome. " +
                     "A backtest that has not finished, or finished without a result, comes back with Valid false " +
                     "and says why.")]
        [ServiceOperation("EntityAnalysisModelBacktestResult", OperationKind.Read, Idempotent = true)]
        public Task<BacktestResultDto> ResultAsync(
            [Description("The instance's id.")] int id,
            CancellationToken token = default)
        {
            return RunAsync("Result", async () =>
            {
                var instance = await RequireInstanceAsync(id, token).ConfigureAwait(false);
                if (instance.Result == null)
                {
                    return new BacktestResultDto
                    {
                        Errors =
                        [
                            Error("Status", "ResultUnavailable",
                                $"The backtest is {(BacktestInstanceStatus)instance.Status} and has no result" +
                                (instance.Error == null ? "." : $": {instance.Error}"))
                        ]
                    };
                }

                return ToDto(JsonConvert.DeserializeObject<BacktestResult>(instance.Result)!);
            }, token);
        }

        [Description("Stops a submitted backtest: a Pending one is cancelled at once, a Running one is asked to " +
                     "stop and is Cancelled when its backtest thread next reads a page.")]
        [ServiceOperation("EntityAnalysisModelBacktestStop", OperationKind.Write, Idempotent = true)]
        public Task<BacktestInstanceDto> StopAsync(
            [Description("The instance's id.")] int id,
            CancellationToken token = default)
        {
            return RunAsync("Stop", async () =>
            {
                await RequireInstanceAsync(id, token).ConfigureAwait(false);
                await new EntityAnalysisModelBacktestInstanceRepository(dbContext, tenantRegistryId)
                    .StopAsync(id, token)
                    .ConfigureAwait(false);
                return ToDto(await RequireInstanceAsync(id, token).ConfigureAwait(false));
            }, token);
        }

        [Description("Explains one archived transaction for a backtest request: whether the filter kept it, " +
                     "whether the rule fired and whether it is positive, with the values the rule and filter " +
                     "read. Use it on an example from a result to see why it was counted as it was.")]
        [ServiceOperation("EntityAnalysisModelBacktestExplain", OperationKind.Read, Idempotent = true)]
        public Task<BacktestExplanationDto> ExplainAsync(
            [Description("The backtest request, as run.")]
            BacktestRequestDto? request,
            [Description("The archived transaction's entry Guid.")]
            Guid entityAnalysisModelInstanceEntryGuid,
            CancellationToken token = default)
        {
            return RunAsync("Explain", async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var model = await RequireModelAsync(request.EntityAnalysisModelId, token).ConfigureAwait(false);
                await using var archive = ReportDbContext();
                var (explanation, errors) = await BacktestExecutor.ExplainAsync(dbContext, tenantRegistryId,
                        ToSpecification(request, model.Id, 1), entityAnalysisModelInstanceEntryGuid, token, archive)
                    .ConfigureAwait(false);
                if (explanation == null)
                {
                    return new BacktestExplanationDto { Errors = errors.Select(ToDto).ToList() };
                }

                return new BacktestExplanationDto
                {
                    Valid = true,
                    EntityAnalysisModelInstanceEntryGuid = explanation.EntityAnalysisModelInstanceEntryGuid,
                    EntryKeyValue = explanation.EntryKeyValue,
                    ReferenceDate = explanation.ReferenceDate,
                    Tags = explanation.Tags.ToList(),
                    PassedFilter = explanation.PassedFilter,
                    FilterError = explanation.FilterError,
                    Fired = explanation.Fired,
                    RuleError = explanation.RuleError,
                    Positive = explanation.Positive,
                    Values = explanation.Values.Select(v => new BacktestValueDto { Name = v.Name, Value = v.Value })
                        .ToList()
                };
            }, token);
        }

        private async Task<Data.Poco.EntityAnalysisModelBacktestInstance> RequireInstanceAsync(int id,
            CancellationToken token)
        {
            return await new EntityAnalysisModelBacktestInstanceRepository(dbContext, tenantRegistryId)
                       .GetByIdAsync(id, token).ConfigureAwait(false)
                   ?? throw new NotFoundException(strings[EntityAnalysisModelBacktestResources.InstanceNotFound]);
        }

        private static BacktestSpecification ToSpecification(BacktestRequestDto request, int modelId, long limit)
        {
            return new BacktestSpecification(modelId, request.RuleType, request.RuleText, request.FilterJson,
                request.FilterRuleText, request.ClassJson, request.From, request.To, Math.Max(1, limit),
                request.SampleSize);
        }

        private static BacktestRequestDto ToRequest(BacktestSpecification specification, int? ruleId)
        {
            return new BacktestRequestDto
            {
                EntityAnalysisModelId = specification.EntityAnalysisModelId, RuleType = specification.RuleType,
                RuleId = ruleId, RuleText = specification.RuleText, FilterJson = specification.FilterJson,
                FilterRuleText = specification.FilterRuleText, ClassJson = specification.ClassJson,
                From = specification.From, To = specification.To, Limit = specification.Limit,
                SampleSize = specification.SampleSize
            };
        }

        private static BacktestInstanceDto ToDto(Data.Poco.EntityAnalysisModelBacktestInstance instance,
            int? ruleId = null)
        {
            var specification = JsonConvert.DeserializeObject<BacktestSpecification>(instance.Request)!;
            var result = instance.Result == null
                ? null
                : JsonConvert.DeserializeObject<BacktestResult>(instance.Result);
            return new BacktestInstanceDto
            {
                Id = instance.Id, Guid = instance.Guid, EntityAnalysisModelId = instance.EntityAnalysisModelId,
                RuleType = instance.RuleType, RuleId = instance.RuleId ?? ruleId,
                Status = ((BacktestInstanceStatus)instance.Status).ToString(), Progress = instance.Progress,
                Scanned = instance.Scanned,
                Evaluated = instance.Evaluated, CreatedDate = instance.CreatedDate, CreatedUser = instance.CreatedUser,
                StartedDate = instance.StartedDate, CompletedDate = instance.CompletedDate, Error = instance.Error,
                Request = ToRequest(specification, instance.RuleId ?? ruleId),
                Fired = result?.Fired,
                Positives = result is { ClassDefined: true } ? result.Positives : null,
                TruePositives = result is { ClassDefined: true } ? result.TruePositives : null,
                FalsePositives = result is { ClassDefined: true } ? result.FalsePositives : null,
                FalseNegatives = result is { ClassDefined: true } ? result.FalseNegatives : null,
                TrueNegatives = result is { ClassDefined: true } ? result.TrueNegatives : null
            };
        }

        private static List<FilterFieldDto> ToFieldDtos(BuilderTarget target, IEnumerable<BacktestField> fields)
        {
            return fields.Select(f => new FilterFieldDto
            {
                Name = f.Name, DataType = f.Type.ToString(),
                Operators = (target == BuilderTarget.RuleText
                    ? BuilderProfile.RuleTextOperators
                    : BuilderProfile.Operators)[f.Type].ToList()
            }).ToList();
        }

        private static BacktestResultDto ToDto(BacktestResult backtest)
        {
            return new BacktestResultDto
            {
                Valid = true,
                Scanned = backtest.Scanned,
                FilteredOut = backtest.FilteredOut,
                FilterErrors = backtest.FilterErrors,
                Evaluated = backtest.Evaluated,
                LimitReached = backtest.LimitReached,
                EarliestReferenceDate = backtest.EarliestReferenceDate,
                LatestReferenceDate = backtest.LatestReferenceDate,
                Fired = backtest.Fired,
                NotFired = backtest.NotFired,
                RuntimeErrors = backtest.RuntimeErrors,
                Aborted = backtest.Aborted,
                AbortReason = backtest.AbortReason,
                ClassDefined = backtest.ClassDefined,
                Positives = backtest.Positives,
                TruePositives = backtest.TruePositives,
                FalsePositives = backtest.FalsePositives,
                FalseNegatives = backtest.FalseNegatives,
                TrueNegatives = backtest.TrueNegatives,
                DurationMicroseconds = backtest.DurationMicroseconds,
                TagsInSample = backtest.TagsInSample.OrderByDescending(t => t.Value)
                    .ThenBy(t => t.Key, StringComparer.Ordinal)
                    .Select(t => new BacktestTagCountDto { Name = t.Key, Count = t.Value }).ToList(),
                TruePositiveSamples = backtest.TruePositiveSamples.Select(ToDto).ToList(),
                FalsePositiveSamples = backtest.FalsePositiveSamples.Select(ToDto).ToList(),
                FalseNegativeSamples = backtest.FalseNegativeSamples.Select(ToDto).ToList(),
                ErrorSamples = backtest.ErrorSamples.Select(ToDto).ToList()
            };
        }

        private static BacktestSampleDto ToDto(BacktestSample sample)
        {
            return new BacktestSampleDto
            {
                EntityAnalysisModelInstanceEntryGuid = sample.EntityAnalysisModelInstanceEntryGuid,
                EntryKeyValue = sample.EntryKeyValue,
                ReferenceDate = sample.ReferenceDate,
                Fired = sample.Fired,
                Positive = sample.Positive,
                Error = sample.Error
            };
        }

        private static ValidationErrorDto ToDto(BacktestError error)
        {
            return new ValidationErrorDto
            {
                PropertyName = error.Property, ErrorCode = error.Code, Message = error.Message, Line = error.Line,
                Start = error.Start, Length = error.Length
            };
        }

        private DbContext? ReportDbContext()
        {
            return string.IsNullOrWhiteSpace(reportConnectionString)
                ? null
                : DataConnectionDbContext.GetResilientDbContextDataConnection(reportConnectionString, log);
        }

        private static long Setting(string key, long fallback)
        {
            return long.TryParse(Environment.GetEnvironmentVariable(key), out var value) && value > 0
                ? value
                : fallback;
        }

        private async Task<T> RunAsync<T>(string operation, Func<Task<T>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityAnalysisModelBacktest", operation, userName, tenantRegistryId,
                auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelBacktest.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"EntityAnalysisModelBacktest.{operation}");
                return await body().ConfigureAwait(false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelBacktest.{operation}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Data.Poco.EntityAnalysisModel> RequireModelAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, tenantRegistryId)
                .GetByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            if (model == null || model.TenantRegistryId != tenantRegistryId)
            {
                throw new NotFoundException(strings[EntityAnalysisModelBacktestResources.ModelNotFound]);
            }

            return model;
        }

        private static ValidationErrorDto Error(string propertyName, string errorCode, string message)
        {
            return new ValidationErrorDto { PropertyName = propertyName, ErrorCode = errorCode, Message = message };
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelBacktestResources.PermissionDenied], permissions);
        }
    }
}
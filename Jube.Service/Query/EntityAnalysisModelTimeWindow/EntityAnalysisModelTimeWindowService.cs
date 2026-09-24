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
using Jube.Data.Repository;
using Jube.Dto.Query.EntityAnalysisModelTimeWindow;
using Jube.Dto.Validation;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelTimeWindow;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelTimeWindow
{
    public sealed class EntityAnalysisModelTimeWindowService
    {
        private static readonly int[] permissions = [8, 10, 13, 14, 16, 17, 25, 26];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelTimeWindowService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelTimeWindowService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelTimeWindowService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelTimeWindowResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelTimeWindow.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTimeWindowResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelTimeWindow.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTimeWindowResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelTimeWindowService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the kinds of time window and the interval codes each allows: s seconds, n minutes, " +
                     "h hours, d days, w weeks, m months and y years. AbstractionRule and SearchKey windows use " +
                     "the engine's Visual Basic date arithmetic and allow s, n, h and d; TtlCounter windows allow " +
                     "s, n, h, d, m and y; Calendar windows are for the agent's own date ranges, such as a " +
                     "backtest's From, and also allow w.")]
        [ServiceOperation("EntityAnalysisModelTimeWindowIntervals", OperationKind.Read, Idempotent = true)]
        public Task<List<TimeWindowKindDto>> IntervalsAsync(CancellationToken token = default)
        {
            return RunAsync("Intervals", () => Task.FromResult(TimeWindow.Intervals.Select(k => new TimeWindowKindDto
            {
                Kind = k.Key.ToString(),
                Intervals = k.Value.Select(i => new TimeWindowIntervalDto
                    { Code = i, Name = TimeWindow.IntervalNames[i] }).ToList(),
                Arithmetic = Arithmetic(k.Key)
            }).ToList()), token);
        }

        [Description("Works out the exact start and end of a time window that ends at a reference date, the " +
                     "way the engine does for the kind of window, so dates are never worked out by hand: e.g. " +
                     "Calendar, d, 30 gives the 30 days up to now. Both ends are included. Invalid input is not " +
                     "an error: Valid is false and Errors says why.")]
        [ServiceOperation("EntityAnalysisModelTimeWindowCalculate", OperationKind.Read, Idempotent = true)]
        public Task<TimeWindowDto> CalculateAsync(
            [Description("The kind of window: AbstractionRule, SearchKey, TtlCounter or Calendar.")]
            string? kind,
            [Description("The interval code, e.g. d for days; see EntityAnalysisModelTimeWindowIntervals.")]
            string? interval,
            [Description("How many intervals the window spans.")]
            int value,
            [Description(
                "The end of the window; the current time when left out. Without an offset it is taken as UTC.")]
            DateTime? referenceDate = null,
            CancellationToken token = default)
        {
            return RunAsync("Calculate", () =>
            {
                if (!Enum.TryParse<TimeWindowKind>(kind, true, out var parsed) ||
                    !Enum.IsDefined(parsed) || int.TryParse(kind, out _))
                {
                    return Task.FromResult(Refused("Kind", "KindInvalid",
                        string.Format(strings[EntityAnalysisModelTimeWindowResources.KindInvalid], kind,
                            string.Join(", ", Enum.GetNames<TimeWindowKind>()))));
                }

                return Task.FromResult(Window(parsed, interval, value, Reference(referenceDate), "Interval"));
            }, token);
        }

        [Description("Works out the window an abstraction rule aggregates over for a transaction with the given " +
                     "reference date, exactly as the engine does: the rule's interval, shortened to the search " +
                     "key's TTL when that is shorter.")]
        [ServiceOperation("EntityAnalysisModelTimeWindowAbstractionRule", OperationKind.Read, Idempotent = true)]
        public Task<TimeWindowDto> AbstractionRuleAsync(
            [Description("Id of the abstraction rule.")]
            int entityAnalysisModelAbstractionRuleId,
            [Description("The transaction's reference date; the current time when left out.")]
            DateTime? referenceDate = null,
            CancellationToken token = default)
        {
            return RunAsync("AbstractionRule", async () =>
            {
                var rule = await new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId)
                    .GetByIdAsync(entityAnalysisModelAbstractionRuleId, token).ConfigureAwait(false);
                if (rule?.EntityAnalysisModelId == null)
                {
                    throw new NotFoundException(
                        strings[EntityAnalysisModelTimeWindowResources.AbstractionRuleNotFound]);
                }

                var model = await RequireModelAsync(rule.EntityAnalysisModelId.Value, token).ConfigureAwait(false);
                if (rule.Search != 1)
                {
                    return Refused("Search", "NotASearchRule",
                        strings[EntityAnalysisModelTimeWindowResources.NotASearchRule]);
                }

                var searchKey = (await new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(model.Id, token).ConfigureAwait(false))
                    .FirstOrDefault(x => x.Name == rule.SearchKey && x.Active == 1);
                var settings = AbstractionSettings.FromRecord(rule.Id, rule.Name, rule.SearchKey,
                    rule.SearchFunctionKey, rule.SearchFunctionTypeId, rule.SearchInterval, rule.SearchValue,
                    rule.Offset == 1, rule.OffsetTypeId, rule.OffsetValue);
                var ttlInterval = searchKey?.SearchKeyTtlInterval ?? "d";
                var ttlValue = searchKey?.SearchKeyTtlIntervalValue ?? 0;
                var reference = Reference(referenceDate);
                var window = TimeWindow.AbstractionRule(settings.IntervalType, settings.IntervalValue, ttlInterval,
                    ttlValue, reference);
                var dto = ToDto(window);
                dto.SearchKeyShortensTheWindow = AbstractionWindow.SearchKeyShortensTheWindow(settings.IntervalType,
                    settings.IntervalValue, ttlInterval, ttlValue, reference);
                dto.Note = (dto.SearchKeyShortensTheWindow
                               ? $"The search key {rule.SearchKey} keeps history for only {ttlValue} " +
                                 $"{Name(ttlInterval)}, which is shorter than the rule's {settings.IntervalValue} " +
                                 $"{Name(settings.IntervalType)}, so the engine uses the search key's window. "
                               : string.Empty) +
                           "Transactions for the same search key value with a reference date in the window are " +
                           "aggregated, up to the cache fetch limit." +
                           (settings.EnableOffset
                               ? " The rule's offset then skips matches by count, which does not change the window."
                               : string.Empty);
                return dto;
            }, token);
        }

        [Description("Works out the window a TTL counter counts over for a transaction with the given reference " +
                     "date, exactly as the engine does. For an online aggregation counter it is the window read at " +
                     "invocation; otherwise it is when increments expire, and a counter that lives forever has none.")]
        [ServiceOperation("EntityAnalysisModelTimeWindowTtlCounter", OperationKind.Read, Idempotent = true)]
        public Task<TimeWindowDto> TtlCounterAsync(
            [Description("Id of the TTL counter.")]
            int entityAnalysisModelTtlCounterId,
            [Description("The transaction's reference date; the current time when left out.")]
            DateTime? referenceDate = null,
            CancellationToken token = default)
        {
            return RunAsync("TtlCounter", async () =>
            {
                var counter = await new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId)
                    .GetByIdAsync(entityAnalysisModelTtlCounterId, token).ConfigureAwait(false);
                if (counter?.EntityAnalysisModelId == null)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelTimeWindowResources.TtlCounterNotFound]);
                }

                await RequireModelAsync(counter.EntityAnalysisModelId.Value, token).ConfigureAwait(false);
                if (counter.EnableLiveForever == 1)
                {
                    return new TimeWindowDto
                    {
                        Valid = true, Kind = nameof(TimeWindowKind.TtlCounter), LiveForever = true,
                        To = Reference(referenceDate),
                        Note = "The counter lives forever: its increments never expire, so it counts every " +
                               "transaction since it was created."
                    };
                }

                var dto = Window(TimeWindowKind.TtlCounter, counter.TtlCounterInterval,
                    counter.TtlCounterValue ?? 0, Reference(referenceDate), "TtlCounterInterval");
                if (dto.Valid)
                {
                    dto.Note = counter.OnlineAggregation == 1
                        ? "An online aggregation counter: at invocation the engine counts the increments for the " +
                          "data value with a reference date in this window."
                        : "A running count: increments older than the interval are taken off by a background " +
                          $"job that runs every {counter.ResolutionInterval ?? "?"} interval, so the count covers " +
                          "about this window, not exactly.";
                }

                return dto;
            }, token);
        }

        private TimeWindowDto Window(TimeWindowKind kind, string? interval, int value, DateTime reference,
            string propertyName)
        {
            if (value < 0)
            {
                return Refused("Value", "ValueInvalid", strings[EntityAnalysisModelTimeWindowResources.ValueInvalid]);
            }

            if (!TimeWindow.IsAllowed(kind, interval!))
            {
                return Refused(propertyName, "IntervalInvalid",
                    string.Format(strings[EntityAnalysisModelTimeWindowResources.IntervalInvalid], interval, kind,
                        string.Join(", ", TimeWindow.Intervals[kind])));
            }

            var dto = ToDto(TimeWindow.Calculate(kind, interval!, value, reference));
            dto.Note = Arithmetic(kind);
            return dto;
        }

        private static TimeWindowDto ToDto(TimeWindowResult window)
        {
            return new TimeWindowDto
            {
                Valid = true,
                Kind = window.Kind.ToString(),
                Interval = window.Interval,
                IntervalName = Name(window.Interval),
                Value = window.Value,
                From = window.From,
                To = window.To,
                LengthSeconds = window.Length.TotalSeconds
            };
        }

        private static string Name(string interval)
        {
            return TimeWindow.IntervalNames.TryGetValue(interval, out var name) ? name : interval;
        }

        private static string Arithmetic(TimeWindowKind kind)
        {
            return kind switch
            {
                TimeWindowKind.TtlCounter => "Subtracts the interval from the reference date; m and y are " +
                                             "calendar months and years.",
                TimeWindowKind.Calendar => "Subtracts the interval from the reference date; w is 7 days, and m " +
                                           "and y are calendar months and years.",
                _ => "Subtracts the interval with Visual Basic DateAdd, as the engine does for abstraction rules " +
                     "and search keys."
            };
        }

        private static DateTime Reference(DateTime? referenceDate)
        {
            return referenceDate switch
            {
                null => DateTime.UtcNow,
                { Kind: DateTimeKind.Unspecified } date => DateTime.SpecifyKind(date, DateTimeKind.Utc),
                { Kind: DateTimeKind.Local } date => date.ToUniversalTime(),
                { } date => date
            };
        }

        private static TimeWindowDto Refused(string propertyName, string errorCode, string message)
        {
            return new TimeWindowDto { Errors = [Error(propertyName, errorCode, message)] };
        }

        private async Task<T> RunAsync<T>(string operation, Func<Task<T>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTimeWindow", operation, userName, tenantRegistryId,
                auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelTimeWindow.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"EntityAnalysisModelTimeWindow.{operation}");
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
                log.Error($"EntityAnalysisModelTimeWindow.{operation}: unexpected failure user={userName}", ex);
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
                throw new NotFoundException(strings[EntityAnalysisModelTimeWindowResources.ModelNotFound]);
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

            throw new ForbiddenException(strings[EntityAnalysisModelTimeWindowResources.PermissionDenied], permissions);
        }
    }
}
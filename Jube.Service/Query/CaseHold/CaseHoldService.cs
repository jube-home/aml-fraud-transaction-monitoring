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
using Jube.Case;
using Jube.Case.Outbound;
using CaseRow = Jube.Data.Poco.Case;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseHoldTransactionsQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models.Hold;
using Jube.Dto.Hold;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseHold;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseHold;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Query.CaseHold;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseHold
{
    public sealed class CaseHoldService
    {
        private const string Area = "CaseHold";
        private static readonly TimeSpan HookTimeout = TimeSpan.FromSeconds(15);
        private static readonly int[] permissions = [1];
        private static readonly int[] decisionPermissions = [68];
        private readonly ArchiveRepository archiveRepository;
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowRepository caseWorkflowRepository;
        private readonly DbContext dbContext;
        private readonly GuardedHttpEndpoint guardedHttp;
        private readonly CaseWorkflowDispatchRepository dispatchRepository;
        private readonly CaseWorkflowReleaseHookRepository hookRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseHoldDecisionDtoValidator validator;

        private CaseHoldService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            guardedHttp = GuardedHttpEndpoint.For(dynamicEnvironment);
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            archiveRepository = new ArchiveRepository(dbContext);
            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
            hookRepository = new CaseWorkflowReleaseHookRepository(dbContext, userName);
            dispatchRepository = new CaseWorkflowDispatchRepository(dbContext, tenantRegistryId, userName);
            validator = new CaseHoldDecisionDtoValidator(strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static IReadOnlyList<int> RequiredDecisionPermissions => decisionPermissions;

        public static Task<CaseHoldService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseHoldService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseHoldResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseHoldResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseHoldResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseHoldService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Lists the payments held, declined, released or refused on the transactions of a case: every " +
                     "archived transaction of the case's model that shares the case's key and carries a hold, " +
                     "most recent first, up to 50. Each row says the state of the hold, the response elevation " +
                     "range that stamped it, when the payment hold diary of the case falls due, who decided and " +
                     "why, the messages sent about it with their outcomes, and the release hook that would " +
                     "release it. The case must be visible to the caller's roles. Nothing is changed.")]
        [ServiceOperation("CaseHoldList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseHoldDto>> ListAsync(
            [Description("Server-assigned identifier of the case to list the holds of.")]
            int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.List", permissions);

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false) ??
                                   throw NotFound(Area + ".List");

                var holds = await LoadAsync(existingCase, token).ConfigureAwait(false);
                op.Rows(holds.Count);

                return [.. holds.Select(s => s.Dto)];
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"{Area}.List: unexpected failure caseId={caseId} user={userName}", ex);
                throw;
            }
        }

        [Description("Releases a payment held on a transaction of a case: calls the release hook of the case " +
                     "workflow that handles the transaction, and only when it answers exactly 200 records the " +
                     "payment as Released, with who, when, why and through which hook, in the Hold block of the " +
                     "archived transaction. Any other answer, or no handling hook, leaves the payment held and " +
                     "records the failed attempt. A reason is required, the case must not be locked by another " +
                     "user, and only a Held payment can be released. SIDE EFFECTS: one outbound HTTP call to " +
                     "the release hook, which moves money in the payment system; not idempotent.")]
        [ServiceOperation("CaseHoldRelease", OperationKind.Write, Idempotent = false)]
        public Task<CaseHoldDecisionResultDto> ReleaseAsync(
            [Description("The case, the held transaction and the reason for the release.")]
            CaseHoldDecisionDto? model,
            CancellationToken token = default)
        {
            return DecideAsync(true, model, "Release", token);
        }

        [Description("Refuses a payment held on a transaction of a case: records it as Refused, with who, when " +
                     "and why, in the Hold block of the archived transaction, calling the refuse URL of the " +
                     "release hook that handles the transaction when it has one, in which case only an answer " +
                     "of exactly 200 records the refusal. A reason is required, the case must not be locked by " +
                     "another user, and only a Held payment can be refused. SIDE EFFECTS: one outbound HTTP " +
                     "call when the release hook has a refuse URL; not idempotent.")]
        [ServiceOperation("CaseHoldRefuse", OperationKind.Write, Idempotent = false)]
        public Task<CaseHoldDecisionResultDto> RefuseAsync(
            [Description("The case, the held transaction and the reason for the refusal.")]
            CaseHoldDecisionDto? model,
            CancellationToken token = default)
        {
            return DecideAsync(false, model, "Refuse", token);
        }

        private async Task<CaseHoldDecisionResultDto> DecideAsync(bool release, CaseHoldDecisionDto? model,
            string operation, CancellationToken token)
        {
            using var op = OperationScope.Start(Area, operation, userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.{operation}", decisionPermissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    throw new DtoValidationException(results);
                }

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token)
                                       .ConfigureAwait(false) ??
                                   throw NotFound($"{Area}.{operation}");

                if (existingCase.Locked == 1 &&
                    !string.Equals(existingCase.LockedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CaseLockedException(strings[CaseHoldResources.CaseLocked]);
                }

                var holds = await LoadAsync(existingCase, token).ConfigureAwait(false);

                var held = holds.FirstOrDefault(f =>
                               f.Dto.EntityAnalysisModelInstanceEntryGuid == model.EntityAnalysisModelInstanceEntryGuid) ??
                           throw NotFound($"{Area}.{operation}");

                if (held.Dto.StateId != (int)ResponseHoldState.Held)
                {
                    throw new HoldStateException(strings[CaseHoldResources.NotHeld]);
                }

                var calls = held.Hook != null &&
                            !string.IsNullOrWhiteSpace(release ? held.Hook.ReleaseUrl : held.Hook.RefuseUrl);

                if (release && !calls)
                {
                    throw new NoReleaseHookException(strings[CaseHoldResources.NoReleaseHook]);
                }

                Failure? failure = null;
                int? httpStatus = null;

                var update = await archiveRepository.UpdateHoldAsync(model.EntityAnalysisModelInstanceEntryGuid,
                    async hold =>
                    {
                        if (hold.StateId != (int)ResponseHoldState.Held)
                        {
                            throw new HoldStateException(strings[CaseHoldResources.NotHeld]);
                        }

                        var now = DateTime.UtcNow;
                        var leg = calls
                            ? await CallAsync(held, existingCase, release, model.Reason!, hold, token)
                                .ConfigureAwait(false)
                            : null;

                        if (leg != null)
                        {
                            hold.Legs.Add(leg.Leg);
                            httpStatus = leg.Leg.HttpStatus;

                            if (leg.Failure != null)
                            {
                                failure = leg.Failure;

                                return hold;
                            }
                        }

                        hold.StateId = (int)(release ? ResponseHoldState.Released : ResponseHoldState.Refused);
                        hold.Decision = new ResponseHoldDecision
                        {
                            Actor = userName,
                            Date = now,
                            Reason = model.Reason!.Trim(),
                            ReleaseHookId = held.Hook?.Id,
                            ReleaseHookName = held.Hook?.Name,
                            HttpStatus = httpStatus
                        };

                        return hold;
                    }, token).ConfigureAwait(false);

                if (update.Archive != null)
                {
                    await caseRepository.UpdateArchiveJsonAsync(model.EntityAnalysisModelInstanceEntryGuid,
                        update.Archive.Json, token).ConfigureAwait(false);
                }

                if (failure != null)
                {
                    op.Outcome("hook_failed");

                    throw new HoldHookException(failure.Message, httpStatus);
                }

                if (update.Archive == null)
                {
                    throw NotFound($"{Area}.{operation}");
                }

                op.Entity(model.CaseId);
                op.Version(update.Archive.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.{operation}: caseId={model.CaseId} " +
                             $"entry={model.EntityAnalysisModelInstanceEntryGuid} hook={held.Hook?.Name} " +
                             $"status={httpStatus} user={userName}");
                }

                return new CaseHoldDecisionResultDto
                {
                    StateId = update.Hold!.StateId,
                    ReleaseHookName = held.Hook?.Name,
                    HttpStatus = httpStatus,
                    Version = update.Archive.Version.GetValueOrDefault()
                };
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
                throw;
            }
            catch (HoldStateException)
            {
                op.Outcome("not_held");
                throw;
            }
            catch (CaseLockedException)
            {
                op.Outcome("locked");
                throw;
            }
            catch (NoReleaseHookException)
            {
                op.Outcome("no_hook");
                throw;
            }
            catch (HoldHookException)
            {
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
                log.Error($"{Area}.{operation}: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<LegResult> CallAsync(HeldTransaction held, CaseRow existingCase, bool release,
            string reason, ResponseHold hold, CancellationToken token)
        {
            var hook = held.Hook!;
            var action = release ? "release" : "refuse";
            var kind = release ? ResponseHoldLegKind.ReleaseHook : ResponseHoldLegKind.RefuseHook;
            var post = hook.HttpEndpointTypeId.GetValueOrDefault(1) == (byte)HttpEndpointType.Post;

            var values = new Dictionary<string, string?>(held.Payload, StringComparer.OrdinalIgnoreCase)
            {
                ["Hold.Name"] = hold.RangeName,
                ["Hold.Elevation"] = hold.Elevation.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Hold.EntryGuid"] = held.Dto.EntityAnalysisModelInstanceEntryGuid.ToString(),
                ["Decision.Action"] = action,
                ["Decision.Reason"] = reason.Trim(),
                ["Decision.User"] = userName
            };

            var (url, refusal) = WebhookTarget.Resolve(release ? hook.ReleaseUrl : hook.RefuseUrl,
                template => HookTokens.Replace(template, values, HookTokens.Url));

            if (url == null)
            {
                var detail = string.Format(strings[CaseHoldResources.HookRefused], refusal);

                await AuditAsync(existingCase, hook, post, release ? hook.ReleaseUrl : hook.RefuseUrl, null,
                    CaseDispatchOutcome.Refused, detail, null, Guid.Empty, token).ConfigureAwait(false);

                return new LegResult(NewLeg(kind, ResponseHoldLegOutcome.Refused, null, null, detail),
                    new Failure(detail));
            }

            string? headerName = null;
            string? headerValue = null;

            if (!string.IsNullOrEmpty(hook.HeaderName) && !string.IsNullOrEmpty(hook.CredentialVariable))
            {
                headerValue = HookCredentials.ReadSecret(hook.CredentialVariable);

                if (string.IsNullOrEmpty(headerValue))
                {
                    var detail = strings[CaseHoldResources.CredentialMissing];

                    await AuditAsync(existingCase, hook, post, url, null, CaseDispatchOutcome.Refused, detail, null,
                        Guid.Empty, token).ConfigureAwait(false);

                    return new LegResult(NewLeg(kind, ResponseHoldLegOutcome.Refused, new Uri(url).GetLeftPart(UriPartial.Path), null, detail),
                        new Failure(detail));
                }

                headerName = hook.HeaderName;
            }

            var body = post ? Body(hook, values, held, existingCase, hold, action, reason) : null;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(HookTimeout);

            var result = post
                ? await guardedHttp.PostAsync(url, body!, log, headerName, headerValue, timeout.Token)
                    .ConfigureAwait(false)
                : await guardedHttp.GetAsync(url, log, headerName, headerValue, timeout.Token)
                    .ConfigureAwait(false);

            if (result.Refusal != null)
            {
                var detail = string.Format(strings[CaseHoldResources.HookBlockedByPolicy], result.Refusal);

                await AuditAsync(existingCase, hook, post, new Uri(url).GetLeftPart(UriPartial.Path), body,
                    CaseDispatchOutcome.Refused, detail, result.Status, result.CorrelationId, token)
                    .ConfigureAwait(false);

                return new LegResult(NewLeg(kind, ResponseHoldLegOutcome.Refused,
                    new Uri(url).GetLeftPart(UriPartial.Path), result.Status, detail), new Failure(detail));
            }

            var released = result.Ok && result.Status == 200;
            var failureText = released
                ? null
                : result.Ok
                    ? string.Format(strings[CaseHoldResources.HookStatusNotOk], result.Status)
                    : string.Format(strings[CaseHoldResources.HookFailed], result.Error);

            await AuditAsync(existingCase, hook, post, url, body,
                released ? CaseDispatchOutcome.Sent : CaseDispatchOutcome.Failed, failureText, result.Status,
                result.CorrelationId, token).ConfigureAwait(false);

            return new LegResult(
                NewLeg(kind, released ? ResponseHoldLegOutcome.Sent : ResponseHoldLegOutcome.Failed,
                    new Uri(url).GetLeftPart(UriPartial.Path), result.Status, failureText),
                released ? null : new Failure(failureText!));
        }

        private string Body(CaseWorkflowReleaseHook hook, IReadOnlyDictionary<string, string?> values,
            HeldTransaction held, CaseRow existingCase, ResponseHold hold, string action, string reason)
        {
            if (!string.IsNullOrWhiteSpace(hook.Body))
            {
                return HookTokens.Replace(hook.Body, values, HookTokens.Json);
            }

            return new JObject
            {
                ["source"] = "caseHold",
                ["action"] = action,
                ["entityAnalysisModelInstanceEntryGuid"] = held.Dto.EntityAnalysisModelInstanceEntryGuid,
                ["caseId"] = existingCase.Id,
                ["caseKey"] = existingCase.CaseKey,
                ["caseKeyValue"] = existingCase.CaseKeyValue,
                ["rangeName"] = hold.RangeName,
                ["elevation"] = hold.Elevation,
                ["reason"] = reason.Trim(),
                ["user"] = userName
            }.ToString(Formatting.None);
        }

        private static ResponseHoldLeg NewLeg(ResponseHoldLegKind kind, ResponseHoldLegOutcome outcome,
            string? destination, int? status, string? detail)
        {
            return new ResponseHoldLeg
            {
                KindId = (int)kind,
                OutcomeId = (int)outcome,
                Date = DateTime.UtcNow,
                Destination = destination,
                HttpStatus = status,
                Detail = detail
            };
        }

        private async Task AuditAsync(CaseRow existingCase, CaseWorkflowReleaseHook hook, bool post,
            string? destination, string? body, CaseDispatchOutcome outcome, string? detail, int? status,
            Guid correlationId, CancellationToken token)
        {
            try
            {
                await dispatchRepository.InsertAsync(new CaseWorkflowDispatch
                {
                    CaseId = existingCase.Id,
                    CaseKey = existingCase.CaseKey,
                    CaseKeyValue = existingCase.CaseKeyValue,
                    SourceKindId = (byte)CaseDispatchSourceKind.ReleaseHook,
                    SourceId = hook.Id,
                    SourceName = hook.Name,
                    ChannelId = (byte)(post ? CaseDispatchChannel.HttpPost : CaseDispatchChannel.HttpGet),
                    ModeId = (byte)CaseDispatchMode.Payload,
                    Destination = destination,
                    Body = body,
                    OutcomeId = (byte)outcome,
                    Detail = detail,
                    ResponseStatus = status,
                    CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId
                }, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"{Area}: the audit of a release hook call could not be written user={userName}", e);
            }
        }

        private async Task<List<HeldTransaction>> LoadAsync(CaseRow existingCase, CancellationToken token)
        {
            var workflow = await caseWorkflowRepository.GetByGuidAsync(existingCase.CaseWorkflowGuid, token)
                .ConfigureAwait(false);

            if (workflow?.EntityAnalysisModelId == null)
            {
                return [];
            }

            var rows = await new GetCaseHoldTransactionsQuery(dbContext).ExecuteAsync(
                workflow.EntityAnalysisModelId.Value, tenantRegistryId,
                existingCase.EntityAnalysisModelInstanceEntryGuid, existingCase.CaseKey,
                existingCase.CaseKeyValue, token).ConfigureAwait(false);

            var hooks = await hookRepository.GetActiveByCaseWorkflowGuidAsync(existingCase.CaseWorkflowGuid, token)
                .ConfigureAwait(false);

            DateTimeOffset? deadline = existingCase.Diary == 1 &&
                                       existingCase.DiaryTypeId == (byte)CaseDiaryType.PaymentHold &&
                                       existingCase.DiaryDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(existingCase.DiaryDate.Value, DateTimeKind.Utc))
                : null;

            var held = new List<HeldTransaction>();

            foreach (var row in rows)
            {
                var hold = ResponseHoldJson.Read(row.Json);

                if (hold == null)
                {
                    continue;
                }

                var payload = PayloadOf(row.Json);
                var hook = hold.StateId == (int)ResponseHoldState.Held ? SelectHook(hooks, payload) : null;

                held.Add(new HeldTransaction(new CaseHoldDto
                {
                    EntityAnalysisModelInstanceEntryGuid = row.EntityAnalysisModelInstanceEntryGuid,
                    EntryKeyValue = row.EntryKeyValue,
                    ReferenceDate = row.ReferenceDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(row.ReferenceDate.Value, DateTimeKind.Utc))
                        : null,
                    StateId = hold.StateId,
                    RangeName = hold.RangeName,
                    RangeMinimum = hold.RangeMinimum,
                    RangeMaximum = hold.RangeMaximum,
                    Elevation = hold.Elevation,
                    StampedDate = new DateTimeOffset(DateTime.SpecifyKind(hold.StampedDate, DateTimeKind.Utc)),
                    DeadlineDate = hold.StateId == (int)ResponseHoldState.Held ? deadline : null,
                    Decision = hold.Decision,
                    Legs = hold.Legs,
                    ReleaseHookName = hook?.Name,
                    RefuseCallsHook = !string.IsNullOrWhiteSpace(hook?.RefuseUrl)
                }, hook, payload));
            }

            return held;
        }

        private static Dictionary<string, string?> PayloadOf(string json)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (JObject.Parse(json)["payload"] is not JObject payload)
            {
                return values;
            }

            foreach (var property in payload.Properties())
            {
                values[$"Payload.{property.Name}"] = property.Value.Type == JTokenType.Null
                    ? null
                    : property.Value.ToString();
            }

            return values;
        }

        private static CaseWorkflowReleaseHook? SelectHook(IReadOnlyList<CaseWorkflowReleaseHook> hooks,
            IReadOnlyDictionary<string, string?> payload)
        {
            var specific = hooks.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.SourceField) &&
                                                     payload.TryGetValue($"Payload.{f.SourceField}", out var value) &&
                                                     string.Equals(value, f.SourceValue,
                                                         StringComparison.OrdinalIgnoreCase));

            return specific ?? hooks.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.SourceField));
        }

        private NotFoundException NotFound(string op)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: case or held payment not found user={userName}");
            }

            return new NotFoundException(strings[CaseHoldResources.NotFound]);
        }

        private void EnsurePermitted(string op, int[] specs)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[CaseHoldResources.PermissionDenied], specs);
        }

        private sealed record HeldTransaction(
            CaseHoldDto Dto,
            CaseWorkflowReleaseHook? Hook,
            IReadOnlyDictionary<string, string?> Payload);

        private sealed record Failure(string Message);

        private sealed record LegResult(ResponseHoldLeg Leg, Failure? Failure);
    }
}

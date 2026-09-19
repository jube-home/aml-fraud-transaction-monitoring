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
using System.Text.Json;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.SessionCaseJournal;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.SessionCaseJournal;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.SessionCaseJournalRepository;

namespace Jube.Service.Repository.SessionCaseJournal
{
    public sealed class SessionCaseJournalService
    {
        private const int MaxJsonLength = 1_000_000;
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private SessionCaseJournalService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
        }

        public static Task<SessionCaseJournalService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<SessionCaseJournalService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(SessionCaseJournalResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("SessionCaseJournal.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[SessionCaseJournalResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"SessionCaseJournal.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[SessionCaseJournalResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new SessionCaseJournalService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Gets the calling user's own saved journal (for example their case grid column layout) for " +
                     "a CaseWorkflow, or null if none has been saved. Journals are private to the user who saved " +
                     "them.")]
        [ServiceOperation("SessionCaseJournalGetByCaseWorkflowGuid", OperationKind.Read, Idempotent = true)]
        public async Task<SessionCaseJournalDto?> GetByCaseWorkflowGuidAsync(
            [Description("The Guid of the CaseWorkflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SessionCaseJournal", "GetByCaseWorkflowGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SessionCaseJournal.GetByCaseWorkflowGuid: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("SessionCaseJournal.GetByCaseWorkflowGuid");
                var dto = SessionCaseJournalMapper.ToDto(
                    await repository.GetByCaseWorkflowGuidAsync(guid, token).ConfigureAwait(false));
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                op.Rows(dto is null ? 0 : 1);
                return dto;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"SessionCaseJournal.GetByCaseWorkflowGuid: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Saves the calling user's own journal for a CaseWorkflow. One journal is kept per user per " +
                     "CaseWorkflow: saving again replaces the earlier content. Owner and timestamp are " +
                     "server-assigned.")]
        [ServiceOperation("SessionCaseJournalCreate", OperationKind.Write, Idempotent = true)]
        public async Task<SessionCaseJournalDto> CreateAsync(SessionCaseJournalDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SessionCaseJournal", "Create", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SessionCaseJournal.Create: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("SessionCaseJournal.Create");
                await ValidateAsync(model, token).ConfigureAwait(false);

                var saved = SessionCaseJournalMapper.ToDto(
                    await repository.UpsertAsync(SessionCaseJournalMapper.ToPoco(model), token)
                        .ConfigureAwait(false));
                op.Entity(saved.Id);

                if (log.IsInfoEnabled)
                {
                    log.Info($"SessionCaseJournal.Create: saved Id={saved.Id} user={userName}");
                }

                return saved;
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"SessionCaseJournal.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task ValidateAsync(SessionCaseJournalDto model, CancellationToken token)
        {
            var failures = new List<ValidationFailure>();

            if (model.Json is { Length: > MaxJsonLength })
            {
                failures.Add(new ValidationFailure(nameof(SessionCaseJournalDto.Json),
                    strings[SessionCaseJournalResources.JsonTooLarge]) { ErrorCode = "JsonTooLarge" });
            }
            else if (model.Json != null && !IsValidJson(model.Json))
            {
                failures.Add(new ValidationFailure(nameof(SessionCaseJournalDto.Json),
                    strings[SessionCaseJournalResources.JsonInvalid]) { ErrorCode = "JsonInvalid" });
            }

            if (!await repository.ExistsCaseWorkflowAsync(model.CaseWorkflowGuid, token).ConfigureAwait(false))
            {
                failures.Add(new ValidationFailure(nameof(SessionCaseJournalDto.CaseWorkflowGuid),
                    strings[SessionCaseJournalResources.CaseWorkflowNotFound]) { ErrorCode = "CaseWorkflowNotFound" });
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }
        }

        private static bool IsValidJson(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return IsStorable(document.RootElement);
            }
            catch (JsonException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool IsStorable(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() is not { } text || !text.Contains('\0');
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Name.Contains('\0') || !IsStorable(property.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (!IsStorable(item))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return true;
            }
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

            throw new ForbiddenException(strings[SessionCaseJournalResources.PermissionDenied], permissions);
        }
    }
}
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
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseResearch;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseResearch;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseResearch;
using log4net;
using Microsoft.Extensions.Localization;
using RulePoco = Jube.Data.Poco.CaseResearch;

namespace Jube.Service.Repository.CaseResearch
{
    public sealed class CaseResearchService
    {
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly CaseRepository caseRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseResearchRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseResearchDtoValidator validator;

        private CaseResearchService(DbContext dbContext, string userName, int tenantRegistryId,
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
            this.dbContext = dbContext;
            repository = new CaseResearchRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
            validator = new CaseResearchDtoValidator(strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseResearchService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseResearchService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseResearchResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseResearchResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseResearchResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseResearchService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Captures research on a case: the topic, what was found, and the sources the findings rely " +
                     "on (at most 10, each a title, an http or https address and when it was retrieved). Use it " +
                     "after researching a case in the chat. Attach only real addresses of sources you actually " +
                     "retrieved, each with its title; never invent, guess or reconstruct an address, and send no " +
                     "sources at all rather than a doubtful one. Research is append-only, rolls up to the case " +
                     "key and value, and is authored by the calling user; when an agent captures it, it is " +
                     "flagged as written by AskJooby. Not idempotent.")]
        [ServiceOperation("CaseResearchCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseResearchDto> CreateAsync(CaseResearchDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseResearch", "Create", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseResearch.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    foreach (var error in results.Errors)
                    {
                        error.AttemptedValue = null;
                        error.FormattedMessagePlaceholderValues = new Dictionary<string, object>();
                    }

                    throw new DtoValidationException(results);
                }

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token)
                    .ConfigureAwait(false);

                if (existingCase is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseResearch.Create: caseId={model.CaseId} not found or not visible user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResearchResources.CaseNotFound]);
                }

                if (CaseLockCheck.HeldByAnother(existingCase, userName))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseResearch.Create: caseId={model.CaseId} is locked to another user " +
                                 $"user={userName}");
                    }

                    throw new CaseLockedException(strings[CaseResearchResources.CaseLocked]);
                }

                var now = DateTime.UtcNow;

                var saved = await repository.InsertAsync(new RulePoco
                {
                    CaseId = existingCase.Id,
                    CaseKey = existingCase.CaseKey,
                    CaseKeyValue = existingCase.CaseKeyValue,
                    Topic = HtmlSanitiser.Sanitise(model.Topic.Trim()),
                    Findings = HtmlSanitiser.Sanitise(model.Findings.Trim()),
                    SourceId = (int)(AgentInvocationScope.Active ? CaseResearchSource.AskJooby : CaseResearchSource.User)
                }, (model.Sources ?? []).Select(s => CaseResearchMapper.ToPoco(s, now)).ToList(), token)
                    .ConfigureAwait(false);

                var sources = await repository.GetCitationsAsync([saved.Id], token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                return CaseResearchMapper.ToDto(saved, sources);
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
            catch (CaseLockedException)
            {
                op.Outcome("locked");
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
                log.Error($"CaseResearch.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the research captured on the Case Key/Value pair, newest first, each with its sources. " +
                     "Only research on cases in the caller's tenant, in workflows and statuses the caller's roles " +
                     "have been granted, is returned. Read-only.")]
        [ServiceOperation("CaseResearchListByCaseKeyValue", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseResearchDto>> GetByCaseKeyValueAsync(
            [Description("The case key.")] string key,
            [Description("The case key value.")] string value,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseResearch", "GetByCaseKeyValue", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseResearch.GetByCaseKeyValue");

                var research = await repository.GetByCaseKeyValueAsync(key, value, token).ConfigureAwait(false);

                var sources = await repository.GetCitationsAsync([.. research.Select(r => r.Id)], token)
                    .ConfigureAwait(false);

                var files = Repository.CaseFile.CaseFileMapper.ToDto(
                    await new CaseFileRepository(dbContext, userName)
                        .GetByCaseKeyValueActiveOnlyAsync(key, value, token).ConfigureAwait(false));

                var dtos = CaseResearchMapper.ToDto(research, sources, files);
                op.Rows(dtos.Count);

                return dtos;
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
                log.Error($"CaseResearch.GetByCaseKeyValue: unexpected failure user={userName}", ex);
                throw;
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
                log.Warn($"{op}: permission denied user={userName}");
            }

            throw new ForbiddenException(strings[CaseResearchResources.PermissionDenied], permissions);
        }
    }
}

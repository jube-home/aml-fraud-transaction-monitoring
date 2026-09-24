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
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Parser;
using Jube.Dto.Query.Parser;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.Parser;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.Parser
{
    public sealed class ParserService
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

        private ParserService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static Task<ParserService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ParserService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ParserResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Parser.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[ParserResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Parser.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[ParserResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ParserService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Parses and compiles a user-authored rule (inline function, gateway rule, abstraction rule, " +
                     "abstraction calculation or activation rule) against the fields, lists, dictionaries, inline " +
                     "scripts and other named entities of an Entity Analysis Model in the caller's tenant. Returns " +
                     "'Compiled', or 'Error' with the located error spans. Nothing is stored or executed.")]
        [ServiceOperation("ParserParse", OperationKind.Read, Idempotent = true)]
        public async Task<ParseRuleResultDto> ParseAsync(
            [Description("The rule type, rule text and the Entity Analysis Model to parse against.")]
            ParseRuleRequestDto request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Parser", "Parse", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Parser.Parse: entry user={userName} model={request.EntityAnalysisModelId}");
            }

            try
            {
                EnsurePermitted("Parser.Parse");
                return await ParseCoreAsync(request, token).ConfigureAwait(false);
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
                log.Error(ex.ToString());
                return new ParseRuleResultDto { Message = "Error" };
            }
        }

        private async Task<ParseRuleResultDto> ParseCoreAsync(ParseRuleRequestDto request, CancellationToken token)
        {
            if (request.RuleText is { Length: > RuleParse.MaximumRuleTextLength })
            {
                return new ParseRuleResultDto { Message = "Error" };
            }

            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(request.EntityAnalysisModelId, request.RuleParseType, token).ConfigureAwait(false);

            var result = RuleParse.Execute(request.RuleText, request.RuleParseType, environment, log,
                RuleParse.DefaultReferences());

            return new ParseRuleResultDto
            {
                Message = result.Message,
                ErrorSpans = result.ErrorSpans.Count > 0 ? ParserMapper.ToDto(result.ErrorSpans) : null
            };
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

            throw new ForbiddenException(strings[ParserResources.PermissionDenied], permissions);
        }
    }
}
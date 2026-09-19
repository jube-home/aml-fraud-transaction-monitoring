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
using System.Reflection;
using System.Text;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Data.SyntaxTree;
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
    using RuleParser = global::Jube.Parser.Parser;
    using Compile = global::Jube.Parser.Compiler.Compile;

    public sealed class ParserService
    {
        private const int MaximumRuleTextLength = 65536;
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
            if (request.RuleText is { Length: > MaximumRuleTextLength })
            {
                return new ParseRuleResultDto { Message = "Error" };
            }

            var modelId = request.EntityAnalysisModelId;
            var tokens = dbContext.RuleScriptToken.Select(s => s.Token).ToList();

            var xPaths = await XPathsAsync(modelId, token).ConfigureAwait(false);
            var inlineScriptProperties = await InlineScriptPropertiesAsync(modelId, token).ConfigureAwait(false);
            var inlineFunctions = await InlineFunctionPropertiesAsync(modelId, token).ConfigureAwait(false);
            var lists = (await new EntityAnalysisModelListRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                .Select(s => s.Name).ToList();
            var dictionaries = (await new EntityAnalysisModelDictionaryRepository(dbContext, userName)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                .Select(s => s.Name).ToList();

            List<string>? ttlCounters = null;
            List<string>? abstractionRules = null;
            List<string>? sanctions = null;

            if (request.RuleParseType > 3)
            {
                ttlCounters = (await new EntityAnalysisModelTtlCounterRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                abstractionRules = (await new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                sanctions = (await new EntityAnalysisModelSanctionRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
            }

            List<string>? abstractionCalculations = null;
            List<string>? httpAdaptations = null;
            List<string>? exhaustiveAdaptations = null;
            List<string>? activationRules = null;

            if (request.RuleParseType > 4)
            {
                abstractionCalculations = (await new EntityAnalysisModelAbstractionCalculationRepository(dbContext,
                            userName)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                httpAdaptations = (await new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
                exhaustiveAdaptations = (await new ExhaustiveSearchInstanceRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
            }

            if (request.RuleParseType >= 5)
            {
                activationRules = (await new EntityAnalysisModelActivationRuleRepository(dbContext, userName)
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(modelId, token).ConfigureAwait(false))
                    .Select(s => s.Name).ToList();
            }

            var parser = new RuleParser(log, tokens)
            {
                EntityAnalysisModelRequestXPaths = xPaths,
                EntityAnalysisModelInlineScriptProperties = inlineScriptProperties,
                EntityAnalysisModelAbstractionCalculations = abstractionCalculations,
                EntityAnalysisModelsAbstractionRule = abstractionRules,
                EntityAnalysisModelsTtlCounters = ttlCounters,
                EntityAnalysisModelsSanctions = sanctions,
                EntityAnalysisModelsLists = lists,
                EntityAnalysisModelsDictionaries = dictionaries,
                EntityAnalysisModelsHttpAdaptations = httpAdaptations,
                EntityAnalysisModelsExhaustiveAdaptations = exhaustiveAdaptations,
                EntityAnalysisModelsActivationRules = activationRules,
                EntityAnalysisModelsInlineFunctions = inlineFunctions
            };

            var errorSpans = new List<global::Jube.Parser.ErrorSpan>();
            var parsedRule = new global::Jube.Parser.ParsedRule
            {
                ErrorSpans = errorSpans,
                OriginalRuleText = request.RuleText
            };
            parsedRule = parser.TranslateFromDotNotation(parsedRule, request.RuleParseType == 3);
            parsedRule = parser.Parse(parsedRule);

            var sb = new StringBuilder();
            foreach (var softParseErrorSpan in parsedRule.ErrorSpans)
            {
                sb.AppendLine(softParseErrorSpan.Message);
            }

            var response = new ParseRuleResultDto { ErrorSpans = ParserMapper.ToDto(errorSpans) };

            parsedRule = request.RuleParseType switch
            {
                1 => parser.WrapInlineFunction(parsedRule, false),
                2 => parser.WrapGatewayRule(parsedRule, false),
                3 => parser.WrapAbstractionRule(parsedRule, false),
                4 => parser.WrapAbstractionCalculation(parsedRule, false),
                5 => parser.WrapActivationRule(parsedRule, false),
                _ => parsedRule
            };

            var codeBase = Assembly.GetExecutingAssembly().Location;
            var strPathBinary = Path.GetDirectoryName(codeBase);
            var strPathFramework = Path.GetDirectoryName(typeof(object).Assembly.Location);

            if (strPathFramework != null && strPathBinary != null)
            {
                var refs = new[]
                {
                    Path.Combine(strPathFramework, "mscorlib.dll"), Path.Combine(strPathFramework, "System.dll"),
                    Path.Combine(strPathFramework, "Microsoft.VisualBasic.dll"),
                    Path.Combine(strPathFramework, "System.Xml.dll"), Path.Combine(strPathBinary, "log4net.dll"),
                    Path.Combine(strPathBinary, "Jube.Dictionary.dll"),
                    Path.Combine(strPathFramework, "System.Collections.dll"),
                    Path.Combine(strPathBinary, "Jube.HttpAdaptationProtocol.dll")
                };

                var compile = new Compile();
                compile.CompileCode(parsedRule.ParsedRuleText, log, refs, Compile.Language.Vb);

                if (!compile.Success)
                {
                    foreach (var err in compile.Errors)
                    {
                        var line = err.Location.GetLineSpan().StartLinePosition.Line - parsedRule.LineOffset;
                        var message = $"Line {line + 1}: {err.GetMessage()}";
                        sb.AppendLine(message);

                        errorSpans.Add(new global::Jube.Parser.ErrorSpan
                        {
                            Message = message,
                            Start = err.Location.SourceSpan.Start - parsedRule.CharOffset,
                            Length = err.Location.SourceSpan.Length,
                            Line = line
                        });
                    }

                    response.Message = sb.ToString();
                    response.ErrorSpans = ParserMapper.ToDto(errorSpans);

                    return response;
                }
            }

            if (errorSpans.Count > 0)
            {
                return new ParseRuleResultDto
                {
                    Message = "Error",
                    ErrorSpans = ParserMapper.ToDto(errorSpans)
                };
            }

            return new ParseRuleResultDto { Message = "Compiled" };
        }

        private async Task<Dictionary<string, int>> InlineScriptPropertiesAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var value = new Dictionary<string, int>();
            var modelInlineScripts = await new EntityAnalysisModelInlineScriptRepository(dbContext, userName)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            var inlineScriptRepository = new EntityAnalysisInlineScriptRepository(dbContext);

            foreach (var modelInlineScript in modelInlineScripts)
            {
                if (!modelInlineScript.EntityAnalysisInlineScriptId.HasValue)
                {
                    continue;
                }

                var inlineScript = await inlineScriptRepository
                    .GetByIdAsync(modelInlineScript.EntityAnalysisInlineScriptId.Value, token)
                    .ConfigureAwait(false);
                foreach (var publicProperty in SyntaxTreeHelpers.GetPublicProperties(inlineScript.Code,
                             inlineScript.LanguageId == 2))
                {
                    value.Add(publicProperty.Key, publicProperty.Value.DataTypeId);
                }
            }

            return value;
        }

        private async Task<Dictionary<string, int>> InlineFunctionPropertiesAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var values = new Dictionary<string, int>();
            var functions = await new EntityAnalysisModelInlineFunctionRepository(dbContext, userName)
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            foreach (var function in functions)
            {
                if (values.ContainsKey(function.Name))
                {
                    continue;
                }

                if (function.ReturnDataTypeId != null)
                {
                    values.Add(function.Name, function.ReturnDataTypeId.Value);
                }
            }

            return values;
        }

        private async Task<Dictionary<string, global::Jube.Parser.EntityAnalysisModelRequestXPath>> XPathsAsync(
            int entityAnalysisModelId, CancellationToken token)
        {
            var values = new Dictionary<string, global::Jube.Parser.EntityAnalysisModelRequestXPath>();
            foreach (var xPath in await new EntityAnalysisModelRequestXPathRepository(dbContext, userName)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                         .ConfigureAwait(false))
            {
                if (!values.ContainsKey(xPath.Name))
                {
                    values.Add(xPath.Name,
                        new global::Jube.Parser.EntityAnalysisModelRequestXPath
                        {
                            DataTypeId = xPath.DataTypeId ?? 1,
                            DefaultValue = xPath.DefaultValue,
                            Cache = xPath.Cache == 1
                        });
                }
            }

            return values;
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
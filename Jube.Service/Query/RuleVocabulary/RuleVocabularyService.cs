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
using Jube.Dto.Query.RuleVocabulary;
using Jube.Parser;
using ParserVocabulary = Jube.Parser.RuleVocabulary;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.RuleVocabulary;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.RuleVocabulary
{
    public sealed class RuleVocabularyService
    {
        private const int MaxTake = 200;

        private static readonly int[] permissions = [8, 10, 13, 14, 16, 17, 25, 26];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private RuleVocabularyService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
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

        public static Task<RuleVocabularyService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RuleVocabularyService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RuleVocabularyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RuleVocabulary.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RuleVocabularyResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"RuleVocabulary.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RuleVocabularyResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RuleVocabularyService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the words rule text may use, in alphabetical order: the rule language's keywords, " +
                     "the words this installation allows and the rule functions (methods called on a value, such " +
                     "as .IsMatch(pattern) on a string). Field names such as Payload.Amount are not words; get " +
                     "them from Completions. Filter by part of the name, by kind or by the type a function is " +
                     "called on, and describe a word for its signatures.")]
        [ServiceOperation("RuleVocabularyList", OperationKind.Read, Idempotent = true)]
        public Task<RuleWordPageDto> ListAsync(
            [Description("Only words containing this text, ignoring case.")]
            string? search = null,
            [Description("Only words of this kind: Keyword, AllowedWord, Function or CuratedExpression.")]
            string? kind = null,
            [Description("Only functions called on a value of this type, e.g. String, Double, DateTime or Integer.")]
            string? receiverType = null,
            [Description("Maximum number of words to return; clamped to 200.")]
            int take = 100,
            [Description("When set, only words after this one in alphabetical order are returned.")]
            string? afterName = null,
            CancellationToken token = default)
        {
            return RunAsync("List", async () =>
            {
                var words = (await WordsAsync(token).ConfigureAwait(false))
                    .Where(w => string.IsNullOrEmpty(search) ||
                                w.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .Where(w => string.IsNullOrEmpty(kind) ||
                                string.Equals(w.Kind.ToString(), kind, StringComparison.OrdinalIgnoreCase))
                    .Where(w => string.IsNullOrEmpty(receiverType) || w.Overloads.Any(o =>
                        string.Equals(o.ReceiverType, receiverType, StringComparison.OrdinalIgnoreCase)))
                    .Where(w => string.IsNullOrEmpty(afterName) ||
                                string.Compare(w.Name, afterName, StringComparison.OrdinalIgnoreCase) > 0)
                    .ToList();

                var clampedTake = Math.Clamp(take, 1, MaxTake);
                return new RuleWordPageDto
                {
                    More = words.Count > clampedTake,
                    Items = words.Take(clampedTake).Select(w => new RuleWordSummaryDto
                    {
                        Name = w.Name,
                        Kind = w.Kind.ToString(),
                        ReceiverTypes = w.Overloads.Select(o => o.ReceiverType).Distinct(StringComparer.Ordinal)
                            .ToList(),
                        Overloads = w.Overloads.Count
                    }).ToList()
                };
            }, token);
        }

        [Description("Describes one word rule text may use: its kind and, for a function, every way to call it " +
                     "as a Visual Basic signature with the type it is called on, its parameters and what it " +
                     "returns. The name is matched ignoring case.")]
        [ServiceOperation("RuleVocabularyDescribe", OperationKind.Read, Idempotent = true)]
        public Task<RuleWordDto> DescribeAsync(
            [Description("The word, e.g. IsMatch.")]
            string? name,
            CancellationToken token = default)
        {
            return RunAsync("Describe", async () =>
            {
                var word = (await WordsAsync(token).ConfigureAwait(false))
                    .FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

                if (word == null)
                {
                    throw new NotFoundException(string.Format(strings[RuleVocabularyResources.WordNotFound], name));
                }

                return new RuleWordDto
                {
                    Name = word.Name,
                    Kind = word.Kind.ToString(),
                    Overloads = word.Overloads.Select(o => new RuleWordOverloadDto
                    {
                        Signature = o.Signature,
                        ReceiverType = o.ReceiverType,
                        ReturnType = o.ReturnType,
                        Parameters = o.Parameters.Select(p => new RuleWordParameterDto
                        {
                            Name = p.Name, Type = p.Type, Optional = p.Optional, DefaultValue = p.DefaultValue
                        }).ToList()
                    }).ToList()
                };
            }, token);
        }

        private async Task<IReadOnlyList<RuleWord>> WordsAsync(CancellationToken token)
        {
            var installation = (await new RuleScriptTokenRepository(dbContext).GetAsync(token).ConfigureAwait(false))
                .Select(s => s.Token);
            return ParserVocabulary.Build(installation);
        }

        private async Task<T> RunAsync<T>(string operation, Func<Task<T>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("RuleVocabulary", operation, userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RuleVocabulary.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"RuleVocabulary.{operation}");
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
                log.Error($"RuleVocabulary.{operation}: unexpected failure user={userName}", ex);
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
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[RuleVocabularyResources.PermissionDenied], permissions);
        }
    }
}
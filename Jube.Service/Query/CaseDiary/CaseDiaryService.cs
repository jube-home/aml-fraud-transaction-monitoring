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
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseDiary;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseDiary;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Query.CaseDiary;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseDiary
{
    public sealed class CaseDiaryService
    {
        public const int DiarySetEventTypeId = 7;
        public const int DiaryDateChangeEventTypeId = 10;
        public const int DiaryClearedEventTypeId = 19;
        private const byte DiaryRecheckTriggerId = (byte)CaseRiskTrigger.DiaryLaneThreshold;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseDiaryService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseDiaryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseDiaryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseDiaryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseDiaryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseDiaryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseDiaryService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Places a diary on an open case, or changes the one it has, saying what it is for and when it " +
                     "falls due. A regulatory deadline puts the case in the Mandatory lane and a payment hold in " +
                     "the Live money lane, so it is ranked ahead of everything else until it is cleared. The case " +
                     "must be open, visible to the caller and not locked to someone else. The ranking is " +
                     "recomputed within one sweep of the case risk worker.")]
        [ServiceOperation("CaseDiarySet", OperationKind.Write, Idempotent = true)]
        public async Task SetAsync(CaseDiarySetDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseDiary", "Set", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted();
                Validate(new CaseDiarySetDtoValidator(strings, DateTime.UtcNow).Validate(model));

                var existing = await GetOpenCaseAsync(model.CaseId, token).ConfigureAwait(false);
                var type = (CaseDiaryType)model.DiaryTypeId;
                var due = TruncateToSecond(model.DueDate!.Value.UtcDateTime);
                var wasOn = existing.Diary == 1;
                var oldType = (CaseDiaryType)existing.DiaryTypeId;
                var changedDate = !wasOn || !existing.DiaryDate.HasValue
                                  || TruncateToSecond(existing.DiaryDate.Value) != due;

                var changed = await dbContext.Case.Where(c => c.Id == existing.Id && c.ClosedStatusId == 0)
                    .Set(s => s.Diary, (byte?)1)
                    .Set(s => s.DiaryDate, due)
                    .Set(s => s.DiaryTypeId, (byte)type)
                    .Set(s => s.DiaryUser, userName)
                    .Set(s => s.RiskCheckedDate, (DateTime?)null)
                    .Set(s => s.RiskRecheckTriggerId, DiaryRecheckTriggerId)
                    .UpdateAsync(token).ConfigureAwait(false);

                if (changed == 0)
                {
                    throw new ConflictException(strings[CaseDiaryResources.CaseChanged]);
                }

                var events = new CaseEventRepository(dbContext, userName);

                if (!wasOn || oldType != type)
                {
                    await events.InsertAsync(EventFor(existing, DiarySetEventTypeId,
                        wasOn ? EnumText.Of<CaseDiaryType>((int)oldType) : null, EnumText.Of<CaseDiaryType>((int)type)),
                        token).ConfigureAwait(false);
                }

                if (changedDate)
                {
                    await events.InsertAsync(EventFor(existing, DiaryDateChangeEventTypeId,
                        wasOn ? existing.DiaryDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture) : null,
                        due.ToString("o", System.Globalization.CultureInfo.InvariantCulture)), token)
                        .ConfigureAwait(false);
                }

                op.Entity(existing.Id);
                op.Updated();
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Takes the diary off an open case, which returns it to ranking on its own merits. The case " +
                     "must be open, visible to the caller, not locked to someone else and have a diary set.")]
        [ServiceOperation("CaseDiaryClear", OperationKind.Write, Idempotent = true)]
        public async Task ClearAsync(CaseDiaryClearDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseDiary", "Clear", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted();
                Validate(new CaseDiaryClearDtoValidator(strings).Validate(model));

                var existing = await GetOpenCaseAsync(model.CaseId, token).ConfigureAwait(false);

                if (existing.Diary != 1)
                {
                    throw new ConflictException(strings[CaseDiaryResources.DiaryNotSet]);
                }

                var changed = await dbContext.Case.Where(c => c.Id == existing.Id && c.ClosedStatusId == 0
                                                              && c.Diary == 1)
                    .Set(s => s.Diary, (byte?)0)
                    .Set(s => s.DiaryTypeId, (byte)CaseDiaryType.Review)
                    .Set(s => s.RiskCheckedDate, (DateTime?)null)
                    .Set(s => s.RiskRecheckTriggerId, DiaryRecheckTriggerId)
                    .UpdateAsync(token).ConfigureAwait(false);

                if (changed == 0)
                {
                    throw new ConflictException(strings[CaseDiaryResources.CaseChanged]);
                }

                await new CaseEventRepository(dbContext, userName).InsertAsync(EventFor(existing,
                    DiaryClearedEventTypeId, EnumText.Of<CaseDiaryType>(existing.DiaryTypeId), null), token)
                    .ConfigureAwait(false);

                op.Entity(existing.Id);
                op.Updated();
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        private static DateTime TruncateToSecond(DateTime value) =>
            new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        private static CaseEvent EventFor(Jube.Data.Poco.Case existing, int type, string? before, string? after) =>
            new()
            {
                CaseId = existing.Id,
                CaseKey = existing.CaseKey,
                CaseKeyValue = existing.CaseKeyValue,
                CaseEventTypeId = type,
                Before = before,
                After = after
            };

        private async Task<Jube.Data.Poco.Case> GetOpenCaseAsync(int caseId, CancellationToken token)
        {
            var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                .ConfigureAwait(false);

            if (existing == null)
            {
                throw new NotFoundException(strings[CaseDiaryResources.NotFound]);
            }

            if (existing.ClosedStatusId != 0)
            {
                throw new ConflictException(strings[CaseDiaryResources.CaseNotOpen]);
            }

            if (existing.Locked == 1 && !string.IsNullOrEmpty(existing.LockedUser)
                                     && !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
            {
                throw new ConflictException(strings[CaseDiaryResources.LockedByAnotherUser]);
            }

            return existing;
        }

        private static void Validate(ValidationResult result)
        {
            if (!result.IsValid)
            {
                throw new DtoValidationException(result);
            }
        }

        private bool Classify(OperationScope op, Exception ex)
        {
            switch (ex)
            {
                case ForbiddenException:
                    op.Outcome("forbidden");
                    break;
                case NotFoundException:
                    op.Outcome("not_found");
                    break;
                case DtoValidationException:
                    op.Outcome("invalid");
                    break;
                case ConflictException:
                    op.Outcome("conflict");
                    break;
                case OperationCanceledException:
                    op.Outcome("cancelled");
                    break;
                default:
                    op.Error(ex);
                    log.Error($"CaseDiary: unexpected failure user={userName}", ex);
                    break;
            }

            return false;
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseDiaryResources.PermissionDenied], permissions);
        }
    }
}

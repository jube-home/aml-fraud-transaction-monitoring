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

using Jube.Data.Context;
using Jube.Data.Query.GetModelGovernanceInventoryQuery;
using Jube.Data.Query.GetModelGovernanceSubjectQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.ModelGovernance;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Query.ModelGovernance
{
    public sealed class ModelGovernanceService
    {
        public const int ModelPermission = 6;
        public const int MaximumOwner = 200;
        public const int MaximumText = 2000;
        public const int DueSoonDays = 30;

        private static readonly int[] permissions = [ModelPermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private ModelGovernanceService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<ModelGovernanceService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false) ?? throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ModelGovernanceService(dbContext, userName, tenant, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", permissions);
            }
        }

        private static void CheckKind(int kindId)
        {
            if (!Enum.IsDefined((ModelGovernanceKind)kindId))
            {
                throw new ArgumentException("The model kind must be 1 (Entity analysis model) or 2 (Exhaustive adaptation).");
            }
        }

        private async Task<GetModelGovernanceSubjectQueryDto> SubjectAsync(int kindId, Guid guid,
            CancellationToken token)
        {
            CheckKind(kindId);

            return await new GetModelGovernanceSubjectQuery(dbContext)
                       .ExecuteAsync(tenantRegistryId, (short)kindId, guid, token).ConfigureAwait(false)
                   ?? throw new CaseExceptions.NotFoundException("The model was not found.");
        }

        public static ModelReviewStatus ReviewStatus(bool recorded, DateTime? nextReview, DateTime now)
        {
            if (!recorded)
            {
                return ModelReviewStatus.NotRecorded;
            }

            if (nextReview is null)
            {
                return ModelReviewStatus.Current;
            }

            if (nextReview.Value < now)
            {
                return ModelReviewStatus.Overdue;
            }

            return nextReview.Value < now.AddDays(DueSoonDays) ? ModelReviewStatus.DueSoon : ModelReviewStatus.Current;
        }

        public async Task<List<ModelGovernanceInventoryRowDto>> InventoryAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("ModelGovernance", "Inventory", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                Ensure();

                var models = await new GetModelGovernanceInventoryQuery(dbContext)
                    .ExecuteAsync(tenantRegistryId, token).ConfigureAwait(false);
                var records = (await new ModelGovernanceRecordRepository(dbContext)
                    .ListCurrentAsync(tenantRegistryId, token).ConfigureAwait(false))
                    .ToDictionary(r => (r.KindId, r.ModelGuid));
                var now = DateTime.UtcNow;

                op.Rows(models.Count);

                return models.Select(m =>
                {
                    records.TryGetValue((m.KindId, m.ModelGuid), out var record);

                    return new ModelGovernanceInventoryRowDto
                    {
                        KindId = m.KindId, ModelGuid = m.ModelGuid, Name = m.Name ?? string.Empty,
                        Recorded = record is not null, OwnerUser = record?.OwnerUser, RiskTierId = record?.RiskTierId,
                        NextReviewDate = record?.NextReviewDate is null
                            ? null
                            : DateTime.SpecifyKind(record.NextReviewDate.Value, DateTimeKind.Utc),
                        ReviewStatusId = (int)ReviewStatus(record is not null, record?.NextReviewDate, now)
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<ModelGovernanceHistoryDto> GetAsync(int kindId, Guid modelGuid,
            CancellationToken token = default)
        {
            Ensure();
            await SubjectAsync(kindId, modelGuid, token).ConfigureAwait(false);

            var history = (await new ModelGovernanceRecordRepository(dbContext)
                .HistoryAsync(tenantRegistryId, (short)kindId, modelGuid, 50, token).ConfigureAwait(false))
                .Select(ToDto).ToList();

            return new ModelGovernanceHistoryDto { Current = history.FirstOrDefault(), History = history };
        }

        public async Task<ModelGovernanceRecordDto> SetAsync(ModelGovernanceRecordDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("ModelGovernance", "Set", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure();
                CheckKind(model.KindId);

                var owner = model.OwnerUser?.Trim();
                var purpose = model.IntendedPurpose?.Trim();
                var oversight = model.HumanOversight?.Trim();

                if (string.IsNullOrEmpty(owner) || owner.Length > MaximumOwner)
                {
                    throw new ArgumentException($"The owner is required, at most {MaximumOwner} characters.");
                }

                if (string.IsNullOrEmpty(purpose) || purpose.Length > MaximumText)
                {
                    throw new ArgumentException($"The intended purpose is required, at most {MaximumText} characters.");
                }

                if (string.IsNullOrEmpty(oversight) || oversight.Length > MaximumText)
                {
                    throw new ArgumentException($"The human oversight statement is required, at most {MaximumText} characters.");
                }

                if (!Enum.IsDefined((ModelRiskTier)model.RiskTierId))
                {
                    throw new ArgumentException("The risk tier must be 1 (Low), 2 (Medium) or 3 (High).");
                }

                await SubjectAsync(model.KindId, model.ModelGuid, token).ConfigureAwait(false);

                var repository = new ModelGovernanceRecordRepository(dbContext);
                var current = await repository.CurrentAsync(tenantRegistryId, (short)model.KindId, model.ModelGuid,
                    token).ConfigureAwait(false);

                var row = new Data.Poco.ModelGovernanceRecord
                {
                    TenantRegistryId = tenantRegistryId, KindId = (short)model.KindId, ModelGuid = model.ModelGuid,
                    OwnerUser = owner, IntendedPurpose = purpose, RiskTierId = (short)model.RiskTierId,
                    HumanOversight = oversight, NextReviewDate = model.NextReviewDate?.ToUniversalTime(),
                    CreatedUser = userName
                };

                if (!await repository.AppendAsync(row, current?.Version ?? 0, token).ConfigureAwait(false))
                {
                    throw new CaseExceptions.ConflictException("The statement was changed by someone else; try again.");
                }

                op.Created();

                return ToDto(row);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<(string Html, string FileName)> CardAsync(int kindId, Guid modelGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ModelGovernance", "Card", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure();

                var subject = await SubjectAsync(kindId, modelGuid, token).ConfigureAwait(false);
                var history = await new ModelGovernanceRecordRepository(dbContext)
                    .HistoryAsync(tenantRegistryId, (short)kindId, modelGuid, 50, token).ConfigureAwait(false);

                var drift = new List<ModelCardDrift>();

                if (kindId == (int)ModelGovernanceKind.EntityAnalysisModel)
                {
                    var modelId = subject.EntityAnalysisModelId;

                    drift = (await dbContext.ModelDriftObservation
                            .Where(w => w.TenantRegistryId == tenantRegistryId && w.EntityAnalysisModelId == modelId)
                            .OrderByDescending(o => o.ObservedDate).Take(300).ToListAsync(token).ConfigureAwait(false))
                        .Select(d => new ModelCardDrift(d.ScoreName, d.ObservedDate, d.Psi, d.StatusId)).ToList();
                }

                var html = ModelCardBuilder.Build(new ModelCardInput(subject.Name ?? string.Empty, kindId, modelGuid,
                    DateTime.UtcNow, userName, history.Count == 0 ? null : ToDto(history[0]), history.Count, drift,
                    kindId == (int)ModelGovernanceKind.ExhaustiveAdaptation
                        ? new ModelCardExhaustive(subject.Score, subject.Models, subject.CompletedDate)
                        : null));

                op.Rows(drift.Count);

                return (html, $"model-card-{modelGuid:N}.html");
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private static ModelGovernanceRecordDto ToDto(Data.Poco.ModelGovernanceRecord row)
        {
            return new ModelGovernanceRecordDto
            {
                KindId = row.KindId, ModelGuid = row.ModelGuid, Version = row.Version, OwnerUser = row.OwnerUser,
                IntendedPurpose = row.IntendedPurpose, RiskTierId = row.RiskTierId,
                HumanOversight = row.HumanOversight,
                NextReviewDate = row.NextReviewDate is null
                    ? null
                    : DateTime.SpecifyKind(row.NextReviewDate.Value, DateTimeKind.Utc),
                CreatedUser = row.CreatedUser, CreatedDate = DateTime.SpecifyKind(row.CreatedDate, DateTimeKind.Utc)
            };
        }
    }
}

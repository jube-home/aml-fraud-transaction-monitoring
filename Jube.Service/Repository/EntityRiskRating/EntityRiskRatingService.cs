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
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.EntityRiskRating;
using Jube.Dto.Validation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.EntityRiskRating
{
    public sealed class EntityRiskRatingService
    {
        public const int ReadPermission = 1;
        public const int WritePermission = 18;
        public const int ApprovePermission = 80;
        public const int MaximumKeyName = 128;
        public const int MaximumKeyValue = 256;
        public const int MaximumReason = 500;
        public const int MaximumImport = 500;
        private static readonly int[] readPermissions = [ReadPermission];
        private static readonly int[] writePermissions = [WritePermission];
        private static readonly int[] approvePermissions = [ApprovePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private EntityRiskRatingService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static async Task<EntityRiskRatingService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityRiskRatingService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", required);
            }
        }

        public async Task<EntityRiskRatingHistoryDto> GetAsync(string? keyName, string? keyValue,
            CancellationToken token = default)
        {
            Ensure(readPermissions);

            if (string.IsNullOrWhiteSpace(keyName) || string.IsNullOrEmpty(keyValue))
            {
                throw new ArgumentException("The key name and value are required.");
            }

            var history = await new EntityRiskRatingRepository(dbContext)
                .HistoryAsync(tenantRegistryId, keyName, keyValue, 50, token).ConfigureAwait(false);

            return new EntityRiskRatingHistoryDto
            {
                Current = history.Count == 0 ? null : ToDto(history[0]),
                History = history.Select(ToDto).ToList()
            };
        }

        public async Task<List<EntityRiskRatingDto>> ListDueAsync(CancellationToken token = default)
        {
            Ensure(readPermissions);

            return (await new EntityRiskRatingRepository(dbContext)
                .ListDueAsync(tenantRegistryId, DateTime.UtcNow, 200, token).ConfigureAwait(false))
                .Select(ToDto).ToList();
        }

        public async Task<EntityRiskRatingRequestDto> SetAsync(EntityRiskRatingDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityRiskRating", "Propose", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(writePermissions);

                var problem = Validate(model, requireReason: true);

                if (problem is not null)
                {
                    throw new ArgumentException(problem);
                }

                var requests = new EntityRiskRatingRequestRepository(dbContext);

                if (await requests.CountPendingForKeyAsync(tenantRegistryId, model.KeyName, model.KeyValue, token)
                        .ConfigureAwait(false) > 0)
                {
                    throw new CaseExceptions.ConflictException("A change to this rating is already waiting for approval.");
                }

                var current = await new EntityRiskRatingRepository(dbContext)
                    .CurrentAsync(tenantRegistryId, model.KeyName, model.KeyValue, token).ConfigureAwait(false);

                var created = await requests.InsertAsync(new Data.Poco.EntityRiskRatingRequest
                {
                    TenantRegistryId = tenantRegistryId,
                    KeyName = model.KeyName,
                    KeyValue = model.KeyValue,
                    TierId = (short)model.TierId,
                    Score = model.Score,
                    ReviewByDate = model.ReviewByDate,
                    Reason = model.Reason!.Trim(),
                    BasedOnVersion = current?.Version ?? 0,
                    StateId = (short)EntityRiskRequestState.Pending,
                    RequestedUser = userName
                }, token).ConfigureAwait(false);

                op.Created();

                return ToDto(created);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<List<EntityRiskRatingRequestDto>> ListPendingAsync(CancellationToken token = default)
        {
            Ensure([ReadPermission]);

            return (await new EntityRiskRatingRequestRepository(dbContext)
                .ListPendingAsync(tenantRegistryId, 200, token).ConfigureAwait(false)).Select(ToDto).ToList();
        }

        public Task<EntityRiskRatingRequestDto> ApproveAsync(EntityRiskRatingDecisionDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            return DecideAsync("Approve", model.Guid, approvePermissions, EntityRiskRequestState.Approved, model.Note,
                false, token);
        }

        public Task<EntityRiskRatingRequestDto> RejectAsync(EntityRiskRatingDecisionDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            return DecideAsync("Reject", model.Guid, approvePermissions, EntityRiskRequestState.Rejected, model.Note,
                true, token);
        }

        public Task<EntityRiskRatingRequestDto> RetractAsync(Guid guid, CancellationToken token = default)
        {
            return DecideAsync("Retract", guid, writePermissions, EntityRiskRequestState.Retracted, null, false,
                token);
        }

        private async Task<EntityRiskRatingRequestDto> DecideAsync(string operation, Guid guid, int[] required,
            EntityRiskRequestState to, string? noteText, bool noteRequired, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityRiskRating", operation, userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                Ensure(required);

                var note = string.IsNullOrWhiteSpace(noteText) ? null : noteText.Trim();

                if (noteRequired && note is null)
                {
                    throw new ArgumentException("A reason is required to reject a change.");
                }

                if (note is { Length: > MaximumReason })
                {
                    throw new ArgumentException($"The note must be at most {MaximumReason} characters.");
                }

                var requests = new EntityRiskRatingRequestRepository(dbContext);
                var request = await requests.GetAsync(tenantRegistryId, guid, token).ConfigureAwait(false)
                              ?? throw new CaseExceptions.NotFoundException("The request was not found.");

                if (request.StateId != (short)EntityRiskRequestState.Pending)
                {
                    throw new CaseExceptions.ConflictException("Only a pending request can be decided.");
                }

                var own = string.Equals(request.RequestedUser, userName, StringComparison.OrdinalIgnoreCase);

                if (to == EntityRiskRequestState.Retracted ? !own : own)
                {
                    throw new CaseExceptions.ConflictException(to == EntityRiskRequestState.Retracted
                        ? "Only the person who proposed a change can retract it."
                        : "A change cannot be decided by the person who proposed it.");
                }

                int? resulting = null;

                await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                if (await requests.DecideAsync(request.Id, to, userName, note, null, token).ConfigureAwait(false) == 0)
                {
                    await transaction.RollbackAsync(token).ConfigureAwait(false);

                    throw new CaseExceptions.ConflictException("The request is no longer pending.");
                }

                if (to == EntityRiskRequestState.Approved)
                {
                    var written = await WriteAsync(new EntityRiskRatingDto
                    {
                        KeyName = request.KeyName, KeyValue = request.KeyValue, TierId = request.TierId,
                        Score = request.Score, ReviewByDate = request.ReviewByDate, Reason = request.Reason
                    }, EntityRiskSource.Manual, request.RequestedUser, request.BasedOnVersion, token)
                        .ConfigureAwait(false);

                    if (written is null)
                    {
                        await transaction.RollbackAsync(token).ConfigureAwait(false);

                        throw new CaseExceptions.ConflictException(
                            "The rating has changed since this was proposed; propose it again.");
                    }

                    resulting = written.Version;

                    await requests.SetResultingVersionAsync(request.Id, resulting.Value, token).ConfigureAwait(false);
                }

                await transaction.CommitAsync(token).ConfigureAwait(false);

                op.Updated();

                return ToDto((await requests.GetAsync(tenantRegistryId, guid, token).ConfigureAwait(false))!);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<EntityRiskRatingImportResultDto> ImportAsync(EntityRiskRatingImportDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityRiskRating", "Import", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(writePermissions);
                Ensure(approvePermissions);

                if (model.Ratings.Count > MaximumImport)
                {
                    throw new ArgumentException($"At most {MaximumImport} ratings can be imported at once.");
                }

                var result = new EntityRiskRatingImportResultDto();
                var repository = new EntityRiskRatingRepository(dbContext);

                for (var i = 0; i < model.Ratings.Count; i++)
                {
                    var rating = model.Ratings[i];
                    var problem = Validate(rating, requireReason: false);

                    if (problem is not null)
                    {
                        result.Errors.Add(new ValidationErrorDto
                        {
                            PropertyName = $"Ratings[{i}]", ErrorCode = "RatingInvalid", Message = problem
                        });

                        continue;
                    }

                    var current = await repository.CurrentAsync(tenantRegistryId, rating.KeyName, rating.KeyValue,
                        token).ConfigureAwait(false);

                    if (current is not null && current.TierId == rating.TierId && Nullable.Equals(current.Score, rating.Score)
                        && current.ReviewByDate == rating.ReviewByDate)
                    {
                        result.Unchanged++;

                        continue;
                    }

                    if (await WriteAsync(rating, EntityRiskSource.Imported, token).ConfigureAwait(false) is null)
                    {
                        result.Errors.Add(new ValidationErrorDto
                        {
                            PropertyName = $"Ratings[{i}]", ErrorCode = "RatingConflict",
                            Message = "The rating was changed by someone else; send it again."
                        });

                        continue;
                    }

                    result.Written++;
                }

                op.Created();

                return result;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private async Task<Data.Poco.EntityRiskRating?> WriteAsync(EntityRiskRatingDto model, EntityRiskSource source,
            CancellationToken token)
        {
            return await WriteAsync(model, source, userName, null, token).ConfigureAwait(false);
        }

        private async Task<Data.Poco.EntityRiskRating?> WriteAsync(EntityRiskRatingDto model, EntityRiskSource source,
            string madeBy, int? expectedVersion, CancellationToken token)
        {
            var repository = new EntityRiskRatingRepository(dbContext);
            var current = await repository.CurrentAsync(tenantRegistryId, model.KeyName, model.KeyValue, token)
                .ConfigureAwait(false);

            if (expectedVersion is not null && (current?.Version ?? 0) != expectedVersion)
            {
                return null;
            }

            var row = new Data.Poco.EntityRiskRating
            {
                TenantRegistryId = tenantRegistryId,
                KeyName = model.KeyName,
                KeyValue = model.KeyValue,
                TierId = (short)model.TierId,
                Score = model.Score,
                SourceId = (short)source,
                Reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim(),
                ReviewByDate = model.ReviewByDate,
                CreatedUser = madeBy
            };

            return await repository.AppendAsync(row, current?.Version ?? 0, token).ConfigureAwait(false) ? row : null;
        }

        private static string? Validate(EntityRiskRatingDto model, bool requireReason)
        {
            if (string.IsNullOrWhiteSpace(model.KeyName) || model.KeyName.Length > MaximumKeyName)
            {
                return $"The key name is required and at most {MaximumKeyName} characters.";
            }

            if (string.IsNullOrEmpty(model.KeyValue) || model.KeyValue.Length > MaximumKeyValue)
            {
                return $"The key value is required and at most {MaximumKeyValue} characters.";
            }

            if (!Enum.IsDefined((EntityRiskTier)model.TierId))
            {
                return "The tier must be 1 (Low), 2 (Medium) or 3 (High).";
            }

            if (model.Score is { } score && !double.IsFinite(score))
            {
                return "The score must be a finite number.";
            }

            if (model.Reason is { Length: > MaximumReason })
            {
                return $"The reason must be at most {MaximumReason} characters.";
            }

            if (requireReason && string.IsNullOrWhiteSpace(model.Reason))
            {
                return "A reason is required when a rating is set by hand.";
            }

            return null;
        }

        private static EntityRiskRatingRequestDto ToDto(Data.Poco.EntityRiskRatingRequest row)
        {
            return new EntityRiskRatingRequestDto
            {
                Guid = row.Guid, KeyName = row.KeyName, KeyValue = row.KeyValue, TierId = row.TierId,
                Score = row.Score,
                ReviewByDate = row.ReviewByDate is null
                    ? null
                    : DateTime.SpecifyKind(row.ReviewByDate.Value, DateTimeKind.Utc),
                Reason = row.Reason, BasedOnVersion = row.BasedOnVersion, StateId = row.StateId,
                ResultingVersion = row.ResultingVersion, RequestedUser = row.RequestedUser,
                RequestedDate = DateTime.SpecifyKind(row.RequestedDate, DateTimeKind.Utc),
                DecidedUser = row.DecidedUser,
                DecidedDate = row.DecidedDate is null ? null : DateTime.SpecifyKind(row.DecidedDate.Value, DateTimeKind.Utc),
                DecisionNote = row.DecisionNote
            };
        }

        private static EntityRiskRatingDto ToDto(Data.Poco.EntityRiskRating row)
        {
            return new EntityRiskRatingDto
            {
                KeyName = row.KeyName,
                KeyValue = row.KeyValue,
                Version = row.Version,
                TierId = row.TierId,
                Score = row.Score,
                SourceId = row.SourceId,
                Reason = row.Reason,
                ReviewByDate = row.ReviewByDate is null
                    ? null
                    : DateTime.SpecifyKind(row.ReviewByDate.Value, DateTimeKind.Utc),
                CreatedUser = row.CreatedUser,
                CreatedDate = DateTime.SpecifyKind(row.CreatedDate, DateTimeKind.Utc)
            };
        }
    }
}

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
using Jube.Data.Helpers;
using Jube.Data.Repository;
using Jube.Dto.Filter;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsInstance
{
    public sealed class AdvancedAnalyticsInstanceService
    {
        private const int MaxListTake = 200;
        private readonly AdvancedAnalyticsAllocationRequestValidator allocationValidator;
        private readonly AdvancedAnalyticsInstanceRepository repository;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly AdvancedAnalyticsInstanceDtoValidator validator;

        private AdvancedAnalyticsInstanceService(AdvancedAnalyticsOperationRunner runner)
        {
            this.runner = runner;

            repository = new AdvancedAnalyticsInstanceRepository(runner.DbContext, runner.TenantRegistryId,
                runner.UserName);

            validator = new AdvancedAnalyticsInstanceDtoValidator(runner.Strings, NameTakenAsync);

            allocationValidator = new AdvancedAnalyticsAllocationRequestValidator(runner.Strings,
                (id, token) => EntityAnalysisModelParentGuard.IsVisibleAsync(runner.DbContext,
                    runner.TenantRegistryId, id, token));
        }

        public static Task<AdvancedAnalyticsInstanceService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsInstanceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.Instance, typeof(AdvancedAnalyticsInstanceResources),
                AdvancedAnalyticsInstanceResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsInstanceService(runner);
        }

        [Description("Returns one Advanced Analytics instance by its numeric identifier, scoped to the caller's " +
                     "tenant. Returns null when it does not exist, was deleted, or belongs to another tenant.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceGet", OperationKind.Read, Idempotent = true)]
        public Task<AdvancedAnalyticsInstanceDto?> GetByIdAsync(
            [Description("Numeric identifier of the instance.")]
            int id,
            CancellationToken token = default)
        {
            return runner.RunAsync("Get", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Get");
                var instance = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (instance == null)
                {
                    return null;
                }

                op.Entity(instance.Id);

                return AdvancedAnalyticsInstanceMapper.ToDto(instance);
            });
        }

        [Description("Returns one Advanced Analytics instance by its Guid, scoped to the caller's " +
                     "tenant. Returns null when it does not exist, was deleted, or belongs to another tenant.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceGetByGuid", OperationKind.Read, Idempotent = true)]
        public Task<AdvancedAnalyticsInstanceDto?> GetByGuidAsync(
            [Description("Globally-addressable Guid of the instance.")]
            Guid guid,
            CancellationToken token = default)
        {
            return runner.RunAsync("Get", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Get");
                var instance = await repository.GetByGuidAsync(guid, token).ConfigureAwait(false);

                if (instance == null)
                {
                    return null;
                }

                op.Entity(instance.Id);

                return AdvancedAnalyticsInstanceMapper.ToDto(instance);
            });
        }

        [Description("Lists the caller's Advanced Analytics instances ordered by identifier, at most 'take' at a " +
                     "time (maximum 200), optionally only those allocated to one Model. When more remain, call " +
                     "again with 'afterId' set to the last identifier returned.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceList", OperationKind.Read, Idempotent = true)]
        public Task<PagedResult<AdvancedAnalyticsInstanceDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to between 1 and 200.")]
            int take = 50,
            [Description("When set, only instances with an identifier above this value are returned.")]
            int? afterId = null,
            [Description("When set, only instances allocated to this Model are returned.")]
            int? entityAnalysisModelId = null,
            [Description("When true, also returns deleted instances, so a pending or rejected deletion stays " +
                         "visible until a checker decides it.")]
            bool includeDeleted = false,
            CancellationToken token = default)
        {
            return runner.RunAsync("List", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "List");
                var clamped = Math.Clamp(take, 1, MaxListTake);

                var page = (await repository.ListAsync(entityAnalysisModelId, includeDeleted, token)
                        .ConfigureAwait(false))
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .Take(clamped)
                    .ToList();

                op.Rows(page.Count);

                return new PagedResult<AdvancedAnalyticsInstanceDto>(AdvancedAnalyticsInstanceMapper.ToDto(page));
            });
        }

        [Description("Creates an Advanced Analytics instance in the caller's tenant. It starts unallocated and " +
                     "not live; use the allocate operation to attach it to a Model. Not idempotent: calling twice " +
                     "with the same name is refused as a duplicate.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceCreate", OperationKind.Write, Idempotent = false)]
        public Task<AdvancedAnalyticsInstanceDto> InsertAsync(
            [Description("The instance to create. Only the name and the output flags are used.")]
            AdvancedAnalyticsInstanceDto? model,
            CancellationToken token = default)
        {
            return runner.RunAsync("Create", async op =>
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Create");
                model.Id = 0;
                model.EntityAnalysisModelId = null;

                if ((model.Active || model.ResponsePayload || model.ReportTable) && !runner.Has(
                        AdvancedAnalyticsPermissionSpecifications.promoteModelOnly))
                {
                    throw PromoteRequired();
                }

                await ValidateOrThrowAsync(model, token).ConfigureAwait(false);

                using var gate = await NameGate.EnterAsync(GateKey(model.Name!), token).ConfigureAwait(false);

                var saved = await UniqueViolation.GuardAsync(
                    () => repository.InsertAsync(AdvancedAnalyticsInstanceMapper.ToPoco(model, null), token),
                    r => new DtoValidationException(r),
                    runner.Strings[AdvancedAnalyticsInstanceResources.NameAlreadyExists]).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Created();

                runner.Info($"AdvancedAnalyticsInstance.Create: Id={saved.Id} name={saved.Name} " +
                            $"user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Renames an Advanced Analytics instance or changes its output flags. It never changes which " +
                     "Model the instance is allocated to; use the allocate and deallocate operations for that. " +
                     "Turning the instance live or changing the output flags needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceUpdate", OperationKind.Write, Idempotent = true)]
        public Task<AdvancedAnalyticsInstanceDto> UpdateAsync(
            [Description("The instance to change. Id selects the row; allocation and audit fields are ignored.")]
            AdvancedAnalyticsInstanceDto? model,
            CancellationToken token = default)
        {
            return runner.RunAsync("Update", async op =>
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Update");

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false)
                               ?? throw NotFound();

                model.EntityAnalysisModelId = existing.EntityAnalysisModelId;

                var liveChange = model.Active != (existing.Active == 1)
                                 || model.ResponsePayload != (existing.ResponsePayload == 1)
                                 || model.ReportTable != (existing.ReportTable == 1);

                if (liveChange && !runner.Has(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly))
                {
                    throw PromoteRequired();
                }

                await ValidateOrThrowAsync(model, token).ConfigureAwait(false);

                using var gate = await NameGate.EnterAsync(GateKey(model.Name!), token).ConfigureAwait(false);
                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(
                            AdvancedAnalyticsInstanceMapper.ToPoco(model, existing.EntityAnalysisModelId), token),
                        r => new DtoValidationException(r),
                        runner.Strings[AdvancedAnalyticsInstanceResources.NameAlreadyExists]).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                runner.Info($"AdvancedAnalyticsInstance.Update: Id={saved.Id} version->{saved.Version} " +
                            $"user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Deletes an Advanced Analytics instance, which also takes it out of the live pipeline. It " +
                     "cannot be found again afterwards. Deleting an instance that is allocated to a Model needs " +
                     "the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public Task DeleteAsync(
            [Description("Numeric identifier of the instance to delete.")]
            int id,
            CancellationToken token = default)
        {
            return runner.RunAsync("Delete", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Delete");

                var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false)
                               ?? throw NotFound();

                if (existing.EntityAnalysisModelId.HasValue &&
                    !runner.Has(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly))
                {
                    throw PromoteRequired();
                }

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }

                op.Entity(id);
                op.Deleted();
                runner.Info($"AdvancedAnalyticsInstance.Delete: soft-deleted Id={id} user={runner.UserName}");
            });
        }

        [Description("Allocates an Advanced Analytics instance to a Jube Model of the caller's tenant so that its " +
                     "live trained model can be attached to that Model's pipeline. The Model must exist and not " +
                     "be deleted. An instance is allocated to at most one Model: allocating to a different Model " +
                     "is refused until the instance is deallocated, and repeating the same allocation changes " +
                     "nothing. Needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AdvancedAnalyticsInstanceAllocateToModel", OperationKind.Write, Idempotent = true)]
        public Task<AdvancedAnalyticsInstanceDto> AllocateToModelAsync(
            [Description("Numeric identifier of the instance to allocate.")]
            int instanceId,
            [Description("Numeric identifier of the Model to allocate it to.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return runner.RunAsync("AllocateToModel", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "AllocateToModel");

                var validation = await allocationValidator
                    .ValidateAsync(new AdvancedAnalyticsAllocationRequest(instanceId, entityAnalysisModelId), token)
                    .ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    throw new DtoValidationException(validation);
                }

                using var gate = await NameGate.EnterAsync($"AdvancedAnalyticsAllocation:{instanceId}", token)
                    .ConfigureAwait(false);

                var existing = await repository.GetByIdAsync(instanceId, token).ConfigureAwait(false)
                               ?? throw NotFound();

                if (existing.EntityAnalysisModelId == entityAnalysisModelId)
                {
                    op.Entity(existing.Id);

                    return AdvancedAnalyticsInstanceMapper.ToDto(existing);
                }

                if (existing.EntityAnalysisModelId.HasValue)
                {
                    throw new ConflictException(
                        runner.Strings[AdvancedAnalyticsInstanceResources.AlreadyAllocated], "AlreadyAllocated");
                }

                existing.EntityAnalysisModelId = entityAnalysisModelId;
                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await repository.UpdateAsync(existing, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                runner.Info($"AdvancedAnalyticsInstance.AllocateToModel: Id={saved.Id} " +
                            $"model={entityAnalysisModelId} user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Removes the allocation of an Advanced Analytics instance to a Jube Model and takes the " +
                     "instance out of the live pipeline. Repeating it on an unallocated instance changes nothing. " +
                     "Needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AdvancedAnalyticsInstanceDeallocate", OperationKind.Write, Idempotent = true)]
        public Task<AdvancedAnalyticsInstanceDto> DeallocateAsync(
            [Description("Numeric identifier of the instance to deallocate.")]
            int instanceId,
            CancellationToken token = default)
        {
            return runner.RunAsync("Deallocate", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "Deallocate");

                using var gate = await NameGate.EnterAsync($"AdvancedAnalyticsAllocation:{instanceId}", token)
                    .ConfigureAwait(false);

                var existing = await repository.GetByIdAsync(instanceId, token).ConfigureAwait(false)
                               ?? throw NotFound();

                if (!existing.EntityAnalysisModelId.HasValue)
                {
                    op.Entity(existing.Id);

                    return AdvancedAnalyticsInstanceMapper.ToDto(existing);
                }

                existing.EntityAnalysisModelId = null;
                existing.Active = 0;
                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await repository.UpdateAsync(existing, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();
                runner.Info($"AdvancedAnalyticsInstance.Deallocate: Id={saved.Id} user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Switches an allocated Advanced Analytics instance on or off in the live pipeline without " +
                     "deleting anything. Off takes its promoted models out of scoring on the next model " +
                     "synchronisation and on restores them; the trained models and versions are untouched. An " +
                     "instance that is not allocated to a Model cannot be switched on. Needs the Promote Model " +
                     "permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        public Task<AdvancedAnalyticsInstanceDto> SetActiveAsync(
            [Description("Numeric identifier of the instance to switch.")]
            int instanceId,
            [Description("True to switch the instance on, false to switch it off.")]
            bool active,
            CancellationToken token = default)
        {
            return runner.RunAsync("SetActive", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "SetActive");

                if (instanceId <= 0)
                {
                    throw NotFound();
                }

                using var gate = await NameGate.EnterAsync($"AdvancedAnalyticsAllocation:{instanceId}", token)
                    .ConfigureAwait(false);

                var existing = await repository.GetByIdAsync(instanceId, token).ConfigureAwait(false)
                               ?? throw NotFound();

                if (active)
                {
                    var candidate = AdvancedAnalyticsInstanceMapper.ToDto(existing);
                    candidate.Active = true;
                    await ValidateOrThrowAsync(candidate, token).ConfigureAwait(false);
                }

                if (existing.Active == (byte)(active ? 1 : 0))
                {
                    op.Entity(existing.Id);

                    return AdvancedAnalyticsInstanceMapper.ToDto(existing);
                }

                existing.Active = (byte)(active ? 1 : 0);
                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await repository.UpdateAsync(existing, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                runner.Info($"AdvancedAnalyticsInstance.SetActive: Id={saved.Id} active={active} " +
                            $"user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Resubmits a rejected change to an Advanced Analytics instance for approval again, " +
                     "unchanged, opening a fresh approval cycle without requiring an edit. Use after a " +
                     "rejection was explained and the maker still wants the same change (including a delete) " +
                     "approved. Needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AdvancedAnalyticsInstanceResubmit", OperationKind.Write, Idempotent = false)]
        public Task<AdvancedAnalyticsInstanceDto> ResubmitAsync(
            [Description("Numeric identifier of the instance to resubmit.")]
            int instanceId,
            [Description("Optional note explaining why this is being resubmitted.")]
            string? note = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("Resubmit", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "Resubmit");

                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await repository.ResubmitAsync(instanceId, note, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ConflictException(ex.Message, "NotRejected");
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                runner.Info($"AdvancedAnalyticsInstance.Resubmit: Id={saved.Id} version->{saved.Version} " +
                            $"user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        [Description("Retracts a rejected change to an Advanced Analytics instance, restoring it to its last " +
                     "approved version (including undeleting it, if the rejected change was a delete) without " +
                     "opening a new approval cycle. Use when the maker agrees with the rejection and wants to " +
                     "abandon the change. Needs the Promote Model permission.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.PromoteModel)]
        [ServiceOperation("AdvancedAnalyticsInstanceRetract", OperationKind.Write, Idempotent = false)]
        public Task<AdvancedAnalyticsInstanceDto> RetractAsync(
            [Description("Numeric identifier of the instance to retract.")]
            int instanceId,
            [Description("Optional note explaining why this is being retracted.")]
            string? note = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("Retract", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.promoteModelOnly, "Retract");

                Jube.Data.Poco.AdvancedAnalyticsInstance saved;

                try
                {
                    saved = await repository.RetractAsync(instanceId, note, token: token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw NotFound(ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ConflictException(ex.Message, "NotRejected");
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                runner.Info($"AdvancedAnalyticsInstance.Retract: Id={saved.Id} version->{saved.Version} " +
                            $"user={runner.UserName}");

                return AdvancedAnalyticsInstanceMapper.ToDto(saved);
            });
        }

        private void Ensure(int[] specifications, string operation)
        {
            runner.EnsurePermitted(specifications, operation, AdvancedAnalyticsInstanceResources.PermissionDenied);
        }

        [Description("Validates an Advanced Analytics instance without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is stored " +
                     "or changed.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceValidate", OperationKind.Read, Idempotent = true)]
        public Task<ValidationResultDto> ValidateAsync(
            [Description("The Advanced Analytics instance to validate.")]
            AdvancedAnalyticsInstanceDto? model,
            CancellationToken token = default)
        {
            return runner.RunAsync("Validate", async op =>
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Validate");
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                op.Rows(results.Errors.Count);

                return ValidationResultMapper.ToDto(results);
            });
        }

        [Description("Lists the fields a query builder JSON filter over Advanced Analytics instances may use, " +
                     "with each field's type, the operators allowed for it and what it means. Use them as rule " +
                     "ids in AdvancedAnalyticsInstanceFilter and AdvancedAnalyticsInstanceCount.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceFilterFields", OperationKind.Read, Idempotent = true)]
        public Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            return runner.RunAsync("FilterFields", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<AdvancedAnalyticsInstanceDto>();
                op.Rows(result.Count);

                return result;
            });
        }

        [Description("Returns the Advanced Analytics instances in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "AdvancedAnalyticsInstanceFilterFields), ordered by id and capped at 'take' rows (max 200). " +
                     "If 'more' is true, call again with 'afterId' set to the last returned Id to continue. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceFilter", OperationKind.Read, Idempotent = true)]
        public Task<FilterResultDto<AdvancedAnalyticsInstanceDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the instances, using the fields from AdvancedAnalyticsInstanceFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("Filter", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Filter");

                var rows = AdvancedAnalyticsInstanceMapper.ToDto(
                    await repository.ListAsync(token: token).ConfigureAwait(false));

                var result = DtoFilter.Filter(rows, builderJson, Math.Clamp(take, 1, MaxListTake), afterId,
                    d => d.Id);

                op.Rows(result.Items.Count);

                return result;
            });
        }

        [Description("Counts the Advanced Analytics instances in the caller's tenant matching query builder JSON " +
                     "(over the fields from AdvancedAnalyticsInstanceFilterFields; empty counts all), optionally " +
                     "broken down by the values of one field. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsInstanceCount", OperationKind.Read, Idempotent = true)]
        public Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the instances, using the fields from AdvancedAnalyticsInstanceFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from AdvancedAnalyticsInstanceFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("Count", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "Count");

                var rows = AdvancedAnalyticsInstanceMapper.ToDto(
                    await repository.ListAsync(token: token).ConfigureAwait(false));

                var result = DtoFilter.Count(rows, builderJson, groupBy);
                op.Rows(result.Count);

                return result;
            });
        }

        private async Task ValidateOrThrowAsync(AdvancedAnalyticsInstanceDto model, CancellationToken token)
        {
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

            if (!results.IsValid)
            {
                throw new DtoValidationException(results);
            }
        }

        private async Task<bool> NameTakenAsync(string name, int excludeId, CancellationToken token)
        {
            return (await repository.ListAsync(token: token).ConfigureAwait(false))
                .Any(w => w.Id != excludeId && string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private string GateKey(string name)
        {
            return $"AdvancedAnalyticsInstanceName:{runner.TenantRegistryId}:{name}";
        }

        private NotFoundException NotFound(Exception? inner = null)
        {
            return new NotFoundException(runner.Strings[AdvancedAnalyticsInstanceResources.InstanceNotFound], inner);
        }

        private ForbiddenException PromoteRequired()
        {
            return new ForbiddenException(
                runner.Strings[AdvancedAnalyticsInstanceResources.PromotePermissionRequired],
                AdvancedAnalyticsPermissionSpecifications.promoteModelOnly);
        }
    }
}
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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalReplayService
    {
        public const int MaximumReplays = 10;
        public const int MaximumNameLength = 100;

        private static readonly int[] permissions = [1];
        private static readonly int[] windows = [7, 30, 90, 365];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly CaseKeyJournalTrackService track;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseKeyJournalReplayService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search, CaseKeyJournalTrackService track)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.search = search;
            this.track = track;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalReplayService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalReplayService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseKeyJournalResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var search = await CaseSearchService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            var track = await CaseKeyJournalTrackService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseKeyJournalReplayService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, search, track);
        }

        [Description("Lists the replays saved on a case. A saved replay is only its settings, the window, the " +
                     "merged keys and the filter on the Archive, which are shared by everyone working the case and draw " +
                     "the movement of the case key again on demand. Read-only.")]
        [ServiceOperation("CaseKeyJournalReplayList", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalReplaysDto> ListAsync(
            [Description("Id of the case.")] int caseId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "ReplayList", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                await ResolveAsync(caseId, token).ConfigureAwait(false);

                var replays = await new CaseKeyJournalReplayRepository(dbContext, userName)
                    .ListAsync(caseId, token).ConfigureAwait(false);

                op.Rows(replays.Count);

                return new CaseKeyJournalReplaysDto { Replays = replays.Select(ToDto).ToList() };
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
                log.Error($"CaseKeyJournal.ReplayList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Saves a replay on a case: a name, how many days back to look (7, 30, 90 or 365), up to three " +
                     "merged keys and an optional filter on the Archive. Only these settings are stored, and the movement " +
                     "is worked out again from the archive whenever the replay is opened or summarised. A case can " +
                     "hold 10 replays and a case locked to another user cannot be changed. SIDE EFFECTS: stores the " +
                     "replay against the case and records who saved it.")]
        [ServiceOperation("CaseKeyJournalReplaySave", OperationKind.Write, Idempotent = false)]
        public async Task<CaseKeyJournalReplayDto> SaveAsync(CaseKeyJournalReplayRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "ReplaySave", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var failures = new List<FluentValidation.Results.ValidationFailure>();
                var name = (request.Name ?? string.Empty).Trim();

                if (name.Length is 0 or > MaximumNameLength || name.Any(char.IsControl))
                {
                    failures.Add(Failure(nameof(request.Name), CaseKeyJournalResources.ReplayNameInvalid,
                        "ReplayNameInvalid"));
                }

                if (!windows.Contains(request.Days))
                {
                    failures.Add(Failure(nameof(request.Days), CaseKeyJournalResources.ReplayWindowInvalid,
                        "ReplayWindowInvalid"));
                }

                var merge = string.IsNullOrWhiteSpace(request.Merge) ? [] : JournalMerge.Parse(request.Merge);

                if (!JournalMerge.IsValid(merge))
                {
                    failures.Add(Failure(nameof(request.Merge), CaseKeyJournalResources.MergeInvalid,
                        "MergeInvalid"));
                }

                if (!JournalTravel.Valid(request.LimitKmh, request.MinimumKilometres))
                {
                    failures.Add(Failure(nameof(request.LimitKmh), CaseKeyJournalResources.TravelLimitInvalid,
                        "TravelLimitInvalid"));
                }

                var lens = string.IsNullOrWhiteSpace(request.Lens) ? null : request.Lens.Trim().ToLowerInvariant();

                if (lens is not null && !JournalLensReader.Lenses.Contains(lens))
                {
                    failures.Add(Failure(nameof(request.Lens), CaseKeyJournalResources.LensInvalid, "LensInvalid"));
                }

                if (failures.Count > 0)
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(failures));
                }

                var existing = await ResolveAsync(request.CaseId, token).ConfigureAwait(false);

                EnsureNotLockedByAnother(existing);

                var filter = await ValidFilterAsync(existing, request.Filter, token).ConfigureAwait(false);

                var repository = new CaseKeyJournalReplayRepository(dbContext, userName);

                if (await repository.CountAsync(request.CaseId, token).ConfigureAwait(false) >= MaximumReplays)
                {
                    throw Invalid(nameof(request.Name), CaseKeyJournalResources.ReplayLimitInvalid,
                        "ReplayLimitInvalid");
                }

                var saved = await repository.InsertAsync(request.CaseId, name, request.Days,
                    string.IsNullOrWhiteSpace(request.Merge) ? null : request.Merge,
                    filter?.Text, request.LimitKmh,
                    request.MinimumKilometres, request.ImpossibleOnly == true, request.ShowAll == true,
                    lens == JournalLensReader.Payload ? null : lens, token)
                    .ConfigureAwait(false);

                op.Rows(1);

                return ToDto(saved);
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
                log.Error($"CaseKeyJournal.ReplaySave: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a saved replay from a case so that it is no longer offered. Only the saved settings " +
                     "are removed, never any transaction. A case locked to another user cannot be changed. " +
                     "SIDE EFFECTS: marks the replay deleted and records who deleted it.")]
        [ServiceOperation("CaseKeyJournalReplayDelete", OperationKind.Write, Idempotent = true)]
        public async Task DeleteAsync(
            [Description("Guid of the saved replay.")] Guid replayGuid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "ReplayDelete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var repository = new CaseKeyJournalReplayRepository(dbContext, userName);
                var replay = await repository.GetAsync(replayGuid, token).ConfigureAwait(false)
                             ?? throw new NotFoundException(strings[CaseKeyJournalResources.ReplayNotFound]);

                var existing = await ResolveAsync(replay.CaseId, token).ConfigureAwait(false);

                EnsureNotLockedByAnother(existing);

                await repository.DeleteAsync(replayGuid, token).ConfigureAwait(false);

                op.Rows(1);
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
                log.Error($"CaseKeyJournal.ReplayDelete: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Works out a saved replay again from the archive as it is now and returns the movement it " +
                     "comes to: how many transactions were read, matched and located, how many legs between " +
                     "consecutive transactions of a key there are, and the fastest legs that could not have been " +
                     "travelled, with their distance, hours and speed. This is what the case narrative can cite. " +
                     "Read-only.")]
        [ServiceOperation("CaseKeyJournalReplaySummaryGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTrackDto> SummaryAsync(
            [Description("Guid of the saved replay.")] Guid replayGuid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "ReplaySummary", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var replay = await new CaseKeyJournalReplayRepository(dbContext, userName)
                                 .GetAsync(replayGuid, token).ConfigureAwait(false)
                             ?? throw new NotFoundException(strings[CaseKeyJournalResources.ReplayNotFound]);

                var existing = await ResolveAsync(replay.CaseId, token).ConfigureAwait(false);
                var now = DateTimeOffset.UtcNow;
                var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

                var result = await track.GetAsync(new CaseKeyJournalTrackRequestDto
                {
                    CaseWorkflowGuid = existing.CaseWorkflowGuid,
                    Key = existing.CaseKey,
                    Value = existing.CaseKeyValue,
                    From = today.AddDays(-replay.Days),
                    To = now,
                    Merge = replay.MergeJson,
                    Filter = replay.FilterJson,
                    CaseId = replay.CaseId,
                    SummaryOnly = true,
                    LimitKmh = replay.LimitKmh,
                    MinimumKilometres = replay.MinimumKilometres
                }, token).ConfigureAwait(false);

                op.Rows(result.Meta.Matched);

                return result;
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
                log.Error($"CaseKeyJournal.ReplaySummary: unexpected failure user={userName}", ex);
                throw;
            }
        }

        public static CaseKeyJournalReplayDto ToDto(CaseKeyJournalReplay replay)
        {
            return new CaseKeyJournalReplayDto
            {
                Guid = replay.Guid,
                CaseId = replay.CaseId,
                Name = replay.Name,
                Days = replay.Days,
                Merge = replay.MergeJson,
                Filter = replay.FilterJson,
                LimitKmh = replay.LimitKmh,
                MinimumKilometres = replay.MinimumKilometres,
                ImpossibleOnly = replay.ImpossibleOnly == 1,
                ShowAll = replay.ShowAll == 1,
                Lens = replay.Lens,
                CreatedUser = replay.CreatedUser,
                CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(replay.CreatedDate, DateTimeKind.Utc))
            };
        }

        private void EnsureNotLockedByAnother(Data.Poco.Case existing)
        {
            if (existing.Locked == 1 && !string.IsNullOrEmpty(existing.LockedUser)
                                     && !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
            {
                throw new ConflictException(strings[CaseKeyJournalResources.CaseLocked]);
            }
        }

        private async Task<Data.Poco.Case> ResolveAsync(int caseId, CancellationToken token)
        {
            var existing = await dbContext.Case.FirstOrDefaultAsync(c => c.Id == caseId, token)
                .ConfigureAwait(false) ?? throw new NotFoundException(strings[CaseKeyJournalResources.CaseNotFound]);

            try
            {
                await search.EverythingAsync(existing.CaseWorkflowGuid, token).ConfigureAwait(false);
            }
            catch (SearchNotFound)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.CaseNotFound]);
            }

            return existing;
        }

        private async Task<ArchiveFilter?> ValidFilterAsync(Data.Poco.Case existing, string? text,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var modelId = await dbContext.CaseWorkflow
                .Where(w => w.Guid == existing.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            IReadOnlyDictionary<string, ArchiveFilterField> fields = modelId is null
                ? new Dictionary<string, ArchiveFilterField>()
                : await ArchiveFilterFields.LoadAsync(dbContext, userName, modelId.Value, token).ConfigureAwait(false);

            try
            {
                return ArchiveFilter.Parse(text, fields);
            }
            catch (ArgumentException)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [
                    Failure(nameof(CaseKeyJournalReplayRequestDto.Filter), CaseKeyJournalResources.FilterInvalid,
                        "FilterInvalid")
                ]));
            }
        }

        private FluentValidation.Results.ValidationFailure Failure(string property, string resource, string code)
        {
            return new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code };
        }

        private DtoValidationException Invalid(string property, string resource, string code)
        {
            return new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [Failure(property, resource, code)]));
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseKeyJournalResources.PermissionDenied], permissions);
        }
    }
}

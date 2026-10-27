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
using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
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
    public sealed class CaseKeyJournalTrackService
    {
        public const int MaximumPoints = 20000;
        public const int MaximumRead = 200000;

        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseKeyJournalTrackService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search, string reportConnectionString)
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
            this.reportConnectionString = reportConnectionString;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalTrackService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalTrackService> CreateAsync(DbContext dbContext, string? userName,
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

            return new CaseKeyJournalTrackService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, search,
                dynamicEnvironment.AppSettings("ReportConnectionString") ?? dbContext.Connection.ConnectionString);
        }

        [Description("Reads the transactions of a journal view in time order, up to 20,000, with their location " +
                     "(the model's latitude and longitude, when the caller's roles can see both columns), and " +
                     "works out the movement between consecutive transactions of each key: the distance, the hours " +
                     "and the speed needed, marking a leg impossible when it is at least 50 kilometres and faster " +
                     "than 900 kilometres an hour. The view is a case key (or any identifier pivoted to), a " +
                     "window, up to three merged keys that are each their own trail, and an optional filter on the Archive. " +
                     "SummaryOnly returns just the movement summary. Read-only.")]
        [ServiceOperation("CaseKeyJournalTrackGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTrackDto> GetAsync(CaseKeyJournalTrackRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Track", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var view = await JournalViewFilter.BuildAsync(dbContext, userName, request, strings, token)
                    .ConfigureAwait(false);

                if (!JournalTravel.Valid(request.LimitKmh, request.MinimumKilometres))
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                    [
                        new FluentValidation.Results.ValidationFailure(nameof(request.LimitKmh),
                            strings[CaseKeyJournalResources.TravelLimitInvalid]) { ErrorCode = "TravelLimitInvalid" }
                    ]));
                }

                var limit = request.LimitKmh ?? JournalTravel.LimitKmh;
                var minimum = request.MinimumKilometres ?? JournalTravel.MinimumKilometres;

                try
                {
                    await search.EverythingAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
                }
                catch (SearchNotFound)
                {
                    throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
                }

                var modelId = await dbContext.CaseWorkflow
                    .Where(w => w.Guid == request.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                    .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false)
                              ?? throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);

                var xPaths = (await new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, userName)
                    .ExecuteAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false)).ToList();

                var location = await JournalLocation.LoadAsync(dbContext, modelId, xPaths, token)
                    .ConfigureAwait(false);

                var dto = new CaseKeyJournalTrackDto
                {
                    Meta = new CaseKeyJournalTrackMetaDto
                    {
                        Key = view.Key,
                        Value = view.Value,
                        From = new DateTimeOffset(view.From, TimeSpan.Zero),
                        To = new DateTimeOffset(view.To, TimeSpan.Zero),
                        Limit = MaximumPoints,
                        Location = location is null
                            ? null
                            : new CaseKeyJournalLocationDto
                            {
                                Latitude = location.Latitude, Longitude = location.Longitude
                            }
                    },
                    Summary = JournalTravel.Summarise([], limit, minimum)
                };

                if (location is null)
                {
                    return dto;
                }

                var reference = await JournalReference.LoadAsync(dbContext, request.CaseId, request.CaseWorkflowGuid,
                    xPaths, token).ConfigureAwait(false);

                var pinned = await PinnedAsync(request, token).ConfigureAwait(false);

                var indexed = await new CaseKeyJournalKeyIndexRepository(dbContext).GetReadyKeyNamesAsync(token)
                    .ConfigureAwait(false);

                var summaryOnly = request.SummaryOnly == true;
                var showAll = request.IncludeUnmatched == true && view.Filter is not null;
                var picked = showAll
                    ? (await JournalMatcher.GuidsAsync(reportConnectionString, log, modelId, userName, view, indexed,
                        MaximumPoints, token).ConfigureAwait(false)).ToHashSet()
                    : [];
                var travel = new List<JournalTravel.Point>();
                var read = 0;
                var matched = 0;
                var drawn = 0;

                await foreach (var (record, lane) in JournalMatcher.RowsAsync(reportConnectionString, log, modelId,
                                   userName, showAll ? view with { Filter = null } : view, indexed, true, token)
                                   .ConfigureAwait(false))
                {
                    read++;

                    var guid = record.EntityAnalysisModelInstanceEntryGuid;
                    var json = JournalRowBuilder.Parse(record.Json);
                    var row = JournalRowBuilder.Base(record, json, pinned.Contains(guid), lane);

                    JournalLensReader.Read(json, JournalLensReader.Payload, row, new JournalColumnSet(), xPaths);

                    drawn++;

                    if (drawn > MaximumPoints)
                    {
                        dto.Meta.Truncated = true;

                        break;
                    }

                    var isMatch = !showAll || picked.Contains(guid);

                    if (isMatch)
                    {
                        matched++;
                    }

                    JournalLensReader.Format(json, xPaths, row, reference);

                    var latitude = Coordinate(row.GetValueOrDefault(location.Latitude));
                    var longitude = Coordinate(row.GetValueOrDefault(location.Longitude));
                    var located = JournalTravel.Located(latitude, longitude);

                    if (located && isMatch)
                    {
                        travel.Add(new JournalTravel.Point(guid,
                            new DateTimeOffset(record.ReferenceDate ?? record.CreatedDate ?? DateTime.UtcNow,
                                TimeSpan.Zero), latitude!.Value, longitude!.Value, lane is { Length: > 0 } ? lane[..1] : "A"));
                    }

                    if (!summaryOnly)
                    {
                        var point = Point(row, located ? latitude : null, located ? longitude : null);

                        if (showAll)
                        {
                            point["Matched"] = isMatch;
                        }

                        dto.Points.Add(point);
                    }

                    if (read >= MaximumRead)
                    {
                        dto.Meta.Truncated = true;

                        break;
                    }
                }

                dto.Meta.Total = view.Filter is null || showAll
                    ? read
                    : await JournalMatcher.CountAsync(reportConnectionString, log, modelId, userName,
                        view with { Filter = null }, indexed, MaximumRead, token).ConfigureAwait(false);
                dto.Meta.Matched = matched;
                dto.Meta.Located = travel.Count;
                dto.Meta.Unlocated = matched - travel.Count;
                dto.Summary = JournalTravel.Summarise(travel, limit, minimum);

                if (!summaryOnly)
                {
                    dto.Points = dto.Points.OrderBy(point => point["ReferenceDate"] as DateTimeOffset?)
                        .ThenBy(point => (long)point["Id"]!).ToList();
                }

                op.Rows(matched);

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
            catch (Npgsql.PostgresException ex) when (ArchiveFilterFailure.Is(ex, request?.Filter))
            {
                op.Outcome("invalid");
                throw ArchiveFilterFailure.Unreadable(strings);
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.Track: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static Dictionary<string, object?> Point(Dictionary<string, object?> row, double? latitude,
            double? longitude)
        {
            var point = new Dictionary<string, object?>
            {
                ["Id"] = row["Id"],
                ["EntityAnalysisModelInstanceEntryGuid"] = row["EntityAnalysisModelInstanceEntryGuid"],
                ["ReferenceDate"] = row["ReferenceDate"],
                ["ResponseElevation"] = row["ResponseElevation"],
                ["Activations"] = row["Activations"],
                ["Pinned"] = row["Pinned"],
                ["Latitude"] = latitude,
                ["Longitude"] = longitude
            };

            foreach (var name in new[] { "Lane", "BoldLine", "CellFormat" })
            {
                if (row.TryGetValue(name, out var value))
                {
                    point[name] = value;
                }
            }

            return point;
        }

        private static double? Coordinate(object? value)
        {
            return value switch
            {
                null => null,
                double number => number,
                long integer => integer,
                int small => small,
                string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    => parsed,
                _ => null
            };
        }

        private async Task<ISet<Guid>> PinnedAsync(CaseKeyJournalTrackRequestDto request, CancellationToken token)
        {
            if (request.CaseId is not > 0)
            {
                return new HashSet<Guid>();
            }

            var onWorkflow = await dbContext.Case.AnyAsync(
                c => c.Id == request.CaseId.Value && c.CaseWorkflowGuid == request.CaseWorkflowGuid, token)
                .ConfigureAwait(false);

            return onWorkflow
                ? (await new CaseKeyJournalPinRepository(dbContext, userName)
                    .GetPinnedAsync(request.CaseId.Value, token).ConfigureAwait(false)).ToHashSet()
                : new HashSet<Guid>();
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

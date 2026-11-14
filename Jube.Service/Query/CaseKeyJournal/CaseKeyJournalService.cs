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
using Jube.CaseRisk.Journal;
using Jube.Data.Context;
using Jube.Data.Query.DynamicResultsSchema;
using Jube.Data.Query.GetCaseKeyJournalPinnedRowsQuery;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Data.Query.GetCaseKeyJournalTransactionQuery;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models.Hold;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Parser.Dependency;
using Jube.Parser.Dependency.Models;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Observability;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalService
    {
        public const int DefaultAnalyticsLimit = 2000;
        public const int MaximumFields = 40;

        public static readonly string[] AllSections =
        [
            "summary", "notable", "progression", "distributions", "velocity", "gaps", "novelty", "counterparties",
            "correlation", "drift", "peers"
        ];

        public const int DefaultLimit = 500;
        public const int MaximumLimit = 5000;
        private const int UnfilteredCountLimit = 100000;
        public const int DefaultDays = 90;
        public const int MaximumDays = 3660;
        private const int AroundLimit = 25;
        private const int MaximumKeyLength = 256;
        private const int MaximumValueLength = 1024;

        private static readonly int[] permissions = [1];
        private readonly ArchiveKeyFieldResolver archiveKeys;
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

        private CaseKeyJournalService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CaseSearchService search)
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

            reportConnectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                     ?? dbContext.Connection.ConnectionString;
            archiveKeys = new ArchiveKeyFieldResolver(dbContext,
                ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment));
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
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

            return new CaseKeyJournalService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment, search);
        }

        [Description("Compares the IP intelligence record archived with one transaction against a fresh lookup of the same address and lists the members that changed since. Fails when Svirfd is disabled or the transaction carries no archived record for the entry. Read-only.")]
        [ServiceOperation("CaseKeyJournalSvirfdGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalSvirfdDto> GetTransactionSvirfdAsync(
            CaseKeyJournalSvirfdRequestDto? request, Jube.Svirfd.Interfaces.ISvirfdLookup lookup,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Svirfd", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                if (!lookup.Enabled)
                {
                    throw new NotFoundException("Svirfd is disabled.");
                }

                var (record, _, _) = await LoadTransactionAsync(new CaseKeyJournalTransactionRequestDto
                {
                    CaseWorkflowGuid = request.CaseWorkflowGuid, Id = request.Id
                }, token).ConfigureAwait(false);

                var json = ParseJson(record.Json);

                if (json["svirfd"] is not JObject records)
                {
                    throw new NotFoundException(strings[CaseKeyJournalResources.TransactionNotFound]);
                }

                var property = records.Properties().FirstOrDefault(p =>
                                   string.Equals(p.Name, request.Field, StringComparison.OrdinalIgnoreCase))
                               ?? records.Properties().FirstOrDefault();

                if (property?.Value is not JObject stored)
                {
                    throw new NotFoundException(strings[CaseKeyJournalResources.TransactionNotFound]);
                }

                var storedJson = stored.ToString(Newtonsoft.Json.Formatting.None);
                var storedRecord = Jube.Svirfd.SvirfdJson.Deserialize(storedJson);
                var ip = storedRecord?.Ip ?? string.Empty;

                var remoteLookupsEnabled = await new Jube.Data.Query.GetTenantSvirfdRemoteLookupsQuery
                        .GetTenantSvirfdRemoteLookupsQuery(dbContext).ExecuteAsync(userName, token)
                    .ConfigureAwait(false);

                var fresh = string.IsNullOrEmpty(ip)
                    ? null
                    : await lookup.LookupFreshAsync(ip, remoteLookupsEnabled, token).ConfigureAwait(false);

                var dto = new CaseKeyJournalSvirfdDto
                {
                    Field = property.Name,
                    Ip = ip,
                    StoredJson = storedJson,
                    StoredAt = storedRecord?.AsOf.IndexLoaded is { } loaded
                        ? new DateTimeOffset(DateTime.SpecifyKind(loaded, DateTimeKind.Utc))
                        : null,
                    Fresh = fresh == null ? null : Jube.Service.Query.Svirfd.SvirfdLookupMapper.ToDto(fresh, lookup),
                    Drift =
                    [
                        .. Jube.Svirfd.SvirfdDrift.Compare(storedRecord, fresh).Select(d =>
                            new Jube.Dto.Query.Svirfd.SvirfdDriftItemDto
                                { Path = d.Path, Stored = d.Stored, Fresh = d.Fresh })
                    ]
                };

                op.Rows(1);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.Svirfd: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Case Key Journal: the transactions of one case key (or any identifier pivoted " +
                     "to) scored by the model the Case Workflow belongs to, newest first, with the columns of one " +
                     "lens. Lenses are payload (the XPaths the caller's roles may see), rules (every visible " +
                     "activation and whether it was forced), features (abstractions, calculations, counters and " +
                     "dictionaries the rules saw), models (Exhaustive and Advanced Analytics scores) and sanctions " +
                     "(sanction distances and HTTP adaptations). Each column carries a role that says what kind of " +
                     "field it is. The response also says which lenses have anything to show. Read-only.")]
        [ServiceOperation("CaseKeyJournalGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalDto> GetAsync(CaseKeyJournalRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var lens = string.IsNullOrWhiteSpace(request.Lens) ? JournalLensReader.Payload : request.Lens;
                var loaded = await LoadAsync(request, lens, DefaultLimit, token, true).ConfigureAwait(false);

                var dto = Build(request, lens, loaded);

                op.Rows(dto.Rows.Count);

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
                log.Error($"CaseKeyJournal.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Analyses the history of a case key (or any identifier pivoted to) so that the endpoint, not " +
                     "the grid, says what is unusual: a summary strip, each value against its own normal as a " +
                     "robust z-score with the rows that stand out, how each measure progressed over time, its " +
                     "distribution with the latest value ranked, velocity and bursts by weekday and hour, gaps and " +
                     "dormancy, first-seen values, counterparty concentration, correlation between measures and " +
                     "drift between the earlier and recent history. Only fields the caller's roles may see are " +
                     "used. Statistics are marked low confidence below twenty transactions. Read-only.")]
        [ServiceOperation("CaseKeyJournalAnalyticsGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalAnalyticsDto> GetAnalyticsAsync(CaseKeyJournalAnalyticsRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Analytics", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var sections = ParseList(request.Sections, AllSections, nameof(request.Sections),
                    CaseKeyJournalResources.SectionInvalid, "SectionInvalid");
                var loaded = await LoadAsync(request, JournalLensReader.Payload, DefaultAnalyticsLimit, token)
                    .ConfigureAwait(false);

                var peers = sections.Contains("peers")
                    ? await LoadPeersAsync(request.CaseWorkflowGuid, request.Key!, loaded, token).ConfigureAwait(false)
                    : null;

                var dto = Analyse(request, sections, loaded, peers);

                op.Rows(dto.Meta.RowCount);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.Analytics: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists every activation rule and every model that fired across the transactions of a journal " +
                     "view, one entry each, with how often it fired and the latest transaction it fired on (its " +
                     "anchor). A rule carries the counters, fields and other entities its text reads with the values " +
                     "they had on the anchor; a model carries its score against its threshold, the band when the " +
                     "score is a calibrated probability, and its largest contributions. The view is the journal's: " +
                     "key, value, window, filter, merged keys and activation-only setting. Read-only.")]
        [ServiceOperation("CaseKeyJournalActivationsGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalActivationsDto> GetActivationsAsync(CaseKeyJournalRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Activations", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var loaded = await LoadAsync(request, JournalLensReader.Payload, DefaultAnalyticsLimit, token, true)
                    .ConfigureAwait(false);

                var records = (request is { PinnedOnly: true, CaseId: > 0 } ? loaded.Pinned ?? [] : loaded.Fetched)
                    .Take(loaded.Limit).ToList();

                var rows = records.Select(record => new JournalActivationRow(record.Id,
                    record.ReferenceDate ?? record.CreatedDate, ParseJson(record.Json))).ToList();

                var ruleNames = rows.SelectMany(row => JournalTransactionReader.Activations(row.Json))
                    .Select(activation => activation.Name).Distinct(StringComparer.Ordinal).ToList();

                var inputs = ruleNames.Count == 0
                    ? new Dictionary<string, IReadOnlyList<JournalRuleInput>>(StringComparer.Ordinal)
                    : await LoadRuleInputsAsync(request.CaseWorkflowGuid, ruleNames, token).ConfigureAwait(false);

                var dto = new CaseKeyJournalActivationsDto
                {
                    Meta = new JournalActivationsMetaDto
                    {
                        Key = request.Key!,
                        Value = request.Value!,
                        CaseWorkflowGuid = request.CaseWorkflowGuid,
                        RowCount = rows.Count,
                        Limit = loaded.Limit,
                        Truncated = loaded.Fetched.Count > loaded.Limit,
                        From = new DateTimeOffset(loaded.From, TimeSpan.Zero),
                        To = new DateTimeOffset(loaded.To, TimeSpan.Zero)
                    },
                    Cards = JournalActivationCards.Build(rows, JournalTransactionReader.VisibleNames(loaded.XPaths),
                        inputs)
                };

                op.Rows(dto.Cards.Count);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.Activations: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Dictionary<string, IReadOnlyList<JournalRuleInput>>> LoadRuleInputsAsync(
            Guid caseWorkflowGuid, List<string> ruleNames, CancellationToken token)
        {
            var inputs = new Dictionary<string, IReadOnlyList<JournalRuleInput>>(StringComparer.Ordinal);

            var modelId = await dbContext.CaseWorkflow
                .Where(w => w.Guid == caseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (modelId is null)
            {
                return inputs;
            }

            var graph = await ModelDependencyGraphBuilder
                .BuildAsync(dbContext, tenantRegistryId, userName, modelId.Value, log, token).ConfigureAwait(false);

            foreach (var name in ruleNames)
            {
                var entity = graph.Entities.FirstOrDefault(candidate =>
                    candidate.Kind == ModelEntityKind.ActivationRule
                    && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

                if (entity is null)
                {
                    continue;
                }

                inputs[name] = graph.DependenciesOf(entity)
                    .Where(dependency => dependency is { Kind: ModelDependencyKind.RuleText, Target: not null })
                    .Select(dependency => new JournalRuleInput(dependency.Namespace, dependency.Name)).ToList();
            }

            return inputs;
        }

        private List<string> ParseList(string? text, string[] allowed, string property, string resource, string code)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return allowed.ToList();
            }

            var items = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.ToLowerInvariant()).Distinct().ToList();

            if (items.Count == 0 || items.Any(item => !allowed.Contains(item)))
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                    [Failure(property, resource, code)]));
            }

            return items;
        }

        private sealed record PeerSet(
            List<Data.Poco.CaseKeyJournalPeer> Rows,
            string? Segment,
            string? SegmentValue,
            bool Fallback);

        private async Task<PeerSet> LoadPeersAsync(Guid workflow, string caseKey, Loaded loaded,
            CancellationToken token)
        {
            var repository = new CaseKeyJournalPeerRepository(dbContext);
            var segment = loaded.XPaths.FirstOrDefault(xPath => xPath.Segment);
            var first = loaded.Fetched.FirstOrDefault();
            string? value = null;

            if (segment is not null && first is not null)
            {
                try
                {
                    var token0 = Data.Helpers.BoundedJsonPath.Select(ParseJson(first.Json), segment.XPath);

                    if (token0 is not null && token0.Type is not (JTokenType.Object or JTokenType.Array
                            or JTokenType.Null))
                    {
                        value = token0.ToString();
                    }
                }
                catch (Exception e) when (e is Newtonsoft.Json.JsonException or ArgumentException)
                {
                    value = null;
                }
            }

            if (!string.IsNullOrEmpty(value))
            {
                var inSegment = await repository.GetAsync(workflow, caseKey, value, token).ConfigureAwait(false);

                if (inSegment.Count > 0)
                {
                    return new PeerSet(inSegment, segment!.Name, value, false);
                }
            }

            var all = await repository.GetAsync(workflow, caseKey, null!, token).ConfigureAwait(false);

            return new PeerSet(all, segment?.Name, string.IsNullOrEmpty(value) ? null : value,
                !string.IsNullOrEmpty(value));
        }

        private static JournalPeersDto BuildPeers(PeerSet peers, List<JournalEvent> events, List<string> measures)
        {
            if (peers.Rows.Count == 0)
            {
                return new JournalPeersDto { Available = false, Reason = "NotComputed" };
            }

            var byField = peers.Rows.ToDictionary(row => row.Field, StringComparer.Ordinal);
            var comparisons = new List<PeerComparison>();

            foreach (var field in measures.Where(byField.ContainsKey))
            {
                var row = byField[field];
                var bins = JArray.Parse(row.Bins).Select(bin => new DistributionBin(bin.Value<double>("From"),
                    bin.Value<double>("To"), bin.Value<int>("Count"))).ToList();

                var peer = new PeerDistribution(row.Field, row.Segment, row.KeyCount, row.RowCount, row.Median,
                    row.Scale, row.Q1, row.Q3, row.Minimum, row.Maximum, row.Mean, bins);

                var comparison = PeerComparisonCalculator.Compare(peer,
                    events.Where(e => e.Measures.ContainsKey(field)).Select(e => e.Measures[field]).ToList());

                if (comparison is not null)
                {
                    comparisons.Add(comparison);
                }
            }

            var used = peers.Rows.Where(row => comparisons.Any(c => c.Field == row.Field)).ToList();

            return new JournalPeersDto
            {
                Available = comparisons.Count > 0,
                Reason = comparisons.Count > 0 ? null : "NoMeasures",
                ComputedDate = peers.Rows.Max(row => (DateTimeOffset?)new DateTimeOffset(row.ComputedDate,
                    TimeSpan.Zero)),
                WindowDays = used.Count > 0 ? used[0].WindowDays : peers.Rows[0].WindowDays,
                Segment = peers.Segment,
                SegmentValue = peers.SegmentValue,
                SegmentFallback = peers.Fallback,
                Comparisons = comparisons.OrderByDescending(c => Math.Abs(c.Z ?? 0))
                    .ThenBy(c => c.Field, StringComparer.Ordinal).ToList()
            };
        }

        private CaseKeyJournalAnalyticsDto Analyse(CaseKeyJournalAnalyticsRequestDto request, List<string> sections,
            Loaded loaded, PeerSet? peers)
        {
            var truncated = loaded.Fetched.Count > loaded.Limit;
            var records = loaded.Fetched.Take(loaded.Limit).ToList();
            var columns = new JournalColumnSet();
            var rows = new List<Dictionary<string, object?>>();
            var documents = new List<JObject>();

            foreach (var record in records)
            {
                var json = ParseJson(record.Json);
                var row = new Dictionary<string, object?> { ["ResponseElevation"] = record.ResponseElevation };

                documents.Add(json);

                foreach (var lens in JournalLensReader.Lenses)
                {
                    JournalLensReader.Read(json, lens, row, columns, loaded.XPaths);
                }

                rows.Add(row);
            }

            var finished = columns.All.Select(column => Finish(column, rows)).ToList();
            finished.Add(Finish(new JournalColumn("ResponseElevation", "ResponseElevation", "number", 1, "base",
                "base", false), rows));

            var only = string.IsNullOrWhiteSpace(request.Fields)
                ? null
                : request.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.Ordinal);

            var measures = finished
                .Where(c => c is { Type: "number", RoleId: (int)JournalFieldRole.Measure }
                            && (only is null || only.Contains(c.Name)))
                .OrderBy(c => c.Lens == JournalLensReader.Payload ? 0 : 1)
                .ThenByDescending(c => rows.Count(row => row.GetValueOrDefault(c.Name) is not null))
                .ThenBy(c => c.Name, StringComparer.Ordinal).Take(MaximumFields).ToList();

            var categories = finished.Where(c => c.Lens != JournalLensReader.Rules && c.Lens != "base"
                                                 && c.Type is "string" or "boolean"
                                                 && c.RoleId is (int)JournalFieldRole.Category
                                                     or (int)JournalFieldRole.Counterparty
                                                     or (int)JournalFieldRole.Identifier
                                                     or (int)JournalFieldRole.Geography).ToList();

            var events = new List<JournalEvent>();

            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                var row = rows[index];
                var at = record.ReferenceDate ?? record.CreatedDate ?? DateTime.UtcNow;
                var values = new Dictionary<string, double>(StringComparer.Ordinal);
                var labels = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var measure in measures)
                {
                    if (row.GetValueOrDefault(measure.Name) is { } value and (long or double or int))
                    {
                        values[measure.Name] = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                foreach (var category in categories)
                {
                    if (row.GetValueOrDefault(category.Name) is { } value)
                    {
                        labels[category.Name] = value.ToString() ?? string.Empty;
                    }
                }

                events.Add(new JournalEvent(record.Id, DateTime.SpecifyKind(at, DateTimeKind.Utc), values, labels,
                    JournalLensReader.VisibleActivations(documents[index]).Count, record.ResponseElevation));
            }

            var names = measures.Select(c => c.Name).ToList();
            var dto = new CaseKeyJournalAnalyticsDto
            {
                Meta = new JournalAnalyticsMetaDto
                {
                    Key = request.Key!,
                    Value = request.Value!,
                    CaseWorkflowGuid = request.CaseWorkflowGuid,
                    RowCount = events.Count,
                    Limit = loaded.Limit,
                    Truncated = truncated,
                    From = new DateTimeOffset(loaded.From, TimeSpan.Zero),
                    To = new DateTimeOffset(loaded.To, TimeSpan.Zero),
                    LowConfidence = events.Count < RobustStatistics.ConfidentRows,
                    Sections = sections
                },
                Fields = measures.Select(c => Field(c, "measure"))
                    .Concat(categories.Select(c => Field(c, c.RoleId == (int)JournalFieldRole.Counterparty
                        ? "counterparty"
                        : "category"))).ToList()
            };

            var counterparties = categories.Where(c => c.RoleId == (int)JournalFieldRole.Counterparty)
                .Select(c => c.Name).ToList();

            if (sections.Contains("summary")) dto.Summary = JournalSummaryCalculator.Compute(events, names);
            if (sections.Contains("notable")) dto.Notable = NotableCalculator.Compute(events, names);
            if (sections.Contains("progression")) dto.Progression = ProgressionCalculator.Compute(events, names);
            if (sections.Contains("distributions")) dto.Distributions = DistributionCalculator.Compute(events, names);
            if (sections.Contains("velocity")) dto.Velocity = VelocityCalculator.Compute(events);
            if (sections.Contains("gaps")) dto.Gaps = GapCalculator.Compute(events);
            if (sections.Contains("novelty"))
                dto.Novelty = NoveltyCalculator.Compute(events, categories.Select(c => c.Name).ToList());
            if (sections.Contains("counterparties"))
                dto.Counterparties = CounterpartyCalculator.Compute(events, counterparties,
                    names.Contains(request.AmountField ?? string.Empty) ? request.AmountField : null);
            if (sections.Contains("correlation")) dto.Correlation = CorrelationCalculator.Compute(events, names);
            if (sections.Contains("drift")) dto.Drift = DriftCalculator.Compute(events, names);
            if (sections.Contains("peers") && peers is not null)
                dto.Peers = BuildPeers(peers, events, names);

            return dto;
        }

        private static JournalAnalyticsFieldDto Field(CaseKeyJournalColumnDto column, string kind)
        {
            return new JournalAnalyticsFieldDto
            {
                Name = column.Name,
                Title = column.Title,
                Lens = column.Lens,
                Group = column.Group,
                RoleId = column.RoleId,
                RoleInferred = column.RoleInferred,
                Kind = kind
            };
        }

        [Description("Opens one archived transaction of the Case Key Journal as the rules and models saw it: " +
                     "which activations fired, the features and sanction distances the rules saw, the identifiers " +
                     "it can be pivoted to, and for every Exhaustive, Advanced Analytics and HTTP model the score, " +
                     "its threshold, the model's version, the calibration, every input's contribution and the " +
                     "decision path it took. Payload inputs the caller's roles may not see are hidden, never named. " +
                     "Read-only.")]
        [ServiceOperation("CaseKeyJournalTransactionGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTransactionDto> GetTransactionAsync(
            CaseKeyJournalTransactionRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Transaction", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var (record, xPaths, _) = await LoadTransactionAsync(request, token).ConfigureAwait(false);
                var json = ParseJson(record.Json);
                var visible = JournalTransactionReader.VisibleNames(xPaths);

                var dto = new CaseKeyJournalTransactionDto
                {
                    Id = record.Id,
                    EntityAnalysisModelInstanceEntryGuid = record.EntityAnalysisModelInstanceEntryGuid,
                    ReferenceDate = record.ReferenceDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(record.ReferenceDate.Value, DateTimeKind.Utc),
                            TimeSpan.Zero)
                        : null,
                    CreatedDate = record.CreatedDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(record.CreatedDate.Value, DateTimeKind.Utc),
                            TimeSpan.Zero)
                        : null,
                    ResponseElevation = record.ResponseElevation,
                    ResponseElevationName = JournalLensReader.Member(json, "responseElevation", "name")?.ToString(),
                    Hold = ResponseHoldJson.Read(json),
                    EntryKeyValue = record.EntryKeyValue,
                    ModelName = json.Value<string>("entityAnalysisModelName"),
                    Tags = json["tag"] is JArray tags ? tags.Select(tag => tag.ToString()).ToList() : [],
                    Activations = JournalTransactionReader.Activations(json),
                    Values = JournalTransactionReader.Values(json),
                    Keys = JournalTransactionReader.Keys(json, xPaths),
                    Adaptations = JournalTransactionReader.Adaptations(json, visible)
                };

                op.Rows(1);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.Transaction: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Opens what fired on one archived transaction of the Case Key Journal and why: every activation " +
                     "rule with the counters, fields and other entities its text read and the values they had on " +
                     "this transaction, and every model whose score crossed its threshold with the score against the " +
                     "threshold, the band when the score is a calibrated probability, and its largest " +
                     "contributions. Payload inputs the caller's roles may not see are hidden, never named. " +
                     "Read-only.")]
        [ServiceOperation("CaseKeyJournalTransactionActivationsGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalActivationsDto> GetTransactionActivationsAsync(
            CaseKeyJournalTransactionRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TransactionActivations", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var (record, xPaths, _) = await LoadTransactionAsync(request, token).ConfigureAwait(false);
                var row = new JournalActivationRow(record.Id, record.ReferenceDate ?? record.CreatedDate,
                    ParseJson(record.Json));

                var ruleNames = JournalTransactionReader.Activations(row.Json).Select(activation => activation.Name)
                    .Distinct(StringComparer.Ordinal).ToList();

                var inputs = ruleNames.Count == 0
                    ? new Dictionary<string, IReadOnlyList<JournalRuleInput>>(StringComparer.Ordinal)
                    : await LoadRuleInputsAsync(request.CaseWorkflowGuid, ruleNames, token).ConfigureAwait(false);

                var moment = new DateTimeOffset(
                    DateTime.SpecifyKind(record.ReferenceDate ?? record.CreatedDate ?? DateTime.UtcNow,
                        DateTimeKind.Utc), TimeSpan.Zero);

                var dto = new CaseKeyJournalActivationsDto
                {
                    Meta = new JournalActivationsMetaDto
                    {
                        Key = string.Empty,
                        Value = record.EntryKeyValue ?? string.Empty,
                        CaseWorkflowGuid = request.CaseWorkflowGuid,
                        RowCount = 1,
                        Limit = 1,
                        From = moment,
                        To = moment
                    },
                    Cards = JournalActivationCards.Build([row], JournalTransactionReader.VisibleNames(xPaths), inputs)
                };

                op.Rows(dto.Cards.Count);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.TransactionActivations: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Shows what else happened on the same day as one transaction, for each identifier it carries " +
                     "that the journal can be pivoted on: the other transactions scored by the same model that " +
                     "shared the account, device, address or counterparty, newest first, up to " +
                     "twenty-five for each. The day is the UTC day the transaction was created on. Read-only.")]
        [ServiceOperation("CaseKeyJournalAroundGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalAroundDto> GetAroundAsync(CaseKeyJournalTransactionRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "Around", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var (record, xPaths, modelId) = await LoadTransactionAsync(request, token).ConfigureAwait(false);
                var json = ParseJson(record.Json);
                var day = (record.CreatedDate ?? record.ReferenceDate ?? DateTime.UtcNow).Date;
                var next = day.AddDays(1);
                var query = new GetCaseKeyJournalRowsQuery(reportConnectionString, log);
                var seen = new HashSet<(string, string)>();
                var groups = new List<CaseKeyJournalAroundGroupDto>();

                foreach (var key in JournalTransactionReader.Keys(json, xPaths))
                {
                    if (!key.Drill || string.IsNullOrEmpty(key.PayloadKey) || string.IsNullOrEmpty(key.Value)
                        || !seen.Add((key.PayloadKey, key.Value)))
                    {
                        continue;
                    }

                    var resolution = await archiveKeys.ResolveAsync(modelId, key.PayloadKey, key.Value, token)
                        .ConfigureAwait(false);

                    if (!resolution.Resolved)
                    {
                        continue;
                    }

                    var found = await query.ExecuteAsync(modelId, userName, resolution.Predicate!, day,
                        next.AddTicks(-1), 0, 0, AroundLimit + 2, token).ConfigureAwait(false);
                    var others = found.Where(other => other.Id != record.Id).ToList();

                    if (others.Count == 0)
                    {
                        continue;
                    }

                    groups.Add(new CaseKeyJournalAroundGroupDto
                    {
                        Name = key.Name,
                        Key = key.PayloadKey,
                        Value = key.Value,
                        Truncated = others.Count > AroundLimit,
                        Rows = others.Take(AroundLimit).Select(other =>
                        {
                            var activations = JournalLensReader.VisibleActivations(ParseJson(other.Json));

                            return new CaseKeyJournalAroundRowDto
                            {
                                Id = other.Id,
                                ReferenceDate = other.ReferenceDate.HasValue
                                    ? DynamicResultsSchema.ToUtcOffset(other.ReferenceDate.Value)
                                    : null,
                                ResponseElevation = other.ResponseElevation,
                                ActivationCount = activations.Count,
                                Activations = string.Join(", ", activations)
                            };
                        }).ToList()
                    });
                }

                op.Rows(groups.Sum(group => group.Rows.Count));

                return new CaseKeyJournalAroundDto
                {
                    From = new DateTimeOffset(day, TimeSpan.Zero),
                    To = new DateTimeOffset(next, TimeSpan.Zero),
                    Groups = groups
                };
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
                log.Error($"CaseKeyJournal.Around: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the payload of one archived transaction, limited to the fields the caller's roles " +
                     "may see on the Case Workflow, and records that the caller read it. Read-only.")]
        [ServiceOperation("CaseKeyJournalTransactionPayloadGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalPayloadDto> GetTransactionPayloadAsync(
            CaseKeyJournalTransactionRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TransactionPayload", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var (record, xPaths, _) = await LoadTransactionAsync(request, token).ConfigureAwait(false);
                var json = ParseJson(record.Json);
                var columns = new JournalColumnSet();
                var row = new Dictionary<string, object?>();

                JournalLensReader.Read(json, JournalLensReader.Payload, row, columns, xPaths);

                var dto = new CaseKeyJournalPayloadDto
                {
                    Id = record.Id,
                    EntityAnalysisModelInstanceEntryGuid = record.EntityAnalysisModelInstanceEntryGuid,
                    Fields = xPaths.Select(xPath => xPath.Name).Distinct(StringComparer.Ordinal)
                        .ToDictionary(name => name, name => row[name], StringComparer.Ordinal)
                };

                op.Rows(dto.Fields.Count);

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
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.TransactionPayload: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<(GetCaseKeyJournalRowsQueryDto Record,
            List<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> XPaths, int ModelId)> LoadTransactionAsync(
            CaseKeyJournalTransactionRequestDto request, CancellationToken token)
        {
            if (request.Id < 1)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                    [Failure(nameof(request.Id), CaseKeyJournalResources.TransactionIdInvalid, "TransactionIdInvalid")]));
            }

            try
            {
                await search.EverythingAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
            }
            catch (SearchNotFound)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
            }

            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == request.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (modelId is null)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
            }

            var record = await new GetCaseKeyJournalTransactionQuery(reportConnectionString, log)
                .ExecuteAsync(request.Id, modelId.Value, userName, token).ConfigureAwait(false);

            if (record is null)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.TransactionNotFound]);
            }

            var xPaths = (await new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, userName)
                .ExecuteAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false)).ToList();

            return (record, xPaths, modelId.Value);
        }

        private sealed record Loaded(
            List<GetCaseKeyJournalRowsQueryDto> Fetched,
            List<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> XPaths,
            int Limit,
            DateTime From,
            DateTime To,
            Dictionary<long, string>? Lanes = null,
            List<GetCaseKeyJournalRowsQueryDto>? Pinned = null,
            Dictionary<string, string>? Reference = null,
            JournalLocation? Location = null,
            int? Total = null);

        private async Task<Loaded> LoadAsync(CaseKeyJournalRequestDto request, string lens, int defaultLimit,
            CancellationToken token, bool withPinsAndLanes = false)
        {
            var now = DateTime.UtcNow;
            var limit = request.Limit ?? defaultLimit;
            var to = request.To?.UtcDateTime ?? now;
            var from = request.From?.UtcDateTime ?? to.AddDays(-DefaultDays);

            Validate(request, lens, limit, from, to);

            try
            {
                await search.EverythingAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
            }
            catch (SearchNotFound)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
            }

            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == request.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            var xPaths = (await new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, userName)
                .ExecuteAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false)).ToList();

            if (modelId is null)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
            }

            ArchiveFilter? archiveFilter;

            try
            {
                archiveFilter = ArchiveFilter.Parse(request.Filter, string.IsNullOrWhiteSpace(request.Filter)
                    ? new Dictionary<string, ArchiveFilterField>()
                    : await ArchiveFilterFields.LoadAsync(dbContext, userName, modelId.Value, token)
                        .ConfigureAwait(false));
            }
            catch (ArgumentException)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [
                    Failure(nameof(request.Filter), CaseKeyJournalResources.FilterInvalid, "FilterInvalid")
                ]));
            }

            Func<List<object>, string>? predicate = archiveFilter is null ? null : archiveFilter.Apply;
            var archiveIds = ParseArchiveIds(request.ArchiveIds);

            if (archiveIds.Count > 0)
            {
                var inner = predicate;
                var idList = string.Join(",", archiveIds.Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                predicate = tokens =>
                    (inner is null ? string.Empty : inner(tokens) + " and ") + $"a.\"Id\" in ({idList})";
            }
            var rowsQuery = new GetCaseKeyJournalRowsQuery(reportConnectionString, log);
            var location = await JournalLocation.LoadAsync(dbContext, modelId.Value, xPaths, token).ConfigureAwait(false);
            var pinnedOnly = withPinsAndLanes && request is { PinnedOnly: true, CaseId: > 0 };
            var merge = withPinsAndLanes && !pinnedOnly ? JournalMerge.Parse(request.Merge) ?? [] : [];
            var lanePredicates = await JournalLanes.ResolveAsync(archiveKeys, modelId.Value,
                [new JournalLane(request.Key!, request.Value!), .. merge], strings, token, nameof(request.Merge),
                request.CaseId is > 0).ConfigureAwait(false);
            var fetched = pinnedOnly
                ? []
                : await rowsQuery.ExecuteAsync(
                    modelId.Value, userName, lanePredicates[0], from, to, request.ResponseElevation ?? 0,
                    request.ActivationsOnly == true ? 1 : 0, limit + 1, token, predicate)
                    .ConfigureAwait(false);

            int? total = null;

            if (archiveFilter is not null && !pinnedOnly)
            {
                total = await JournalMatcher.CountAsync(reportConnectionString, log, modelId.Value, userName,
                    new JournalMatchFilter(request.Key!, request.Value!, from, to, request.ActivationsOnly == true,
                        request.ResponseElevation ?? 0, merge) { Predicates = lanePredicates },
                    UnfilteredCountLimit, token).ConfigureAwait(false);
            }

            if (!withPinsAndLanes)
            {
                return new Loaded(fetched, xPaths, limit, from, to, Location: location, Total: total);
            }

            var lanes = new Dictionary<long, string>();

            if (merge.Count > 0)
            {
                foreach (var record in fetched)
                {
                    lanes[record.Id] = "A";
                }

                for (var index = 0; index < merge.Count; index++)
                {
                    var letter = (char)('B' + index);
                    var extra = await rowsQuery.ExecuteAsync(modelId.Value, userName, lanePredicates[index + 1],
                        from, to, request.ResponseElevation ?? 0, request.ActivationsOnly == true ? 1 : 0,
                        limit + 1, token, predicate).ConfigureAwait(false);

                    foreach (var record in extra)
                    {
                        if (lanes.TryGetValue(record.Id, out var existing))
                        {
                            lanes[record.Id] = existing + letter;
                        }
                        else
                        {
                            lanes[record.Id] = letter.ToString();
                            fetched.Add(record);
                        }
                    }
                }

                fetched = fetched.OrderByDescending(record => record.Id).ToList();
            }

            var pinned = await LoadPinnedAsync(request, modelId.Value, predicate, token).ConfigureAwait(false);
            var reference = await JournalReference.LoadAsync(dbContext, request.CaseId, request.CaseWorkflowGuid, xPaths, token)
                .ConfigureAwait(false);

            return new Loaded(fetched, xPaths, limit, from, to, merge.Count > 0 ? lanes : null, pinned, reference,
                location, total);
        }

        private async Task<List<GetCaseKeyJournalRowsQueryDto>> LoadPinnedAsync(CaseKeyJournalRequestDto request,
            int modelId, Func<List<object>, string>? predicate, CancellationToken token)
        {
            if (request.CaseId is not > 0)
            {
                return [];
            }

            var onWorkflow = await dbContext.Case.AnyAsync(
                c => c.Id == request.CaseId.Value && c.CaseWorkflowGuid == request.CaseWorkflowGuid, token)
                .ConfigureAwait(false);

            if (!onWorkflow)
            {
                return [];
            }

            var pinned = await new CaseKeyJournalPinRepository(dbContext, userName)
                .GetPinnedAsync(request.CaseId.Value, token).ConfigureAwait(false);

            var records = await new GetCaseKeyJournalPinnedRowsQuery(reportConnectionString, log)
                .ExecuteAsync(modelId, userName, pinned, token, predicate).ConfigureAwait(false);

            var order = pinned.Select((guid, index) => (guid, index)).ToDictionary(x => x.guid, x => x.index);

            return records.OrderBy(record => order[record.EntityAnalysisModelInstanceEntryGuid]).ToList();
        }

        private CaseKeyJournalDto Build(CaseKeyJournalRequestDto request, string lens, Loaded loaded)
        {
            var (fetched, xPaths, limit, from, to, lanes, pinned, reference, location, total) = loaded;
            var truncated = fetched.Count > limit;
            var pinnedIds = (pinned ?? []).Select(record => record.Id).ToHashSet();
            var records = (pinned ?? []).Concat(fetched.Where(record => !pinnedIds.Contains(record.Id)).Take(limit))
                .ToList();
            var lensColumns = new JournalColumnSet();
            var presence = JournalLensReader.Lenses.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
            var rows = new List<Dictionary<string, object?>>();

            foreach (var record in records)
            {
                var json = ParseJson(record.Json);

                var row = JournalRowBuilder.Base(record, json, pinnedIds.Contains(record.Id),
                    lanes is null ? null : lanes.GetValueOrDefault(record.Id, string.Empty));

                foreach (var (name, present) in JournalLensReader.Presence(json))
                {
                    if (present)
                    {
                        presence[name]++;
                    }
                }

                JournalLensReader.Read(json, lens, row, lensColumns, xPaths);
                JournalLensReader.Format(json, xPaths, row, reference);
                rows.Add(row);
            }

            var columns = BaseColumns().Concat(OrderedLensColumns(lens, lensColumns, xPaths))
                .Select(column => Finish(column, rows)).ToList();

            return new CaseKeyJournalDto
            {
                Meta = new CaseKeyJournalMetaDto
                {
                    Key = request.Key!,
                    Value = request.Value!,
                    CaseWorkflowGuid = request.CaseWorkflowGuid,
                    Lens = lens,
                    RowCount = rows.Count,
                    Limit = limit,
                    Truncated = truncated,
                    Total = total ?? rows.Count,
                    DrillKeys = xPaths.Where(xPath => xPath.Drill)
                        .Select(xPath => (xPath.Key, xPath.Name, xPath.DataTypeId))
                        .Where(found => !string.IsNullOrEmpty(found.Key))
                        .GroupBy(found => found.Key!, StringComparer.Ordinal)
                        .Select(group => new CaseKeyJournalDrillKeyDto
                        {
                            Key = group.Key, Title = group.First().Name, DataTypeId = group.First().DataTypeId
                        })
                        .OrderBy(key => key.Title, StringComparer.OrdinalIgnoreCase).ToList(),
                    Location = location is null
                        ? null
                        : new CaseKeyJournalLocationDto { Latitude = location.Latitude, Longitude = location.Longitude },
                    From = new DateTimeOffset(from, TimeSpan.Zero),
                    To = new DateTimeOffset(to, TimeSpan.Zero)
                },
                Columns = columns,
                Lenses = JournalLensReader.Lenses
                    .Select(name => new CaseKeyJournalLensDto { Lens = name, Rows = presence[name] }).ToList(),
                Rows = rows
            };
        }

        private static IEnumerable<JournalColumn> OrderedLensColumns(string lens, JournalColumnSet columns,
            IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
        {
            if (lens != JournalLensReader.Payload)
            {
                return columns.All.OrderBy(column => column.Group, StringComparer.Ordinal)
                    .ThenBy(column => column.Title, StringComparer.OrdinalIgnoreCase);
            }

            var order = xPaths.Select((xPath, index) => (xPath.Name, index)).ToDictionary(x => x.Name, x => x.index);

            return columns.All.OrderBy(column => order.GetValueOrDefault(column.Name, int.MaxValue));
        }

        private static CaseKeyJournalColumnDto Finish(JournalColumn column, List<Dictionary<string, object?>> rows)
        {
            var values = rows.Select(row => row.GetValueOrDefault(column.Name)).ToList();
            var (roleId, inferred) = FieldRoleInference.Infer(column.RoleId, column.Type, column.Drill, values);

            return new CaseKeyJournalColumnDto
            {
                Name = column.Name,
                Title = column.Title,
                Type = column.Type,
                RoleId = roleId,
                RoleInferred = inferred,
                Lens = column.Lens,
                Group = column.Group,
                Drill = column.Drill
            };
        }

        private static IEnumerable<JournalColumn> BaseColumns()
        {
            return
            [
                new JournalColumn("Id", "Id", "number", 4, "base", "base", false),
                new JournalColumn("EntityAnalysisModelInstanceEntryGuid", "EntityAnalysisModelInstanceEntryGuid",
                    "string", 4, "base", "base", false),
                new JournalColumn("ReferenceDate", "ReferenceDate", "date", 5, "base", "base", false),
                new JournalColumn("CreatedDate", "CreatedDate", "date", 5, "base", "base", false),
                new JournalColumn("ResponseElevation", "ResponseElevation", "number", 1, "base", "base", false),
                new JournalColumn("ResponseElevationName", "ResponseElevationName", "string", 2, "base", "base",
                    false),
                new JournalColumn("HoldState", "HoldState", "number", 1, "base", "base", false),
                new JournalColumn("EntryKeyValue", "EntryKeyValue", "string", 4, "base", "base", false),
                new JournalColumn("Activations", "Activations", "string", 2, "base", "base", false),
                new JournalColumn("ActivationCount", "ActivationCount", "number", 1, "base", "base", false),
                new JournalColumn("Tag", "Tag", "array", 2, "base", "base", false)
            ];
        }

        private static JObject ParseJson(string? json)
        {
            try
            {
                return JObject.Parse(json ?? "{}");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return new JObject();
            }
        }

        private void Validate(CaseKeyJournalRequestDto request, string lens, int limit, DateTime from, DateTime to)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                failures.Add(Failure(nameof(request.Key), CaseKeyJournalResources.KeyRequired, "KeyRequired"));
            }
            else if (request.Key.Length > MaximumKeyLength || request.Key.Any(char.IsControl))
            {
                failures.Add(Failure(nameof(request.Key), CaseKeyJournalResources.KeyInvalid, "KeyInvalid"));
            }

            if (string.IsNullOrEmpty(request.Value) || request.Value.Length > MaximumValueLength
                                                    || request.Value.Contains('\0'))
            {
                failures.Add(Failure(nameof(request.Value), CaseKeyJournalResources.ValueRequired, "ValueRequired"));
            }

            if (!JournalLensReader.IsLens(lens))
            {
                failures.Add(Failure(nameof(request.Lens), CaseKeyJournalResources.LensInvalid, "LensInvalid"));
            }

            if (!JournalMerge.IsValid(JournalMerge.Parse(request.Merge)))
            {
                failures.Add(Failure(nameof(request.Merge), CaseKeyJournalResources.MergeInvalid, "MergeInvalid"));
            }

            if (limit is < 1 or > MaximumLimit)
            {
                failures.Add(Failure(nameof(request.Limit), CaseKeyJournalResources.LimitInvalid, "LimitInvalid"));
            }

            if (from >= to || (to - from).TotalDays > MaximumDays)
            {
                failures.Add(Failure(nameof(request.From), CaseKeyJournalResources.PeriodInvalid, "PeriodInvalid"));
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(failures));
            }
        }

        private FluentValidation.Results.ValidationFailure Failure(string property, string resource, string code)
        {
            return new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code };
        }

        private static List<long> ParseArchiveIds(string? list)
        {
            var ids = new List<long>();

            if (string.IsNullOrWhiteSpace(list))
            {
                return ids;
            }

            foreach (var part in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (long.TryParse(part, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var id) && ids.Count < 500)
                {
                    ids.Add(id);
                }
            }

            return ids;
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

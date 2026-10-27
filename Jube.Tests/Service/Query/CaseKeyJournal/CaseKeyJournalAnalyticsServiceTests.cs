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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseKeyJournalAnalyticsServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string DrillName = "AccountId";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly List<long> createdArchiveIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdXPathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.Archive.Where(w => createdArchiveIds.Contains(w.Id)).DeleteAsync();

            var caseWorkflowXPathGuids = dbContext.CaseWorkflowXPath.Where(s => createdXPathIds.Contains(s.Id))
                .Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => caseWorkflowXPathGuids.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId != null && createdXPathIds.Contains(w.CaseWorkflowXPathId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id)).DeleteAsync();

            var caseWorkflowGuids =
                dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.CaseKeyJournalPeer.Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid))
                .DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseKeyJournalService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(saved.Id);

            return saved.Id;
        }

        private async Task<Guid> CreateWorkflowAsync(DbContext dbContext, int modelId, string user,
            bool grantRoleToUser)
        {
            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdWorkflowIds.Add(id);

            if (!grantRoleToUser)
            {
                return guid;
            }

            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == user)
                .Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return guid;
        }

        private async Task CreateXPathAsync(DbContext dbContext, Guid workflowGuid, string user, string name,
            string xPath, bool grantRoleToUser, bool boldLine = false, bool regexFormat = false)
        {
            var workflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == workflowGuid).Select(w => w.Id)
                .FirstAsync();

            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
            {
                Guid = guid,
                CaseWorkflowId = workflowId,
                Name = name,
                XPath = xPath,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Drill = 0,
                Version = 1,
                BoldLineMatched = (byte)(boldLine ? 1 : 0),
                BoldLineFormatBackColor = "#111111",
                BoldLineFormatForeColor = "#222222",
                ConditionalRegularExpressionFormatting = (byte)(regexFormat ? 1 : 0),
                RegularExpression = "^Match",
                ConditionalFormatBackColor = "#333333",
                ConditionalFormatForeColor = "#444444",
                ForeRowColorScope = 1,
                BackRowColorScope = 0,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdXPathIds.Add(id);

            if (grantRoleToUser)
            {
                var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == user)
                    .Select(u => u.RoleRegistryGuid).FirstAsync();

                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
                {
                    CaseWorkflowXPathGuid = guid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = roleGuid,
                    CreatedUser = user,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }
        }

        private async Task CreateArchiveAsync(DbContext dbContext, int modelId, string keyValue,
            double responseElevation = 5, int activationRuleCount = 0, JObject? payload = null,
            JObject? facets = null, DateTime? created = null, JToken? keyToken = null, string[]? tags = null)
        {
            var at = created ?? DateTime.UtcNow;
            payload ??= new JObject();
            payload[DrillName] = keyToken ?? keyValue;

            var json = facets ?? new JObject();
            json["payload"] = payload;
            json["tag"] = new JArray(tags ?? []);

            var id = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                EntryKeyValue = keyValue,
                ResponseElevation = responseElevation,
                ActivationRuleCount = activationRuleCount,
                CreatedDate = at,
                ReferenceDate = at,
            }).ConfigureAwait(false);
            createdArchiveIds.Add(id);
        }

        private static string NewKey() => $"{DatabaseFixture.Prefix}Acct{Guid.NewGuid():N}";

        private string Me => fx.Seed.UserWithPermission;

        private async Task<(Guid Workflow, int Model)> SetupAsync(DbContext dbContext, bool grant = true)
        {
            var model = await CreateModelAsync(dbContext, Me);
            var workflow = await CreateWorkflowAsync(dbContext, model, Me, grant);

            return (workflow, model);
        }

        private async Task<CaseKeyJournalAnalyticsDto> AnalyseAsync(DbContext dbContext, Guid workflow, string key,
            Action<CaseKeyJournalAnalyticsRequestDto>? change = null)
        {
            var service = await BuildServiceAsync(dbContext, Me);
            var request = new CaseKeyJournalAnalyticsRequestDto
            {
                CaseWorkflowGuid = workflow, Key = DrillName, Value = key
            };

            change?.Invoke(request);

            return await service.GetAnalyticsAsync(request);
        }

        private static async Task SeedHistoryAsync(CaseKeyJournalAnalyticsServiceTests self, DbContext dbContext,
            int model, string key, int count, Func<int, double> amount, Func<int, string>? counterparty = null,
            JObject? facets = null)
        {
            for (var i = 0; i < count; i++)
            {
                var payload = new JObject { ["Amount"] = amount(i) };

                if (counterparty is not null) payload["Beneficiary"] = counterparty(i);

                await self.CreateArchiveAsync(dbContext, model, key, payload: payload,
                    facets: facets is null ? null : (JObject)facets.DeepClone(),
                    created: DateTime.UtcNow.AddHours(-(count - i)));
            }
        }

        [Fact]
        public async Task AnUnusualTransactionStandsOutAgainstTheKeysOwnNormalAndNamesTheFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 30, i => i == 29 ? 9000 : 100 + i % 4);

            var report = await AnalyseAsync(dbContext, workflow, key);

            report.Meta.RowCount.Should().Be(30);
            report.Meta.LowConfidence.Should().BeFalse();
            report.Notable!.Rows.Should().ContainSingle().Which.Worst.Should().Be("Amount");
            report.Fields.Should().Contain(field => field.Name == "Amount" && field.Kind == "measure");
            report.Summary!.Count.Should().Be(30);
            report.Distributions!.Single(d => d.Field == "Amount").Count.Should().Be(30);
            report.Progression!.Series.Should().Contain(series => series.Field == "Amount");
            report.Peers!.Available.Should().BeFalse();
            report.Peers.Reason.Should().Be("NotComputed");
        }

        private static async Task StorePeerAsync(DbContext dbContext, Guid workflow, string field, string? segment,
            Func<int, double> keyMedian, int keys = 40, string caseKey = DrillName)
        {
            var peer = PeerDistributionCalculator.Build(field, segment,
                Enumerable.Range(0, keys).Select(i => (IReadOnlyList<double>)new[] { keyMedian(i) }).ToList())!;

            await new CaseKeyJournalPeerRepository(dbContext).UpsertAsync(new Data.Poco.CaseKeyJournalPeer
            {
                CaseWorkflowGuid = workflow,
                CaseKey = caseKey,
                Field = peer.Field,
                Segment = peer.Segment,
                KeyCount = peer.KeyCount,
                RowCount = peer.RowCount,
                Median = peer.Median,
                Scale = peer.Scale,
                Q1 = peer.Q1,
                Q3 = peer.Q3,
                Minimum = peer.Minimum,
                Maximum = peer.Maximum,
                Mean = peer.Mean,
                Bins = Newtonsoft.Json.JsonConvert.SerializeObject(peer.Bins),
                WindowDays = 90,
                ComputedDate = new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc)
            });
        }

        [Fact]
        public async Task AKeyIsComparedWithItsPeersFieldByFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await StorePeerAsync(dbContext, workflow, "Amount", null, i => 10 + i);
            await StorePeerAsync(dbContext, workflow, "Elsewhere", null, i => 10 + i);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 30, i => 500 + i % 3);

            var report = await AnalyseAsync(dbContext, workflow, key);

            var peers = report.Peers!;

            peers.Available.Should().BeTrue();
            peers.Reason.Should().BeNull();
            peers.WindowDays.Should().Be(90);
            peers.ComputedDate.Should().Be(new DateTimeOffset(2026, 10, 20, 3, 0, 0, TimeSpan.Zero));
            peers.Segment.Should().BeNull();
            peers.SegmentFallback.Should().BeFalse();

            var comparison = peers.Comparisons.Should().ContainSingle().Subject;

            comparison.Field.Should().Be("Amount");
            comparison.Direction.Should().Be("above");
            comparison.Z.Should().Be(5);
            comparison.Percentile.Should().Be(1);
            comparison.KeyMedian.Should().Be(501);
            comparison.PeerKeys.Should().Be(40);
            comparison.KeyRows.Should().Be(30);
            comparison.LowConfidence.Should().BeFalse();
        }

        [Fact]
        public async Task ThePeersAreTheKeysSegmentWhenItHasOneAndAllKeysWhenItDoesNotAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Tier", "$.payload.Tier", true);
            await dbContext.CaseWorkflowXPath.Where(w => w.Name == "Tier" && createdXPathIds.Contains(w.Id))
                .Set(w => w.Segment, (byte)1).UpdateAsync();
            await StorePeerAsync(dbContext, workflow, "Amount", null, i => 10 + i);
            await StorePeerAsync(dbContext, workflow, "Amount", "Gold", i => 480 + i);
            var gold = NewKey();
            var silver = NewKey();

            for (var i = 0; i < 30; i++)
            {
                await CreateArchiveAsync(dbContext, model, gold,
                    payload: new JObject { ["Amount"] = 500 + i % 3, ["Tier"] = "Gold" },
                    created: DateTime.UtcNow.AddHours(-(30 - i)));
                await CreateArchiveAsync(dbContext, model, silver,
                    payload: new JObject { ["Amount"] = 500 + i % 3, ["Tier"] = "Silver" },
                    created: DateTime.UtcNow.AddHours(-(30 - i)));
            }

            var inSegment = (await AnalyseAsync(dbContext, workflow, gold)).Peers!;
            var fallback = (await AnalyseAsync(dbContext, workflow, silver)).Peers!;

            inSegment.Segment.Should().Be("Tier");
            inSegment.SegmentValue.Should().Be("Gold");
            inSegment.SegmentFallback.Should().BeFalse();
            inSegment.Comparisons.Single().Direction.Should().Be("typical");
            inSegment.Comparisons.Single().PeerMedian.Should().BeApproximately(499.5, 0.6);

            fallback.Segment.Should().Be("Tier");
            fallback.SegmentValue.Should().Be("Silver");
            fallback.SegmentFallback.Should().BeTrue();
            fallback.Comparisons.Single().Direction.Should().Be("above");
        }

        [Fact]
        public async Task TheMostUnusualPeerComparisonComesFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Balance", "$.payload.Balance", true);
            await StorePeerAsync(dbContext, workflow, "Amount", null, i => 10 + i);
            await StorePeerAsync(dbContext, workflow, "Balance", null, i => 10 + i);
            var key = NewKey();

            for (var i = 0; i < 30; i++)
            {
                await CreateArchiveAsync(dbContext, model, key,
                    payload: new JObject { ["Amount"] = 45, ["Balance"] = 90000 },
                    created: DateTime.UtcNow.AddHours(-(30 - i)));
            }

            var comparisons = (await AnalyseAsync(dbContext, workflow, key)).Peers!.Comparisons;

            comparisons.Select(c => c.Field).Should().Equal("Balance", "Amount");
            comparisons[0].Z.Should().Be(5);
            comparisons[1].Z.Should().BeInRange(0.5, 3);
        }

        [Fact]
        public async Task PeersWithNothingToCompareSayWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await StorePeerAsync(dbContext, workflow, "Elsewhere", null, i => 10 + i);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 30, i => 100 + i % 3);

            var peers = (await AnalyseAsync(dbContext, workflow, key)).Peers!;

            peers.Available.Should().BeFalse();
            peers.Reason.Should().Be("NoMeasures");
            peers.Comparisons.Should().BeEmpty();
        }

        [Fact]
        public async Task ThePeerRepositoryUpdatesInPlaceAndKeepsSegmentsApartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            await StorePeerAsync(dbContext, workflow, "Amount", null, i => 10 + i);
            await StorePeerAsync(dbContext, workflow, "Amount", "Gold", i => 10 + i);
            await StorePeerAsync(dbContext, workflow, "Amount", null, i => 100 + i);
            var repository = new CaseKeyJournalPeerRepository(dbContext);

            var all = await repository.GetAsync(workflow, DrillName, null!);
            var gold = await repository.GetAsync(workflow, DrillName, "Gold");

            all.Should().ContainSingle().Which.Minimum.Should().Be(100);
            gold.Should().ContainSingle().Which.Minimum.Should().Be(10);
            (await dbContext.CaseKeyJournalPeer.CountAsync(p => p.CaseWorkflowGuid == workflow)).Should().Be(2);
            (await repository.GetAsync(Guid.NewGuid(), DrillName, null!)).Should().BeEmpty();
            (await repository.GetAsync(workflow, "OtherKey", null!)).Should().BeEmpty();
        }

        [Fact]
        public async Task OnlyTheRequestedSectionsAreComputedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key);

            var report = await AnalyseAsync(dbContext, workflow, key, r => r.Sections = "summary, Velocity");

            report.Meta.Sections.Should().Equal("summary", "velocity");
            report.Summary.Should().NotBeNull();
            report.Velocity.Should().NotBeNull();
            report.Notable.Should().BeNull();
            report.Gaps.Should().BeNull();
            report.Peers.Should().BeNull();
        }

        [Fact]
        public async Task FeaturesAndModelScoresAreAnalysedAsMeasuresAlongsideThePayloadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 12, i => i,
                facets: JObject.Parse("{\"abstraction\":{\"Volume\":12.5},\"exhaustiveAdaptation\":{\"StructNet\":0.4}}"));

            var report = await AnalyseAsync(dbContext, workflow, key);

            report.Fields.Select(field => field.Name).Should().Contain(["abstraction:Volume", "exhaustive:StructNet",
                "ResponseElevation"]);
            report.Meta.LowConfidence.Should().BeTrue();
        }

        [Fact]
        public async Task ACounterpartyRoleBuildsAProfileAndAFirstSeenValueIsNovelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Beneficiary", "$.payload.Beneficiary", true);
            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id))
                .Set(w => w.RoleId, (byte)3).UpdateAsync();
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 12, _ => 10, i => i == 11 ? "STRANGER" : "FRIEND");

            var report = await AnalyseAsync(dbContext, workflow, key);

            report.Counterparties!.Single().Top.Select(entry => entry.Value).Should().Contain(["FRIEND", "STRANGER"]);
            report.Fields.Should().Contain(field => field.Name == "Beneficiary" && field.Kind == "counterparty");
            report.Novelty!.Rows.Values.Should().ContainSingle().Which.Should().Contain("Beneficiary");
        }

        [Fact]
        public async Task APayloadFieldTheCallersRolesCannotSeeIsNeverAnalysedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", false);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 25, i => 100 + i);

            var report = await AnalyseAsync(dbContext, workflow, key);

            report.Fields.Select(field => field.Name).Should().NotContain("Amount");
            report.Distributions!.Select(d => d.Field).Should().NotContain("Amount");
        }

        [Fact]
        public async Task TheFieldsListNarrowsTheMeasuresAndTheLimitTruncatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            var key = NewKey();
            await SeedHistoryAsync(this, dbContext, model, key, 6, i => i);

            var report = await AnalyseAsync(dbContext, workflow, key, r =>
            {
                r.Fields = "Amount";
                r.Limit = 4;
            });

            report.Fields.Where(field => field.Kind == "measure").Select(field => field.Name).Should().Equal("Amount");
            report.Meta.RowCount.Should().Be(4);
            report.Meta.Truncated.Should().BeTrue();
        }

        [Fact]
        public async Task ABadSectionIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAnalyticsAsync(
                new CaseKeyJournalAnalyticsRequestDto
                {
                    CaseWorkflowGuid = workflow, Key = DrillName, Value = "v", Sections = "summary,rhubarb"
                }));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("SectionInvalid");
        }

        [Fact]
        public async Task ThePermissionTheWorkflowAndTheKeyAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (hidden, _) = await SetupAsync(dbContext, grant: false);
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetAnalyticsAsync(
                new CaseKeyJournalAnalyticsRequestDto { CaseWorkflowGuid = hidden, Key = DrillName, Value = "v" }));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.GetAnalyticsAsync(
                new CaseKeyJournalAnalyticsRequestDto { CaseWorkflowGuid = hidden, Key = DrillName, Value = "v" }));
            await Assert.ThrowsAsync<DtoValidationException>(() => allowed.GetAnalyticsAsync(
                new CaseKeyJournalAnalyticsRequestDto { CaseWorkflowGuid = hidden, Key = "", Value = "v" }));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.GetAnalyticsAsync(null));
        }

        [Fact]
        public async Task AKeyWithNoTransactionsGivesAnEmptyButWellFormedReportAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);

            var report = await AnalyseAsync(dbContext, workflow, NewKey());

            report.Meta.RowCount.Should().Be(0);
            report.Meta.LowConfidence.Should().BeTrue();
            report.Summary!.Count.Should().Be(0);
            report.Notable!.Rows.Should().BeEmpty();
            report.Gaps!.Longest.Should().BeNull();
        }
    }
}

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
using Jube.Data.Context;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
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
    public sealed class CaseKeyJournalServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
        private readonly List<int> createdCaseIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseKeyJournalPin.Where(w => createdCaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
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

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();

            await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value))
                .DeleteAsync();

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
            string xPath, bool grantRoleToUser, bool boldLine = false, bool regexFormat = false,
            bool drill = false)
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
                Drill = (byte)(drill ? 1 : 0),
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

        private async Task<long> CreateArchiveAsync(DbContext dbContext, int modelId, string keyValue,
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

            return id;
        }

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid workflow, string? json = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = DrillName,
                Json = json,
            });
            createdCaseIds.Add(id);

            return id;
        }

        private async Task ReplaceWorkflowRowWithAnotherModelAsync(DbContext dbContext, Guid workflow, int model)
        {
            var original = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow);

            await dbContext.CaseWorkflow.Where(w => w.Id == original.Id).Set(w => w.Deleted, (byte)1).UpdateAsync();

            original.Id = 0;
            original.EntityAnalysisModelId = model;
            original.Deleted = 0;
            createdWorkflowIds.Add(await dbContext.InsertWithInt32IdentityAsync(original));
        }

        private static string NewKey() => $"{DatabaseFixture.Prefix}Acct{Guid.NewGuid():N}";

        private string Me => fx.Seed.UserWithPermission;

        private async Task<(Guid Workflow, int Model)> SetupAsync(DbContext dbContext, bool grant = true)
        {
            var model = await CreateModelAsync(dbContext, Me);
            var workflow = await CreateWorkflowAsync(dbContext, model, Me, grant);

            return (workflow, model);
        }

        private static CaseKeyJournalRequestDto Request(Guid workflow, string key, string? lens = null,
            Action<CaseKeyJournalRequestDto>? change = null)
        {
            var request = new CaseKeyJournalRequestDto
            {
                CaseWorkflowGuid = workflow, Key = DrillName, Value = key, Lens = lens
            };

            change?.Invoke(request);

            return request;
        }

        private static JObject Facets() => JObject.Parse("""
            {
              "activation": { "Velocity": { "forced": false, "visible": true }, "Hidden": { "forced": false, "visible": false } },
              "abstraction": { "Volume": 123.45 },
              "ttlCounter": { "All": 3 },
              "exhaustiveAdaptation": { "StructNet": 0.91 },
              "sanction": { "Fuzzy": 0.82 }
            }
            """);

        [Fact]
        public async Task ReturnsOnlyTheKeysTransactionsOfTheWorkflowsModelNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var otherModel = await CreateModelAsync(dbContext, Me);
            var key = NewKey();
            var first = await CreateArchiveAsync(dbContext, model, key);
            var second = await CreateArchiveAsync(dbContext, model, key);
            await CreateArchiveAsync(dbContext, otherModel, key);
            await CreateArchiveAsync(dbContext, model, NewKey());

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key));

            journal.Rows.Select(row => row["Id"]).Should().Equal(second, first);
            journal.Meta.RowCount.Should().Be(2);
            journal.Meta.Truncated.Should().BeFalse();
            journal.Meta.Lens.Should().Be("payload");
        }

        [Fact]
        public async Task AWorkflowRowThatWasReplacedWithAnotherModelReadsTheLiveRowsTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, oldModel) = await SetupAsync(dbContext);
            var newModel = await CreateModelAsync(dbContext, Me);
            await ReplaceWorkflowRowWithAnotherModelAsync(dbContext, workflow, newModel);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, oldModel, key);
            var live = await CreateArchiveAsync(dbContext, newModel, key);
            var service = await BuildServiceAsync(dbContext, Me);

            var journal = await service.GetAsync(Request(workflow, key));

            journal.Rows.Select(row => (long)row["Id"]!).Should().Equal(live);
            (await service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = live })).Id
                .Should().Be(live);
        }

        [Fact]
        public async Task MergedKeysShareOneTimelineAndEachRowSaysWhichLanesItBelongsToAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            var device = $"{DatabaseFixture.Prefix}Dev{Guid.NewGuid():N}";
            var both = await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["DeviceId"] = device });
            var accountOnly = await CreateArchiveAsync(dbContext, model, key);
            var deviceOnly = await CreateArchiveAsync(dbContext, model, NewKey(),
                payload: new JObject { ["DeviceId"] = device });
            var service = await BuildServiceAsync(dbContext, Me);

            var journal = await service.GetAsync(Request(workflow, key, change: request =>
            {
                request.Merge = $"[{{\"key\":\"DeviceId\",\"value\":\"{device}\"}}]";
            }));

            journal.Rows.Select(row => (long)row["Id"]!).Should().Equal(deviceOnly, accountOnly, both);
            journal.Rows.Select(row => (string)row["Lane"]!).Should().Equal("B", "A", "AB");

            var plain = await service.GetAsync(Request(workflow, key));

            plain.Rows.Should().HaveCount(2).And.OnlyContain(row => !row.ContainsKey("Lane"));
        }

        [Fact]
        public async Task MergeNeedsMatchingKeysAndValuesWithAtMostThreeExtraAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            async Task<string[]> CodesAsync(string merge)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(
                    Request(workflow, NewKey(), change: request => request.Merge = merge)));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            (await CodesAsync("{not json")).Should().Contain("MergeInvalid");
            (await CodesAsync("{\"key\":\"A\",\"value\":\"1\"}")).Should().Contain("MergeInvalid");
            (await CodesAsync("[\"A\"]")).Should().Contain("MergeInvalid");
            (await CodesAsync("[{\"key\":\"A\"}]")).Should().Contain("MergeInvalid");
            (await CodesAsync("[{\"key\":\"A\",\"value\":1}]")).Should().Contain("MergeInvalid");
            (await CodesAsync(
                    "[{\"key\":\"A\",\"value\":\"1\"},{\"key\":\"B\",\"value\":\"2\"},{\"key\":\"C\",\"value\":\"3\"},{\"key\":\"D\",\"value\":\"4\"}]"))
                .Should().Contain("MergeInvalid");
            (await CodesAsync("[{\"key\":\" \",\"value\":\"1\"}]")).Should().Contain("MergeInvalid");
            (await CodesAsync("[{\"key\":\"A\",\"value\":\"\"}]")).Should().Contain("MergeInvalid");
        }

        private Task CreateRequestFieldAsync(DbContext dbContext, int model, string name, int dataTypeId)
        {
            return dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model,
                Name = name,
                DataTypeId = dataTypeId,
                XPath = $"$.{name}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });
        }

        [Fact]
        public async Task TheJournalSaysWhichColumnsLocateATransactionOnlyWhenTheCallerCanSeeThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateRequestFieldAsync(dbContext, model, "Lat", 6);
            await CreateRequestFieldAsync(dbContext, model, "Lon", 7);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key);
            var service = await BuildServiceAsync(dbContext, Me);

            (await service.GetAsync(Request(workflow, key))).Meta.Location.Should().BeNull();

            await CreateXPathAsync(dbContext, workflow, Me, "Latitude", "$.payload.Lat", true);

            (await service.GetAsync(Request(workflow, key))).Meta.Location.Should().BeNull();

            await CreateXPathAsync(dbContext, workflow, Me, "Longitude", "$.payload['Lon']", false);

            (await service.GetAsync(Request(workflow, key))).Meta.Location.Should().BeNull();

            var (shown, shownModel) = await SetupAsync(dbContext);
            await CreateRequestFieldAsync(dbContext, shownModel, "Lat", 6);
            await CreateRequestFieldAsync(dbContext, shownModel, "Lon", 7);
            await CreateXPathAsync(dbContext, shown, Me, "Latitude", "$.payload.Lat", true);
            await CreateXPathAsync(dbContext, shown, Me, "Longitude", "$.payload['Lon']", true);
            var another = NewKey();
            await CreateArchiveAsync(dbContext, shownModel, another);

            var location = (await service.GetAsync(Request(shown, another))).Meta.Location;

            location.Should().NotBeNull();
            location!.Latitude.Should().Be("Latitude");
            location.Longitude.Should().Be("Longitude");
        }

        private static Task<CaseKeyJournalTrackService> BuildTrackAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalTrackService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private static CaseKeyJournalTrackRequestDto TrackRequest(Guid workflow, string key,
            Action<CaseKeyJournalTrackRequestDto>? change = null)
        {
            var request = new CaseKeyJournalTrackRequestDto
            {
                CaseWorkflowGuid = workflow, Key = DrillName, Value = key
            };

            change?.Invoke(request);

            return request;
        }

        private async Task<(Guid Workflow, int Model)> SetupLocatedAsync(DbContext dbContext)
        {
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateRequestFieldAsync(dbContext, model, "Lat", 6);
            await CreateRequestFieldAsync(dbContext, model, "Lon", 7);
            await CreateXPathAsync(dbContext, workflow, Me, "Latitude", "$.payload.Lat", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Longitude", "$.payload.Lon", true);

            return (workflow, model);
        }

        private Task<long> CreateLocatedAsync(DbContext dbContext, int model, string key, double? latitude,
            double? longitude, DateTime at, JObject? extra = null)
        {
            var payload = extra ?? new JObject();

            if (latitude is not null)
            {
                payload["Lat"] = latitude;
                payload["Lon"] = longitude;
            }

            return CreateArchiveAsync(dbContext, model, key, payload: payload, created: at);
        }

        [Fact]
        public async Task TheTrackPutsTransactionsInTimeOrderAndMarksTheMovementThatCouldNotHappenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupLocatedAsync(dbContext);
            var key = NewKey();
            var start = DateTime.UtcNow.AddDays(-2);
            var paris = await CreateLocatedAsync(dbContext, model, key, 48.8566, 2.3522, start);
            var sydney = await CreateLocatedAsync(dbContext, model, key, -33.8688, 151.2093, start.AddHours(2));
            var nowhere = await CreateLocatedAsync(dbContext, model, key, null, null, start.AddHours(3));
            var london = await CreateLocatedAsync(dbContext, model, key, 51.5074, -0.1278, start.AddHours(30));
            var service = await BuildTrackAsync(dbContext, Me);

            var track = await service.GetAsync(TrackRequest(workflow, key));

            track.Points.Select(point => (long)point["Id"]!).Should().Equal(paris, sydney, nowhere, london);
            track.Points[2]["Latitude"].Should().BeNull();
            track.Points[0]["Latitude"].Should().Be(48.8566);
            track.Meta.Location.Should().NotBeNull();
            track.Meta.Total.Should().Be(4);
            track.Meta.Matched.Should().Be(4);
            track.Meta.Located.Should().Be(3);
            track.Meta.Unlocated.Should().Be(1);
            track.Meta.Truncated.Should().BeFalse();
            track.Summary.Legs.Should().Be(2);
            track.Summary.Impossible.Should().Be(1);

            var worst = track.Summary.Worst.Single();

            worst.Kilometres.Should().BeInRange(16900, 17000);
            worst.Hours.Should().BeApproximately(2, 0.01);
            worst.SpeedKmh.Should().BeInRange(8400, 8500);
            worst.Impossible.Should().BeTrue();
        }

        [Fact]
        public async Task TheTrackUsesTheSpeedAndDistanceLimitsItIsGivenAndRefusesSillyOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupLocatedAsync(dbContext);
            var key = NewKey();
            var start = DateTime.UtcNow.AddDays(-2);
            await CreateLocatedAsync(dbContext, model, key, 51.5074, -0.1278, start);
            await CreateLocatedAsync(dbContext, model, key, 48.8566, 2.3522, start.AddHours(1));
            var service = await BuildTrackAsync(dbContext, Me);

            var normal = await service.GetAsync(TrackRequest(workflow, key));

            normal.Summary.Impossible.Should().Be(0);
            normal.Summary.LimitKmh.Should().Be(900);
            normal.Summary.MinimumKilometres.Should().Be(50);

            var strict = await service.GetAsync(TrackRequest(workflow, key, request => request.LimitKmh = 300));

            strict.Summary.Impossible.Should().Be(1);
            strict.Summary.LimitKmh.Should().Be(300);

            var far = await service.GetAsync(TrackRequest(workflow, key, request =>
            {
                request.LimitKmh = 300;
                request.MinimumKilometres = 400;
            }));

            far.Summary.Impossible.Should().Be(0);

            foreach (var bad in new[] { (0d, (double?)null), (20000d, null), (300d, -5d), (300d, 20001d) })
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(
                    TrackRequest(workflow, key, request =>
                    {
                        request.LimitKmh = bad.Item1;
                        request.MinimumKilometres = bad.Item2;
                    })));

                ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("TravelLimitInvalid");
            }
        }

        [Fact]
        public async Task TheTrackAppliesTheFilterAndCanReturnOnlyTheSummaryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupLocatedAsync(dbContext);
            var key = NewKey();
            var start = DateTime.UtcNow.AddDays(-2);
            var south = await CreateLocatedAsync(dbContext, model, key, 40.0, 3.0, start);
            var north = await CreateLocatedAsync(dbContext, model, key, 55.0, 3.0, start.AddHours(1));
            var service = await BuildTrackAsync(dbContext, Me);

            var narrowed = await service.GetAsync(TrackRequest(workflow, key, request =>
                request.Filter = """{"condition":"AND","rules":[{"id":"Payload.Lat","operator":"greater","value":"50"}]}"""));

            narrowed.Points.Select(point => (long)point["Id"]!).Should().Equal(north);
            narrowed.Meta.Total.Should().Be(2);
            narrowed.Meta.Matched.Should().Be(1);
            narrowed.Summary.Legs.Should().Be(0);

            var summary = await service.GetAsync(TrackRequest(workflow, key, request => request.SummaryOnly = true));

            summary.Points.Should().BeEmpty();
            summary.Meta.Matched.Should().Be(2);
            summary.Summary.Legs.Should().Be(1);
            south.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task TheTrackCanReturnEveryTransactionWithTheFilteredOnesPickedOutAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupLocatedAsync(dbContext);
            var key = NewKey();
            var start = DateTime.UtcNow.AddDays(-2);
            var south = await CreateLocatedAsync(dbContext, model, key, 40.0, 3.0, start);
            var north = await CreateLocatedAsync(dbContext, model, key, 55.0, 3.0, start.AddHours(1));
            var farNorth = await CreateLocatedAsync(dbContext, model, key, 60.0, 3.0, start.AddHours(5));
            var service = await BuildTrackAsync(dbContext, Me);

            var all = await service.GetAsync(TrackRequest(workflow, key, request =>
            {
                request.Filter = """{"condition":"AND","rules":[{"id":"Payload.Lat","operator":"greater","value":"50"}]}""";
                request.IncludeUnmatched = true;
            }));

            all.Points.Select(point => (long)point["Id"]!).Should().Equal(south, north, farNorth);
            all.Points.Select(point => (bool)point["Matched"]!).Should().Equal(false, true, true);
            all.Meta.Total.Should().Be(3);
            all.Meta.Matched.Should().Be(2);
            all.Meta.Located.Should().Be(2);
            all.Summary.Legs.Should().Be(1);

            var narrowed = await service.GetAsync(TrackRequest(workflow, key, request =>
            {
                request.Filter = """{"condition":"AND","rules":[{"id":"Payload.Lat","operator":"greater","value":"50"}]}""";
            }));

            narrowed.Points.Should().HaveCount(2);
            narrowed.Points.Should().OnlyContain(point => !point.ContainsKey("Matched"));

            var unfiltered = await service.GetAsync(TrackRequest(workflow, key, request => request.IncludeUnmatched = true));

            unfiltered.Points.Should().HaveCount(3);
            unfiltered.Points.Should().OnlyContain(point => !point.ContainsKey("Matched"));
        }

        [Fact]
        public async Task EachMergedKeyIsItsOwnTrailAndMovementIsOnlyCountedWithinATrailAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupLocatedAsync(dbContext);
            var key = NewKey();
            var device = $"{DatabaseFixture.Prefix}Dev{Guid.NewGuid():N}"[..30];
            var start = DateTime.UtcNow.AddDays(-2);
            await CreateLocatedAsync(dbContext, model, key, 51.5074, -0.1278, start);
            await CreateLocatedAsync(dbContext, model, "other-account", 40.7128, -74.006, start.AddMinutes(30),
                new JObject { ["DeviceId"] = device });
            var service = await BuildTrackAsync(dbContext, Me);

            var alone = await service.GetAsync(TrackRequest(workflow, key));

            alone.Points.Should().HaveCount(1);
            alone.Summary.Legs.Should().Be(0);

            var merged = await service.GetAsync(TrackRequest(workflow, key, request =>
                request.Merge = $"[{{\"key\":\"DeviceId\",\"value\":\"{device}\"}}]"));

            merged.Points.Should().HaveCount(2);
            merged.Points.Select(point => (string)point["Lane"]!).Should().BeEquivalentTo("A", "B");
            merged.Summary.Legs.Should().Be(0);
            merged.Summary.Impossible.Should().Be(0);
        }

        [Fact]
        public async Task TheTrackHasNothingToDrawWhenTheCallerCannotSeeTheLocationColumnsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateLocatedAsync(dbContext, model, key, 51.5, -0.1, DateTime.UtcNow.AddDays(-1));
            var service = await BuildTrackAsync(dbContext, Me);

            var track = await service.GetAsync(TrackRequest(workflow, key));

            track.Meta.Location.Should().BeNull();
            track.Points.Should().BeEmpty();
            track.Meta.Matched.Should().Be(0);
        }

        [Fact]
        public async Task TheTrackRefusesAViewThatIsNotValidAndAWorkflowTheCallerCannotSeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupLocatedAsync(dbContext);
            var (hidden, _) = await SetupAsync(dbContext, grant: false);
            var service = await BuildTrackAsync(dbContext, Me);

            var broken = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.GetAsync(TrackRequest(workflow, "x", request => request.Filter = "not json")));

            broken.Result.Errors.Select(e => e.ErrorCode).Should().Contain("FilterInvalid");

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.GetAsync(TrackRequest(workflow, "", request => request.Value = "")));

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(TrackRequest(hidden, "x")));
        }

        [Fact]
        public async Task TheBoldLineMarksRowsSharingTheCasesValueInEveryLensAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Device", "$.payload.Device", true, boldLine: true);
            var key = NewKey();
            var same = await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Device"] = "D1" });
            var other = await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Device"] = "D2" });
            var caseId = await CreateCaseAsync(dbContext, workflow, """{ "payload": { "Device": "D1" } }""");
            var elsewhere = await CreateCaseAsync(dbContext, workflow, """{ "payload": { "Device": "D2" } }""");
            var service = await BuildServiceAsync(dbContext, Me);

            foreach (var lens in new[] { "payload", "rules" })
            {
                var journal = await service.GetAsync(Request(workflow, key, lens,
                    request => request.CaseId = caseId));

                var marked = journal.Rows.Where(row => row.ContainsKey("BoldLine")).Select(row => (long)row["Id"]!);

                marked.Should().Equal(same);

                var line = (Dictionary<string, object?>)journal.Rows.Single(row => (long)row["Id"]! == same)["BoldLine"]!;

                line["BoldLineKey"].Should().Be("Device");
                line["BoldLineValue"].Should().Be("D1");
                line["BoldLineFormatBackColor"].Should().Be("#111111");
            }

            var swapped = await service.GetAsync(Request(workflow, key, change: request => request.CaseId = elsewhere));

            swapped.Rows.Where(row => row.ContainsKey("BoldLine")).Select(row => (long)row["Id"]!).Should().Equal(other);

            (await service.GetAsync(Request(workflow, key))).Rows.Should().OnlyContain(row => !row.ContainsKey("BoldLine"));
        }

        private static string ArchiveFilter(string field, string op, string value)
        {
            return new JObject
            {
                ["condition"] = "AND",
                ["rules"] = new JArray(new JObject { ["id"] = field, ["operator"] = op, ["value"] = value })
            }.ToString(Newtonsoft.Json.Formatting.None);
        }

        [Fact]
        public async Task TheFilterIsAppliedInTheArchiveBeforeTheLimitAndTheUnfilteredTotalIsReportedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateRequestFieldAsync(dbContext, model, "Channel", 1);
            var key = NewKey();
            var start = DateTime.UtcNow.AddDays(-2);
            var cards = new List<long>();

            for (var i = 0; i < 3; i++)
            {
                cards.Add(await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Channel"] = "Card" },
                    created: start.AddMinutes(i)));
            }

            for (var i = 0; i < 6; i++)
            {
                await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Channel"] = "Web" },
                    created: start.AddHours(1).AddMinutes(i));
            }

            var service = await BuildServiceAsync(dbContext, Me);

            var plain = await service.GetAsync(Request(workflow, key));

            plain.Meta.Total.Should().Be(9);
            plain.Rows.Should().HaveCount(9);

            var narrowed = await service.GetAsync(Request(workflow, key, change: request =>
            {
                request.Filter = ArchiveFilter("Payload.Channel", "equal", "Card");
                request.Limit = 3;
            }));

            narrowed.Rows.Select(row => (long)row["Id"]!).Should().BeEquivalentTo(cards);
            narrowed.Meta.Truncated.Should().BeFalse();
            narrowed.Meta.RowCount.Should().Be(3);
            narrowed.Meta.Total.Should().Be(9);

            var codes = (await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(Request(workflow, key,
                change: request => request.Filter = ArchiveFilter("Payload.Missing", "equal", "x")))))
                .Result.Errors.Select(e => e.ErrorCode);

            codes.Should().Contain("FilterInvalid");
        }

        [Fact]
        public async Task APinnedTransactionIsHeldToTheFilterToo()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateRequestFieldAsync(dbContext, model, "Channel", 1);
            var key = NewKey();
            var card = await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Channel"] = "Card" });
            var web = await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Channel"] = "Web" });
            var caseId = await CreateCaseAsync(dbContext, workflow);
            var guids = await dbContext.Archive.Where(a => a.Id == card || a.Id == web)
                .Select(a => a.EntityAnalysisModelInstanceEntryGuid).ToListAsync();
            await new CaseKeyJournalPinRepository(dbContext, Me).SetAsync(caseId, guids, true);
            var service = await BuildServiceAsync(dbContext, Me);

            var journal = await service.GetAsync(Request(workflow, key, change: request =>
            {
                request.CaseId = caseId;
                request.Filter = ArchiveFilter("Payload.Channel", "equal", "Card");
            }));

            journal.Rows.Select(row => (long)row["Id"]!).Should().Equal(card);
        }

        [Fact]
        public async Task TheJournalOffersThePayloadFieldsSetAsDrillFieldsAsKeysAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Device", "$.payload.DeviceId", true, drill: true);
            await CreateXPathAsync(dbContext, workflow, Me, "Account", "$.payload['AccountRef']", true, drill: true);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Nested", "$.payload.a.b", true, drill: true);
            await CreateXPathAsync(dbContext, workflow, Me, "Hidden", "$.payload.Hidden", false, drill: true);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key);
            var service = await BuildServiceAsync(dbContext, Me);

            var journal = await service.GetAsync(Request(workflow, key, lens: "rules"));

            journal.Meta.DrillKeys.Select(drillKey => (drillKey.Key, drillKey.Title)).Should()
                .Equal(("AccountRef", "Account"), ("DeviceId", "Device"));
        }

        [Fact]
        public async Task PinnedTransactionsComeFirstEvenOutsideTheWindowAndAreMarkedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var (otherWorkflow, _) = await SetupAsync(dbContext);
            var key = NewKey();
            var old = await CreateArchiveAsync(dbContext, model, NewKey(), created: DateTime.UtcNow.AddDays(-400));
            var recent = await CreateArchiveAsync(dbContext, model, key);
            var newest = await CreateArchiveAsync(dbContext, model, key);
            var caseId = await CreateCaseAsync(dbContext, workflow);
            var elsewhere = await CreateCaseAsync(dbContext, otherWorkflow);
            var oldGuid = await dbContext.Archive.Where(a => a.Id == old).Select(a => a.EntityAnalysisModelInstanceEntryGuid)
                .FirstAsync();
            var pins = new CaseKeyJournalPinRepository(dbContext, Me);
            await pins.SetAsync(caseId, [oldGuid], true);
            await pins.SetAsync(elsewhere, [oldGuid], true);
            var service = await BuildServiceAsync(dbContext, Me);

            var journal = await service.GetAsync(Request(workflow, key, change: request => request.CaseId = caseId));

            journal.Rows.Select(row => (long)row["Id"]!).Should().Equal(old, newest, recent);
            journal.Rows.Select(row => (bool)row["Pinned"]!).Should().Equal(true, false, false);

            var unrelated = await service.GetAsync(Request(workflow, key, change: request => request.CaseId = elsewhere));

            var only = await service.GetAsync(Request(workflow, key, change: request =>
            {
                request.CaseId = caseId;
                request.PinnedOnly = true;
            }));

            only.Rows.Select(row => (long)row["Id"]!).Should().Equal(old);
            only.Rows.Single()["Pinned"].Should().Be(true);

            var noCase = await service.GetAsync(Request(workflow, key, change: request => request.PinnedOnly = true));

            noCase.Rows.Should().HaveCount(2);

            unrelated.Rows.Should().OnlyContain(row => !(bool)row["Pinned"]!);
            (await service.GetAsync(Request(workflow, key))).Rows.Should().OnlyContain(row => !(bool)row["Pinned"]!);
        }

        [Fact]
        public async Task PayloadColumnsAreOnlyTheXPathsTheCallersRolesMaySeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Secret", "$.payload.Secret", false);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Amount"] = 90.5, ["Secret"] = "x" });

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key));

            journal.Columns.Select(column => column.Name).Should().Contain("Amount").And.NotContain("Secret");
            journal.Rows.Single()["Amount"].Should().Be(90.5);
            journal.Rows.Single().Should().NotContainKey("Secret");
            journal.Columns.Single(column => column.Name == "Amount").RoleId.Should().Be(1);
            journal.Columns.Single(column => column.Name == "Amount").RoleInferred.Should().BeTrue();
        }

        [Fact]
        public async Task AConfiguredRoleIsReturnedAsConfiguredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Code", "$.payload.Code", true);
            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id))
                .Set(w => w.RoleId, (byte)3).UpdateAsync();
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, payload: new JObject { ["Code"] = "P1" });

            var service = await BuildServiceAsync(dbContext, Me);
            var column = (await service.GetAsync(Request(workflow, key))).Columns.Single(c => c.Name == "Code");

            column.RoleId.Should().Be(3);
            column.RoleInferred.Should().BeFalse();
        }

        [Theory]
        [InlineData("rules", "rule:Velocity")]
        [InlineData("features", "abstraction:Volume")]
        [InlineData("models", "exhaustive:StructNet")]
        [InlineData("sanctions", "sanction:Fuzzy")]
        public async Task EachLensReturnsItsOwnColumnsAndTheBaseColumnsAsync(string lens, string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, facets: Facets());

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key, lens));

            journal.Columns.Select(column => column.Name).Should().Contain([expected, "Id", "Activations", "Tag"]);
            journal.Rows.Single().Should().ContainKey(expected);
            journal.Meta.Lens.Should().Be(lens);
        }

        [Fact]
        public async Task HiddenActivationsAreNeverNamedAndTheLensCountsSayWhatHasContentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, facets: Facets());
            await CreateArchiveAsync(dbContext, model, key);

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key));

            journal.Rows.Select(row => row["Activations"]).Should().BeEquivalentTo(new object[] { "Velocity", string.Empty });
            journal.Rows.SelectMany(row => row.Keys).Should().NotContain("rule:Hidden");
            journal.Lenses.Single(l => l.Lens == "rules").Rows.Should().Be(1);
            journal.Lenses.Single(l => l.Lens == "features").Rows.Should().Be(1);
            journal.Lenses.Single(l => l.Lens == "payload").Rows.Should().Be(2);
        }

        [Fact]
        public async Task ANumericKeyIsFoundWhenTheValueIsAskedForAsText()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var number = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000);
            await CreateArchiveAsync(dbContext, model, number.ToString(), keyToken: number);

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, number.ToString()));

            journal.Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task TheLimitTruncatesTheNewestFirstAndSaysSoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            var ids = new List<long>();
            for (var i = 0; i < 4; i++) ids.Add(await CreateArchiveAsync(dbContext, model, key));

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key, null, r => r.Limit = 3));

            journal.Rows.Select(row => row["Id"]).Should().Equal(ids.AsEnumerable().Reverse().Take(3).Cast<object>().ToArray());
            journal.Meta.Truncated.Should().BeTrue();
            journal.Meta.Limit.Should().Be(3);
        }

        [Fact]
        public async Task TheWindowTheElevationAndActivationsOnlyAllNarrowTheRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, created: DateTime.UtcNow.AddDays(-200));
            var recentQuiet = await CreateArchiveAsync(dbContext, model, key, responseElevation: 1);
            var recentLoud = await CreateArchiveAsync(dbContext, model, key, responseElevation: 8, activationRuleCount: 2);

            var service = await BuildServiceAsync(dbContext, Me);

            (await service.GetAsync(Request(workflow, key))).Rows.Select(r => r["Id"])
                .Should().Equal(recentLoud, recentQuiet);
            (await service.GetAsync(Request(workflow, key, null, r => r.ResponseElevation = 5))).Rows
                .Select(r => r["Id"]).Should().Equal(recentLoud);
            (await service.GetAsync(Request(workflow, key, null, r => r.ActivationsOnly = true))).Rows
                .Select(r => r["Id"]).Should().Equal(recentLoud);
            (await service.GetAsync(Request(workflow, key, null, r => r.From = DateTimeOffset.UtcNow.AddDays(-300))))
                .Rows.Should().HaveCount(3);
        }

        [Fact]
        public async Task TagsComeBackAsAPlainListAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key, tags: ["mule", "review"]);

            var service = await BuildServiceAsync(dbContext, Me);
            var journal = await service.GetAsync(Request(workflow, key));

            journal.Rows.Single()["Tag"].Should().BeEquivalentTo(new List<string> { "mule", "review" });
        }

        [Theory]
        [InlineData(null, "v", "KeyRequired")]
        [InlineData("", "v", "KeyRequired")]
        [InlineData("a\u0001b", "v", "KeyInvalid")]
        [InlineData("k", "", "ValueRequired")]
        [InlineData("k", null, "ValueRequired")]
        public async Task ABadKeyOrValueIsRefusedAsync(string? key, string? value, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(
                new CaseKeyJournalRequestDto { CaseWorkflowGuid = workflow, Key = key, Value = value }));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain(code);
        }

        [Theory]
        [InlineData("everything", 100, "LensInvalid")]
        [InlineData("payload", 0, "LimitInvalid")]
        [InlineData("payload", 5001, "LimitInvalid")]
        public async Task ABadLensOrLimitIsRefusedAsync(string lens, int limit, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.GetAsync(Request(workflow, "v", lens, r => r.Limit = limit)));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain(code);
        }

        [Fact]
        public async Task AReversedOrOversizedWindowIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var reversed = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(
                Request(workflow, "v", null, r =>
                {
                    r.From = DateTimeOffset.UtcNow;
                    r.To = DateTimeOffset.UtcNow.AddDays(-1);
                })));
            var oversized = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(
                Request(workflow, "v", null, r => r.From = DateTimeOffset.UtcNow.AddDays(-4000))));

            reversed.Result.Errors.Select(e => e.ErrorCode).Should().Contain("PeriodInvalid");
            oversized.Result.Errors.Select(e => e.ErrorCode).Should().Contain("PeriodInvalid");
        }

        [Fact]
        public async Task ARoleWithoutThePermissionIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(Request(workflow, "v")));
        }

        [Fact]
        public async Task AWorkflowTheCallerHasNoRoleOnIsNotFoundAndAnUnknownOneTooAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (hidden, model) = await SetupAsync(dbContext, grant: false);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key);
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Request(hidden, key)));
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Request(Guid.NewGuid(), key)));
        }

        [Fact]
        public async Task TheQueryNeverReturnsAnotherTenantsTransactionsEvenWhenAskedForTheirModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model) = await SetupAsync(dbContext);
            var key = NewKey();
            await CreateArchiveAsync(dbContext, model, key);
            var query = new GetCaseKeyJournalRowsQuery(connectionString, TestLog.NoOp);

            var own = await query.ExecuteAsync(model, Me, DrillName, key, DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1), 0, 0, 10);
            var foreign = await query.ExecuteAsync(model, fx.Seed.UserTenantB, DrillName, key,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 0, 0, 10);

            own.Should().ContainSingle();
            foreign.Should().BeEmpty();
        }

        [Fact]
        public async Task ANullRequestIsRefusedAndAnUnauthenticatedCallerNeverGetsAServiceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetAsync(null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }
    }
}

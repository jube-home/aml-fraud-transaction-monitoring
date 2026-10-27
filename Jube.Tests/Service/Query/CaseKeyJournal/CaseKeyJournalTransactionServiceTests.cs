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
using Jube.Data.Query.GetCaseKeyJournalTransactionQuery;
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
    public sealed class CaseKeyJournalTransactionServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

        private static string NewKey() => $"{DatabaseFixture.Prefix}Acct{Guid.NewGuid():N}";

        private string Me => fx.Seed.UserWithPermission;

        private async Task<(Guid Workflow, int Model)> SetupAsync(DbContext dbContext, bool grant = true)
        {
            var model = await CreateModelAsync(dbContext, Me);
            var workflow = await CreateWorkflowAsync(dbContext, model, Me, grant);

            return (workflow, model);
        }

        private static JObject Facets() => JObject.Parse("""
            {
              "entityAnalysisModelName": "Retail",
              "activation": { "Velocity": { "forced": false, "visible": true }, "Hidden": { "forced": false, "visible": false } },
              "abstraction": { "Volume": 12.5 },
              "exhaustiveAdaptation": { "StructNet": 0.91 },
              "exhaustiveAdaptationDetail": {
                "StructNet": {
                  "value": 0.91,
                  "humanLabel": "Very likely",
                  "result": { "threshold": 0.8, "activated": true },
                  "contribution": { "space": "LogOdds", "exact": true, "baseValue": -3,
                    "items": [ { "name": "Amount", "weight": 2, "source": "Payload" },
                               { "name": "Secret", "weight": 1, "source": "Payload" } ] },
                  "journey": { "path": [ { "feature": "Secret", "operator": ">", "threshold": 7, "source": "Payload" },
                                         { "feature": "Amount", "operator": ">", "threshold": 100, "source": "Payload" } ] }
                }
              }
            }
            """);

        private async Task<(Guid Workflow, int Model, long Id)> SeedAsync(DbContext dbContext)
        {
            var (workflow, model) = await SetupAsync(dbContext);
            await CreateXPathAsync(dbContext, workflow, Me, "Amount", "$.payload.Amount", true);
            await CreateXPathAsync(dbContext, workflow, Me, "Secret", "$.payload.Secret", false);
            await CreateXPathAsync(dbContext, workflow, Me, "Device", "$.payload.Device", true);
            await dbContext.CaseWorkflowXPath.Where(w => w.Name == "Device" && createdXPathIds.Contains(w.Id))
                .Set(w => w.Drill, (byte)1).UpdateAsync();
            var id = await CreateArchiveAsync(dbContext, model, NewKey(),
                payload: new JObject { ["Amount"] = 250, ["Secret"] = 9, ["Device"] = "D-1" }, facets: Facets(),
                tags: ["mule"]);

            return (workflow, model, id);
        }

        [Fact]
        public async Task OpensATransactionWithItsActivationsFeaturesKeysAndModelJourneyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, id) = await SeedAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var card = await service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id });

            card.Id.Should().Be(id);
            card.ModelName.Should().Be("Retail");
            card.Tags.Should().Equal("mule");
            card.Activations.Select(a => a.Name).Should().Equal("Velocity");
            card.Values.Should().Contain(value => value.Group == "abstraction" && value.Name == "Volume");
            card.Keys.Should().ContainSingle(key => key.Name == "Device" && key.Value == "D-1" && key.Drill);

            var adaptation = card.Adaptations.Single();

            adaptation.HumanLabel.Should().Be("Very likely");
            adaptation.Result!.Activated.Should().BeTrue();
            adaptation.Journey.Select(node => node.Hidden).Should().Equal(true, false);
            adaptation.Contribution!.HiddenItems.Should().Be(1);
            adaptation.Contribution.Items.Single(item => !item.Hidden).Name.Should().Be("Amount");
        }

        [Fact]
        public async Task TheJourneyNeverNamesAPayloadInputTheCallersRolesCannotSeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, id) = await SeedAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var card = await service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id });
            var text = Newtonsoft.Json.JsonConvert.SerializeObject(card);

            text.Should().NotContain("Secret");
            text.Should().NotContain("\"threshold\":7");
        }

        [Fact]
        public async Task ThePayloadIsOnlyTheFieldsTheCallersRolesMaySeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, id) = await SeedAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var payload = await service.GetTransactionPayloadAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id });

            payload.Fields.Keys.Should().BeEquivalentTo("Amount", "Device");
            payload.Fields["Amount"].Should().Be(250L);
        }

        [Fact]
        public async Task ATransactionOfAnotherModelIsNotFoundEvenWithItsRealIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var otherModel = await CreateModelAsync(dbContext, Me);
            var foreign = await CreateArchiveAsync(dbContext, otherModel, NewKey());
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = foreign }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTransactionPayloadAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = foreign }));
        }

        [Fact]
        public async Task AnUnknownTransactionAndABadIdAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = long.MaxValue - 1 }));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = 0 }));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("TransactionIdInvalid");
        }

        [Fact]
        public async Task ThePermissionAndTheWorkflowRolesAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (hidden, model) = await SetupAsync(dbContext, grant: false);
            var id = await CreateArchiveAsync(dbContext, model, NewKey());
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);
            var request = new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = hidden, Id = id };

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetTransactionAsync(request));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetTransactionPayloadAsync(request));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.GetTransactionAsync(request));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.GetTransactionAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.GetTransactionPayloadAsync(null));
        }

        [Fact]
        public async Task TheQueryNeverReturnsAnotherTenantsTransactionEvenForTheirModelAndIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, id) = await SeedAsync(dbContext);
            var query = new GetCaseKeyJournalTransactionQuery(connectionString, TestLog.NoOp);

            (await query.ExecuteAsync(id, model, Me)).Should().NotBeNull();
            (await query.ExecuteAsync(id, model, fx.Seed.UserTenantB)).Should().BeNull();
            (await query.ExecuteAsync(id, model + 1_000_000, Me)).Should().BeNull();
        }

        [Fact]
        public async Task AnotherTenantsUserNeverGetsATransactionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, id) = await SeedAsync(dbContext);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var ex = await Record.ExceptionAsync(() => outsider.GetTransactionAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id }));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
        }

        [Fact]
        public async Task AroundListsTheSameDaysTransactionsThatShareAKeyAndNothingElseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, id) = await SeedAsync(dbContext);
            await dbContext.CaseWorkflowXPath.Where(w => w.Name == "Secret" && createdXPathIds.Contains(w.Id))
                .Set(w => w.Drill, (byte)1).UpdateAsync();
            var now = DateTime.UtcNow;
            var sameA = await CreateArchiveAsync(dbContext, model, NewKey(),
                payload: new JObject { ["Device"] = "D-1", ["Secret"] = 9 }, created: now.AddMinutes(-1));
            var sameB = await CreateArchiveAsync(dbContext, model, NewKey(),
                payload: new JObject { ["Device"] = "D-1" }, facets: Facets(), created: now.AddMinutes(-2));
            await CreateArchiveAsync(dbContext, model, NewKey(), payload: new JObject { ["Device"] = "D-2" },
                created: now);
            await CreateArchiveAsync(dbContext, model, NewKey(), payload: new JObject { ["Device"] = "D-1" },
                created: now.AddDays(-3));
            await CreateArchiveAsync(dbContext, model, NewKey(), payload: new JObject { ["Device"] = "D-1" },
                created: now.Date.AddDays(1).AddHours(1));
            await CreateXPathAsync(dbContext, workflow, Me, "Branch", "$.payload.Branch", true);
            await dbContext.CaseWorkflowXPath.Where(w => w.Name == "Branch" && createdXPathIds.Contains(w.Id))
                .Set(w => w.RoleId, (byte)4).UpdateAsync();
            await CreateArchiveAsync(dbContext, model, NewKey(), payload: new JObject { ["Branch"] = "B-1" });
            await LinqToDB.Data.DataConnectionExtensions.ExecuteAsync(dbContext,
                "update \"Archive\" set \"Json\" = jsonb_set(\"Json\", '{payload,Branch}', '\"B-1\"') where \"Id\" = @id",
                new LinqToDB.Data.DataParameter("id", id));
            var service = await BuildServiceAsync(dbContext, Me);

            var around = await service.GetAroundAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id });

            var group = around.Groups.Should().ContainSingle().Subject;

            group.Name.Should().Be("Device");
            group.Key.Should().Be("Device");
            group.Value.Should().Be("D-1");
            group.Truncated.Should().BeFalse();
            group.Rows.Select(row => row.Id).Should().Equal(sameB, sameA);
            group.Rows.Single(row => row.Id == sameB).Activations.Should().Be("Velocity");
            group.Rows.Single(row => row.Id == sameB).ActivationCount.Should().Be(1);
            around.Groups.Should().NotContain(g => g.Name == "Secret" || g.Name == "Branch");
            (around.To - around.From).Should().Be(TimeSpan.FromDays(1));
        }

        [Fact]
        public async Task AroundCapsEachGroupAndSaysSoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, id) = await SeedAsync(dbContext);

            for (var i = 0; i < 27; i++)
            {
                await CreateArchiveAsync(dbContext, model, NewKey(), payload: new JObject { ["Device"] = "D-1" });
            }

            var service = await BuildServiceAsync(dbContext, Me);
            var around = await service.GetAroundAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id });

            around.Groups.Single().Rows.Should().HaveCount(25);
            around.Groups.Single().Truncated.Should().BeTrue();
        }

        [Fact]
        public async Task AroundIsRefusedWithoutThePermissionOrAnotherTenantsTransactionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, id) = await SeedAsync(dbContext);
            var request = new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id };
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var allowed = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetAroundAsync(request));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.GetAroundAsync(null));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.GetAroundAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = id + 1_000_000 }));
            await Assert.ThrowsAsync<DtoValidationException>(() => allowed.GetAroundAsync(
                new CaseKeyJournalTransactionRequestDto { CaseWorkflowGuid = workflow, Id = 0 }));

            var ex = await Record.ExceptionAsync(() => outsider.GetAroundAsync(request));
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
        }
    }
}

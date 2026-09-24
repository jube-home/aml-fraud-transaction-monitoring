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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dictionary;
using Jube.Dto.Query.EntityAnalysisModelBacktest;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.Helpers;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelBacktest;
using Jube.Service.Query.EntityAnalysisModelBacktest;
using Jube.Service.Reactivity;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using BackgroundContext = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;

namespace Jube.Test.Service.Query.EntityAnalysisModelBacktest
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelBacktestServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Rule = "If (Payload.Amount > 100) Then\n   Return True\nEnd If";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly DateTime start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<Guid> entries = [];
        private int modelId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Bt{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            modelId = model.Id;

            foreach (var (name, dataTypeId) in new[] { ("Amount", 3), ("Country", 1) })
            {
                await dbContext.InsertAsync(new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = modelId, Name = name, DataTypeId = dataTypeId, XPath = $"$.{name}",
                    Active = 1, Cache = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            }

            foreach (var tag in new[] { "Fraud", "Reviewed" })
            {
                await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelTag
                {
                    EntityAnalysisModelId = modelId, Name = tag, Active = 1, Deleted = 0, Guid = Guid.NewGuid()
                });
            }

            var amounts = new (double Amount, string[] Tags)[]
            {
                (500, ["Fraud"]), (700, []), (50, ["Fraud", "Reviewed"]), (20, []), (900, ["Fraud"])
            };
            for (var i = 0; i < amounts.Length; i++)
            {
                await ArchiveAsync(dbContext, i, amounts[i].Amount, amounts[i].Tags);
            }
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelBacktestInstance.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTag>()
                .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.ArchiveTag.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();
            await dbContext.Archive.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        private async Task ArchiveAsync(DbContext dbContext, int index, double amount, string[] tags)
        {
            var guid = Guid.NewGuid();
            entries.Add(guid);
            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityInstanceEntryId = "TX" + index,
                ReferenceDate = start.AddHours(index)
            };
            payload.Payload.TryAdd("Amount", amount);
            payload.Payload.TryAdd("Country", "GB");

            await dbContext.InsertAsync(new Archive
            {
                Json = Encoding.UTF8.GetString(BuildJsonResponses.BuildFullJson(payload,
                    new JsonSerializationHelper().ArchiveJsonSerializer)),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntryKeyValue = "TX" + index,
                EntityAnalysisModelId = modelId,
                ReferenceDate = payload.ReferenceDate,
                CreatedDate = DateTime.UtcNow
            });

            foreach (var tag in tags)
            {
                await dbContext.InsertAsync(new ArchiveTag
                {
                    EntityAnalysisModelInstanceEntryGuid = guid, Name = tag, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = "test"
                });
            }
        }

        private Task<EntityAnalysisModelBacktestService> ServiceAsync(DbContext dbContext, string? userName = null)
        {
            return EntityAnalysisModelBacktestService.CreateAsync(dbContext, userName ?? fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus());
        }

        private Task<EntityAnalysisModelBacktestService> ReportServiceAsync(DbContext dbContext,
            string reportConnectionString)
        {
            return EntityAnalysisModelBacktestService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), reportConnectionString);
        }

        private static string ReportConnectionString(string? database)
        {
            return new Npgsql.NpgsqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("JubeTestConnectionString"))
            {
                Database = database ?? new Npgsql.NpgsqlConnectionStringBuilder(
                    Environment.GetEnvironmentVariable("JubeTestConnectionString")).Database,
                ApplicationName = "Jube Backtest Report"
            }.ConnectionString;
        }

        private BackgroundContext EngineContext(string? reportConnectionString)
        {
            return new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
                    {
                        ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")!,
                        ["BacktestPageSize"] = "2"
                    }),
                    TaskCoordinator = new TaskCoordinator(new CancellationTokenProvider()),
                    ReportConnectionString = reportConnectionString
                }
            };
        }

        private const string Fraud =
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Tag.Fraud\",\"operator\":\"equal\",\"value\":\"True\"}]}";

        private BacktestRequestDto Request(string? classJson = Fraud)
        {
            return new BacktestRequestDto
            {
                EntityAnalysisModelId = modelId, RuleType = "ActivationRule", RuleText = Rule, ClassJson = classJson
            };
        }

        [Fact]
        public async Task AnActivationRuleIsCountedAgainstATagClassOfArchivedTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var result = await service.RunRuleAsync(Request());

            result.Valid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
            (result.Scanned, result.Evaluated, result.LimitReached).Should().Be((5L, 5L, false));
            (result.Fired, result.NotFired, result.Positives).Should().Be((3L, 2L, 3L));
            (result.TruePositives, result.FalsePositives, result.FalseNegatives, result.TrueNegatives).Should()
                .Be((2L, 1L, 1L, 1L));
            result.TruePositiveSamples.Select(s => s.EntryKeyValue).Should().Equal("TX4", "TX0");
            result.FalseNegativeSamples.Should().ContainSingle().Which.EntryKeyValue.Should().Be("TX2");
            result.TagsInSample.Select(t => (t.Name, t.Count)).Should().Equal(("Fraud", 3L), ("Reviewed", 1L));
        }

        [Fact]
        public async Task ADeletedTagNoLongerCountsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ArchiveTag.Where(w => w.EntityAnalysisModelInstanceEntryGuid == entries[4])
                .Set(s => s.Deleted, (byte)1).UpdateAsync();
            var service = await ServiceAsync(dbContext);

            var result = await service.RunRuleAsync(Request());

            (result.TruePositives, result.FalsePositives).Should().Be((1L, 2L));
        }

        [Fact]
        public async Task TheFilterBuilderChoosesTheTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var request = Request();
            request.FilterJson =
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\",\"operator\":\"greater_or_equal\",\"value\":50}]}";

            var result = await service.RunRuleAsync(request);

            result.Valid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
            (result.Scanned, result.FilteredOut, result.Evaluated).Should().Be((5L, 1L, 4L));
            result.TrueNegatives.Should().Be(0);
        }

        [Fact]
        public async Task TheRangeAndLimitChooseTheNewestTransactionsInRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var request = Request();
            request.From = start.AddHours(1);
            request.To = start.AddHours(3);
            var ranged = await service.RunRuleAsync(request);
            var limited = Request();
            limited.Limit = 2;
            var newest = await service.RunRuleAsync(limited);

            ranged.Evaluated.Should().Be(3);
            (ranged.EarliestReferenceDate, ranged.LatestReferenceDate).Should()
                .Be((start.AddHours(1), start.AddHours(3)));
            newest.Evaluated.Should().Be(2);
            newest.LimitReached.Should().BeTrue();
            newest.EarliestReferenceDate.Should().Be(start.AddHours(3));
        }

        [Fact]
        public async Task WithoutAClassOnlyFiringIsCountedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var result = await service.RunRuleAsync(Request(null));

            result.Fired.Should().Be(3);
            result.ClassDefined.Should().BeFalse();
            (result.TruePositives + result.FalsePositives + result.FalseNegatives + result.TrueNegatives).Should()
                .Be(0);
        }

        [Fact]
        public async Task AReprocessingRuleRunsAsAGatewayRuleInReprocessingModeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var request = Request();
            request.RuleType = "ReprocessingRule";

            var result = await service.RunRuleAsync(request);

            result.Valid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
            result.Fired.Should().Be(3);
        }

        [Fact]
        public async Task AnInvalidRequestComesBackAsErrorsWithTheirPartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var badRule = Request();
            badRule.RuleText = "If (Payload.Nope > 1) Then\n   Return True\nEnd If";
            var badType = Request();
            badType.RuleType = "InlineFunction";
            var badRange = Request();
            (badRange.From, badRange.To) = (start.AddDays(1), start);
            var badClass = Request(
                "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Tag.Unknown\",\"operator\":\"equal\",\"value\":\"True\"}]}");
            var tooMany = Request();
            tooMany.Limit = 20_000;

            (await service.RunRuleAsync(badRule)).Errors.Should().Contain(e => e.ErrorCode == "RuleScriptInvalid");
            (await service.RunRuleAsync(badType)).Errors.Should().ContainSingle(e => e.ErrorCode == "RuleTypeInvalid");
            (await service.RunRuleAsync(badRange)).Errors.Should().ContainSingle(e => e.ErrorCode == "RangeInvalid");
            (await service.RunRuleAsync(badClass)).Errors.Should()
                .ContainSingle(e => e.ErrorCode == "FieldUnknown" && e.PropertyName == "Class.rules[0].id");
            (await service.RunRuleAsync(tooMany)).Errors.Should().ContainSingle(e => e.ErrorCode == "LimitTooLarge");
        }

        [Fact]
        public async Task TheFieldsIncludeTheModelsTagsForTheClassOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var fields = await service.FieldsAsync(modelId);

            fields.ClassFields.Should().Contain(f => f.Name == "Tag.Fraud" && f.DataType == "Boolean");
            fields.ClassFields.Should().Contain(f => f.Name == "Payload.Amount");
            fields.FilterFields.Should().Contain(f => f.Name == "Payload.Amount");
            fields.FilterFields.Should().NotContain(f => f.Name.StartsWith("Tag."));
        }

        [Fact]
        public async Task ASubmittedBacktestIsListedAndCanBeStoppedBeforeItStartsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var request = Request();
            request.RuleId = 42;

            var submitted = await service.SubmitAsync(request);
            var listed = await service.ListAsync(modelId, "ActivationRule", 42);
            var stopped = await service.StopAsync(submitted.Instance!.Id);
            var result = await service.ResultAsync(submitted.Instance.Id);

            submitted.Valid.Should().BeTrue();
            submitted.Instance.Status.Should().Be("Pending");
            listed.Should().ContainSingle(r => r.Id == submitted.Instance.Id && r.Request.ClassJson == Fraud);
            stopped.Status.Should().Be("Cancelled");
            result.Valid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorCode == "ResultUnavailable");
            (await service.SubmitAsync(Request("{\"rules\":[]}"))).Valid.Should().BeFalse();
        }

        [Fact]
        public async Task ABacktestThreadRunsASubmittedBacktestToTheSameCountsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var submitted = await service.SubmitAsync(Request());
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
                    {
                        ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")!,
                        ["BacktestPageSize"] = "2"
                    }),
                    TaskCoordinator = new TaskCoordinator(new CancellationTokenProvider())
                }
            };
            var starter = new BacktestTaskStarter(context, 1);

            for (var i = 0;
                 i < 10 && (await service.StatusAsync(submitted.Instance!.Id)).Status is "Pending" or "Running";
                 i++)
            {
                await starter.RunNextAsync(CancellationToken.None);
            }

            var run = await service.StatusAsync(submitted.Instance!.Id);
            var result = await service.ResultAsync(submitted.Instance.Id);
            run.Status.Should().Be("Succeeded", run.Error);
            (run.Scanned, run.Progress, run.TruePositives, run.FalsePositives).Should().Be((5L, 1d, 2L, 1L));
            result.Valid.Should().BeTrue();
            (result.FalseNegatives, result.TrueNegatives).Should().Be((1L, 1L));
        }

        [Fact]
        public async Task WithAReportConnectionTheArchiveIsReadThroughItAndTheCountsAreTheSameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var direct = await (await ServiceAsync(dbContext)).RunRuleAsync(Request());
            var reported = await (await ReportServiceAsync(dbContext, ReportConnectionString(null)))
                .RunRuleAsync(Request());

            reported.Valid.Should().BeTrue(string.Join("; ", reported.Errors.Select(e => e.Message)));
            (reported.Scanned, reported.Fired, reported.TruePositives, reported.FalseNegatives).Should()
                .Be((direct.Scanned, direct.Fired, direct.TruePositives, direct.FalseNegatives));
        }

        [Fact]
        public async Task TheArchiveIsReadOnlyThroughTheReportConnectionWhenOneIsSetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ReportServiceAsync(dbContext, ReportConnectionString("jube_backtest_report_missing"));
            var entry = await dbContext.Archive.Where(a => a.EntityAnalysisModelId == modelId)
                .Select(a => a.EntityAnalysisModelInstanceEntryGuid).FirstAsync();

            var run = () => service.RunRuleAsync(Request());
            var explain = () => service.ExplainAsync(Request(), entry);

            (await run.Should().ThrowAsync<Npgsql.PostgresException>()).Which.SqlState.Should().Be("3D000");
            (await explain.Should().ThrowAsync<Npgsql.PostgresException>()).Which.SqlState.Should().Be("3D000");
        }

        [Fact]
        public async Task ABacktestThreadReadsTheArchiveThroughTheReportConnectionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);
            var missing = await service.SubmitAsync(Request());
            await new BacktestTaskStarter(EngineContext(ReportConnectionString("jube_backtest_report_missing")), 1)
                .RunNextAsync(CancellationToken.None);
            var failed = await service.StatusAsync(missing.Instance!.Id);

            var working = await service.SubmitAsync(Request());
            var starter = new BacktestTaskStarter(EngineContext(ReportConnectionString(null)), 1);
            for (var i = 0;
                 i < 10 && (await service.StatusAsync(working.Instance!.Id)).Status is "Pending" or "Running";
                 i++)
            {
                await starter.RunNextAsync(CancellationToken.None);
            }

            var succeeded = await service.StatusAsync(working.Instance!.Id);

            failed.Status.Should().Be("Failed");
            succeeded.Status.Should().Be("Succeeded", succeeded.Error);
            (succeeded.Scanned, succeeded.TruePositives).Should().Be((5L, 2L));
        }

        [Fact]
        public async Task AnExplanationSaysWhyATransactionWasCountedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext);

            var explained = await service.ExplainAsync(Request(), entries[2]);

            explained.Valid.Should().BeTrue();
            (explained.PassedFilter, explained.Fired, explained.Positive).Should().Be((true, false, true));
            explained.Values.Should().ContainSingle(v => v.Name == "Payload.Amount" && v.Value == "50");
            explained.Tags.Should().BeEquivalentTo("Fraud", "Reviewed");
        }

        [Fact]
        public async Task AnotherTenantsModelIsNotFoundAndNoPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserTenantB)).RunRuleAsync(Request()));
            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission)).RunRuleAsync(Request()));
        }

        [Fact]
        public void EveryOperationIsCatalogued()
        {
            foreach (var name in new[]
                     {
                         "Fields", "RunRule", "Submit", "Status", "List", "Result", "Stop", "Explain"
                     })
            {
                ServiceToolCatalogue.All.Should().ContainSingle(t => t.Name == "EntityAnalysisModelBacktest" + name);
            }
        }
    }
}
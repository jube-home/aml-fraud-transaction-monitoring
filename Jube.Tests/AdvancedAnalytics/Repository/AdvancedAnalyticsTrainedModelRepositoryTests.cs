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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB.Data;
using Npgsql;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainedModelRepositoryTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private AdvancedAnalyticsTrainedModelRepository Models(int tenant)
        {
            return new AdvancedAnalyticsTrainedModelRepository(Db, tenant, "ZzTestUser");
        }

        private AdvancedAnalyticsTrainedModelVersionRepository Versions(int tenant)
        {
            return new AdvancedAnalyticsTrainedModelVersionRepository(Db, tenant, "ZzTestUser");
        }

        private Task<AdvancedAnalyticsTrainedModelVersion> NewVersionAsync(int tenant, int modelId,
            string modelJson = "{\"a\":1}", int? runId = null)
        {
            return Versions(tenant).InsertAsync(new AdvancedAnalyticsTrainedModelVersion
            {
                AdvancedAnalyticsTrainedModelId = modelId,
                ModelJson = modelJson,
                VariablesJson = "[\"x\",\"y\"]",
                MetricsJson = "{\"auc\":0.9}",
                CalibratorJson = "{\"a\":-1.5,\"b\":0.25}",
                AdvancedAnalyticsTrainingRunId = runId
            });
        }

        [Fact]
        public async Task InsertRoundTripsEveryKindAndSourceAndStartsInactiveAsync()
        {
            var instance = await NewInstanceAsync(TenantA);

            foreach (var kind in Enum.GetValues<AdvancedAnalyticsKind>())
            {
                foreach (var source in Enum.GetValues<AdvancedAnalyticsSource>())
                {
                    var inserted = await Models(TenantA).InsertAsync(new AdvancedAnalyticsTrainedModel
                    {
                        AdvancedAnalyticsInstanceId = instance.Id,
                        Name = UniqueName("Kind"),
                        KindId = kind,
                        SourceId = source,
                        CorrelationKey = "corr",
                        Active = 1,
                        ActiveVersionId = 12345
                    });

                    var read = await Models(TenantA).GetByIdAsync(inserted.Id);

                    read!.KindId.Should().Be(kind);
                    read.SourceId.Should().Be(source);
                    read.Active.Should().Be(0);
                    read.ActiveVersionId.Should().BeNull();
                    read.TenantRegistryId.Should().Be(TenantA);
                    read.AdvancedAnalyticsInstanceId.Should().Be(instance.Id);
                    read.CorrelationKey.Should().Be("corr");
                    read.Version.Should().Be(1);
                }
            }

            Db.AdvancedAnalyticsTrainedModel
                .Where(w => w.AdvancedAnalyticsInstanceId == instance.Id)
                .Select(s => s.KindId).Distinct().Count().Should().Be(Enum.GetValues<AdvancedAnalyticsKind>().Length);
        }

        [Fact]
        public async Task KindIsStoredAsANumberNotClearTextAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);

            Db.AdvancedAnalyticsTrainedModel.Where(w => w.Id == model.Id)
                .Select(s => s.KindId).Single().Should().Be(AdvancedAnalyticsKind.Classifier);

            var stored = Db.Execute<int>(
                "select \"KindId\" from \"AdvancedAnalyticsTrainedModel\" where \"Id\" = " + model.Id);

            stored.Should().Be((int)AdvancedAnalyticsKind.Classifier);
        }

        [Fact]
        public async Task InsertAgainstAForeignTenantsInstanceIsNotFoundAsync()
        {
            var foreign = await NewInstanceAsync(TenantB);

            var insert = () => NewTrainedModelAsync(TenantA, foreign.Id);

            await insert.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ForeignTenantReadsAreNotFoundAndListsAreScopedAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);

            (await Models(TenantB).GetByIdAsync(model.Id)).Should().BeNull();
            (await Models(TenantB).GetByGuidAsync(model.Guid)).Should().BeNull();
            (await Models(TenantB).ListAsync()).Should().NotContain(c => c.Id == model.Id);
            (await Models(TenantB).ListAsync(instance.Id)).Should().BeEmpty();
            (await Models(TenantA).ListAsync(instance.Id)).Select(s => s.Id).Should().Equal(model.Id);
        }

        [Fact]
        public async Task ForeignTenantCannotMutateAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var version = await NewVersionAsync(TenantA, model.Id);

            await ((Func<Task>)(() => Models(TenantB).UpdateAsync(model.Id, "ZzTestX", null)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Models(TenantB).SetActiveVersionAsync(model.Id, version.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Models(TenantB).SetActiveAsync(model.Id, false)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Models(TenantB).DeleteAsync(model.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            (await Models(TenantA).GetByIdAsync(model.Id))!.ActiveVersionId.Should().BeNull();
        }

        [Fact]
        public async Task ActivationRequiresAVersionAndActiveVersionMustBelongToTheModelAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var other = await NewTrainedModelAsync(TenantA, instance.Id);
            var version = await NewVersionAsync(TenantA, model.Id);
            var otherVersion = await NewVersionAsync(TenantA, other.Id);

            await ((Func<Task>)(() => Models(TenantA).SetActiveAsync(model.Id, true)))
                .Should().ThrowAsync<InvalidOperationException>();

            await ((Func<Task>)(() => Models(TenantA).SetActiveVersionAsync(model.Id, otherVersion.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await Models(TenantA).SetActiveVersionAsync(model.Id, version.Id);
            await Models(TenantA).SetActiveAsync(model.Id, true);

            var read = await Models(TenantA).GetByIdAsync(model.Id);
            read!.Active.Should().Be(1);
            read.ActiveVersionId.Should().Be(version.Id);

            (await Models(TenantA).ListActiveAsync()).Select(s => s.Id).Should().Contain(model.Id)
                .And.NotContain(other.Id);

            await Models(TenantA).SetActiveAsync(model.Id, false);
            (await Models(TenantA).ListActiveAsync()).Select(s => s.Id).Should().NotContain(model.Id);
        }

        [Fact]
        public async Task RollbackIsFlippingTheActiveVersionAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var first = await NewVersionAsync(TenantA, model.Id, "{\"v\":1}");
            var second = await NewVersionAsync(TenantA, model.Id, "{\"v\":2}");

            await Models(TenantA).SetActiveVersionAsync(model.Id, second.Id);
            (await Versions(TenantA).GetActiveByModelIdAsync(model.Id))!.ModelJson.Should().Be("{\"v\":2}");

            await Models(TenantA).SetActiveVersionAsync(model.Id, first.Id);
            (await Versions(TenantA).GetActiveByModelIdAsync(model.Id))!.ModelJson.Should().Be("{\"v\":1}");
            (await Versions(TenantB).GetActiveByModelIdAsync(model.Id)).Should().BeNull();
        }

        [Fact]
        public async Task UpdateChangesNameAndCorrelationKeyAndListsByCorrelationKeyAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var key = Guid.NewGuid().ToString("N");

            await Models(TenantA).UpdateAsync(model.Id, UniqueName("Renamed"), key);

            var read = await Models(TenantA).GetByIdAsync(model.Id);
            read!.CorrelationKey.Should().Be(key);
            read.Version.Should().Be(2);
            (await Models(TenantA).ListByCorrelationKeyAsync(key)).Select(s => s.Id).Should().Equal(model.Id);
            (await Models(TenantB).ListByCorrelationKeyAsync(key)).Should().BeEmpty();
        }

        [Fact]
        public async Task SoftDeleteHidesTheModelAndDeactivatesItAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var version = await NewVersionAsync(TenantA, model.Id);
            await Models(TenantA).SetActiveVersionAsync(model.Id, version.Id);
            await Models(TenantA).SetActiveAsync(model.Id, true);

            await Models(TenantA).DeleteAsync(model.Id);

            (await Models(TenantA).GetByIdAsync(model.Id)).Should().BeNull();
            (await Models(TenantA).ListActiveAsync()).Select(s => s.Id).Should().NotContain(model.Id);
            (await Versions(TenantA).GetActiveByModelIdAsync(model.Id)).Should().BeNull();
            var raw = Db.AdvancedAnalyticsTrainedModel.Single(s => s.Id == model.Id);
            raw.Deleted.Should().Be(1);
            raw.Active.Should().Be(0);
            raw.DeletedUser.Should().Be("ZzTestUser");

            await ((Func<Task>)(() => Models(TenantA).DeleteAsync(model.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => NewVersionAsync(TenantA, model.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task VersionNumbersIncrementPerModelAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var other = await NewTrainedModelAsync(TenantA, instance.Id);

            var one = await NewVersionAsync(TenantA, model.Id);
            var two = await NewVersionAsync(TenantA, model.Id);
            var otherOne = await NewVersionAsync(TenantA, other.Id);

            one.VersionNumber.Should().Be(1);
            two.VersionNumber.Should().Be(2);
            otherOne.VersionNumber.Should().Be(1);

            (await Versions(TenantA).ListByModelIdAsync(model.Id)).Select(s => s.VersionNumber)
                .Should().Equal(2, 1);

            (await Versions(TenantA).GetByModelIdAndVersionNumberAsync(model.Id, 2))!.Id.Should().Be(two.Id);
        }

        [Fact]
        public async Task ModelPayloadIsStoredVerbatimAsJsonNotJsonbAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            const string payload = "{\"z\":-0.0,\"a\":1.0,\"m\":[0.10,1E-7, -0]}";

            var version = await NewVersionAsync(TenantA, model.Id, payload);
            var read = await Versions(TenantA).GetByIdAsync(version.Id);

            read!.ModelJson.Should().Be(payload);

            var types = Db.Query<string>(
                "select data_type from information_schema.columns where table_name = " +
                "'AdvancedAnalyticsTrainedModelVersion' and column_name in " +
                "('ModelJson','CalibratorJson','VariablesJson','MetricsJson') order by column_name").ToList();

            types.Should().Equal("json", "jsonb", "json", "jsonb");
            read.CalibratorJson.Should().Be("{\"a\":-1.5,\"b\":0.25}");
        }

        [Fact]
        public async Task VersionsAreImmutableAndPayloadIsValidatedAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);

            await ((Func<Task>)(() => NewVersionAsync(TenantA, model.Id, " ")))
                .Should().ThrowAsync<ArgumentException>();

            var invalid = await ((Func<Task>)(() => NewVersionAsync(TenantA, model.Id, "{not json")))
                .Should().ThrowAsync<PostgresException>();

            invalid.Which.SqlState.Should().Be("22P02");

            typeof(AdvancedAnalyticsTrainedModelVersionRepository).GetMethods()
                .Select(s => s.Name).Should().NotContain(n => n.StartsWith("Update") || n.StartsWith("Delete"));
        }

        [Fact]
        public async Task VersionListOmitsThePayloadAndForeignTenantSeesNothingAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var version = await NewVersionAsync(TenantA, model.Id);

            var listed = (await Versions(TenantA).ListByModelIdAsync(model.Id)).Single();

            listed.ModelJson.Should().BeNull();
            listed.VariablesJson.Should().NotBeNull();
            listed.MetricsJson.Should().NotBeNull();
            (await Versions(TenantB).GetByIdAsync(version.Id)).Should().BeNull();
            (await Versions(TenantB).ListByModelIdAsync(model.Id)).Should().BeEmpty();
            (await Versions(TenantB).GetByModelIdAndVersionNumberAsync(model.Id, 1)).Should().BeNull();

            await ((Func<Task>)(() => NewVersionAsync(TenantB, model.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task VersionCanReferenceItsProducingRunButNotAForeignOneAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var foreignInstance = await NewInstanceAsync(TenantB);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            var run = await NewRunAsync(TenantA, instance.Id);
            var foreignRun = await NewRunAsync(TenantB, foreignInstance.Id);

            var version = await NewVersionAsync(TenantA, model.Id, runId: run.Id);

            version.AdvancedAnalyticsTrainingRunId.Should().Be(run.Id);

            await ((Func<Task>)(() => NewVersionAsync(TenantA, model.Id, runId: foreignRun.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ConcurrentVersionInsertsGetDistinctConsecutiveNumbersAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);

            var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(async i =>
            {
                await using var db = Fixture.GetDbContext();
                var repository = new AdvancedAnalyticsTrainedModelVersionRepository(db, TenantA, "ZzTestUser");

                return await repository.InsertAsync(new AdvancedAnalyticsTrainedModelVersion
                {
                    AdvancedAnalyticsTrainedModelId = model.Id,
                    ModelJson = $"{{\"i\":{i}}}"
                });
            }));

            results.Select(s => s.VersionNumber).OrderBy(o => o).Should().Equal(1, 2, 3, 4, 5, 6);
        }

        [Fact]
        public async Task CancelledTokenStopsEveryOperationAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var model = await NewTrainedModelAsync(TenantA, instance.Id);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            await ((Func<Task>)(() => Models(TenantA).GetByIdAsync(model.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Models(TenantA).ListActiveAsync(token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Models(TenantA).SetActiveAsync(model.Id, false, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Versions(TenantA).ListByModelIdAsync(model.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Versions(TenantA).InsertAsync(new AdvancedAnalyticsTrainedModelVersion
                {
                    AdvancedAnalyticsTrainedModelId = model.Id,
                    ModelJson = "{}"
                }, token)))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
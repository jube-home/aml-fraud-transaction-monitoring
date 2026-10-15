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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Storage;
using Jube.Engine.Exhaustive.Utilities;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveBaseRateTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Aggregate = "jsonb_build_object('abstraction', jsonb_build_object('A', g))";
        private const string PositiveSql = "(\"Json\"->'abstraction'->>'A')::double precision >= (@1)";
        private readonly LoadModelSeed seed = new(fx);

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return seed.DisposeAsync();
        }

        private static string RuleJson(string field, string op, string value)
        {
            return "{\"Id\":\"" + field + "\",\"Field\":\"" + field + "\",\"Type\":\"double\",\"Operator\":\"" + op +
                   "\",\"Value\":\"" + value + "\"}";
        }

        [Fact]
        public async Task Archive_TheBaseRateIsTheCountOfFilterMatchesOverThePopulationAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 30, Aggregate);

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "22"), PositiveSql, "[22]", false, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(9 / 30d, 1e-12);
            resolved.Source.Should().Be(BaseRateResolution.SourcePopulationCount);
        }

        [Fact]
        public async Task Archive_TheSampleCapsDoNotDistortTheBaseRateAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 25000, Aggregate);

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "12001"), PositiveSql, "[12001]", false, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(13000 / 25000d, 1e-12);
            resolved.Rate.Should().NotBe(0.5);
        }

        [Fact]
        public async Task Archive_OtherModelsAndTheMockTableAreNotCountedAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            var (otherId, _) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 10, Aggregate);
            await seed.AddRowsAsync("Archive", otherId, 500, Aggregate);
            await seed.AddRowsAsync("MockArchive", modelId, 300, Aggregate);

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "9"), PositiveSql, "[9]", false, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(0.2, 1e-12);
        }

        [Fact]
        public async Task Demo_TheMockArchiveIsCountedInsteadOfTheArchiveAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "IsChip");

            await seed.AddRowsAsync("MockArchive", modelId, 10,
                "jsonb_build_object('abstraction', jsonb_build_object('IsChip', case when g % 5 = 0 then 1 else 0 end))");

            await seed.AddRowsAsync("Archive", modelId, 50, Aggregate);

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.IsChip", "equal", "1"),
                "(\"Json\"->'abstraction'->>'IsChip')::double precision = (@1)", "[1]", true, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(0.2, 1e-12);
        }

        [Fact]
        public async Task Archive_AFilterThatMatchesNothingOrEverythingHasNoUsableBaseRateAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 10, Aggregate);

            await using var db = fx.GetDbContext();

            var none = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "100"), PositiveSql, "[100]", false, TestLog.NoOp,
                ConnectionString);
            var all = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "0"), PositiveSql, "[0]", false, TestLog.NoOp,
                ConnectionString);

            none.IsUsable.Should().BeFalse();
            none.Source.Should().Be(BaseRateResolution.SourceSamplePrior);
            all.IsUsable.Should().BeFalse();
        }

        [Fact]
        public async Task Archive_AFailureIsLoggedAndFallsBackToTheSamplePriorAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            var log = new TestLog();

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveArchiveBaseRateAsync(db, tenantId, modelId, "not json", "true",
                "[]", false, log, ConnectionString);

            resolved.IsUsable.Should().BeFalse();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("base rate"));
        }

        [Fact]
        public async Task Dataset_TheClassProportionIsCountedOverTheWholeDatasetAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("Archive", modelId, 200, Aggregate);

            var plan = new DatasetTrainingPlan
            {
                Source = SampleSource.Archive(modelId),
                Variables = [],
                PositiveSql = "(\"Json\"->'abstraction'->>'A')::double precision >= 199"
            };

            await using var db = fx.GetDbContext();

            var resolved = await Extraction.ResolveDatasetBaseRateAsync(db, plan, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(2 / 200d, 1e-12);
            resolved.Source.Should().Be(BaseRateResolution.SourcePopulationCount);
        }

        [Theory]
        [InlineData(DatasetKind.Csv)]
        [InlineData(DatasetKind.Json)]
        public async Task Dataset_TheClassProportionIsCountedForBothStoredDatasetKindsAsync(DatasetKind kind)
        {
            var (_, tenantId) = await seed.CreateModelAsync();
            await using var db = fx.GetDbContext();
            var repository = new ExhaustiveInstanceDatasetRepository(db, tenantId);

            IDatasetStore dataset = kind == DatasetKind.Csv
                ? await repository.InsertAsync(new ExhaustiveInstanceCsv
                {
                    Name = $"{DatabaseFixture.Prefix}Br{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 1
                })
                : await repository.InsertAsync(new ExhaustiveInstanceJson
                {
                    Name = $"{DatabaseFixture.Prefix}Br{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 1
                });

            await repository.InsertRowsAsync(kind, Enumerable.Range(0, 40).Select(i =>
                ExhaustiveInstanceDatasetRepository.NewRow(kind, dataset.Id, Guid.NewGuid(),
                    $"{{\"values\":{{\"c0\":{(i < 10 ? 1 : 0)}}}}}")));

            var plan = new DatasetTrainingPlan
            {
                Source = SampleSource.Dataset(DatasetStores.ReferenceOf(dataset)),
                Variables = [],
                PositiveSql = "(\"Json\"->'values'->>'c0')::double precision >= 1"
            };

            var resolved = await Extraction.ResolveDatasetBaseRateAsync(db, plan, TestLog.NoOp,
                ConnectionString);

            resolved.Rate.Should().BeApproximately(10 / 40d, 1e-12);
            resolved.Source.Should().Be(BaseRateResolution.SourcePopulationCount);
        }

        [Fact]
        public void TheCountSql_IsScopedToTheKeyAndTheFilter()
        {
            Postgres.BuildArchiveCountSql("x = (@1)", 2, SampleSource.Archive(5)).Should().Be(
                "select count(*) from \"Archive\" where \"EntityAnalysisModelId\" = (@2) and (x = (@1))");
        }
    }
}
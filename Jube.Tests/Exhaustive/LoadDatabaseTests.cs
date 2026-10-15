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
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Storage;
using Jube.Engine.Exhaustive.Utilities;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class LoadDatabaseTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Aggregate = "jsonb_build_object('abstraction', jsonb_build_object('A', g))";
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
        public async Task RealFields_OnlyTtlCounterAbstractionAndAdaptationFieldsSurviveInQueryOrderAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelRequestXpath", modelId, "PayloadField");
            await seed.AddChildAsync("EntityAnalysisModelSanction", modelId, "SanctionField");
            await seed.AddChildAsync("EntityAnalysisModelActivationRule", modelId, "ActivationField");
            await seed.AddChildAsync("EntityAnalysisModelTag", modelId, "TagField");
            await seed.AddChildAsync("ExhaustiveSearchInstance", modelId, "Adapt");
            await seed.AddChildAsync("EntityAnalysisModelHttpAdaptation", modelId, "Http");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionCalculation", modelId, "Calc");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Bravo");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Alpha");
            await seed.AddChildAsync("EntityAnalysisModelTtlCounter", modelId, "Counter");
            await seed.AddRowsAsync("Archive", modelId, 1, Aggregate);

            await using var db = fx.GetDbContext();
            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);

            variables.Keys.Should().Equal(0, 1, 2, 3, 4, 5);

            variables.Values.Select(v => v.Name).Should().Equal("TTLCounter.Counter", "Abstraction.Alpha",
                "Abstraction.Bravo", "AbstractionCalculation.Calc", "HTTPAdaptation.Http",
                "ExhaustiveAdaptation.Adapt");

            variables.Values.Select(v => (int)v.ProcessingTypeId).Should().Equal(3, 5, 5, 7, 7, 7);
            variables[0].ValueJsonPath.Should().Be("ttlCounter.Counter");
            variables[1].ValueJsonPath.Should().Be("abstraction.Alpha");
            variables[3].ValueJsonPath.Should().Be("abstractionCalculation.Calc");
            variables[4].ValueJsonPath.Should().Be("HttpAdaptation.Http");
            variables[5].ValueJsonPath.Should().Be("ExhaustiveAdaptation.Adapt");
            rows.Should().ContainSingle().Which.Should().Equal(0, 0, 0, 0, 0, 0);
        }

        [Fact]
        public async Task RealFields_InactiveDeletedAndOtherTenantFieldsAreExcludedAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Live");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Off", 0);
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Gone", 1, 1);
            await seed.AddChildAsync("EntityAnalysisModelTtlCounter", modelId, "OffCounter", 0);

            await using var db = fx.GetDbContext();
            var (variables, _) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);
            var (foreign, _) = await Extraction.GetSampleDataAsync(db, tenantId + 100000, modelId, false);

            variables.Values.Select(v => v.Name).Should().Equal("Abstraction.Live");
            foreign.Should().BeEmpty();
        }

        [Fact]
        public async Task RealFields_ValuesAreExtractedByTheJsonPathAndMissingIsZeroAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelTtlCounter", modelId, "T");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionCalculation", modelId, "C");

            await seed.AddRowsAsync("Archive", modelId, 3,
                "jsonb_build_object('abstraction', jsonb_build_object('A', g), " +
                "'ttlCounter', jsonb_build_object('T', g * 10)) || case when g = 3 then '{}'::jsonb else " +
                "jsonb_build_object('abstractionCalculation', jsonb_build_object('C', g * 100)) end");

            await using var db = fx.GetDbContext();
            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);

            variables.Values.Select(v => v.Name).Should()
                .Equal("TTLCounter.T", "Abstraction.A", "AbstractionCalculation.C");

            rows.Should().HaveCount(3);
            rows[0].Should().Equal(10, 1, 100);
            rows[1].Should().Equal(20, 2, 200);
            rows[2].Should().Equal(30, 3, 0);
        }

        [Fact]
        public async Task RealSample_IsOrderedByEntryGuidAndCappedAtTenThousandAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 10005, Aggregate);

            await using var db = fx.GetDbContext();
            var (_, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);

            rows.Should().HaveCount(10000);
            rows.Select(r => r[0]).Should().Equal(Enumerable.Range(1, 10000).Select(i => (double)i));
        }

        [Fact]
        public async Task RealSample_ExactlyAtAndJustBelowTheCapIsNotTruncatedAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 10000, Aggregate);
            var (smallModelId, _) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", smallModelId, "A");
            await seed.AddRowsAsync("Archive", smallModelId, 9999, Aggregate);

            await using var db = fx.GetDbContext();
            var (_, exact) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);
            var (_, below) = await Extraction.GetSampleDataAsync(db, tenantId, smallModelId, false);

            exact.Should().HaveCount(10000);
            below.Should().HaveCount(9999);
        }

        [Fact]
        public async Task RealSample_OtherModelsAndTheMockTableAreNotMixedInAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            var (otherModelId, _) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 4, Aggregate);
            await seed.AddRowsAsync("Archive", otherModelId, 7, Aggregate);
            await seed.AddRowsAsync("MockArchive", modelId, 9, Aggregate);

            await using var db = fx.GetDbContext();
            var (_, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);

            rows.Should().HaveCount(4);
        }

        [Fact]
        public async Task RealSample_ModelWithNoArchiveRowsGivesVariablesAndNoRowsAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");

            await using var db = fx.GetDbContext();
            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, false);

            variables.Should().HaveCount(1);
            rows.Should().BeEmpty();
        }

        [Fact]
        public async Task MockSample_ReadsMockArchiveWithTheFixedVariableListIgnoringModelFieldsAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "Unrelated");
            await seed.AddRowsAsync("Archive", modelId, 5, Aggregate);

            await seed.AddRowsAsync("MockArchive", modelId, 3,
                "jsonb_build_object('abstraction', jsonb_build_object('IsChip', g, 'HighRiskCountry', g * 2))");

            await using var db = fx.GetDbContext();
            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, true);

            variables.Should().HaveCount(26);
            rows.Should().HaveCount(3);
            rows.Should().AllSatisfy(r => r.Should().HaveCount(26));
            rows.Select(r => r[0]).Should().Equal(1, 2, 3);
            rows.Select(r => r[25]).Should().Equal(2, 4, 6);
        }

        [Fact]
        public async Task MockSample_IsOrderedByEntryGuidAndCappedAtTenThousandAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();

            await seed.AddRowsAsync("MockArchive", modelId, 10003,
                "jsonb_build_object('abstraction', jsonb_build_object('IsChip', g))");

            await using var db = fx.GetDbContext();
            var (_, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, true);

            rows.Should().HaveCount(10000);
            rows.Select(r => r[0]).Should().Equal(Enumerable.Range(1, 10000).Select(i => (double)i));
        }

        [Fact]
        public async Task ClassData_ReturnsOnlyRowsMatchingTheFilterLabelledOneAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("Archive", modelId, 20, Aggregate);

            var variables = Extraction.BuildVariablesFromFields(
            [
                new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto
                {
                    Name = "Abstraction.A", ProcessingTypeId = 5, ValueJsonPath = "abstraction.A"
                }
            ]);

            await using var db = fx.GetDbContext();

            var (data, outputs) = await Extraction.GetClassDataAsync(db, modelId,
                "(\"Json\"->'abstraction'->>'A')::double precision >= (@1)", "[15]", variables, false,
                TestLog.NoOp, ConnectionString);

            data.Select(r => r[0]).Should().Equal(Enumerable.Range(15, 6).Select(i => (double)i));
            outputs.Should().Equal(Enumerable.Repeat(1d, 6));
        }

        [Fact]
        public async Task ClassData_WorksWithTheSelectOnlyGuardEnabledAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("Archive", modelId, 5, Aggregate);
            var variables = Extraction.BuildVariablesFromFields([]);

            await using var db = fx.GetDbContext();

            var (guarded, _) = await Extraction.GetClassDataAsync(db, modelId, "true", "[]", variables, false,
                TestLog.NoOp, ConnectionString);

            guarded.Should().HaveCount(5);
        }

        [Fact]
        public async Task ClassData_ClassCapIsTenThousandAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("Archive", modelId, 10010, Aggregate);
            var variables = Extraction.BuildVariablesFromFields([]);

            await using var db = fx.GetDbContext();

            var (data, outputs) = await Extraction.GetClassDataAsync(db, modelId, "true", "[]", variables, false,
                TestLog.NoOp, ConnectionString);

            data.Should().HaveCount(10000);
            outputs.Should().HaveCount(10000).And.OnlyContain(o => o == 1);
        }

        [Fact]
        public async Task ClassData_MockTableIsUsedWhenMockDataIsSetAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("Archive", modelId, 6, Aggregate);
            await seed.AddRowsAsync("MockArchive", modelId, 2, Aggregate);

            await using var db = fx.GetDbContext();

            var (data, _) = await Extraction.GetClassDataAsync(db, modelId, "true", "[]",
                Extraction.BuildMockVariables(), true, TestLog.NoOp, ConnectionString);

            data.Should().HaveCount(2);
            data.Should().AllSatisfy(r => r.Should().HaveCount(26));
        }

        [Fact]
        public async Task NegativeSample_IsTheComplementOfTheFilterAndDisjointFromTheClassAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 30, Aggregate);
            var filterJson = RuleJson("Abstraction.A", "greater_or_equal", "22");

            await using var db = fx.GetDbContext();

            var (variables, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson, "[22]",
                false, TestLog.NoOp, ConnectionString);

            var (positives, labels) = await Extraction.GetClassDataAsync(db, modelId,
                "(\"Json\"->'abstraction'->>'A')::double precision >= (@1)", "[22]", variables, false, TestLog.NoOp,
                ConnectionString);

            negatives.Select(r => r[0]).Should().Equal(Enumerable.Range(1, 21).Select(i => (double)i));
            positives.Select(r => r[0]).Should().Equal(Enumerable.Range(22, 9).Select(i => (double)i));
            negatives.Select(r => r[0]).Intersect(positives.Select(r => r[0])).Should().BeEmpty();
            negatives.Length.Should().Be(21);
            labels.Should().OnlyContain(l => l == 1);
        }

        [Fact]
        public async Task NegativeSample_OfAStrictGreaterThanFilter_KeepsTheBoundaryValueAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 30, Aggregate);

            await using var db = fx.GetDbContext();

            var (_, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater", "22"), "[22]", false, TestLog.NoOp, ConnectionString);

            negatives.Select(r => r[0]).Should().Equal(Enumerable.Range(1, 22).Select(i => (double)i));
        }

        [Fact]
        public async Task NegativeSample_RowsMissingTheFilteredKeyAreInNeitherClassAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");

            await seed.AddRowsAsync("Archive", modelId, 4,
                "case when g <= 2 then jsonb_build_object('abstraction', jsonb_build_object('A', g)) " +
                "else jsonb_build_object('abstraction', jsonb_build_object('Other', g)) end");

            await using var db = fx.GetDbContext();

            var (_, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId,
                RuleJson("Abstraction.A", "greater_or_equal", "2"), "[2]", false, TestLog.NoOp,
                ConnectionString);

            var (positives, _) = await Extraction.GetClassDataAsync(db, modelId,
                "(\"Json\"->'abstraction'->>'A')::double precision >= (@1)", "[2]",
                Extraction.BuildVariablesFromFields([]), false, TestLog.NoOp, ConnectionString);

            negatives.Should().ContainSingle().Which[0].Should().Be(1);
            positives.Should().ContainSingle();
        }

        [Fact]
        public async Task NegativeSample_CapIsTenThousandAndAppliesIndependentlyOfTheClassCapAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");
            await seed.AddRowsAsync("Archive", modelId, 20050, Aggregate);
            var filterJson = RuleJson("Abstraction.A", "greater_or_equal", "10500");

            await using var db = fx.GetDbContext();

            var (variables, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson,
                "[10500]", false, TestLog.NoOp, ConnectionString);

            var (positives, _) = await Extraction.GetClassDataAsync(db, modelId,
                "(\"Json\"->'abstraction'->>'A')::double precision >= (@1)", "[10500]", variables, false,
                TestLog.NoOp, ConnectionString);

            negatives.Should().HaveCount(10000);
            positives.Should().HaveCount(9551);
            negatives.Max(r => r[0]).Should().Be(10000);
        }

        [Fact]
        public async Task NegativeSample_MockDataFilterUsesTheMockTableAndFixedVariablesAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "IsChip");

            await seed.AddRowsAsync("MockArchive", modelId, 10,
                "jsonb_build_object('abstraction', jsonb_build_object('IsChip', g % 2))");

            await seed.AddRowsAsync("Archive", modelId, 50, Aggregate);

            await using var db = fx.GetDbContext();

            var (variables, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId,
                RuleJson("Abstraction.IsChip", "equal", "1"), "[1]", true, TestLog.NoOp, ConnectionString);

            variables.Should().HaveCount(26);
            negatives.Should().HaveCount(5);
            negatives.Should().OnlyContain(r => r[0] == 0);
        }

        [Fact]
        public async Task LoadMock_PopulatesMockArchiveOncePerModelFromTheEmbeddedCsvAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            var expected = Mock.Get().Split("\n").Skip(1).Count(l => l.Split(",").All(c => double.TryParse(c, out _)));

            await LoadMockAsync(modelId);

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == modelId)).Should().Be(expected);
            expected.Should().BeGreaterThan(1000);

            await LoadMockAsync(modelId);
            (await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == modelId)).Should().Be(expected);
            _ = tenantId;
        }

        [Fact]
        public async Task LoadMock_RoundTripsEveryCsvRowThroughJsonBackToTheSameFeatureMatrixAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await LoadMockAsync(modelId);

            await using var db = fx.GetDbContext();
            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, true);

            var csvRows = Mock.Get().Split("\n").Skip(1)
                .Select(l => l.Split(",")).Where(p => p.All(c => double.TryParse(c, out _)))
                .Select(p => p.Skip(1).Select(double.Parse).ToArray()).ToList();

            variables.Should().HaveCount(26);
            rows.Should().HaveCount(csvRows.Count);
            Canonical(rows).Should().Equal(Canonical(csvRows));
        }

        [Fact]
        public async Task LoadMock_TagsFraudExactlyWhenTheDependentColumnIsPositiveAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            var log = new TestLog();
            await LoadMockAsync(modelId, log);

            var csv = Mock.Get().Split("\n").Skip(1).Select(l => l.Split(","))
                .Where(p => p.All(c => double.TryParse(c, out _))).ToList();

            await using var db = fx.GetDbContext();
            var total = await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == modelId);

            var fraud = await db.MockArchive.CountAsync(m =>
                m.EntityAnalysisModelId == modelId && Sql.Expr<bool>("\"Json\" -> 'tag' ? 'Fraud'"));

            var errors = log.Entries.Where(e => e.Level == "ERROR").Select(e => e.Message).ToList();
            errors.Should().BeEmpty();
            total.Should().Be(csv.Count);
            fraud.Should().Be(csv.Count(p => double.Parse(p[0]) > 0));
        }

        [Fact]
        public async Task LoadMock_DoesNotTouchAnotherModelAndSkipsOnlyTheModelAlreadyLoadedAsync()
        {
            var (first, _) = await seed.CreateModelAsync();
            var (second, _) = await seed.CreateModelAsync();
            await LoadMockAsync(first);

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == second)).Should().Be(0);

            await LoadMockAsync(second);
            var a = await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == first);
            var b = await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == second);
            a.Should().Be(b).And.BeGreaterThan(0);
        }

        [Fact]
        public async Task LoadMock_SkipsWhenTheModelAlreadyHasAnyMockRowAsync()
        {
            var (modelId, _) = await seed.CreateModelAsync();
            await seed.AddRowsAsync("MockArchive", modelId, 1, Aggregate);

            await LoadMockAsync(modelId);

            await using var db = fx.GetDbContext();
            (await db.MockArchive.CountAsync(m => m.EntityAnalysisModelId == modelId)).Should().Be(1);
        }

        private static IEnumerable<string> Canonical(IEnumerable<double[]> rows)
        {
            return rows.Select(r => string.Join(",", r.Select(v => v.ToString("R"))))
                .OrderBy(s => s, StringComparer.Ordinal);
        }

        private async Task LoadMockAsync(int modelId, TestLog? log = null)
        {
            var loader = new MockDataLoader(log ?? TestLog.NoOp, new JsonSerializationHelper());

            await using var db = fx.GetDbContext();
            await loader.LoadMockDataAsync(db, modelId);
        }
    }
}
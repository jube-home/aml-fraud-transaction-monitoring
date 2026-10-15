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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Analytics.Categorical.Models;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Exhaustive;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Utilities;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using log4net;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CategoricalTrainingTests(DatabaseFixture fx)
    {
        private const string FilterSql = "(\"Json\"->'payload'->>'Flag')::int >= (@1)";

        private const string CountryExpression =
            "(SELECT CASE WHEN g % 50 = 0 THEN NULL WHEN g % 7 = 0 THEN ' ' || lower(c) || ' ' ELSE c END FROM (SELECT " +
            "CASE WHEN g > 400 THEN (CASE WHEN g % 10 < 6 THEN 'NG' WHEN g % 10 < 8 THEN 'RO' ELSE 'GB' END) " +
            "ELSE (CASE WHEN g % 20 < 10 THEN 'GB' WHEN g % 20 < 16 THEN 'US' WHEN g % 20 < 19 THEN 'FR' ELSE 'NG' END) END AS c) s)";

        private static readonly string rowExpression =
            "jsonb_build_object('payload', jsonb_build_object('Country', " + CountryExpression +
            ", 'Merchant', 'M' || g, 'Flag', CASE WHEN g > 400 THEN 1 ELSE 0 END), " +
            "'abstraction', jsonb_build_object('A', g % 13))";

        private static readonly string filterJson =
            "{\"Id\":\"Payload.Flag\",\"Field\":\"Payload.Flag\",\"Type\":\"integer\",\"Operator\":\"greater_or_equal\",\"Value\":\"1\"}";

        private readonly LoadModelSeed seed = new(fx);

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private async Task<(int ModelId, int TenantId)> SeedAsync()
        {
            var (modelId, tenantId) = await seed.CreateModelAsync();
            await seed.AddChildAsync("EntityAnalysisModelRequestXpath", modelId, "Country");
            await seed.AddChildAsync("EntityAnalysisModelRequestXpath", modelId, "Merchant");
            await seed.AddChildAsync("EntityAnalysisModelRequestXpath", modelId, "Flag");
            await seed.AddChildAsync("EntityAnalysisModelAbstractionRule", modelId, "A");

            await using var db = fx.GetDbContext();

            foreach (var (name, type) in new[] { ("Country", 1), ("Merchant", 1), ("Flag", 2) })
            {
                await db.ExecuteAsync(
                    "UPDATE \"EntityAnalysisModelRequestXpath\" SET \"DataTypeId\" = @t " +
                    "WHERE \"EntityAnalysisModelId\" = @m AND \"Name\" = @n",
                    new DataParameter("t", type), new DataParameter("m", modelId), new DataParameter("n", name));
            }

            await seed.AddRowsAsync("Archive", modelId, 600, rowExpression);

            return (modelId, tenantId);
        }

        private static CategoricalOptions Options()
        {
            return new CategoricalOptions { Enabled = true, MinLevelCount = 5 }.Sanitised();
        }

        [Fact]
        public async Task Extraction_ExplodesAPayloadStringFieldIntoIndicatorColumnsFittedOnBothClassesAsync()
        {
            var (modelId, tenantId) = await SeedAsync();
            await using var db = fx.GetDbContext();

            var classJson = await Extraction.GetClassJsonAsync(db, modelId, FilterSql, "[1]", false, TestLog.NoOp,
                ConnectionString);

            CategoricalPlan? plan = null;

            var (variables, negatives) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson, "[1]",
                false, TestLog.NoOp, ConnectionString, CancellationToken.None, Options(), p => plan = p,
                classJson);

            variables.Values.Select(v => v.Name).Should().Equal("Abstraction.A", "Payload.Country=NG",
                "Payload.Country=US", "Payload.Country=FR", "Payload.Country=RO", "Payload.Country=[Missing]");

            plan!.Report.Single(s => s.SourceName == "Payload.Merchant").Decision.Should()
                .Be(CategoricalDecision.SkippedIdentifier);

            plan.Encoders.Single().Reference.Label.Should().Be("GB");
            negatives.Should().HaveCount(400);
            negatives.Should().AllSatisfy(r => r.Should().HaveCount(6));
            negatives.Should().AllSatisfy(r => r.Skip(1).Sum().Should().BeLessThanOrEqualTo(1));
            negatives[18].Skip(1).Should().Equal(1, 0, 0, 0, 0);
            negatives[9].Skip(1).Should().Equal(0, 1, 0, 0, 0);
            negatives[49].Skip(1).Should().Equal(0, 0, 0, 0, 1);
            negatives[13].Skip(1).Should().Equal(0, 1, 0, 0, 0);
            negatives[20].Skip(1).Should().Equal(0, 0, 0, 0, 0);

            var (positives, labels) = await Extraction.GetClassDataAsync(db, modelId, FilterSql, "[1]", variables,
                false, TestLog.NoOp, ConnectionString);

            labels.Should().OnlyContain(l => l == 1);
            positives.Should().HaveCount(200);
            positives.Count(r => r[1] == 1).Should().BeGreaterThan(100);
            positives.Count(r => r[4] == 1).Should().BeGreaterThan(30);
        }

        [Fact]
        public async Task Extraction_WithoutTheClassRowsInTheFitAFraudOnlyLevelWouldBeLostAsync()
        {
            var (modelId, tenantId) = await SeedAsync();
            await using var db = fx.GetDbContext();

            var (variables, _) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson, "[1]", false,
                TestLog.NoOp, ConnectionString, CancellationToken.None, Options());

            variables.Values.Select(v => v.Name).Should().NotContain("Payload.Country=RO");
        }

        [Fact]
        public async Task Extraction_WithoutTheOptionOnlyTheNumericFieldsAreReadAsBeforeAsync()
        {
            var (modelId, tenantId) = await SeedAsync();
            await using var db = fx.GetDbContext();

            var (variables, rows) = await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson, "[1]", false,
                TestLog.NoOp, ConnectionString);

            variables.Values.Select(v => v.Name).Should().Equal("Abstraction.A");
            rows.Should().AllSatisfy(r => r.Should().HaveCount(1));
            variables.Values.Should().OnlyContain(v => v.CategoricalEncoder == null);
        }

        [Fact]
        public async Task Extraction_AnUndeclaredTypeIsInferredFromTheSampleAsync()
        {
            var (modelId, tenantId) = await SeedAsync();
            await using var db = fx.GetDbContext();

            await db.ExecuteAsync(
                "UPDATE \"EntityAnalysisModelRequestXpath\" SET \"DataTypeId\" = NULL " +
                "WHERE \"EntityAnalysisModelId\" = @m AND \"Name\" = 'Country'", new DataParameter("m", modelId));

            CategoricalPlan? plan = null;

            await Extraction.GetSampleDataAsync(db, tenantId, modelId, filterJson, "[1]", false, TestLog.NoOp,
                ConnectionString, CancellationToken.None, Options(), p => plan = p);

            plan!.Report.Single(s => s.SourceName == "Payload.Country").Decision.Should()
                .Be(CategoricalDecision.Categorical);
        }

        [Fact]
        public async Task Training_ReachesAPromotedTrialWithIndicatorVariablesAndPersistsTheMetadataAsync()
        {
            var (modelId, tenantId) = await SeedAsync();
            await using var db = fx.GetDbContext();

            var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Cat{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = modelId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 0,
                    Anomaly = 0,
                    Filter = 1,
                    FilterJson = filterJson,
                    FilterSql = FilterSql,
                    FilterTokens = "[1]",
                    CategoricalOptionsJson = Options().ToJson()
                });

            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["ReportConnectionString"] = ConnectionString,
                ["ExhaustiveTrialsLimit"] = "3",
                ["ExhaustiveTopologySinceImprovementLimit"] = "2",
                ["ExhaustiveTopologyExplorationEpochs"] = "2",
                ["ExhaustiveTopologyFinalisationEpochs"] = "2",
                ["ExhaustiveActivationFunctionExplorationEpochs"] = "3",
                ["ExhaustiveSimulationsCount"] = "50"
            });

            var training = new Training(LogManager.GetLogger(typeof(CategoricalTrainingTests)), environment,
                new JsonSerializationHelper())
            {
                seeded = new Random(1),
                pollDelay = TimeSpan.FromMilliseconds(20)
            };

            var completed = await training.RunOneInstanceAsync(new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = instance.Id,
                EntityAnalysisModelId = modelId,
                TenantRegistryId = tenantId,
                Anomaly = false,
                Filter = true,
                FilterJson = filterJson,
                FilterSql = FilterSql,
                FilterTokens = "[1]",
                CategoricalOptionsJson = Options().ToJson()
            }, db, CancellationToken.None);

            completed.Should().BeTrue();
            (await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instance.Id)).StatusId.Should().Be(16);

            var fields = await db.ExhaustiveSearchInstanceCategoricalField
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).ToListAsync();
            fields.Should().ContainSingle();
            fields[0].SourceName.Should().Be("Payload.Country");
            var encoder = CategoricalFieldEncoder.FromJson(fields[0].VocabularyJson);
            encoder.Reference.Label.Should().Be("GB");

            encoder.Indicators.Select(s => s.Name).Should().Equal("Payload.Country=NG", "Payload.Country=US",
                "Payload.Country=FR", "Payload.Country=RO", "Payload.Country=[Missing]");

            var variables = await db.ExhaustiveSearchInstanceVariable
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).OrderBy(o => o.VariableSequence).ToListAsync();
            variables.Should().HaveCount(7);
            variables.Where(v => v.CategoricalFieldId == null).Select(s => s.Name).Should().Contain("Abstraction.A");
            var indicators = variables.Where(v => v.CategoricalFieldId.HasValue).ToList();
            indicators.Should().HaveCount(5);
            indicators.Should().OnlyContain(v => v.CategoricalFieldId == fields[0].Id);
            indicators.Select(s => s.CategoricalIndicatorIndex).Should().Equal(0, 1, 2, 3, 4);
            indicators.Select(s => s.CategoricalKind).Should().Equal(1, 1, 1, 1, 3);
            indicators.Select(s => s.CategoricalLevel).Should().Equal("NG", "US", "FR", "RO", "[Missing]");
            indicators.Should().OnlyContain(v => v.NormalisationTypeId == 0 || v.NormalisationTypeId == 1);

            var promoted = await new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(db)
                .ExecuteByExhaustiveSearchInstanceIdAsync(instance.Id);

            var rows = promoted.ToList();
            rows.Should().NotBeEmpty();

            rows.Where(w => w.CategoricalFieldId.HasValue).Should().OnlyContain(v =>
                v.CategoricalSourceName == "Payload.Country" &&
                v.CategoricalVocabularyJson == fields[0].VocabularyJson);

            var helper = new JsonSerializationHelper();

            var trial = await db.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == instance.Id
                            && w.Active == 1)
                .OrderByDescending(o => o.Id).FirstAsync();

            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(
                await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instance.Id),
                new ExhaustiveSearchInstancePromotedTrialInstance { Json = trial.Json },
                rows, helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.NetworkVariablesInOrder.Should().HaveCount(rows.Count);
            var categorical = model.NetworkVariablesInOrder.Where(w => w.IsCategorical).ToList();

            foreach (var variable in categorical)
            {
                variable.CategoricalEncoder!.ToJson().Should().Be(encoder.ToJson());
            }

            foreach (var country in new[] { "NG", "ng ", "US", "FR", "RO", "GB", "unseen", "", null })
            {
                var body = new JObject { ["Payload.Country"] = country, ["Abstraction.A"] = 3 };
                var inputs = ExhaustiveExtensions.BuildScoreInputs(model.NetworkVariablesInOrder, body);

                for (var i = 0; i < model.NetworkVariablesInOrder.Count; i++)
                {
                    var variable = model.NetworkVariablesInOrder[i];

                    if (variable.IsCategorical)
                    {
                        inputs[i].Should().Be(encoder.Indicator(country, variable.CategoricalIndicatorIndex));
                    }
                }

                double.IsFinite(model.TopologyNetwork.Compute(inputs)[0]).Should().BeTrue();
            }
        }
    }
}
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
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Extensions;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.Exhaustive.Models;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsRecallScoreTests
    {
        private static readonly TrainedFixture fixture = EngineModelFactory.Trained(21, ["Abstraction.A", "B"]);
        private static readonly TrainedFixture other = EngineModelFactory.Trained(22, ["Abstraction.A", "B"]);

        private static IReadOnlyList<AdvancedAnalyticsModelVariable> Variables(TrainedFixture trained)
        {
            return EngineModelFactory.Model("M", trained).VariablesInOrder;
        }

        private static double Score(TrainedFixture trained, string json, List<string>? trace = null)
        {
            return AdvancedAnalyticsExtensions.Score(trained.Recall.Predict, Variables(trained), JObject.Parse(json),
                trace == null ? null : trace.Add);
        }

        [Fact]
        public void ScoreAppliesTheRecallToTheRawInputsInVariableOrder()
        {
            var score = Score(fixture, "{\"Abstraction.A\": 0.4, \"B\": -0.2}");

            score.Should().Be(fixture.Recall.Predict([0.4, -0.2])[0]);
        }

        [Fact]
        public void ScoreMatchesByTheFullPrefixedKeyAndIgnoresExtraKeys()
        {
            var score = Score(fixture, "{\"A\": 99, \"Abstraction.A\": 0.4, \"B\": -0.2, \"Extra\": 5}");

            score.Should().Be(fixture.Recall.Predict([0.4, -0.2])[0]);
        }

        [Theory]
        [InlineData("{\"B\": -0.2}")]
        [InlineData("{\"Abstraction.A\": null, \"B\": -0.2}")]
        [InlineData("{\"Abstraction.A\": \"abc\", \"B\": -0.2}")]
        [InlineData("{\"abstraction.a\": 0.4, \"B\": -0.2}")]
        public void AMissingNullUnconvertibleOrDifferentlyCasedKeyIsPassedAsNaN(string json)
        {
            var trace = new List<string>();

            var score = Score(fixture, json, trace);

            score.Should().Be(fixture.Recall.Predict([double.NaN, -0.2])[0]);
            trace.Should().NotBeEmpty();
        }

        [Fact]
        public void APresentZeroAndANumericStringAreValues()
        {
            Score(fixture, "{\"Abstraction.A\": 0, \"B\": \"0.5\"}").Should()
                .Be(fixture.Recall.Predict([0, 0.5])[0]);
        }

        [Fact]
        public void AllMissingScoresTheImputedRowAndNothingIsRounded()
        {
            var score = Score(fixture, "{}");

            score.Should().Be(fixture.Recall.Predict([double.NaN, double.NaN])[0]);
            score.Should().NotBe(Math.Round(score, 2));
        }

        [Fact]
        public void ARecallThatThrowsPropagatesFromTheCoreRatherThanReturningZero()
        {
            var act = () => AdvancedAnalyticsExtensions.Score(_ => throw new ArgumentException("rejects missing"),
                Variables(fixture), JObject.Parse("{}"));

            act.Should().Throw<ArgumentException>();
        }

        private static EntityAnalysisModel EngineModel(params AdvancedAnalyticsModel[] models)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.PublishSnapshot(new ModelSnapshot { AdvancedAnalyticsModels = models });

            return entityAnalysisModel;
        }

        [Fact]
        public void RecallByGuidScoresTheKnownModel()
        {
            var guid = Guid.NewGuid();
            var known = EngineModelFactory.Model("Known", fixture, 5, 1, guid);
            var models = new[] { EngineModel(EngineModelFactory.Model("Other", other, 5, 2)), EngineModel(known) };

            var score = AdvancedAnalyticsExtensions.Recall(models, guid,
                JObject.Parse("{\"Abstraction.A\": 0.4, \"B\": -0.2}"));

            score.Should().Be(fixture.Recall.Predict([0.4, -0.2])[0]);
        }

        [Fact]
        public void RecallByGuidScoresTheRequestedModelAndNotTheOtherModel()
        {
            var guidOne = Guid.NewGuid();
            var guidTwo = Guid.NewGuid();

            var models = new[]
            {
                EngineModel(EngineModelFactory.Model("One", fixture, 5, 1, guidOne),
                    EngineModelFactory.Model("Two", other, 5, 2, guidTwo))
            };

            var body = JObject.Parse("{\"Abstraction.A\": 0.4, \"B\": -0.2}");

            var one = AdvancedAnalyticsExtensions.Recall(models, guidOne, body);
            var two = AdvancedAnalyticsExtensions.Recall(models, guidTwo, body);

            one.Should().Be(fixture.Recall.Predict([0.4, -0.2])[0]);
            two.Should().Be(other.Recall.Predict([0.4, -0.2])[0]);
            one.Should().NotBe(two);
        }

        [Fact]
        public void RecallByAnUnknownGuidRaisesKeyNotFound()
        {
            var models = new[] { EngineModel(EngineModelFactory.Model("One", fixture, 5, 1)) };

            var act = () => AdvancedAnalyticsExtensions.Recall(models, Guid.NewGuid(), new JObject());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void AnExhaustiveGuidIsNotFoundAmongAdvancedAnalyticsModels()
        {
            var exhaustive = new EntityAnalysisModel();
            var guid = Guid.NewGuid();

            exhaustive.Snapshot.ExhaustiveModels.Add(new ExhaustiveSearchInstance
                { Guid = guid });

            var act = () => AdvancedAnalyticsExtensions.Recall([exhaustive], guid, new JObject());

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void FindReturnsNullWhenNoModelHasTheGuid()
        {
            new[] { EngineModel() }.FindAdvancedAnalyticsModel(Guid.NewGuid()).Should().BeNull();
        }
    }
}
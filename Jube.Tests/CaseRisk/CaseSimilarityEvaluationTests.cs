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
using Jube.CaseRisk.Similarity;
using Jube.Dto.Options;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseSimilarityEvaluationTests(ITestOutputHelper output)
    {
        private static readonly DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static List<CaseSimilarityEvaluationCase> World(int count)
        {
            var random = new Random(20261114);
            var cases = new List<CaseSimilarityEvaluationCase>();

            for (var id = 0; id < count; id++)
            {
                var mule = random.NextDouble() < 0.35;
                var amount = Math.Pow(10, (mule ? 3.4 : 2.2) + random.NextDouble());

                var vector = new List<SimilarityComponent>
                {
                    new(CaseSimilarityKind.Rule, "Velocity"),
                    new(CaseSimilarityKind.Category, "Channel=" + (random.NextDouble() < 0.8 ? "Card" : "Web")),
                    new(CaseSimilarityKind.Entity, "TxnId=" + id),
                    new(CaseSimilarityKind.Measure, "Amount:sum=" + SimilarityBanding.Band(amount)),
                    new(CaseSimilarityKind.Entity,
                        mule ? "Device=ring" + random.Next(6) : "Device=own" + random.Next(40))
                };

                if (mule || random.NextDouble() < 0.1)
                {
                    vector.Add(new SimilarityComponent(CaseSimilarityKind.Rule, "RapidOutflow"));
                }

                var confirmed = random.NextDouble() < (mule ? 0.85 : 0.08);
                var created = start.AddHours(id * 6);

                cases.Add(new CaseSimilarityEvaluationCase(id, vector, confirmed, created, created.AddDays(2)));
            }

            return cases;
        }

        [Fact]
        public void OnAWorldWithOneConfirmedTypologyTheVectorSeparatesOutcomesBetterThanSharingTheCommonRule()
        {
            var cases = World(600);

            var vector = CaseSimilarityEvaluation.LeaveOneOut(cases,
                CaseSimilarityEvaluation.ByVector(cases, new SimilaritySettings()));

            var rule = CaseSimilarityEvaluation.LeaveOneOut(cases,
                CaseSimilarityEvaluation.ByShared(CaseSimilarityKind.Rule));

            output.WriteLine($"vector AUC {vector.Auc:0.000} over {vector.Scored} (abstained {vector.Abstained}); " +
                             $"same rule AUC {rule.Auc:0.000} over {rule.Scored} (abstained {rule.Abstained})");

            vector.Auc.Should().BeGreaterThan(0.8);
            vector.Auc.Should().BeGreaterThan(rule.Auc + 0.1);
            vector.Scored.Should().BeGreaterThan(cases.Count / 2);
        }

        [Fact]
        public void OnlyCasesClosedBeforeTheTargetWasCreatedMayPredictIt()
        {
            var shared = new SimilarityComponent(CaseSimilarityKind.Rule, "R");

            var cases = new List<CaseSimilarityEvaluationCase>
            {
                new(1, [shared], true, start, start.AddDays(1)),
                new(2, [shared], true, start.AddDays(2), start.AddDays(3)),
                new(3, [shared], false, start.AddHours(12), start.AddDays(5))
            };

            var outcome = CaseSimilarityEvaluation.LeaveOneOut(cases,
                CaseSimilarityEvaluation.ByShared(CaseSimilarityKind.Rule));

            outcome.Scored.Should().Be(1);
            outcome.Abstained.Should().Be(2);
        }

        [Fact]
        public void TheAreaUnderTheCurveIsOneForAPerfectRankingAndAHalfForTies()
        {
            CaseSimilarityEvaluation.Auc([(0.9, true), (0.8, true), (0.1, false)]).Should().Be(1);
            CaseSimilarityEvaluation.Auc([(0.5, true), (0.5, false)]).Should().Be(0.5);
            CaseSimilarityEvaluation.Auc([(0.1, true), (0.9, false)]).Should().Be(0);
        }
    }
}

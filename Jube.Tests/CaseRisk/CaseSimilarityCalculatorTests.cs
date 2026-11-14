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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Similarity;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseSimilarityCalculatorTests
    {
        private const int TotalCases = 100;

        private static readonly SimilaritySettings settings = new();

        private static readonly SimilarityComponent ruleA = new(CaseSimilarityKind.Rule, "A");
        private static readonly SimilarityComponent ruleB = new(CaseSimilarityKind.Rule, "B");
        private static readonly SimilarityComponent ruleC = new(CaseSimilarityKind.Rule, "C");
        private static readonly SimilarityComponent device = new(CaseSimilarityKind.Entity, "Device=1");
        private static readonly SimilarityComponent card = new(CaseSimilarityKind.Category, "Channel=Card");
        private static readonly SimilarityComponent web = new(CaseSimilarityKind.Category, "Channel=Web");
        private static readonly SimilarityComponent unique = new(CaseSimilarityKind.Entity, "TxnId=1");

        private static readonly Dictionary<SimilarityComponent, int> caseCounts = new()
        {
            [ruleA] = 50,
            [ruleB] = 4,
            [ruleC] = 9,
            [device] = 2,
            [card] = 90,
            [web] = 9,
            [unique] = 1
        };

        private static double Weight(SimilarityComponent component) =>
            SimilarityWeight.Of(caseCounts[component], TotalCases, settings);

        [Fact]
        public void TheWorkedExampleScoresEachKindAndTheWhole()
        {
            var result = SimilarityCalculator.Compare([ruleA, ruleB, device, card], [ruleA, ruleB, ruleC, device, web],
                Weight, settings);

            var rule = result.Kinds.Single(kind => kind.Kind == CaseSimilarityKind.Rule);
            var entity = result.Kinds.Single(kind => kind.Kind == CaseSimilarityKind.Entity);
            var category = result.Kinds.Single(kind => kind.Kind == CaseSimilarityKind.Category);

            rule.SharedWeight.Should().BeApproximately(3.68898, 0.0001);
            rule.TotalWeight.Should().BeApproximately(6.00152, 0.0001);
            rule.Score.Should().BeApproximately(0.61467, 0.0001);
            rule.Shared.Should().Equal(ruleB, ruleA);
            entity.Score.Should().BeApproximately(1, 0.0001);
            category.Score.Should().Be(0);
            result.Score.Should().BeApproximately(0.69200, 0.0001);
            result.Band.Should().Be(CaseSimilarityBand.Strong);
        }

        [Fact]
        public void ARuleOnHalfOfAllCasesAloneNeverMakesTwoCasesSimilar()
        {
            var result = SimilarityCalculator.Compare([ruleA], [ruleA], Weight, settings);

            result.Score.Should().Be(1);
            result.Band.Should().Be(CaseSimilarityBand.Weak);
        }

        [Fact]
        public void AValueOnlyOneCaseCarriesWeighsNothingAndDoesNotDiluteTheScore()
        {
            var plain = SimilarityCalculator.Compare([ruleB, device], [ruleB, device], Weight, settings);
            var noisy = SimilarityCalculator.Compare([ruleB, device, unique], [ruleB, device], Weight, settings);

            Weight(unique).Should().Be(0);
            noisy.Score.Should().Be(plain.Score);
        }

        [Fact]
        public void AKindOnlyOneCaseHasIsLeftOutOfTheMean()
        {
            var result = SimilarityCalculator.Compare([ruleB, device], [ruleB], Weight, settings);

            result.Kinds.Select(kind => kind.Kind).Should().Equal(CaseSimilarityKind.Rule);
            result.Score.Should().Be(1);
        }

        [Fact]
        public void CasesWithNothingInCommonScoreZero()
        {
            var result = SimilarityCalculator.Compare([ruleB], [ruleC], Weight, settings);

            result.Score.Should().Be(0);
            result.Band.Should().Be(CaseSimilarityBand.Weak);
        }

        [Theory]
        [InlineData(630, "+e2.5")]
        [InlineData(999, "+e2.5")]
        [InlineData(1000, "+e3.0")]
        [InlineData(0.5, "+e-0.5")]
        [InlineData(0, "0")]
        public void AmountsFallIntoHalfDecadeBands(double value, string band)
        {
            SimilarityBanding.Band(value).Should().Be(band);
        }

        [Fact]
        public void NegativeAmountsBandBySizeWithTheirSign()
        {
            SimilarityBanding.Band(-630).Should().Be("-e2.5");
        }

        [Theory]
        [InlineData(0, "q0")]
        [InlineData(0.26, "q1")]
        [InlineData(0.99, "q3")]
        [InlineData(1, "q3")]
        [InlineData(630, "+e2.5")]
        public void AScoreBetweenZeroAndOneFallsIntoAQuarterAndAnythingElseIntoASizeBand(double value, string band)
        {
            SimilarityBanding.Share(value).Should().Be(band);
        }

        [Fact]
        public void AMeasureBecomesItsSumMeanAndMaximumBands()
        {
            var components = SimilarityVectorBuilder.Measures(CaseSimilarityKind.Measure, "Amount", [600, 30, 3000])
                .Select(component => component.Value);

            components.Should().Equal("Amount:sum=+e3.5", "Amount:mean=+e3.0", "Amount:max=+e3.0");
        }

        [Fact]
        public void AFieldWithMoreDistinctValuesThanTheCapContributesNothing()
        {
            var many = Enumerable.Range(0, settings.MaximumValuesPerField + 1).Select(index => index.ToString());

            SimilarityVectorBuilder.Values(CaseSimilarityKind.Entity, "TxnId", many, settings).Should().BeEmpty();

            SimilarityVectorBuilder.Values(CaseSimilarityKind.Category, "Channel", ["Card", "Card", "", null, "Web"],
                settings).Select(component => component.Value).Should().Equal("Channel=Card", "Channel=Web");
        }
    }
}

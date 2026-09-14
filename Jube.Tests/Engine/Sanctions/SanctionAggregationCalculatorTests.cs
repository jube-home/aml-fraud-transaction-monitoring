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
using FluentAssertions;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionAggregationCalculatorTests
    {
        private const byte Sum = 1;
        private const byte Average = 2;
        private const byte Count = 3;
        private const byte Max = 4;
        private const byte Min = 5;
        private const byte First = 6;
        private const byte Last = 7;
        private const byte Confidence = 8;

        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<SanctionEntryReturn> empty = [];

        private static List<SanctionEntryReturn> Returns(params int[] distances)
        {
            return
            [
                .. distances.Select((d, i) => new SanctionEntryReturn
                {
                    LevenshteinDistance = d,
                    SanctionEntry = new SanctionEntry
                    {
                        SanctionEntryId = i + 1,
                        SanctionEntrySourceId = 1,
                        SanctionElementValue = ["entry", i.ToString()],
                        SanctionEntryReference = $"REF{i}"
                    }
                })
            ];
        }

        [Fact]
        public void CalculateSumWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateSum(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateSumAddsAllDistances()
        {
            SanctionAggregationCalculator.CalculateSum(Returns(1, 2, 3)).Should().Be(6);
        }

        [Fact]
        public void CalculateSumWithSingleZeroDistanceReturnsZeroNotNull()
        {
            var result = SanctionAggregationCalculator.CalculateSum(Returns(0));
            result.Should().NotBeNull();
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateCountWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateCount(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateCountReturnsNumberOfMatches()
        {
            SanctionAggregationCalculator.CalculateCount(Returns(5, 1, 3, 0)).Should().Be(4);
        }

        [Fact]
        public void CalculateMaxWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateMax(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateMaxReturnsWorstMatch()
        {
            SanctionAggregationCalculator.CalculateMax(Returns(5, 1, 3)).Should().Be(5);
        }

        [Fact]
        public void CalculateMinWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateMin(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateMinReturnsBestMatch()
        {
            SanctionAggregationCalculator.CalculateMin(Returns(5, 1, 3)).Should().Be(1);
        }

        [Fact]
        public void CalculateFirstWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateFirst(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateFirstReturnsElementAtIndexZeroRegardlessOfValue()
        {
            SanctionAggregationCalculator.CalculateFirst(Returns(5, 1, 3)).Should().Be(5);
        }

        [Fact]
        public void CalculateLastWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateLast(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateLastReturnsFinalElementRegardlessOfValue()
        {
            SanctionAggregationCalculator.CalculateLast(Returns(5, 1, 3)).Should().Be(3);
        }

        [Fact]
        public void CalculateFirstAndLastAreSameElementForSingleMatch()
        {
            var returns = Returns(4);
            SanctionAggregationCalculator.CalculateFirst(returns).Should()
                .Be(SanctionAggregationCalculator.CalculateLast(returns));
        }

        [Fact]
        public void CalculateAverageWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateAverage(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateAverageDividesSumByCount()
        {
            SanctionAggregationCalculator.CalculateAverage(Returns(1, 2, 3)).Should().Be(2);
        }

        [Fact]
        public void CalculateAverageWithAllZeroDistancesReturnsZeroNotNull()
        {
            var result = SanctionAggregationCalculator.CalculateAverage(Returns(0, 0, 0));
            result.Should().NotBeNull();
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateAverageWithNonIntegerResultIsNotTruncated()
        {
            SanctionAggregationCalculator.CalculateAverage(Returns(1, 2)).Should().Be(1.5);
        }

        [Theory]
        [InlineData(Sum, 6d)]
        [InlineData(Average, 2d)]
        [InlineData(Count, 3d)]
        [InlineData(Max, 3d)]
        [InlineData(Min, 1d)]
        [InlineData(First, 1d)]
        [InlineData(Last, 3d)]
        public void CalculateDispatchesToTheCorrectAggregationForEachKnownTypeId(byte typeId, double expected)
        {
            var result = SanctionAggregationCalculator.Calculate(typeId, Returns(1, 2, 3));
            result.Should().Be(expected);
        }

        [Fact]
        public void CalculateWithUnknownAggregationTypeIdFallsBackToAverage()
        {
            var result = SanctionAggregationCalculator.Calculate(99, Returns(1, 2, 3));
            result.Should().Be(SanctionAggregationCalculator.CalculateAverage(Returns(1, 2, 3)));
        }

        [Fact]
        public void CalculateWithZeroAggregationTypeIdFallsBackToAverage()
        {
            var result = SanctionAggregationCalculator.Calculate(0, Returns(4, 8));
            result.Should().Be(6);
        }

        [Fact]
        public void CalculateWithConfidenceTypeIdDispatchesToConfidence()
        {
            var result = SanctionAggregationCalculator.Calculate(Confidence, Returns(1, 3));
            result.Should().Be(SanctionAggregationCalculator.CalculateConfidence(Returns(1, 3)));
        }

        [Fact]
        public void CalculateConfidenceWithEmptyCollectionReturnsNull()
        {
            SanctionAggregationCalculator.CalculateConfidence(empty).Should().BeNull();
        }

        [Fact]
        public void CalculateConfidenceWithSingleMatchIsPureClosenessOfThatDistance()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(2));
            result.Should().BeApproximately(0.3333, 1e-4);
        }

        [Fact]
        public void CalculateConfidenceWithSingleExactMatchIsOne()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(0));
            result.Should().Be(1.0);
        }

        [Fact]
        public void CalculateConfidenceWithTwoIdenticalDistancesShortCircuitsOnZeroStdDevToCloseness()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(4, 4));
            result.Should().Be(0.2);
        }

        [Fact]
        public void CalculateConfidenceWithExactlyTwoDistinctDistancesIsFiniteNotNaN()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(1, 3));

            result.Should().NotBeNull();
            double.IsNaN(result!.Value).Should().BeFalse();
            result.Value.Should().BeApproximately(0.1558, 1e-4);
        }

        [Fact]
        public void CalculateConfidenceWithTwoDistinctDistancesIsSymmetricUnderReordering()
        {
            var forward = SanctionAggregationCalculator.CalculateConfidence(Returns(1, 3));
            var backward = SanctionAggregationCalculator.CalculateConfidence(Returns(3, 1));

            forward.Should().Be(backward);
        }

        [Fact]
        public void CalculateConfidenceWithTwoDistinctDistancesWidelySeparatedIsHigherThanCloselySeparated()
        {
            var wide = SanctionAggregationCalculator.CalculateConfidence(Returns(0, 10));
            var narrow = SanctionAggregationCalculator.CalculateConfidence(Returns(1, 3));

            wide.Should().BeApproximately(0.3115, 1e-4);
            wide.Should().BeGreaterThan(narrow!.Value);
        }

        [Fact]
        public void CalculateConfidenceWithThreeMatchesUsesSkewAdjustedReliability()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(0, 2, 5));
            result.Should().BeApproximately(0.3606, 1e-4);
        }

        [Fact]
        public void CalculateConfidenceWithFiveEvenlySpacedMatches()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(0, 1, 2, 3, 4));
            result.Should().BeApproximately(0.4088, 1e-4);
        }

        [Fact]
        public void CalculateConfidenceWithTwoNearDuplicatesAndOneOutlierIsLowerThanIsolatedMatch()
        {
            var ambiguous = SanctionAggregationCalculator.CalculateConfidence(Returns(0, 0, 1));
            var isolated = SanctionAggregationCalculator.CalculateConfidence(Returns(0));

            ambiguous.Should().BeApproximately(0.2844, 1e-4);
            ambiguous.Should().BeLessThan(isolated!.Value);
        }

        [Fact]
        public void CalculateConfidenceWithManyFarMatchesAndOneExactMatchIsHigh()
        {
            var distances = new List<int> { 0 };
            distances.AddRange(Enumerable.Repeat(10, 11));

            var result = SanctionAggregationCalculator.CalculateConfidence(Returns([.. distances]));
            result.Should().BeApproximately(0.8423, 1e-4);
        }

        [Fact]
        public void CalculateConfidenceIsRoundedToFourDecimalPlaces()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(0, 2, 5));
            var scaled = result!.Value * 10000d;
            scaled.Should().BeApproximately(Math.Round(scaled), 1e-9);
        }

        [Fact]
        public void CalculateConfidenceNeverExceedsOne()
        {
            foreach (var distances in new[] { new[] { 0 }, new[] { 0, 0 }, new[] { 0, 0, 0 }, new[] { 0, 1, 2, 3 } })
            {
                var result = SanctionAggregationCalculator.CalculateConfidence(Returns(distances));
                result.Should().NotBeNull();
                result!.Value.Should().BeLessThanOrEqualTo(1.0);
            }
        }

        [Fact]
        public void CalculateConfidenceIsNeverNegative()
        {
            var result = SanctionAggregationCalculator.CalculateConfidence(Returns(50, 1, 2, 3, 4, 5, 6, 7));
            result.Should().NotBeNull();
            result!.Value.Should().BeGreaterThanOrEqualTo(0.0);
        }

        [Fact]
        public void CalculateConfidenceIsNeverNaNAcrossASweepOfClusterSizes()
        {
            for (var n = 1; n <= 15; n++)
            {
                var distances = Enumerable.Range(0, n).Select(i => i % 4).ToArray();
                var result = SanctionAggregationCalculator.CalculateConfidence(Returns(distances));

                result.Should().NotBeNull($"n={n} should always produce a value once there is at least one match");
                double.IsNaN(result!.Value).Should().BeFalse($"n={n} must never produce NaN");
                double.IsInfinity(result.Value).Should().BeFalse($"n={n} must never produce Infinity");
            }
        }
    }
}
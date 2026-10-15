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
using Jube.Engine.AdvancedAnalytics;
using Newtonsoft.Json;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    [Trait("Category", "Unit")]
    [Trait("Covers", "EdgeCases")]
    public sealed class VariableTransformationApplierEdgeCaseTests
    {
        private static string Json(object parameters) => JsonConvert.SerializeObject(parameters);

        [Theory]
        [InlineData(VariableTransformationKind.ZScoreStandard)]
        [InlineData(VariableTransformationKind.MinMax)]
        [InlineData(VariableTransformationKind.RobustMad)]
        [InlineData(VariableTransformationKind.RobustIqr)]
        [InlineData(VariableTransformationKind.MaxAbs)]
        public void AZeroScaleNeverDividesByZeroAndReturnsZeroInstead(VariableTransformationKind kind)
        {
            var actual = VariableTransformationApplier.ApplyNumeric(kind, Json(new { center = 5.0, scale = 0.0 }),
                12.0);

            actual.Should().Be(0.0);
            double.IsNaN(actual).Should().BeFalse();
            double.IsInfinity(actual).Should().BeFalse();
        }

        [Theory]
        [InlineData(VariableTransformationKind.ZScoreStandard)]
        [InlineData(VariableTransformationKind.MinMax)]
        [InlineData(VariableTransformationKind.RobustMad)]
        [InlineData(VariableTransformationKind.RobustIqr)]
        [InlineData(VariableTransformationKind.MaxAbs)]
        public void ANonFiniteScaleNeverThrowsAndReturnsZeroInstead(VariableTransformationKind kind)
        {
            var actual = VariableTransformationApplier.ApplyNumeric(kind, "{\"center\":5,\"scale\":NaN}", 12.0);

            actual.Should().Be(0.0);

            var infinite = VariableTransformationApplier.ApplyNumeric(kind, "{\"center\":5,\"scale\":Infinity}",
                12.0);

            infinite.Should().Be(0.0);
        }

        [Fact]
        public void LogOfZeroIsNaNNotAnException()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Log,
                Json(new { offset = 0 }), 0.0);

            double.IsNaN(actual).Should().BeTrue();
        }

        [Fact]
        public void LogOfANegativeValueIsNaNNotAnException()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Log,
                Json(new { offset = 0 }), -5.0);

            double.IsNaN(actual).Should().BeTrue();
        }

        [Fact]
        public void SqrtOfANegativeValueIsNaNNotAnException()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Sqrt,
                Json(new { offset = 0 }), -9.0);

            double.IsNaN(actual).Should().BeTrue();
        }

        [Fact]
        public void ReciprocalOfZeroIsNaNNotAnException()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Reciprocal,
                Json(new { offset = 0 }), 0.0);

            double.IsNaN(actual).Should().BeTrue();
        }

        [Fact]
        public void SquareAndAbsoluteValueAreAlwaysDefinedForNegativeInputs()
        {
            VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Square, Json(new { offset = 0 }),
                -4.0).Should().Be(16.0);
            VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.AbsoluteValue,
                Json(new { offset = 0 }), -4.0).Should().Be(4.0);
        }

        [Theory]
        [InlineData(-27.0)]
        [InlineData(0.0)]
        [InlineData(27.0)]
        public void SignedLog1PIsDefinedForEveryRealValueIncludingNegatives(double value)
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.SignedLog1P,
                Json(new { offset = 0 }), value);

            double.IsNaN(actual).Should().BeFalse();
            double.IsInfinity(actual).Should().BeFalse();
        }

        [Theory]
        [InlineData(-27.0)]
        [InlineData(0.0)]
        [InlineData(27.0)]
        public void SignedSqrtIsDefinedForEveryRealValueIncludingNegatives(double value)
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.SignedSqrt,
                Json(new { offset = 0 }), value);

            double.IsNaN(actual).Should().BeFalse();
            double.IsInfinity(actual).Should().BeFalse();
        }

        [Fact]
        public void CubeRootIsDefinedForNegativeValuesUnlikeSquareRoot()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.CubeRoot,
                Json(new { offset = 0 }), -8.0);

            actual.Should().BeApproximately(-2.0, 1e-9);
        }

        [Theory]
        [InlineData("not valid json at all")]
        [InlineData("{\"scale\":")]
        [InlineData("null")]
        [InlineData("42")]
        [InlineData("[1,2,3]")]
        [InlineData("{\"center\":\"not-a-number\",\"scale\":\"also-not-a-number\"}")]
        public void MalformedParametersJsonForAScalingKindNeverThrowsAndFallsBackToTheRawValue(string parametersJson)
        {
            var act = () => VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.ZScoreStandard,
                parametersJson, 42.0);

            act.Should().NotThrow();
            act().Should().Be(42.0);
        }

        [Theory]
        [InlineData("not valid json at all")]
        [InlineData("{\"edges\":")]
        public void UnparsableParametersJsonForADiscretisationKindNeverThrowsAndBehavesAsNoEdges(
            string parametersJson)
        {
            var act = () => VariableTransformationApplier.ApplyNumeric(
                VariableTransformationKind.DiscretiseCustomEdges, parametersJson, 42.0);

            act.Should().NotThrow();
            act().Should().Be(0.0);
        }

        [Theory]
        [InlineData("{\"edges\":[\"a\",\"b\",\"c\"]}")]
        [InlineData("{\"edges\":[{\"nested\":true}]}")]
        public void StructurallyValidButWrongTypedEdgesNeverThrowsAndFallsBackToTheRawValue(string parametersJson)
        {
            var act = () => VariableTransformationApplier.ApplyNumeric(
                VariableTransformationKind.DiscretiseCustomEdges, parametersJson, 42.0);

            act.Should().NotThrow();
            act().Should().Be(42.0);
        }

        [Theory]
        [InlineData("not valid json at all")]
        [InlineData("{\"categories\":[1,2,3],\"values\":\"nope\"}")]
        [InlineData("[1,2,3]")]
        [InlineData("null")]
        public void MalformedParametersJsonForACategoricalKindNeverThrowsAndFallsBackToZero(string parametersJson)
        {
            var act = () => VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.WeightOfEvidence,
                parametersJson, "AnyCategory");

            act.Should().NotThrow();
            act().Should().Be(0.0);
        }

        [Fact]
        public void MalformedParametersJsonForAOneHotSlotNeverThrowsAndFallsBackToZero()
        {
            var act = () => VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.OneHotIndicator,
                "{\"category\":123}", "US");

            act.Should().NotThrow();
            act().Should().Be(0.0);
        }

        [Theory]
        [InlineData(VariableTransformationKind.LabelOrdinal)]
        [InlineData(VariableTransformationKind.OneHotIndicator)]
        [InlineData(VariableTransformationKind.TargetMean)]
        [InlineData(VariableTransformationKind.WeightOfEvidence)]
        [InlineData(VariableTransformationKind.Frequency)]
        public void AnUnseenCategoryNeverThrowsForAnyCategoricalTransformationKind(VariableTransformationKind kind)
        {
            var parametersJson = kind switch
            {
                VariableTransformationKind.LabelOrdinal => Json(new
                    { categories = new[] { "A", "B" }, unknownCode = -1 }),
                VariableTransformationKind.OneHotIndicator => Json(new { category = "A" }),
                _ => Json(new
                {
                    categories = new[] { "A", "B" }, values = new[] { 0.25, 0.75 }, defaultValue = 0.5
                })
            };

            var act = () => VariableTransformationApplier.ApplyCategorical(kind, parametersJson, "NeverSeenBefore");

            act.Should().NotThrow();
        }

        [Fact]
        public void LabelOrdinalOfAnUnseenCategoryReturnsTheUnknownCode()
        {
            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.LabelOrdinal,
                Json(new { categories = new[] { "A", "B" }, unknownCode = -1 }), "NeverSeenBefore");

            actual.Should().Be(-1);
        }

        [Fact]
        public void OneHotIndicatorOfAnUnseenCategoryReturnsZero()
        {
            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.OneHotIndicator,
                Json(new { category = "A" }), "NeverSeenBefore");

            actual.Should().Be(0);
        }

        [Fact]
        public void TargetMeanOfAnUnseenCategoryReturnsTheDefaultValue()
        {
            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.TargetMean,
                Json(new { categories = new[] { "A", "B" }, values = new[] { 0.1, 0.9 }, defaultValue = 0.42 }),
                "NeverSeenBefore");

            actual.Should().Be(0.42);
        }

        [Fact]
        public void WeightOfEvidenceOfAnUnseenCategoryReturnsTheDefaultValue()
        {
            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.WeightOfEvidence,
                Json(new { categories = new[] { "A", "B" }, values = new[] { -0.3, 0.6 }, defaultValue = 0.0 }),
                "NeverSeenBefore");

            actual.Should().Be(0.0);
        }

        [Fact]
        public void FrequencyOfAnUnseenCategoryReturnsTheDefaultValue()
        {
            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.Frequency,
                Json(new { categories = new[] { "A", "B" }, values = new[] { 0.4, 0.6 }, defaultValue = 0.0 }),
                "NeverSeenBefore");

            actual.Should().Be(0.0);
        }

        [Fact]
        public void CallingApplyNumericWithACategoricalKindThrowsALoudProgrammingError()
        {
            var act = () => VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.WeightOfEvidence,
                "{}", 1.0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CallingApplyCategoricalWithANumericKindThrowsALoudProgrammingError()
        {
            var act = () => VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.Log, "{}",
                "text");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TheApplierIsPureAndSafeUnderConcurrentUseAcrossManyThreads()
        {
            var kinds = new[]
            {
                (VariableTransformationKind.Log, Json(new { offset = 0 }), 10.0),
                (VariableTransformationKind.ZScoreStandard, Json(new { center = 5.0, scale = 2.0 }), 9.0),
                (VariableTransformationKind.Winsorise, Json(new { lowerBound = 0.0, upperBound = 10.0 }), 55.0),
                (VariableTransformationKind.DiscretiseCustomEdges, Json(new { edges = new[] { 10.0, 20.0 } }), 15.0)
            };

            var iterations = Enumerable.Range(0, 2000).ToArray();

            Parallel.ForEach(iterations, i =>
            {
                var (kind, parameters, value) = kinds[i % kinds.Length];
                var result = VariableTransformationApplier.ApplyNumeric(kind, parameters, value);
                double.IsNaN(result).Should().BeFalse();
            });

            foreach (var (kind, parameters, value) in kinds)
            {
                var first = VariableTransformationApplier.ApplyNumeric(kind, parameters, value);
                var second = VariableTransformationApplier.ApplyNumeric(kind, parameters, value);
                first.Should().Be(second);
            }
        }
    }
}
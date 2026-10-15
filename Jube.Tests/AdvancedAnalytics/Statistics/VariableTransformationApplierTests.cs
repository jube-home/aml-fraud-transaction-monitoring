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
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling;
using Newtonsoft.Json;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    [Trait("Category", "Unit")]
    public sealed class VariableTransformationApplierTests
    {
        private static readonly ScalingTools scaling = new();
        private static readonly BinningTools binning = new();
        private static readonly EncodingTools encoding = new();
        private static readonly FeatureEngineeringTools engineering = new();
        private static string Json(object parameters) => JsonConvert.SerializeObject(parameters);

        [Theory]
        [InlineData(ColumnFunction.NaturalLog, VariableTransformationKind.Log)]
        [InlineData(ColumnFunction.Log10, VariableTransformationKind.Log10)]
        [InlineData(ColumnFunction.Log2, VariableTransformationKind.Log2)]
        [InlineData(ColumnFunction.Log1P, VariableTransformationKind.Log1P)]
        [InlineData(ColumnFunction.SignedLog1P, VariableTransformationKind.SignedLog1P)]
        [InlineData(ColumnFunction.Sqrt, VariableTransformationKind.Sqrt)]
        [InlineData(ColumnFunction.SignedSqrt, VariableTransformationKind.SignedSqrt)]
        [InlineData(ColumnFunction.CubeRoot, VariableTransformationKind.CubeRoot)]
        [InlineData(ColumnFunction.Reciprocal, VariableTransformationKind.Reciprocal)]
        [InlineData(ColumnFunction.Square, VariableTransformationKind.Square)]
        [InlineData(ColumnFunction.AbsoluteValue, VariableTransformationKind.AbsoluteValue)]
        public void ColumnFunctionsMatchScalingToolsApplyColumnFunction(ColumnFunction function,
            VariableTransformationKind kind)
        {
            double[] values = [2.5, -1.5, 0.5, 4.0];
            var expected = scaling.ApplyColumnFunction([.. values.Select(v => new[] { v })], function).Table;
            var parameters = Json(new { offset = 0 });

            for (var i = 0; i < values.Length; i++)
            {
                var actual = VariableTransformationApplier.ApplyNumeric(kind, parameters, values[i]);
                var same = expected[i][0] == actual || (double.IsNaN(expected[i][0]) && double.IsNaN(actual));
                same.Should().BeTrue($"value {values[i]} under {kind}");
            }
        }

        [Fact]
        public void BoxCoxMatchesScalingToolsApplyPowerTransformer()
        {
            double[] fitData = [1.0, 2.0, 3.0, 4.0, 5.0, 8.0];

            var fitted = scaling.FitPowerTransformer([.. fitData.Select(v => new[] { v })],
                method: PowerTransformMethod.BoxCox);

            var lambda = fitted.Lambdas[0];

            double testValue = 6.0;

            var expected = scaling.ApplyPowerTransformer([[testValue]], [lambda],
                PowerTransformMethod.BoxCox).Table[0][0];

            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.BoxCox,
                Json(new { lambda }), testValue);

            actual.Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void YeoJohnsonMatchesScalingToolsApplyPowerTransformer()
        {
            double[] fitData = [-3.0, -1.0, 0.0, 2.0, 5.0, 8.0];

            var fitted = scaling.FitPowerTransformer([.. fitData.Select(v => new[] { v })],
                method: PowerTransformMethod.YeoJohnson);

            var lambda = fitted.Lambdas[0];

            double testValue = -2.0;

            var expected = scaling.ApplyPowerTransformer([[testValue]], [lambda]).Table[0][0];

            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.YeoJohnson,
                Json(new { lambda }), testValue);

            actual.Should().BeApproximately(expected, 1e-9);
        }

        [Theory]
        [InlineData(TableScalingMethod.Standard, VariableTransformationKind.ZScoreStandard)]
        [InlineData(TableScalingMethod.MinMax, VariableTransformationKind.MinMax)]
        [InlineData(TableScalingMethod.RobustMad, VariableTransformationKind.RobustMad)]
        [InlineData(TableScalingMethod.RobustIqr, VariableTransformationKind.RobustIqr)]
        [InlineData(TableScalingMethod.MaxAbs, VariableTransformationKind.MaxAbs)]
        public void TableScalingMatchesScalingToolsApplyTableScaler(TableScalingMethod method,
            VariableTransformationKind kind)
        {
            double[] fitData = [1.0, 4.0, 9.0, 16.0, 25.0];
            var fitted = scaling.FitTableScaler([.. fitData.Select(v => new[] { v })], method: method);
            var center = fitted.Centers[0];
            var scale = fitted.Scales[0];

            double testValue = 12.0;
            var expected = scaling.ApplyTableScaler([[testValue]], [center], [scale])[0][0];

            var actual = VariableTransformationApplier.ApplyNumeric(kind, Json(new { center, scale }), testValue);

            actual.Should().BeApproximately(expected, 1e-9);
        }

        [Theory]
        [InlineData(QuantileOutputDistribution.Uniform, VariableTransformationKind.QuantileUniform)]
        [InlineData(QuantileOutputDistribution.Normal, VariableTransformationKind.QuantileNormal)]
        public void QuantileTransformMatchesScalingToolsApplyQuantileTransformer(QuantileOutputDistribution output,
            VariableTransformationKind kind)
        {
            double[] fitData = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0];
            var fitted = scaling.FitQuantileTransformer([.. fitData.Select(v => new[] { v })], output: output);
            var references = fitted.ReferenceQuantiles[0];

            double testValue = 6.5;
            var expected = scaling.ApplyQuantileTransformer([[testValue]], [references], output)[0][0];

            var actual = VariableTransformationApplier.ApplyNumeric(kind, Json(new { referenceQuantiles = references }),
                testValue);

            actual.Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void WinsoriseMatchesScalingToolsApplyOutlierClipper()
        {
            double[] fitData = [1.0, 2.0, 3.0, 4.0, 5.0, 100.0];
            var fitted = scaling.FitOutlierClipper([.. fitData.Select(v => new[] { v })]);
            var lower = fitted.LowerBounds[0];
            var upper = fitted.UpperBounds[0];

            double testValue = 250.0;
            var expected = scaling.ApplyOutlierClipper([[testValue]], [lower], [upper]).Table[0][0];

            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.Winsorise,
                Json(new { lowerBound = lower, upperBound = upper }), testValue);

            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData(VariableTransformationKind.DiscretiseEqualWidth)]
        [InlineData(VariableTransformationKind.DiscretiseEqualFrequency)]
        [InlineData(VariableTransformationKind.DiscretiseKMeans)]
        [InlineData(VariableTransformationKind.DiscretiseCustomEdges)]
        public void DiscretisationMatchesBinningToolsApplyBinEdges(VariableTransformationKind kind)
        {
            double[] fitData = [1.0, 2.0, 3.0, 10.0, 11.0, 12.0, 20.0, 21.0, 22.0];

            var edges = kind switch
            {
                VariableTransformationKind.DiscretiseEqualWidth => binning.DiscretiseEqualWidth(fitData, 3).Edges,
                VariableTransformationKind.DiscretiseEqualFrequency => binning.DiscretiseEqualFrequency(fitData, 3)
                    .Edges,
                VariableTransformationKind.DiscretiseKMeans => binning.DiscretiseKMeans(fitData, 3).Edges,
                _ => binning.DiscretiseCustomEdges(fitData, [5.0, 15.0]).Edges
            };

            double testValue = 11.5;
            var expected = binning.ApplyBinEdges([testValue], edges)[0];

            var actual = VariableTransformationApplier.ApplyNumeric(kind, Json(new { edges }), testValue);

            actual.Should().Be(expected);
        }

        [Fact]
        public void DiscretiseSupervisedMatchesBinningToolsApplyBinEdges()
        {
            double[] fitData = [1.0, 2.0, 3.0, 4.0, 10.0, 11.0, 12.0, 13.0];
            int[] labels = [0, 0, 0, 0, 1, 1, 1, 1];
            var edges = binning.DiscretiseSupervised(fitData, labels).Edges;

            double testValue = 7.0;
            var expected = binning.ApplyBinEdges([testValue], edges)[0];

            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.DiscretiseSupervised,
                Json(new { edges }), testValue);

            actual.Should().Be(expected);
        }

        [Fact]
        public void DiscretisationOfAMissingValueReturnsMinusOne()
        {
            var actual = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.DiscretiseCustomEdges,
                Json(new { edges = new[] { 5.0, 10.0 } }), double.NaN);

            actual.Should().Be(-1);
        }

        [Fact]
        public void CyclicalSinAndCosMatchFeatureEngineeringToolsEncodeCyclicalFeature()
        {
            double testValue = 6.0;
            var expected = engineering.EncodeCyclicalFeature([testValue]).Table[0];

            var sin = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.CyclicalSin,
                Json(new { period = 24 }), testValue);
            var cos = VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.CyclicalCos,
                Json(new { period = 24 }), testValue);

            sin.Should().BeApproximately(expected[0], 1e-9);
            cos.Should().BeApproximately(expected[1], 1e-9);
        }

        [Fact]
        public void LabelOrdinalMatchesEncodingToolsApplyLabelEncoder()
        {
            var fit = encoding.FitLabelEncoder([["a"], ["b"], ["c"]]);
            var categories = fit.Categories[0];

            var expected = encoding.ApplyLabelEncoder([["b"]], [categories])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.LabelOrdinal,
                Json(new { categories, unknownCode = -1 }), "b");

            actual.Should().Be(expected);
        }

        [Fact]
        public void LabelOrdinalOfAnUnseenCategoryMatchesTheUnknownCode()
        {
            var fit = encoding.FitLabelEncoder([["a"], ["b"], ["c"]]);
            var categories = fit.Categories[0];

            var expected = encoding.ApplyLabelEncoder([["z"]], [categories])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.LabelOrdinal,
                Json(new { categories, unknownCode = -1 }), "z");

            actual.Should().Be(expected);
        }

        [Fact]
        public void OneHotIndicatorMatchesEncodingToolsApplyOneHotEncoderForOneSelectedCategory()
        {
            var fit = encoding.FitOneHotEncoder([["US"], ["CA"], ["US"]]);
            var categories = fit.Categories;
            var featureIndex = Array.IndexOf(fit.FeatureNames, "column_0=US");

            var expected = encoding.ApplyOneHotEncoder([["US"]], categories)[0][featureIndex];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.OneHotIndicator,
                Json(new { category = "US" }), "US");

            actual.Should().Be(expected);
        }

        [Fact]
        public void TargetMeanMatchesEncodingToolsApplyCategoryEncoder()
        {
            var data = new[] { new[] { "A" }, new[] { "A" }, new[] { "B" }, new[] { "B" } };
            int[] labels = [1, 0, 0, 0];
            var fit = encoding.FitTargetMeanEncoder(data, labels);
            var table = fit.Tables[0];

            var expected = encoding.ApplyCategoryEncoder([["A"]], [table.Categories],
                [table.Values], [table.DefaultValue])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.TargetMean,
                Json(new { categories = table.Categories, values = table.Values, defaultValue = table.DefaultValue }),
                "A");

            actual.Should().Be(expected);
        }

        [Fact]
        public void WeightOfEvidenceMatchesEncodingToolsApplyCategoryEncoder()
        {
            var data = new[] { new[] { "A" }, new[] { "A" }, new[] { "B" }, new[] { "B" } };
            int[] labels = [1, 0, 0, 0];
            var fit = encoding.FitWeightOfEvidenceEncoder(data, labels);
            var table = fit.Tables[0];

            var expected = encoding.ApplyCategoryEncoder([["B"]], [table.Categories],
                [table.Values], [table.DefaultValue])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.WeightOfEvidence,
                Json(new { categories = table.Categories, values = table.Values, defaultValue = table.DefaultValue }),
                "B");

            actual.Should().Be(expected);
        }

        [Fact]
        public void FrequencyMatchesEncodingToolsApplyCategoryEncoder()
        {
            var data = new[] { new[] { "A" }, new[] { "A" }, new[] { "B" }, new[] { "C" } };
            var fit = encoding.FitFrequencyEncoder(data);
            var table = fit.Tables[0];

            var expected = encoding.ApplyCategoryEncoder([["A"]], [table.Categories],
                [table.Values], [table.DefaultValue])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.Frequency,
                Json(new { categories = table.Categories, values = table.Values, defaultValue = table.DefaultValue }),
                "A");

            actual.Should().Be(expected);
        }

        [Fact]
        public void ACategoricalTransformationOfAnUnseenCategoryFallsBackToTheDefaultValue()
        {
            var data = new[] { new[] { "A" }, new[] { "A" }, new[] { "B" }, new[] { "C" } };
            var fit = encoding.FitFrequencyEncoder(data);
            var table = fit.Tables[0];

            var expected = encoding.ApplyCategoryEncoder([["Z"]], [table.Categories],
                [table.Values], [table.DefaultValue])[0][0];

            var actual = VariableTransformationApplier.ApplyCategorical(VariableTransformationKind.Frequency,
                Json(new { categories = table.Categories, values = table.Values, defaultValue = table.DefaultValue }),
                "Z");

            actual.Should().Be(expected);
        }

        [Fact]
        public void NoneReturnsTheRawValueUnchanged()
        {
            VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.None, null, 3.75).Should().Be(3.75);

            double.IsNaN(VariableTransformationApplier.ApplyNumeric(VariableTransformationKind.None, null, double.NaN))
                .Should().BeTrue();
        }

        [Fact]
        public void EveryNumericTransformationOfAMissingValueIsNaNExceptDiscretisationWhichIsMinusOne()
        {
            foreach (VariableTransformationKind kind in Enum.GetValues(typeof(VariableTransformationKind)))
            {
                if (kind == VariableTransformationKind.None || VariableTransformationApplier.IsCategorical(kind))
                {
                    continue;
                }

                var result = VariableTransformationApplier.ApplyNumeric(kind, "{}", double.NaN);

                var isDiscretisation = kind is VariableTransformationKind.DiscretiseEqualWidth
                    or VariableTransformationKind.DiscretiseEqualFrequency
                    or VariableTransformationKind.DiscretiseKMeans
                    or VariableTransformationKind.DiscretiseSupervised
                    or VariableTransformationKind.DiscretiseCustomEdges;

                if (isDiscretisation)
                {
                    result.Should().Be(-1, kind.ToString());
                }
                else
                {
                    double.IsNaN(result).Should().BeTrue(kind.ToString());
                }
            }
        }
    }
}
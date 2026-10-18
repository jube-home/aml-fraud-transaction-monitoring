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
using Accord.Math;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class NumericsAndOptimisationSpecialFunctionToolsTests
    {
        private const double EulerMascheroni = 0.5772156649015329;
        private readonly SpecialFunctionTools tools = new();

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.GammaFunction")]
        public void GammaFunctionKnownValues()
        {
            var result = tools.GammaFunction([5.0, 0.5, -0.5, 1.0, 0.0]);
            result.Values[0].Should().BeApproximately(24.0, 1e-9);
            result.Values[1].Should().BeApproximately(Math.Sqrt(Math.PI), 1e-12);
            result.Values[2].Should().BeApproximately(-2.0 * Math.Sqrt(Math.PI), 1e-10);
            result.Values[3].Should().BeApproximately(1.0, 1e-12);
            result.Count.Should().Be(5);
            tools.GammaFunction([200.0]).Values[0].Should().Be(double.PositiveInfinity);
            tools.GammaFunction([0.0]).Values[0].Should().Be(double.NaN);
            tools.GammaFunction([200.0]).NonFiniteCount.Should().Be(1);
            NumericsTestSupport.ShouldRejectWith(() => tools.GammaFunction([]), "x");
            NumericsTestSupport.ShouldRejectWith(() => tools.GammaFunction([double.NaN]), "x");
            NumericsTestSupport.ShouldRejectWith(() => tools.GammaFunction(new double[200001]), "x");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.LogGammaFunction")]
        public void LogGammaFunctionKnownValuesAndLargeArguments()
        {
            var values = tools.LogGammaFunction([10.0, 1.0, 2.0, 0.5, 1e6]).Values;
            values[0].Should().BeApproximately(Math.Log(362880.0), 1e-10);
            values[1].Should().BeApproximately(0.0, 1e-12);
            values[2].Should().BeApproximately(0.0, 1e-12);
            values[3].Should().BeApproximately(0.5 * Math.Log(Math.PI), 1e-12);

            values[4].Should()
                .BeApproximately(
                    1e6 * (Math.Log(1e6) - 1) - 0.5 * Math.Log(1e6) + 0.5 * Math.Log(2 * Math.PI) + 1.0 / 12e6, 1e-6);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.DigammaFunction")]
        public void DigammaFunctionKnownValues()
        {
            var values = tools.DigammaFunction([1.0, 0.5, 2.0]).Values;
            values[0].Should().BeApproximately(-EulerMascheroni, 1e-10);
            values[1].Should().BeApproximately(-EulerMascheroni - 2.0 * Math.Log(2.0), 1e-10);
            values[2].Should().BeApproximately(1.0 - EulerMascheroni, 1e-10);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.TrigammaFunction")]
        public void TrigammaFunctionKnownValues()
        {
            var values = tools.TrigammaFunction([1.0, 0.5, 2.0]).Values;
            values[0].Should().BeApproximately(Math.PI * Math.PI / 6.0, 5e-9);
            values[1].Should().BeApproximately(Math.PI * Math.PI / 2.0, 5e-8);
            values[2].Should().BeApproximately(Math.PI * Math.PI / 6.0 - 1.0, 5e-9);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.MultivariateGammaFunction")]
        public void MultivariateGammaFunctionMatchesClosedForm()
        {
            tools.MultivariateGammaFunction([2.0], 2).Values[0].Should().BeApproximately(Math.PI / 2.0, 1e-9);

            tools.MultivariateGammaFunction([2.0], 2, true).Values[0].Should()
                .BeApproximately(Math.Log(Math.PI / 2.0), 1e-9);

            tools.MultivariateGammaFunction([3.0], 1).Values[0].Should().BeApproximately(2.0, 1e-9);
            NumericsTestSupport.ShouldRejectWith(() => tools.MultivariateGammaFunction([2.0], 0), "dimension");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.RegularisedLowerIncompleteGamma")]
        public void RegularisedLowerIncompleteGammaClosedForms()
        {
            var values = tools.RegularisedLowerIncompleteGamma(1.0, [0.0, 1.0, 3.0]).Values;
            values[0].Should().BeApproximately(0.0, 1e-12);
            values[1].Should().BeApproximately(1.0 - Math.Exp(-1.0), 1e-10);
            values[2].Should().BeApproximately(1.0 - Math.Exp(-3.0), 1e-10);

            tools.RegularisedLowerIncompleteGamma(2.0, [1.0]).Values[0].Should()
                .BeApproximately(1.0 - 2.0 / Math.E, 1e-10);

            tools.RegularisedLowerIncompleteGamma(0.5, [1.0]).Values[0].Should()
                .BeApproximately(0.8427007929497149, 1e-9);

            NumericsTestSupport.ShouldRejectWith(() => tools.RegularisedLowerIncompleteGamma(0.0, [1.0]), "a");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.RegularisedUpperIncompleteGamma")]
        public void RegularisedUpperIncompleteGammaClosedForms()
        {
            var values = tools.RegularisedUpperIncompleteGamma(1.0, [1.0, 3.0]).Values;
            values[0].Should().BeApproximately(Math.Exp(-1.0), 1e-10);
            values[1].Should().BeApproximately(Math.Exp(-3.0), 1e-10);

            tools.RegularisedUpperIncompleteGamma(2.0, [1.0]).Values[0].Should()
                .BeApproximately(2.0 / Math.E, 1e-10);

            tools.RegularisedUpperIncompleteGamma(1.0, [0.0]).Values[0].Should().BeApproximately(1.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.InverseRegularisedIncompleteGamma")]
        public void InverseRegularisedIncompleteGammaInvertsClosedForm()
        {
            tools.InverseRegularisedIncompleteGamma(1.0, [0.5]).Values[0].Should()
                .BeApproximately(Math.Log(2.0), 1e-8);

            tools.InverseRegularisedIncompleteGamma(1.0, [0.25], true).Values[0].Should()
                .BeApproximately(Math.Log(4.0), 1e-8);

            tools.InverseRegularisedIncompleteGamma(1.5, [0.95]).Values[0].Should().BeApproximately(
                tools.InverseRegularisedIncompleteGamma(1.5, [0.05], true).Values[0], 1e-8);

            var x = tools.InverseRegularisedIncompleteGamma(3.0, [0.7]).Values[0];
            tools.RegularisedLowerIncompleteGamma(3.0, [x]).Values[0].Should().BeApproximately(0.7, 1e-8);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.BetaFunction")]
        public void BetaFunctionKnownValues()
        {
            var values = tools.BetaFunction([2.0, 1.0, 0.5], [3.0, 1.0, 0.5]).Values;
            values[0].Should().BeApproximately(1.0 / 12.0, 1e-12);
            values[1].Should().BeApproximately(1.0, 1e-12);
            values[2].Should().BeApproximately(Math.PI, 1e-10);

            tools.BetaFunction([2.0], [3.0], true).Values[0].Should()
                .BeApproximately(Math.Log(1.0 / 12.0), 1e-10);

            NumericsTestSupport.ShouldRejectWith(() => tools.BetaFunction([2.0, 3.0], [3.0]), "b");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.MultinomialBetaFunction")]
        public void MultinomialBetaFunctionReducesToBeta()
        {
            tools.MultinomialBetaFunction([2.0, 3.0]).Should().BeApproximately(1.0 / 12.0, 1e-12);
            tools.MultinomialBetaFunction([1.0, 1.0, 1.0]).Should().BeApproximately(0.5, 1e-12);
            NumericsTestSupport.ShouldRejectWith(() => tools.MultinomialBetaFunction([]), "alpha");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.RegularisedIncompleteBeta")]
        public void RegularisedIncompleteBetaClosedForms()
        {
            tools.RegularisedIncompleteBeta(1.0, 1.0, [0.3, 0.9]).Values.Should().BeEquivalentTo(
                [0.3, 0.9],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-10))
                    .WhenTypeIs<double>());

            tools.RegularisedIncompleteBeta(2.0, 1.0, [0.5]).Values[0].Should().BeApproximately(0.25, 1e-10);
            tools.RegularisedIncompleteBeta(2.0, 2.0, [0.5]).Values[0].Should().BeApproximately(0.5, 1e-10);
            tools.RegularisedIncompleteBeta(2.0, 3.0, [0.0, 1.0]).Values.Should().Equal(0.0, 1.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.RegularisedIncompleteBeta(-1.0, 1.0, [0.5]), "a");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.InverseRegularisedIncompleteBeta")]
        public void InverseRegularisedIncompleteBetaInvertsClosedForms()
        {
            tools.InverseRegularisedIncompleteBeta(2.0, 1.0, [0.25]).Values[0].Should()
                .BeApproximately(0.5, 1e-8);
            tools.InverseRegularisedIncompleteBeta(2.0, 2.0, [0.5]).Values[0].Should()
                .BeApproximately(0.5, 1e-8);

            var x = tools.InverseRegularisedIncompleteBeta(3.0, 4.0, [0.9]).Values[0];
            tools.RegularisedIncompleteBeta(3.0, 4.0, [x]).Values[0].Should().BeApproximately(0.9, 1e-8);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.ErrorFunction")]
        public void ErrorFunctionFamilyKnownValues()
        {
            tools.ErrorFunction([0.0, 1.0, -1.0]).Values.Should().BeEquivalentTo(
                [0.0, 0.8427007929497149, -0.8427007929497149],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-10))
                    .WhenTypeIs<double>());

            tools.ErrorFunction([1.0], NumericsErrorFunctionKind.Erfc).Values[0].Should()
                .BeApproximately(0.15729920705028513, 1e-10);
            tools.ErrorFunction([0.8427007929497149], NumericsErrorFunctionKind.InverseErf).Values[0].Should()
                .BeApproximately(1.0, 1e-8);
            tools.ErrorFunction([0.15729920705028513], NumericsErrorFunctionKind.InverseErfc).Values[0]
                .Should().BeApproximately(1.0, 1e-8);
            tools.ErrorFunction([6.0], NumericsErrorFunctionKind.Erfc).Values[0].Should()
                .BeApproximately(2.151973671249892e-17, 1e-25);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.StandardNormalFunction")]
        public void StandardNormalFunctionKnownValues()
        {
            tools.StandardNormalFunction([1.96]).Values[0].Should().BeApproximately(0.9750021048517795, 1e-9);
            tools.StandardNormalFunction([0.0]).Values[0].Should().BeApproximately(0.5, 1e-12);

            tools.StandardNormalFunction([1.96], NumericsNormalFunctionKind.Survival).Values[0].Should()
                .BeApproximately(0.024997895148220435, 1e-9);
            tools.StandardNormalFunction([0.975], NumericsNormalFunctionKind.Quantile).Values[0].Should()
                .BeApproximately(1.959963984540054, 1e-7);
            tools.StandardNormalFunction([0.0], NumericsNormalFunctionKind.Density).Values[0].Should()
                .BeApproximately(0.3989422804014327, 1e-12);

            tools.StandardNormalFunction([0.0], NumericsNormalFunctionKind.LogDensity).Values[0].Should()
                .BeApproximately(-0.9189385332046727, 1e-12);

            tools.StandardNormalFunction([0.0], NumericsNormalFunctionKind.LogCdf).Values[0].Should()
                .BeApproximately(Math.Log(0.5), 1e-10);

            tools.StandardNormalFunction([1.96], NumericsNormalFunctionKind.HighAccuracyCdf).Values[0].Should()
                .BeApproximately(0.9750021048517795, 1e-12);
            tools.StandardNormalFunction([1.96], NumericsNormalFunctionKind.HighAccuracySurvival).Values[0]
                .Should().BeApproximately(0.024997895148220435, 1e-12);

            tools.StandardNormalFunction([0.0, 1.0], NumericsNormalFunctionKind.Quantile).Values.Should()
                .Equal(double.NegativeInfinity, double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.BivariateNormalCdf")]
        public void BivariateNormalCdfKnownValues()
        {
            tools.BivariateNormalCdf([0.0], [0.0], 0.0).Values[0].Should().BeApproximately(0.25, 1e-9);

            tools.BivariateNormalCdf([0.0], [0.0], 0.5).Values[0].Should()
                .BeApproximately(1.0 / 3.0, 1e-9);

            var product = tools.BivariateNormalCdf([1.0], [-0.5], 0.0).Values[0];
            product.Should().BeApproximately(0.8413447460685429 * 0.3085375387259869, 1e-9);

            NumericsTestSupport.ShouldRejectWith(() => tools.BivariateNormalCdf([0.0], [0.0], 1.0),
                "rho");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.BivariateNormalCdf([0.0], [0.0, 1.0], 0.2), "y");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.OwensTFunction")]
        public void OwensTFunctionKnownValues()
        {
            var values = tools.OwensTFunction([0.0, 1.0, 0.0], [1.0, 0.0, 2.0]).Values;
            values[0].Should().BeApproximately(0.125, 1e-10);
            values[1].Should().BeApproximately(0.0, 1e-12);
            values[2].Should().BeApproximately(Math.Atan(2.0) / (2.0 * Math.PI), 1e-10);
            var h = 1.5;
            var phi = 0.9331927987311419;

            tools.OwensTFunction([h], [1.0]).Values[0].Should()
                .BeApproximately(0.5 * phi * (1.0 - phi), 1e-9);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.BesselFunction")]
        public void BesselFunctionKnownValues()
        {
            tools.BesselFunction([0.0, 1.0]).Values.Should().BeEquivalentTo([1.0, 0.7651976865579666],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-8))
                    .WhenTypeIs<double>());

            tools.BesselFunction([1.0], NumericsBesselKind.FirstKindJ, 1).Values[0].Should()
                .BeApproximately(0.44005058574493355, 1e-8);
            tools.BesselFunction([1.0], NumericsBesselKind.FirstKindJ, 2).Values[0].Should()
                .BeApproximately(0.11490348493190049, 1e-8);

            tools.BesselFunction([1.0], NumericsBesselKind.SecondKindY).Values[0].Should()
                .BeApproximately(0.08825696421567697, 1e-8);
            tools.BesselFunction([1.0], NumericsBesselKind.ModifiedFirstKindI).Values[0].Should()
                .BeApproximately(1.2660658777520082, 1e-6);

            tools.BesselFunction([1.0], NumericsBesselKind.ModifiedFirstKindI, 1).Values[0].Should()
                .BeApproximately(0.5651591039924851, 1e-6);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.BesselFunction([1.0], NumericsBesselKind.FirstKindJ, 101), "order");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.FactorialFunction")]
        public void FactorialFunctionKnownValuesAndBounds()
        {
            var values = tools.FactorialFunction([0.0, 1.0, 5.0, 20.0]).Values;
            values.Should().Equal(1.0, 1.0, 120.0, 2432902008176640000.0);

            tools.FactorialFunction([10.0], true).Values[0].Should()
                .BeApproximately(15.104412573075516, 1e-10);
            tools.FactorialFunction([1000.0], true).Values[0].Should()
                .BeApproximately(5912.128178488163, 1e-6);

            NumericsTestSupport.ShouldRejectWith(() => tools.FactorialFunction([171.0]), "n");
            NumericsTestSupport.ShouldRejectWith(() => tools.FactorialFunction([2.5]), "n");
            NumericsTestSupport.ShouldRejectWith(() => tools.FactorialFunction([-1.0]), "n");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.BinomialCoefficient")]
        public void BinomialCoefficientKnownValues()
        {
            tools.BinomialCoefficient([10, 52, 5, 4], [3, 5, 0, 5]).Values.Should()
                .Equal(120.0, 2598960.0, 1.0, 0.0);

            tools.BinomialCoefficient([52], [5], true).Values[0].Should()
                .BeApproximately(Math.Log(2598960.0), 1e-10);

            tools.BinomialCoefficient([4], [5], true).Values[0].Should().Be(double.NegativeInfinity);
            NumericsTestSupport.ShouldRejectWith(() => tools.BinomialCoefficient([4, 5], [1]), "k");
            NumericsTestSupport.ShouldRejectWith(() => tools.BinomialCoefficient([-4], [1]), "n");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.LogSumExp")]
        public void LogSumExpIsStable()
        {
            tools.LogSumExp([Math.Log(1.0), Math.Log(2.0), Math.Log(3.0)]).Should()
                .BeApproximately(Math.Log(6.0), 1e-12);

            tools.LogSumExp([1000.0, 1000.0]).Should().BeApproximately(1000.0 + Math.Log(2.0), 1e-9);
            tools.LogSumExp([-1000.0, -1000.0]).Should().BeApproximately(-1000.0 + Math.Log(2.0), 1e-9);
            tools.LogSumExp([7.0]).Should().BeApproximately(7.0, 1e-12);
            tools.LogSumExp([double.NegativeInfinity, 2.0]).Should().BeApproximately(2.0, 1e-12);
            NumericsTestSupport.ShouldRejectWith(() => tools.LogSumExp([]), "values");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.SoftmaxProbabilities")]
        public void SoftmaxProbabilitiesSumToOneAndMatchKnownValues()
        {
            var values = tools.SoftmaxProbabilities([1.0, 2.0, 3.0]).Values;

            NumericsTestSupport.VectorShouldMatch(values,
                [0.09003057317038046, 0.24472847105479764, 0.6652409557748219], 1e-9);

            Enumerable.Sum(values).Should().BeApproximately(1.0, 1e-12);

            NumericsTestSupport.VectorShouldMatch(tools.SoftmaxProbabilities([1001.0, 1002.0, 1003.0]).Values,
                values, 1e-9);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.SigmoidAndLogit")]
        public void SigmoidAndLogitInvertEachOther()
        {
            NumericsTestSupport.VectorShouldMatch(tools.SigmoidAndLogit([0.0, Math.Log(3.0)]).Values,
                [0.5, 0.75], 1e-12);

            tools.SigmoidAndLogit([0.75], NumericsLogisticDirection.Logit).Values[0].Should()
                .BeApproximately(Math.Log(3.0), 1e-12);

            tools.SigmoidAndLogit([0.0, 1.0], NumericsLogisticDirection.Logit).Values.Should()
                .Equal(double.NegativeInfinity, double.PositiveInfinity);

            tools.SigmoidAndLogit([800.0]).Values[0].Should().BeApproximately(1.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.StableLogExpFunctions")]
        public void StableLogExpFunctionsKeepPrecisionNearZero()
        {
            tools.StableLogExpFunctions([1e-10]).Values[0].Should().BeApproximately(9.9999999995e-11, 1e-20);

            tools.StableLogExpFunctions([1e-10], NumericsStableFunctionKind.Expm1).Values[0].Should()
                .BeApproximately(1.00000000005e-10, 1e-20);

            tools.StableLogExpFunctions([0.5], NumericsStableFunctionKind.Log1M).Values[0].Should()
                .BeApproximately(Math.Log(0.5), 1e-12);
            tools.StableLogExpFunctions([0.0], NumericsStableFunctionKind.Softplus).Values[0].Should()
                .BeApproximately(Math.Log(2.0), 1e-12);

            tools.StableLogExpFunctions([1000.0], NumericsStableFunctionKind.Softplus).Values[0].Should()
                .BeApproximately(1000.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.LogAddAndSubtractPairs")]
        public void LogAddAndSubtractPairsKnownValues()
        {
            tools.LogAddAndSubtractPairs([Math.Log(2.0)], [Math.Log(3.0)]).Values[0].Should()
                .BeApproximately(Math.Log(5.0), 1e-12);

            tools.LogAddAndSubtractPairs([Math.Log(5.0)], [Math.Log(3.0)],
                NumericsLogPairOperation.LogDifference).Values[0].Should().BeApproximately(Math.Log(2.0), 1e-12);

            NumericsTestSupport.ShouldRejectWith(() => tools.LogAddAndSubtractPairs([1.0], [1.0, 2.0]),
                "logB");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.ReciprocalAndInverseHyperbolicFunctions")]
        public void ReciprocalAndInverseHyperbolicFunctionsKnownValues()
        {
            tools.ReciprocalAndInverseHyperbolicFunctions([0.0], NumericsHyperbolicKind.Secant).Values[0]
                .Should().BeApproximately(1.0, 1e-12);

            tools.ReciprocalAndInverseHyperbolicFunctions([Math.PI / 2.0], NumericsHyperbolicKind.Cosecant)
                .Values[0].Should().BeApproximately(1.0, 1e-12);
            tools.ReciprocalAndInverseHyperbolicFunctions([Math.PI / 4.0], NumericsHyperbolicKind.Cotangent)
                .Values[0].Should().BeApproximately(1.0, 1e-12);

            tools.ReciprocalAndInverseHyperbolicFunctions([0.0], NumericsHyperbolicKind.HyperbolicSecant)
                .Values[0].Should().BeApproximately(1.0, 1e-12);
            tools.ReciprocalAndInverseHyperbolicFunctions([1.0], NumericsHyperbolicKind.HyperbolicCotangent)
                .Values[0].Should().BeApproximately(1.3130352854993315, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([1.0], NumericsHyperbolicKind.HyperbolicCosecant)
                .Values[0].Should().BeApproximately(0.8509181282393216, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([1.0], NumericsHyperbolicKind.InverseHyperbolicSine)
                .Values[0].Should().BeApproximately(0.881373587019543, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([2.0],
                    NumericsHyperbolicKind.InverseHyperbolicCosine).Values[0].Should()
                .BeApproximately(1.3169578969248166, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([0.5],
                    NumericsHyperbolicKind.InverseHyperbolicTangent).Values[0].Should()
                .BeApproximately(0.5493061443340548, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([2.0],
                    NumericsHyperbolicKind.InverseHyperbolicCotangent).Values[0].Should()
                .BeApproximately(0.5493061443340548, 1e-10);

            tools.ReciprocalAndInverseHyperbolicFunctions([2.0], NumericsHyperbolicKind.InverseSecant)
                .Values[0].Should().BeApproximately(Math.PI / 3.0, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([2.0], NumericsHyperbolicKind.InverseCosecant)
                .Values[0].Should().BeApproximately(Math.PI / 6.0, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([1.0], NumericsHyperbolicKind.InverseCotangent)
                .Values[0].Should().BeApproximately(Math.PI / 4.0, 1e-10);

            tools.ReciprocalAndInverseHyperbolicFunctions([0.5],
                    NumericsHyperbolicKind.InverseHyperbolicSecant).Values[0].Should()
                .BeApproximately(1.3169578969248166, 1e-10);
            tools.ReciprocalAndInverseHyperbolicFunctions([1.0],
                    NumericsHyperbolicKind.InverseHyperbolicCosecant).Values[0].Should()
                .BeApproximately(0.881373587019543, 1e-10);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.StirlingGammaApproximation")]
        public void StirlingGammaApproximationIsCloseForLargeArguments()
        {
            var approximate = tools.StirlingGammaApproximation([10.0, 50.0]).Values;
            (approximate[0] / 362880.0).Should().BeApproximately(1.0, 0.01);
            (approximate[1] / Math.Exp(Gamma.Log(50.0))).Should().BeApproximately(1.0, 0.005);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.GaussianBellFunction")]
        public void GaussianBellFunctionMatchesNormalDensity()
        {
            tools.GaussianBellFunction(1.0, [0.0]).Values[0].Should()
                .BeApproximately(0.3989422804014327, 1e-12);

            tools.GaussianBellFunction(4.0, [0.0, 2.0]).Values.Should().BeEquivalentTo(
                [0.19947114020071635, 0.19947114020071635 * Math.Exp(-0.5)],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-12))
                    .WhenTypeIs<double>());

            tools.GaussianBellFunction(1.0, [0.0], [0.0]).Values[0].Should()
                .BeApproximately(1.0 / (2.0 * Math.PI), 1e-12);

            NumericsTestSupport.ShouldRejectWith(() => tools.GaussianBellFunction(0.0, [0.0]), "variance");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.GaussianKernelWeights")]
        public void GaussianKernelWeightsAreSymmetricAndPeakAtCentre()
        {
            var weights = tools.GaussianKernelWeights(1.0, 5).Values;
            weights.Length.Should().Be(5);
            weights[2].Should().BeApproximately(0.3989422804014327, 1e-12);
            weights[0].Should().BeApproximately(weights[4], 1e-15);
            weights[1].Should().BeApproximately(0.3989422804014327 * Math.Exp(-0.5), 1e-12);
            NumericsTestSupport.ShouldRejectWith(() => tools.GaussianKernelWeights(1.0, 4), "size");
            NumericsTestSupport.ShouldRejectWith(() => tools.GaussianKernelWeights(1.0, 103), "size");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.TaylorSeriesApproximation")]
        public void TaylorSeriesApproximationConvergesToTrueValues()
        {
            tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.Sine, 12).Values[0].Should()
                .BeApproximately(Math.Sin(1.0), 1e-12);
            tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.Cosine, 12).Values[0].Should()
                .BeApproximately(Math.Cos(1.0), 1e-12);
            tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.HyperbolicSine, 12).Values[0]
                .Should().BeApproximately(Math.Sinh(1.0), 1e-12);
            tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.HyperbolicCosine, 12).Values[0]
                .Should().BeApproximately(Math.Cosh(1.0), 1e-12);

            tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.Exponential, 20).Values[0].Should()
                .BeApproximately(Math.E, 1e-12);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.TaylorSeriesApproximation([1.0], NumericsTaylorFunction.Sine, 0), "terms");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.BSplineBasisFunction")]
        public void BSplineBasisFunctionKnownValues()
        {
            tools.BSplineBasisFunction([0.0, 0.5, 1.0, 2.0], 1).Values.Should().BeEquivalentTo(
                [1.0, 0.5, 0.0, 0.0],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-12))
                    .WhenTypeIs<double>());

            tools.BSplineBasisFunction([0.0, 1.0, 2.0], 3).Values.Should().BeEquivalentTo(
                [2.0 / 3.0, 1.0 / 6.0, 0.0],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-12))
                    .WhenTypeIs<double>());

            NumericsTestSupport.ShouldRejectWith(() => tools.BSplineBasisFunction([0.0], 21), "order");
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.StableHypotenuse")]
        public void StableHypotenuseHandlesHugeMagnitudes()
        {
            tools.StableHypotenuse([3.0, 3e200], [4.0, 4e200]).Values.Should().BeEquivalentTo(
                [5.0, 5e200],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, c.Expectation * 1e-12))
                    .WhenTypeIs<double>());

            tools.StableHypotenuse([0.0], [0.0]).Values[0].Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "SpecialFunctionTools.PlanarAngle")]
        public void PlanarAngleKnownQuadrants()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.PlanarAngle([1.0, 0.0, -1.0, 0.0], [0.0, 1.0, 0.0, -1.0]).Values,
                [0.0, Math.PI / 2.0, Math.PI, 3.0 * Math.PI / 2.0], 1e-12);
        }
    }
}
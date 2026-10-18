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
using Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class NumericsAndOptimisationCalculusAndRootFindingToolsTests
    {
        private static readonly double[] quadratic = [0.0, 0.0, 1.0];
        private readonly CalculusAndRootFindingTools tools = new();

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.EvaluateNamedFunction")]
        public void EvaluateNamedFunctionEveryFamily()
        {
            double At(NumericsUnivariateFunction f, double[]? p, double x)
            {
                return tools.EvaluateNamedFunction(f, p, [x]).Values[0];
            }

            At(NumericsUnivariateFunction.Polynomial, [1.0, 2.0, 3.0], 2.0).Should()
                .BeApproximately(17.0, 1e-12);

            At(NumericsUnivariateFunction.Sine, [2.0, 1.0, 0.0, 1.0], Math.PI / 2.0).Should()
                .BeApproximately(3.0, 1e-12);

            At(NumericsUnivariateFunction.Cosine, null, 0.0).Should().BeApproximately(1.0, 1e-12);
            At(NumericsUnivariateFunction.Exponential, null, 0.0).Should().BeApproximately(1.0, 1e-12);
            At(NumericsUnivariateFunction.NaturalLog, null, Math.E).Should().BeApproximately(1.0, 1e-12);
            At(NumericsUnivariateFunction.SquareRoot, null, 9.0).Should().BeApproximately(3.0, 1e-12);
            At(NumericsUnivariateFunction.Power, [2.0, 3.0, 1.0], 2.0).Should().BeApproximately(17.0, 1e-12);
            At(NumericsUnivariateFunction.Reciprocal, [1.0, 1.0], 3.0).Should().BeApproximately(0.5, 1e-12);
            At(NumericsUnivariateFunction.Gaussian, null, 1.0).Should().BeApproximately(Math.Exp(-0.5), 1e-12);
            At(NumericsUnivariateFunction.Logistic, null, 0.0).Should().BeApproximately(0.5, 1e-12);
            At(NumericsUnivariateFunction.HyperbolicTangent, null, 1.0).Should()
                .BeApproximately(0.7615941559557649, 1e-12);

            At(NumericsUnivariateFunction.AbsoluteValue, [2.0, 1.0, 3.0], -1.0).Should()
                .BeApproximately(7.0, 1e-12);

            double.IsNaN(At(NumericsUnivariateFunction.NaturalLog, null, -1.0)).Should().BeTrue();

            tools.EvaluateNamedFunction(NumericsUnivariateFunction.Polynomial, [0.0, 1.0],
                [1.0, 2.0, 3.0]).NonFiniteCount.Should().Be(0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedFunction(NumericsUnivariateFunction.Polynomial, [],
                    [1.0]), "parameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedFunction(NumericsUnivariateFunction.Gaussian, [1.0, 0.0, 0.0],
                    [1.0]), "parameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedFunction(NumericsUnivariateFunction.Sine, [1.0, 2.0, 3.0, 4.0, 5.0],
                    [1.0]), "parameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedFunction(NumericsUnivariateFunction.Sine, null, []), "x");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateTrapezoidalRule")]
        public void IntegrateTrapezoidalRuleKnownIntegrals()
        {
            tools.IntegrateTrapezoidalRule(NumericsUnivariateFunction.Polynomial, [1.0, 1.0], 0.0, 2.0, 1).Area
                .Should().BeApproximately(4.0, 1e-12);

            tools.IntegrateTrapezoidalRule(NumericsUnivariateFunction.Polynomial, quadratic, 0.0, 1.0).Area
                .Should().BeApproximately(1.0 / 3.0, 1e-6);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateTrapezoidalRule(NumericsUnivariateFunction.Polynomial, quadratic, 1.0, 0.0),
                "upper");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateTrapezoidalRule(NumericsUnivariateFunction.Polynomial, quadratic, 0.0, 1.0, 0),
                "steps");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateTrapezoidalRule(NumericsUnivariateFunction.Polynomial, quadratic, 0.0,
                    double.PositiveInfinity), "upper");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateRomberg")]
        public void IntegrateRombergKnownIntegrals()
        {
            tools.IntegrateRomberg(NumericsUnivariateFunction.Sine, null, 0.0, Math.PI).Area.Should()
                .BeApproximately(2.0, 1e-9);

            tools.IntegrateRomberg(NumericsUnivariateFunction.Polynomial, [0.0, 0.0, 0.0, 1.0], 0.0, 1.0, 5)
                .Area.Should().BeApproximately(0.25, 1e-12);

            tools.IntegrateRomberg(NumericsUnivariateFunction.Exponential, null, 0.0, 1.0).Area.Should()
                .BeApproximately(Math.E - 1.0, 1e-10);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateRomberg(NumericsUnivariateFunction.Sine, null, 0.0, 1.0, 31), "steps");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateGaussKronrod")]
        public void IntegrateGaussKronrodKnownIntegralsAndErrorEstimate()
        {
            var sine = tools.IntegrateGaussKronrod(NumericsUnivariateFunction.Sine, null, 0.0, Math.PI);
            sine.Area.Should().BeApproximately(2.0, 1e-9);
            sine.Converged.Should().BeTrue();
            sine.Status.Should().Be("Success");
            sine.ErrorEstimate.Should().BeLessThan(1e-7);
            sine.FunctionEvaluations.Should().BeGreaterThan(0);

            tools.IntegrateGaussKronrod(NumericsUnivariateFunction.Exponential, null, 0.0, 1.0).Area.Should()
                .BeApproximately(Math.E - 1.0, 1e-8);

            tools.IntegrateGaussKronrod(NumericsUnivariateFunction.Polynomial, quadratic, -1.0, 1.0).Area.Should()
                .BeApproximately(2.0 / 3.0, 1e-12);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateGaussKronrod(NumericsUnivariateFunction.Sine, null, 0.0, 1.0, 0.5),
                "relativeTolerance");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateAdaptiveGaussKronrodInfinite")]
        public void IntegrateAdaptiveGaussKronrodInfiniteKnownIntegrals()
        {
            var gaussian =
                tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Gaussian, null, 0.0, 0.0, true,
                    true);

            gaussian.Area.Should().BeApproximately(Math.Sqrt(2.0 * Math.PI), 1e-9);
            gaussian.ErrorEstimate.Should().BeLessThan(1e-6);
            gaussian.Status.Should().Be("Success");
            gaussian.Converged.Should().BeTrue();

            tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Exponential,
                [1.0, -1.0, 0.0], 0.0, 0.0, false, true).Area.Should().BeApproximately(1.0, 1e-9);

            tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Gaussian, null, 0.0, 0.0,
                true).Area.Should().BeApproximately(Math.Sqrt(2.0 * Math.PI) / 2.0, 1e-9);

            tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Sine, null, 0.0, Math.PI).Area
                .Should().BeApproximately(2.0, 1e-9);

            var diverging = tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Polynomial,
                [1.0], 0.0, 0.0, false, true);
            diverging.Converged.Should().BeFalse();

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Sine, null, 0.0, 1.0,
                    false, false, 200, 1e-15), "relativeTolerance");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Sine, null, 2.0, 1.0),
                "upper");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateAdaptiveGaussKronrodInfinite(NumericsUnivariateFunction.Sine, null, 0.0, 1.0,
                    false, false, 5000), "subintervals");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateMonteCarloOnInterval")]
        public void IntegrateMonteCarloOnIntervalIsWithinStandardErrorAndReproducible()
        {
            var first = tools.IntegrateMonteCarloOnInterval(NumericsUnivariateFunction.Polynomial, quadratic, 0.0, 1.0,
                200000, 5);
            first.ErrorEstimate.Should().BeGreaterThan(0.0);
            Math.Abs(first.Area - 1.0 / 3.0).Should().BeLessThan(5.0 * first.ErrorEstimate);
            first.ErrorEstimate.Should().BeApproximately(Math.Sqrt(1.0 / 5.0 - 1.0 / 9.0) / Math.Sqrt(200000.0), 2e-4);

            var second = tools.IntegrateMonteCarloOnInterval(NumericsUnivariateFunction.Polynomial, quadratic, 0.0,
                1.0, 200000, 5);

            second.Area.Should().Be(first.Area);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateMonteCarloOnInterval(NumericsUnivariateFunction.Sine, null, 0.0, 1.0, 5000001),
                "samples");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.IntegrateMonteCarloObjectiveOverBox")]
        public void IntegrateMonteCarloObjectiveOverBoxKnownIntegral()
        {
            var result = tools.IntegrateMonteCarloObjectiveOverBox(NumericsObjectiveFunction.Sphere,
                [0.0, 0.0, 0.0], [1.0, 1.0, 1.0], 300000, 3);

            Math.Abs(result.Area - 1.0).Should().BeLessThan(5.0 * result.ErrorEstimate);
            result.ErrorEstimate.Should().BeInRange(1e-4, 5e-3);

            var quadraticIntegral = tools.IntegrateMonteCarloObjectiveOverBox(NumericsObjectiveFunction.Quadratic,
                [0.0], [2.0], 200000, 3,
                [[2.0]], [1.0], 3.0);

            Math.Abs(quadraticIntegral.Area - (8.0 / 3.0 + 2.0 + 6.0)).Should()
                .BeLessThan(5.0 * quadraticIntegral.ErrorEstimate);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateMonteCarloObjectiveOverBox(NumericsObjectiveFunction.Sphere, [0.0],
                    [0.0]), "upperBounds");
            NumericsTestSupport.ShouldRejectWith(
                () => tools.IntegrateMonteCarloObjectiveOverBox(NumericsObjectiveFunction.Himmelblau, [0.0],
                    [1.0]), "lowerBounds");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.NumericalDerivativeOfNamedFunction")]
        public void NumericalDerivativeOfNamedFunctionKnownDerivatives()
        {
            tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0).Should()
                .BeApproximately(Math.Cos(1.0), 1e-7);

            tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0, 2).Should()
                .BeApproximately(-Math.Sin(1.0), 1e-4);

            tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Polynomial,
                [0.0, 0.0, 0.0, 1.0], 2.0).Should().BeApproximately(12.0, 1e-6);

            tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Exponential, null, 0.5, 1, 0.01, 7)
                .Should().BeApproximately(Math.Exp(0.5), 1e-9);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0, 3, 0.01, 3),
                "points");
            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0, 1, 0.01, 4),
                "points");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0, 5),
                "order");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalDerivativeOfNamedFunction(NumericsUnivariateFunction.Sine, null, 1.0, 1, 0.0),
                "stepSize");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.NumericalGradientOfObjective")]
        public void NumericalGradientOfObjectiveKnownGradients()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Sphere, [1.0, 2.0, 3.0]).Values,
                [2.0, 4.0, 6.0], 1e-6);

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Rosenbrock, [1.0, 1.0]).Values,
                [0.0, 0.0], 1e-4);

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Rosenbrock, [0.0, 0.0]).Values,
                [-2.0, 0.0], 1e-4);

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Quadratic, [1.0, 1.0], 0.001, 5,
                    [[2.0, 0.0], [0.0, 4.0]], [1.0, -1.0]).Values,
                [3.0, 3.0], 1e-6);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Quadratic, [1.0, 1.0]),
                "quadraticMatrix");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Booth, [1.0, 1.0, 1.0]),
                "point");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalGradientOfObjective(NumericsObjectiveFunction.Sphere, []),
                "point");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.NumericalHessianOfObjective")]
        public void NumericalHessianOfObjectiveKnownHessians()
        {
            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalHessianOfObjective(NumericsObjectiveFunction.Sphere, [1.0, -2.0]).Matrix,
                [[2.0, 0.0], [0.0, 2.0]], 1e-3);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalHessianOfObjective(NumericsObjectiveFunction.Quadratic, [1.0, 1.0], 0.01,
                    [[2.0, 1.0], [1.0, 4.0]]).Matrix,
                [[2.0, 1.0], [1.0, 4.0]], 1e-3);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalHessianOfObjective(NumericsObjectiveFunction.Rosenbrock, [1.0, 1.0], 0.001)
                    .Matrix, [[802.0, -400.0], [-400.0, 200.0]], 0.5);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalHessianOfObjective(NumericsObjectiveFunction.Sphere, new double[21]), "point");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalHessianOfObjective(NumericsObjectiveFunction.Sphere, [1.0], 0.0),
                "stepSize");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.NumericalJacobianOfVectorFunction")]
        public void NumericalJacobianOfVectorFunctionKnownJacobians()
        {
            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.PolarToCartesian,
                    [2.0, Math.PI / 2.0]).Matrix, [[0.0, -2.0], [1.0, 0.0]], 1e-6);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.SphericalToCartesian,
                    [1.0, Math.PI / 2.0, 0.0]).Matrix,
                [[1.0, 0.0, 0.0], [0.0, 0.0, 1.0], [0.0, -1.0, 0.0]], 1e-6);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.Softmax, [0.0, 0.0]).Matrix,
                [[0.25, -0.25], [-0.25, 0.25]], 1e-6);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.RosenbrockResiduals, [1.0, 2.0])
                    .Matrix, [[-20.0, 10.0], [-1.0, 0.0]], 1e-6);

            NumericsTestSupport.MatrixShouldMatch(
                tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.HimmelblauResiduals, [3.0, 2.0])
                    .Matrix, [[6.0, 1.0], [1.0, 4.0]], 1e-6);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericalJacobianOfVectorFunction(NumericsVectorFunction.PolarToCartesian, [1.0]),
                "point");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.FindRootBrent")]
        public void FindRootBrentKnownRoots()
        {
            var sqrtTwo =
                tools.FindRootBrent(NumericsUnivariateFunction.Polynomial, [-2.0, 0.0, 1.0], 0.0, 2.0);
            sqrtTwo.Converged.Should().BeTrue();
            sqrtTwo.Solution.Should().BeApproximately(Math.Sqrt(2.0), 1e-8);
            Math.Abs(sqrtTwo.FunctionValue).Should().BeLessThan(1e-8);

            tools.FindRootBrent(NumericsUnivariateFunction.Cosine, null, 0.0, 1.5, 0.5).Solution.Should()
                .BeApproximately(Math.PI / 3.0, 1e-8);

            var unbracketed =
                tools.FindRootBrent(NumericsUnivariateFunction.Polynomial, [1.0, 0.0, 1.0], 0.0, 1.0);
            unbracketed.Converged.Should().BeFalse();
            unbracketed.Status.Should().Be("RootNotBracketed");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FindRootBrent(NumericsUnivariateFunction.Sine, null, 3.0, 1.0), "upper");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FindRootBrent(NumericsUnivariateFunction.Sine, null, 0.0, 1.0, 0.0, 0.0), "tolerance");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.FindRootBracketsByScanning")]
        public void FindRootBracketsByScanningFindsSineZeros()
        {
            var result = tools.FindRootBracketsByScanning(NumericsUnivariateFunction.Sine, null, 1.0, 10.0);
            result.Count.Should().Be(3);

            for (var k = 0; k < 3; k++)
            {
                result.Lower[k].Should().BeLessThan((k + 1) * Math.PI);
                result.Upper[k].Should().BeGreaterThan((k + 1) * Math.PI);
            }

            tools.FindRootBracketsByScanning(NumericsUnivariateFunction.Polynomial, [0.0, 1.0], -1.0, 1.0, 101)
                .ExactZeros.Should().ContainSingle().Which.Should().BeApproximately(0.0, 1e-12);

            tools.FindRootBracketsByScanning(NumericsUnivariateFunction.Polynomial, [1.0, 0.0, 1.0], -5.0, 5.0)
                .Count.Should().Be(0);

            tools.FindRootBracketsByScanning(NumericsUnivariateFunction.Polynomial, quadratic, 0.0, 4.0, 100, 2.25)
                .Count.Should().Be(1);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FindRootBracketsByScanning(NumericsUnivariateFunction.Sine, null, 0.0, 1.0, 1),
                "gridPoints");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.FindExtremumBrent")]
        public void FindExtremumBrentKnownExtrema()
        {
            var minimum = tools.FindExtremumBrent(NumericsUnivariateFunction.Polynomial, [4.0, -4.0, 1.0], 0.0,
                5.0);
            minimum.Converged.Should().BeTrue();
            minimum.Solution.Should().BeApproximately(2.0, 1e-6);
            minimum.FunctionValue.Should().BeApproximately(0.0, 1e-10);
            var maximum = tools.FindExtremumBrent(NumericsUnivariateFunction.Sine, null, 0.0, 3.0, true);
            maximum.Solution.Should().BeApproximately(Math.PI / 2.0, 1e-6);
            maximum.FunctionValue.Should().BeApproximately(1.0, 1e-10);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FindExtremumBrent(NumericsUnivariateFunction.Sine, null, 1.0, 1.0), "upper");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.EvaluatePolynomial")]
        public void EvaluatePolynomialHorner()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.EvaluatePolynomial([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]).Values,
                [1.0, 6.0, 17.0], 1e-12);

            tools.EvaluatePolynomial([5.0], [1e100]).Values[0].Should().Be(5.0);

            NumericsTestSupport.ShouldRejectWith(() => tools.EvaluatePolynomial(new double[41], [1.0]),
                "coefficients");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.PolynomialDerivativeOrAntiderivative")]
        public void PolynomialDerivativeOrAntiderivativeKnownAnswers()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.PolynomialDerivativeOrAntiderivative([1.0, 2.0, 3.0, 4.0]).Values,
                [2.0, 6.0, 12.0], 1e-12);

            NumericsTestSupport.VectorShouldMatch(
                tools.PolynomialDerivativeOrAntiderivative([2.0, 6.0, 12.0],
                    NumericsPolynomialCalculus.Antiderivative, 5.0).Values, [5.0, 2.0, 3.0, 4.0], 1e-12);

            NumericsTestSupport.VectorShouldMatch(tools.PolynomialDerivativeOrAntiderivative([7.0]).Values,
                [0.0], 0.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.PolynomialDerivativeOrAntiderivative([]), "coefficients");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.PolynomialMultiply")]
        public void PolynomialMultiplyKnownProduct()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.PolynomialMultiply([1.0, 1.0], [1.0, -1.0]).Values, [1.0, 0.0, -1.0],
                1e-12);

            NumericsTestSupport.VectorShouldMatch(
                tools.PolynomialMultiply([1.0, 2.0, 3.0], [4.0]).Values, [4.0, 8.0, 12.0],
                1e-12);

            NumericsTestSupport.ShouldRejectWith(() => tools.PolynomialMultiply(new double[21], [1.0]), "a");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.PolynomialRoots")]
        public void PolynomialRootsKnownRoots()
        {
            var real = tools.PolynomialRoots([2.0, -3.0, 1.0]);
            NumericsTestSupport.VectorShouldMatch(real.RealParts, [1.0, 2.0], 1e-9);
            real.ImaginaryParts.Should().OnlyContain(v => Math.Abs(v) < 1e-9);
            real.Degree.Should().Be(2);

            var complex = tools.PolynomialRoots([1.0, 0.0, 1.0]);
            complex.RealParts.Should().OnlyContain(v => Math.Abs(v) < 1e-9);

            complex.ImaginaryParts.OrderBy(v => v).ToArray().Should().BeEquivalentTo([-1.0, 1.0],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());

            NumericsTestSupport.VectorShouldMatch(tools.PolynomialRoots([-6.0, 11.0, -6.0, 1.0]).RealParts,
                [1.0, 2.0, 3.0], 1e-8);

            NumericsTestSupport.VectorShouldMatch(tools.PolynomialRoots([-4.0, 0.0, 2.0]).RealParts,
                [-Math.Sqrt(2.0), Math.Sqrt(2.0)], 1e-9);

            NumericsTestSupport.ShouldRejectWith(() => tools.PolynomialRoots([1.0, 2.0, 0.0]), "coefficients");
            NumericsTestSupport.ShouldRejectWith(() => tools.PolynomialRoots([1.0]), "coefficients");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.FitPolynomialByLeastSquares")]
        public void FitPolynomialByLeastSquaresKnownFits()
        {
            var exact = tools.FitPolynomialByLeastSquares([0.0, 1.0, 2.0], [1.0, 2.0, 5.0], 2);
            NumericsTestSupport.VectorShouldMatch(exact.Coefficients, [1.0, 0.0, 1.0], 1e-9);
            exact.ResidualSumOfSquares.Should().BeLessThan(1e-15);
            var line = tools.FitPolynomialByLeastSquares([0.0, 1.0, 2.0], [0.0, 1.0, 1.0], 1);
            NumericsTestSupport.VectorShouldMatch(line.Coefficients, [1.0 / 6.0, 0.5], 1e-9);
            line.ResidualSumOfSquares.Should().BeApproximately(1.0 / 6.0, 1e-9);

            tools.FitPolynomialByLeastSquares([1.0, 2.0, 3.0], [4.0, 4.0, 4.0], 0).Coefficients[0]
                .Should().BeApproximately(4.0, 1e-12);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FitPolynomialByLeastSquares([0.0, 1.0], [0.0, 1.0], 2), "degree");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FitPolynomialByLeastSquares([0.0, 1.0], [0.0], 1), "y");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.InterpolatePolynomialThroughPoints")]
        public void InterpolatePolynomialThroughPointsRecoversQuadratic()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.InterpolatePolynomialThroughPoints([0.0, 1.0, 2.0], [1.0, 3.0, 7.0]).Values,
                [1.0, 1.0, 1.0], 1e-9);

            NumericsTestSupport.VectorShouldMatch(
                tools.InterpolatePolynomialThroughPoints([1.0, 3.0], [2.0, 8.0]).Values,
                [-1.0, 3.0], 1e-9);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.InterpolatePolynomialThroughPoints([1.0, 1.0], [2.0, 8.0]), "x");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.InterpolatePolynomialThroughPoints(new double[21], new double[21]), "x");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.InterpolateLinearly")]
        public void InterpolateLinearlyKnownValues()
        {
            var x = new[] { 0.0, 1.0, 2.0 };
            var y = new[] { 0.0, 10.0, 20.0 };

            NumericsTestSupport.VectorShouldMatch(
                tools.InterpolateLinearly(x, y, [0.5, 1.5, 2.0, -1.0, 3.0], -9.0, 99.0).Values,
                [5.0, 15.0, 20.0, -9.0, 99.0], 1e-12);

            double.IsNaN(tools.InterpolateLinearly(x, y, [5.0]).Values[0]).Should().BeTrue();

            NumericsTestSupport.ShouldRejectWith(
                () => tools.InterpolateLinearly([0.0, 0.0], [1.0, 2.0], [0.0]), "x");

            NumericsTestSupport.ShouldRejectWith(() => tools.InterpolateLinearly(x, [1.0], [0.0]),
                "y");
        }

        [Fact]
        [Trait("Covers", "CalculusAndRootFindingTools.ExtrapolateLinearly")]
        public void ExtrapolateLinearlyExtendsEndSegments()
        {
            var x = new[] { 0.0, 1.0, 2.0 };
            var y = new[] { 0.0, 10.0, 25.0 };

            NumericsTestSupport.VectorShouldMatch(
                tools.ExtrapolateLinearly(x, y, [-1.0, 3.0, 0.5, 1.5]).Values,
                [-10.0, 40.0, 5.0, 17.5], 1e-12);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.ExtrapolateLinearly([1.0, 0.0], [1.0, 2.0], [0.0]), "x");
        }
    }
}
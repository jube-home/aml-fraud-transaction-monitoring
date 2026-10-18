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
    public class NumericsAndOptimisationOptimisationToolsTests
    {
        private static readonly double[][] diagonal24 = [[2.0, 0.0], [0.0, 4.0]];

        private static readonly double[][] nocedalConstraints =
        [
            [1.0, -2.0], [-1.0, -2.0], [-1.0, 2.0], [1.0, 0.0], [0.0, 1.0]
        ];

        private static readonly double[] nocedalValues = [-2.0, -6.0, -2.0, 0.0, 0.0];
        private readonly OptimisationTools tools = new();

        [Fact]
        [Trait("Covers", "OptimisationTools.EvaluateNamedObjective")]
        public void EvaluateNamedObjectiveKnownValuesAtKnownMinima()
        {
            double At(NumericsObjectiveFunction f, params double[] point)
            {
                return tools.EvaluateNamedObjective(f, [point]).Values[0];
            }

            At(NumericsObjectiveFunction.Sphere, 1.0, 2.0, 2.0).Should().BeApproximately(9.0, 1e-12);
            At(NumericsObjectiveFunction.Rosenbrock, 1.0, 1.0).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Rosenbrock, 0.0, 0.0).Should().BeApproximately(1.0, 1e-12);
            At(NumericsObjectiveFunction.Rosenbrock, -1.0, 1.0, 1.0).Should().BeApproximately(4.0, 1e-12);
            At(NumericsObjectiveFunction.Rastrigin, 0.0, 0.0).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Rastrigin, 1.0, 1.0).Should().BeApproximately(2.0, 1e-12);
            At(NumericsObjectiveFunction.Ackley, 0.0, 0.0).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Himmelblau, 3.0, 2.0).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Himmelblau, 0.0, 0.0).Should().BeApproximately(170.0, 1e-12);
            At(NumericsObjectiveFunction.Booth, 1.0, 3.0).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Beale, 3.0, 0.5).Should().BeApproximately(0.0, 1e-12);
            At(NumericsObjectiveFunction.Matyas, 1.0, 1.0).Should().BeApproximately(0.04, 1e-12);

            At(NumericsObjectiveFunction.StyblinskiTang, -2.903534, -2.903534).Should()
                .BeApproximately(-78.33233, 1e-3);

            At(NumericsObjectiveFunction.Griewank, 0.0, 0.0, 0.0).Should().BeApproximately(0.0, 1e-12);

            tools.EvaluateNamedObjective(NumericsObjectiveFunction.Quadratic, [[1.0, 1.0]], diagonal24,
                [1.0, -1.0], 3.0).Values[0].Should().BeApproximately(6.0, 1e-12);

            NumericsTestSupport.VectorShouldMatch(
                tools.EvaluateNamedObjective(NumericsObjectiveFunction.Sphere,
                    [[1.0], [2.0], [3.0]]).Values, [1.0, 4.0, 9.0], 1e-12);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedObjective(NumericsObjectiveFunction.Himmelblau,
                    [[1.0, 2.0, 3.0]]), "points");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedObjective(NumericsObjectiveFunction.Sphere,
                    [[1.0, 2.0], [1.0]]), "points");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.EvaluateNamedObjective(NumericsObjectiveFunction.Rosenbrock, [[1.0]]),
                "points");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithLimitedMemoryBfgs")]
        public void MinimiseWithLimitedMemoryBfgsFindsKnownMinima()
        {
            var sphere =
                tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Sphere, [3.0, -4.0, 5.0]);
            sphere.Converged.Should().BeTrue();
            sphere.Solution.Should().OnlyContain(v => Math.Abs(v) < 1e-5);
            sphere.Value.Should().BeLessThan(1e-9);
            sphere.GradientNorm.Should().BeLessThan(1e-4);
            sphere.Evaluations.Should().BeGreaterThan(0);

            var rosenbrock =
                tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Rosenbrock, [-1.2, 1.0]);

            NumericsTestSupport.VectorShouldMatch(rosenbrock.Solution, [1.0, 1.0], 1e-4);

            var quadratic = tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Quadratic,
                [0.0, 0.0], 500, 1e-8, diagonal24, [-2.0, -4.0]);

            NumericsTestSupport.VectorShouldMatch(quadratic.Solution, [1.0, 1.0], 1e-5);
            quadratic.Value.Should().BeApproximately(-3.0, 1e-8);

            var himmelblau =
                tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Himmelblau, [2.0, 2.0]);

            NumericsTestSupport.VectorShouldMatch(himmelblau.Solution, [3.0, 2.0], 1e-4);

            var ackley = tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Ackley, [0.3, -0.2]);
            ackley.Value.Should().BeLessThan(1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Sphere, []),
                "start");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Sphere, [1.0], 0),
                "maxIterations");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Sphere, [1.0], 10, 0.0),
                "tolerance");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLimitedMemoryBfgs(NumericsObjectiveFunction.Sphere, new double[51]), "start");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithBoundedBfgs")]
        public void MinimiseWithBoundedBfgsRespectsBounds()
        {
            var bounded = tools.MinimiseWithBoundedBfgs(NumericsObjectiveFunction.Quadratic, [0.0, 0.0], null,
                [0.5, 2.0], 500, 1e-12, diagonal24, [-2.0, -4.0]);

            NumericsTestSupport.VectorShouldMatch(bounded.Solution, [0.5, 1.0], 1e-5);
            bounded.Value.Should().BeApproximately(0.5 * (2.0 * 0.25 + 4.0) - 1.0 - 4.0, 1e-8);

            var lowerBounded = tools.MinimiseWithBoundedBfgs(NumericsObjectiveFunction.Sphere, [3.0, 3.0],
                [1.0, -5.0]);

            NumericsTestSupport.VectorShouldMatch(lowerBounded.Solution, [1.0, 0.0], 1e-5);

            var free = tools.MinimiseWithBoundedBfgs(NumericsObjectiveFunction.Booth, [0.0, 0.0]);
            NumericsTestSupport.VectorShouldMatch(free.Solution, [1.0, 3.0], 1e-4);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithBoundedBfgs(NumericsObjectiveFunction.Sphere, [1.0, 1.0],
                    [0.0]), "lowerBounds");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithBoundedBfgs(NumericsObjectiveFunction.Sphere, [1.0], [2.0],
                    [1.0]), "lowerBounds");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithConjugateGradient")]
        public void MinimiseWithConjugateGradientFindsKnownMinima()
        {
            foreach (var variant in Enum.GetValues<NumericsConjugateGradientVariant>())
            {
                var result = tools.MinimiseWithConjugateGradient(NumericsObjectiveFunction.Quadratic,
                    [0.0, 0.0], variant, 1000, 1e-10, diagonal24, [-2.0, -4.0]);

                NumericsTestSupport.VectorShouldMatch(result.Solution, [1.0, 1.0], 1e-4);
            }

            var rosenbrock =
                tools.MinimiseWithConjugateGradient(NumericsObjectiveFunction.Rosenbrock, [-1.2, 1.0]);

            NumericsTestSupport.VectorShouldMatch(rosenbrock.Solution, [1.0, 1.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithConjugateGradient(NumericsObjectiveFunction.Sphere, [1.0],
                    NumericsConjugateGradientVariant.PolakRibiere, 100001), "maxIterations");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithGradientDescent")]
        public void MinimiseWithGradientDescentConvergesOnConvexProblemsAndRejectsBadRates()
        {
            var sphere = tools.MinimiseWithGradientDescent(NumericsObjectiveFunction.Sphere, [3.0, -2.0], 0.1,
                5000, 1e-12);

            sphere.Solution.Should().OnlyContain(v => Math.Abs(v) < 1e-4);

            var booth = tools.MinimiseWithGradientDescent(NumericsObjectiveFunction.Booth, [0.0, 0.0], 0.02,
                20000, 1e-14);

            NumericsTestSupport.VectorShouldMatch(booth.Solution, [1.0, 3.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithGradientDescent(NumericsObjectiveFunction.Sphere, [1.0], 0.0),
                "learningRate");
            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithGradientDescent(NumericsObjectiveFunction.Sphere, [1.0], 11.0),
                "learningRate");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithNelderMead")]
        public void MinimiseWithNelderMeadFindsKnownMinimaDerivativeFree()
        {
            var rosenbrock = tools.MinimiseWithNelderMead(NumericsObjectiveFunction.Rosenbrock, [-1.2, 1.0],
                20000, 1e-12);

            NumericsTestSupport.VectorShouldMatch(rosenbrock.Solution, [1.0, 1.0], 1e-3);

            var beale = tools.MinimiseWithNelderMead(NumericsObjectiveFunction.Beale, [1.0, 1.0], 20000,
                1e-14);

            NumericsTestSupport.VectorShouldMatch(beale.Solution, [3.0, 0.5], 1e-3);

            var bounded = tools.MinimiseWithNelderMead(NumericsObjectiveFunction.Sphere, [3.0, 3.0], 5000,
                1e-12, 0.5, [1.0, 1.0], [5.0, 5.0]);

            NumericsTestSupport.VectorShouldMatch(bounded.Solution, [1.0, 1.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithNelderMead(NumericsObjectiveFunction.Sphere, [1.0], 200001),
                "maxEvaluations");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithNelderMead(NumericsObjectiveFunction.Sphere, [1.0], 100, 1e-8, 0.0),
                "initialStep");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithSubplex")]
        public void MinimiseWithSubplexFindsKnownMinima()
        {
            var sphere = tools.MinimiseWithSubplex(NumericsObjectiveFunction.Sphere,
                [.. Enumerable.Range(1, 10).Select(i => (double)i)], 100000, 1e-14);
            sphere.Value.Should().BeLessThan(1e-6);
            var booth = tools.MinimiseWithSubplex(NumericsObjectiveFunction.Booth, [0.0, 0.0], 20000, 1e-14);
            NumericsTestSupport.VectorShouldMatch(booth.Solution, [1.0, 3.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithSubplex(NumericsObjectiveFunction.Sphere, [1.0], 0), "maxEvaluations");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.CompareUnconstrainedOptimisers")]
        public void CompareUnconstrainedOptimisersAgreeOnConvexProblem()
        {
            var entries = tools.CompareUnconstrainedOptimisers(NumericsObjectiveFunction.Quadratic, [0.0, 0.0],
                5000, 1e-10, diagonal24, [-2.0, -4.0]);

            entries.Select(e => e.Method).Should().BeEquivalentTo("LimitedMemoryBfgs", "ConjugateGradient",
                "GradientDescent", "NelderMead", "Subplex");

            foreach (var entry in entries)
            {
                entry.Error.Should().BeEmpty();
                entry.Value.Should().BeApproximately(-3.0, 1e-4, entry.Method);
                NumericsTestSupport.VectorShouldMatch(entry.Solution, [1.0, 1.0], 1e-2);
            }

            NumericsTestSupport.ShouldRejectWith(
                () => tools.CompareUnconstrainedOptimisers(NumericsObjectiveFunction.Sphere, new double[21]), "start");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.SolveQuadraticProgram")]
        public void SolveQuadraticProgramMatchesTextbookExamples()
        {
            var simple = tools.SolveQuadraticProgram([[1.0, 0.0], [0.0, 1.0]], null,
                [[1.0, 1.0]], [2.0]);
            simple.Converged.Should().BeTrue();
            NumericsTestSupport.VectorShouldMatch(simple.Solution, [1.0, 1.0], 1e-9);
            simple.Value.Should().BeApproximately(1.0, 1e-9);
            simple.LagrangeMultipliers[0].Should().BeApproximately(1.0, 1e-9);
            simple.MaximumConstraintViolation.Should().BeLessThan(1e-9);

            var nocedal = tools.SolveQuadraticProgram([[2.0, 0.0], [0.0, 2.0]],
                [-2.0, -5.0], nocedalConstraints, nocedalValues);

            NumericsTestSupport.VectorShouldMatch(nocedal.Solution, [1.4, 1.7], 1e-9);
            NumericsTestSupport.VectorShouldMatch(nocedal.LagrangeMultipliers, [0.8, 0.0, 0.0, 0.0, 0.0], 1e-8);

            var lessThan = tools.SolveQuadraticProgram([[1.0, 0.0], [0.0, 1.0]], null,
                [[1.0, 1.0]], [-2.0], [NumericsConstraintRelation.LessThanOrEqual]);

            NumericsTestSupport.VectorShouldMatch(lessThan.Solution, [-1.0, -1.0], 1e-9);

            var equality = tools.SolveQuadraticProgram([[1.0, 0.0], [0.0, 1.0]], null,
                [[1.0, 1.0], [1.0, 0.0]], [2.0, 0.0],
                [NumericsConstraintRelation.GreaterThanOrEqual, NumericsConstraintRelation.Equal]);

            NumericsTestSupport.VectorShouldMatch(equality.Solution, [0.0, 2.0], 1e-9);

            var unconstrained = tools.SolveQuadraticProgram(diagonal24, [-2.0, -4.0]);
            NumericsTestSupport.VectorShouldMatch(unconstrained.Solution, [1.0, 1.0], 1e-9);

            tools.SolveQuadraticProgram([[1.0, 2.0], [2.0, 1.0]]).Status.Should()
                .Be("NonPositiveDefinite");

            var infeasible = tools.SolveQuadraticProgram([[1.0]], null,
                [[1.0], [1.0]], [1.0, 0.0],
                [NumericsConstraintRelation.GreaterThanOrEqual, NumericsConstraintRelation.LessThanOrEqual]);
            infeasible.Converged.Should().BeFalse();
            infeasible.Status.Should().Be("NoPossibleSolution");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveQuadraticProgram([[1.0, 2.0], [3.0, 1.0]]),
                "quadraticMatrix");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveQuadraticProgram(diagonal24, null, [[1.0]], [1.0]),
                "constraintMatrix");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveQuadraticProgram(diagonal24, null, [[1.0, 1.0]], [1.0, 2.0]),
                "constraintValues");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.MinimiseWithLinearConstraintsCobyla")]
        public void MinimiseWithLinearConstraintsCobylaMatchesQuadraticProgram()
        {
            var nocedal = tools.MinimiseWithLinearConstraintsCobyla(NumericsObjectiveFunction.Quadratic,
                [2.0, 0.0], nocedalConstraints, nocedalValues, null, 20000, 1e-9, 0.5,
                [[2.0, 0.0], [0.0, 2.0]], [-2.0, -5.0]);

            NumericsTestSupport.VectorShouldMatch(nocedal.Solution, [1.4, 1.7], 1e-4);
            nocedal.MaximumConstraintViolation.Should().BeLessThan(1e-5);

            var equality = tools.MinimiseWithLinearConstraintsCobyla(NumericsObjectiveFunction.Sphere,
                [0.0, 0.0], [[1.0, 1.0]], [2.0],
                [NumericsConstraintRelation.Equal]);

            NumericsTestSupport.VectorShouldMatch(equality.Solution, [1.0, 1.0], 1e-4);

            var unconstrained =
                tools.MinimiseWithLinearConstraintsCobyla(NumericsObjectiveFunction.Booth, [0.0, 0.0]);

            NumericsTestSupport.VectorShouldMatch(unconstrained.Solution, [1.0, 3.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLinearConstraintsCobyla(NumericsObjectiveFunction.Sphere, [0.0, 0.0],
                    [[1.0]], [1.0]), "constraintMatrix");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MinimiseWithLinearConstraintsCobyla(NumericsObjectiveFunction.Sphere, [0.0], null,
                    null, null, 100, 1e-13), "precision");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.SolveLinearProgram")]
        public void SolveLinearProgramMatchesTextbookExamples()
        {
            var maximise = tools.SolveLinearProgram([3.0, 2.0],
                [[1.0, 1.0], [1.0, 3.0], [1.0, 0.0]], [4.0, 6.0, 3.0]);

            NumericsTestSupport.VectorShouldMatch(maximise.Solution, [3.0, 1.0], 1e-4);
            maximise.Value.Should().BeApproximately(11.0, 1e-4);
            maximise.MaximumConstraintViolation.Should().BeLessThan(1e-6);

            var minimise = tools.SolveLinearProgram([2.0, 3.0],
                [[1.0, 1.0], [1.0, 3.0]], [4.0, 6.0],
                [NumericsConstraintRelation.GreaterThanOrEqual, NumericsConstraintRelation.GreaterThanOrEqual],
                false);

            NumericsTestSupport.VectorShouldMatch(minimise.Solution, [3.0, 1.0], 1e-4);
            minimise.Value.Should().BeApproximately(9.0, 1e-4);

            var boxed = tools.SolveLinearProgram([1.0, 1.0], [[1.0, 1.0]], [100.0],
                null, true, 0, 2.5);

            NumericsTestSupport.VectorShouldMatch(boxed.Solution, [2.5, 2.5], 1e-4);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveLinearProgram([1.0, 1.0], [[1.0]], [1.0]),
                "constraintMatrix");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveLinearProgram([], [[1.0]], [1.0]),
                "objectiveCoefficients");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.FitNamedCurveLeastSquares")]
        public void FitNamedCurveLeastSquaresRecoversKnownCurves()
        {
            var xs = new[] { 0.0, 1.0, 2.0, 3.0, 4.0 };

            var exponential = tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Exponential, xs,
                [.. xs.Select(v => 2.0 * Math.Exp(0.5 * v) + 1.0)], [1.0, 0.3, 0.0]);

            NumericsTestSupport.VectorShouldMatch(exponential.Parameters, [2.0, 0.5, 1.0], 1e-4);
            exponential.SumOfSquaredResiduals.Should().BeLessThan(1e-8);
            exponential.Converged.Should().BeTrue();
            exponential.StandardErrors.Should().NotBeEmpty();

            var gaussXs = Enumerable.Range(-10, 21).Select(i => i / 4.0).ToArray();

            var gauss = tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Gaussian, gaussXs,
                [.. gaussXs.Select(v => 3.0 * Math.Exp(-(v - 0.5) * (v - 0.5) / (2 * 0.7 * 0.7)))],
                [2.0, 0.0, 1.0]);

            NumericsTestSupport.VectorShouldMatch(gauss.Parameters, [3.0, 0.5, 0.7], 1e-4);

            var sineXs = Enumerable.Range(0, 40).Select(i => i * 0.2).ToArray();

            var sine = tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Sine, sineXs,
                [.. sineXs.Select(v => 2.0 * Math.Sin(1.5 * v + 0.3) + 0.5)], [1.5, 1.4, 0.2, 0.0]);

            NumericsTestSupport.VectorShouldMatch(sine.Parameters, [2.0, 1.5, 0.3, 0.5], 1e-4);

            var line = tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Polynomial, [0.0, 1.0, 2.0],
                [0.0, 1.0, 1.0], [0.0, 0.0]);

            NumericsTestSupport.VectorShouldMatch(line.Parameters, [1.0 / 6.0, 0.5], 1e-6);
            line.SumOfSquaredResiduals.Should().BeApproximately(1.0 / 6.0, 1e-9);
            line.RootMeanSquaredError.Should().BeApproximately(Math.Sqrt(1.0 / 18.0), 1e-9);

            var gaussNewton = tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Exponential, xs,
                [.. xs.Select(v => 2.0 * Math.Exp(0.5 * v))], [1.5, 0.4, 0.1],
                NumericsLeastSquaresMethod.GaussNewton);

            NumericsTestSupport.VectorShouldMatch(gaussNewton.Parameters, [2.0, 0.5, 0.0], 1e-3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Sine, xs, xs, [1.0, 1.0]),
                "initialParameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Exponential, xs, [1.0],
                    [1.0, 1.0, 1.0]), "y");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FitNamedCurveLeastSquares(NumericsUnivariateFunction.Exponential, [1.0, 2.0],
                    [1.0, 2.0], [1.0, 1.0, 1.0]), "x");
        }

        [Fact]
        [Trait("Covers", "OptimisationTools.SolveAssignmentProblemMunkres")]
        public void SolveAssignmentProblemMunkresKnownAssignments()
        {
            var square = tools.SolveAssignmentProblemMunkres([
                [4.0, 1.0, 3.0], [2.0, 0.0, 5.0], [3.0, 2.0, 2.0]
            ]);

            square.ColumnForRow.Should().Equal(1, 0, 2);
            square.TotalCost.Should().Be(5.0);
            square.AssignedCount.Should().Be(3);

            var profits = new[] { new[] { 1.0, 2.0 }, new[] { 3.0, 10.0 } };
            tools.SolveAssignmentProblemMunkres(profits, true).TotalCost.Should().Be(11.0);
            tools.SolveAssignmentProblemMunkres(profits).TotalCost.Should().Be(5.0);

            var wide = tools.SolveAssignmentProblemMunkres([[1.0, 9.0, 9.0], [9.0, 1.0, 9.0]]);
            wide.ColumnForRow.Should().Equal(0, 1);
            wide.TotalCost.Should().Be(2.0);

            var tall = tools.SolveAssignmentProblemMunkres([[1.0, 9.0], [9.0, 1.0], [5.0, 5.0]]);

            tall.ColumnForRow.Should().Equal(0, 1, -1);
            tall.TotalCost.Should().Be(2.0);
            tall.AssignedCount.Should().Be(2);

            tools.SolveAssignmentProblemMunkres([[7.0]]).ColumnForRow.Should().Equal(0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveAssignmentProblemMunkres([[1.0, double.NaN]]), "costMatrix");
        }
    }
}
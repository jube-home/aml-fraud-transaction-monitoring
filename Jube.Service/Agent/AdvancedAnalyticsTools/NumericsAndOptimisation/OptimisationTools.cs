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

using System.ComponentModel;
using Accord.Math;
using Accord.Math.Differentiation;
using Accord.Math.Optimization;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public enum NumericsConjugateGradientVariant
    {
        FletcherReeves,
        PolakRibiere,
        PositivePolakRibiere
    }

    public enum NumericsUnconstrainedMethod
    {
        LimitedMemoryBfgs,
        ConjugateGradient,
        GradientDescent,
        NelderMead,
        Subplex
    }

    public enum NumericsConstraintRelation
    {
        GreaterThanOrEqual,
        LessThanOrEqual,
        Equal
    }

    public enum NumericsLeastSquaresMethod
    {
        LevenbergMarquardt,
        GaussNewton
    }

    public sealed record NumericsOptimisationResult
    {
        public double[] Solution { get; init; } = Array.Empty<double>();
        public double Value { get; init; }
        public bool Converged { get; init; }
        public string Status { get; init; } = string.Empty;
        public double GradientNorm { get; init; }
        public int Evaluations { get; init; }
    }

    public sealed record NumericsMethodComparisonEntry
    {
        public string Method { get; init; } = string.Empty;
        public double[] Solution { get; init; } = Array.Empty<double>();
        public double Value { get; init; }
        public bool Converged { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }

    public sealed record NumericsConstrainedResult
    {
        public double[] Solution { get; init; } = Array.Empty<double>();
        public double Value { get; init; }
        public bool Converged { get; init; }
        public string Status { get; init; } = string.Empty;
        public double[] LagrangeMultipliers { get; init; } = Array.Empty<double>();
        public double MaximumConstraintViolation { get; init; }
    }

    public sealed record NumericsCurveFitResult
    {
        public double[] Parameters { get; init; } = Array.Empty<double>();
        public double[] StandardErrors { get; init; } = Array.Empty<double>();
        public double SumOfSquaredResiduals { get; init; }
        public double RootMeanSquaredError { get; init; }
        public bool Converged { get; init; }
        public int Iterations { get; init; }
    }

    public sealed record NumericsAssignmentResult
    {
        public int[] ColumnForRow { get; init; } = Array.Empty<int>();
        public double TotalCost { get; init; }
        public int AssignedCount { get; init; }
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class OptimisationTools
    {
        private const int MaximumEvaluations = 200000;
        private static readonly TimeSpan timeLimit = TimeSpan.FromSeconds(20);

        private static void RequireSetup(double[] start, int maxIterations, double tolerance)
        {
            NumericsGuard.RequireVector(start, nameof(start), 1, NumericsGuard.MaxVariables);
            NumericsGuard.RequireInRange(maxIterations, nameof(maxIterations), 1, 100000);

            if (!(tolerance >= 1e-15 && tolerance <= 1))
            {
                throw new ArgumentException("'tolerance' must be between 1e-15 and 1.", nameof(tolerance));
            }
        }

        private static Func<double[], double[]> GradientFor(Func<double[], double> function,
            NumericsObjectiveFunction objective, int dimension, double[][]? matrix, double[]? linear)
        {
            var analytic = NumericsFunctions.Gradient(objective, dimension, matrix, linear, nameof(objective));

            if (analytic != null)
            {
                return analytic;
            }

            var differences = new FiniteDifferences(dimension, function, 1, 1e-4) { NumberOfPoints = 5 };

            return x => differences.Gradient(x);
        }

        private static double Norm(double[] v)
        {
            return Math.Sqrt(v.Sum(d => d * d));
        }

        private static NumericsOptimisationResult Finish(double[] solution, double value, bool converged, string status,
            Func<double[], double[]> gradient, int evaluations)
        {
            double gradientNorm;

            try
            {
                gradientNorm = Norm(gradient(solution));
            }
            catch (Exception)
            {
                gradientNorm = double.NaN;
            }

            return new NumericsOptimisationResult
            {
                Solution = (double[])solution.Clone(),
                Value = value,
                Converged = converged,
                Status = status,
                GradientNorm = gradientNorm,
                Evaluations = evaluations
            };
        }

        private static Func<double[], double> Counted(Func<double[], double> function, Counter counter)
        {
            return x =>
            {
                counter.Count++;

                return function(x);
            };
        }

        private static NumericsOptimisationResult RunLimitedMemoryBfgs(Func<double[], double> f,
            Func<double[], double[]> g,
            double[] start, int maxIterations, double tolerance)
        {
            var counter = new Counter();

            var solver = new BroydenFletcherGoldfarbShanno(start.Length, Counted(f, counter), g)
            {
                MaxIterations = maxIterations,
                Epsilon = tolerance,
                Token = new CancellationTokenSource(timeLimit).Token
            };

            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, solver.Status.ToString(), g, counter.Count);
        }

        private static NumericsOptimisationResult RunConjugateGradient(Func<double[], double> f,
            Func<double[], double[]> g,
            double[] start, int maxIterations, double tolerance, NumericsConjugateGradientVariant variant)
        {
            var counter = new Counter();

            var solver = new ConjugateGradient(start.Length, Counted(f, counter), g)
            {
                MaxIterations = maxIterations,
                Tolerance = tolerance,
                Method = variant switch
                {
                    NumericsConjugateGradientVariant.FletcherReeves => ConjugateGradientMethod.FletcherReeves,
                    NumericsConjugateGradientVariant.PolakRibiere => ConjugateGradientMethod.PolakRibiere,
                    _ => ConjugateGradientMethod.PositivePolakRibiere
                },
                Token = new CancellationTokenSource(timeLimit).Token
            };

            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, solver.Status.ToString(), g, counter.Count);
        }

        private static NumericsOptimisationResult RunGradientDescent(Func<double[], double> f,
            Func<double[], double[]> g,
            double[] start, int maxIterations, double tolerance, double learningRate)
        {
            var counter = new Counter();

            var solver = new GradientDescent
            {
                Function = Counted(f, counter),
                Gradient = g,
                NumberOfVariables = start.Length,
                LearningRate = learningRate,
                Iterations = maxIterations,
                Tolerance = tolerance,
                Token = new CancellationTokenSource(timeLimit).Token
            };

            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, converged ? "Success" : "NotConverged", g,
                counter.Count);
        }

        private static NumericsOptimisationResult RunNelderMead(Func<double[], double> f, Func<double[], double[]> g,
            double[] start, int maxEvaluations, double tolerance, double initialStep, double[]? lower, double[]? upper)
        {
            var counter = new Counter();
            var solver = new NelderMead(start.Length, Counted(f, counter));

            for (var i = 0; i < start.Length; i++)
            {
                solver.StepSize[i] = initialStep;

                if (lower != null)
                {
                    solver.LowerBounds[i] = lower[i];
                }

                if (upper != null)
                {
                    solver.UpperBounds[i] = upper[i];
                }
            }

            solver.Convergence.MaximumEvaluations = maxEvaluations;
            solver.Convergence.AbsoluteFunctionTolerance = tolerance;
            solver.Convergence.RelativeFunctionTolerance = tolerance;
            solver.Convergence.MaximumTime = timeLimit;
            solver.Convergence.StartTime = DateTime.Now;
            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, solver.Status.ToString(), g, counter.Count);
        }

        private static NumericsOptimisationResult RunSubplex(Func<double[], double> f, Func<double[], double[]> g,
            double[] start, int maxEvaluations, double tolerance, double initialStep, double[]? lower, double[]? upper)
        {
            var counter = new Counter();
            var solver = new Subplex(start.Length, Counted(f, counter));

            for (var i = 0; i < start.Length; i++)
            {
                solver.StepSize[i] = initialStep;

                if (lower != null)
                {
                    solver.LowerBounds[i] = lower[i];
                }

                if (upper != null)
                {
                    solver.UpperBounds[i] = upper[i];
                }
            }

            solver.Convergence.MaximumEvaluations = maxEvaluations;
            solver.Convergence.AbsoluteFunctionTolerance = tolerance;
            solver.Convergence.RelativeFunctionTolerance = tolerance;
            solver.Convergence.MaximumTime = timeLimit;
            solver.Convergence.StartTime = DateTime.Now;
            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, solver.Status.ToString(), g, counter.Count);
        }

        private static void RequireBounds(double[]? lowerBounds, double[]? upperBounds, int dimension)
        {
            if (lowerBounds != null && lowerBounds.Length > 0)
            {
                NumericsGuard.RequireVector(lowerBounds, nameof(lowerBounds), dimension, dimension, false);
            }

            if (upperBounds != null && upperBounds.Length > 0)
            {
                NumericsGuard.RequireVector(upperBounds, nameof(upperBounds), dimension, dimension, false);
            }

            if (lowerBounds != null && lowerBounds.Length > 0 && upperBounds != null && upperBounds.Length > 0)
            {
                for (var i = 0; i < dimension; i++)
                {
                    if (lowerBounds[i] >= upperBounds[i])
                    {
                        throw new ArgumentException($"Lower bound {i} must be below upper bound {i}.",
                            nameof(lowerBounds));
                    }
                }
            }
        }

        [AdvancedAnalyticsTool("EvaluateNamedObjective")]
        [Description(
            "Evaluates a built-in multi-variable test objective (Sphere, Rosenbrock, Rastrigin, Ackley, Himmelblau, Booth, Beale, Matyas, Styblinski-Tang, Griewank, or a Quadratic 0.5 x'Qx + c'x + constant) at one or more points, returning the value at each. " +
            "Use it to inspect a landscape or to confirm the value at a candidate optimum before or after running an optimiser. Each row of 'points' is one point and all rows must have the same number of variables (at most 50). Up to 10000 points.")]
        public NumericsVectorResult EvaluateNamedObjective(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Points at which to evaluate, one row per point with one value per variable.")]
            double[][] points,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            NumericsGuard.RequireMatrix(points, nameof(points), out var rows, out var columns);

            if (rows > 10000)
            {
                throw new ArgumentException("At most 10000 points can be evaluated per call.", nameof(points));
            }

            var f = NumericsFunctions.Objective(objective, columns, quadraticMatrix, linearTerms, constantTerm,
                nameof(points));

            return NumericsGuard.ToVectorResult(points.Select(p => f(p)).ToArray());
        }

        [AdvancedAnalyticsTool("MinimiseWithLimitedMemoryBfgs")]
        [Description(
            "Minimises a built-in smooth objective from a starting point with the limited-memory BFGS quasi-Newton method, a fast general-purpose choice for smooth functions of many variables that uses the gradient. " +
            "Solution is the best point found, Value the objective there, GradientNorm the size of the gradient at that point (close to 0 at a true minimum) and Converged whether the method reported success. " +
            "Local search only: multimodal objectives such as Rastrigin return whichever minimum is nearest the start. Up to 50 variables and 100000 iterations, with a 20 second time cap.")]
        public NumericsOptimisationResult MinimiseWithLimitedMemoryBfgs(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable (1 to 50).")]
            double[] start,
            [Description("Maximum iterations, from 1 to 100000.")]
            int maxIterations = 500,
            [Description("Gradient convergence tolerance, between 1e-15 and 1. Default 1e-8.")]
            double tolerance = 1e-8,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, tolerance);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            return RunLimitedMemoryBfgs(f, g, start, maxIterations, tolerance);
        }

        [AdvancedAnalyticsTool("MinimiseWithBoundedBfgs")]
        [Description(
            "Minimises a built-in smooth objective from a starting point subject to simple lower and upper bounds on each variable, using the bound-constrained limited-memory BFGS method (L-BFGS-B). " +
            "Use it when variables must stay in a box, such as probabilities in [0, 1]. The start must lie inside the bounds; an empty bound array means unbounded on that side. GradientNorm here is the unprojected gradient so it can stay non-zero at a bound. Up to 50 variables and 100000 iterations.")]
        public NumericsOptimisationResult MinimiseWithBoundedBfgs(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point inside the bounds, one value per variable.")]
            double[] start,
            [Description(
                "Lower bound per variable; leave empty for none. Use a very large negative number rather than infinity.")]
            double[]? lowerBounds = null,
            [Description("Upper bound per variable; leave empty for none.")]
            double[]? upperBounds = null,
            [Description("Maximum iterations, from 1 to 100000.")]
            int maxIterations = 500,
            [Description("Function value convergence tolerance, between 1e-15 and 1. Default 1e-10.")]
            double tolerance = 1e-10,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, tolerance);
            RequireBounds(lowerBounds, upperBounds, start.Length);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);
            var counter = new Counter();

            var solver = new BoundedBroydenFletcherGoldfarbShanno(start.Length, Counted(f, counter), g)
            {
                MaxIterations = maxIterations,
                FunctionTolerance = tolerance
            };

            for (var i = 0; i < start.Length; i++)
            {
                if (lowerBounds != null && lowerBounds.Length > 0)
                {
                    solver.LowerBounds[i] = lowerBounds[i];
                }

                if (upperBounds != null && upperBounds.Length > 0)
                {
                    solver.UpperBounds[i] = upperBounds[i];
                }
            }

            var converged = solver.Minimize((double[])start.Clone());

            return Finish(solver.Solution, solver.Value, converged, solver.Status.ToString(), g, counter.Count);
        }

        [AdvancedAnalyticsTool("MinimiseWithConjugateGradient")]
        [Description(
            "Minimises a built-in smooth objective with the nonlinear conjugate gradient method, which needs the gradient but little memory and suits large problems. " +
            "Choose Fletcher-Reeves, Polak-Ribiere or Positive Polak-Ribiere direction updates (the last is the most robust). Solution, Value, GradientNorm and Converged are read as for the other minimisers. Up to 50 variables and 100000 iterations, 20 second cap.")]
        public NumericsOptimisationResult MinimiseWithConjugateGradient(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable.")]
            double[] start,
            [Description("Direction update formula.")]
            NumericsConjugateGradientVariant variant = NumericsConjugateGradientVariant.PositivePolakRibiere,
            [Description("Maximum iterations, from 1 to 100000.")]
            int maxIterations = 1000,
            [Description("Convergence tolerance, between 1e-15 and 1. Default 1e-8.")]
            double tolerance = 1e-8,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, tolerance);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            return RunConjugateGradient(f, g, start, maxIterations, tolerance, variant);
        }

        [AdvancedAnalyticsTool("MinimiseWithGradientDescent")]
        [Description(
            "Minimises a built-in smooth objective by plain gradient descent with a fixed learning rate: repeatedly step against the gradient. It is the simplest method and slow on curved valleys such as Rosenbrock, but easy to reason about. " +
            "A learning rate that is too large diverges (Converged false, huge Value) and one too small crawls. Up to 50 variables and 100000 iterations, 20 second cap.")]
        public NumericsOptimisationResult MinimiseWithGradientDescent(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable.")]
            double[] start,
            [Description("Step size multiplying the gradient, greater than 0 and at most 10. Default 0.01.")]
            double learningRate = 0.01,
            [Description("Maximum iterations, from 1 to 100000.")]
            int maxIterations = 10000,
            [Description("Convergence tolerance on the relative change in the objective, between 1e-15 and 1.")]
            double tolerance = 1e-10,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, tolerance);

            if (!(learningRate > 0 && learningRate <= 10))
            {
                throw new ArgumentException("'learningRate' must be greater than 0 and at most 10.",
                    nameof(learningRate));
            }

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            return RunGradientDescent(f, g, start, maxIterations, tolerance, learningRate);
        }

        [AdvancedAnalyticsTool("MinimiseWithNelderMead")]
        [Description(
            "Minimises a built-in objective with the Nelder-Mead simplex method, which uses only function values (no gradient) and therefore tolerates rough or noisy surfaces, at the cost of slow convergence in many dimensions. " +
            "Optional lower and upper bounds keep the search in a box. The initial step sets the size of the starting simplex; use roughly the scale of the expected solution. Evaluations are limited to 200000 and time to 20 seconds.")]
        public NumericsOptimisationResult MinimiseWithNelderMead(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable.")]
            double[] start,
            [Description("Maximum function evaluations, from 1 to 200000.")]
            int maxEvaluations = 20000,
            [Description("Convergence tolerance on the function value, between 1e-15 and 1. Default 1e-10.")]
            double tolerance = 1e-10,
            [Description("Initial simplex step per variable, greater than 0. Default 0.5.")]
            double initialStep = 0.5,
            [Description("Optional lower bound per variable; empty for none.")]
            double[]? lowerBounds = null,
            [Description("Optional upper bound per variable; empty for none.")]
            double[]? upperBounds = null,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, 1, tolerance);
            NumericsGuard.RequireInRange(maxEvaluations, nameof(maxEvaluations), 1, MaximumEvaluations);
            NumericsGuard.RequirePositive(initialStep, nameof(initialStep));
            RequireBounds(lowerBounds, upperBounds, start.Length);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            return RunNelderMead(f, g, start, maxEvaluations, tolerance, initialStep,
                lowerBounds != null && lowerBounds.Length > 0 ? lowerBounds : null,
                upperBounds != null && upperBounds.Length > 0 ? upperBounds : null);
        }

        [AdvancedAnalyticsTool("MinimiseWithSubplex")]
        [Description(
            "Minimises a built-in objective with the Subplex method, which runs Nelder-Mead on rotating subspaces of the variables and is markedly better than plain Nelder-Mead when there are many variables (10 to 50). No gradient is needed. " +
            "Optional bounds keep the search in a box. Evaluations are limited to 200000 and time to 20 seconds.")]
        public NumericsOptimisationResult MinimiseWithSubplex(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable.")]
            double[] start,
            [Description("Maximum function evaluations, from 1 to 200000.")]
            int maxEvaluations = 50000,
            [Description("Convergence tolerance on the function value, between 1e-15 and 1. Default 1e-10.")]
            double tolerance = 1e-10,
            [Description("Initial step per variable, greater than 0. Default 0.5.")]
            double initialStep = 0.5,
            [Description("Optional lower bound per variable; empty for none.")]
            double[]? lowerBounds = null,
            [Description("Optional upper bound per variable; empty for none.")]
            double[]? upperBounds = null,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, 1, tolerance);
            NumericsGuard.RequireInRange(maxEvaluations, nameof(maxEvaluations), 1, MaximumEvaluations);
            NumericsGuard.RequirePositive(initialStep, nameof(initialStep));
            RequireBounds(lowerBounds, upperBounds, start.Length);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            return RunSubplex(f, g, start, maxEvaluations, tolerance, initialStep,
                lowerBounds != null && lowerBounds.Length > 0 ? lowerBounds : null,
                upperBounds != null && upperBounds.Length > 0 ? upperBounds : null);
        }

        [AdvancedAnalyticsTool("CompareUnconstrainedOptimisers")]
        [Description(
            "Runs several unconstrained minimisers (limited-memory BFGS, conjugate gradient, gradient descent, Nelder-Mead and Subplex) on the same built-in objective and start, and returns one row per method with its solution, value, convergence flag and any error. " +
            "Use it to see whether methods agree (good evidence of a true minimum) or land in different local minima. Methods that fail are reported in the Error field rather than aborting the call. Up to 20 variables; each method is capped at 20 seconds.")]
        public NumericsMethodComparisonEntry[] CompareUnconstrainedOptimisers(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable (1 to 20).")]
            double[] start,
            [Description("Iteration budget given to each method, from 10 to 100000.")]
            int maxIterations = 2000,
            [Description("Convergence tolerance, between 1e-15 and 1. Default 1e-8.")]
            double tolerance = 1e-8,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, tolerance);
            NumericsGuard.RequireVector(start, nameof(start), 1, 20);

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var g = GradientFor(f, objective, start.Length, quadraticMatrix, linearTerms);

            var runs = new List<(string Name, Func<NumericsOptimisationResult> Run)>
            {
                ("LimitedMemoryBfgs", () => RunLimitedMemoryBfgs(f, g, start, maxIterations, tolerance)),
                ("ConjugateGradient",
                    () => RunConjugateGradient(f, g, start, maxIterations, tolerance,
                        NumericsConjugateGradientVariant.PositivePolakRibiere)),
                ("GradientDescent", () => RunGradientDescent(f, g, start, maxIterations, tolerance, 0.01)),
                ("NelderMead",
                    () => RunNelderMead(f, g, start, Math.Min(maxIterations * 10, MaximumEvaluations), tolerance, 0.5,
                        null,
                        null)),
                ("Subplex",
                    () => RunSubplex(f, g, start, Math.Min(maxIterations * 10, MaximumEvaluations), tolerance, 0.5,
                        null,
                        null))
            };

            var entries = new List<NumericsMethodComparisonEntry>();

            foreach (var (name, run) in runs)
            {
                try
                {
                    var result = run();

                    entries.Add(new NumericsMethodComparisonEntry
                    {
                        Method = name,
                        Solution = result.Solution,
                        Value = result.Value,
                        Converged = result.Converged,
                        Status = result.Status
                    });
                }
                catch (Exception exception)
                {
                    entries.Add(new NumericsMethodComparisonEntry
                        { Method = name, Error = exception.Message, Value = double.NaN });
                }
            }

            return entries.ToArray();
        }

        private static (double[,] Matrix, double[] Values, NumericsConstraintRelation[] Relations)
            RequireLinearConstraints(
                double[][]? constraintMatrix, double[]? constraintValues,
                NumericsConstraintRelation[]? constraintRelations, int variables)
        {
            if (constraintMatrix == null || constraintMatrix.Length == 0)
            {
                return (new double[0, variables], Array.Empty<double>(), Array.Empty<NumericsConstraintRelation>());
            }

            NumericsGuard.RequireMatrix(constraintMatrix, nameof(constraintMatrix), out var rows, out var columns);

            if (columns != variables)
            {
                throw new ArgumentException(
                    $"'constraintMatrix' has {columns} columns but there are {variables} variables; each row needs one coefficient per variable.",
                    nameof(constraintMatrix));
            }

            if (rows > 100)
            {
                throw new ArgumentException("At most 100 constraints are allowed.", nameof(constraintMatrix));
            }

            NumericsGuard.RequireVector(constraintValues, nameof(constraintValues), rows, rows);

            var rel = constraintRelations == null || constraintRelations.Length == 0
                ? Enumerable.Repeat(NumericsConstraintRelation.GreaterThanOrEqual, rows).ToArray()
                : constraintRelations;

            if (rel.Length != rows)
            {
                throw new ArgumentException(
                    $"'constraintRelations' must be empty (all greater-or-equal) or have {rows} entries, one per constraint row.",
                    nameof(constraintRelations));
            }

            return (NumericsGuard.ToRectangular(constraintMatrix), constraintValues!, rel);
        }

        private static double Violation(double[,] a, double[] b, NumericsConstraintRelation[] relations, double[] x)
        {
            var worst = 0.0;

            for (var i = 0; i < b.Length; i++)
            {
                var lhs = 0.0;

                for (var j = 0; j < x.Length; j++)
                {
                    lhs += a[i, j] * x[j];
                }

                var violation = relations[i] switch
                {
                    NumericsConstraintRelation.GreaterThanOrEqual => b[i] - lhs,
                    NumericsConstraintRelation.LessThanOrEqual => lhs - b[i],
                    _ => Math.Abs(lhs - b[i])
                };

                worst = Math.Max(worst, violation);
            }

            return worst;
        }

        [AdvancedAnalyticsTool("SolveQuadraticProgram")]
        [Description(
            "Solves the convex quadratic programme: minimise 0.5 x'Qx + c'x subject to linear constraints, each row of the constraint matrix A combined with a relation (>=, <=, =) and a value b, using the exact Goldfarb-Idnani dual active-set method. " +
            "Q must be symmetric positive definite (the strictly convex case), otherwise Status reports NonPositiveDefinite; infeasible constraints give NoPossibleSolution. LagrangeMultipliers has one value per constraint row (zero for inactive constraints) and shows which constraints bind. " +
            "Up to 50 variables and 100 constraints. Typical uses are portfolio weights with a budget constraint and least squares with bounds.")]
        public NumericsConstrainedResult SolveQuadraticProgram(
            [Description("Symmetric positive definite matrix Q of quadratic terms, one row per variable.")]
            double[][] quadraticMatrix,
            [Description("Vector c of linear terms, one per variable; empty means zeros.")]
            double[]? linearTerms = null,
            [Description(
                "Constraint matrix A: one row per constraint, one coefficient per variable; empty for an unconstrained problem.")]
            double[][]? constraintMatrix = null,
            [Description("Right hand side b of each constraint row.")]
            double[]? constraintValues = null,
            [Description(
                "Relation for each constraint row: GreaterThanOrEqual, LessThanOrEqual or Equal; empty means all GreaterThanOrEqual.")]
            NumericsConstraintRelation[]? constraintRelations = null)
        {
            NumericsGuard.RequireMatrix(quadraticMatrix, nameof(quadraticMatrix), out var n, out _, symmetric: true);

            if (n > NumericsGuard.MaxVariables)
            {
                throw new ArgumentException($"At most {NumericsGuard.MaxVariables} variables are allowed.",
                    nameof(quadraticMatrix));
            }

            var linear = linearTerms == null || linearTerms.Length == 0 ? new double[n] : linearTerms;
            NumericsGuard.RequireVector(linear, nameof(linearTerms), n, n);

            var (a, b, relations) =
                RequireLinearConstraints(constraintMatrix, constraintValues, constraintRelations, n);

            var order = Enumerable.Range(0, b.Length)
                .OrderBy(i => relations[i] == NumericsConstraintRelation.Equal ? 0 : 1)
                .ToArray();

            var rows = b.Length;
            var arranged = new double[rows, n];
            var arrangedValues = new double[rows];

            for (var r = 0; r < rows; r++)
            {
                var source = order[r];
                var sign = relations[source] == NumericsConstraintRelation.LessThanOrEqual ? -1.0 : 1.0;

                for (var j = 0; j < n; j++)
                {
                    arranged[r, j] = sign * a[source, j];
                }

                arrangedValues[r] = sign * b[source];
            }

            var equalities = relations.Count(rel => rel == NumericsConstraintRelation.Equal);

            var solver = new GoldfarbIdnani(NumericsGuard.ToRectangular(quadraticMatrix), linear, arranged,
                arrangedValues,
                equalities);

            var success = solver.Minimize();
            var x = solver.Solution;

            var f = NumericsFunctions.Objective(NumericsObjectiveFunction.Quadratic, n, quadraticMatrix, linear, 0,
                nameof(quadraticMatrix));

            var multipliers = new double[rows];

            if (solver.Lagrangian != null)
            {
                for (var r = 0; r < rows; r++)
                {
                    multipliers[order[r]] = r < solver.Lagrangian.Length ? solver.Lagrangian[r] : 0.0;
                }
            }

            return new NumericsConstrainedResult
            {
                Solution = (double[])x.Clone(),
                Value = f(x),
                Converged = success,
                Status = solver.Status.ToString(),
                LagrangeMultipliers = multipliers,
                MaximumConstraintViolation = Violation(a, b, relations, x)
            };
        }

        [AdvancedAnalyticsTool("MinimiseWithLinearConstraintsCobyla")]
        [Description(
            "Minimises a built-in objective subject to linear constraints A x (>=, <=, =) b using COBYLA, a derivative-free method that builds linear approximations inside a shrinking trust region. " +
            "It handles non-quadratic objectives and returns Converged true when it stops on the requested precision; MaximumConstraintViolation should be close to 0 for a feasible answer. Constraints are optional (none means unconstrained). " +
            "Up to 50 variables and 100 constraints; iterations are limited to 100000.")]
        public NumericsConstrainedResult MinimiseWithLinearConstraintsCobyla(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Starting point, one value per variable (1 to 50).")]
            double[] start,
            [Description("Constraint matrix A: one row per constraint, one coefficient per variable; empty for none.")]
            double[][]? constraintMatrix = null,
            [Description("Right hand side b of each constraint row.")]
            double[]? constraintValues = null,
            [Description(
                "Relation for each constraint row: GreaterThanOrEqual, LessThanOrEqual or Equal; empty means all GreaterThanOrEqual.")]
            NumericsConstraintRelation[]? constraintRelations = null,
            [Description("Maximum function evaluations, from 1 to 100000.")]
            int maxIterations = 5000,
            [Description("Final trust region size (precision of the answer), between 1e-12 and 1. Default 1e-8.")]
            double precision = 1e-8,
            [Description("Initial trust region size, greater than 0. Default 0.5.")]
            double initialStep = 0.5,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            RequireSetup(start, maxIterations, 1e-6);

            if (!(precision >= 1e-12 && precision <= 1))
            {
                throw new ArgumentException("'precision' must be between 1e-12 and 1.", nameof(precision));
            }

            NumericsGuard.RequirePositive(initialStep, nameof(initialStep));

            var f = NumericsFunctions.Objective(objective, start.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(start));

            var (a, b, relations) =
                RequireLinearConstraints(constraintMatrix, constraintValues, constraintRelations, start.Length);

            return RunCobyla(f, start, a, b, relations, maxIterations, precision, initialStep);
        }

        private static NumericsConstrainedResult RunCobyla(Func<double[], double> f, double[] start, double[,] a,
            double[] b, NumericsConstraintRelation[] relations, int maxIterations, double precision, double initialStep)
        {
            var n = start.Length;
            var constraints = new List<NonlinearConstraint>();

            for (var i = 0; i < b.Length; i++)
            {
                var row = new double[n];

                for (var j = 0; j < n; j++)
                {
                    row[j] = a[i, j];
                }

                Func<double[], double> linearRow = x => row.Zip(x, (p, q) => p * q).Sum();

                if (relations[i] != NumericsConstraintRelation.LessThanOrEqual)
                {
                    constraints.Add(new NonlinearConstraint(n, linearRow, ConstraintType.GreaterThanOrEqualTo, b[i]));
                }

                if (relations[i] != NumericsConstraintRelation.GreaterThanOrEqual)
                {
                    constraints.Add(new NonlinearConstraint(n, linearRow, ConstraintType.LesserThanOrEqualTo, b[i]));
                }
            }

            var counter = new Counter();

            var solver = new Cobyla(new NonlinearObjectiveFunction(n, Counted(f, counter)), constraints.ToArray())
            {
                MaxIterations = maxIterations,
                RhoEnd = precision,
                RhoBeg = initialStep,
                Token = new CancellationTokenSource(timeLimit).Token
            };

            var converged = solver.Minimize((double[])start.Clone());

            return new NumericsConstrainedResult
            {
                Solution = (double[])solver.Solution.Clone(),
                Value = solver.Value,
                Converged = converged,
                Status = solver.Status.ToString(),
                MaximumConstraintViolation = Violation(a, b, relations, solver.Solution)
            };
        }

        [AdvancedAnalyticsTool("SolveLinearProgram")]
        [Description(
            "Solves a linear programme: maximise or minimise c'x subject to linear constraints A x (>=, <=, =) b and simple bounds on each variable (lower bounds default to 0, the usual non-negativity). " +
            "It uses COBYLA, which is a general derivative-free solver, so the answer is accurate to roughly 1e-6 rather than exact; check MaximumConstraintViolation and Converged, and treat an unbounded problem as non-converged. " +
            "Use it for resource allocation and blending problems. Up to 50 variables and 100 constraints.")]
        public NumericsConstrainedResult SolveLinearProgram(
            [Description("Objective coefficients c, one per variable.")]
            double[] objectiveCoefficients,
            [Description("Constraint matrix A: one row per constraint, one coefficient per variable.")]
            double[][] constraintMatrix,
            [Description("Right hand side b of each constraint row.")]
            double[] constraintValues,
            [Description(
                "Relation for each constraint row: GreaterThanOrEqual, LessThanOrEqual or Equal; empty means all LessThanOrEqual.")]
            NumericsConstraintRelation[]? constraintRelations = null,
            [Description("True to maximise c'x; false to minimise.")]
            bool maximise = true,
            [Description("Lower bound applied to every variable. Default 0.")]
            double lowerBound = 0,
            [Description("Optional upper bound applied to every variable; leave NaN for none.")]
            double upperBound = double.NaN)
        {
            NumericsGuard.RequireVector(objectiveCoefficients, nameof(objectiveCoefficients), 1,
                NumericsGuard.MaxVariables);

            NumericsGuard.RequireFinite(lowerBound, nameof(lowerBound));
            var n = objectiveCoefficients.Length;

            var relations = constraintRelations == null || constraintRelations.Length == 0
                ? Enumerable.Repeat(NumericsConstraintRelation.LessThanOrEqual, constraintMatrix.Length).ToArray()
                : constraintRelations;

            var (a, b, rel) = RequireLinearConstraints(constraintMatrix, constraintValues, relations, n);
            var extra = new List<double[]>();
            var extraValues = new List<double>();
            var extraRelations = new List<NumericsConstraintRelation>();

            for (var j = 0; j < n; j++)
            {
                var lowerRow = new double[n];
                lowerRow[j] = 1;
                extra.Add(lowerRow);
                extraValues.Add(lowerBound);
                extraRelations.Add(NumericsConstraintRelation.GreaterThanOrEqual);

                if (!double.IsNaN(upperBound))
                {
                    extra.Add((double[])lowerRow.Clone());
                    extraValues.Add(upperBound);
                    extraRelations.Add(NumericsConstraintRelation.LessThanOrEqual);
                }
            }

            var total = b.Length + extra.Count;
            var all = new double[total, n];
            var allValues = new double[total];
            var allRelations = new NumericsConstraintRelation[total];

            for (var i = 0; i < b.Length; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    all[i, j] = a[i, j];
                }

                allValues[i] = b[i];
                allRelations[i] = rel[i];
            }

            for (var i = 0; i < extra.Count; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    all[b.Length + i, j] = extra[i][j];
                }

                allValues[b.Length + i] = extraValues[i];
                allRelations[b.Length + i] = extraRelations[i];
            }

            var sign = maximise ? -1.0 : 1.0;
            var start = new double[n];

            for (var j = 0; j < n; j++)
            {
                start[j] = double.IsNaN(upperBound) ? lowerBound + 1 : (lowerBound + upperBound) / 2;
            }

            var result = RunCobyla(x => sign * x.Zip(objectiveCoefficients, (p, q) => p * q).Sum(), start, all,
                allValues,
                allRelations, 20000, 1e-9, 1.0);

            return result with { Value = sign * result.Value };
        }

        [AdvancedAnalyticsTool("FitNamedCurveLeastSquares")]
        [Description(
            "Fits the parameters of a built-in curve (polynomial, sine, cosine, exponential, logarithm, square root, power, reciprocal, Gaussian, logistic, hyperbolic tangent or absolute value) to (x, y) data by non-linear least squares with the Levenberg-Marquardt or Gauss-Newton method, using exact parameter derivatives. " +
            "You must give a starting guess with one value per parameter in the order listed for the function (polynomial: one per coefficient). Results depend on the start for non-linear families, so try several starts. " +
            "Parameters are the fitted values, StandardErrors their approximate standard errors, SumOfSquaredResiduals the fit error and RootMeanSquaredError its size in y units. Up to 200000 points and 1000 iterations.")]
        public NumericsCurveFitResult FitNamedCurveLeastSquares(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description("x values of the data points.")]
            double[] x,
            [Description("y values of the data points, same length as x.")]
            double[] y,
            [Description(
                "Starting values for every parameter in order (exactly 2 to 4 numbers depending on the function; any length up to 40 for a polynomial).")]
            double[] initialParameters,
            [Description("Which least squares algorithm to use.")]
            NumericsLeastSquaresMethod method = NumericsLeastSquaresMethod.LevenbergMarquardt,
            [Description("Maximum iterations, from 1 to 1000.")]
            int maxIterations = 200,
            [Description("Relative convergence tolerance on the error, between 1e-15 and 0.1.")]
            double tolerance = 1e-10)
        {
            NumericsGuard.RequireVector(x, nameof(x), 2);
            NumericsGuard.RequireVector(y, nameof(y), x.Length, x.Length);
            NumericsGuard.RequireInRange(maxIterations, nameof(maxIterations), 1, 1000);

            if (!(tolerance >= 1e-15 && tolerance <= 0.1))
            {
                throw new ArgumentException("'tolerance' must be between 1e-15 and 0.1.", nameof(tolerance));
            }

            NumericsGuard.RequireVector(initialParameters, nameof(initialParameters), 1,
                NumericsGuard.MaxPolynomialCoefficients);

            var expected = NumericsFunctions.ParameterCount(function, initialParameters.Length);

            if (initialParameters.Length != expected)
            {
                throw new ArgumentException(
                    $"The {function} function has {expected} parameters but {initialParameters.Length} starting values were given; supply exactly {expected}.",
                    nameof(initialParameters));
            }

            if (x.Length < expected)
            {
                throw new ArgumentException($"At least {expected} data points are needed to fit {expected} parameters.",
                    nameof(x));
            }

            var inputs = x.Select(v => new[] { v }).ToArray();

            LeastSquaresFunction model = (p, input) =>
                NumericsFunctions.Univariate(function, p, nameof(initialParameters))(input[0]);

            LeastSquaresGradientFunction gradient = (p, input, result) =>
            {
                var partials = NumericsFunctions.PartialDerivatives(function, p, input[0]);

                for (var i = 0; i < partials.Length; i++)
                {
                    result[i] = partials[i];
                }
            };

            double[] parameters;
            double[] errors;
            bool converged;
            int iterations;
            double sum;

            if (method == NumericsLeastSquaresMethod.LevenbergMarquardt)
            {
                var solver = new LevenbergMarquardt(expected)
                    { Function = model, Gradient = gradient, MaxIterations = maxIterations, Tolerance = tolerance };

                solver.Solution = (double[])initialParameters.Clone();
                solver.Minimize(inputs, y);
                parameters = solver.Solution;
                errors = solver.StandardErrors ?? Array.Empty<double>();
                converged = solver.HasConverged;
                iterations = solver.CurrentIteration;
            }
            else
            {
                var solver = new GaussNewton(expected)
                    { Function = model, Gradient = gradient, MaxIterations = maxIterations, Tolerance = tolerance };

                solver.Solution = (double[])initialParameters.Clone();
                solver.Minimize(inputs, y);
                parameters = solver.Solution;
                errors = solver.StandardErrors ?? Array.Empty<double>();
                converged = solver.HasConverged;
                iterations = solver.CurrentIteration;
            }

            var fitted = NumericsFunctions.Univariate(function, parameters, nameof(initialParameters));
            sum = 0;

            for (var i = 0; i < x.Length; i++)
            {
                var difference = y[i] - fitted(x[i]);
                sum += difference * difference;
            }

            return new NumericsCurveFitResult
            {
                Parameters = (double[])parameters.Clone(),
                StandardErrors = errors,
                SumOfSquaredResiduals = sum,
                RootMeanSquaredError = Math.Sqrt(sum / x.Length),
                Converged = converged && double.IsFinite(sum),
                Iterations = iterations
            };
        }

        [AdvancedAnalyticsTool("SolveAssignmentProblemMunkres")]
        [Description(
            "Solves the assignment problem with the Hungarian (Munkres) algorithm: given a cost matrix with one row per worker and one column per task, choose one task for each worker (each task at most once) to minimise the total cost, or maximise it. " +
            "ColumnForRow[i] is the task assigned to worker i, or -1 when a worker gets no task in a rectangular matrix; TotalCost is the summed original cost. " +
            "Use it to match transactions to accounts, analysts to cases and similar one-to-one matchings. Up to 300 by 300; costs must be finite.")]
        public NumericsAssignmentResult SolveAssignmentProblemMunkres(
            [Description("Cost matrix with one row per worker and one column per task.")]
            double[][] costMatrix,
            [Description("True to maximise the total (profits) instead of minimising cost.")]
            bool maximise = false)
        {
            NumericsGuard.RequireMatrix(costMatrix, nameof(costMatrix), out var rows, out var columns);
            var size = Math.Max(rows, columns);
            var padded = new double[size][];

            for (var i = 0; i < size; i++)
            {
                padded[i] = new double[size];

                for (var j = 0; j < size; j++)
                {
                    padded[i][j] = i < rows && j < columns ? costMatrix[i][j] : 0.0;
                }
            }

            var munkres = new Munkres(padded);

            if (maximise)
            {
                munkres.Maximize();
            }
            else
            {
                munkres.Minimize();
            }

            var assignment = new int[rows];
            var total = 0.0;
            var assigned = 0;

            for (var i = 0; i < rows; i++)
            {
                var column = (int)Math.Round(munkres.Solution[i]);

                if (column >= 0 && column < columns)
                {
                    assignment[i] = column;
                    total += costMatrix[i][column];
                    assigned++;
                }
                else
                {
                    assignment[i] = -1;
                }
            }

            return new NumericsAssignmentResult
                { ColumnForRow = assignment, TotalCost = total, AssignedCount = assigned };
        }

        private sealed class Counter
        {
            public int Count;
        }
    }
}
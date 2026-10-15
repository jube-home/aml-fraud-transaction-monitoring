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
using Accord;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Math.Differentiation;
using Accord.Math.Integration;
using Accord.Math.Optimization;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public sealed record NumericsIntegrationResult
    {
        public double Area { get; init; }
        public double ErrorEstimate { get; init; }
        public bool Converged { get; init; }
        public string Status { get; init; } = string.Empty;
        public int FunctionEvaluations { get; init; }
    }

    public sealed record NumericsRootResult
    {
        public double Solution { get; init; }
        public double FunctionValue { get; init; }
        public bool Converged { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public sealed record NumericsBracketResult
    {
        public double[] Lower { get; init; } = Array.Empty<double>();
        public double[] Upper { get; init; } = Array.Empty<double>();
        public int Count { get; init; }
        public double[] ExactZeros { get; init; } = Array.Empty<double>();
    }

    public sealed record NumericsPolynomialRootsResult
    {
        public double[] RealParts { get; init; } = Array.Empty<double>();
        public double[] ImaginaryParts { get; init; } = Array.Empty<double>();
        public int Degree { get; init; }
    }

    public sealed record NumericsPolynomialFitResult
    {
        public double[] Coefficients { get; init; } = Array.Empty<double>();
        public double ResidualSumOfSquares { get; init; }
        public int Degree { get; init; }
    }

    public enum NumericsVectorFunction
    {
        PolarToCartesian,
        SphericalToCartesian,
        Softmax,
        RosenbrockResiduals,
        HimmelblauResiduals
    }

    public enum NumericsPolynomialCalculus
    {
        Derivative,
        Antiderivative
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class CalculusAndRootFindingTools
    {
        private const int MaxIntegrationSteps = 1000000;
        private const int MaxRombergSteps = 30;

        private static void RequireInterval(double lower, double upper)
        {
            NumericsGuard.RequireFinite(lower, nameof(lower));
            NumericsGuard.RequireFinite(upper, nameof(upper));

            if (!(upper > lower))
            {
                throw new ArgumentException(
                    $"The upper limit must be greater than the lower limit but got {lower} to {upper}; swap them and negate the result if you meant a reversed integral.",
                    nameof(upper));
            }
        }

        [AdvancedAnalyticsTool("EvaluateNamedFunction")]
        [Description(
            "Evaluates one of the built-in one-variable functions (polynomial, sine, cosine, exponential, logarithm, square root, power, reciprocal, Gaussian, logistic, hyperbolic tangent or absolute value) at each supplied x, giving a table of values. " +
            "Use it to inspect a function before integrating, differentiating or searching for roots with the other tools. Points outside the function's domain give NaN. Maximum 200000 values.")]
        public NumericsVectorResult EvaluateNamedFunction(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Values of x at which to evaluate the function.")]
            double[] x)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            NumericsGuard.RequireVector(x, nameof(x));

            return NumericsGuard.ToVectorResult(x.Select(v => f(v)).ToArray());
        }

        [AdvancedAnalyticsTool("IntegrateTrapezoidalRule")]
        [Description(
            "Approximates the definite integral of a built-in function from 'lower' to 'upper' with the composite trapezoidal rule using the given number of equal steps. It is simple and robust but only second-order accurate, so the error shrinks with the square of the step. " +
            "Use it as a baseline and compare with IntegrateRomberg or IntegrateGaussKronrod. Steps are limited to 1000000 and the limits must be finite with upper greater than lower.")]
        public NumericsIntegrationResult IntegrateTrapezoidalRule(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Lower limit of integration.")]
            double lower,
            [Description("Upper limit of integration, greater than lower.")]
            double upper,
            [Description("Number of equal-width steps, from 1 to 1000000.")]
            int steps = 1000)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            NumericsGuard.RequireInRange(steps, nameof(steps), 1, MaxIntegrationSteps);
            var area = TrapezoidalRule.Integrate(f, lower, upper, steps);

            return new NumericsIntegrationResult
            {
                Area = area,
                ErrorEstimate = double.NaN,
                Converged = double.IsFinite(area),
                Status = "Completed",
                FunctionEvaluations = steps + 1
            };
        }

        [AdvancedAnalyticsTool("IntegrateRomberg")]
        [Description(
            "Approximates the definite integral of a built-in function by Romberg's method, which repeatedly halves the step and applies Richardson extrapolation, giving very high accuracy for smooth functions with few evaluations. " +
            "It is not suited to functions with kinks or singularities inside the interval. Steps (levels of refinement) are limited to 30; the limits must be finite with upper greater than lower.")]
        public NumericsIntegrationResult IntegrateRomberg(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Lower limit of integration.")]
            double lower,
            [Description("Upper limit of integration, greater than lower.")]
            double upper,
            [Description("Number of Romberg refinement levels, from 2 to 30.")]
            int steps = 10)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            NumericsGuard.RequireInRange(steps, nameof(steps), 2, MaxRombergSteps);
            var area = RombergMethod.Integrate(f, lower, upper, steps);

            return new NumericsIntegrationResult
            {
                Area = area,
                ErrorEstimate = double.NaN,
                Converged = double.IsFinite(area),
                Status = "Completed",
                FunctionEvaluations = (1 << (steps - 1)) + 1
            };
        }

        [AdvancedAnalyticsTool("IntegrateGaussKronrod")]
        [Description(
            "Approximates the definite integral of a built-in function over a finite interval with a Gauss-Kronrod rule of increasing order until the requested relative tolerance is met, and reports an error estimate. " +
            "Area is the integral; ErrorEstimate is the estimated absolute error; Status Success means the tolerance was reached and MaximumSteps means the best estimate was returned without reaching it. " +
            "Use this as the default accurate integrator for smooth functions. Limits must be finite with upper greater than lower.")]
        public NumericsIntegrationResult IntegrateGaussKronrod(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Lower limit of integration.")]
            double lower,
            [Description("Upper limit of integration, greater than lower.")]
            double upper,
            [Description("Target relative tolerance, between 1e-14 and 0.1. Default 1e-8.")]
            double relativeTolerance = 1e-8)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);

            if (!(relativeTolerance >= 1e-14 && relativeTolerance <= 0.1))
            {
                throw new ArgumentException("'relativeTolerance' must be between 1e-14 and 0.1.",
                    nameof(relativeTolerance));
            }

            var integrator = new NonAdaptiveGaussKronrod(f, lower, upper)
                { ToleranceAbsolute = 0, ToleranceRelative = relativeTolerance };

            var success = integrator.Compute();

            return new NumericsIntegrationResult
            {
                Area = integrator.Area,
                ErrorEstimate = integrator.Error,
                Converged = success,
                Status = integrator.Status.ToString(),
                FunctionEvaluations = integrator.FunctionEvaluations
            };
        }

        [AdvancedAnalyticsTool("IntegrateAdaptiveGaussKronrodInfinite")]
        [Description(
            "Approximates the integral of a built-in function over a range where either or both ends may be infinite, using adaptive Gauss-Kronrod subdivision. Use it for tail areas and expectations such as the integral of a Gaussian over the whole real line (which is a times c times sqrt(2 pi)). " +
            "Set lowerIsInfinite for minus infinity and upperIsInfinite for plus infinity; the matching numeric limit is then ignored. Area is the integral and ErrorEstimate the estimated error; Converged false means the subinterval budget ran out or the integral diverges. Subintervals are limited to 2000.")]
        public NumericsIntegrationResult IntegrateAdaptiveGaussKronrodInfinite(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Lower limit; ignored when lowerIsInfinite is true.")]
            double lower,
            [Description("Upper limit; ignored when upperIsInfinite is true.")]
            double upper,
            [Description("True integrates from minus infinity.")]
            bool lowerIsInfinite = false,
            [Description("True integrates to plus infinity.")]
            bool upperIsInfinite = false,
            [Description("Maximum number of subintervals, from 1 to 2000.")]
            int subintervals = 200,
            [Description("Target relative tolerance, between 1e-13 and 0.1. Default 1e-8.")]
            double relativeTolerance = 1e-8)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            NumericsGuard.RequireInRange(subintervals, nameof(subintervals), 1, 2000);

            if (!(relativeTolerance >= 1e-13 && relativeTolerance <= 0.1))
            {
                throw new ArgumentException("'relativeTolerance' must be between 1e-13 and 0.1.",
                    nameof(relativeTolerance));
            }

            var a = lowerIsInfinite ? double.NegativeInfinity : lower;
            var b = upperIsInfinite ? double.PositiveInfinity : upper;

            if (!lowerIsInfinite)
            {
                NumericsGuard.RequireFinite(lower, nameof(lower));
            }

            if (!upperIsInfinite)
            {
                NumericsGuard.RequireFinite(upper, nameof(upper));
            }

            if (!(b > a))
            {
                throw new ArgumentException("The upper limit must be greater than the lower limit.", nameof(upper));
            }

            var integrator = new InfiniteAdaptiveGaussKronrod(subintervals, f, a, b)
                { ToleranceAbsolute = 0, ToleranceRelative = relativeTolerance };

            var success = integrator.Compute();

            return new NumericsIntegrationResult
            {
                Area = integrator.Area,
                ErrorEstimate = integrator.Error,
                Converged = success && double.IsFinite(integrator.Area),
                Status = integrator.Status.ToString(),
                FunctionEvaluations = integrator.FunctionEvaluations
            };
        }

        [AdvancedAnalyticsTool("IntegrateMonteCarloOnInterval")]
        [Description(
            "Estimates the integral of a built-in function over a finite interval by averaging the function at uniformly random points, and reports the Monte Carlo standard error. It is unbiased but converges slowly (error shrinks with the square root of the sample count), so prefer IntegrateGaussKronrod in one dimension. " +
            "The seed makes the answer reproducible. Area is the estimate, ErrorEstimate its one-standard-error uncertainty. Samples are limited to 5000000.")]
        public NumericsIntegrationResult IntegrateMonteCarloOnInterval(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Lower limit of integration.")]
            double lower,
            [Description("Upper limit of integration, greater than lower.")]
            double upper,
            [Description("Number of random samples, from 10 to 5000000.")]
            int samples = 100000,
            [Description("Seed for the random number generator so the estimate is reproducible.")]
            int seed = 1)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            NumericsGuard.RequireInRange(samples, nameof(samples), 10, NumericsGuard.MaxSamples);

            var integrator = new MonteCarloIntegration(1, x => f(x[0]))
            {
                Random = new Random(seed),
                Iterations = samples
            };

            integrator.Range[0] = new DoubleRange(lower, upper);
            integrator.Compute();

            return new NumericsIntegrationResult
            {
                Area = integrator.Area,
                ErrorEstimate = integrator.Error,
                Converged = double.IsFinite(integrator.Area),
                Status = "Completed",
                FunctionEvaluations = samples
            };
        }

        [AdvancedAnalyticsTool("IntegrateMonteCarloObjectiveOverBox")]
        [Description(
            "Estimates the integral of a built-in multi-variable objective over a rectangular box by uniform random sampling, returning the estimate and its standard error. Monte Carlo is the practical route in many dimensions where grid methods explode. " +
            "The box is given as one lower and upper bound per variable; the number of variables is the length of the bounds (1 to 50). Samples are limited to 5000000 and the seed makes the result reproducible.")]
        public NumericsIntegrationResult IntegrateMonteCarloObjectiveOverBox(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Lower bound of the box for each variable.")]
            double[] lowerBounds,
            [Description("Upper bound of the box for each variable, each greater than the matching lower bound.")]
            double[] upperBounds,
            [Description("Number of random samples, from 10 to 5000000.")]
            int samples = 100000,
            [Description("Seed for the random number generator so the estimate is reproducible.")]
            int seed = 1,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            NumericsGuard.RequireVector(lowerBounds, nameof(lowerBounds), 1, NumericsGuard.MaxVariables);
            NumericsGuard.RequireVector(upperBounds, nameof(upperBounds), lowerBounds.Length, lowerBounds.Length);
            NumericsGuard.RequireInRange(samples, nameof(samples), 10, NumericsGuard.MaxSamples);

            for (var i = 0; i < lowerBounds.Length; i++)
            {
                if (!(upperBounds[i] > lowerBounds[i]))
                {
                    throw new ArgumentException($"Upper bound {i} must be greater than lower bound {i}.",
                        nameof(upperBounds));
                }
            }

            var f = NumericsFunctions.Objective(objective, lowerBounds.Length, quadraticMatrix, linearTerms,
                constantTerm,
                nameof(lowerBounds));

            var integrator = new MonteCarloIntegration(lowerBounds.Length, f)
            {
                Random = new Random(seed),
                Iterations = samples
            };

            for (var i = 0; i < lowerBounds.Length; i++)
            {
                integrator.Range[i] = new DoubleRange(lowerBounds[i], upperBounds[i]);
            }

            integrator.Compute();

            return new NumericsIntegrationResult
            {
                Area = integrator.Area,
                ErrorEstimate = integrator.Error,
                Converged = double.IsFinite(integrator.Area),
                Status = "Completed",
                FunctionEvaluations = samples
            };
        }

        [AdvancedAnalyticsTool("NumericalDerivativeOfNamedFunction")]
        [Description(
            "Estimates the first or a higher derivative of a built-in function at a point by central finite differences. The result is a numerical approximation: accuracy improves with more stencil points, and too small a step amplifies rounding error while too large a step adds truncation error. " +
            "Order 1 is the slope, order 2 the curvature. Stencil points must be odd, from 3 to 9, and greater than the order; the step is relative to the size of x (absolute near zero).")]
        public double NumericalDerivativeOfNamedFunction(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Point at which to differentiate.")]
            double x,
            [Description("Derivative order, from 1 to 4.")]
            int order = 1,
            [Description("Step size h, between 1e-8 and 1. Default 0.01.")]
            double stepSize = 0.01,
            [Description(
                "Odd number of stencil points, from 3 to 9, greater than the order. More points are more accurate.")]
            int points = 5)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            NumericsGuard.RequireFinite(x, nameof(x));
            NumericsGuard.RequireInRange(order, nameof(order), 1, 4);
            RequireStep(stepSize);
            RequirePoints(points, order);
            var differences = new FiniteDifferences(1, v => f(v[0]), order, stepSize) { NumberOfPoints = points };

            return differences.Gradient(new[] { x })[0];
        }

        [AdvancedAnalyticsTool("NumericalGradientOfObjective")]
        [Description(
            "Estimates the gradient vector (partial derivatives) of a built-in multi-variable objective at a point by finite differences. The gradient points uphill and is zero at a minimum, so it verifies whether a candidate solution is stationary. " +
            "Up to 50 variables. Stencil points must be odd, from 3 to 9. The result is numerical, so expect small rounding errors near 1e-6 relative.")]
        public NumericsVectorResult NumericalGradientOfObjective(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Point at which to evaluate the gradient, one value per variable.")]
            double[] point,
            [Description("Step size h, between 1e-8 and 1. Default 0.001.")]
            double stepSize = 0.001,
            [Description("Odd number of stencil points, from 3 to 9. More points are more accurate.")]
            int points = 5,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            NumericsGuard.RequireVector(point, nameof(point), 1, NumericsGuard.MaxVariables);
            RequireStep(stepSize);
            RequirePoints(points, 1);

            var f = NumericsFunctions.Objective(objective, point.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(point));

            var differences = new FiniteDifferences(point.Length, f, 1, stepSize) { NumberOfPoints = points };

            return NumericsGuard.ToVectorResult(differences.Gradient(point));
        }

        [AdvancedAnalyticsTool("NumericalHessianOfObjective")]
        [Description(
            "Estimates the Hessian matrix of second partial derivatives of a built-in multi-variable objective at a point by nested finite differences. The Hessian describes curvature: positive definite at a minimum, negative definite at a maximum, indefinite at a saddle point. " +
            "Pair it with SymmetricEigenDecomposition to inspect the curvature directions. Up to 20 variables; the result is symmetric and numerical, with error growing for very small or very large steps.")]
        public NumericsMatrixResult NumericalHessianOfObjective(
            [Description(NumericsText.Objective)] NumericsObjectiveFunction objective,
            [Description("Point at which to evaluate the Hessian, one value per variable (at most 20).")]
            double[] point,
            [Description("Step size h, between 1e-6 and 1. Default 0.01.")]
            double stepSize = 0.01,
            [Description(NumericsText.QuadraticMatrix)]
            double[][]? quadraticMatrix = null,
            [Description(NumericsText.LinearTerms)]
            double[]? linearTerms = null,
            [Description(NumericsText.ConstantTerm)]
            double constantTerm = 0)
        {
            NumericsGuard.RequireVector(point, nameof(point), 1, 20);

            if (!(stepSize >= 1e-6 && stepSize <= 1))
            {
                throw new ArgumentException("'stepSize' for the Hessian must be between 1e-6 and 1.", nameof(stepSize));
            }

            var f = NumericsFunctions.Objective(objective, point.Length, quadraticMatrix, linearTerms, constantTerm,
                nameof(point));

            var differences = new FiniteDifferences(point.Length, f, 1, stepSize);

            return NumericsGuard.ToMatrixResult(differences.Hessian(point));
        }

        [AdvancedAnalyticsTool("NumericalJacobianOfVectorFunction")]
        [Description(
            "Estimates the Jacobian matrix (rows are outputs, columns are inputs, entry i,j is d output_i / d input_j) of a built-in vector-valued function at a point by finite differences. " +
            "Functions: PolarToCartesian (r, theta gives x, y), SphericalToCartesian (r, theta, phi gives x, y, z), Softmax (any length), RosenbrockResiduals (x, y gives 10(y - x^2) and 1 - x) and HimmelblauResiduals (x, y gives x^2 + y - 11 and x + y^2 - 7). " +
            "Use it to linearise a transformation or to check Gauss-Newton style fits. The point length must suit the function.")]
        public NumericsMatrixResult NumericalJacobianOfVectorFunction(
            [Description("Which vector-valued function to differentiate.")]
            NumericsVectorFunction function,
            [Description("Point at which to evaluate the Jacobian.")]
            double[] point,
            [Description("Step size h, between 1e-6 and 1. Default 0.001.")]
            double stepSize = 0.001)
        {
            NumericsGuard.RequireVector(point, nameof(point), 1, NumericsGuard.MaxVariables);

            if (!(stepSize >= 1e-6 && stepSize <= 1))
            {
                throw new ArgumentException("'stepSize' must be between 1e-6 and 1.", nameof(stepSize));
            }

            Func<double[], double[]> vector;

            switch (function)
            {
                case NumericsVectorFunction.PolarToCartesian:
                    RequireLength(point, 2, function);
                    vector = p => new[] { p[0] * Math.Cos(p[1]), p[0] * Math.Sin(p[1]) };
                    break;
                case NumericsVectorFunction.SphericalToCartesian:
                    RequireLength(point, 3, function);

                    vector = p => new[]
                    {
                        p[0] * Math.Sin(p[1]) * Math.Cos(p[2]),
                        p[0] * Math.Sin(p[1]) * Math.Sin(p[2]),
                        p[0] * Math.Cos(p[1])
                    };

                    break;
                case NumericsVectorFunction.Softmax:
                    vector = p => Special.Softmax(p);
                    break;
                case NumericsVectorFunction.RosenbrockResiduals:
                    RequireLength(point, 2, function);
                    vector = p => new[] { 10 * (p[1] - p[0] * p[0]), 1 - p[0] };
                    break;
                default:
                    RequireLength(point, 2, function);
                    vector = p => new[] { p[0] * p[0] + p[1] - 11, p[0] + p[1] * p[1] - 7 };
                    break;
            }

            var outputs = vector(point).Length;
            var jacobian = new double[outputs][];

            for (var i = 0; i < outputs; i++)
            {
                var row = i;

                var differences = new FiniteDifferences(point.Length, p => vector(p)[row], 1, stepSize)
                    { NumberOfPoints = 5 };

                jacobian[i] = differences.Gradient(point);
            }

            return NumericsGuard.ToMatrixResult(jacobian);
        }

        [AdvancedAnalyticsTool("FindRootBrent")]
        [Description(
            "Finds a root of a built-in function (a value x where f(x) = target) inside an interval using Brent's method, which combines bisection, secant and inverse quadratic steps and is guaranteed to converge when the interval brackets a sign change. " +
            "The function value minus the target must have opposite signs at the two ends, otherwise Status is RootNotBracketed; use FindRootBracketsByScanning to locate suitable intervals. " +
            "Solution is the root, FunctionValue the function at the root, Converged true when found. Iterations are limited to 500 and tolerance to 1e-14 or more.")]
        public NumericsRootResult FindRootBrent(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Left end of the interval that brackets the root.")]
            double lower,
            [Description("Right end of the interval, greater than lower.")]
            double upper,
            [Description("The value the function should equal at the root. Default 0.")]
            double target = 0,
            [Description("Convergence tolerance on x, between 1e-14 and 1. Default 1e-10.")]
            double tolerance = 1e-10,
            [Description("Maximum iterations, from 1 to 500.")]
            int maximumIterations = 200)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            NumericsGuard.RequireFinite(target, nameof(target));
            RequireTolerance(tolerance);
            NumericsGuard.RequireInRange(maximumIterations, nameof(maximumIterations), 1, 500);
            var search = new BrentSearch(f, lower, upper, tolerance, maximumIterations);
            var converged = search.Find(target);

            return new NumericsRootResult
            {
                Solution = search.Solution,
                FunctionValue = search.Value,
                Converged = converged,
                Status = search.Status.ToString()
            };
        }

        [AdvancedAnalyticsTool("FindRootBracketsByScanning")]
        [Description(
            "Scans an interval on an even grid and reports every sub-interval where the function minus the target changes sign, each of which brackets at least one root and can be passed to FindRootBrent. " +
            "It can miss a pair of roots closer together than the grid spacing (two sign changes inside one cell cancel), so use a fine grid for oscillating functions. Points that are exactly zero are listed in ExactZeros. Grid points are limited to 200000.")]
        public NumericsBracketResult FindRootBracketsByScanning(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Left end of the interval to scan.")]
            double lower,
            [Description("Right end of the interval, greater than lower.")]
            double upper,
            [Description("Number of grid points, from 2 to 200000.")]
            int gridPoints = 1000,
            [Description("The value the function should equal at a root. Default 0.")]
            double target = 0)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            NumericsGuard.RequireInRange(gridPoints, nameof(gridPoints), 2, NumericsGuard.MaxVectorLength);
            NumericsGuard.RequireFinite(target, nameof(target));
            var step = (upper - lower) / (gridPoints - 1);
            var lowers = new List<double>();
            var uppers = new List<double>();
            var zeros = new List<double>();
            var previousX = lower;
            var previous = f(lower) - target;

            if (previous == 0)
            {
                zeros.Add(lower);
            }

            for (var i = 1; i < gridPoints; i++)
            {
                var x = i == gridPoints - 1 ? upper : lower + i * step;
                var current = f(x) - target;

                if (current == 0)
                {
                    zeros.Add(x);
                }
                else if (double.IsFinite(previous) && double.IsFinite(current) && previous != 0 &&
                         Math.Sign(previous) != Math.Sign(current))
                {
                    lowers.Add(previousX);
                    uppers.Add(x);
                }

                previousX = x;
                previous = current;
            }

            return new NumericsBracketResult
            {
                Lower = lowers.ToArray(),
                Upper = uppers.ToArray(),
                Count = lowers.Count,
                ExactZeros = zeros.ToArray()
            };
        }

        [AdvancedAnalyticsTool("FindExtremumBrent")]
        [Description(
            "Finds a local minimum or maximum of a built-in function inside an interval using Brent's derivative-free method (golden section with parabolic steps). " +
            "It finds one local extremum in the interval, not necessarily the global one; the function should be unimodal there. Solution is the location, FunctionValue the function value at it. Converged false means the iteration limit was hit. Iterations are limited to 500.")]
        public NumericsRootResult FindExtremumBrent(
            [Description(NumericsText.Function)] NumericsUnivariateFunction function,
            [Description(NumericsText.FunctionParameters)]
            double[]? parameters,
            [Description("Left end of the interval to search.")]
            double lower,
            [Description("Right end of the interval, greater than lower.")]
            double upper,
            [Description("True searches for a maximum; false for a minimum.")]
            bool maximise = false,
            [Description("Convergence tolerance on x, between 1e-14 and 1. Default 1e-8.")]
            double tolerance = 1e-8,
            [Description("Maximum iterations, from 1 to 500.")]
            int maximumIterations = 200)
        {
            var f = NumericsFunctions.Univariate(function, parameters, nameof(parameters));
            RequireInterval(lower, upper);
            RequireTolerance(tolerance);
            NumericsGuard.RequireInRange(maximumIterations, nameof(maximumIterations), 1, 500);
            var search = new BrentSearch(f, lower, upper, tolerance, maximumIterations);
            var converged = maximise ? search.Maximize() : search.Minimize();

            return new NumericsRootResult
            {
                Solution = search.Solution,
                FunctionValue = search.Value,
                Converged = converged,
                Status = search.Status.ToString()
            };
        }

        [AdvancedAnalyticsTool("EvaluatePolynomial")]
        [Description(
            "Evaluates a polynomial c0 + c1 x + c2 x^2 + ... at each supplied x using Horner's scheme, which is numerically stable. Coefficients run from the constant term upwards. " +
            "Up to 40 coefficients and 200000 points.")]
        public NumericsVectorResult EvaluatePolynomial(
            [Description("Polynomial coefficients from the constant term upwards, at least one and at most 40.")]
            double[] coefficients,
            [Description("Values of x at which to evaluate.")]
            double[] x)
        {
            var f = NumericsFunctions.Univariate(NumericsUnivariateFunction.Polynomial, coefficients,
                nameof(coefficients));

            NumericsGuard.RequireVector(x, nameof(x));

            return NumericsGuard.ToVectorResult(x.Select(v => f(v)).ToArray());
        }

        [AdvancedAnalyticsTool("PolynomialDerivativeOrAntiderivative")]
        [Description(
            "Returns the coefficients of the derivative of a polynomial, or of its antiderivative with a chosen constant of integration. Coefficients run from the constant term upwards. " +
            "The derivative of a constant is the single coefficient 0. Up to 39 coefficients for the antiderivative.")]
        public NumericsVectorResult PolynomialDerivativeOrAntiderivative(
            [Description("Polynomial coefficients from the constant term upwards.")]
            double[] coefficients,
            [Description("Derivative differentiates; Antiderivative integrates.")]
            NumericsPolynomialCalculus operation = NumericsPolynomialCalculus.Derivative,
            [Description("Constant of integration for the antiderivative; ignored for the derivative.")]
            double constant = 0)
        {
            NumericsGuard.RequireVector(coefficients, nameof(coefficients), 1,
                operation == NumericsPolynomialCalculus.Derivative
                    ? NumericsGuard.MaxPolynomialCoefficients
                    : NumericsGuard.MaxPolynomialCoefficients - 1);

            NumericsGuard.RequireFinite(constant, nameof(constant));

            if (operation == NumericsPolynomialCalculus.Derivative)
            {
                if (coefficients.Length == 1)
                {
                    return NumericsGuard.ToVectorResult(new[] { 0.0 });
                }

                return NumericsGuard.ToVectorResult(Enumerable.Range(1, coefficients.Length - 1)
                    .Select(i => i * coefficients[i]).ToArray());
            }

            return NumericsGuard.ToVectorResult(new[] { constant }
                .Concat(Enumerable.Range(0, coefficients.Length).Select(i => coefficients[i] / (i + 1))).ToArray());
        }

        [AdvancedAnalyticsTool("PolynomialMultiply")]
        [Description(
            "Multiplies two polynomials given by coefficients from the constant term upwards, returning the product's coefficients; the degree of the result is the sum of the degrees. " +
            "Each input is limited to 20 coefficients.")]
        public NumericsVectorResult PolynomialMultiply(
            [Description("First polynomial coefficients from the constant term upwards.")]
            double[] a,
            [Description("Second polynomial coefficients from the constant term upwards.")]
            double[] b)
        {
            NumericsGuard.RequireVector(a, nameof(a), 1, 20);
            NumericsGuard.RequireVector(b, nameof(b), 1, 20);
            var result = new double[a.Length + b.Length - 1];

            for (var i = 0; i < a.Length; i++)
            {
                for (var j = 0; j < b.Length; j++)
                {
                    result[i + j] += a[i] * b[j];
                }
            }

            return NumericsGuard.ToVectorResult(result);
        }

        [AdvancedAnalyticsTool("PolynomialRoots")]
        [Description(
            "Finds all real and complex roots of a polynomial as the eigenvalues of its companion matrix. Coefficients run from the constant term upwards and the leading coefficient must not be zero. " +
            "RealParts and ImaginaryParts are parallel arrays; a non-zero imaginary part means a complex conjugate pair. Accuracy degrades for high degrees or repeated roots. Degree is limited to 30.")]
        public NumericsPolynomialRootsResult PolynomialRoots(
            [Description(
                "Polynomial coefficients from the constant term upwards, with a non-zero last (leading) coefficient; degree 1 to 30.")]
            double[] coefficients)
        {
            NumericsGuard.RequireVector(coefficients, nameof(coefficients), 2, 31);
            var degree = coefficients.Length - 1;
            var leading = coefficients[degree];

            if (leading == 0)
            {
                throw new ArgumentException(
                    "The last (leading) coefficient must not be zero; drop trailing zeros to lower the degree.",
                    nameof(coefficients));
            }

            var companion = new double[degree][];

            for (var i = 0; i < degree; i++)
            {
                companion[i] = new double[degree];
            }

            for (var i = 1; i < degree; i++)
            {
                companion[i][i - 1] = 1;
            }

            for (var i = 0; i < degree; i++)
            {
                companion[i][degree - 1] = -coefficients[i] / leading;
            }

            var eigen = new JaggedEigenvalueDecomposition(companion, false, false, false);

            var order = Enumerable.Range(0, degree)
                .OrderBy(i => eigen.RealEigenvalues[i])
                .ThenBy(i => eigen.ImaginaryEigenvalues[i])
                .ToArray();

            return new NumericsPolynomialRootsResult
            {
                RealParts = order.Select(i => eigen.RealEigenvalues[i]).ToArray(),
                ImaginaryParts = order.Select(i => eigen.ImaginaryEigenvalues[i]).ToArray(),
                Degree = degree
            };
        }

        [AdvancedAnalyticsTool("FitPolynomialByLeastSquares")]
        [Description(
            "Fits a polynomial of the chosen degree to (x, y) points by ordinary least squares and returns its coefficients from the constant term upwards together with the residual sum of squares. " +
            "There must be more points than the degree. High degrees on wide x ranges are ill-conditioned, so centre and scale x first; degree is limited to 15 and points to 200000. Use EvaluatePolynomial to predict.")]
        public NumericsPolynomialFitResult FitPolynomialByLeastSquares(
            [Description("The x values of the data points.")]
            double[] x,
            [Description("The y values of the data points, same length as x.")]
            double[] y,
            [Description("Polynomial degree, from 0 to 15, smaller than the number of points.")]
            int degree)
        {
            NumericsGuard.RequireVector(x, nameof(x), 2);
            NumericsGuard.RequireVector(y, nameof(y), x.Length, x.Length);
            NumericsGuard.RequireInRange(degree, nameof(degree), 0, 15);

            if (x.Length <= degree)
            {
                throw new ArgumentException(
                    $"Fitting degree {degree} needs at least {degree + 1} points but only {x.Length} were given; lower the degree.",
                    nameof(degree));
            }

            var design = new double[x.Length][];

            for (var i = 0; i < x.Length; i++)
            {
                design[i] = new double[degree + 1];
                var power = 1.0;

                for (var j = 0; j <= degree; j++)
                {
                    design[i][j] = power;
                    power *= x[i];
                }
            }

            var coefficients = design.Solve(y, true);
            var f = NumericsFunctions.Univariate(NumericsUnivariateFunction.Polynomial, coefficients, nameof(x));
            var residual = 0.0;

            for (var i = 0; i < x.Length; i++)
            {
                var difference = y[i] - f(x[i]);
                residual += difference * difference;
            }

            return new NumericsPolynomialFitResult
                { Coefficients = coefficients, ResidualSumOfSquares = residual, Degree = degree };
        }

        [AdvancedAnalyticsTool("InterpolatePolynomialThroughPoints")]
        [Description(
            "Builds the unique polynomial of degree n-1 that passes exactly through n points with distinct x values and returns its coefficients from the constant term upwards. " +
            "Use it for small tables; with many points high-degree interpolation oscillates wildly (Runge's phenomenon) and is ill-conditioned, so at most 20 points are accepted. Duplicate x values are rejected.")]
        public NumericsVectorResult InterpolatePolynomialThroughPoints(
            [Description("Distinct x values of the points, 2 to 20.")]
            double[] x,
            [Description("The y values, same length as x.")]
            double[] y)
        {
            NumericsGuard.RequireVector(x, nameof(x), 2, 20);
            NumericsGuard.RequireVector(y, nameof(y), x.Length, x.Length);

            if (x.Distinct().Count() != x.Length)
            {
                throw new ArgumentException(
                    "The x values must all be different; a polynomial cannot pass through two points with the same x.",
                    nameof(x));
            }

            var design = new double[x.Length][];

            for (var i = 0; i < x.Length; i++)
            {
                design[i] = new double[x.Length];
                var power = 1.0;

                for (var j = 0; j < x.Length; j++)
                {
                    design[i][j] = power;
                    power *= x[i];
                }
            }

            return NumericsGuard.ToVectorResult(design.Solve(y));
        }

        [AdvancedAnalyticsTool("InterpolateLinearly")]
        [Description(
            "Estimates y at each query x by piecewise linear interpolation between the supplied (x, y) points, which must be sorted by increasing x. " +
            "Queries before the first x return 'belowValue' and after the last x return 'aboveValue' (pass the end y values to hold the edge flat), so it never extrapolates by itself. Up to 200000 points and queries.")]
        public NumericsVectorResult InterpolateLinearly(
            [Description("Known x values sorted in strictly increasing order, at least 2.")]
            double[] x,
            [Description("Known y values, same length as x.")]
            double[] y,
            [Description("Query positions at which to estimate y.")]
            double[] queries,
            [Description("Value returned for queries below the first x.")]
            double belowValue = double.NaN,
            [Description("Value returned for queries above the last x.")]
            double aboveValue = double.NaN)
        {
            NumericsGuard.RequireVector(x, nameof(x), 2);
            NumericsGuard.RequireVector(y, nameof(y), x.Length, x.Length);
            NumericsGuard.RequireVector(queries, nameof(queries));

            for (var i = 1; i < x.Length; i++)
            {
                if (!(x[i] > x[i - 1]))
                {
                    throw new ArgumentException(
                        $"The x values must be strictly increasing but x[{i}] = {x[i]} does not exceed x[{i - 1}] = {x[i - 1]}.",
                        nameof(x));
                }
            }

            return NumericsGuard.ToVectorResult(queries.Select(q =>
                q == x[^1] ? y[^1] : Tools.Interpolate1D(q, x, y, belowValue, aboveValue)).ToArray());
        }

        [AdvancedAnalyticsTool("ExtrapolateLinearly")]
        [Description(
            "Extends a straight line through the first two or last two of the supplied sorted points to estimate y at queries outside the data range, and interpolates inside it. Use it only for short distances beyond the data, because real relationships rarely stay linear. " +
            "The result at each query lies on the segment nearest to it. Up to 200000 points and queries; x must be strictly increasing.")]
        public NumericsVectorResult ExtrapolateLinearly(
            [Description("Known x values sorted in strictly increasing order, at least 2.")]
            double[] x,
            [Description("Known y values, same length as x.")]
            double[] y,
            [Description("Query positions, inside or outside the data range.")]
            double[] queries)
        {
            NumericsGuard.RequireVector(x, nameof(x), 2);
            NumericsGuard.RequireVector(y, nameof(y), x.Length, x.Length);
            NumericsGuard.RequireVector(queries, nameof(queries));

            for (var i = 1; i < x.Length; i++)
            {
                if (!(x[i] > x[i - 1]))
                {
                    throw new ArgumentException(
                        $"The x values must be strictly increasing but x[{i}] = {x[i]} does not exceed x[{i - 1}] = {x[i - 1]}.",
                        nameof(x));
                }
            }

            var n = x.Length;

            return NumericsGuard.ToVectorResult(queries.Select(q =>
            {
                int segment;

                if (q <= x[0])
                {
                    segment = 0;
                }
                else if (q >= x[n - 1])
                {
                    segment = n - 2;
                }
                else
                {
                    segment = Array.BinarySearch(x, q);
                    segment = segment >= 0 ? Math.Min(segment, n - 2) : ~segment - 1;
                }

                return y[segment] + (y[segment + 1] - y[segment]) * (q - x[segment]) / (x[segment + 1] - x[segment]);
            }).ToArray());
        }

        private static void RequireStep(double stepSize)
        {
            if (!(stepSize >= 1e-8 && stepSize <= 1))
            {
                throw new ArgumentException("'stepSize' must be between 1e-8 and 1.", nameof(stepSize));
            }
        }

        private static void RequirePoints(int points, int order)
        {
            if (points < 3 || points > 9 || points % 2 == 0)
            {
                throw new ArgumentException("'points' must be an odd number from 3 to 9.", nameof(points));
            }

            if (points <= order)
            {
                throw new ArgumentException(
                    $"'points' ({points}) must exceed the derivative order ({order}); use more stencil points.",
                    nameof(points));
            }
        }

        private static void RequireTolerance(double tolerance)
        {
            if (!(tolerance >= 1e-14 && tolerance <= 1))
            {
                throw new ArgumentException("'tolerance' must be between 1e-14 and 1.", nameof(tolerance));
            }
        }

        private static void RequireLength(double[] point, int length, NumericsVectorFunction function)
        {
            if (point.Length != length)
            {
                throw new ArgumentException(
                    $"The {function} function needs a point with exactly {length} values but {point.Length} were given.",
                    nameof(point));
            }
        }
    }
}
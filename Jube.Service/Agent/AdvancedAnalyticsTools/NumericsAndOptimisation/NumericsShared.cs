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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public enum NumericsUnivariateFunction
    {
        Polynomial,
        Sine,
        Cosine,
        Exponential,
        NaturalLog,
        SquareRoot,
        Power,
        Reciprocal,
        Gaussian,
        Logistic,
        HyperbolicTangent,
        AbsoluteValue
    }

    public enum NumericsObjectiveFunction
    {
        Sphere,
        Rosenbrock,
        Rastrigin,
        Ackley,
        Himmelblau,
        Booth,
        Beale,
        Matyas,
        StyblinskiTang,
        Griewank,
        Quadratic
    }

    public sealed record NumericsVectorResult
    {
        public double[] Values { get; init; } = Array.Empty<double>();
        public int Count { get; init; }
        public int NonFiniteCount { get; init; }
    }

    public sealed record NumericsMatrixResult
    {
        public double[][] Matrix { get; init; } = Array.Empty<double[]>();
        public int Rows { get; init; }
        public int Columns { get; init; }
    }

    public static class NumericsFunctions
    {
        private static double Parameter(double[] parameters, int index, double fallback)
        {
            return index < parameters.Length ? parameters[index] : fallback;
        }

        public static Func<double, double> Univariate(NumericsUnivariateFunction function, double[]? parameters,
            string parameterName)
        {
            var p = parameters ?? Array.Empty<double>();

            if (function == NumericsUnivariateFunction.Polynomial)
            {
                NumericsGuard.RequireVector(p, parameterName, 1, NumericsGuard.MaxPolynomialCoefficients);
                var coefficients = (double[])p.Clone();

                return x =>
                {
                    var result = 0.0;

                    for (var i = coefficients.Length - 1; i >= 0; i--)
                    {
                        result = result * x + coefficients[i];
                    }

                    return result;
                };
            }

            NumericsGuard.RequireVector(p, parameterName, 0, 4);

            var a = Parameter(p, 0, 1);
            var b = Parameter(p, 1, 1);
            var c = Parameter(p, 2, 0);
            var d = Parameter(p, 3, 0);

            switch (function)
            {
                case NumericsUnivariateFunction.Sine:
                    return x => a * Math.Sin(b * x + c) + d;
                case NumericsUnivariateFunction.Cosine:
                    return x => a * Math.Cos(b * x + c) + d;
                case NumericsUnivariateFunction.Exponential:
                    return x => a * Math.Exp(b * x) + c;
                case NumericsUnivariateFunction.NaturalLog:
                    b = Parameter(p, 1, 0);

                    return x => a * Math.Log(x) + b;
                case NumericsUnivariateFunction.SquareRoot:
                    b = Parameter(p, 1, 0);

                    return x => a * Math.Sqrt(x) + b;
                case NumericsUnivariateFunction.Power:
                    b = Parameter(p, 1, 2);

                    return x => a * Math.Pow(x, b) + c;
                case NumericsUnivariateFunction.Reciprocal:
                    b = Parameter(p, 1, 0);

                    return x => a / (x - b) + c;
                case NumericsUnivariateFunction.Gaussian:
                    b = Parameter(p, 1, 0);
                    c = Parameter(p, 2, 1);

                    if (c == 0)
                    {
                        throw new ArgumentException(
                            $"'{parameterName}' third value (the width) must not be 0 for the Gaussian family.",
                            parameterName);
                    }

                    return x => a * Math.Exp(-((x - b) * (x - b)) / (2 * c * c));
                case NumericsUnivariateFunction.Logistic:
                    return x => a / (1 + Math.Exp(-b * (x - c))) + d;
                case NumericsUnivariateFunction.HyperbolicTangent:
                    return x => a * Math.Tanh(b * x + c) + d;
                case NumericsUnivariateFunction.AbsoluteValue:
                    b = Parameter(p, 1, 0);

                    return x => a * Math.Abs(x - b) + c;
                default:
                    throw new ArgumentException($"Unknown function family {function}.", nameof(function));
            }
        }

        public static Func<double[], double> Objective(NumericsObjectiveFunction family, int dimension,
            double[][]? quadraticMatrix, double[]? linearTerms, double constantTerm, string parameterName)
        {
            RequireDimension(family, dimension, parameterName);

            switch (family)
            {
                case NumericsObjectiveFunction.Sphere:
                    return x => x.Sum(v => v * v);
                case NumericsObjectiveFunction.Rosenbrock:
                    return x =>
                    {
                        var s = 0.0;

                        for (var i = 0; i < x.Length - 1; i++)
                        {
                            s += 100 * Math.Pow(x[i + 1] - x[i] * x[i], 2) + Math.Pow(1 - x[i], 2);
                        }

                        return s;
                    };
                case NumericsObjectiveFunction.Rastrigin:
                    return x => 10 * x.Length + x.Sum(v => v * v - 10 * Math.Cos(2 * Math.PI * v));
                case NumericsObjectiveFunction.Ackley:
                    return x =>
                    {
                        var n = x.Length;
                        var s1 = x.Sum(v => v * v) / n;
                        var s2 = x.Sum(v => Math.Cos(2 * Math.PI * v)) / n;

                        return -20 * Math.Exp(-0.2 * Math.Sqrt(s1)) - Math.Exp(s2) + 20 + Math.E;
                    };
                case NumericsObjectiveFunction.Himmelblau:
                    return x => Math.Pow(x[0] * x[0] + x[1] - 11, 2) + Math.Pow(x[0] + x[1] * x[1] - 7, 2);
                case NumericsObjectiveFunction.Booth:
                    return x => Math.Pow(x[0] + 2 * x[1] - 7, 2) + Math.Pow(2 * x[0] + x[1] - 5, 2);
                case NumericsObjectiveFunction.Beale:
                    return x =>
                        Math.Pow(1.5 - x[0] + x[0] * x[1], 2) +
                        Math.Pow(2.25 - x[0] + x[0] * x[1] * x[1], 2) +
                        Math.Pow(2.625 - x[0] + x[0] * x[1] * x[1] * x[1], 2);
                case NumericsObjectiveFunction.Matyas:
                    return x => 0.26 * (x[0] * x[0] + x[1] * x[1]) - 0.48 * x[0] * x[1];
                case NumericsObjectiveFunction.StyblinskiTang:
                    return x => 0.5 * x.Sum(v => Math.Pow(v, 4) - 16 * v * v + 5 * v);
                case NumericsObjectiveFunction.Griewank:
                    return x =>
                    {
                        var sum = 0.0;
                        var product = 1.0;

                        for (var i = 0; i < x.Length; i++)
                        {
                            sum += x[i] * x[i] / 4000.0;
                            product *= Math.Cos(x[i] / Math.Sqrt(i + 1));
                        }

                        return 1 + sum - product;
                    };
                case NumericsObjectiveFunction.Quadratic:
                    var q = QuadraticParts(quadraticMatrix, linearTerms, dimension, parameterName);

                    return x =>
                    {
                        var s = constantTerm;

                        for (var i = 0; i < dimension; i++)
                        {
                            s += q.Linear[i] * x[i];

                            for (var j = 0; j < dimension; j++)
                            {
                                s += 0.5 * x[i] * q.Matrix[i][j] * x[j];
                            }
                        }

                        return s;
                    };
                default:
                    throw new ArgumentException($"Unknown objective family {family}.", parameterName);
            }
        }

        public static Func<double[], double[]>? Gradient(NumericsObjectiveFunction family, int dimension,
            double[][]? quadraticMatrix, double[]? linearTerms, string parameterName)
        {
            switch (family)
            {
                case NumericsObjectiveFunction.Sphere:
                    return x => x.Select(v => 2 * v).ToArray();
                case NumericsObjectiveFunction.Rosenbrock:
                    return x =>
                    {
                        var g = new double[x.Length];

                        for (var i = 0; i < x.Length - 1; i++)
                        {
                            g[i] += -400 * x[i] * (x[i + 1] - x[i] * x[i]) - 2 * (1 - x[i]);
                            g[i + 1] += 200 * (x[i + 1] - x[i] * x[i]);
                        }

                        return g;
                    };
                case NumericsObjectiveFunction.Rastrigin:
                    return x => x.Select(v => 2 * v + 20 * Math.PI * Math.Sin(2 * Math.PI * v)).ToArray();
                case NumericsObjectiveFunction.Himmelblau:
                    return x =>
                    {
                        var u = x[0] * x[0] + x[1] - 11;
                        var v = x[0] + x[1] * x[1] - 7;

                        return new[] { 4 * x[0] * u + 2 * v, 2 * u + 4 * x[1] * v };
                    };
                case NumericsObjectiveFunction.Booth:
                    return x =>
                    {
                        var u = x[0] + 2 * x[1] - 7;
                        var v = 2 * x[0] + x[1] - 5;

                        return new[] { 2 * u + 4 * v, 4 * u + 2 * v };
                    };
                case NumericsObjectiveFunction.Beale:
                    return x =>
                    {
                        var a = 1.5 - x[0] + x[0] * x[1];
                        var b = 2.25 - x[0] + x[0] * x[1] * x[1];
                        var c = 2.625 - x[0] + x[0] * x[1] * x[1] * x[1];

                        return new[]
                        {
                            2 * a * (x[1] - 1) + 2 * b * (x[1] * x[1] - 1) + 2 * c * (x[1] * x[1] * x[1] - 1),
                            2 * a * x[0] + 2 * b * 2 * x[0] * x[1] + 2 * c * 3 * x[0] * x[1] * x[1]
                        };
                    };
                case NumericsObjectiveFunction.Matyas:
                    return x => new[] { 0.52 * x[0] - 0.48 * x[1], 0.52 * x[1] - 0.48 * x[0] };
                case NumericsObjectiveFunction.StyblinskiTang:
                    return x => x.Select(v => 0.5 * (4 * v * v * v - 32 * v + 5)).ToArray();
                case NumericsObjectiveFunction.Griewank:
                    return x =>
                    {
                        var n = x.Length;
                        var cosines = new double[n];

                        for (var i = 0; i < n; i++)
                        {
                            cosines[i] = Math.Cos(x[i] / Math.Sqrt(i + 1));
                        }

                        var g = new double[n];

                        for (var j = 0; j < n; j++)
                        {
                            var product = 1.0;

                            for (var i = 0; i < n; i++)
                            {
                                if (i != j)
                                {
                                    product *= cosines[i];
                                }
                            }

                            g[j] = x[j] / 2000.0 + Math.Sin(x[j] / Math.Sqrt(j + 1)) / Math.Sqrt(j + 1) * product;
                        }

                        return g;
                    };
                case NumericsObjectiveFunction.Quadratic:
                    var q = QuadraticParts(quadraticMatrix, linearTerms, dimension, parameterName);

                    return x =>
                    {
                        var g = new double[dimension];

                        for (var i = 0; i < dimension; i++)
                        {
                            g[i] = q.Linear[i];

                            for (var j = 0; j < dimension; j++)
                            {
                                g[i] += 0.5 * (q.Matrix[i][j] + q.Matrix[j][i]) * x[j];
                            }
                        }

                        return g;
                    };
                default:
                    return null;
            }
        }

        public static int ParameterCount(NumericsUnivariateFunction family, int polynomialCoefficients)
        {
            switch (family)
            {
                case NumericsUnivariateFunction.Polynomial:
                    return polynomialCoefficients;
                case NumericsUnivariateFunction.Sine:
                case NumericsUnivariateFunction.Cosine:
                case NumericsUnivariateFunction.Logistic:
                case NumericsUnivariateFunction.HyperbolicTangent:
                    return 4;
                case NumericsUnivariateFunction.NaturalLog:
                case NumericsUnivariateFunction.SquareRoot:
                    return 2;
                default:
                    return 3;
            }
        }

        public static double[] PartialDerivatives(NumericsUnivariateFunction family, double[] p, double x)
        {
            switch (family)
            {
                case NumericsUnivariateFunction.Polynomial:
                    var powers = new double[p.Length];
                    var power = 1.0;

                    for (var i = 0; i < p.Length; i++)
                    {
                        powers[i] = power;
                        power *= x;
                    }

                    return powers;
                case NumericsUnivariateFunction.Sine:
                    return new[]
                    {
                        Math.Sin(p[1] * x + p[2]), p[0] * x * Math.Cos(p[1] * x + p[2]),
                        p[0] * Math.Cos(p[1] * x + p[2]), 1
                    };
                case NumericsUnivariateFunction.Cosine:
                    return new[]
                    {
                        Math.Cos(p[1] * x + p[2]), -p[0] * x * Math.Sin(p[1] * x + p[2]),
                        -p[0] * Math.Sin(p[1] * x + p[2]),
                        1
                    };
                case NumericsUnivariateFunction.Exponential:
                    var e = Math.Exp(p[1] * x);

                    return new[] { e, p[0] * x * e, 1 };
                case NumericsUnivariateFunction.NaturalLog:
                    return new[] { Math.Log(x), 1 };
                case NumericsUnivariateFunction.SquareRoot:
                    return new[] { Math.Sqrt(x), 1 };
                case NumericsUnivariateFunction.Power:
                    var xb = Math.Pow(x, p[1]);

                    return new[] { xb, p[0] * xb * Math.Log(x), 1 };
                case NumericsUnivariateFunction.Reciprocal:
                    return new[] { 1 / (x - p[1]), p[0] / ((x - p[1]) * (x - p[1])), 1 };
                case NumericsUnivariateFunction.Gaussian:
                    var g = Math.Exp(-((x - p[1]) * (x - p[1])) / (2 * p[2] * p[2]));

                    return new[]
                    {
                        g, p[0] * g * (x - p[1]) / (p[2] * p[2]),
                        p[0] * g * (x - p[1]) * (x - p[1]) / (p[2] * p[2] * p[2])
                    };
                case NumericsUnivariateFunction.Logistic:
                    var s = 1 / (1 + Math.Exp(-p[1] * (x - p[2])));

                    return new[] { s, p[0] * s * (1 - s) * (x - p[2]), -p[0] * s * (1 - s) * p[1], 1 };
                case NumericsUnivariateFunction.HyperbolicTangent:
                    var t = Math.Tanh(p[1] * x + p[2]);

                    return new[] { t, p[0] * (1 - t * t) * x, p[0] * (1 - t * t), 1 };
                default:
                    return new[] { Math.Abs(x - p[1]), -p[0] * Math.Sign(x - p[1]), 1 };
            }
        }

        public static (double[][] Matrix, double[] Linear) QuadraticParts(double[][]? quadraticMatrix,
            double[]? linearTerms, int dimension, string parameterName)
        {
            if (quadraticMatrix == null)
            {
                throw new ArgumentException(
                    "The Quadratic family needs 'quadraticMatrix': a square matrix Q so the objective is 0.5 x'Qx + c'x + constant.",
                    nameof(quadraticMatrix));
            }

            NumericsGuard.RequireMatrix(quadraticMatrix, nameof(quadraticMatrix), out var rows, out _, true);

            if (rows != dimension)
            {
                throw new ArgumentException(
                    $"'quadraticMatrix' is {rows} by {rows} but the point has {dimension} coordinates; they must agree.",
                    nameof(quadraticMatrix));
            }

            var lin = linearTerms ?? new double[dimension];
            NumericsGuard.RequireVector(lin, nameof(linearTerms), dimension, dimension);

            return (quadraticMatrix, lin);
        }

        public static void RequireDimension(NumericsObjectiveFunction family, int dimension, string parameterName)
        {
            NumericsGuard.RequireInRange(dimension, parameterName, 1, NumericsGuard.MaxVariables);

            switch (family)
            {
                case NumericsObjectiveFunction.Himmelblau:
                case NumericsObjectiveFunction.Booth:
                case NumericsObjectiveFunction.Beale:
                case NumericsObjectiveFunction.Matyas:
                    if (dimension != 2)
                    {
                        throw new ArgumentException(
                            $"The {family} function is defined for exactly 2 variables but {dimension} were supplied.",
                            parameterName);
                    }

                    break;
                case NumericsObjectiveFunction.Rosenbrock:
                    if (dimension < 2)
                    {
                        throw new ArgumentException("The Rosenbrock function needs at least 2 variables.",
                            parameterName);
                    }

                    break;
            }
        }
    }
}
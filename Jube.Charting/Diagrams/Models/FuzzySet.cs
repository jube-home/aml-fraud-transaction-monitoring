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

using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;
using Jube.Charting.Ticks;

namespace Jube.Charting.Diagrams.Models
{
    public sealed record FuzzySet(string Label, FuzzyShape Shape, IReadOnlyList<double> Parameters)
    {
        public static FuzzySet Triangular(string label, double a, double b, double c)
        {
            return new FuzzySet(label, FuzzyShape.Triangular, [a, b, c]);
        }

        public static FuzzySet Trapezoidal(string label, double a, double b, double c, double d)
        {
            return new FuzzySet(label, FuzzyShape.Trapezoidal, [a, b, c, d]);
        }

        public static FuzzySet Gaussian(string label, double mean, double sigma)
        {
            return new FuzzySet(label, FuzzyShape.Gaussian, [mean, sigma]);
        }

        public static FuzzySet Sigmoid(string label, double centre, double slope)
        {
            return new FuzzySet(label, FuzzyShape.Sigmoid, [centre, slope]);
        }

        public static FuzzySet Points(string label, params double[] xThenMu)
        {
            return new FuzzySet(label, FuzzyShape.Points, xThenMu);
        }

        public double Membership(double x)
        {
            var p = Parameters;

            switch (Shape)
            {
                case FuzzyShape.Triangular:
                    return x <= p[0] || x >= p[2] ? Math.Abs(x - p[1]) < 0.0001 ? 1 : 0 :
                        x < p[1] ? (x - p[0]) / (p[1] - p[0]) :
                        Math.Abs(x - p[1]) < 0.0001 ? 1 : (p[2] - x) / (p[2] - p[1]);
                case FuzzyShape.Trapezoidal:
                    if (x < p[0] || x > p[3])
                    {
                        return 0;
                    }

                    if (x >= p[1] && x <= p[2])
                    {
                        return 1;
                    }

                    return x < p[1] ? (x - p[0]) / (p[1] - p[0]) : (p[3] - x) / (p[3] - p[2]);
                case FuzzyShape.Gaussian:
                    return Math.Exp(-0.5 * (x - p[0]) * (x - p[0]) / (p[1] * p[1]));
                case FuzzyShape.Sigmoid:
                    return 1 / (1 + Math.Exp(-p[1] * (x - p[0])));
                case FuzzyShape.Points:
                    if (x < p[0] || x > p[^2])
                    {
                        return 0;
                    }

                    for (var i = 2; i < p.Count; i += 2)
                    {
                        if (x <= p[i])
                        {
                            var t = Math.Abs(p[i] - p[i - 2]) < 0.0001 ? 1 : (x - p[i - 2]) / (p[i] - p[i - 2]);

                            return p[i - 1] + t * (p[i + 1] - p[i - 1]);
                        }
                    }

                    return p[^1];
                default:
                    throw new DiagramNotSupportedException("the membership shape " + Shape + " is unknown.");
            }
        }

        internal IEnumerable<double> KeyPoints()
        {
            return Shape switch
            {
                FuzzyShape.Triangular => Parameters,
                FuzzyShape.Trapezoidal => Parameters,
                FuzzyShape.Gaussian => [Parameters[0]],
                FuzzyShape.Points => Parameters.Where((_, i) => i % 2 == 0),
                _ => []
            };
        }

        internal string Describe()
        {
            var args = Shape == FuzzyShape.Points
                ? Parameters.Count / 2 + " points"
                : string.Join(", ", Parameters.Select(NumberFormat.Value));

            return Shape + "(" + args + ")";
        }

        internal void Validate()
        {
            var p = Parameters;

            if (p.Any(v => !double.IsFinite(v)))
            {
                throw new ChartSpecificationException("Membership function parameters must be finite.");
            }

            void Ok(bool condition, string message)
            {
                if (!condition)
                {
                    throw new ChartSpecificationException(Label + ": " + message);
                }
            }

            switch (Shape)
            {
                case FuzzyShape.Triangular:
                    Ok(p.Count == 3 && p[0] <= p[1] && p[1] <= p[2] && p[0] < p[2],
                        "a triangle needs a <= b <= c and a < c.");

                    break;
                case FuzzyShape.Trapezoidal:
                    Ok(p.Count == 4 && p[0] <= p[1] && p[1] <= p[2] && p[2] <= p[3] && p[0] < p[3],
                        "a trapezoid needs a <= b <= c <= d and a < d.");

                    break;
                case FuzzyShape.Gaussian:
                    Ok(p is [_, > 0], "a Gaussian needs a mean and a positive sigma.");
                    break;
                case FuzzyShape.Sigmoid:
                    Ok(p.Count == 2 && p[1] != 0, "a sigmoid needs a centre and a non-zero slope.");
                    break;
                case FuzzyShape.Points:
                    Ok(p.Count >= 4 && p.Count % 2 == 0, "a point set needs at least two x and membership pairs.");

                    for (var i = 0; i < p.Count; i += 2)
                    {
                        Ok(p[i + 1] >= 0 && p[i + 1] <= 1 && (i == 0 || p[i] > p[i - 2]),
                            "points need increasing x and membership within 0 and 1.");
                    }

                    break;
                default:
                    throw new DiagramNotSupportedException("the membership shape " + Shape + " is unknown.");
            }
        }
    }
}
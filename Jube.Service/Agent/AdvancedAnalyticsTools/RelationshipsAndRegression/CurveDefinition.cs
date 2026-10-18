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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal sealed class CurveDefinition
    {
        private static readonly Dictionary<NonlinearCurveFamily, CurveDefinition> definitions = Build();
        public NonlinearCurveFamily Family { get; init; }
        public string Formula { get; init; } = string.Empty;
        public string[] Parameters { get; init; } = Array.Empty<string>();
        public bool RequiresPositiveX { get; init; }
        public bool RequiresNonNegativeX { get; init; }
        public Func<double[], double, double> Evaluate { get; init; } = (_, _) => 0;
        public Action<double[], double, double[]> Gradient { get; init; } = (_, _, _) => { };
        public Func<double[], double[], double[]> Start { get; init; } = (_, _) => Array.Empty<double>();

        public static CurveDefinition For(NonlinearCurveFamily family)
        {
            return definitions.TryGetValue(family, out var definition)
                ? definition
                : throw new ArgumentException(
                    "Unknown curve family. Use one of Exponential, AsymptoticExponential, SaturatingExponential, Power, Logarithmic, Logistic3, Logistic4, Gompertz, MichaelisMenten, Hill, GaussianPeak or Reciprocal.",
                    "family");
        }

        private static double[]? LinearStart(double[] x, double[] y)
        {
            if (x.Length < 3 || !(x.Max() > x.Min()))
            {
                return null;
            }

            try
            {
                var fit = LinearFit.Fit(x.Select(v => new[] { v }).ToArray(), y, null, new[] { "x" }, true);

                return new[] { fit.Beta[0], fit.Beta[1] };
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static double XAtLevel(double[] x, double[] y, double level)
        {
            var index = Enumerable.Range(0, x.Length).OrderBy(i => Math.Abs(y[i] - level)).First();

            return x[index];
        }

        private static Dictionary<NonlinearCurveFamily, CurveDefinition> Build()
        {
            var list = new List<CurveDefinition>
            {
                new()
                {
                    Family = NonlinearCurveFamily.Exponential,
                    Formula = "y = scale * exp(rate * x)",
                    Parameters = new[] { "scale", "rate" },
                    Evaluate = (p, x) => p[0] * Math.Exp(p[1] * x),
                    Gradient = (p, x, g) =>
                    {
                        var e = Math.Exp(p[1] * x);
                        g[0] = e;
                        g[1] = p[0] * x * e;
                    },
                    Start = (x, y) =>
                    {
                        var fit = y.All(v => v > 0) ? LinearStart(x, y.Select(v => Math.Log(v)).ToArray()) : null;

                        return fit == null ? new[] { y.Average(), 0.01 } : new[] { Math.Exp(fit[0]), fit[1] };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.AsymptoticExponential,
                    Formula = "y = asymptote - amplitude * exp(-rate * x)",
                    Parameters = new[] { "asymptote", "amplitude", "rate" },
                    Evaluate = (p, x) => p[0] - p[1] * Math.Exp(-p[2] * x),
                    Gradient = (p, x, g) =>
                    {
                        var e = Math.Exp(-p[2] * x);
                        g[0] = 1;
                        g[1] = -e;
                        g[2] = p[1] * x * e;
                    },
                    Start = (x, y) =>
                    {
                        var range = y.Max() - y.Min();
                        var rising = y[Array.IndexOf(x, x.Max())] >= y[Array.IndexOf(x, x.Min())];
                        var a = rising ? y.Max() + 0.05 * range : y.Min() - 0.05 * range;
                        var c = 3 / Math.Max(x.Max() - x.Min(), 1e-12);
                        var first = y[Array.IndexOf(x, x.Min())];

                        return new[] { a, (a - first) * Math.Exp(c * x.Min()), c };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.SaturatingExponential,
                    Formula = "y = maximum * (1 - exp(-rate * x))",
                    Parameters = new[] { "maximum", "rate" },
                    RequiresNonNegativeX = true,
                    Evaluate = (p, x) => p[0] * (1 - Math.Exp(-p[1] * x)),
                    Gradient = (p, x, g) =>
                    {
                        var e = Math.Exp(-p[1] * x);
                        g[0] = 1 - e;
                        g[1] = p[0] * x * e;
                    },
                    Start = (x, y) => new[]
                    {
                        y.Max() > 0 ? 1.05 * y.Max() : 1.05 * y.Min(), 3 / Math.Max(x.Max(), 1e-12)
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Power,
                    Formula = "y = scale * x ^ exponent",
                    Parameters = new[] { "scale", "exponent" },
                    RequiresPositiveX = true,
                    Evaluate = (p, x) => p[0] * Math.Pow(x, p[1]),
                    Gradient = (p, x, g) =>
                    {
                        var power = Math.Pow(x, p[1]);
                        g[0] = power;
                        g[1] = p[0] * power * Math.Log(x);
                    },
                    Start = (x, y) =>
                    {
                        var fit = y.All(v => v > 0)
                            ? LinearStart(x.Select(v => Math.Log(v)).ToArray(), y.Select(v => Math.Log(v)).ToArray())
                            : null;

                        return fit == null ? new[] { 1d, 1d } : new[] { Math.Exp(fit[0]), fit[1] };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Logarithmic,
                    Formula = "y = intercept + slope * ln(x)",
                    Parameters = new[] { "intercept", "slope" },
                    RequiresPositiveX = true,
                    Evaluate = (p, x) => p[0] + p[1] * Math.Log(x),
                    Gradient = (_, x, g) =>
                    {
                        g[0] = 1;
                        g[1] = Math.Log(x);
                    },
                    Start = (x, y) => LinearStart(x.Select(v => Math.Log(v)).ToArray(), y) ?? new[] { y.Average(), 0d }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Logistic3,
                    Formula = "y = maximum / (1 + exp(-steepness * (x - midpoint)))",
                    Parameters = new[] { "maximum", "steepness", "midpoint" },
                    Evaluate = (p, x) => p[0] / (1 + Math.Exp(-p[1] * (x - p[2]))),
                    Gradient = (p, x, g) =>
                    {
                        var e = Math.Exp(-p[1] * (x - p[2]));
                        var d = 1 + e;
                        g[0] = 1 / d;
                        g[1] = p[0] * (x - p[2]) * e / (d * d);
                        g[2] = -p[0] * p[1] * e / (d * d);
                    },
                    Start = (x, y) =>
                    {
                        var top = y.Max() > 0 ? 1.1 * y.Max() : 1;
                        var rising = y[Array.IndexOf(x, x.Max())] >= y[Array.IndexOf(x, x.Min())];
                        var span = Math.Max(x.Max() - x.Min(), 1e-12);

                        return new[] { top, (rising ? 1 : -1) * 8 / span, XAtLevel(x, y, top / 2) };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Logistic4,
                    Formula = "y = lower + (upper - lower) / (1 + exp(-steepness * (x - midpoint)))",
                    Parameters = new[] { "lower", "upper", "steepness", "midpoint" },
                    Evaluate = (p, x) => p[0] + (p[1] - p[0]) / (1 + Math.Exp(-p[2] * (x - p[3]))),
                    Gradient = (p, x, g) =>
                    {
                        var e = Math.Exp(-p[2] * (x - p[3]));
                        var d = 1 + e;
                        g[0] = 1 - 1 / d;
                        g[1] = 1 / d;
                        g[2] = (p[1] - p[0]) * (x - p[3]) * e / (d * d);
                        g[3] = -(p[1] - p[0]) * p[2] * e / (d * d);
                    },
                    Start = (x, y) =>
                    {
                        var lo = y.Min();
                        var hi = y.Max();
                        var rising = y[Array.IndexOf(x, x.Max())] >= y[Array.IndexOf(x, x.Min())];
                        var span = Math.Max(x.Max() - x.Min(), 1e-12);

                        return new[] { lo, hi, (rising ? 1 : -1) * 8 / span, XAtLevel(x, y, (lo + hi) / 2) };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Gompertz,
                    Formula = "y = maximum * exp(-displacement * exp(-rate * x))",
                    Parameters = new[] { "maximum", "displacement", "rate" },
                    Evaluate = (p, x) => p[0] * Math.Exp(-p[1] * Math.Exp(-p[2] * x)),
                    Gradient = (p, x, g) =>
                    {
                        var gg = Math.Exp(-p[2] * x);
                        var h = Math.Exp(-p[1] * gg);
                        g[0] = h;
                        g[1] = -p[0] * gg * h;
                        g[2] = p[0] * p[1] * x * gg * h;
                    },
                    Start = (x, y) =>
                    {
                        var top = y.Max() > 0 ? 1.1 * y.Max() : 1;
                        var index = Enumerable.Range(0, x.Length).Where(i => y[i] > 0 && y[i] < top).ToArray();

                        var fit = index.Length >= 3
                            ? LinearStart(index.Select(i => x[i]).ToArray(),
                                index.Select(i => Math.Log(-Math.Log(y[i] / top))).ToArray())
                            : null;

                        return fit == null || !(fit[1] < 0)
                            ? new[] { top, 1d, 1 / Math.Max(x.Max() - x.Min(), 1e-12) }
                            : new[] { top, Math.Exp(fit[0]), -fit[1] };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.MichaelisMenten,
                    Formula = "y = maximumRate * x / (halfSaturation + x)",
                    Parameters = new[] { "maximumRate", "halfSaturation" },
                    RequiresNonNegativeX = true,
                    Evaluate = (p, x) => p[0] * x / (p[1] + x),
                    Gradient = (p, x, g) =>
                    {
                        g[0] = x / (p[1] + x);
                        g[1] = -p[0] * x / ((p[1] + x) * (p[1] + x));
                    },
                    Start = (x, y) =>
                    {
                        var index = Enumerable.Range(0, x.Length).Where(i => x[i] > 0 && y[i] > 0).ToArray();

                        var fit = index.Length >= 3
                            ? LinearStart(index.Select(i => 1 / x[i]).ToArray(), index.Select(i => 1 / y[i]).ToArray())
                            : null;

                        if (fit != null && fit[0] > 0 && fit[1] > 0)
                        {
                            var v = 1 / fit[0];

                            return new[] { v, fit[1] * v };
                        }

                        return new[] { 1.05 * y.Max(), x.OrderBy(v => v).ElementAt(x.Length / 2) };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Hill,
                    Formula = "y = maximum * x^coefficient / (halfSaturation^coefficient + x^coefficient)",
                    Parameters = new[] { "maximum", "halfSaturation", "coefficient" },
                    RequiresPositiveX = true,
                    Evaluate = (p, x) => p[0] * Math.Pow(x, p[2]) / (Math.Pow(p[1], p[2]) + Math.Pow(x, p[2])),
                    Gradient = (p, x, g) =>
                    {
                        var xn = Math.Pow(x, p[2]);
                        var kn = Math.Pow(p[1], p[2]);
                        var d = kn + xn;
                        g[0] = xn / d;
                        g[1] = -p[0] * xn * p[2] * Math.Pow(p[1], p[2] - 1) / (d * d);
                        g[2] = p[0] * xn * kn * (Math.Log(x) - Math.Log(p[1])) / (d * d);
                    },
                    Start = (x, y) => new[]
                    {
                        1.05 * y.Max(), Math.Max(XAtLevel(x, y, y.Max() / 2), 1e-9), 1d
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.GaussianPeak,
                    Formula = "y = height * exp(-(x - centre)^2 / (2 * width^2))",
                    Parameters = new[] { "height", "centre", "width" },
                    Evaluate = (p, x) => p[0] * Math.Exp(-(x - p[1]) * (x - p[1]) / (2 * p[2] * p[2])),
                    Gradient = (p, x, g) =>
                    {
                        var q = Math.Exp(-(x - p[1]) * (x - p[1]) / (2 * p[2] * p[2]));
                        g[0] = q;
                        g[1] = p[0] * q * (x - p[1]) / (p[2] * p[2]);
                        g[2] = p[0] * q * (x - p[1]) * (x - p[1]) / (p[2] * p[2] * p[2]);
                    },
                    Start = (x, y) =>
                    {
                        var floor = y.Min();
                        var w = y.Select(v => v - floor).ToArray();
                        var total = w.Sum();
                        var centre = total > 0 ? x.Zip(w, (a, b) => a * b).Sum() / total : x.Average();

                        var width = total > 0
                            ? Math.Sqrt(x.Zip(w, (a, b) => b * (a - centre) * (a - centre)).Sum() / total)
                            : 1;

                        return new[] { y.Max(), centre, Math.Max(width, 1e-6) };
                    }
                },
                new()
                {
                    Family = NonlinearCurveFamily.Reciprocal,
                    Formula = "y = 1 / (intercept + slope * x)",
                    Parameters = new[] { "intercept", "slope" },
                    Evaluate = (p, x) => 1 / (p[0] + p[1] * x),
                    Gradient = (p, x, g) =>
                    {
                        var d = p[0] + p[1] * x;
                        g[0] = -1 / (d * d);
                        g[1] = -x / (d * d);
                    },
                    Start = (x, y) =>
                        y.All(v => v != 0)
                            ? LinearStart(x, y.Select(v => 1 / v).ToArray()) ?? new[] { 1d, 0.1 }
                            : new[] { 1d, 0.1 }
                }
            };

            return list.ToDictionary(d => d.Family);
        }
    }
}
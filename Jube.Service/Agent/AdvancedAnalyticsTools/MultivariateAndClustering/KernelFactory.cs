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

using Accord.Statistics.Kernels;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    public enum SimilarityKernelKind
    {
        Linear,
        Polynomial,
        NormalizedPolynomial,
        Quadratic,
        Gaussian,
        Laplacian,
        Cauchy,
        Bessel,
        Spherical,
        Circular,
        Wave,
        ThinPlateSpline,
        RationalQuadratic,
        Multiquadric,
        InverseMultiquadric,
        Sigmoid,
        Power,
        Log,
        TStudent,
        Hypersecant,
        SquaredSinc,
        SymmetricTriangle,
        Pearson,
        ChiSquare,
        Hellinger,
        HistogramIntersection,
        Spline,
        BSpline,
        Anova,
        Dirichlet
    }

    internal static class KernelFactory
    {
        public static IKernel Create(SimilarityKernelKind kind, double sigma, double gamma, double? constantOrDefault,
            int degree,
            double alpha, double beta, int dimensions)
        {
            var constant = constantOrDefault ?? (kind == SimilarityKernelKind.Linear ? 0.0 : 1.0);

            switch (kind)
            {
                case SimilarityKernelKind.Linear:
                    return new Linear(constant);
                case SimilarityKernelKind.Polynomial:
                    RequireDegree(degree, kind);

                    return new Polynomial(degree, constant);
                case SimilarityKernelKind.NormalizedPolynomial:
                    RequireDegree(degree, kind);

                    return new NormalizedPolynomial(degree, constant);
                case SimilarityKernelKind.Quadratic:
                    return new Quadratic(constant);
                case SimilarityKernelKind.Gaussian:
                    return new Gaussian(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Laplacian:
                    return new Laplacian(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Cauchy:
                    return new Cauchy(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Bessel:
                    RequireDegree(degree, kind);

                    return new Bessel(degree, RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Spherical:
                    return new Spherical(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Circular:
                    return new Circular(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.Wave:
                    return new Wave(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.ThinPlateSpline:
                    return new ThinSplinePlate(RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.RationalQuadratic:
                    return new RationalQuadratic(constant);
                case SimilarityKernelKind.Multiquadric:
                    return new Multiquadric(constant);
                case SimilarityKernelKind.InverseMultiquadric:
                    return new InverseMultiquadric(constant);
                case SimilarityKernelKind.Sigmoid:
                    return new Sigmoid(alpha, constant);
                case SimilarityKernelKind.Power:
                    RequireDegree(degree, kind);

                    return new Power(degree);
                case SimilarityKernelKind.Log:
                    RequireDegree(degree, kind);

                    return new Log(degree);
                case SimilarityKernelKind.TStudent:
                    RequireDegree(degree, kind);

                    return new TStudent(degree);
                case SimilarityKernelKind.Hypersecant:
                    return new Hypersecant(RequireScale(gamma, "kernelGamma", kind));
                case SimilarityKernelKind.SquaredSinc:
                    return new SquaredSinc(RequireScale(gamma, "kernelGamma", kind));
                case SimilarityKernelKind.SymmetricTriangle:
                    return new SymmetricTriangle(RequireScale(gamma, "kernelGamma", kind));
                case SimilarityKernelKind.Pearson:
                    return new Pearson(alpha, RequireScale(sigma, "kernelSigma", kind));
                case SimilarityKernelKind.ChiSquare:
                    return new ChiSquare();
                case SimilarityKernelKind.Hellinger:
                    return new Hellinger();
                case SimilarityKernelKind.HistogramIntersection:
                    return new HistogramIntersection(alpha, beta);
                case SimilarityKernelKind.Spline:
                    return new Spline();
                case SimilarityKernelKind.BSpline:
                    RequireDegree(degree, kind);

                    return new BSpline(degree);
                case SimilarityKernelKind.Anova:
                    RequireDegree(degree, kind);

                    return new Anova(dimensions, Math.Min(degree, dimensions));
                case SimilarityKernelKind.Dirichlet:
                    return new Dirichlet(dimensions);
                default:
                    throw new ArgumentException($"Kernel {kind} is not supported.", "kernel");
            }
        }

        private static double RequireScale(double value, string name, SimilarityKernelKind kind)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException(
                    $"{name} must be a finite number greater than zero for the {kind} kernel but was {value}.", name);
            }

            return value;
        }

        private static void RequireDegree(int degree, SimilarityKernelKind kind)
        {
            if (degree < 1 || degree > 20)
            {
                throw new ArgumentException(
                    $"kernelDegree must be between 1 and 20 for the {kind} kernel but was {degree}.",
                    "kernelDegree");
            }
        }
    }
}
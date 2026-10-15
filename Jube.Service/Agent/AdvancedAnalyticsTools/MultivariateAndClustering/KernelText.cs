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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    internal static class KernelText
    {
        public const string Kernel =
            "Which kernel to use. Linear, Polynomial and Sigmoid are dot product kernels; Gaussian, Laplacian, Cauchy, Spherical, Circular, Wave and ThinPlateSpline are distance kernels controlled by kernelSigma; Hypersecant, SquaredSinc and SymmetricTriangle use kernelGamma; Power, Log, TStudent, BSpline and Bessel use kernelDegree; ChiSquare, Hellinger and HistogramIntersection are for non negative histogram style data.";

        public const string Sigma =
            "Bandwidth for Gaussian, Laplacian, Cauchy, Bessel, Spherical, Circular, Wave, ThinPlateSpline and Pearson kernels; must be above zero. Larger values make similarity fall off more slowly. Default 1.";

        public const string Gamma =
            "Scale for Hypersecant, SquaredSinc and SymmetricTriangle kernels; must be above zero. Default 1.";

        public const string Constant =
            "Additive constant for Linear, Polynomial, NormalizedPolynomial, Quadratic, RationalQuadratic, Multiquadric, InverseMultiquadric and Sigmoid kernels. Leave empty for the default: 0 for Linear (plain dot product) and 1 for the others.";

        public const string Degree =
            "Integer order between 1 and 20 for Polynomial, NormalizedPolynomial, Power, Log, TStudent, BSpline, Bessel and Anova kernels. Default 2.";

        public const string Alpha =
            "Slope for the Sigmoid kernel, omega for Pearson and the alpha exponent for HistogramIntersection. Default 1.";

        public const string Beta = "The beta exponent for the HistogramIntersection kernel. Default 1.";
    }
}
// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Diego Catalano, 2013
// diego.catalano at live.com
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System.Numerics;
using Accord.Diagnostics;

namespace Accord.Math
{
    /// <summary>
    ///     Gabor kernel types.
    /// </summary>
    public enum GaborKernelKind
    {
        /// <summary>
        ///     Creates kernel based on the real part of the Gabor function.
        /// </summary>
        Real,

        /// <summary>
        ///     Creates a kernel based on the imaginary part of the Gabor function.
        /// </summary>
        Imaginary,

        /// <summary>
        ///     Creates a kernel based on the Magnitude of the Gabor function.
        /// </summary>
        Magnitude,

        /// <summary>
        ///     Creates a kernel based on the Squared Magnitude of the Gabor function.
        /// </summary>
        SquaredMagnitude
    }

    /// <summary>
    ///     Gabor functions.
    /// </summary>
    /// <remarks>
    ///     This class has been contributed by Diego Catalano, author of the Catalano
    ///     Framework, a native port of AForge.NET and Accord.NET for Java and Android.
    /// </remarks>
    public static class Gabor
    {
        /// <summary>
        ///     1-D Gabor function.
        /// </summary>
        public static double Function1D(double x, double mean, double amplitude,
            double position, double width, double phase, double frequency)
        {
            var a = (x - position) * (x - position);
            var b = 2 * width * (2 * width);

            var envelope = mean + amplitude * System.Math.Exp(-a / b);
            var carry = System.Math.Cos(2 * System.Math.PI * frequency * (x - position) + phase);

            return envelope * carry;
        }

        /// <summary>
        ///     2-D Gabor function.
        /// </summary>
        public static Complex Function2D(int x, int y, double lambda, double theta,
            double psi, double sigma, double gamma)
        {
            var X = +x * System.Math.Cos(theta) + y * System.Math.Sin(theta);
            var Y = -x * System.Math.Sin(theta) + y * System.Math.Cos(theta);

            var envelope = System.Math.Exp(-((X * X + gamma * gamma * Y * Y) / (2 * sigma * sigma)));
            var real = System.Math.Cos(2 * System.Math.PI * (X / lambda) + psi);
            var imaginary = System.Math.Sin(2 * System.Math.PI * (X / lambda) + psi);

            return new Complex(envelope * real, envelope * imaginary);
        }

        /// <summary>
        ///     Real part of the 2-D Gabor function.
        /// </summary>
        public static double RealFunction2D(int x, int y, double lambda, double theta,
            double psi, double sigma, double gamma)
        {
            var X = +x * System.Math.Cos(theta) + y * System.Math.Sin(theta);
            var Y = -x * System.Math.Sin(theta) + y * System.Math.Cos(theta);

            var envelope = System.Math.Exp(-((X * X + gamma * gamma * Y * Y) / (2 * sigma * sigma)));
            var carrier = System.Math.Cos(2 * System.Math.PI * (X / lambda) + psi);

            return envelope * carrier;
        }

        /// <summary>
        ///     Imaginary part of the 2-D Gabor function.
        /// </summary>
        public static double ImaginaryFunction2D(int x, int y, double lambda, double theta,
            double psi, double sigma, double gamma)
        {
            var X = +x * System.Math.Cos(theta) + y * System.Math.Sin(theta);
            var Y = -x * System.Math.Sin(theta) + y * System.Math.Cos(theta);

            var envelope = System.Math.Exp(-((X * X + gamma * gamma * Y * Y) / (2 * sigma * sigma)));
            var carrier = System.Math.Sin(2 * System.Math.PI * (X / lambda) + psi);

            return envelope * carrier;
        }

        /// <summary>
        ///     Computes the 2-D Gabor kernel.
        /// </summary>
        public static double[,] Kernel2D(double lambda, double theta, double psi,
            double sigma, double gamma)
        {
            return Kernel2D(3, lambda, theta, psi, sigma, gamma, false, GaborKernelKind.Real);
        }

        /// <summary>
        ///     Computes the 2-D Gabor kernel.
        /// </summary>
        public static double[,] Kernel2D(double lambda, double theta, double psi,
            double sigma, double gamma, bool normalized)
        {
            return Kernel2D(3, lambda, theta, psi, sigma, gamma, normalized, GaborKernelKind.Imaginary);
        }

        /// <summary>
        ///     Computes the 2-D Gabor kernel.
        /// </summary>
        public static double[,] Kernel2D(int size, double lambda, double theta, double psi,
            double sigma, double gamma, bool normalized)
        {
            return Kernel2D(size, lambda, theta, psi, sigma,
                gamma, normalized, GaborKernelKind.Imaginary);
        }

        /// <summary>
        ///     Computes the 2-D Gabor kernel.
        /// </summary>
        public static double[,] Kernel2D(int size, double lambda, double theta,
            double psi, double sigma, double gamma, bool normalized, GaborKernelKind function)
        {
            var sigmaX = sigma;
            var sigmaY = sigma / gamma;

            var a = System.Math.Max(
                System.Math.Abs(size * sigmaX * System.Math.Cos(theta)),
                System.Math.Abs(size * sigmaY * System.Math.Sin(theta)));
            var xMax = (int)System.Math.Ceiling(System.Math.Max(1, a));

            var b = System.Math.Max(
                System.Math.Abs(size * sigmaX * System.Math.Sin(theta)),
                System.Math.Abs(size * sigmaY * System.Math.Cos(theta)));
            var yMax = (int)System.Math.Ceiling(System.Math.Max(1, b));

            var xValues = Vector.Interval(-xMax, xMax);
            var yValues = Vector.Interval(-yMax, yMax);

            Debug.Assert(xValues.Length == 2 * xMax + 1);
            Debug.Assert(yValues.Length == 2 * yMax + 1);

            var kernel = new double[xValues.Length, yValues.Length];

            double sum = 0;

            switch (function)
            {
                case GaborKernelKind.Real:
                    for (var i = 0; i < xValues.Length; i++)
                    {
                        for (var j = 0; j < yValues.Length; j++)
                        {
                            sum += kernel[i, j] = RealFunction2D(
                                xValues[i], yValues[j], lambda, theta, psi, sigma, gamma);
                        }
                    }

                    break;

                case GaborKernelKind.Imaginary:
                    for (var i = 0; i < xValues.Length; i++)
                    {
                        for (var j = 0; j < yValues.Length; j++)
                        {
                            sum += kernel[i, j] = ImaginaryFunction2D(
                                xValues[i], yValues[j], lambda, theta, psi, sigma, gamma);
                        }
                    }

                    break;

                case GaborKernelKind.Magnitude:
                    for (var i = 0; i < xValues.Length; i++)
                    {
                        for (var j = 0; j < yValues.Length; j++)
                        {
                            sum += kernel[i, j] = Function2D(
                                xValues[i], yValues[j], lambda, theta, psi, sigma, gamma).Magnitude;
                        }
                    }

                    break;

                case GaborKernelKind.SquaredMagnitude:
                    for (var i = 0; i < xValues.Length; i++)
                    {
                        for (var j = 0; j < yValues.Length; j++)
                        {
                            var v = Function2D(
                                xValues[i], yValues[j], lambda, theta, psi, sigma, gamma).Magnitude;
                            sum += kernel[i, j] = v * v;
                        }
                    }

                    break;
            }

            if (normalized)
            {
                kernel.Divide(sum, kernel);
            }

            return kernel;
        }
    }
}
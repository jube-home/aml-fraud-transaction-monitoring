// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Kernel function interface.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In Machine Learning and statistics, a Kernel is a function that returns
    ///         the value of the dot product between the images of the two arguments.
    ///     </para>
    ///     <para>
    ///         <c>k(x,y) = ‹S(x),S(y)›</c>
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.support-vector.net/icml-tutorial.pdf">
    ///                         http://www.support-vector.net/icml-tutorial.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public interface ILinear<T> : IKernel<T>
    {
        /// <summary>
        ///     Elementwise addition of a and b, storing in result.
        /// </summary>
        /// <param name="a">The first vector to add.</param>
        /// <param name="b">The second vector to add.</param>
        /// <param name="result">An array to store the result.</param>
        /// <returns>The same vector passed as result.</returns>
        void Add(T a, double[] b, double[] result);

        /// <summary>
        ///     Elementwise multiplication of scalar a and vector b, accumulating in result.
        /// </summary>
        /// <param name="a">The scalar to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
        void Product(double a, T b, double[] accumulate);

        /// <summary>
        ///     Elementwise multiplication of vector a and vector b, accumulating in result.
        /// </summary>
        /// <param name="a">The vector to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
        void Product(double[] a, T b, double[] accumulate);

        ///// <summary>
        /////   Elementwise multiplication of scalar a and vector b, accumulating in result.
        ///// </summary>
        ///// 
        ///// <param name="a">The scalar to be multiplied.</param>
        ///// <param name="b">The vector to be multiplied.</param>
        ///// <param name="accumulate">An array to store the result.</param>
        ///// 
        //void Product(double a, T b, T accumulate); 

        /// <summary>
        ///     Compress a set of support vectors and weights into a single
        ///     parameter vector.
        /// </summary>
        /// <param name="weights">The weights associated with each support vector.</param>
        /// <param name="supportVectors">The support vectors.</param>
        /// <param name="c">The constant (bias) value.</param>
        /// <returns>A single parameter vector.</returns>
        T Compress(double[] weights, T[] supportVectors, out double c);

        /// <summary>
        ///     The kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        double Function(double[] y, T x);

        /// <summary>
        ///     Gets the number of parameters in the input vectors.
        /// </summary>
        int GetLength(T[] inputs);

        /// <summary>
        ///     Creates an input vector from the given double values.
        /// </summary>
        T CreateVector(double[] values);

        ///// <summary>
        /////   Creates an input vector with the given dimensions.
        ///// </summary>
        ///// 
        //T CreateVector(int dimensions);

        /// <summary>
        ///     Converts the input vectors to a double-precision representation.
        /// </summary>
        double[][] ToDouble(T[] input);
    }
}
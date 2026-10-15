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

using System.Numerics;

namespace Accord.Math.Transforms
{
    /// <summary>
    ///     Discrete Hilbert Transformation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The discrete Hilbert transform is a transformation operating on the time
    ///         domain. It performs a 90 degree phase shift, shifting positive frequencies
    ///         by +90 degrees and negative frequencies by -90 degrees. It is useful to
    ///         create analytic representation of signals.
    ///     </para>
    ///     <para>
    ///         The Hilbert transform can be implemented efficiently by using the Fast
    ///         Fourier Transform. After transforming a signal from the time-domain to
    ///         the frequency domain, one can zero its negative frequency components and
    ///         revert the signal back to obtain the phase shifting.
    ///     </para>
    ///     <para>
    ///         By applying the Hilbert transform to a signal twice, the negative of
    ///         the original signal is recovered.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Marple, S.L., "Computing the discrete-time analytic signal via FFT," IEEE
    ///                     Transactions on Signal Processing, Vol. 47, No.9 (September 1999). Available on:
    ///                     http://classes.engr.oregonstate.edu/eecs/winter2009/ece464/AnalyticSignal_Sept1999_SPTrans.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     J. F. Culling, Online, cross-indexed dictionary of DSP terms. Available on:
    ///                     http://www.cardiff.ac.uk/psych/home2/CullingJ/frames_dict.html
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static class HilbertTransform2
    {
        /// <summary>
        ///     Performs the Fast Hilbert Transform over a double[] array.
        /// </summary>
        public static void FHT(double[] data, FourierTransform.Direction direction)
        {
            var N = data.Length;

            // Forward operation
            if (direction == FourierTransform.Direction.Forward)
            {
                // Copy the input to a complex array which can be processed
                //  in the complex domain by the FFT
                var cdata = new Complex[N];
                for (var i = 0; i < N; i++)
                {
                    cdata[i] = new Complex(data[i], 0.0);
                }

                // Perform FFT
                FourierTransform2.FFT(cdata, FourierTransform.Direction.Forward);

                //double positive frequencies
                for (var i = 1; i < (N + 1) / 2; i++)
                {
                    cdata[i] *= 2.0;
                }

                // zero out negative frequencies
                //  (leaving out the dc component)
                for (var i = N / 2 + 1; i < N; i++)
                {
                    cdata[i] = Complex.Zero;
                }

                // Reverse the FFT
                FourierTransform2.FFT(cdata, FourierTransform.Direction.Backward);

                // Convert back to our initial double array
                for (var i = 0; i < N; i++)
                {
                    data[i] = cdata[i].Imaginary;
                }
            }

            else // Backward operation
            {
                // The inverse Hilbert can be calculated by
                //  negating the transform and reapplying the
                //  transformation.
                //
                // H^–1{h(t)} = –H{h(t)}

                FHT(data, FourierTransform.Direction.Forward);

                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = -data[i];
                }
            }
        }

        /// <summary>
        ///     Performs the Fast Hilbert Transform over a complex[] array.
        /// </summary>
        public static void FHT(Complex[] data, FourierTransform.Direction direction)
        {
            var N = data.Length;

            // Forward operation
            if (direction == FourierTransform.Direction.Forward)
            {
                // Makes a copy of the data so we don't lose the
                //  original information to build our final signal
                var shift = (Complex[])data.Clone();

                // Perform FFT
                FourierTransform2.FFT(shift, FourierTransform.Direction.Backward);

                //double positive frequencies
                for (var i = 1; i < N / 2; i++)
                {
                    shift[i] *= 2.0;
                }

                // zero out negative frequencies
                //  (leaving out the dc component)
                for (var i = N / 2 + 1; i < N; i++)
                {
                    shift[i] = Complex.Zero;
                }

                // Reverse the FFT
                FourierTransform2.FFT(shift, FourierTransform.Direction.Forward);

                // Put the Hilbert transform in the Imaginary part
                //  of the input signal, creating a Analytic Signal
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = new Complex(data[i].Real, shift[i].Imaginary);
                }
            }

            else // Backward operation
            {
                // Just discard the imaginary part
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = new Complex(data[i].Real, 0.0);
                }
            }
        }
    }
}
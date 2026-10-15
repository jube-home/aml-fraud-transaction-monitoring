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

namespace Accord.Statistics.Models.Markov.Topology
{
    /// <summary>
    ///     Hidden Markov model topology (architecture) specification.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         An Hidden Markov Model Topology specifies how many states and which
    ///         initial probabilities a Markov model should have. Two common topologies
    ///         can be discussed in terms of transition state probabilities and are
    ///         available to construction through the <see cref="Ergodic" /> and
    ///         <see cref="Forward" /> classes implementing this interface.
    ///     </para>
    ///     <para>
    ///         Topology specification is important with regard to both learning and
    ///         performance: A model with too many states (and thus too many settable
    ///         parameters) will require too much training data while an model with an
    ///         insufficient number of states will prohibit the HMM from capturing subtle
    ///         statistical patterns.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Alexander Schliep, "Learning Hidden Markov Model Topology".
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Richard Hughey and Anders Krogh, "Hidden Markov models for sequence analysis:
    ///                     extension and analysis of the basic method", CABIOS 12(2):95-107, 1996. Available in:
    ///                     http://compbio.soe.ucsc.edu/html_format_papers/hughkrogh96/cabios.html
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="HiddenMarkovModel" />
    /// <seealso cref="Ergodic" />
    /// <seealso cref="Forward" />
    /// <seealso cref="Custom" />
    public interface ITopology
    {
        /// <summary>
        ///     Gets the number of states in this topology.
        /// </summary>
        int States { get; set; }

        /// <summary>
        ///     Creates the state transitions matrix and the
        ///     initial state probabilities for this topology.
        /// </summary>
        int Create(bool logarithm, out double[,] transitionMatrix, out double[] initialState);
    }
}
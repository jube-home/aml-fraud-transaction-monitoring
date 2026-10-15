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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Parameters for learning a binary decision model. An object of this class is passed by
    ///     <see cref="OneVsRestLearning{TBinary, TModel}" /> or <see cref="OneVsOneLearning{TBinary, TModel}" />
    ///     to instruct how binary learning algorithms should create their binary classifiers.
    /// </summary>
    /// <typeparam name="TBinary">The type of the binary model to be learned.</typeparam>
    /// <typeparam name="TInput">The input type for the binary classifiers.</typeparam>
    public class InnerParameters<TBinary, TInput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InnerParameters{TBinary, TInput}" /> class.
        /// </summary>
        /// <param name="model">The binary model to be learned.</param>
        /// <param name="inputs">The inputs to be used.</param>
        /// <param name="outputs">The outputs to be used.</param>
        /// <param name="pair">The class labels for the problem to be learned.</param>
        public InnerParameters(TBinary model, TInput[] inputs, bool[] outputs, ClassPair pair)
        {
            Model = model;
            Inputs = inputs;
            Outputs = outputs;
            Pair = pair;
        }

        /// <summary>
        ///     Gets or sets the binary model to be learned.
        /// </summary>
        public TBinary Model { get; private set; }

        /// <summary>
        ///     Gets or sets the input data that should be used to train the classifier.
        /// </summary>
        public TInput[] Inputs { get; private set; }

        /// <summary>
        ///     Gets or sets the output data that should be used to train the classifier.
        /// </summary>
        public bool[] Outputs { get; private set; }

        /// <summary>
        ///     Gets or sets the class pair that the classifier will be designated
        ///     to learn. For <see cref="OneVsRest{TModel, TInput}" /> classifiers, the first element
        ///     in the pair designates the class to be learned against all others.
        /// </summary>
        public ClassPair Pair { get; private set; }
    }
}
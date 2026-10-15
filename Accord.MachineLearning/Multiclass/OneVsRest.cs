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

using System;
using System.Collections;
using System.Collections.Generic;
using Accord.MachineLearning.VectorMachines;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base class for multi-class classifiers based on the
    ///     "one-vs-rest" construction based on binary classifiers.
    /// </summary>
    /// <typeparam name="TModel">The type for the inner binary classifiers.</typeparam>
    /// <typeparam name="TInput">The input type handled by the classifiers. Default is double.</typeparam>
    /// <seealso cref="MultilabelSupportVectorMachine{TKernel}" />
    [Serializable]
    public class OneVsRest<TModel, TInput> :
        MultilabelLikelihoodClassifierBase<TInput>,
        IEnumerable<KeyValuePair<int, TModel>>
        where TModel : IBinaryScoreClassifier<TInput>
    {
        private TModel[] models;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OneVsRest{TBinary, TInput}" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the multi-label classification problem.</param>
        /// <param name="initializer">A function to create the inner binary classifiers.</param>
        protected OneVsRest(int classes, Func<TModel> initializer)
        {
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            Models = new TModel[classes];
            for (var i = 0; i < Models.Length; i++)
            {
                var model = initializer();
                models[i] = model;
                if (model != null)
                {
                    NumberOfInputs = model.NumberOfInputs;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the inner binary classifiers used to distinguish
        ///     between each class and all other classes.
        /// </summary>
        /// <param name="classIndex">The classifier index.</param>
        /// <returns>
        ///     A <see cref="IBinaryClassifier{T}" /> that has been trained
        ///     to distinguish between the chosen class and all other classes.
        /// </returns>
        public TModel this[int classIndex]
        {
            get => models[classIndex];
            set => models[classIndex] = value;
        }

        /// <summary>
        ///     Gets or sets the binary classifiers that have been trained
        ///     to distinguish between each class and all other classes.
        /// </summary>
        public TModel[] Models
        {
            get => models;
            set => models = value;
        }

        /// <summary>
        ///     Gets the total number of binary models in this one-vs-rest
        ///     multi-label configuration. Should be equal to the
        ///     <see cref="ITransform.NumberOfOutputs" /> (number of classes).
        /// </summary>
        public int Count => models.Length;

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<int, TModel>> GetEnumerator()
        {
            for (var i = 0; i < models.Length; i++)
            {
                yield return new KeyValuePair<int, TModel>(i, models[i]);
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Gets the binary classifier for particular class index.
        /// </summary>
        /// <param name="classIndex">Index of the class.</param>
        /// <returns>
        ///     A <see cref="IBinaryClassifier{T}" /> that has been trained
        ///     to distinguish between the chosen class and all other classes.
        /// </returns>
        public TModel GetClassifierForClass(int classIndex)
        {
            return models[classIndex];
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and a given
        ///     <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="decision">
        ///     The class label associated with the input
        ///     vector, as predicted by the classifier.
        /// </param>
        /// <returns></returns>
        public override double Score(TInput input, int classIndex, out bool decision)
        {
            return models[classIndex].Score(input, out decision);
        }

        /// <summary>
        ///     Computes whether a class label applies to an <paramref name="input" /> vector.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified as
        ///     any of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="classIndex">The class label index to be tested.</param>
        /// <returns>
        ///     A boolean value indicating whether the given
        ///     <paramref name="classIndex">
        ///         class label
        ///     </paramref>
        ///     applies to the <paramref name="input" /> vector.
        /// </returns>
        public override bool Decide(TInput input, int classIndex)
        {
            return ((IClassifier<TInput, bool>)models[classIndex]).Decide(input);
        }

        /// <summary>
        ///     Computes a log-likelihood measuring the association between
        ///     the given <paramref name="input" /> vector and a given
        ///     <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <param name="decision">
        ///     The class label associated with the input
        ///     vector, as predicted by the classifier.
        /// </param>
        public override double LogLikelihood(TInput input, int classIndex, out bool decision)
        {
            var model = models[classIndex] as IBinaryLikelihoodClassifier<TInput>;
            if (model == null)
            {
                throw new NotSupportedException();
            }

            return model.LogLikelihood(input, out decision);
        }
    }

    /// <summary>
    ///     Base class for multi-class classifiers based on the
    ///     "one-vs-rest" construction based on binary classifiers.
    /// </summary>
    /// <typeparam name="TModel">The type for the inner binary classifiers.</typeparam>
    /// <seealso cref="MultilabelSupportVectorMachine{TKernel}" />
    [Serializable]
    public class OneVsRest<TModel> : OneVsRest<TModel, double[]>
        where TModel : IBinaryScoreClassifier<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OneVsRest{TBinary}" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the multi-label classification problem.</param>
        /// <param name="initializer">A function to create the inner binary classifiers.</param>
        protected OneVsRest(int classes, Func<TModel> initializer)
            : base(classes, initializer)
        {
        }
    }
}
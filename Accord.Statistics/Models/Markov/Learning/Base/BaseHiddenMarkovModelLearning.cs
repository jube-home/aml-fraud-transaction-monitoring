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
using Accord.MachineLearning;
using Accord.Statistics.Models.Markov.Topology;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Base class for implementations of the Baum-Welch learning algorithm.
    ///     This class cannot be instantiated.
    /// </summary>
    public abstract class BaseHiddenMarkovModelLearning<TModel, TObservation> : ParallelLearningBase
    {
        private int numberOfStates;
        private ITopology topology;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovModelLearning{TModel, TObservation}" /> class.
        /// </summary>
        /// <param name="model">The model to be learned.</param>
        public BaseHiddenMarkovModelLearning(TModel model)
        {
            Model = model;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovModelLearning{TModel, TObservation}" /> class.
        /// </summary>
        public BaseHiddenMarkovModelLearning()
        {
        }

        /// <summary>
        ///     Gets or sets the number of states to be used when this
        ///     learning algorithm needs to create new models.
        /// </summary>
        /// <value>The number of states.</value>
        public int NumberOfStates
        {
            get => numberOfStates;
            set
            {
                numberOfStates = value;
                topology = null;
            }
        }

        /// <summary>
        ///     Gets or sets the state transition topology to be used when this learning
        ///     algorithm needs to create new models. Default is <see cref="Forward" />.
        /// </summary>
        /// <value>The topology to be used when this learning algorithm needs to create a new model.</value>
        public ITopology Topology
        {
            get
            {
                if (topology != null)
                {
                    return topology;
                }

                if (numberOfStates == 0)
                {
                    throw new InvalidOperationException(
                        "No topology has been set and no NumberOfStates has been set. Please set at least one of those properties before continuing.");
                }

                topology = new Ergodic(numberOfStates);
                return topology;
            }
            set
            {
                topology = value;
                numberOfStates = value.States;
            }
        }

        /// <summary>
        ///     Gets or sets the model being trained.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected abstract TModel Create(TObservation[][] x);
    }
}
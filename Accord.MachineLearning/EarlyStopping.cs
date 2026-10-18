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
using System.Collections.Generic;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Modes for storing models.
    /// </summary>
    public enum ModelStorageMode
    {
        /// <summary>
        ///     Stores a model on each iteration. This is the most
        ///     intensive method, but enables a quick restoration
        ///     of any point on the learning history.
        /// </summary>
        AllModels,

        /// <summary>
        ///     Stores only the model which had shown the minimum
        ///     validation value in the training history. All other
        ///     models are discarded and only their validation and
        ///     training values will be registered.
        /// </summary>
        MinimumOnly,

        /// <summary>
        ///     Stores only the model which had shown the maximum
        ///     validation value in the training history. All other
        ///     models are discarded and only their validation and
        ///     training values will be registered.
        /// </summary>
        MaximumOnly
    }

    /// <summary>
    ///     Early stopping training procedure.
    /// </summary>
    /// <remarks>
    ///     The early stopping training procedure monitors a validation set
    ///     during training to determine when a learning algorithm has stopped
    ///     learning and started to overfit data. This class keeps an history
    ///     of training and validation errors and will keep the best model found
    ///     during learning.
    /// </remarks>
    /// <typeparam name="TModel">The type of the model to be trained.</typeparam>
    public class EarlyStopping<TModel> where TModel : class, ICloneable
    {
        /// <summary>
        ///     Creates a new early stopping procedure object.
        /// </summary>
        public EarlyStopping()
        {
            History = new Dictionary<int, CrossValidationValues<TModel>>();
            Tolerance = 1e-5;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the early stopping algorithm. Default
        ///     is 0 (run until convergence).
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets the minimum tolerance value used
        ///     to determine convergence. Default is 1e-5.
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Gets the history of training and validation values
        ///     registered at each iteration of the learning algorithm.
        /// </summary>
        public Dictionary<int, CrossValidationValues<TModel>> History { get; }

        /// <summary>
        ///     Gets the model with minimum validation error found during learning.
        /// </summary>
        public KeyValuePair<int, CrossValidationValues<TModel>> MinValidationValue { get; private set; }

        /// <summary>
        ///     Gets the model with maximum validation error found during learning.
        /// </summary>
        public KeyValuePair<int, CrossValidationValues<TModel>> MaxValidationValue { get; private set; }

        /// <summary>
        ///     Gets or sets the storage policy for the procedure.
        /// </summary>
        public ModelStorageMode Mode { get; set; }

        /// <summary>
        ///     Gets or sets the iteration function for the procedure. This
        ///     function will be called on each iteration and should run one
        ///     iteration of the learning algorithm for the given model.
        /// </summary>
        public Func<int, CrossValidationValues<TModel>> IterationFunction { get; set; }

        /// <summary>
        ///     Starts the model training, calling the <see cref="IterationFunction" />
        ///     on each iteration.
        /// </summary>
        /// <returns>True if the model training has converged, false otherwise.</returns>
        public bool Compute()
        {
            var lastError = double.PositiveInfinity;

            for (var i = 0; i < MaxIterations; i++)
            {
                var value = IterationFunction(i);

                var currentError = value.TrainingValue;

                // If the storage mode is set to all models, the history should store
                // all created models alongside with the validation and training errors.

                if (Mode == ModelStorageMode.AllModels)
                {
                    // Create a copy of the model information and of the created model. We 
                    // have to clone it because it will keep changing in further iterations.
                    var clone = cloneValue(value, true);

                    History[i] = clone;

                    // Check if we should store the value as current maximum or minimum
                    if (MinValidationValue.Value == null || MaxValidationValue.Value == null)
                    {
                        // If this is the first iteration, store the first model as max/min
                        MinValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                        MaxValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                    }
                    else
                    {
                        // Store information only if the model is better
                        if (value.ValidationValue < MinValidationValue.Value.ValidationValue)
                        {
                            MinValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                        }

                        if (value.ValidationValue > MaxValidationValue.Value.ValidationValue)
                        {
                            MaxValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                        }
                    }
                }
                else // if (Mode == ModelStorageMode.MinimumOnly || Mode == ModelStorageMode.MaximumOnly)
                {
                    // Create a copy of the model information and of the created model. We 
                    // will not include the model at this step because we will be storing it
                    // only if it is a minimum.
                    var copy = cloneValue(value, false);

                    History[i] = copy;

                    // Check if we should store the value as current maximum or minimum
                    if (MinValidationValue.Value == null || MaxValidationValue.Value == null)
                    {
                        // If this is the first iteration, store the first model as current maximum and minimum
                        var clone = cloneValue(value, true);
                        MaxValidationValue = MinValidationValue =
                            new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                    }
                    else
                    {
                        // Store information only if the model is better
                        if (value.ValidationValue < MinValidationValue.Value.ValidationValue)
                        {
                            var clone = cloneValue(value, true);
                            MinValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                        }

                        if (value.ValidationValue > MaxValidationValue.Value.ValidationValue)
                        {
                            var clone = cloneValue(value, true);
                            MaxValidationValue = new KeyValuePair<int, CrossValidationValues<TModel>>(i, clone);
                        }
                    }
                }

                // Check for convergence
                if (System.Math.Abs(currentError - lastError) < Tolerance * System.Math.Abs(lastError))
                {
                    return true; // converged
                }
            }

            // Maximum iterations reached
            return Tolerance == 0 ? false : true;
        }

        private static CrossValidationValues<TModel> cloneValue(CrossValidationValues<TModel> value, bool includeModel)
        {
            if (includeModel)
            {
                return new CrossValidationValues<TModel>((TModel)value.Model.Clone(), value.TrainingValue,
                    value.ValidationValue)
                {
                    Tag = value.Tag
                };
            }

            return new CrossValidationValues<TModel>(null, value.TrainingValue, value.ValidationValue)
            {
                Tag = value.Tag
            };
        }
    }
}
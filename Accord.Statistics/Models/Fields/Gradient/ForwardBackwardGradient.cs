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

// #define SERIAL

using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Linear Gradient calculator class for
    ///     <see cref="HiddenConditionalRandomField{T}">
    ///         Hidden Conditional Random Fields
    ///     </see>
    ///     .
    /// </summary>
    /// <typeparam name="T">The type of the observations being modeled.</typeparam>
    public class ForwardBackwardGradient<T> : ParallelLearningBase,
        IHiddenRandomFieldGradient, IDisposable
    {
        private ThreadLocal<double> error;
        private IPotentialFunction<T> function;

        private ThreadLocal<double[]> gradient;

        private ThreadLocal<T[][]> inputs;
        private ThreadLocal<double[]> lnZx, lnZxy;
        private ThreadLocal<double[][]> logLikelihoods;
        private HiddenConditionalRandomField<T> model;
        private ThreadLocal<int[]> outputs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ForwardBackwardGradient{T}" /> class.
        /// </summary>
        public ForwardBackwardGradient()
        {
            gradient = new ThreadLocal<double[]>();
            lnZx = new ThreadLocal<double[]>();
            lnZxy = new ThreadLocal<double[]>();
            inputs = new ThreadLocal<T[][]>();
            outputs = new ThreadLocal<int[]>();
            logLikelihoods = new ThreadLocal<double[][]>();
            error = new ThreadLocal<double>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ForwardBackwardGradient{T}" /> class.
        /// </summary>
        /// <param name="model">The model to be trained.</param>
        public ForwardBackwardGradient(HiddenConditionalRandomField<T> model)
            : this()
        {
            this.model = model;
            function = model.Function;
        }

        /// <summary>
        ///     Gets or sets the inputs to be used in the next
        ///     call to the Objective or Gradient functions.
        /// </summary>
        public T[][] Inputs
        {
            get => inputs.Value;
            set
            {
                if (inputs.Value != value)
                {
                    inputs.Value = value;

                    gradient.Value = new double[model.Function.Weights.Length];
                    lnZx.Value = new double[value.Length];
                    lnZxy.Value = new double[value.Length];
                }
            }
        }

        /// <summary>
        ///     Gets or sets the outputs to be used in the next
        ///     call to the Objective or Gradient functions.
        /// </summary>
        public int[] Outputs
        {
            get => outputs.Value;
            set => outputs.Value = value;
        }

        /// <summary>
        ///     Gets or sets the current parameter
        ///     vector for the model being learned.
        /// </summary>
        public double[] Parameters
        {
            get => model.Function.Weights;
            set => model.Function.Weights = value;
        }

        /// <summary>
        ///     Gets the error computed in the last call
        ///     to the gradient or objective functions.
        /// </summary>
        public double LastError => error.Value;

        /// <summary>
        ///     Gets or sets the amount of the parameter weights
        ///     which should be included in the objective function.
        ///     Default is 0 (do not include regularization).
        /// </summary>
        public double Regularization { get; set; }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public HiddenConditionalRandomField<T> Model
        {
            get => model;
            set
            {
                model = value;
                function = model.Function;
            }
        }

        /// <summary>
        ///     Computes the gradient using the input/outputs stored in this object.
        ///     This method is thread-safe.
        /// </summary>
        /// <returns>The value of the gradient vector for the given parameters.</returns>
        public double[] Gradient()
        {
            // Localize thread locals
            var logLikelihoods = this.logLikelihoods.Value;
            var inputs = this.inputs.Value;
            var outputs = this.outputs.Value;
            var lnZx = this.lnZx.Value;
            var lnZxy = this.lnZxy.Value;
            var gradient = this.gradient.Value;

            double error = 0;

            // The previous call to Objective could have computed
            // the log-likelihoods for all input values. However, if
            // this hasn't been the case, compute them now:

            if (logLikelihoods == null)
            {
                model.LogLikelihood(inputs, outputs, out logLikelihoods);
            }

            // Compute the partition function using the previously
            // computed likelihoods. Also compute the total error

            // For each x, compute lnZ(x) and lnZ(x,y)
            for (var i = 0; i < inputs.Length; i++)
            {
                var lli = logLikelihoods[i];

                // Compute the marginal likelihood
                var sum = double.NegativeInfinity;
                for (var j = 0; j < lli.Length; j++)
                {
                    sum = Special.LogSum(sum, lli[j]);
                }

                lnZx[i] = sum;
                lnZxy[i] = lli[outputs[i]];

                // compute and return the negative
                // log-likelihood as error function
                error -= lnZxy[i] - lnZx[i];

                Debug.Assert(!double.IsNaN(error));
            }

            // Now start computing the gradient w.r.t to the
            // feature functions. Each feature function belongs
            // to a factor potential function, so:

            // For each clique potential (factor potential function)
            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                Parallel.For(0, function.Factors.Length, ParallelOptions,
                    c => { InnerGradient(function.Factors[c], inputs, outputs, lnZx, lnZxy, gradient); });
            }
            else
            {
                for (var c = 0; c < function.Factors.Length; c++)
                {
                    InnerGradient(function.Factors[c], inputs, outputs, lnZx, lnZxy, gradient);
                }
            }

            // Reset log-likelihoods so they are recomputed in the next run,
            // either by the Objective function or by the Gradient calculation.

            this.logLikelihoods.Value = null;
            this.error.Value = error;

            Debug.Assert(!double.IsNaN(error));

            return gradient; // return the gradient.
        }

        /// <summary>
        ///     Computes the objective (cost) function for the Hidden
        ///     Conditional Random Field (negative log-likelihood) using
        ///     the input/outputs stored in this object.
        /// </summary>
        public double Objective()
        {
            var inputs = this.inputs.Value;
            var outputs = this.outputs.Value;
            var parameters = Parameters;
            double sumSquaredWeights = 0;

            // Regularization
            if (Regularization != 0)
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!(double.IsInfinity(parameters[i]) || double.IsNaN(parameters[i])))
                    {
                        sumSquaredWeights += parameters[i] * parameters[i];
                    }
                }

                sumSquaredWeights = sumSquaredWeights / (2.0 * Regularization);
            }

            double[][] logLikelihoods;
            var logLikelihood = model.LogLikelihood(inputs, outputs, out logLikelihoods);

            this.logLikelihoods.Value = logLikelihoods;

            // Maximize the log-likelihood and minimize
            // a portion of the sum of squared weights
            var objective = logLikelihood - sumSquaredWeights;

            return -objective; // convert to a minimization problem
        }

        /// <summary>
        ///     Computes the gradient (vector of derivatives) vector for
        ///     the cost function, which may be used to guide optimization.
        /// </summary>
        /// <param name="parameters">The parameter vector lambda to use in the model.</param>
        /// <param name="input">The inputs to compute the cost function.</param>
        /// <param name="output">The respective outputs to compute the cost function.</param>
        /// <returns>The value of the gradient vector for the given parameters.</returns>
        public double[] Gradient(double[] parameters, T[] input, int output)
        {
            Inputs = new[] { input };
            Outputs = new[] { output };
            Parameters = parameters;
            return Gradient();
        }

        /// <summary>
        ///     Computes the gradient (vector of derivatives) vector for
        ///     the cost function, which may be used to guide optimization.
        /// </summary>
        /// <param name="parameters">The parameter vector lambda to use in the model.</param>
        /// <param name="inputs">The inputs to compute the cost function.</param>
        /// <param name="outputs">The respective outputs to compute the cost function.</param>
        /// <returns>The value of the gradient vector for the given parameters.</returns>
        public double[] Gradient(double[] parameters, T[][] inputs, int[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
            Parameters = parameters;
            return Gradient();
        }

        /// <summary>
        ///     Computes the gradient using the input/outputs stored in this object.
        ///     This method is <b>not</b> thread safe.
        /// </summary>
        /// <param name="parameters">The parameter vector lambda to use in the model.</param>
        /// <returns>The value of the gradient vector for the given parameters.</returns>
        public double[] Gradient(double[] parameters)
        {
            Parameters = parameters;
            return Gradient();
        }

        private void InnerGradient(FactorPotential<T> factor, T[][] inputs, int[] outputs, double[] lnZx,
            double[] lnZxy, double[] gradient)
        {
            var factorIndex = factor.Index;

            // Compute all forward and backward matrices to be
            //  used in the feature functions marginal computations.

            var lnFwds = new double[inputs.Length][,];
            var lnBwds = new double[inputs.Length][,];
            for (var i = 0; i < inputs.Length; i++)
            {
                lnFwds[i] = ForwardBackwardAlgorithm.LogForward(factor, inputs[i], factorIndex);
                lnBwds[i] = ForwardBackwardAlgorithm.LogBackward(factor, inputs[i], factorIndex);
            }

            var marginals = new double[function.Outputs];

            // For each feature in the factor potential function
            var end = factor.FactorParameters.Offset + factor.FactorParameters.Count;
            for (var k = factor.FactorParameters.Offset; k < end; k++)
            {
                var feature = function.Features[k];
                var parameter = function.Weights[k];

                if (double.IsInfinity(parameter))
                {
                    gradient[k] = 0;
                    continue;
                }


                // Compute the two marginal sums for the gradient calculation
                // as given in eq. 1.52 of Sutton, McCallum; "An introduction to
                // Conditional Random Fields for Relational Learning". The sums
                // will be computed in the log domain for numerical stability.

                var lnsum1 = double.NegativeInfinity;
                var lnsum2 = double.NegativeInfinity;

                // For each training sample (sequences)
                for (var i = 0; i < inputs.Length; i++)
                {
                    var x = inputs[i]; // training input
                    var y = outputs[i]; // training output

                    // Compute marginals for all possible outputs
                    for (var j = 0; j < marginals.Length; j++)
                    {
                        marginals[j] = double.NegativeInfinity;
                    }

                    // However, making the assumption that each factor is responsible for only 
                    // one output label, we can compute the marginal only for the current factor
                    marginals[factorIndex] = feature.LogMarginal(lnFwds[i], lnBwds[i], x, factorIndex);

                    // The first term contains a marginal probability p(w|x,y), which is
                    // exactly a marginal distribution of the clamped CRF (eq. 1.46).
                    lnsum1 = Special.LogSum(lnsum1, marginals[y] == lnZxy[i] ? 0 : marginals[y] - lnZxy[i]);

                    // The second term contains a different marginal p(w,y|x) which is the
                    // same marginal probability required in as fully-observed CRF.
                    for (var j = 0; j < marginals.Length; j++)
                    {
                        lnsum2 = Special.LogSum(lnsum2, marginals[j] - lnZx[i]);
                    }

                    Debug.Assert(!marginals.HasNaN());
                    Debug.Assert(!double.IsNaN(lnsum1));
                    Debug.Assert(!double.IsNaN(lnsum2));
                }

                // Compute the current derivative
                var sum1 = System.Math.Exp(lnsum1);
                var sum2 = System.Math.Exp(lnsum2);
                var derivative = sum1 - sum2;

                if (sum1 == sum2)
                {
                    derivative = 0;
                }

                Debug.Assert(!double.IsNaN(derivative));

                // Include regularization derivative if required
                if (Regularization != 0)
                {
                    derivative -= parameter / Regularization;
                }

                gradient[k] = -derivative;
            }
        }

        /// <summary>
        ///     Computes the objective (cost) function for the Hidden
        ///     Conditional Random Field (negative log-likelihood).
        /// </summary>
        /// <param name="parameters">The parameter vector lambda to use in the model.</param>
        /// <param name="inputs">The inputs to compute the cost function.</param>
        /// <param name="outputs">The respective outputs to compute the cost function.</param>
        /// <returns>The value of the objective function for the given parameters.</returns>
        public double Objective(double[] parameters, T[][] inputs, int[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
            return Objective(parameters);
        }

        /// <summary>
        ///     Computes the objective (cost) function for the Hidden
        ///     Conditional Random Field (negative log-likelihood) using
        ///     the input/outputs stored in this object.
        /// </summary>
        /// <param name="parameters">The parameter vector lambda to use in the model.</param>
        public double Objective(double[] parameters)
        {
            Parameters = parameters;
            return Objective();
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing,
        ///     releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before
        ///     the <see cref="ForwardBackwardGradient{T}" /> is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~ForwardBackwardGradient()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed
        ///     and unmanaged resources; <c>false</c> to release only unmanaged
        ///     resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (inputs != null)
                {
                    inputs.Dispose();
                }

                if (outputs != null)
                {
                    outputs.Dispose();
                }

                if (logLikelihoods != null)
                {
                    logLikelihoods.Dispose();
                }

                if (gradient != null)
                {
                    gradient.Dispose();
                }

                if (lnZx != null)
                {
                    lnZx.Dispose();
                }

                if (lnZxy != null)
                {
                    lnZxy.Dispose();
                }

                if (error != null)
                {
                    error.Dispose();
                }

                inputs = null;
                outputs = null;
                gradient = null;
                logLikelihoods = null;
                lnZxy = null;
                lnZx = null;
                error = null;
            }
        }

        #endregion
    }
}
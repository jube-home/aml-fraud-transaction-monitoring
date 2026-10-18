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
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Decompositions;

namespace Accord.Neuro.Learning
{
    /// <summary>
    ///     The Jacobian computation method used by the Levenberg-Marquardt.
    /// </summary>
    public enum JacobianMethod
    {
        /// <summary>
        ///     Computes the Jacobian using approximation by finite differences. This
        ///     method is slow in comparison with back-propagation and should be used
        ///     only for debugging or comparison purposes.
        /// </summary>
        ByFiniteDifferences,

        /// <summary>
        ///     Computes the Jacobian using back-propagation for the chain rule of
        ///     calculus. This is the preferred way of computing the Jacobian.
        /// </summary>
        ByBackpropagation
    }

    /// <summary>
    ///     Levenberg-Marquardt Learning Algorithm with optional Bayesian Regularization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements the Levenberg-Marquardt learning algorithm,
    ///         which treats the neural network learning as a function optimization
    ///         problem. The Levenberg-Marquardt is one of the fastest and accurate
    ///         learning algorithms for small to medium sized networks.
    ///     </para>
    ///     <para>
    ///         However, in general, the standard LM algorithm does not perform as well
    ///         on pattern recognition problems as it does on function approximation problems.
    ///         The LM algorithm is designed for least squares problems that are approximately
    ///         linear. Because the output neurons in pattern recognition problems are generally
    ///         saturated, it will not be operating in the linear region.
    ///     </para>
    ///     <para>
    ///         The advantages of the LM algorithm decreases as the number of network
    ///         parameters increases.
    ///     </para>
    ///     <example>
    ///         <para>Sample usage (training network to calculate XOR function):</para>
    ///         <code>
    ///   // initialize input and output values
    ///   double[][] input =
    ///   {
    ///       new double[] {0, 0}, new double[] {0, 1},
    ///       new double[] {1, 0}, new double[] {1, 1}
    ///   };
    /// 
    ///   double[][] output = 
    ///   {
    ///       new double[] {0}, new double[] {1},
    ///       new double[] {1}, new double[] {0}
    ///   };
    ///   
    ///   // create neural network
    ///   ActivationNetwork   network = new ActivationNetwork(
    ///       SigmoidFunction( 2 ),
    ///       2, // two inputs in the network
    ///       2, // two neurons in the first layer
    ///       1 ); // one neuron in the second layer
    ///     
    ///   // create teacher
    ///   LevenbergMarquardtLearning teacher = new LevenbergMarquardtLearning( network );
    ///   
    ///   // loop
    ///   while ( !needToStop )
    ///   {
    ///       // run epoch of learning procedure
    ///       double error = teacher.RunEpoch( input, output );
    ///       
    ///       // check error value to see if we need to stop
    ///       // ...
    ///   }
    /// </code>
    ///         <para>
    ///             The following example shows how to create a neural network to learn a classification
    ///             problem with multiple classes.
    ///         </para>
    ///         <code>
    /// // Here we will be creating a neural network to process 3-valued input
    /// // vectors and classify them into 4-possible classes. We will be using
    /// // a single hidden layer with 5 hidden neurons to accomplish this task.
    /// //
    /// int numberOfInputs = 3;
    /// int numberOfClasses = 4;
    /// int hiddenNeurons = 5;
    /// 
    /// // Those are the input vectors and their expected class labels
    /// // that we expect our network to learn.
    /// //
    /// double[][] input = 
    /// {
    ///     new double[] { -1, -1, -1 }, // 0
    ///     new double[] { -1,  1, -1 }, // 1
    ///     new double[] {  1, -1, -1 }, // 1
    ///     new double[] {  1,  1, -1 }, // 0
    ///     new double[] { -1, -1,  1 }, // 2
    ///     new double[] { -1,  1,  1 }, // 3
    ///     new double[] {  1, -1,  1 }, // 3
    ///     new double[] {  1,  1,  1 }  // 2
    ///  };
    /// 
    ///  int[] labels =
    ///  {
    ///     0,
    ///     1,
    ///     1,
    ///     0,
    ///     2,
    ///     3,
    ///     3,
    ///     2,
    /// };
    /// 
    /// // In order to perform multi-class classification, we have to select a 
    /// // decision strategy in order to be able to interpret neural network 
    /// // outputs as labels. For this, we will be expanding our 4 possible class
    /// // labels into 4-dimensional output vectors where one single dimension 
    /// // corresponding to a label will contain the value +1 and -1 otherwise.
    /// 
    /// double[][] outputs = Accord.Statistics.Tools
    ///   .Expand(labels, numberOfClasses, -1, 1);
    /// 
    /// // Next we can proceed to create our network
    /// var function = new BipolarSigmoidFunction(2);
    /// var network = new ActivationNetwork(function,
    ///   numberOfInputs, hiddenNeurons, numberOfClasses);
    /// 
    /// // Heuristically randomize the network
    /// new NguyenWidrow(network).Randomize();
    /// 
    /// // Create the learning algorithm
    /// var teacher = new LevenbergMarquardtLearning(network);
    /// 
    /// // Teach the network for 10 iterations:
    /// double error = Double.PositiveInfinity;
    /// for (int i = 0; i &lt; 10; i++)
    ///    error = teacher.RunEpoch(input, outputs);
    /// 
    /// // At this point, the network should be able to 
    /// // perfectly classify the training input points.
    /// 
    /// for (int i = 0; i &lt; input.Length; i++)
    /// {
    ///    int answer;
    ///    double[] output = network.Compute(input[i]);
    ///    double response = output.Max(out answer);
    /// 
    ///    int expected = labels[i];
    ///   
    ///    // at this point, the variables 'answer' and
    ///    // 'expected' should contain the same value.
    /// }
    /// </code>
    ///     </example>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.cs.nyu.edu/~roweis/notes/lm.pdf">
    ///                         Sam Roweis. Levenberg-Marquardt Optimization.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www-alg.ist.hokudai.ac.jp/~jan/alpha.pdf">
    ///                         Jan Poland. (2001). On the Robustness of Update Strategies for the Bayesian
    ///                         Hyperparameter alpha. Available on: http://www-alg.ist.hokudai.ac.jp/~jan/alpha.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://cs.olemiss.edu/~ychen/publications/conference/chen_ijcnn99.pdf">
    ///                         B. Wilamowski, Y. Chen. (1999). Efficient Algorithm for Training Neural Networks
    ///                         with one Hidden Layer. Available on:
    ///                         http://cs.olemiss.edu/~ychen/publications/conference/chen_ijcnn99.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.inference.phy.cam.ac.uk/mackay/Bayes_FAQ.html">
    ///                         David MacKay. (2004). Bayesian methods for neural networks - FAQ. Available on:
    ///                         http://www.inference.phy.cam.ac.uk/mackay/Bayes_FAQ.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public class LevenbergMarquardtLearning : ISupervisedLearning
    {
        private const double lambdaMax = 1e25;

        private readonly float[] diagonal;

        // Levenberg-Marquardt variables

        private readonly JacobianMethod method;

        // network to teach
        private readonly ActivationNetwork network;

        // Total of weights in the network
        private readonly int outputCount;
        private readonly float[] weights;

        private float[] deltas;
        private double[] errors;

        // Bayesian Regularization Hyperparameters

        // Levenberg damping factor
        private double lambda = 0.1;

        // Bayesian Regularization variables

        // The amount the damping factor is adjusted
        // when searching the minimum error surface

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardtLearning" /> class.
        /// </summary>
        /// <param name="network">Network to teach.</param>
        public LevenbergMarquardtLearning(ActivationNetwork network) :
            this(network, false, JacobianMethod.ByBackpropagation)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardtLearning" /> class.
        /// </summary>
        /// <param name="network">Network to teach.</param>
        /// <param name="useRegularization">True to use Bayesian regularization, false otherwise.</param>
        public LevenbergMarquardtLearning(ActivationNetwork network, bool useRegularization) :
            this(network, useRegularization, JacobianMethod.ByBackpropagation)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardtLearning" /> class.
        /// </summary>
        /// <param name="network">Network to teach.</param>
        /// <param name="method">The method by which the Jacobian matrix will be calculated.</param>
        public LevenbergMarquardtLearning(ActivationNetwork network, JacobianMethod method)
            : this(network, false, method)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardtLearning" /> class.
        /// </summary>
        /// <param name="network">Network to teach.</param>
        /// <param name="useRegularization">True to use Bayesian regularization, false otherwise.</param>
        /// <param name="method">The method by which the Jacobian matrix will be calculated.</param>
        public LevenbergMarquardtLearning(ActivationNetwork network, bool useRegularization, JacobianMethod method)
        {
            ParallelOptions = new ParallelOptions();
            this.network = network;
            NumberOfParameters = getNumberOfParameters(network);
            outputCount = network.Layers[network.Layers.Length - 1].Neurons.Length;

            UseRegularization = useRegularization;
            this.method = method;

            weights = new float[NumberOfParameters];
            Hessian = new float[NumberOfParameters][];
            for (var i = 0; i < Hessian.Length; i++)
            {
                Hessian[i] = new float[NumberOfParameters];
            }

            diagonal = new float[NumberOfParameters];
            Gradient = new float[NumberOfParameters];
            Jacobian = new float[NumberOfParameters][];


            // Will use Backpropagation method for Jacobian computation
            if (method == JacobianMethod.ByBackpropagation)
            {
                // create weight derivatives arrays
                weightDerivatives = new float[network.Layers.Length][][];
                thresholdsDerivatives = new float[network.Layers.Length][];

                // initialize arrays
                for (var i = 0; i < network.Layers.Length; i++)
                {
                    var layer = (ActivationLayer)network.Layers[i];

                    weightDerivatives[i] = new float[layer.Neurons.Length][];
                    thresholdsDerivatives[i] = new float[layer.Neurons.Length];

                    for (var j = 0; j < layer.Neurons.Length; j++)
                    {
                        weightDerivatives[i][j] = new float[layer.InputsCount];
                    }
                }
            }
            else // Will use finite difference method for Jacobian computation
            {
                // create differential coefficient arrays
                differentialCoefficients = createCoefficients(3);
                derivativeStepSize = new double[NumberOfParameters];

                // initialize arrays
                for (var i = 0; i < NumberOfParameters; i++)
                {
                    derivativeStepSize[i] = derivativeStep;
                }
            }
        }

        /// <summary>
        ///     Levenberg's damping factor (lambda). This
        ///     value  must be positive. Default is 0.1.
        /// </summary>
        /// <remarks>
        ///     The value determines speed of learning. Default value is <b>0.1</b>.
        /// </remarks>
        public double LearningRate
        {
            get => lambda;
            set
            {
                if (lambda <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be positive.");
                }

                lambda = value;
            }
        }

        /// <summary>
        ///     Learning rate adjustment. Default value is 10.
        /// </summary>
        /// <remarks>
        ///     The value by which the learning rate is adjusted when searching
        ///     for the minimum cost surface. Default value is 10.
        /// </remarks>
        public double Adjustment { get; set; } = 10.0;

        /// <summary>
        ///     Gets the total number of parameters
        ///     in the network being trained.
        /// </summary>
        public int NumberOfParameters { get; }

        /// <summary>
        ///     Gets the number of effective parameters being used
        ///     by the network as determined by the Bayesian regularization.
        /// </summary>
        /// <remarks>
        ///     If no regularization is being used, the value will be 0.
        /// </remarks>
        public double EffectiveParameters { get; private set; }

        /// <summary>
        ///     Gets or sets the importance of the squared sum of network
        ///     weights in the cost function. Used by the regularization.
        /// </summary>
        /// <remarks>
        ///     This is the first Bayesian hyperparameter. The default
        ///     value is 0.
        /// </remarks>
        public double Alpha { get; set; }

        /// <summary>
        ///     Gets or sets the importance of the squared sum of network
        ///     errors in the cost function. Used by the regularization.
        /// </summary>
        /// <remarks>
        ///     This is the second Bayesian hyperparameter. The default
        ///     value is 1.
        /// </remarks>
        public double Beta { get; set; } = 1;

        /// <summary>
        ///     Gets or sets whether to use Bayesian Regularization.
        /// </summary>
        public bool UseRegularization { get; set; }

        /// <summary>
        ///     Gets or sets the number of blocks to divide the
        ///     Jacobian matrix in the Hessian calculation to
        ///     preserve memory. Default is 1.
        /// </summary>
        public int Blocks { get; set; } = 1;

        /// <summary>
        ///     Gets or sets a small epsilon value to be added to the
        ///     diagonal of the Hessian matrix. Default is 1e-12.
        /// </summary>
        public double Epsilon { get; set; } = 1e-12;

        /// <summary>
        ///     Gets the approximate Hessian matrix of second derivatives
        ///     generated in the last algorithm iteration. The Hessian is
        ///     stored in the upper triangular part of this matrix. See
        ///     remarks for details.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Hessian needs only be upper-triangular, since
        ///         it is symmetric. The Cholesky decomposition will
        ///         make use of this fact and use the lower-triangular
        ///         portion to hold the decomposition, conserving memory
        ///     </para>
        ///     <para>
        ///         Thus said, this property will hold the Hessian matrix
        ///         in the upper-triangular part of this matrix, and store
        ///         its Cholesky decomposition on its lower triangular part.
        ///     </para>
        /// </remarks>
        public float[][] Hessian { get; }

        /// <summary>
        ///     Gets the Jacobian matrix created in the last iteration.
        /// </summary>
        public float[][] Jacobian { get; }

        /// <summary>
        ///     Gets the gradient vector computed in the last iteration.
        /// </summary>
        public float[] Gradient { get; }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions { get; set; }

        /// <summary>
        ///     This method should not be called. Use <see cref="RunEpoch" /> instead.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <param name="output">Array of output vectors.</param>
        /// <returns>Nothing.</returns>
        /// <remarks>
        ///     <para>
        ///         Online learning mode is not supported by the
        ///         Levenberg Marquardt. Use batch learning mode instead.
        ///     </para>
        /// </remarks>
        public double Run(double[] input, double[] output)
        {
            throw new InvalidOperationException("Learning can only be done in batch mode.");
        }

        /// <summary>
        ///     Runs a single learning epoch.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <param name="output">Array of output vectors.</param>
        /// <returns>Returns summary learning error for the epoch.</returns>
        /// <remarks>
        ///     <para>
        ///         The method runs one learning epoch, by calling running necessary
        ///         iterations of the Levenberg Marquardt to achieve an error decrease.
        ///     </para>
        /// </remarks>
        public double RunEpoch(double[][] input, double[][] output)
        {
            // Initial definitions and memory allocations
            var N = input.Length;

            JaggedCholeskyDecompositionF decomposition = null;
            var sumOfSquaredErrors = 0.0;
            double sumOfSquaredWeights;
            double trace;

            // Set upper triangular Hessian to zero
            for (var i = 0; i < Hessian.Length; i++)
            {
                Array.Clear(Hessian[i], i, Hessian.Length - i);
            }

            // Set Gradient vector to zero
            Array.Clear(Gradient, 0, Gradient.Length);


            // Divide the problem into blocks. Instead of computing
            // a single Jacobian and a single error vector, we will
            // be computing multiple Jacobians for smaller problems
            // and then sum all blocks into the final Hessian matrix
            // and gradient vector.

            var blockSize = input.Length / Blocks;
            var finalBlock = input.Length % Blocks;
            var jacobianSize = blockSize * outputCount;

            // Re-allocate the partial Jacobian matrix only if needed
            if (Jacobian[0] == null || Jacobian[0].Length < jacobianSize)
            {
                for (var i = 0; i < Jacobian.Length; i++)
                {
                    Jacobian[i] = new float[jacobianSize];
                }
            }

            // Re-allocate error vector only if needed
            if (errors == null || errors.Length < jacobianSize)
            {
                errors = new double[jacobianSize];
            }


            // For each block
            for (var s = 0; s <= Blocks; s++)
            {
                if (s == Blocks && finalBlock == 0)
                {
                    continue;
                }

                var B = s == Blocks ? finalBlock : blockSize;
                var block = Vector.Range(s * blockSize, s * blockSize + B);

                var inputBlock = input.Get(block);
                var outputBlock = output.Get(block);


                // Compute the partial Jacobian matrix
                if (method == JacobianMethod.ByBackpropagation)
                {
                    sumOfSquaredErrors = JacobianByChainRule(inputBlock, outputBlock);
                }
                else
                {
                    sumOfSquaredErrors = JacobianByFiniteDifference(inputBlock, outputBlock);
                }

                if (double.IsNaN(sumOfSquaredErrors))
                {
                    throw new ArithmeticException("Jacobian calculation has produced a non-finite number.");
                }


                // Compute error gradient using Jacobian
                for (var i = 0; i < Jacobian.Length; i++)
                {
                    double gsum = 0;
                    for (var j = 0; j < jacobianSize; j++)
                    {
                        gsum += Jacobian[i][j] * errors[j];
                    }

                    Gradient[i] += (float)gsum;
                }


                // Compute Quasi-Hessian Matrix approximation
                //  using the outer product Jacobian (H ~ J'J)
                Parallel.For(0, Jacobian.Length, ParallelOptions, i =>
                {
                    var ji = Jacobian[i];
                    var hi = Hessian[i];

                    for (var j = i; j < hi.Length; j++)
                    {
                        var jj = Jacobian[j];

                        double hsum = 0;
                        for (var k = 0; k < jacobianSize; k++)
                        {
                            hsum += ji[k] * jj[k];
                        }

                        // The Hessian need only be upper-triangular, since
                        // it is symmetric. The Cholesky decomposition will
                        // make use of this fact and use the lower-triangular
                        // portion to hold the decomposition, conserving memory.
                        hi[j] += (float)(2 * Beta * hsum);
                    }
                });
            }


            // Store the Hessian's diagonal for future computations. The
            // diagonal will be destroyed in the decomposition, so it can
            // still be updated on every iteration by restoring this copy.
            for (var i = 0; i < Hessian.Length; i++)
            {
                diagonal[i] = Hessian[i][i] + (float)Epsilon;
            }

            // Create the initial weights vector
            sumOfSquaredWeights = saveNetworkToArray();


            // Define the objective function: (Bayesian regularization objective)
            var objective = Beta * sumOfSquaredErrors + Alpha * sumOfSquaredWeights;
            var current = objective + 1.0; // (starting value to enter iteration)


            // Begin of the main Levenberg-Marquardt method
            lambda /= Adjustment;

            // We'll try to find a direction with less error
            //  (or where the objective function is smaller)
            while (current >= objective && lambda < lambdaMax)
            {
                lambda *= Adjustment;

                // Update diagonal (Levenberg-Marquardt)
                for (var i = 0; i < diagonal.Length; i++)
                {
                    Hessian[i][i] = (float)(diagonal[i] * (1 + lambda) + 2 * Alpha);
                }

                // Decompose to solve the linear system. The Cholesky decomposition
                // is done in place, occupying the Hessian's lower-triangular part.
                decomposition = new JaggedCholeskyDecompositionF(Hessian, true, true);

                // Check if the decomposition exists
                if (decomposition.IsUndefined)
                {
                    // The Hessian is singular. Continue to the next
                    // iteration until the diagonal update transforms
                    // it back to non-singular.
                    continue;
                }


                // Solve using Cholesky decomposition
                deltas = decomposition.Solve(Gradient);

                // Update weights using the calculated deltas
                sumOfSquaredWeights = loadArrayIntoNetwork();

                // Calculate the new error
                sumOfSquaredErrors = ComputeError(input, output);

                // Update the objective function
                current = Beta * sumOfSquaredErrors + Alpha * sumOfSquaredWeights;

                // If the object function is bigger than before, the method
                //  is tried again using a greater damping factor.
            }

            // If this iteration caused a error drop, then next
            // iteration will use a smaller damping factor.
            lambda /= Adjustment;

            if (lambda < 1e-300)
            {
                lambda = 1e-300;
            }


            // If we are using Bayesian regularization, we need to
            //   update the Bayesian hyperparameters alpha and beta
            if (UseRegularization)
            {
                // References: 
                // - http://www-alg.ist.hokudai.ac.jp/~jan/alpha.pdf
                // - http://www.inference.phy.cam.ac.uk/mackay/Bayes_FAQ.html

                // Compute the trace for the inverse Hessian in place. The
                // Hessian which was still being hold together with the L
                // factorization will be destroyed after this computation.
                trace = decomposition.InverseTrace(true);


                // Poland update's formula:
                EffectiveParameters = NumberOfParameters - Alpha * trace;
                Alpha = NumberOfParameters / (2 * sumOfSquaredWeights + trace);
                Beta = System.Math.Abs((N - EffectiveParameters) / (2 * sumOfSquaredErrors));
                //beta = (N - gamma) / (2.0 * sumOfSquaredErrors);

                // Original MacKay's update formula:
                //  gamma = (double)networkParameters - (alpha * trace);
                //  alpha = gamma / (2.0 * sumOfSquaredWeights);
                //  beta = (gamma - N) / (2.0 * sumOfSquaredErrors);
            }

            return sumOfSquaredErrors;
        }

        /// <summary>
        ///     Compute network error for a given data set.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The output points.</param>
        /// <returns>The sum of squared errors for the data.</returns>
        public double ComputeError(double[][] input, double[][] output)
        {
            double sumOfSquaredErrors = 0;

#if NET35
            for (int i = 0; i < input.Length; i++)
            {
                // Compute network answer
                double[] y = network.Compute(input[i]);

                for (int j = 0; j < y.Length; j++)
                {
                    double e = (y[j] - output[i][j]);
                    sumOfSquaredErrors += e * e;
                }
            }
#else
            var lockSum = new object();

            Parallel.For(0, input.Length,

                // Initialize
                () => 0.0,

                // Map
                (i, loopState, partialSum) =>
                {
                    // Compute network answer
                    var y = network.Compute(input[i]);

                    for (var j = 0; j < y.Length; j++)
                    {
                        var e = y[j] - output[i][j];
                        partialSum += e * e;
                    }

                    return partialSum;
                },

                // Reduce
                partialSum =>
                {
                    lock (lockSum)
                    {
                        sumOfSquaredErrors += partialSum;
                    }
                }
            );
#endif

            return sumOfSquaredErrors / 2.0;
        }

        /// <summary>
        ///     Update network's weights.
        /// </summary>
        /// <returns>The sum of squared weights divided by 2.</returns>
        private double loadArrayIntoNetwork()
        {
            double w, sumOfSquaredWeights = 0.0;

            // For each layer in the network
            for (int li = 0, cur = 0; li < network.Layers.Length; li++)
            {
                var layer = network.Layers[li] as ActivationLayer;

                // for each neuron in the layer
                for (var ni = 0; ni < layer.Neurons.Length; ni++, cur++)
                {
                    var neuron = layer.Neurons[ni] as ActivationNeuron;

                    // for each weight in the neuron
                    for (var wi = 0; wi < neuron.Weights.Length; wi++, cur++)
                    {
                        neuron.Weights[wi] = w = weights[cur] + deltas[cur];
                        sumOfSquaredWeights += w * w;
                    }

                    // for each threshold value (bias):
                    neuron.Threshold = w = weights[cur] + deltas[cur];
                    sumOfSquaredWeights += w * w;
                }
            }

            return sumOfSquaredWeights / 2.0;
        }

        /// <summary>
        ///     Creates the initial weight vector w
        /// </summary>
        /// <returns>The sum of squared weights divided by 2.</returns>
        private double saveNetworkToArray()
        {
            double w, sumOfSquaredWeights = 0.0;

            // for each layer in the network
            for (int li = 0, cur = 0; li < network.Layers.Length; li++)
            {
                var layer = network.Layers[li] as ActivationLayer;

                // for each neuron in the layer
                for (var ni = 0; ni < network.Layers[li].Neurons.Length; ni++, cur++)
                {
                    var neuron = layer.Neurons[ni] as ActivationNeuron;

                    // for each weight in the neuron
                    for (var wi = 0; wi < neuron.InputsCount; wi++, cur++)
                    {
                        // We copy it to the starting weights vector
                        w = weights[cur] = (float)neuron.Weights[wi];
                        sumOfSquaredWeights += w * w;
                    }

                    // and also for the threshold value (bias):
                    w = weights[cur] = (float)neuron.Threshold;
                    sumOfSquaredWeights += w * w;
                }
            }

            return sumOfSquaredWeights / 2.0;
        }

        /// <summary>
        ///     Gets the number of parameters in a network.
        /// </summary>
        private static int getNumberOfParameters(ActivationNetwork network)
        {
            var sum = 0;

            for (var i = 0; i < network.Layers.Length; i++)
            {
                for (var j = 0; j < network.Layers[i].Neurons.Length; j++)
                {
                    // number of weights plus the bias value
                    sum += network.Layers[i].Neurons[j].InputsCount + 1;
                }
            }

            return sum;
        }

        #region Jacobian Calculation By Chain Rule

        private readonly float[][][] weightDerivatives;
        private readonly float[][] thresholdsDerivatives;

        /// <summary>
        ///     Calculates the Jacobian matrix by using the chain rule.
        /// </summary>
        /// <param name="input">The input vectors.</param>
        /// <param name="output">The desired output vectors.</param>
        /// <returns>The sum of squared errors for the last error divided by 2.</returns>
        private double JacobianByChainRule(double[][] input, double[][] output)
        {
            double e, sumOfSquaredErrors = 0.0;

            // foreach input training sample
            for (int i = 0, row = 0; i < input.Length; i++)
            {
                // Compute a forward pass
                network.Compute(input[i]);

                // for each output of for the input sample
                for (var j = 0; j < output[i].Length; j++, row++)
                {
                    // Calculate the derivatives for the input/output
                    //  pair by computing a backpropagation chain pass
                    e = errors[row] = CalculateDerivatives(input[i], output[i], j);
                    sumOfSquaredErrors += e * e;

                    // Create the Jacobian matrix row: for each layer in the network
                    for (int li = 0, col = 0; li < weightDerivatives.Length; li++)
                    {
                        var layerDerivatives = weightDerivatives[li];
                        var layerThresoldDerivatives = thresholdsDerivatives[li];

                        // for each neuron in the layer
                        for (var ni = 0; ni < layerDerivatives.Length; ni++, col++)
                        {
                            var neuronWeightDerivatives = layerDerivatives[ni];

                            // for each weight of the neuron, copy the derivative
                            for (var wi = 0; wi < neuronWeightDerivatives.Length; wi++, col++)
                            {
                                Jacobian[col][row] = neuronWeightDerivatives[wi];
                            }

                            // also copy the neuron threshold
                            Jacobian[col][row] = layerThresoldDerivatives[ni];
                        }
                    }
                }
            }

            return sumOfSquaredErrors / 2.0;
        }

        /// <summary>
        ///     Calculates partial derivatives for all weights of the network.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="desiredOutput">Desired output vector.</param>
        /// <param name="outputIndex">The current output location (index) in the desired output vector.</param>
        /// <returns>Returns summary squared error of the last layer.</returns>
        private double CalculateDerivatives(double[] input, double[] desiredOutput, int outputIndex)
        {
            // Start by the output layer first
            var outputLayerIndex = network.Layers.Length - 1;
            var outputLayer = network.Layers[outputLayerIndex] as ActivationLayer;
            double[] previousLayerOutput;

            // If we have only one single layer, the previous layer outputs is given by the input layer
            previousLayerOutput = outputLayerIndex == 0 ? input : network.Layers[outputLayerIndex - 1].Output;

            // Clear derivatives for other output's neurons
            for (var i = 0; i < thresholdsDerivatives[outputLayerIndex].Length; i++)
            {
                thresholdsDerivatives[outputLayerIndex][i] = 0;
            }

            for (var i = 0; i < weightDerivatives[outputLayerIndex].Length; i++)
            {
                for (var j = 0; j < weightDerivatives[outputLayerIndex][i].Length; j++)
                {
                    weightDerivatives[outputLayerIndex][i][j] = 0;
                }
            }

            // Retrieve current desired output neuron
            var outputNeuron = outputLayer.Neurons[outputIndex] as ActivationNeuron;
            var neuronWeightDerivatives = weightDerivatives[outputLayerIndex][outputIndex];

            var output = outputNeuron.Output;
            var error = desiredOutput[outputIndex] - output;
            var derivative = outputNeuron.ActivationFunction.Derivative2(output);

            // Set derivative for each weight in the neuron
            for (var i = 0; i < neuronWeightDerivatives.Length; i++)
            {
                neuronWeightDerivatives[i] = (float)(derivative * previousLayerOutput[i]);
            }

            // Set derivative for the current threshold (bias) term
            thresholdsDerivatives[outputLayerIndex][outputIndex] = (float)derivative;


            // Now, proceed to the next hidden layers
            for (var li = network.Layers.Length - 2; li >= 0; li--)
            {
                var nextLayerIndex = li + 1;

                var layer = network.Layers[li] as ActivationLayer;
                var nextLayer = network.Layers[nextLayerIndex] as ActivationLayer;

                // If we are in the first layer, the previous layer is just the input layer
                previousLayerOutput = li == 0 ? input : network.Layers[li - 1].Output;

                // Now, we will compute the derivatives for the current layer applying the chain
                //  rule. To apply the chain-rule, we will make use of the previous derivatives
                //  computed for the inner layers (forming a calculation chain, hence the name).

                // So, for each neuron in the current layer:
                for (var ni = 0; ni < layer.Neurons.Length; ni++)
                {
                    var neuron = layer.Neurons[ni] as ActivationNeuron;

                    neuronWeightDerivatives = weightDerivatives[li][ni];

                    var layerDerivatives = thresholdsDerivatives[li];
                    var nextLayerDerivatives = thresholdsDerivatives[li + 1];

                    double sum = 0;

                    // The chain-rule can be stated as (f(w*g(x))' = f'(w*g(x)) * w*g'(x)
                    //
                    // We will start computing the second part of the product. Since the g' 
                    //  derivatives have already been computed in the previous computation,
                    //  we will be summing all previous function derivatives and weighting
                    //  them using their connection weight (synapses).
                    //
                    // So, for each neuron in the next layer:
                    for (var nj = 0; nj < nextLayerDerivatives.Length; nj++)
                    {
                        // retrieve the weight connecting the output of the current
                        //   neuron and the activation function of the next neuron.
                        var weight = nextLayer.Neurons[nj].Weights[ni];

                        // accumulate the synapse weight * next layer derivative
                        sum += weight * nextLayerDerivatives[nj];
                    }

                    // Continue forming the chain-rule statement
                    derivative = sum * neuron.ActivationFunction.Derivative2(neuron.Output);

                    // Set derivative for each weight in the neuron
                    for (var wi = 0; wi < neuronWeightDerivatives.Length; wi++)
                    {
                        neuronWeightDerivatives[wi] = (float)(derivative * previousLayerOutput[wi]);
                    }

                    // Set derivative for the current threshold
                    layerDerivatives[ni] = (float)derivative;

                    // The threshold derivatives also gather the derivatives for
                    // the layer, and thus can be re-used in next calculations.
                }
            }

            // return error
            return error;
        }

        #endregion

        #region Jacobian Calculation by Finite Differences

        // References:
        // - Trent F. Guidry, http://www.trentfguidry.net/

        private readonly double[] derivativeStepSize;
        private const double derivativeStep = 1e-2;
        private readonly double[][,] differentialCoefficients;

        /// <summary>
        ///     Calculates the Jacobian Matrix using Finite Differences
        /// </summary>
        /// <returns>Returns the sum of squared errors of the network divided by 2.</returns>
        private double JacobianByFiniteDifference(double[][] input, double[][] desiredOutput)
        {
            double e, sumOfSquaredErrors = 0;

            // for each input training sample
            for (int i = 0, row = 0; i < input.Length; i++)
            {
                // Compute a forward pass
                var networkOutput = network.Compute(input[i]);

                // for each output respective to the input
                for (var j = 0; j < networkOutput.Length; j++, row++)
                {
                    // Calculate network error to build the residuals vector
                    e = errors[row] = desiredOutput[i][j] - networkOutput[j];
                    sumOfSquaredErrors += e * e;

                    // Computation of one of the Jacobian Matrix rows by numerical differentiation:
                    // for each weight w_j in the network, we have to compute its partial derivative
                    // to build the Jacobian matrix.

                    // So, for each layer:
                    for (int li = 0, col = 0; li < network.Layers.Length; li++)
                    {
                        var layer = network.Layers[li] as ActivationLayer;

                        // for each neuron:
                        for (var ni = 0; ni < layer.Neurons.Length; ni++, col++)
                        {
                            var neuron = layer.Neurons[ni] as ActivationNeuron;

                            // for each weight:
                            for (var wi = 0; wi < neuron.InputsCount; wi++, col++)
                            {
                                // Compute its partial derivative
                                Jacobian[col][row] = (float)ComputeDerivative(input[i], li, ni,
                                    wi, out derivativeStepSize[col], networkOutput[j], j);
                            }

                            // and also for each threshold value (bias)
                            Jacobian[col][row] = (float)ComputeDerivative(input[i], li, ni,
                                -1, out derivativeStepSize[col], networkOutput[j], j);
                        }
                    }
                }
            }

            // returns the sum of squared errors / 2
            return sumOfSquaredErrors / 2.0;
        }

        /// <summary>
        ///     Creates the coefficients to be used when calculating
        ///     the approximate Jacobian by using finite differences.
        /// </summary>
        private static double[][,] createCoefficients(int points)
        {
            var coefficients = new double[points][,];

            for (var i = 0; i < coefficients.Length; i++)
            {
                var delts = new double[points, points];

                for (var j = 0; j < points; j++)
                {
                    double delt = j - i;
                    var hterm = 1.0;

                    for (var k = 0; k < points; k++)
                    {
                        delts[j, k] = hterm / Special.Factorial(k);
                        hterm *= delt;
                    }
                }

                coefficients[i] = delts.Inverse();
                var fac = Special.Factorial(points);

                for (var j = 0; j < points; j++)
                {
                    for (var k = 0; k < points; k++)
                    {
                        coefficients[i][j, k] =
                            System.Math.Round(coefficients[i][j, k] * fac, MidpointRounding.AwayFromZero) / fac;
                    }
                }
            }

            return coefficients;
        }

        /// <summary>
        ///     Computes the derivative of the network in
        ///     respect to the weight passed as parameter.
        /// </summary>
        private double ComputeDerivative(double[] inputs,
            int layer, int neuron, int weight,
            out double stepSize, double networkOutput, int outputIndex)
        {
            var numPoints = differentialCoefficients.Length;
            double originalValue;

            // Saves a copy of the original value in the neuron
            if (weight >= 0)
            {
                originalValue = network.Layers[layer].Neurons[neuron].Weights[weight];
            }
            else
            {
                originalValue = (network.Layers[layer].Neurons[neuron] as ActivationNeuron).Threshold;
            }

            var points = new double[numPoints];

            if (originalValue != 0.0)
            {
                stepSize = derivativeStep * System.Math.Abs(originalValue);
            }
            else
            {
                stepSize = derivativeStep;
            }

            var centerPoint = (numPoints - 1) / 2;

            for (var i = 0; i < numPoints; i++)
            {
                if (i != centerPoint)
                {
                    var newValue = originalValue + (i - centerPoint) * stepSize;

                    if (weight >= 0)
                    {
                        network.Layers[layer].Neurons[neuron].Weights[weight] = newValue;
                    }
                    else
                    {
                        (network.Layers[layer].Neurons[neuron] as ActivationNeuron).Threshold = newValue;
                    }

                    points[i] = network.Compute(inputs)[outputIndex];
                }
                else
                {
                    points[i] = networkOutput;
                }
            }

            var ret = 0.0;
            for (var i = 0; i < differentialCoefficients.Length; i++)
            {
                ret += differentialCoefficients[centerPoint][1, i] * points[i];
            }

            ret /= System.Math.Pow(stepSize, 1);


            // Changes back the modified value
            if (weight >= 0)
            {
                network.Layers[layer].Neurons[neuron].Weights[weight] = originalValue;
            }
            else
            {
                (network.Layers[layer].Neurons[neuron] as ActivationNeuron).Threshold = originalValue;
            }

            return ret;
        }

        #endregion
    }
}
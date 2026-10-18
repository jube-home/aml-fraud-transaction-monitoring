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
using Accord.Math;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Probabilistic Output Calibration for Linear machines.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Instead of producing probabilistic outputs, Support Vector Machines
    ///         express their decisions in the form of a distance from support vectors in
    ///         feature space. In order to convert the SVM outputs into probabilities,
    ///         Platt (1999) proposed the calibration of the SVM outputs using a sigmoid
    ///         (Logit) link function. Later, Lin et al (2007) provided a corrected and
    ///         improved version of Platt's probabilistic outputs. This class implements
    ///         the later.
    ///     </para>
    ///     <para>
    ///         This class is not an actual learning algorithm, but a calibrator.
    ///         Machines passed as input to this algorithm should already have been trained
    ///         by a proper learning algorithm such as
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization (SMO)
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         This class can also be used in combination with <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         or <see cref="MultilabelSupportVectorLearning{TKernel}" /> to learn
    ///         <see cref="MulticlassSupportVectorMachine{TKernel}" />s
    ///         using the <c>one-vs-one</c> or <c>one-vs-all</c> multi-class decision strategies, respectively.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     John C. Platt. 1999. Probabilistic Outputs for Support Vector Machines and Comparisons to
    ///                     Regularized Likelihood Methods. In ADVANCES IN LARGE MARGIN CLASSIFIERS (1999), pp. 61-74.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Hsuan-Tien Lin, Chih-Jen Lin, and Ruby C. Weng. 2007. A note on Platt's probabilistic outputs
    ///                     for support vector machines. Mach. Learn. 68, 3 (October 2007), 267-276.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to calibrate a SVM that has
    ///         been trained to perform a simple XOR function.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\ProbabilisticOutputCalibrationTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO and calibrated using Platt's scaling.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    public class ProbabilisticOutputCalibration
        : ProbabilisticOutputCalibrationBase<SupportVectorMachine<IKernel<double[]>, double[]>, IKernel<double[]>,
                double[]>,
            ISupportVectorMachineLearning
    {
        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        /// <param name="machine">The support vector machine to be calibrated.</param>
        public ProbabilisticOutputCalibration(SupportVectorMachine<IKernel<double[]>, double[]> machine)
            : base(machine)
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public ProbabilisticOutputCalibration(ISupportVectorMachine<double[]> model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        public ProbabilisticOutputCalibration()
        {
        }
    }

    /// <summary>
    ///     Probabilistic Output Calibration for Kernel machines.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Instead of producing probabilistic outputs, Support Vector Machines
    ///         express their decisions in the form of a distance from support vectors in
    ///         feature space. In order to convert the SVM outputs into probabilities,
    ///         Platt (1999) proposed the calibration of the SVM outputs using a sigmoid
    ///         (Logit) link function. Later, Lin et al (2007) provided a corrected and
    ///         improved version of Platt's probabilistic outputs. This class implements
    ///         the later.
    ///     </para>
    ///     <para>
    ///         This class is not an actual learning algorithm, but a calibrator.
    ///         Machines passed as input to this algorithm should already have been trained
    ///         by a proper learning algorithm such as
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization (SMO)
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         This class can also be used in combination with <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         or <see cref="MultilabelSupportVectorLearning{TKernel}" /> to learn
    ///         <see cref="MulticlassSupportVectorMachine{TKernel}" />s
    ///         using the <c>one-vs-one</c> or <c>one-vs-all</c> multi-class decision strategies, respectively.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     John C. Platt. 1999. Probabilistic Outputs for Support Vector Machines and Comparisons to
    ///                     Regularized Likelihood Methods. In ADVANCES IN LARGE MARGIN CLASSIFIERS (1999), pp. 61-74.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Hsuan-Tien Lin, Chih-Jen Lin, and Ruby C. Weng. 2007. A note on Platt's probabilistic outputs
    ///                     for support vector machines. Mach. Learn. 68, 3 (October 2007), 267-276.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to calibrate a SVM that has
    ///         been trained to perform a simple XOR function.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\ProbabilisticOutputCalibrationTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO and calibrated using Platt's scaling.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="SequentialMinimalOptimization{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    public class ProbabilisticOutputCalibration<TKernel> :
        ProbabilisticOutputCalibrationBase<SupportVectorMachine<TKernel>, TKernel, double[]>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        /// <param name="machine">The support vector machine to be calibrated.</param>
        public ProbabilisticOutputCalibration(SupportVectorMachine<TKernel> machine)
            : base(machine)
        {
        }

        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        public ProbabilisticOutputCalibration()
        {
        }
    }

    /// <summary>
    ///     Probabilistic Output Calibration for structured Kernel machines.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Instead of producing probabilistic outputs, Support Vector Machines
    ///         express their decisions in the form of a distance from support vectors in
    ///         feature space. In order to convert the SVM outputs into probabilities,
    ///         Platt (1999) proposed the calibration of the SVM outputs using a sigmoid
    ///         (Logit) link function. Later, Lin et al (2007) provided a corrected and
    ///         improved version of Platt's probabilistic outputs. This class implements
    ///         the later.
    ///     </para>
    ///     <para>
    ///         This class is not an actual learning algorithm, but a calibrator.
    ///         Machines passed as input to this algorithm should already have been trained
    ///         by a proper learning algorithm such as
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization (SMO)
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         This class can also be used in combination with <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         or <see cref="MultilabelSupportVectorLearning{TKernel}" /> to learn
    ///         <see cref="MulticlassSupportVectorMachine{TKernel}" />s
    ///         using the <c>one-vs-one</c> or <c>one-vs-all</c> multi-class decision strategies, respectively.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     John C. Platt. 1999. Probabilistic Outputs for Support Vector Machines and Comparisons to
    ///                     Regularized Likelihood Methods. In ADVANCES IN LARGE MARGIN CLASSIFIERS (1999), pp. 61-74.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Hsuan-Tien Lin, Chih-Jen Lin, and Ruby C. Weng. 2007. A note on Platt's probabilistic outputs
    ///                     for support vector machines. Mach. Learn. 68, 3 (October 2007), 267-276.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to calibrate a SVM that has
    ///         been trained to perform a simple XOR function.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\ProbabilisticOutputCalibrationTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO and calibrated using Platt's scaling.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    public class ProbabilisticOutputCalibration<TKernel, TInput>
        : ProbabilisticOutputCalibrationBase<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        /// <param name="machine">The support vector machine to be calibrated.</param>
        public ProbabilisticOutputCalibration(SupportVectorMachine<TKernel, TInput> machine)
            : base(machine)
        {
        }

        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        public ProbabilisticOutputCalibration()
        {
        }
    }

    /// <summary>
    ///     Probabilistic Output Calibration.
    /// </summary>
    public abstract partial class ProbabilisticOutputCalibrationBase<TModel, TKernel, TInput> :
        BinaryLearningBase<TModel, TInput>,
        ISupportVectorMachineLearning<TInput>
        where TKernel : IKernel<TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        private readonly TInput[] input;
        private readonly int[] output;
        private readonly double sigma = 1e-12; // Set to any value > 0
        private double[] distances;

        // Parameter setting
        private double[] targets;

        /// <summary>
        ///     Initializes a new instance of Platt's Probabilistic Output Calibration algorithm.
        /// </summary>
        /// <param name="machine">The support vector machine to be calibrated.</param>
        public ProbabilisticOutputCalibrationBase(TModel machine)
        {
            Model = machine;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProbabilisticOutputCalibrationBase{TModel, TKernel, TInput}" /> class.
        /// </summary>
        public ProbabilisticOutputCalibrationBase()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected ProbabilisticOutputCalibrationBase(ISupportVectorMachine<double[]> model, TInput[] input,
            int[] output)
        {
            Model = (TModel)model;
            this.input = input;
            this.output = output;
        }

        /// <summary>
        ///     Gets or sets the maximum number of
        ///     iterations. Default is 100.
        /// </summary>
        public int Iterations { get; set; } = 100;

        /// <summary>
        ///     Gets or sets the tolerance under which the
        ///     answer must be found. Default is 1-e5.
        /// </summary>
        public double Tolerance { get; set; } = 1e-5;

        /// <summary>
        ///     Gets or sets the minimum step size used
        ///     during line search. Default is 1e-10.
        /// </summary>
        public double StepSize { get; set; } = 1e-10;

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public double Run()
        {
            Learn(input, output);
            return logLikelihood(input, output);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public double Run(bool computeError)
        {
            Learn(input, output);
            if (computeError)
            {
                return logLikelihood(input, output);
            }

            return 0;
        }

        ISupportVectorMachine<TInput> ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, int>.Learn(TInput[] x,
            int[] y, double[] weights)
        {
            return Learn(x, y, weights);
        }

        ISupportVectorMachine<TInput> ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, int[]>.Learn(
            TInput[] x, int[][] y, double[] weights)
        {
            return Learn(x, y, weights);
        }

        ISupportVectorMachine<TInput> ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, bool[]>.Learn(
            TInput[] x, bool[][] y, double[] weights)
        {
            return Learn(x, y, weights);
        }

        ISupportVectorMachine<TInput> ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, bool>.Learn(TInput[] x,
            bool[] y, double[] weights)
        {
            return Learn(x, y, weights);
        }

        ISupportVectorMachine<TInput> ISupervisedLearning<ISupportVectorMachine<TInput>, TInput, double>.Learn(
            TInput[] x, double[] y, double[] weights)
        {
            return Learn(x, y, weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="inputs">The model inputs.</param>
        /// <param name="outputs">The desired outputs associated with each <paramref name="inputs">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="outputs" /> given <paramref name="inputs" />.</returns>
        public override TModel Learn(TInput[] inputs, bool[] outputs, double[] weights = null)
        {
            if (weights != null)
            {
                throw new NotSupportedException();
            }

            // This method is a direct implementation of the algorithm
            // as published by Hsuan-Tien Lin, Chih-Jen Lin and Ruby C.
            // Weng, 2007. See references in documentation for more details.
            // 

            // Learning data
            distances = new double[outputs.Length];
            targets = new double[outputs.Length];
            int positives, negatives;
            countClasses(outputs, out positives, out negatives);

            // Compute the Support Vector Machine outputs
            // TODO: rename all 'results' to 'result'
            Model.Score(inputs, distances);

            initializeTargets(outputs, positives, negatives);

            // Initialize 
            var A = 0.0;
            var B = System.Math.Log((negatives + 1.0) / (positives + 1.0));
            var iterations = 0;

            var logLikelihood = computeLogLikelihood(A, B);

            // Start main algorithm loop.
            while (iterations < Iterations)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                iterations++;

                double h11, h22, h21, g1, g2;
                computeGradientAndHessian(A, B, out h11, out h22, out h21, out g1, out g2);

                // Check if the gradient is near zero as stopping criteria
                if (System.Math.Abs(g1) < Tolerance && System.Math.Abs(g2) < Tolerance)
                {
                    break;
                }

                // Compute modified Newton directions
                var det = h11 * h22 - h21 * h21;
                var dA = -(h22 * g1 - h21 * g2) / det;
                var dB = -(-h21 * g1 + h11 * g2) / det;
                var gd = g1 * dA + g2 * dB;

                lineSearch(ref A, ref B, ref logLikelihood, dA, dB, gd);
            }


            // The iterative algorithm has converged
            for (var i = 0; i < Model.Weights.Length; i++)
            {
                Model.Weights[i] *= -A;
            }

            Model.Threshold = Model.Threshold * -A - B;
            Model.IsProbabilistic = true;

            return Model;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please use Accord.Math.Optimization.BinaryCrossEntropyLoss or any other losses of your choice from the Accord.Math.Optimization namespace.")]
        public double LogLikelihood(TInput[] inputs, int[] outputs)
        {
            return logLikelihood(inputs, outputs);
        }

        private double logLikelihood(TInput[] inputs, int[] outputs)
        {
            // Compute the log-likelihood of the model
            var logLikelihood = 0.0;

            // Compute the new log-likelihood function
            for (var i = 0; i < inputs.Length; i++)
            {
                var y = Model.Score(inputs[i]);
                double t = outputs[i] == 1 ? 1 : 0;

                if (y >= 0)
                {
                    logLikelihood += t * y + Special.Log1p(System.Math.Exp(-y));
                }
                else
                {
                    logLikelihood += (t - 1) * y + Special.Log1p(System.Math.Exp(y));
                }
            }

            return logLikelihood;
        }
    }
}
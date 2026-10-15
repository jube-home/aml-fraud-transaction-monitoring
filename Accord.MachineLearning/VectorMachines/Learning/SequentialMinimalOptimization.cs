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
using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Gets the selection strategy to be used in SMO.
    /// </summary>
    public enum SelectionStrategy
    {
        /// <summary>
        ///     Uses the sequential selection strategy as
        ///     suggested by Keerthi et al's algorithm 1.
        /// </summary>
        Sequential,

        /// <summary>
        ///     Always select the worst violation pair
        ///     to be optimized first, as suggested in
        ///     Keerthi et al's algorithm 2.
        /// </summary>
        WorstPair,

        /// <summary>
        ///     Use a second order selection algorithm, using
        ///     the same algorithm as LibSVM's implementation.
        /// </summary>
        SecondOrder
    }

    /// <summary>
    ///     Sequential Minimal Optimization (SMO) Algorithm
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The SMO algorithm is an algorithm for solving large quadratic programming (QP)
    ///         optimization problems, widely used for the training of support vector machines.
    ///         First developed by John C. Platt in 1998, SMO breaks up large QP problems into
    ///         a series of smallest possible QP problems, which are then solved analytically.
    ///     </para>
    ///     <para>
    ///         This class follows the original algorithm by Platt with additional modifications
    ///         by Keerthi et al.
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
    ///                     <a href="http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization">
    ///                         Wikipedia, The Free Encyclopedia. Sequential Minimal Optimization. Available on:
    ///                         http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf">
    ///                         John C. Platt, Sequential Minimal Optimization: A Fast Algorithm for Training Support
    ///                         Vector Machines. 1998. Available on:
    ///                         http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf">
    ///                         S. S. Keerthi et al. Improvements to Platt's SMO Algorithm for SVM Classifier Design.
    ///                         Technical Report CD-99-14. Available on: http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf">
    ///                         J. P. Lewis. A Short SVM (Support Vector Machine) Tutorial. Available on:
    ///                         http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>The following example shows how to use a SVM to learn a simple XOR function.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_normal" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    ///     <para>The same as before, but using a Gaussian kernel.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         The following example shows how to learn a simple binary SVM using
    ///         a precomputed kernel matrix obtained from a Gaussian kernel.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_precomputed" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    //[Obsolete("Please use SequentialMinimalOptimization<TKernel> instead.")]
    public class SequentialMinimalOptimization :
        BaseSequentialMinimalOptimization<ISupportVectorMachine<double[]>, IKernel<double[]>, double[]>,
        ISupportVectorMachineLearning
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public SequentialMinimalOptimization(SupportVectorMachine model, double[][] input, int[] output)
            : base(model, input, output)
        {
            init();
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public SequentialMinimalOptimization(KernelSupportVectorMachine model, double[][] input, int[] output)
            : base(model, input, output)
        {
            init();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SequentialMinimalOptimization" /> class.
        /// </summary>
        public SequentialMinimalOptimization()
        {
            init();
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override ISupportVectorMachine<double[]> Create(int inputs, IKernel<double[]> kernel)
        {
            if (kernel is Linear)
            {
                return new SupportVectorMachine(inputs) { Kernel = (Linear)kernel };
            }

            return new SupportVectorMachine<IKernel<double[]>>(inputs, kernel);
        }

        private void init()
        {
            if (Kernel == null)
            {
                Kernel = new Linear();
            }
        }

        // TODO: Those methods are being shadowed to provide temporary support to the 
        // previous way of creating support vector machines in the framework. After a
        // a few releases, those methods will have to be removed and this class will need
        // to be updated to derive from SequentialMinimalOptimization<SupportVectorMachine>.

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public new SupportVectorMachine Learn(double[][] x, double[] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public new SupportVectorMachine Learn(double[][] x, int[] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public new SupportVectorMachine Learn(double[][] x, int[][] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y.GetColumn(0)), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public new SupportVectorMachine Learn(double[][] x, bool[][] y, double[] weights = null)
        {
            return Learn(x, y.GetColumn(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public new SupportVectorMachine Learn(double[][] x, bool[] y, double[] weights = null)
        {
            var svm = base.Learn(x, y, weights);
            return (SupportVectorMachine)svm;
        }
    }

    /// <summary>
    ///     Sequential Minimal Optimization (SMO) Algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The SMO algorithm is an algorithm for solving large quadratic programming (QP)
    ///         optimization problems, widely used for the training of support vector machines.
    ///         First developed by John C. Platt in 1998, SMO breaks up large QP problems into
    ///         a series of smallest possible QP problems, which are then solved analytically.
    ///     </para>
    ///     <para>
    ///         This class follows the original algorithm by Platt with additional modifications
    ///         by Keerthi et al.
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
    ///                     <a href="http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization">
    ///                         Wikipedia, The Free Encyclopedia. Sequential Minimal Optimization. Available on:
    ///                         http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf">
    ///                         John C. Platt, Sequential Minimal Optimization: A Fast Algorithm for Training Support
    ///                         Vector Machines. 1998. Available on:
    ///                         http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf">
    ///                         S. S. Keerthi et al. Improvements to Platt's SMO Algorithm for SVM Classifier Design.
    ///                         Technical Report CD-99-14. Available on: http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf">
    ///                         J. P. Lewis. A Short SVM (Support Vector Machine) Tutorial. Available on:
    ///                         http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>The following example shows how to use a SVM to learn a simple XOR function.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_normal" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    ///     <para>The same as before, but using a Gaussian kernel.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         The following example shows how to learn a simple binary SVM using
    ///         a precomputed kernel matrix obtained from a Gaussian kernel.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_precomputed" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    public class SequentialMinimalOptimization<TKernel> :
        BaseSequentialMinimalOptimization<
            SupportVectorMachine<TKernel>, TKernel, double[]>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine<TKernel> Create(int inputs, TKernel kernel)
        {
            return new SupportVectorMachine<TKernel>(inputs, kernel);
        }
    }

    /// <summary>
    ///     Sequential Minimal Optimization (SMO) Algorithm (for arbitrary data types).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The SMO algorithm is an algorithm for solving large quadratic programming (QP)
    ///         optimization problems, widely used for the training of support vector machines.
    ///         First developed by John C. Platt in 1998, SMO breaks up large QP problems into
    ///         a series of smallest possible QP problems, which are then solved analytically.
    ///     </para>
    ///     <para>
    ///         This class follows the original algorithm by Platt with additional modifications
    ///         by Keerthi et al.
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
    ///                     <a href="http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization">
    ///                         Wikipedia, The Free Encyclopedia. Sequential Minimal Optimization. Available on:
    ///                         http://en.wikipedia.org/wiki/Sequential_Minimal_Optimization
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf">
    ///                         John C. Platt, Sequential Minimal Optimization: A Fast Algorithm for Training Support
    ///                         Vector Machines. 1998. Available on:
    ///                         http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf">
    ///                         S. S. Keerthi et al. Improvements to Platt's SMO Algorithm for SVM Classifier Design.
    ///                         Technical Report CD-99-14. Available on: http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf">
    ///                         J. P. Lewis. A Short SVM (Support Vector Machine) Tutorial. Available on:
    ///                         http://www.idiom.com/~zilla/Work/Notes/svmtutorial.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>The following example shows how to use a SVM to learn a simple XOR function.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_normal" />
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using SMO.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    ///     <para>The same as before, but using a Gaussian kernel.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         The following example shows how to learn a simple binary SVM using
    ///         a precomputed kernel matrix obtained from a Gaussian kernel.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_precomputed" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    public class SequentialMinimalOptimization<TKernel, TInput> :
        BaseSequentialMinimalOptimization<
            SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : IKernel<TInput>
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine<TKernel, TInput> Create(int inputs, TKernel kernel)
        {
            return new SupportVectorMachine<TKernel, TInput>(inputs, kernel);
        }
    }

    /// <summary>
    ///     Base class for Sequential Minimal Optimization.
    /// </summary>
    public abstract partial class BaseSequentialMinimalOptimization<TModel, TKernel, TInput> :
        BaseSupportVectorClassification<TModel, TKernel, TInput>,
        ISupportVectorMachineLearning<TInput>
        where TKernel : IKernel<TInput>
        where TModel : ISupportVectorMachine<TInput>
    // TODO: after a few releases, the TModel constraint should be changed to:
    // where TModel : SupportVectorMachine<TKernel, TInput>, ISupportVectorMachine<TInput>
    {
        private readonly int maxChecks = 100;

        // Support Vector Machine parameters
        private double b_lower;
        private double b_upper;

        private int cacheSize = -1;
        private double epsilon = 1e-6;

        // Error cache to speed up computations
        private double[] errors;

        // Keerthi's improvements
        private int i_lower;
        private int i_upper;
        private bool isCompact;
        private KernelFunctionCache<TKernel, TInput> kernelCache;

        // Learning algorithm parameters
        private double tolerance = 1e-2;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSequentialMinimalOptimization{TModel, TKernel, TInput}" /> class.
        /// </summary>
        public BaseSequentialMinimalOptimization()
        {
            // Index sets
            ActiveExamples = new HashSet<int>();
            NonBoundExamples = new HashSet<int>();
            BoundedExamples = new HashSet<int>();
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseSequentialMinimalOptimization(ISupportVectorMachine<TInput> model, TInput[] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Epsilon for round-off errors. Default value is 1e-6.
        /// </summary>
        public double Epsilon
        {
            get => epsilon;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                epsilon = value;
            }
        }

        /// <summary>
        ///     Convergence tolerance. Default value is 1e-2.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 0.01.
        /// </remarks>
        public double Tolerance
        {
            get => tolerance;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                tolerance = value;
            }
        }

        /// <summary>
        ///     Gets or sets the
        ///     <see cref="SelectionStrategy">
        ///         pair selection
        ///         strategy
        ///     </see>
        ///     to be used during optimization.
        /// </summary>
        public SelectionStrategy Strategy { get; set; } = SelectionStrategy.WorstPair;

        /// <summary>
        ///     Gets or sets the cache size to partially store the kernel
        ///     matrix. Default is the same number of input vectors, meaning
        ///     the entire kernel matrix will be computed and cached in memory.
        ///     If set to zero, the cache will be disabled and all operations will
        ///     be computed as needed.
        /// </summary>
        /// <remarks>
        ///     In order to know how many rows can fit under a amount of memory, you can use
        ///     <see cref="KernelFunctionCache.GetNumberOfRowsForMaximumSizeInBytes(int)" />.
        ///     Be sure to also test the algorithm with the cache disabled, as sometimes the
        ///     cost of the extra memory allocations needed by the cache will be higher than
        ///     the cost of evaluating the kernel function, specially for fast kernels such
        ///     as <see cref="Linear" />.
        /// </remarks>
        public int CacheSize
        {
            get => cacheSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                cacheSize = value;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether shrinking heuristics should be used. Default is false. Note:
        ///     this property can only be used when <see cref="Strategy" /> is set to <see cref="SelectionStrategy.SecondOrder" />.
        /// </summary>
        /// <value>
        ///     <c>true</c> to use shrinking heuristics; otherwise, <c>false</c>.
        /// </value>
        public bool Shrinking { get; set; }

        /// <summary>
        ///     Gets the value for the Lagrange multipliers
        ///     (alpha) for every observation vector.
        /// </summary>
        public double[] Lagrange { get; private set; }

        /// <summary>
        ///     Gets or sets whether to produce compact models. Compact
        ///     formulation is currently limited to linear models.
        /// </summary>
        [Obsolete("To generate compact linear machines, call the Compact method after creation.")]
        public bool Compact
        {
            get => isCompact;
            set
            {
                if (!(Kernel is Linear) && value)
                {
                    throw new InvalidOperationException("Only linear machines can be created in compact form.");
                }

                isCompact = value;
            }
        }

        /// <summary>
        ///     Gets the indices of the active examples (examples which have
        ///     the corresponding Lagrange multiplier different than zero).
        /// </summary>
        public HashSet<int> ActiveExamples { get; private set; }

        /// <summary>
        ///     Gets the indices of the non-bounded examples (examples which
        ///     have the corresponding Lagrange multipliers between 0 and C).
        /// </summary>
        public HashSet<int> NonBoundExamples { get; private set; }

        /// <summary>
        ///     Gets the indices of the examples at the boundary (examples
        ///     which have the corresponding Lagrange multipliers equal to C).
        /// </summary>
        public HashSet<int> BoundedExamples { get; private set; }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            // Initialize variables
            var samples = Inputs.Length;
            var c = C;
            var x = Inputs;
            var y = Outputs;

            // Lagrange multipliers
            Lagrange = new double[samples];

            // Prepare indices sets
            ActiveExamples = new HashSet<int>();
            NonBoundExamples = new HashSet<int>();
            BoundedExamples = new HashSet<int>();

            // Kernel cache
            if (cacheSize == -1)
            {
                cacheSize = samples;
            }

            using (kernelCache = new KernelFunctionCache<TKernel, TInput>(Kernel, Inputs, cacheSize))
            {
                bool diverged;


                if (Strategy == SelectionStrategy.SecondOrder)
                {
                    diverged = solveSecondOrder(c, x, y);
                }
                else // Strategy is Strategy.WorstPair or Strategy.Sequential
                {
                    diverged = solveKeerthi(samples, c, y);
                }


                storeBoundedExamples(c);


                storeSupportVectors(x, y);

                Model.Threshold = -(b_lower + b_upper) / 2;

                if (isCompact)
                {
                    Model.Compress();
                }

                if (diverged)
                {
                    throw new ConvergenceException("Convergence could not be attained. " +
                                                   "Please reduce the cost of misclassification errors by reducing " +
                                                   "the complexity parameter C or try a different kernel function.");
                }
            }
        }

        /// <summary>
        ///     Chooses which multipliers to optimize using heuristics.
        /// </summary>
        private bool examineExample(int i2)
        {
            // double[] p2 = Inputs[i2]; // Input point at index i2
            double y2 = Outputs[i2]; // Classification label for p2
            // double alph2 = alpha[i2]; // Lagrange multiplier for p2

            double e2; // SVM output on p2 - y2. Check if it has already been computed

            var set = I(i2);

            if (set == 0)
            {
                // i2 is in I0
                e2 = errors[i2];
            }
            else
            {
                // Compute error and update
                e2 = errors[i2] = computeNoBias(i2) - y2;

                if ((set == 1 || set == 2) && e2 < b_upper)
                {
                    // i2 is in I1 or I2
                    b_upper = e2;
                    i_upper = i2;
                }
                else if ((set == 3 || set == 4) && e2 > b_lower)
                {
                    // i2 is in I3 or I4
                    b_lower = e2;
                    i_lower = i2;
                }
            }


            var i1 = -1;

            // [Keerthi] Check optimality using current b_low
            // and b_up and, if violated, find an index i1 to
            // do a joint optimization with i2.

            var optimality = true;
            if (set == 0 || set == 1 || set == 2)
            {
                // i2 is in I0, I1 or I2
                if (b_lower - e2 > 2 * tolerance)
                {
                    optimality = false;
                    i1 = i_lower;
                }
            }

            if (set == 0 || set == 3 || set == 4)
            {
                // i2 is in I0, I3 or I4
                if (e2 - b_upper > 2 * tolerance)
                {
                    optimality = false;
                    i1 = i_upper;
                }
            }

            if (optimality)
            {
                return false; // no need to take step
            }

            if (set == 0)
            {
                // [Keerthi] for i2 in I0, select the best i1
                i1 = b_lower - e2 > e2 - b_upper ? i_lower : i_upper;
            }

            return takeStep(i1, i2);
        }

        private int I(int i)
        {
            var a = Lagrange[i];
            double y = Outputs[i];
            var c = C[i];

            // From Keerthi's technical report, define:
            //
            //   I0 = { 0 < a[i] < c }
            //   I1 = { y[i] = +1, a[i] = 0 }
            //   I2 = { y[i] = -1, a[i] = c }
            //   I3 = { y[i] = +1, a[i] = c }
            //   I4 = { y[i] = -1, a[i] = 0 }
            //

            if (y == +1 && a == 0)
            {
                return 1; // I1
            }

            if (y == -1 && a == c)
            {
                return 2; // I2
            }

            if (y == +1 && a == c)
            {
                return 3; // I3
            }

            if (y == -1 && a == 0)
            {
                return 4; // I4
            }

            return 0; // I0 (not at bounds)
        }

        /// <summary>
        ///     Analytically solves the optimization problem for two Lagrange multipliers.
        /// </summary>
        private bool takeStep(int i1, int i2)
        {
            if (i1 == i2)
            {
                return false;
            }

            //TInput p1 = Inputs[i1]; // Input point at index i1
            var alph1 = Lagrange[i1]; // Lagrange multiplier for p1
            double y1 = Outputs[i1]; // Classification label for p1
            var c1 = C[i1];

            // SVM output on p1 - y1 [without bias threshold]. Check if it has already been computed
            var e1 = alph1 > 0 && alph1 < c1 ? errors[i1] : errors[i1] = computeNoBias(i1) - y1;

            //TInput p2 = Inputs[i2]; // Input point at index i2
            var alph2 = Lagrange[i2]; // Lagrange multiplier for p2
            double y2 = Outputs[i2]; // Classification label for p2
            var c2 = C[i2];


            // SVM output on p2 - y2. Should have been computed already.
            var e2 = errors[i2];

            var s = y1 * y2;


            double L, H;
            computeBounds(y1, y2, alph1, alph2, c1, c2, out L, out H);

            if (L >= H)
            {
                return false;
            }


            var k11 = kernelCache.GetOrCompute(i1);
            var k22 = kernelCache.GetOrCompute(i2);
            var k12 = kernelCache.GetOrCompute(i1, i2);

            var eta = 2.0 * k12 - k11 - k22;


            double a1, a2;

            a2 = computeUnclippedAlpha2(alph2, y2, e1, e2, eta, L, H);

            if (System.Math.Abs(a2 - alph2) < epsilon * (a2 + alph2 + epsilon))
            {
                return false; // no step need to be taken
            }

            computeUpdateStep(alph1, alph2, c1, c2, s, ref a2, out a1);

            // Update error cache using new Lagrange multipliers
            var t1 = y1 * (a1 - alph1);
            var t2 = y2 * (a2 - alph2);
            updateErrorCache(i1, i2, t1, t2, k11, k12, k22);


            applyMultipliers(i1, i2, a1, a2);

            updateThresholdBounds(i1, i2);

            // step taken
            return true;
        }

        private void updateSets(int index, double value)
        {
            if (value > 0)
            {
                ActiveExamples.Add(index);
            }
            else
            {
                ActiveExamples.Remove(index);
            }

            if (value == 0 || value == C[index])
            {
                // Value is at boundaries
                NonBoundExamples.Remove(index);
            }
            else
            {
                NonBoundExamples.Add(index);
            }
        }

        /// <summary>
        ///     Computes the SVM output for a given point.
        /// </summary>
        private double computeNoBias(int j)
        {
            double sum = 0;
            foreach (var i in ActiveExamples)
            {
                sum += Lagrange[i] * Outputs[i] * kernelCache.GetOrCompute(i, j);
            }

            return sum;
        }
    }
}
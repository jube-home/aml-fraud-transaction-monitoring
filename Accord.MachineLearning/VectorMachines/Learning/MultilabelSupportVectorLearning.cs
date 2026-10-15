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
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    using InnerParameters = InnerParameters<SupportVectorMachine<IKernel<double[]>>, double[]>;
    using InnerLearning =
        ISupervisedLearning<SupportVectorMachine<IKernel<double[]>>, double[], bool>;

    /// <summary>
    ///     Obsolete. Please use <see cref="MultilabelSupportVectorLearning{TKernel}" /> instead.
    /// </summary>
    [Obsolete("Please specify the desired kernel function as a template parameter.")]
    public class MultilabelSupportVectorLearning :
        BaseMultilabelSupportVectorLearning<double[],
            SupportVectorMachine<IKernel<double[]>>, IKernel<double[]>,
            MultilabelSupportVectorMachine>
    {
        private readonly double[][] input;
        private readonly int[][] output;

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public MultilabelSupportVectorLearning(MultilabelSupportVectorMachine model, double[][] input, int[][] output)
        {
            Model = model;
            this.input = input;
            this.output = output;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public MultilabelSupportVectorLearning(MultilabelSupportVectorMachine model, double[][] input, int[] output)
        {
            Model = model;
            this.input = input;
            this.output = Jagged.OneHot<int>(output);
        }

        /// <summary>
        ///     Gets or sets the kernel function to be used to learn the
        ///     <see cref="SupportVectorMachine{TKernel}">
        ///         kernel support
        ///         vector machines
        ///     </see>
        ///     .
        /// </summary>
        public new IKernel Kernel
        {
            // TODO: Remove the shadowing and convert this class to a Linear
            // Multi label support vector learning
            get => base.Kernel as IKernel;
            set => base.Kernel = value;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public double Run()
        {
            Learn(input, output);
            return new HammingLoss(output)
            {
                Mean = true
            }.Loss(Model.Decide(input));
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override MultilabelSupportVectorMachine Create(int inputs, int classes, bool multilabel)
        {
            return new MultilabelSupportVectorMachine(inputs, Kernel, classes)
            {
                Method = multilabel ? MultilabelProbabilityMethod.PerClass : MultilabelProbabilityMethod.SumsToOne
            };
        }

#pragma warning disable 0618
#pragma warning disable 0612
        private SupportVectorMachineLearningConfigurationFunction algorithm;

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use the Configure method instead.")]
        public SupportVectorMachineLearningConfigurationFunction Algorithm
        {
            get => algorithm;
            set
            {
                algorithm = value;
                Learner = Convert(value);
            }
        }

        /// <summary>
        ///     Converts <see cref="SupportVectorMachineLearningConfigurationFunction" />
        ///     into a lambda function that can be passed to the <see cref="OneVsRestLearning{TInput, TBinary, TModel}.Learner" />
        ///     property of a <see cref="MultilabelSupportVectorLearning" /> learning algorithm.
        /// </summary>
        public static Func<InnerParameters, InnerLearning> Convert(
            SupportVectorMachineLearningConfigurationFunction conf)
        {
            return delegate(InnerParameters parameters)
            {
                var y = parameters.Outputs.ToMinusOnePlusOne();
                var machine = (KernelSupportVectorMachine)parameters.Model;
                var r = conf(machine, parameters.Inputs, y, parameters.Pair.Class1,
                    parameters.Pair.Class2);

                var c = r as InnerLearning;
                if (c != null)
                {
                    return c;
                }


                // TODO: The following checks exist only to provide support to previous way of using
                // the library and should be removed after a few releases.
                var svc = r;
                if (svc != null)
                {
                    svc.Run();
                    return null;
                }

                throw new Exception();
            };
        }
#pragma warning restore 0618
#pragma warning restore 0612
    }

    /// <summary>
    ///     One-against-all Multi-label Support Vector Machine Learning Algorithm
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class can be used to train Kernel Support Vector Machines with
    ///         any algorithm using a <c>one-against-all</c> strategy. The underlying
    ///         training algorithm can be configured by defining the
    ///         <see cref="OneVsRestLearning{TInput, TBinary, TModel}.Learner" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         One example of learning algorithm that can be used with this class is the
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization
    ///         </see>
    ///         (SMO) algorithm.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to learn a linear, multi-label (one-vs-rest) support
    ///         vector machine using the <see cref="LinearDualCoordinateDescent" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    ///     <para>
    ///         The following example shows how to learn a non-linear, multi-label (one-vs-rest)
    ///         support vector machine using the <see cref="Gaussian" /> kernel and the
    ///         <see cref="SequentialMinimalOptimization{TKernel}" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         Support vector machines can have their weights calibrated in order to produce probability
    ///         estimates (instead of simple class separation distances). The following example shows how
    ///         to use <see cref="ProbabilisticOutputCalibration" /> within
    ///         <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         to generate a probabilistic SVM:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    public class MultilabelSupportVectorLearning<TKernel> :
        BaseMultilabelSupportVectorLearning<double[],
            SupportVectorMachine<TKernel>, TKernel,
            MultilabelSupportVectorMachine<TKernel>>
        where TKernel : IKernel<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorLearning{TKernel}" /> class.
        /// </summary>
        public MultilabelSupportVectorLearning()
        {
            Learner = p => new SequentialMinimalOptimization<TKernel>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorLearning{TKernel}" /> class.
        /// </summary>
        public MultilabelSupportVectorLearning(MultilabelSupportVectorMachine<TKernel> machine)
        {
            Model = machine;
            Learner = p => new SequentialMinimalOptimization<TKernel>
            {
                Model = p.Model
            };
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override MultilabelSupportVectorMachine<TKernel> Create(int inputs, int classes, bool multilabel)
        {
            return new MultilabelSupportVectorMachine<TKernel>(inputs, Kernel, classes)
            {
                Method = multilabel ? MultilabelProbabilityMethod.PerClass : MultilabelProbabilityMethod.SumsToOne
            };
        }
    }

    /// <summary>
    ///     One-against-all Multi-label Support Vector Machine Learning Algorithm
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class can be used to train Kernel Support Vector Machines with
    ///         any algorithm using a <c>one-against-all</c> strategy. The underlying
    ///         training algorithm can be configured by defining the
    ///         <see cref="OneVsRestLearning{TInput, TBinary, TModel}.Learner" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         One example of learning algorithm that can be used with this class is the
    ///         <see cref="SequentialMinimalOptimization{TKernel}">
    ///             Sequential Minimal Optimization
    ///         </see>
    ///         (SMO) algorithm.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to learn a linear, multi-label (one-vs-rest) support
    ///         vector machine using the <see cref="LinearDualCoordinateDescent" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    ///     <para>
    ///         The following example shows how to learn a non-linear, multi-label (one-vs-rest)
    ///         support vector machine using the <see cref="Gaussian" /> kernel and the
    ///         <see cref="SequentialMinimalOptimization{TKernel}" /> algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_gaussian" />
    ///     <para>
    ///         Support vector machines can have their weights calibrated in order to produce probability
    ///         estimates (instead of simple class separation distances). The following example shows how
    ///         to use <see cref="ProbabilisticOutputCalibration" /> within
    ///         <see cref="MulticlassSupportVectorLearning{TKernel}" />
    ///         to generate a probabilistic SVM:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MultilabelSupportVectorLearningTest.cs"
    ///         region="doc_learn_calibration" />
    /// </example>
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    public class MultilabelSupportVectorLearning<TKernel, TInput> :
        BaseMultilabelSupportVectorLearning<TInput,
            SupportVectorMachine<TKernel, TInput>, TKernel,
            MultilabelSupportVectorMachine<TKernel, TInput>>
        where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorLearning{TKernel, TInput}" /> class.
        /// </summary>
        public MultilabelSupportVectorLearning()
        {
            Learner = p => new SequentialMinimalOptimization<TKernel, TInput>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorLearning{TKernel, TInput}" /> class.
        /// </summary>
        public MultilabelSupportVectorLearning(MultilabelSupportVectorMachine<TKernel, TInput> machine)
        {
            Model = machine;
            Learner = p => new SequentialMinimalOptimization<TKernel, TInput>
            {
                Model = p.Model
            };
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override MultilabelSupportVectorMachine<TKernel, TInput> Create(int inputs, int classes,
            bool multilabel)
        {
            return new MultilabelSupportVectorMachine<TKernel, TInput>(inputs, Kernel, classes)
            {
                Method = multilabel ? MultilabelProbabilityMethod.PerClass : MultilabelProbabilityMethod.SumsToOne
            };
        }
    }

    // TODO: Probably this class will prove not to be necessary in the long term:

    /// <summary>
    ///     Base class for multi-label support vector learning algorithms.
    /// </summary>
    public abstract class BaseMultilabelSupportVectorLearning<TInput, TBinary, TKernel, TModel> :
        OneVsRestLearning<TInput, TBinary, TModel>
        where TBinary : SupportVectorMachine<TKernel, TInput>
        where TModel : OneVsRest<TBinary, TInput>
        where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        /// <summary>
        ///     Gets or sets the kernel function to be used to learn the
        ///     <see cref="SupportVectorMachine{TKernel}">
        ///         kernel support
        ///         vector machines
        ///     </see>
        ///     .
        /// </summary>
        public TKernel Kernel { get; set; }
    }
}
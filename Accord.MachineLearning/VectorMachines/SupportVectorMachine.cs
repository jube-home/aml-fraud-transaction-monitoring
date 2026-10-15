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
using System.Reflection;
using System.Runtime.Serialization;
using Accord.IO;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;
using Accord.Statistics.Links;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Linear Support Vector Machine (SVM).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a linear support vector machine classifier. For its kernel
    ///         counterpart, which can produce non-linear decision boundaries, please check
    ///         <see cref="SupportVectorMachine{TKernel}" /> and <see cref="SupportVectorMachine{TKernel, TData}" />.
    ///     </para>
    ///     <para>
    ///         Note: a linear SVM model can be converted to <see cref="MultipleLinearRegression" /> and
    ///         <see cref="LogisticRegression" />. This means that linear and logistic regressions
    ///         can be created using any of the highly optimized LIBLINEAR learning algorithms
    ///         such as <see cref="LinearCoordinateDescent" />, <see cref="LinearDualCoordinateDescent" />,
    ///         <see cref="ProbabilisticCoordinateDescent" /> and <see cref="ProbabilisticDualCoordinateDescent" />.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Support_vector_machine">
    ///                         http://en.wikipedia.org/wiki/Support_vector_machine
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.kernel-machines.org/">
    ///                         http://www.kernel-machines.org/
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to learn a linear SVM. However, since the
    ///         problem being learned is not linearly separable, the classifier will
    ///         not be able to produce a perfect decision boundary.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_linear" />
    ///     <para>
    ///         The second example shows how to learn an SVM using a  standard kernel
    ///         that operates on vectors of doubles. With kernels, it is possible to
    ///         produce non-linear boundaries that perfectly separate the data.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_normal" />
    ///     <para>
    ///         The third example shows how to learn an SVM using a Sparse kernel that
    ///         operates on sparse vectors.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs"
    ///         region="doc_xor_sparse" />
    /// </example>
    /// <seealso cref="Statistics.Kernels" />
    /// <seealso cref="MulticlassSupportVectorMachine{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorMachine{TKernel}" />
    /// <seealso cref="SequentialMinimalOptimization{TKernel}" />
    [Serializable]
#if !NETSTANDARD1_4
    [SerializationBinder(typeof(SupportVectorMachineBinder))]
#endif
    public class SupportVectorMachine : SupportVectorMachine<Linear>,
        IBinaryClassifier<double[]>, ISupportVectorMachine<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SupportVectorMachine" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs for this machine.</param>
        public SupportVectorMachine(int inputs)
            : base(inputs, new Linear())
        {
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var clone = new SupportVectorMachine(NumberOfInputs) { Kernel = Kernel };
            clone.SupportVectors = Matrix.MemberwiseClone(SupportVectors);
            clone.IsProbabilistic = IsProbabilistic;
            clone.Weights = (double[])Weights.Clone();
            clone.Threshold = Threshold;
            return clone;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="MultipleLinearRegression" /> to <see cref="SupportVectorMachine" />
        ///     .
        /// </summary>
        /// <param name="regression">The linear regression to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator SupportVectorMachine(MultipleLinearRegression regression)
        {
            return FromRegression(regression);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="MultipleLinearRegression" /> to <see cref="SupportVectorMachine" />
        ///     .
        /// </summary>
        /// <param name="regression">The linear regression to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static SupportVectorMachine FromRegression(MultipleLinearRegression regression)
        {
            return new SupportVectorMachine(regression.NumberOfInputs)
            {
                Weights = new[] { 1.0 },
                SupportVectors = new[] { regression.Weights },
                Threshold = regression.Intercept
            };
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="LogisticRegression" /> to <see cref="SupportVectorMachine" />.
        /// </summary>
        /// <param name="regression">The logistic regression to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator SupportVectorMachine(LogisticRegression regression)
        {
            return FromLogisticRegression(regression);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="LogisticRegression" /> to <see cref="SupportVectorMachine" />.
        /// </summary>
        /// <param name="regression">The logistic regression to be converted.</param>
        /// <returns>The result of the conversion.</returns>
        public static SupportVectorMachine FromLogisticRegression(LogisticRegression regression)
        {
            return new SupportVectorMachine(regression.NumberOfInputs)
            {
                Weights = new[] { 1.0 },
                SupportVectors = new[] { regression.Weights },
                Threshold = regression.Intercept,
                IsProbabilistic = true
            };
        }

        /// <summary>
        ///     Creates a new linear <see cref="SupportVectorMachine" />
        ///     with the given set of linear <paramref name="weights" />.
        /// </summary>
        /// <param name="weights">The machine's linear coefficients.</param>
        /// <param name="interceptIndex">The index of the intercept term in the given weights vector.</param>
        /// <returns>
        ///     A <see cref="SupportVectorMachine" /> whose linear coefficients
        ///     are defined by the given <paramref name="weights" /> vector.
        /// </returns>
        public static SupportVectorMachine FromWeights(double[] weights, int interceptIndex = -1)
        {
            var newWeights = weights;
            double bias = 0;

            if (interceptIndex >= 0)
            {
                newWeights = new double[weights.Length - 1];
                for (int i = 0, j = 0; i < weights.Length; i++)
                {
                    if (i != interceptIndex)
                    {
                        newWeights[j++] = weights[i];
                    }
                }

                bias = weights[interceptIndex];
            }

            return new SupportVectorMachine(newWeights.Length)
            {
                Weights = new[] { 1.0 },
                SupportVectors = new[] { newWeights },
                Threshold = bias
            };
        }

        #region Serialization backwards compatibility

#if !NETSTANDARD1_4
        internal class SupportVectorMachineBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
                    if (typeName == "Accord.MachineLearning.VectorMachines.SupportVectorMachine")
                    {
                        return typeof(SupportVectorMachine_2_13);
                    }
                }

                return null;
            }
        }

#pragma warning disable 0169
#pragma warning disable 0649

        [Serializable]
        internal class SupportVectorMachine_2_13 : ISerializable
        {
            public int inputCount;
            public ILinkFunction linkFunction;
            public double[][] supportVectors;
            public double threshold;
            public double[] weights;

            private SupportVectorMachine_2_13(SerializationInfo info, StreamingContext context)
            {
                info.GetValue("inputCount", out inputCount);
                info.GetValue("linkFunction", out linkFunction);
                info.GetValue("supportVectors", out supportVectors);
                info.GetValue("threshold", out threshold);
                info.GetValue("weights", out weights);
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
            }

            public static implicit operator SupportVectorMachine(SupportVectorMachine_2_13 obj)
            {
                var svm = new SupportVectorMachine(obj.inputCount)
                {
                    SupportVectors = obj.supportVectors,
                    Weights = obj.weights,
                    Threshold = obj.threshold
                };

                var fn = obj.linkFunction as LogLinkFunction;
                if (fn != null)
                {
                    svm.Weights.Multiply(fn.B, svm.Weights);
                    svm.Threshold = svm.Threshold * fn.B + fn.A;
                    svm.IsProbabilistic = true;
                }

                return svm;
            }
        }

#pragma warning restore 0169
#pragma warning restore 0649
#endif

        #endregion
    }
}
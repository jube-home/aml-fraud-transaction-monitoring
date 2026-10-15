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
using System.Security.Permissions;
using Accord.IO;
using Accord.Math;
using Accord.Statistics.Kernels;
using Accord.Statistics.Links;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Obsolete.
    /// </summary>
    [Serializable]
    [Obsolete("Please use SupportVectorMachine<TKernel>.")]
#if !NETSTANDARD1_4
    [SerializationBinder(typeof(KernelSupportVectorMachineBinder))]
#endif
    public class KernelSupportVectorMachine :
        SupportVectorMachine<IKernel<double[]>>,
        ISupportVectorMachine<double[]>
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        public KernelSupportVectorMachine(IKernel<double[]> kernel, int inputs)
            : base(inputs, kernel)
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        public KernelSupportVectorMachine(IKernel kernel, int inputs)
            : base(inputs, kernel)
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        public new IKernel Kernel
        {
            get => (IKernel)base.Kernel;
            set => base.Kernel = value;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        public override object Clone()
        {
            var clone = new KernelSupportVectorMachine(Kernel, NumberOfInputs);
            clone.SupportVectors = Matrix.MemberwiseClone(SupportVectors);
            clone.IsProbabilistic = IsProbabilistic;
            clone.Weights = (double[])Weights.Clone();
            clone.Threshold = Threshold;
            return clone;
        }

        #region Serialization backwards compatibility

#if !NETSTANDARD1_4
        internal class KernelSupportVectorMachineBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
                    if (typeName == "Accord.MachineLearning.VectorMachines.SupportVectorMachine")
                    {
                        return typeof(SupportVectorMachine.SupportVectorMachine_2_13);
                    }

                    if (typeName == "Accord.MachineLearning.VectorMachines.KernelSupportVectorMachine")
                    {
                        return typeof(KernelSupportVectorMachine_2_13);
                    }
                }

                return null;
            }
        }

#pragma warning disable 0169
#pragma warning disable 0649

        [Serializable]
        internal class KernelSupportVectorMachine_2_13 : ISerializable
        {
            public int inputCount;
            public IKernel kernel;
            public ILinkFunction linkFunction;
            public double[][] supportVectors;
            public double threshold;
            public double[] weights;

            [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
            private KernelSupportVectorMachine_2_13(SerializationInfo info, StreamingContext context)
            {
                info.GetValue("SupportVectorMachine+inputCount", out inputCount);
                info.GetValue("SupportVectorMachine+linkFunction", out linkFunction);
                info.GetValue("SupportVectorMachine+supportVectors", out supportVectors);
                info.GetValue("SupportVectorMachine+threshold", out threshold);
                info.GetValue("SupportVectorMachine+weights", out weights);
                info.GetValue("kernel", out kernel);
            }

            [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
            }

            public static implicit operator KernelSupportVectorMachine(KernelSupportVectorMachine_2_13 obj)
            {
                var svm = new KernelSupportVectorMachine(obj.kernel, obj.inputCount)
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
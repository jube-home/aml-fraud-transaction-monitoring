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
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Accord.IO;
using Accord.Math;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines
{
    /// <summary>
    ///     Obsolete. Please use <see cref="MultilabelSupportVectorMachine{TKernel}" /> instead.
    /// </summary>
    [Serializable]
    [Obsolete("Please use MultilabelSupportVectorMachine<TKernel> instead.")]
#if !NETSTANDARD1_4
    [SerializationBinder(typeof(MultilabelSupportVectorMachineBinder))]
#endif
    public class MultilabelSupportVectorMachine :
        MultilabelSupportVectorMachine<IKernel<double[]>>
    {
#pragma warning disable 0618
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorMachine" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs by the machine.</param>
        /// <param name="classes">The number of classes to be handled by the machine.</param>
        public MultilabelSupportVectorMachine(int inputs, int classes)
            : base(classes, () => new KernelSupportVectorMachine(new Linear(), inputs))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorMachine" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs by the machine.</param>
        /// <param name="classes">The number of classes to be handled by the machine.</param>
        /// <param name="kernel">The kernel function to be used in the machine.</param>
        public MultilabelSupportVectorMachine(int inputs, IKernel kernel, int classes)
            : base(classes, () => new KernelSupportVectorMachine(kernel, inputs))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultilabelSupportVectorMachine" /> class.
        /// </summary>
        /// <param name="machines">The existing machines for detecting each of the classes against all other classes.</param>
        public MultilabelSupportVectorMachine(KernelSupportVectorMachine[] machines)
            : base(machines.Length, () => null)
        {
            NumberOfInputs = machines[0].NumberOfInputs;
            for (var i = 0; i < machines.Length; i++)
            {
                Models[i] = machines[i];
                if (machines[i].NumberOfInputs != NumberOfInputs)
                {
                    throw new ArgumentException();
                }
            }
        }
#pragma warning restore 0618

        #region Obsolete

#if !NETSTANDARD1_4
        /// <summary>
        ///     Gets the classifier for class <paramref name="index" />.
        /// </summary>
        [Obsolete("Use the Models property instead.")]
        public new KernelSupportVectorMachine this[int index] => Models[index] as KernelSupportVectorMachine;

        /// <summary>
        ///     Saves the machine to a stream.
        /// </summary>
        /// <param name="stream">The stream to which the machine is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save(stream) instead (or use it as an extension method).")]
        public void Save(Stream stream)
        {
            Serializer.Save(this, stream);
        }

        /// <summary>
        ///     Saves the machine to a file.
        /// </summary>
        /// <param name="path">The path to the file to which the machine is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save(path) instead (or use it as an extension method).")]
        public void Save(string path)
        {
            Serializer.Save(this, path);
        }

        /// <summary>
        ///     Loads a machine from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the machine is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<MultilabelSupportVectorMachine>(stream) instead.")]
        public static MultilabelSupportVectorMachine Load(Stream stream)
        {
            return Serializer.Load<MultilabelSupportVectorMachine>(stream);
        }

        /// <summary>
        ///     Loads a machine from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the machine is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<MultilabelSupportVectorMachine>(path) instead.")]
        public static MultilabelSupportVectorMachine Load(string path)
        {
            return Serializer.Load<MultilabelSupportVectorMachine>(path);
        }
#endif

        /// <summary>
        ///     Gets the number of classes.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int Classes => NumberOfOutputs;

        /// <summary>
        ///     Gets the number of inputs of the machines.
        /// </summary>
        [Obsolete("Please use NumberOfInputs instead.")]
        public int Inputs => NumberOfInputs;

        /// <summary>
        ///     Gets the subproblems classifiers.
        /// </summary>
#pragma warning disable 0618

        [Obsolete("Please use Models instead.")]
        public KernelSupportVectorMachine[] Machines
        {
            get { return Models.Apply(x => (KernelSupportVectorMachine)x); }
        }
#pragma warning restore 0618

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="output">The output for the given input.</param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide method instead.")]
        public int Compute(double[] inputs, out double output)
        {
            double[] responses;
            Compute(inputs, out responses);

            int imax;
            output = responses.Max(out imax);
            return imax;
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding outputs.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="responses">The model response for each class.</param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide or Transform methods instead.")]
        public int[] Compute(double[] inputs, out double[] responses)
        {
            int[] decision = null;
            responses = Probabilities(inputs, ref decision);
            return decision;
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding outputs.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide method instead.")]
        public int[] Compute(double[] inputs)
        {
            return Decide(inputs).ToInt32();
        }

        #endregion

        #region Serialization backwards compatibility

#if !NETSTANDARD1_4
        internal class MultilabelSupportVectorMachineBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
#pragma warning disable 618
                    if (typeName == "Accord.MachineLearning.VectorMachines.MultilabelSupportVectorMachine")
                    {
                        return typeof(MultilabelSupportVectorMachine_2_13);
                    }

                    if (typeName == "Accord.MachineLearning.VectorMachines.KernelSupportVectorMachine[]")
                    {
                        return typeof(KernelSupportVectorMachine.KernelSupportVectorMachine_2_13[]);
                    }

                    if (typeName == "Accord.MachineLearning.VectorMachines.KernelSupportVectorMachine")
                    {
                        return typeof(KernelSupportVectorMachine.KernelSupportVectorMachine_2_13);
                    }
#pragma warning restore 618
                }

                return Type.GetType(typeName);
            }
        }

#pragma warning disable 0169
#pragma warning disable 0649

        [Serializable]
        internal class MultilabelSupportVectorMachine_2_13
        {
            public KernelSupportVectorMachine.KernelSupportVectorMachine_2_13[] machines;

            public static implicit operator MultilabelSupportVectorMachine(MultilabelSupportVectorMachine_2_13 obj)
            {
#pragma warning disable 618
                var svms = obj.machines.Apply((x, i) => (KernelSupportVectorMachine)x);
#pragma warning restore 618
                var svm = new MultilabelSupportVectorMachine(svms);
                return svm;
            }
        }

#pragma warning restore 0169
#pragma warning restore 0649
#endif

        #endregion
    }
}
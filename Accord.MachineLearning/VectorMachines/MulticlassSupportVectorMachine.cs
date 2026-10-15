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
    ///     Obsolete. Please use <see cref="MulticlassSupportVectorMachine{TKernel}" /> instead.
    /// </summary>
    [Serializable]
    [Obsolete("Please use MulticlassSupportVectorMachine<TKernel> instead.")]
#if !NETSTANDARD
    [SerializationBinder(typeof(MulticlassSupportVectorMachineBinder))]
#endif
    public class MulticlassSupportVectorMachine :
        MulticlassSupportVectorMachine<IKernel<double[]>>, ICloneable
    {
        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var clone = new MulticlassSupportVectorMachine(NumberOfInputs, Kernel as IKernel, NumberOfOutputs);
            clone.Method = Method;
            for (var i = 0; i < Models.Length; i++)
            {
                for (var j = 0; j < Models[i].Length; j++)
                {
                    clone.Models[i][j] = (SupportVectorMachine<IKernel<double[]>>)Models[i][j].Clone();
                }
            }

            return clone;
        }
#pragma warning disable 0618

        /// <summary>
        ///     Initializes a new instance of the <see cref="MulticlassSupportVectorMachine" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs by the machine.</param>
        /// <param name="classes">The number of classes to be handled by the machine.</param>
        public MulticlassSupportVectorMachine(int inputs, int classes)
            : base(classes, () => new KernelSupportVectorMachine(new Linear(), inputs))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MulticlassSupportVectorMachine" /> class.
        /// </summary>
        /// <param name="inputs">The number of inputs by the machine.</param>
        /// <param name="classes">The number of classes to be handled by the machine.</param>
        /// <param name="kernel">The kernel function to be used in the machine.</param>
        public MulticlassSupportVectorMachine(int inputs, IKernel kernel, int classes)
            : base(classes, () => new KernelSupportVectorMachine(kernel, inputs))
        {
        }

        /// <summary>
        ///     Constructs a new Multi-class Kernel Support Vector Machine
        /// </summary>
        /// <param name="machines">
        ///     The machines to be used in each of the pair-wise class subproblems.
        /// </param>
        public MulticlassSupportVectorMachine(KernelSupportVectorMachine[][] machines)
            : base(machines.Length + 1, () => null)
        {
            NumberOfInputs = machines[0][0].NumberOfInputs;
            for (var i = 0; i < Models.Length; i++)
            {
                for (var j = 0; j < Models[i].Length; j++)
                {
                    Models[i][j] = machines[i][j];
                    if (machines[i][j].NumberOfInputs != NumberOfInputs)
                    {
                        throw new ArgumentException();
                    }
                }
            }
        }
#pragma warning restore 0618

        #region Obsolete

        /// <summary>
        ///     Gets the total number of machines
        ///     in this multi-class classifier.
        /// </summary>
        [Obsolete("Please use the Count property.")]
        public int MachinesCount => Count;

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
        [Obsolete("Please use the Models property instead.")]
        public KernelSupportVectorMachine[][] Machines
        {
            get { return Models.Apply((x, i, j) => (KernelSupportVectorMachine)x); }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(params double[] inputs)
        {
            // Compute using elimination method as default.
            return Compute(inputs, MulticlassComputeMethod.Elimination, out double _);
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="output">
        ///     The output of the machine. If this is a
        ///     <see cref="IsProbabilistic">probabilistic</see> machine, the
        ///     output is the probability of the positive class. If this is
        ///     a standard machine, the output is the distance to the decision
        ///     hyperplane in feature space.
        /// </param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, out double output)
        {
            // Compute using elimination method as default.
            return Compute(inputs, MulticlassComputeMethod.Elimination, out output);
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="output">
        ///     The output of the machine. If this is a
        ///     <see cref="IsProbabilistic">probabilistic</see> machine, the
        ///     output is the probability of the positive class. If this is
        ///     a standard machine, the output is the distance to the decision
        ///     hyperplane in feature space.
        /// </param>
        /// <param name="decisionPath">
        ///     The decision path followed by the Decision
        ///     Directed Acyclic Graph used by the
        ///     <see cref="MulticlassComputeMethod.Elimination">
        ///         elimination
        ///     </see>
        ///     method.
        /// </param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, out double output, out Tuple<int, int>[] decisionPath)
        {
            lock (Models)
            {
                var prev = Method;
                Method = MulticlassComputeMethod.Elimination;
                int decision;
                try
                {
                    output = Probabilities(inputs, out decision)[decision];
                    decisionPath = LastDecisionPath.Apply(x => x.Pair.ToTuple());
                }
                finally
                {
                    Method = prev;
                }

                return decision;
            }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="responses">The model response for each class.</param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, out double[] responses)
        {
            lock (Models)
            {
                var prev = Method;
                Method = MulticlassComputeMethod.Elimination;
                int decision;
                try
                {
                    responses = Probabilities(inputs, out decision);
                }
                finally
                {
                    Method = prev;
                }

                return decision;
            }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="method">
        ///     The
        ///     <see cref="MulticlassComputeMethod">
        ///         multi-class classification method
        ///     </see>
        ///     to use.
        /// </param>
        /// <param name="responses">The model response for each class.</param>
        /// <param name="output">
        ///     The output of the machine. If this is a
        ///     <see cref="IsProbabilistic">probabilistic</see> machine, the
        ///     output is the probability of the positive class. If this is
        ///     a standard machine, the output is the distance to the decision
        ///     hyperplane in feature space.
        /// </param>
        /// <returns>The decision label for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, MulticlassComputeMethod method, out double[] responses, out double output)
        {
            lock (Models)
            {
                var prev = Method;
                Method = method;
                int decision;
                try
                {
                    responses = Probabilities(inputs, out decision);
                }
                finally
                {
                    Method = prev;
                }

                output = responses[decision];
                return decision;
            }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="method">
        ///     The
        ///     <see cref="MulticlassComputeMethod">
        ///         multi-class classification method
        ///     </see>
        ///     to use.
        /// </param>
        /// <param name="responses">The model response for each class.</param>
        /// <returns>The class decision for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, MulticlassComputeMethod method, out double[] responses)
        {
            lock (Models)
            {
                var prev = Method;
                Method = method;
                int decision;
                try
                {
                    responses = Probabilities(inputs, out decision);
                }
                finally
                {
                    Method = prev;
                }

                return decision;
            }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="method">
        ///     The
        ///     <see cref="MulticlassComputeMethod">
        ///         multi-class classification method
        ///     </see>
        ///     to use.
        /// </param>
        /// <param name="output">
        ///     The output of the machine. If this is a
        ///     <see cref="IsProbabilistic">probabilistic</see> machine, the
        ///     output is the probability of the positive class. If this is
        ///     a standard machine, the output is the distance to the decision
        ///     hyperplane in feature space.
        /// </param>
        /// <returns>The class decision for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, MulticlassComputeMethod method, out double output)
        {
            lock (Models)
            {
                var prev = Method;
                Method = method;
                int decision;
                double[] responses;
                try
                {
                    responses = Probabilities(inputs, out decision);
                }
                finally
                {
                    Method = prev;
                }

                output = responses[decision];
                return decision;
            }
        }

        /// <summary>
        ///     Computes the given input to produce the corresponding output.
        /// </summary>
        /// <param name="inputs">An input vector.</param>
        /// <param name="method">
        ///     The
        ///     <see cref="MulticlassComputeMethod">
        ///         multi-class classification method
        ///     </see>
        ///     to use.
        /// </param>
        /// <returns>The class decision for the given input.</returns>
        [Obsolete("Please use the Decide, Distance or Probability methods")]
        public int Compute(double[] inputs, MulticlassComputeMethod method)
        {
            lock (Models)
            {
                var prev = Method;
                Method = method;
                int decision;
                try
                {
                    Probabilities(inputs, out decision);
                }
                finally
                {
                    Method = prev;
                }

                return decision;
            }
        }

        /// <summary>
        ///     Gets whether this machine has been calibrated to
        ///     produce probabilistic outputs (through the Probability(TInput)
        ///     and Probabilities(TInput) methods).
        /// </summary>
        public bool IsProbabilistic => Models[0][0].IsProbabilistic;

#if !NETSTANDARD1_4
        /// <summary>
        ///     Saves the machine to a stream.
        /// </summary>
        /// <param name="stream">The stream to which the machine is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save() instead (or use it as an extension method).")]
        public void Save(Stream stream)
        {
            Serializer.Save(this, stream);
        }

        /// <summary>
        ///     Saves the machine to a file.
        /// </summary>
        /// <param name="path">The path to the file to which the machine is to be serialized.</param>
        [Obsolete("Please use Accord.IO.Serializer.Save() instead (or use it as an extension method).")]
        public void Save(string path)
        {
            Serializer.Save(this, path);
        }

        /// <summary>
        ///     Loads a machine from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the machine is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<MulticlassSupportVectorMachine>(stream) instead.")]
        public static MulticlassSupportVectorMachine Load(Stream stream)
        {
            return Serializer.Load<MulticlassSupportVectorMachine>(stream);
        }

        /// <summary>
        ///     Loads a machine from a file.
        /// </summary>
        /// <param name="path">The path to the file from which the machine is to be deserialized.</param>
        /// <returns>The deserialized machine.</returns>
        [Obsolete("Please use Accord.IO.Serializer.Load<MulticlassSupportVectorMachine>(path) instead.")]
        public static MulticlassSupportVectorMachine Load(string path)
        {
            return Serializer.Load<MulticlassSupportVectorMachine>(path);
        }
#endif

        #endregion

        #region Serialization backwards compatibility

#if !NETSTANDARD
        internal class MulticlassSupportVectorMachineBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
#pragma warning disable 618
                    if (typeName == "Accord.MachineLearning.VectorMachines.MulticlassSupportVectorMachine")
                    {
                        return typeof(MulticlassSupportVectorMachine_2_13);
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
        internal class MulticlassSupportVectorMachine_2_13
        {
            public KernelSupportVectorMachine.KernelSupportVectorMachine_2_13[][] machines;

            public static implicit operator MulticlassSupportVectorMachine(MulticlassSupportVectorMachine_2_13 obj)
            {
#pragma warning disable 618
                var svms = obj.machines.Apply((x, i, j) => (KernelSupportVectorMachine)x);
#pragma warning restore 618
                var svm = new MulticlassSupportVectorMachine(svms);
                return svm;
            }
        }

#pragma warning restore 0169
#pragma warning restore 0649
#endif

        #endregion
    }
}
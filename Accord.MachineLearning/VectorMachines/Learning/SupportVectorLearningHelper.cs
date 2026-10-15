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
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    internal static class SupportVectorLearningHelper
    {
        public static TModel Create<TModel, TInput, TKernel>(int inputs, TKernel kernel)
            where TModel : class, ISupportVectorMachine<TInput>
            where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
            where TInput : ICloneable
#endif
        {
            TModel result = null;
            var type = typeof(TModel);
            if (type == typeof(SupportVectorMachine))
            {
                result = new SupportVectorMachine(inputs) as TModel;
            }

            if (type == typeof(SupportVectorMachine<IKernel>))
            {
                result = new SupportVectorMachine<IKernel>(inputs, kernel as IKernel) as TModel;
            }

            if (type == typeof(SupportVectorMachine<IKernel<double[]>>))
            {
                result = new SupportVectorMachine<IKernel<double[]>>(inputs, kernel as IKernel<double[]>) as TModel;
            }
#if !NETSTANDARD1_4
#pragma warning disable 0618
            else if (type == typeof(KernelSupportVectorMachine))
            {
                result = new KernelSupportVectorMachine(kernel as IKernel, inputs) as TModel;
            }
#pragma warning restore 0618
#endif
            else if (type == typeof(SupportVectorMachine<TKernel, TInput>))
            {
                result = new SupportVectorMachine<TKernel, TInput>(inputs, kernel) as TModel;
            }

            if (result == null)
            {
                throw new NotSupportedException(
                    "If you are implementing your own support vector machine type, please override the Create method in your learning algorithm to instruct the framework how to instantiate a type of your new class.");
            }

            return result;
        }

        public static TModel Create<TModel, TKernel>(int inputs, TKernel kernel)
            where TModel : class, ISupportVectorMachine<double[]>
            where TKernel : IKernel<double[]>
        {
            var result = Create<TModel, double[], TKernel>(inputs, kernel);

            if (result == null)
            {
                throw new NotSupportedException(
                    "If you are implementing your own support vector machine type, please override the Create method in your learning algorithm to instruct the framework how to instantiate a type of your new class.");
            }

            return result;
        }

        public static int GetNumberOfInputs<TKernel, TInput>(TKernel kernel, TInput[] x)
        {
            if (x.Length == 0)
            {
                throw new ArgumentException(
                    "Impossible to determine number of inputs because there are no training samples in this set.");
            }

            var linear = kernel as ILinear<TInput>;
            if (linear != null)
            {
                return linear.GetLength(x);
            }

            return Tools.GetNumberOfInputs(x);
        }

        public static void CheckOutput<TInput>(ISupportVectorMachine<TInput> model)
        {
            if (model.SupportVectors == null)
            {
                throw new Exception();
            }

            if (model.Weights == null)
            {
                throw new Exception();
            }

            if (model.SupportVectors.Length != model.Weights.Length)
            {
                throw new Exception();
            }
        }

        public static TKernel EstimateKernel<TKernel, TInput>(TKernel kernel, TInput[] x)
            where TKernel : IKernel<TInput>
        {
            var estimable = kernel as IEstimable<TInput>;
            if (estimable == null)
            {
                throw new InvalidOperationException("Kernel type does not support estimation.");
            }

            estimable.Estimate(x);
            return (TKernel)estimable;
        }

        public static TKernel CreateKernel<TKernel, TInput>(TInput[] x)
            where TKernel : IKernel<TInput>
        {
            if (!typeof(TKernel).HasDefaultConstructor())
            {
                throw new InvalidOperationException("Please set the kernel function before learning a model.");
            }

            var kernel = Activator.CreateInstance<TKernel>();
            var estimable = kernel as IEstimable<TInput>;
            if (estimable != null)
            {
                estimable.Estimate(x);
                return (TKernel)estimable;
            }

            return kernel;
        }
    }
}
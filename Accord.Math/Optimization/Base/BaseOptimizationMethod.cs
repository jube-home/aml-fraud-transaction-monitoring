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
using System.Threading;
using Accord.Math.Random;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Base class for optimization methods.
    /// </summary>
    public abstract class BaseOptimizationMethod
    {
        private int numberOfVariables;

        private double[] x;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptimizationMethod" /> class.
        /// </summary>
        protected BaseOptimizationMethod()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptimizationMethod" /> class.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        protected BaseOptimizationMethod(int numberOfVariables)
        {
            if (numberOfVariables <= 0)
            {
                throw new ArgumentOutOfRangeException("numberOfVariables");
            }

            NumberOfVariables = numberOfVariables;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptimizationMethod" /> class.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        /// <param name="function">The objective function whose optimum values should be found.</param>
        protected BaseOptimizationMethod(int numberOfVariables, Func<double[], double> function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            NumberOfVariables = numberOfVariables;
            Function = function;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseOptimizationMethod" /> class.
        /// </summary>
        /// <param name="function">The objective function whose optimum values should be found.</param>
        protected BaseOptimizationMethod(NonlinearObjectiveFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            NumberOfVariables = function.NumberOfVariables;
            Function = function.Function;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Gets or sets the function to be optimized.
        /// </summary>
        /// <value>The function to be optimized.</value>
        public Func<double[], double> Function { get; set; }

        /// <summary>
        ///     Gets the number of variables (free parameters)
        ///     in the optimization problem.
        /// </summary>
        /// <value>The number of parameters.</value>
        public virtual int NumberOfVariables
        {
            get => numberOfVariables;
            set
            {
                numberOfVariables = value;
                OnNumberOfVariablesChanged(value);
            }
        }

        /// <summary>
        ///     Gets the current solution found, the values of
        ///     the parameters which optimizes the function.
        /// </summary>
        public double[] Solution
        {
            get => x;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfVariables)
                {
                    throw new DimensionMismatchException("value");
                }

                x = value;
            }
        }

        /// <summary>
        ///     Gets the output of the function at the current <see cref="Solution" />.
        /// </summary>
        public double Value { get; protected set; }

        /// <summary>
        ///     Called when the <see cref="NumberOfVariables" /> property has changed.
        /// </summary>
        protected virtual void OnNumberOfVariablesChanged(int numberOfVariables)
        {
            if (Solution == null || Solution.Length != numberOfVariables)
            {
                Solution = new double[numberOfVariables];
                for (var i = 0; i < Solution.Length; i++)
                {
                    Solution[i] = Generator.Random.NextDouble() * 2 - 1;
                }
            }
        }

        /// <summary>
        ///     Finds the maximum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <param name="values">The initial solution vector to start the search.</param>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        public bool Maximize(double[] values)
        {
            Solution = values;
            return Maximize();
        }

        /// <summary>
        ///     Finds the minimum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <param name="values">The initial solution vector to start the search.</param>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        public bool Minimize(double[] values)
        {
            Solution = values;
            return Minimize();
        }

        /// <summary>
        ///     Finds the maximum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        public virtual bool Maximize()
        {
            if (Function == null)
            {
                throw new InvalidOperationException("function");
            }

            var f = Function;

            Function = x => -f(x);

            var success = Optimize();

            Function = f;

            Value = Function(Solution);

            return success;
        }

        /// <summary>
        ///     Finds the minimum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        public virtual bool Minimize()
        {
            if (Function == null)
            {
                throw new InvalidOperationException("function");
            }

            var success = Optimize();

            Value = Function(Solution);

            return success;
        }

        /// <summary>
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected abstract bool Optimize();

        /// <summary>
        ///     Creates an exception with a given inner optimization algorithm code (for debugging purposes).
        /// </summary>
        protected static ArgumentOutOfRangeException ArgumentException(string paramName, string message, string code)
        {
            var e = new ArgumentOutOfRangeException(paramName, message);
            e.Data["Code"] = code;
            return e;
        }

        /// <summary>
        ///     Creates an exception with a given inner optimization algorithm code (for debugging purposes).
        /// </summary>
        protected static InvalidOperationException OperationException(string message, string code)
        {
            var e = new InvalidOperationException(message);
            e.Data["Code"] = code;
            return e;
        }
    }
}
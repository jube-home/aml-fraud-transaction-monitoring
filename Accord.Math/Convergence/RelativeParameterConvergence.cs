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

namespace Accord.Math
{
    /// <summary>
    ///     Relative parameter change convergence criteria.
    /// </summary>
    /// <remarks>
    ///     This class can be used to track progress and convergence
    ///     of methods which rely on the maximum relative change of
    ///     the values within a parameter vector.
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Converge if the maximum change amongst all parameters is less than 0.1:
    ///   var criteria = new RelativeParameterConvergence(iterations: 0, tolerance: 0.1);
    /// 
    ///   int progress = 1;
    ///   double[] parameters = { 12345.6, 952.12, 1925.1 };
    ///   
    ///   do
    ///   {
    ///       // Do some processing...
    /// 
    ///       // Update current iteration information:
    ///       criteria.NewValues = parameters.Divide(progress++);
    /// 
    ///   } while (!criteria.HasConverged);
    /// 
    /// 
    ///   // The method will converge after reaching the 
    ///   // maximum of 11 iterations with a final value
    ///   // of { 1234.56, 95.212, 192.51 }:
    /// 
    ///   int iterations = criteria.CurrentIteration; // 11
    ///   var v = criteria.OldValues; // { 1234.56, 95.212, 192.51 }
    /// 
    /// </code>
    /// </example>
    public class RelativeParameterConvergence : IConvergence<double[]>
    {
        private int maxIterations = 100;
        private double[] newValues;

        private double tolerance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RelativeParameterConvergence" /> class.
        /// </summary>
        public RelativeParameterConvergence()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RelativeParameterConvergence" /> class.
        /// </summary>
        /// <param name="iterations">
        ///     The maximum number of iterations which should be
        ///     performed by the iterative algorithm. Setting to zero indicates there
        ///     is no maximum number of iterations. Default is 0.
        /// </param>
        /// <param name="tolerance">
        ///     The maximum relative change in the watched value
        ///     after an iteration of the algorithm used to detect convergence.
        ///     Default is 0.
        /// </param>
        public RelativeParameterConvergence(int iterations, double tolerance)
        {
            MaxIterations = iterations;
            this.tolerance = tolerance;
        }

        /// <summary>
        ///     Gets the maximum relative parameter
        ///     change after the last iteration.
        /// </summary>
        public double Delta { get; private set; }

        /// <summary>
        ///     Gets or sets the watched value before the iteration.
        /// </summary>
        public double[] OldValues { get; private set; }

        /// <summary>
        ///     Gets or sets the watched value after the iteration.
        /// </summary>
        public double[] NewValues
        {
            get => newValues;
            set
            {
                OldValues = newValues;
                newValues = (double[])value.Clone();
                CurrentIteration++;
            }
        }

        /// <summary>
        ///     Gets whether the algorithm has diverged.
        /// </summary>
        public bool HasDiverged
        {
            get
            {
                for (var i = 0; i < NewValues.Length; i++)
                {
                    if (double.IsNaN(NewValues[i]) || double.IsInfinity(NewValues[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum change in the watched value
        ///     after an iteration of the algorithm used to detect convergence.
        /// </summary>
        public double Tolerance
        {
            get => tolerance;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Tolerance should be positive.");
                }

                tolerance = value;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the iterative algorithm.
        /// </summary>
        public int MaxIterations
        {
            get => maxIterations;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The maximum number of iterations should be positive.");
                }

                maxIterations = value;
            }
        }

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the current iteration number.
        /// </summary>
        public int CurrentIteration { get; set; }

        /// <summary>
        ///     Gets whether the algorithm has converged.
        /// </summary>
        public bool HasConverged
        {
            get
            {
                if (maxIterations > 0 && CurrentIteration >= maxIterations)
                {
                    return true;
                }

                if (NewValues == null && OldValues == null)
                {
                    return true;
                }

                if (OldValues == null)
                {
                    return false;
                }

                if (NewValues.Length == 0 || OldValues.Length == 0)
                {
                    return true;
                }

                // Check if we have reached an invalid or perfectly separable answer
                for (var i = 0; i < NewValues.Length; i++)
                {
                    if (double.IsNaN(NewValues[i]) || double.IsInfinity(NewValues[i]))
                    {
                        return true;
                    }
                }

                // Update and verify stop criteria
                if (tolerance > 0)
                {
                    // Stopping criteria is likelihood convergence
                    Delta = System.Math.Abs(OldValues[0] - NewValues[0]) / System.Math.Abs(OldValues[0]);

                    if (double.IsNaN(Delta))
                    {
                        Delta = 0;
                    }

                    for (var i = 1; i < OldValues.Length; i++)
                    {
                        var delta = System.Math.Abs(OldValues[i] - NewValues[i]) / System.Math.Abs(OldValues[i]);

                        if (delta > Delta)
                        {
                            Delta = delta;
                        }
                    }

                    if (double.IsNaN(Delta))
                    {
                        return true;
                    }

                    if (Delta <= tolerance)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        public void Clear()
        {
            CurrentIteration = 0;
            newValues = null;
            OldValues = null;
        }

        double[] IConvergence<double[]>.NewValue
        {
            // TODO: Remove this explicit implementation.
            get => NewValues;
            set => NewValues = value;
        }
    }
}
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
using System.Diagnostics;
using Accord.Math;
using Accord.Math.Optimization;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    public abstract partial class BaseSequentialMinimalOptimization<TModel, TKernel, TInput>
    {
        private bool solveSecondOrder(double[] c, TInput[] x, int[] y)
        {
            bool diverged;

            var minusOnes = Vector.Create(Inputs.Length, -1.0);
            Func<int, int[], int, double[], double[]> Q;

            if (kernelCache.Enabled)
            {
                Q = (i, indices, length, row) =>
                {
                    for (var j = 0; j < length; j++)
                    {
                        row[j] = y[i] * y[indices[j]] * kernelCache.GetOrCompute(i, indices[j]);
                    }

                    return row;
                };
            }
            else
            {
                Q = (i, indices, length, row) =>
                {
                    for (var j = 0; j < length; j++)
                    {
                        row[j] = y[i] * y[indices[j]] * Kernel.Function(x[i], x[indices[j]]);
                    }

                    return row;
                };
            }

            var s = new FanChenLinQuadraticOptimization(Lagrange.Length, Q, minusOnes, y)
            {
                Tolerance = tolerance,
                Shrinking = Shrinking,
                Solution = Lagrange,
                Token = Token,
                UpperBounds = c
            };

            diverged = !s.Minimize();

            // Store information about active examples
            for (var i = 0; i < Lagrange.Length; i++)
            {
                if (Lagrange[i] > 0)
                {
                    ActiveExamples.Add(i);
                }
            }

            b_lower = b_upper = s.Rho;

            return diverged;
        }

        private bool solveKeerthi(int samples, double[] c, int[] y)
        {
            var diverged = false;

            if (Shrinking)
            {
                throw new InvalidOperationException(
                    "Shrinking heuristic can only be used if Strategy is set to SelectionStrategy.SecondOrder.");
            }

            initializeKeerthiState(samples, y);

            // Algorithm:
            var numChanged = 0;
            var wholeSetChecks = 0;
            var examineAll = true;
            var shouldStop = false;

            while ((numChanged > 0 || examineAll) && !shouldStop)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                if (examineAll)
                {
                    numChanged = examineAllExamples(samples);

                    wholeSetChecks++;
                }
                else
                {
                    if (Strategy == SelectionStrategy.Sequential)
                    {
                        numChanged = examineNonBoundExamples(c);
                    }
                    else if (Strategy == SelectionStrategy.WorstPair)
                    {
                        takeWorstPairSteps(samples);

                        numChanged = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown strategy");
                    }
                }

                if (examineAll)
                {
                    examineAll = false;
                }

                else if (numChanged == 0)
                {
                    examineAll = true;
                }

                if (wholeSetChecks > maxChecks)
                {
                    shouldStop = diverged = true;
                }

                if (Token.IsCancellationRequested)
                {
                    shouldStop = true;
                }
            }

            return diverged;
        }

        private void initializeKeerthiState(int samples, int[] y)
        {
            // The SMO algorithm chooses to solve the smallest possible optimization problem
            // at every step. At every step, SMO chooses two Lagrange multipliers to jointly
            // optimize, finds the optimal values for these multipliers, and updates the SVM
            // to reflect the new optimal values.
            //
            // Reference: http://research.microsoft.com/en-us/um/people/jplatt/smoTR.pdf

            // The algorithm has been updated to implement the improvements suggested
            // by Keerthi et al. The code has been based on the pseudo-code available
            // on the author's technical report.
            //
            // Reference: http://www.cs.iastate.edu/~honavar/keerthi-svm.pdf


            // Error cache
            errors = new double[samples];

            // [Keerthi] Initialize b_up to -1 and 
            //   i_up to any one index of class 1:
            b_upper = -1;
            i_upper = y.First(y_i => y_i > 0);

            // [Keerthi] Initialize b_low to +1 and 
            //   i_low to any one index of class 2:
            b_lower = +1;
            i_lower = y.First(y_i => y_i < 0);

            // [Keerthi] Set error cache for i_low and i_up:
            errors[i_lower] = +1;
            errors[i_upper] = -1;
        }

        private int examineAllExamples(int samples)
        {
            var numChanged = 0;

            // loop I over all training examples
            for (var i = 0; i < samples; i++)
            {
                if (examineExample(i))
                {
                    numChanged++;
                }
            }

            return numChanged;
        }

        private int examineNonBoundExamples(double[] c)
        {
            var numChanged = 0;

            // loop I over examples not at bounds
            for (var i = 0; i < Lagrange.Length; i++)
            {
                if (Lagrange[i] != 0 && Lagrange[i] != c[i])
                {
                    if (examineExample(i))
                    {
                        numChanged++;
                    }

                    if (b_upper > b_lower - 2.0 * tolerance)
                    {
                        numChanged = 0;
                        break;
                    }
                }
            }

            return numChanged;
        }

        private void takeWorstPairSteps(int samples)
        {
            var attempts = 0;
            do
            {
                attempts++;

                if (!takeStep(i_upper, i_lower))
                {
                    break;
                }

                if (attempts > samples * maxChecks)
                {
                    break;
                }
            } while (b_upper <= b_lower - 2.0 * tolerance);
        }

        private void storeBoundedExamples(double[] c)
        {
            // Store information about bounded examples
            for (var i = 0; i < Lagrange.Length; i++)
            {
                if (Lagrange[i] == c[i])
                {
                    BoundedExamples.Add(i);
                }
            }
        }

        private void storeSupportVectors(TInput[] x, int[] y)
        {
            // Store Support Vectors in the SV Machine. Only vectors which have Lagrange multipliers
            // greater than zero will be stored as only those are actually required during evaluation.

            var activeCount = ActiveExamples.Count;
            Model.SupportVectors = new TInput[activeCount];
            Model.Weights = new double[activeCount];
            var index = 0;
            foreach (var j in ActiveExamples)
            {
                Model.SupportVectors[index] = x[j];
                Model.Weights[index] = Lagrange[j] * y[j];
                index++;
            }
        }

        private void computeBounds(double y1, double y2, double alph1, double alph2, double c1, double c2,
            out double L, out double H)
        {
            // Compute L and H according to equations (13) and (14) (Platt, 1998)
            if (y1 != y2)
            {
                // If the target y1 does not equal the target           (13)
                // y2, then the following bounds apply to a2:
                L = System.Math.Max(0, alph2 - alph1);
                H = System.Math.Min(c2, c1 + alph2 - alph1);
            }
            else
            {
                // If the target y1 does equal the target               (14)
                // y2, then the following bounds apply to a2:
                L = System.Math.Max(0, alph2 + alph1 - c1);
                H = System.Math.Min(c2, alph2 + alph1);
            }
        }

        private double computeUnclippedAlpha2(double alph2, double y2, double e1, double e2, double eta,
            double L, double H)
        {
            double a2;

            if (eta < 0)
            {
                // Under usual circumstances, eta will be negative.
                // Compute as indicated in Platt's SMO book (eq 12.6).

                a2 = alph2 - y2 * (e1 - e2) / eta;

                if (a2 < L)
                {
                    a2 = L;
                }
                else if (a2 > H)
                {
                    a2 = H;
                }
            }
            else
            {
                // Under unusual circumstances, eta could be zero. 
                // In this case, compute the objective function as 
                // suggested in "A Practical SMO Algorithm" by J.
                // Dong, A. Krzyzak and C. Y. Suen.

                //*
                var Lobj = y2 * (e1 - e2) * L;
                var Hobj = y2 * (e1 - e2) * H;

                /*/
                double L1 = alph1 + s * (alph2 - L);
                double H1 = alph1 + s * (alph2 - H);
                double f1 = y1 * e1 - alph1 * k11 - s * alph2 * k12;
                double f2 = y2 * e2 - alph2 * k22 - s * alph1 * k12;
                double Lobj = -0.5 * L1 * L1 * k11 - 0.5 * L * L * k22 - s * L * L1 * k12 - L1 * f1 - L * f2;
                double Hobj = -0.5 * H1 * H1 * k11 - 0.5 * H * H * k22 - s * H * H1 * k12 - H1 * f1 - H * f2;
                //*/

                if (Lobj > Hobj + epsilon)
                {
                    a2 = L;
                }
                else if (Lobj < Hobj - epsilon)
                {
                    a2 = H;
                }
                else
                {
                    a2 = alph2;
                }

                Trace.WriteLine("SMO: eta is zero.");
            }

            return a2;
        }

        private void computeUpdateStep(double alph1, double alph2, double c1, double c2, double s,
            ref double a2, out double a1)
        {
            // Approximate precision as
            // suggested in Platt's errata
            var roundoff = 1e-8;
            if (a2 > c2 - c2 * roundoff)
            {
                a2 = c2;
            }
            else if (a2 < c2 * roundoff)
            {
                a2 = 0;
            }

            // Compute update step
            a1 = alph1 + s * (alph2 - a2);

            if (a1 > c1 - c1 * roundoff)
            {
                a1 = c1;
            }
            else if (a1 < c1 * roundoff)
            {
                a1 = 0;
            }
        }

        private void applyMultipliers(int i1, int i2, double a1, double a2)
        {
            // Update Lagrange multipliers
            Lagrange[i1] = a1;
            Lagrange[i2] = a2;

            // Update indices 
            updateSets(i1, a1);
            updateSets(i2, a2);
        }

        private void updateErrorCache(int i1, int i2, double t1, double t2, double k11, double k12, double k22)
        {
            // Update error cache using new Lagrange multipliers
            foreach (var i in NonBoundExamples) // (alpha[i] > 0 && alpha[i] < c)
            {
                if (i != i1 && i != i2)
                {
                    errors[i] += t1 * kernelCache.GetOrCompute(i1, i) +
                                 t2 * kernelCache.GetOrCompute(i2, i);
                }
            }

            errors[i1] += t1 * k11 + t2 * k12;
            errors[i2] += t1 * k12 + t2 * k22;
        }

        private void updateThresholdBounds(int i1, int i2)
        {
            // Update threshold (bias) to reflect change in Lagrange multipliers
            // [Keerthi] This should be done by computing (i_low, b_low) and (i_up, b_up)
            //   by applying equations (11a) and (11b), using only i1, i2 and indices in
            //   I0 according to item 3 of section 5 from Keerthi's technical report.

            i_lower = i1;
            b_lower = errors[i1];
            i_upper = i1;
            b_upper = errors[i1];

            if (errors[i2] > b_lower)
            {
                i_lower = i2;
                b_lower = errors[i2];
            }

            if (errors[i2] < b_upper)
            {
                i_upper = i2;
                b_upper = errors[i2];
            }

            foreach (var i in NonBoundExamples)
            {
                if (errors[i] > b_lower)
                {
                    i_lower = i;
                    b_lower = errors[i];
                }

                if (errors[i] < b_upper)
                {
                    i_upper = i;
                    b_upper = errors[i];
                }
            }
        }
    }
}
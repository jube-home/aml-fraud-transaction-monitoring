// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Copyright © Jorge Nocedal, 1990
// http://users.eecs.northwestern.edu/~nocedal/
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Math.Optimization
{
    public partial class ConjugateGradient
    {
        private sealed class Session
        {
            private readonly int irest;
            private readonly int method;
            private readonly int n;
            private readonly ConjugateGradient owner;
            private readonly double stp1;
            private readonly double[] x;
            private double betapr;
            private bool bnew;
            private double dg;
            private double dg0;
            private double dgold;
            private double dgout;

            private double f;
            private double f_old;
            private bool finish;
            private double gg;
            private double gg0;
            private double gnorm;
            private int ides;
            private int info;

            private int nfev;
            private int nrst;
            private double stp;
            private double xnorm;

            public Session(ConjugateGradient owner)
            {
                this.owner = owner;

                irest = 1;
                n = owner.NumberOfVariables;
                x = owner.Solution;

                f = owner.Function(x);
                owner.g = owner.Gradient(x);

                method = (int)owner.Method;

                owner.Iterations = 0;
                owner.Evaluations = 1;
                bnew = true;
                nrst = 0;
                owner.Searches = 0; // Number of line search owner.iterations after Wolfe conditions were satisfied.
                dg0 = 0;

                for (var i = 0; i < owner.g.Length; ++i)
                {
                    owner.d[i] = -owner.g[i];
                }

                gnorm = owner.g.Euclidean();
                xnorm = System.Math.Max(1.0, x.Euclidean());
                stp1 = 1.0 / gnorm;
                f_old = f;

                finish = false;
            }

            public bool Run()
            {
                // This code has been adapted from the original
                // FORTRAN function CGFAM by Jorge Nocedal, 1992.

                // Make initial progress report with initialization parameters
                if (owner.Progress != null)
                {
                    owner.Progress(owner, new OptimizationProgressEventArgs
                        (owner.Iterations, owner.Evaluations, owner.g, gnorm, owner.Solution, xnorm, f, stp1, finish));
                }

                // Main iteration
                while (!finish)
                {
                    if (owner.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    owner.Iterations++;
                    nrst++;

                    PrepareLineSearch();

                    if (!AcceptableStep())
                    {
                        return false;
                    }

                    UpdateDirection();

                    CheckTermination();
                }

                return owner.Status == ConjugateGradientCode.Success;
            }

            private void PrepareLineSearch()
            {
                // Call the line search routine of Mor'e and Thuente
                // (modified for Nocedal's CG method) 
                // ------------------------------------------------- 
                //
                //  J.J. Mor'e and D. Thuente, "Linesearch Algorithms with Guaranteed 
                //  Sufficient Decrease". ACM Transactions on Mathematical 
                //  Software 20 (1994), pp 286-307. 
                //

                nfev = 0;
                info = 0;
                dgout = 0;

                // Save original gradient
                for (var i = 0; i < owner.g.Length; i++)
                {
                    owner.gold[i] = owner.g[i];
                }

                dg = owner.d.Dot(owner.g);
                dgold = dg;
                stp = 1.0;

                // Shanno-Phua's formula for trial step
                if (!bnew)
                {
                    stp = dg0 / dg;
                }

                if (owner.Iterations == 1)
                {
                    stp = stp1;
                }

                ides = 0;
                bnew = false;
            }

            private bool AcceptableStep()
            {
                while (true)
                {
                    // Call to the line search subroutine
                    owner.Status = owner.cvsmod(ref f, owner.d, ref stp, ref info, ref nfev, owner.w, ref dg,
                        ref dgout);

                    if (owner.Status != ConjugateGradientCode.Success)
                    {
                        return false;
                    }

                    // Test if descent direction is obtained for methods 2 and 3
                    gg = owner.g.Dot(owner.g);
                    gg0 = owner.g.Dot(owner.gold);
                    betapr = (gg - gg0) / (gnorm * gnorm);

                    // When nrst > n and irest == 1 then restart.
                    if (irest == 1 && nrst > n)
                    {
                        nrst = 0;
                        bnew = true;
                    }
                    else
                    {
                        if (method != 1)
                        {
                            var dg1 = -gg + betapr * dgout;

                            if (dg1 >= 0.0)
                            {
                                ides++;

                                if (ides > 5)
                                {
                                    owner.Status = ConjugateGradientCode.DescentNotObtained;
                                    return false;
                                }

                                continue; // retry
                            }
                        }
                    }

                    return true;
                }
            }

            private void UpdateDirection()
            {
                owner.Evaluations += nfev;
                owner.Searches += ides;

                // Determine correct beta value for method chosen
                var betafr = gg / (gnorm * gnorm);
                double beta = 0;

                if (nrst == 0)
                {
                    beta = 0.0;
                }
                else
                {
                    if (method == 1)
                    {
                        beta = betafr;
                    }
                    else if (method == 2)
                    {
                        beta = betapr;
                    }
                    else if (method == 3)
                    {
                        beta = System.Math.Max(0.0, betapr);
                    }

                    if ((method == 2 || method == 3) && betapr < 0.0)
                    {
                    }
                }

                // Compute the new direction
                for (var i = 0; i < owner.g.Length; i++)
                {
                    owner.d[i] = -owner.g[i] + beta * owner.d[i];
                }

                dg0 = dgold * stp;
            }

            private void CheckTermination()
            {
                // Check for termination
                gnorm = owner.g.Euclidean();
                xnorm = System.Math.Max(1.0, x.Euclidean());

                // Convergence test
                if (gnorm / xnorm <= owner.epsilon)
                {
                    finish = true;
                }

                // Stopping criteria by function delta
                if (owner.Tolerance > 0 && owner.Iterations > 1)
                {
                    var delta = (f_old - f) / f;
                    f_old = f;

                    if (delta < owner.Tolerance)
                    {
                        finish = true;
                    }
                }

                // Stopping criteria by max iterations
                if (owner.MaxIterations > 0)
                {
                    if (owner.Iterations > owner.MaxIterations)
                    {
                        finish = true;
                    }
                }

                if (owner.Progress != null)
                {
                    owner.Progress(owner, new OptimizationProgressEventArgs(owner.Iterations,
                        owner.Evaluations, owner.g, gnorm, owner.Solution, xnorm, f, stp, finish));
                }
            }
        }
    }
}
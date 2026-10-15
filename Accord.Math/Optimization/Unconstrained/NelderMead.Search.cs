// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Steven G. Johnson, 2008
// stevenj@alum.mit.edu
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//
//
// The source code presented in this file has been adapted from the
// Nelder Mead Simplex method implementation presented in the NLopt
// Numerical Optimization Library. Original license is given below.
//
//    Copyright (c) 2007-2011 Massachusetts Institute of Technology
//
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
// 
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
// 
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
//

using System;
using System.Collections.Generic;
using Accord.Collections;

namespace Accord.Math.Optimization
{
    public partial class NelderMead
    {
        private NelderMeadStatus InitializeSimplex(double[] x, int n)
        {
            for (var i = 0; i < n; i++)
            {
                var pt = pts[i + 1];

                Array.Copy(x, pt, x.Length);

                pt[i] += StepSize[i];

                if (pt[i] > UpperBounds[i])
                {
                    if (UpperBounds[i] - x[i] > System.Math.Abs(StepSize[i]) * 0.1)
                    {
                        pt[i] = UpperBounds[i];
                    }
                    else
                    {
                        // ub is too close to pt, go in other direction
                        pt[i] = x[i] - System.Math.Abs(StepSize[i]);
                    }
                }

                if (pt[i] < LowerBounds[i])
                {
                    if (x[i] - LowerBounds[i] > System.Math.Abs(StepSize[i]) * 0.1)
                    {
                        pt[i] = LowerBounds[i];
                    }
                    else
                    {
                        // lb is too close to pt, go in other direction
                        pt[i] = x[i] + System.Math.Abs(StepSize[i]);

                        if (pt[i] > UpperBounds[i])
                        {
                            // go towards further of lb, ub
                            pt[i] = 0.5 * ((UpperBounds[i] - x[i] > x[i] - LowerBounds[i]
                                ? UpperBounds[i]
                                : LowerBounds[i]) + x[i]);
                        }
                    }
                }

                if (close(pt[i], x[i]))
                {
                    return NelderMeadStatus.Failure;
                }

                val[i + 1] = Function(pt);

                var ret = checkeval(pt, val[i + 1]);
                if (ret != NelderMeadStatus.Success)
                {
                    return ret;
                }
            }

            return NelderMeadStatus.Success;
        }

        private NelderMeadStatus? SearchUntilRestart(RedBlackTree<double, double[]> t, int n, double ninv,
            ref double init_diam)
        {
            while (true)
            {
                var low = t.Min();
                var high = t.Max();
                var fl = low.Value.Key;
                var xl = low.Value.Value;
                var fh = high.Value.Key;
                var xh = high.Value.Value;

                Difference = fh - fl;

                if (init_diam == 0)
                {
                    // initialize diam for psi convergence test
                    for (var i = 0; i < n; i++)
                    {
                        init_diam += System.Math.Abs(xl[i] - xh[i]);
                    }
                }

                if (DiameterTolerance <= 0 && nlopt_stop_ftol(Convergence, fl, fh))
                {
                    return NelderMeadStatus.FunctionToleranceReached;
                }

                ComputeCentroid(n, xh, ninv);

                ComputeExtent(n);

                if (HasConverged(n, xl, xh, init_diam))
                {
                    return NelderMeadStatus.SolutionToleranceReached;
                }


                // reflection
                if (!reflectpt(n, xcur, c, alpha, xh, LowerBounds, UpperBounds))
                {
                    return NelderMeadStatus.SolutionToleranceReached;
                }

                var fr = Function(xcur);

                var ret = checkeval(xcur, fr);
                if (ret != NelderMeadStatus.Success)
                {
                    return ret;
                }

                if (fr < fl)
                {
                    var expansion = Expand(n, xh, fr, ref fh);
                    if (expansion.HasValue)
                    {
                        return expansion.Value;
                    }
                }
                else if (fr < t.GetPreviousNode(high).Value.Key)
                {
                    // accept new point
                    Array.Copy(xcur, xh, n);
                    fh = fr;
                }
                else
                {
                    var contraction = Contract(t, n, fr, xl, xh, ref fh, out var restart);
                    if (contraction.HasValue)
                    {
                        return contraction.Value;
                    }

                    if (restart)
                    {
                        return null;
                    }
                }

                high.Value = new KeyValuePair<double, double[]>(fh, high.Value.Value);
                t.Resort(high);
            }
        }

        private bool HasConverged(int n, double[] xl, double[] xh, double init_diam)
        {
            if (DiameterTolerance > 0)
            {
                double diam = 0;
                for (var i = 0; i < n; i++)
                {
                    diam += System.Math.Abs(xl[i] - xh[i]);
                }

                if (diam < DiameterTolerance * init_diam)
                {
                    return true;
                }
            }
            else if (nlopt_stop_xtol(Convergence, c, xcur, n))
            {
                return true;
            }

            return false;
        }

        private void ComputeCentroid(int n, double[] xh, double ninv)
        {
            // compute centroid ... if we cared about the performance of this,
            //   we could do it iteratively by updating the centroid on
            //   each step, but then we would have to be more careful about
            //   accumulation of rounding errors... anyway n is unlikely to
            //   be very large for Nelder-Mead in practical cases

            Array.Clear(c, 0, n);
            for (var i = 0; i < n + 1; i++)
            {
                var xi = pts[i];

                if (xi != xh)
                {
                    for (var j = 0; j < n; ++j)
                    {
                        c[j] += xi[j];
                    }
                }
            }

            for (var i = 0; i < n; i++)
            {
                c[i] *= ninv;
            }
        }

        private void ComputeExtent(int n)
        {
            // x convergence check: find xcur = max radius from centroid
            Array.Clear(xcur, 0, n);

            for (var i = 0; i < n + 1; i++)
            {
                var xi = pts[i];
                for (var j = 0; j < n; j++)
                {
                    var dx = System.Math.Abs(xi[j] - c[j]);

                    if (dx > xcur[j])
                    {
                        xcur[j] = dx;
                    }
                }
            }

            for (var i = 0; i < n; i++)
            {
                xcur[i] += c[i];
            }
        }

        private NelderMeadStatus? Expand(int n, double[] xh, double fr, ref double fh)
        {
            // new best point, expand simplex 
            if (!reflectpt(n, xh, c, gamm, xh, LowerBounds, UpperBounds))
            {
                return NelderMeadStatus.SolutionToleranceReached;
            }

            fh = Function(xh);

            var ret = checkeval(xh, fh);
            if (ret != NelderMeadStatus.Success)
            {
                return ret;
            }

            if (fh >= fr)
            {
                // expanding didn't improve
                fh = fr;
                Array.Copy(xcur, xh, n);
            }

            return null;
        }

        private NelderMeadStatus? Contract(RedBlackTree<double, double[]> t, int n, double fr, double[] xl, double[] xh,
            ref double fh, out bool restart)
        {
            restart = false;

            // new worst point, contract
            if (!reflectpt(n, xcur, c, fh <= fr ? -beta : beta, xh, LowerBounds, UpperBounds))
            {
                return NelderMeadStatus.SolutionToleranceReached;
            }

            var fc = Function(xcur);

            var ret = checkeval(xcur, fc);
            if (ret != NelderMeadStatus.Success)
            {
                return ret;
            }

            if (fc < fr && fc < fh)
            {
                // successful contraction
                Array.Copy(xcur, xh, n);
                fh = fc;
            }
            else
            {
                var shrinking = ShrinkSimplex(t, n, xl);
                if (shrinking.HasValue)
                {
                    return shrinking.Value;
                }

                restart = true;
            }

            return null;
        }

        private NelderMeadStatus? ShrinkSimplex(RedBlackTree<double, double[]> t, int n, double[] xl)
        {
            // failed contraction, shrink simplex
            t.Clear();

            for (var i = 0; i < n + 1; i++)
            {
                var pt = pts[i];

                if (pt != xl)
                {
                    if (!reflectpt(n, pt, xl, -delta, pt, LowerBounds, UpperBounds))
                    {
                        return NelderMeadStatus.SolutionToleranceReached;
                    }

                    val[i] = Function(pt);
                    var ret = checkeval(pt, val[i]);
                    if (ret != NelderMeadStatus.Success)
                    {
                        return ret;
                    }
                }
            }

            return null;
        }
    }
}
// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
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
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

// Contains functions from the Cephes Math Library Release 2.8:
// June, 2000 Copyright 1984, 1987, 1988, 2000 by Stephen L. Moshier
//
// Original license is listed below:
//
//   Some software in this archive may be from the book _Methods and
// Programs for Mathematical Functions_ (Prentice-Hall or Simon & Schuster
// International, 1989) or from the Cephes Mathematical Library, a
// commercial product. In either event, it is copyrighted by the author.
// What you see here may be used freely but it comes with no support or
// guarantee.
//
//   The two known misprints in the book are repaired here in the
// source listings for the gamma function and the incomplete beta
// integral.
//
//
//   Stephen L. Moshier
//   moshier@na-net.ornl.gov
//

using System;

namespace Accord.Math
{
    public static partial class Beta
    {
        private sealed class InverseSolver
        {
            private readonly double aa;
            private readonly double bb;
            private readonly double yy0;

            private double a;
            private double b;
            private double d;
            private double di;
            private int dir;
            private double dithresh;
            private double lgm;
            private bool nflg;
            private bool rflg;
            private double x;
            private double x0;
            private double x1;
            private double y;
            private double y0;
            private double yh;
            private double yl;
            private double yp;

            public InverseSolver(double aa, double bb, double yy0)
            {
                this.aa = aa;
                this.bb = bb;
                this.yy0 = yy0;
            }

            public double Solve()
            {
                var step = Start();

                while (true)
                {
                    switch (step)
                    {
                        case Step.Halve:
                            step = Halve();
                            break;
                        case Step.Newton:
                            step = Newton();
                            break;
                        default:
                            return Done();
                    }
                }
            }

            private Step Start()
            {
                if (aa <= 1.0 || bb <= 1.0)
                {
                    nflg = true;
                    dithresh = 4.0 * Constants.DoubleEpsilon;
                    rflg = false;
                    a = aa;
                    b = bb;
                    y0 = yy0;
                    x = a / (a + b);
                    y = Incomplete(a, b, x);
                    return Step.Halve;
                }

                nflg = false;
                dithresh = 1.0e-4;

                if (ApproximateFromNormal())
                {
                    return Step.Newton;
                }

                return Step.Halve;
            }

            private bool ApproximateFromNormal()
            {
                /* approximation to inverse function */

                yp = -Normal.Inverse(yy0);

                if (yy0 > 0.5)
                {
                    rflg = true;
                    a = bb;
                    b = aa;
                    y0 = 1.0 - yy0;
                    yp = -yp;
                }
                else
                {
                    rflg = false;
                    a = aa;
                    b = bb;
                    y0 = yy0;
                }

                lgm = (yp * yp - 3.0) / 6.0;
                x0 = 2.0 / (1.0 / (2.0 * a - 1.0) + 1.0 / (2.0 * b - 1.0));
                y = yp * System.Math.Sqrt(x0 + lgm) / x0
                    - (1.0 / (2.0 * b - 1.0) - 1.0 / (2.0 * a - 1.0))
                    * (lgm + 5.0 / 6.0 - 2.0 / (3.0 * x0));
                y = 2.0 * y;

                if (y < Constants.LogMin)
                {
                    x0 = 1.0;
                    throw new ArithmeticException("underflow");
                }

                x = a / (a + b * System.Math.Exp(y));
                y = Incomplete(a, b, x);
                yp = (y - y0) / y0;

                if (System.Math.Abs(yp) < 1.0e-2)
                {
                    return true;
                }

                return false;
            }

            private Step Halve()
            {
                /* Resort to interval halving if not close enough */
                x0 = 0.0;
                yl = 0.0;
                x1 = 1.0;
                yh = 1.0;
                di = 0.5;
                dir = 0;

                for (var i = 0; i < 400; i++)
                {
                    var next = HalveIteration(i);

                    if (next.HasValue)
                    {
                        return next.Value;
                    }
                }

                if (x0 >= 1.0)
                {
                    x0 = 1.0 - Constants.DoubleEpsilon;
                    return Step.Done;
                }

                if (x == 0.0)
                {
                    throw new ArithmeticException("underflow");
                }

                return Step.Newton;
            }

            private Step? HalveIteration(int i)
            {
                if (i != 0)
                {
                    if (NextTrialPoint())
                    {
                        return Step.Newton;
                    }
                }

                if (y < y0)
                {
                    if (RaiseLowerBound())
                    {
                        return Step.Halve;
                    }
                }
                else
                {
                    if (LowerUpperBound())
                    {
                        return Step.Done;
                    }
                }

                return null;
            }

            private bool NextTrialPoint()
            {
                x = x0 + di * (x1 - x0);
                if (x == 1.0)
                {
                    x = 1.0 - Constants.DoubleEpsilon;
                }

                y = Incomplete(a, b, x);
                yp = (x1 - x0) / (x1 + x0);
                if (System.Math.Abs(yp) < dithresh)
                {
                    x0 = x;
                    return true;
                }

                return false;
            }

            private bool RaiseLowerBound()
            {
                x0 = x;
                yl = y;
                if (dir < 0)
                {
                    dir = 0;
                    di = 0.5;
                }
                else if (dir > 1)
                {
                    di = 0.5 * di + 0.5;
                }
                else
                {
                    di = (y0 - y) / (yh - yl);
                }

                dir += 1;
                if (x0 > 0.75)
                {
                    if (rflg)
                    {
                        rflg = false;
                        a = aa;
                        b = bb;
                        y0 = yy0;
                    }
                    else
                    {
                        rflg = true;
                        a = bb;
                        b = aa;
                        y0 = 1.0 - yy0;
                    }

                    x = 1.0 - x;
                    y = Incomplete(a, b, x);
                    return true;
                }

                return false;
            }

            private bool LowerUpperBound()
            {
                x1 = x;
                if (rflg && x1 < Constants.DoubleEpsilon)
                {
                    x0 = 0.0;
                    return true;
                }

                yh = y;
                if (dir > 0)
                {
                    dir = 0;
                    di = 0.5;
                }
                else if (dir < -1)
                {
                    di = 0.5 * di;
                }
                else
                {
                    di = (y - y0) / (yh - yl);
                }

                dir -= 1;

                return false;
            }

            private Step Newton()
            {
                if (nflg)
                {
                    return Step.Done;
                }

                x0 = x;
                lgm = Gamma.Log(a + b) - Gamma.Log(a) - Gamma.Log(b);

                for (var i = 0; i < 10; i++)
                {
                    /* Compute the function at this point. */
                    if (i != 0)
                    {
                        y = Incomplete(a, b, x0);
                    }

                    /* Compute the derivative of the function at this point. */
                    d = (a - 1.0) * System.Math.Log(x0) + (b - 1.0) * System.Math.Log(1.0 - x0) + lgm;

                    if (d < Constants.LogMin)
                    {
                        throw new ArithmeticException("underflow");
                    }

                    d = System.Math.Exp(d);

                    /* compute the step to the next approximation of x */
                    d = (y - y0) / d;
                    x = x0;
                    x0 = x0 - d;

                    if (x0 <= 0.0)
                    {
                        throw new ArithmeticException("underflow");
                    }

                    if (x0 >= 1.0)
                    {
                        x0 = 1.0 - Constants.DoubleEpsilon;
                        return Step.Done;
                    }

                    if (System.Math.Abs(d / x0) < 64.0 * Constants.DoubleEpsilon)
                    {
                        return Step.Done;
                    }
                }

                return Step.Done;
            }

            private double Done()
            {
                if (rflg)
                {
                    if (x0 <= double.Epsilon)
                    {
                        x0 = 1.0 - double.Epsilon;
                    }
                    else
                    {
                        x0 = 1.0 - x0;
                    }
                }

                return x0;
            }

            private enum Step
            {
                Halve,
                Newton,
                Done
            }
        }
    }
}
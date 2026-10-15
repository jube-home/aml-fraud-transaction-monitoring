// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Anders Gustafsson, Cureos AB, 2012
// http://www.cureos.com/
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
// This implementation has been based on the original Fortran 77 version of this,
// code, written by Michael Powell (M.J.D.Powell @ damtp.cam.ac.uk); and in the
// Fortran 90 version was by Alan Miller (Alan.Miller @ vic.cmis.csiro.au). 
//

namespace Accord.Math.Optimization
{
    public partial class Cobyla
    {
        private sealed class Solver
        {
            private const double alpha = 0.25;
            private const double beta = 2.1;
            private const double gamma = 0.5;
            private const double delta = 1.1;
            private readonly double[,] a;
            private readonly double[] con;
            private readonly double[,] datmat;
            private readonly double[] dx;
            private readonly int m;
            private readonly int mp;
            private readonly int mpp;
            private readonly int n;
            private readonly int np;

            private readonly Cobyla owner;
            private readonly double[] sigbar;
            private readonly double[,] sim;
            private readonly double[,] simi;
            private readonly double[] veta;
            private readonly double[] vsig;
            private readonly double[] w;
            private readonly double[] x;

            private double f;
            private bool ibrnch;
            private bool iflag;
            private bool ifull;
            private int jdrop;
            private double pareta;
            private double parmu;
            private double parsig;
            private double prerec;
            private double prerem;
            private double resmax;
            private double rho;
            private CobylaStatus status;

            public Solver(Cobyla owner)
            {
                // N.B. Arguments CON, SIM, SIMI, DATMAT, A, VSIG, VETA, SIGBAR, DX, W & IACT
                //      have been removed.

                //     Set the initial values of some parameters. The last column of SIM holds
                //     the optimal vertex of the current simplex, and the preceding N columns
                //     hold the displacements from the optimal vertex to the other vertices.
                //     Further, SIMI holds the inverse of the matrix that is contained in the
                //     first N columns of SIM.

                // Local variables

                this.owner = owner;
                n = owner.NumberOfVariables;
                m = owner.constraints.Length;
                mpp = m + 2;

                x = owner.Solution;

                np = n + 1;
                mp = m + 1;
                rho = owner.RhoBeg;
                parmu = 0.0;

                iflag = false;
                ifull = false;
                parsig = 0.0;
                prerec = 0.0;
                prerem = 0.0;

                con = new double[1 + mpp];
                sim = new double[1 + n, 1 + np];
                simi = new double[1 + n, 1 + n];
                datmat = new double[1 + mpp, 1 + np];
                a = new double[1 + n, 1 + mp];
                vsig = new double[1 + n];
                veta = new double[1 + n];
                sigbar = new double[1 + n];
                dx = new double[1 + n];
                w = new double[1 + n];

                owner.Iterations = 0;
                var temp = 1.0 / rho;

                for (var i = 1; i <= n; ++i)
                {
                    sim[i, np] = owner.Solution[i - 1];
                    sim[i, i] = rho;
                    simi[i, i] = temp;
                }

                jdrop = np;
                ibrnch = false;
            }

            public CobylaStatus Run()
            {
                var step = Step.Evaluate;

                while (true)
                {
                    switch (step)
                    {
                        case Step.Evaluate:
                            step = Evaluate();
                            break;
                        case Step.Refine:
                            step = Refine();
                            break;
                        case Step.AssessTrial:
                            step = AssessTrial();
                            break;
                        case Step.ShrinkOrFinish:
                            step = ShrinkOrFinish();
                            break;
                        case Step.Restore:
                            step = Restore();
                            break;
                        default:
                            return Complete();
                    }
                }
            }

            private Step Evaluate()
            {
                var m = this.m;
                var mpp = this.mpp;
                var mp = this.mp;
                var x = this.x;
                var con = this.con;

                //     Make the next call of the user-supplied subroutine CALCFC. These
                //     instructions are also used for calling CALCFC during the iterations of
                //     the algorithm.


                if (owner.Iterations >= owner.MaxIterations && owner.Iterations > 0)
                {
                    status = CobylaStatus.MaxIterationsReached;
                    return Step.Restore;
                }

                ++owner.Iterations;

                // Compute objective function
                f = owner.Function(x);

                // Compute constraints
                for (var i = 1; i <= m; i++)
                {
                    con[i] = owner.constraints[i - 1].GetViolation(x);
                }

                resmax = 0.0;

                for (var k = 1; k <= m; ++k)
                {
                    resmax = System.Math.Max(resmax, -con[k]);
                }

                con[mp] = f;
                con[mpp] = resmax;

                if (ibrnch)
                {
                    return Step.AssessTrial;
                }

                if (ContinueInitialSimplex())
                {
                    return Step.Evaluate;
                }

                ibrnch = true;

                return Step.Refine;
            }

            private bool ContinueInitialSimplex()
            {
                var n = this.n;
                var mpp = this.mpp;
                var np = this.np;
                var mp = this.mp;
                var x = this.x;
                var con = this.con;
                var sim = this.sim;
                var simi = this.simi;
                var datmat = this.datmat;

                //     Set the recently calculated function values in a column of DATMAT. This
                //     array has a column for each vertex of the current simplex, the entries of
                //     each column being the values of the constraint functions (if any)
                //     followed by the objective function and the greatest constraint violation
                //     at the vertex.

                for (var i = 1; i <= mpp; ++i)
                {
                    datmat[i, jdrop] = con[i];
                }

                if (owner.Iterations <= np)
                {
                    //     Exchange the new vertex of the initial simplex with the optimal vertex if
                    //     necessary. Then, if the initial simplex is not complete, pick its next
                    //     vertex and calculate the function values there.

                    if (jdrop <= n)
                    {
                        if (datmat[mp, np] <= f)
                        {
                            x[jdrop - 1] = sim[jdrop, np];
                        }
                        else
                        {
                            sim[jdrop, np] = x[jdrop - 1];
                            for (var k = 1; k <= mpp; ++k)
                            {
                                datmat[k, jdrop] = datmat[k, np];
                                datmat[k, np] = con[k];
                            }

                            for (var k = 1; k <= jdrop; ++k)
                            {
                                sim[jdrop, k] = -rho;
                                var temp = 0.0;
                                for (var i = k; i <= jdrop; ++i)
                                {
                                    temp -= simi[i, k];
                                }

                                simi[jdrop, k] = temp;
                            }
                        }
                    }

                    if (owner.Iterations <= n)
                    {
                        jdrop = owner.Iterations;
                        x[jdrop - 1] += rho;
                        return true;
                    }
                }

                return false;
            }

            private Step Refine()
            {
                var n = this.n;
                var np = this.np;
                var x = this.x;
                var sim = this.sim;
                var dx = this.dx;

                var nbest = FindBestVertex();

                MoveBestVertexToPole(nbest);

                if (InverseIsInaccurate())
                {
                    status = CobylaStatus.DivergingRoundingErrors;
                    return Step.Restore;
                }

                BuildLinearApproximations();

                ComputeSigmaAndEta();

                //     If a new vertex is needed to improve acceptability, then decide which
                //     vertex to drop from the simplex.

                if (!ibrnch && !iflag)
                {
                    ReplaceVertexForAcceptability();
                    return Step.Evaluate;
                }

                //     Calculate DX = x(*)-x(0).
                //     Branch if the length of DX is less than 0.5*RHO.

                if (TrustRegionStepIsTooShort())
                {
                    return Step.ShrinkOrFinish;
                }

                //     Predict the change to F and the new maximum constraint violation if the
                //     variables are altered from x(0) to x(0) + DX.

                if (PredictReductionsRaisingPenalty())
                {
                    return Step.Refine;
                }

                //     Calculate the constraint and objective functions at x(*).
                //     Then find the actual reduction in the merit function.

                for (var k = 1; k <= n; ++k)
                {
                    x[k - 1] = sim[k, np] + dx[k];
                }

                ibrnch = true;
                return Step.Evaluate;
            }

            private int FindBestVertex()
            {
                var n = this.n;
                var mpp = this.mpp;
                var np = this.np;
                var mp = this.mp;
                var datmat = this.datmat;

                //     Identify the optimal vertex of the current simplex.

                var phimin = datmat[mp, np] + parmu * datmat[mpp, np];
                var nbest = np;

                for (var j = 1; j <= n; ++j)
                {
                    var temp = datmat[mp, j] + parmu * datmat[mpp, j];
                    if (temp < phimin)
                    {
                        nbest = j;
                        phimin = temp;
                    }
                    else if (temp == phimin && parmu == 0.0 && datmat[mpp, j] < datmat[mpp, nbest])
                    {
                        nbest = j;
                    }
                }

                return nbest;
            }

            private void MoveBestVertexToPole(int nbest)
            {
                var n = this.n;
                var mpp = this.mpp;
                var np = this.np;
                var sim = this.sim;
                var simi = this.simi;
                var datmat = this.datmat;

                double temp;

                //     Switch the best vertex into pole position if it is not there already,
                //     and also update SIM, SIMI and DATMAT.

                if (nbest <= n)
                {
                    for (var i = 1; i <= mpp; ++i)
                    {
                        temp = datmat[i, np];
                        datmat[i, np] = datmat[i, nbest];
                        datmat[i, nbest] = temp;
                    }

                    for (var i = 1; i <= n; ++i)
                    {
                        temp = sim[i, nbest];
                        sim[i, nbest] = 0.0;
                        sim[i, np] += temp;

                        var tempa = 0.0;
                        for (var k = 1; k <= n; ++k)
                        {
                            sim[i, k] -= temp;
                            tempa -= simi[k, i];
                        }

                        simi[nbest, i] = tempa;
                    }
                }
            }

            private bool InverseIsInaccurate()
            {
                var n = this.n;
                var sim = this.sim;
                var simi = this.simi;

                //     Make an error return if SIGI is a poor approximation to the inverse of
                //     the leading N by N submatrix of SIG.

                var error = 0.0;
                for (var i = 1; i <= n; ++i)
                {
                    for (var j = 1; j <= n; ++j)
                    {
                        var temp = 0.0;
                        for (var ii = 1; ii <= n; ii++)
                        {
                            temp += simi[i, ii] * sim[ii, j];
                        }

                        if (i == j)
                        {
                            temp -= 1.0;
                        }

                        error = System.Math.Max(error, System.Math.Abs(temp));
                    }
                }

                return error > 0.1;
            }

            private void BuildLinearApproximations()
            {
                var n = this.n;
                var np = this.np;
                var mp = this.mp;
                var con = this.con;
                var simi = this.simi;
                var datmat = this.datmat;
                var a = this.a;
                var w = this.w;

                //     Calculate the coefficients of the linear approximations to the objective
                //     and constraint functions, placing minus the objective function gradient
                //     after the constraint gradients in the array A. The vector W is used for
                //     working space.

                for (var k = 1; k <= mp; ++k)
                {
                    con[k] = -datmat[k, np];
                    for (var j = 1; j <= n; ++j)
                    {
                        w[j] = datmat[k, j] + con[k];
                    }

                    for (var i = 1; i <= n; ++i)
                    {
                        var temp = 0.0;
                        for (var ii = 1; ii <= n; ii++)
                        {
                            temp += w[ii] * simi[ii, i];
                        }

                        a[i, k] = (k == mp ? -1.0 : 1.0) * temp;
                    }
                }
            }

            private void ComputeSigmaAndEta()
            {
                var n = this.n;
                var sim = this.sim;
                var simi = this.simi;
                var vsig = this.vsig;
                var veta = this.veta;

                //     Calculate the values of sigma and eta, and set IFLAG = 0 if the current
                //     simplex is not acceptable.

                iflag = true;
                parsig = alpha * rho;
                pareta = beta * rho;

                for (var j = 1; j <= n; ++j)
                {
                    var wsig = 0.0;
                    for (var k = 1; k <= n; ++k)
                    {
                        wsig += simi[j, k] * simi[j, k];
                    }

                    var weta = 0.0;
                    for (var k = 1; k <= n; ++k)
                    {
                        weta += sim[k, j] * sim[k, j];
                    }

                    vsig[j] = 1.0 / System.Math.Sqrt(wsig);
                    veta[j] = System.Math.Sqrt(weta);

                    if (vsig[j] < parsig || veta[j] > pareta)
                    {
                        iflag = false;
                    }
                }
            }

            private void ReplaceVertexForAcceptability()
            {
                SelectVertexToDrop();

                var dxsign = ChooseStepSign();

                ApplyNewVertex(dxsign);
            }

            private void SelectVertexToDrop()
            {
                var n = this.n;
                var vsig = this.vsig;
                var veta = this.veta;

                jdrop = 0;
                var temp = pareta;
                for (var j = 1; j <= n; ++j)
                {
                    if (veta[j] > temp)
                    {
                        jdrop = j;
                        temp = veta[j];
                    }
                }

                if (jdrop == 0)
                {
                    for (var j = 1; j <= n; ++j)
                    {
                        if (vsig[j] < temp)
                        {
                            jdrop = j;
                            temp = vsig[j];
                        }
                    }
                }
            }

            private double ChooseStepSign()
            {
                var n = this.n;
                var np = this.np;
                var mp = this.mp;
                var simi = this.simi;
                var datmat = this.datmat;
                var a = this.a;
                var vsig = this.vsig;
                var dx = this.dx;

                //     Calculate the step to the new vertex and its sign.
                var temp = gamma * rho * vsig[jdrop];
                for (var k = 1; k <= n; ++k)
                {
                    dx[k] = temp * simi[jdrop, k];
                }

                var cvmaxp = 0.0;
                var cvmaxm = 0.0;

                var total = 0.0;
                for (var k = 1; k <= mp; ++k)
                {
                    total = 0.0;
                    for (var ii = 1; ii <= n; ii++)
                    {
                        total += a[ii, k] * dx[ii];
                    }

                    if (k < mp)
                    {
                        temp = datmat[k, np];
                        cvmaxp = System.Math.Max(cvmaxp, -total - temp);
                        cvmaxm = System.Math.Max(cvmaxm, total - temp);
                    }
                }

                var dxsign = parmu * (cvmaxp - cvmaxm) > 2.0 * total ? -1.0 : 1.0;
                return dxsign;
            }

            private void ApplyNewVertex(double dxsign)
            {
                var n = this.n;
                var np = this.np;
                var x = this.x;
                var sim = this.sim;
                var simi = this.simi;
                var dx = this.dx;

                var temp =
                    //     Update the elements of SIM and SIMI, and set the next X.
                    0.0;
                for (var i = 1; i <= n; ++i)
                {
                    dx[i] = dxsign * dx[i];
                    sim[i, jdrop] = dx[i];
                    temp += simi[jdrop, i] * dx[i];
                }

                for (var k = 1; k <= n; ++k)
                {
                    simi[jdrop, k] /= temp;
                }

                for (var j = 1; j <= n; ++j)
                {
                    if (j != jdrop)
                    {
                        temp = 0.0;
                        for (var ii = 1; ii <= n; ii++)
                        {
                            temp += simi[j, ii] * dx[ii];
                        }

                        for (var k = 1; k <= n; ++k)
                        {
                            simi[j, k] -= temp * simi[jdrop, k];
                        }
                    }

                    x[j - 1] = sim[j, np] + dx[j];
                }
            }

            private bool TrustRegionStepIsTooShort()
            {
                var n = this.n;
                var m = this.m;
                var con = this.con;
                var a = this.a;
                var dx = this.dx;

                trstlp(n, m, a, con, rho, dx, out ifull);

                if (!ifull)
                {
                    var temp = 0.0;
                    for (var k = 1; k <= n; ++k)
                    {
                        temp += dx[k] * dx[k];
                    }

                    if (temp < 0.25 * rho * rho)
                    {
                        ibrnch = true;
                        return true;
                    }
                }

                return false;
            }

            private bool PredictReductionsRaisingPenalty()
            {
                var n = this.n;
                var mpp = this.mpp;
                var np = this.np;
                var mp = this.mp;
                var con = this.con;
                var datmat = this.datmat;
                var a = this.a;
                var dx = this.dx;

                var total = 0.0;
                var resnew = 0.0;
                con[mp] = 0.0;

                for (var k = 1; k <= mp; ++k)
                {
                    total = 0.0;
                    for (var ii = 1; ii <= n; ii++)
                    {
                        total += a[ii, k] * dx[ii];
                    }

                    total = con[k] - total;

                    if (k < mp)
                    {
                        resnew = System.Math.Max(resnew, total);
                    }
                }

                //     Increase PARMU if necessary and branch back if this change alters the
                //     optimal vertex. Otherwise PREREM and PREREC will be set to the predicted
                //     reductions in the merit function and the maximum constraint violation
                //     respectively.

                prerec = datmat[mpp, np] - resnew;
                var barmu = prerec > 0.0 ? total / prerec : 0.0;

                if (parmu < 1.5 * barmu)
                {
                    parmu = 2.0 * barmu;

                    var phi = datmat[mp, np] + parmu * datmat[mpp, np];

                    for (var j = 1; j <= n; ++j)
                    {
                        var temp = datmat[mp, j] + parmu * datmat[mpp, j];
                        if (temp < phi ||
                            (temp == phi && parmu == 0.0 && datmat[mpp, j] < datmat[mpp, np]))
                        {
                            return true;
                        }
                    }
                }

                prerem = parmu * prerec - total;

                return false;
            }

            private Step AssessTrial()
            {
                var trured = ReductionAchieved();

                ChooseVertexByRatio(trured);

                ChooseVertexByEdgeLength(trured);

                if (jdrop == 0)
                {
                    return Step.ShrinkOrFinish;
                }

                ReviseSimplex();

                //     Branch back for further iterations with the current RHO.

                if (trured > 0.0 && trured >= 0.1 * prerem)
                {
                    return Step.Refine;
                }

                return Step.ShrinkOrFinish;
            }

            private double ReductionAchieved()
            {
                var mpp = this.mpp;
                var np = this.np;
                var mp = this.mp;
                var datmat = this.datmat;

                var vmold = datmat[mp, np] + parmu * datmat[mpp, np];
                var vmnew = f + parmu * resmax;
                var trured = vmold - vmnew;

                if (parmu == 0.0 && f == datmat[mp, np])
                {
                    prerem = prerec;
                    trured = datmat[mpp, np] - resmax;
                }

                return trured;
            }

            private void ChooseVertexByRatio(double trured)
            {
                var n = this.n;
                var simi = this.simi;
                var vsig = this.vsig;
                var sigbar = this.sigbar;
                var dx = this.dx;

                //     Begin the operations that decide whether x(*) should replace one of the
                //     vertices of the current simplex, the change being mandatory if TRURED is
                //     positive. Firstly, JDROP is set to the index of the vertex that is to be
                //     replaced.

                var ratio = trured <= 0.0 ? 1.0 : 0.0;

                jdrop = 0;

                for (var j = 1; j <= n; ++j)
                {
                    var temp = 0.0;
                    for (var ii = 1; ii <= n; ii++)
                    {
                        temp += simi[j, ii] * dx[ii];
                    }

                    temp = System.Math.Abs(temp);

                    if (temp > ratio)
                    {
                        jdrop = j;
                        ratio = temp;
                    }

                    sigbar[j] = temp * vsig[j];
                }
            }

            private void ChooseVertexByEdgeLength(double trured)
            {
                var n = this.n;
                var sim = this.sim;
                var vsig = this.vsig;
                var veta = this.veta;
                var sigbar = this.sigbar;
                var dx = this.dx;

                //     Calculate the value of ell.

                var edgmax = delta * rho;
                var l = 0;

                for (var j = 1; j <= n; ++j)
                {
                    if (sigbar[j] >= parsig || sigbar[j] >= vsig[j])
                    {
                        var temp = veta[j];

                        if (trured > 0.0)
                        {
                            temp = 0.0;
                            for (var k = 1; k <= n; ++k)
                            {
                                temp += System.Math.Pow(dx[k] - sim[k, j], 2.0);
                            }

                            temp = System.Math.Sqrt(temp);
                        }

                        if (temp > edgmax)
                        {
                            l = j;
                            edgmax = temp;
                        }
                    }
                }

                if (l > 0)
                {
                    jdrop = l;
                }
            }

            private void ReviseSimplex()
            {
                var n = this.n;
                var mpp = this.mpp;
                var con = this.con;
                var sim = this.sim;
                var simi = this.simi;
                var datmat = this.datmat;
                var dx = this.dx;

                var temp =
                    //     Revise the simplex by updating the elements of SIM, SIMI and DATMAT.
                    0.0;
                for (var i = 1; i <= n; ++i)
                {
                    sim[i, jdrop] = dx[i];
                    temp += simi[jdrop, i] * dx[i];
                }

                for (var k = 1; k <= n; ++k)
                {
                    simi[jdrop, k] /= temp;
                }

                for (var j = 1; j <= n; ++j)
                {
                    if (j != jdrop)
                    {
                        temp = 0.0;
                        for (var ii = 1; ii <= n; ii++)
                        {
                            temp += simi[j, ii] * dx[ii];
                        }

                        for (var k = 1; k <= n; ++k)
                        {
                            simi[j, k] -= temp * simi[jdrop, k];
                        }
                    }
                }

                for (var k = 1; k <= mpp; ++k)
                {
                    datmat[k, jdrop] = con[k];
                }
            }

            private Step ShrinkOrFinish()
            {
                if (!iflag)
                {
                    ibrnch = false;
                    return Step.Refine;
                }

                //     Otherwise reduce RHO if it is not at its least value and reset PARMU.

                if (rho > owner.RhoEnd)
                {
                    ReduceRho();
                    return Step.Refine;
                }

                //     Return the best calculated values of the variables.

                status = CobylaStatus.Success;

                if (ifull)
                {
                    return Step.Complete;
                }

                return Step.Restore;
            }

            private void ReduceRho()
            {
                var n = this.n;
                var m = this.m;
                var np = this.np;
                var mp = this.mp;
                var datmat = this.datmat;

                double cmin = 0.0, cmax = 0.0;

                rho *= 0.5;
                if (rho <= 1.5 * owner.RhoEnd)
                {
                    rho = owner.RhoEnd;
                }

                if (parmu > 0.0)
                {
                    var denom = 0.0;

                    for (var k = 1; k <= mp; ++k)
                    {
                        cmin = datmat[k, np];
                        cmax = cmin;

                        for (var i = 1; i <= n; ++i)
                        {
                            cmin = System.Math.Min(cmin, datmat[k, i]);
                            cmax = System.Math.Max(cmax, datmat[k, i]);
                        }

                        if (k <= m && cmin < 0.5 * cmax)
                        {
                            var temp = System.Math.Max(cmax, 0.0) - cmin;
                            denom = denom <= 0.0 ? temp : System.Math.Min(denom, temp);
                        }
                    }

                    if (denom == 0.0)
                    {
                        parmu = 0.0;
                    }
                    else if (cmax - cmin < parmu * denom)
                    {
                        parmu = (cmax - cmin) / denom;
                    }
                }
            }

            private Step Restore()
            {
                var n = this.n;
                var mpp = this.mpp;
                var np = this.np;
                var mp = this.mp;
                var x = this.x;
                var sim = this.sim;
                var datmat = this.datmat;

                for (var k = 1; k <= n; ++k)
                {
                    x[k - 1] = sim[k, np];
                }

                f = datmat[mp, np];
                resmax = datmat[mpp, np];

                return Step.Complete;
            }

            private CobylaStatus Complete()
            {
                var x = this.x;

                owner.MaxIterations = owner.Iterations;

                if (status == CobylaStatus.Success)
                {
                    // Check if all constraints have been fulfilled
                    for (var i = 0; i < owner.constraints.Length; i++)
                    {
                        if (owner.constraints[i].IsViolated(x))
                        {
                            status = CobylaStatus.NoPossibleSolution;
                        }
                    }
                }

                return status;
            }

            private enum Step
            {
                Evaluate,
                Refine,
                AssessTrial,
                ShrinkOrFinish,
                Restore,
                Complete
            }
        }
    }
}
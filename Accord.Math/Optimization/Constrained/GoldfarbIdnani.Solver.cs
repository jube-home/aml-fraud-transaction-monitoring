// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © 1995, 1996, 1997, 1998
// Berwin A. Turlach <bturlach@stats.adelaide.edu.au>
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
// This work is based on the original Fortran implementation by Berwin Turlach,
// also shared under the LGPL license.
//

using System;

namespace Accord.Math.Optimization
{
    public partial class GoldfarbIdnani
    {
        private sealed class QuadraticProgramSolver
        {
            private readonly double[,] amat;
            private readonly double[] bvec;
            private readonly double[] constraintTolerances;
            private readonly double[,] dmat;
            private readonly double[] dvec;
            private readonly int[] iact;
            private readonly double[] iwnbv;
            private readonly double[] iwrm;
            private readonly double[] iwrv;
            private readonly double[] iwsv;
            private readonly double[] iwuv;
            private readonly double[] iwzv;
            private readonly int meq;
            private readonly int n;

            private readonly GoldfarbIdnani owner;
            private readonly double[] sol;
            private readonly double[] work;

            private double f;
            public int ierr;
            private int it1;

            public int nact;
            private int nvl;
            private double t1;
            private bool t1inf;

            public QuadraticProgramSolver(GoldfarbIdnani owner, double[,] dmat, double[] dvec, int[] iact, int ierr)
            {
                this.owner = owner;
                this.dmat = dmat;
                this.dvec = dvec;
                this.iact = iact;
                this.ierr = ierr;

                n = owner.NumberOfVariables;
                meq = owner.NumberOfEqualities;
                amat = (double[,])owner.ConstraintMatrix.Clone();
                bvec = (double[])owner.constraintValues.Clone();
                sol = owner.Solution;

                work = owner.work;
                iwzv = owner.iwzv;
                iwrv = owner.iwrv;
                iwuv = owner.iwuv;
                iwrm = owner.iwrm;
                iwsv = owner.iwsv;
                iwnbv = owner.iwnbv;
                constraintTolerances = owner.constraintTolerances;

                f = 0;
                nact = 0;
            }

            public void Run()
            {
                if (!Initialize())
                {
                    return;
                }

                var step = QpStep.NewIteration;

                while (step != QpStep.Finished)
                {
                    switch (step)
                    {
                        case QpStep.NewIteration:
                            step = NewIteration();
                            break;
                        case QpStep.Direction:
                            step = Direction();
                            break;
                        default:
                            step = Drop();
                            break;
                    }
                }
            }

            private bool Initialize()
            {
                // Store the initial dvec to calculate below the
                //  unconstrained minima of the critical value.

                Array.Clear(iwzv, 0, iwzv.Length);
                Array.Clear(iwrv, 0, iwrv.Length);
                Array.Clear(iwuv, 0, iwuv.Length);
                Array.Clear(iwrm, 0, iwrm.Length);
                Array.Clear(iwsv, 0, iwsv.Length);
                Array.Clear(iwnbv, 0, iwnbv.Length);

                for (var i = 0; i < dvec.Length; i++)
                {
                    work[i] = dvec[i];
                }

                for (var i = 0; i < iact.Length; i++)
                {
                    iact[i] = -1;
                }

                if (!ComputeUnconstrainedSolution())
                {
                    return false;
                }

                // Set upper triangular of dmat to zero, store dvec in sol and 
                //   calculate value of the criterion at unconstrained minima

                f = 0.0;

                // calculate some constants, i.e., from which index on 
                // the different quantities are stored in the work matrix 

                for (var j = 0; j < sol.Length; j++)
                {
                    sol[j] = dvec[j];
                    f += work[j] * sol[j];
                    work[j] = 0.0;

                    for (var i = j + 1; i < n; i++)
                    {
                        dmat[j, i] = 0.0;
                    }
                }

                f = -f / 2.0;
                ierr = 0;


                // calculate the norm of each column of the A matrix 

                for (var i = 0; i < iwnbv.Length; i++)
                {
                    var sum = 0.0;
                    for (var j = 0; j < n; j++)
                    {
                        sum += amat[i, j] * amat[i, j];
                    }

                    iwnbv[i] = System.Math.Sqrt(sum);
                }

                nact = 0;
                owner.Iterations = 0;
                owner.Deletions = 0;
                return true;
            }

            private bool ComputeUnconstrainedSolution()
            {
                // Get the initial solution
                if (ierr == 0)
                {
                    // L'L = chol(D)
                    var success = owner.dpofa(dmat);

                    if (!success)
                    {
                        ierr = 2;
                        return false;
                    }

                    // L*x = d
                    owner.dposl(dmat, dvec);

                    // D = inv(L)
                    owner.dpori(dmat);
                }
                else
                {
                    // Matrix D is already factorized, so we have to multiply d first with 
                    // R^-T and then with R^-1.  R^-1 is stored in the upper half of the 
                    // array dmat. 

                    for (var j = 0; j < sol.Length; j++)
                    {
                        sol[j] = 0.0;

                        for (var i = 0; i < j; i++)
                        {
                            sol[j] += dmat[j, i] * dvec[i];
                        }
                    }

                    for (var j = 0; j < dvec.Length; j++)
                    {
                        dvec[j] = 0.0;

                        for (var i = j; i < sol.Length; i++)
                        {
                            dvec[j] += dmat[i, j] * sol[i];
                        }
                    }
                }

                return true;
            }

            private QpStep NewIteration()
            {
                // start a new iteration

                if (owner.Token.IsCancellationRequested)
                {
                    return QpStep.Finished;
                }

                owner.Iterations++;

                if (owner.MaxIterations > 0 && owner.Iterations > owner.MaxIterations)
                {
                    return QpStep.Finished;
                }

                CalculateViolations();

                // as safeguard against rounding errors set 
                // already active constraints explicitly to zero 

                for (var i = 0; i < nact; i++)
                {
                    iwsv[iact[i]] = 0.0;
                }

                SelectViolatedConstraint();

                if (nvl == -1)
                {
                    return QpStep.Finished;
                }

                return QpStep.Direction;
            }

            private void CalculateViolations()
            {
                // calculate all constraints and check which are still violated 
                // for the equality constraints we have to check whether the normal 
                // vector has to be negated (as well as bvec in that case) 

                var l = 0;

                for (var i = 0; i < bvec.Length; i++)
                {
                    var sum = -bvec[i];

                    for (var j = 0; j < sol.Length; j++)
                    {
                        sum += amat[i, j] * sol[j];
                    }

                    if (System.Math.Abs(sum) < Constants.DoubleEpsilon)
                    {
                        sum = 0.0;
                    }

                    if (double.IsNaN(sum))
                    {
                        sum = 0.0;
                    }

                    if (i >= meq)
                    {
                        // this is an inequality constraint
                        iwsv[l] = sum;
                    }
                    else
                    {
                        // this is an equality constraint
                        iwsv[l] = -System.Math.Abs(sum);

                        if (sum > 0.0)
                        {
                            for (var j = 0; j < n; j++)
                            {
                                amat[i, j] = -amat[i, j];
                            }

                            bvec[i] = -bvec[i];
                        }
                    }

                    l++;
                }
            }

            private void SelectViolatedConstraint()
            {
                // We weight each violation by the number of non-zero elements in the 
                // corresponding row of A. then we choose the violated constraint which 
                // has maximal absolute value, i.e., the minimum. By obvious commenting
                // and uncommenting we can choose the strategy to take always the first
                // constraint which is violated. ;-) 

                nvl = -1;
                var temp = 0.0;
                for (var i = 0; i < iwnbv.Length; i++)
                {
                    var w = temp * iwnbv[i];
                    var tol = constraintTolerances[i];

                    if (iwsv[i] < w - tol)
                    {
                        nvl = i;
                        temp = iwsv[i] / iwnbv[i];
                    }

                    // if (work(iwsv+i) .LT. 0.d0) then 
                    //     nvl = i 
                    //     goto 72 
                    // endif 
                }
            }

            private QpStep Direction()
            {
                ComputeDirection();

                FindPartialStepLength();

                // test if the z vector is equal to zero 
                var sum = 0.0;
                for (var i = 0; i < iwzv.Length; i++)
                {
                    sum += iwzv[i] * iwzv[i];
                }

                if (System.Math.Abs(sum) <= Constants.DoubleEpsilon)
                {
                    // No step in primal space such that the new constraint becomes 
                    // feasible. Take step in dual space and drop a constant. 

                    if (t1inf)
                    {
                        // No step in dual space possible 
                        // either, problem is not solvable
                        ierr = 1;
                        return QpStep.Finished;
                    }
                    // we take a partial step in dual space and drop constraint it1, 
                    // that is, we drop the it1-th active constraint. 
                    // then we continue at step 2(a) (marked by label 55) 

                    for (var i = 0; i < nact; i++)
                    {
                        iwuv[i] -= t1 * iwrv[i];
                    }

                    iwuv[nact] += t1;
                    return QpStep.Drop;
                }

                return TakeStep();
            }

            private void ComputeDirection()
            {
                // calculate d=J^Tn^+ where n^+ is the normal vector of the violated 
                // constraint. J is stored in dmat in this implementation!! 
                // if we drop a constraint, we have to jump back here. 

                for (var i = 0; i < work.Length; i++)
                {
                    var sum = 0.0;
                    for (var j = 0; j < n; j++)
                    {
                        sum += dmat[i, j] * amat[nvl, j];
                    }

                    work[i] = sum;
                }

                // Now calculate z = J_2 d_2 ...

                for (var i = 0; i < iwzv.Length; i++)
                {
                    iwzv[i] = 0.0;
                }

                for (var j = nact; j < work.Length; j++)
                {
                    for (var i = 0; i < iwzv.Length; i++)
                    {
                        iwzv[i] += dmat[j, i] * work[j];
                    }
                }
            }

            private void FindPartialStepLength()
            {
                // ... and r = R^{-1} d_1, check also if r has positive elements
                // (among the entries corresponding to inequalities constraints). 
                it1 = 0;
                t1 = 0;
                t1inf = true;

                for (var i = nact - 1; i >= 0; i--)
                {
                    var sum = work[i];
                    var l = (i + 1) * (i + 4) / 2 - 1;
                    var l1 = l - i - 1;

                    for (var j = i + 1; j < nact; j++)
                    {
                        sum -= iwrm[l] * iwrv[j];
                        l += j + 1;
                    }

                    sum /= iwrm[l1];

                    iwrv[i] = sum;

                    if (iact[i] < meq)
                    {
                        continue;
                    }

                    if (sum <= 0.0)
                    {
                        continue;
                    }

                    if (double.IsNaN(sum))
                    {
                        continue;
                    }

                    t1inf = false;
                    it1 = i + 1;
                }


                // if r has positive elements, find the partial step length t1, which is 
                // the maximum step in dual space without violating dual feasibility. 
                // it1 stores in which component t1, the min of u/r, occurs. 

                if (!t1inf)
                {
                    t1 = iwuv[it1 - 1] / iwrv[it1 - 1];

                    for (var i = 0; i < nact; i++)
                    {
                        if (iact[i] < meq)
                        {
                            continue;
                        }

                        if (iwrv[i] <= 0.0)
                        {
                            continue;
                        }

                        var temp = iwuv[i] / iwrv[i];

                        if (temp < t1)
                        {
                            t1 = temp;
                            it1 = i + 1;
                        }
                    }
                }
            }

            private QpStep TakeStep()
            {
                // compute full step length t2, minimum step in primal space such that 
                // the constraint becomes feasible. 
                // keep sum (which is z^Tn^+) to update crval below! 
                var sum = 0.0;
                for (var i = 0; i < iwzv.Length; i++)
                {
                    sum += iwzv[i] * amat[nvl, i];
                }

                var tt = -iwsv[nvl] / sum;
                var t2min = true;

                if (!t1inf)
                {
                    if (t1 < tt)
                    {
                        tt = t1;
                        t2min = false;
                    }
                }

                // take step in primal and dual space 
                for (var i = 0; i < sol.Length; i++)
                {
                    sol[i] += tt * iwzv[i];
                }

                f += tt * sum * (tt / 2.0 + iwuv[nact]);

                for (var i = 0; i < nact; i++)
                {
                    iwuv[i] -= tt * iwrv[i];
                }

                iwuv[nact] += tt;

                // if it was a full step, then we check whether further constraints are 
                // violated otherwise we can drop the current constraint and iterate once 
                // more 
                if (t2min)
                {
                    // we took a full step. Thus add constraint nvl to the list of active 
                    // constraints and update J and R 

                    iact[nact++] = nvl;

                    AddActiveConstraint();

                    return QpStep.NewIteration;
                }

                // We took a partial step in dual space. Thus drop constraint it1, 
                // that is, we drop the it1-th active constraint. Then we continue
                // at step 2(a) (marked by label 55) but since the fit changed, we
                // have to recalculate now "how much" the fit violates the chosen
                // constraint now. 

                ReviseViolation();

                return QpStep.Drop;
            }

            private void AddActiveConstraint()
            {
                var l =
                    // to update R we have to put the first nact-1 components of the d vector 
                    // into column (nact) of R 
                    (nact - 1) * nact / 2;
                for (var i = 0; i < nact - 1; i++, l++)
                {
                    iwrm[l] = work[i];
                }

                // if now nact=n, then we just have to add the last element to the new 
                // row of R. 

                // Otherwise we use Givens transformations to turn the vector d(nact:n) 
                // into a multiple of the first unit vector. That multiple goes into the 
                // last element of the new row of R and J is accordingly updated by the 
                // Givens transformations. 

                if (nact == n)
                {
                    iwrm[l] = work[n - 1];
                }
                else
                {
                    RotateDirectionIntoLastColumn(l);
                }
            }

            private void RotateDirectionIntoLastColumn(int l)
            {
                for (var i = n - 1; i >= nact; i--)
                {
                    // We have to find the Givens rotation which will reduce the element 
                    // (l1) of d to zero. If it is already zero we don't have to do anything,
                    // except of decreasing l1 

                    if (work[i] == 0.0)
                    {
                        continue;
                    }

                    var gc = System.Math.Max(System.Math.Abs(work[i - 1]), System.Math.Abs(work[i]));
                    var gs = System.Math.Min(System.Math.Abs(work[i - 1]), System.Math.Abs(work[i]));
                    var temp = Special.Sign(gc * System.Math.Sqrt(1.0 + gs * gs / (gc * gc)), work[i - 1]);
                    gc = work[i - 1] / temp;
                    gs = work[i] / temp;

                    // The Givens rotation is done with the matrix (gc gs, gs -gc). If
                    // gc is one, then element (i) of d is zero compared with element 
                    // (l1-1). Hence we don't have to do anything. If gc is zero, then
                    // we just have to switch column (i) and column (i-1) of J. Since 
                    // we only switch columns in J, we have to be careful how we update
                    // d depending on the sign of gs. Otherwise we have to apply the
                    // Givens rotation to these columns. The i-1 element of d has to be
                    // updated to temp. 

                    if (gc == 1.0)
                    {
                        continue;
                    }

                    if (gc == 0.0)
                    {
                        work[i - 1] = gs * temp;

                        for (var j = 0; j < n; j++)
                        {
                            temp = dmat[i - 1, j];
                            dmat[i - 1, j] = dmat[i, j];
                            dmat[i, j] = temp;
                        }
                    }
                    else
                    {
                        work[i - 1] = temp;
                        var nu = gs / (gc + 1.0);

                        for (var j = 0; j < n; j++)
                        {
                            temp = gc * dmat[i - 1, j] + gs * dmat[i, j];
                            dmat[i, j] = nu * (dmat[i - 1, j] + temp) - dmat[i, j];
                            dmat[i - 1, j] = temp;
                        }
                    }
                }

                // l is still pointing to element (nact,nact) of
                // the matrix R. So store d(nact) in R(nact,nact) 
                iwrm[l] = work[nact - 1];
            }

            private void ReviseViolation()
            {
                var sum = -bvec[nvl];

                for (var j = 0; j < sol.Length; j++)
                {
                    sum += sol[j] * amat[nvl, j];
                }

                if (nvl + 1 > meq)
                {
                    iwsv[nvl] = sum;
                }
                else
                {
                    iwsv[nvl] = -System.Math.Abs(sum);

                    if (sum > 0.0)
                    {
                        for (var j = 0; j < n; j++)
                        {
                            amat[nvl, j] = -amat[nvl, j];
                        }

                        bvec[nvl] = -bvec[nvl];
                    }
                }
            }

            private QpStep Drop()
            {
                // Drop constraint it1

                // if it1 = nact it is only necessary
                // to update the vector u and nact

                if (it1 != nact)
                {
                    do
                    {
                        // After updating one row of R (column of J) we will also come back here

                        var l = it1 * (it1 + 1) / 2;

                        EliminateRowOfR(l);

                        ShiftColumnOfR(l);
                    } while (it1 < nact);
                }

                iwuv[nact - 1] = iwuv[nact];
                iwuv[nact] = 0.0;
                iact[nact - 1] = -1;

                nact--;
                owner.Deletions++;

                return QpStep.Direction;
            }

            private void EliminateRowOfR(int l)
            {
                var l1 =
                    // We have to find the Givens rotation which will reduce the element
                    // (it1+1,it1+1) of R to zero. If it is already zero we don't have to
                    // do anything except of updating u, iact, and shifting column (it1+1)
                    // of R to column (it1). Then l  will point to element (1,it1+1) of R 
                    // and l1 will point to element (it1+1,it1+1) of R.
                    l + it1;

                if (iwrm[l1] == 0.0)
                {
                    return;
                }

                var gc = System.Math.Max(System.Math.Abs(iwrm[l1 - 1]), System.Math.Abs(iwrm[l1]));
                var gs = System.Math.Min(System.Math.Abs(iwrm[l1 - 1]), System.Math.Abs(iwrm[l1]));
                var temp = Special.Sign(gc * System.Math.Sqrt(1.0 + gs * gs / (gc * gc)), iwrm[l1 - 1]);
                gc = iwrm[l1 - 1] / temp;
                gs = iwrm[l1] / temp;


                // The Givens rotation is done with the matrix (gc gs, gs -gc). If gc is
                // one, then element (it1+1,it1+1) of R is zero compared with element 
                // (it1,it1+1). Hence we don't have to do anything. if gc is zero, then
                // we just have to switch row (it1) and row (it1+1) of R and column (it1)
                // and column (it1+1) of J. Since we switch rows in R and columns in J,
                // we can ignore the sign of gs. Otherwise we have to apply the Givens 
                // rotation to these rows/columns. 

                if (gc == 1.0)
                {
                    return;
                }

                if (gc == 0.0)
                {
                    for (var i = it1; i < nact; i++)
                    {
                        temp = iwrm[l1 - 1];
                        iwrm[l1 - 1] = iwrm[l1];
                        iwrm[l1] = temp;
                        l1 += i + 1;
                    }

                    for (var i = 0; i < n; i++)
                    {
                        temp = dmat[it1 - 1, i];
                        dmat[it1 - 1, i] = dmat[it1, i];
                        dmat[it1, i] = temp;
                    }
                }
                else
                {
                    var nu = gs / (gc + 1.0);

                    for (var i = it1; i < nact; i++)
                    {
                        temp = gc * iwrm[l1 - 1] + gs * iwrm[l1];
                        iwrm[l1] = nu * (iwrm[l1 - 1] + temp) - iwrm[l1];
                        iwrm[l1 - 1] = temp;
                        l1 += i + 1;
                    }

                    for (var i = 0; i < n; i++)
                    {
                        temp = gc * dmat[it1 - 1, i] + gs * dmat[it1, i];
                        dmat[it1, i] = nu * (dmat[it1 - 1, i] + temp) - dmat[it1, i];
                        dmat[it1 - 1, i] = temp;
                    }
                }
            }

            private void ShiftColumnOfR(int l)
            {
                var l1 =
                    // shift column (it1+1) of R to column (it1) (that is, the first it1 
                    // elements). The position of element (1,it1+1) of R was calculated 
                    // above and stored in l. 
                    l - it1;
                for (var i = 0; i < it1; i++, l++, l1++)
                {
                    iwrm[l1] = iwrm[l];
                }

                // update vector u and iact as necessary 
                // Continue with updating the matrices J and R 

                iwuv[it1 - 1] = iwuv[it1];
                iact[it1 - 1] = iact[it1];
                it1++;
            }

            private enum QpStep
            {
                NewIteration,
                Direction,
                Drop,
                Finished
            }
        }
    }
}
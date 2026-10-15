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
//
// The source code presented in this file has been adapted from the
// Augmented Lagrangian method implementation presented in the NLopt
// Numerical Optimization Library. This file is thus licensed under
// the same MIT license as the original. Details are given below.
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

using Accord.Diagnostics;

namespace Accord.Math.Optimization
{
    public partial class AugmentedLagrangian
    {
        // magic parameters from Birgin & Martinez
        private const double tau = 0.5, gam = 10;
        private const double lam_min = -1e20;
        private const double lam_max = 1e20;
        private const double mu_max = 1e20;
        private const double nu_max = 1e20;

        private void InitializeRho(double[] currentSolution, out double minValue, out double minPenalty,
            out bool minFeasible)
        {
            double con2 = 0;
            double penalty = 0;

            // Evaluate function
            Evaluations++;
            var currentValue = Optimizer.Function(currentSolution);

            var feasible = true;

            // For each equality constraint
            for (var i = 0; i < equalityConstraints.Length; i++)
            {
                var actual = equalityConstraints[i].Function(currentSolution);
                var c = actual - equalityConstraints[i].Value;

                penalty += System.Math.Abs(c);
                con2 += c * c;

                feasible = feasible && System.Math.Abs(c) <= equalityConstraints[i].Tolerance;
            }

            // For each "lesser than" inequality constraint
            for (var i = 0; i < lesserThanConstraints.Length; i++)
            {
                var actual = lesserThanConstraints[i].Function(currentSolution);
                var c = actual - lesserThanConstraints[i].Value;

                if (c > 0)
                {
                    penalty += c;
                    con2 += c * c;
                }

                feasible = feasible && c <= lesserThanConstraints[i].Tolerance;
            }

            // For each "greater than" inequality constraint
            for (var i = 0; i < greaterThanConstraints.Length; i++)
            {
                var actual = greaterThanConstraints[i].Function(currentSolution);
                var c = greaterThanConstraints[i].Value - actual;
                if (c > 0)
                {
                    penalty += c;
                    con2 += c * c;
                }

                feasible = feasible && c <= greaterThanConstraints[i].Tolerance;
            }

            minValue = currentValue;
            minPenalty = penalty;
            minFeasible = feasible;


            SelectStartingRho(minValue, con2);
        }

        private void SelectStartingRho(double minValue, double con2)
        {
            var num = 2.0 * System.Math.Abs(minValue);

            if (num < 1e-300)
            {
                rho = rhoMin;
            }
            else if (con2 < 1e-300)
            {
                rho = rhoMax;
            }
            else
            {
                rho = num / con2;
                if (rho < rhoMin)
                {
                    rho = rhoMin;
                }
                else if (rho > rhoMax)
                {
                    rho = rhoMax;
                }
            }

            Debug.Assert(!double.IsNaN(rho));
        }

        private void MinimizeDual(double[] currentSolution)
        {
            for (var i = 0; i < Optimizer.Solution.Length; i++)
            {
                Optimizer.Solution[i] = currentSolution[i];
            }

            Optimizer.Minimize();

            // Retrieve the solution found
            for (var i = 0; i < currentSolution.Length; i++)
            {
                if (!double.IsNaN(Optimizer.Solution[i]))
                {
                    currentSolution[i] = Optimizer.Solution[i];
                }
            }
        }

        private void UpdateMultipliers(double[] currentSolution, out double ICM, out double penalty, out bool feasible)
        {
            ICM = 0;
            penalty = 0;
            feasible = true;


            // Update lambdas
            for (var i = 0; i < equalityConstraints.Length; i++)
            {
                var actual = equalityConstraints[i].Function(currentSolution);
                var c = actual - equalityConstraints[i].Value;

                var newLambda = lambda[i] + rho * c;
                penalty += System.Math.Abs(c);
                feasible = feasible && System.Math.Abs(c) <= equalityConstraints[i].Tolerance;
                ICM = System.Math.Max(ICM, System.Math.Abs(c));
                lambda[i] = System.Math.Min(System.Math.Max(lam_min, newLambda), lam_max);
            }

            // Update mus
            for (var i = 0; i < lesserThanConstraints.Length; i++)
            {
                var actual = lesserThanConstraints[i].Function(currentSolution);
                var c = actual - lesserThanConstraints[i].Value;

                var newMu = mu[i] + rho * c;
                penalty += c > 0 ? c : 0;
                feasible = feasible && c <= lesserThanConstraints[i].Tolerance;
                ICM = System.Math.Max(ICM, System.Math.Abs(System.Math.Max(c, -mu[i] / rho)));
                mu[i] = System.Math.Min(System.Math.Max(0.0, newMu), mu_max);
            }

            // Update nus
            for (var i = 0; i < greaterThanConstraints.Length; i++)
            {
                var actual = greaterThanConstraints[i].Function(currentSolution);
                var c = greaterThanConstraints[i].Value - actual;

                var newNu = nu[i] + rho * c;
                penalty += c > 0 ? c : 0;
                feasible = feasible && c <= greaterThanConstraints[i].Tolerance;
                ICM = System.Math.Max(ICM, System.Math.Abs(System.Math.Max(c, -nu[i] / rho)));
                nu[i] = System.Math.Min(System.Math.Max(0.0, newNu), nu_max);
            }
        }

        private AugmentedLagrangianStatus? CheckProgress(double[] currentSolution, bool feasible, double penalty,
            double currentValue, double ICM, ref double minValue, ref double minPenalty, ref bool minFeasible,
            ref int noProgressCounter, int maxCount)
        {
            // Check if we should stop
            var a = !minFeasible || penalty < minPenalty || currentValue < minValue;
            var b = !minFeasible && penalty < minPenalty;
            if ((feasible && a) || b)
            {
                AugmentedLagrangianStatus? r = null;

                if (feasible)
                {
                    if (relstop(minValue, currentValue, ftol_rel, ftol_abs))
                    {
                        r = AugmentedLagrangianStatus.Converged;
                    }

                    if (xtolreach(currentSolution, xtol_rel, 0))
                    {
                        r = AugmentedLagrangianStatus.Converged;
                    }
                }

                minValue = currentValue;
                minPenalty = penalty;
                minFeasible = feasible;

                // Save the current solution 
                for (var i = 0; i < Solution.Length; i++)
                {
                    Solution[i] = currentSolution[i];
                }

                if (r.HasValue)
                {
                    return r.Value;
                }

                noProgressCounter = 0;
            }
            else
            {
                if (ICM == 0)
                {
                    return AugmentedLagrangianStatus.Converged;
                }

                noProgressCounter++;

                if (noProgressCounter > maxCount)
                {
                    return AugmentedLagrangianStatus.NoProgress;
                }
            }

            return null;
        }
    }
}
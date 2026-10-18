using System;
using Accord.Math;
using Accord.Math.Decompositions;

namespace Accord.Statistics.Models.Regression.Fitting
{
    public partial class LowerBoundNewtonRaphson
    {
        private void accumulateGradientAndLowerBound(double[][] x, double[][] y, double[][] design)
        {
            // Reset Hessian matrix and gradient
            Array.Clear(Gradient, 0, Gradient.Length);

            if (UpdateLowerBound)
            {
                Array.Clear(HessianLowerBound, 0, HessianLowerBound.Length);
            }

            // For each input sample in the dataset
            for (var i = 0; i < x.Length; i++)
            {
                // Grab variables related to the sample
                var rx = design[i];
                var ry = y[i];

                // Compute and estimate outputs
                compute(x[i], output);

                // Compute errors for the sample
                for (var j = 0; j < errors.Length; j++)
                {
                    errors[j] = ry[j + 1] - output[j];
                }


                // Compute current gradient and Hessian
                //   We can take advantage of the block structure of the 
                //   Hessian matrix and gradient vector by employing the
                //   Kronecker product. See [Böhning, 1992]

                // (Re-) Compute error gradient
                var g = errors.Kronecker(rx);
                for (var j = 0; j < g.Length; j++)
                {
                    Gradient[j] += g[j];
                }

                if (UpdateLowerBound)
                {
                    // Compute xxt matrix
                    for (var k = 0; k < rx.Length; k++)
                    {
                        for (var j = 0; j < rx.Length; j++)
                        {
                            xxt[k, j] = rx[k] * rx[j];
                        }
                    }

                    // (Re-) Compute weighted "Hessian" matrix 
                    var h = weights.Kronecker(xxt);
                    for (var j = 0; j < Parameters; j++)
                    {
                        for (var k = 0; k < Parameters; k++)
                        {
                            HessianLowerBound[j, k] += h[j, k];
                        }
                    }
                }
            }
        }

        private void solveForDeltas()
        {
            if (UpdateLowerBound)
            {
                UpdateLowerBound = false;

                // Decompose to solve the linear system. Usually the Hessian will
                // be invertible and LU will succeed. However, sometimes the Hessian
                // may be singular and a Singular Value Decomposition may be needed.

                // The SVD is very stable, but is quite expensive, being on average
                // about 10-15 times more expensive than LU decomposition. There are
                // other ways to avoid a singular Hessian. For a very interesting 
                // reading on the subject, please see:
                //
                //  - Jeff Gill & Gary King, "What to Do When Your Hessian Is Not Invertible",
                //    Sociological Methods & Research, Vol 33, No. 1, August 2004, 54-87.
                //    Available in: http://gking.harvard.edu/files/help.pdf
                //

                // Moreover, the computation of the inverse is optional, as it will
                // be used only to compute the standard errors of the regression.


                decomposition = new SingularValueDecomposition(HessianLowerBound);
                ParameterChange = decomposition.Solve(Gradient);
            }
            else
            {
                ParameterChange = decomposition.Solve(Gradient);
            }
        }

        private void updateCoefficients(double[][] coefficients)
        {
            Previous = coefficients.Reshape();

            // Update coefficients using the calculated deltas
            for (int i = 0, k = 0; i < coefficients.Length; i++)
            {
                for (var j = 0; j < coefficients[i].Length; j++)
                {
                    coefficients[i][j] -= ParameterChange[k++];
                }
            }

            Solution = coefficients.Reshape();

            // Return the relative maximum parameter change
            for (var i = 0; i < ParameterChange.Length; i++)
            {
                ParameterChange[i] = System.Math.Abs(ParameterChange[i]) / System.Math.Abs(Previous[i]);
            }

            convergence.NewValue = ParameterChange.Max();
        }

        private void computeStandardErrorsFromInverse()
        {
            // Grab the regression information matrix
            var inverse = decomposition.Inverse();

            // Calculate coefficients' standard errors
            var standardErrors = regression.StandardErrors;
            for (int i = 0, k = 0; i < standardErrors.Length; i++)
            {
                for (var j = 0; j < standardErrors[i].Length; j++, k++)
                {
                    standardErrors[i][j] = System.Math.Sqrt(System.Math.Abs(inverse[k, k]));
                }
            }
        }
    }
}
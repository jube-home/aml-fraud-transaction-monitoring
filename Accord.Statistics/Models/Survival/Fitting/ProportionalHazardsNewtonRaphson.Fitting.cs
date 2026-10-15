using System;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Models.Regression.Fitting
{
    public partial class ProportionalHazardsNewtonRaphson
    {
        private void standardizeInputs(ref double[][] inputs, out double[] sdev)
        {
            // Store means as regression centers
            var means = inputs.Mean(0);
            for (var i = 0; i < means.Length; i++)
            {
                regression.Offsets[i] = means[i];
            }

            // Convert to unit scores for increased accuracy
            sdev = inputs.StandardDeviation();
            inputs = inputs.Subtract(means, VectorType.RowVector).Divide(sdev, VectorType.RowVector);

            for (var i = 0; i < regression.Coefficients.Length; i++)
            {
                regression.Coefficients[i] *= sdev[i];
            }
        }

        private void computeOutputs(double[][] inputs, double[] output)
        {
            for (var i = 0; i < output.Length; i++)
            {
                double sum = 0;
                for (var j = 0; j < regression.Coefficients.Length; j++)
                {
                    sum += regression.Coefficients[j] * inputs[i][j];
                }

                output[i] = System.Math.Exp(sum);
            }
        }

        private bool iterate(double[][] inputs, double[] time, SurvivalOutcome[] censor, double[] output)
        {
            CurrentIteration = 0;
            var smooth = Lambda;

            do
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                // learning iterations until convergence
                // or maximum number of iterations reached

                CurrentIteration++;

                accumulateGradientAndHessian(inputs, time, censor, output);

                smooth = applyNewtonStep(smooth);

                // Recompute current outputs
                computeOutputs(inputs, output);

                if (Token.IsCancellationRequested)
                {
                    return false;
                }
            } while (!convergence.HasConverged);

            return true;
        }

        private void accumulateGradientAndHessian(double[][] inputs, double[] time, SurvivalOutcome[] censor,
            double[] output)
        {
            // Reset Hessian matrix and gradient
            Array.Clear(Gradient, 0, Gradient.Length);
            Array.Clear(Hessian, 0, Hessian.Length);

            // For each observation instance
            for (var i = 0; i < inputs.Length; i++)
            {
                // Check if we should censor
                if (censor[i] == SurvivalOutcome.Censored)
                {
                    continue;
                }

                // Compute partials 
                double den = 0;
                Array.Clear(partialGradient, 0, partialGradient.Length);
                Array.Clear(partialHessian, 0, partialHessian.Length);

                for (var j = 0; j < inputs.Length; j++)
                {
                    if (time[j] >= time[i])
                    {
                        den += output[j];
                    }
                }

                for (var j = 0; j < inputs.Length; j++)
                {
                    if (time[j] >= time[i])
                    {
                        // Compute partial gradient
                        for (var k = 0; k < partialGradient.Length; k++)
                        {
                            partialGradient[k] += inputs[j][k] * output[j] / den;
                        }

                        // Compute partial Hessian
                        for (var ii = 0; ii < inputs[j].Length; ii++)
                        {
                            for (var jj = 0; jj < inputs[j].Length; jj++)
                            {
                                partialHessian[ii, jj] += inputs[j][ii] * inputs[j][jj] * output[j] / den;
                            }
                        }
                    }
                }

                // Compute gradient vector
                for (var j = 0; j < Gradient.Length; j++)
                {
                    Gradient[j] += inputs[i][j] - partialGradient[j];
                }

                // Compute Hessian matrix
                for (var j = 0; j < partialGradient.Length; j++)
                {
                    for (var k = 0; k < partialGradient.Length; k++)
                    {
                        Hessian[j, k] -= partialHessian[j, k] - partialGradient[j] * partialGradient[k];
                    }
                }
            }
        }

        private double applyNewtonStep(double smooth)
        {
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

            decomposition = new SingularValueDecomposition(Hessian);
            var deltas = decomposition.Solve(Gradient);

            if (convergence.MaxIterations > 0 || convergence.Tolerance > 0)
            {
                // Update coefficients using the calculated deltas
                for (var i = 0; i < regression.Coefficients.Length; i++)
                {
                    regression.Coefficients[i] -= smooth * deltas[i];
                }
            }

            smooth += Lambda;
            if (smooth > 1)
            {
                smooth = 1;
            }

            // Check relative maximum parameter change
            convergence.NewValues = regression.Coefficients;


            if (convergence.HasDiverged)
            {
                // Restore previous coefficients
                for (var i = 0; i < regression.Coefficients.Length; i++)
                {
                    regression.Coefficients[i] = convergence.OldValues[i];
                }
            }

            return smooth;
        }

        private void computeStandardErrorsFromHessian(double[] sdev)
        {
            // Grab the regression information matrix
            var inverse = decomposition.Inverse();

            // Calculate coefficients' standard errors
            var standardErrors = regression.StandardErrors;
            for (var i = 0; i < standardErrors.Length; i++)
            {
                standardErrors[i] = System.Math.Sqrt(System.Math.Abs(inverse[i, i])) / sdev[i];
            }
        }
    }
}
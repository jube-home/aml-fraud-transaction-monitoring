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

using Accord.Math;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    public abstract partial class ProbabilisticOutputCalibrationBase<TModel, TKernel, TInput>
    {
        private static void countClasses(bool[] outputs, out int positives, out int negatives)
        {
            positives = 0;
            negatives = 0;

            for (var i = 0; i < outputs.Length; i++)
            {
                if (outputs[i])
                {
                    positives++;
                }
                else
                {
                    negatives++;
                }
            }
        }

        private void initializeTargets(bool[] outputs, int positives, int negatives)
        {
            // Define the target probabilities we aim to produce
            var high = (positives + 1.0) / (positives + 2.0);
            var low = 1.0 / (negatives + 2.0);

            for (var i = 0; i < outputs.Length; i++)
            {
                targets[i] = outputs[i] ? high : low;
            }
        }

        private double computeLogLikelihood(double A, double B)
        {
            var logLikelihood = 0.0;

            // Compute the log-likelihood function
            for (var i = 0; i < distances.Length; i++)
            {
                var y = distances[i] * A + B;

                if (y >= 0)
                {
                    logLikelihood += targets[i] * y + Special.Log1p(System.Math.Exp(-y));
                }
                else
                {
                    logLikelihood += (targets[i] - 1) * y + Special.Log1p(System.Math.Exp(y));
                }
            }

            return logLikelihood;
        }

        private void computeGradientAndHessian(double A, double B, out double h11, out double h22, out double h21,
            out double g1, out double g2)
        {
            // Update the Gradient and Hessian
            //  (Using that H' = H + sigma I)

            h11 = sigma;
            h22 = sigma;
            h21 = 0;

            g1 = 0;
            g2 = 0;

            for (var i = 0; i < distances.Length; i++)
            {
                double p, q;
                var y = distances[i] * A + B;

                if (y >= 0)
                {
                    p = System.Math.Exp(-y) / (1.0 + System.Math.Exp(-y));
                    q = 1.0 / (1.0 + System.Math.Exp(-y));
                }
                else
                {
                    p = 1.0 / (1.0 + System.Math.Exp(y));
                    q = System.Math.Exp(y) / (1.0 + System.Math.Exp(y));
                }

                var d1 = targets[i] - p;
                var d2 = p * q;

                // Update Hessian
                h11 += distances[i] * distances[i] * d2;
                h22 += d2;
                h21 += distances[i] * d2;

                // Update Gradient
                g1 += distances[i] * d1;
                g2 += d1;
            }
        }

        private void lineSearch(ref double A, ref double B, ref double logLikelihood, double dA, double dB, double gd)
        {
            double stepSize = 1;

            // Perform a line search
            while (stepSize >= StepSize)
            {
                var newA = A + stepSize * dA;
                var newB = B + stepSize * dB;

                // Compute the new log-likelihood function
                var newLogLikelihood = computeLogLikelihood(newA, newB);


                // Check if a sufficient decrease has been obtained
                if (newLogLikelihood < logLikelihood + 1e-4 * stepSize * gd)
                {
                    // Yes, it has. Update parameters with the new values
                    A = newA;
                    B = newB;
                    logLikelihood = newLogLikelihood;
                    break;
                }

                // Decrease the step size until it can achieve
                // a sufficient decrease or until it fails.
                stepSize /= 2.0;

                if (stepSize < StepSize)
                {
                    // No decrease could be obtained. 
                    break; // throw new LineSearchFailedException("No sufficient decrease was obtained.");
                }
            }
        }
    }
}
// Accord Machine Learning Library
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
// The code contained in this class was adapted from Laurens van der Maaten excellent 
// BH T-SNE code from https://github.com/lvdmaaten/bhtsne/. The original license is 
// listed below:
//  
//    Copyright (c) 2014, Laurens van der Maaten (Delft University of Technology)
//    All rights reserved.
//   
//    Redistribution and use in source and binary forms, with or without
//    modification, are permitted provided that the following conditions are met:
//    1. Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//    2. Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//    3. All advertising materials mentioning features or use of this software
//       must display the following acknowledgement:
//       This product includes software developed by the Delft University of Technology.
//    4. Neither the name of the Delft University of Technology nor the names of 
//       its contributors may be used to endorse or promote products derived from 
//       this software without specific prior written permission.
//   
//    THIS SOFTWARE IS PROVIDED BY LAURENS VAN DER MAATEN ''AS IS'' AND ANY EXPRESS
//    OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
//    OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO 
//    EVENT SHALL LAURENS VAN DER MAATEN BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
//    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
//    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
//    BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
//    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING 
//    IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY 
//    OF SUCH DAMAGE.
//   

using System;
using System.Diagnostics;
using Accord.Math;

#if !MONO
namespace Accord.MachineLearning.Clustering
{
    public partial class TSNE
    {
        private sealed class Embedding
        {
            private readonly int D;
            private readonly int N;
            private readonly double[][] X;
            private readonly double[][] Y;

            private readonly double[][] dY;
            private readonly double eta = 200.0;
            private readonly bool exact;
            private readonly double final_momentum = 0.8;
            private readonly double[][] gains;
            private readonly int max_iter = 1000;
            private readonly int mom_switch_iter = 250;
            private readonly int no_dims;
            private readonly double perplexity;
            private readonly int stop_lying_iter = 250;
            private readonly double theta;
            private readonly double[][] uY;

            private double[][] P;
            private int[] col_P;
            private TimeSpan end;
            private double momentum = 0.5;
            private int[] row_P;
            private Stopwatch start;

            // Set learning parameters
            private TimeSpan total_time = TimeSpan.Zero;
            private double[] val_P;

            public Embedding(double[][] X, double[][] Y, int N, int D, int no_dims, double perplexity, double theta,
                bool exact)
            {
                this.X = X;
                this.Y = Y;
                this.N = N;
                this.D = D;
                this.no_dims = no_dims;
                this.perplexity = perplexity;
                this.theta = theta;
                this.exact = exact;

                // Allocate some memory
                dY = Jagged.Create<double>(N, no_dims);
                uY = Jagged.Create<double>(N, no_dims);
                gains = Jagged.Ones<double>(N, no_dims);
            }

            public void ComputeInputSimilarities()
            {
                // Normalize input data (to prevent numerical problems)
                Debug.Write("Computing input similarities...");
                start = Stopwatch.StartNew();
                Statistics.Tools.Center(X, true);

                X.Divide(X.Max(), X);

                // Compute input similarities for exact t-SNE
                if (exact)
                {
                    ComputeExactSimilarities();
                }

                // Compute input similarities for approximate t-SNE
                else
                {
                    ComputeApproximateSimilarities();
                }

                end = start.Elapsed;
            }

            private void ComputeExactSimilarities()
            {
                Trace.Write("Exact?");
                // Compute similarities
                P = Jagged.Create<double>(N, N);

                computeGaussianPerplexity(X, N, D, ref P, perplexity);

                // Symmetrize input similarities
                Debug.Write("Symmetrizing...");
                for (var n = 0; n < N; n++)
                {
                    for (var m = n + 1; m < N; m++)
                    {
                        P[n][m] += P[m][n];
                        P[m][n] = P[n][m];
                    }
                }

                P.Divide(P.Sum(), P);
            }

            private void ComputeApproximateSimilarities()
            {
                // Compute asymmetric pairwise input similarities
                computeGaussianPerplexity(X, N, D, ref row_P, ref col_P, ref val_P, perplexity, (int)(3 * perplexity));

                // Symmetrize input similarities
                symmetrizeMatrix(ref row_P, ref col_P, ref val_P, N);
                var sum_P = 0.0;
                for (var i = 0; i < row_P[N]; i++)
                {
                    sum_P += val_P[i];
                }

                for (var i = 0; i < row_P[N]; i++)
                {
                    val_P[i] /= sum_P;
                }
            }

            public void LieAboutSimilarities()
            {
                // Lie about the P-values
                if (exact)
                {
                    P.Multiply(12.0, P);
                }
                else
                {
                    for (var i = 0; i < row_P[N]; i++)
                    {
                        val_P[i] *= 12.0;
                    }
                }
            }

            public void InitializeSolution()
            {
                // Initialize solution (randomly)
                for (var i = 0; i < Y.Length; i++)
                {
                    for (var j = 0; j < Y[i].Length; j++)
                    {
                        Y[i][j] = randn() * 0.0001;
                    }
                }
            }

            public void Optimize()
            {
                // Perform main training loop
                if (exact)
                {
                    Debug.Write(string.Format("Input similarities computed in {0} seconds!", end));
                    Debug.Write("Learning embedding...");
                }
                else
                {
                    Debug.Write(string.Format("Input similarities computed in {0} seconds (sparsity = {1})!", end,
                        row_P[N] / (N * (double)N)));
                    Debug.Write("Learning embedding...");
                }

                start = Stopwatch.StartNew();
                for (var iter = 0; iter < max_iter; iter++)
                {
                    // Compute (approximate) gradient
                    if (exact)
                    {
                        computeExactGradient(P, Y, N, no_dims, dY);
                    }
                    else
                    {
                        computeGradient(P, row_P, col_P, val_P, Y, N, no_dims, dY, theta);
                    }

                    UpdateGains();

                    ApplyGradientUpdate();

                    // Make solution zero-mean
                    Statistics.Tools.Center(Y, true);

                    // Stop lying about the P-values after a while, and switch momentum
                    if (iter == stop_lying_iter)
                    {
                        StopLyingAboutSimilarities();
                    }

                    if (iter == mom_switch_iter)
                    {
                        momentum = final_momentum;
                    }

                    // Print out progress
                    if (iter > 0 && (iter % 50 == 0 || iter == max_iter - 1))
                    {
                        ReportProgress(iter);
                    }
                }

                end = start.Elapsed;
                total_time += end;

                Debug.WriteLine("Fitting performed in {0} seconds.", total_time);
            }

            private void UpdateGains()
            {
                // Update gains
                for (var i = 0; i < gains.Length; i++)
                {
                    for (var j = 0; j < gains[i].Length; j++)
                    {
                        gains[i][j] = System.Math.Sign(dY[i][j]) != System.Math.Sign(uY[i][j])
                            ? gains[i][j] + 0.2
                            : gains[i][j] * 0.8;
                    }
                }

                for (var i = 0; i < gains.Length; i++)
                {
                    for (var j = 0; j < gains[i].Length; j++)
                    {
                        if (gains[i][j] < 0.01)
                        {
                            gains[i][j] = 0.01;
                        }
                    }
                }
            }

            private void ApplyGradientUpdate()
            {
                // Perform gradient update (with momentum and gains)
                for (var i = 0; i < uY.Length; i++)
                {
                    for (var j = 0; j < uY[i].Length; j++)
                    {
                        uY[i][j] = momentum * uY[i][j] - eta * gains[i][j] * dY[i][j];
                    }
                }

                for (var i = 0; i < Y.Length; i++)
                {
                    for (var j = 0; j < Y[i].Length; j++)
                    {
                        Y[i][j] = Y[i][j] + uY[i][j];
                    }
                }
            }

            private void StopLyingAboutSimilarities()
            {
                if (exact)
                {
                    P.Divide(12.0, P);
                }
                else
                {
                    for (var i = 0; i < row_P[N]; i++)
                    {
                        val_P[i] /= 12.0;
                    }
                }
            }

            private void ReportProgress(int iter)
            {
                end = start.Elapsed;
                double C;
                if (exact)
                {
                    C = evaluateError(P, Y, N, no_dims);
                }
                else
                {
                    C = evaluateError(row_P, col_P, val_P, Y, N, no_dims, theta); // doing approximate computation here!
                }

                if (iter == 0)
                {
                    Debug.WriteLine("Iteration {0}: error is {1}", iter + 1, C);
                }
                else
                {
                    total_time += end;
                    Debug.WriteLine("Iteration {0}: error is {1} (50 iterations in {2} seconds)", iter, C, end);
                }

                start = Stopwatch.StartNew();
            }
        }
    }
}
#endif
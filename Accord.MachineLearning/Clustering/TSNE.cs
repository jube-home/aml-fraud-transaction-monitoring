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
using System.Collections.Generic;
using System.Diagnostics;
using Accord.Collections;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.MachineLearning.Clustering
{
#if !MONO
    /// <summary>
    ///     Barnes-Hutt t-SNE.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The code contained in this class was adapted from Laurens van der Maaten excellent
    ///         BH T-SNE code from https://github.com/lvdmaaten/bhtsne/. The original license is
    ///         listed below:
    ///     </para>
    ///     <code>
    ///    Copyright (c) 2014, Laurens van der Maaten (Delft University of Technology)
    ///    All rights reserved.
    ///   
    ///    Redistribution and use in source and binary forms, with or without
    ///    modification, are permitted provided that the following conditions are met:
    ///    1. Redistributions of source code must retain the above copyright
    ///       notice, this list of conditions and the following disclaimer.
    ///    2. Redistributions in binary form must reproduce the above copyright
    ///       notice, this list of conditions and the following disclaimer in the
    ///       documentation and/or other materials provided with the distribution.
    ///    3. All advertising materials mentioning features or use of this software
    ///       must display the following acknowledgement:
    ///       This product includes software developed by the Delft University of Technology.
    ///    4. Neither the name of the Delft University of Technology nor the names of 
    ///       its contributors may be used to endorse or promote products derived from 
    ///       this software without specific prior written permission.
    ///   
    ///    THIS SOFTWARE IS PROVIDED BY LAURENS VAN DER MAATEN ''AS IS'' AND ANY EXPRESS
    ///    OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
    ///    OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO 
    ///    EVENT SHALL LAURENS VAN DER MAATEN BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
    ///    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
    ///    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
    ///    BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
    ///    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING 
    ///    IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY 
    ///    OF SUCH DAMAGE.
    /// </code>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\TSNETest.cs" region="doc_learn" />
    /// </example>
    public partial class TSNE : MultipleTransformBase<double[], double>
    {
        private const double DBL_MAX = -1.7976931348623157e+308;
        private const double DBL_MIN = 2.2250738585072014e-308;

        private const double FLT_MIN = 1.175494351e-38F;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TSNE" /> class.
        /// </summary>
        public TSNE()
        {
            NumberOfOutputs = 2;
        }

        /// <summary>
        ///     Gets or sets t-SNE's perplexity value. Default is 50.
        /// </summary>
        public double Perplexity { get; set; } = 50;

        /// <summary>
        ///     Gets or sets t-SNE's Theta value. Default is 0.5
        /// </summary>
        public double Theta { get; set; } = 0.5;

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double[] Transform(double[] input, double[] result)
        {
            return Transform(new[] { input }, new[] { result })[0];
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">The location to where to store the result of this transformation.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[][] Transform(double[][] input, double[][] result)
        {
            run(input, result, Perplexity, Theta);
            return result;
        }

        internal static void run(double[][] X, double[][] Y, double perplexity, double theta,
            bool skip_random_init = false)
        {
            var N = X.Rows();
            var D = X.Columns();
            var no_dims = Y.Columns();

            // Determine whether we are using an exact algorithm
            if (N - 1 < 3 * perplexity)
            {
                throw new Exception(string.Format(
                    "Perplexity too large for the number of data points. For {0} points, should be less than {1}", N,
                    (N - 1) / 3.0));
            }

            Debug.Write(string.Format("Using no_dims = {0}, perplexity = {1}, and theta = {2}", no_dims, perplexity,
                theta));

            var exact = theta == 0.0;

            var embedding = new Embedding(X, Y, N, D, no_dims, perplexity, theta, exact);

            embedding.ComputeInputSimilarities();

            embedding.LieAboutSimilarities();

            if (!skip_random_init)
            {
                embedding.InitializeSolution();
            }

            embedding.Optimize();
        }

        // Compute gradient of the t-SNE cost function (using Barnes-Hut algorithm)
        internal static void computeGradient(double[][] P, int[] inp_row_P, int[] inp_col_P, double[] inp_val_P,
            double[][] Y, int N, int D, double[][] dC, double theta)
        {
            // Construct space-partitioning tree on current map
            var tree = SPTree.FromData(Y);

            // Compute all terms required for t-SNE gradient
            var pos_f = Jagged.Create<double>(N, D);
            var neg_f = Jagged.Create<double>(N, D);
            tree.ComputeEdgeForces(Y, inp_row_P, inp_col_P, inp_val_P, pos_f);

            var sum_Q = 0.0;
            for (var n = 0; n < Y.Length; n++)
            {
                tree.ComputeNonEdgeForces(Y[n], theta, neg_f[n], ref sum_Q);
            }

            // Compute final t-SNE gradient
            for (var i = 0; i < dC.Length; i++)
            {
                for (var j = 0; j < dC[i].Length; j++)
                {
                    dC[i][j] = pos_f[i][j] - neg_f[i][j] / sum_Q;
                }
            }
        }

        // Compute gradient of the t-SNE cost function (exact)
        internal static void computeExactGradient(double[][] P, double[][] Y, int N, int D, double[][] dC)
        {
            // Make sure the current gradient contains zeros
            for (var i = 0; i < dC.Length; i++)
            {
                for (var j = 0; j < dC[i].Length; j++)
                {
                    dC[i][j] = 0.0;
                }
            }

            // Compute the squared Euclidean distance matrix
            var DD = Jagged.Create<double>(N, N);
            computeSquaredEuclideanDistance(Y, N, D, DD);

            // Compute Q-matrix and normalization sum
            var Q = Jagged.Zeros(N, N);

            var sum_Q = 0.0;
            for (var n = 0; n < N; n++)
            {
                for (var m = 0; m < N; m++)
                {
                    if (n != m)
                    {
                        Q[n][m] = 1.0 / (1.0 + DD[n][m]);
                        sum_Q += Q[n][m];
                    }
                }
            }

            // Perform the computation of the gradient
            for (var n = 0; n < N; n++)
            {
                for (var m = 0; m < N; m++)
                {
                    if (n != m)
                    {
                        var mult = (P[n][m] - Q[n][m] / sum_Q) * Q[n][m];
                        for (var d = 0; d < D; d++)
                        {
                            dC[n][d] += (Y[n][d] - Y[m][d]) * mult;
                        }
                    }
                }
            }
        }

        // Evaluate t-SNE cost function (exactly)
        internal static double evaluateError(double[][] P, double[][] Y, int N, int D)
        {
            // Compute the squared Euclidean distance matrix
            var DD = Jagged.Zeros(N, N);
            var Q = Jagged.Zeros(N, N);
            computeSquaredEuclideanDistance(Y, N, D, DD);

            // Compute Q-matrix and normalization sum
            var sum_Q = DBL_MIN;
            for (var n = 0; n < N; n++)
            {
                for (var m = 0; m < N; m++)
                {
                    if (n != m)
                    {
                        Q[n][m] = 1.0 / (1.0 + DD[n][m]);
                        sum_Q += Q[n][m];
                    }
                    else
                    {
                        Q[n][m] = DBL_MIN;
                    }
                }
            }

            Q.Divide(sum_Q, Q);

            // Sum t-SNE error
            var C = 0.0;
            for (var n = 0; n < P.Length; n++)
            {
                for (var j = 0; j < P[n].Length; j++)
                {
                    C += P[n][j] * System.Math.Log((P[n][j] + FLT_MIN) / (Q[n][j] + FLT_MIN));
                }
            }

            return C;
        }

        // Evaluate t-SNE cost function (approximately)
        internal static double evaluateError(int[] row_P, int[] col_P, double[] val_P, double[][] Y, int N, int D,
            double theta)
        {
            // Get estimate of normalization term
            var tree = SPTree.FromData(Y);
            var buff = new double[D];
            var sum_Q = 0.0;
            for (var n = 0; n < Y.Length; n++)
            {
                tree.ComputeNonEdgeForces(Y[n], theta, buff, ref sum_Q);
            }

            // Loop over all edges to compute t-SNE error
            var C = 0.0;
            for (var n = 0; n < N; n++)
            {
                for (var i = row_P[n]; i < row_P[n + 1]; i++)
                {
                    var j = col_P[i];
                    for (var d = 0; d < buff.Length; d++)
                    {
                        buff[d] = Y[n][d];
                    }

                    for (var d = 0; d < buff.Length; d++)
                    {
                        buff[d] -= Y[j][d];
                    }

                    var Q = 0.0;
                    for (var d = 0; d < buff.Length; d++)
                    {
                        Q += buff[d] * buff[d];
                    }

                    Q = 1.0 / (1.0 + Q) / sum_Q;

                    C += val_P[i] * System.Math.Log((val_P[i] + FLT_MIN) / (Q + FLT_MIN));
                }
            }

            return C;
        }

        // Compute input similarities with a fixed perplexity
        internal static void computeGaussianPerplexity(double[][] X, int N, int D, ref double[][] P, double perplexity)
        {
            // Compute the squared Euclidean distance matrix
            var DD = Jagged.Zeros(N, N);
            computeSquaredEuclideanDistance(X, N, D, DD);

            // Compute the Gaussian kernel row by row
            for (var n = 0; n < P.Length; n++)
            {
                // Initialize some variables
                var found = false;
                var beta = 1.0;
                var min_beta = -DBL_MAX;
                var max_beta = DBL_MAX;
                var tol = 1e-5;
                double sum_P = 0;

                // Iterate until we found a good perplexity
                var iter = 0;
                while (!found && iter < 200)
                {
                    // Compute Gaussian kernel row
                    for (var m = 0; m < DD[n].Length; m++)
                    {
                        P[n][m] = System.Math.Exp(-beta * DD[n][m]);
                    }

                    P[n][n] = DBL_MIN;

                    // Compute entropy of current row
                    sum_P = DBL_MIN;
                    for (var m = 0; m < P[n].Length; m++)
                    {
                        sum_P += P[n][m];
                    }

                    var H = 0.0;
                    for (var m = 0; m < DD[n].Length; m++)
                    {
                        H += beta * (DD[n][m] * P[n][m]);
                    }

                    H = H / sum_P + System.Math.Log(sum_P);

                    // Evaluate whether the entropy is within the tolerance level
                    var Hdiff = H - System.Math.Log(perplexity);

                    if (Hdiff < tol && -Hdiff < tol)
                    {
                        found = true;
                    }
                    else
                    {
                        if (Hdiff > 0)
                        {
                            min_beta = beta;
                            if (max_beta == DBL_MAX || max_beta == -DBL_MAX)
                            {
                                beta *= 2.0;
                            }
                            else
                            {
                                beta = (beta + max_beta) / 2.0;
                            }
                        }
                        else
                        {
                            max_beta = beta;
                            if (min_beta == -DBL_MAX || min_beta == DBL_MAX)
                            {
                                beta /= 2.0;
                            }
                            else
                            {
                                beta = (beta + min_beta) / 2.0;
                            }
                        }
                    }

                    // Update iteration counter
                    iter++;
                }

                // Row normalize P
                P[n].Divide(sum_P, P[n]);
            }
        }

        // Compute input similarities with a fixed perplexity using ball trees (this function allocates memory another function should free)
        internal static void computeGaussianPerplexity(double[][] X, int N, int D, ref int[] _row_P, ref int[] _col_P,
            ref double[] _val_P, double perplexity, int K)
        {
            if (perplexity > K)
            {
                throw new Exception(string.Format("Perplexity should be lower than K ({0}).", K));
            }

            // Allocate the memory we need
            _row_P = new int[N + 1];
            _col_P = new int[N * K];
            _val_P = new double[N * K];

            var row_P = _row_P;
            var col_P = _col_P;
            var val_P = _val_P;
            var cur_P = new double[N - 1];

            row_P[0] = 0;
            for (var n = 0; n < N; n++)
            {
                row_P[n + 1] = row_P[n] + K;
            }

            // Build ball tree on data set
            var idx = Vector.Range(0, N);
            var tree = VPTree.FromData(X, idx);

            // Loop over all points to find nearest neighbors
            var results = new List<NodeDistance<VPTreeNode<double[], int>>>();

            Debug.Write("Building tree...");
            for (var n = 0; n < X.Length; n++)
            {
                if (n % 10000 == 0)
                {
                    Debug.Write(string.Format(" - point {0} of {1}", n, N));
                }

                // Find nearest neighbors
                results.Clear();
                tree.Nearest(X[n], K + 1, results);

                // Initialize some variables for binary search
                var found = false;
                var beta = 1.0;
                var min_beta = -DBL_MIN;
                var max_beta = DBL_MAX;
                var tol = 1e-5;

                // Iterate until we found a good perplexity
                var iter = 0;
                double sum_P = 0;
                while (!found && iter < 200)
                {
                    // Compute Gaussian kernel row
                    for (var m = 0; m < K; m++)
                    {
                        var d = results[m + 1].Distance;
                        cur_P[m] = System.Math.Exp(-beta * d * d);
                    }

                    // Compute entropy of current row
                    sum_P = DBL_MIN;
                    for (var m = 0; m < K; m++)
                    {
                        sum_P += cur_P[m];
                    }

                    var H = 0.0;
                    for (var m = 0; m < K; m++)
                    {
                        var d = results[m + 1].Distance;
                        H += beta * (d * d * cur_P[m]);
                    }

                    H = H / sum_P + System.Math.Log(sum_P);

                    // Evaluate whether the entropy is within the tolerance level
                    var Hdiff = H - System.Math.Log(perplexity);

                    if (Hdiff < tol && -Hdiff < tol)
                    {
                        found = true;
                    }
                    else
                    {
                        if (Hdiff > 0)
                        {
                            min_beta = beta;
                            if (max_beta == DBL_MAX || max_beta == -DBL_MAX)
                            {
                                beta *= 2.0;
                            }
                            else
                            {
                                beta = (beta + max_beta) / 2.0;
                            }
                        }
                        else
                        {
                            max_beta = beta;
                            if (min_beta == -DBL_MAX || min_beta == DBL_MAX)
                            {
                                beta /= 2.0;
                            }
                            else
                            {
                                beta = (beta + min_beta) / 2.0;
                            }
                        }
                    }

                    // Update iteration counter
                    iter++;
                }

                // Row-normalize current row of P and store in matrix
                cur_P.Divide(sum_P, cur_P);

                for (var m = 0; m < K; m++)
                {
                    var j = results[m + 1].Node.Value; // holds the index
                    col_P[row_P[n] + m] = j;
                    val_P[row_P[n] + m] = cur_P[m];
                }
            }
        }

        // Symmetrizes a sparse matrix
        internal static void symmetrizeMatrix(ref int[] _row_P, ref int[] _col_P, ref double[] _val_P, int N)
        {
            // Get sparse matrix
            var row_P = _row_P;
            var col_P = _col_P;
            var val_P = _val_P;

            // Count number of elements and row counts of symmetric matrix
            var row_counts = new int[N];

            for (var n = 0; n < row_P.Length - 1; n++)
            {
                for (var i = row_P[n]; i < row_P[n + 1]; i++)
                {
                    // Check whether element (col_P[i], n) is present
                    var present = false;
                    for (var m = row_P[col_P[i]]; m < row_P[col_P[i] + 1]; m++)
                    {
                        if (col_P[m] == n)
                        {
                            present = true;
                        }
                    }

                    if (present)
                    {
                        row_counts[n]++;
                    }
                    else
                    {
                        row_counts[n]++;
                        row_counts[col_P[i]]++;
                    }
                }
            }

            var no_elem = 0;
            for (var n = 0; n < N; n++)
            {
                no_elem += row_counts[n];
            }

            // Allocate memory for symmetrized matrix
            var sym_row_P = new int[N + 1];
            var sym_col_P = new int[no_elem];
            var sym_val_P = new double[no_elem];

            // Construct new row indices for symmetric matrix
            sym_row_P[0] = 0;
            for (var n = 0; n < N; n++)
            {
                sym_row_P[n + 1] = sym_row_P[n] + row_counts[n];
            }

            // Fill the result matrix
            var offset = new int[N];

            for (var n = 0; n < N; n++)
            {
                for (var i = row_P[n]; i < row_P[n + 1]; i++)
                {
                    // considering element(n, col_P[i])

                    // Check whether element (col_P[i], n) is present
                    var present = false;
                    for (var m = row_P[col_P[i]]; m < row_P[col_P[i] + 1]; m++)
                    {
                        if (col_P[m] == n)
                        {
                            present = true;
                            if (n <= col_P[i])
                            {
                                // make sure we do not add elements twice
                                sym_col_P[sym_row_P[n] + offset[n]] = col_P[i];
                                sym_col_P[sym_row_P[col_P[i]] + offset[col_P[i]]] = n;
                                sym_val_P[sym_row_P[n] + offset[n]] = val_P[i] + val_P[m];
                                sym_val_P[sym_row_P[col_P[i]] + offset[col_P[i]]] = val_P[i] + val_P[m];
                            }
                        }
                    }

                    // If (col_P[i], n) is not present, there is no addition involved
                    if (!present)
                    {
                        sym_col_P[sym_row_P[n] + offset[n]] = col_P[i];
                        sym_col_P[sym_row_P[col_P[i]] + offset[col_P[i]]] = n;
                        sym_val_P[sym_row_P[n] + offset[n]] = val_P[i];
                        sym_val_P[sym_row_P[col_P[i]] + offset[col_P[i]]] = val_P[i];
                    }

                    // Update offsets
                    if (!present || (present && n <= col_P[i]))
                    {
                        offset[n]++;
                        if (col_P[i] != n)
                        {
                            offset[col_P[i]]++;
                        }
                    }
                }
            }

            // Divide the result by two
            for (var i = 0; i < no_elem; i++)
            {
                sym_val_P[i] /= 2.0;
            }

            // Return symmetrized matrices
            _row_P = sym_row_P;
            _col_P = sym_col_P;
            _val_P = sym_val_P;
        }

        // Compute squared Euclidean distance matrix (using BLAS)
        internal static void computeSquaredEuclideanDistance(double[][] X, int N, int D, double[][] DD)
        {
            var dataSums = new double[N];

            for (var n = 0; n < X.Length; n++)
            {
                for (var d = 0; d < X[n].Length; d++)
                {
                    dataSums[n] += X[n][d] * X[n][d];
                }
            }

            for (var n = 0; n < DD.Length; n++)
            {
                for (var m = 0; m < DD[n].Length; m++)
                {
                    DD[n][m] = dataSums[n] + dataSums[m];
                }
            }

            for (var n = 0; n < X.Length; n++)
            {
                DD[n][n] = 0.0;
                for (var m = n + 1; m < X.Length; m++)
                {
                    DD[n][m] = 0.0;
                    for (var d = 0; d < D; d++)
                    {
                        DD[n][m] += (X[n][d] - X[m][d]) * (X[n][d] - X[m][d]);
                    }

                    DD[m][n] = DD[n][m];
                }
            }
        }

        // Generates a Gaussian random number
        private static double randn()
        {
            return NormalDistribution.Standard.Generate();
        }
    }

#endif
}
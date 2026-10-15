using Accord.Math;
using Accord.Math.Decompositions;

namespace Accord.Statistics.Analysis
{
    public partial class PartialLeastSquaresAnalysis
    {
        private sealed class SimplsRun
        {
            public readonly double[][] C;
            public readonly double[][] P;

            public readonly double[][] T;
            public readonly double[][] U;

            private readonly double[][] V;
            public readonly double[][] W;
            private readonly int factors;
            private readonly double[][] inputsX;
            private readonly double[][] outputsY;
            private readonly PartialLeastSquaresAnalysis owner;
            private readonly int rows;
            public readonly double[] varX;
            public readonly double[] varY;
            private readonly int xcols;
            private readonly int ycols;
            private double[] c;
            private double[][] covariance;
            private double norm_t;
            private double[] p;
            private double[] t;
            private double[] u;
            private double[] v;

            private double[] w;

            public SimplsRun(PartialLeastSquaresAnalysis owner, double[][] inputsX, double[][] outputsY, int factors)
            {
                this.owner = owner;
                this.inputsX = inputsX;
                this.outputsY = outputsY;
                this.factors = factors;

                // Initialize and prepare the data
                rows = inputsX.Rows();
                xcols = inputsX.Columns();
                ycols = outputsY.Columns();

                // Initialize storage variables
                T = Jagged.Zeros(rows, factors); // factor score matrix T
                U = Jagged.Zeros(rows, factors); // factor score matrix U
                P = Jagged.Zeros(xcols, factors); // loading matrix P, the loadings for X such that X = TP + F
                C = Jagged.Zeros(ycols, factors); // loading matrix C, the loadings for Y such that Y = TC + E
                W = Jagged.Zeros(xcols, factors); // weight matrix W
                varX = new double[factors];
                varY = new double[factors];

                // Orthogonal loadings
                V = Jagged.Zeros(xcols, factors);


                // Create covariance matrix C = X'Y
                covariance = inputsX.TransposeAndDot(outputsY);
            }

            public void Extract()
            {
                for (var factor = 0; factor < factors; factor++)
                {
                    estimateWeights();
                    estimateScoresAndLoadings();
                    estimateYScores();
                    orthogonalizeLoadings(factor);
                    deflateCovariance();
                    saveFactor(factor);

                    // Compute explained variance
                    varX[factor] = p.Dot(p);
                    varY[factor] = c.Dot(c);

                    if (owner.Token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            private void estimateWeights()
            {
                // Step 1. Obtain the dominant eigenvector w of C'C. However, we
                //   can avoid computing the matrix multiplication by using the
                //   singular value decomposition instead, which is also more
                //   stable. The first weight vector w is the left singular vector
                //   of C=X'Y [Abdi, 2007].

                var svd = new JaggedSingularValueDecomposition(covariance,
                    true,
                    false,
                    true);

                w = svd.LeftSingularVectors.GetColumn(0);
                c = covariance.TransposeAndDot(w);
            }

            private void estimateScoresAndLoadings()
            {
                // Step 2. Estimate X factor scores: t ∝ X*w
                //   Similarly to NIPALS, the T factor of SIMPLS
                //   is computed as T=X*W [Statsoft] [Abdi, 2010].

                // 2.1. Estimate t (X factor scores): t = X*w [Abdi, 2010]
                t = new double[rows];
                for (var i = 0; i < t.Length; i++)
                {
                    for (var j = 0; j < w.Length; j++)
                    {
                        t[i] += inputsX[i][j] * w[j];
                    }
                }

                // 2.2. Normalize t (X factor scores): t = t/norm(t)
                norm_t = t.Euclidean();
                t.Divide(norm_t, t);

                // Step 3. Estimate p (X factor loadings): p = X'*t
                p = new double[xcols];
                for (var i = 0; i < p.Length; i++)
                {
                    for (var j = 0; j < t.Length; j++)
                    {
                        p[i] += inputsX[j][i] * t[j];
                    }
                }


                // Step 4. Estimate X and Y weights. Actually, the weights have
                //   been computed in the first step during SVD. However, since
                //   the X factor scores have been normalized, we also have to
                //   normalize weights accordingly: w = w/norm(t), c = c/norm(t)
                w = w.Divide(norm_t);
                c = c.Divide(norm_t);
            }

            private void estimateYScores()
            {
                // Step 5. Estimate u (Y factor scores): u = Y*c [Abdi, 2010]
                u = new double[rows];
                for (var i = 0; i < u.Length; i++)
                {
                    for (var j = 0; j < c.Length; j++)
                    {
                        u[i] += outputsY[i][j] * c[j];
                    }
                }
            }

            private void orthogonalizeLoadings(int factor)
            {
                // Step 6. Create orthogonal loading
                v = (double[])p.Clone();

                // Step 7. Make v orthogonal to the previous loadings
                // http://en.wikipedia.org/wiki/Gram%E2%80%93Schmidt_process

                if (factor > 0)
                {
                    // 7.1. MGS for v [Martin Anderson, 2009]
                    for (var j = 0; j < factor; j++)
                    {
                        var proj = 0.0;
                        for (var k = 0; k < v.Length; k++)
                        {
                            proj += v[k] * V[k][j];
                        }

                        for (var k = 0; k < v.Length; k++)
                        {
                            v[k] -= proj * V[k][j];
                        }
                    }

                    // 7.1. MGS for u [Martin Anderson, 2009]
                    for (var j = 0; j < factor; j++)
                    {
                        var proj = 0.0;
                        for (var k = 0; k < u.Length; k++)
                        {
                            proj += u[k] * T[k][j];
                        }

                        for (var k = 0; k < u.Length; k++)
                        {
                            u[k] -= proj * T[k][j];
                        }
                    }
                }

                // 7.2. Normalize orthogonal loadings
                v = v.Divide(v.Euclidean());
            }

            private void deflateCovariance()
            {
                // Step 8. Deflate covariance matrix as s = s - v * (v' * s)
                //   as shown in simpls1 in [Martin Anderson, 2009] appendix.
                var cov = covariance.Copy();
                for (var i = 0; i < v.Length; i++)
                {
                    for (var j = 0; j < v.Length; j++)
                    {
                        var d = v[i] * v[j];

                        for (var k = 0; k < ycols; k++)
                        {
                            cov[i][k] -= d * covariance[j][k];
                        }
                    }
                }

                covariance = cov;
            }

            private void saveFactor(int factor)
            {
                // Save iteration cols
                W.SetColumn(factor, w);
                U.SetColumn(factor, u);
                C.SetColumn(factor, c);
                T.SetColumn(factor, t);
                P.SetColumn(factor, p);
                V.SetColumn(factor, v);
            }
        }
    }
}
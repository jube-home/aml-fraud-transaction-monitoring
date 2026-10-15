using Accord.Math;

namespace Accord.Statistics.Analysis
{
    public partial class PartialLeastSquaresAnalysis
    {
        private sealed class NipalsRun
        {
            public readonly double[] B;
            public readonly double[][] C;

            public readonly double[][] E;
            public readonly double[][] F;
            public readonly double[][] P;
            public readonly double[][] T;
            public readonly double[][] U;
            public readonly double[][] W;
            private readonly int factors;
            private readonly PartialLeastSquaresAnalysis owner;
            private readonly int rows;
            public readonly double[] varX;
            public readonly double[] varY;
            private readonly int xcols;
            private readonly int ycols;
            private double b;
            private double[] c;
            private double norm_t;
            private double[] p;

            private double[] t;
            private double[] u;
            private double[] w;

            public NipalsRun(PartialLeastSquaresAnalysis owner, double[][] inputsX, double[][] outputsY, int factors)
            {
                this.owner = owner;
                this.factors = factors;

                // Initialize and prepare the data
                rows = inputsX.Rows();
                xcols = inputsX.Columns();
                ycols = outputsY.Columns();

                // Initialize storage variables
                E = inputsX.Copy();
                F = outputsY.Copy();

                T = Jagged.Zeros(rows, factors); // factor score matrix T
                U = Jagged.Zeros(rows, factors); // factor score matrix U
                P = Jagged.Zeros(xcols, factors); // loading matrix P, the loadings for X such that X = TP + F
                C = Jagged.Zeros(ycols, factors); // loading matrix C, the loadings for Y such that Y = TC + E
                W = Jagged.Zeros(xcols, xcols); // weight matrix W
                B = new double[factors];

                varX = new double[factors];
                varY = new double[factors];
            }

            public void Extract(double tolerance)
            {
                // Initialize the algorithm
                var stop = false;

                for (var factor = 0; factor < factors && !stop; factor++)
                {
                    selectStartingVectors();
                    iterateUntilConverged();
                    computeLoadings();
                    deflate();

                    // Calculate explained variances
                    varY[factor] = b * b;
                    varX[factor] = p.Dot(p);

                    saveFactor(factor);
                    stop = residualsBelow(tolerance);
                }
            }

            private void selectStartingVectors()
            {
                // Select t as the largest column from X,
                t = E.GetColumn(largest(E));

                // Select u as the largest column from Y.
                u = F.GetColumn(largest(F));

                // Will store weights for X and Y
                w = new double[xcols];
                c = new double[ycols];

                norm_t = t.Euclidean();
            }

            private void iterateUntilConverged()
            {
                while (norm_t > 1e-14)
                {
                    #region Iteration

                    // Store initial t to check convergence
                    var t0 = (double[])t.Clone();


                    // Step 1. Estimate w (X weights): w ∝ E'*u
                    //   (in Abdi's paper, E is the residuals for X).

                    // 1.1. Compute w = E'*u;
                    w = new double[xcols];
                    for (var j = 0; j < w.Length; j++)
                    {
                        for (var i = 0; i < u.Length; i++)
                        {
                            w[j] += E[i][j] * u[i];
                        }
                    }

                    // 1.2. Normalize w (w = w/norm(w))
                    w = w.Divide(w.Euclidean());


                    // Step 2. Estimate t (X factor scores): t ∝ E*w
                    //   (in Abdi's paper, E is the residuals for X).

                    // 2.1. Compute t = E*w
                    t = new double[rows];
                    for (var i = 0; i < t.Length; i++)
                    {
                        for (var j = 0; j < w.Length; j++)
                        {
                            t[i] += E[i][j] * w[j];
                        }
                    }

                    // 2.2. Normalize t: t = t/norm(t)
                    t = t.Divide(norm_t = t.Euclidean());


                    // Step 3. Estimate c (Y weights): c ∝ F't
                    //   (in Abdi's paper, F is the residuals for Y).

                    // 3.1. Compute c = F'*t0;
                    c = new double[ycols];
                    for (var j = 0; j < c.Length; j++)
                    {
                        for (var i = 0; i < t.Length; i++)
                        {
                            c[j] += F[i][j] * t[i];
                        }
                    }

                    // 3.2. Normalize q: c = c/norm(q)
                    c = c.Divide(c.Euclidean());


                    // Step 4. Estimate u (Y scores): u = F*q
                    //   (in Abdi's paper, F is the residuals for Y).

                    // 4.1. Compute u = F*q;
                    u = new double[rows];
                    for (var i = 0; i < u.Length; i++)
                    {
                        for (var j = 0; j < c.Length; j++)
                        {
                            u[i] += F[i][j] * c[j];
                        }
                    }


                    // Recalculate norm of the difference
                    norm_t = 0.0;
                    for (var i = 0; i < t.Length; i++)
                    {
                        var d = t0[i] - t[i];
                        norm_t += d * d;
                    }

                    norm_t = System.Math.Sqrt(norm_t);

                    #endregion
                }
            }

            private void computeLoadings()
            {
                // Compute the value of b which is used to
                // predict Y from t as b = t'u [Abdi, 2010]
                b = t.Dot(u);

                // Compute factor loadings for X as p = E'*t [Abdi, 2010]
                p = new double[xcols];
                for (var j = 0; j < p.Length; j++)
                {
                    for (var i = 0; i < rows; i++)
                    {
                        p[j] += E[i][j] * t[i];
                    }
                }
            }

            private void deflate()
            {
                // Perform deflation of X and Y
                for (var i = 0; i < t.Length; i++)
                {
                    // Deflate X as X = X - t*p';
                    for (var j = 0; j < p.Length; j++)
                    {
                        E[i][j] -= t[i] * p[j];
                    }

                    // Deflate Y as Y = Y - b*t*q';
                    for (var j = 0; j < c.Length; j++)
                    {
                        F[i][j] -= b * t[i] * c[j];
                    }
                }
            }

            private void saveFactor(int factor)
            {
                // Save iteration cols
                T.SetColumn(factor, t);
                P.SetColumn(factor, p);
                U.SetColumn(factor, u);
                C.SetColumn(factor, c);
                W.SetColumn(factor, w);
                B[factor] = b;
            }

            private bool residualsBelow(double tolerance)
            {
                // Check for residuals as stop criteria
                var norm_x = E.Euclidean(0);
                var norm_y = F.Euclidean(0);

                var stop = true;
                if (!owner.Token.IsCancellationRequested)
                {
                    for (var i = 0; i < norm_x.Length && stop; i++)
                    {
                        // If any of the residuals is higher than the tolerance
                        if (norm_x[i] > tolerance || norm_y[i] > tolerance)
                        {
                            stop = false;
                        }
                    }
                }

                return stop;
            }
        }
    }
}
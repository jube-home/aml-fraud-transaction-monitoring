using Accord.Math;
using Accord.Math.Comparers;
using Accord.Math.Decompositions;

namespace Accord.Statistics.Analysis
{
    public partial class KernelDiscriminantAnalysis
    {
#pragma warning disable 612, 618
        private double[,] createGramMatrix(int dimension)
        {
            // Create the Gram (Kernel) Matrix
            var K = new double[dimension, dimension];
            for (var i = 0; i < dimension; i++)
            {
                var row = Source.GetRow(i);
                for (var j = i; j < dimension; j++)
                {
                    var s = kernel.Function(row, Source.GetRow(j));
                    K[i, j] = s; // Assume K will be symmetric
                    K[j, i] = s;
                }
            }

            return K;
        }

        private void computeScatterMatrices(double[,] K, int dimension, double total, double[,] Sb, double[,] Sw)
        {
            // For each class
            for (var c = 0; c < Classes.Count; c++)
            {
                var classNumber = Classes[c].Number;

                // Get the Kernel matrix class subset
                var Kc = K.Submatrix(Classifications.Find(y_i => y_i == classNumber));
                var count = Kc.GetLength(0);

                // Get the Kernel matrix class mean
                var mean = Kc.Mean(0);

                // Construct the Kernel equivalent of the Within-Class Scatter matrix
                var Swi = Kc.Scatter(mean, (double)count);

                // Sw = Sw + Swi
                for (var i = 0; i < dimension; i++)
                {
                    for (var j = 0; j < dimension; j++)
                    {
                        Sw[i, j] += Swi[i, j];
                    }
                }

                // Construct the Kernel equivalent of the Between-Class Scatter matrix
                var d = mean.Subtract(Means);
                var Sbi = d.Outer(d).Multiply(total);

                // Sb = Sb + Sbi
                for (var i = 0; i < dimension; i++)
                {
                    for (var j = 0; j < dimension; j++)
                    {
                        Sb[i, j] += Sbi[i, j];
                    }
                }

                // Store additional information
                ClassScatter[c] = Swi.ToJagged();
                ClassCount[c] = count;
                ClassMeans[c] = mean;
                ClassStandardDeviations[c] = Kc.StandardDeviation(mean);
            }
        }

        private void decomposeScatterMatrices(double[,] Sb, double[,] Sw, int dimension, out double[] evals,
            out double[,] eigs)
        {
            // Add regularization to avoid singularity
            for (var i = 0; i < dimension; i++)
            {
                Sw[i, i] += regularization;
            }


            // Compute the generalized eigenvalue decomposition
            var gevd = new GeneralizedEigenvalueDecomposition(Sb, Sw);

            if (gevd.IsSingular) // check validity of the results
            {
                throw new SingularMatrixException("One of the matrices is singular. Please retry " +
                                                  "the method with a higher regularization constant.");
            }


            // Get the eigenvalues and corresponding eigenvectors
            evals = gevd.RealEigenvalues;
            eigs = gevd.Eigenvectors;

            // Sort eigenvalues and vectors in descending order
            eigs = Matrix.Sort(evals, eigs, new GeneralComparer(ComparerDirection.Descending, true));
        }

        private void discardMinorComponents(int dimension, ref double[] evals, ref double[,] eigs)
        {
            // We will be discarding less important
            // eigenvectors to conserve memory.

            // Calculate component proportions
            var sum = 0.0; // total variance
            for (var i = 0; i < dimension; i++)
            {
                sum += System.Math.Abs(evals[i]);
            }

            if (sum > 0)
            {
                var keep = 0;

                // Now we will detect how many components we have
                //  have to keep in order to achieve the level of
                //  explained variance specified by the threshold.

                while (keep < dimension)
                {
                    // Get the variance explained by the component
                    var explainedVariance = System.Math.Abs(evals[keep]);

                    // Check its proportion
                    var proportion = explainedVariance / sum;

                    // Now, if the component explains an
                    // enough proportion of the variance,
                    if (proportion > Threshold)
                    {
                        keep++; // We can keep it.
                    }
                    else
                    {
                        break; // Otherwise we can stop, since the
                    }
                    // components are ordered by variance.
                }

                if (keep > 0)
                {
                    // Resize the vectors keeping only needed components
                    eigs = eigs.Submatrix(0, dimension - 1, 0, keep - 1);
                    evals = evals.Submatrix(0, keep - 1);
                }
                else
                {
                    // No component will be kept.
                    eigs = new double[dimension, 0];
                    evals = new double[0];
                }
            }
        }
    }
}
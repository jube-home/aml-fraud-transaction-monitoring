/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    [AdvancedAnalyticsToolGroup("DistributionsAndHistograms")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class MultivariateDistributionTools
    {
        private const string MeanText = "The mean vector, one number per variable.";

        private const string CovarianceText =
            "The covariance matrix as an array of rows; it must be square, symmetric and positive definite, with size equal to the length of the mean.";

        private const string SeedText =
            "Seed of the random number generator; reuse it to reproduce the same sample. Default 1.";

        private const string CountText = "How many random draws to generate, between 1 and 100000.";

        [AdvancedAnalyticsTool("MultivariateNormalDensity")]
        [Description(
            "Evaluates the multivariate normal (Gaussian) probability density at each point, given a mean vector and covariance matrix. " +
            "For every point it returns the density, its natural logarithm and the Mahalanobis distance from the mean (the number of standard deviations away allowing for correlation; 0 at the mean). " +
            "Use the log density for high dimensions where the density underflows. " +
            "Points must have the same length as the mean.")]
        public MultivariateNormalEvaluation MultivariateNormalDensity(
            [Description(MeanText)] double[] mean,
            [Description(CovarianceText)] double[][] covariance,
            [Description("The points to evaluate, one array of numbers per point, each the same length as the mean.")]
            double[][] points)
        {
            MultivariateEngine.Vector(mean, nameof(mean));

            return MultivariateEngine.NormalDensity(mean, covariance, points);
        }

        [AdvancedAnalyticsTool("MultivariateNormalMahalanobisDistance")]
        [Description(
            "Computes the Mahalanobis distance of each point from a multivariate normal centre, which measures how far a point is in units of standard deviation while accounting for correlation and different scales of the variables. " +
            "It returns the distance, the squared distance, and the chi-square upper tail probability of the squared distance with degrees of freedom equal to the number of variables: a small tail probability (below 0.001, say) marks a multivariate outlier if the data is roughly normal. " +
            "Use MahalanobisOutlierScoresFromSample when you have only the data and no given mean and covariance.")]
        public MahalanobisResult MultivariateNormalMahalanobisDistance(
            [Description(MeanText)] double[] mean,
            [Description(CovarianceText)] double[][] covariance,
            [Description("The points to score, one array of numbers per point, each the same length as the mean.")]
            double[][] points)
        {
            MultivariateEngine.Vector(mean, nameof(mean));

            return MultivariateEngine.Mahalanobis(mean, covariance, points);
        }

        [AdvancedAnalyticsTool("MahalanobisOutlierScoresFromSample")]
        [Description(
            "Estimates the mean and covariance of a multivariate sample and scores rows by their Mahalanobis distance from the centre, the standard multivariate outlier screen. " +
            "By default every row of the sample is scored; supply pointsToScore to score other rows against the mean and covariance estimated from the sample. " +
            "It returns the distance, squared distance and chi-square upper tail probability per scored row (small values are unusual if the data is roughly normal). " +
            "The sample needs more rows than variables and no perfectly redundant variables, otherwise the covariance is singular and an error explains it.")]
        public MahalanobisResult MahalanobisOutlierScoresFromSample(
            [Description(
                "The sample rows, one array of numbers per case, all the same length, more rows than columns, no missing values.")]
            double[][] data,
            [Description(
                "Optional rows to score instead of the sample itself, each with the same number of variables. Leave empty or null to score the sample rows.")]
            double[][]? pointsToScore = null)
        {
            var rows = MultivariateEngine.Rows(data, nameof(data), 3);
            var p = rows[0].Length;

            if (rows.Length <= p)
            {
                throw new ArgumentException(
                    $"data has {rows.Length} rows but {p} variables; more rows than variables are needed to estimate the covariance.",
                    nameof(data));
            }

            var (distribution, _, _) = MultivariateEngine.Estimate(rows);

            var targets = pointsToScore is { Length: > 0 }
                ? MultivariateEngine.Rows(pointsToScore, nameof(pointsToScore), 1, p)
                : rows;

            return MultivariateEngine.MahalanobisFor(distribution, targets, p);
        }

        [AdvancedAnalyticsTool("FitMultivariateNormal")]
        [Description(
            "Fits a multivariate normal distribution to rows of data by maximum likelihood, returning the mean vector, the (unbiased) covariance matrix, the correlation matrix, the standard deviations and the total log-likelihood of the sample. " +
            "The sample needs more rows than variables and no constant or perfectly correlated variables, otherwise the covariance is singular and an error explains it.")]
        public MultivariateNormalFitResult FitMultivariateNormal(
            [Description(
                "The sample rows, one array of numbers per case, all the same length, more rows than columns, no missing values.")]
            double[][] data)
        {
            var rows = MultivariateEngine.Rows(data, nameof(data), 3);

            if (rows.Length <= rows[0].Length)
            {
                throw new ArgumentException(
                    $"data has {rows.Length} rows but {rows[0].Length} variables; more rows than variables are needed.",
                    nameof(data));
            }

            return MultivariateEngine.FitNormal(rows);
        }

        [AdvancedAnalyticsTool("MultivariateNormalRandomSample")]
        [Description(
            "Draws random vectors from a multivariate normal distribution with a given mean and covariance. " +
            "The same seed always gives the same rows. Returns the rows and their column means as a sanity check.")]
        public MultivariateSampleResult MultivariateNormalRandomSample(
            [Description(MeanText)] double[] mean,
            [Description(CovarianceText)] double[][] covariance,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            MultivariateEngine.Vector(mean, nameof(mean));
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleNormal(mean, covariance, count, seed);
        }

        [AdvancedAnalyticsTool("MultivariateNormalOrthantProbability")]
        [Description(
            "Evaluates the multivariate normal cumulative distribution: for each point, the probability that every variable is at or below its coordinate (lowerOrthantProbability) and the probability that every variable is above its coordinate (upperOrthantProbability). " +
            "In one dimension these are the ordinary cumulative and upper tail probabilities. Use it for joint exceedance risks. " +
            "Higher dimensions are computed numerically and are approximate; a value is null if it could not be computed.")]
        public MultivariateNormalCumulativeResult MultivariateNormalOrthantProbability(
            [Description(MeanText)] double[] mean,
            [Description(CovarianceText)] double[][] covariance,
            [Description("The points to evaluate, one array of numbers per point, each the same length as the mean.")]
            double[][] points)
        {
            MultivariateEngine.Vector(mean, nameof(mean));

            if (mean.Length > 8)
            {
                throw new ArgumentException("At most 8 dimensions are supported for cumulative probabilities.",
                    nameof(mean));
            }

            return MultivariateEngine.NormalOrthant(mean, covariance, points);
        }

        [AdvancedAnalyticsTool("DirichletSummary")]
        [Description(
            "Summarises a Dirichlet distribution, the distribution of a set of proportions that sum to 1 (a multivariate beta). " +
            "Given the positive concentration parameters it returns the mean of each proportion (concentration divided by the total), the variance of each, the covariance matrix (proportions are negatively correlated because they sum to 1) and the total concentration, which measures how tightly the proportions cluster around the mean. " +
            "Use it for uncertainty about category shares, such as a distribution of transaction types.")]
        public DirichletSummary DirichletSummary(
            [Description(
                "The positive concentration parameters, one per component, at least 2. A larger total means less uncertainty.")]
            double[] concentrations)
        {
            return MultivariateEngine.DirichletSummaryOf(concentrations);
        }

        [AdvancedAnalyticsTool("DirichletDensity")]
        [Description(
            "Evaluates the Dirichlet probability density at points on the simplex, that is at proportion vectors whose entries are strictly between 0 and 1 and sum to 1. " +
            "The result has one density per point in the values array; the inputs array echoes the first component of each point for reference. " +
            "A point that is not on the simplex is an error.")]
        public DistributionEvaluation DirichletDensity(
            [Description("The positive concentration parameters, one per component, at least 2.")]
            double[] concentrations,
            [Description(
                "The proportion vectors to evaluate, one array per point, each the same length as concentrations, entries in (0,1) summing to 1.")]
            double[][] points)
        {
            return MultivariateEngine.DirichletDensity(concentrations, points);
        }

        [AdvancedAnalyticsTool("FitDirichlet")]
        [Description(
            "Estimates the concentration parameters of a Dirichlet distribution from observed proportion vectors by numerical maximum likelihood. " +
            "Each data row must be a set of proportions strictly between 0 and 1 summing to 1 (convert counts to shares first, and avoid exact zeros). " +
            "Returns the concentrations, the implied mean proportions, the total concentration (precision) and the log-likelihood. Between 2 and 12 components are supported.")]
        public DirichletFitResult FitDirichlet(
            [Description(
                "The observed proportion vectors, one array per case, all the same length, each summing to 1.")]
            double[][] data)
        {
            var rows = MultivariateEngine.Rows(data, nameof(data), 2);

            return MultivariateEngine.FitDirichlet(rows);
        }

        [AdvancedAnalyticsTool("DirichletRandomSample")]
        [Description(
            "Draws random proportion vectors from a Dirichlet distribution. Each row sums to 1. The same seed always gives the same rows. " +
            "Returns the rows and the mean of each component as a sanity check.")]
        public MultivariateSampleResult DirichletRandomSample(
            [Description("The positive concentration parameters, one per component, at least 2.")]
            double[] concentrations,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleDirichlet(concentrations, count, seed);
        }

        [AdvancedAnalyticsTool("MultinomialSummary")]
        [Description(
            "Summarises a multinomial distribution: the counts in each of several categories after a fixed number of independent trials. " +
            "Returns the expected count of each category (trials times probability), its variance and the covariance matrix of the counts (categories compete, so covariances are negative).")]
        public MultinomialSummary MultinomialSummary(
            [Description("The number of independent trials, at least 1.")]
            int trials,
            [Description("The probability of each category, at least 2, each between 0 and 1, summing to 1.")]
            double[] probabilities)
        {
            return MultivariateEngine.MultinomialSummaryOf(trials, probabilities);
        }

        [AdvancedAnalyticsTool("MultinomialProbability")]
        [Description(
            "Evaluates the multinomial probability mass: the probability of observing exactly the given counts in each category out of a fixed number of trials. " +
            "Each count row must sum to the number of trials, otherwise its probability is 0. " +
            "Returns the probability and its natural logarithm per row (the log is null when the probability is 0).")]
        public MultinomialEvaluation MultinomialProbability(
            [Description("The number of independent trials, at least 1.")]
            int trials,
            [Description("The probability of each category, at least 2, each between 0 and 1, summing to 1.")]
            double[] probabilities,
            [Description(
                "The count patterns to evaluate, one array of whole numbers per pattern, each as long as probabilities.")]
            double[][] counts)
        {
            return MultivariateEngine.MultinomialProbabilityOf(trials, probabilities, counts);
        }

        [AdvancedAnalyticsTool("MultinomialRandomSample")]
        [Description(
            "Draws random category counts from a multinomial distribution. Each row holds the count in every category and sums to the number of trials. The same seed always gives the same rows. " +
            "Returns the rows and the mean count per category as a sanity check.")]
        public MultivariateSampleResult MultinomialRandomSample(
            [Description("The number of independent trials in each draw, at least 1.")]
            int trials,
            [Description("The probability of each category, at least 2, each between 0 and 1, summing to 1.")]
            double[] probabilities,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleMultinomial(trials, probabilities, count, seed);
        }

        [AdvancedAnalyticsTool("WishartSummary")]
        [Description(
            "Summarises a Wishart distribution, the distribution of a sample covariance (scatter) matrix of multivariate normal data. " +
            "Given the degrees of freedom and a positive definite scale matrix it returns the mean matrix (degrees of freedom times the scale matrix) and the variance of each matrix entry as a matrix. " +
            "The degrees of freedom must be at least the matrix size.")]
        public MatrixDistributionSummary WishartSummary(
            [Description("Degrees of freedom, a whole number of at least the matrix size.")]
            int degreesOfFreedom,
            [Description("The scale matrix as an array of rows; square, symmetric and positive definite.")]
            double[][] scale)
        {
            return MultivariateEngine.WishartSummaryOf(degreesOfFreedom, scale);
        }

        [AdvancedAnalyticsTool("WishartDensity")]
        [Description(
            "Evaluates the Wishart probability density at a symmetric matrix. " +
            "A matrix that is not positive definite has density 0. Returns the density and its natural logarithm (null when the density is 0). " +
            "The matrix must have the same size as the scale matrix.")]
        public MatrixDensityResult WishartDensity(
            [Description("Degrees of freedom, a whole number of at least the matrix size.")]
            int degreesOfFreedom,
            [Description("The scale matrix as an array of rows; square, symmetric and positive definite.")]
            double[][] scale,
            [Description("The symmetric matrix at which to evaluate the density, same size as scale.")]
            double[][] matrix)
        {
            return MultivariateEngine.WishartDensityOf(degreesOfFreedom, scale, matrix);
        }

        [AdvancedAnalyticsTool("WishartRandomSample")]
        [Description(
            "Draws random symmetric positive definite matrices from a Wishart distribution, for example to simulate sample covariance matrices. The same seed always gives the same matrices. " +
            "Returns the matrices and their entrywise mean, which should approach degrees of freedom times the scale matrix.")]
        public MatrixSampleResult WishartRandomSample(
            [Description("Degrees of freedom, a whole number of at least the matrix size.")]
            int degreesOfFreedom,
            [Description("The scale matrix as an array of rows; square, symmetric and positive definite.")]
            double[][] scale,
            [Description("How many matrices to draw, between 1 and 10000.")]
            int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count), 10000);

            return MultivariateEngine.SampleWishart(degreesOfFreedom, scale, count, seed);
        }

        [AdvancedAnalyticsTool("InverseWishartSummary")]
        [Description(
            "Summarises an inverse Wishart distribution, the distribution of the inverse of a Wishart matrix and the usual prior for a covariance matrix. " +
            "Given the degrees of freedom (which must exceed the matrix size plus 1 so the mean exists) and a positive definite scale matrix it returns the mean matrix and the variance of each entry as a matrix.")]
        public MatrixDistributionSummary InverseWishartSummary(
            [Description("Degrees of freedom, a number greater than the matrix size plus 1.")]
            double degreesOfFreedom,
            [Description("The scale matrix as an array of rows; square, symmetric and positive definite.")]
            double[][] inverseScale)
        {
            return MultivariateEngine.InverseWishartSummaryOf(degreesOfFreedom, inverseScale);
        }

        [AdvancedAnalyticsTool("InverseWishartDensity")]
        [Description(
            "Evaluates the inverse Wishart probability density at a symmetric matrix. " +
            "A matrix that is not positive definite has density 0. Returns the density and its natural logarithm (null when the density is 0). The matrix must have the same size as the scale matrix.")]
        public MatrixDensityResult InverseWishartDensity(
            [Description("Degrees of freedom, a number greater than the matrix size plus 1.")]
            double degreesOfFreedom,
            [Description("The scale matrix as an array of rows; square, symmetric and positive definite.")]
            double[][] inverseScale,
            [Description("The symmetric matrix at which to evaluate the density, same size as the scale.")]
            double[][] matrix)
        {
            return MultivariateEngine.InverseWishartDensityOf(degreesOfFreedom, inverseScale, matrix);
        }

        [AdvancedAnalyticsTool("VonMisesFisherDensity")]
        [Description(
            "Evaluates the von Mises-Fisher density, the normal-like distribution of directions (unit vectors) on a sphere, around a mean direction with a concentration. " +
            "The concentration is 0 for uniform directions and larger for tighter clustering. Points are normalised to unit length before evaluation. " +
            "Only an even number of dimensions (2, 4, 6 and so on) is supported for the density; use VonMisesFisherRandomSample for others.")]
        public DirectionalEvaluation VonMisesFisherDensity(
            [Description(
                "The mean direction, a vector of an even length of at least 2; it is normalised to unit length automatically.")]
            double[] meanDirection,
            [Description("The concentration kappa, 0 or more.")]
            double concentration,
            [Description(
                "The directions to evaluate, one array per point, each as long as the mean direction and not all zero.")]
            double[][] points)
        {
            return MultivariateEngine.VonMisesFisherDensity(meanDirection, concentration, points);
        }

        [AdvancedAnalyticsTool("VonMisesFisherRandomSample")]
        [Description(
            "Draws random unit vectors from a von Mises-Fisher distribution, useful for simulating directional data around a mean direction. " +
            "Needs at least 3 dimensions (use the VonMises distribution for angles on a circle). The same seed always gives the same rows. " +
            "Returns the rows and their mean vector, whose direction should approach the mean direction.")]
        public MultivariateSampleResult VonMisesFisherRandomSample(
            [Description(
                "The mean direction, a vector of at least 3 numbers; it is normalised to unit length automatically.")]
            double[] meanDirection,
            [Description("The concentration kappa, 0 or more.")]
            double concentration,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleVonMisesFisher(meanDirection, concentration, count, seed);
        }

        [AdvancedAnalyticsTool("UniformSphereRandomSample")]
        [Description(
            "Draws random points uniformly on the surface of a sphere (a circle in 2 dimensions) with a given centre and radius. " +
            "Use it to simulate isotropic directions or points at a fixed distance from a centre. The same seed always gives the same points.")]
        public MultivariateSampleResult UniformSphereRandomSample(
            [Description("The centre of the sphere, at least 2 numbers.")]
            double[] center,
            [Description("The radius, greater than 0.")]
            double radius,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleSphere(center, radius, count, seed);
        }

        [AdvancedAnalyticsTool("UniformBallRandomSample")]
        [Description(
            "Draws random points uniformly inside a solid ball (a disc in 2 dimensions) with a given centre and radius. " +
            "Use it to simulate points scattered evenly around a centre within a maximum distance. The same seed always gives the same points.")]
        public MultivariateSampleResult UniformBallRandomSample(
            [Description("The centre of the ball, at least 2 numbers.")]
            double[] center,
            [Description("The radius, greater than 0.")]
            double radius,
            [Description(CountText)] int count,
            [Description(SeedText)] int seed = 1)
        {
            DistributionGuard.PositiveCount(count, nameof(count));

            return MultivariateEngine.SampleBall(center, radius, count, seed);
        }
    }
}
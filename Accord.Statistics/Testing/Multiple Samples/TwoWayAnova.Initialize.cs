using System;

namespace Accord.Statistics.Testing
{
    public partial class TwoWayAnova
    {
        private double computeCellMeans(double[][][] samples)
        {
            // Step 1. Calculate cell means
            cellMeans = new double[FirstFactorSamples, SecondFactorSamples];

            double sum = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    sum += cellMeans[i, j] = samples[i][j].Mean();
                }
            }

            return sum;
        }

        private void computeFactorMeans(double[][][] samples)
        {
            double sum;

            // Step 3. Calculate factor means
            aMean = new double[FirstFactorSamples];
            for (var i = 0; i < samples.Length; i++)
            {
                sum = 0;
                for (var j = 0; j < samples[i].Length; j++)
                {
                    for (var k = 0; k < samples[i][j].Length; k++)
                    {
                        sum += samples[i][j][k];
                    }
                }

                aMean[i] = sum / (SecondFactorSamples * Replications);
            }

            bMean = new double[SecondFactorSamples];
            for (var j = 0; j < samples[0].Length; j++)
            {
                sum = 0;
                for (var i = 0; i < samples.Length; i++)
                {
                    for (var k = 0; k < samples[i][j].Length; k++)
                    {
                        sum += samples[i][j][k];
                    }
                }

                bMean[j] = sum / (FirstFactorSamples * Replications);
            }
        }

        private double computeTotalSumOfSquares(double[][][] samples)
        {
            // Step 4. Calculate total sum of squares
            double ssum = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    for (var k = 0; k < samples[i][j].Length; k++)
                    {
                        var u = samples[i][j][k] - totalMean;
                        ssum += u * u;
                    }
                }
            }

            var totalSumOfSquares = ssum;
            return totalSumOfSquares;
        }

        private double computeCellSumOfSquares()
        {
            // Step 5. Calculate the cell sum of squares
            double ssum = 0;
            for (var i = 0; i < FirstFactorSamples; i++)
            {
                for (var j = 0; j < SecondFactorSamples; j++)
                {
                    var u = cellMeans[i, j] - totalMean;
                    ssum += u * u;
                }
            }

            var cellSumOfSquares = ssum * Replications;
            return cellSumOfSquares;
        }

        private double computeErrorSumOfSquares(double[][][] samples)
        {
            // Step 6. Compute within-cells error sum of squares
            double ssum = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    for (var k = 0; k < samples[i][j].Length; k++)
                    {
                        var u = samples[i][j][k] - cellMeans[i, j];
                        ssum += u * u;
                    }
                }
            }

            var errorSumOfSquares = ssum;
            return errorSumOfSquares;
        }

        private void computeFactorSumsOfSquares(out double aSumOfSquares, out double bSumOfSquares)
        {
            // Step 7. Compute factors sum of squares
            double ssum = 0;
            for (var i = 0; i < aMean.Length; i++)
            {
                var u = aMean[i] - totalMean;
                ssum += u * u;
            }

            aSumOfSquares = ssum * SecondFactorSamples * Replications;

            ssum = 0;
            for (var i = 0; i < bMean.Length; i++)
            {
                var u = bMean[i] - totalMean;
                ssum += u * u;
            }

            bSumOfSquares = ssum * FirstFactorSamples * Replications;
        }

        private static void createSignificances(TwoWayAnovaModel type,
            double aMeanSquares, double bMeanSquares, double abMeanSquares, double errorMeanSquares,
            int aDegreesOfFreedom, int bDegreesOfFreedom, int abDegreesOfFreedom, int errorDegreesOfFreedom,
            out FTest aSignificance, out FTest bSignificance, out FTest abSignificance)
        {
            // Step 10. Create the F-Statistics
            if (type == TwoWayAnovaModel.Fixed)
            {
                // Model 1: Factors A and B fixed
                aSignificance = new FTest(aMeanSquares / abMeanSquares, aDegreesOfFreedom, abDegreesOfFreedom);
                bSignificance = new FTest(bMeanSquares / abMeanSquares, bDegreesOfFreedom, abDegreesOfFreedom);
                abSignificance = new FTest(abMeanSquares / errorMeanSquares, abDegreesOfFreedom, errorDegreesOfFreedom);
            }
            else if (type == TwoWayAnovaModel.Mixed)
            {
                // Model 2: Factors A and B random
                aSignificance = new FTest(aMeanSquares / errorMeanSquares, aDegreesOfFreedom, errorDegreesOfFreedom);
                bSignificance = new FTest(bMeanSquares / errorMeanSquares, bDegreesOfFreedom, errorDegreesOfFreedom);
                abSignificance = new FTest(abMeanSquares / errorMeanSquares, abDegreesOfFreedom, errorDegreesOfFreedom);
            }
            else if (type == TwoWayAnovaModel.Random)
            {
                // Model 3: Factor A fixed, factor B random
                aSignificance = new FTest(aMeanSquares / abMeanSquares, aDegreesOfFreedom, abDegreesOfFreedom);
                bSignificance = new FTest(bMeanSquares / errorMeanSquares, bDegreesOfFreedom, errorDegreesOfFreedom);
                abSignificance = new FTest(abMeanSquares / errorMeanSquares, abDegreesOfFreedom, errorDegreesOfFreedom);
            }
            else
            {
                throw new ArgumentException("Unhandled analysis type.", "type");
            }
        }

        private void createSources(
            double cellSumOfSquares, double aSumOfSquares, double bSumOfSquares, double abSumOfSquares,
            double errorSumOfSquares, double totalSumOfSquares,
            int cellDegreesOfFreedom, int aDegreesOfFreedom, int bDegreesOfFreedom, int abDegreesOfFreedom,
            int errorDegreesOfFreedom, int totalDegreesOfFreedom,
            double aMeanSquares, double bMeanSquares, double abMeanSquares, double errorMeanSquares,
            FTest aSignificance, FTest bSignificance, FTest abSignificance)
        {
            // Step 11. Create the ANOVA table and sources
            var cell = new AnovaVariationSource(this, "Cells", cellSumOfSquares, cellDegreesOfFreedom);
            var a = new AnovaVariationSource(this, "Factor A", aSumOfSquares, aDegreesOfFreedom,
                aMeanSquares, aSignificance);
            var b = new AnovaVariationSource(this, "Factor B", bSumOfSquares, bDegreesOfFreedom,
                bMeanSquares, bSignificance);
            var ab = new AnovaVariationSource(this, "Interaction AxB", abSumOfSquares,
                abDegreesOfFreedom, abMeanSquares, abSignificance);
            var error = new AnovaVariationSource(this, "Within-cells (error)", errorSumOfSquares,
                errorDegreesOfFreedom, errorMeanSquares);
            var total =
                new AnovaVariationSource(this, "Total", totalSumOfSquares, totalDegreesOfFreedom);

            Sources = new TwoWayAnovaVariationSources
            {
                Cells = cell,
                FactorA = a,
                FactorB = b,
                Interaction = ab,
                Error = error,
                Total = total
            };

            Table = new AnovaSourceCollection(cell, a, b, ab, error, total);
        }
    }
}
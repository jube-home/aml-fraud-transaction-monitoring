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

using System;
using System.Linq;
using Accord.Statistics.Analysis;
using FluentAssertions;
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class PrincipalComponentRecallTests
    {
        private static double[][] Correlated(int n, int seed, double correlation, int noiseColumns = 0)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var x0 = SliceData.Gaussian(random);
                var x1 = correlation * x0 + Math.Sqrt(1 - correlation * correlation) * SliceData.Gaussian(random);
                var row = new double[2 + noiseColumns];
                row[0] = x0;
                row[1] = x1;

                for (var j = 0; j < noiseColumns; j++)
                {
                    row[2 + j] = SliceData.Gaussian(random);
                }

                rows[i] = row;
            }

            return rows;
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallPrincipalComponent")]
        public void RecallReproducesTrainingTimeProjectionsBitForBit()
        {
            var data = Correlated(2000, 101, 0.9);
            var result = PrincipalComponentTrainer.Train(data, Names(2), new PrincipalComponentOptions { Seed = 5 });

            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);
            recall.Variables.Should().Equal("x0", "x1");
            recall.ComponentCount.Should().Be(result.Details.RetainedComponents);

            foreach (var row in result.TrainRows.Concat(result.TestRows))
            {
                var predicted = recall.Predict(data[row]);
                predicted.Should().HaveCount(result.Details.RetainedComponents);
            }
        }

        [Fact]
        public void Pc1IsTheDominantDirectionOfPositivelyCorrelatedVariables()
        {
            var data = Correlated(3000, 102, 0.85);
            var result = PrincipalComponentTrainer.Train(data, Names(2), new PrincipalComponentOptions { Seed = 7 });
            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);

            result.Details.Eigenvalues[0].Should().BeGreaterThan(result.Details.Eigenvalues[^1]);
            result.Details.CumulativeVariance[0].Should().BeGreaterThan(0.5);

            var scores = data.Select(row => recall.Predict(row)[0]).ToArray();
            var x0 = data.Select(row => row[0]).ToArray();

            var correlation = Correlation(scores, x0);
            Math.Abs(correlation).Should().BeGreaterThan(0.5);
        }

        [Fact]
        public void RecallMatchesAccordsOwnTransformOnTheSameStandardisedRows()
        {
            var data = Correlated(1500, 103, 0.6, 1);
            var names = Names(3);
            var options = new PrincipalComponentOptions { Seed = 11, TestFraction = 0.25 };
            var result = PrincipalComponentTrainer.Train(data, names, options);
            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);

            var trainRows = result.TrainRows.Select(r => data[r]).ToArray();
            var means = new double[3];
            var deviations = new double[3];

            for (var j = 0; j < 3; j++)
            {
                var column = trainRows.Select(r => r[j]).ToArray();
                var scaler = Standardisation.Fit(column);
                means[j] = scaler.Mean;
                deviations[j] = scaler.StandardDeviation;
            }

            double[] Standardise(double[] row)
            {
                return [.. Enumerable.Range(0, 3).Select(j => Standardisation.ZScore(row[j], means[j], deviations[j]))];
            }

            var design = trainRows.Select(Standardise).ToArray();
            var analysis = new PrincipalComponentAnalysis();
            analysis.Learn(design);
            analysis.NumberOfOutputs = result.Details.RetainedComponents;

            foreach (var row in result.TestRows.Take(25))
            {
                var standardised = Standardise(data[row]);
                var oracle = analysis.Transform([standardised])[0];
                var actual = recall.Predict(data[row]);

                for (var c = 0; c < result.Details.RetainedComponents; c++)
                {
                    Math.Abs(Math.Abs(actual[c]) - Math.Abs(oracle[c])).Should().BeLessThan(1e-6);
                }
            }
        }

        [Fact]
        public void TrainingIsDeterministicForTheSameSeed()
        {
            var data = Correlated(800, 104, 0.7);
            var options = new PrincipalComponentOptions { Seed = 3 };

            var first = PrincipalComponentTrainer.Train(data, Names(2), options);
            var second = PrincipalComponentTrainer.Train(data, Names(2), options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);
        }

        [Fact]
        public void FewerRowsThanVariablesStillTrainsButCapsTheAvailableComponents()
        {
            var random = new Random(105);

            var rows = Enumerable.Range(0, 4)
                .Select(_ => Enumerable.Range(0, 10).Select(_ => SliceData.Gaussian(random)).ToArray())
                .ToArray();

            var result = PrincipalComponentTrainer.Train(rows, Names(10),
                new PrincipalComponentOptions { Seed = 1, TestFraction = 0 });

            result.Details.AvailableComponents.Should().BeLessThan(10);
            result.Details.RetainedComponents.Should().BeLessThanOrEqualTo(result.Details.AvailableComponents);

            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);
            recall.Predict(rows[0]).Should().HaveCount(result.Details.RetainedComponents);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var result = PrincipalComponentTrainer.Train(Correlated(500, 106, 0.5), Names(2),
                new PrincipalComponentOptions());

            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var data = Correlated(1000, 108, 0.6);
            var result = PrincipalComponentTrainer.Train(data, Names(2), new PrincipalComponentOptions());
            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);

            var row = new[] { 0.3, -0.2 };
            var withMissing = new[] { double.NaN, row[1] };
            var filled = new[] { result.FillValues[0], row[1] };

            recall.Predict(withMissing)[0].Should().Be(recall.Predict(filled)[0]);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var result = PrincipalComponentTrainer.Train(Correlated(1000, 109, 0.7), Names(2),
                new PrincipalComponentOptions());

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsPrincipalComponentPipelineSnapshot>(
                    result.SnapshotJson, PrincipalComponentRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("PrincipalComponentPipeline");
            envelope.Payload.Components.Should().HaveCount(result.Details.RetainedComponents);
            envelope.VariablesInOrder.Should().Equal(result.Variables);

            var parsed = JObject.Parse(result.SnapshotJson);
            var firstComponent = (JObject)parsed["payload"]!["components"]![0]!;
            firstComponent["eigenvalue"].Should().NotBeNull();
            firstComponent["eigenvector"].Should().NotBeNull();
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var result = PrincipalComponentTrainer.Train(Correlated(500, 110, 0.6), Names(2),
                new PrincipalComponentOptions());

            Action wrongKind = () => PrincipalComponentRecall.FromSnapshot(
                result.SnapshotJson.Replace("PrincipalComponentPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["components"]!).RemoveAt(0);
            Action noComponents = () => PrincipalComponentRecall.FromSnapshot(tampered.ToString());

            if (result.Details.RetainedComponents == 1)
            {
                noComponents.Should().Throw<AdvancedAnalyticsSnapshotException>();
            }

            Action garbage = () => PrincipalComponentRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void ElementZeroIsAlwaysThePc1Score()
        {
            var data = Correlated(1200, 111, 0.8, 2);

            var result = PrincipalComponentTrainer.Train(data, Names(4),
                new PrincipalComponentOptions { Seed = 9, VarianceThreshold = 0.99 });

            var recall = PrincipalComponentRecall.FromSnapshot(result.SnapshotJson);

            result.Details.Eigenvalues[0].Should().Be(result.Details.Eigenvalues.Max());

            result.Details.CumulativeVariance.Zip(result.Details.CumulativeVariance.Skip(1), (a, b) => b >= a - 1e-9)
                .Should().OnlyContain(inOrder => inOrder);

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsPrincipalComponentPipelineSnapshot>(
                    result.SnapshotJson, PrincipalComponentRecall.ModelKind);

            var firstComponent = envelope.Payload.Components[0].Eigenvector;
            var means = envelope.Payload.Means;
            var deviations = envelope.Payload.StandardDeviations;

            foreach (var row in data.Take(50))
            {
                var standardised = Enumerable.Range(0, row.Length)
                    .Select(j => Standardisation.ZScore(row[j], means[j], deviations[j])).ToArray();

                var expected = Enumerable.Range(0, row.Length).Sum(j => firstComponent[j] * standardised[j]);

                recall.Predict(row)[0].Should().BeApproximately(expected, 1e-9);
            }
        }

        private static double Correlation(double[] a, double[] b)
        {
            var meanA = a.Average();
            var meanB = b.Average();
            var covariance = 0d;
            var varianceA = 0d;
            var varianceB = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                covariance += (a[i] - meanA) * (b[i] - meanB);
                varianceA += (a[i] - meanA) * (a[i] - meanA);
                varianceB += (b[i] - meanB) * (b[i] - meanB);
            }

            return covariance / Math.Sqrt(varianceA * varianceB);
        }
    }
}
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
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Regression;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class LogisticRegressionRecoveryTests
    {
        private const double TrueIntercept = -0.7;
        private static readonly double[] trueSlopes = [0.8, -0.6, 0.4];

        private static LogisticRegressionOptions FullSample(int seed = 1)
        {
            return new LogisticRegressionOptions
            {
                ValidationFraction = 0, TestFraction = 0, Calibrate = false, Seed = seed
            };
        }

        [Fact]
        [Trait("Covers", "TrainLogisticRegression")]
        public void EstimatesRecoverTheKnownCoefficientsAtLargeSample()
        {
            var data = SliceData.Logistic(30000, 11, TrueIntercept, trueSlopes);

            var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                new LogisticRegressionOptions
                {
                    Seed = 3
                });

            var truth = new[] { TrueIntercept }.Concat(trueSlopes).ToArray();

            for (var k = 0; k < truth.Length; k++)
            {
                var row = result.Coefficients[k];
                row.StandardError.Should().NotBeNull();
                Math.Abs(row.Estimate - truth[k]).Should().BeLessThan(4d * row.StandardError!.Value, row.Name);
                Math.Abs(row.Estimate - truth[k]).Should().BeLessThan(0.08, row.Name);
            }
        }

        [Fact]
        public void WaldIntervalsCoverTheTruthAtAboutTheNominalRate()
        {
            var covered = 0;
            var total = 0;

            for (var seed = 0; seed < 250; seed++)
            {
                var data = SliceData.Logistic(400, 1000 + seed, TrueIntercept, trueSlopes);
                var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, FullSample(seed));

                for (var k = 0; k < trueSlopes.Length; k++)
                {
                    var row = result.Coefficients[k + 1];
                    var truth = Math.Exp(trueSlopes[k]);
                    total++;

                    if (row.OddsRatioLower95 <= truth && truth <= row.OddsRatioUpper95)
                    {
                        covered++;
                    }
                }
            }

            var rate = covered / (double)total;
            rate.Should().BeInRange(0.92, 0.98);
        }

        [Fact]
        public void TruePredictorsAreSignificantAndNoiseFollowsTheFalsePositiveRate()
        {
            var noiseRejections = 0;
            var noiseTests = 0;

            for (var seed = 0; seed < 300; seed++)
            {
                var data = SliceData.Logistic(1500, 5000 + seed, -0.3, [0.9, -0.8], 2);
                var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, FullSample(seed));

                result.Coefficients.Single(c => c.Name == "x0").PValue.Should().BeLessThan(0.001);
                result.Coefficients.Single(c => c.Name == "x1").PValue.Should().BeLessThan(0.001);

                foreach (var noise in new[] { "x2", "x3" })
                {
                    noiseTests++;

                    if (result.Coefficients.Single(c => c.Name == noise).PValue < 0.05)
                    {
                        noiseRejections++;
                    }
                }
            }

            (noiseRejections / (double)noiseTests).Should().BeInRange(0.025, 0.08);
        }

        [Fact]
        public void CoefficientTableMatchesAnIndependentPythonImplementation()
        {
            var data = SliceData.Reference(800, 99, 0d);
            data.Labels.Sum().Should().Be(321);

            var expected = new[]
            {
                (-0.462448040886103, 0.0782498170703716, -5.9098929326597, 3.42330161409196e-09, 0.62974012740056,
                    0.540200525875634, 0.73412114402453),
                (1.29084390295267, 0.150547682256745, 8.57431933592484, 9.96745137233913e-18, 3.63585356900239,
                    2.70682366402511, 4.88374301987957),
                (-0.0755738787627113, 0.132884829398381, -0.568717129749593, 0.569548121042672, 0.927211226785337,
                    0.714606413504855, 1.20306877020623),
                (-0.640286449374605, 0.139762218769408, -4.58125561408699, 4.62192498158869e-06, 0.527141403088909,
                    0.400831008950974, 0.693254894569625)
            };

            foreach (var standardise in new[] { true, false })
            {
                var options = FullSample() with { Standardise = standardise };
                var result = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, options);

                result.Coefficients.Should().HaveCount(4);

                for (var k = 0; k < 4; k++)
                {
                    var row = result.Coefficients[k];
                    row.Estimate.Should().BeApproximately(expected[k].Item1, 1e-7);
                    row.StandardError!.Value.Should().BeApproximately(expected[k].Item2, 1e-7);
                    row.WaldZ!.Value.Should().BeApproximately(expected[k].Item3, 1e-5);

                    row.PValue!.Value.Should()
                        .BeApproximately(expected[k].Item4, Math.Max(1e-9 * expected[k].Item4, 1e-15));

                    row.OddsRatio!.Value.Should().BeApproximately(expected[k].Item5, 1e-6);
                    row.OddsRatioLower95!.Value.Should().BeApproximately(expected[k].Item6, 1e-6);
                    row.OddsRatioUpper95!.Value.Should().BeApproximately(expected[k].Item7, 1e-6);
                }

                result.Fit.Converged.Should().BeTrue();
                result.Fit.MaximumGradient.Should().BeLessThan(1e-9);
                result.Fit.EventsPerVariable.Should().BeApproximately(321d / 3d, 1e-9);
            }
        }

        [Fact]
        public void RidgePenaltyMatchesAnIndependentPythonImplementationAndLeavesTheInterceptUnpenalised()
        {
            var data = SliceData.Reference(800, 99, 0d);

            var lambda20 = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                FullSample() with { RidgeLambda = 20d });

            var expected = new[] { -0.448343674052672, 1.12381399261732, -0.0700804423754092, -0.560886227407717 };

            for (var k = 0; k < 4; k++)
            {
                lambda20.Coefficients[k].Estimate.Should().BeApproximately(expected[k], 1e-7);
            }

            var plain = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names, FullSample());

            for (var k = 1; k < 4; k++)
            {
                Math.Abs(lambda20.Coefficients[k].Estimate).Should()
                    .BeLessThan(Math.Abs(plain.Coefficients[k].Estimate));
            }

            var enormous = LogisticRegressionTrainer.Train(data.Inputs, data.Labels, data.Names,
                FullSample() with { RidgeLambda = 1e9 });

            for (var k = 1; k < 4; k++)
            {
                Math.Abs(enormous.Coefficients[k].Estimate).Should().BeLessThan(1e-5);
            }

            var baseRate = data.Labels.Average();
            enormous.Coefficients[0].Estimate.Should().BeApproximately(Math.Log(baseRate / (1d - baseRate)), 1e-6);
            enormous.Fit.Penalised.Should().BeTrue();
            enormous.Fit.StandardErrorBasis.Should().Contain("ridge");
        }
    }
}
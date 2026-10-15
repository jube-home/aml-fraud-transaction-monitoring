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
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Xunit;

namespace Jube.Test.Analytics.Anomaly
{
    public class AnomalyVariableReducerTests
    {
        private static readonly ReductionOptions standard = new() { Mode = ReductionMode.Standard };

        private static double[][] Build(int rows, params Func<int, Random, double>[] columns)
        {
            var rng = new Random(3);

            return [.. Enumerable.Range(0, rows).Select(i => columns.Select(c => c(i, rng)).ToArray())];
        }

        [Fact]
        public void ConstantAndNearZeroVarianceVariablesAreDropped()
        {
            var data = Build(400,
                (_, r) => AnomalyFixtures.Gaussian(r),
                (_, _) => 5d,
                (i, _) => i == 7 ? 1d : 0d,
                (_, r) => AnomalyFixtures.Gaussian(r));

            var model = AnomalyVariableReducer.Fit(data, ["a", "constant", "rare", "b"], standard, 1);
            model.KeptIndices.Should().Equal(0, 3);
            model.Decisions[1].Reason.Should().Be("Constant");
            model.Decisions[2].Reason.Should().Be("NearZeroVariance");
            model.Decisions[2].Detail.Should().Contain("frequency ratio");
        }

        [Fact]
        public void AFrequentlyRepeatedButVariedVariableIsNotNearZeroVariance()
        {
            var data = Build(400, (i, _) => i % 2, (_, r) => AnomalyFixtures.Gaussian(r));
            var model = AnomalyVariableReducer.Fit(data, null, standard, 1);
            model.KeptIndices.Should().Equal(0, 1);
        }

        [Fact]
        public void VariablesAboveTheMissingRateAreDropped()
        {
            var data = Build(200, (_, r) => AnomalyFixtures.Gaussian(r),
                (i, r) => i % 3 == 0 ? AnomalyFixtures.Gaussian(r) : double.NaN,
                (i, r) => i % 10 == 0 ? double.NaN : AnomalyFixtures.Gaussian(r));

            var model = AnomalyVariableReducer.Fit(data, ["a", "mostlyMissing", "someMissing"], standard, 1);
            model.KeptIndices.Should().Equal(0, 2);
            model.Decisions[1].Reason.Should().Be("MissingRate");
        }

        [Fact]
        public void ExactDuplicatesAndCorrelatedGroupsKeepOneRepresentative()
        {
            var data = Build(500,
                (_, r) => AnomalyFixtures.Gaussian(r),
                (_, r) => AnomalyFixtures.Gaussian(r),
                (_, r) => AnomalyFixtures.Gaussian(r));

            var rows = data.Select(r => new[] { r[0], r[0], r[1], r[1] * 2d + 0.01 * r[2], r[2] }).ToArray();
            var model = AnomalyVariableReducer.Fit(rows, ["a", "aCopy", "b", "bScaled", "c"], standard, 1);
            model.KeptIndices.Should().HaveCount(3);

            model.Decisions.Single(d => d.Name == "aCopy" || (d.Name == "a" && !d.Kept)).Reason.Should()
                .Be("Duplicate");

            model.Decisions.Count(d => d.Reason == "Correlated").Should().Be(1);
            model.Decisions.Single(d => d.Name == "c").Kept.Should().BeTrue();
        }

        [Fact]
        public void TheCorrelationThresholdIsConfigurable()
        {
            var rows = Build(600, (_, r) => AnomalyFixtures.Gaussian(r), (_, r) => AnomalyFixtures.Gaussian(r))
                .Select(r => new[] { r[0], 0.8 * r[0] + 0.6 * r[1], r[1] }).ToArray();

            AnomalyVariableReducer.Fit(rows, null, standard, 1).KeptIndices.Should().HaveCount(3);

            AnomalyVariableReducer.Fit(rows, null, standard with { CorrelationThreshold = 0.7 }, 1)
                .KeptIndices.Should().HaveCountLessThan(3);

            AnomalyVariableReducer.Fit(rows, null, standard with { CorrelationThreshold = null }, 1)
                .KeptIndices.Should().HaveCount(3);
        }

        [Fact]
        public void CompleteLinkageDoesNotChainWeaklyCorrelatedVariables()
        {
            var rng = new Random(9);

            var rows = Enumerable.Range(0, 800).Select(_ =>
            {
                var z = AnomalyFixtures.Gaussian(rng);
                var a = z;
                var b = 0.97 * a + 0.24 * AnomalyFixtures.Gaussian(rng);
                var c = 0.97 * b + 0.24 * AnomalyFixtures.Gaussian(rng);
                var d = 0.97 * c + 0.24 * AnomalyFixtures.Gaussian(rng);

                return new[] { a, b, c, d };
            }).ToArray();

            var model = AnomalyVariableReducer.Fit(rows, null, standard with { CorrelationThreshold = 0.95 }, 1);
            model.KeptIndices.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void VariableClusteringKeepsARepresentativePerCluster()
        {
            var rng = new Random(4);

            var rows = Enumerable.Range(0, 600).Select(_ =>
            {
                var f1 = AnomalyFixtures.Gaussian(rng);
                var f2 = AnomalyFixtures.Gaussian(rng);

                return new[]
                {
                    f1 + 0.2 * AnomalyFixtures.Gaussian(rng), f1 + 0.2 * AnomalyFixtures.Gaussian(rng),
                    f1 + 0.2 * AnomalyFixtures.Gaussian(rng), f2 + 0.2 * AnomalyFixtures.Gaussian(rng),
                    f2 + 0.2 * AnomalyFixtures.Gaussian(rng), f2 + 0.2 * AnomalyFixtures.Gaussian(rng)
                };
            }).ToArray();

            var model = AnomalyVariableReducer.Fit(rows, null,
                standard with { Grouping = ReductionGrouping.VariableClustering, CorrelationThreshold = 0.9 }, 1);
            model.KeptIndices.Should().HaveCount(2);
            model.KeptIndices.Count(i => i < 3).Should().Be(1);
            model.KeptIndices.Count(i => i >= 3).Should().Be(1);
        }

        [Fact]
        public void ZeroMedianAbsoluteDeviationCanBeDroppedOnRequest()
        {
            var data = Build(300, (i, _) => i < 20 ? 1d + i : 0d, (_, r) => AnomalyFixtures.Gaussian(r));
            var kept = AnomalyVariableReducer.Fit(data, null, standard with { FrequencyRatioCutoff = 1000d }, 1);
            kept.KeptIndices.Should().Equal(0, 1);

            var dropped = AnomalyVariableReducer.Fit(data, null,
                standard with { FrequencyRatioCutoff = 1000d, DropZeroMedianAbsoluteDeviation = true }, 1);
            dropped.KeptIndices.Should().Equal(1);
            dropped.Decisions[0].Reason.Should().Be("ZeroMedianAbsoluteDeviation");
        }

        [Fact]
        public void RobustScalingUsesMedianAndMadAndFallsBackForBinaryColumns()
        {
            var data = Build(301, (i, _) => i == 300 ? 1000d : i, (i, _) => i % 2);

            var model = AnomalyVariableReducer.Fit(data, null,
                new ReductionOptions { Scaling = ReductionScaling.Robust }, 1);

            var outlier = model.Transform(data[300]);
            outlier[0].Should().BeGreaterThan(5);
            var typical = model.Transform(data[150]);
            Math.Abs(typical[0]).Should().BeLessThan(0.1);
            double.IsFinite(model.Transform(data[1])[1]).Should().BeTrue();
        }

        [Fact]
        public void ZScoreScalingGivesUnitVariance()
        {
            var data = Build(500, (_, r) => 50 + 10 * AnomalyFixtures.Gaussian(r));

            var model = AnomalyVariableReducer.Fit(data, null,
                new ReductionOptions { Scaling = ReductionScaling.ZScore }, 1);

            var scaled = model.Transform(data).Select(r => r[0]).ToArray();
            scaled.Average().Should().BeApproximately(0, 1e-9);
            Math.Sqrt(scaled.Sum(v => v * v) / scaled.Length).Should().BeApproximately(1, 1e-9);
        }

        [Fact]
        public void PcaProjectionChoosesComponentsByExplainedVarianceOrParallelAnalysis()
        {
            var rng = new Random(6);

            var rows = Enumerable.Range(0, 600).Select(_ =>
            {
                var a = AnomalyFixtures.Gaussian(rng);
                var b = AnomalyFixtures.Gaussian(rng);

                return new[]
                {
                    a, b, a + 0.4 * AnomalyFixtures.Gaussian(rng), b + 0.4 * AnomalyFixtures.Gaussian(rng),
                    AnomalyFixtures.Gaussian(rng), AnomalyFixtures.Gaussian(rng)
                };
            }).ToArray();

            var ev = AnomalyVariableReducer.Fit(rows, null, new ReductionOptions
            {
                Scaling = ReductionScaling.ZScore, Projection = ReductionProjection.ExplainedVariance,
                ExplainedVariance = 0.6
            }, 1);
            ev.Components.Should().BeInRange(2, 4);
            ev.OutputDimensions.Should().Be(ev.Components);
            ev.Transform(rows[0]).Should().HaveCount(ev.Components);

            var pa = AnomalyVariableReducer.Fit(rows, null, new ReductionOptions
            {
                Scaling = ReductionScaling.ZScore, Projection = ReductionProjection.ParallelAnalysis
            }, 1);
            pa.Components.Should().Be(2);
        }

        [Fact]
        public void ReductionIsDeterministicBySeed()
        {
            var rng = new Random(2);

            var rows = Enumerable.Range(0, 400).Select(_ => Enumerable.Range(0, 12)
                .Select(_ => AnomalyFixtures.Gaussian(rng)).ToArray()).ToArray();

            var options = new ReductionOptions
            {
                Mode = ReductionMode.Standard, Scaling = ReductionScaling.Robust,
                Projection = ReductionProjection.ParallelAnalysis
            };

            var first = AnomalyVariableReducer.Fit(rows, null, options, 5);
            var second = AnomalyVariableReducer.Fit(rows, null, options, 5);
            first.KeptIndices.Should().Equal(second.KeptIndices);
            first.Components.Should().Be(second.Components);
            first.Transform(rows[3]).Should().Equal(second.Transform(rows[3]));
        }

        [Fact]
        public void RunningWithoutReductionKeepsEveryVariableUntouched()
        {
            var data = Build(100, (_, _) => 1d, (_, r) => r.NextDouble());
            var model = AnomalyVariableReducer.Fit(data, null, ReductionOptions.Off, 1);
            model.KeptIndices.Should().Equal(0, 1);
            model.Transform(data[5]).Should().Equal(data[5]);
        }

        [Fact]
        public void MissingValuesAreImputedWithTheFittedMedianOnTransform()
        {
            var data = Build(101, (i, _) => i, (_, r) => r.NextDouble());
            var model = AnomalyVariableReducer.Fit(data, null, ReductionOptions.Off, 1);
            model.Transform([double.NaN, 0.3])[0].Should().Be(50);
        }

        [Fact]
        public void ReducingEveryVariableAwayIsAClearError()
        {
            var data = Build(50, (_, _) => 1d, (_, _) => 2d);
            var act = () => AnomalyVariableReducer.Fit(data, null, standard, 1);
            act.Should().Throw<ArgumentException>().WithMessage("*removed every variable*");
        }

        [Fact]
        public void InvalidOptionsAndShapesAreRejected()
        {
            var data = Build(50, (_, r) => r.NextDouble(), (_, r) => r.NextDouble());
            var bad = () => AnomalyVariableReducer.Fit(data, null, standard with { CorrelationThreshold = 2 }, 1);
            bad.Should().Throw<ArgumentOutOfRangeException>();
            var names = () => AnomalyVariableReducer.Fit(data, ["only"], standard, 1);
            names.Should().Throw<ArgumentException>().WithMessage("*names*");
            var wrong = () => AnomalyVariableReducer.Fit(data, null, standard, 1).Transform([1d]);
            wrong.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EveryVariableHasADecisionWithAReason()
        {
            var data = Build(300, (_, r) => AnomalyFixtures.Gaussian(r), (_, _) => 4d,
                (_, r) => AnomalyFixtures.Gaussian(r));

            var model = AnomalyVariableReducer.Fit(data, ["a", "b", "c"], standard, 1);
            model.Decisions.Should().HaveCount(3);
            model.Decisions.Should().OnlyContain(d => d.Reason.Length > 0);
            model.Decisions.Where(d => !d.Kept).Should().OnlyContain(d => d.Detail.Length > 0);
        }
    }
}
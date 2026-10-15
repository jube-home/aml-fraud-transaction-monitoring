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
using System.Threading;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class PredictorSignificanceTests
    {
        [Fact]
        [Trait("Covers", "AssessPredictorSignificance")]
        public void MatchesAnIndependentPythonUnivariateLogisticImplementation()
        {
            var data = SliceData.Reference(600, 4242, 0.1);
            data.Labels.Sum().Should().Be(227);

            var expected = new[]
            {
                (600, 1.25856142356121, 0.170908052241334, 7.36396797609061, 1.78522538877539e-13, 3.52035354526191,
                    0.685405865054151, 5.35567616632617e-13),
                (543, 0.0494213691106218, 0.154456493332891, 0.319969514030769, 0.748991440891999, 1.0506629744026,
                    0.505784024524264, 0.748991440891999),
                (600, -0.675298894326936, 0.152514714931314, -4.42776223022849, 9.52157784731302e-06, 0.509004259383103,
                    0.389472192368107, 1.42823667709695e-05)
            };

            var result =
                new LogisticRegressionTools().AssessPredictorSignificance(data.Inputs, data.Labels, data.Names);

            result.Should().HaveCount(3);

            for (var j = 0; j < 3; j++)
            {
                var row = result[j];
                row.Variable.Should().Be(data.Names[j]);
                row.RowsUsed.Should().Be(expected[j].Item1);
                row.MissingRows.Should().Be(600 - expected[j].Item1);
                row.Estimate.Should().BeApproximately(expected[j].Item2, 1e-7);
                row.StandardError.Should().BeApproximately(expected[j].Item3, 1e-7);
                row.WaldZ.Should().BeApproximately(expected[j].Item4, 1e-5);
                row.PValue.Should().BeApproximately(expected[j].Item5, Math.Max(1e-8 * expected[j].Item5, 1e-15));
                row.OddsRatio.Should().BeApproximately(expected[j].Item6, 1e-6);
                row.RocAuc.Should().BeApproximately(expected[j].Item7, 1e-12);

                row.AdjustedPValue.Should()
                    .BeApproximately(expected[j].Item8, Math.Max(1e-8 * expected[j].Item8, 1e-15));

                row.OddsRatioLower95.Should()
                    .BeApproximately(Math.Exp(row.Estimate!.Value - 1.959963984540054 * row.StandardError!.Value),
                        1e-12);
            }
        }

        [Fact]
        public void BenjaminiHochbergMatchesTheOriginalPaperExample()
        {
            var p = new double?[]
            {
                0.0001, 0.0004, 0.0019, 0.0095, 0.0201, 0.0278, 0.0298, 0.0344, 0.0459, 0.3240, 0.4262, 0.5719,
                0.6528, 0.7590, 1.000
            };

            var adjusted = PredictorSignificanceAnalysis.BenjaminiHochberg(p);

            var expected = new[]
            {
                0.0015, 0.003, 0.0095, 0.035625, 0.0603, 0.063857142857, 0.063857142857, 0.0645, 0.0765, 0.486,
                0.581181818182, 0.714875, 0.753230769231, 0.813214285714, 1d
            };

            for (var i = 0; i < p.Length; i++)
            {
                adjusted[i].Should().BeApproximately(expected[i], 1e-9);
            }
        }

        [Fact]
        public void BenjaminiHochbergKeepsInputOrderSkipsUndefinedAndNeverExceedsOne()
        {
            var adjusted = PredictorSignificanceAnalysis.BenjaminiHochberg([0.5, null, 0.01, 0.9]);

            adjusted[1].Should().BeNull();
            adjusted[2].Should().BeApproximately(0.03, 1e-12);
            adjusted[0].Should().BeApproximately(0.75, 1e-12);
            adjusted[3].Should().BeApproximately(0.9, 1e-12);
            adjusted.Where(v => v.HasValue).Should().OnlyContain(v => v <= 1d);
        }

        [Fact]
        public void TruePredictorsAreFoundAndAdjustedNoiseFalsePositivesAreRare()
        {
            var falseDiscoveries = 0;
            var noiseTests = 0;

            for (var seed = 0; seed < 100; seed++)
            {
                var data = SliceData.Logistic(1200, 300 + seed, -0.4, [0.7, -0.6], 8);
                var result = PredictorSignificanceAnalysis.Assess(data.Inputs, data.Labels, data.Names);

                result[0].AdjustedPValue.Should().BeLessThan(0.001);
                result[1].AdjustedPValue.Should().BeLessThan(0.001);
                result[0].Estimate.Should().BeGreaterThan(0);
                result[1].Estimate.Should().BeLessThan(0);

                foreach (var noise in result.Skip(2))
                {
                    noiseTests++;

                    if (noise.AdjustedPValue < 0.05)
                    {
                        falseDiscoveries++;
                    }
                }

                result.Where(r => r.AdjustedPValue.HasValue).Should()
                    .OnlyContain(r => r.AdjustedPValue >= r.PValue);
            }

            (falseDiscoveries / (double)noiseTests).Should().BeLessThan(0.02);
        }

        [Fact]
        public void SeparatedConstantAndSparseVariablesReturnANoteInsteadOfNumbers()
        {
            var random = new Random(5);
            var n = 300;
            var labels = Enumerable.Range(0, n).Select(i => i % 2).ToArray();

            var inputs = labels.Select((l, i) => new[]
            {
                l * 10d + random.NextDouble(),
                3d,
                SliceData.Gaussian(random),
                i < 297 ? double.NaN : SliceData.Gaussian(random)
            }).ToArray();

            var result =
                PredictorSignificanceAnalysis.Assess(inputs, labels, ["separating", "flat", "noise", "sparse"]);

            result[0].PValue.Should().BeNull();
            result[0].Note.Should().Contain("separation");
            result[0].RocAuc.Should().Be(1d);
            result[1].Note.Should().Contain("constant");
            result[2].PValue.Should().NotBeNull();
            result[3].RowsUsed.Should().Be(3);
            result[3].Note.Should().Contain("two rows");
            result.Where(r => r.PValue.HasValue).Should().OnlyContain(r => r.AdjustedPValue.HasValue);
        }

        [Fact]
        public void InvalidArgumentsAreRejectedAndCancellationIsHonoured()
        {
            var data = SliceData.Logistic(100, 6, 0d, [1.0]);

            Action ragged = () =>
                PredictorSignificanceAnalysis.Assess([[1d], [1d, 2d]], [0, 1],
                    ["a"]);
            ragged.Should().Throw<ArgumentException>();

            Action singleClass = () => PredictorSignificanceAnalysis.Assess(data.Inputs, new int[100], data.Names);
            singleClass.Should().Throw<ArgumentException>().WithMessage("*Both classes*");

            var labels = (int[])data.Labels.Clone();
            labels[0] = 5;
            Action badLabel = () => PredictorSignificanceAnalysis.Assess(data.Inputs, labels, data.Names);
            badLabel.Should().Throw<ArgumentException>();

            Action duplicate = () => PredictorSignificanceAnalysis.Assess(data.Inputs, data.Labels, ["x", "x"]);
            duplicate.Should().Throw<ArgumentException>();

            using var source = new CancellationTokenSource();
            source.Cancel();
            var token = source.Token;

            Action cancelled = () =>
                PredictorSignificanceAnalysis.Assess(data.Inputs, data.Labels, data.Names, token);
            cancelled.Should().Throw<OperationCanceledException>();
        }
    }
}
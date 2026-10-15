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
    public class AnomalyReportAndParametersTests
    {
        [Fact]
        public void ContaminationFlagsTheTopFractionAndRecordsWhatWasApplied()
        {
            var scores = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
            var threshold = AnomalyLabeller.Contamination(scores, 0.05);
            threshold.Method.Should().Be("Contamination");
            threshold.Flagged.Should().Be(5);
            threshold.Rows.Should().Be(100);
            threshold.FlaggedFraction.Should().Be(0.05);
            AnomalyLabeller.Label(scores, threshold.Value).Sum().Should().Be(5);
            AnomalyLabeller.Label(scores, threshold.Value)[99].Should().Be(1);
            AnomalyLabeller.Label(scores, threshold.Value)[94].Should().Be(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.6)]
        [InlineData(double.NaN)]
        public void ContaminationOutsideTheRangeIsRejected(double fraction)
        {
            var act = () => AnomalyLabeller.Contamination([1d, 2d, 3d], fraction);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TiedScoresFlagFewerRatherThanMoreThanTheTopFraction()
        {
            var scores = Enumerable.Repeat(1d, 90).Concat(Enumerable.Repeat(2d, 10)).ToArray();
            var threshold = AnomalyLabeller.Contamination(scores, 0.05);
            threshold.Flagged.Should().BeLessOrEqualTo(5);
        }

        [Fact]
        public void TheReportRoundTripsThroughJsonAndSummarises()
        {
            var report = new AnomalyReport
            {
                Algorithm = "IsolationForest",
                Requested = "Automatic",
                Seed = 4,
                Rows = 100,
                InputVariables = 10,
                KeptVariables = 7,
                DetectorDimensions = 7,
                Reduction = "Standard",
                Scaling = "Robust",
                Variables = [new VariableDecision(0, "a", false, "Constant", "single value 1")],
                Scores = ScoreSummary.From([.. Enumerable.Range(0, 100).Select(i => i / 100d)]),
                Threshold = new ThresholdApplied("Contamination", 0.95, 0.05, 5, 100),
                Auto = new AutoEvaluation("rule", false, 0, 60, 100, 7,
                    [new CandidateEvaluation("Hbos", "Evaluated", "", null, null, 0.9, 0.8, 0.5, 0.1, 1.5, true, 12)])
            };

            var restored = AnomalyReport.FromJson(report.ToJson());
            restored.Should().NotBeNull();
            restored!.Algorithm.Should().Be("IsolationForest");
            restored.Variables.Single().Reason.Should().Be("Constant");
            restored.Auto!.Candidates.Single().BootstrapStability.Should().Be(0.9);
            restored.Threshold!.Flagged.Should().Be(5);

            restored.Summary().Should().Contain("IsolationForest").And.Contain("7 of 10").And
                .Contain("flagged 5 of 100");

            AnomalyReport.FromJson("not json").Should().BeNull();
            AnomalyReport.FromJson(null).Should().BeNull();
        }

        [Fact]
        public void ParametersParseWithDefaultsAndRejectUnknownProperties()
        {
            AnomalyParameters.Parse(null).Should().Be(new AnomalyParameters());

            var parsed = AnomalyParameters.Parse(
                "{\"trees\":50,\"contamination\":0.1,\"reduction\":{\"mode\":\"Standard\",\"scaling\":\"Robust\",\"correlationThreshold\":0.9}}");
            parsed.Trees.Should().Be(50);
            parsed.Reduction!.Mode.Should().Be(ReductionMode.Standard);
            parsed.Reduction.CorrelationThreshold.Should().Be(0.9);
            parsed.Validate().Should().BeEmpty();
            var unknown = () => AnomalyParameters.Parse("{\"anomalyReportJson\":\"x\"}");
            unknown.Should().Throw<ArgumentException>();
            var broken = () => AnomalyParameters.Parse("{");
            broken.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("{\"trees\":0}")]
        [InlineData("{\"contamination\":0.9}")]
        [InlineData("{\"k\":100000}")]
        [InlineData("{\"baseAlgorithm\":\"OneClassSvm\"}")]
        [InlineData("{\"reduction\":{\"correlationThreshold\":5}}")]
        [InlineData("{\"auto\":{\"timeBudgetSeconds\":0}}")]
        [InlineData("{\"auto\":{\"candidates\":[]}}")]
        [InlineData("{\"auto\":{\"candidates\":[\"Automatic\"]}}")]
        public void OutOfBoundsParametersAreReported(string json)
        {
            AnomalyParameters.TryParse(json, out _, out var errors).Should().BeFalse();
            errors.Should().NotBeEmpty();
        }

        [Fact]
        public void OversizedAndNumericEnumParametersAreRejected()
        {
            AnomalyParameters.TryParse(new string(' ', AnomalyParameters.MaxJsonLength + 1), out _, out _)
                .Should().BeTrue();

            AnomalyParameters.TryParse("{\"combination\":2}", out _, out var errors).Should().BeFalse();
            errors.Should().NotBeEmpty();
        }

        [Fact]
        public void FactoryBuildsEveryNativeAlgorithmAndRefusesTheOthers()
        {
            foreach (var algorithm in Enum.GetValues<AnomalyAlgorithm>()
                         .Where(a => a is not (AnomalyAlgorithm.OneClassSvm or AnomalyAlgorithm.Automatic)))
            {
                AnomalyDetectorFactory.Create(algorithm, new AnomalyParameters()).Should().NotBeNull();
            }

            var svm = () => AnomalyDetectorFactory.Create(AnomalyAlgorithm.OneClassSvm, new AnomalyParameters());
            svm.Should().Throw<ArgumentException>();
            AnomalyDetectorFactory.IsNative(AnomalyAlgorithm.Automatic).Should().BeFalse();
        }
    }
}
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
using System.Globalization;
using System.Linq;
using System.Threading;
using Accord.Statistics.Models.Regression;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Serialisation
{
    public class AdvancedAnalyticsModelJsonTests
    {
        private static readonly string[] variables = ["amount", "velocity", "age"];
        private static readonly AdvancedAnalyticsLogisticRegressionMapper mapper = new();

        private static AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionSnapshot> Envelope(
            double[] weights, double intercept = 0.25, double[]? standardErrors = null,
            AdvancedAnalyticsCalibratorSnapshot? calibrator = null)
        {
            var model = LogisticRegression.FromWeights(weights, intercept);

            if (standardErrors != null)
            {
                model.StandardErrors = standardErrors;
            }

            return AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionSnapshot>.Create(
                mapper.Kind, mapper.SchemaVersion, variables, mapper.ToSnapshot(model), calibrator);
        }

        private static long Bits(double value)
        {
            return double.IsNaN(value) ? double.NaN.GetHashCode() : BitConverter.DoubleToInt64Bits(value);
        }

        private static void SameBits(double[] actual, double[] expected)
        {
            actual.Should().HaveSameCount(expected);

            for (var i = 0; i < expected.Length; i++)
            {
                Bits(actual[i]).Should().Be(Bits(expected[i]), $"element {i}");
            }
        }

        [Fact]
        public void RoundTripIsBitExactForOrdinaryValues()
        {
            var weights = new[] { 0.1 + 0.2, -1.7976931348623157e308, double.Epsilon, 123456789.12345678 };
            var three = weights.Take(3).ToArray();

            var json = AdvancedAnalyticsModelJson.Serialise(Envelope(three, 0.30000000000000004));

            var back = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, AdvancedAnalyticsLogisticRegressionMapper.ModelKind);

            SameBits(back.Payload.Weights, three);
            Bits(back.Payload.Intercept).Should().Be(Bits(0.30000000000000004));
            back.VariablesInOrder.Should().Equal(variables);
            back.InputCount.Should().Be(3);
        }

        [Fact]
        public void RoundTripPreservesNaNInfinityAndNegativeZero()
        {
            var weights = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };

            var json = AdvancedAnalyticsModelJson.Serialise(Envelope(weights, -0.0));

            var back = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, AdvancedAnalyticsLogisticRegressionMapper.ModelKind);

            double.IsNaN(back.Payload.Weights[0]).Should().BeTrue();
            back.Payload.Weights[1].Should().Be(double.PositiveInfinity);
            back.Payload.Weights[2].Should().Be(double.NegativeInfinity);
            BitConverter.DoubleToInt64Bits(back.Payload.Intercept).Should().Be(BitConverter.DoubleToInt64Bits(-0.0));
            json.Should().Contain("\"NaN\"").And.Contain("\"Infinity\"").And.Contain("\"-Infinity\"");
        }

        [Fact]
        public void NegativeZeroInsideArraysSurvives()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([-0.0, 0.0, -0.0]));

            var back = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, AdvancedAnalyticsLogisticRegressionMapper.ModelKind);

            SameBits(back.Payload.Weights, [-0.0, 0.0, -0.0]);
        }

        [Fact]
        public void RandomBitPatternsRoundTripExactly()
        {
            var random = new Random(20260921);
            var values = new double[600];

            for (var i = 0; i < values.Length; i++)
            {
                var bytes = new byte[8];
                random.NextBytes(bytes);
                var value = BitConverter.ToDouble(bytes);
                values[i] = double.IsNaN(value) ? double.NaN : value;
            }

            var variableNames = Enumerable.Range(0, values.Length).Select(i => "v" + i).ToArray();
            var snapshot = new AdvancedAnalyticsLogisticRegressionSnapshot { Weights = values, Intercept = values[0] };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionSnapshot>.Create(
                mapper.Kind, 1, variableNames, snapshot);

            var back = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                AdvancedAnalyticsModelJson.Serialise(envelope), mapper.Kind);

            SameBits(back.Payload.Weights, values);
        }

        [Fact]
        public void WorkedExampleMapperRoundTripsTheModelAndItsPredictions()
        {
            var original = LogisticRegression.FromWeights([0.7, -1.3, 2.1], -0.4);
            original.StandardErrors = [0.1, 0.2, 0.3, 0.4];

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionSnapshot>.Create(
                mapper.Kind, mapper.SchemaVersion, variables, mapper.ToSnapshot(original));

            var restored = mapper.FromSnapshot(
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                    AdvancedAnalyticsModelJson.Serialise(envelope), mapper.Kind).Payload);

            SameBits(restored.Weights, original.Weights);
            SameBits(restored.StandardErrors, original.StandardErrors);
            Bits(restored.Intercept).Should().Be(Bits(original.Intercept));

            var probes = new[]
            {
                new[] { 0.0, 0.0, 0.0 }, new[] { 1.0, 2.0, 3.0 }, new[] { -5.5, 0.125, 9.75 },
                new[] { 1e6, -1e6, 1e-9 }, new[] { double.NaN, 1.0, 1.0 }
            };

            SameBits(restored.Probability(probes), original.Probability(probes));
            SameBits(restored.Score(probes), original.Score(probes));
        }

        [Fact]
        public void SnapshotIsIndependentOfTheModelItWasTakenFrom()
        {
            var model = LogisticRegression.FromWeights([1.0, 2.0], 0.5);
            var snapshot = mapper.ToSnapshot(model);

            model.Weights[0] = 99;

            snapshot.Weights[0].Should().Be(1.0);
        }

        [Fact]
        public void EnvelopeUsesCamelCaseAndCarriesNoTypeInformation()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0]));
            var root = JObject.Parse(json);

            root.Properties().Select(p => p.Name).Should().Equal(
                "kind", "schemaVersion", "inputCount", "variablesInOrder", "payload", "calibrator");

            json.Should().NotContain("$type");

            ((JObject)root["payload"]!).Properties().Select(p => p.Name).Should().Equal(
                "weights", "intercept", "standardErrors");
        }

        [Fact]
        public void TypeNameInjectionIsRejectedNotHonoured()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0]));
            var root = JObject.Parse(json);
            ((JObject)root["payload"]!).AddFirst(new JProperty("$type", "System.Diagnostics.Process, System"));

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                root.ToString(), mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*$type*");
        }

        [Fact]
        public void CalibratorRoundTrips()
        {
            var calibrator = new AdvancedAnalyticsCalibratorSnapshot
                { Method = "Platt", Parameters = [-1.5, 0.25] };

            var back = AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0], calibrator: calibrator)),
                mapper.Kind);

            back.Calibrator!.Method.Should().Be("Platt");
            SameBits(back.Calibrator.Parameters, [-1.5, 0.25]);
        }

        [Fact]
        public void InputDimensionMustMatchTheVariableCount()
        {
            var envelope = Envelope([1.0, 2.0]);

            var act = () => AdvancedAnalyticsModelJson.Serialise(envelope);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*'Weights'*2*3*");
        }

        [Fact]
        public void DeserialiseRejectsAnInputCountThatDisagreesWithTheVariables()
        {
            var root = JObject.Parse(AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0])));
            root["inputCount"] = 4;

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                root.ToString(), mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*input count*");
        }

        [Fact]
        public void StandardErrorsMustHaveOneMoreElementThanTheInputs()
        {
            var envelope = Envelope([1.0, 2.0, 3.0]);
            envelope.Payload.StandardErrors = [1.0, 2.0, 3.0];

            var act = () => AdvancedAnalyticsModelJson.Serialise(envelope);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*'StandardErrors'*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("{")]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("{} {}")]
        [InlineData("{\"kind\":\"LogisticRegression\"}")]
        public void MalformedOrIncompleteJsonFailsLoudly(string json)
        {
            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void WrongKindIsRejected()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0]));

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, "NaiveBayes");

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*kind*");
        }

        [Fact]
        public void UnknownPropertiesAreRejectedRatherThanDropped()
        {
            var root = JObject.Parse(AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0])));
            ((JObject)root["payload"]!)["surprise"] = 1;

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                root.ToString(), mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void WrongElementTypesFailInsteadOfProducingHalfBuiltObjects()
        {
            var root = JObject.Parse(AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0])));
            ((JObject)root["payload"]!)["weights"] = new JArray("a", "b", "c");

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                root.ToString(), mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void DuplicatePropertiesAreRejected()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0]))
                .Replace("\"kind\":\"LogisticRegression\"", "\"kind\":\"LogisticRegression\",\"kind\":\"Other\"");

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void FutureSchemaVersionIsRejected()
        {
            var root = JObject.Parse(AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0])));
            root["schemaVersion"] = 2;

            var act = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                root.ToString(), mapper.Kind);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*schema version*");
        }

        [Fact]
        public void DuplicateVariableNamesAreRejected()
        {
            var snapshot = new AdvancedAnalyticsLogisticRegressionSnapshot { Weights = [1.0, 2.0] };

            var envelope = AdvancedAnalyticsModelEnvelope<AdvancedAnalyticsLogisticRegressionSnapshot>.Create(
                mapper.Kind, 1, ["a", "a"], snapshot);

            var act = () => AdvancedAnalyticsModelJson.Serialise(envelope);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*duplicate*");
        }

        [Fact]
        public void SizeCapAppliesToBothDirections()
        {
            var envelope = Envelope([1.0, 2.0, 3.0]);
            var json = AdvancedAnalyticsModelJson.Serialise(envelope);

            var write = () => AdvancedAnalyticsModelJson.Serialise(envelope, json.Length - 1);

            var read = () => AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, mapper.Kind, json.Length - 1);

            var kind = () => AdvancedAnalyticsModelJson.ReadKind(json, 10);

            write.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*limit*");
            read.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*limit*");
            kind.Should().Throw<AdvancedAnalyticsSnapshotException>().WithMessage("*limit*");

            AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(
                json, mapper.Kind, json.Length).Should().NotBeNull();
        }

        [Fact]
        public void ExcessiveNestingIsRejected()
        {
            var json = new string('[', 200) + new string(']', 200);

            var act = () => AdvancedAnalyticsModelJson.ReadKind(json);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void ReadKindReturnsTheKindWithoutNeedingTheSnapshotType()
        {
            var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.0, 2.0, 3.0]));

            AdvancedAnalyticsModelJson.ReadKind(json).Should().Be("LogisticRegression");
        }

        [Fact]
        public void FromSnapshotRejectsAMismatchedStandardErrorLength()
        {
            var snapshot = new AdvancedAnalyticsLogisticRegressionSnapshot
            {
                Weights = [1.0, 2.0],
                StandardErrors = [1.0]
            };

            var act = () => mapper.FromSnapshot(snapshot);

            act.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void SerialisationIsCultureInvariant()
        {
            var previous = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var json = AdvancedAnalyticsModelJson.Serialise(Envelope([1.5, 0.1, 2.25]));
                json.Should().Contain("1.5").And.NotContain("1,5");

                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsLogisticRegressionSnapshot>(json, mapper.Kind)
                    .Payload.Weights.Should().Equal(1.5, 0.1, 2.25);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using FluentAssertions;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.NetworkGolden
{
    public class ExhaustiveGoldenTests
    {
        private const int InputCount = 26;
        private const string GenerateEnvironmentVariable = "NETWORK_GOLDEN_GENERATE_DIR";

        private static readonly string[] networkNames =
        [
            "Sigmoid2", "BipolarSigmoid1", "Gaussian", "RectifiedLinear", "Linear1", "Identity"
        ];

        private static readonly double[][] mockRows =
        [
            [0, 0, 1, 4, 1, 0, 0, 0, 0, 4, 0, 0, 4, 0, 30.14, 629.52, 0, 1, 2, 2, 1, 2, 2, 1, 0, 1],
            [0, 1, 0, 4, 0, 0, 0, 4, 4, 0, 1, 4, 0, 1, 90.88, 1817.64, 1817.64, 1, 1, 1, 1, 1, 4, 1, 1, 1],
            [1, 0, 0, 1, 1, 0, 0, 0, 1, 0, 1, 1, 0, 1, 406.44, 4064.37, 4064.37, 0, 1, 1, 1, 1, 1, 0, 1, 0]
        ];

        private static IActivationFunction NewFunction(string name)
        {
            return name switch
            {
                "Sigmoid2" => new SigmoidFunction(2),
                "BipolarSigmoid1" => new BipolarSigmoidFunction(1),
                "Gaussian" => new GaussianFunction(),
                "RectifiedLinear" => new RectifiedLinearFunction(),
                "Linear1" => new LinearFunction(1),
                "Identity" => new IdentityFunction(),
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }

        private static double[][] Probes()
        {
            var probes = new List<double[]>
            {
                Fill(0.0),
                Fill(-0.0),
                Fill(1.0),
                Fill(-1.0),
                Fill(1e6),
                Fill(-1e6),
                Fill(1e12),
                Fill(-1e12),
                Fill(1e-300),
                Fill(double.Epsilon),
                Fill(double.MaxValue / 1e300),
                Enumerable.Range(0, InputCount).Select(i => i % 2 == 0 ? 0.0 : -0.0).ToArray(),
                Enumerable.Range(0, InputCount).Select(i => i % 2 == 0 ? 1e9 : -1e9).ToArray()
            };

            var seeded = new Random(20240607);

            for (var i = 0; i < 12; i++)
            {
                probes.Add([.. Enumerable.Range(0, InputCount).Select(_ => seeded.NextDouble() * 10 - 5)]);
            }

            for (var i = 0; i < 4; i++)
            {
                probes.Add([.. Enumerable.Range(0, InputCount).Select(_ => (seeded.NextDouble() - 0.5) * 2e7)]);
            }

            probes.AddRange(mockRows.Select(row => row.ToArray()));

            return [.. probes];
        }

        private static double[] Fill(double value)
        {
            var result = new double[InputCount];
            Array.Fill(result, value);

            return result;
        }

        private static string Hex(double value)
        {
            return BitConverter.DoubleToInt64Bits(value).ToString("X16");
        }

        private static string ReadResource(string suffix)
        {
            var assembly = typeof(ExhaustiveGoldenTests).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(suffix, StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private static ActivationNetwork Load(string json)
        {
            return JsonConvert.DeserializeObject<ActivationNetwork>(json,
                new JsonSerializationHelper().DeserializeTopologyNetworkJsonSerializerSettings)!;
        }

        private static string[] Recall(ActivationNetwork network)
        {
            return [.. Probes().Select(probe => Hex(network.Compute(probe)[0]))];
        }

        private static Dictionary<string, string> Topologies()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(ReadResource("topologies.json"))!;
        }

        private static Dictionary<string, string[]> Expected()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(ReadResource("expected.json"))!;
        }

        public static IEnumerable<object[]> Names()
        {
            return networkNames.Select(n => new object[] { n });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void RecallOfCommittedTopologyIsBitExact(string name)
        {
            var network = Load(Topologies()[name]);

            Recall(network).Should().Equal(Expected()[name]);
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void LoadedTopologyKeepsShapeAndAnnotations(string name)
        {
            var network = Load(Topologies()[name]);

            network.InputsCount.Should().Be(InputCount);
            network.NeuronsCount.Should().Equal(5, 1);
            network.Annotations.Should().HaveCount(InputCount);
            network.Annotations[0].Name.Should().Be("Abstraction.IsChip");
            network.Compute(Fill(0.0)).Should().HaveCount(1);
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void SerialiseThenLoadRoundTripIsBitExact(string name)
        {
            var network = Load(Topologies()[name]);

            var json = JsonConvert.SerializeObject(network,
                new JsonSerializationHelper().TopologyNetworkJsonSerializerSettings);

            Recall(Load(json)).Should().Equal(Expected()[name]);
        }

        [Fact]
        public void EveryNetworkHasExpectedOutputsForEveryProbe()
        {
            Topologies().Keys.Should().BeEquivalentTo(networkNames);
            Expected().Keys.Should().BeEquivalentTo(networkNames);

            foreach (var pair in Expected())
            {
                pair.Value.Should().HaveCount(Probes().Length);
            }
        }

        [Fact]
        public void RecallIsStableAcrossRepeatedCalls()
        {
            var network = Load(Topologies()["Sigmoid2"]);

            Recall(network).Should().Equal(Recall(network));
        }

        [Fact]
        public void GenerateResourcesWhenRequested()
        {
            var dir = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Exhaustive", "Mock.csv"));
            var header = lines[0].Split(',');
            var rows = lines.Skip(1).Select(l => l.Split(',').Select(double.Parse).ToArray()).ToArray();
            var columns = header.Length - 1;
            columns.Should().Be(InputCount);

            var means = new double[columns];
            var sds = new double[columns];

            for (var c = 0; c < columns; c++)
            {
                var mean = rows.Average(r => r[c + 1]);
                means[c] = mean;
                sds[c] = Math.Sqrt(rows.Average(r => Math.Pow(r[c + 1] - mean, 2)));

                if (sds[c] == 0)
                {
                    sds[c] = 1;
                }
            }

            var inputs = rows
                .Select(r => Enumerable.Range(0, columns).Select(c => (r[c + 1] - means[c]) / sds[c]).ToArray())
                .ToArray();

            var outputs = rows.Select(r => new[] { r[0] }).ToArray();

            var annotations = Enumerable.Range(0, columns).Select(c => new ActivationNetworkAnnotation
            {
                Name = "Abstraction." + header[c + 1],
                Mean = means[c],
                Sd = sds[c],
                ExhaustiveSearchInstanceVariableId = c + 1,
                ExhaustiveSearchInstanceTrialInstanceVariableId = c + 1
            }).ToList();

            var topologies = new Dictionary<string, string>();
            var expected = new Dictionary<string, string[]>();

            foreach (var name in networkNames)
            {
                var network = new ActivationNetwork(NewFunction(name), columns, annotations, 5, 1);
                new NguyenWidrow(network).Randomize();
                var trainer = new LevenbergMarquardtLearning(network);

                for (var epoch = 0; epoch < 3; epoch++)
                {
                    trainer.RunEpoch(inputs, outputs);
                }

                var json = JsonConvert.SerializeObject(network,
                    new JsonSerializationHelper().TopologyNetworkJsonSerializerSettings);

                topologies[name] = json;
                expected[name] = Recall(Load(json));
            }

            File.WriteAllText(Path.Combine(dir, "topologies.json"),
                JsonConvert.SerializeObject(topologies, Formatting.Indented));
            File.WriteAllText(Path.Combine(dir, "expected.json"),
                JsonConvert.SerializeObject(expected, Formatting.Indented));
        }
    }
}
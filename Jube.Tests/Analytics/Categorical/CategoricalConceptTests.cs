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
using System.Linq;
using Accord.Math.Random;
using Accord.Neuro;
using Accord.Neuro.Learning;
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Categorical;
using Xunit;

namespace Jube.Test.Analytics.Categorical
{
    [Trait("Category", "Unit")]
    public class CategoricalConceptTests
    {
        private static readonly string[] countries = ["GB", "US", "FR", "NG", "RO"];
        private static readonly double[] genuine = [0.5, 0.25, 0.15, 0.05, 0.05];
        private static readonly double[] fraud = [0.15, 0.15, 0.1, 0.35, 0.25];

        private static int Pick(Random random, double[] weights)
        {
            var draw = random.NextDouble();
            var cumulative = 0d;

            for (var i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];

                if (draw < cumulative)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        private static double Gaussian(Random random)
        {
            return Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        }

        private static List<Row> Generate(int seed, int count)
        {
            var random = new Random(seed);
            var rows = new List<Row>();

            for (var i = 0; i < count; i++)
            {
                var label = random.NextDouble() < 0.5 ? 1 : 0;
                var numeric = new[] { Gaussian(random) + 0.3 * label, Gaussian(random), Gaussian(random) };
                var country = countries[Pick(random, label == 1 ? fraud : genuine)];
                rows.Add(new Row(numeric, country, label));
            }

            return rows;
        }

        private static double HeldOutAuc(List<Row> train, List<Row> test, bool encode, int seed)
        {
            CategoricalFieldEncoder? encoder = null;

            if (encode)
            {
                var counter = new CategoricalCounter();
                train.ForEach(r => counter.Add(r.Country));

                encoder = CategoricalFieldEncoder.Fit("Payload.Country", counter,
                    new CategoricalOptions { Enabled = true, MinLevelCount = 10 }.Sanitised());
            }

            var width = 3 + (encoder?.Indicators.Count ?? 0);

            double[] Encode(Row row)
            {
                var input = new double[width];
                Array.Copy(row.Numeric, input, 3);
                encoder?.Encode(row.Country, input.AsSpan(3));

                return input;
            }

            var inputs = train.Select(Encode).ToArray();
            var outputs = train.Select(r => new[] { (double)r.Label }).ToArray();
            Generator.Seed = seed;

            var network = new ActivationNetwork(new SigmoidFunction(1), width, [],
                4, 1);
            new NguyenWidrow(network).Randomize();
            var learning = new LevenbergMarquardtLearning(network);

            for (var epoch = 0; epoch < 12; epoch++)
            {
                learning.RunEpoch(inputs, outputs);
            }

            var scores = test.Select(r => network.Compute(Encode(r))[0]).ToArray();

            return BinaryMetrics.RocAuc(scores, [.. test.Select(r => r.Label)]);
        }

        [Fact]
        public void EncodedCategoryBeatsTheStringAsZeroBaselineOnHeldOutData()
        {
            var train = Generate(1, 1500);
            var test = Generate(2, 3000);

            var baseline = HeldOutAuc(train, test, false, 3);
            var encoded = HeldOutAuc(train, test, true, 3);

            encoded.Should().BeGreaterThan(baseline + 0.08);
        }

        [Fact]
        public void TheSameStringsNoisedAtRecallGiveTheSameScores()
        {
            var train = Generate(5, 1000);
            var counter = new CategoricalCounter();
            train.ForEach(r => counter.Add(r.Country));

            var encoder = CategoricalFieldEncoder.Fit("Payload.Country", counter,
                new CategoricalOptions { Enabled = true, MinLevelCount = 10 }.Sanitised())!;

            var restored = CategoricalFieldEncoder.FromJson(encoder.ToJson());

            foreach (var row in train.Take(200))
            {
                var noisy = "  " + row.Country!.ToLowerInvariant() + "​ ";
                var a = new double[encoder.Indicators.Count];
                var b = new double[encoder.Indicators.Count];
                encoder.Encode(row.Country, a);
                restored.Encode(noisy, b);
                b.Should().Equal(a);
            }
        }

        [Fact]
        public void ALevelThatOnlyAppearsAtRecallDoesNotMoveOtherRowsAndStaysFinite()
        {
            var train = Generate(7, 1200);
            var counter = new CategoricalCounter();
            train.ForEach(r => counter.Add(r.Country));
            var options = new CategoricalOptions { Enabled = true, MinLevelCount = 10 }.Sanitised();
            var encoder = CategoricalFieldEncoder.Fit("Payload.Country", counter, options)!;

            var before = new double[encoder.Indicators.Count];
            var unseen = new double[encoder.Indicators.Count];
            var after = new double[encoder.Indicators.Count];
            encoder.Encode("NG", before);
            encoder.Encode("ATLANTIS", unseen);
            encoder.Encode("NG", after);

            encoder.HasOther.Should().BeFalse();
            unseen.Should().OnlyContain(v => v == 0);
            after.Should().Equal(before);
        }

        private sealed record Row(double[] Numeric, string? Country, int Label);
    }
}
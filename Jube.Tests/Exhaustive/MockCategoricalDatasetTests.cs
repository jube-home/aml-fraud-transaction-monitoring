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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Accord.MachineLearning;
using Accord.Math.Random;
using FluentAssertions;
using Jube.Analytics;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class MockCategoricalDatasetTests
    {
        private const string GenerateEnvironmentVariable = "MOCK_CATEGORICAL_GENERATE";
        internal const string FileName = "MockCategorical.csv";

        internal static readonly (string Column, string[] Features, string[] Labels)[] specs =
        [
            ("SpendingProfile",
                ["TransactionAmt", "SumTransactions1Day", "SumATMTransactions1Day"],
                ["Low", "Medium", "High", "VeryHigh"]),
            ("ChannelMix",
                ["CountInPerson1Day", "CountInternet1Day", "CountATM1Day"],
                ["InPersonHeavy", "OnlineHeavy", "AtmHeavy"]),
            ("RiskSignal",
                [
                    "CountTransactionsPINDecline1Day", "CountTransactionsDeclined1Day",
                    "CountUnsafeTerminals1Day", "DifferentDeclineReasons1Day"
                ],
                ["Clean", "Elevated", "HighRisk"]),
            ("GeographicSpread",
                ["DifferentCountryTransactions1Week", "DifferentCities1Week", "HasBeenAbroad"],
                ["Local", "Regional", "National", "International"]),
            ("MerchantVariety",
                ["DifferentMerchantTypes1Week", "CountSameMerchantUsedBefore1Week"],
                ["Repeat", "Mixed", "Varied"])
        ];

        [Fact]
        public void GenerateResourcesWhenRequested()
        {
            var dir = Environment.GetEnvironmentVariable(GenerateEnvironmentVariable);

            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            var (header, rows) = ReadCsv(Path.Combine(AppContext.BaseDirectory, "Exhaustive", "Mock.csv"));
            var columnIndex = header.Select((name, index) => (name, index)).ToDictionary(t => t.name, t => t.index);
            var dependentIndex = columnIndex["Dependent"];

            var sb = new StringBuilder();
            sb.AppendLine("Dependent," + string.Join(",", specs.Select(s => s.Column)));

            var assignments = new int[specs.Length][];

            for (var s = 0; s < specs.Length; s++)
            {
                assignments[s] = ClusterLabels(rows, columnIndex, specs[s].Features, specs[s].Labels.Length,
                    1000 + s);
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var fields = new string[specs.Length + 1];
                fields[0] = rows[i][dependentIndex].ToString(CultureInfo.InvariantCulture);

                for (var s = 0; s < specs.Length; s++)
                {
                    fields[s + 1] = specs[s].Labels[assignments[s][i]];
                }

                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(Path.Combine(dir, FileName), sb.ToString());
        }

        [Fact]
        public void TheCommittedFileIsRowAlignedWithMockCsvAndEveryCategoryIsAKnownLabel()
        {
            var (mockHeader, mockRows) = ReadCsv(Path.Combine(AppContext.BaseDirectory, "Exhaustive", "Mock.csv"));

            var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Exhaustive", FileName))
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            var header = lines[0].Split(',');

            header.Should().Equal(["Dependent", .. specs.Select(s => s.Column)]);

            var body = lines.Skip(1).Select(l => l.Split(',')).ToArray();
            body.Should().HaveCount(mockRows.Length, "every Mock.csv row must have exactly one categorical row");

            var dependentIndex = mockHeader.ToList().IndexOf("Dependent");

            for (var i = 0; i < body.Length; i++)
            {
                body[i][0].Should().Be(mockRows[i][dependentIndex].ToString(CultureInfo.InvariantCulture),
                    $"row {i}'s Dependent must match Mock.csv's own row {i}");

                for (var s = 0; s < specs.Length; s++)
                {
                    specs[s].Labels.Should().Contain(body[i][s + 1], $"row {i} column {specs[s].Column}");
                }
            }
        }

        [Fact]
        public void EveryCategoricalColumnUsesEveryOneOfItsLabelsAtLeastOnce()
        {
            var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Exhaustive", FileName))
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            var body = lines.Skip(1).Select(l => l.Split(',')).ToArray();

            for (var s = 0; s < specs.Length; s++)
            {
                var seen = body.Select(r => r[s + 1]).Distinct().ToArray();

                seen.Should().BeEquivalentTo(specs[s].Labels,
                    $"clustering {specs[s].Column} should populate every label, not collapse to fewer categories");
            }
        }

        private static (string[] Header, double[][] Rows) ReadCsv(string path)
        {
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var header = lines[0].Split(',');

            var rows = lines.Skip(1)
                .Select(l => l.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray())
                .ToArray();

            return (header, rows);
        }

        private static int[] ClusterLabels(double[][] rows, Dictionary<string, int> columnIndex,
            string[] features, int k, int seed)
        {
            var raw = rows.Select(r => features.Select(f => r[columnIndex[f]]).ToArray()).ToArray();

            var scalers = Enumerable.Range(0, features.Length)
                .Select(c => Standardisation.Fit([.. raw.Select(r => r[c])])).ToArray();

            var standardised = raw
                .Select(r => r
                    .Select((v, c) => Standardisation.ZScore(v, scalers[c].Mean, scalers[c].StandardDeviation))
                    .ToArray())
                .ToArray();

            var previousSeed = Generator.ThreadSeed;
            Generator.ThreadSeed = seed;

            try
            {
                var model = new KMeans(k) { MaxIterations = 100 };
                var learned = model.Learn(standardised);
                var assignments = learned.Decide(standardised);

                var order = Enumerable.Range(0, k)
                    .OrderBy(c => model.Clusters.Centroids[c].Average())
                    .Select((c, rank) => (c, rank))
                    .ToDictionary(t => t.c, t => t.rank);

                return [.. assignments.Select(a => order[a])];
            }
            finally
            {
                Generator.ThreadSeed = previousSeed;
            }
        }
    }
}
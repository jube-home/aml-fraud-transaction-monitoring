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

using System.Collections.Concurrent;
using System.Linq;
using FluentAssertions;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionMatchBenchmarkTests(ITestOutputHelper output)
    {
        private static readonly (string Input, string Listed, bool Same)[] pairs =
        [
            ("Mohammed Ali", "Muhammad Ali", true), ("Mohamed Salah", "Muhammad Salah", true),
            ("Alexander Petrov", "Aleksandr Petrov", true), ("Jon Smith", "John Smith", true),
            ("Christofer Jonson", "Christopher Johnson", true), ("Smith John", "John Smith", true),
            ("Maria Garcia Lopez", "Garcia Lopez Maria", true), ("Yusuf Al Rashid", "Yusuf Alrashid", true),
            ("Katarzyna Nowak", "Katarzyna Novak", true), ("Sergey Ivanov", "Sergei Ivanov", true),
            ("Jean Pierre Dubois", "Jean-Pierre Dubois", true), ("Ahmad Khan", "Ahmed Khan", true),
            ("Maria Garcia", "Mario Gomez", false), ("John Smith", "Jane Smyth", false),
            ("Ali Hassan", "Ali Hussein", false), ("Peter Brown", "Paul Brown", false),
            ("Anna Kowalska", "Hanna Kowalczyk", false), ("David Cohen", "Daniel Cohn", false),
            ("Omar Farouk", "Omar Faruq Khan Mahmoud", false), ("Li Wei", "Wei Li Chen Zhang", false),
            ("Sofia Rossi", "Sophie Russo", false), ("Ivan Petrov", "Igor Petrenko", false),
            ("Carlos Mendez", "Carla Mendes", false), ("Robert Johnson", "Roberta Jonas", false)
        ];

        private static readonly ConcurrentDictionary<string, byte> noStopTokens = new();

        private (int Tp, int Fp, int Fn) Score(SanctionMatchOptions options, int distance)
        {
            int tp = 0, fp = 0, fn = 0;

            foreach (var (input, listed, same) in pairs)
            {
                var entries = new ConcurrentDictionary<int, SanctionEntry>();
                entries[1] = new SanctionEntry { SanctionEntryId = 1, SanctionElementValue = [listed] };

                var matched = new LevenshteinDistance(null, null, options)
                    .CheckMultipartString(input, distance, entries, noStopTokens).Any();

                if (matched && same)
                {
                    tp++;
                }
                else if (matched)
                {
                    fp++;
                }
                else if (same)
                {
                    fn++;
                }
            }

            return (tp, fp, fn);
        }

        private (double Precision, double Recall) Report(string name, SanctionMatchOptions options, int distance)
        {
            var (tp, fp, fn) = Score(options, distance);
            var precision = tp + fp == 0 ? 1d : (double)tp / (tp + fp);
            var recall = tp + fn == 0 ? 1d : (double)tp / (tp + fn);
            output.WriteLine($"{name}: true positives {tp}, false positives {fp}, missed {fn}, precision {precision:0.00}, recall {recall:0.00}");

            return (precision, recall);
        }

        [Fact]
        public void EachAlgorithmIsMeasuredAgainstTheSameLabelledPairs()
        {
            var levenshtein = Report("Levenshtein distance 3", new SanctionMatchOptions(), 3);
            var jaroWinkler = Report("Jaro-Winkler 0.9 distance 3",
                new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.9), 3);
            var strict = Report("Jaro-Winkler 0.95 distance 3",
                new SanctionMatchOptions(SanctionMatchOptions.JaroWinkler, 0.95), 3);
            var tokenSet = Report("Token set 0.85 distance 3",
                new SanctionMatchOptions(SanctionMatchOptions.TokenSet, 0.85), 3);

            strict.Recall.Should().BeLessThanOrEqualTo(jaroWinkler.Recall);
            (levenshtein.Recall + jaroWinkler.Recall + tokenSet.Recall).Should().BeGreaterThan(0);
        }
    }
}

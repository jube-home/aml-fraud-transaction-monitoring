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
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;

namespace Jube.Engine.AdvancedAnalytics.Sequences
{
    public sealed class AdvancedAnalyticsSymbolSequenceSet
    {
        private readonly int[] offsets;
        private readonly int[] sequenceLengths;
        private readonly int[] symbols;

        [JsonConstructor]
        public AdvancedAnalyticsSymbolSequenceSet(int[] symbols, int[] sequenceLengths)
        {
            if (symbols == null)
            {
                throw new AdvancedAnalyticsSnapshotException("A symbol sequence set needs symbols.");
            }

            for (var i = 0; i < symbols.Length; i++)
            {
                if (symbols[i] < 0)
                {
                    throw new AdvancedAnalyticsSnapshotException($"Symbol at position {i} is negative.");
                }
            }

            this.sequenceLengths =
                AdvancedAnalyticsSequenceLayout.CopyLengths(sequenceLengths, symbols.Length, "symbols");

            this.symbols = (int[])symbols.Clone();
            offsets = AdvancedAnalyticsSequenceLayout.Offsets(this.sequenceLengths);
        }

        [JsonProperty("symbols")] public IReadOnlyList<int> Symbols => Array.AsReadOnly(symbols);

        [JsonProperty("sequenceLengths")]
        public IReadOnlyList<int> SequenceLengths => Array.AsReadOnly(sequenceLengths);

        [JsonIgnore] public int SequenceCount => sequenceLengths.Length;
        [JsonIgnore] public int SymbolCount => symbols.Length;
        [JsonIgnore] public int MaximumSymbol => symbols.Max();

        public int Offset(int sequence)
        {
            RequireIndex(sequence);

            return offsets[sequence];
        }

        public int[] GetSequence(int sequence)
        {
            RequireIndex(sequence);

            var result = new int[sequenceLengths[sequence]];
            Array.Copy(symbols, offsets[sequence], result, 0, result.Length);

            return result;
        }

        public int[][] ToJagged()
        {
            var sequences = new int[sequenceLengths.Length][];

            for (var i = 0; i < sequences.Length; i++)
            {
                sequences[i] = GetSequence(i);
            }

            return sequences;
        }

        public static AdvancedAnalyticsSymbolSequenceSet FromJagged(int[][] sequences)
        {
            if (sequences == null || sequences.Length == 0)
            {
                throw new AdvancedAnalyticsSnapshotException("A symbol sequence set needs at least one sequence.");
            }

            var lengths = new int[sequences.Length];
            var flat = new List<int>();

            for (var i = 0; i < sequences.Length; i++)
            {
                var sequence = sequences[i] ??
                               throw new AdvancedAnalyticsSnapshotException($"Sequence {i} is missing.");

                lengths[i] = sequence.Length;
                flat.AddRange(sequence);
            }

            return new AdvancedAnalyticsSymbolSequenceSet([.. flat], lengths);
        }

        private void RequireIndex(int sequence)
        {
            if (sequence < 0 || sequence >= sequenceLengths.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }
        }
    }
}
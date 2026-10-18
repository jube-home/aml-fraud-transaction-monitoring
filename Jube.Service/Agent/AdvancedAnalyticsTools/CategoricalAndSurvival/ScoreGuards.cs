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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class ScoreGuards
    {
        public static void Pairs(double[] scores, int[] labels, out double[] cleanScores, out int[] cleanLabels,
            out int skipped)
        {
            CategoricalGuards.Required(scores, nameof(scores));
            CategoricalGuards.BinaryLabels(labels, nameof(labels));
            CategoricalGuards.SameLength(scores.Length, labels.Length, nameof(scores), nameof(labels));
            var keep = new List<int>();

            for (var i = 0; i < scores.Length; i++)
            {
                if (!double.IsNaN(scores[i]))
                {
                    keep.Add(i);
                }
            }

            if (keep.Count == 0)
            {
                throw new ArgumentException("There are no usable rows: the input is empty or every score is NaN.",
                    nameof(scores));
            }

            skipped = scores.Length - keep.Count;
            cleanScores = keep.Select(i => scores[i]).ToArray();
            cleanLabels = keep.Select(i => labels[i]).ToArray();
        }

        public static void BothClasses(int[] labels)
        {
            var positives = labels.Count(l => l == 1);

            if (positives == 0 || positives == labels.Length)
            {
                throw new ArgumentException(
                    "The outcome contains only one class among the usable rows; this measure needs at least one event and one non-event.",
                    "labels");
            }
        }

        public static void Probabilities(double[] scores)
        {
            for (var i = 0; i < scores.Length; i++)
            {
                if (scores[i] < 0d || scores[i] > 1d)
                {
                    throw new ArgumentException(
                        $"probabilities must lie between 0 and 1; entry {i} is {scores[i]}. Use a score-based tool for uncalibrated scores.",
                        nameof(scores));
                }
            }
        }
    }
}
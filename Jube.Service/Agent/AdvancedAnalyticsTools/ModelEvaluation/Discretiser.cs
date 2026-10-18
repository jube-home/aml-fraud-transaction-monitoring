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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class Discretiser
    {
        private readonly double[][] cuts;
        private readonly string[] names;

        private Discretiser(double[][] cuts, string[] names)
        {
            this.cuts = cuts;
            this.names = names;
        }

        public int[] Symbols => cuts.Select(c => c.Length + 1).ToArray();

        public static Discretiser Fit(double[][] train, string[] names, int bins)
        {
            var cuts = new double[names.Length][];

            for (var j = 0; j < names.Length; j++)
            {
                var values = train.Select(r => r[j]).Where(double.IsFinite).OrderBy(v => v).ToArray();
                var distinct = values.Distinct().ToArray();

                if (distinct.Length <= bins)
                {
                    cuts[j] = distinct.Zip(distinct.Skip(1), (a, b) => a + (b - a) / 2d).ToArray();
                    continue;
                }

                var edges = new List<double>();

                for (var b = 1; b < bins; b++)
                {
                    var position = (int)Math.Floor(b * (double)values.Length / bins);
                    position = Math.Min(Math.Max(position, 1), values.Length - 1);
                    var edge = values[position - 1] + (values[position] - values[position - 1]) / 2d;

                    if (edges.Count == 0 || edge > edges[^1])
                    {
                        edges.Add(edge);
                    }
                }

                cuts[j] = edges.ToArray();
            }

            return new Discretiser(cuts, names);
        }

        public int Code(int column, double value)
        {
            var code = 0;

            foreach (var cut in cuts[column])
            {
                if (value > cut)
                {
                    code++;
                }
            }

            return code;
        }

        public double[] Apply(double[] row)
        {
            var result = new double[row.Length];

            for (var j = 0; j < row.Length; j++)
            {
                result[j] = Code(j, row[j]);
            }

            return result;
        }

        public int[] ApplyInt(double[] row)
        {
            var result = new int[row.Length];

            for (var j = 0; j < row.Length; j++)
            {
                result[j] = Code(j, row[j]);
            }

            return result;
        }

        public string Describe(int column, int code)
        {
            var edges = cuts[column];
            var lower = code == 0 ? "-inf" : ModelEvaluationGuard.Format(edges[code - 1]);
            var upper = code >= edges.Length ? "+inf" : ModelEvaluationGuard.Format(edges[code]);

            return $"{names[column]} in ({lower}, {upper}]";
        }
    }
}
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

using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class KNearestNeighboursPipelineKindAdapter : ISnapshotKindAdapter
    {
        private readonly bool regressor;

        private KNearestNeighboursPipelineKindAdapter(string kind, bool regressor)
        {
            Kind = kind;
            this.regressor = regressor;
        }

        public static IReadOnlyList<ISnapshotKindAdapter> All { get; } =
        [
            new KNearestNeighboursPipelineKindAdapter("KNearestNeighboursClassifierPipeline", false),
            new KNearestNeighboursPipelineKindAdapter("KNearestNeighboursRegressorPipeline", true)
        ];

        public string Kind { get; }

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var rows = ModelJson.Matrix(payload, "rows") ?? throw ModelJson.Inconsistent("the payload has no rows");

            if (rows.Length == 0)
            {
                throw ModelJson.Inconsistent("the payload has no rows");
            }

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), rows[0].Length);
            var k = ModelJson.Integer(payload, "neighbourCount");

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var space = standardise ? "standardised" : "original";
            var vote = k is { } n ? "the " + n + " nearest" : "the nearest";

            if (!regressor)
            {
                var labels = ModelJson.Integers(payload, "labels") ??
                             throw ModelJson.Inconsistent("the payload has no labels");

                if (labels.Length != rows.Length)
                {
                    throw ModelJson.Inconsistent("the payload has " + labels.Length + " labels for " + rows.Length +
                                                 " rows");
                }

                var groups = labels.Distinct().Order().Select(c => new ProjectedGroup("Class " + c,
                    Enumerable.Range(0, rows.Length).Where(i => labels[i] == c).ToArray())).ToArray();

                return SupportSetProjection.Build(rows, names, groups, i => "Reference row " + (i + 1) + ", class " +
                                                                            labels[i], options,
                    "K-nearest neighbours classifier",
                    rows.Length + " stored reference rows" + (k is { } kc ? ", k = " + kc : ""),
                    "A k-nearest neighbours classifier has no fitted structure: the model is its stored reference " +
                    "rows, on the " + space + " scale. A new row takes the class mix of " + vote +
                    " of them.");
            }

            var targets = ModelJson.Doubles(payload, "targets") ??
                          throw ModelJson.Inconsistent("the payload has no targets");

            if (targets.Length != rows.Length || targets.Any(t => !double.IsFinite(t)))
            {
                throw ModelJson.Inconsistent("the payload needs one finite target per row");
            }

            var ordered = targets.Order().ToArray();

            var cuts = new[] { 0.25, 0.5, 0.75 }.Select(q => ordered[(int)Math.Floor(q * (ordered.Length - 1))])
                .ToArray();

            var bands = new List<ProjectedGroup>();
            var used = new bool[rows.Length];

            for (var b = 0; b < 4; b++)
            {
                var high = b < 3 ? cuts[b] : double.PositiveInfinity;
                var members = Enumerable.Range(0, rows.Length).Where(i => !used[i] && targets[i] <= high).ToArray();

                foreach (var i in members)
                {
                    used[i] = true;
                }

                if (members.Length == 0)
                {
                    continue;
                }

                var low = members.Min(i => targets[i]);
                var top = members.Max(i => targets[i]);

                bands.Add(new ProjectedGroup("Target " + CoefficientMapping.Short(low) + " to " +
                                             CoefficientMapping.Short(top), members));
            }

            return SupportSetProjection.Build(rows, names, bands,
                i => "Reference row " + (i + 1) + ", target " + ModelJson.Format(targets[i]), options,
                "K-nearest neighbours regressor",
                rows.Length + " stored reference rows in target quarters" + (k is { } kr ? ", k = " + kr : ""),
                "A k-nearest neighbours regressor has no fitted structure: the model is its stored reference rows, " +
                "on the " + space + " scale, coloured by quarter of their target. A new row is predicted as the " +
                "average target of " + vote + " of them.");
        }
    }
}
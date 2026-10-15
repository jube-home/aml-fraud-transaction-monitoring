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
using Jube.Service.ModelDiagrams.Exceptions;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class SnapshotEnvelopeAdapter : IModelDiagramAdapter
    {
        private readonly IReadOnlyDictionary<string, ISnapshotKindAdapter> kinds;

        public SnapshotEnvelopeAdapter(IEnumerable<ISnapshotKindAdapter> kinds)
        {
            this.kinds = kinds.ToDictionary(k => k.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<string> Kinds => kinds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        public string Name => "SnapshotEnvelope";

        public double? Detect(JToken model)
        {
            if (model is not JObject)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(ModelJson.String(model, "kind")) ||
                ModelJson.Integer(model, "schemaVersion") is null ||
                ModelJson.Get(model, "payload") is not JObject)
            {
                return null;
            }

            return 0.99;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var envelope = (JObject)model;
            var kind = ModelJson.String(envelope, "kind")!;

            if (!kinds.TryGetValue(kind, out var adapter))
            {
                throw new ModelDiagramNotSupportedException(
                    "The model snapshot is of kind '" + Clean(kind) +
                    "', which has no diagram yet. The kinds with diagrams are " +
                    string.Join(", ", Kinds) + ". " + ModelDiagramNotSupportedException.SummaryAdvice);
            }

            return adapter.Build(envelope, (JObject)ModelJson.Get(envelope, "payload")!, options);
        }

        private static string Clean(string kind)
        {
            var trimmed = kind.Length > 60 ? kind[..60] : kind;

            return new string(trimmed.Select(c => char.IsControl(c) ? '?' : c).ToArray());
        }
    }
}
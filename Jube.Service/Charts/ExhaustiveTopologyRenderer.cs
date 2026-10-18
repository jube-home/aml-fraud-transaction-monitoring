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

using System.Globalization;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Charting.Performance.Models;
using Jube.Service.ModelDiagrams;
using Jube.Service.ModelDiagrams.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Charts
{
    public sealed record TopologyInputSensitivity(string? Name, double Sensitivity);

    public static class ExhaustiveTopologyRenderer
    {
        public const string Title = "Model Topology";
        public const string NotTrainedMessage = "No data available. This instance has not been trained yet.";

        public const string UnreadableMessage =
            "The stored topology could not be read, so it is not drawn.";

        public const string TooLargeMessage =
            "This topology is too large to draw as a diagram. The layer sizes are summarised instead.";

        public const string SensitivityTitle = "Input sensitivity";
        public const string RelevanceTitle = "Relevance to this score";
        public const int DiagramWidth = 1200;
        public const int DiagramHeight = 700;
        public const int MaximumJsonCharacters = 64 * 1024 * 1024;

        public static string Empty(ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            return Chart.Line()
                .Title(Title)
                .Subtitle(NotTrainedMessage)
                .Size(DiagramWidth, 320)
                .Legend(LegendPosition.None)
                .Description(NotTrainedMessage)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Topology(string? json, IReadOnlyList<string?>? variableNames = null,
            IReadOnlyList<TopologyInputSensitivity>? sensitivities = null, int? topologyComplexity = null,
            double? score = null, ChartStyleMode styleMode = ChartStyleMode.External, string? id = null,
            IReadOnlyList<IReadOnlyList<double>>? liveRelevance = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Empty(styleMode, id);
            }

            try
            {
                return Draw(json, variableNames, sensitivities, liveRelevance, topologyComplexity, score, styleMode,
                    id);
            }
            catch (ModelDiagramNotSupportedException)
            {
                return Summary(json, TooLargeMessage, topologyComplexity, styleMode, id);
            }
            catch (ModelDiagramException)
            {
                return Message(UnreadableMessage, styleMode, id);
            }
            catch (ChartingException)
            {
                return Summary(json, TooLargeMessage, topologyComplexity, styleMode, id);
            }
        }

        private static string Draw(string json, IReadOnlyList<string?>? variableNames,
            IReadOnlyList<TopologyInputSensitivity>? sensitivities,
            IReadOnlyList<IReadOnlyList<double>>? liveRelevance, int? topologyComplexity, double? score,
            ChartStyleMode styleMode, string? id)
        {
            var names = variableNames is { Count: > 0 } && variableNames.All(n => !string.IsNullOrWhiteSpace(n))
                ? variableNames.Select(n => n!).ToArray()
                : null;

            var chart = Build(json, names);

            if (chart is not NetworkDiagram diagram)
            {
                return ((IStyleModeChart)chart).WithStyleMode(styleMode).Render();
            }

            var layers = diagram.Spec.Layers;
            string? emphasisTitle = null;

            if (liveRelevance is { Count: > 0 } && liveRelevance.Count == layers.Count)
            {
                layers = layers.Select((l, i) => EmphasiseByPosition(l, liveRelevance[i])).ToArray();
                emphasisTitle = RelevanceTitle;
            }
            else if (layers.Count > 0 && names is not null && names.Length == layers[0].Nodes.Count &&
                     sensitivities is { Count: > 0 })
            {
                layers = layers.Select((l, i) => i == 0 ? l with { Nodes = Emphasise(l.Nodes, sensitivities) } : l)
                    .ToArray();

                emphasisTitle = SensitivityTitle;
            }

            var emphasised = layers.Any(l => l.Nodes.Any(n => n.Emphasis is not null));

            var spec = diagram.Spec with
            {
                Layers = layers,
                EmphasisTitle = emphasised ? emphasisTitle : null,
                Options = diagram.Spec.Options with
                {
                    Title = Title,
                    StyleMode = styleMode,
                    Id = id
                }
            };

            var result = new NetworkDiagram(spec);

            spec = spec with
            {
                Options = spec.Options with { Subtitle = Caption(layers, topologyComplexity, score, result.Detail) }
            };

            return new NetworkDiagram(spec).Render();
        }

        private static NetworkLayer EmphasiseByPosition(NetworkLayer layer, IReadOnlyList<double> relevance)
        {
            if (relevance.Count != layer.Nodes.Count)
            {
                return layer;
            }

            var maximum = relevance.Count == 0 ? 0 : relevance.Max(Math.Abs);

            if (!(maximum > 0))
            {
                return layer;
            }

            return layer with
            {
                Nodes = layer.Nodes.Select((n, i) => n with
                {
                    Emphasis = Math.Abs(relevance[i]) / maximum,
                    EmphasisText = relevance[i].ToString("+0.####;-0.####;0", CultureInfo.InvariantCulture)
                }).ToArray()
            };
        }

        private static ISvgChart Build(string json, string[]? names)
        {
            var options = new ModelDiagramOptions
            {
                Title = Title,
                Width = DiagramWidth,
                Height = DiagramHeight,
                MaximumJsonCharacters = MaximumJsonCharacters,
                VariableNames = names
            };

            try
            {
                return ModelDiagramRenderer.Build(json, options);
            }
            catch (ArgumentException) when (names is not null)
            {
                return ModelDiagramRenderer.Build(json, options with { VariableNames = null });
            }
        }

        private static NetworkNode[] Emphasise(IReadOnlyList<NetworkNode> nodes,
            IReadOnlyList<TopologyInputSensitivity> sensitivities)
        {
            var byName = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var s in sensitivities)
            {
                if (s.Name is null || !double.IsFinite(s.Sensitivity) || s.Sensitivity < 0)
                {
                    continue;
                }

                byName[s.Name] = byName.TryGetValue(s.Name, out var known)
                    ? Math.Max(known, s.Sensitivity)
                    : s.Sensitivity;
            }

            var maximum = byName.Count == 0 ? 0 : byName.Values.Max();

            if (!(maximum > 0))
            {
                return nodes.ToArray();
            }

            return nodes.Select(n => byName.TryGetValue(n.Label, out var value)
                ? n with
                {
                    Emphasis = value / maximum,
                    EmphasisText = value.ToString("0.####", CultureInfo.InvariantCulture)
                }
                : n).ToArray();
        }

        private static string Caption(IReadOnlyList<NetworkLayer> layers, int? topologyComplexity, double? score,
            NetworkDetail detail)
        {
            var parts = new List<string>();

            if (layers.Count > 0)
            {
                var hidden = layers.Skip(1).SkipLast(1).Select(l => l.Nodes.Count).ToArray();
                var output = layers[^1].Nodes.Count;

                parts.Add(layers[0].Nodes.Count + " inputs, " +
                          (hidden.Length == 0
                              ? "no hidden layer"
                              : string.Join(" then ", hidden) + " hidden " +
                                (hidden.Sum() == 1 ? "neuron" : "neurons")) +
                          ", " + output + (output == 1 ? " output" : " outputs"));

                long weights = 0;

                for (var i = 1; i < layers.Count; i++)
                {
                    weights += (long)layers[i].Nodes.Count * layers[i - 1].Nodes.Count;
                }

                parts.Add(layers.Skip(1).Sum(l => (long)l.Nodes.Count).ToString("N0", CultureInfo.InvariantCulture) +
                          " PEs and " + weights.ToString("N0", CultureInfo.InvariantCulture) + " weights in " +
                          (layers.Count - 1).ToString(CultureInfo.InvariantCulture) + " layers");

                var activation = layers.Count > 1 ? layers[1].Activation : null;
                var outputActivation = layers[^1].Activation;

                if (!string.IsNullOrWhiteSpace(activation))
                {
                    parts.Add("activation " + activation +
                              (layers.Count > 2 && !string.IsNullOrWhiteSpace(outputActivation) &&
                               outputActivation != activation
                                  ? " (output " + outputActivation + ")"
                                  : string.Empty));
                }
            }

            if (topologyComplexity is { } complexity)
            {
                parts.Add("topology complexity " + complexity.ToString(CultureInfo.InvariantCulture));
            }

            if (score is { } s && double.IsFinite(s))
            {
                parts.Add("score " + s.ToString("0.000", CultureInfo.InvariantCulture));
            }

            parts.Add("view: " + NetworkDetails.Describe(detail));

            return string.Join("; ", parts);
        }

        private static string Summary(string json, string message, int? topologyComplexity,
            ChartStyleMode styleMode, string? id)
        {
            var widths = LayerWidths(json);

            if (widths is null)
            {
                return Message(message, styleMode, id);
            }

            var layers = new List<MetricTile> { new("Inputs", widths.Value.Inputs, null, null, "0") };

            for (var i = 0; i < widths.Value.Layers.Count - 1; i++)
            {
                layers.Add(new MetricTile("Hidden layer " + (i + 1), widths.Value.Layers[i], null, null, "0"));
            }

            layers.Add(new MetricTile("Output", widths.Value.Layers[^1], null, null, "0"));

            var totals = new List<MetricTile>
            {
                new("Layers", widths.Value.Layers.Count, "weighted layers", null, "0"),
                new("Neurons", widths.Value.Layers.Sum(), "excluding inputs", null, "0")
            };

            if (topologyComplexity is { } complexity)
            {
                totals.Add(new MetricTile("Topology complexity", complexity, null, null, "0"));
            }

            return Chart.MetricsPanel()
                .Title(Title)
                .Subtitle(message)
                .Group("Layer sizes", layers)
                .Group("Totals", totals)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        private static (int Inputs, IReadOnlyList<int> Layers)? LayerWidths(string json)
        {
            try
            {
                if (json.Length > ModelDiagramOptions.DefaultMaximumJsonCharacters)
                {
                    return null;
                }

                var model = JToken.Parse(json);

                if (model is not JObject root || root.GetValue("layers", StringComparison.OrdinalIgnoreCase) is not
                        JArray layers || layers.Count == 0 || layers.Count > 1000)
                {
                    return null;
                }

                var widths = new List<int>();
                var inputs = -1;

                foreach (var layer in layers)
                {
                    if (layer is not JObject l ||
                        l.GetValue("neurons", StringComparison.OrdinalIgnoreCase) is not JArray neurons)
                    {
                        return null;
                    }

                    widths.Add(neurons.Count);

                    if (inputs < 0 && neurons.Count > 0 && neurons[0] is JObject n &&
                        n.GetValue("weights", StringComparison.OrdinalIgnoreCase) is JArray weights)
                    {
                        inputs = weights.Count;
                    }
                }

                return inputs < 0 ? null : (inputs, widths);
            }
            catch (Exception e) when (e is JsonException or InvalidCastException)
            {
                return null;
            }
        }

        private static string Message(string message, ChartStyleMode styleMode, string? id)
        {
            return Chart.Line()
                .Title(Title)
                .Subtitle(message)
                .Size(DiagramWidth, 320)
                .Legend(LegendPosition.None)
                .Description(message)
                .StyleMode(styleMode).Id(id)
                .Render();
        }
    }
}
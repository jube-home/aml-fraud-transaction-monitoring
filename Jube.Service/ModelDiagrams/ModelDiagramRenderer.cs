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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Exceptions;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams
{
    public static class ModelDiagramRenderer
    {
        public static string Render(string json, ModelDiagramOptions? options = null)
        {
            return Render(json, options, ModelDiagramAdapters.Default);
        }

        public static string Render(string json, ModelDiagramOptions? options,
            IReadOnlyList<IModelDiagramAdapter> adapters)
        {
            var (adapter, chart) = Prepare(json, options, adapters);

            return Guard(adapter, chart.Render);
        }

        public static ISvgChart Build(string json, ModelDiagramOptions? options = null)
        {
            return Build(json, options, ModelDiagramAdapters.Default);
        }

        public static ISvgChart Build(string json, ModelDiagramOptions? options,
            IReadOnlyList<IModelDiagramAdapter> adapters)
        {
            return Prepare(json, options, adapters).Chart;
        }

        private static (IModelDiagramAdapter Adapter, ISvgChart Chart) Prepare(string json,
            ModelDiagramOptions? options,
            IReadOnlyList<IModelDiagramAdapter> adapters)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentNullException.ThrowIfNull(adapters);
            options ??= new ModelDiagramOptions();
            options.Validate();

            var model = ModelJson.Parse(json, options);
            var adapter = Select(model, adapters);

            return (adapter, Guard(adapter, () => adapter.Build(model, options)));
        }

        private static T Guard<T>(IModelDiagramAdapter adapter, Func<T> work)
        {
            try
            {
                return work();
            }
            catch (ChartingException e)
            {
                throw ModelDiagramNotSupportedException.Wrap(e);
            }
            catch (Exception e) when (e is InvalidCastException or IndexOutOfRangeException
                                          or ArgumentOutOfRangeException
                                          or NullReferenceException or InvalidOperationException or KeyNotFoundException
                                          or OverflowException or FormatException)
            {
                throw new ModelDiagramNotRecognisedException(
                    "The model JSON was claimed by the " + adapter.Name + " adapter but is malformed: " + e.Message, e);
            }
        }

        public static IModelDiagramAdapter Select(JToken model, IReadOnlyList<IModelDiagramAdapter> adapters)
        {
            var scored = new List<(IModelDiagramAdapter Adapter, double Confidence)>();

            foreach (var adapter in adapters)
            {
                if (adapter.Detect(model) is { } confidence && confidence > 0)
                {
                    scored.Add((adapter, confidence));
                }
            }

            if (scored.Count == 0)
            {
                throw new ModelDiagramNotRecognisedException(
                    "The JSON is not a serialised model that any diagram adapter recognises.");
            }

            var best = scored.Max(s => s.Confidence);
            var winners = scored.Where(s => s.Confidence >= best).ToArray();

            if (winners.Length > 1)
            {
                throw new ModelDiagramAmbiguousException(winners.Select(w => w.Adapter.Name).ToArray());
            }

            return winners[0].Adapter;
        }

        public static ModelDiagramInfo Describe(string json, ModelDiagramOptions? options = null)
        {
            var (adapter, chart) = Prepare(json, options, ModelDiagramAdapters.Default);

            return new ModelDiagramInfo(adapter.Name, chart is NetworkDiagram network ? network.Detail : null);
        }

        public static string DetectedAdapter(string json, ModelDiagramOptions? options = null)
        {
            options ??= new ModelDiagramOptions();
            var model = ModelJson.Parse(json, options);

            return Select(model, ModelDiagramAdapters.Default).Name;
        }
    }

    public sealed record ModelDiagramInfo(string Adapter, NetworkDetail? Detail);
}
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


using Jube.CaseRisk.Journal;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Dto.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalPeerAccumulator(
        IReadOnlyList<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths)
    {
        public const int MaximumFields = 200;

        private readonly JournalColumnSet columns = new();
        private readonly Dictionary<string, KeyValues> keys = new(StringComparer.Ordinal);
        private readonly GetCaseWorkflowXPathByCaseWorkflowIdQueryDto? segmentPath = xPaths.FirstOrDefault(x => x.Segment);

        public int Rows { get; private set; }

        public int Keys => keys.Count;

        public void Add(string? keyValue, string? json)
        {
            if (string.IsNullOrEmpty(keyValue) || string.IsNullOrEmpty(json))
            {
                return;
            }

            JObject document;

            try
            {
                document = JObject.Parse(json);
            }
            catch (JsonException)
            {
                return;
            }

            var row = new Dictionary<string, object?>();

            foreach (var lens in JournalLensReader.Lenses)
            {
                JournalLensReader.Read(document, lens, row, columns, xPaths);
            }

            if (!keys.TryGetValue(keyValue, out var entry))
            {
                entry = new KeyValues(SegmentOf(document));
                keys[keyValue] = entry;
            }

            Rows++;

            foreach (var (name, value) in row)
            {
                if (value is not (long or double or int))
                {
                    continue;
                }

                if (!entry.Values.TryGetValue(name, out var list))
                {
                    list = [];
                    entry.Values[name] = list;
                }

                list.Add(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public IReadOnlyList<PeerDistribution> Build()
        {
            var measures = columns.All
                .Where(column => column.Type == "number"
                                 && FieldRoleInference.Infer(column.RoleId, column.Type, column.Drill, [])
                                     .RoleId == (int)JournalFieldRole.Measure)
                .Select(column => column.Name).OrderBy(name => name, StringComparer.Ordinal)
                .Take(MaximumFields).ToList();

            var distributions = new List<PeerDistribution>();
            var segments = keys.Values.Select(entry => entry.Segment).Where(segment => segment is not null)
                .Distinct(StringComparer.Ordinal).ToList();

            foreach (var field in measures)
            {
                var all = keys.Values.Where(entry => entry.Values.ContainsKey(field)).ToList();
                var overall = PeerDistributionCalculator.Build(field, null, Lists(all, field));

                if (overall is not null)
                {
                    distributions.Add(overall);
                }

                foreach (var segment in segments)
                {
                    var inSegment = all.Where(entry => entry.Segment == segment).ToList();
                    var built = PeerDistributionCalculator.Build(field, segment, Lists(inSegment, field));

                    if (built is not null)
                    {
                        distributions.Add(built);
                    }
                }
            }

            return distributions;
        }

        private static IReadOnlyList<IReadOnlyList<double>> Lists(List<KeyValues> entries, string field)
        {
            return entries.Select(entry => (IReadOnlyList<double>)entry.Values[field]).ToList();
        }

        private string? SegmentOf(JObject document)
        {
            if (segmentPath is null)
            {
                return null;
            }

            try
            {
                var token = document.SelectToken(segmentPath.XPath);

                if (token is not null && token.Type is not (JTokenType.Object or JTokenType.Array or JTokenType.Null))
                {
                    var value = token.ToString();

                    return string.IsNullOrEmpty(value) ? null : value;
                }
            }
            catch (Exception e) when (e is JsonException or ArgumentException)
            {
                return null;
            }

            return null;
        }

        private sealed class KeyValues(string? segment)
        {
            public string? Segment { get; } = segment;

            public Dictionary<string, List<double>> Values { get; } = new(StringComparer.Ordinal);
        }
    }
}

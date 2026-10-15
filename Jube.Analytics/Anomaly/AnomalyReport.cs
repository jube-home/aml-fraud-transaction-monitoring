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

using System.Text.Json;
using System.Text.Json.Serialization;
using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed record AnomalyReport
    {
        private static readonly JsonSerializerOptions writeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public string Algorithm { get; init; } = string.Empty;
        public string Requested { get; init; } = string.Empty;
        public string? Parameters { get; init; }
        public int Seed { get; init; }
        public int Rows { get; init; }
        public int InputVariables { get; init; }
        public int KeptVariables { get; init; }
        public int DetectorDimensions { get; init; }
        public int Components { get; init; }
        public string Reduction { get; init; } = "None";
        public string Scaling { get; init; } = "None";
        public IReadOnlyList<VariableDecision> Variables { get; init; } = [];
        public ScoreSummary? Scores { get; init; }
        public ThresholdApplied? Threshold { get; init; }
        public AutoEvaluation? Auto { get; init; }
        public IReadOnlyList<string> Warnings { get; init; } = [];
        public long ElapsedMilliseconds { get; init; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, writeOptions);
        }

        public static AnomalyReport? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<AnomalyReport>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public string Summary()
        {
            var flagged = Threshold == null
                ? "no threshold"
                : $"flagged {Threshold.Flagged} of {Threshold.Rows} ({Threshold.FlaggedFraction:P1}) at {Threshold.Method} {Threshold.Value:0.####}";

            var variables = Reduction == "None"
                ? $"{InputVariables} variables, no reduction"
                : $"{KeptVariables} of {InputVariables} variables kept" +
                  (Components > 0 ? $", {Components} components" : string.Empty);

            return
                $"{Algorithm}{(Requested != Algorithm ? $" (selected by {Requested})" : string.Empty)}; {variables}; {flagged}";
        }
    }
}
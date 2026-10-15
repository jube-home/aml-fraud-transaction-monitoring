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

namespace Jube.Engine.AdvancedAnalytics.Serialisation
{
    public sealed class AdvancedAnalyticsModelEnvelope<TSnapshot> where TSnapshot : AdvancedAnalyticsSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        [JsonProperty(Required = Required.Always)]
        public string Kind { get; set; } = string.Empty;

        [JsonProperty(Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int InputCount { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string[] VariablesInOrder { get; set; } = [];

        [JsonProperty(Required = Required.Always)]
        public TSnapshot Payload { get; set; } = null!;

        [JsonProperty(Required = Required.AllowNull)]
        public AdvancedAnalyticsCalibratorSnapshot Calibrator { get; set; }

        public static AdvancedAnalyticsModelEnvelope<TSnapshot> Create(
            string kind,
            int schemaVersion,
            IReadOnlyList<string> variablesInOrder,
            TSnapshot payload,
            AdvancedAnalyticsCalibratorSnapshot calibrator = null)
        {
            return new AdvancedAnalyticsModelEnvelope<TSnapshot>
            {
                Kind = kind,
                SchemaVersion = schemaVersion,
                InputCount = variablesInOrder.Count,
                VariablesInOrder = [.. variablesInOrder],
                Payload = payload,
                Calibrator = calibrator
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Kind))
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope kind is missing.");
            }

            if (SchemaVersion < 1 || SchemaVersion > CurrentSchemaVersion)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Envelope schema version {SchemaVersion} is not supported; supported versions are 1 to {CurrentSchemaVersion}.");
            }

            if (VariablesInOrder == null)
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope variablesInOrder is missing.");
            }

            if (InputCount < 1)
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope input count must be at least one.");
            }

            if (InputCount != VariablesInOrder.Length)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Envelope input count {InputCount} does not match the {VariablesInOrder.Length} variables listed.");
            }

            if (VariablesInOrder.Any(string.IsNullOrWhiteSpace))
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope variablesInOrder contains an empty name.");
            }

            if (VariablesInOrder.Distinct(StringComparer.Ordinal).Count() != VariablesInOrder.Length)
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope variablesInOrder contains a duplicate name.");
            }

            if (Payload == null)
            {
                throw new AdvancedAnalyticsSnapshotException("Envelope payload is missing.");
            }

            Payload.Validate(InputCount);
            Calibrator?.Validate();
        }
    }
}
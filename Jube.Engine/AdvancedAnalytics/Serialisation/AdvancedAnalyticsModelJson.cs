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
using System.Globalization;
using System.IO;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jube.Engine.AdvancedAnalytics.Serialisation
{
    public static class AdvancedAnalyticsModelJson
    {
        public const int DefaultMaximumCharacters = 16 * 1024 * 1024;
        public const int MaximumDepth = 32;

        private static readonly JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Formatting = Formatting.None,
            FloatFormatHandling = FloatFormatHandling.String,
            FloatParseHandling = FloatParseHandling.Double,
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MaxDepth = MaximumDepth,
            Culture = CultureInfo.InvariantCulture
        };

        private static readonly JsonSerializer serializer = JsonSerializer.Create(settings);

        public static string Serialise<TSnapshot>(
            AdvancedAnalyticsModelEnvelope<TSnapshot> envelope,
            int maximumCharacters = DefaultMaximumCharacters) where TSnapshot : AdvancedAnalyticsSnapshot
        {
            ArgumentNullException.ThrowIfNull(envelope);

            envelope.Validate();

            var json = JsonConvert.SerializeObject(envelope, settings);

            if (json.Length > maximumCharacters)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Serialised model is {json.Length} characters, above the limit of {maximumCharacters}.");
            }

            return json;
        }

        public static string ReadKind(string json, int maximumCharacters = DefaultMaximumCharacters)
        {
            var root = Parse(json, maximumCharacters);

            if (root["kind"] is not JValue { Type: JTokenType.String } kind || string.IsNullOrWhiteSpace((string)kind))
            {
                throw new AdvancedAnalyticsSnapshotException("Model JSON has no kind.");
            }

            return (string)kind!;
        }

        public static AdvancedAnalyticsModelEnvelope<TSnapshot> Deserialise<TSnapshot>(
            string json,
            string expectedKind,
            int maximumCharacters = DefaultMaximumCharacters) where TSnapshot : AdvancedAnalyticsSnapshot
        {
            var root = Parse(json, maximumCharacters);

            AdvancedAnalyticsModelEnvelope<TSnapshot> envelope;

            try
            {
                envelope = root.ToObject<AdvancedAnalyticsModelEnvelope<TSnapshot>>(serializer);
            }
            catch (Exception e) when (e is JsonException or InvalidCastException or FormatException
                                          or OverflowException)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Model JSON does not match the envelope shape: {e.Message}",
                    e);
            }

            if (envelope == null)
            {
                throw new AdvancedAnalyticsSnapshotException("Model JSON is empty.");
            }

            if (!string.Equals(envelope.Kind, expectedKind, StringComparison.Ordinal))
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Model kind '{envelope.Kind}' does not match the expected kind '{expectedKind}'.");
            }

            envelope.Validate();

            return envelope;
        }

        private static JObject Parse(string json, int maximumCharacters)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new AdvancedAnalyticsSnapshotException("Model JSON is empty.");
            }

            if (json.Length > maximumCharacters)
            {
                throw new AdvancedAnalyticsSnapshotException(
                    $"Model JSON is {json.Length} characters, above the limit of {maximumCharacters}.");
            }

            try
            {
                using var reader = new JsonTextReader(new StringReader(json));
                reader.MaxDepth = MaximumDepth;
                reader.FloatParseHandling = FloatParseHandling.Double;
                reader.DateParseHandling = DateParseHandling.None;

                var token = JToken.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Ignore
                });

                if (reader.Read())
                {
                    throw new AdvancedAnalyticsSnapshotException("Model JSON has trailing content.");
                }

                return token as JObject ?? throw new AdvancedAnalyticsSnapshotException("Model JSON is not an object.");
            }
            catch (JsonException e)
            {
                throw new AdvancedAnalyticsSnapshotException($"Model JSON is malformed: {e.Message}", e);
            }
        }
    }
}
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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    public static class AdvancedAnalyticsEvidenceJson
    {
        private static readonly JsonSerializerSettings settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() },
            FloatFormatHandling = FloatFormatHandling.String,
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None
        };

        public static string Serialise<T>(T value)
        {
            return JsonConvert.SerializeObject(value, settings);
        }

        public static T Deserialise<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static string PeekKind(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JObject.Parse(json)["kind"]?.Value<string>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static AdvancedAnalyticsLogisticEvidence FromResult(LogisticRegressionResult result)
        {
            return new AdvancedAnalyticsLogisticEvidence
            {
                Kind = result.Kind,
                Seed = result.Seed,
                Variables = result.Variables,
                SplitSizes = result.SplitSizes,
                TrainMetrics = result.TrainMetrics,
                ValidationMetrics = result.ValidationMetrics,
                TestMetrics = result.TestMetrics,
                Coefficients = result.Coefficients,
                Preprocessing = result.Preprocessing,
                Calibration = result.Calibration,
                Priors = result.Priors,
                Fit = result.Fit,
                Warnings = result.Warnings
            };
        }
    }
}
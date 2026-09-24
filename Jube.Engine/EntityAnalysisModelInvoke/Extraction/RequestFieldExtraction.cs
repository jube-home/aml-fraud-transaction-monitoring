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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System;
    using System.Globalization;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class RequestFieldExtraction
    {
        private const int ReferenceDateFromClock = 3;

        public static JObject Parse(TextReader reader)
        {
            return JObject.Load(new JsonTextReader(reader)
            {
                DateParseHandling = DateParseHandling.None
            });
        }

        public static Selection Select(JObject json, string xPath, string defaultValue)
        {
            try
            {
                var value = json.SelectToken(xPath)?.ToString();
                if (value == null && !string.IsNullOrEmpty(defaultValue))
                {
                    return new Selection(defaultValue, true, null);
                }

                return new Selection(value, false, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return string.IsNullOrEmpty(defaultValue)
                    ? new Selection(null, false, ex)
                    : new Selection(defaultValue, true, ex);
            }
        }

        public static bool TryConvert(int dataTypeId, string value, bool defaultFallback, string defaultValue,
            bool assumeLocal, DateTime utcNow, out object converted)
        {
            switch (dataTypeId)
            {
                case 2:
                    if (int.TryParse(value, out var intValue))
                    {
                        converted = intValue;
                        return true;
                    }

                    converted = null;
                    return false;
                case 4:
                    converted = ConvertDate(value, defaultFallback, defaultValue, assumeLocal, utcNow);
                    return true;
                case 5:
                    converted = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                    return true;
                case 3:
                case 6:
                case 7:
                    if (double.TryParse(value, out var doubleValue))
                    {
                        converted = doubleValue;
                        return true;
                    }

                    converted = null;
                    return false;
                default:
                    converted = value;
                    return true;
            }
        }

        public static DateTime ConvertDate(string value, bool defaultFallback, string defaultValue, bool assumeLocal,
            DateTime utcNow)
        {
            if (defaultFallback && int.TryParse(defaultValue, out var daysBack))
            {
                return utcNow.AddDays(-daysBack);
            }

            return ParseDate(value, assumeLocal, utcNow);
        }

        public static DateTime ParseDate(string value, bool assumeLocal, DateTime utcNow)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                assumeLocal ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.UtcDateTime
                : utcNow;
        }

        public static DateTime ReferenceDate(JObject json, int referenceDatePayloadLocationTypeId,
            string referenceDateXPath, bool assumeLocal, DateTime utcNow)
        {
            if (referenceDatePayloadLocationTypeId == ReferenceDateFromClock)
            {
                return utcNow;
            }

            var token = json?.SelectToken(referenceDateXPath);
            return token == null ? utcNow : ParseDate(token.Value<string>(), assumeLocal, utcNow);
        }

        public sealed record Selection(string Value, bool DefaultFallback, Exception Error);
    }
}
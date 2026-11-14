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
    using System.IO;
    using System.Text;
    using Newtonsoft.Json.Linq;

    public sealed class RequestJsonAccessor
    {
        private readonly byte[] bytes;
        private readonly FastPathRequestReader reader;
        private JObject jObject;
        private bool jObjectParsed;

        private RequestJsonAccessor(byte[] bytes, FastPathRequestReader reader, JObject jObject, bool jObjectParsed)
        {
            this.bytes = bytes;
            this.reader = reader;
            this.jObject = jObject;
            this.jObjectParsed = jObjectParsed;
        }

        public JObject JObject
        {
            get
            {
                if (jObjectParsed)
                {
                    return jObject;
                }

                jObject = ParseNewtonsoft(bytes);
                jObjectParsed = true;

                return jObject;
            }
        }

        public static RequestJsonAccessor Create(MemoryStream inputStream, FastPathPlan plan = null)
        {
            if (inputStream.Length == 0)
            {
                return new RequestJsonAccessor(null, null, null, true);
            }

            var bytes = inputStream.ToArray();
            var reader = plan != null ? FastPathRequestReader.TryRead(bytes, plan) : null;

            return reader != null
                ? new RequestJsonAccessor(bytes, reader, null, false)
                : new RequestJsonAccessor(bytes, null, ParseNewtonsoft(bytes), true);
        }

        public RequestFieldExtraction.Selection Select(string xPath, string defaultValue)
        {
            if (reader == null || !reader.TrySelect(xPath, out var value))
            {
                return RequestFieldExtraction.Select(JObject, xPath, defaultValue);
            }

            return value == null && !string.IsNullOrEmpty(defaultValue)
                ? new RequestFieldExtraction.Selection(defaultValue, true, null)
                : new RequestFieldExtraction.Selection(value, false, null);
        }

        public bool TryGetInt32(string xPath, out int value)
        {
            value = 0;

            return reader != null && reader.TryGetInt32(xPath, out value);
        }

        public bool TryGetDouble(string xPath, out double value)
        {
            value = 0;

            return reader != null && reader.TryGetDouble(xPath, out value);
        }

        public bool TryGetBoolean(string xPath, out bool value)
        {
            value = false;

            return reader != null && reader.TryGetBoolean(xPath, out value);
        }

        public bool TrySelectToString(string xPath, out string value)
        {
            value = null;

            if (reader != null && reader.TrySelect(xPath, out value))
            {
                return true;
            }

            value = Jube.Data.Helpers.BoundedJsonPath.Select(JObject, xPath)?.ToString();

            return value != null;
        }

        public string SelectToString(string xPath)
        {
            return reader != null && reader.TrySelect(xPath, out var value)
                ? value
                : Jube.Data.Helpers.BoundedJsonPath.Select(JObject, xPath)?.ToString();
        }

        public DateTime ReferenceDate(int referenceDatePayloadLocationTypeId, string referenceDateXPath,
            bool assumeLocal, DateTime utcNow)
        {
            if (referenceDatePayloadLocationTypeId != RequestFieldExtraction.ReferenceDateFromClock &&
                reader != null && reader.TrySelectString(referenceDateXPath, out var value))
            {
                return value == null ? utcNow : RequestFieldExtraction.ParseDate(value, assumeLocal, utcNow);
            }

            return RequestFieldExtraction.ReferenceDate(JObject, referenceDatePayloadLocationTypeId,
                referenceDateXPath, assumeLocal, utcNow);
        }

        private static JObject ParseNewtonsoft(byte[] bytes)
        {
            return bytes == null
                ? null
                : RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));
        }
    }
}

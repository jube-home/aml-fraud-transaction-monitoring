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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;

namespace Jube.Dictionary.Extensions
{
    internal static class StructuredDataSupport
    {
        internal const int MaximumDocumentLength = 1_000_000;

        internal static JsonNode? Parse(string? json)
        {
            var text = json ?? "";
            if (text.Length is 0 or > MaximumDocumentLength)
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { MaxDepth = 64 });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal static JsonNode? Navigate(JsonNode? root, string? path)
        {
            var current = root;
            var text = (path ?? "").Trim();
            if (text.StartsWith('$'))
            {
                text = text.Substring(1);
            }

            var i = 0;
            while (i < text.Length && current is not null)
            {
                if (text[i] == '.')
                {
                    i++;
                    var start = i;
                    while (i < text.Length && text[i] != '.' && text[i] != '[')
                    {
                        i++;
                    }

                    current = current is JsonObject obj &&
                              obj.TryGetPropertyValue(text.Substring(start, i - start), out var child)
                        ? child
                        : null;
                }
                else if (text[i] == '[')
                {
                    var close = text.IndexOf(']', i);
                    if (close < 0)
                    {
                        return null;
                    }

                    var inner = text.Substring(i + 1, close - i - 1).Trim();
                    i = close + 1;
                    if (inner.Length >= 2 && (inner[0] == '\'' || inner[0] == '"') && inner[^1] == inner[0])
                    {
                        current = current is JsonObject obj &&
                                  obj.TryGetPropertyValue(inner.Substring(1, inner.Length - 2), out var child)
                            ? child
                            : null;
                    }
                    else if (int.TryParse(inner, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                                 out var index) &&
                             current is JsonArray array)
                    {
                        if (index < 0)
                        {
                            index += array.Count;
                        }

                        current = index >= 0 && index < array.Count ? array[index] : null;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    var start = i;
                    while (i < text.Length && text[i] != '.' && text[i] != '[')
                    {
                        i++;
                    }

                    current = current is JsonObject obj &&
                              obj.TryGetPropertyValue(text.Substring(start, i - start), out var child)
                        ? child
                        : null;
                }
            }

            return current;
        }

        internal static XmlDocument? ParseXml(string? xml)
        {
            var text = xml ?? "";
            if (text.Length is 0 or > MaximumDocumentLength)
            {
                return null;
            }

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = MaximumDocumentLength
            };

            try
            {
                using var stringReader = new StringReader(text);
                using var reader = XmlReader.Create(stringReader, settings);
                var document = new XmlDocument { XmlResolver = null };
                document.Load(reader);
                return document;
            }
            catch (XmlException)
            {
                return null;
            }
        }
    }
}
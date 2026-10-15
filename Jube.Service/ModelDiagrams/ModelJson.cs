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
using Jube.Service.ModelDiagrams.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams
{
    public static class ModelJson
    {
        public static JToken Parse(string json, ModelDiagramOptions options)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ModelDiagramNotRecognisedException("The model JSON is empty.");
            }

            if (json.Length > options.MaximumJsonCharacters)
            {
                throw new ModelDiagramNotSupportedException(
                    "The model JSON has " + json.Length + " characters and the maximum is " +
                    options.MaximumJsonCharacters + ". " + ModelDiagramNotSupportedException.SummaryAdvice);
            }

            try
            {
                using var reader = new JsonTextReader(new StringReader(json));
                reader.MaxDepth = options.MaximumJsonDepth;
                reader.DateParseHandling = DateParseHandling.None;
                reader.FloatParseHandling = FloatParseHandling.Double;
                reader.CloseInput = true;

                var token = JToken.ReadFrom(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Ignore
                });

                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.Comment)
                    {
                        throw new ModelDiagramNotRecognisedException("The model JSON has content after the document.");
                    }
                }

                return token;
            }
            catch (JsonReaderException e) when (e.Message.Contains("MaxDepth", StringComparison.Ordinal))
            {
                throw new ModelDiagramNotSupportedException(
                    "The model JSON is nested deeper than " + options.MaximumJsonDepth + " levels. " +
                    ModelDiagramNotSupportedException.SummaryAdvice, e);
            }
            catch (Exception e) when (e is JsonException or ArgumentException)
            {
                throw new ModelDiagramNotRecognisedException("The model text is not valid JSON: " + e.Message, e);
            }
        }

        public static JToken? Get(JToken? token, string name)
        {
            return token is JObject o ? o.GetValue(name, StringComparison.OrdinalIgnoreCase) : null;
        }

        public static bool Has(JToken? token, string name)
        {
            var v = Get(token, name);

            return v is not null && v.Type != JTokenType.Null;
        }

        public static string? String(JToken? token, string name)
        {
            return Get(token, name) is { Type: JTokenType.String } v ? v.Value<string>() : null;
        }

        public static double? Number(JToken? token)
        {
            switch (token)
            {
                case { Type: JTokenType.Float } or { Type: JTokenType.Integer }:
                    return token.Value<double>();
                case { Type: JTokenType.String }:
                    var s = token.Value<string>();

                    return s switch
                    {
                        "NaN" => double.NaN,
                        "Infinity" => double.PositiveInfinity,
                        "-Infinity" => double.NegativeInfinity,
                        _ => null
                    };
                default:
                    return null;
            }
        }

        public static double? Number(JToken? token, string name)
        {
            return Number(Get(token, name));
        }

        public static int? Integer(JToken? token, string name)
        {
            var v = Get(token, name);

            if (v is { Type: JTokenType.Integer })
            {
                var l = v.Value<long>();

                return l is >= int.MinValue and <= int.MaxValue ? (int)l : null;
            }

            return null;
        }

        public static double[]? Doubles(JToken? token)
        {
            if (token is not JArray a)
            {
                return null;
            }

            var result = new double[a.Count];

            for (var i = 0; i < result.Length; i++)
            {
                if (Number(a[i]) is not { } d)
                {
                    return null;
                }

                result[i] = d;
            }

            return result;
        }

        public static double[]? Doubles(JToken? token, string name)
        {
            return Doubles(Get(token, name));
        }

        public static double[][]? Matrix(JToken? token)
        {
            if (token is not JArray a)
            {
                return null;
            }

            var result = new double[a.Count][];

            for (var i = 0; i < result.Length; i++)
            {
                if (Doubles(a[i]) is not { } row)
                {
                    return null;
                }

                result[i] = row;
            }

            return result;
        }

        public static double[][]? Matrix(JToken? token, string name)
        {
            return Matrix(Get(token, name));
        }

        public static int[]? Integers(JToken? token)
        {
            if (token is not JArray a)
            {
                return null;
            }

            var result = new int[a.Count];

            for (var i = 0; i < result.Length; i++)
            {
                if (a[i] is not { Type: JTokenType.Integer } v)
                {
                    return null;
                }

                var l = v.Value<long>();

                if (l is < int.MinValue or > int.MaxValue)
                {
                    return null;
                }

                result[i] = (int)l;
            }

            return result;
        }

        public static int[]? Integers(JToken? token, string name)
        {
            return Integers(Get(token, name));
        }

        public static int[][]? IntegerRows(JToken? token, string name)
        {
            if (Get(token, name) is not JArray a)
            {
                return null;
            }

            var result = new int[a.Count][];

            for (var i = 0; i < result.Length; i++)
            {
                if (Integers(a[i]) is not { } row)
                {
                    return null;
                }

                result[i] = row;
            }

            return result;
        }

        public static string[]? Strings(JToken? token, string name)
        {
            if (Get(token, name) is not JArray a)
            {
                return null;
            }

            var result = new string[a.Count];

            for (var i = 0; i < result.Length; i++)
            {
                if (a[i] is not { Type: JTokenType.String } v)
                {
                    return null;
                }

                result[i] = v.Value<string>() ?? string.Empty;
            }

            return result;
        }

        public static bool IsRectangular(double[][] rows, int rowCount, int columnCount)
        {
            return rows.Length == rowCount && rows.All(r => r.Length == columnCount);
        }

        public static string Format(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        public static ModelDiagramNotRecognisedException Inconsistent(string what)
        {
            return new ModelDiagramNotRecognisedException("The model JSON is inconsistent: " + what + ".");
        }
    }
}
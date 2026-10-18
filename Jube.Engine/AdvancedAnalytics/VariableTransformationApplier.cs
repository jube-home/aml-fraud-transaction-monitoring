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
using System.Linq;
using Accord.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jube.Engine.AdvancedAnalytics.Models;

namespace Jube.Engine.AdvancedAnalytics
{
    public static class VariableTransformationApplier
    {
        private const double Epsilon = 1e-12;

        public static bool IsCategorical(VariableTransformationKind kind)
        {
            return kind is VariableTransformationKind.LabelOrdinal or VariableTransformationKind.OneHotIndicator
                or VariableTransformationKind.TargetMean or VariableTransformationKind.WeightOfEvidence
                or VariableTransformationKind.Frequency;
        }

        public static string SourceNameOverride(string parametersJson)
        {
            var parameters = Parse(parametersJson);

            return parameters == null ? null : GetString(parameters, "sourceName");
        }

        public static double ApplyNumeric(VariableTransformationKind kind, string parametersJson, double value)
        {
            if (kind == VariableTransformationKind.None)
            {
                return value;
            }

            if (IsCategorical(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind,
                    $"{kind} is a categorical transformation; call {nameof(ApplyCategorical)} instead.");
            }

            try
            {
                var parameters = Parse(parametersJson);

                return kind switch
                {
                    VariableTransformationKind.Log => ColumnFunction(value, parameters,
                        x => x > 0 ? Math.Log(x) : double.NaN),
                    VariableTransformationKind.Log10 => ColumnFunction(value, parameters,
                        x => x > 0 ? Math.Log10(x) : double.NaN),
                    VariableTransformationKind.Log2 => ColumnFunction(value, parameters,
                        x => x > 0 ? Math.Log2(x) : double.NaN),
                    VariableTransformationKind.Log1P => ColumnFunction(value, parameters,
                        x => x > -1 ? Math.Log(1 + x) : double.NaN),
                    VariableTransformationKind.SignedLog1P => ColumnFunction(value, parameters,
                        x => Math.Sign(x) * Math.Log(1 + Math.Abs(x))),
                    VariableTransformationKind.Sqrt => ColumnFunction(value, parameters,
                        x => x >= 0 ? Math.Sqrt(x) : double.NaN),
                    VariableTransformationKind.SignedSqrt => ColumnFunction(value, parameters,
                        x => Math.Sign(x) * Math.Sqrt(Math.Abs(x))),
                    VariableTransformationKind.CubeRoot => ColumnFunction(value, parameters, Math.Cbrt),
                    VariableTransformationKind.Reciprocal => ColumnFunction(value, parameters,
                        x => x != 0 ? 1 / x : double.NaN),
                    VariableTransformationKind.Square => ColumnFunction(value, parameters, x => x * x),
                    VariableTransformationKind.AbsoluteValue => ColumnFunction(value, parameters, Math.Abs),
                    VariableTransformationKind.BoxCox => Power(value, parameters, boxCox: true),
                    VariableTransformationKind.YeoJohnson => Power(value, parameters, boxCox: false),
                    VariableTransformationKind.ZScoreStandard => Scale(value, parameters),
                    VariableTransformationKind.MinMax => Scale(value, parameters),
                    VariableTransformationKind.RobustMad => Scale(value, parameters),
                    VariableTransformationKind.RobustIqr => Scale(value, parameters),
                    VariableTransformationKind.MaxAbs => Scale(value, parameters),
                    VariableTransformationKind.QuantileUniform => Quantile(value, parameters, normal: false),
                    VariableTransformationKind.QuantileNormal => Quantile(value, parameters, normal: true),
                    VariableTransformationKind.Winsorise => Winsorise(value, parameters),
                    VariableTransformationKind.DiscretiseEqualWidth => Discretise(value, parameters),
                    VariableTransformationKind.DiscretiseEqualFrequency => Discretise(value, parameters),
                    VariableTransformationKind.DiscretiseKMeans => Discretise(value, parameters),
                    VariableTransformationKind.DiscretiseSupervised => Discretise(value, parameters),
                    VariableTransformationKind.DiscretiseCustomEdges => Discretise(value, parameters),
                    VariableTransformationKind.CyclicalSin => Cyclical(value, parameters, sine: true),
                    VariableTransformationKind.CyclicalCos => Cyclical(value, parameters, sine: false),
                    _ => value
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return value;
            }
        }

        public static double ApplyCategorical(VariableTransformationKind kind, string parametersJson, string text)
        {
            if (!IsCategorical(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind,
                    $"{kind} is not a categorical transformation; call {nameof(ApplyNumeric)} instead.");
            }

            try
            {
                var parameters = Parse(parametersJson);

                return kind switch
                {
                    VariableTransformationKind.LabelOrdinal => LabelOrdinal(text, parameters),
                    VariableTransformationKind.OneHotIndicator => OneHotIndicator(text, parameters),
                    VariableTransformationKind.TargetMean => CategoryLookup(text, parameters),
                    VariableTransformationKind.WeightOfEvidence => CategoryLookup(text, parameters),
                    VariableTransformationKind.Frequency => CategoryLookup(text, parameters),
                    _ => 0
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return 0;
            }
        }

        private static double ColumnFunction(double value, JObject parameters, Func<double, double> function)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var offset = GetDouble(parameters, "offset", 0);
            var y = function(value + offset);

            return double.IsNaN(y) ? double.NaN : y;
        }

        private static double Power(double value, JObject parameters, bool boxCox)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var lambda = GetDouble(parameters, "lambda", 1);

            if (boxCox)
            {
                if (value <= 0)
                {
                    return double.NaN;
                }

                return Math.Abs(lambda) < Epsilon ? Math.Log(value) : (Math.Pow(value, lambda) - 1) / lambda;
            }

            if (value >= 0)
            {
                return Math.Abs(lambda) < Epsilon ? Math.Log(value + 1) : (Math.Pow(value + 1, lambda) - 1) / lambda;
            }

            return Math.Abs(lambda - 2) < Epsilon
                ? -Math.Log(-value + 1)
                : -(Math.Pow(-value + 1, 2 - lambda) - 1) / (2 - lambda);
        }

        private static double Scale(double value, JObject parameters)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var center = GetDouble(parameters, "center", 0);
            var scale = GetDouble(parameters, "scale", 1);

            return scale == 0 || !double.IsFinite(scale) ? 0 : (value - center) / scale;
        }

        private static double Quantile(double value, JObject parameters, bool normal)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var references = GetDoubleArray(parameters, "referenceQuantiles");

            if (references.Length < 2)
            {
                return double.NaN;
            }

            var p = EmpiricalPosition(references, value);

            return normal ? Normal.Inverse(Math.Min(Math.Max(p, 1e-7), 1 - 1e-7)) : p;
        }

        private static double EmpiricalPosition(double[] references, double x)
        {
            var n = references.Length;

            if (x <= references[0])
            {
                return 0;
            }

            if (x >= references[n - 1])
            {
                return 1;
            }

            var high = 0;

            while (high + 1 < n && references[high + 1] <= x)
            {
                high++;
            }

            var upper = high + (x - references[high]) / (references[high + 1] - references[high]);

            var low = n - 1;

            while (low - 1 >= 0 && references[low - 1] >= x)
            {
                low--;
            }

            var lower = Math.Abs(references[low] - x) < 0.0001
                ? low
                : low - 1 + (x - references[low - 1]) / (references[low] - references[low - 1]);

            return (upper + lower) / 2 / (n - 1);
        }

        private static double Winsorise(double value, JObject parameters)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var lower = GetDouble(parameters, "lowerBound", double.NegativeInfinity);
            var upper = GetDouble(parameters, "upperBound", double.PositiveInfinity);

            if (value < lower)
            {
                return lower;
            }

            return value > upper ? upper : value;
        }

        private static double Discretise(double value, JObject parameters)
        {
            if (IsMissing(value))
            {
                return -1;
            }

            var edges = GetDoubleArray(parameters, "edges");
            var index = 0;

            while (index < edges.Length && edges[index] < value)
            {
                index++;
            }

            return index;
        }

        private static double Cyclical(double value, JObject parameters, bool sine)
        {
            if (IsMissing(value))
            {
                return double.NaN;
            }

            var period = GetDouble(parameters, "period", 24);

            if (!(period > 0) || !double.IsFinite(period))
            {
                return double.NaN;
            }

            return sine ? Math.Sin(2 * Math.PI * value / period) : Math.Cos(2 * Math.PI * value / period);
        }

        private static double LabelOrdinal(string text, JObject parameters)
        {
            var categories = GetStringArray(parameters, "categories");
            var unknownCode = GetDouble(parameters, "unknownCode", -1);

            if (string.IsNullOrEmpty(text))
            {
                return unknownCode;
            }

            var index = Array.IndexOf(categories, text);

            return index >= 0 ? index : unknownCode;
        }

        private static double OneHotIndicator(string text, JObject parameters)
        {
            var category = GetString(parameters, "category");

            return !string.IsNullOrEmpty(text) && string.Equals(text, category, StringComparison.Ordinal) ? 1 : 0;
        }

        private static double CategoryLookup(string text, JObject parameters)
        {
            var categories = GetStringArray(parameters, "categories");
            var values = GetDoubleArray(parameters, "values");
            var defaultValue = GetDouble(parameters, "defaultValue", 0);
            var key = text ?? string.Empty;
            var index = Array.IndexOf(categories, key);

            return index >= 0 && index < values.Length ? values[index] : defaultValue;
        }

        private static bool IsMissing(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        private static JObject Parse(string parametersJson)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return null;
            }

            try
            {
                return JObject.Parse(parametersJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static JProperty Find(JObject parameters, string name)
        {
            return parameters?.Properties()
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static double GetDouble(JObject parameters, string name, double fallback)
        {
            var property = Find(parameters, name);

            return property is { Value.Type: JTokenType.Float or JTokenType.Integer }
                ? property.Value.Value<double>()
                : fallback;
        }

        private static string GetString(JObject parameters, string name)
        {
            var property = Find(parameters, name);

            return property is { Value.Type: JTokenType.String } ? property.Value.Value<string>() : null;
        }

        private static double[] GetDoubleArray(JObject parameters, string name)
        {
            var property = Find(parameters, name);

            return property is { Value.Type: JTokenType.Array }
                ? [.. property.Value.Select(t => t.Value<double>())]
                : [];
        }

        private static string[] GetStringArray(JObject parameters, string name)
        {
            var property = Find(parameters, name);

            return property is { Value.Type: JTokenType.Array }
                ? [.. property.Value.Select(t => t.Value<string>() ?? string.Empty)]
                : [];
        }
    }
}
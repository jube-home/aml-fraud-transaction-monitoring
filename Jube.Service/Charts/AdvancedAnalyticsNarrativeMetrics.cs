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

using System.Reflection;

namespace Jube.Service.Charts
{
    internal sealed record AdvancedAnalyticsQualitySummary(
        int? Rows,
        int? Positives,
        double? RocAuc,
        double? F1,
        double? Precision,
        double? Recall,
        double? BrierScore,
        bool? Calibrated,
        double? CalibrationSlope,
        double? CalibrationIntercept);

    internal static class AdvancedAnalyticsNarrativeMetrics
    {
        public static AdvancedAnalyticsQualitySummary? FromDetails(object? details)
        {
            if (details == null)
            {
                return null;
            }

            var split = ReadObject(details, "TestMetrics");

            if (split == null)
            {
                return null;
            }

            var calibrated = ReadBool(details, "Calibrated") ?? ReadBool(ReadObject(details, "Calibration"),
                "Calibrated");
            var slope = ReadDouble(details, "CalibrationSlope") ?? ReadDouble(ReadObject(details, "Calibration"),
                "Slope");
            var intercept = ReadDouble(details, "CalibrationIntercept") ?? ReadDouble(
                ReadObject(details, "Calibration"), "Intercept");

            return new AdvancedAnalyticsQualitySummary(
                ReadInt(split, "Rows"),
                ReadInt(split, "Positives"),
                ReadDouble(split, "RocAuc"),
                ReadDouble(split, "F1"),
                ReadDouble(split, "Precision"),
                ReadDouble(split, "Recall"),
                ReadDouble(split, "BrierScore"),
                calibrated,
                slope,
                intercept);
        }

        private static object? ReadObject(object? source, string propertyName)
        {
            return source?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(source);
        }

        private static double? ReadDouble(object? source, string propertyName)
        {
            return ReadObject(source, propertyName) switch
            {
                double d when double.IsFinite(d) => d,
                _ => null
            };
        }

        private static int? ReadInt(object? source, string propertyName)
        {
            return ReadObject(source, propertyName) as int?;
        }

        private static bool? ReadBool(object? source, string propertyName)
        {
            return ReadObject(source, propertyName) as bool?;
        }
    }
}
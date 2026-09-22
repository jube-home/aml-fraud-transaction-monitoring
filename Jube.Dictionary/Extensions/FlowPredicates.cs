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

using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    internal static partial class FlowPredicates
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        internal static bool SafeRegexIsMatch(string? value, string? pattern)
        {
            if (value is null || pattern is null)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(value, pattern, RegexOptions.None, RegexTimeout);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        internal static bool FuzzyContains(string? value, string? term, double threshold)
        {
            if (value is null || term is null)
            {
                return false;
            }

            var termWords = Words(term);
            if (termWords.Length == 0)
            {
                return false;
            }

            var valueWords = Words(value);
            var window = Math.Min(termWords.Length, valueWords.Length);
            if (window == 0 || valueWords.Length < termWords.Length)
            {
                return false;
            }

            var target = string.Join(' ', termWords);
            for (var i = 0; i + window <= valueWords.Length; i++)
            {
                var candidate = string.Join(' ', valueWords, i, window);
                if (candidate.JaroWinklerSimilarity(target) >= threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] Words(string text)
        {
            return Regex.Split(text.ToLowerInvariant(), "[^\\p{L}\\p{N}]+").Where(w => w.Length > 0).ToArray();
        }

        internal static double ImpliedSpeedMph(double latitude1, double longitude1, double latitude2,
            double longitude2, double hoursElapsed)
        {
            if (hoursElapsed <= 0)
            {
                return double.PositiveInfinity;
            }

            return latitude1.HaversineDistanceMiles(longitude1, latitude2, longitude2) / hoursElapsed;
        }

        private static DateTime? ToLocal(DateTime value, string? zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return null;
            }

            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
                var utc = value.Kind == DateTimeKind.Local
                    ? value.ToUniversalTime()
                    : DateTime.SpecifyKind(value, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }

        internal static bool? LocalWeekday(DateTime value, string? zoneId)
        {
            var local = ToLocal(value, zoneId);
            if (local is null)
            {
                return null;
            }

            return local.Value.DayOfWeek != DayOfWeek.Saturday && local.Value.DayOfWeek != DayOfWeek.Sunday;
        }

        internal static bool LocalHourWithin(DateTime value, string? zoneId, int startHour, int endHour,
            bool weekdaysOnly)
        {
            var local = ToLocal(value, zoneId);
            if (local is null)
            {
                return false;
            }

            if (weekdaysOnly && !(LocalWeekday(value, zoneId) ?? false))
            {
                return false;
            }

            return local.Value.Hour >= startHour && local.Value.Hour < endHour;
        }

        internal static bool LocalHourOutside(DateTime value, string? zoneId, int startHour, int endHour,
            bool weekdaysOnly)
        {
            var local = ToLocal(value, zoneId);
            if (local is null)
            {
                return false;
            }

            return !LocalHourWithin(value, zoneId, startHour, endHour, weekdaysOnly);
        }

        internal static DateTime? ConvertToZone(DateTime value, string? zoneId)
        {
            return ToLocal(value, zoneId);
        }
    }
}

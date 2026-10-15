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

using System.ComponentModel;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Extensions.TimeZonesAndFormats.Models;

namespace Jube.Service.Extensions.TimeZonesAndFormats
{
    [FunctionToolGroup("TimeZonesAndFormats",
        "Converts date-times between UTC and IANA time zones, reports offsets and daylight saving, and converts to and from Unix epoch and standard text formats.")]
    public static class TimeZonesAndFormatsTools
    {
        [FunctionTool("ConvertUtcToTimeZone")]
        [Description(
            "Converts a UTC date-time to the local wall-clock time in an IANA time zone such as Europe/London or Asia/Kolkata, allowing for daylight saving. The result has no zone marker.")]
        public static DateTime ConvertUtcToTimeZone(
            [Description("The UTC date-time, in ISO 8601 form.")]
            DateTime utc,
            [Description("IANA time zone id, such as America/New_York.")]
            string timeZoneId)
        {
            return FunctionToolInput.Utc(utc).UtcToZone(ZoneId(timeZoneId));
        }

        [FunctionTool("ConvertTimeZoneToUtc")]
        [Description(
            "Converts a local wall-clock date-time in an IANA time zone to UTC, allowing for daylight saving. A local time skipped by a clock change is refused.")]
        public static DateTime ConvertTimeZoneToUtc(
            [Description("The local date-time, without a zone marker.")]
            DateTime local,
            [Description("IANA time zone id the local time belongs to.")]
            string timeZoneId)
        {
            var zone = ZoneId(timeZoneId);

            try
            {
                return local.ZoneToUtc(zone);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("That local time does not exist in the zone because of a clock change.",
                    nameof(local));
            }
        }

        [FunctionTool("ConvertBetweenTimeZones")]
        [Description(
            "Converts a local wall-clock date-time in one IANA time zone to the wall-clock time in another; 09:00 in Europe/London in July is 04:00 in America/New_York.")]
        public static DateTime ConvertBetweenTimeZones(
            [Description("The local date-time in the source zone.")]
            DateTime local,
            [Description("IANA time zone id of the source.")]
            string fromTimeZoneId,
            [Description("IANA time zone id of the target.")]
            string toTimeZoneId)
        {
            return ConvertUtcToTimeZone(ConvertTimeZoneToUtc(local, fromTimeZoneId), toTimeZoneId);
        }

        [FunctionTool("DescribeTimeZoneAt")]
        [Description(
            "Offset from UTC in hours and whether daylight saving is in force for an IANA time zone at a given UTC instant; Europe/London is +1 with daylight saving in July.")]
        public static TimeZoneOffset DescribeTimeZoneAt(
            [Description("The UTC instant to evaluate at.")]
            DateTime utc,
            [Description("IANA time zone id.")] string timeZoneId)
        {
            var instant = FunctionToolInput.Utc(utc);
            var zone = ZoneId(timeZoneId);

            return new TimeZoneOffset(zone, instant.UtcOffsetHoursIn(zone), instant.IsDaylightSavingTimeIn(zone));
        }

        [FunctionTool("IsValidTimeZoneId")]
        [Description(
            "Whether a text is a time zone id known to this system, such as Europe/Paris. Use it before converting times with a zone taken from data.")]
        public static bool IsValidTimeZoneId([Description("The time zone id to check.")] string timeZoneId)
        {
            return FunctionToolInput.Text(timeZoneId, nameof(timeZoneId), 64).IsValidTimeZoneId();
        }

        [FunctionTool("ToUnixEpoch")]
        [Description(
            "Converts a UTC date-time to Unix epoch time, returning both seconds and milliseconds since 1970-01-01T00:00:00Z.")]
        public static EpochTime ToUnixEpoch([Description("The UTC date-time, in ISO 8601 form.")] DateTime utc)
        {
            var instant = FunctionToolInput.Utc(utc);

            return new EpochTime(instant.ToEpochSeconds(), instant.ToEpochMilliseconds());
        }

        [FunctionTool("FromUnixEpoch")]
        [Description(
            "Converts a Unix epoch value to a UTC date-time. Values above 100000000000 are usually milliseconds; say which unit the value is in.")]
        public static DateTime FromUnixEpoch(
            [Description("The epoch value.")] double epoch,
            [Description("The unit of the value: Seconds or Milliseconds.")]
            string unit = "Seconds")
        {
            FunctionToolInput.Finite(epoch, nameof(epoch));

            try
            {
                return FunctionToolInput.Choice<EpochUnit>(unit, nameof(unit)) == EpochUnit.Milliseconds
                    ? epoch.FromEpochMilliseconds()
                    : epoch.FromEpochSeconds();
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("The epoch value is outside the years 1 to 9999.", nameof(epoch));
            }
        }

        [FunctionTool("FormatDateTime")]
        [Description(
            "Writes a date-time in a standard text format: Iso8601 (2026-09-23T12:00:00.0000000Z for UTC), Rfc1123 (Wed, 23 Sep 2026 12:00:00 GMT), Sortable (2026-09-23T12:00:00) or UniversalSortable (2026-09-23 12:00:00Z).")]
        public static string FormatDateTime(
            [Description("The date-time to write; mark UTC values with a Z.")]
            DateTime dateTime,
            [Description("The format: Iso8601, Rfc1123, Sortable or UniversalSortable.")]
            string format)
        {
            return FunctionToolInput.Choice<DateTimeTextFormat>(format, nameof(format)) switch
            {
                DateTimeTextFormat.Iso8601 => dateTime.ToIso8601String(),
                DateTimeTextFormat.Rfc1123 => dateTime.ToRfc1123String(),
                DateTimeTextFormat.Sortable => dateTime.ToSortableDateTimeString(),
                _ => dateTime.ToUniversalSortableDateTimeString()
            };
        }

        [FunctionTool("ParseDateTimeWithFormat")]
        [Description(
            "Reads a date-time from text using an exact .NET format pattern such as dd/MM/yyyy HH:mm, treating it as UTC. Use it for unambiguous parsing of data where day and month order matter.")]
        public static DateTime ParseDateTimeWithFormat(
            [Description("The text to read, such as 23/09/2026 14:05.")]
            string text,
            [Description("The exact pattern, such as dd/MM/yyyy HH:mm or yyyyMMdd.")]
            string format)
        {
            var parsed = FunctionToolInput.Text(text, nameof(text), 200)
                .ToDateTimeWithFormat(FunctionToolInput.Required(format, nameof(format), 100));

            return parsed ??
                   throw new ArgumentException($"'{text}' does not match the pattern '{format}'.", nameof(text));
        }

        private static string ZoneId(string? timeZoneId)
        {
            var id = FunctionToolInput.Required(timeZoneId, nameof(timeZoneId), 64).Trim();

            if (!id.IsValidTimeZoneId())
            {
                throw new ArgumentException(
                    $"'{timeZoneId}' is not a known time zone id; use an IANA id such as Europe/London.",
                    nameof(timeZoneId));
            }

            return id;
        }
    }
}
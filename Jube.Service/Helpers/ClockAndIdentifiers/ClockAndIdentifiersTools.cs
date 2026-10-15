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
using Jube.Service.Helpers.ClockAndIdentifiers.Models;

namespace Jube.Service.Helpers.ClockAndIdentifiers
{
    [FunctionToolGroup("ClockAndIdentifiers",
        "The current date and time in UTC or any time zone, the list of time zone ids, and fresh random or time-ordered UUIDs; the only tools here that are not repeatable, because they read the clock or a random source.")]
    public sealed class ClockAndIdentifiersTools
    {
        private readonly TimeProvider timeProvider;

        public ClockAndIdentifiersTools(TimeProvider? timeProvider = null)
        {
            this.timeProvider = timeProvider ?? TimeProvider.System;
        }

        [FunctionTool("CurrentUtcTime")]
        [Description(
            "The current date and time in UTC from the server clock, with today's UTC date. Call it whenever a question depends on now or today; never assume the current date.")]
        public CurrentTime CurrentUtcTime()
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            return new CurrentTime("UTC", now, now.Date, 0);
        }

        [FunctionTool("CurrentTimeInZone")]
        [Description(
            "The current local date and time, today's local date and the UTC offset in an IANA time zone such as Europe/London. Use it for questions about local business hours or the local date.")]
        public CurrentTime CurrentTimeInZone(
            [Description("IANA time zone id, such as Asia/Singapore.")]
            string timeZoneId)
        {
            var id = FunctionToolInput.Required(timeZoneId, nameof(timeZoneId), 64).Trim();

            if (!id.IsValidTimeZoneId())
            {
                throw new ArgumentException($"'{timeZoneId}' is not a known time zone id.", nameof(timeZoneId));
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var local = now.UtcToZone(id);

            return new CurrentTime(id, local, local.Date, now.UtcOffsetHoursIn(id));
        }

        [FunctionTool("ListTimeZoneIds")]
        [Description(
            "Time zone ids known to this system whose id contains a filter, case-insensitive, sorted; filter by city or region such as London, America or Kolkata. At most 100 are returned.")]
        public string[] ListTimeZoneIds(
            [Description("Text the id must contain; empty lists the first 100.")]
            string filter = "")
        {
            var text = FunctionToolInput.Text(filter, nameof(filter), 64).Trim();

            return TimeZoneInfo.GetSystemTimeZones()
                .Select(zone => zone.Id)
                .Where(id => id.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .Take(100)
                .ToArray();
        }

        [FunctionTool("NewRandomUuid")]
        [Description(
            "One or more new random (version 4) UUIDs in canonical lowercase form, for correlation ids or test data. Up to 100 at once.")]
        public string[] NewRandomUuid([Description("How many UUIDs to create, 1 to 100.")] int count = 1)
        {
            return Enumerable.Range(0, Count(count)).Select(_ => Guid.NewGuid().ToString()).ToArray();
        }

        [FunctionTool("NewTimeOrderedUuid")]
        [Description(
            "One or more new time-ordered (version 7) UUIDs, which sort by creation time and suit database keys. Up to 100 at once.")]
        public string[] NewTimeOrderedUuid([Description("How many UUIDs to create, 1 to 100.")] int count = 1)
        {
            var now = timeProvider.GetUtcNow();

            return Enumerable.Range(0, Count(count)).Select(_ => Guid.CreateVersion7(now).ToString()).ToArray();
        }

        [FunctionTool("UuidVersion7Timestamp")]
        [Description(
            "The UTC creation time embedded in a version 7 UUID, to the millisecond. Refused for other UUID versions, which carry no time.")]
        public DateTime UuidVersion7Timestamp([Description("A version 7 UUID.")] string uuid)
        {
            var value = FunctionToolInput.Required(uuid, nameof(uuid), 100).Trim();

            if (value.UuidVersion() != 7)
            {
                throw new ArgumentException("The value is not a version 7 UUID.", nameof(uuid));
            }

            var hex = value.Replace("-", "").Substring(0, 12);

            return DateTime.UnixEpoch.AddMilliseconds(Convert.ToInt64(hex, 16));
        }

        [FunctionTool("TimeSince")]
        [Description(
            "Exact time elapsed from a past date-time until now, in total days, hours and minutes and as completed whole years; negative for a future date-time. Use it for questions such as how long ago an account was opened.")]
        public ElapsedSinceNow TimeSince(
            [Description("The earlier date-time, in UTC ISO 8601 form.")]
            DateTime dateTime)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var then = FunctionToolInput.Utc(dateTime);

            return new ElapsedSinceNow(now, then.DaysBetween(now), then.HoursBetween(now), then.MinutesBetween(now),
                then.WholeYearsBetween(now));
        }

        private static int Count(int count)
        {
            if (count is < 1 or > 100)
            {
                throw new ArgumentException("count must be from 1 to 100.", nameof(count));
            }

            return count;
        }
    }
}
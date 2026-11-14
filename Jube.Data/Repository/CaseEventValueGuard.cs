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

namespace Jube.Data.Repository
{
    using System;
    using System.Globalization;
    using Poco;

    public static class CaseEventValueGuard
    {
        private enum Kind
        {
            Free,
            None,
            Date,
            EnumerationId,
            Guid,
            Number
        }

        public static string Violation(CaseEvent model)
        {
            var (before, after) = KindsFor(model.CaseEventTypeId.GetValueOrDefault());

            return Check(before, model.Before, "Before", model.CaseEventTypeId)
                   ?? Check(after, model.After, "After", model.CaseEventTypeId);
        }

        public static void EnsureCanonical(CaseEvent model)
        {
            var violation = Violation(model);

            if (violation != null)
            {
                throw new InvalidOperationException(violation);
            }
        }

        private static (Kind Before, Kind After) KindsFor(int typeId)
        {
            return typeId switch
            {
                5 => (Kind.None, Kind.Date),
                7 => (Kind.EnumerationId, Kind.EnumerationId),
                9 => (Kind.Guid, Kind.Guid),
                10 => (Kind.Date, Kind.Date),
                11 => (Kind.Number, Kind.Number),
                12 => (Kind.Date, Kind.Date),
                13 => (Kind.Date, Kind.Date),
                15 => (Kind.EnumerationId, Kind.EnumerationId),
                17 => (Kind.Guid, Kind.Guid),
                19 => (Kind.EnumerationId, Kind.None),
                20 => (Kind.None, Kind.Number),
                29 => (Kind.None, Kind.EnumerationId),
                30 => (Kind.EnumerationId, Kind.None),
                31 => (Kind.EnumerationId, Kind.Free),
                34 => (Kind.EnumerationId, Kind.EnumerationId),
                35 => (Kind.EnumerationId, Kind.None),
                _ => (Kind.Free, Kind.Free)
            };
        }

        private static string Check(Kind kind, string value, string side, int? typeId)
        {
            if (string.IsNullOrEmpty(value) || kind == Kind.Free)
            {
                return null;
            }

            var ok = kind switch
            {
                Kind.None => false,
                Kind.Date => IsIsoUtcDate(value),
                Kind.EnumerationId => IsDigits(value),
                Kind.Guid => Guid.TryParse(value, out _),
                Kind.Number => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                _ => true
            };

            return ok
                ? null
                : $"Case event type {typeId} must store {side} as {Describe(kind)} and never display text; got '{value}'.";
        }

        private static bool IsDigits(string value)
        {
            foreach (var c in value)
            {
                if (c is < '0' or > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsIsoUtcDate(string value)
        {
            return value.Length >= 20
                   && value[4] == '-' && value[10] == 'T'
                   && value.EndsWith('Z')
                   && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);
        }

        private static string Describe(Kind kind)
        {
            return kind switch
            {
                Kind.None => "nothing",
                Kind.Date => "an ISO 8601 UTC date such as 2026-11-05T09:00:00.0000000Z",
                Kind.EnumerationId => "the integer enumeration id",
                Kind.Guid => "the Guid",
                Kind.Number => "an invariant-culture number",
                _ => "a value"
            };
        }
    }
}

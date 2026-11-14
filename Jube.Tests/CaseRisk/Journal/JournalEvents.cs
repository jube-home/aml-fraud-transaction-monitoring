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
using System.Collections.Generic;
using System.Linq;
using Jube.CaseRisk.Journal;

namespace Jube.Test.CaseRisk.Journal
{
    internal static class JournalEvents
    {
        public static readonly DateTime Start = new(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);

        public static JournalEvent Event(long id, DateTime at, double? amount = null, string? counterparty = null,
            string? channel = null, int activations = 0, double elevation = 0, double? other = null)
        {
            var measures = new Dictionary<string, double>();
            var categories = new Dictionary<string, string>();

            if (amount.HasValue) measures["Amount"] = amount.Value;
            if (other.HasValue) measures["Other"] = other.Value;
            if (counterparty is not null) categories["Counterparty"] = counterparty;
            if (channel is not null) categories["Channel"] = channel;

            return new JournalEvent(id, at, measures, categories, activations, elevation);
        }

        public static List<JournalEvent> Daily(params double[] amounts)
        {
            return amounts.Select((amount, index) => Event(index + 1, Start.AddDays(index), amount)).ToList();
        }
    }
}

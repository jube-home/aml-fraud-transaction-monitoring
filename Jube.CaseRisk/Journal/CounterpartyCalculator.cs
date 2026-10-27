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


namespace Jube.CaseRisk.Journal
{
    public sealed record CounterpartyEntry(
        string Value,
        int Count,
        double Share,
        double? Amount,
        DateTime FirstSeen,
        DateTime LastSeen,
        bool New);

    public sealed record CounterpartyProfile(
        string Field,
        int Distinct,
        double Concentration,
        int NewCount,
        IReadOnlyList<CounterpartyEntry> Top);

    public static class CounterpartyCalculator
    {
        public const int Top = 10;
        public const int NewDays = 30;

        public static IReadOnlyList<CounterpartyProfile> Compute(IReadOnlyList<JournalEvent> events,
            IReadOnlyList<string> fields, string? amountField = null)
        {
            var profiles = new List<CounterpartyProfile>();
            var end = events.Count == 0 ? DateTime.MinValue : events.Max(e => e.At);

            foreach (var field in fields)
            {
                var present = events.Where(e => !string.IsNullOrEmpty(e.Categories.GetValueOrDefault(field))).ToList();

                if (present.Count == 0)
                {
                    continue;
                }

                var entries = present.GroupBy(e => e.Categories[field], StringComparer.Ordinal).Select(group =>
                {
                    var first = group.Min(e => e.At);
                    double? amount = amountField is null
                        ? null
                        : group.Where(e => e.Measures.ContainsKey(amountField)).Sum(e => e.Measures[amountField]);

                    return new CounterpartyEntry(group.Key, group.Count(), (double)group.Count() / present.Count,
                        amount, first, group.Max(e => e.At), first >= end.AddDays(-NewDays));
                }).ToList();

                profiles.Add(new CounterpartyProfile(field, entries.Count,
                    entries.Sum(entry => entry.Share * entry.Share), entries.Count(entry => entry.New),
                    entries.OrderByDescending(entry => entry.Count).ThenBy(entry => entry.Value,
                        StringComparer.Ordinal).Take(Top).ToList()));
            }

            return profiles;
        }
    }
}

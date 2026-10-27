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
    public sealed record NewValue(string Value, DateTime FirstSeen, long EventId);

    public sealed record NoveltyField(string Field, int Distinct, IReadOnlyList<NewValue> Recent);

    public sealed record NoveltyReport(
        IReadOnlyList<NoveltyField> Fields,
        IReadOnlyDictionary<long, IReadOnlyList<string>> Rows);

    public static class NoveltyCalculator
    {
        public const int WarmUpEvents = 5;
        public const int RecentDays = 7;
        public const int MaximumRecent = 20;

        public static NoveltyReport Compute(IReadOnlyList<JournalEvent> events, IReadOnlyList<string> categories)
        {
            var ordered = events.OrderBy(e => e.At).ThenBy(e => e.Id).ToList();
            var rows = new Dictionary<long, List<string>>();
            var fields = new List<NoveltyField>();
            var horizon = ordered.Count == 0 ? DateTime.MinValue : ordered[^1].At.AddDays(-RecentDays);

            foreach (var field in categories)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var recent = new List<NewValue>();

                for (var index = 0; index < ordered.Count; index++)
                {
                    var evt = ordered[index];

                    if (!evt.Categories.TryGetValue(field, out var value) || string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!seen.Add(value) || index < WarmUpEvents)
                    {
                        continue;
                    }

                    if (!rows.TryGetValue(evt.Id, out var list))
                    {
                        rows[evt.Id] = list = [];
                    }

                    list.Add(field);

                    if (evt.At >= horizon)
                    {
                        recent.Add(new NewValue(value, evt.At, evt.Id));
                    }
                }

                var distinct = ordered.Select(e => e.Categories.GetValueOrDefault(field))
                    .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).Count();

                fields.Add(new NoveltyField(field, distinct,
                    recent.OrderByDescending(item => item.FirstSeen).Take(MaximumRecent).ToList()));
            }

            return new NoveltyReport(fields,
                rows.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value));
        }
    }
}

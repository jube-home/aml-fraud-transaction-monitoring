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


namespace Jube.CaseRisk.Analytics
{
    public static class MyDayCalculator
    {
        public const int Window = 14;

        public static MyDayReport Compute(IReadOnlyCollection<CaseFact> facts, string user, DateTime now, int? goal)
        {
            var today = now.Date;

            var perDay = facts
                .Where(f => f is { Closed: true, ClosedDate: not null } && f.ClosedUser == user)
                .GroupBy(f => f.ClosedDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var days = Enumerable.Range(0, Window)
                .Select(offset => today.AddDays(offset - (Window - 1)))
                .Select(d => new MyDay(d, perDay.GetValueOrDefault(d)))
                .ToList();

            var closedToday = days[^1].Closed;
            var threshold = Math.Max(1, goal ?? 1);

            var streak = 0;
            var cursor = closedToday >= threshold ? Window - 1 : Window - 2;

            while (cursor >= 0 && days[cursor].Closed >= threshold)
            {
                streak++;
                cursor--;
            }

            var earlier = days.Take(Window - 1).Select(d => (double)d.Closed).OrderBy(c => c).ToList();
            var typical = (earlier[(earlier.Count - 1) / 2] + earlier[earlier.Count / 2]) / 2;

            return new MyDayReport(goal, closedToday, streak, days.Max(d => d.Closed), typical, days);
        }
    }
}

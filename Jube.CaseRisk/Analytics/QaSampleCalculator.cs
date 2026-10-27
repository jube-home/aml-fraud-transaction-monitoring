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
    public static class QaSampleCalculator
    {
        public const int MinimumDecided = 10;

        public static QaSampleReport Compute(IReadOnlyCollection<CaseFact> facts)
        {
            var picked = facts.Where(f => f.QaSample).ToList();
            var closed = picked.Where(f => f.Closed).ToList();
            var decided = closed.Where(f => f.OutcomeId is 1 or 2).ToList();
            var confirmed = decided.Count(f => f.OutcomeId == 1);

            var everyone = facts.Where(f => f is { Closed: true } && f.OutcomeId is 1 or 2).ToList();

            return new QaSampleReport(picked.Count, picked.Count(f => !f.Closed), closed.Count, decided.Count,
                confirmed, decided.Count > 0 ? (double)confirmed / decided.Count : null,
                everyone.Count > 0 ? (double)everyone.Count(f => f.OutcomeId == 1) / everyone.Count : null,
                decided.Count >= MinimumDecided);
        }
    }
}

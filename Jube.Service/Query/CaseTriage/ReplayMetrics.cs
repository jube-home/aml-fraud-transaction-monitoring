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

namespace Jube.Service.Query.CaseTriage
{
    public sealed record ReplayMetrics(int Scored, int Unusable, int Agreed, int MissedRisk, int OverEscalation,
        int Inconclusive)
    {
        public double? AgreementRate => Scored == 0 ? null : (double)Agreed / Scored;

        public static ReplayMetrics Compute(IEnumerable<(int Human, int? Recommended)> results)
        {
            int scored = 0, unusable = 0, agreed = 0, missed = 0, over = 0, inconclusive = 0;

            foreach (var (human, recommended) in results)
            {
                if (recommended is null)
                {
                    unusable++;

                    continue;
                }

                scored++;

                if (recommended == human)
                {
                    agreed++;
                }

                if (human == 1 && recommended != 1)
                {
                    missed++;
                }

                if (recommended == 1 && human == 2)
                {
                    over++;
                }

                if (recommended == 0)
                {
                    inconclusive++;
                }
            }

            return new ReplayMetrics(scored, unusable, agreed, missed, over, inconclusive);
        }
    }
}

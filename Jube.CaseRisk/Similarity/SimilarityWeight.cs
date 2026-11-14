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

namespace Jube.CaseRisk.Similarity
{
    public static class SimilarityWeight
    {
        public static double Of(int caseCount, int totalCases, SimilaritySettings settings)
        {
            if (caseCount < settings.MinimumCaseCount || totalCases <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Log((totalCases + 1.0) / (caseCount + 1.0)));
        }
    }
}

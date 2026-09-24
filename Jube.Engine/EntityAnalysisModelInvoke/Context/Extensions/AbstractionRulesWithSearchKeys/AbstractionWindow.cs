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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    using System;
    using Microsoft.VisualBasic;

    public static class AbstractionWindow
    {
        public static DateTime FromDate(string ruleIntervalType, int ruleIntervalValue, string searchKeyTtlInterval,
            int searchKeyTtlIntervalValue, DateTime referenceDate)
        {
            var fromDateModel = DateAndTime.DateAdd(ruleIntervalType, ruleIntervalValue * -1, referenceDate);
            var fromDateSearchKey = DateAndTime.DateAdd(searchKeyTtlInterval, searchKeyTtlIntervalValue * -1,
                referenceDate);

            return fromDateSearchKey > fromDateModel ? fromDateSearchKey : fromDateModel;
        }

        public static bool SearchKeyShortensTheWindow(string ruleIntervalType, int ruleIntervalValue,
            string searchKeyTtlInterval, int searchKeyTtlIntervalValue, DateTime referenceDate)
        {
            return DateAndTime.DateAdd(searchKeyTtlInterval, searchKeyTtlIntervalValue * -1, referenceDate) >
                   DateAndTime.DateAdd(ruleIntervalType, ruleIntervalValue * -1, referenceDate);
        }
    }
}
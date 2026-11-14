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

using Jube.Dto.Options;

namespace Jube.Service.Repository.CaseSuppression
{
    public static class CaseSuppressionLapse
    {
        public static DateTime PendingSince(Data.Poco.CaseSuppression row)
        {
            return row.UpdatedDate ?? row.CreatedDate ?? DateTime.MinValue;
        }

        public static bool IsLapsed(Data.Poco.CaseSuppression row, DateTime now, int lapseDays)
        {
            return row.StateId == (int)CaseSuppressionState.Pending
                   && (row.ExpiryDate <= now || PendingSince(row).AddDays(lapseDays) <= now);
        }

        public static CaseSuppressionStanding Apply(Data.Poco.CaseSuppression row, CaseSuppressionStanding standing,
            DateTime now, int lapseDays)
        {
            return IsLapsed(row, now, lapseDays) ? CaseSuppressionStanding.Expired : standing;
        }
    }
}

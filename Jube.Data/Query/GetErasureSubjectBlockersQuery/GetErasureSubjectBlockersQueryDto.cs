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

namespace Jube.Data.Query.GetErasureSubjectBlockersQuery
{
    public class GetErasureSubjectBlockersQueryDto
    {
        public long ArchiveRows { get; set; }
        public long LegalHoldCases { get; set; }
        public long OpenCases { get; set; }
        public long WithinRetention { get; set; }

        public ErasureRefusalReason? Refusal()
        {
            if (ArchiveRows == 0)
            {
                return ErasureRefusalReason.NothingFound;
            }

            if (LegalHoldCases > 0)
            {
                return ErasureRefusalReason.LegalHold;
            }

            if (OpenCases > 0)
            {
                return ErasureRefusalReason.OpenCase;
            }

            return WithinRetention > 0 ? ErasureRefusalReason.RetentionMinimum : null;
        }
    }
}

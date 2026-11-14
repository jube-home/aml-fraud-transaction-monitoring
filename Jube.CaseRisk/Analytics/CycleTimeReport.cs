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

using MessagePack;

namespace Jube.CaseRisk.Analytics
{
    [MessagePackObject]
    public sealed record CycleTimeReport(
        [property: Key(0)] int Cases,
        [property: Key(1)] DistributionStats FirstView,
        [property: Key(2)] DistributionStats Ownership,
        [property: Key(3)] DistributionStats Resolution,
        [property: Key(4)] DistributionStats WaitingUnviewed,
        [property: Key(5)] int NeverViewed,
        [property: Key(6)] int NeverOwned,
        [property: Key(7)] List<HistogramBucket> FirstViewHistogram,
        [property: Key(8)] List<HistogramBucket> ResolutionHistogram);
}

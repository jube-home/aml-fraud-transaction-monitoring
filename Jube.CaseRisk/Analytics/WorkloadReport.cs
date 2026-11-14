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
    public sealed record PersonWorkload([property: Key(0)] int Closed, [property: Key(1)] DistributionStats Resolution, [property: Key(2)] int HeldNow);

    [MessagePackObject]
    public sealed record TeamWorkload([property: Key(0)] int People, [property: Key(1)] double MedianClosedPerPerson, [property: Key(2)] double MedianResolutionMinutes);

    [MessagePackObject]
    public sealed record WorkloadReport([property: Key(0)] PersonWorkload Mine, [property: Key(1)] TeamWorkload? Team);
}

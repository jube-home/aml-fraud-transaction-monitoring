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
    public sealed record StatusDwell(
        [property: Key(0)] Guid CaseWorkflowStatusGuid,
        [property: Key(1)] int Stays,
        [property: Key(2)] int CurrentlyIn,
        [property: Key(3)] DistributionStats Completed,
        [property: Key(4)] DistributionStats Current,
        [property: Key(5)] double TotalMinutes,
        [property: Key(6)] double ShareOfTime,
        [property: Key(7)] int? MaxDwellMinutes,
        [property: Key(8)] int Breaches);
}
